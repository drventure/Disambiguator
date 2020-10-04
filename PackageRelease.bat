echo !!!!!! Packaging Release !!!!!!
echo Setting up
set version=%1
set output=%~dp0Releases\v%version%\
set zipfile="%output%Disambiguator-v%version%.zip"
set buildoutputs="%~dp0Releases\Build Outputs"

echo Make sure the Releases folder exists
mkdir "%~dp0Releases"

echo Remove Output Version folder to start fresh
rmdir /s /q "%output%"

echo Remove the Build output folder as well
rmdir "%buildoutputs%"

echo Create the build outputs folder
mkdir "%buildoutputs%"

copy "%~dp0Readme.txt" %buildoutputs%

pushd "%buildoutputs%""
"%ProgramFiles%\7-Zip\7z.exe" a -tzip -mx9 -bd %zipfile% *
popd
copy "%~dp0Readme.txt" "%output%"

set version=
set output=
set zipfile=
set buildoutputs=