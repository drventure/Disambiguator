echo on
echo !!!!!! Packaging Release !!!!!!
echo For this script to execute properly, you must have the 32bit 7Zip application installed.

cd /D %~dp0

echo Setting up
set version=%1
set buildoutputs=%~dp0Releases\Build Outputs
set output=%~dp0Releases\v%version%
set zipfile=%output%\Disambiguator-v%version%.zip

echo Remove Output Version folder %output% to start fresh
if exist "%output%\NUL" rmdir /s /q "%output%"

pushd "%buildoutputs%"
"%ProgramFiles%\7-Zip\7z.exe" a -tzip -mx9 -bd "%zipfile%" *
popd
copy "%~dp0Readme.txt" "%output%\"

set version=
set output=
set zipfile=
set buildoutputs=