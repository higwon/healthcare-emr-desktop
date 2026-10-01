using System;

namespace HealthNote.Domain.Emr
{
    public sealed class Patient
    {
        public Patient(Guid id, string patientNumber, string displayName)
        {
            if (id == Guid.Empty) throw new ArgumentException("Patient ID is required.", nameof(id));
            Id = id;
            PatientNumber = patientNumber;
            DisplayName = displayName;
        }

        public Guid Id { get; }
        public string PatientNumber { get; }
        public string DisplayName { get; }
    }
}
