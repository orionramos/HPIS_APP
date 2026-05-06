@echo off
setlocal

echo ====================================================
echo  Convertidor SPLIT-ALPHA a MP4 (Para HPIS APP / Quest)
echo ====================================================
echo.

:: --- BUSQUEDA DE FFMPEG ---
set "FFMPEG_EXE=ffmpeg"

:: 1. Intentar si ya esta en el PATH
"%FFMPEG_EXE%" -version >nul 2>nul
if %ERRORLEVEL% EQU 0 goto :CHECK_INPUT

:: 2. Buscar en la carpeta de WinGet (de forma dinamica)
set "WINGET_PATH=%LOCALAPPDATA%\Microsoft\WinGet\Packages"
for /d %%D in ("%WINGET_PATH%\Gyan.FFmpeg*") do (
    for /f "delims=" %%F in ('dir /b /s "%%D\ffmpeg.exe" 2^>nul') do (
        set "FFMPEG_EXE=%%F"
    )
)

:: 3. Verificar si se encontro en WinGet
if exist "%FFMPEG_EXE%" goto :CHECK_INPUT

:: 4. Error si no se encuentra
echo [ERROR] No se encontro FFmpeg en este equipo.
echo Por favor, instala FFmpeg ejecutando: winget install Gyan.FFmpeg
echo.
pause
exit /b

:CHECK_INPUT
if "%~1"=="" (
    echo [ERROR] Por favor, arrastra un archivo de video maestro encima de este script.
    echo.
    pause
    exit /b
)

set "INPUT=%~1"
set "OUTPUT=%~dpn1.mp4"

echo FFmpeg encontrado en: "%FFMPEG_EXE%"
echo Procesando: "%~nx1"
echo.

:: --- EJECUCION ---
"%FFMPEG_EXE%" -y -i "%INPUT%" -filter_complex "[0:v]split=2[color][alpha_src];[alpha_src]alphaextract,format=gray[mask];[color][mask]vstack" -c:v libx264 -preset slow -crf 20 -pix_fmt yuv420p "%OUTPUT%"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ====================================================
    echo [EXITO] Archivo convertido correctamente.
    echo Guardado en: "%OUTPUT%"
    echo ====================================================
) else (
    echo.
    echo ====================================================
    echo [ERROR] Hubo un problema al convertir el video.
    echo ====================================================
)

pause
exit /b
