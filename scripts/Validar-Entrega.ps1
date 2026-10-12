param([switch]$SomentePacote)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$artifactRoot = Join-Path $projectRoot 'artifacts'
New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
$packageRoot = Join-Path $artifactRoot ('validacao-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $packageRoot | Out-Null
$allowedRoots = @('src', 'tests', 'docs', 'scripts', '.github', 'postman_collection')
$files = @('README.md', '.gitignore', '.env.example', 'docker-compose.yml') |
    ForEach-Object { Get-Item -LiteralPath (Join-Path $projectRoot $_) }
$files += foreach ($root in $allowedRoots) {
    Get-ChildItem -LiteralPath (Join-Path $projectRoot $root) -File -Recurse -Force |
        Where-Object {
            $_.FullName -notmatch '[\\/](bin|obj|PublishProfiles)[\\/]' -and
            $_.Name -notmatch '^\.env($|\.)|\.(user|publishsettings|pubxml|pfx|p12|pem|key)$'
        }
}
Add-Type -AssemblyName System.IO.Compression
$zipPath = Join-Path $artifactRoot ('greenhouse-' + [guid]::NewGuid().ToString('N') + '.zip')
$zip = [System.IO.Compression.ZipArchive]::new(
    [System.IO.File]::Open($zipPath, [System.IO.FileMode]::CreateNew),
    [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($projectRoot.Length + 1).Replace('\', '/')
        $entry = $zip.CreateEntry($relative)
        $output = $entry.Open()
        $input = $file.OpenRead()
        try { $input.CopyTo($output) } finally { $input.Dispose(); $output.Dispose() }
    }
} finally { $zip.Dispose() }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::ExtractToDirectory($zipPath, $packageRoot)
Copy-Item -LiteralPath (Join-Path $packageRoot '.env.example') -Destination (Join-Path $packageRoot '.env')
Write-Output "ZIP: $zipPath"
Write-Output "Pasta limpa: $packageRoot"
if (!$SomentePacote) {
    Push-Location $packageRoot
    try {
        docker compose config --quiet
        if ($LASTEXITCODE -ne 0) { throw 'Configuracao Compose invalida.' }
        docker compose build api
        if ($LASTEXITCODE -ne 0) { throw 'Build Docker falhou.' }
        Write-Output 'Compose e build validados. Execute docker compose up nessa pasta para testar a API e o banco.'
    } finally { Pop-Location }
}
