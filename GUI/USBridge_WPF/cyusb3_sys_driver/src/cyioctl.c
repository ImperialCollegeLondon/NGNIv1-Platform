/*
 ## Cypress CyUSB3 driver source file (cyioctl.c)
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
  This source provide defination for all user IOCTLs.
*/
#include "..\inc\cyioctl.h"
#include "..\inc\cytrace.h"
#include "..\inc\cydef.h"
#include "..\inc\cydevice.h"
#include "..\inc\cyusbif.h"
#include "..\inc\cypnppower.h"
#include "..\inc\cyinterruptep.h"
#include "..\inc\cyiso.h"


#include <wdf.h>
#include <wdfusb.h>
#include <ntddk.h>
#include <ntintsafe.h>
#include <wdfrequest.h>

#if defined(EVENT_TRACING)
#include "cyioctl.tmh"
#endif

//extrern defination
extern USHORT CYUSB_ProductVersion;
extern USHORT CYUSB_ProductBuild;

//static method
static NTSTATUS ControlerTransferOperation(PDEVICE_CONTEXT pDevContext,									
									PSINGLE_TRANSFER pSingleTranfer,
									size_t szNumByteReturn);
static VOID CopyFroUserSetupRequestToWdf(IN PSETUP_PACKET UserSetupPacket,
										 OUT PWDF_USB_CONTROL_SETUP_PACKET pWdfControlSetupReq);
//ioctl handler function defination
VOID
CyIoctlHandler_GetCyUSBDriverVersion(
    __in
    WDFQUEUE Queue,
    __in
    WDFREQUEST Request,
    __in
    size_t OutputBufferLength,
    __in
    size_t InputBufferLength  
    )
{
	size_t szBytestoReturn=0,szRequiredSize=0,szOutputBuf=0;
	NTSTATUS NTStatus = STATUS_SUCCESS;
	PULONG ulDriverVersion=NULL;

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start CyIoctlHandler_GetCyUSBDriverVersion\n");
	
	szRequiredSize = sizeof(ULONG);

	NTStatus = WdfRequestRetrieveOutputBuffer(Request,
                                        szRequiredSize,  // Minimum required size
                                        &ulDriverVersion,
                                        &szOutputBuf);
    if(!NT_SUCCESS(NTStatus))
	{
       CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestRetrieveInputBuffer failed 0x%x\n", NTStatus);					    
	   goto ReqComp;
    }
	*ulDriverVersion = (ULONG)(CYUSB3_VERSION_MAJOR<<24 |(CYUSB3_VERSION_MINOR<<16) |(CYUSB3_VERSION_PATCH<<8) |(CYUSB3_VERSION_BUILD));
	 szBytestoReturn = sizeof(ULONG);
	 CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Driver Version is :%x\n",*ulDriverVersion);
	
ReqComp:
	 WdfRequestCompleteWithInformation(Request, NTStatus, szBytestoReturn);
	 CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_GetCyUSBDriverVersion\n");
}
VOID
CyIoctlHandler_GetUSBDIVersion(
    __in
    WDFQUEUE Queue,
    __in
    WDFREQUEST Request,
    __in
    size_t OutputBufferLength,
    __in
    size_t InputBufferLength  
    )
{
	size_t szBytestoReturn=0,szRequiredSize=0,szOutBufSize=0;
	NTSTATUS NTStatus = STATUS_SUCCESS;
	PULONG ulUSBDIVersion=NULL;
	WDFDEVICE           device;
    PDEVICE_CONTEXT     pDevContext;

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start CyIoctlHandler_GetUSBDIVersion\n");
		
	device = WdfIoQueueGetDevice(Queue);
    pDevContext = CyGetDeviceContext(device);
	szRequiredSize = 0;

	NTStatus = WdfRequestRetrieveOutputBuffer(Request,
                                        szRequiredSize,
                                        &ulUSBDIVersion,
                                        &szOutBufSize);
    if(!NT_SUCCESS(NTStatus))
	{
       CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestRetrieveInputBuffer failed 0x%x\n", NTStatus);					    
	   goto ReqComp;
    }

	*ulUSBDIVersion = pDevContext->ulUSBDIVersion;
	 szBytestoReturn = sizeof(ULONG);
	
ReqComp:
	 WdfRequestCompleteWithInformation(Request, NTStatus, szBytestoReturn);
	 CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_GetUSBDIVersion\n");
}
// NOTE : Interface number index start from 0
VOID
CyIoctlHandler_GetAltIntrfSetting(
    __in
    WDFQUEUE Queue,
    __in
    WDFREQUEST Request,
    __in
    size_t OutputBufferLength,
    __in
    size_t InputBufferLength  
    )
{
	size_t szBytestoReturn=0,szRequiredSize=0,szOutBufSize=0;
	NTSTATUS NTStatus = STATUS_SUCCESS;	
	PUCHAR pucInterfaceNum=NULL,pulAlternateInterface=NULL;
	WDFDEVICE           device;
    PDEVICE_CONTEXT     pDevContext;

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start CyIoctlHandler_GetAltIntrfSetting\n");
		
	device = WdfIoQueueGetDevice(Queue);
    pDevContext = CyGetDeviceContext(device);

	szRequiredSize = sizeof(UCHAR);
	
	NTStatus = WdfRequestRetrieveInputBuffer(Request,
                                        szRequiredSize,  // Minimum required size
                                        &pucInterfaceNum,
                                        &szOutBufSize);
    if(!NT_SUCCESS(NTStatus))
	{
       CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestRetrieveInputBuffer failed 0x%x\n", NTStatus);  
	   goto ReqComp;
    }


	// validate the input interface number
	/* CPP library does not send the valid interface number, to keep backward compatibility with the old wdm driver commenting this section.
	if((*pucInterfaceNum) > pDevContext->ucNumberOfInterface)
	{// invalid interface number / not exist 
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, " Input interface number is not valid :%x\n",*pucInterfaceNum);
		NTStatus = STATUS_INVALID_PARAMETER;
		goto ReqComp;
	}*/

	NTStatus = WdfRequestRetrieveOutputBuffer(Request,
                                        szRequiredSize,  // Minimum required size
                                        &pulAlternateInterface,
                                        &szOutBufSize);
    if(!NT_SUCCESS(NTStatus))
	{
       CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestRetrieveInputBuffer failed 0x%x\n", NTStatus);					    
	   goto ReqComp;
    }
	if(pDevContext->bIsMultiplInterface)
	{//multiple interface
		// Get alternate interface setting for given interface.
		*pulAlternateInterface = pDevContext->MultipleInterfacePair[(*pucInterfaceNum)].SettingIndex;
	}
	else
	{//single interface
		*pulAlternateInterface = pDevContext->ucActiveAltSettings;		
	}
	CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_IOCTL, "Alternate settings :%x for interface number :%x number of interface :%x\n",*pulAlternateInterface, *pucInterfaceNum, pDevContext->ucNumberOfInterface);
	szBytestoReturn = sizeof(UCHAR);
	
ReqComp:
	 WdfRequestCompleteWithInformation(Request, NTStatus, szBytestoReturn);
	 CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_GetAltIntrfSetting\n");
}
VOID
CyIoctlHandler_SetInterface(
    __in
    WDFQUEUE Queue,
    __in
    WDFREQUEST Request,
    __in
    size_t OutputBufferLength,
    __in
    size_t InputBufferLength  
    )
{
	size_t szBytestoReturn=0,szRequiredSize=0,szOutBufSize=0;
	NTSTATUS NTStatus = STATUS_SUCCESS;	
	PUCHAR ulAlternateInterface=NULL;
	WDFDEVICE           device;
    PDEVICE_CONTEXT     pDevContext;

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start CyIoctlHandler_SetInterface\n");
		
	device = WdfIoQueueGetDevice(Queue);
    pDevContext = CyGetDeviceContext(device);
	szRequiredSize = sizeof(UCHAR);	
	NTStatus = WdfRequestRetrieveInputBuffer(Request,
                                        szRequiredSize,  // Minimum required size
                                        &ulAlternateInterface,
                                        &szOutBufSize);
    if(!NT_SUCCESS(NTStatus))
	{
       CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestRetrieveInputBuffer failed 0x%x\n", NTStatus);					    
	   goto ReqComp;
    }
	// NOTE : SetInterface will set the alternate setting of ZEROth interface
	if(pDevContext->bIsMultiplInterface)
	{
		WDF_OBJECT_ATTRIBUTES  pipesAttributes;
		WDF_USB_INTERFACE_SELECT_SETTING_PARAMS  selectSettingParams;
		WDFUSBINTERFACE UsbInterface = pDevContext->MultipleInterfacePair[0].UsbInterface;
		
		CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_IOCTL, "Multiple interface\n");			    
		if((*ulAlternateInterface) > WdfUsbInterfaceGetNumSettings(UsbInterface))
		{// validate the input alternate interface number
			NTStatus = STATUS_INVALID_PARAMETER;
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
											  (*ulAlternateInterface)
											  );
		NTStatus = WdfUsbInterfaceSelectSetting(
											  UsbInterface,
											  &pipesAttributes,
											  &selectSettingParams
											  );
		if(!NT_SUCCESS(NTStatus))
		{
			CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfUsbInterfaceSelectSetting failed 0x%x\n", NTStatus);					    
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
											  (*ulAlternateInterface)
											  );
		NTStatus = WdfUsbInterfaceSelectSetting(
											  UsbInterface,
											  &pipesAttributes,
											  &selectSettingParams
											  );
		if(!NT_SUCCESS(NTStatus))
		{
			CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfUsbInterfaceSelectSetting failed 0x%x\n", NTStatus);					    
     	    goto ReqComp;
		}
		//After setting the alternate setting update the configured pipe handle table
		CyGetActiveAltInterfaceConfig(pDevContext);
	}
	CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_IOCTL, "Alternate interface settings successful 0x%x\n", *ulAlternateInterface);
	pDevContext->ucActiveAltSettings = *ulAlternateInterface;	
	szBytestoReturn = sizeof(UCHAR);

	// Driver provide bulk transfer like interface for interrupt transfer,interrupt reader is disabled.	
	//CyCheckAndConfigureInterruptInEp(pDevContext);

ReqComp:
	 WdfRequestCompleteWithInformation(Request, NTStatus, szBytestoReturn);
	 CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_SetInterface\n");
}
VOID
CyIoctlHandler_GetDeviceAddress(
    __in
    WDFQUEUE Queue,
    __in
    WDFREQUEST Request,
    __in
    size_t OutputBufferLength,
    __in
    size_t InputBufferLength  
    )
{
	
	NTSTATUS NTStatus = STATUS_INVALID_PARAMETER;		
	size_t szBytestoReturn = 0, szRequiredSize = 0, szOutBufferLength = 0, szDeviceAddressBufSize = 1;//sizeof(UCHAR);
	WDFDEVICE                   device;
    PDEVICE_CONTEXT             pDevContext;
	PUCHAR                      puDeviceAddress = NULL;
	//WDF_OBJECT_ATTRIBUTES       attributes;
    //DEVICE_REGISTRY_PROPERTY    DeviceProperty=DevicePropertyAddress;
	
	
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start CyIoctlHandler_GetDeviceAddress\n");
		
	device = WdfIoQueueGetDevice(Queue);
    pDevContext = CyGetDeviceContext(device);		
	//WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
	//attributes.ParentObject = device;

	/*NTStatus = WdfDeviceAllocAndQueryProperty(device,
											DeviceProperty,
											NonPagedPool,
											&attributes,
											&memory
											);
    
	if (!NT_SUCCESS(NTStatus)) 
	{
	   CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfDeviceAllocAndQueryProperty failed 0x%x\n", NTStatus);					    
	   goto ReqComp;		
	}*/
	// get input buffer
	szRequiredSize = szDeviceAddressBufSize;
	NTStatus = WdfRequestRetrieveOutputBuffer(Request,
                                        szRequiredSize,  // Minimum required size
                                        &puDeviceAddress,
                                        &szOutBufferLength);
	if(!NT_SUCCESS(NTStatus))
	{
       CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestRetrieveInputBuffer failed 0x%x\n", NTStatus);					    
	   goto ReqComp;
    }

    if (pDevContext)
        *((PUCHAR)puDeviceAddress) = (UCHAR)pDevContext->ucDeviceInstaceNumber;

	szBytestoReturn =  szDeviceAddressBufSize;
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Device address :0x%x\n", *puDeviceAddress);					    

ReqComp:
	WdfRequestCompleteWithInformation(Request, NTStatus, szBytestoReturn);	 
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_GetDeviceAddress\n");
}
VOID
CyIoctlHandler_GetNoOfEndpoint(
    __in
    WDFQUEUE Queue,
    __in
    WDFREQUEST Request,
    __in
    size_t OutputBufferLength,
    __in
    size_t InputBufferLength  
    )
{
	NTSTATUS NTStatus = STATUS_INVALID_PARAMETER;		
	size_t szBytestoReturn=0,szRequiredSize=0,szOutBuf=0;
	WDFDEVICE           device;
    PDEVICE_CONTEXT     pDevContext;
	PUCHAR pucNumberOfEp=0;

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start CyIoctlHandler_GetNoOfEndpoint\n");
	device = WdfIoQueueGetDevice(Queue);
    pDevContext = CyGetDeviceContext(device);	

	szRequiredSize = sizeof(UCHAR);
	NTStatus = WdfRequestRetrieveOutputBuffer(Request,
                                        szRequiredSize,  // Minimum required size
                                        &pucNumberOfEp,
                                        &szOutBuf);
    if(!NT_SUCCESS(NTStatus))
	{
       CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestRetrieveInputBuffer failed 0x%x\n", NTStatus);					    
	   goto ReqComp;
    }
	
	if(!(pDevContext->bIsMultiplInterface))
	{
		*pucNumberOfEp = WdfUsbInterfaceGetNumEndpoints(pDevContext->UsbInterfaceConfig.Types.SingleInterface.ConfiguredUsbInterface,pDevContext->ucActiveAltSettings);
		CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Single interface - Number of endpoint :0x%x\n", *pucNumberOfEp);					    
	}
	else
	{
		//TODO : Multiple interface
		//*pucNumberOfEp = WdfUsbInterfaceGetNumEndpoints(pDevContext->UsbInterfaceConfig.Types.SingleInterface.ConfiguredUsbInterface,pDevContext->ucActiveAltSettings);
	}
	szBytestoReturn = sizeof(UCHAR);

ReqComp:
	WdfRequestCompleteWithInformation(Request, NTStatus, szBytestoReturn);	 
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_GetNoOfEndpoint\n");
}
	VOID
CyIoctlHandler_GetPowerState(
    __in
    WDFQUEUE Queue,
    __in
    WDFREQUEST Request,
    __in
    size_t OutputBufferLength,
    __in
    size_t InputBufferLength  
    )
{
	NTSTATUS NTStatus = STATUS_INVALID_PARAMETER;		
	size_t szBytestoReturn=0,szRequiredSize=0;
	WDFDEVICE           device;
    PDEVICE_CONTEXT     pDevContext;
	PULONG DevPwState=NULL;
	WDF_DEVICE_POWER_STATE PwState;

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start CyIoctlHandler_GetPowerState\n");

	device = WdfIoQueueGetDevice(Queue);
    pDevContext = CyGetDeviceContext(device);	

	szRequiredSize = sizeof(ULONG);
	NTStatus = WdfRequestRetrieveOutputBuffer(Request,
                                        szRequiredSize,  // Minimum required size
                                        &DevPwState,
                                        NULL);
    if(!NT_SUCCESS(NTStatus))
	{
       CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestRetrieveInputBuffer failed 0x%x\n", NTStatus);					    
	   goto ReqComp;
    }		
	*DevPwState = 0; 	
	szBytestoReturn = sizeof(ULONG);
	NTStatus = STATUS_REQUEST_NOT_ACCEPTED;

ReqComp:
	WdfRequestCompleteWithInformation(Request, NTStatus, szBytestoReturn);	 
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_GetPowerState\n");
}
	// NOTE  As per the Framework guideline user can not set the device power state, it will be done base on the system power state in the driver and 
	// device bus activity. This API is kept for backware compatibilty with wdm driver. 
	// Refer WDK link link : ms-help://MS.WDK.v10.7600.090618.01/KMDF_d/hh/KMDF_d/Ch4_DFPnPPackage_6f5228dc-744a-4ea9-8bc3-067dd5f587d2.xml.htm
	VOID
CyIoctlHandler_SetPowerState(
    __in
    WDFQUEUE Queue,
    __in
    WDFREQUEST Request,
    __in
    size_t OutputBufferLength,
    __in
    size_t InputBufferLength  
    )
{
	NTSTATUS NTStatus = STATUS_INVALID_PARAMETER;		
	size_t szBytestoReturn=0,szRequiredSize=0;
	WDFDEVICE           device;
    PDEVICE_CONTEXT     pDevContext;
	PULONG DevPwState=NULL;
	WDF_DEVICE_POWER_STATE PwState;

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start CyIoctlHandler_SetPowerState\n");

	device = WdfIoQueueGetDevice(Queue);
    pDevContext = CyGetDeviceContext(device);	

	szRequiredSize = sizeof(ULONG);
	NTStatus = WdfRequestRetrieveInputBuffer(Request,
                                        szRequiredSize,  // Minimum required size
                                        &DevPwState,
                                        NULL);
    if(!NT_SUCCESS(NTStatus))
	{
       CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestRetrieveInputBuffer failed 0x%x\n", NTStatus);					    
	   goto ReqComp;
    }
	NTStatus = STATUS_REQUEST_NOT_ACCEPTED;
ReqComp:
	WdfRequestCompleteWithInformation(Request, NTStatus, szBytestoReturn);	 
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_SetPowerState\n");
}
	VOID
CyIoctlHandler_ControlTransfer(
    __in
    WDFQUEUE Queue,
    __in
    WDFREQUEST Request,
    __in
    size_t OutputBufferLength,
    __in
    size_t InputBufferLength  
    )
{
	NTSTATUS NtStatus = STATUS_INVALID_PARAMETER;		
	size_t szBytestoReturn=0,szRequiredSize=0,szBufferSize;
	WDFDEVICE           device;
    PDEVICE_CONTEXT     pDevContext;	
	PSINGLE_TRANSFER    pSingleTrasfer;	
	WDF_USB_CONTROL_SETUP_PACKET  controlSetupPacket;
	WDFMEMORY MemoryObject=NULL;
	WDFMEMORY_OFFSET MemoryOffset;
	WDF_REQUEST_SEND_OPTIONS     RequestSendOptions;


	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start CyIoctlHandler_ControlTransfer\n");
	device = WdfIoQueueGetDevice(Queue);
    pDevContext = CyGetDeviceContext(device);	
	
	NtStatus = WdfRequestRetrieveOutputMemory(Request,
											 &MemoryObject);
    if(!NT_SUCCESS(NtStatus))
	{
       CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestRetrieveInputMemory failed 0x%x\n", NtStatus);					    
	   goto ReqComp;
    }
    // get buffer point from memory object	
	pSingleTrasfer = WdfMemoryGetBuffer(MemoryObject, &szBufferSize);	
	// this is generic part of adding buffer length and offset for reciev/transmit data
	if(pSingleTrasfer->BufferLength>0)
	{
		MemoryOffset.BufferLength = pSingleTrasfer->BufferLength; //Request size
		MemoryOffset.BufferOffset = pSingleTrasfer->BufferOffset; //Request buffer start address
	}
	else
	{
		MemoryOffset.BufferLength = 0; //Request size
		MemoryOffset.BufferOffset = 0; //Request buffer start address
	}
	//initialize the control setup packet from received user Setup packet
	CopyFroUserSetupRequestToWdf(&(pSingleTrasfer->SetupPacket),&controlSetupPacket);
	switch(controlSetupPacket.Packet.bm.Request.Type)
	{
		case BMREQUEST_STANDARD:
		{
			switch(controlSetupPacket.Packet.bRequest)
			{
			case USB_REQUEST_GET_STATUS:
				{
					goto common;
					break;
				}
			case USB_REQUEST_CLEAR_FEATURE:
			case USB_REQUEST_SET_FEATURE:
				{
					goto common;
					break;
				}
			case USB_REQUEST_GET_DESCRIPTOR:
				{
					if((pDevContext->UsbDeviceDescriptor.bcdUSB & BCDUSBJJMASK) == BCDUSB30MAJORVER)
					{//for USB3.0 device only, This is special case where USBDI discarding the SS endpoint descriptor while retrieving the configuration , so reading it directly from the device.
						// Check if device has multiple interface, get configuration descriptor
						if(pDevContext->ucNumberOfInterfaceCompositUSB30Only>=2)
						{// USB3.0 device is composite device.
							CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "USB3.0 and it's composite device uses USBDI to read device configuration\n");					    
						}
						else
						{
							CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "USB3.0 Device , so reading configuration directly from device\n");					    
							goto common;
							break;
						}
					}
					if(controlSetupPacket.Packet.wValue.Bytes.HiByte == USB_CONFIGURATION_DESCRIPTOR_TYPE)
					{	/* WdfUsbTargetDeviceRetrieveConfigDescriptor function return the selected interface detail while the 
						   WdfUsbTargetDeviceFormatRequestForControlTransfer function return the device whole configuration(including the multiple interface)
						   which is we don't want, adding specific implementation for the getting configuration descriptor here*/						
						USHORT  ConfigLen = 0;
						PUSB_CONFIGURATION_DESCRIPTOR  configurationDescriptor = NULL;
						WDF_OBJECT_ATTRIBUTES  objectAttribs;
						WDFMEMORY  memoryHandle;
						// first get the configuration length
						WdfUsbTargetDeviceRetrieveConfigDescriptor(pDevContext->CyUsbDevice,
																			  NULL,
																			  &ConfigLen);
						CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP, "WdfUsbTargetDeviceRetrieveConfigDescriptor status:%x ,len :%x\n",NtStatus,ConfigLen);
						// allocate memory to get the device configuration
						WDF_OBJECT_ATTRIBUTES_INIT(&objectAttribs);
						objectAttribs.ParentObject = pDevContext->CyUsbDevice;
						NtStatus = WdfMemoryCreate(
												   &objectAttribs,
												   NonPagedPool,
												   CYMEM_TAG,
												   ConfigLen,
												   &memoryHandle,
												   (PVOID)&configurationDescriptor
												   );
						if (!NT_SUCCESS(NtStatus)) {
							CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "WdfMemoryCreate failed:%x \n",NtStatus);
							WdfRequestCompleteWithInformation(Request, NtStatus, 0);	
							CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "End CyIoctlHandler_ControlTransfer\n");
							return;
						}
						// Get the device whole configuration
						NtStatus = WdfUsbTargetDeviceRetrieveConfigDescriptor(
                                            pDevContext->CyUsbDevice,
                                            configurationDescriptor, // buffer
                                            &ConfigLen
                                            );
						if (!NT_SUCCESS(NtStatus)) 
						{
							CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "WdfUsbTargetDeviceRetrieveConfigDescriptor failed:%x \n",NtStatus);
							WdfRequestCompleteWithInformation(Request, NtStatus, 0);	
							CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "End CyIoctlHandler_ControlTransfer\n");
							return;
						}
						CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "Expected length :%x \n",controlSetupPacket.Packet.wLength);
						//copy the config descriptor to user buffer, it will copy only user requested length
						WdfMemoryCopyFromBuffer(MemoryObject,
												pSingleTrasfer->BufferOffset,
												configurationDescriptor,
												controlSetupPacket.Packet.wLength);
						pSingleTrasfer->NtStatus =NtStatus; // update status
						pSingleTrasfer->UsbdStatus =0;
						szBytestoReturn = controlSetupPacket.Packet.wLength; // number of byte user requested
						szBytestoReturn += pSingleTrasfer->BufferOffset; // plus the size of the input paramter
						WdfRequestCompleteWithInformation(Request, NtStatus, szBytestoReturn);	 // complete the request
						//delete the object
						WdfObjectDelete(memoryHandle);
						CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "End CyIoctlHandler_ControlTransfer\n");
						return;
					}
					goto common;
					break;
				}
			case USB_REQUEST_SET_DESCRIPTOR:
				{
					goto common;
					break;
				}
			case USB_REQUEST_GET_CONFIGURATION:				
			case USB_REQUEST_SET_CONFIGURATION:
				{
					goto common;
					break;
				}
			case USB_REQUEST_GET_INTERFACE:
			case USB_REQUEST_SET_INTERFACE:
				{
					goto common;
					break;
				}
			default:
				{	
					CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Not standard control request\n");					
					NtStatus =  STATUS_REQUEST_NOT_ACCEPTED;
					pSingleTrasfer->NtStatus = NtStatus;
					goto ReqComp;
					break;
				}
			}
			break;
		}
		case BMREQUEST_VENDOR:
		{			
			goto common;
			break;
		}
		case BMREQUEST_CLASS:
		{	
			goto common;
			break;
		}
		case TYPE_RESERVED:
		{
			CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "USB Request type reserved\n");
			NtStatus =  STATUS_REQUEST_NOT_ACCEPTED;
			pSingleTrasfer->NtStatus = NtStatus;
			goto ReqComp;
			break;
		}
	}
common:
	if(pSingleTrasfer->BufferLength>0)
	{
	NtStatus = WdfUsbTargetDeviceFormatRequestForControlTransfer(
				 pDevContext->CyUsbDevice,
                 Request,
                 &controlSetupPacket,
                 MemoryObject,
				 &MemoryOffset
                 );
	}
	else
	{
		NtStatus = WdfUsbTargetDeviceFormatRequestForControlTransfer(
				 pDevContext->CyUsbDevice,
                 Request,
                 &controlSetupPacket,
                 NULL,
				 NULL
                 );

	}
	if (!NT_SUCCESS(NtStatus))
	{
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfUsbTargetDeviceFormatRequestForControlTransfer failed 0x%x\n", NtStatus);					    
		goto ReqComp;				
	}
	WdfRequestSetCompletionRoutine(
								   Request,
								   EvtControlTransferCompletionRoutine,
								   pSingleTrasfer
								   );

	WDF_REQUEST_SEND_OPTIONS_INIT(&RequestSendOptions,
                                  WDF_REQUEST_SEND_OPTION_TIMEOUT);

	WDF_REQUEST_SEND_OPTIONS_SET_TIMEOUT(
                                     &RequestSendOptions,
                                     WDF_REL_TIMEOUT_IN_SEC(pSingleTrasfer->SetupPacket.ulTimeOut)									 
                                     );

	if (WdfRequestSend(
					   Request,
					   WdfUsbTargetDeviceGetIoTarget(pDevContext->CyUsbDevice),
					   &RequestSendOptions
					   ) == FALSE) 
	{
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestSend failed\n");
		NtStatus = WdfRequestGetStatus(Request);
		if(!NT_SUCCESS(NtStatus))
			WdfRequestCompleteWithInformation(Request, NtStatus, 0);
	}
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_ControlTransfer\n");
	return;

ReqComp:
	WdfRequestCompleteWithInformation(Request, NtStatus, szBytestoReturn);	 
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_ControlTransfer\n");
}
	VOID
CyIoctlHandler_BulkInterruptIsoOperation(
    __in
    WDFQUEUE Queue,
    __in
    WDFREQUEST Request,
    __in
    size_t OutputBufferLength,
    __in
    size_t InputBufferLength  
    )
{
	NTSTATUS NtStatus = STATUS_INVALID_PARAMETER;		
	size_t szBytestoReturn=0,szRequiredSize=0;
	WDFDEVICE           device;
    PDEVICE_CONTEXT     pDevContext;	
	PSINGLE_TRANSFER    pSingleTrasfer;	
	WDFMEMORY MemoryObject=NULL;
	WDFMEMORY_OFFSET MemoryOffset;
	PREQUEST_CONTEXT pReqContext;
	WDF_USB_PIPE_TYPE UsbPipeType;
	WDFUSBPIPE UsbPipeHandle;
	WDF_REQUEST_SEND_OPTIONS     RequestSendOptions;
	WDF_OBJECT_ATTRIBUTES  Attributes;
	size_t szBuf;
  
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start CyIoctlHandler_BulkInterruptIsoOperation\n");
	device = WdfIoQueueGetDevice(Queue);
    pDevContext = CyGetDeviceContext(device);	
	NtStatus = WdfRequestRetrieveOutputMemory(Request,
											 &MemoryObject);
    if(!NT_SUCCESS(NtStatus))
	{
       CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestRetrieveInputMemory failed 0x%x\n", NtStatus);					    
	   goto ReqComp;
    }
    // get buffer point from memory object
	pSingleTrasfer = WdfMemoryGetBuffer(MemoryObject, &szBuf);

	CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "Input buffer size 0x%x Buflen :%x,BufOff:%x IsoLen :%x,IsoOff :%x\n", szBuf, pSingleTrasfer->BufferLength,pSingleTrasfer->BufferOffset,pSingleTrasfer->IsoPacketLength,pSingleTrasfer->IsoPacketOffset);					    
	// this is generic part of adding buffer length and offset for reciev/transmit data for bulk and interrrupt transfer
	/*if(pSingleTrasfer->BufferLength<=0) // Allow to send zero length packet
	{// zero length
		NtStatus = STATUS_SUCCESS;
		szBytestoReturn =0;
		goto ReqComp;
	}*/
	 // Next, allocate context space for the request, so that the
    // driver can store handles to the memory objects that will
    // be created for input and output buffers.
    //
    WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(&Attributes,
                                        REQUEST_CONTEXT);
    NtStatus = WdfObjectAllocateContext(
                                      Request,
                                      &Attributes,
                                      &pReqContext
                                      );
    if(!NT_SUCCESS(NtStatus))
	{
        goto ReqComp;
    }

	UsbPipeType = CyFindUsbPipeType(pSingleTrasfer->ucEndpointAddress,pDevContext,&UsbPipeHandle);
	switch(UsbPipeType)
	{
	case WdfUsbPipeTypeBulk:
	case WdfUsbPipeTypeInterrupt:
		{
			pReqContext->ulRemainingSubRequest = (pSingleTrasfer->BufferLength/BULK_STAGESIZE);						
			pReqContext->ulLastRequestSize  = (pSingleTrasfer->BufferLength%BULK_STAGESIZE) ? (pSingleTrasfer->BufferLength%BULK_STAGESIZE):0;	
			pReqContext->UsbPipeHandle = UsbPipeHandle;
			pReqContext->pDevContext = pDevContext;
			pReqContext->WdfMemoryBufferIo = MemoryObject;
			CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Number of SubRequest:%x LastRequestSize:%x,BufferSize:%x\n",pReqContext->ulRemainingSubRequest,pReqContext->ulLastRequestSize,szRequiredSize);

			if(pReqContext->ulRemainingSubRequest>=1)
			{
				MemoryOffset.BufferLength = BULK_STAGESIZE;
				pReqContext->ulRemainingSubRequest--;		
				MemoryOffset.BufferOffset = pSingleTrasfer->BufferOffset;
				pReqContext->ulMemBufferOffset = pSingleTrasfer->BufferOffset;
			}
			else
			{
				MemoryOffset.BufferLength = pReqContext->ulLastRequestSize;
				pReqContext->ulLastRequestSize = 0;
				pReqContext->ulMemBufferOffset = (pReqContext->ulLastRequestSize+pSingleTrasfer->BufferOffset);
				MemoryOffset.BufferOffset = (pReqContext->ulLastRequestSize+pSingleTrasfer->BufferOffset);
			}

			if(WdfUsbTargetPipeIsInEndpoint(UsbPipeHandle)==TRUE)
			{//IN: Prepare request for Read
				CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "BULK IN  :0x%x\n", pSingleTrasfer->ucEndpointAddress);
				NtStatus = WdfUsbTargetPipeFormatRequestForRead(
					 UsbPipeHandle,
					 Request,					 
					 MemoryObject,
					 &MemoryOffset
					 );
				if (!NT_SUCCESS(NtStatus))
				{
					CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfUsbTargetPipeFormatRequestForRead failed 0x%x\n", NtStatus);					    
					goto ReqComp;				
				}				
			}
			else
			{//OUT : Prepare request for Write
				CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "BULK OUT  :0x%x\n", pSingleTrasfer->ucEndpointAddress);
				NtStatus = WdfUsbTargetPipeFormatRequestForWrite(
					 UsbPipeHandle,
					 Request,					 
					 MemoryObject,
					 &MemoryOffset
					 );
				if (!NT_SUCCESS(NtStatus))
				{
					CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfUsbTargetPipeFormatRequestForWrite failed 0x%x\n", NtStatus);					    
					goto ReqComp;				
				}				
			}
			WdfRequestSetCompletionRoutine(
										   Request,
										   EvtBulkRWBufferedIOCompletionRoutine,
										   pSingleTrasfer
										   );

			WDF_REQUEST_SEND_OPTIONS_INIT(&RequestSendOptions,
                                  WDF_REQUEST_SEND_OPTION_SYNCHRONOUS);
			

			if (WdfRequestSend(
							   Request,
							   WdfUsbTargetDeviceGetIoTarget(pDevContext->CyUsbDevice),
							   WDF_NO_SEND_OPTIONS//&RequestSendOptions
							   ) == FALSE) 
			{
				CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestSend failed\n");
				NtStatus = WdfRequestGetStatus(Request);
				if(!NT_SUCCESS(NtStatus))
					WdfRequestCompleteWithInformation(Request, NtStatus, 0);
			}
			CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_BulkInterruptIsoOperation\n");
			return;
			break;
		}		
	case WdfUsbPipeTypeIsochronous:
		{			
			CyIsoReadWrite(Request,pDevContext,UsbPipeHandle,FALSE);
			return;
		}
	case WdfUsbPipeTypeControl:
		{
			CyTraceEvents(TRACE_LEVEL_WARNING, DBG_IOCTL, "This ioctl does not support Control transfer operation\n");
			break;
		}
	default:
		{		
		 CyTraceEvents(TRACE_LEVEL_WARNING, DBG_IOCTL, "Invalid pipe type\n");
		 break;
		}
	}

ReqComp:
	WdfRequestCompleteWithInformation(Request, NtStatus, szBytestoReturn);	 
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_BulkInterruptIsoOperation\n");	
}
	VOID
CyIoctlHandler_CyclePort(
    __in
    WDFQUEUE Queue,
    __in
    WDFREQUEST Request,
    __in
    size_t OutputBufferLength,
    __in
    size_t InputBufferLength  
    )
{
	NTSTATUS NTStatus = STATUS_SUCCESS;		
	size_t szBytestoReturn=0;
	WDFDEVICE           device;
    PDEVICE_CONTEXT     pDevContext;

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start CyIoctlHandler_CyclePort\n");
		
	device = WdfIoQueueGetDevice(Queue);
    pDevContext = CyGetDeviceContext(device);	

	WdfIoTargetStop(
                    WdfDeviceGetIoTarget(device),
                    WdfIoTargetCancelSentIo
                    );

	NTStatus = WdfUsbTargetDeviceCyclePortSynchronously(pDevContext->CyUsbDevice);

	WdfIoTargetStart(WdfDeviceGetIoTarget(device));

	WdfRequestCompleteWithInformation(Request, NTStatus, szBytestoReturn);	 
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_CyclePort\n");
}
// if requested ep address does not match with the curretn cofig it will return STATUS_INVALID_PARAMETER
VOID CyIoctlHandler_ResetPipe(
    __in
    WDFQUEUE Queue,
    __in
    WDFREQUEST Request,
    __in
    size_t OutputBufferLength,
    __in
    size_t InputBufferLength  
    )
{
	NTSTATUS NtStatus = STATUS_INVALID_PARAMETER;		
	size_t szBytestoReturn=0,szRequiredSize=0;
	WDFDEVICE           device;
    PDEVICE_CONTEXT     pDevContext;
	PUCHAR puEpAddress;
	

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start CyIoctlHandler_ResetPipe\n");
		
	device = WdfIoQueueGetDevice(Queue);
    pDevContext = CyGetDeviceContext(device);	
	// get input buffer
	szRequiredSize = sizeof(UCHAR);	
	NtStatus = WdfRequestRetrieveInputBuffer(Request,
                                        szRequiredSize,  // Minimum required size
                                        &puEpAddress,
                                        NULL);
	if(!NT_SUCCESS(NtStatus))
	{
       CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestRetrieveInputBuffer failed 0x%x\n", NtStatus);					    
	   goto ReqComp;
    }
	// get the pipe address
	if(!(pDevContext->bIsMultiplInterface))
	{//single interface
		BYTE bCount=0,iIndex;
		WDF_USB_PIPE_INFORMATION  UsbPipeInfo;
		WDFUSBPIPE  UsbPipe;
		WDFUSBINTERFACE UsbInterface = pDevContext->UsbInterfaceConfig.Types.SingleInterface.ConfiguredUsbInterface;

		bCount = WdfUsbInterfaceGetNumConfiguredPipes(UsbInterface);
		for(iIndex=0;iIndex<bCount;iIndex++)
		{		
			WDF_USB_PIPE_INFORMATION_INIT(&UsbPipeInfo);

			UsbPipe = WdfUsbInterfaceGetConfiguredPipe(
                                            UsbInterface,
                                            iIndex,
                                            &UsbPipeInfo
                                            );
			if(UsbPipe)
			{
				if(UsbPipeInfo.EndpointAddress==*puEpAddress)
				{
					// send reset pipe command	
					NtStatus = WdfUsbTargetPipeResetSynchronously(
																UsbPipe, 
																WDF_NO_HANDLE,
																NULL
																);
					if(!NT_SUCCESS(NtStatus))
					{
					   CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfUsbTargetPipeResetSynchronously failed 0x%x\n", NtStatus);					    
					   goto ReqComp;
					}
					goto ReqComp;					
				}
				NtStatus = STATUS_UNSUCCESSFUL;
			}
		}
	}
	else
	{//multiple interface
		//TODO : implement
	}

ReqComp:
	WdfRequestCompleteWithInformation(Request, NtStatus, szBytestoReturn);	 
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_ResetPipe\n");
}

	VOID
CyIoctlHandler_ResetParentPort(
    __in
    WDFQUEUE Queue,
    __in
    WDFREQUEST Request,
    __in
    size_t OutputBufferLength,
    __in
    size_t InputBufferLength  
    )
{
	NTSTATUS NTStatus = STATUS_UNSUCCESSFUL;		
	size_t szBytestoReturn=0;
	WDFDEVICE           device;
    PDEVICE_CONTEXT     pDevContext;

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start CyIoctlHandler_ResetParentPort\n");
		
	device = WdfIoQueueGetDevice(Queue);
    pDevContext = CyGetDeviceContext(device);	

	NTStatus = WdfUsbTargetDeviceResetPortSynchronously(pDevContext->CyUsbDevice);

	if(!NT_SUCCESS(NTStatus))
	{
       CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfUsbTargetDeviceResetPortSynchronously failed 0x%x\n", NTStatus);					    	   
    }

	WdfRequestCompleteWithInformation(Request, NTStatus, szBytestoReturn);
	 
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_ResetParentPort\n");
}
	VOID
CyIoctlHandler_GetTransferSize(
    __in
    WDFQUEUE Queue,
    __in
    WDFREQUEST Request,
    __in
    size_t OutputBufferLength,
    __in
    size_t InputBufferLength  
    )
{
	//This request not valid : In Windows Server 2003, Windows XP and later operating systems, setting maximum transfer size member is not used and does not contain valid data. 
	NTSTATUS NTStatus = STATUS_SUCCESS;		
	size_t szBytestoReturn=0,szRequiredSize=0;
	WDFDEVICE           device;
    PDEVICE_CONTEXT     pDevContext;
	PSET_TRANSFER_SIZE_INFO pSetTransferInfo;

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start CyIoctlHandler_GetTransferSize\n");
		
	device = WdfIoQueueGetDevice(Queue);
    pDevContext = CyGetDeviceContext(device);	

	szRequiredSize = sizeof(SET_TRANSFER_SIZE_INFO);
	NTStatus = WdfRequestRetrieveOutputBuffer(Request,
                                        szRequiredSize,  // Minimum required size
                                        &pSetTransferInfo,
                                        NULL);
    if(!NT_SUCCESS(NTStatus))
	{
       CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestRetrieveInputBuffer failed 0x%x\n", NTStatus);					    
	   goto ReqComp;
    }	
	// MaximumTransferSize  parameter of WDF_USB_PIPE_INFORMATION is not used.
	pSetTransferInfo->TransferSize = 0; 
	NTStatus = STATUS_SUCCESS;
	szBytestoReturn = sizeof(SET_TRANSFER_SIZE_INFO);

ReqComp:
	WdfRequestCompleteWithInformation(Request, NTStatus, szBytestoReturn);	 
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_GetTransferSize\n");
}
	VOID
CyIoctlHandler_SetTransferSize(
    __in
    WDFQUEUE Queue,
    __in
    WDFREQUEST Request,
    __in
    size_t OutputBufferLength,
    __in
    size_t InputBufferLength  
    )
{
	//This request not valid : In Windows Server 2003, Windows XP and later operating systems, setting maximum transfer size member is not used and does not contain valid data. 
	NTSTATUS NTStatus = STATUS_SUCCESS;		
	size_t szBytestoReturn=0,szRequiredSize=0;
	WDFDEVICE           device;
    PDEVICE_CONTEXT     pDevContext;
	PSET_TRANSFER_SIZE_INFO pSetTransferInfo;

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start CyIoctlHandler_SetTransferSize\n");
		
	device = WdfIoQueueGetDevice(Queue);
    pDevContext = CyGetDeviceContext(device);	

	szRequiredSize = sizeof(SET_TRANSFER_SIZE_INFO);
	NTStatus = WdfRequestRetrieveOutputBuffer(Request,
                                        szRequiredSize,  // Minimum required size
                                        &pSetTransferInfo,
                                        NULL);
    if(!NT_SUCCESS(NTStatus))
	{
       CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestRetrieveInputBuffer failed 0x%x\n", NTStatus);					    
	   goto ReqComp;
    }		
	// MaximumTransferSize  parameter of WDF_USB_PIPE_INFORMATION is not used. 
	NTStatus = STATUS_SUCCESS;
	szBytestoReturn = sizeof(SET_TRANSFER_SIZE_INFO);

ReqComp:
	WdfRequestCompleteWithInformation(Request, NTStatus, szBytestoReturn);	 
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_SetTransferSize\n");
}
	VOID
CyIoctlHandler_GetDeviceName(
    __in
    WDFQUEUE Queue,
    __in
    WDFREQUEST Request,
    __in
    size_t OutputBufferLength,
    __in
    size_t InputBufferLength  
    )
{

	NTSTATUS NTStatus = STATUS_INVALID_PARAMETER;		
	size_t szBytestoReturn=0,szRequiredSize=0,szOutBufferSize=0;
	WDFDEVICE           device=NULL;
    PDEVICE_CONTEXT     pDevContext;	
	USHORT  usnumCharacters;
	PUSHORT  pusStrBuf;
	WDFMEMORY  MemoryHandle;
	PUCHAR puDeviceName=NULL;
	ANSI_STRING     ansiDeviceName;
    UNICODE_STRING  unicodeDeviceName;
	WDF_OBJECT_ATTRIBUTES    Attributes;

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start CyIoctlHandler_GetDeviceName\n");
	device = WdfIoQueueGetDevice(Queue);
    pDevContext = CyGetDeviceContext(device);	
	NTStatus = WdfUsbTargetDeviceQueryString(
										   pDevContext->CyUsbDevice,
										   NULL,
										   NULL,
										   NULL,
										   &usnumCharacters,
										   pDevContext->UsbDeviceDescriptor.iProduct,
										   LANG_ID
										   );
	if (!NT_SUCCESS(NTStatus)) 
	{	
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfUsbTargetDeviceQueryString(get string size) failed 0x%x\n", NTStatus);					    
		goto ReqComp;
	}
	WDF_OBJECT_ATTRIBUTES_INIT(&Attributes);
	Attributes.ParentObject = Request;
	NTStatus = WdfMemoryCreate(
							   &Attributes,
							   NonPagedPool,
							   CYMEM_TAG,
							   ((usnumCharacters*2)+2),// Adding more byte length if device doesn't return NULL terminated string
							   &MemoryHandle,
							   (PVOID)&pusStrBuf
							   );
	if (!NT_SUCCESS(NTStatus)) 
	{	
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfMemoryCreate failed 0x%x\n", NTStatus);					    
		goto ReqComp;
	}

	NTStatus = WdfUsbTargetDeviceQueryString(
										   pDevContext->CyUsbDevice,
										   NULL,
										   NULL,
										   pusStrBuf,
										   &usnumCharacters,
										   pDevContext->UsbDeviceDescriptor.iProduct,
										   LANG_ID
										   );

	if (!NT_SUCCESS(NTStatus)) 
	{	
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfUsbTargetDeviceQueryString(get string) failed 0x%x\n", NTStatus);					    
		goto ReqComp;
	}

	//Check if device returned string is NULL terminated, if not then add NULL character to string
	if(!(pusStrBuf[usnumCharacters-1]=='\0'))
		pusStrBuf[usnumCharacters]='\0';

	// Get output buffer
	szRequiredSize = (usnumCharacters*2)+1; //	usnumCharacters is unicode and required output is ansi NULL terminated string
	NTStatus = WdfRequestRetrieveOutputBuffer(Request,
                                        szRequiredSize,  // Minimum required size
                                        &puDeviceName,
                                        &szOutBufferSize);
	if(!NT_SUCCESS(NTStatus))
	{
       CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestRetrieveInputBuffer failed 0x%x\n", NTStatus);					    
	   goto ReqComp;
    }
	// convert Unicode to ansi string	            
    // initialize the ansi string
    ansiDeviceName.Length=0;
    ansiDeviceName.MaximumLength=(USHORT) szOutBufferSize;
    ansiDeviceName.Buffer=puDeviceName;

    // initialize the unicode string
    RtlInitUnicodeString(&unicodeDeviceName, (PCWSTR)pusStrBuf);

    // convert unicode string to ansi string
    RtlUnicodeStringToAnsiString(&ansiDeviceName, &unicodeDeviceName, FALSE); 
	
	szBytestoReturn = szRequiredSize;

ReqComp:
	WdfRequestCompleteWithInformation(Request, NTStatus, szBytestoReturn);	 
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_GetDeviceName\n");

}
	VOID
CyIoctlHandler_GetFriendlyName(
    __in
    WDFQUEUE Queue,
    __in
    WDFREQUEST Request,
    __in
    size_t OutputBufferLength,
    __in
    size_t InputBufferLength  
    )
{
	NTSTATUS NTStatus = STATUS_INVALID_PARAMETER;		
	size_t szBytestoReturn=0,szRequiredSize=0,szOutBufferLength=0,szFriendlyNameSize=0;
	WDFDEVICE           device;
    PDEVICE_CONTEXT     pDevContext;
	PVOID  puFriendlyName=NULL,pucFriendlBuftmp=NULL;	
	WDFMEMORY  memory;	
	ANSI_STRING     ansiFriendlyName;
    UNICODE_STRING  unicodeFriendlyName;
	
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start CyIoctlHandler_GetFriendlyName\n");
		
	device = WdfIoQueueGetDevice(Queue);
    pDevContext = CyGetDeviceContext(device);		
	NTStatus = WdfIoTargetAllocAndQueryTargetProperty(WdfDeviceGetIoTarget(device),
											DevicePropertyFriendlyName,
											NonPagedPool,
											WDF_NO_OBJECT_ATTRIBUTES,
											&memory);
	
	if (!NT_SUCCESS(NTStatus)) 
	{
	   CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "DevicePropertyFriendlyName - WdfIoTargetAllocAndQueryTargetProperty failed 0x%x\n", NTStatus);
	   NTStatus = WdfIoTargetAllocAndQueryTargetProperty(WdfDeviceGetIoTarget(device),
											DevicePropertyDeviceDescription,
											NonPagedPool,
											WDF_NO_OBJECT_ATTRIBUTES,
											&memory);
	   if (!NT_SUCCESS(NTStatus)) 
	   {
		   CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "DevicePropertyDeviceDescription - WdfIoTargetAllocAndQueryTargetProperty failed 0x%x\n", NTStatus);
		   goto ReqComp;		
	   }
	}
	// get input buffer
	pucFriendlBuftmp = WdfMemoryGetBuffer(memory, &szFriendlyNameSize);
	szRequiredSize   = szFriendlyNameSize;
	NTStatus = WdfRequestRetrieveOutputBuffer(Request,
                                        szRequiredSize,  // Minimum required size
                                        &puFriendlyName,
                                        &szOutBufferLength);
	if(!NT_SUCCESS(NTStatus))
	{
       CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestRetrieveInputBuffer failed 0x%x\n", NTStatus);					    
	   goto ReqComp;
    }	
	                
    // initialize the ansi string
    ansiFriendlyName.Length=0;
    ansiFriendlyName.MaximumLength=(USHORT)szOutBufferLength;
    ansiFriendlyName.Buffer=puFriendlyName;
    // initialize the unicode string
    RtlInitUnicodeString(&unicodeFriendlyName, (PCWSTR)pucFriendlBuftmp);
    // convert unicode string to ansi string
    RtlUnicodeStringToAnsiString(&ansiFriendlyName, &unicodeFriendlyName, FALSE);	
	szBytestoReturn =  szFriendlyNameSize;

ReqComp:
	WdfRequestCompleteWithInformation(Request, NTStatus, szBytestoReturn);
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_GetFriendlyName\n");
}
	VOID
CyIoctlHandler_AbortPipe(
    __in
    WDFQUEUE Queue,
    __in
    WDFREQUEST Request,
    __in
    size_t OutputBufferLength,
    __in
    size_t InputBufferLength  
    )
{
	NTSTATUS NtStatus = STATUS_INVALID_PARAMETER;		
	size_t szBytestoReturn=0,szRequiredSize=0;
	WDFDEVICE           device;
    PDEVICE_CONTEXT     pDevContext;
	PUCHAR puEpAddress=NULL;
	

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start CyIoctlHandler_AbortPipe\n");
		
	device = WdfIoQueueGetDevice(Queue);
    pDevContext = CyGetDeviceContext(device);	
	// get input buffer
	szRequiredSize = sizeof(UCHAR);	
	NtStatus = WdfRequestRetrieveInputBuffer(Request,
                                        szRequiredSize,  // Minimum required size
                                        &puEpAddress,
                                        NULL);
	if(!NT_SUCCESS(NtStatus))
	{
       CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestRetrieveInputBuffer failed 0x%x\n", NtStatus);					    
	   goto ReqComp;
    }
	// get the pipe address
	if(!(pDevContext->bIsMultiplInterface))
	{//single interface
		BYTE bCount=0,iIndex;
		WDF_USB_PIPE_INFORMATION  UsbPipeInfo;
		WDFUSBPIPE  UsbPipe;
		WDFUSBINTERFACE UsbInterface = pDevContext->UsbInterfaceConfig.Types.SingleInterface.ConfiguredUsbInterface;

		bCount = WdfUsbInterfaceGetNumConfiguredPipes(UsbInterface);
		for(iIndex=0;iIndex<bCount;iIndex++)
		{		
			WDF_USB_PIPE_INFORMATION_INIT(&UsbPipeInfo);

			UsbPipe = WdfUsbInterfaceGetConfiguredPipe(
                                            UsbInterface,
                                            iIndex,
                                            &UsbPipeInfo
                                            );
			if(UsbPipe)
			{
				if(UsbPipeInfo.EndpointAddress==*puEpAddress)
				{
					// send abort pipe command	
					NtStatus = WdfUsbTargetPipeAbortSynchronously(
													UsbPipe,
													WDF_NO_HANDLE,
													NULL
													);
					if(!NT_SUCCESS(NtStatus))
					{
					   CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfUsbTargetPipeAbortSynchronously failed 0x%x\n", NtStatus);					    
					   goto ReqComp;
					}
					goto ReqComp;
				}
			}
			NtStatus = STATUS_UNSUCCESSFUL;
		}
	}
	else
	{//multiple interface
		//TODO : implement
	}

ReqComp:
	WdfRequestCompleteWithInformation(Request, NtStatus, szBytestoReturn);	 
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_AbortPipe\n");
}
VOID
CyIoctlHandler_BulkIntIsoDirectTransfer(
    __in
    WDFQUEUE Queue,
    __in
    WDFREQUEST Request,
    __in
    size_t OutputBufferLength,
    __in
    size_t InputBufferLength  
    )
{
	NTSTATUS NtStatus = STATUS_INVALID_PARAMETER;		
	size_t szBytestoReturn=0,szRequiredSize=0;
	WDFDEVICE           device;
    PDEVICE_CONTEXT     pDevContext;	
	PREQUEST_CONTEXT    pReqContext;
	PSINGLE_TRANSFER    pSingleTrasfer;	
	WDF_USB_PIPE_TYPE UsbPipeType;
	WDFUSBPIPE UsbPipeHandle;	
	size_t InBufSize=0,OutBufSize=0;	
	WDF_REQUEST_SEND_OPTIONS     RequestSendOptions;	
	//URB Based 	
	PURB  pUrb = NULL;
	WDFMEMORY  urbMemory;
	USBD_PIPE_HANDLE wdmhUSBPipe;
	PMDL  pMainMdl,subMdl;	
	PUCHAR ulpVA;
	WDF_OBJECT_ATTRIBUTES    Attributes;

  
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start CyIoctlHandler_BulkIntIsoDirectTransfer\n");
	device = WdfIoQueueGetDevice(Queue);
    pDevContext = CyGetDeviceContext(device);	
	pReqContext = CyGetRequestContext(Request);
	
	//Validate buffer size
	// ZLP , the buffere size will be zero , don't validate the size of output buffer
	if((pReqContext->ulszInputMemoryBuffer<=0) /*|| (pReqContext->ulszOutputMemoryBuffer<=0) */)
	{// input buffer is zero
		NtStatus = STATUS_INVALID_PARAMETER;
		szBytestoReturn =0;
		goto ReqComp;
	}

	// Get input buffer
	pSingleTrasfer = WdfMemoryGetBuffer(pReqContext->InputMemoryBufferWrite, &InBufSize);	
	
	UsbPipeType = CyFindUsbPipeType(pSingleTrasfer->ucEndpointAddress,pDevContext,&UsbPipeHandle);
	switch(UsbPipeType)
	{
	case WdfUsbPipeTypeBulk:
	case WdfUsbPipeTypeInterrupt: //  This is a common code for both bulk and interrupt transfer. 
		{		
			WDFMEMORY_OFFSET WdfMemoryOffset;
			size_t szBufferSize=0;/* used when main request length is not multiple of BULK_STAGESIZE*/;			
			//Get size and find out does it requre sub request.
			if(pReqContext->ulszOutputMemoryBuffer<=0)
			{
				szRequiredSize =0;
			    ulpVA =NULL;
			}
			else
			{
				if(WdfUsbTargetPipeIsInEndpoint(UsbPipeHandle)==TRUE)			
						ulpVA = (PUCHAR)WdfMemoryGetBuffer(pReqContext->OutputMemoryBufferWrite,&szRequiredSize); //IN PIPE GET Output buffer VA
				else
						ulpVA = (PUCHAR)WdfMemoryGetBuffer(pReqContext->OutputMemoryBufferRead,&szRequiredSize);  //OUT PIPE GET Output buffer VA			
			}

			if(ulpVA)
			{
				pMainMdl = IoAllocateMdl(ulpVA,szRequiredSize,FALSE,TRUE,NULL);
				if(pMainMdl==NULL)
				{
					CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"IoAllocateMdl failed\n");
					goto ReqComp;
				}
				MmBuildMdlForNonPagedPool(pMainMdl);		
				ulpVA = (PUCHAR) MmGetMdlVirtualAddress(pMainMdl);
				pReqContext->Mdl = pMainMdl;
			}
			else
			{//This is for the ZLP	
				pMainMdl  = NULL;
				pReqContext->Mdl = NULL;	
			}
			
			if(szRequiredSize)
			{
				pReqContext->ulRemainingSubRequest = (szRequiredSize/BULK_STAGESIZE);						
				pReqContext->ulLastRequestSize  = (szRequiredSize%BULK_STAGESIZE) ? (szRequiredSize%BULK_STAGESIZE):0;
			}
			else
			{
				pReqContext->ulRemainingSubRequest = 0;						
				pReqContext->ulLastRequestSize  = 0;
			}
			
			pReqContext->UsbPipeHandle = UsbPipeHandle;
			pReqContext->pDevContext = pDevContext;
			CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Number of SubRequest:%x LastRequestSize:%x,BufferSize:%x\n",pReqContext->ulRemainingSubRequest,pReqContext->ulLastRequestSize,szRequiredSize);
			
			////////////URB based implementation
			//Create URB
			WDF_OBJECT_ATTRIBUTES_INIT(&Attributes);
			Attributes.ParentObject = Request;
			NtStatus = WdfMemoryCreate(
                          &Attributes,
                         NonPagedPool,
                         CYMEM_TAG,
                         sizeof(struct _URB_BULK_OR_INTERRUPT_TRANSFER),
                         &urbMemory,
                        (PVOID*) &pUrb
                         );
			if (!NT_SUCCESS(NtStatus))
			{
					CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfMemoryCreate URB failed 0x%x\n", NtStatus);
					goto ReqComp;				
			}
			RtlZeroMemory(pUrb,sizeof(struct _URB_BULK_OR_INTERRUPT_TRANSFER));
			//store the URB memory handle , latter in the completion routine we can delete it.
			pReqContext->UrbMemory = urbMemory;
			//Initialize URB
			wdmhUSBPipe = WdfUsbTargetPipeWdmGetPipeHandle(UsbPipeHandle);
			pUrb->UrbHeader.Length = (USHORT) sizeof(struct _URB_BULK_OR_INTERRUPT_TRANSFER);
			pUrb->UrbHeader.Function = URB_FUNCTION_BULK_OR_INTERRUPT_TRANSFER;
			pUrb->UrbBulkOrInterruptTransfer.PipeHandle = wdmhUSBPipe;			
			if(pReqContext->ulRemainingSubRequest)
			{				
				pReqContext->ulRemainingSubRequest--;
				pReqContext->ulMemBufferOffset =BULK_STAGESIZE;
				pUrb->UrbBulkOrInterruptTransfer.TransferBufferLength = BULK_STAGESIZE;
			}
			else
			{				
				pUrb->UrbBulkOrInterruptTransfer.TransferBufferLength =pReqContext->ulLastRequestSize;
				pReqContext->ulLastRequestSize = 0;
				pReqContext->ulMemBufferOffset = pReqContext->ulLastRequestSize;				
			}
			if(ulpVA==NULL)
			{			
				pUrb->UrbBulkOrInterruptTransfer.TransferBufferMDL = NULL;	
				pUrb->UrbBulkOrInterruptTransfer.TransferBuffer  = NULL;
				subMdl = NULL;
				pReqContext->pMainBufPtr = NULL;
			}
			else
			{
				pUrb->UrbBulkOrInterruptTransfer.TransferBuffer  = NULL;
				subMdl = IoAllocateMdl((PVOID) ulpVA,
								   pUrb->UrbBulkOrInterruptTransfer.TransferBufferLength,
								   FALSE,
								   FALSE,
								   NULL);

				if(subMdl == NULL) {

					CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"SubMDl IoAllocateMdl Failed \n");
					NtStatus = STATUS_INSUFFICIENT_RESOURCES;
					goto ReqComp;
				}

				IoBuildPartialMdl(pMainMdl,
								  subMdl,
								  (PVOID) ulpVA,
								  pUrb->UrbBulkOrInterruptTransfer.TransferBufferLength);

				pReqContext->pSubMdl = subMdl;
				pUrb->UrbBulkOrInterruptTransfer.TransferBufferMDL = subMdl;				
				pReqContext->pMainBufPtr = (ulpVA+pUrb->UrbBulkOrInterruptTransfer.TransferBufferLength);

			}		
			
			if(WdfUsbTargetPipeIsInEndpoint(UsbPipeHandle)==TRUE)
				pUrb->UrbBulkOrInterruptTransfer.TransferFlags = (USBD_TRANSFER_DIRECTION_IN|USBD_SHORT_TRANSFER_OK);
			else 
				pUrb->UrbBulkOrInterruptTransfer.TransferFlags = (USBD_TRANSFER_DIRECTION_OUT|USBD_SHORT_TRANSFER_OK);

			NtStatus = WdfUsbTargetPipeFormatRequestForUrb(
														 UsbPipeHandle,
														 Request,
														 urbMemory,
														 NULL
														 );
			if (!NT_SUCCESS(NtStatus))
			{
					CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfUsbTargetPipeFormatRequestForUrb failed 0x%x\n", NtStatus);
					goto ReqComp;				
			}
			
			WdfRequestSetCompletionRoutine(
										   Request,
										   EvtBulkRWURBCompletionRoutine,
										   pSingleTrasfer
										   );
			
			

			if (WdfRequestSend(
							   Request,
							   WdfUsbTargetDeviceGetIoTarget(pDevContext->CyUsbDevice),
							   WDF_NO_SEND_OPTIONS//&RequestSendOptions
							   ) == FALSE) 
			{
				CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestSend failed\n");
				NtStatus = WdfRequestGetStatus(Request);
				if(!NT_SUCCESS(NtStatus))
				{

					NTSTATUS tempStatus = WdfRequestUnmarkCancelable(Request);
					if (NT_SUCCESS(tempStatus)) {
						//
						// If WdfRequestUnmarkCancelable returns STATUS_SUCCESS 
						// that means the cancel routine has been removed. In that case
						// we release the reference otherwise the cancel routine does it.
						//
						WdfObjectDereference(Request);
					}
					WdfRequestCompleteWithInformation(Request, NtStatus, 0);
				}
			}
			CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_BulkIntIsoDirectTransfer\n");
			return;
			break;
		}
	case WdfUsbPipeTypeIsochronous:
		{
			CyIsoReadWrite(Request,pDevContext,UsbPipeHandle,TRUE);
			return;
		}
	case WdfUsbPipeTypeControl:
		{
			CyTraceEvents(TRACE_LEVEL_WARNING, DBG_IOCTL, "This ioctl does not support Control transfer operation\n");
			break;
		}
	default:
		{		
			CyTraceEvents(TRACE_LEVEL_WARNING, DBG_IOCTL, "Invalid pipe type\n");
			break;
		}
	}

ReqComp:
	WdfRequestCompleteWithInformation(Request, NtStatus, szBytestoReturn);	 
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_BulkIntIsoDirectTransfer\n");
}
VOID
CyIoctlHandler_GetDeviceSpeed(
    __in
    WDFQUEUE Queue,
    __in
    WDFREQUEST Request,
    __in
    size_t OutputBufferLength,
    __in
    size_t InputBufferLength  
    )
{
	NTSTATUS NTStatus = STATUS_UNSUCCESSFUL;		
	size_t szBytestoReturn=0,szRequiredSize=0;
	WDFDEVICE           device;
    PDEVICE_CONTEXT     pDevContext;
	PULONG pulDeviceSpeed=0;

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start CyIoctlHandler_GetDeviceSpeed\n");
		
	device = WdfIoQueueGetDevice(Queue);
    pDevContext = CyGetDeviceContext(device);	

	// get input buffer
	szRequiredSize = sizeof(UCHAR);	
	NTStatus = WdfRequestRetrieveInputBuffer(Request,
                                        szRequiredSize,  // Minimum required size
                                        &pulDeviceSpeed,
                                        NULL);

	if(!NT_SUCCESS(NTStatus))
	{
       CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestRetrieveInputBuffer failed 0x%x\n", NTStatus);					    	   
	   goto ReqComp;
    }

	if((pDevContext->UsbDeviceDescriptor.bcdUSB & BCDUSBJJMASK) == BCDUSB30MAJORVER)
	{// usb3.0 device
			*pulDeviceSpeed = DEVICE_SPEED_SUPER;
	}
	else
	{//usb2.0 device
		//TODO : Add Super speed check here
		if(pDevContext->ulUSBDeviceTrait & WDF_USB_DEVICE_TRAIT_AT_HIGH_SPEED)
			*pulDeviceSpeed = DEVICE_SPEED_HIGH;
		else
			*pulDeviceSpeed = DEVICE_SPEED_LOW_FULL;
	}
	szBytestoReturn = sizeof(ULONG);

ReqComp:
	WdfRequestCompleteWithInformation(Request, NTStatus, szBytestoReturn);	 
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_GetDeviceSpeed\n");
}
VOID
CyIoctlHandler_GetCurrentFrame(
    __in
    WDFQUEUE Queue,
    __in
    WDFREQUEST Request,
    __in
    size_t OutputBufferLength,
    __in
    size_t InputBufferLength  
    )
{
	NTSTATUS NTStatus = STATUS_UNSUCCESSFUL;		
	size_t szBytestoReturn=0,szRequiredSize=0;
	WDFDEVICE           device;
    PDEVICE_CONTEXT     pDevContext;
	PULONG pulCurrentFrNo =0;

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start CyIoctlHandler_GetCurrentFrame\n");
		
	device = WdfIoQueueGetDevice(Queue);
    pDevContext = CyGetDeviceContext(device);	

	// get input buffer
	szRequiredSize = sizeof(UCHAR);	
	NTStatus = WdfRequestRetrieveInputBuffer(Request,
                                        szRequiredSize,  // Minimum required size
                                        &pulCurrentFrNo,
                                        NULL);

	if(!NT_SUCCESS(NTStatus))
	{
       CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestRetrieveInputBuffer failed 0x%x\n", NTStatus);					    	   
	   goto ReqComp;
    }

	NTStatus = WdfUsbTargetDeviceRetrieveCurrentFrameNumber(
											  pDevContext->CyUsbDevice,
                                              pulCurrentFrNo
                                              );	
	if(!NT_SUCCESS(NTStatus))
	{
       CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfUsbTargetDeviceRetrieveCurrentFrameNumber failed 0x%x\n", NTStatus);					    	   
	   goto ReqComp;
    }

	szBytestoReturn = sizeof(ULONG);

ReqComp:
	WdfRequestCompleteWithInformation(Request, NTStatus, szBytestoReturn);	 
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End CyIoctlHandler_GetCurrentFrame\n");
}
VOID CopyFroUserSetupRequestToWdf(IN PSETUP_PACKET UserSetupPacket,OUT PWDF_USB_CONTROL_SETUP_PACKET pWdfControlSetupReq)
{
	pWdfControlSetupReq->Packet.bm.Byte = UserSetupPacket->bmRequest;	
	pWdfControlSetupReq->Packet.bRequest = UserSetupPacket->bRequest;
	pWdfControlSetupReq->Packet.wIndex.Value = UserSetupPacket->wIndex;
	pWdfControlSetupReq->Packet.wValue.Value = UserSetupPacket->wValue;
	pWdfControlSetupReq->Packet.wLength = UserSetupPacket->wLength;
}
VOID
  EvtControlTransferCompletionRoutine (
    IN WDFREQUEST  Request,
    IN WDFIOTARGET  Target,
    IN PWDF_REQUEST_COMPLETION_PARAMS  Params,
    IN WDFCONTEXT  Context
    )
{
	NTSTATUS NtStatus;
	PWDF_USB_REQUEST_COMPLETION_PARAMS ControlTrafrCompletionParams;
	size_t ByteRead=0;
	PSINGLE_TRANSFER pSingleTrasfer;
	PUCHAR ptr;

	UNREFERENCED_PARAMETER(Target);
    UNREFERENCED_PARAMETER(Context);

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start ControlTransferCompletionRoutine\n");

	pSingleTrasfer = (PSINGLE_TRANSFER)Context;
	NtStatus = Params->IoStatus.Status;
	ControlTrafrCompletionParams = Params->Parameters.Usb.Completion;
	if(ControlTrafrCompletionParams->Type == WdfUsbRequestTypeDeviceControlTransfer)
		ByteRead =  ControlTrafrCompletionParams->Parameters.DeviceControlTransfer.Length;
	else if(ControlTrafrCompletionParams->Type == WdfUsbRequestTypeDeviceString)
		ByteRead =  ControlTrafrCompletionParams->Parameters.DeviceControlTransfer.Length;

	pSingleTrasfer->NtStatus = NtStatus;
	pSingleTrasfer->UsbdStatus = ControlTrafrCompletionParams->UsbdStatus; 
	ByteRead +=pSingleTrasfer->BufferOffset; // Number of bytes read = SINGLE_TRANSFER + REQUESTED BYTE
	
	if (NT_SUCCESS(NtStatus)){
       CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL,
                    "Number of bytes read: %d\n", ByteRead);
    } else {
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL,
            "Read failed - request status 0x%x UsbdStatus 0x%x\n",
                NtStatus, ControlTrafrCompletionParams->UsbdStatus);

    }

	WdfRequestCompleteWithInformation(Request, NtStatus, ByteRead);
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End ControlTransferCompletionRoutine\n");
}

VOID
  EvtBulkRWURBCompletionRoutine (
    IN WDFREQUEST  Request,
    IN WDFIOTARGET  Target,
    IN PWDF_REQUEST_COMPLETION_PARAMS  Params,
    IN WDFCONTEXT  Context
    )
{
	NTSTATUS NtStatus,tmStatus;
	PWDF_USB_REQUEST_COMPLETION_PARAMS TrafrCompletionParams;	
	PSINGLE_TRANSFER pSingleTrasfer;
	PREQUEST_CONTEXT pMainReqContext;
    WDF_REQUEST_REUSE_PARAMS  params;
	WDFREQUEST pMainRequest;
	//URB Based 	
	PURB  pUrb = NULL;
	WDFMEMORY  urbMemory;
	USBD_PIPE_HANDLE wdmhUSBPipe;
	WDF_OBJECT_ATTRIBUTES    Attributes;

	
	UNREFERENCED_PARAMETER(Target);
    UNREFERENCED_PARAMETER(Context);

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start EvtBulkRWCompletionRoutine\n");
	// Get the main and sub request context	
	pMainReqContext = CyGetRequestContext(Request);
	NtStatus = Params->IoStatus.Status;
	TrafrCompletionParams = Params->Parameters.Usb.Completion;
	pSingleTrasfer = (PSINGLE_TRANSFER)Context;	
	pSingleTrasfer->NtStatus = NtStatus;
	pSingleTrasfer->UsbdStatus = TrafrCompletionParams->UsbdStatus; 
	pUrb = WdfMemoryGetBuffer(pMainReqContext->UrbMemory,NULL);
	if (NT_SUCCESS(NtStatus))
	{
		pMainReqContext->ulBytesReadWrite+= pUrb->UrbBulkOrInterruptTransfer.TransferBufferLength;
			CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL,
                    "Number of bytes read: %d\n", pMainReqContext->ulBytesReadWrite);
    }
	else
	{
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL,
            "ReadWrite failed - request status 0x%x UsbdStatus 0x%x\n",
                NtStatus, TrafrCompletionParams->UsbdStatus);
		if(NtStatus == STATUS_CANCELLED)
		{
			pSingleTrasfer->UsbdStatus = USBD_STATUS_CANCELED;
			goto ReqCancelled;
		}
    }
	//Free the allocated URB Memory
	if(pMainReqContext->UrbMemory)
	{
		WdfObjectDelete(pMainReqContext->UrbMemory);
		pMainReqContext->UrbMemory = NULL;
	}
	if(pMainReqContext->pSubMdl)
	{
		IoFreeMdl(pMainReqContext->pSubMdl);
		pMainReqContext->pSubMdl =NULL;
	}
	// Check for remaining requests
	if((pMainReqContext->ulRemainingSubRequest>=1) || (pMainReqContext->ulLastRequestSize!=0))
	{
		// Reuse the sub request
		WDF_REQUEST_REUSE_PARAMS_INIT(&params,WDF_REQUEST_REUSE_NO_FLAGS,STATUS_SUCCESS);
		NtStatus = WdfRequestReuse(Request,&params);
		if (!NT_SUCCESS(NtStatus))
		{// If reuse request fail then complete the main reuest			
			CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestReuse failed 0x%x\n", NtStatus);
			goto ReqComp;	
		}
		////////////////UBR 
		///////////URB based implementation
		//Create URB
		WDF_OBJECT_ATTRIBUTES_INIT(&Attributes);
		Attributes.ParentObject = Request;
		NtStatus = WdfMemoryCreate(
                     &Attributes,
                     NonPagedPool,
                     CYMEM_TAG,
                     sizeof(struct _URB_BULK_OR_INTERRUPT_TRANSFER),
                     &urbMemory,
                     &pUrb
                     );
		if (!NT_SUCCESS(NtStatus))
		{
				CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfMemoryCreate URB failed 0x%x\n", NtStatus);
				goto ReqComp;				
		}
		//store the URB memory handle , latter in the completion routine we can delete it.
		pMainReqContext->UrbMemory = urbMemory;
		wdmhUSBPipe = WdfUsbTargetPipeWdmGetPipeHandle(pMainReqContext->UsbPipeHandle);
		pUrb->UrbHeader.Length = (USHORT) sizeof(struct _URB_BULK_OR_INTERRUPT_TRANSFER);
		pUrb->UrbHeader.Function = URB_FUNCTION_BULK_OR_INTERRUPT_TRANSFER;
		pUrb->UrbBulkOrInterruptTransfer.PipeHandle = wdmhUSBPipe;
			
		if(pMainReqContext->ulRemainingSubRequest)
		{
			pMainReqContext->ulRemainingSubRequest--;
			pMainReqContext->ulMemBufferOffset+=BULK_STAGESIZE;
			pUrb->UrbBulkOrInterruptTransfer.TransferBufferLength = BULK_STAGESIZE;
		}
		else if(pMainReqContext->ulLastRequestSize)
		{			
			pUrb->UrbBulkOrInterruptTransfer.TransferBufferLength = pMainReqContext->ulLastRequestSize;			
			pMainReqContext->ulLastRequestSize = 0;
			pMainReqContext->ulMemBufferOffset+=pMainReqContext->ulLastRequestSize;			
		}
		pUrb->UrbBulkOrInterruptTransfer.TransferBuffer = NULL;
		/////
		pMainReqContext->pSubMdl = IoAllocateMdl((PVOID) pMainReqContext->pMainBufPtr,
							   pUrb->UrbBulkOrInterruptTransfer.TransferBufferLength,
							   FALSE,
							   FALSE,
							   NULL);
		if(pMainReqContext->pSubMdl == NULL)
		{
			CyTraceEvents(TRACE_LEVEL_INFORMATION,DBG_IOCTL,"SubMDl IoAllocateMdl Failed \n");
			NtStatus = STATUS_INSUFFICIENT_RESOURCES;
			goto ReqComp;
		}

		IoBuildPartialMdl(pMainReqContext->Mdl,
						  pMainReqContext->pSubMdl,
						  (PVOID) pMainReqContext->pMainBufPtr,
						  pUrb->UrbBulkOrInterruptTransfer.TransferBufferLength);

		
		pUrb->UrbBulkOrInterruptTransfer.TransferBufferMDL = pMainReqContext->pSubMdl;		
		pMainReqContext->pMainBufPtr = ((PUCHAR)pMainReqContext->pMainBufPtr+pUrb->UrbBulkOrInterruptTransfer.TransferBufferLength); //update the VA.
		
		if(TrafrCompletionParams->Type==WdfUsbRequestTypePipeRead)
			pUrb->UrbBulkOrInterruptTransfer.TransferFlags =(USBD_TRANSFER_DIRECTION_IN|USBD_SHORT_TRANSFER_OK);
		else 
			pUrb->UrbBulkOrInterruptTransfer.TransferFlags =(USBD_TRANSFER_DIRECTION_OUT|USBD_SHORT_TRANSFER_OK);

		NtStatus = WdfUsbTargetPipeFormatRequestForUrb(
													 pMainReqContext->UsbPipeHandle,
													 Request,
													 urbMemory,
													 NULL
													 );
		if (!NT_SUCCESS(NtStatus))
		{
				CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfUsbTargetPipeFormatRequestForUrb failed 0x%x\n", NtStatus);
				goto ReqComp;				
		}		
			
		WdfRequestSetCompletionRoutine(
									   Request,
									   EvtBulkRWURBCompletionRoutine,
									   pSingleTrasfer
									   );
		if (WdfRequestSend(
						   Request,
						   WdfUsbTargetDeviceGetIoTarget(pMainReqContext->pDevContext->CyUsbDevice),
						   WDF_NO_SEND_OPTIONS//&RequestSendOptions
						   ) == FALSE) 
		{
			CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestSend failed\n");
			NtStatus = WdfRequestGetStatus(Request);
			if(!NT_SUCCESS(NtStatus))
				goto ReqComp;
		}
		return;
	}
	// Free the main MDL
	if(pMainReqContext->Mdl)
	{
		IoFreeMdl(pMainReqContext->Mdl);
		pMainReqContext->Mdl = NULL;
	}

ReqComp:	
	WdfRequestCompleteWithInformation(Request, NtStatus, pMainReqContext->ulBytesReadWrite);	
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End EvtBulkRWCompletionRoutine\n");
	return;

ReqCancelled:
	if(pMainReqContext->pSubMdl)
	{
		IoFreeMdl(pMainReqContext->pSubMdl);
		pMainReqContext->pSubMdl =NULL;
	}

	if(pMainReqContext->Mdl)
	{
		IoFreeMdl(pMainReqContext->Mdl);
		pMainReqContext->Mdl = NULL;
	}

	goto ReqComp;
}
VOID
  EvtBulkRWCompletionRoutine (
    IN WDFREQUEST  Request,
    IN WDFIOTARGET  Target,
    IN PWDF_REQUEST_COMPLETION_PARAMS  Params,
    IN WDFCONTEXT  Context
    )
{
	NTSTATUS NtStatus,tmStatus;
	PWDF_USB_REQUEST_COMPLETION_PARAMS TrafrCompletionParams;	
	PSINGLE_TRANSFER pSingleTrasfer;
	PREQUEST_CONTEXT pMainReqContext;
    WDF_REQUEST_REUSE_PARAMS  params;
	WDFREQUEST pMainRequest;

	
	UNREFERENCED_PARAMETER(Target);
    UNREFERENCED_PARAMETER(Context);

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start EvtBulkRWCompletionRoutine\n");

	// Get the main and sub request context	
	pMainReqContext = CyGetRequestContext(Request);

	pSingleTrasfer = (PSINGLE_TRANSFER)Context;
	NtStatus = Params->IoStatus.Status;
	TrafrCompletionParams = Params->Parameters.Usb.Completion;
	pSingleTrasfer->NtStatus = NtStatus;
	pSingleTrasfer->UsbdStatus = TrafrCompletionParams->UsbdStatus; 
	
	if (NT_SUCCESS(NtStatus))
	{
		if(TrafrCompletionParams->Type == WdfUsbRequestTypePipeRead)
		{
			pMainReqContext->ulBytesReadWrite+=  TrafrCompletionParams->Parameters.PipeRead.Length;
			CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL,
                    "Number of bytes read: %d\n", pMainReqContext->ulBytesReadWrite);
		}
		else if(TrafrCompletionParams->Type == WdfUsbRequestTypePipeWrite)
		{
			pMainReqContext->ulBytesReadWrite+=  TrafrCompletionParams->Parameters.PipeWrite.Length;
			CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL,
                    "Number of bytes written: %d\n", pMainReqContext->ulBytesReadWrite);
		}
       
    }
	else
	{
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL,
            "ReadWrite failed - request status 0x%x UsbdStatus 0x%x\n",
                NtStatus, TrafrCompletionParams->UsbdStatus);
		if(NtStatus == STATUS_CANCELLED)
		{
			pSingleTrasfer->UsbdStatus = USBD_STATUS_CANCELED;
			goto ReqComp;
		}
    }

	// Check for remaining requests
	if((pMainReqContext->ulRemainingSubRequest>=1) || (pMainReqContext->ulLastRequestSize!=0))
	{
		WDFMEMORY_OFFSET WdfMemoryOffset;
		
		WdfMemoryOffset.BufferOffset =pMainReqContext->ulMemBufferOffset;
		if(pMainReqContext->ulRemainingSubRequest)
		{
			WdfMemoryOffset.BufferLength = BULK_STAGESIZE;
			pMainReqContext->ulRemainingSubRequest--;
			pMainReqContext->ulMemBufferOffset+=BULK_STAGESIZE;
		}
		else if(pMainReqContext->ulLastRequestSize)
		{
			WdfMemoryOffset.BufferLength = pMainReqContext->ulLastRequestSize;
			pMainReqContext->ulLastRequestSize = 0;
			pMainReqContext->ulMemBufferOffset+=pMainReqContext->ulLastRequestSize;
		}
		

		// Reuse the sub request
		WDF_REQUEST_REUSE_PARAMS_INIT(&params,WDF_REQUEST_REUSE_NO_FLAGS,STATUS_SUCCESS);
		NtStatus = WdfRequestReuse(Request,&params);
		if (!NT_SUCCESS(NtStatus))
		{// If reuse request fail then complete the main reuest			
			CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestReuse failed 0x%x\n", NtStatus);
			goto ReqComp;	
		}
		
		if(TrafrCompletionParams->Type==WdfUsbRequestTypePipeRead)
			{//IN: Prepare request for Read
				CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "BULK  IN  :0x%x\n", pSingleTrasfer->ucEndpointAddress);
				NtStatus = WdfUsbTargetPipeFormatRequestForRead(
					 pMainReqContext->UsbPipeHandle,
					 Request,					 
					 pMainReqContext->OutputMemoryBufferWrite,					  
					 &WdfMemoryOffset
					 );
				if (!NT_SUCCESS(NtStatus))
				{
					CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfUsbTargetPipeFormatRequestForRead failed 0x%x\n", NtStatus);
					goto ReqComp;				
				}
				
			}
			else
			{//OUT : Prepare request for Write
				CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "BULK  OUT  :0x%x\n", pSingleTrasfer->ucEndpointAddress);									
				NtStatus = WdfUsbTargetPipeFormatRequestForWrite(
					 pMainReqContext->UsbPipeHandle,
					 Request,					 
					 pMainReqContext->OutputMemoryBufferRead,
					 &WdfMemoryOffset
					 );
				if (!NT_SUCCESS(NtStatus))
				{
					CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfUsbTargetPipeFormatRequestForWrite failed 0x%x\n", NtStatus);					    
					goto ReqComp;				
				}
				
			}
			
			WdfRequestSetCompletionRoutine(
										   Request,
										   EvtBulkRWCompletionRoutine,
										   pSingleTrasfer
										   );
			if (WdfRequestSend(
							   Request,
							   WdfUsbTargetDeviceGetIoTarget(pMainReqContext->pDevContext->CyUsbDevice),
							   WDF_NO_SEND_OPTIONS//&RequestSendOptions
							   ) == FALSE) 
			{
				CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestSend failed\n");
				NtStatus = WdfRequestGetStatus(Request);
				if(!NT_SUCCESS(NtStatus))
					goto ReqComp;
			}
			return;
	}
	
ReqComp:	
	WdfRequestCompleteWithInformation(Request, NtStatus, pMainReqContext->ulBytesReadWrite);	
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End EvtBulkRWCompletionRoutine\n");

}
VOID
  EvtBulkRWBufferedIOCompletionRoutine (
    IN WDFREQUEST  Request,
    IN WDFIOTARGET  Target,
    IN PWDF_REQUEST_COMPLETION_PARAMS  Params,
    IN WDFCONTEXT  Context
    )
{
	NTSTATUS NtStatus,tmStatus;
	PWDF_USB_REQUEST_COMPLETION_PARAMS TrafrCompletionParams;	
	PSINGLE_TRANSFER pSingleTrasfer;
	PREQUEST_CONTEXT pMainReqContext;	
    WDF_REQUEST_REUSE_PARAMS  params;
	WDFREQUEST pMainRequest;

	
	UNREFERENCED_PARAMETER(Target);
    UNREFERENCED_PARAMETER(Context);

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Start EvtBulkRWBufferedIOCompletionRoutine\n");

	// Get the main and sub request context	
	pMainReqContext = CyGetRequestContext(Request);

	pSingleTrasfer = (PSINGLE_TRANSFER)Context;
	NtStatus = Params->IoStatus.Status;
	TrafrCompletionParams = Params->Parameters.Usb.Completion;
	pSingleTrasfer->NtStatus = NtStatus;
	pSingleTrasfer->UsbdStatus = TrafrCompletionParams->UsbdStatus; 
	
	if (NT_SUCCESS(NtStatus))
	{
		if(TrafrCompletionParams->Type == WdfUsbRequestTypePipeRead)
		{
			pMainReqContext->ulBytesReadWrite+=  TrafrCompletionParams->Parameters.PipeRead.Length;
			CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL,
                    "Number of bytes read: %d\n", pMainReqContext->ulBytesReadWrite);
		}
		else if(TrafrCompletionParams->Type == WdfUsbRequestTypePipeWrite)
		{
			pMainReqContext->ulBytesReadWrite+=  TrafrCompletionParams->Parameters.PipeWrite.Length;
			CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL,
                    "Number of bytes written: %d\n", pMainReqContext->ulBytesReadWrite);
		}
       
    }
	else
	{
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL,
            "ReadWrite failed - request status 0x%x UsbdStatus 0x%x\n",
                NtStatus, TrafrCompletionParams->UsbdStatus);
		if(NtStatus == STATUS_CANCELLED)
		{
			pSingleTrasfer->UsbdStatus = USBD_STATUS_CANCELED;
			goto ReqComp;
		}
    }

	// Check for remaining requests
	if((pMainReqContext->ulRemainingSubRequest>=1) || (pMainReqContext->ulLastRequestSize!=0))
	{
		WDFMEMORY_OFFSET WdfMemoryOffset;
		
		WdfMemoryOffset.BufferOffset =pMainReqContext->ulMemBufferOffset;
		if(pMainReqContext->ulRemainingSubRequest)
		{
			WdfMemoryOffset.BufferLength = BULK_STAGESIZE;
			pMainReqContext->ulRemainingSubRequest--;
			pMainReqContext->ulMemBufferOffset+=BULK_STAGESIZE;
		}
		else if(pMainReqContext->ulLastRequestSize)
		{
			WdfMemoryOffset.BufferLength = pMainReqContext->ulLastRequestSize;
			pMainReqContext->ulLastRequestSize = 0;
			pMainReqContext->ulMemBufferOffset+=pMainReqContext->ulLastRequestSize;
		}
		

		// Reuse the sub request
		WDF_REQUEST_REUSE_PARAMS_INIT(&params,WDF_REQUEST_REUSE_NO_FLAGS,STATUS_SUCCESS);
		NtStatus = WdfRequestReuse(Request,&params);
		if (!NT_SUCCESS(NtStatus))
		{// If reuse request fail then complete the main reuest			
			CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestReuse failed 0x%x\n", NtStatus);
			goto ReqComp;	
		}
		
		if(TrafrCompletionParams->Type==WdfUsbRequestTypePipeRead)
			{//IN: Prepare request for Read
				CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "BULK  IN  :0x%x\n", pSingleTrasfer->ucEndpointAddress);
				NtStatus = WdfUsbTargetPipeFormatRequestForRead(
					 pMainReqContext->UsbPipeHandle,
					 Request,					 
					 pMainReqContext->WdfMemoryBufferIo,					  
					 &WdfMemoryOffset
					 );
				if (!NT_SUCCESS(NtStatus))
				{
					CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfUsbTargetPipeFormatRequestForRead failed 0x%x\n", NtStatus);
					goto ReqComp;				
				}
				
			}
			else
			{//OUT : Prepare request for Write
				CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "BULK  OUT  :0x%x\n", pSingleTrasfer->ucEndpointAddress);									
				NtStatus = WdfUsbTargetPipeFormatRequestForWrite(
					 pMainReqContext->UsbPipeHandle,
					 Request,					 
					 pMainReqContext->WdfMemoryBufferIo,
					 &WdfMemoryOffset
					 );
				if (!NT_SUCCESS(NtStatus))
				{
					CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfUsbTargetPipeFormatRequestForWrite failed 0x%x\n", NtStatus);					    
					goto ReqComp;				
				}
				
			}
			
			WdfRequestSetCompletionRoutine(
										   Request,
										   EvtBulkRWBufferedIOCompletionRoutine,
										   pSingleTrasfer
										   );
			if (WdfRequestSend(
							   Request,
							   WdfUsbTargetDeviceGetIoTarget(pMainReqContext->pDevContext->CyUsbDevice),
							   WDF_NO_SEND_OPTIONS//&RequestSendOptions
							   ) == FALSE) 
			{
				CyTraceEvents(TRACE_LEVEL_ERROR, DBG_IOCTL, "WdfRequestSend failed\n");
				NtStatus = WdfRequestGetStatus(Request);
				if(!NT_SUCCESS(NtStatus))
					goto ReqComp;
			}
			return;
	}
	
ReqComp:
	pMainReqContext->ulBytesReadWrite = (pMainReqContext->ulBytesReadWrite + sizeof(SINGLE_TRANSFER)); // This is for buffered IO
	WdfRequestCompleteWithInformation(Request, NtStatus, pMainReqContext->ulBytesReadWrite);	
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "End EvtBulkRWBufferedIOCompletionRoutine :%d\n",pMainReqContext->ulBytesReadWrite);

}

// Preprocessing function for Neither IO buffering method
VOID
  CyEvtIoInCallerContextNeither  (
    IN WDFDEVICE  Device,
    IN WDFREQUEST  Request
    )
{
	NTSTATUS  NtStatus = STATUS_SUCCESS;
    PREQUEST_CONTEXT  pReqContext = NULL;
    WDF_OBJECT_ATTRIBUTES  Attributes;
    WDF_REQUEST_PARAMETERS  Params;
    size_t  szInBufLen, szOutBufLen;
    PVOID  pInBuf, pOutBuf;
	WDF_IO_QUEUE_STATE queueStatus;
	PDEVICE_CONTEXT     pDevContext;


    WDF_REQUEST_PARAMETERS_INIT(&Params);
    WdfRequestGetParameters(
                            Request,
                            &Params
                            );
    //
    // Check to see whether the driver received a METHOD_NEITHER I/O control code.
    // If not, just send the request back to the framework.
    //
    if(!(Params.Type == WdfRequestTypeDeviceControl &&
            Params.Parameters.DeviceIoControl.IoControlCode ==
			IOCTL_ADAPT_SEND_NON_EP0_DIRECT)) 
	{
        NtStatus = WdfDeviceEnqueueRequest(
                                         Device,
                                         Request
                                         );
		
		if (NtStatus == STATUS_WDF_BUSY)
		{
				pDevContext = CyGetDeviceContext(Device);	

				queueStatus = WdfIoQueueGetState(pDevContext->hQueue, NULL, NULL);
				if (WDF_IO_QUEUE_DRAINED(queueStatus))
				{
						WdfIoQueueStart(pDevContext->hQueue);
						NtStatus = WdfDeviceEnqueueRequest(Device, Request);
				}
		 }		
        if( !NT_SUCCESS(NtStatus) ) 
		{
			//CyTraceEvents(TRACE_LEVEL_ERROR, DBG_READ, "WdfRequestForwardToIoQueue 
			//		failed%X\n", NtStatus);
            goto End;
        }
        return;
    }

    //
    // The I/O control code is METHOD_NEITHER.
    // First, retrieve the virtual addresses of 
    // the input and output buffers.
    //
    NtStatus = WdfRequestRetrieveUnsafeUserInputBuffer(
                                                     Request,
                                                     0,
                                                     &pInBuf,
                                                     &szInBufLen
                                                     );
    if(!NT_SUCCESS(NtStatus)) {
        goto End;
    }
    NtStatus = WdfRequestRetrieveUnsafeUserOutputBuffer(
                                                      Request,
                                                      0,
                                                      &pOutBuf,
                                                      &szOutBufLen
                                                      );
    if(!NT_SUCCESS(NtStatus)) {
       goto End;
    }

	// Allow application to request more than 4 MBytes, Driver will split the main request into subrequests if requested length is more than 4MBytes.
	/*if((szInBufLen>STAGE_SIZE) || (szOutBufLen>STAGE_SIZE))
	{
		NtStatus = STATUS_INVALID_PARAMETER;
		goto End;
	}*/

	// Commenting this section, as user can send the ZLP packet so the length will be zero for the buffer.
	if((szInBufLen<=0) /*|| (szOutBufLen<=0)*/)
	{//input buffer is command buffer , and it should not be zero.
		NtStatus = STATUS_INVALID_PARAMETER;
		goto End;
	}
   //
    // Next, allocate context space for the request, so that the
    // driver can store handles to the memory objects that will
    // be created for input and output buffers.
    //
    WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(&Attributes,
                                        REQUEST_CONTEXT);
    NtStatus = WdfObjectAllocateContext(
                                      Request,
                                      &Attributes,
                                      &pReqContext
                                      );
    if(!NT_SUCCESS(NtStatus)) {
        goto End;
    }

    //
    // Next, probe and lock the read and write buffers.
    //
    NtStatus = WdfRequestProbeAndLockUserBufferForRead(
                                                     Request,
                                                     pInBuf,
                                                     szInBufLen,
                                                     &pReqContext->InputMemoryBufferRead
                                                     );
    if(!NT_SUCCESS(NtStatus)) {
        goto End;
    }
	//CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "WdfRequestProbeAndLockUserBufferForRead : pInBuf %X,szInBufLen %X\n",pInBuf,szInBufLen); 
	NtStatus = WdfRequestProbeAndLockUserBufferForWrite(
                                                     Request,
                                                     pInBuf,
                                                     szInBufLen,
                                                     &pReqContext->InputMemoryBufferWrite
                                                     );
    if(!NT_SUCCESS(NtStatus)) {
        goto End;
    }
	//CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "WdfRequestProbeAndLockUserBufferForWrite : pInBuf %X,szInBufLen %X\n",pInBuf,szInBufLen); 
	
	
			//		failed%X\n", NtStatus);
	// We are checking out buffer  size, to make sure that it doesn't crash for zero length packet
	if(szOutBufLen > 0 && pOutBuf != NULL && szOutBufLen < 0x01FFFFFF /*Hopefully, any uninitialized numbers will be caught here...*/)
	{
		NtStatus = WdfRequestProbeAndLockUserBufferForRead(
														  Request,
														  pOutBuf,
														  szOutBufLen,
														  &pReqContext->OutputMemoryBufferRead
														  );
		if(!NT_SUCCESS(NtStatus)) {
			goto End;
		}
		//CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "WdfRequestProbeAndLockUserBufferForRead : pOutBuf %X,szOutBufLen %X\n",pOutBuf,szOutBufLen); 

		NtStatus = WdfRequestProbeAndLockUserBufferForWrite(
														  Request,
														  pOutBuf,
														  szOutBufLen,
														  &pReqContext->OutputMemoryBufferWrite
														  );
		if(!NT_SUCCESS(NtStatus)) {
			goto End;
		}
		//CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "WdfRequestProbeAndLockUserBufferForWrite : pOutBuf %X,szOutBufLen %X\n",pOutBuf,szOutBufLen); 
	}
	else
	{// for ZLP set it to NULL
		pReqContext->OutputMemoryBufferRead = NULL;
		pReqContext->OutputMemoryBufferWrite = NULL;
	}
    // update request context 
	pReqContext->IsNeitherIO = TRUE; // Set the method is Neither IO
	pReqContext->ulszInputMemoryBuffer  = szInBufLen;
	pReqContext->ulszOutputMemoryBuffer = szOutBufLen;
	
	
    //
    // Finally, return the request to the framework.
    //
    NtStatus = WdfDeviceEnqueueRequest(
                                     Device,
                                     Request
                                     );
	if(!NT_SUCCESS(NtStatus)) {
        goto End;
    }
    return;

End:
    WdfRequestComplete(
                       Request,
                       NtStatus
                       );
    return;

}


