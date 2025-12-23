@echo off
REM Run this to create a plgx file and package up as a release
REM Usage:
REM 
REM     CreatePlgX {version}
REM 
REM where {version} is the version of the release you'd like to use.
REM 
REM Example
REM 
REM     CreatePlgX 1.0.0.1

echo.
echo.
echo Building The Disambiguator, a Keepass plugin
echo.
echo.

cd /D %~dp0

:CheckVersion
if %1. NEQ . goto Continue
echo Please specify a complete version (a.b.c.d) when running this bat file.
goto Done

:Continue
set version=%1

echo Updating VERSION file
for /F "delims=. tokens=1,2,3,4" %%J in ("%version%") do (set major=%%J&set minor=%%K&set patch=%%L&if [%%M]==[] (set build=0) else set build=%%M)
(
    @echo :
    @echo|set /p= The Disambiguator:%major%.%minor%.%patch%
        if not %build%==0 (@echo .%build%) ELSE (@echo.)
    @echo|set /p= :
)>"VERSION."

echo Deleting existing PlgX folder
if exist ".\Plgx" rmdir /s /q "PlgX"
if exist ".\Releases\Build Outputs" rmdir /s /q ".\Releases\Build Outputs"

echo Creating a temporary PlgX folder for the plugin source
if not exist Plgx\nul mkdir PlgX

echo Verifying the Releases folder exists
if not exist "%~dp0Releases\NUL" mkdir "%~dp0Releases"

echo Verifying the Build Outputs folder exists
if not exist "%~dp0Releases\Build Outputs\NUL" mkdir "%~dp0Releases\Build Outputs"

echo Verifying the version folder exists
if exist "%~dp0Releases\v%version%" rmdir "%~dp0Releases\v%version%\" /S /Q
if not exist "%~dp0Releases\v%version%\NUL" mkdir "%~dp0Releases\v%version%"

echo Copying plugin source to the PlgX folder
robocopy ".\Disambiguator" ".\PlgX" /E /XF *.user *.sln *.suo *.pdb *.plgx *.exe *.exe.config /XD bin obj
robocopy ".\Disambiguator\bin\Release\tessdata" ".\PlgX\tessdata" /E
robocopy ".\Disambiguator\bin\Release\x64" ".\PlgX\x64" /E
robocopy ".\Disambiguator\bin\Release\x86" ".\PlgX\x86" /E
robocopy ".\Disambiguator\bin\Release" ".\PlgX" /E patagames.ocr.dll

echo Compiling PlgX
@echo on
".\Disambiguator\KeePass.exe" --plgx-create "%~dp0PlgX" --plgx-prereq-os:Windows
@echo off

echo Zip up plgx and readme
move /y PlgX.plgx ".\Releases\Build Outputs\Disambiguator.plgx"
copy "%~dp0Readme.txt" ".\Releases\Build Outputs\"
copy "%~dp0VERSION" ".\Releases\Build Outputs\"
setlocal

set sourceDir=.\Releases\Build Outputs
set zipFile=.\Releases\v%version%\Disambiguator-v%version%.zip

rem Create PowerShell script
echo Write-Output 'Custom PowerShell profile in effect!'    > %~dp0TempZipScript.ps1
echo Add-Type -A System.IO.Compression.FileSystem           >> %~dp0TempZipScript.ps1
echo [IO.Compression.ZipFile]::CreateFromDirectory('%sourceDir%','%~dp0%zipFile%') >> %~dp0TempZipScript.ps1

rem Execute script with flag "-ExecutionPolicy Bypass" to get around ExecutionPolicy
PowerShell.exe -ExecutionPolicy Bypass -Command "& '%~dp0TempZipScript.ps1'"
del %~dp0TempZipScript.ps1
endlocal


echo Cleaning up
rem rmdir ".\PlgX" /S /Q
rem rmdir ".\Releases\Build Outputs" /S /Q


:Done