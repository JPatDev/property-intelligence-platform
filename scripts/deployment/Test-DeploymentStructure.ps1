[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
foreach ($file in Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.ps1') {
    $tokens = $null
    $parseErrors = $null
    $null = [Management.Automation.Language.Parser]::ParseFile($file.FullName, [ref]$tokens, [ref]$parseErrors)
    if ($parseErrors.Count) { throw "$($file.Name): $($parseErrors -join '; ')" }
}
foreach ($environment in 'dev', 'test', 'staging', 'production') {
    $config = Get-Content -LiteralPath (Join-Path $root "infrastructure/parameters/$environment.json") -Raw | ConvertFrom-Json
    foreach ($section in 'foundation', 'data', 'web', 'runtime', 'application') {
        if (!$config.PSObject.Properties[$section]) { throw "$environment is missing section $section" }
        if (!(Test-Path -LiteralPath (Join-Path $root "infrastructure/templates/$section.yml"))) { throw "Missing $section template" }
    }
}
[xml]$solution = Get-Content -LiteralPath (Join-Path $root 'PropertyIntelligencePlatform.slnx') -Raw
foreach ($project in $solution.SelectNodes('//Project')) {
    $path = Join-Path $root $project.Path
    if (!(Test-Path -LiteralPath $path)) { throw "Solution project not found: $path" }
    [xml]$projectXml = Get-Content -LiteralPath $path -Raw
    foreach ($reference in $projectXml.SelectNodes('//ProjectReference')) {
        if (!(Test-Path -LiteralPath (Join-Path (Split-Path $path) $reference.Include))) {
            throw "Broken project reference in $path : $($reference.Include)"
        }
    }
}
Write-Host 'Deployment script syntax, parameter files, solution paths, and project references are valid.'
