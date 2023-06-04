@echo off
REM Run this to package up a release properly
REM Usage:
REM 
REM     PackageRelease {version}
REM 
REM where {version} is the version of the release you'd like to use.
REM 
REM Example
REM     PackageRelease 1.0.0.1

echo.
echo.
echo Building The Disambiguator, a Keepass plugin
echo.
echo.

cd /D %~dp0
if EXIST "%ProgramFiles%\7-Zip\7z.exe" goto CheckVersion
echo %ProgramFiles%\7-Zip\7z.exe not found.
echo For this script to execute properly, you must have the 32bit 7Zip application installed.
goto Done

:CheckVersion
if %1. NEQ . goto Continue
echo Please specify a complete version (a.b.c.d) when running this bat file.
goto Done

:Continue
echo Setting up
set version=%1
set buildoutputs=%~dp0Releases\Build Outputs
set output=%~dp0Releases\v%version%
set zipfile=%output%\Disambiguator-v%version%.zip
set versionfile=%~dp0VERSION

echo Writing VERSION file
for /F "delims=. tokens=1,2,3,4" %%J in ("%version%") do (set major=%%J&set minor=%%K&set patch=%%L&if [%%M]==[] (set build=0) else set build=%%M)
(
    @echo :
    @echo|set /p= The Disambiguator:%major%.%minor%.%patch%
        if not %build%==0 (@echo .%build%) ELSE (@echo.)
    @echo|set /p= :
)>"%versionfile%"

echo Remove Output Version folder %output% to start fresh
if exist "%output%\NUL" rmdir /s /q "%output%"

pushd "%buildoutputs%"
"%ProgramFiles%\7-Zip\7z.exe" a -tzip -mx9 -bd "%zipfile%" *
popd
copy "%~dp0Readme.txt" "%output%\"
copy "%~dp0VERSION" "%output%\"

set version=
set output=
set zipfile=
set buildoutputs=
set versionfile=

echo Build complete.

:Done