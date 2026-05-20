# =============================================================================
#  PDV — Lançar nova versão
#
#  PRÉ-REQUISITO (instalar uma vez só):
#    winget install --id GitHub.cli
#    gh auth login
#
#  USO:
#    .\release.ps1 1.0.1
#
#  O QUE FAZ:
#    1. Atualiza a versão no .csproj
#    2. Builda e gera PDV_v1.0.1.exe localmente
#    3. Cria a release no GitHub e faz upload do arquivo
#    4. Commita e faz push da mudança de versão
# =============================================================================

param(
    [Parameter(Mandatory, HelpMessage = "Versao no formato 1.0.1")]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version
)

$ErrorActionPreference = "Stop"
$csproj     = "src\PDV.WPF\PDV.WPF.csproj"
$tag        = "v$Version"
$releaseDir = ".\release"
$exeName    = "PDV_v$Version.exe"
$exePath    = Join-Path $releaseDir $exeName

# ── Verifica se gh CLI está instalado ────────────────────────────────────────
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Host ""
    Write-Error @"
GitHub CLI nao encontrado.
Instale com:  winget install --id GitHub.cli
Depois:       gh auth login
"@
    exit 1
}

# ── Verifica se a tag já existe ───────────────────────────────────────────────
$existing = git tag -l $tag
if ($existing) {
    Write-Error "A tag '$tag' ja existe. Escolha outro numero de versao."
    exit 1
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  PDV — Publicando versao $Version" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# ── 1. Atualiza versão no .csproj ─────────────────────────────────────────────
Write-Host ""
Write-Host "[1/4] Atualizando versao no projeto..." -ForegroundColor Yellow
[xml]$xml = Get-Content $csproj -Encoding UTF8
$pg = $xml.Project.PropertyGroup | Where-Object { $_.AssemblyVersion }
$pg.AssemblyVersion = "$Version.0"
$pg.FileVersion     = "$Version.0"
$xml.Save((Resolve-Path $csproj))
Write-Host "      AssemblyVersion = $Version.0" -ForegroundColor Gray

# ── 2. Build + publish local ──────────────────────────────────────────────────
Write-Host ""
Write-Host "[2/4] Compilando e gerando executavel..." -ForegroundColor Yellow
$publishTmp = ".\publish_tmp"
if (Test-Path $publishTmp) { Remove-Item $publishTmp -Recurse -Force }

dotnet publish $csproj -c Release -o $publishTmp
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish falhou."; exit 1 }

if (-not (Test-Path $releaseDir)) { New-Item -ItemType Directory $releaseDir | Out-Null }
Copy-Item (Join-Path $publishTmp "PDV.WPF.exe") $exePath -Force
Remove-Item $publishTmp -Recurse -Force
Write-Host "      Gerado: $exePath" -ForegroundColor Gray

# ── 3. Publica no GitHub com gh CLI ──────────────────────────────────────────
Write-Host ""
Write-Host "[3/4] Criando release no GitHub e fazendo upload..." -ForegroundColor Yellow
gh release create $tag $exePath `
    --title "PDV v$Version" `
    --generate-notes

if ($LASTEXITCODE -ne 0) { Write-Error "Falha ao criar release no GitHub."; exit 1 }

# ── 4. Commit + push da mudança de versão ────────────────────────────────────
Write-Host ""
Write-Host "[4/4] Commitando mudanca de versao..." -ForegroundColor Yellow
git add $csproj
git commit -m "chore: bump version para $Version"
git push

# ── Resultado ────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "  Versao $Version publicada com sucesso!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Release: https://github.com/eduvendrami/pdv/releases/tag/$tag" -ForegroundColor White
Write-Host "Em alguns instantes o cliente tera a atualizacao disponivel." -ForegroundColor Yellow
Write-Host ""
