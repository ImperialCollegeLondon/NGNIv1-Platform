/*
 ## Cypress CyUSB3 driver source file (cyscript.c)
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

#include "..\inc\cydevice.h"
#include "..\inc\cyscript.h"
#include "..\inc\cytrace.h"
#include "..\inc\cyentry.h"
#include <wdf.h>
#include "..\inc\cypnppower.h"
#include "..\inc\cyfileio.h"

#if defined(EVENT_TRACING)
#include "cyscript.tmh"
#endif

NTSTATUS
CySetInterfaces(
    __in WDFDEVICE Device,
	__in UCHAR IntfNu,
    __in UCHAR AltIntfNu
    )
{
	NTSTATUS NtStatus = STATUS_SUCCESS;
	PDEVICE_CONTEXT pDevContext;

	pDevContext = CyGetDeviceContext(Device);

	CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_IOCTL, "Start CySetInterfaces\n");			    

	// NOTE : SetInterface will set the alternate setting of ZEROth interface
	if(pDevContext->bIsMultiplInterface)
	{
		WDF_OBJECT_ATTRIBUTES  pipesAttributes;
		WDF_USB_INTERFACE_SELECT_SETTING_PARAMS  selectSettingParams;
		WDFUSBINTERFACE UsbInterface = pDevContext->MultipleInterfacePair[0].UsbInterface;
		
		CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_IOCTL, "Multiple interface\n");			    
		if(AltIntfNu > WdfUsbInterfaceGetNumSettings(UsbInterface))
		{// validate the input alternate interface number
			NtStatus = STATUS_INVALID_PARAMETER;
			goto ReqComp;
		}
		//set the interface
		WDF_OBJECT_ATTRIBUTES_INIT(&pipesAttributes);
		WDF_OBJECT_ATTRIBUTES_SET_CONTEXT_TYPE(
											   &pipesAttributes,
											   CY_PIPE_CONTEXT
											   );
		WDF_USB_INTERFACE_SELECT_SETTING_PARAMS_INIT_SETTING(
											  &selectSettingParams,
											  AltIntfNu
											  );
		NtStatus = WdfUsbInterfaceSelectSetting(
											  UsbInterface,
											  &pipesAttributes,
											  &selectSettingParams
											  );
		if(!NT_SUCCESS(NtStatus))
		{
			CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfUsbInterfaceSelectSetting failed 0x%x\n", NtStatus);					    
     	    goto ReqComp;
		}
		//After setting the alternate setting update the configured pipe handle table
		CyGetActiveAltInterfaceConfig(pDevContext);
	}
	else
	{
		WDF_OBJECT_ATTRIBUTES  pipesAttributes;
		WDF_USB_INTERFACE_SELECT_SETTING_PARAMS  selectSettingParams;
		WDFUSBINTERFACE UsbInterface = pDevContext->UsbInterfaceConfig.Types.SingleInterface.ConfiguredUsbInterface;
		
		CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_IOCTL, "Single interface with alternate settings\n");

		/* CPP library does not send the valid interface number, to keep backward compatibility with the old wdm driver commenting this section.	
		if((*ulAlternateInterface) > pDevContext->ucNumAltSettigns)
		{// validate the input alternate interface number
			CyTraceEvents(TRACE_LEVEL_WARNING, DBG_IOCTL, "Invalidate input - Alternate settings does not exist :%x\n",*ulAlternateInterface);
			NTStatus = STATUS_INVALID_PARAMETER;
			goto ReqComp;
		}*/

		//set the interface
		WDF_OBJECT_ATTRIBUTES_INIT(&pipesAttributes);
		WDF_OBJECT_ATTRIBUTES_SET_CONTEXT_TYPE(
											   &pipesAttributes,
											   CY_PIPE_CONTEXT
											   );
		WDF_USB_INTERFACE_SELECT_SETTING_PARAMS_INIT_SETTING(
											  &selectSettingParams,
											  AltIntfNu
											  );
		NtStatus = WdfUsbInterfaceSelectSetting(
											  UsbInterface,
											  &pipesAttributes,
											  &selectSettingParams
											  );
		if(!NT_SUCCESS(NtStatus))
		{
			CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfUsbInterfaceSelectSetting failed 0x%x\n", NtStatus);					    
     	    goto ReqComp;
		}
		//After setting the alternate setting update the configured pipe handle table
		CyGetActiveAltInterfaceConfig(pDevContext);
		
	}
	pDevContext->ucActiveAltSettings = AltIntfNu;	
ReqComp:	
	CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_IOCTL, "End CySetInterfaces\n");

	return NtStatus;
}

// *********************************************************************
//
// Function:  CyExecuteScriptTransfer
//
// Purpose:   
//
// Called by: This function does not have significance in the scripting. It is copied from the legacy driver so keeping it for future use.
//
// *********************************************************************
NTSTATUS
CyExecuteScriptTransfer(
    IN WDFDEVICE  Device,
    IN PSCRIPT_HEADER pScriptHdr
    )
{
    NTSTATUS                 NtStatus = STATUS_SUCCESS;
	PDEVICE_CONTEXT pDevContext;
	WDFUSBPIPE UsbPipeHandle;
	WDF_USB_PIPE_TYPE UsbPipeType;

	
    CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP, "Start CyExecuteScriptTransfer\n");	
	pDevContext = CyGetDeviceContext(Device);

    if (pScriptHdr)
    {
		UsbPipeType = CyFindUsbPipeType(pScriptHdr->EndPtAddr,pDevContext,&UsbPipeHandle);        
        if (!UsbPipeHandle)
        {
            // unable to locate endpoint
			CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP, "Unable to locate endpoint address 0x%x\n", pScriptHdr->EndPtAddr);            
            NtStatus = STATUS_INVALID_PARAMETER;
            goto ExecuteScriptTransferDone;
        }

        // abort all requests for right now
		CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP, "Unsupported Endpoint Type = 0x%x\n", UsbPipeType);        
        NtStatus = STATUS_INVALID_PARAMETER;
        goto ExecuteScriptTransferDone;
    }
    else
    {
		CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP, "Error - Invalid pScriptHdr pointer\n");        
        NtStatus = STATUS_INVALID_PARAMETER;
        goto ExecuteScriptTransferDone;
    }
ExecuteScriptTransferDone:
    CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP, "End CyExecuteScriptTransfer\n");	
    return NtStatus;
}

// *********************************************************************
//
// Function:  CYDVK_ExecuteScriptRecord
//
// Purpose:   
//
// Called by: 
//
// *********************************************************************
NTSTATUS
CyExecuteScriptRecord(
    IN WDFDEVICE  Device,
    IN PSCRIPT_HEADER pScriptHdr
    )
{   
    NTSTATUS                 NtStatus = STATUS_SUCCESS;
	PDEVICE_CONTEXT pDevContext;
		
    CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP, "Start CyExecuteScriptRecord\n");	

	pDevContext = CyGetDeviceContext(Device);

    // validate header size
    if (pScriptHdr->HeaderSize < sizeof(SCRIPT_HEADER))
    {
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "Invalid HeaderSize = 0x%x, expected >= 0x%x\n", pScriptHdr->HeaderSize, sizeof(SCRIPT_HEADER));	        
        goto ExecuteScriptRecordDone;
    }

    // validate tag (we only support 0x00 right now)
    if (pScriptHdr->Tag != 0x00)
    {
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "Unsupported Tag = 0x%x\n", pScriptHdr->Tag);        
        goto ExecuteScriptRecordDone;
    }

    // if data provided, validate correct data length
    if ((pScriptHdr->DataLen) && (pScriptHdr->DataLen+1 == (pScriptHdr->RecordSize - sizeof(SCRIPT_HEADER))))
    {
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "Invalid data length = 0x%x, script header size = 0x%x, script record size = 0x%x\n", pScriptHdr->DataLen, sizeof(SCRIPT_HEADER), pScriptHdr->RecordSize);                
        goto ExecuteScriptRecordDone;
    }

    // validate correct configuration number
    if (pScriptHdr->ConfigNum != pDevContext->ucActiveConfigNum)
    {// This condition will not occure as we do not support multiple configurations
        // not the same, so attempt to set new configuration index
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "Setting Configuration Index to 0x%x\n", pScriptHdr->ConfigNum);                        
        NtStatus = CySelectInterfaces(Device);
        if (!NT_SUCCESS(NtStatus))
        {
            // failed to set configuration index
			CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "Unable to set Configuration Index\n");            
            goto ExecuteScriptRecordDone;
        }
    }

    // validate correct inteface is selected
    if (pScriptHdr->AltIntfc != pDevContext->ucActiveAltSettings)
    {
        // not the same, so attempt to set new configuration index
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "Setting Interface Index to 0x%x/0x%x\n", pScriptHdr->IntfcNum, pScriptHdr->AltIntfc);        
		// Need to implement this function -
        NtStatus = CySetInterfaces(Device, pScriptHdr->IntfcNum, pScriptHdr->AltIntfc);
        if (!NT_SUCCESS(NtStatus))
        {
            // failed to set interface index
			CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "Unable to set Interface Index\n");                            
            goto ExecuteScriptRecordDone;
        }
    }


    // determine if this is a non-EP0 request
    if (pScriptHdr->EndPtAddr)
    {// execute script record transfer     
          NtStatus = CyExecuteScriptTransfer(Device, pScriptHdr);
    }
    else
    {		
		WDF_USB_CONTROL_SETUP_PACKET  controlSetupPacket;
		WDF_MEMORY_DESCRIPTOR  memoryDescriptor;		
		WDF_MEMORY_DESCRIPTOR_INIT_BUFFER(&memoryDescriptor,
						                 (PUCHAR)pScriptHdr+sizeof(SCRIPT_HEADER),
					                      pScriptHdr->DataLen);

		//Initialize control setup packet to get total configuration  length.
		controlSetupPacket.Packet.bm.Byte =pScriptHdr->ReqType;		
		controlSetupPacket.Packet.bRequest = pScriptHdr->bRequest;
		controlSetupPacket.Packet.wIndex.Value  = pScriptHdr->wIndex;
		controlSetupPacket.Packet.wValue.Value  = pScriptHdr->wValue;		
		controlSetupPacket.Packet.wLength =(USHORT) pScriptHdr->DataLen;
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "wLenght:%x\n",controlSetupPacket.Packet.wLength);

		
		NtStatus = WdfUsbTargetDeviceSendControlTransferSynchronously(
											 pDevContext->CyUsbDevice,
											 WDF_NO_HANDLE,
											 NULL,
											 &controlSetupPacket,
											 &memoryDescriptor,//(PUCHAR)pScriptHdr+sizeof(SCRIPT_HEADER),
											 NULL
											 );
		if (!NT_SUCCESS(NtStatus)) 
		{
			CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "WdfUsbTargetDeviceSendControlTransferSynchronously failed:%x \n",NtStatus);					
			goto ExecuteScriptRecordDone;
		}
    }

ExecuteScriptRecordDone:    
	CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "End CyExecuteScriptRecord: %x\n",NtStatus);                
    return NtStatus;
}

// *********************************************************************
//
// Function:  CYDVK_ProcessScript
//
// Purpose:   
//
// Called by: 
//
// *********************************************************************
NTSTATUS
CyProcessScript(
    IN WDFDEVICE  Device,
    IN PUCHAR pScriptBuffer,
    IN ULONG ulScriptBufferLength
    )
{    
    PSCRIPT_HEADER         pScriptPtr = (PSCRIPT_HEADER)pScriptBuffer;
    NTSTATUS               NtStatus = STATUS_SUCCESS;
	PDEVICE_CONTEXT pDevContext;


	CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP, "Start CyProcessScript\n");	

	pDevContext = CyGetDeviceContext(Device);
    // validate length at least equals a script header
    if (ulScriptBufferLength >= sizeof(SCRIPT_HEADER))
    {
        // loop until done processing the script file
        while ((ULONG)((PUCHAR)pScriptPtr - pScriptBuffer) < ulScriptBufferLength)
        {
            // validate signature is correct
            if (pScriptPtr->Signature == CYSCRIPT_SIGNATURE)
            {
                // validate script data area does not overrun buffer size
                if (((PUCHAR)pScriptPtr + pScriptPtr->RecordSize) <= (pScriptBuffer + ulScriptBufferLength))
                {
                    // execute this script record
                    NtStatus = CyExecuteScriptRecord(Device, pScriptPtr);
                    if (!NT_SUCCESS(NtStatus))
                    {
						CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "ProcessingScript: executing script record, stop processing script\n");                        
                        break;
                    }
                }
                else
                {
					CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "ProcessingScript: Script size too large\n");                    
                    break;
                }
            }
            else
            {
				CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "ProcessingScript: Invalid script signature, stop processing script\n");                
                NtStatus = STATUS_INVALID_PARAMETER;
                break;
            }
            // advance to next script header
            (PUCHAR)pScriptPtr += pScriptPtr->RecordSize;
        }
    }
    else
    {
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "ProcessingScript: Invalid script length\n");        
        NtStatus = STATUS_INVALID_PARAMETER;
    }
    
	CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "End CyProcessScript :%x\n",NtStatus);
    return NtStatus;
}

// *********************************************************************
//
// Function:  CyExecuteScriptFile
//
// Purpose:   
//
// Called by: cypnppower.c-> PrepareHardware function to execute the script
//
// *********************************************************************
NTSTATUS
CyExecuteScriptFile(
    IN WDFDEVICE  Device,
    IN PWSTR ScriptFileName
    )
{
    
    HANDLE                 hScriptFile;
    NTSTATUS               NtStatus = STATUS_SUCCESS;
	WDF_OBJECT_ATTRIBUTES  attributes;
	WDFMEMORY			   hScriptFileBufMem;
	PVOID				   pScriptFileBuf=NULL;

    CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP, "Start CyExecuteScriptFile\n");	
    // open handle to script file
    NtStatus = CyFileOpen(ScriptFileName, TRUE, &hScriptFile);
    if ((NT_SUCCESS(NtStatus)) && (hScriptFile != NULL))
    {
        ULONG fileSize = (ULONG)CyFileGetSize(hScriptFile);

        if (fileSize)
        {
            //PUCHAR pScriptBuffer = CYWDM_AllocateNonPagedPool(fileSize+1, "pScriptBuffer");
			//TODO : Allocated Memory to store file data
			WDF_OBJECT_ATTRIBUTES_INIT(&attributes);	
			NtStatus = WdfMemoryCreate(
									 &attributes,
									 NonPagedPool,
									 0,
									 (fileSize+1),
									 &hScriptFileBufMem,
									 &pScriptFileBuf
									 );
			if (!NT_SUCCESS(NtStatus)) {
				CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "WdfMemoryCreate failed  %!STATUS!\n",NtStatus);        
				return NtStatus;
			}
            
            NtStatus = CyFileRead(hScriptFile, pScriptFileBuf, fileSize, &fileSize);
            if ((NT_SUCCESS(NtStatus)) && (fileSize))
            {
                NtStatus = CyProcessScript(Device, pScriptFileBuf, fileSize);
                if (!NT_SUCCESS(NtStatus))
                {
					CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "Start CyExecuteScriptFile - Error Executing the Script file\n");                        
                }
            }
            else
            {
				CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "Start CyExecuteScriptFile -Unable to read script file to fill buffer\n");                                            
            }
			//Delete the allocated buffer, as we are done with processing the file buffer
			WdfObjectDelete(hScriptFileBufMem);            
        }
        else
        {
			CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "Start CyExecuteScriptFile -Error - Script file size returned zero length\n");            
            NtStatus = STATUS_INVALID_PARAMETER;
        }
        // close handle to script file
		CyFileClose(hScriptFile);
    }
    else
    {
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "Start CyExecuteScriptFile -Error - Unable to open handle to script file\n");                    
    }

	CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP, "End CyExecuteScriptFile :%x\n",NtStatus);
    return NtStatus;
} 

