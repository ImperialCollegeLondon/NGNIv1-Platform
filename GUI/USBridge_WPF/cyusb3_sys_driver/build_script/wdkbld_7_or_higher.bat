@echo off

set srcpath=%CD%
set drive=%CD:~0,2%

if "%1" == "/?" goto usage
if "%1" == "/h" goto usage

rem *** %1 is the CPU type, %2 is chk or fre (debug or release), %3 is the OS (wxp, wnet, wlh and win7) use lowercase***
if "%1" == ""	goto usage
if "%2" == ""	goto usage
if "%3" == ""	goto usage

call "C:\Program Files (x86)\Microsoft Visual Studio 12.0\VC\vcvarsall.bat"

%drive%
CD %srcpath%
copy /Y cyusb3.txt ..\src\cyusb3.inx
CD ..\src

if "%1"=="x86" (
	set CPU_TYPE=Win32
	set CPU_TYPE_PATH=x86
	echo "32 bit compilation"
)
if "%1"=="x64" (
	set CPU_TYPE=x64
	set CPU_TYPE_PATH=x64
	echo "64 bit compilation"
)

if "%1"=="%CPU_TYPE_PATH%" goto Config
goto ErrorExit

:Config
if "%2"=="chk" goto chkbuild
goto freeBuild
:chkbuild
	
if "%3"=="Win8.1" (
	set CONFIGURATION="Win8.1 Debug"
	set CONFIGURATION_PATH="Win8.1Debug"
)

if "%3"=="Win8" (
	set CONFIGURATION="Win8 Debug"
	set CONFIGURATION_PATH="Win8Debug"
)

if "%3"=="Win7" (
	set CONFIGURATION="Win7 Debug"
	set CONFIGURATION_PATH="Win7Debug"
)

goto performBuild

:freeBuild
echo "Perform Free build"
if "%3"=="Win8.1" (
	set CONFIGURATION="Win8.1 Release"
	set CONFIGURATION_PATH="Win8.1Release"
)

if "%3"=="Win8" (
	set CONFIGURATION="Win8 Release"
	set CONFIGURATION_PATH="Win8Release"
)

if "%3"=="Win7" (
	set CONFIGURATION="Win7 Release"
	set CONFIGURATION_PATH="Win7Release"
)


echo "%CONFIGURATION%"
:performBuild
:BuildConfig
msbuild /t:clean /t:build .\cyusb3.sln /p:Configuration=%CONFIGURATION% /p:Platform=%CPU_TYPE%

if "%ERRORLEVEL%"=="0" goto package
goto ErrorExit

:package
md cyusb3
md cyusb3\%3
md cyusb3\%3\%1

copy cyusb3-Package\%1\%CONFIGURATION_PATH%\cyusb3-Package\*.* cyusb3\%3\%1
copy build-objects\%CONFIGURATION_PATH%\%1\cyusb3.pdb cyusb3\%3\%1
if "%ERRORLEVEL%"=="0" goto Infpackage
goto ErrorExit

:Infpackage
if "%CPU_TYPE%"=="x64" inf2cat /driver:%CD%\cyusb3\%3\%1 /os:6_3_x64,8_x64,7_x64

if "%CPU_TYPE%"=="Win32" inf2cat /driver:%CD%\cyusb3\%3\%1 /os:6_3_x86,8_x86,7_x86
goto signature

:signature
rem Enable below three lines to create test sign certificate and add it load machine.
rem Makecert -r -pe -ss PrivateCertStore -n "CN=DriverTestCertificate" CySuiteUSB3.cer
rem certmgr.exe -add CySuiteUSB3.cer -s -r localMachine root 
rem certmgr.exe -add CySuiteUSB3.cer -s -r localMachine trustedpublisher

rem Enable below lines to test sign driver and cat files.
rem copy CyUSBSuiteTest.cer cyusb3\%3\%1
rem SignTool sign /v /s PrivateCertStore /n CyUSB3DriverTestCer /t http://timestamp.verisign.com/scripts/timstamp.dl %CD%\cyusb3\%3\%1\CyUSB3.cat
rem SignTool sign /v /s PrivateCertStore /n CyUSB3DriverTestCer /t http://timestamp.verisign.com/scripts/timstamp.dl %CD%\cyusb3\%3\%1\CyUSB3.sys
rem Signtool verify /pa /v /c %CD%\cyusb3\%3\%1\CyUSB3.cat %CD%\cyusb3\%3\%1\CyUSB3.sys

:success
echo BUILD OPERATION SUCCESSFULLY COMPLETED
goto end

:ErrorExit
echo BUILD OPERATION FAILED PLEASE LOOK AT THE ERROR LOG IN SRC DIR
goto end

:end
cd ..\build_script
echo DONE