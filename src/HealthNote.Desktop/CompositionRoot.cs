using System;
using System.Net.Http;
using System.Windows.Threading;
using HealthNote.Desktop.Configuration;
using HealthNote.Desktop.Diagnostics;
using HealthNote.Desktop.Threading;
using HealthNote.Desktop.ViewModels;
using HealthNote.Infrastructure;
using HealthNote.Infrastructure.Emr;

namespace HealthNote.Desktop
{
    public sealed class CompositionRoot : IDisposable
    {
        private readonly HttpClient client;
        private bool disposed;

        public CompositionRoot(DemoOptions options, Dispatcher dispatcher)
        {
            client = new HttpClient
            {
                BaseAddress = options.ApiBaseUri,
                Timeout = TimeSpan.FromSeconds(10)
            };
            ClientReadinessQuery query = new ClientReadinessQuery(client);
            WpfUiDispatcher ui = new WpfUiDispatcher(dispatcher);
            ClientDiagnostics diagnostics = new ClientDiagnostics();
            Shell = new ShellViewModel(() => new ConnectionViewModel(query, ui, diagnostics),
                new EmrWorkspaceViewModel(new HttpEmrQuery(client), ui));
        }

        public ShellViewModel Shell { get; }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            Shell.Dispose();
            client.Dispose();
        }
    }
}
