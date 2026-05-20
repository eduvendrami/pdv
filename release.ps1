# =============================================================================
#  PDV — Lançar nova versão
#
#  USO:
#    .\release.ps1 1.0.1
#
#  O QUE FAZ AUTOMATICAMENTE:
#    1. Atualiza a versão no .csproj
#    2. Faz commit da mudança
#    3. Cria a tag v1.0.1
#    4. Faz push para o GitHub
#    5. O GitHub Actions builda, renomeia e publica a release
# =============================================================================

param(
    [Parameter(Mandatory, HelpMessage = "Versao no formato 1.0.1")]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version
)

$ErrorActionPreference = "Stop"
$csproj = "src\PDV.WPF\PDV.WPF.csproj"
$tag    = "v$Version"

# ── Verifica se o git está limpo ──────────────────────────────────────────────
$status = git status --porcelain
if ($status) {
    Write-Host ""
    Write-Warning "Ha arquivos nao commitados:"
    git status --short
    $resp = Read-Host "`nDeseja commitar tudo antes de continuar? (s/n)"
    if ($resp -ne 's') { exit 0 }
    git add -A
    git commit -m "chore: preparar release $tag"
}

# ── Verifica se a tag já existe ───────────────────────────────────────────────
$existing = git tag -l $tag
if ($existing) {
    Write-Error "A tag '$tag' ja existe. Escolha outro numero de versao."
    exit 1
}

# ── Atualiza a versão no .csproj ──────────────────────────────────────────────
Write-Host ""
Write-Host "Atualizando versao para $Version..." -ForegroundColor Cyan

[xml]$xml = Get-Content $csproj -Encoding UTF8
$pg = $xml.Project.PropertyGroup | Where-Object { $_.AssemblyVersion }
$pg.AssemblyVersion = "$Version.0"
$pg.FileVersion     = "$Version.0"
$xml.Save((Resolve-Path $csproj))

Write-Host "  AssemblyVersion = $Version.0" -ForegroundColor Gray
Write-Host "  FileVersion     = $Version.0" -ForegroundColor Gray

# ── Commit da versão ──────────────────────────────────────────────────────────
git add $csproj
git commit -m "chore: bump version para $Version"

# ── Tag + push ────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "Criando tag $tag e enviando para o GitHub..." -ForegroundColor Cyan

git tag $tag
git push
git push origin $tag

# ── Feedback ──────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "  Tag $tag enviada com sucesso!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "O GitHub esta buildando e publicando a release." -ForegroundColor White
Write-Host "Acompanhe em: https://github.com/$(git remote get-url origin | ForEach-Object { ($_ -replace 'https://github.com/','') -replace '\.git$','' })/actions"
Write-Host ""
Write-Host "Em alguns minutos o cliente tera a atualizacao disponivel automaticamente." -ForegroundColor Yellow
Write-Host ""
