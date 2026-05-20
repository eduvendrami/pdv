$project = "..\src\PDV.WPF\PDV.WPF.csproj"
$output = "..\publish"

Write-Host "Building release..." -ForegroundColor Cyan

dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $output

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful! Output: $output" -ForegroundColor Green
} else {
    Write-Host "Build failed!" -ForegroundColor Red
}
