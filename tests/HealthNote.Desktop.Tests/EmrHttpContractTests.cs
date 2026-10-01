using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HealthNote.Infrastructure.Emr;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthNote.Desktop.Tests
{
    [TestClass]
    public sealed class EmrHttpContractTests
    {
        [TestMethod]
        public async Task AdapterRejectsWrongPageAndPatientContext()
        {
            using (var client = new HttpClient(new JsonHandler("{\"items\":[],\"totalCount\":0,\"page\":2,\"pageSize\":20}"))
                { BaseAddress = new Uri("http://127.0.0.1:5078/") })
            {
                var query = new HttpEmrQuery(client);
                await Assert.ThrowsExactlyAsync<InvalidDataException>(() => query.GetPatientsAsync("", 1, 20, CancellationToken.None));
            }
            string foreign = "{\"items\":[{\"id\":\"00000000-0000-0000-0000-000000000003\",\"patientId\":\"00000000-0000-0000-0000-000000000002\",\"visitDate\":\"2026-10-01\",\"chiefComplaint\":\"synthetic\",\"version\":1}],\"totalCount\":1,\"page\":1,\"pageSize\":20}";
            using (var client = new HttpClient(new JsonHandler(foreign)) { BaseAddress = new Uri("http://127.0.0.1:5078/") })
            {
                var query = new HttpEmrQuery(client);
                await Assert.ThrowsExactlyAsync<InvalidDataException>(() => query.GetEncountersAsync(EmrQueryStub.A.Id, 1, 20, CancellationToken.None));
            }
        }

        [TestMethod]
        public async Task AdapterPropagatesCancellationAndHttpFailure()
        {
            using (var source = new CancellationTokenSource())
            using (var client = new HttpClient(new JsonHandler("{}")) { BaseAddress = new Uri("http://127.0.0.1:5078/") })
            {
                source.Cancel();
                await Assert.ThrowsAsync<OperationCanceledException>(() =>
                    new HttpEmrQuery(client).GetPatientsAsync("", 1, 20, source.Token));
            }
            using (var client = new HttpClient(new JsonHandler("{}", HttpStatusCode.ServiceUnavailable)) { BaseAddress = new Uri("http://127.0.0.1:5078/") })
            {
                await Assert.ThrowsExactlyAsync<HttpRequestException>(() =>
                    new HttpEmrQuery(client).GetPatientsAsync("", 1, 20, CancellationToken.None));
            }
        }

        private sealed class JsonHandler : HttpMessageHandler
        {
            private readonly string _json;
            private readonly HttpStatusCode _status;
            public JsonHandler(string json, HttpStatusCode status = HttpStatusCode.OK) { _json = json; _status = status; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                return Task.FromResult(new HttpResponseMessage(_status) { Content = new StringContent(_json, Encoding.UTF8, "application/json") });
            }
        }
    }
}
