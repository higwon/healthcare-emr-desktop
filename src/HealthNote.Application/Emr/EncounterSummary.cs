using System;

namespace HealthNote.Application.Emr
{
    public sealed class EncounterSummary
    {
        public EncounterSummary(Guid id, Guid patientId, string visitDate, string chiefComplaint, int version)
        {
            Id = id;
            PatientId = patientId;
            VisitDate = visitDate;
            ChiefComplaint = chiefComplaint;
            Version = version;
        }
        public Guid Id { get; }
        public Guid PatientId { get; }
        public string VisitDate { get; }
        public string ChiefComplaint { get; }
        public int Version { get; }
        public string AccessibleName => VisitDate + " · " + ChiefComplaint;
    }
}
