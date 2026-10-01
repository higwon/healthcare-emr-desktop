using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HealthNote.Application.Emr;
using HealthNote.Domain.Emr;

namespace HealthNote.Desktop.Tests
{
    internal sealed class EmrQueryStub : IEmrQuery
    {
        internal static readonly Patient A = new Patient(Guid.Parse("00000000-0000-0000-0000-000000000001"), "DEMO-001", "합성환자 가");
        internal static readonly Patient B = new Patient(Guid.Parse("00000000-0000-0000-0000-000000000002"), "DEMO-002", "합성환자 나");
        public Func<string, int, int, CancellationToken, Task<Page<Patient>>> Patients { get; set; } =
            (_, page, size, _) => Task.FromResult(new Page<Patient>(new[] { A, B }, 2, page, size));
        public Func<Guid, int, int, CancellationToken, Task<Page<EncounterSummary>>> History { get; set; } =
            (_, page, size, _) => Task.FromResult(new Page<EncounterSummary>(Array.Empty<EncounterSummary>(), 0, page, size));
        public Func<Guid, Guid, CancellationToken, Task<EncounterNote>> Detail { get; set; } =
            (_, _, _) => Task.FromException<EncounterNote>(new InvalidOperationException("No detail fixture."));
        public Task<Page<Patient>> GetPatientsAsync(string search, int page, int size, CancellationToken token) => Patients(search, page, size, token);
        public Task<Page<EncounterSummary>> GetEncountersAsync(Guid patient, int page, int size, CancellationToken token) => History(patient, page, size, token);
        public Task<EncounterNote> GetEncounterAsync(Guid patient, Guid id, CancellationToken token) => Detail(patient, id, token);
        internal static EncounterSummary Record(Patient patient) => new EncounterSummary(Guid.NewGuid(), patient.Id, "2026-10-01", "합성 주호소", 1);
        internal static EncounterNote Note(EncounterSummary summary) => new EncounterNote(summary.Id, summary.PatientId,
            new EncounterContent(new DateTime(2026, 10, 1), summary.ChiefComplaint, "합성 평가", "합성 계획"), 1, new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
    }
}
