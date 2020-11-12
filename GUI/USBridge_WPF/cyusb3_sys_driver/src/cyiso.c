/*
 ## Cypress CyUSB3 driver source file (cyiso.c)
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

#include "..\inc\cypnppower.h"
#include "..\inc\cydevice.h"
#include "..\inc\cytrace.h"
#include "..\inc\cydef.h"
#include "..\inc\cyiso.h"
#if (NTDDI_VERSION < NTDDI_WIN7)
	#include "..\inc\CyUSB30_def.h"
#else
	#ifndef USB30MAJORVER
		#define USB30MAJORVER 0x0300
	#else
		#include "..\inc\CyUSB30_def.h"
	#endif

	#ifndef USB20MAJORVER
		#define USB20MAJORVER 0x0200
	#endif
#endif

#include <wdf.h>
#include <wdfusb.h>
#include <ntddk.h>
#include <ntintsafe.h>

#if defined(EVENT_TRACING)
#include "cyiso.tmh"
#endif

#define ISO_FRAME_LATENCY 7 // 7 frame latency
#define ISO_READWRITE_STAGESIZE_HIGH_SPEED  0x300000
#define ISO_READWRITE_STAGESIZE_SS_SPEED  0x300000
//static functions
static NTSTATUS CyIsoFullSpeedRW(WDFREQUEST Request,
								 PDEVICE_CONTEXT pDevContext,
								 WDFUSBPIPE UsbPipeHandle,
								 BOOLEAN IsRead,
								 BOOLEAN bIsDirectMethod);
static NTSTATUS CyIsoHighSpeedRW(WDFREQUEST Request,
								 PDEVICE_CONTEXT pDevContext,
								 WDFUSBPIPE UsbPipeHandle,								 
								 BOOLEAN IsRead,
								 BOOLEAN bIsDirectMethod);
static NTSTATUS CyIsoSuperSpeedRW(WDFREQUEST Request,
								 PDEVICE_CONTEXT pDevContext,
								 WDFUSBPIPE UsbPipeHandle,								 
								 BOOLEAN IsRead,
								 BOOLEAN bIsDirectMethod);
static ULONG CyGetCurrentFrame(IN PDEVICE_CONTEXT pDevContext);
static UCHAR GetMaxburst(IN PDEVICE_CONTEXT pDevContext,IN WDF_USB_PIPE_INFORMATION UsbPipeInfo);
static UCHAR GetMultSS(IN PDEVICE_CONTEXT pDevContext,IN WDF_USB_PIPE_INFORMATION UsbPipeInfo);
static USHORT GetMaxPacketSizeSS(IN PDEVICE_CONTEXT pDevContext,IN WDF_USB_PIPE_INFORMATION UsbPipeInfo);


static VOID
DoSubRequestCleanup(
    PREQUEST_CONTEXT    MainRequestContext,
    PLIST_ENTRY         SubRequestsList,
    PBOOLEAN            CompleteRequest
    );

NTSTATUS CyIsoReadWrite(WDFREQUEST Request,PDEVICE_CONTEXT pDevContext,WDFUSBPIPE UsbPipeHandle,BOOLEAN bIsDirectMethod)
{
	NTSTATUS NtStatus=STATUS_SUCCESS;

	//Using the bcdUSB of USBdevice descriptor to check USB version and decide the device speed.
	if(pDevContext->UsbDeviceDescriptor.bcdUSB  == USB30MAJORVER)
	{// USB3.0 device(SS Speed)
		NtStatus = CyIsoSuperSpeedRW(Request,pDevContext,UsbPipeHandle,WdfUsbTargetPipeIsInEndpoint(UsbPipeHandle),bIsDirectMethod);
	}
	else
	{
		if(pDevContext->ulUSBDeviceTrait & WDF_USB_DEVICE_TRAIT_AT_HIGH_SPEED)
		{//High speed
			NtStatus = CyIsoHighSpeedRW(Request,pDevContext,UsbPipeHandle,WdfUsbTargetPipeIsInEndpoint(UsbPipeHandle),bIsDirectMethod);
		}
		else
		{//full speed
			NtStatus = CyIsoFullSpeedRW(Request,pDevContext,UsbPipeHandle,WdfUsbTargetPipeIsInEndpoint(UsbPipeHandle),bIsDirectMethod);
		}
	}
	return NtStatus;
}

NTSTATUS CyIsoFullSpeedRW(WDFREQUEST Request,PDEVICE_CONTEXT pDevContext,WDFUSBPIPE UsbPipeHandle,BOOLEAN IsRead,BOOLEAN bIsDirectMethod)
{
	NTSTATUS				 NtStatus = STATUS_SUCCESS;
    WDF_OBJECT_ATTRIBUTES    Attributes;
    WDFREQUEST               SubRequest = NULL;    
    ULONG                    iIndex, jIndex;
	WDF_USB_PIPE_INFORMATION UsbPipeInfo;
	ULONG					 ulPacketSize=0;
	size_t					 ulTotalLenght,ulBufferoffset,ulStageLength,ulNumOfSubRequest;
	WDFCOLLECTION            hCollection = NULL;
    WDFSPINLOCK              hSpinLock = NULL;	
	PREQUEST_CONTEXT         pMainReqContext =NULL;
    PSUB_REQUEST_CONTEXT     pSubReqContext = NULL;
	PMDL                     pMainMdl;
	PUCHAR                   ulpVA; /* Virtual address */
	PLIST_ENTRY              pthisEntry;
    LIST_ENTRY               SubRequestsList;
	BOOLEAN                  IsCancelable;
	PSINGLE_TRANSFER		 pSingleTransfer;	
	ISO_ADV_PARAMS           IsoParams;
	USBD_PIPE_HANDLE		 wdmhUSBPipe;
	size_t szInputBuf=0,szOutputBuf=0;

    IsCancelable = FALSE;

	InitializeListHead(&SubRequestsList);
	pMainReqContext = CyGetRequestContext(Request);	

	if(pMainReqContext->IsNeitherIO)
	{//NEITHER IO
		pSingleTransfer = WdfMemoryGetBuffer(pMainReqContext->InputMemoryBufferWrite, &szInputBuf); // Input buffer		
		ulTotalLenght = pMainReqContext->ulszOutputMemoryBuffer;
	}
	else
	{//BUFFERED
		WDFMEMORY MemoryObject=NULL;
		PVOID buffer=NULL;
		NtStatus = WdfRequestRetrieveOutputMemory(Request,
												 &MemoryObject);
		if(!NT_SUCCESS(NtStatus))
		{
		   CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestRetrieveInputMemory failed 0x%x\n", NtStatus);					    
		   WdfRequestCompleteWithInformation(Request, NtStatus, 0);	 		   
		   return NtStatus;
		}
		// get buffer point from memory object
		pSingleTransfer = WdfMemoryGetBuffer(MemoryObject, NULL);
		pMainReqContext->InputMemoryBufferWrite = MemoryObject;		// Input buffer -> PSINGLE_TRANSFER needs to be updated to while returning request to caller application
		WdfRequestRetrieveOutputBuffer(Request,0,&buffer,&ulTotalLenght);
		ulTotalLenght-=pSingleTransfer->BufferOffset;
		CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"BUFFERED IO TotalLenght :%x\n",ulTotalLenght);

	}

	// get buffer lenght and offset		
	pMainReqContext->ulLastIsoPktIndex = 0; //Reset the iso last packet index
	pMainReqContext->ulNoOfIsoUserRequestPkt=(pSingleTransfer->IsoPacketLength)/sizeof(ISO_PACKET_INFO);//Reset the iso last transfer user provided buffer length
	
	//create collection object
	WDF_OBJECT_ATTRIBUTES_INIT(&Attributes);
    Attributes.ParentObject = Request;
    NtStatus = WdfCollectionCreate(&Attributes,
                                &hCollection);
    if (!NT_SUCCESS(NtStatus))
	{
        CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfCollectionCreate failed 0x%x\n", NtStatus);
        goto Exit;
    }

    WDF_OBJECT_ATTRIBUTES_INIT(&Attributes);
    Attributes.ParentObject = hCollection;
    NtStatus = WdfSpinLockCreate(&Attributes, &hSpinLock);
    if (!NT_SUCCESS(NtStatus))
	{
        CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfSpinLockCreate failed 0x%x\n", NtStatus);
        goto Exit;
    }

	WDF_USB_PIPE_INFORMATION_INIT(&UsbPipeInfo);
    WdfUsbTargetPipeGetInformation(UsbPipeHandle, &UsbPipeInfo);
	ulPacketSize = UsbPipeInfo.MaximumPacketSize;
	CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"ulTotalLenght = %d\n", ulTotalLenght);
    CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"MaxpacketSize = %d\n", ulPacketSize);
    if(ulTotalLenght > (ulPacketSize * 255))
        ulStageLength = ulPacketSize * 255;
    else
        ulStageLength = ulTotalLenght;

    CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"CyIsoFullSpeedRW::Stage size = %d\n", ulStageLength);

    //find number of stages required to finish the request
	if(ulTotalLenght==0)
	{
		ulNumOfSubRequest =1;
		CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"CyIsoFullSpeedRW  Zero Lenght Packet= %x\n", ulTotalLenght);
	}
	else
		ulNumOfSubRequest = (ulTotalLenght + ulStageLength - 1) / ulStageLength;
	

    CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"CyIsoFullSpeedRW  Number of subrequest= %d\n", ulNumOfSubRequest);

	pMainReqContext->SubRequestCollection = hCollection;
    pMainReqContext->SubRequestCollectionLock = hSpinLock;

	//TODO : Check in Neither IO buffer method the WdmMdl work properly or not, if not then create MDL with Buffer address
    if(!pMainReqContext->IsNeitherIO)
	{//BUFFERED IO
		WDFMEMORY WdfMemory=NULL;
		NtStatus = WdfRequestRetrieveOutputWdmMdl(Request, &pMainMdl);
		if(!NT_SUCCESS(NtStatus))
		{
			CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"WdfRequestRetrieveOutputWdmMdl failed %x\n", NtStatus);
			goto Exit;
		}
		ulpVA = (PUCHAR) MmGetMdlVirtualAddress(pMainMdl);
		// Get the buffer pointer at the Data buffer
		ulpVA+=pSingleTransfer->BufferOffset;
		pMainReqContext->Mdl = NULL; // No allocated MDL
		CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"NO USER ALLOCATED MDL\n");
	}
	else
	{//NEITHER IO
		if(ulTotalLenght)
		{
			if(IsRead)
				ulpVA = (PUCHAR) WdfMemoryGetBuffer(pMainReqContext->OutputMemoryBufferWrite, NULL);
			else
				ulpVA = (PUCHAR) WdfMemoryGetBuffer(pMainReqContext->OutputMemoryBufferRead, NULL); 
			
			pMainMdl = IoAllocateMdl(ulpVA,ulTotalLenght,FALSE,TRUE,NULL);
			if(!pMainMdl)
			{
				CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"IoAllocateMdl failed\n");
				goto Exit;
			}
			MmBuildMdlForNonPagedPool(pMainMdl);		
			ulpVA = (PUCHAR) MmGetMdlVirtualAddress(pMainMdl);
			pMainReqContext->Mdl = pMainMdl;
		}
		else
		{
			ulpVA = NULL;
			pMainMdl = NULL;
			pMainReqContext->Mdl = NULL;			
		}
	}
    

	for(iIndex = 0; iIndex < ulNumOfSubRequest; iIndex++)
	{
        WDFMEMORY               pSubUrbMemory;
        PURB                    pSubUrb;
        PMDL                    pSubMdl = NULL;
        ULONG                   ulnPackets;
        ULONG                   ulSize;
        ULONG                   ulOffset;
		
		if(ulPacketSize==0)
			ulnPackets = 0;
		else
			ulnPackets = (ulStageLength + ulPacketSize - 1) / ulPacketSize;

		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "nPackets = %d for Irp/URB pair %d\n", ulnPackets, iIndex);                    
        ASSERT(ulnPackets <= 255);
        ulSize = GET_ISO_URB_SIZE(ulnPackets);
        WDF_OBJECT_ATTRIBUTES_INIT(&Attributes);
        WDF_OBJECT_ATTRIBUTES_SET_CONTEXT_TYPE(&Attributes, SUB_REQUEST_CONTEXT);
		Attributes.ParentObject = Request;

        NtStatus = WdfRequestCreate(
            &Attributes,
			WdfUsbTargetDeviceGetIoTarget(pDevContext->CyUsbDevice),
            &SubRequest);
        if(!NT_SUCCESS(NtStatus))
		{
			CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestCreate failed 0x%x\n", NtStatus);                       
            goto Exit;
        }

		pSubReqContext = CyGetSubRequestContext(SubRequest);
        pSubReqContext->UserRequest = Request; //TODO : Check , is it subrequest or main request
        
        // Allocate memory for URB        
        WDF_OBJECT_ATTRIBUTES_INIT(&Attributes);
		Attributes.ParentObject = SubRequest;
        NtStatus = WdfMemoryCreate(
                            &Attributes,
                            NonPagedPool,
                            CYMEM_TAG,
                            ulSize,
                            &pSubUrbMemory,
                            (PVOID*) &pSubUrb);

        if (!NT_SUCCESS(NtStatus)) 
		{
            WdfObjectDelete(SubRequest);
			CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "Failed to alloc MemoryBuffer for suburb 0x%x\n", NtStatus);              
            goto Exit;
        }
		// Initialize urb to zero
		RtlZeroMemory(pSubUrb,ulSize);
        pSubReqContext->SubUrb = pSubUrb;
        
		if(ulTotalLenght)
		{
			// Allocate a mdl and build the MDL to describe the staged buffer.        
			pSubMdl = IoAllocateMdl((PVOID) ulpVA,
								ulStageLength,
								FALSE,
								FALSE,
								NULL);

			if(pSubMdl == NULL) 
			{
				CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "Fail to allocate memory for the mdl");            
				WdfObjectDelete(SubRequest);			
				NtStatus = STATUS_INSUFFICIENT_RESOURCES;
				goto Exit;
			}

			IoBuildPartialMdl(pMainMdl,
							  pSubMdl,
							  (PVOID) ulpVA,
							  ulStageLength);
			pSubReqContext->SubMdl = pSubMdl;
			ulpVA += ulStageLength;
			ulTotalLenght -= ulStageLength;

			pSubUrb->UrbIsochronousTransfer.TransferBufferLength = ulStageLength;
			pSubUrb->UrbIsochronousTransfer.TransferBufferMDL = pSubMdl;
			pSubUrb->UrbIsochronousTransfer.TransferBuffer = NULL;
		}
		else
		{// Total length ZERO
			pSubUrb->UrbIsochronousTransfer.TransferBufferLength = 0;
			pSubUrb->UrbIsochronousTransfer.TransferBufferMDL = NULL;
			pSubUrb->UrbIsochronousTransfer.TransferBuffer = NULL;
			pSubReqContext->SubMdl = NULL;
		}
		
        
        // Initialize the subsidiary urb        
		wdmhUSBPipe = WdfUsbTargetPipeWdmGetPipeHandle(UsbPipeHandle);
		pSubUrb->UrbIsochronousTransfer.Hdr.Length = (USHORT) ulSize;
        pSubUrb->UrbIsochronousTransfer.Hdr.Function = URB_FUNCTION_ISOCH_TRANSFER;
        pSubUrb->UrbIsochronousTransfer.PipeHandle = wdmhUSBPipe;
        if(IsRead)		
            pSubUrb->UrbIsochronousTransfer.TransferFlags =
                                                     USBD_TRANSFER_DIRECTION_IN;
        else
            pSubUrb->UrbIsochronousTransfer.TransferFlags =
                                                     USBD_TRANSFER_DIRECTION_OUT;

        

#if 0
        //
        // This is a way to set the start frame and NOT specify ASAP flag.
        //
        NtStatus = WdfUsbTargetDeviceRetrieveCurrentFrameNumber(wdfUsbDevice, &frameNumber);
        subUrb->UrbIsochronousTransfer.StartFrame = frameNumber  + SOME_LATENCY;
#endif  

		IsoParams = pSingleTransfer->IsoParams;

		if ((IsoParams.isoId == USB_ISO_ID) && (IsoParams.isoCmd == USB_ISO_CMD_SET_FRAME))
        {            
            pSubUrb->UrbIsochronousTransfer.StartFrame = IsoParams.ulParam1;
        }
        else if ((IsoParams.isoId == USB_ISO_ID) && (IsoParams.isoCmd == USB_ISO_CMD_CURRENT_FRAME))
        {            
			pSubUrb->UrbIsochronousTransfer.StartFrame = CyGetCurrentFrame(pDevContext) + IsoParams.ulParam1;
        }
        else
        {
            /* default to ASAP transfer */
            pSubUrb->UrbIsochronousTransfer.TransferFlags |=  USBD_START_ISO_TRANSFER_ASAP;
            pSubUrb->UrbIsochronousTransfer.StartFrame = 0;
        }      

		pSubUrb->UrbIsochronousTransfer.NumberOfPackets = ulnPackets;
        pSubUrb->UrbIsochronousTransfer.UrbLink = NULL;
        //
        // set the offsets for every packet for reads/writes
        //
        if(IsRead)
		{
            ulOffset = 0;
            for(jIndex = 0; jIndex < ulnPackets; jIndex++)
			{
                pSubUrb->UrbIsochronousTransfer.IsoPacket[jIndex].Offset = ulOffset;
                pSubUrb->UrbIsochronousTransfer.IsoPacket[jIndex].Length = 0;
                if(ulStageLength > ulPacketSize) 
				{
					ulOffset += ulPacketSize;
					ulStageLength -= ulPacketSize;
                }
                else
				{
					ulOffset += ulStageLength;
					ulStageLength = 0;
                }
            }
        }
        else
		{
			ulOffset = 0;
			for(jIndex = 0; jIndex < ulnPackets; jIndex++) {

                pSubUrb->UrbIsochronousTransfer.IsoPacket[jIndex].Offset = ulOffset;
				if(ulStageLength > ulPacketSize)
				{
					pSubUrb->UrbIsochronousTransfer.IsoPacket[jIndex].Length = ulPacketSize;
					ulOffset += ulPacketSize;
					ulStageLength -= ulPacketSize;
                }
                else {

					pSubUrb->UrbIsochronousTransfer.IsoPacket[jIndex].Length = ulStageLength;
					ulOffset += ulStageLength;
					ulStageLength = 0;
					ASSERT(ulOffset == (pSubUrb->UrbIsochronousTransfer.IsoPacket[jIndex].Length +
                                      pSubUrb->UrbIsochronousTransfer.IsoPacket[jIndex].Offset));
                }
            }
        }


        //
        // Associate the URB with the request.
        //
		NtStatus = WdfUsbTargetPipeFormatRequestForUrb(UsbPipeHandle,
                                          SubRequest,
										  pSubUrbMemory,
                                          NULL);

		if (!NT_SUCCESS(NtStatus))
		{
			CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "Failed to format requset for urb\n");                        
            WdfObjectDelete(SubRequest);
			WdfObjectDelete(pSubUrb);
            IoFreeMdl(pSubMdl);
            goto Exit;
        }

        WdfRequestSetCompletionRoutine(SubRequest,
                                       CySubRequestCompletionRoutine, //TODO
                                       pMainReqContext);

		if(ulTotalLenght > (ulPacketSize * 255)) {

			ulStageLength = ulPacketSize * 255;
        }
        else {

			ulStageLength = ulTotalLenght;
        }

        //
        // WdfCollectionAdd takes a reference on the request object and removes
        // it when you call WdfCollectionRemove.
        //
		NtStatus = WdfCollectionAdd(hCollection, SubRequest);
		if (!NT_SUCCESS(NtStatus)) 
		{
			CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfCollectionAdd failed 0x%x\n", NtStatus);            
            WdfObjectDelete(SubRequest);			
            IoFreeMdl(pSubMdl);
            goto Exit;
        }
		InsertTailList(&SubRequestsList, &pSubReqContext->ListEntry);
    }

	WdfObjectReference(Request);
    
    // Mark the main request cancelable so that we can cancel the subrequests
    // if the main requests gets cancelled for any reason.    
    WdfRequestMarkCancelable(Request, CyEvtRequestCancel); //TODO
    IsCancelable = TRUE;

    while(!IsListEmpty(&SubRequestsList)) 
	{

        pthisEntry = RemoveHeadList(&SubRequestsList);
        pSubReqContext = CONTAINING_RECORD(pthisEntry, SUB_REQUEST_CONTEXT, ListEntry);
		SubRequest = WdfObjectContextGetObject(pSubReqContext);
		CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Sending subRequest 0x%p\n", SubRequest);            
        
        if (WdfRequestSend(SubRequest, WdfUsbTargetPipeGetIoTarget(UsbPipeHandle), WDF_NO_SEND_OPTIONS) == FALSE)
		{
            NtStatus = WdfRequestGetStatus(SubRequest);
            // 
            // Insert the subrequest back into the subrequestlist so cleanup can find it and delete it
            //
			InsertHeadList(&SubRequestsList, &pSubReqContext->ListEntry);
			CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestSend failed with status code 0x%x\n", NtStatus);            
            ASSERT(!NT_SUCCESS(NtStatus));
            goto Exit;
        }
	}
Exit:
	if(NT_SUCCESS(NtStatus) && IsListEmpty(&SubRequestsList)) 
	      return NtStatus;
    else
	{
        BOOLEAN  bCompleteRequest;
        NTSTATUS tempNtStatus;
        bCompleteRequest = TRUE;
        tempNtStatus = STATUS_SUCCESS;

        if(hCollection)
			DoSubRequestCleanup(pMainReqContext, &SubRequestsList, &bCompleteRequest);   //TODO        

        if (bCompleteRequest)
		{
            if (IsCancelable)
			{
                // Mark the main request as not cancelable before completing it.
                tempNtStatus = WdfRequestUnmarkCancelable(Request);
                if (NT_SUCCESS(tempNtStatus))
				{
                    // If WdfRequestUnmarkCancelable returns STATUS_SUCCESS 
                    // that means the cancel routine has been removed. In that case
                    // we release the reference otherwise the cancel routine does it.
                    WdfObjectDereference(Request);
                }
            }            
			if (pMainReqContext->Numxfer > 0 )
				WdfRequestCompleteWithInformation(Request, STATUS_SUCCESS, pMainReqContext->Numxfer);            
            else
                WdfRequestCompleteWithInformation(Request, NtStatus, pMainReqContext->Numxfer); 
        }
    }
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIsoFullSpeedRW \n");    
	return NtStatus;
}
NTSTATUS CyIsoHighSpeedRW(WDFREQUEST Request,PDEVICE_CONTEXT pDevContext,WDFUSBPIPE UsbPipeHandle,BOOLEAN IsRead,BOOLEAN bIsDirectMethod)
{
	NTSTATUS NtStatus = STATUS_SUCCESS;
	ULONG                   ulNumberOfPackets;
    ULONG                   ulActualPackets;
    //ULONG                   ulMinDataInEachPacket;
    //ULONG                   ulDataLeftToBeDistributed;
    //ULONG                   ulNumberOfPacketsFilledToBrim;
    ULONG                   ulPacketSize;
    ULONG                   ulNumSubRequests;
	size_t					ulTotalLenght,ulBufferoffset;
    ULONG                   ulStageSize;
    PUCHAR                  ulpVA = NULL;    
    PREQUEST_CONTEXT        pMainReqContext;
    PMDL                    pMainMdl = NULL;
    WDF_USB_PIPE_INFORMATION   UsbPipeInfo;    
    WDFCOLLECTION           hCollection = NULL;
    WDFSPINLOCK             hSpinLock = NULL;    
    WDF_OBJECT_ATTRIBUTES   attributes;
    WDFREQUEST              SubRequest;
    PSUB_REQUEST_CONTEXT    pSubReqContext;
    ULONG                   iIndex, jIndex;
    PLIST_ENTRY             pthisEntry;
    LIST_ENTRY              SubRequestsList;    
    BOOLEAN                 IsCancelable;
	PSINGLE_TRANSFER        pSingleTrasfer;
	ISO_ADV_PARAMS			IsoParams;	
	USBD_PIPE_HANDLE		wdmhUSBPipe;
	PUCHAR pucOutPutBuffer=NULL;
	size_t szInputBuf=0,szOutputBuf=0;
	WDF_REQUEST_SEND_OPTIONS     RequestSendOptions;
	ULONG ulNoOfFullPacket=0; // This will store the number of packets hold Maximum packet size data
	ULONG ulNoOfZeroLenghtPacket=0; // This will store the number of Zero length packets
	ULONG ulPartialDataPacketLength=0; //This will stare the partial data length if length is not multiple of mamimum packet size
	ULONG ulIndexOfPartialDtPktLen=0; //This will store the index of partial data length packet
	ULONG ulStartIndexOfZeroLength=0; //This will store ZERO Length packet index
	ULONG ulPACKETS_PER_STAGE =0;

	
	CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"Start  CyIsoHighSpeedRW \n");

	IsCancelable = FALSE;    
	InitializeListHead(&SubRequestsList); 
    pMainReqContext = CyGetRequestContext(Request);	

	if(pMainReqContext->IsNeitherIO)
	{// NEITHER IO
		pSingleTrasfer = WdfMemoryGetBuffer(pMainReqContext->InputMemoryBufferWrite, &szInputBuf); // Input buffer		
		ulTotalLenght = pMainReqContext->ulszOutputMemoryBuffer;
	}
	else
	{//BUFFERED IO
		WDFMEMORY MemoryObject=NULL;
		PVOID buffer=NULL;
		NtStatus = WdfRequestRetrieveOutputMemory(Request,
												 &MemoryObject);
		if(!NT_SUCCESS(NtStatus))
		{
		   CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestRetrieveInputMemory failed 0x%x\n", NtStatus);					    
		   WdfRequestCompleteWithInformation(Request, NtStatus, 0);	 		   
		   return NtStatus;
		}
		// get buffer point from memory object
		pSingleTrasfer = WdfMemoryGetBuffer(MemoryObject, NULL);
		pMainReqContext->InputMemoryBufferWrite = MemoryObject;		
		WdfRequestRetrieveOutputBuffer(Request,0,&buffer,&ulTotalLenght);
		ulTotalLenght-=pSingleTrasfer->BufferOffset;
		if(ulTotalLenght<=0)
		{
		   WdfRequestCompleteWithInformation(Request, STATUS_INVALID_PARAMETER, 0);	 		   
		   return STATUS_INVALID_PARAMETER;
		}

		CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"BUFFERED IO TotalLenght :%x\n",ulTotalLenght);
	}
	// We only support one request at a time for the isochronous transfer and the limit for that is 1024 frams for transfer
	if(ulTotalLenght>ISO_READWRITE_STAGESIZE_SS_SPEED)
	{
		WdfRequestCompleteWithInformation(Request, STATUS_INVALID_BUFFER_SIZE, 0);	
		CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"TotalLenght :%x Is greater than the 1024 frams per transfer\n",ulTotalLenght);
		return STATUS_INVALID_BUFFER_SIZE;
	}

	pMainReqContext->ulLastIsoPktIndex = 0; //Reset the iso last packet index
	if(pSingleTrasfer->IsoPacketOffset)
		pMainReqContext->ulNoOfIsoUserRequestPkt = (pSingleTrasfer->IsoPacketLength)/sizeof(ISO_PACKET_INFO);//Reset the iso last transfer user provided buffer length
	else
		pMainReqContext->ulNoOfIsoUserRequestPkt = 0;

	CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"ulNoOfIsoUserRequestPkt = %d IsoPacketLength=%d\n", pMainReqContext->ulNoOfIsoUserRequestPkt,pSingleTrasfer->IsoPacketLength);
    //
    // Create a collection to store all the sub requests.
    //
    WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
    attributes.ParentObject = Request;
    NtStatus = WdfCollectionCreate(&attributes,
                                &hCollection);
    if (!NT_SUCCESS(NtStatus)){
		CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"WdfCollectionCreate failed with status 0x%x\n", NtStatus);
        goto Exit;
    }

    WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
    attributes.ParentObject = hCollection;
    NtStatus = WdfSpinLockCreate(&attributes, &hSpinLock);
    if (!NT_SUCCESS(NtStatus)){
		CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"WdfSpinLockCreate failed with status 0x%x\n", NtStatus);
        goto Exit;
    }
    WDF_USB_PIPE_INFORMATION_INIT(&UsbPipeInfo);
    WdfUsbTargetPipeGetInformation(UsbPipeHandle, &UsbPipeInfo);
	ulPacketSize = UsbPipeInfo.MaximumPacketSize;
	//

	if(ulTotalLenght==0)
	{
		ulActualPackets =8;
		ulNoOfFullPacket = 0;
		ulNoOfZeroLenghtPacket =8;
		ulStartIndexOfZeroLength =0;
		ulPartialDataPacketLength =0;
		ulIndexOfPartialDtPktLen = 0;
	}
	else
	{
		//ulNumberOfPackets = (ulTotalLenght + ulPacketSize - 1) / ulPacketSize;			
		ulNoOfFullPacket = (ulTotalLenght/ulPacketSize);
		if(ulNoOfFullPacket==0)
		{
			ulPartialDataPacketLength = (ulTotalLenght%ulPacketSize);	
			ulIndexOfPartialDtPktLen = 0; 
			ulNoOfZeroLenghtPacket = 7;
			ulStartIndexOfZeroLength =1;		
		}
		else if((((ulNoOfFullPacket%8)==0)&&(ulTotalLenght%ulPacketSize))||(ulNoOfFullPacket%8)!=0)
		{
			ulPartialDataPacketLength = (ulTotalLenght%ulPacketSize);	
			ulIndexOfPartialDtPktLen = (ulPartialDataPacketLength!=0)? ulNoOfFullPacket : 0; 
			ulNoOfZeroLenghtPacket = (8-(ulNoOfFullPacket%8))-((ulPartialDataPacketLength!=0)? 1:0);
			ulStartIndexOfZeroLength =(ulPartialDataPacketLength!=0)? (ulNoOfFullPacket+1):ulNoOfFullPacket;					
		}
		else
		{		
			ulNoOfZeroLenghtPacket =0;
			ulStartIndexOfZeroLength =0;
			ulPartialDataPacketLength =0;
			ulIndexOfPartialDtPktLen = 0;		
		}		
	}

	if(((ulNoOfFullPacket%8)!=0) || (ulTotalLenght<ulPacketSize))
	{//8th packet
		if((((ulNoOfFullPacket+1)%8)==0) &&(ulNoOfZeroLenghtPacket==0))
		{//7th packet
			CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"Mutliple of eight or last packet is of partial length \n");
		}		
		else
		{
			if(ulTotalLenght!=0)
			{
				CyTraceEvents(TRACE_LEVEL_ERROR,DBG_IOCTL,"User request does not satisfy minimum required(multiple of 8) packets for High Speed Isochronous transfer\n" );
				NtStatus =  STATUS_INVALID_PARAMETER;
				WdfRequestCompleteWithInformation(Request, NtStatus, pMainReqContext->Numxfer); 
				return NtStatus;
			}
		}
	}

	if((ulNoOfFullPacket%8==0)&& ulPartialDataPacketLength)
	{// Number of Packet 8 and first packe is partial - Return invalid packets.
		CyTraceEvents(TRACE_LEVEL_ERROR,DBG_IOCTL,"User request does not satisfy minimum required(multiple of 8) packets for High Speed Isochronous transfer\n" );
		NtStatus =  STATUS_INVALID_PARAMETER;
		WdfRequestCompleteWithInformation(Request, NtStatus, pMainReqContext->Numxfer); 
		return NtStatus;
	}

	ulActualPackets = ulNoOfFullPacket + ulNoOfZeroLenghtPacket + ((ulPartialDataPacketLength!=0)? 1 : 0);
	CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL, "ulTotalLenght = %d\n", ulTotalLenght);
	CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL, "ulNoOfFullPacket = %d\n", ulNoOfFullPacket);
	CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL, "ulActualPackets = %d\n", ulActualPackets);
    //
    // determine how many stages of transfer needs to be done.
    // in other words, how many irp/urb pairs required.
    // this irp/urb pair is also called the subsidiary irp/urb pair
    //
	ulPACKETS_PER_STAGE = ISO_READWRITE_STAGESIZE_HIGH_SPEED/ulPacketSize;
	if(ulPACKETS_PER_STAGE%8!=0)
	{// ulPACKETS_PER_STAGE shuold be multiple of 8, if is not then adjust it to 8, this is the case when user mamimum packet size random(like 450,766)
		ulPACKETS_PER_STAGE-= (ulPACKETS_PER_STAGE%8);
	}


    //ulNumSubRequests = (ulActualPackets + 1023) / 1024;
	ulNumSubRequests = (ulActualPackets + (ulPACKETS_PER_STAGE-1)) / ulPACKETS_PER_STAGE;

	CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"PeformHighSpeedIsochTransfer::ulNumSubRequests = %d ulPACKETS_PER_STAGE=%d ulPacketSize=%d\n", ulNumSubRequests,ulPACKETS_PER_STAGE,ulPacketSize);

    pMainReqContext->SubRequestCollection = hCollection;
    pMainReqContext->SubRequestCollectionLock = hSpinLock;
    
	if(!pMainReqContext->IsNeitherIO)
	{// BUFFERED IO
		WDFMEMORY WdfMemory=NULL;
		NtStatus = WdfRequestRetrieveOutputWdmMdl(Request, &pMainMdl);
		if(!NT_SUCCESS(NtStatus))
		{
			CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"WdfRequestRetrieveOutputWdmMdl failed %x\n", NtStatus);
			goto Exit;
		}
		ulpVA = (PUCHAR) MmGetMdlVirtualAddress(pMainMdl);
		// Get the buffer pointer at the Data buffer
		ulpVA+=pSingleTrasfer->BufferOffset;
		pMainReqContext->Mdl = NULL; // No allocated MDL
		CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"NO USER ALLOCATED MDL\n");
	}
	else // neither io
	{// NEITHER IO
		if(ulTotalLenght==0)
		{
			CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"Requested length is ZERO\n");
		}
		else
		{
			if(IsRead)
				ulpVA = (PUCHAR) WdfMemoryGetBuffer(pMainReqContext->OutputMemoryBufferWrite, NULL);
			else
				ulpVA = (PUCHAR) WdfMemoryGetBuffer(pMainReqContext->OutputMemoryBufferRead, NULL);

			pMainMdl = IoAllocateMdl(ulpVA,ulTotalLenght,FALSE,TRUE,NULL);
			if(!pMainMdl)
			{
				CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"IoAllocateMdl failed\n");
				goto Exit;
			}
			MmBuildMdlForNonPagedPool(pMainMdl);		
			ulpVA = (PUCHAR) MmGetMdlVirtualAddress(pMainMdl);
			pMainReqContext->Mdl = pMainMdl;
		}
	}

	for(iIndex = 0; iIndex < ulNumSubRequests; iIndex++)
	{

		WDFMEMORY               subUrbMemory;
		PURB                    subUrb;
		PMDL                    subMdl = NULL;
		ULONG                   nPackets;
		ULONG                   siz;
		ULONG                   offset;
		
		if(ulActualPackets <= ulPACKETS_PER_STAGE/*1024*/) 
		{

			nPackets = ulActualPackets;
			ulActualPackets = 0;
			ulStageSize = (ulNoOfFullPacket * ulPacketSize)+(ulPartialDataPacketLength);
		}
		else {

			nPackets =ulPACKETS_PER_STAGE; //1024;
			ulActualPackets -= ulPACKETS_PER_STAGE;// 1024;				
			if(((ulPartialDataPacketLength)&&(ulIndexOfPartialDtPktLen<=ulPACKETS_PER_STAGE)) ||((ulNoOfZeroLenghtPacket)&&(ulStartIndexOfZeroLength<=ulPACKETS_PER_STAGE)))
				ulStageSize = (ulNoOfFullPacket * ulPacketSize)+(ulPartialDataPacketLength);
			else
				ulStageSize = (nPackets * ulPacketSize);
		}
		ASSERT(nPackets <= ulPACKETS_PER_STAGE /*1024*/);

		siz = GET_ISO_URB_SIZE(nPackets);


		WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
		WDF_OBJECT_ATTRIBUTES_SET_CONTEXT_TYPE(&attributes, SUB_REQUEST_CONTEXT);
		attributes.ParentObject = Request; 

		NtStatus = WdfRequestCreate(
			&attributes,
			WdfUsbTargetDeviceGetIoTarget(pDevContext->CyUsbDevice),
			&SubRequest);

		if(!NT_SUCCESS(NtStatus)) {

			CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"WdfRequestCreate failed 0x%x\n", NtStatus);
			goto Exit;
		}

		pSubReqContext = CyGetSubRequestContext(SubRequest);
		pSubReqContext->UserRequest = Request;

		//
		// Allocate memory for URB.
		//
		WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
		attributes.ParentObject = SubRequest;
		NtStatus = WdfMemoryCreate(
							&attributes,
							NonPagedPool,
							CYMEM_TAG,
							siz,
							&subUrbMemory,
							(PVOID*) &subUrb);

		if (!NT_SUCCESS(NtStatus)) {
			WdfObjectDelete(SubRequest);
			CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"Failed to alloc MemoryBuffer for suburb\n");
			goto Exit;
		}
		// Initialize urb to zero
		RtlZeroMemory(subUrb,siz);
		pSubReqContext->SubUrb = subUrb;
		if(ulTotalLenght==0)
		{
			subUrb->UrbIsochronousTransfer.TransferBufferMDL = NULL;
			subUrb->UrbIsochronousTransfer.TransferBuffer  = NULL;
			subUrb->UrbIsochronousTransfer.TransferBufferLength = 0;
		}
		else
		{
			subMdl = IoAllocateMdl((PVOID) ulpVA,
								   ulStageSize,
								   FALSE,
								   FALSE,
								   NULL);

			if(subMdl == NULL) {

				CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"failed to alloc mem for sub context mdl\n");
				WdfObjectDelete(SubRequest);				
				NtStatus = STATUS_INSUFFICIENT_RESOURCES;
				goto Exit;
			}

			IoBuildPartialMdl(pMainMdl,
							  subMdl,
							  (PVOID) ulpVA,
							  ulStageSize);

			pSubReqContext->SubMdl = subMdl;
			subUrb->UrbIsochronousTransfer.TransferBufferMDL = subMdl;
			subUrb->UrbIsochronousTransfer.TransferBufferLength = ulStageSize;
			ulpVA += ulStageSize;
			ulTotalLenght -= ulStageSize;
		}

		wdmhUSBPipe = WdfUsbTargetPipeWdmGetPipeHandle(UsbPipeHandle);
		subUrb->UrbIsochronousTransfer.Hdr.Length = (USHORT) siz;
		subUrb->UrbIsochronousTransfer.Hdr.Function = URB_FUNCTION_ISOCH_TRANSFER;
		subUrb->UrbIsochronousTransfer.PipeHandle = wdmhUSBPipe;

		if(IsRead) {

			subUrb->UrbIsochronousTransfer.TransferFlags =
													 (USBD_TRANSFER_DIRECTION_IN|USBD_SHORT_TRANSFER_OK|USBD_START_ISO_TRANSFER_ASAP);
		}
		else {

			subUrb->UrbIsochronousTransfer.TransferFlags =
													 (USBD_TRANSFER_DIRECTION_OUT|USBD_SHORT_TRANSFER_OK);
		}
	
		IsoParams = pSingleTrasfer->IsoParams;

		if ((IsoParams.isoId == USB_ISO_ID) && (IsoParams.isoCmd == USB_ISO_CMD_SET_FRAME))
		{            
			subUrb->UrbIsochronousTransfer.StartFrame = IsoParams.ulParam1;
			CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"Setting USB_ISO_CMD_SET_FRAME and frame number:%x \n",subUrb->UrbIsochronousTransfer.StartFrame);
		}
		else if ((IsoParams.isoId == USB_ISO_ID) && (IsoParams.isoCmd == USB_ISO_CMD_CURRENT_FRAME))
		{  
			subUrb->UrbIsochronousTransfer.StartFrame = CyGetCurrentFrame(pDevContext) + IsoParams.ulParam1;
			CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"Setting USB_ISO_CMD_CURRENT_FRAME and frame number:%x \n",subUrb->UrbIsochronousTransfer.StartFrame);
		}
		else
		{// This implementation is to fix iso issue of sending transaction every frame, where HC return error, or not able manage transaction in every frame.
			// if we set the ASAP flag then , HC send transaction every frame without delay, that casue transaction to fail. The WDM driver behavoir for ASAP flag is little bit
			// different , there is 6 frame difference between each consecutive transaction. To make it working in the WDF driver
			// We are getting current frame mumber and adding 7 frame latency. This way it works fine.
			//subUrb->UrbIsochronousTransfer.TransferFlags|=USBD_START_ISO_TRANSFER_ASAP;
			ULONG  frameNumber=0;
			WdfUsbTargetDeviceRetrieveCurrentFrameNumber(pDevContext->CyUsbDevice, &frameNumber);
			subUrb->UrbIsochronousTransfer.StartFrame = frameNumber  + ISO_FRAME_LATENCY;
		}			
		subUrb->UrbIsochronousTransfer.NumberOfPackets = nPackets;
		subUrb->UrbIsochronousTransfer.UrbLink = NULL;
		subUrb->UrbIsochronousTransfer.ErrorCount =0;

		//
		// set the offsets for every packet for reads/writes
		//
		if(IsRead) {

			offset = 0;

			for(jIndex = 0; jIndex < nPackets; jIndex++)
			{
				subUrb->UrbIsochronousTransfer.IsoPacket[jIndex].Offset = offset;
				subUrb->UrbIsochronousTransfer.IsoPacket[jIndex].Length = 0;
				subUrb->UrbIsochronousTransfer.IsoPacket[jIndex].Status = USBD_STATUS_SUCCESS ;
				//check for ZLP
				if((ulNoOfZeroLenghtPacket)&&(iIndex==(ulNumSubRequests-1))&&(ulStartIndexOfZeroLength%ulPACKETS_PER_STAGE /*1024*/)==jIndex)
				{
					ulStartIndexOfZeroLength++;
					ulNoOfZeroLenghtPacket--;
					CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"ReadPkt ulStartIndexOfZeroLength=%d ulNoOfZeroLenghtPacket=%d jIndex=%d\n",ulStartIndexOfZeroLength,ulNoOfZeroLenghtPacket,jIndex);
					//No updated
				}
				else if((ulPartialDataPacketLength)&&(iIndex==(ulNumSubRequests-1))&&((ulIndexOfPartialDtPktLen%ulPACKETS_PER_STAGE /*1024*/)==jIndex))
				{
					offset+=ulPartialDataPacketLength;
					ulStageSize -=ulPartialDataPacketLength;
					ulPartialDataPacketLength = 0;
					CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"ReadPkt ulIndexOfPartialDtPktLen=%d ulPartialDataPacketLength=%d jIndex=%d\n",ulIndexOfPartialDtPktLen,ulPartialDataPacketLength,jIndex);
				}
				else
				{
					offset+=ulPacketSize;
					//NtStatus = RtlULongSub(ulStageSize, ulPacketSize, &ulStageSize); //TODO : Enable this comment and find the RtlULongSub function defination
					ulStageSize-=ulPacketSize;
					ulNoOfFullPacket--;
					//ASSERT(NT_SUCCESS(NtStatus));
				}
			}
			ASSERT(ulStageSize == 0);
		}
		else 
		{

			offset = 0;

			for(jIndex = 0; jIndex < nPackets; jIndex++) {

				subUrb->UrbIsochronousTransfer.IsoPacket[jIndex].Offset = offset;
				subUrb->UrbIsochronousTransfer.IsoPacket[jIndex].Status = USBD_STATUS_SUCCESS;
				//check for ZLP
				if((ulNoOfZeroLenghtPacket)&&(iIndex==(ulNumSubRequests-1))&&((ulStartIndexOfZeroLength%ulPACKETS_PER_STAGE /*1024*/)==jIndex))
				{
					subUrb->UrbIsochronousTransfer.IsoPacket[jIndex].Length = 0; //ZERO lenght
					ulStartIndexOfZeroLength++;
					ulNoOfZeroLenghtPacket--;
					CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"WritePkt ulStartIndexOfZeroLength=%d ulNoOfZeroLenghtPacket=%d jIndex=%d\n",ulStartIndexOfZeroLength,ulNoOfZeroLenghtPacket,jIndex);
					//No updated
				}
				else if((ulPartialDataPacketLength)&&(iIndex==(ulNumSubRequests-1))&&((ulIndexOfPartialDtPktLen%ulPACKETS_PER_STAGE /*1024*/)==jIndex))
				{
					offset+=ulPartialDataPacketLength;
					ulStageSize -=ulPartialDataPacketLength;
					subUrb->UrbIsochronousTransfer.IsoPacket[jIndex].Length = ulPartialDataPacketLength; // last packet is less than maximum packet size packet
					ulPartialDataPacketLength =0;
					CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"WritePkt ulIndexOfPartialDtPktLen=%d ulStageSize=%d jIndex=%d\n",ulIndexOfPartialDtPktLen,ulStageSize,jIndex);
				}
				else
				{
					offset+=ulPacketSize; 
					subUrb->UrbIsochronousTransfer.IsoPacket[jIndex].Length = ulPacketSize;		 // packet is of maximum packet size.				
					ulStageSize-=ulPacketSize;	
					ulNoOfFullPacket--;
				}
			}
			ASSERT(ulStageSize == 0);
		}

		//
		// Associate the URB with the request.
		//
		NtStatus = WdfUsbTargetPipeFormatRequestForUrb(UsbPipeHandle,
										  SubRequest,
										  subUrbMemory,
										  NULL );
		if (!NT_SUCCESS(NtStatus)) {
			CyTraceEvents(TRACE_LEVEL_ERROR,DBG_IOCTL,"Failed to format requset for urb\n");
			WdfObjectDelete(SubRequest);			
			IoFreeMdl(subMdl);
			goto Exit;
		}

		WdfRequestSetCompletionRoutine(SubRequest,
										CySubRequestCompletionRoutine,
										pMainReqContext);

		//
		// WdfCollectionAdd takes a reference on the request object and removes
		// it when you call WdfCollectionRemove.
		//
		NtStatus = WdfCollectionAdd(hCollection, SubRequest);
		if (!NT_SUCCESS(NtStatus)) {
			CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"WdfCollectionAdd failed 0x%x\n", NtStatus);
			WdfObjectDelete(SubRequest);			
			IoFreeMdl(subMdl);
			goto Exit;
		}

		InsertTailList(&SubRequestsList, &pSubReqContext->ListEntry);

	}
	
	WdfObjectReference(Request);
	  
	WdfRequestMarkCancelable(Request, CyEvtRequestCancel);
	IsCancelable = TRUE;

	while(!IsListEmpty(&SubRequestsList))
	{
		pthisEntry = RemoveHeadList(&SubRequestsList);
		pSubReqContext = CONTAINING_RECORD(pthisEntry, SUB_REQUEST_CONTEXT, ListEntry);
		SubRequest = WdfObjectContextGetObject(pSubReqContext);
		CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"Sending SubRequest 0x%p\n", SubRequest);	       
		WDF_REQUEST_SEND_OPTIONS_INIT(&RequestSendOptions,
                                  WDF_REQUEST_SEND_OPTION_SYNCHRONOUS);
		if (WdfRequestSend(SubRequest, WdfUsbTargetPipeGetIoTarget(UsbPipeHandle),WDF_NO_SEND_OPTIONS) == FALSE) 
		{
			NtStatus = WdfRequestGetStatus(SubRequest);
			// Insert the subrequest back into the subrequestlist so cleanup can find it and delete it
			InsertHeadList(&SubRequestsList, &pSubReqContext->ListEntry);
			CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"WdfRequestSend failed with NtStatus code 0x%x\n", NtStatus);
			WdfVerifierDbgBreakPoint(); // break into the debugger if the registry value is set.
			goto Exit;
		}
				
	}

Exit:

    //
    // Checking the status besides the number of list entries will help differentiate
    // failures where everything succeeded vs where there were failures before adding
    // list entries.
    //
    if(NT_SUCCESS(NtStatus) && IsListEmpty(&SubRequestsList))
	{
        //
        // We will let the completion routine to cleanup and complete the
        // main request.
        //
        return STATUS_PENDING;
    }
    else {
        BOOLEAN  completeRequest;
        NTSTATUS tempStatus;

        completeRequest = TRUE;
        tempStatus = STATUS_SUCCESS;

        if(hCollection) {                       
            DoSubRequestCleanup(pMainReqContext, &SubRequestsList, &completeRequest);  
        }

        if (completeRequest) {
            if (IsCancelable) {
                //
                // Mark the main request as not cancelable before completing it.
                //
                tempStatus = WdfRequestUnmarkCancelable(Request);
                if (NT_SUCCESS(tempStatus)) {
                    //
                    // If WdfRequestUnmarkCancelable returns STATUS_SUCCESS 
                    // that means the cancel routine has been removed. In that case
                    // we release the reference otherwise the cancel routine does it.
                    //
                    WdfObjectDereference(Request);
                }            
            }

            if (pMainReqContext->Numxfer > 0 ) {
                WdfRequestCompleteWithInformation(Request, STATUS_SUCCESS, pMainReqContext->Numxfer);
            }
            else {
                WdfRequestCompleteWithInformation(Request, NtStatus, pMainReqContext->Numxfer); 
            }
            
        }

    }
   // CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"PerformHighSpeedIsochTransfer -- ends\n");    
	return NtStatus;
}
NTSTATUS CyIsoSuperSpeedRW(WDFREQUEST Request,PDEVICE_CONTEXT pDevContext,WDFUSBPIPE UsbPipeHandle,BOOLEAN IsRead,BOOLEAN bIsDirectMethod)
{
	NTSTATUS NtStatus = STATUS_SUCCESS;
	ULONG                   ulNumberOfPackets;
    ULONG                   ulActualPackets;
    ULONG                   ulMinDataInEachPacket;
    ULONG                   ulDataLeftToBeDistributed;
    ULONG                   ulNumberOfPacketsFilledToBrim;
    ULONG                   ulPacketSize;
    ULONG                   ulNumSubRequests;
	size_t					ulTotalLenght,ulBufferoffset;
    ULONG                   ulStageSize;
    PUCHAR                  ulpVA = NULL;    
    PREQUEST_CONTEXT        pMainReqContext;
    PMDL                    pMainMdl = NULL;
    WDF_USB_PIPE_INFORMATION   UsbPipeInfo;    
    WDFCOLLECTION           hCollection = NULL;
    WDFSPINLOCK             hSpinLock = NULL;    
    WDF_OBJECT_ATTRIBUTES   attributes;
    WDFREQUEST              SubRequest;
    PSUB_REQUEST_CONTEXT    pSubReqContext;
    ULONG                   iIndex, jIndex;
    PLIST_ENTRY             pthisEntry;
    LIST_ENTRY              SubRequestsList;    
    BOOLEAN                 IsCancelable;
	PSINGLE_TRANSFER        pSingleTrasfer;
	ISO_ADV_PARAMS			IsoParams;	
	USBD_PIPE_HANDLE		wdmhUSBPipe;
	PUCHAR pucOutPutBuffer=NULL;
	size_t szInputBuf=0,szOutputBuf=0;
	WDF_REQUEST_SEND_OPTIONS     RequestSendOptions;
	ULONG ulNoOfFullPacket=0; // This will store the number of packets hold Maximum packet size data
	ULONG ulNoOfZeroLenghtPacket=0; // This will store the number of Zero length packets
	ULONG ulPartialDataPacketLength=0; //This will stare the partial data length if length is not multiple of mamimum packet size
	ULONG ulIndexOfPartialDtPktLen=0; //This will store the index of partial data length packet
	ULONG ulStartIndexOfZeroLength=0; //This will store ZERO Length packet index
	ULONG ulPACKETS_PER_STAGE =0;
	UCHAR uMaxBurst =0;
	UCHAR uMultSS =0;
	USHORT usMaxpacket=0;

	
	CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"Start  CyIsoSuperSpeedRW \n");

	IsCancelable = FALSE;    
	InitializeListHead(&SubRequestsList); 
    pMainReqContext = CyGetRequestContext(Request);	

	if(pMainReqContext->IsNeitherIO)
	{//NEITHER IO
		pSingleTrasfer = WdfMemoryGetBuffer(pMainReqContext->InputMemoryBufferWrite, &szInputBuf); // Input buffer		
		ulTotalLenght = pMainReqContext->ulszOutputMemoryBuffer;
	}
	else
	{//BUFFERED IO
		WDFMEMORY MemoryObject=NULL;
		PVOID buffer=NULL;
		size_t SingleTraBufSize =0;
		NtStatus = WdfRequestRetrieveOutputMemory(Request,
												 &MemoryObject);
		if(!NT_SUCCESS(NtStatus))
		{
		   CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestRetrieveInputMemory failed 0x%x\n", NtStatus);					    
		   WdfRequestCompleteWithInformation(Request, NtStatus, 0);	 		   
		   return NtStatus;
		}
		// get buffer point from memory object
		pSingleTrasfer = WdfMemoryGetBuffer(MemoryObject, &SingleTraBufSize);
		pMainReqContext->InputMemoryBufferWrite = MemoryObject;		
		WdfRequestRetrieveOutputBuffer(Request,0,&buffer,&ulTotalLenght);
		ulTotalLenght-=pSingleTrasfer->BufferOffset;
		if(ulTotalLenght<=0)
		{
		   WdfRequestCompleteWithInformation(Request, STATUS_INVALID_PARAMETER, 0);	 		   
		   return STATUS_INVALID_PARAMETER;
		}
		CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"BUFFERED IO TotalLenght :%x\n",ulTotalLenght);

	}
	// We only support one request at a time for the isochronous transfer and the limit for that is 1024 frams for transfer
	/*if(ulTotalLenght>ISO_READWRITE_STAGESIZE_SS_SPEED)
	{
		WdfRequestCompleteWithInformation(Request, STATUS_INVALID_BUFFER_SIZE, 0);	
		CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"TotalLenght :%x Is greater than the 1024 frams per transfer\n",ulTotalLenght);
		return STATUS_INVALID_BUFFER_SIZE;
	}*/

	pMainReqContext->ulLastIsoPktIndex = 0; //Reset the iso last packet index
	if(pSingleTrasfer->IsoPacketOffset)
		pMainReqContext->ulNoOfIsoUserRequestPkt = (pSingleTrasfer->IsoPacketLength)/sizeof(ISO_PACKET_INFO);//Reset the iso last transfer user provided buffer length
	else
		pMainReqContext->ulNoOfIsoUserRequestPkt =0;

	CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"ulNoOfIsoUserRequestPkt = %d IsoPacketLength=%d\n", pMainReqContext->ulNoOfIsoUserRequestPkt,pSingleTrasfer->IsoPacketLength);
    //
    // Create a collection to store all the sub requests.
    //
    WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
    attributes.ParentObject = Request;
    NtStatus = WdfCollectionCreate(&attributes,
                                &hCollection);
    if (!NT_SUCCESS(NtStatus)){
		CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"WdfCollectionCreate failed with status 0x%x\n", NtStatus);
        goto Exit;
    }

    WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
    attributes.ParentObject = hCollection;
    NtStatus = WdfSpinLockCreate(&attributes, &hSpinLock);
    if (!NT_SUCCESS(NtStatus)){
		CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"WdfSpinLockCreate failed with status 0x%x\n", NtStatus);
        goto Exit;
    }
    WDF_USB_PIPE_INFORMATION_INIT(&UsbPipeInfo);
    WdfUsbTargetPipeGetInformation(UsbPipeHandle, &UsbPipeInfo);
	//Get maxburst of the given endpoint
	uMaxBurst = GetMaxburst(pDevContext,UsbPipeInfo);
	uMultSS = GetMultSS(pDevContext,UsbPipeInfo);	
	usMaxpacket =  GetMaxPacketSizeSS(pDevContext,UsbPipeInfo);	
	ulPacketSize = usMaxpacket * uMaxBurst * uMultSS;
	CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"uMaxBurst :0x%x uMultSS :0x%x ulPacketSize(Final):0x%x\n MaximumPacketSize(USBDI):0x%x,usMaxpacket(Dev):%x", uMaxBurst,uMultSS,ulPacketSize,UsbPipeInfo.MaximumPacketSize,usMaxpacket);

	if(ulTotalLenght==0)
	{
		ulActualPackets =8;
		ulNoOfFullPacket = 0;
		ulNoOfZeroLenghtPacket =8;
		ulStartIndexOfZeroLength =0;
		ulPartialDataPacketLength =0;
		ulIndexOfPartialDtPktLen = 0;
	}
	else
	{
		//ulNumberOfPackets = (ulTotalLenght + ulPacketSize - 1) / ulPacketSize;			
		ulNoOfFullPacket = (ulTotalLenght/ulPacketSize);
		if(ulNoOfFullPacket==0)
		{
			ulPartialDataPacketLength = (ulTotalLenght%ulPacketSize);	
			ulIndexOfPartialDtPktLen = 0; 
			ulNoOfZeroLenghtPacket = 7;
			ulStartIndexOfZeroLength =1;		
		}
		else if((((ulNoOfFullPacket%8)==0)&&(ulTotalLenght%ulPacketSize))||(ulNoOfFullPacket%8)!=0)
		{
			ulPartialDataPacketLength = (ulTotalLenght%ulPacketSize);	
			ulIndexOfPartialDtPktLen = (ulPartialDataPacketLength!=0)? ulNoOfFullPacket : 0; 
			ulNoOfZeroLenghtPacket = (8-(ulNoOfFullPacket%8))-((ulPartialDataPacketLength!=0)? 1:0);
			ulStartIndexOfZeroLength =(ulPartialDataPacketLength!=0)? (ulNoOfFullPacket+1):ulNoOfFullPacket;					
		}
		else
		{		
			ulNoOfZeroLenghtPacket =0;
			ulStartIndexOfZeroLength =0;
			ulPartialDataPacketLength =0;
			ulIndexOfPartialDtPktLen = 0;		
		}		
	}
	ulActualPackets = ulNoOfFullPacket + ulNoOfZeroLenghtPacket + ((ulPartialDataPacketLength!=0)? 1 : 0); 
	// Add condition to check if 8th packet is partila or not if is not then do not foward it to host stack.
	if(((ulNoOfFullPacket%8)!=0) || (ulTotalLenght<ulPacketSize))
	{//8th packet
		if((((ulNoOfFullPacket+1)%8)==0) &&(ulNoOfZeroLenghtPacket==0))
		{//7th packet
			CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"Mutliple of eight or last packet is of partial length \n");
		}
		else if(ulTotalLenght!=0)
		{ 
			// Number of packet is not multiple of 8 
			//check if last 8th packet is partial or not.		
			// When there is a zero length packet and that is not multiple of eight
			CyTraceEvents(TRACE_LEVEL_ERROR,DBG_IOCTL,"User entered number of packet is not multiple of eight\n");	
			NtStatus = STATUS_INVALID_PARAMETER;
			CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"End  CyIsoSuperSpeedRW \n");
			WdfRequestCompleteWithInformation(Request,NtStatus,0);
			return NtStatus;			
		}	  
	}
	if((ulNoOfFullPacket%8==0)&& ulPartialDataPacketLength)
	{// 8 packet and 9 packet is partial
		CyTraceEvents(TRACE_LEVEL_ERROR,DBG_IOCTL,"User entered number of packet is not multiple of eight\n");	
		NtStatus = STATUS_INVALID_PARAMETER;
		CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"End  CyIsoSuperSpeedRW \n");
		WdfRequestCompleteWithInformation(Request,NtStatus,0);
		return NtStatus;			
	}
	
	CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL, "ulTotalLenght = %d\n", ulTotalLenght);
	CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL, "ulNoOfFullPacket = %d\n", ulNoOfFullPacket);
	CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL, "ulNoOfZeroLenghtPacket = %d ulStartIndexOfZeroLength = %d\n", ulNoOfZeroLenghtPacket,ulStartIndexOfZeroLength);
	CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL, "ulPartialDataPacketLength = %d ulIndexOfPartialDtPktLen = %d\n", ulPartialDataPacketLength,ulIndexOfPartialDtPktLen);
	CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL, "ulActualPackets = %d\n", ulActualPackets);
    //
    // determine how many stages of transfer needs to be done.
    // in other words, how many irp/urb pairs required.
    // this irp/urb pair is also called the subsidiary irp/urb pair
    //
	ulPACKETS_PER_STAGE = ISO_READWRITE_STAGESIZE_SS_SPEED/ulPacketSize;
	if(ulPACKETS_PER_STAGE%8!=0)
	{// ulPACKETS_PER_STAGE shuold be multiple of 8, if is not then adjust it to 8, this is the case when user mamimum packet size random(like 450,766)
		ulPACKETS_PER_STAGE-= (ulPACKETS_PER_STAGE%8);
	}


    //ulNumSubRequests = (ulActualPackets + 1023) / 1024;
	ulNumSubRequests = (ulActualPackets + (ulPACKETS_PER_STAGE-1)) / ulPACKETS_PER_STAGE;

	CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"PeformSuperSpeedIsochTransfer::ulNumSubRequests = %d ulPACKETS_PER_STAGE=%d ulPacketSize=%d\n", ulNumSubRequests,ulPACKETS_PER_STAGE,ulPacketSize);

    pMainReqContext->SubRequestCollection = hCollection;
    pMainReqContext->SubRequestCollectionLock = hSpinLock;
    
	if(!pMainReqContext->IsNeitherIO)
	{//BUFFERED IO
		if(ulTotalLenght==0)
		{
			CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"Requested length is ZERO\n");
		}
		else
		{
			WDFMEMORY WdfMemory=NULL;
			NtStatus = WdfRequestRetrieveOutputWdmMdl(Request, &pMainMdl);
			if(!NT_SUCCESS(NtStatus))
			{
				CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"WdfRequestRetrieveOutputWdmMdl failed %x\n", NtStatus);
				goto Exit;
			}
			ulpVA = (PUCHAR) MmGetMdlVirtualAddress(pMainMdl);
			// Get the buffer pointer at the Data buffer
			ulpVA+=pSingleTrasfer->BufferOffset;
			CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"BUFFERED IO INCREMENT THE VA\n");
			pMainReqContext->Mdl = NULL;
		}
	}
	else 
	{//NEITHER IO
		if(ulTotalLenght==0)
		{
			CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"Requested length is ZERO\n");
		}
		else
		{
			if(IsRead)
				ulpVA = (PUCHAR) WdfMemoryGetBuffer(pMainReqContext->OutputMemoryBufferWrite, NULL);
			else
				ulpVA = (PUCHAR) WdfMemoryGetBuffer(pMainReqContext->OutputMemoryBufferRead, NULL);

			pMainMdl = IoAllocateMdl(ulpVA,ulTotalLenght,FALSE,TRUE,NULL);
			if(!pMainMdl)
			{
				CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"IoAllocateMdl failed\n");
				goto Exit;
			}
			MmBuildMdlForNonPagedPool(pMainMdl);		
			ulpVA = (PUCHAR) MmGetMdlVirtualAddress(pMainMdl);
			pMainReqContext->Mdl = pMainMdl;
		}
	}

	for(iIndex = 0; iIndex < ulNumSubRequests; iIndex++)
	{

		WDFMEMORY               subUrbMemory;
		PURB                    subUrb;
		PMDL                    subMdl = NULL;
		ULONG                   nPackets;
		ULONG                   siz;
		ULONG                   offset;
		// For every stage of transfer we need to do the following
		// tasks
		// 1. allocate a request
		// 2. allocate an urb
		// 3. allocate a mdl.
		// 4. Format the request for transfering URB
		// 5. Send the Request.
		if(ulActualPackets <= ulPACKETS_PER_STAGE/*1024*/) 
		{

			nPackets = ulActualPackets;
			ulActualPackets = 0;
			ulStageSize = (ulNoOfFullPacket * ulPacketSize)+(ulPartialDataPacketLength);
		}
		else {

			nPackets =ulPACKETS_PER_STAGE; //1024;
			ulActualPackets -= ulPACKETS_PER_STAGE;// 1024;				
			if(((ulPartialDataPacketLength)&&(ulIndexOfPartialDtPktLen<=ulPACKETS_PER_STAGE)) ||((ulNoOfZeroLenghtPacket)&&(ulStartIndexOfZeroLength<=ulPACKETS_PER_STAGE)))
				ulStageSize = (ulNoOfFullPacket * ulPacketSize)+(ulPartialDataPacketLength);
			else
				ulStageSize = (nPackets * ulPacketSize);
		}

		CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"nPackets =%d,ulActualPackets=%d ulStageSize :%d\n", nPackets, ulActualPackets,ulStageSize);

		ASSERT(nPackets <= ulPACKETS_PER_STAGE /*1024*/);

		siz = GET_ISO_URB_SIZE(nPackets);


		WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
		WDF_OBJECT_ATTRIBUTES_SET_CONTEXT_TYPE(&attributes, SUB_REQUEST_CONTEXT);


		NtStatus = WdfRequestCreate(
			&attributes,
			WdfUsbTargetDeviceGetIoTarget(pDevContext->CyUsbDevice),
			&SubRequest);

		if(!NT_SUCCESS(NtStatus)) {

			CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"WdfRequestCreate failed 0x%x\n", NtStatus);
			goto Exit;
		}

		pSubReqContext = CyGetSubRequestContext(SubRequest);
		pSubReqContext->UserRequest = Request;

		//
		// Allocate memory for URB.
		//
		WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
		attributes.ParentObject = SubRequest;
		NtStatus = WdfMemoryCreate(
							&attributes,
							NonPagedPool,
							CYMEM_TAG,
							siz,
							&subUrbMemory,
							(PVOID*) &subUrb);

		if (!NT_SUCCESS(NtStatus)) {
			WdfObjectDelete(SubRequest);
			CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"Failed to alloc MemoryBuffer for suburb\n");
			goto Exit;
		}
		// Initialize urb to zero
		RtlZeroMemory(subUrb,siz);
		pSubReqContext->SubUrb = subUrb;
		if(ulTotalLenght==0)
		{
			subUrb->UrbIsochronousTransfer.TransferBufferMDL = NULL;
			subUrb->UrbIsochronousTransfer.TransferBuffer  = NULL;
			subUrb->UrbIsochronousTransfer.TransferBufferLength = 0;
		}
		else
		{
			subMdl = IoAllocateMdl((PVOID) ulpVA,
								   ulStageSize,
								   FALSE,
								   FALSE,
								   NULL);

			if(subMdl == NULL) {

				CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"failed to alloc mem for sub context mdl\n");
				WdfObjectDelete(SubRequest);				
				NtStatus = STATUS_INSUFFICIENT_RESOURCES;
				goto Exit;
			}

			IoBuildPartialMdl(pMainMdl,
							  subMdl,
							  (PVOID) ulpVA,
							  ulStageSize);

			pSubReqContext->SubMdl = subMdl;
			subUrb->UrbIsochronousTransfer.TransferBufferMDL = subMdl;
			subUrb->UrbIsochronousTransfer.TransferBufferLength = ulStageSize;
			ulpVA += ulStageSize;
			ulTotalLenght -= ulStageSize;
		}

		wdmhUSBPipe = WdfUsbTargetPipeWdmGetPipeHandle(UsbPipeHandle);
		subUrb->UrbIsochronousTransfer.Hdr.Length = (USHORT) siz;
		subUrb->UrbIsochronousTransfer.Hdr.Function = URB_FUNCTION_ISOCH_TRANSFER;
		subUrb->UrbIsochronousTransfer.PipeHandle = wdmhUSBPipe;

		if(IsRead) {

			subUrb->UrbIsochronousTransfer.TransferFlags =
													 (USBD_TRANSFER_DIRECTION_IN|USBD_SHORT_TRANSFER_OK);
		}
		else {

			subUrb->UrbIsochronousTransfer.TransferFlags =
													 (USBD_TRANSFER_DIRECTION_OUT|USBD_SHORT_TRANSFER_OK);
		}

		
        
/*
		This is a way to set the start frame and NOT specify ASAP flag.

		NtStatus = WdfUsbTargetDeviceRetrieveCurrentFrameNumber(wdfUsbDevice, &frameNumber);
		subUrb->UrbIsochronousTransfer.StartFrame = frameNumber  + SOME_LATENCY;
*/
        
		IsoParams = pSingleTrasfer->IsoParams;

		if ((IsoParams.isoId == USB_ISO_ID) && (IsoParams.isoCmd == USB_ISO_CMD_SET_FRAME))
		{            
			subUrb->UrbIsochronousTransfer.StartFrame = IsoParams.ulParam1;
			CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"Setting USB_ISO_CMD_SET_FRAME and frame number:%x \n",subUrb->UrbIsochronousTransfer.StartFrame);
		}
		else if ((IsoParams.isoId == USB_ISO_ID) && (IsoParams.isoCmd == USB_ISO_CMD_CURRENT_FRAME))
		{  
			subUrb->UrbIsochronousTransfer.StartFrame = CyGetCurrentFrame(pDevContext) + IsoParams.ulParam1;
			CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"Setting USB_ISO_CMD_CURRENT_FRAME and frame number:%x \n",subUrb->UrbIsochronousTransfer.StartFrame);
		}
		else
		{// This implementation is to fix iso issue of sending transaction every frame, where HC return error, or not able manage transaction in very frame.
			// if we set the ASAP flag then , HC send transaction every frame without delay, that casue transaction to fail. The WDM driver behavoir for ASAP flag is little bit
			// different , there is 6 frame difference between each consecutive transaction. To make it working in the WDF driver
			// We are getting current frame mumber and adding 7 frame latency. This way it works fine.
			subUrb->UrbIsochronousTransfer.TransferFlags|=USBD_START_ISO_TRANSFER_ASAP;
		    CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"USBD_START_ISO_TRANSFER_ASAP\n");					
			/*ULONG  frameNumber=0;
			NtStatus = WdfUsbTargetDeviceRetrieveCurrentFrameNumber(pDevContext->CyUsbDevice, &frameNumber);
			subUrb->UrbIsochronousTransfer.StartFrame = frameNumber  + ISO_FRAME_LATENCY;*/
		}			
		subUrb->UrbIsochronousTransfer.NumberOfPackets = nPackets;
		subUrb->UrbIsochronousTransfer.UrbLink = NULL;
		subUrb->UrbIsochronousTransfer.ErrorCount =0;

		//
		// set the offsets for every packet for reads/writes
		//
		if(IsRead) {

			offset = 0;

			for(jIndex = 0; jIndex < nPackets; jIndex++)
			{
				subUrb->UrbIsochronousTransfer.IsoPacket[jIndex].Offset = offset;
				subUrb->UrbIsochronousTransfer.IsoPacket[jIndex].Length = 0;
				subUrb->UrbIsochronousTransfer.IsoPacket[jIndex].Status = USBD_STATUS_SUCCESS ;
				//check for ZLP
				if((ulNoOfZeroLenghtPacket)&&(iIndex==(ulNumSubRequests-1))&&(ulStartIndexOfZeroLength%ulPACKETS_PER_STAGE /*1024*/)==jIndex)
				{
					ulStartIndexOfZeroLength++;
					ulNoOfZeroLenghtPacket--;
					CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"ReadPkt ulStartIndexOfZeroLength=%d ulNoOfZeroLenghtPacket=%d jIndex=%d\n",ulStartIndexOfZeroLength,ulNoOfZeroLenghtPacket,jIndex);
					//No updated
				}
				else if((ulPartialDataPacketLength)&&(iIndex==(ulNumSubRequests-1))&&((ulIndexOfPartialDtPktLen%ulPACKETS_PER_STAGE /*1024*/)==jIndex))
				{
					offset+=ulPartialDataPacketLength;
					ulStageSize -=ulPartialDataPacketLength;
					ulPartialDataPacketLength = 0;
					CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"ReadPkt ulIndexOfPartialDtPktLen=%d ulPartialDataPacketLength=%d jIndex=%d\n",ulIndexOfPartialDtPktLen,ulPartialDataPacketLength,jIndex);
				}
				else
				{
					offset+=ulPacketSize;
					//NtStatus = RtlULongSub(ulStageSize, ulPacketSize, &ulStageSize); //TODO : Enable this comment and find the RtlULongSub function defination
					ulStageSize-=ulPacketSize;
					ulNoOfFullPacket--;
					//ASSERT(NT_SUCCESS(NtStatus));
				}
			}
			ASSERT(ulStageSize == 0);
		}
		else 
		{

			offset = 0;

			for(jIndex = 0; jIndex < nPackets; jIndex++) {

				subUrb->UrbIsochronousTransfer.IsoPacket[jIndex].Offset = offset;
				subUrb->UrbIsochronousTransfer.IsoPacket[jIndex].Status = USBD_STATUS_SUCCESS;
				//check for ZLP
				if((ulNoOfZeroLenghtPacket)&&(iIndex==(ulNumSubRequests-1))&&((ulStartIndexOfZeroLength%ulPACKETS_PER_STAGE /*1024*/)==jIndex))
				{
					subUrb->UrbIsochronousTransfer.IsoPacket[jIndex].Length = 0; //ZERO lenght
					ulStartIndexOfZeroLength++;
					ulNoOfZeroLenghtPacket--;
					CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"WritePkt ulStartIndexOfZeroLength=%d ulNoOfZeroLenghtPacket=%d jIndex=%d\n",ulStartIndexOfZeroLength,ulNoOfZeroLenghtPacket,jIndex);
					//No updated
				}
				else if((ulPartialDataPacketLength)&&(iIndex==(ulNumSubRequests-1))&&((ulIndexOfPartialDtPktLen%ulPACKETS_PER_STAGE /*1024*/)==jIndex))
				{
					offset+=ulPartialDataPacketLength;
					ulStageSize -=ulPartialDataPacketLength;
					subUrb->UrbIsochronousTransfer.IsoPacket[jIndex].Length = ulPartialDataPacketLength; // last packet is less than maximum packet size packet
					ulPartialDataPacketLength =0;
					CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"WritePkt ulIndexOfPartialDtPktLen=%d ulStageSize=%d jIndex=%d\n",ulIndexOfPartialDtPktLen,ulStageSize,jIndex);
				}
				else
				{
					offset+=ulPacketSize; 
					subUrb->UrbIsochronousTransfer.IsoPacket[jIndex].Length = ulPacketSize;		 // packet is of maximum packet size.				
					ulStageSize-=ulPacketSize;	
					ulNoOfFullPacket--;
				}
			}
			ASSERT(ulStageSize == 0);
		}

		//
		// Associate the URB with the request.
		//
		NtStatus = WdfUsbTargetPipeFormatRequestForUrb(UsbPipeHandle,
										  SubRequest,
										  subUrbMemory,
										  NULL );
		if (!NT_SUCCESS(NtStatus)) {
			CyTraceEvents(TRACE_LEVEL_ERROR,DBG_IOCTL,"Failed to format requset for urb\n");
			WdfObjectDelete(SubRequest);			
			IoFreeMdl(subMdl);
			goto Exit;
		}

		WdfRequestSetCompletionRoutine(SubRequest,
										CySubRequestCompletionRoutine,
										pMainReqContext);

		//
		// WdfCollectionAdd takes a reference on the request object and removes
		// it when you call WdfCollectionRemove.
		//
		NtStatus = WdfCollectionAdd(hCollection, SubRequest);
		if (!NT_SUCCESS(NtStatus)) {
			CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"WdfCollectionAdd failed 0x%x\n", NtStatus);
			WdfObjectDelete(SubRequest);			
			IoFreeMdl(subMdl);
			goto Exit;
		}

		InsertTailList(&SubRequestsList, &pSubReqContext->ListEntry);

	}
	

	
	WdfObjectReference(Request);
	//
	// Mark the main request cancelable so that we can cancel the subrequests
	// if the main requests gets cancelled for any reason.
	//  
	WdfRequestMarkCancelable(Request, CyEvtRequestCancel);
	IsCancelable = TRUE;

	while(!IsListEmpty(&SubRequestsList))
	{
		pthisEntry = RemoveHeadList(&SubRequestsList);
		pSubReqContext = CONTAINING_RECORD(pthisEntry, SUB_REQUEST_CONTEXT, ListEntry);
		SubRequest = WdfObjectContextGetObject(pSubReqContext);
		CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"Sending SubRequest 0x%p\n", SubRequest);	       
		WDF_REQUEST_SEND_OPTIONS_INIT(&RequestSendOptions,
                                  WDF_REQUEST_SEND_OPTION_SYNCHRONOUS);
		if (WdfRequestSend(SubRequest, WdfUsbTargetPipeGetIoTarget(UsbPipeHandle),WDF_NO_SEND_OPTIONS) == FALSE) 
		{
			NtStatus = WdfRequestGetStatus(SubRequest);
			// Insert the subrequest back into the subrequestlist so cleanup can find it and delete it
			InsertHeadList(&SubRequestsList, &pSubReqContext->ListEntry);
			CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"WdfRequestSend failed with NtStatus code 0x%x\n", NtStatus);
			WdfVerifierDbgBreakPoint(); // break into the debugger if the registry value is set.
			goto Exit;
		}
		
	}

Exit:    
    if(NT_SUCCESS(NtStatus) && IsListEmpty(&SubRequestsList))
	{
        //
        // We will let the completion routine to cleanup and complete the
        // main request.
        //
        return STATUS_PENDING; //TODO : Need to validate the status code 
    }
    else {
        BOOLEAN  completeRequest;
        NTSTATUS tempStatus;

        completeRequest = TRUE;
        tempStatus = STATUS_SUCCESS;

        if(hCollection) {                       
            DoSubRequestCleanup(pMainReqContext, &SubRequestsList, &completeRequest);  
        }

        if (completeRequest) {
            if (IsCancelable) {
                //
                // Mark the main request as not cancelable before completing it.
                //
                tempStatus = WdfRequestUnmarkCancelable(Request);
                if (NT_SUCCESS(tempStatus)) {
                    //
                    // If WdfRequestUnmarkCancelable returns STATUS_SUCCESS 
                    // that means the cancel routine has been removed. In that case
                    // we release the reference otherwise the cancel routine does it.
                    //
                    WdfObjectDereference(Request);
                }            
            }

            if (pMainReqContext->Numxfer > 0 ) {
                WdfRequestCompleteWithInformation(Request, STATUS_SUCCESS, pMainReqContext->Numxfer);
            }
            else {
                WdfRequestCompleteWithInformation(Request, NtStatus, pMainReqContext->Numxfer); 
            }
            
        }

    }
    CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"PerformSuperSpeedIsochTransfer -- ends\n");    
	return NtStatus;
}
VOID
DoSubRequestCleanup(
    PREQUEST_CONTEXT    MainRequestContext,
    PLIST_ENTRY         SubRequestsList,
    PBOOLEAN            CompleteRequest
    )
{   
    PLIST_ENTRY           thisEntry;
    PSUB_REQUEST_CONTEXT  subReqContext;
    WDFREQUEST            subRequest;
    ULONG                 numPendingRequests;

    *CompleteRequest = TRUE;
    WdfSpinLockAcquire(MainRequestContext->SubRequestCollectionLock);

    while(!IsListEmpty(SubRequestsList)) {
        thisEntry = RemoveHeadList(SubRequestsList);
        subReqContext = CONTAINING_RECORD(thisEntry, SUB_REQUEST_CONTEXT, ListEntry);
        subRequest = WdfObjectContextGetObject(subReqContext);
        WdfCollectionRemove(MainRequestContext->SubRequestCollection, subRequest);

        if(subReqContext->SubMdl) {
            IoFreeMdl(subReqContext->SubMdl);
            subReqContext->SubMdl = NULL;
        }
        
        WdfObjectDelete(subRequest);
    }
    numPendingRequests = WdfCollectionGetCount(MainRequestContext->SubRequestCollection);
    WdfSpinLockRelease(MainRequestContext->SubRequestCollectionLock);

    if (numPendingRequests > 0) {
        *CompleteRequest = FALSE;
    }       
       
    return; 
}

VOID
CySubRequestCompletionRoutine(
    IN WDFREQUEST                  Request,
    IN WDFIOTARGET                 Target,
    PWDF_REQUEST_COMPLETION_PARAMS CompletionParams,
    IN WDFCONTEXT                  Context
    )
{
    PURB                    urb;
    ULONG                   i;
    ULONG                   numPendingRequests;
    NTSTATUS                NtStatus;
    PREQUEST_CONTEXT        rwContext;
    WDFREQUEST              mainRequest;
    PSUB_REQUEST_CONTEXT    subReqContext;
	PSINGLE_TRANSFER        pSingleTransfer;
	PISO_PACKET_INFO        pIsoPktInfo = NULL;
	ULONG ulIsoPktStsIndex;	

    UNREFERENCED_PARAMETER(Target);

	CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"Start CySubRequestCompletionRoutine\n");
	
    subReqContext = CyGetSubRequestContext(Request);
	rwContext = (PREQUEST_CONTEXT) Context;
	ASSERT(rwContext);

    urb = (PURB) subReqContext->SubUrb;
	if(!rwContext->IsNeitherIO)
	{//BUFFERED IO
		if(subReqContext->SubMdl)
			IoFreeMdl(subReqContext->SubMdl);
		subReqContext->SubMdl = NULL;
	}
	pSingleTransfer = (PSINGLE_TRANSFER) WdfMemoryGetBuffer(rwContext->InputMemoryBufferWrite,NULL);	
	
	if(pSingleTransfer->IsoPacketOffset)
		pIsoPktInfo     = (PISO_PACKET_INFO) ((PUCHAR)pSingleTransfer + pSingleTransfer->IsoPacketOffset);

    NtStatus = CompletionParams->IoStatus.Status;
	pSingleTransfer->NtStatus = NtStatus;
	pSingleTransfer->UsbdStatus = urb->UrbHeader.Status;
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "NumberofPacket %d,ErrorCount:%d bytetransfered:%d\n",urb->UrbIsochronousTransfer.NumberOfPackets,urb->UrbIsochronousTransfer.ErrorCount,urb->UrbIsochronousTransfer.TransferBufferLength);  

    if(NT_SUCCESS(NtStatus) &&
       USBD_SUCCESS(urb->UrbHeader.Status)) {

        rwContext->Numxfer +=
                urb->UrbIsochronousTransfer.TransferBufferLength;        		
    }
    else
	{
		CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "read-write irp failed with status %X\n", NtStatus);  
        CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "urb header status %X\n", urb->UrbHeader.Status);        
    }

	ulIsoPktStsIndex = rwContext->ulLastIsoPktIndex;

	
    for(i = 0; i < urb->UrbIsochronousTransfer.NumberOfPackets; i++)
	{
		if(pSingleTransfer->IsoPacketOffset)
		{
			if(rwContext->ulNoOfIsoUserRequestPkt>0)
			{
				
				pIsoPktInfo[ulIsoPktStsIndex].Length = urb->UrbIsochronousTransfer.IsoPacket[i].Length;
				pIsoPktInfo[ulIsoPktStsIndex].Status = urb->UrbIsochronousTransfer.IsoPacket[i].Status;
				ulIsoPktStsIndex++;
				rwContext->ulNoOfIsoUserRequestPkt--;
				CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"IsoPacket[%d].Length = %d IsoPacket[%d].Status = %X\n",
								i,
								urb->UrbIsochronousTransfer.IsoPacket[i].Length,
								i,
								urb->UrbIsochronousTransfer.IsoPacket[i].Status);

			}
		}
		else			
			CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"User buffer doesn't have sufficient space to hold Iso packet information\n");		
    }
	rwContext->ulLastIsoPktIndex = ulIsoPktStsIndex; // completion routine will be called for each stage and one request can have multiple stage.

    //
    // Remove the SubRequest from the collection.
    //
    WdfSpinLockAcquire(rwContext->SubRequestCollectionLock);
	//Free the SubMdl
	if(subReqContext->SubMdl)
	{
		IoFreeMdl(subReqContext->SubMdl);
		subReqContext->SubMdl = NULL;
	}
    WdfCollectionRemove(rwContext->SubRequestCollection, Request);
    numPendingRequests = WdfCollectionGetCount(rwContext->SubRequestCollection);
    WdfSpinLockRelease(rwContext->SubRequestCollectionLock);
    //
    // If all the sub requests are completed. Complete the main request sent
    // by the user application.
    //
    if(numPendingRequests == 0)
	{

		rwContext->ulLastIsoPktIndex =0;
		rwContext->ulNoOfIsoUserRequestPkt=0;//Reset the iso last transfer user provided buffer length

        CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL,"no more pending sub requests\n");
        if(NT_SUCCESS(NtStatus)) {

            CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL,"urb start frame %X\n",
                                urb->UrbIsochronousTransfer.StartFrame);
        }

        mainRequest = WdfObjectContextGetObject(rwContext);
		//Free the mani  MDL
		if(rwContext->Mdl)
		{
			IoFreeMdl(rwContext->Mdl);
			rwContext->Mdl= NULL;
		}

        //
        // if we transferred some data, main Irp completes with success
        //
        //CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"Total data transferred = %X\n", rwContext->Numxfer);

        //CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL, "SubRequestCompletionRoutine %s completed\n",
          //                  rwContext->Read?"Read":"Write");
        //
        // Mark the main request as not cancelable before completing it.
        //
        NtStatus = WdfRequestUnmarkCancelable(mainRequest);
        if (NT_SUCCESS(NtStatus)) {
            //
            // If WdfRequestUnmarkCancelable returns STATUS_SUCCESS 
            // that means the cancel routine has been removed. In that case
            // we release the reference otherwise the cancel routine does it.
            //
            WdfObjectDereference(mainRequest);
        }
        
        if (rwContext->Numxfer > 0 )
		{
            WdfRequestCompleteWithInformation(mainRequest, STATUS_SUCCESS, rwContext->Numxfer);
        }
        else 
		{
		//	CyTraceEvents(TRACE_LEVEL_ERROR,DBG_IOCTL, "SubRequestCompletionRoutine completiong with failure status %x\n",
          //                       CompletionParams->IoStatus.Status);
            WdfRequestCompleteWithInformation(mainRequest, CompletionParams->IoStatus.Status, rwContext->Numxfer); 
        }       
        
    }

    //
    // Since we created the subrequests, we should free it by removing the
    // reference.
    //
    WdfObjectDelete(Request);

	CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL, ("SubRequestCompletionRoutine - ends\n"));

    return;
}

VOID
CyEvtRequestCancel(
    WDFREQUEST Request
    )
/*++

Routine Description:

    This is the cancellation routine for the main read/write Irp.

Arguments:


Return Value:

    None

--*/
{
    PREQUEST_CONTEXT        rwContext;
    LIST_ENTRY              cancelList;
    PSUB_REQUEST_CONTEXT    subReqContext;
    WDFREQUEST              subRequest;
    ULONG                   i;
    PLIST_ENTRY             thisEntry;

    CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL,"UsbSamp_EvtIoCancel - begins\n");

    //
    // In this case since the cancel routine just references the request context, a reference 
    // on the WDFREQUEST is enough. If it needed to access the underlying WDFREQUEST  to get the 
    // underlying IRP etc. consider using a spinlock for synchronisation with the request 
    // completion routine. 
    //
    rwContext = CyGetRequestContext(Request);

    if(!rwContext->SubRequestCollection) {
        ASSERTMSG("Very unlikely, collection is created before\
                        the request is made cancellable", FALSE);
        return;
    }

    InitializeListHead(&cancelList);

    //
    // We cannot call the WdfRequestCancelSentRequest with the collection lock
    // acquired because that call can recurse into our completion routine,
    // when the lower driver completes the request in the CancelRoutine,
    // and can cause deadlock when we acquire the collection to remove the
    // subrequest. So to workaround that, we will get the item from the
    // collection, take an extra reference, and put them in the local list.
    // Then we drop the lock, walk the list and call WdfRequestCancelSentRequest and
    // remove the extra reference.
    //
    WdfSpinLockAcquire(rwContext->SubRequestCollectionLock);

    for(i=0; i<WdfCollectionGetCount(rwContext->SubRequestCollection); i++) {


        subRequest = WdfCollectionGetItem(rwContext->SubRequestCollection, i);

        subReqContext = CyGetSubRequestContext(subRequest);

        WdfObjectReference(subRequest);

        InsertTailList(&cancelList, &subReqContext->ListEntry);
    }

    WdfSpinLockRelease(rwContext->SubRequestCollectionLock);

    while(!IsListEmpty(&cancelList))
    {
        thisEntry = RemoveHeadList(&cancelList);

        subReqContext = CONTAINING_RECORD(thisEntry, SUB_REQUEST_CONTEXT, ListEntry);

        subRequest = WdfObjectContextGetObject(subReqContext);

        CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL,"Cancelling subRequest 0x%p\n", subRequest);

        if(!WdfRequestCancelSentRequest(subRequest)) {
            CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL,"WdfRequestCancelSentRequest failed\n");
        }

        WdfObjectDereference(subRequest);
    }

    //
    // Release the reference we took earlier on the main request.
    //
    WdfObjectDereference(Request);
    CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL,"UsbSamp_EvtIoCancel - ends\n");
    return;
}

ULONG
CyGetCurrentFrame(
    IN PDEVICE_CONTEXT pDevContext
    )
{
    ULONG  ulFrameNumber;
	NTSTATUS NtStatus;
	

    NtStatus = WdfUsbTargetDeviceRetrieveCurrentFrameNumber(
											   pDevContext->CyUsbDevice,
                                              &ulFrameNumber
											  );

	if (!NT_SUCCESS(NtStatus))
	{
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "CyGetCurrentFrame failed 0x%x\n", NtStatus);
		return 0;
	}	

	return ulFrameNumber;
}

UCHAR GetMaxburst(IN PDEVICE_CONTEXT pDevContext,IN WDF_USB_PIPE_INFORMATION UsbPipeInfo)
{
	int i=0;

	for(i=0;i<MAX_USB_ENDPOINT;i++)
	{
		if(pDevContext->SS_Custom_Infc[0][pDevContext->ucActiveAltSettings].SS_CustomerEp[i].bEndpointAddress == UsbPipeInfo.EndpointAddress)
			return pDevContext->SS_Custom_Infc[0][pDevContext->ucActiveAltSettings].SS_CustomerEp[i].bMaxBurst;
	}
	return 1;
}
UCHAR GetMultSS(IN PDEVICE_CONTEXT pDevContext,IN WDF_USB_PIPE_INFORMATION UsbPipeInfo)
{
	int i=0;

	for(i=0;i<MAX_USB_ENDPOINT;i++)
	{
		if(pDevContext->SS_Custom_Infc[0][pDevContext->ucActiveAltSettings].SS_CustomerEp[i].bEndpointAddress == UsbPipeInfo.EndpointAddress)
			return ((pDevContext->SS_Custom_Infc[0][pDevContext->ucActiveAltSettings].SS_CustomerEp[i].bmAttributes & 0x03)+1); // Get 0:1 bit and (MULT +1)
	}
	return 1;

}
USHORT GetMaxPacketSizeSS(IN PDEVICE_CONTEXT pDevContext,IN WDF_USB_PIPE_INFORMATION UsbPipeInfo)
{
	int i=0;

	for(i=0;i<MAX_USB_ENDPOINT;i++)
	{
		if(pDevContext->SS_Custom_Infc[0][pDevContext->ucActiveAltSettings].SS_CustomerEp[i].bEndpointAddress == UsbPipeInfo.EndpointAddress)
			return (pDevContext->SS_Custom_Infc[0][pDevContext->ucActiveAltSettings].SS_CustomerEp[i].wMaxPacketSize);
	}
	return 1;
}