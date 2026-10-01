using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HealthNote.Domain.Emr;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HealthNote.Api.Tests
{
    [TestClass]
    public sealed class EmrIntegrationTests
    {
        private const string Patient = "00000000-0000-0000-0000-000000000001";
        private const string Other = "00000000-0000-0000-0000-000000000002";
        private static string PathFor(string id, string patient = Patient) => $"/api/v1/patients/{patient}/encounters/{id}";
        private static object Input(int version, string chief = "합성 주호소", string date = "2026-10-01") =>
            new { expectedVersion = version, visitDate = date, chiefComplaint = chief, assessment = "합성 평가", plan = "합성 계획" };

        [TestMethod]
        public async Task Patients_SearchLiteralPagingAndProblemContract()
        {
            using var api = new ApiProcess();
            await api.StartAsync();
            using var page = await Read(await api.Client.GetAsync("/api/v1/patients?page=2&pageSize=1"), HttpStatusCode.OK);
            Assert.AreEqual(3, page.RootElement.GetProperty("totalCount").GetInt32());
            Assert.AreEqual("DEMO-002", page.RootElement.GetProperty("items")[0].GetProperty("patientNumber").GetString());
            using var search = await Read(await api.Client.GetAsync("/api/v1/patients?search=DEMO-001"), HttpStatusCode.OK);
            Assert.AreEqual(1, search.RootElement.GetProperty("items").GetArrayLength());
            using var literal = await Read(await api.Client.GetAsync("/api/v1/patients?search=%25"), HttpStatusCode.OK);
            Assert.AreEqual(0, literal.RootElement.GetProperty("totalCount").GetInt32());
            foreach (string query in new[] { "page=0", "pageSize=101", "page=invalid", "page=2147483648" })
                await Problem(await api.Client.GetAsync("/api/v1/patients?" + query), HttpStatusCode.BadRequest, "validation");
            using var far = await Read(await api.Client.GetAsync("/api/v1/patients?page=2147483647&pageSize=100"), HttpStatusCode.OK);
            Assert.AreEqual(0, far.RootElement.GetProperty("items").GetArrayLength());
            await Problem(await api.Client.GetAsync("/api/v1/patients/not-a-guid"), HttpStatusCode.BadRequest, "validation");
            await Problem(await api.Client.GetAsync("/api/v1/patients/" + Guid.NewGuid()), HttpStatusCode.NotFound, "not_found");
            using var detail = await Read(await api.Client.GetAsync("/api/v1/patients/" + Patient), HttpStatusCode.OK);
            Assert.AreEqual(Patient, detail.RootElement.GetProperty("id").GetString());
            using var empty = await Read(await api.Client.GetAsync($"/api/v1/patients/{Patient}/encounters"), HttpStatusCode.OK);
            Assert.AreEqual(0, empty.RootElement.GetProperty("totalCount").GetInt32());
        }

        [TestMethod]
        public async Task CreateUpdateReadAndRestart_PreserveCommittedRecordAndSeed()
        {
            using var api = new ApiProcess();
            await api.StartAsync();
            string id = Guid.NewGuid().ToString("D");
            string path = PathFor(id);
            const string chief = "합성 새 기록";
            using var response = await api.Client.PutAsJsonAsync(path, Input(0, "  " + chief + "  "));
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            Assert.AreEqual(path, response.Headers.Location?.OriginalString);
            using var created = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.AreEqual(1, created.RootElement.GetProperty("version").GetInt32());
            Assert.AreEqual(chief, created.RootElement.GetProperty("chiefComplaint").GetString());
            Assert.IsTrue(created.RootElement.GetProperty("savedAtUtc").GetString()?.EndsWith("Z", StringComparison.Ordinal));
            using var initial = await Read(await api.Client.GetAsync(path), HttpStatusCode.OK);
            Assert.AreEqual(created.RootElement.GetRawText(), initial.RootElement.GetRawText());
            using var update = await Read(await api.Client.PutAsJsonAsync(path, Input(1, "수정 합성 기록")), HttpStatusCode.OK);
            Assert.AreEqual(2, update.RootElement.GetProperty("version").GetInt32());
            string saved = update.RootElement.GetRawText();
            await api.RestartAsync();
            using var after = await Read(await api.Client.GetAsync(path), HttpStatusCode.OK);
            Assert.AreEqual(saved, after.RootElement.GetRawText());
            using var patients = await Read(await api.Client.GetAsync("/api/v1/patients"), HttpStatusCode.OK);
            Assert.AreEqual(3, patients.RootElement.GetProperty("totalCount").GetInt32());
            using var list = await Read(await api.Client.GetAsync($"/api/v1/patients/{Patient}/encounters"), HttpStatusCode.OK);
            Assert.AreEqual(1, list.RootElement.GetProperty("totalCount").GetInt32());
            Assert.IsFalse(list.RootElement.GetProperty("items")[0].TryGetProperty("assessment", out _));
        }

        [TestMethod]
        public async Task InvalidBody_DoesNotWriteAndReturnsSafeFieldErrors()
        {
            using var api = new ApiProcess();
            await api.StartAsync();
            string path = PathFor(Guid.NewGuid().ToString("D"));
            using var invalid = await Read(await api.Client.PutAsJsonAsync(path, Input(0, "", "2026-02-30")), HttpStatusCode.BadRequest);
            Assert.IsTrue(invalid.RootElement.GetProperty("errors").TryGetProperty("chiefComplaint", out _));
            Assert.IsTrue(invalid.RootElement.GetProperty("errors").TryGetProperty("visitDate", out _));
            foreach (object input in new object[]
            {
                Input(-1), Input(int.MaxValue), Input(0, new string('x', 501)),
                new { expectedVersion = 0, visitDate = "2026-10-01", chiefComplaint = "合成", assessment = new string('x', 4001) },
                new { visitDate = "2026-10-01", chiefComplaint = "合成" },
                new { expectedVersion = 0, visitDate = "2026-10-01", chiefComplaint = "合成", patientId = Other }
            })
                await Problem(await api.Client.PutAsJsonAsync(path, input), HttpStatusCode.BadRequest, "validation");
            await Problem(await api.Client.PutAsync(path, new StringContent("{", Encoding.UTF8, "application/json")),
                HttpStatusCode.BadRequest, "validation");
            await Problem(await api.Client.GetAsync(path), HttpStatusCode.NotFound, "not_found");
            using var allowed = await Read(await api.Client.PutAsJsonAsync(path,
                new { expectedVersion = 0, visitDate = "2026-10-01", chiefComplaint = new string('x', 500), assessment = new string('x', 4000), plan = (string?)null }),
                HttpStatusCode.Created);
            Assert.AreEqual(string.Empty, allowed.RootElement.GetProperty("plan").GetString());
        }

        [TestMethod]
        public async Task WrongPatientAndStaleVersion_DoNotChangeOriginal()
        {
            using var api = new ApiProcess();
            await api.StartAsync();
            string id = Guid.NewGuid().ToString("D");
            using var created = await Read(await api.Client.PutAsJsonAsync(PathFor(id), Input(0)), HttpStatusCode.Created);
            string snapshot = created.RootElement.GetRawText();
            await Problem(await api.Client.GetAsync(PathFor(id, Other)), HttpStatusCode.NotFound, "not_found");
            await Problem(await api.Client.PutAsJsonAsync(PathFor(id, Other), Input(0)), HttpStatusCode.NotFound, "not_found");
            await Problem(await api.Client.PutAsJsonAsync(PathFor(id), Input(0, "다른 입력")), HttpStatusCode.Conflict, "version_conflict");
            await Problem(await api.Client.PutAsJsonAsync(PathFor(Guid.NewGuid().ToString("D")), Input(3)),
                HttpStatusCode.Conflict, "version_conflict");
            await Problem(await api.Client.PutAsJsonAsync(PathFor(Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D")), Input(0)),
                HttpStatusCode.NotFound, "not_found");
            using var original = await Read(await api.Client.GetAsync(PathFor(id)), HttpStatusCode.OK);
            Assert.AreEqual(snapshot, original.RootElement.GetRawText());
        }

        [TestMethod]
        public async Task ConcurrentWrites_OnlyOneVersionWins()
        {
            using var api = new ApiProcess();
            await api.StartAsync();
            string path = PathFor(Guid.NewGuid().ToString("D"));
            using var created = await Read(await api.Client.PutAsJsonAsync(path, Input(0)), HttpStatusCode.Created);
            HttpResponseMessage[] results = await Task.WhenAll(
                api.Client.PutAsJsonAsync(path, Input(1, "동시 A")),
                api.Client.PutAsJsonAsync(path, Input(1, "동시 B")));
            try
            {
                Assert.AreEqual(1, results.Count(r => r.StatusCode == HttpStatusCode.OK));
                Assert.AreEqual(1, results.Count(r => r.StatusCode == HttpStatusCode.Conflict));
                using var final = await Read(await api.Client.GetAsync(path), HttpStatusCode.OK);
                Assert.AreEqual(2, final.RootElement.GetProperty("version").GetInt32());
                string? chief = final.RootElement.GetProperty("chiefComplaint").GetString();
                Assert.IsTrue(chief == "동시 A" || chief == "동시 B");
            }
            finally { foreach (var result in results) result.Dispose(); }
        }

        [TestMethod]
        public async Task EncounterPaging_HasDeterministicDateAndIdOrdering()
        {
            using var api = new ApiProcess();
            await api.StartAsync();
            string[] ids = { "10000000-0000-0000-0000-000000000003", "10000000-0000-0000-0000-000000000002", "10000000-0000-0000-0000-000000000001" };
            for (int i = 0; i < ids.Length; i++)
            {
                using var saved = await Read(await api.Client.PutAsJsonAsync(PathFor(ids[i]),
                    Input(0, "이력 합성", i == 0 ? "2026-09-01" : "2026-10-01")), HttpStatusCode.Created);
            }
            using var first = await Read(await api.Client.GetAsync($"/api/v1/patients/{Patient}/encounters?pageSize=2"), HttpStatusCode.OK);
            Assert.AreEqual(3, first.RootElement.GetProperty("totalCount").GetInt32());
            Assert.AreEqual(ids[2], first.RootElement.GetProperty("items")[0].GetProperty("id").GetString());
            Assert.AreEqual(ids[1], first.RootElement.GetProperty("items")[1].GetProperty("id").GetString());
            using var second = await Read(await api.Client.GetAsync($"/api/v1/patients/{Patient}/encounters?page=2&pageSize=2"), HttpStatusCode.OK);
            Assert.AreEqual(ids[0], second.RootElement.GetProperty("items")[0].GetProperty("id").GetString());
            await Problem(await api.Client.GetAsync($"/api/v1/patients/{Other}/encounters?pageSize=0"), HttpStatusCode.BadRequest, "validation");
        }

        [TestMethod]
        public void DomainContent_RejectsInvalidFieldsAndKeepsCalendarDateAndUtcInvariant()
        {
            DateTime date = new DateTime(2026, 10, 1);
            Assert.HasCount(0, EncounterContent.Validate(date, "chief", null, null));
            Assert.IsTrue(EncounterContent.Validate(date.AddHours(1), "chief", null, null).ContainsKey("visitDate"));
            Assert.ThrowsExactly<ArgumentException>(() => new EncounterContent(date, "", null, null));
            var content = new EncounterContent(date, " chief ", null, null);
            Assert.AreEqual("chief", content.ChiefComplaint);
            Assert.AreEqual(DateTimeKind.Unspecified, content.VisitDate.Kind);
            Assert.ThrowsExactly<ArgumentException>(() => new EncounterNote(Guid.NewGuid(), Guid.NewGuid(), content, 1, date));
        }

        private static async Task<JsonDocument> Read(HttpResponseMessage response, HttpStatusCode status)
        {
            using (response)
            {
                Assert.AreEqual(status, response.StatusCode);
                return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            }
        }

        private static async Task Problem(HttpResponseMessage response, HttpStatusCode status, string code)
        {
            Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            using var body = await Read(response, status);
            Assert.AreEqual((int)status, body.RootElement.GetProperty("status").GetInt32());
            Assert.AreEqual(code, body.RootElement.GetProperty("code").GetString());
            Assert.IsFalse(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("traceId").GetString()));
            Assert.IsFalse(body.RootElement.TryGetProperty("detail", out _));
        }
    }
}
