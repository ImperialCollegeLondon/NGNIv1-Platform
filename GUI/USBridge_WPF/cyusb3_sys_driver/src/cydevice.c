/*
 ## Cypress CyUSB3 driver source file (cydevice.c)
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
 * Descripton : This source file provides the callback routines for the IO manager to create 
 *				and add device object to driver stack. It also initialize callback routine for driver commmunication.
 *				It provide IOCTL dispatcher & initialization routine.
 *			    It provides the custome GUID API for registering the driver interface to application.		
 */
#include "..\inc\cydevice.h"
#include "..\inc\cyguid.h"
#include "..\inc\cypnppower.h"
#include "..\inc\cyio.h"
#include "..\inc\cytrace.h"
#if defined(EVENT_TRACING)
#include "cydevice.tmh"
#endif

#ifdef ALLOC_PRAGMA
#pragma alloc_text(PAGE, CyEvtDeviceAdd)
#endif

BOOLEAN CyGetOverriddenGUID(IN WDFDEVICE wdfDevice);



NTSTATUS
CyEvtDeviceAdd(
    WDFDRIVER Driver,
    PWDFDEVICE_INIT DeviceInit
    )
/*++
Routine Description:

    EvtDeviceAdd is called by the framework in response to AddDevice
    call from the PnP manager. We create and initialize a device object to
    represent a new instance of the device. All the software resources
    should be allocated in this callback.

Arguments:

    Driver	   - Handle to a framework driver object created in DriverEntry

    DeviceInit - Pointer to a framework-allocated WDFDEVICE_INIT structure.

Return Value:

    NTSTATUS

--*/
{
	WDF_FILEOBJECT_CONFIG				fileConfig;	
    WDF_PNPPOWER_EVENT_CALLBACKS        pnpPowerCallbacks;
    WDF_OBJECT_ATTRIBUTES               fdoAttributes,requestAttributes,queueAttributes,fileObjectAttributes;
    NTSTATUS                            status;
    WDFDEVICE                           device;
    WDF_DEVICE_PNP_CAPABILITIES         pnpCaps;    
    PDEVICE_CONTEXT                     pDevContext;    
    WDF_IO_QUEUE_CONFIG			        ioQueueConfig;
	WDFQUEUE                            hQueue;

    UNREFERENCED_PARAMETER(Driver);

    PAGED_CODE();

	CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP, "Start CyEvtDeviceAdd\n");
    //
    // Initialize the pnpPowerCallbacks structure.  Callback events for PNP
    // and Power are specified here.  If you don't supply any callbacks,
    // the Framework will take appropriate default actions based on whether
    // DeviceInit is initialized to be an FDO, a PDO or a filter device
    // object.
    //

    WDF_PNPPOWER_EVENT_CALLBACKS_INIT(&pnpPowerCallbacks);

    pnpPowerCallbacks.EvtDevicePrepareHardware = CyEvtDevicePrepareHardware;
	pnpPowerCallbacks.EvtDeviceReleaseHardware = CyEvtDeviceReleaseHardware;

    pnpPowerCallbacks.EvtDeviceD0Entry = CyEvtDeviceD0Entry; /* Called when device enter into the D0 power state*/
    pnpPowerCallbacks.EvtDeviceD0Exit  = CyEvtDeviceD0Exit;  /* Called when device leave the D0 power state*/


    WdfDeviceInitSetPnpPowerEventCallbacks(DeviceInit, &pnpPowerCallbacks);

    //
    // Initialize the request attributes to specify the context size and type
    // for every request created by framework for this device.
    //
    WDF_OBJECT_ATTRIBUTES_INIT(&requestAttributes);
    WDF_OBJECT_ATTRIBUTES_SET_CONTEXT_TYPE(&requestAttributes, REQUEST_CONTEXT);

    WdfDeviceInitSetRequestAttributes(DeviceInit, &requestAttributes);

    //
    // Initialize WDF_FILEOBJECT_CONFIG_INIT struct to tell the
    // framework whether you are interested in handle Create, Close and
    // Cleanup requests that gets genereate when an application or another
    // kernel component opens an handle to the device. If you don't register
    // the framework default behaviour would be complete these requests
    // with STATUS_SUCCESS. A driver might be interested in registering these
    // events if it wants to do security validation and also wants to maintain
    // per handle (fileobject) context.
    //
    WDF_FILEOBJECT_CONFIG_INIT(
        &fileConfig,
		NULL,/*CyEvtDeviceIoCreate,*/
        NULL,/*CyEvtDeviceIoClose,*/
        WDF_NO_EVENT_CALLBACK
        );

    //
    // Specify a context for FileObject. If you register FILE_EVENT callbacks,
    // the framework by default creates a framework FILEOBJECT corresponding
    // to the WDM fileobject. If you want to track any per handle context,
    // use the context for FileObject. Driver that typically use FsContext
    // field should instead use Framework FileObject context.
    //
    WDF_OBJECT_ATTRIBUTES_INIT(&fileObjectAttributes);
    WDF_OBJECT_ATTRIBUTES_SET_CONTEXT_TYPE(&fileObjectAttributes, FILE_CONTEXT);
    WdfDeviceInitSetFileObjectConfig(DeviceInit,
                                       &fileConfig,
                                       &fileObjectAttributes);

#if !defined(BUFFERED_READ_WRITE)
    //
    // I/O type is Buffered by default. We want to do direct I/O for Reads
    // and Writes so set it explicitly. Please note that this sample
    // can do isoch transfer only if the io type is directio.
    //
    WdfDeviceInitSetIoType(DeviceInit, WdfDeviceIoDirect);

#endif

	WdfDeviceInitSetIoInCallerContextCallback(
                                          DeviceInit, 
                                          CyEvtIoInCallerContextNeither
                                          );
    //
    // Now specify the size of device extension where we track per device
    // context.DeviceInit is completely initialized. So call the framework
    // to create the device and attach it to the lower stack.
    //
    WDF_OBJECT_ATTRIBUTES_INIT(&fdoAttributes);
    WDF_OBJECT_ATTRIBUTES_SET_CONTEXT_TYPE(&fdoAttributes, DEVICE_CONTEXT);

    status = WdfDeviceCreate(&DeviceInit, &fdoAttributes, &device);
    if (!NT_SUCCESS(status)) {
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "WdfDeviceCreate failed with Status code %!STATUS!\n",status);        
        return status;
    }

    
    //
    // Tell the framework to set the SurpriseRemovalOK in the DeviceCaps so
    // that you don't get the popup in usermode (on Win2K) when you surprise
    // remove the device.
    //
    WDF_DEVICE_PNP_CAPABILITIES_INIT(&pnpCaps);
    pnpCaps.SurpriseRemovalOK = WdfTrue;
    WdfDeviceSetPnpCapabilities(device, &pnpCaps);

    //
    // Register I/O callbacks to tell the framework that you are interested
    // in handling WdfRequestTypeRead, WdfRequestTypeWrite, and IRP_MJ_DEVICE_CONTROL requests.
    // WdfIoQueueDispatchParallel means that we are capable of handling
    // all the I/O request simultaneously and we are responsible for protecting
    // data that could be accessed by these callbacks simultaneously.
    // This queue will be,  by default,  automanaged by the framework with
    // respect to PNP and Power events. That is, framework will take care
    // of queuing, failing, dispatching incoming requests based on the current
    // pnp/power state of the device.
    //

    WDF_IO_QUEUE_CONFIG_INIT_DEFAULT_QUEUE(&ioQueueConfig,
                                           WdfIoQueueDispatchParallel);

    //ioQueueConfig.EvtIoRead = CyEvtIoRead;
    //ioQueueConfig.EvtIoWrite = CyEvtIoWrite;
    ioQueueConfig.EvtIoDeviceControl = CyEvtIoDeviceControl;
    //ioQueueConfig.EvtIoStop = CyEvtIoStop;
    //ioQueueConfig.EvtIoResume = CyEvtIoResume;

    status = WdfIoQueueCreate(device,
                              &ioQueueConfig,
                              WDF_NO_OBJECT_ATTRIBUTES,
                              &hQueue);// pointer to default queue
    if (!NT_SUCCESS(status)) {
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "WdfIoQueueCreate failed  for Default Queue %!STATUS!\n",status);		
        return status;
	}
	CyInitIoctlDispatcher(device); /* Initialize the IOCTL handler function pointer */ 

	pDevContext = CyGetDeviceContext(device);
	pDevContext->hQueue = hQueue;  //store the queque handle.

	// We don't need interrupt reader , since the driver provides generic implementation for interrupt transfer same bulk transfer.
	// interrupt in continous reader
	/*WDF_IO_QUEUE_CONFIG_INIT(&ioQueueConfig, WdfIoQueueDispatchManual);    
    ioQueueConfig.PowerManaged = WdfFalse;
    status = WdfIoQueueCreate(device,
                              &ioQueueConfig,
                              WDF_NO_OBJECT_ATTRIBUTES,
                              &pDevContext->IntInMsgQ
                              );
    if (!NT_SUCCESS(status)) {
        CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP,
            "WdfIoQueueCreate failed 0x%x\n", status);
        return status;
    }*/

	/* Need to understand the ISO QUEUE , why is it required here and what is alterantive mehanism
    WDF_IO_QUEUE_CONFIG_INIT(&ioQueueConfig,
                             WdfIoQueueDispatchParallel);

    WDF_OBJECT_ATTRIBUTES_INIT(&queueAttributes);
    queueAttributes.SynchronizationScope=WdfSynchronizationScopeQueue;
    

    ioQueueConfig.EvtIoRead = CyEvtIsochRead;

    status = WdfIoQueueCreate(device,
                              &ioQueueConfig,
                              &queueAttributes,
                              &pDevContext->IsochReadQ);// pointer to IsochRead queue
    if (!NT_SUCCESS(status)) {
        //UsbSamp_DbgPrint(1, ("WdfIoQueueCreate failed  for IsochRead Queue %!STATUS!\n", status));
        return status;
    }

    WDF_IO_QUEUE_CONFIG_INIT(&ioQueueConfig,
                             WdfIoQueueDispatchParallel);

    WDF_OBJECT_ATTRIBUTES_INIT(&queueAttributes);
    queueAttributes.SynchronizationScope=WdfSynchronizationScopeQueue;
    ioQueueConfig.EvtIoWrite = CyEvtIsochWrite;

    status = WdfIoQueueCreate(device,
                              &ioQueueConfig,
                              &queueAttributes,
                              &pDevContext->IsochWriteQ);// pointer to IsochWrite queue
    if (!NT_SUCCESS(status))
	{
        //UsbSamp_DbgPrint(1, ("WdfIoQueueCreate failed  for IsochWrite Queue %!STATUS!\n", status));
        return status;
    }
	*/	
	// Check if user has overridden the GUID or not, if yes then register driver with the overriden GUID.
    if(CyGetOverriddenGUID(device))
	{
		CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "CyGetOverriddenGUID success\n");        
	}
	else
	{
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "CyGetOverriddenGUID failed\n");
		CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "Driver will be registered with default GUID\n");        
	}
	//
	// Register a device interface so that app can find our device and talk to it.
	//
	status = WdfDeviceCreateDeviceInterface(device,
						(LPGUID) &DEFAULT_WDF_CYUSB_GUID_APP_INTERFACE,
						NULL);// Reference String
	if (!NT_SUCCESS(status)) {
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "WdfDeviceCreateDeviceInterface failed  %!STATUS!\n",status);        
		return status;
	}

    CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP, "End CyEvtDeviceAdd\n");
    return status;	
}
__drv_requiresIRQL(PASSIVE_LEVEL)
void 
CyInitIoctlDispatcher(
    __in WDFDEVICE Device
    )
{
	 PDEVICE_CONTEXT  pDeviceContext;

	 CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "Start CyInitIoctlDispatcher\n");

	 pDeviceContext = CyGetDeviceContext(Device);

	 pDeviceContext->CyDispatchIoctl[FUNCTION_FROM_CTL_CODE(IOCTL_ADAPT_GET_DRIVER_VERSION)]        = CyIoctlHandler_GetCyUSBDriverVersion;
	 pDeviceContext->CyDispatchIoctl[FUNCTION_FROM_CTL_CODE(IOCTL_ADAPT_GET_USBDI_VERSION)]         = CyIoctlHandler_GetUSBDIVersion;
	 pDeviceContext->CyDispatchIoctl[FUNCTION_FROM_CTL_CODE(IOCTL_ADAPT_GET_ALT_INTERFACE_SETTING)] = CyIoctlHandler_GetAltIntrfSetting;
	 pDeviceContext->CyDispatchIoctl[FUNCTION_FROM_CTL_CODE(IOCTL_ADAPT_SELECT_INTERFACE)]          = CyIoctlHandler_SetInterface;
	 pDeviceContext->CyDispatchIoctl[FUNCTION_FROM_CTL_CODE(IOCTL_ADAPT_GET_ADDRESS)]               = CyIoctlHandler_GetDeviceAddress;
	 pDeviceContext->CyDispatchIoctl[FUNCTION_FROM_CTL_CODE(IOCTL_ADAPT_GET_NUMBER_ENDPOINTS)]      = CyIoctlHandler_GetNoOfEndpoint;
	 pDeviceContext->CyDispatchIoctl[FUNCTION_FROM_CTL_CODE(IOCTL_ADAPT_GET_DEVICE_POWER_STATE)]    = CyIoctlHandler_GetPowerState;
	 pDeviceContext->CyDispatchIoctl[FUNCTION_FROM_CTL_CODE(IOCTL_ADAPT_SET_DEVICE_POWER_STATE)]    = CyIoctlHandler_SetPowerState;
	 pDeviceContext->CyDispatchIoctl[FUNCTION_FROM_CTL_CODE(IOCTL_ADAPT_SEND_EP0_CONTROL_TRANSFER)] = CyIoctlHandler_ControlTransfer;
	 pDeviceContext->CyDispatchIoctl[FUNCTION_FROM_CTL_CODE(IOCTL_ADAPT_SEND_NON_EP0_TRANSFER)]     = CyIoctlHandler_BulkInterruptIsoOperation;
	 pDeviceContext->CyDispatchIoctl[FUNCTION_FROM_CTL_CODE(IOCTL_ADAPT_CYCLE_PORT)]                = CyIoctlHandler_CyclePort;
	 pDeviceContext->CyDispatchIoctl[FUNCTION_FROM_CTL_CODE(IOCTL_ADAPT_RESET_PIPE)]                = CyIoctlHandler_ResetPipe;
	 pDeviceContext->CyDispatchIoctl[FUNCTION_FROM_CTL_CODE(IOCTL_ADAPT_RESET_PARENT_PORT)]         = CyIoctlHandler_ResetParentPort;
	 pDeviceContext->CyDispatchIoctl[FUNCTION_FROM_CTL_CODE(IOCTL_ADAPT_GET_TRANSFER_SIZE)]         = CyIoctlHandler_GetTransferSize;
	 pDeviceContext->CyDispatchIoctl[FUNCTION_FROM_CTL_CODE(IOCTL_ADAPT_SET_TRANSFER_SIZE)]         = CyIoctlHandler_SetTransferSize;
	 pDeviceContext->CyDispatchIoctl[FUNCTION_FROM_CTL_CODE(IOCTL_ADAPT_GET_DEVICE_NAME)]           = CyIoctlHandler_GetDeviceName;
	 pDeviceContext->CyDispatchIoctl[FUNCTION_FROM_CTL_CODE(IOCTL_ADAPT_GET_FRIENDLY_NAME)]         = CyIoctlHandler_GetFriendlyName;
	 pDeviceContext->CyDispatchIoctl[FUNCTION_FROM_CTL_CODE(IOCTL_ADAPT_ABORT_PIPE)]                = CyIoctlHandler_AbortPipe;
	 pDeviceContext->CyDispatchIoctl[FUNCTION_FROM_CTL_CODE(IOCTL_ADAPT_SEND_NON_EP0_DIRECT)]       = CyIoctlHandler_BulkIntIsoDirectTransfer;
	 pDeviceContext->CyDispatchIoctl[FUNCTION_FROM_CTL_CODE(IOCTL_ADAPT_GET_DEVICE_SPEED)]          = CyIoctlHandler_GetDeviceSpeed;
	 pDeviceContext->CyDispatchIoctl[FUNCTION_FROM_CTL_CODE(IOCTL_ADAPT_GET_CURRENT_FRAME)]         = CyIoctlHandler_GetCurrentFrame;

	 CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "End CyInitIoctlDispatcher\n");
}
// Get overridden guid if user has specified in the  INF
BOOLEAN  CyGetOverriddenGUID(IN WDFDEVICE wdfDevice)
{
	UNICODE_STRING unicodeGUID;
	NTSTATUS  NtStatus;
	WDF_OBJECT_ATTRIBUTES  attributes;
	WDFMEMORY  GUIDBufferMemHandle;
	PVOID  GUIDBuffer=NULL;
	ULONG GUIDBufferlen=0;


	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "Start CyGetOverriddenGUID\n");
	
   
	// Allocate buffer to get the registry key
	WDF_OBJECT_ATTRIBUTES_INIT(&attributes);	
	NtStatus = WdfMemoryCreate(
							 &attributes,
							 NonPagedPool,
							 0,
							 GUID_BUFFER_SIZE,
							 &GUIDBufferMemHandle,
							 &GUIDBuffer
							 );
	if (!NT_SUCCESS(NtStatus)) {
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "WdfMemoryCreate failed  %!STATUS!\n",NtStatus);        
		return FALSE;
	}
   
	 // Get the registry key
   if(GetRegistryKey(wdfDevice,NULL,REG_SZ,CYREG_GUID_PARAMETER_NAME,GUIDBuffer,&GUIDBufferlen))
   {
     unicodeGUID.Buffer = NULL;	 
	 RtlInitUnicodeString(&unicodeGUID, GUIDBuffer);
	 // Copy to GUID to DEFAULT_WDF_CYUSB_GUID_APP_INTERFACE variable
	 RtlGUIDFromString(&unicodeGUID, &DEFAULT_WDF_CYUSB_GUID_APP_INTERFACE);
	 WdfObjectDelete(GUIDBufferMemHandle);
	 return TRUE;
   }
   else
   {
	 WdfObjectDelete(GUIDBufferMemHandle);
	 return FALSE;
   }

   CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "End CyGetOverriddenGUID\n");
   return TRUE;
}
BOOLEAN GetRegistryKey(IN WDFDEVICE wdfDevice,					   
					   IN PWSTR SubkeyName OPTIONAL,
					   IN ULONG RegType,
					   IN PWSTR ParameterName,
					   IN OUT PVOID ParameterValue,
					   IN OUT PULONG ParameterValuelen)
{

	WDFKEY hKey=NULL;
	NTSTATUS NtStatus;
	UNICODE_STRING valueName;
	//DECLARE_CONST_UNICODE_STRING(valueName,CYREG_GUID_PARAMETER_NAME);
	ULONG  length, valueType, value;

	RtlInitUnicodeString(&valueName,ParameterName);
	// Open registry key
	NtStatus = WdfDeviceOpenRegistryKey(wdfDevice,
		                     PLUGPLAY_REGKEY_DEVICE,							 
							 KEY_QUERY_VALUE,   
							 NULL,
							 &hKey);
	// send query to read the key
	if (!NT_SUCCESS (NtStatus)) 
	{
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "WdfDeviceOpenRegistryKey Failed :%x\n",NtStatus);
		return FALSE;
	}
	NtStatus = WdfRegistryQueryValue(
                               hKey,
                               &valueName,
                               GUID_BUFFER_SIZE,
                               ParameterValue,
                               ParameterValuelen,
                               &valueType
                               );
	if (!NT_SUCCESS (NtStatus))
	{
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "WdfRegistryQueryValue Failed :%x\n",NtStatus);
		return FALSE;
	}
	
    return TRUE;
}