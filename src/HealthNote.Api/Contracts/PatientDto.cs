using System;
using HealthNote.Domain.Emr;

namespace HealthNote.Api.Contracts
{
    public sealed record PatientDto(Guid Id, string PatientNumber, string DisplayName)
    {
        public static PatientDto From(Patient patient) => new PatientDto(patient.Id, patient.PatientNumber, patient.DisplayName);
    }
}
