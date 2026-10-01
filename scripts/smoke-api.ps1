param([int]$Port = 5078)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$api = Join-Path $root 'src/HealthNote.Api/bin/Release/net10.0/HealthNote.Api.dll'
$artifacts = Join-Path $root 'artifacts'
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
if ($Port -ne 5078) { throw 'Foundation host uses fixed loopback port 5078.' }
# Verify the port is free; never stop a pre-existing listener.
$probe = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, $Port)
try { $probe.Start() } finally { $probe.Stop() }
$database = Join-Path $artifacts ('api-smoke-' + [Guid]::NewGuid().ToString('N') + '.db')
$process = Start-Process -FilePath 'dotnet' -ArgumentList @('"' + $api + '"', '--Emr:DatabasePath="' + $database + '"') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $artifacts 'api-smoke.stdout.log') -RedirectStandardError (Join-Path $artifacts 'api-smoke.stderr.log')
try {
    $deadline = [DateTime]::UtcNow.AddSeconds(20)
    $ready = $false
    do {
        if ($process.HasExited) { throw "API exited before readiness: $($process.ExitCode)" }
        try {
            $response = Invoke-RestMethod -Uri "http://127.0.0.1:$Port/api/v1/health" -TimeoutSec 2
            if ($response.mode -ne 'demo' -or $response.version -ne 'v1' -or $response.ready -ne $true) {
                throw 'Unexpected readiness contract.'
            }
            $ready = $true
        } catch {
            if ($_.Exception.Message -eq 'Unexpected readiness contract.') { throw }
            Start-Sleep -Milliseconds 100
        }
    } while (-not $ready -and [DateTime]::UtcNow -lt $deadline)
    if (-not $ready) { throw 'API readiness timed out.' }
    $response | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $artifacts 'api-readiness.json') -Encoding utf8
    Write-Output 'API loopback readiness smoke passed.'
} finally {
    if (-not $process.HasExited) { Stop-Process -Id $process.Id }
    $process.Dispose()
}
