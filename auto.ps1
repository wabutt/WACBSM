# BuildRelease.ps1 - Para .NET Framework
param(
    [Parameter(Mandatory=$true)]
    [string]$Version
)

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  WAButt Release Builder v$Version" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

# Configuracion
$solutionPath = ".\butt.sln"
$outputPath = ".\Presentation\bin\Release"
$repoRoot = ".\"

$zipName = "WAButt_$Version.zip"
$xmlName = "AutoUpdater.xml"
$changelogName = "CHANGELOG.txt"

# Buscar MSBuild
Write-Host "`nBuscando MSBuild..." -ForegroundColor Yellow

$msbuildPaths = @(
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"
)

$msbuild = $null
foreach ($path in $msbuildPaths) {
    if (Test-Path $path) {
        $msbuild = $path
        Write-Host "[OK] MSBuild encontrado: $path" -ForegroundColor Green
        break
    }
}

if ($null -eq $msbuild) {
    Write-Host "[ERROR] MSBuild no encontrado" -ForegroundColor Red
    Write-Host "Instala Visual Studio o especifica la ruta manualmente" -ForegroundColor Yellow
    exit 1
}

# [1/7] Verificar version en AssemblyInfo
Write-Host "`n[1/7] Verificando version en AssemblyInfo..." -ForegroundColor Yellow

$assemblyInfoPath = ".\Presentation\Properties\AssemblyInfo.cs"
if (-not (Test-Path $assemblyInfoPath)) {
    Write-Host "[ERROR] AssemblyInfo.cs no encontrado en $assemblyInfoPath" -ForegroundColor Red
    exit 1
}

$assemblyContent = Get-Content $assemblyInfoPath -Raw

if ($assemblyContent -notmatch "AssemblyVersion\(`"$Version`"\)") {
    Write-Host "[ERROR] La version en AssemblyInfo.cs no coincide con $Version" -ForegroundColor Red
    Write-Host "Actualiza AssemblyInfo.cs primero:" -ForegroundColor Yellow
    Write-Host "[assembly: AssemblyVersion(`"$Version`")]" -ForegroundColor White
    Write-Host "[assembly: AssemblyFileVersion(`"$Version`")]" -ForegroundColor White
    exit 1
}
Write-Host "[OK] Version verificada" -ForegroundColor Green

# [2/7] Limpiar build anterior
Write-Host "`n[2/7] Limpiando builds anteriores..." -ForegroundColor Yellow

& $msbuild $solutionPath /t:Clean /p:Configuration=Release /v:minimal /nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Error en clean" -ForegroundColor Red
    exit 1
}
Write-Host "[OK] Clean completado" -ForegroundColor Green

# [3/7] Compilar
Write-Host "`n[3/7] Compilando version Release..." -ForegroundColor Yellow

& $msbuild $solutionPath /t:Rebuild /p:Configuration=Release /v:minimal /nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Build fallo" -ForegroundColor Red
    exit 1
}
Write-Host "[OK] Build exitoso" -ForegroundColor Green

# Verificar EXE
$exePath = Join-Path $outputPath "WAButt.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "[ERROR] WAButt.exe no encontrado en $exePath" -ForegroundColor Red
    Write-Host "Verifica que el proyecto se llame 'WAButt' o ajusta la ruta" -ForegroundColor Yellow
    exit 1
}

$exeSize = (Get-Item $exePath).Length / 1MB
Write-Host "     Tamano del EXE: $([math]::Round($exeSize, 2)) MB" -ForegroundColor Cyan

if ($exeSize -lt 2) {
    Write-Host "[WARN] EXE muy pequeno. Costura esta funcionando?" -ForegroundColor Yellow
}

# [4/7] Crear paquete
Write-Host "`n[4/7] Creando paquete..." -ForegroundColor Yellow

# ZIP va a RAIZ del repo
$zipPath = Join-Path $repoRoot $zipName

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path $exePath -DestinationPath $zipPath -CompressionLevel Optimal

$zipSize = (Get-Item $zipPath).Length / 1MB
Write-Host "[OK] Paquete creado: $([math]::Round($zipSize, 2)) MB" -ForegroundColor Green

# [5/7] Calcular SHA256
Write-Host "`n[5/7] Calculando hash SHA256..." -ForegroundColor Yellow
$hash = Get-FileHash -Path $zipPath -Algorithm SHA256
$hashValue = $hash.Hash
Write-Host "[OK] SHA256: $hashValue" -ForegroundColor Green

# [6/7] Generar XML de AutoUpdater
Write-Host "`n[6/7] Generando AutoUpdater.xml..." -ForegroundColor Yellow

$xmlContent = @"
<?xml version="1.0" encoding="UTF-8"?>
<item>
    <version>$Version</version>
    <url>https://raw.githubusercontent.com/wabutt/itsmevsauce/master/$zipName</url>
    <changelog>https://raw.githubusercontent.com/wabutt/itsmevsauce/master/$changelogName</changelog>
    <mandatory>true</mandatory>
    <checksum algorithm="SHA256">$hashValue</checksum>
</item>
"@

$xmlPath = Join-Path $repoRoot $xmlName
$xmlContent | Out-File -FilePath $xmlPath -Encoding UTF8 -NoNewline

Write-Host "[OK] XML actualizado en raiz" -ForegroundColor Green

# [7/7] Generar CHANGELOG
Write-Host "`n[7/7] Generando CHANGELOG..." -ForegroundColor Yellow

$currentDate = Get-Date -Format "dd/MM/yyyy"

$changelogContent = @"
WAButt - Historial de Cambios
================================

Version $Version ($currentDate)
--------------------------------
[NUEVO]
* Sistema de almacenamiento de licencias optimizado
* Validacion de HWID en archivo portable
* Proteccion anti-cracking mejorada

[MEJORAS]
* Reduccion de consumo de memoria en Chrome
* Optimizacion de velocidad de envio de mensajes
* Interfaz mas responsive

[CORRECCIONES]
* Corregido error al copiar license.dat entre PCs
* Solucionado problema de XanderUI en inicio
* Mejoras en manejo de errores de red

[TECNICO]
* Migracion a AES-256 para encriptacion
* Actualizacion de ChromeDriver a ultima version
* Implementacion de Costura.Fody para single-file

================================
Versiones anteriores:

Version 1.0.0.14
* Version base
"@

$changelogPath = Join-Path $repoRoot $changelogName
$changelogContent | Out-File -FilePath $changelogPath -Encoding UTF8

Write-Host "[OK] CHANGELOG generado" -ForegroundColor Green

# Resumen
Write-Host "`n=====================================" -ForegroundColor Cyan
Write-Host "  Release $Version completado" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Archivos generados en RAIZ del repositorio:" -ForegroundColor White
Write-Host "  * $zipName ($([math]::Round($zipSize, 2)) MB)" -ForegroundColor Cyan
Write-Host "  * $xmlName (actualizado)" -ForegroundColor Cyan
Write-Host "  * $changelogName (nuevo)" -ForegroundColor Cyan
Write-Host ""
Write-Host "SHA256: $hashValue" -ForegroundColor Yellow
Write-Host ""
Write-Host "Proximos pasos:" -ForegroundColor Yellow
Write-Host "1. Revisar/editar CHANGELOG.txt con cambios reales" -ForegroundColor White
Write-Host ""
Write-Host "2. Subir a GitHub:" -ForegroundColor White
Write-Host "   git add $zipName $xmlName $changelogName" -ForegroundColor Gray
Write-Host "   git commit -m 'Release v$Version'" -ForegroundColor Gray
Write-Host "   git push origin master" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Verificar URLs en GitHub:" -ForegroundColor White
Write-Host "   https://raw.githubusercontent.com/wabutt/itsmevsauce/master/$zipName" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Probar actualizacion desde v1.0.0.14" -ForegroundColor White
Write-Host ""