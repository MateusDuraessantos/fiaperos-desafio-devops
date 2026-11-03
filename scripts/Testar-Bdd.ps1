param(
    [ValidateSet('sqlite', 'sqlserver')][string]$Provedor = 'sqlite',
    [switch]$SemBuild
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$resultsRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'test-results'))
$results = [IO.Path]::GetFullPath((Join-Path $resultsRoot $Provedor))
$environmentNames = @('BDD_SQLSERVER_CONNECTION', 'BDD_RESULTS_DIRECTORY',
    'REQNROLL_FORMATTERS_HTML', 'REQNROLL_FORMATTERS_MESSAGE',
    'DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE', 'MSBuildEnableWorkloadResolver')
$savedEnvironment = @{}
foreach ($name in $environmentNames) {
    $savedEnvironment[$name] = [Environment]::GetEnvironmentVariable($name)
}

function Invoke-Dotnet([string[]]$Arguments) {
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet retornou codigo $LASTEXITCODE." }
}

function Test-ApiCoverage {
    $suites = @{}
    foreach ($suite in @('unit', 'bdd')) {
        [xml]$trx = Get-Content -LiteralPath (Join-Path $results "$suite.trx") -Raw
        $counters = $trx.SelectSingleNode("//*[local-name()='Counters']")
        if ($null -eq $counters) { throw "Contadores ausentes: $suite." }
        $counts = @{}
        foreach ($attribute in $counters.Attributes) { $counts[$attribute.Name] = [int]$attribute.Value }
        if ($counts.total -eq 0 -or $counts.passed -ne $counts.total) {
            throw "Suite $suite vazia, com falhas ou com testes ignorados."
        }
        if ($suite -eq 'bdd' -and $counts.passed -lt 3) { throw 'Sao exigidos pelo menos tres casos BDD.' }
        $suites[$suite] = $counts
    }

    $traces = @(Get-ChildItem -LiteralPath (Join-Path $results 'http') -Filter '*.json' -File |
        ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json })
    if ($traces.Count -ne $suites.bdd.passed -or @($traces | Where-Object { !$_.passed }).Count -gt 0) {
        throw 'Os registros HTTP nao correspondem aos resultados BDD.'
    }
    $openApi = Get-Content -LiteralPath (Join-Path $results 'openapi.json') -Raw | ConvertFrom-Json
    $operations = @(foreach ($path in $openApi.paths.PSObject.Properties) {
        foreach ($method in $path.Value.PSObject.Properties.Name) {
            if ($method -in @('get', 'post', 'put', 'delete', 'patch')) {
                $parts = foreach ($part in $path.Name.Split('/')) {
                    if ($part.StartsWith('{') -and $part.EndsWith('}')) { '\d+' }
                    else { [regex]::Escape($part) }
                }
                [pscustomobject]@{ Method = $method.ToUpperInvariant(); Path = $path.Name;
                    Pattern = '^' + ($parts -join '/') + '$' }
            }
        }
    })
    if ($operations.Count -eq 0) { throw 'O OpenAPI nao declarou operacoes HTTP.' }
    $observed = @{}
    foreach ($trace in $traces) {
        foreach ($exchange in $trace.exchanges) {
            if (!$exchange.statusValidated -or !($exchange.contract -or $exchange.emptyBodyValidated)) {
                throw "Resposta sem assercao de status/contrato/corpo vazio: $($exchange.method) $($exchange.path)."
            }
            $path = $exchange.path.Split('?')[0]
            foreach ($operation in $operations) {
                if ($exchange.method -eq $operation.Method -and $path -match $operation.Pattern) {
                    $key = "$($operation.Method) $($operation.Path)"
                    if (!$observed.ContainsKey($key)) { $observed[$key] = [Collections.Generic.HashSet[int]]::new() }
                    [void]$observed[$key].Add([int]$exchange.status)
                }
            }
        }
    }
    $coverage = @(foreach ($operation in ($operations | Sort-Object Method, Path)) {
        $key = "$($operation.Method) $($operation.Path)"
        $statuses = @($observed[$key] | Sort-Object)
        if (@($statuses | Where-Object { $_ -ge 200 -and $_ -lt 300 }).Count -eq 0) {
            throw "Operacao sem teste HTTP positivo: $key."
        }
        [ordered]@{ method = $operation.Method; path = $operation.Path; statuses = $statuses }
    })
    $summary = [ordered]@{
        provider = @($traces.provider | Sort-Object -Unique)
        startedUtc = ($traces.startedUtc | Sort-Object | Select-Object -First 1)
        finishedUtc = ($traces.finishedUtc | Sort-Object | Select-Object -Last 1)
        suites = $suites
        operationsCovered = $operations.Count
        operations = $coverage
    }
    $summary | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $results 'summary.json') -Encoding UTF8
    $lines = @('# Resultado dos testes', '', "Banco: $($summary.provider -join ', ')",
        "Unitarios: $($suites.unit.passed) aprovados.", "BDD: $($suites.bdd.passed) aprovados.",
        "Operacoes HTTP cobertas: $($operations.Count)/$($operations.Count).", '',
        '| Metodo | Rota | Status observados |', '|---|---|---|')
    $lines += foreach ($operation in $coverage) {
        "| $($operation.method) | $($operation.path) | $($operation.statuses -join ', ') |"
    }
    $lines | Set-Content -LiteralPath (Join-Path $results 'summary.md') -Encoding UTF8
    $lines | ForEach-Object { Write-Host $_ }
    if ($env:GITHUB_STEP_SUMMARY) { $lines | Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Encoding UTF8 }
}

Push-Location $projectRoot
try {
    if ($Provedor -eq 'sqlserver' -and [string]::IsNullOrWhiteSpace($env:BDD_SQLSERVER_CONNECTION)) {
        throw 'Configure BDD_SQLSERVER_CONNECTION para uma instancia exclusiva de testes.'
    }
    if ($Provedor -eq 'sqlite') { [Environment]::SetEnvironmentVariable('BDD_SQLSERVER_CONNECTION', $null) }
    if ([IO.Path]::GetDirectoryName($results) -ne $resultsRoot) { throw 'Diretorio de resultados fora da pasta esperada.' }
    foreach ($path in @($resultsRoot, $results)) {
        if ((Test-Path -LiteralPath $path) -and
            ((Get-Item -LiteralPath $path).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw 'O diretorio de resultados nao pode ser um link.'
        }
    }
    if (Test-Path -LiteralPath $results) { Remove-Item -LiteralPath $results -Recurse -Force }
    New-Item -ItemType Directory -Path $results -Force | Out-Null
    $env:BDD_RESULTS_DIRECTORY = $results
    $env:REQNROLL_FORMATTERS_HTML = 'outputFilePath=' + (Join-Path $results 'bdd-report.html')
    $env:REQNROLL_FORMATTERS_MESSAGE = 'outputFilePath=' + (Join-Path $results 'bdd-messages.ndjson')
    $env:DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE = 'true'
    $env:MSBuildEnableWorkloadResolver = 'false'
    if (!$SemBuild) {
        Invoke-Dotnet -Arguments @('restore', 'Greenhouse.sln', '--nologo')
        Invoke-Dotnet -Arguments @('build', 'Greenhouse.sln', '-c', 'Release', '--no-restore', '--nologo')
    }
    $failed = $false
    foreach ($suite in @(
        @{ Name = 'unit'; Project = 'Greenhouse.API.Tests'; Title = 'Testes unitarios'; Verbosity = 'normal' },
        @{ Name = 'bdd'; Project = 'Greenhouse.API.BddTests'; Title = 'Cenarios BDD em Gherkin'; Verbosity = 'detailed' }
    )) {
        if ($env:GITHUB_ACTIONS -eq 'true') { Write-Host "::group::$($suite.Title)" }
        try {
            Invoke-Dotnet -Arguments @('test', "tests/$($suite.Project)/$($suite.Project).csproj", '-c', 'Release',
                '--no-build', '--no-restore', '--nologo', '--logger', "trx;LogFileName=$($suite.Name).trx",
                '--logger', "console;verbosity=$($suite.Verbosity)", '--results-directory', $results)
        }
        catch { $failed = $true; Write-Warning $_.Exception.Message }
        finally { if ($env:GITHUB_ACTIONS -eq 'true') { Write-Host '::endgroup::' } }
    }
    if ($failed) { throw 'Uma ou mais suites de testes falharam. Consulte os relatorios TRX.' }
    Test-ApiCoverage
}
finally {
    foreach ($name in $environmentNames) { [Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name]) }
    Pop-Location
}
