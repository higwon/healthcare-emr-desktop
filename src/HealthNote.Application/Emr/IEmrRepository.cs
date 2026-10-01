using System;
using HealthNote.Domain.Emr;

namespace HealthNote.Application.Emr
{
    // Server persistence boundary; no HTTP DTOs, WPF or SQLite dependencies.
    public interface IEmrRepository
    {
        Page<Patient> GetPatients(string search, int page, int pageSize);
        Patient? GetPatient(Guid patientId);
        Page<EncounterNote> GetEncounters(Guid patientId, int page, int pageSize);
        EncounterNote? GetEncounter(Guid patientId, Guid id);
        SaveEncounterResult Save(Guid patientId, Guid id, EncounterContent content, int expectedVersion, DateTime savedAtUtc);
    }
}
