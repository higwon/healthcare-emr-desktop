using System;
using System.Collections.Generic;

namespace HealthNote.Domain.Emr
{
    public sealed class EncounterContent
    {
        public EncounterContent(DateTime visitDate, string chiefComplaint, string? assessment, string? plan)
        {
            if (Validate(visitDate, chiefComplaint, assessment, plan).Count != 0)
                throw new ArgumentException("Invalid encounter content.");
            VisitDate = DateTime.SpecifyKind(visitDate.Date, DateTimeKind.Unspecified);
            ChiefComplaint = chiefComplaint.Trim();
            Assessment = assessment?.Trim() ?? string.Empty;
            Plan = plan?.Trim() ?? string.Empty;
        }

        public DateTime VisitDate { get; }
        public string ChiefComplaint { get; }
        public string Assessment { get; }
        public string Plan { get; }

        public static IReadOnlyDictionary<string, string> Validate(DateTime visitDate, string? chiefComplaint, string? assessment, string? plan)
        {
            var errors = new Dictionary<string, string>();
            if (visitDate == default || visitDate.TimeOfDay != TimeSpan.Zero)
                errors["visitDate"] = "A calendar date is required.";
            string normalizedChief = chiefComplaint?.Trim() ?? string.Empty;
            if (normalizedChief.Length == 0 || normalizedChief.Length > 500)
                errors["chiefComplaint"] = "Required; maximum 500 characters.";
            if ((assessment?.Trim().Length ?? 0) > 4000)
                errors["assessment"] = "Maximum 4000 characters.";
            if ((plan?.Trim().Length ?? 0) > 4000)
                errors["plan"] = "Maximum 4000 characters.";
            return errors;
        }
    }
}
