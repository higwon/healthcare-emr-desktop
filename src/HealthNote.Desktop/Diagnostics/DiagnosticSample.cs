using System;

namespace HealthNote.Desktop.Diagnostics
{
    public readonly struct DiagnosticSample
    {
        public DiagnosticSample(DiagnosticKind kind, long scopeId, TimeSpan elapsed)
        {
            Kind = kind;
            ScopeId = scopeId;
            Elapsed = elapsed;
        }

        public DiagnosticKind Kind { get; }
        public long ScopeId { get; }
        public TimeSpan Elapsed { get; }
    }
}
