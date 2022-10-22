echo on
echo !!!!!! Packaging Release !!!!!!
echo For this script to execute properly, you must have the 32bit 7Zip application installed.

cd /D %~dp0

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