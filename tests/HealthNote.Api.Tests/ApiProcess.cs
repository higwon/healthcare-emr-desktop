using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HealthNote.Api.Tests
{
    internal sealed class ApiProcess : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "HealthNote-emr-" + Guid.NewGuid().ToString("N"));
        private Process? _process;
        public HttpClient Client { get; private set; } = new HttpClient();

        public async Task StartAsync()
        {
            Directory.CreateDirectory(_directory);
            string api = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/HealthNote.Api/bin/Release/net10.0/HealthNote.Api.dll"));
            var start = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = _directory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            start.ArgumentList.Add(api);
            start.ArgumentList.Add("--Emr:Port=0");
            start.ArgumentList.Add("--Emr:DatabasePath=" + Path.Combine(_directory, "emr.db"));
            var listening = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _process = new Process { StartInfo = start, EnableRaisingEvents = true };
            _process.OutputDataReceived += (_, e) =>
            {
                if (e.Data == null) return;
                Match match = Regex.Match(e.Data, @"Now listening on: (http://127\.0\.0\.1:\d+)");
                if (match.Success) listening.TrySetResult(match.Groups[1].Value);
            };
            _process.ErrorDataReceived += (_, _) => { };
            _process.Exited += (_, _) => listening.TrySetException(new InvalidOperationException("API exited before startup."));
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            string address = await listening.Task.WaitAsync(TimeSpan.FromSeconds(30));
            Client.Dispose();
            Client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(15) };
            using var ready = await Client.GetAsync("/api/v1/health");
            ready.EnsureSuccessStatusCode();
        }

        public async Task RestartAsync()
        {
            Stop();
            await StartAsync();
        }

        private void Stop()
        {
            Client.Dispose();
            if (_process == null) return;
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(5000);
            }
            _process.Dispose();
            _process = null;
        }

        public void Dispose()
        {
            Stop();
            // Only this fixture's randomly named, owned temporary directory is removed.
            if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        }
    }
}
