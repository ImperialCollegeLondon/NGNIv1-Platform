/*
 ## Cypress CyUSB3 driver source file (cyfileio.c)
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
#include "..\inc\cyfileio.h"
#include "..\inc\cyentry.h"
#include "..\inc\cytrace.h"
#if defined(EVENT_TRACING)
#include "cyfileio.tmh"
#endif
// *********************************************************************
//
// Function:  CyFileOpen
//
// Purpose:   Creates a new file, or opens a handle to an existing file
//
// Called by: 
//
// *********************************************************************
NTSTATUS
CyFileOpen(
    IN PWCHAR filename,
    IN BOOLEAN read,
    IN PHANDLE pHandle
    )
{
    NTSTATUS NtStatus = STATUS_SUCCESS;
    OBJECT_ATTRIBUTES objectAttrib;
    UNICODE_STRING usFilename;
    HANDLE handle;
    IO_STATUS_BLOCK ioStatus;
    

    CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP, "Start CyFileOpen\n");

    RtlInitUnicodeString(&usFilename, filename);
    InitializeObjectAttributes(&objectAttrib, &usFilename, OBJ_CASE_INSENSITIVE | OBJ_KERNEL_HANDLE, NULL, NULL);

    if (read)
        NtStatus = ZwCreateFile(&handle, GENERIC_WRITE, &objectAttrib, &ioStatus, NULL,
                                    0, FILE_SHARE_READ, FILE_OPEN, FILE_SYNCHRONOUS_IO_NONALERT, NULL, 0);
    else
        NtStatus = ZwCreateFile(&handle, GENERIC_WRITE, &objectAttrib, &ioStatus, NULL,
                                    FILE_ATTRIBUTE_NORMAL, 0, FILE_OVERWRITE_IF, FILE_SYNCHRONOUS_IO_NONALERT, NULL, 0);

    if (NT_SUCCESS(NtStatus))
        *pHandle = handle;

	CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP, "End CyFileOpen Status:%x\n",NtStatus);

    return NtStatus;
}


// *********************************************************************
//
// Function:  CyFileClose
//
// Purpose:   Closes a handle to a file
//
// Called by: 
//
// *********************************************************************
NTSTATUS
CyFileClose(
    IN HANDLE handle
    )
{
    NTSTATUS NtStatus = STATUS_SUCCESS;
    
    CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP, "Start CyFileClose\n");

    NtStatus = ZwClose(handle);

	CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP, "End CyFileClose Status :%x\n",NtStatus);

    return NtStatus;
}

ULONGLONG
CyFileGetSize(
    IN HANDLE handle
    )
{
    NTSTATUS NtStatus = STATUS_SUCCESS;
    IO_STATUS_BLOCK ioStatus;
    FILE_STANDARD_INFORMATION fileInfo;
    ULONGLONG fileLength = 0;
    
    CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP, "Start CyFileGetSize\n");

    NtStatus = ZwQueryInformationFile(handle, &ioStatus, (PVOID) &fileInfo, sizeof(fileInfo), FileStandardInformation);
    if (NT_SUCCESS(NtStatus))
    {
        fileLength = fileInfo.EndOfFile.QuadPart;
    }

	CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP, "Start CyFileGetSize Status:%x\n",NtStatus);

    return fileLength;
}

// *********************************************************************
//
// Function:  CyFileRead
//
// Purpose:   Reads data from an open file
//
// Called by: 
//
// *********************************************************************
NTSTATUS
CyFileRead(
    IN HANDLE handle,
    IN PVOID buffer,
    IN ULONG bufferLength,
    IN PULONG pBytesRead
    )
{
    NTSTATUS NtStatus = STATUS_SUCCESS;
    IO_STATUS_BLOCK ioStatus;
    
    CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP, "Start CyFileRead\n");

    ZwReadFile(handle, NULL, NULL, NULL, &ioStatus, buffer, bufferLength, NULL, NULL);
    if (NT_SUCCESS(ioStatus.Status))
    {
        *pBytesRead = (ULONG)ioStatus.Information;
    }

    NtStatus = ioStatus.Status;

	CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP, "Start CyFileRead Status:%x ReadReqLen:%x ActualReas:%x\n",NtStatus,bufferLength,*pBytesRead);

    return NtStatus;
}


// *********************************************************************
//
// Function:  FileIoWriteWDM
//
// Purpose:   Writes data from an open file
//
// Called by: 
//
// *********************************************************************
NTSTATUS
CyFileWrite(
    IN HANDLE handle,
    IN PVOID buffer,
    IN ULONG bufferLength,
    IN PULONG pBytesWritten
    )
{
    NTSTATUS NtStatus = STATUS_SUCCESS;
    IO_STATUS_BLOCK ioStatus;
    
    CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP, "Start CyFileWrite\n");

    ZwWriteFile(handle, NULL, NULL, NULL, &ioStatus, buffer, bufferLength, NULL, NULL);
    if (NT_SUCCESS(ioStatus.Status))
    {
        *pBytesWritten = (ULONG)ioStatus.Information;
    }

    NtStatus = ioStatus.Status;

	CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP, "End CyFileWrite Status:%x\n",NtStatus);

    return NtStatus;
}


