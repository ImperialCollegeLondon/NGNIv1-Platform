/*
 ## Cypress CyUSB3 driver source file (cyio.c)
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
#include "..\inc\cyio.h"
#include "..\inc\cytrace.h"
#include "..\inc\cyioctl.h"
#include "..\inc\cydevice.h"
#if defined(EVENT_TRACING)
#include "cyio.tmh"
#endif

#ifdef ALLOC_PRAGMA
#pragma alloc_text(PAGE, CyEvtDeviceIoCreate)
#pragma alloc_text(PAGE, CyEvtDeviceIoClose)
#pragma alloc_text(PAGE, CyEvtIoRead)
#pragma alloc_text(PAGE, CyEvtIoWrite)
#pragma alloc_text(PAGE, CyEvtIoDeviceControl)
#pragma alloc_text(PAGE, CyEvtIoStop)
#pragma alloc_text(PAGE, CyEvtIoResume)
#endif

VOID
  CyEvtDeviceIoCreate(
    IN WDFDEVICE  Device,
    IN WDFREQUEST  Request,
    IN WDFFILEOBJECT  FileObject
    )
{
	PAGED_CODE(); 

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "Start CyEvtDeviceIoCreate\n");
	
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "End CyEvtDeviceIoCreate\n");
}

VOID
  CyEvtDeviceIoClose (
    IN WDFFILEOBJECT  FileObject
    )
{
	PAGED_CODE(); 

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "Start CyEvtDeviceIoClose\n");
	
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "End CyEvtDeviceIoClose\n");
}
VOID
  CyEvtIoRead (
    IN WDFQUEUE  Queue,
    IN WDFREQUEST  Request,
    IN size_t  Length
    )
{
	PAGED_CODE(); 
	
}
VOID
  CyEvtIoWrite (
    IN WDFQUEUE  Queue,
    IN WDFREQUEST  Request,
    IN size_t  Length
    )
{
	PAGED_CODE(); 	
}


VOID
  CyEvtIoDeviceControl (
    IN WDFQUEUE  Queue,
    IN WDFREQUEST  Request,
    IN size_t  OutputBufferLength,
    IN size_t  InputBufferLength,
    IN ULONG  IoControlCode
    )
{

	WDFDEVICE           device;
    PDEVICE_CONTEXT     pDevContext;
	size_t szBytestoReturn=0;

	PAGED_CODE(); 

    CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "Start CyEvtIoDeviceControl\n");	
   
    device = WdfIoQueueGetDevice(Queue);
    pDevContext = CyGetDeviceContext(device);

	if((FUNCTION_FROM_CTL_CODE(IoControlCode) < (IOCTL_ADAPT_INDEX+NUMBER_OF_ADAPT_IOCTLS)))
	{
		CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "IOCTL CODE WITHING RANGE: IOCTL CODE :%d\n",FUNCTION_FROM_CTL_CODE(IoControlCode));
		(*pDevContext->CyDispatchIoctl[FUNCTION_FROM_CTL_CODE(IoControlCode)])(Queue,Request,OutputBufferLength,InputBufferLength);
	}
	else
	{
		CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "IOCTL CODE OUT OF RANGE: IOCTL CODE :%d\n",FUNCTION_FROM_CTL_CODE(IoControlCode));
		WdfRequestCompleteWithInformation(Request, STATUS_INVALID_PARAMETER, szBytestoReturn);
	}
	
    CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "End CyEvtIoDeviceControl\n");
}

VOID
  CyEvtIoStop (
    IN WDFQUEUE  Queue,
    IN WDFREQUEST  Request,
    IN ULONG  ActionFlags
    )
{
	PAGED_CODE(); 

	
}
VOID
  CyEvtIoResume (
    IN WDFQUEUE  Queue,
    IN WDFREQUEST  Request
    )
{
	PAGED_CODE(); 
}








