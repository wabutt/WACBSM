@echo off
echo =====================================
echo   WAButt Release Builder
echo =====================================
echo.

if "%~1"=="" (
    echo ERROR: Debes especificar una version
    echo.
    echo Uso: build.bat "1.0.0.15"
    exit /b 1
)

echo Ejecutando build para version %~1...
echo.

powershell -ExecutionPolicy Bypass -File "%~dp0auto.ps1" -Version %~1

if %ERRORLEVEL% EQU 0 (
    echo.
    echo =====================================
    echo   Build completado exitosamente
    echo =====================================
) else (
    echo.
    echo =====================================
    echo   Build fallo - Error %ERRORLEVEL%
    echo =====================================
)

pause