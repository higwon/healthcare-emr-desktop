$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$allowed = @{
    'HealthNote.Domain' = @()
    'HealthNote.Application' = @('HealthNote.Domain')
    'HealthNote.Infrastructure' = @('HealthNote.Domain', 'HealthNote.Application')
    'HealthNote.Desktop' = @('HealthNote.Domain', 'HealthNote.Application', 'HealthNote.Infrastructure')
    'HealthNote.Api' = @('HealthNote.Domain', 'HealthNote.Application', 'HealthNote.Infrastructure')
}
foreach ($name in $allowed.Keys) {
    [xml]$project = Get-Content -LiteralPath (Join-Path $root "src/$name/$name.csproj") -Raw
    $actual = @($project.Project.ItemGroup.ProjectReference | Where-Object { $_ } |
        ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_.Include) })
    $difference = @($actual | Where-Object { $_ -notin $allowed[$name] })
    if ($difference.Count -or $actual.Count -ne $allowed[$name].Count) { throw "Unexpected project references: $name" }
}
Write-Output 'Project reference boundaries passed.'
