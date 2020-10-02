@echo off
set version=0.0.1
set output=%~dp0Releases\v%version%\
set zipfile="%output%Disambiguator-v%version%.zip"
set buildoutputs="%~dp0Releases\Build Outputs"

rd /s /q "%output%"

copy "%~dp0\Readme.txt" %buildoutputs%

pushd %buildoutputs%
"%ProgramFiles%\7-Zip\7z.exe" a -tzip -mx9 -bd %zipfile% *
popd
copy "%~dp0\Readme.txt" "%output%"

set version=
set output=
set zipfile=
set buildoutputs=