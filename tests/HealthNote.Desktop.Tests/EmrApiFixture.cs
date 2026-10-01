using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HealthNote.Desktop.Tests
{
    internal sealed class EmrApiFixture : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "HealthNote-wpf-emr-" + Guid.NewGuid().ToString("N"));
        private Process? _process;
        public HttpClient Client { get; private set; } = new HttpClient();
        public string PatientId => "00000000-0000-0000-0000-000000000001";
        public string RecordId { get; } = Guid.NewGuid().ToString("D");
        public async Task StartAsync()
        {
            Directory.CreateDirectory(_directory);
            string api = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/HealthNote.Api/bin/Release/net10.0/HealthNote.Api.dll"));
            var ready = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _process = new Process { StartInfo = new ProcessStartInfo("dotnet",
                "\"" + api + "\" --Emr:Port=0 --Emr:DatabasePath=\"" + Path.Combine(_directory, "emr.db") + "\"")
            {
                WorkingDirectory = _directory, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            }, EnableRaisingEvents = true };
            _process.OutputDataReceived += (_, e) =>
            {
                Match match = Regex.Match(e.Data ?? string.Empty, @"Now listening on: (http://127\.0\.0\.1:\d+)");
                if (match.Success) ready.TrySetResult(match.Groups[1].Value);
            };
            _process.ErrorDataReceived += (_, _) => { };
            _process.Exited += (_, _) => ready.TrySetException(new InvalidOperationException("API startup failed."));
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            await TestCompletion.WaitAsync(ready.Task).ConfigureAwait(false);
            Client.Dispose();
            Client = new HttpClient { BaseAddress = new Uri(await ready.Task.ConfigureAwait(false)), Timeout = TimeSpan.FromSeconds(10) };
            string body = "{\"expectedVersion\":0,\"visitDate\":\"2026-10-01\",\"chiefComplaint\":\"합성 진료 기록 — 긴 주호소와 진료일, 환자 문맥을 함께 확인하는 조회 예시\",\"assessment\":\"합성 평가 기록입니다.\",\"plan\":\"합성 계획 기록입니다.\"}";
            using (var response = await Client.PutAsync("api/v1/patients/" + PatientId + "/encounters/" + RecordId,
                new StringContent(body, Encoding.UTF8, "application/json")).ConfigureAwait(false))
                response.EnsureSuccessStatusCode();
        }
        public void Dispose()
        {
            Client.Dispose();
            if (_process != null)
            {
                if (!_process.HasExited) { _process.Kill(); _process.WaitForExit(5000); }
                _process.Dispose();
            }
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }
    }
}
