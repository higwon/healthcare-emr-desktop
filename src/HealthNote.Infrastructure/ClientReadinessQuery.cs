using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using HealthNote.Application;

namespace HealthNote.Infrastructure
{
    public sealed class ClientReadinessQuery : IClientReadinessQuery
    {
        private readonly HttpClient client;

        public ClientReadinessQuery(HttpClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public async Task<bool> IsReadyAsync(CancellationToken cancellationToken)
        {
            using (HttpResponseMessage response = await client.GetAsync("api/v1/health", cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                using (Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ReadinessResponse));
                    ReadinessResponse? readiness = serializer.ReadObject(stream) as ReadinessResponse;
                    if (readiness == null || readiness.Mode != "demo" || readiness.Version != "v1")
                    {
                        throw new InvalidDataException("Unsupported readiness contract.");
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    return readiness.Ready;
                }
            }
        }

        [DataContract]
        private sealed class ReadinessResponse
        {
            [DataMember(Name = "mode", IsRequired = true)]
            public string? Mode { get; set; }

            [DataMember(Name = "version", IsRequired = true)]
            public string? Version { get; set; }

            [DataMember(Name = "ready", IsRequired = true)]
            public bool Ready { get; set; }
        }
    }
}
