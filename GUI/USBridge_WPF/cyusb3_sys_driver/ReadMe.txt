This directory contains source code for CyUSB3 WDF kernel mode generic driver.

This document help the customers or interested people to compile Cypress CyUSB3 driver.


Pre-Requirements:

Prior to compile the driver package, the target PC needs following build environments.

WDK 7.1 (For compile xp and Vista drivers)

WDK 8.1 and Visual Studio 2013 (professional or higher) (For compiling Win 7, 8 and 8.1 drivers).

Modify cywdkbuild.bat. Go to the line "set BASEDIR=C:\WINDDK\7600.16385.1". Please set the right WDK 7.1 installation path.

Modify wdkbld_7_or_higher.bat. Go to the line "call "C:\Program Files (x86)\Microsoft Visual Studio 12.0\VC\vcvarsall.bat"". Please set the path for WDK 8.1 environment.



How to build and compile the driver.

BUILD_SCRIPT folder holds readily driver compilable batch script.

cybuildall.bat script helps the user to build the cypress CyUSB3 driver. This batch script builds for xp, vista, 7, 8 and 8.1 OS'es.

Just execute this batch file.

src\cyusb3 holds all the driver files.





DIRECTORY STRUCTURE:

build_script : Build script for driver.

->cybuildall.bat   : Build script to build driver for Windows XP(x86 only),Vista(x86 and x64), 7 (x86 & x64), 8 (x86 & x64), and 8.1 (x86 & x64) build.


->cywdkbuild.bat   : Driver build script.	

->MAKE_INX.exe 	   : This application is used to generate CyUSB.txt.
	
->cyusb3.txt      : Source for INF file, user can update this file to add their VID/PID and strings, build operation generates INF file from this file.

	 inc          : Header files. 
	 src          : Source files.
	 res          : Resource files. 
	 workspace    : cyusb_driver.sln. 
	 ReadMe.txt   : ReadMe file.

VISUAL STUDIO SOLUTION FILE:
	Open CyUSB3_sys_driver\workspace\cyusb_driver.sln file in Visual Studio 2008 or higher.
	This project is created using Visual Studio 2008. Visual studio solution is only for code navigation. Please follow the build driver steps to build driver binaries.

	
API GUIDE DOCUMENT:
	Please refer Cypress CyUSB Driver Programmer's Reference document for IOCTL interface of driver.
	You can find this document after installing FX3 SDK installer under following directory. 
	Directory : Cypress\Cypress USBSuite\driver\CyUSB.pdf or CyUSB.chm


