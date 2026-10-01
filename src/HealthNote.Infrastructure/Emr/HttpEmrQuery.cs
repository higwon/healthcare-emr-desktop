using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using HealthNote.Application.Emr;
using HealthNote.Domain.Emr;

namespace HealthNote.Infrastructure.Emr
{
    public sealed class HttpEmrQuery : IEmrQuery
    {
        private readonly HttpClient _client;
        public HttpEmrQuery(HttpClient client) => _client = client ?? throw new ArgumentNullException(nameof(client));

        public async Task<Page<Patient>> GetPatientsAsync(string search, int page, int pageSize, CancellationToken cancellationToken)
        {
            var dto = await GetAsync<PageResponse<PatientResponse>>(
                "api/v1/patients?search=" + Uri.EscapeDataString(search) + "&" + Paging(page, pageSize), cancellationToken).ConfigureAwait(false);
            ValidatePage(dto, page, pageSize);
            var items = dto.Items.Select(item =>
            {
                if (string.IsNullOrWhiteSpace(item.PatientNumber) || string.IsNullOrWhiteSpace(item.DisplayName))
                    throw new InvalidDataException("Invalid patient contract.");
                return new Patient(Id(item.Id), item.PatientNumber, item.DisplayName);
            }).ToArray();
            return new Page<Patient>(items, dto.TotalCount, page, pageSize);
        }

        public async Task<Page<EncounterSummary>> GetEncountersAsync(Guid patientId, int page, int pageSize, CancellationToken cancellationToken)
        {
            var dto = await GetAsync<PageResponse<EncounterResponse>>(
                $"api/v1/patients/{patientId:D}/encounters?" + Paging(page, pageSize), cancellationToken).ConfigureAwait(false);
            ValidatePage(dto, page, pageSize);
            var items = dto.Items.Select(item =>
            {
                if (Id(item.PatientId) != patientId || item.Version < 1 || string.IsNullOrWhiteSpace(item.ChiefComplaint))
                    throw new InvalidDataException("Invalid encounter context.");
                CalendarDate(item.VisitDate);
                return new EncounterSummary(Id(item.Id), patientId, item.VisitDate, item.ChiefComplaint, item.Version);
            }).ToArray();
            return new Page<EncounterSummary>(items, dto.TotalCount, page, pageSize);
        }

        public async Task<EncounterNote> GetEncounterAsync(Guid patientId, Guid id, CancellationToken cancellationToken)
        {
            var dto = await GetAsync<EncounterDetailResponse>($"api/v1/patients/{patientId:D}/encounters/{id:D}", cancellationToken).ConfigureAwait(false);
            if (Id(dto.Id) != id || Id(dto.PatientId) != patientId || dto.Version < 1 ||
                !DateTime.TryParse(dto.SavedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime saved) ||
                saved.Kind != DateTimeKind.Utc)
                throw new InvalidDataException("Invalid detail context.");
            return new EncounterNote(id, patientId,
                new EncounterContent(CalendarDate(dto.VisitDate), dto.ChiefComplaint, dto.Assessment, dto.Plan), dto.Version, saved);
        }

        private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken) where T : class
        {
            using (HttpResponseMessage response = await _client.GetAsync(path, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                using (Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var serializer = new DataContractJsonSerializer(typeof(T));
                    T? result = serializer.ReadObject(stream) as T;
                    cancellationToken.ThrowIfCancellationRequested();
                    return result ?? throw new InvalidDataException("Missing response.");
                }
            }
        }

        private static string Paging(int page, int size) => "page=" + page.ToString(CultureInfo.InvariantCulture)
            + "&pageSize=" + size.ToString(CultureInfo.InvariantCulture);
        private static Guid Id(string text) => Guid.TryParse(text, out Guid id) && id != Guid.Empty
            ? id : throw new InvalidDataException("Invalid ID.");
        private static DateTime CalendarDate(string text) =>
            DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date)
            ? date : throw new InvalidDataException("Invalid calendar date.");
        private static void ValidatePage<T>(PageResponse<T> dto, int page, int size)
        {
            if (dto.Items == null || dto.Page != page || dto.PageSize != size || dto.Items.Length > size || dto.TotalCount < dto.Items.Length)
                throw new InvalidDataException("Invalid paging context.");
        }

        [DataContract]
        private sealed class PageResponse<T>
        {
            [DataMember(Name = "items", IsRequired = true)] public T[] Items { get; set; } = Array.Empty<T>();
            [DataMember(Name = "totalCount", IsRequired = true)] public long TotalCount { get; set; }
            [DataMember(Name = "page", IsRequired = true)] public int Page { get; set; }
            [DataMember(Name = "pageSize", IsRequired = true)] public int PageSize { get; set; }
        }
        [DataContract]
        private sealed class PatientResponse
        {
            [DataMember(Name = "id", IsRequired = true)] public string Id { get; set; } = string.Empty;
            [DataMember(Name = "patientNumber", IsRequired = true)] public string PatientNumber { get; set; } = string.Empty;
            [DataMember(Name = "displayName", IsRequired = true)] public string DisplayName { get; set; } = string.Empty;
        }
        [DataContract]
        private class EncounterResponse
        {
            [DataMember(Name = "id", IsRequired = true)] public string Id { get; set; } = string.Empty;
            [DataMember(Name = "patientId", IsRequired = true)] public string PatientId { get; set; } = string.Empty;
            [DataMember(Name = "visitDate", IsRequired = true)] public string VisitDate { get; set; } = string.Empty;
            [DataMember(Name = "chiefComplaint", IsRequired = true)] public string ChiefComplaint { get; set; } = string.Empty;
            [DataMember(Name = "version", IsRequired = true)] public int Version { get; set; }
        }
        [DataContract]
        private sealed class EncounterDetailResponse : EncounterResponse
        {
            [DataMember(Name = "assessment", IsRequired = true)] public string Assessment { get; set; } = string.Empty;
            [DataMember(Name = "plan", IsRequired = true)] public string Plan { get; set; } = string.Empty;
            [DataMember(Name = "savedAtUtc", IsRequired = true)] public string SavedAtUtc { get; set; } = string.Empty;
        }
    }
}
