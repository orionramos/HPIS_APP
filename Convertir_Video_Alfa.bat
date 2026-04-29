@echo off
setlocal
echo ====================================================
echo  Convertidor SPLIT-ALPHA a MP4 (Para HPIS APP / Quest)
echo ====================================================
echo.

if "%~1"=="" (
    echo [ERROR] Por favor, arrastra un archivo de video maestro MOV encima de este script.
    pause
    exit /b
)

set "INPUT=%~1"
set "OUTPUT=%~dpn1_SplitAlpha.mp4"

echo Procesando: "%~nx1"
echo Creando video de doble altura (Color arriba, Mascara alfa abajo)...
echo.

set "FFMPEG_PATH=C:\Users\ORION\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-8.1-full_build\bin\ffmpeg.exe"

"%FFMPEG_PATH%" -y -i "%INPUT%" -filter_complex "[0:v]split=2[color][alpha_src];[alpha_src]alphaextract,format=gray[mask];[color][mask]vstack" -c:v libx264 -preset slow -crf 20 -pix_fmt yuv420p "%OUTPUT%"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ====================================================
    echo [EXITO] Archivo convertido correctamente.
    echo Guardado en la misma carpeta que el original:
    echo %OUTPUT%
    echo ====================================================
) else (
    echo.
    echo ====================================================
    echo [ERROR] Hubo un problema al convertir el video.
    echo ====================================================
)
pause
