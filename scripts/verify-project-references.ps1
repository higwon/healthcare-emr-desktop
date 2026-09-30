param([string]$RepositoryRoot = (Split-Path $PSScriptRoot -Parent))

$ErrorActionPreference = 'Stop'
$allowed = @{
    'HealthNote.Domain' = @()
    'HealthNote.Application' = @('HealthNote.Domain')
    'HealthNote.Infrastructure' = @('HealthNote.Domain', 'HealthNote.Application')
    'HealthNote.Desktop' = @('HealthNote.Domain', 'HealthNote.Application', 'HealthNote.Infrastructure')
    'HealthNote.Api' = @('HealthNote.Domain', 'HealthNote.Application', 'HealthNote.Infrastructure')
}
foreach ($name in $allowed.Keys) {
    [xml]$project = Get-Content -LiteralPath (Join-Path $RepositoryRoot "src/$name/$name.csproj") -Raw
    $actual = @($project.Project.ItemGroup.ProjectReference | Where-Object { $_ } |
        ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_.Include) })
    $forbidden = @($actual | Where-Object { $_ -notin $allowed[$name] })
    if ($forbidden.Count) { throw "Forbidden project references: $name -> $($forbidden -join ', ')" }
}
Write-Output 'Project reference boundaries passed.'
