using System;
using System.Globalization;
using HealthNote.Domain.Emr;

namespace HealthNote.Api.Contracts
{
    public sealed record EncounterDto(Guid Id, Guid PatientId, string VisitDate, string ChiefComplaint,
        string Assessment, string Plan, int Version, DateTime SavedAtUtc)
    {
        public static EncounterDto From(EncounterNote note) => new EncounterDto(note.Id, note.PatientId,
            note.Content.VisitDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            note.Content.ChiefComplaint, note.Content.Assessment, note.Content.Plan, note.Version, note.SavedAtUtc);
    }
}
