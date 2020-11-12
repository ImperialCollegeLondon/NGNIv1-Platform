@echo off

set srcpath=%CD%
set drive=%CD:~0,2%
set BASEDIR=C:\WINDDK\7600.16385.1


if "%1" == "/?" goto usage
if "%1" == "/h" goto usage

rem *** %1 is the CPU type, %2 is chk or fre (debug or release), %3 is the OS (wxp, wnet, wlh and win7) use lowercase***
if "%1" == ""	goto usage
if "%2" == ""	goto usage
if "%3" == ""	goto usage
if "%4" == ""	goto usage

:MakeInx
rem *** Use the MAKEINX.exe to fill in (search and replace) all the specific versions automatically. ***
set STAMPINF_PLATFORM=%1
set STAMPINF_OPERATING_SYSTEM=%3
set STAMPINF_VERSION=1.2.3.10
set STAMPINF_DATE=08/21/2014

MAKE_INX.exe.txt /FILE:"CYUSB3" /VERSION:"%STAMPINF_VERSION%" /DATE:"%STAMPINF_DATE%" /PLATFORM:"%STAMPINF_PLATFORM%" /OPERATING_SYSTEM:"%STAMPINF_OPERATING_SYSTEM%"

call %BASEDIR%\BIN\SETENV %BASEDIR% %1 %2 %3

CD %srcpath%
%drive%
CD..
CD src
rem *** copy .inx file to src folder  and delete it from \buildscript
copy %srcpath%\cyusb3.inx 
del  %srcpath%\cyusb3.inx

build -czg
if "%ERRORLEVEL%"=="0" goto inf2cat 
goto fail

:inf2cat
if "%1"=="x86" set CPU_TYPE=i386
if "%1"=="x64" set CPU_TYPE=amd64

rem if "%2"=="chk" goto success


goto package

:package
md cyusb3
md cyusb3\%4
md cyusb3\%4\%1
goto copyfile

:copyfile
if "%1"=="x64"  copy  %BASEDIR%\redist\wdf\%CPU_TYPE%\WdfCoInstaller01009.dll cyusb3\%4\%1
if "%1"=="x86"  copy  %BASEDIR%\redist\wdf\%1\WdfCoInstaller01009.dll cyusb3\%4\%1



copy obj%BUILD_ALT_DIR%\%CPU_TYPE%\cyusb3.inf                 cyusb3\%4\%1
copy obj%BUILD_ALT_DIR%\%CPU_TYPE%\cyusb3.sys                 cyusb3\%4\%1
copy obj%BUILD_ALT_DIR%\%CPU_TYPE%\cyusb3.pdb                 cyusb3\%4\%1
inf2cat /driver:%CD%\cyusb3\%4\%1 /os:7_X86,7_X64,Vista_X86,Vista_X64,XP_X86,XP_X64,2000
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
goto success


:success
echo BUILD OPERATION SUCCESSFULLY COMPLETED
goto end

:fail
echo BUILD OPERATION FAILED PLEASE LOOK AT THE ERROR LOG IN SRC DIR
goto end

:end
echo DONE