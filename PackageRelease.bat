echo on
echo !!!!!! Packaging Release !!!!!!

cd /D %~dp0

echo Setting up
echo Current Dir is %CD%
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