/*
 ## Cypress CyUSB3 driver header file (cyfileio.h)
 ## =======================================================
 ##
 ##  Copyright Cypress Semiconductor Corporation, 2009-2012,
 ##  All Rights Reserved
 ##  UNPUBLISHED, LICENSED SOFTWARE.
 ##
 ##  CONFIDENTIAL AND PROPRIETARY INFORMATION
 ##  WHICH IS THE PROPERTY OF CYPRESS.
 ##
 ##  Use of this file is governed
 ##  by the license agreement included in the file
 ##
 ##  <install>/license/license.rtf
 ##
 ##  where <install> is the Cypress software
 ##  install root directory path.
 ##
 ## =======================================================
*/
#ifndef _CYFILEIO_H_
#define _CYFILEIO_H_

#include <ntddk.h>
#include <wdf.h>

NTSTATUS
CyFileOpen(
    IN PWCHAR filename,
    IN BOOLEAN read,
    IN PHANDLE pHandle
    );
NTSTATUS
CyFileClose(
    IN HANDLE handle
    );
	ULONGLONG
CyFileGetSize(
    IN HANDLE handle
    );

	NTSTATUS
CyFileRead(
    IN HANDLE handle,
    IN PVOID buffer,
    IN ULONG bufferLength,
    IN PULONG pBytesRead
    );
NTSTATUS
CyFileWrite(
    IN HANDLE handle,
    IN PVOID buffer,
    IN ULONG bufferLength,
    IN PULONG pBytesWritten
    );

#endif /*_CYFILEIO_H_s*/