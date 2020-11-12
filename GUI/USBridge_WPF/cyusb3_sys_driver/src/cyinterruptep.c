/*
 ## Cypress CyUSB3 driver source file (cyinterruptep.c)
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
/*
 This source file provide API for interrupt reader. 
 Please note,presenlty, this interface is not enabled, since driver provides API interface for interrupt
 trasfer similar to the bulk transfer.
*/
#include "..\inc\cyinterruptep.h"
#include "..\inc\cytrace.h"
#include "..\inc\cydevice.h"
#include <wdf.h>
#if defined(EVENT_TRACING)
#include "cyinterruptep.tmh"
#endif

__drv_requiresIRQL(PASSIVE_LEVEL)
NTSTATUS
 CyCheckAndConfigureInterruptInEp(
 __in PDEVICE_CONTEXT pDevContext
    )
{
   UCHAR ucIndex=0;
   NTSTATUS NtStatus=STATUS_SUCCESS;

   CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start CyCheckAndConfigureInterruptInEp\n");

   if(pDevContext->ucActiveInterruptInPipe)
   {
		for(ucIndex=0;ucIndex<pDevContext->ucActiveInterruptInPipe;ucIndex++)
		{// NOTE :	WdfUsbTargetPipeConfigContinuousReader can support max of 10 interrupt reader
			if(ucIndex == MAX_INTERRUPT_REDEAR_EP)
				break;
			NtStatus = CyConfigInterruptINepReader(pDevContext,pDevContext->WdfUsbInterruptInPipeArray[ucIndex]);
			CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_IOCTL, "Found interrupt endpoint 0x:%x\n",ucIndex);
		}
   }
   else
   {
	   // Do Nothing
   }
   CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyCheckAndConfigureInterruptInEp\n");
   return NtStatus;
}
__drv_requiresIRQL(PASSIVE_LEVEL)
NTSTATUS
CyConfigInterruptINepReader(
    __in PDEVICE_CONTEXT pDevContext,
	__in WDFUSBPIPE  IntUsbPipe
    )
{
	NTSTATUS NtStatus;
	WDF_USB_CONTINUOUS_READER_CONFIG UsbContiReaderConfi;
    
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start CyConfigInterruptINepReader\n");

    WDF_USB_CONTINUOUS_READER_CONFIG_INIT(&UsbContiReaderConfi,
                                          CyEvtInterruptINepReaderComplete,
                                          pDevContext,
                                          sizeof(UCHAR));
    
    NtStatus = WdfUsbTargetPipeConfigContinuousReader(IntUsbPipe,
                                                    &UsbContiReaderConfi);

    if (!NT_SUCCESS(NtStatus)) {
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL,
                    "WdfUsbTargetPipeConfigContinuousReader failed %x\n",
                    NtStatus);
		CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyConfigInterruptINepReader\n");
        return NtStatus;
    }
	CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_IOCTL, "CyConfigInterruptINepReader successfull\n");

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyConfigInterruptINepReader\n");
	return NtStatus;
}
VOID
CyEvtInterruptINepReaderComplete(
    WDFUSBPIPE  Pipe,
    WDFMEMORY   Buffer,
    size_t      szNumBytesTransferred,
    WDFCONTEXT  Context
    )
{
	PUCHAR          pucIntData = NULL;    
    PDEVICE_CONTEXT pDeviceContext = Context;
	WDFDEVICE WdfDevice;

    UNREFERENCED_PARAMETER(Pipe); 

	WdfDevice = WdfObjectContextGetObject(pDeviceContext);

    if (szNumBytesTransferred == 0) {
        CyTraceEvents(TRACE_LEVEL_WARNING, DBG_INIT,
                    "CyEvtInterruptINepReaderComplete Zero length read "
                    "occured on the Interrupt Pipe's Continuous Reader\n"
                    );
        return;
    }


    ASSERT(szNumBytesTransferred == sizeof(UCHAR));

    pucIntData = WdfMemoryGetBuffer(Buffer, NULL);

    CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_INIT,
                "CyEvtInterruptINepReaderComplete Data %x\n",
                *pucIntData);

    pDeviceContext->ucIntInData = *pucIntData;
    
    CyCompleteIoctlRequest(WdfDevice);
}
VOID
CyCompleteIoctlRequest(
    __in WDFDEVICE WdfDevice
    )
{
	NTSTATUS            NtStatus;
    WDFREQUEST          request;
    PDEVICE_CONTEXT     pDevContext;
    size_t              szBytesReturned = 0;
    PUCHAR              pucData;
	PREQUEST_CONTEXT    pReqContext;

    pDevContext = CyGetDeviceContext(WdfDevice);

    do 
	{
       //check for pending request
        NtStatus = WdfIoQueueRetrieveNextRequest(pDevContext->IntInMsgQ, &request);
        if (NT_SUCCESS(NtStatus))
		{
			pReqContext = CyGetRequestContext(request);
			if(pReqContext->IsNeitherIO)
			{
				pucData = WdfMemoryGetBuffer(pReqContext->OutputMemoryBufferWrite,NULL);
				*pucData = pDevContext->ucIntInData;
				szBytesReturned = sizeof(UCHAR);
			}
			else
			{
				NtStatus = WdfRequestRetrieveOutputBuffer(request,
														sizeof(UCHAR),
														&pucData,
														NULL);// BufferLength

				if (!NT_SUCCESS(NtStatus)) 
				{

					CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL,
						"User's output buffer is too small for this IOCTL, expecting a SWITCH_STATE\n");
					szBytesReturned = sizeof(UCHAR);

				}
				else
				{
				   *pucData = pDevContext->ucIntInData;
					szBytesReturned = sizeof(UCHAR);
				}
			}

            WdfRequestCompleteWithInformation(request, NtStatus, szBytesReturned);
            NtStatus = STATUS_SUCCESS;

        }
		else if (NtStatus != STATUS_NO_MORE_ENTRIES)
		{
            CyTraceEvents(TRACE_LEVEL_ERROR,DBG_IOCTL,"WdfIoQueueRetrieveNextRequest status %08x\n", NtStatus);
        }
        request = NULL;
    } while (NtStatus == STATUS_SUCCESS);

}