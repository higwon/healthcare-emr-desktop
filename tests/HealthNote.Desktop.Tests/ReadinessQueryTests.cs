using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HealthNote.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthNote.Desktop.Tests
{
    [TestClass]
    public sealed class ReadinessQueryTests
    {
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task ValidContract_MapsReadyAndUsesVersionedRelativePath(bool ready)
        {
            using (HttpClient client = CreateClient(new StubHandler((request, _) =>
            {
                Assert.AreEqual("http://127.0.0.1:5078/api/v1/health", request.RequestUri?.AbsoluteUri);
                return Task.FromResult(Response("{\"mode\":\"demo\",\"version\":\"v1\",\"ready\":" + ready.ToString().ToLowerInvariant() + "}"));
            })))
            {
                Assert.AreEqual(ready, await new ClientReadinessQuery(client).IsReadyAsync(CancellationToken.None));
            }
        }

        [TestMethod]
        [DataRow("{\"mode\":\"production\",\"version\":\"v1\",\"ready\":true}")]
        [DataRow("{\"mode\":\"demo\",\"version\":\"v2\",\"ready\":true}")]
        public async Task WrongModeOrVersion_IsRejected(string json)
        {
            using (HttpClient client = CreateClient(new StubHandler((_, _) => Task.FromResult(Response(json)))))
            {
                await Assert.ThrowsExactlyAsync<InvalidDataException>(() =>
                    new ClientReadinessQuery(client).IsReadyAsync(CancellationToken.None));
            }
        }

        [TestMethod]
        [DataRow("{\"mode\":\"demo\",\"version\":\"v1\"}")]
        [DataRow("invalid json")]
        public async Task MissingFieldOrMalformedPayload_IsRejected(string json)
        {
            using (HttpClient client = CreateClient(new StubHandler((_, _) => Task.FromResult(Response(json)))))
            {
                await Assert.ThrowsExactlyAsync<SerializationException>(() =>
                    new ClientReadinessQuery(client).IsReadyAsync(CancellationToken.None));
            }
        }

        [TestMethod]
        public async Task HttpFailure_IsNotReportedReady()
        {
            using (HttpClient client = CreateClient(new StubHandler((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)))))
            {
                await Assert.ThrowsExactlyAsync<HttpRequestException>(() =>
                    new ClientReadinessQuery(client).IsReadyAsync(CancellationToken.None));
            }
        }

        [TestMethod]
        public async Task Cancellation_PropagatesToHttpHandler()
        {
            TaskCompletionSource<bool> entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (CancellationTokenSource cancellation = new CancellationTokenSource())
            using (HttpClient client = CreateClient(new StubHandler(async (_, token) =>
            {
                entered.SetResult(true);
                await Task.Delay(Timeout.Infinite, token);
                return Response("{}");
            })))
            {
                Task<bool> query = new ClientReadinessQuery(client).IsReadyAsync(cancellation.Token);
                await TestCompletion.WaitAsync(entered.Task);
                cancellation.Cancel();
                await Assert.ThrowsAsync<OperationCanceledException>(() => TestCompletion.WaitAsync(query));
            }
        }

        private static HttpClient CreateClient(HttpMessageHandler handler) =>
            new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:5078/") };

        private static HttpResponseMessage Response(string json) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send;
            public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
            {
                this.send = send;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                send(request, cancellationToken);
        }
    }
}
