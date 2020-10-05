echo on
echo !!!!!! Building Plgx !!!!!!
cd /D %~dp0

echo Deleting existing PlgX folder
if exist Plgx rmdir /s /q PlgX

echo Creating PlgX folder
if not exist Plgx\nul mkdir PlgX

echo Make sure the Releases folder exists
if not exist "%~dp0Releases\NUL" mkdir "%~dp0Releases"

echo Make sure the Build Output folder exists
if not exist "%~dp0Releases\Build Outputs\NUL" mkdir "%~dp0Releases\Build Outputs"

echo Copying files
xcopy "Disambiguator" PlgX /s /e /exclude:PlgXExclude.txt

echo Compiling PlgX
".\Disambiguator\KeePass.exe" --plgx-create "%~dp0PlgX" --plgx-prereq-os:Windows

echo Releasing PlgX
move /y PlgX.plgx ".\Releases\Build Outputs\Disambiguator.plgx"
copy /y ".\Releases\Build Outputs\Disambiguator.plgx" ".\Disambiguator\Plugins\Disambiguator.plgx"
copy "%~dp0Readme.txt" ".\Releases\Build Outputs\"

echo Cleaning up
rmdir /s /q PlgX
