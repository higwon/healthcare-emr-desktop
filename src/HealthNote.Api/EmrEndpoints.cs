using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HealthNote.Api.Contracts;
using HealthNote.Application.Emr;
using HealthNote.Domain.Emr;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HealthNote.Api
{
    internal static class EmrEndpoints
    {
        public static void MapEmr(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/api/v1/patients", (HttpContext context, IEmrRepository repository) =>
            {
                if (!TryPage(context, out int page, out int size)) return Problem(context, 400, "validation", "Invalid paging.");
                string search = context.Request.Query["search"].ToString().Trim();
                if (search.Length > 100) return Problem(context, 400, "validation", "Search must not exceed 100 characters.");
                var result = repository.GetPatients(search, page, size);
                return Results.Ok(new { items = result.Items.Select(PatientDto.From), result.TotalCount, page, pageSize = size });
            });

            endpoints.MapGet("/api/v1/patients/{patientId}", (HttpContext context, string patientId, IEmrRepository repository) =>
            {
                if (!TryId(patientId, out Guid patient)) return Problem(context, 400, "validation", "Invalid patient ID.");
                Patient? found = repository.GetPatient(patient);
                return found == null ? Problem(context, 404, "not_found", "Patient not found.") : Results.Ok(PatientDto.From(found));
            });

            endpoints.MapGet("/api/v1/patients/{patientId}/encounters", (HttpContext context, string patientId, IEmrRepository repository) =>
            {
                if (!TryId(patientId, out Guid patient) || !TryPage(context, out int page, out int size))
                    return Problem(context, 400, "validation", "Invalid ID or paging.");
                if (repository.GetPatient(patient) == null) return Problem(context, 404, "not_found", "Patient not found.");
                var result = repository.GetEncounters(patient, page, size);
                return Results.Ok(new
                {
                    items = result.Items.Select(note => new { note.Id, note.PatientId,
                        visitDate = note.Content.VisitDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        chiefComplaint = note.Content.ChiefComplaint, note.Version }),
                    result.TotalCount, page, pageSize = size
                });
            });

            endpoints.MapGet("/api/v1/patients/{patientId}/encounters/{id}", (HttpContext context, string patientId, string id, IEmrRepository repository) =>
            {
                if (!TryId(patientId, out Guid patient) || !TryId(id, out Guid encounter))
                    return Problem(context, 400, "validation", "Invalid ID.");
                EncounterNote? found = repository.GetEncounter(patient, encounter);
                return found == null ? Problem(context, 404, "not_found", "Encounter not found.") : Results.Ok(EncounterDto.From(found));
            });

            endpoints.MapPut("/api/v1/patients/{patientId}/encounters/{id}",
                (HttpContext context, string patientId, string id, SaveEncounterRequest request, IEmrRepository repository) =>
            {
                if (!TryId(patientId, out Guid patient) || !TryId(id, out Guid encounter))
                    return Problem(context, 400, "validation", "Invalid ID.");
                var errors = new Dictionary<string, string[]>();
                bool dateValid = DateTime.TryParseExact(request.VisitDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime date);
                if (!dateValid) errors["visitDate"] = new[] { "Use YYYY-MM-DD." };
                foreach (var error in EncounterContent.Validate(date, request.ChiefComplaint, request.Assessment, request.Plan))
                    errors[error.Key] = new[] { error.Value };
                if (!request.ExpectedVersion.HasValue || request.ExpectedVersion < 0 || request.ExpectedVersion >= int.MaxValue)
                    errors["expectedVersion"] = new[] { "Required non-negative integer below int.MaxValue." };
                if (errors.Count > 0) return Problem(context, 400, "validation", "Invalid encounter fields.", errors);
                var content = new EncounterContent(date, request.ChiefComplaint ?? string.Empty, request.Assessment, request.Plan);
                var result = repository.Save(patient, encounter, content, request.ExpectedVersion.GetValueOrDefault(), DateTime.UtcNow);
                return result.Status switch
                {
                    SaveEncounterStatus.NotFound => Problem(context, 404, "not_found", "Patient or encounter not found."),
                    SaveEncounterStatus.Conflict => Problem(context, 409, "version_conflict", "Read the current record before retrying."),
                    SaveEncounterStatus.Created when result.Note != null => Results.Created(
                        $"/api/v1/patients/{patient:D}/encounters/{encounter:D}", EncounterDto.From(result.Note)),
                    SaveEncounterStatus.Updated when result.Note != null => Results.Ok(EncounterDto.From(result.Note)),
                    _ => throw new InvalidOperationException("Invalid persistence result.")
                };
            });
        }

        internal static IResult Problem(HttpContext context, int status, string code, string title, IDictionary<string, string[]>? errors = null)
        {
            var extensions = new Dictionary<string, object?> { ["code"] = code, ["traceId"] = context.TraceIdentifier };
            if (errors != null) extensions["errors"] = errors;
            return Results.Problem(statusCode: status, title: title, type: "urn:healthnote:error:" + code, extensions: extensions);
        }

        private static bool TryId(string text, out Guid id) => Guid.TryParse(text, out id) && id != Guid.Empty;

        private static bool TryPage(HttpContext context, out int page, out int size)
        {
            var query = context.Request.Query;
            page = 1;
            size = 20;
            return (!query.ContainsKey("page") || int.TryParse(query["page"], out page))
                && (!query.ContainsKey("pageSize") || int.TryParse(query["pageSize"], out size))
                && page >= 1 && size >= 1 && size <= 100;
        }
    }
}
