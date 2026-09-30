using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace HealthNote.Desktop.Diagnostics
{
    public sealed class ClientDiagnostics
    {
        private readonly Action<DiagnosticSample> sink;
        private long nextScope;

        public ClientDiagnostics(Action<DiagnosticSample>? sink = null)
        {
            this.sink = sink ?? WriteTrace;
        }

        public long OpenScope()
        {
            long id = Interlocked.Increment(ref nextScope);
            Record(DiagnosticKind.ScopeOpened, id, TimeSpan.Zero);
            return id;
        }

        public void Record(DiagnosticKind kind, long scopeId, TimeSpan elapsed)
        {
            try
            {
                sink(new DiagnosticSample(kind, scopeId, elapsed));
            }
            catch (Exception)
            {
                // A diagnostic sink must never interrupt a command or scope cleanup.
            }
        }

        private static void WriteTrace(DiagnosticSample sample)
        {
            Trace.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "client kind={0} scope={1} elapsedMs={2:F3}",
                sample.Kind, sample.ScopeId, sample.Elapsed.TotalMilliseconds));
        }
    }
}
