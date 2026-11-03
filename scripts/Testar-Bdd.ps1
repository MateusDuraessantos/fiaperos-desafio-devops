param(
    [ValidateSet('sqlite', 'sqlserver')][string]$Provedor = 'sqlite',
    [switch]$SemBuild
)
$ErrorActionPreference = 'Stop'
$arguments = @((Join-Path $PSScriptRoot 'test_bdd.py'), '--provider', $Provedor)
if ($SemBuild) { $arguments += '--no-build' }
python @arguments
if ($LASTEXITCODE -ne 0) { throw 'Testes ou verificacao de cobertura falharam. Consulte test-results.' }
