/*
 ## Cypress CyUSB3 driver source file (cypnppower.c)
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

#if (NTDDI_VERSION < NTDDI_WIN7)
	#include "..\inc\CyUSB30_def.h"
#else
	#ifndef USB30MAJORVER
		#define USB30MAJORVER 0x0300
	#endif
	#ifndef USB20MAJORVER
		#define USB20MAJORVER 0x0200
	#endif
#endif

#ifdef WIN7_DDK
	#include "..\inc\CyUSB30_def.h"
#endif
#include "..\inc\cyscript.h"

#include <wdfusb.h>
#include <Ntstrsafe.h>
#if defined(EVENT_TRACING)
#include "cypnppower.tmh"
#endif

#define CYREG_SCRIPTFILE L"DriverEXECSCRIPT"
#define CYREG_DRIVER_POWER_POLICY_SETUP L"DriverPowerPolicySetup"
#define CYREGSCRIPT_BUFFER_SIZE (sizeof(WCHAR)*256)
#ifdef ALLOC_PRAGMA
#pragma alloc_text(PAGE, CyEvtDevicePrepareHardware)
#pragma alloc_text(PAGE, CyEvtDeviceReleaseHardware)
#pragma alloc_text(PAGE, CyEvtDeviceD0Exit)
#pragma alloc_text(PAGE, CySelectInterfaces)
#pragma alloc_text(PAGE, CySetPowerPolicy)
#endif



static VOID cyGetMultipleInterfaceConfig(PDEVICE_CONTEXT pDevContext);
__drv_requiresIRQL(PASSIVE_LEVEL)
static NTSTATUS CyGetAndParseUSB30DeviceConfiguration(__in PDEVICE_CONTEXT pDevContext);
__drv_requiresIRQL(PASSIVE_LEVEL)
static NTSTATUS CyGetUSB30DeviceConfiguration(__in PDEVICE_CONTEXT pDevContext,WDFMEMORY *pUsb30DeviceConfig);
__drv_requiresIRQL(PASSIVE_LEVEL)
static void CyParseAndStoreUSB30DeviceConfiguration(__in PDEVICE_CONTEXT pDevContext,PVOID pUsb30DevConfigBuf,size_t Usb30DevConfigBufSize);

EVT_WDF_IO_QUEUE_STATE  EvtIoQueueState;

static USHORT deviceCount = 0;
static USHORT deviceId[1024]; 

VOID
  EvtIoQueueState (
    IN WDFQUEUE  Queue,
    IN WDFCONTEXT  Context
    )
 {
	 CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "Start EvtIoQueueState\n");

	 CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "End EvtIoQueueState\n");
}

NTSTATUS
  CyEvtDevicePrepareHardware (
    IN WDFDEVICE  Device,
    IN WDFCMRESLIST  ResourcesRaw,
    IN WDFCMRESLIST  ResourcesTranslated
    )
{
	NTSTATUS  NTStatus;
	PDEVICE_CONTEXT  pDeviceContext;
	WDF_USB_DEVICE_INFORMATION UsbDeviceInfo;
    ULONG ulWaitWakeEnable;
    USHORT nTemp = 0;

	UNICODE_STRING unicodeSCRIPTFILE;	
	WDF_OBJECT_ATTRIBUTES  attributes;
	WDFMEMORY  hScriptFileNameBufMem;
	PVOID  pScriptFNBuf=NULL;
	ULONG ScripFileNtBufferlen=0;

	UNREFERENCED_PARAMETER(ResourcesRaw);
    UNREFERENCED_PARAMETER(ResourcesTranslated);
    ulWaitWakeEnable = FALSE;

    PAGED_CODE();

    if (deviceCount == 0 ) 
    {
        //DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "CyUSB3 : Start CyEvtDevicePrepareHardware - First Device Arrival...\n");
        RtlZeroMemory(deviceId, sizeof(deviceId));    
    }
    deviceCount++;
    for (nTemp = 0; nTemp < deviceCount; nTemp++ ) {
        if (deviceId[nTemp] == 0)  {
            deviceId[nTemp] = (nTemp+1);
            break;
        }
    }

    CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "Start CyEvtDevicePrepareHardware\n");
    //DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "CyUSB3 : Start CyEvtDevicePrepareHardware - %d\n", (nTemp+1));

    pDeviceContext = CyGetDeviceContext(Device);

	//
    // Create a USB device handle so that we can communicate with the
    // underlying USB stack. The WDFUSBDEVICE handle is used to query,
    // configure, and manage all aspects of the USB device.
    // These aspects include device properties, bus properties,
    // and I/O creation and synchronization. We only create device the first
    // the PrepareHardware is called. If the device is restarted by pnp manager
    // for resource rebalance, we will use the same device handle but then select
    // the interfaces again because the USB stack could reconfigure the device on
    // restart.
    //	
    if (pDeviceContext->CyUsbDevice == NULL)
	{
        NTStatus = WdfUsbTargetDeviceCreate(Device,
                                    WDF_NO_OBJECT_ATTRIBUTES,
                                    &pDeviceContext->CyUsbDevice);
        if (!NT_SUCCESS(NTStatus)) {
            CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP,
                 "WdfUsbTargetDeviceCreate failed with Status code %!STATUS!\n", NTStatus);
            return NTStatus;
        }
    }    

	 //
    // Retrieve USBD version information, port driver capabilites and device
    // capabilites such as speed, power, etc.
    //
    WDF_USB_DEVICE_INFORMATION_INIT(&UsbDeviceInfo);

    NTStatus = WdfUsbTargetDeviceRetrieveInformation(
                                pDeviceContext->CyUsbDevice,
                                &UsbDeviceInfo);
    if (NT_SUCCESS(NTStatus))
	{
        CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "IsDeviceHighSpeed: %s\n",
            (UsbDeviceInfo.Traits & WDF_USB_DEVICE_TRAIT_AT_HIGH_SPEED) ? "TRUE" : "FALSE");
        CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP,
                    "IsDeviceSelfPowered: %s\n",
            (UsbDeviceInfo.Traits & WDF_USB_DEVICE_TRAIT_SELF_POWERED) ? "TRUE" : "FALSE");

        ulWaitWakeEnable = UsbDeviceInfo.Traits &
                            WDF_USB_DEVICE_TRAIT_REMOTE_WAKE_CAPABLE;

        //DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "CyUSB3 : IsDeviceRemoteWakeable: %s\n",
        //                    ulWaitWakeEnable ? "TRUE" : "FALSE");

        CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP,
                            "IsDeviceRemoteWakeable: %s\n",
                            ulWaitWakeEnable ? "TRUE" : "FALSE");
        
        pDeviceContext->ulUSBDeviceTrait = UsbDeviceInfo.Traits;
		pDeviceContext->ulUSBDIVersion = UsbDeviceInfo.UsbdVersionInformation.USBDI_Version;
		pDeviceContext->ulWaitWakeEnable = ulWaitWakeEnable;
    }   
	
    pDeviceContext->ucDeviceInstaceNumber = (nTemp+1);
    
	//Get device descriptor
	WdfUsbTargetDeviceGetDeviceDescriptor(
									  pDeviceContext->CyUsbDevice,
									  &pDeviceContext->UsbDeviceDescriptor
									  );
	
    NTStatus = CySelectInterfaces(Device);

    if (!NT_SUCCESS(NTStatus))
	{
        CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP,
                    "SelectInterfaces failed 0x%x\n", NTStatus);

        return NTStatus;
    }
   	
	// Check for the script file in the registry and execute if it is exist
	
	// Allocate buffer to get the registry key
	WDF_OBJECT_ATTRIBUTES_INIT(&attributes);	
	NTStatus = WdfMemoryCreate(
							 &attributes,
							 NonPagedPool,
							 0,
							 CYREGSCRIPT_BUFFER_SIZE,
							 &hScriptFileNameBufMem,
							 &pScriptFNBuf
							 );
	if (!NT_SUCCESS(NTStatus)) {
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "WdfMemoryCreate failed  %!STATUS!\n",NTStatus);        
		return FALSE;
	}
    
    if (ulWaitWakeEnable )
    {
        RTL_OSVERSIONINFOW osVer;
        ULONG nEnablePowerMgtSetting = 1;
        if (RtlGetVersion(&osVer) == STATUS_SUCCESS)
            nEnablePowerMgtSetting = ((osVer.dwMajorVersion < 6) ? 0 : 1); // Please disable the power management policies setup in XP and below.
                                                                           // To avoid annoying message box for device holding up on suspend.
        
        //DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "CyUSB3 : About to query registry for Power Policy.....\n");
        
        if (nEnablePowerMgtSetting && (GetRegistryKey(Device, NULL, REG_SZ, CYREG_DRIVER_POWER_POLICY_SETUP, pScriptFNBuf, &ScripFileNtBufferlen) == TRUE) )
        {
            // OKAY, we received existence of Power Policy registry configuration            
            UNICODE_STRING stringData;
            //DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "CyUSB3 : Registry Power Policy Setting %ws.....\n", (PWCHAR)pScriptFNBuf);

            //Initialize the Unicode String Structure.
            stringData.Buffer = (PWCHAR)pScriptFNBuf;
            RtlStringCbLengthW((PWCHAR)pScriptFNBuf, CYREGSCRIPT_BUFFER_SIZE, (size_t *)&stringData.Length);
            stringData.MaximumLength = CYREGSCRIPT_BUFFER_SIZE;            
            RtlUnicodeStringToInteger(&stringData, 10, &nEnablePowerMgtSetting);
        }

        if (nEnablePowerMgtSetting )
        {
            //DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "CyUSB3 : Setting up Power Policy.....\n");
            NTStatus = CySetPowerPolicy(Device);
            if (!NT_SUCCESS (NTStatus)) 
		    {
                CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP,
                                    "Set power policy failed  %!STATUS!\n", NTStatus);
                //DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "CyUSB3 : Set power policy failed  0x%X\n", NTStatus);
                return NTStatus;
            }
        }
    }
    
	RtlZeroMemory(pScriptFNBuf, CYREGSCRIPT_BUFFER_SIZE);
    if(GetRegistryKey(Device, NULL, REG_SZ, CYREG_SCRIPTFILE, pScriptFNBuf,&ScripFileNtBufferlen)) 
    {   
        CyExecuteScriptFile(Device, pScriptFNBuf);
    }
    
    CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "End CyEvtDevicePrepareHardware\n");		
        
	////
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "End CyEvtDevicePrepareHardware\n");	
    //DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "CyUSB3 : End CyEvtDevicePrepareHardware\n");
	return NTStatus;
}



NTSTATUS
  CyEvtDeviceReleaseHardware (
    IN WDFDEVICE  Device,
    IN WDFCMRESLIST  ResourcesTranslated
    )
{
	PDEVICE_CONTEXT  pDeviceContext;
	NTSTATUS NTStatus = STATUS_SUCCESS;
	WDF_IO_QUEUE_STATE queueStatus;

	PAGED_CODE();

    CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "Start CyEvtDeviceReleaseHardware\n");

    pDeviceContext = CyGetDeviceContext(Device);
	
	if(pDeviceContext->bIsMultiplInterface)
	{
		if(pDeviceContext->MultipleInterfacePair)
			ExFreePoolWithTag(pDeviceContext->MultipleInterfacePair,CYMEM_TAG);
	}
	
	
	/*The default queue does not stop even after device get disconnected and due to this the application device IO which are dependent on the driver
	  get stuck at that point and application hangs. To roslve this issue the queue should be stopped once the device is disconnected.*/
	WdfIoQueueStop(
			pDeviceContext->hQueue,
			EvtIoQueueState, 
			WDF_NO_CONTEXT 
			);	 
	WdfIoQueuePurge(
			pDeviceContext->hQueue,
			EvtIoQueueState, 
			WDF_NO_CONTEXT 
			);

	/*
	queueStatus = WdfIoQueueGetState(pDeviceContext->hQueue, NULL, NULL);
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "Queue Purge states :%d\n",(WDF_IO_QUEUE_PURGED(queueStatus)) ? TRUE : FALSE);
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "Queue Ready states :%d\n",(WDF_IO_QUEUE_READY(queueStatus)) ? TRUE : FALSE);
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "Queue Drain states :%d\n",(WDF_IO_QUEUE_DRAINED(queueStatus)) ? TRUE : FALSE);
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "Queue Stopped states :%d\n",(WDF_IO_QUEUE_STOPPED(queueStatus)) ? TRUE : FALSE);
    CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "Queue IDLE states :%d\n",(WDF_IO_QUEUE_IDLE(queueStatus)) ? TRUE : FALSE);
	*/
    
    if (pDeviceContext->ucDeviceInstaceNumber > 0 )
        deviceId[(pDeviceContext->ucDeviceInstaceNumber-1)] = 0;

    deviceCount--;
    //DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "CyUSB3 : End CyEvtDeviceReleaseHardware - %d\n", pDeviceContext->ucDeviceInstaceNumber);
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "End CyEvtDeviceReleaseHardware\n");

	return NTStatus;
}

__drv_requiresIRQL(PASSIVE_LEVEL)
NTSTATUS
CySelectInterfaces(
    __in WDFDEVICE Device
    )
{	    
    NTSTATUS                            NTStatus;
    PDEVICE_CONTEXT                     pDeviceContext;
	BYTE altSettings;
    	
    PAGED_CODE();

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP,"Start CySelectInterfaces\n");      

    pDeviceContext = CyGetDeviceContext(Device);

	pDeviceContext->ucNumberOfInterface = WdfUsbTargetDeviceGetNumInterfaces(pDeviceContext->CyUsbDevice);
	/*if(pDeviceContext->ucNumberOfInterface == 1)*/
	{		
		CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP," Single interface\n");      
		pDeviceContext->bIsMultiplInterface = FALSE;        
		WDF_USB_DEVICE_SELECT_CONFIG_PARAMS_INIT_SINGLE_INTERFACE(&pDeviceContext->UsbInterfaceConfig);
	}
	//else
	//{
	//	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP,"Multiple interface :0x%x\n",pDeviceContext->ucNumberOfInterface);      
	//	pDeviceContext->bIsMultiplInterface      = TRUE;
	//	pDeviceContext->MultipleInterfacePair = ExAllocatePoolWithTag(PagedPool,
	//																	sizeof(WDF_USB_INTERFACE_SETTING_PAIR) * pDeviceContext->ucNumberOfInterface,
	//																	CYMEM_TAG
	//																	 );
	//	if (pDeviceContext->MultipleInterfacePair == NULL)	
	//	{
	//		CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP,"Insufficient resources\n");      
	//		return STATUS_INSUFFICIENT_RESOURCES;   
	//	}

	//	//
	//	// Call driver-defined routine to populate the
	//	// WDF_USB_INTERFACE_SETTING_PAIR structures 
	//	// that ExAllocatePoolWithTag allocated.
	//	//
	//	InitInterfacePair(
	//					 pDeviceContext->CyUsbDevice,
	//					 pDeviceContext->MultipleInterfacePair,
	//					 pDeviceContext->ucNumberOfInterface
	//					 );

	//	WDF_USB_DEVICE_SELECT_CONFIG_PARAMS_INIT_MULTIPLE_INTERFACES(
	//				    &pDeviceContext->UsbInterfaceConfig,
	//					pDeviceContext->ucNumberOfInterface,
	//					pDeviceContext->MultipleInterfacePair
	//					);
	//}

    NTStatus = WdfUsbTargetDeviceSelectConfig(pDeviceContext->CyUsbDevice,
                                        WDF_NO_OBJECT_ATTRIBUTES,
                                        &pDeviceContext->UsbInterfaceConfig);
    if(!NT_SUCCESS(NTStatus)) 
	{
        CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP,
                        "WdfUsbTargetDeviceSelectConfig failed %!STATUS! \n",
                        NTStatus);               
        return NTStatus;
    }
	pDeviceContext->ucNumAltSettigns = WdfUsbInterfaceGetNumSettings(pDeviceContext->UsbInterfaceConfig.Types.SingleInterface.ConfiguredUsbInterface);
	pDeviceContext->ucActiveAltSettings = 0;  /* First active alternat settings will be ZERO */
	pDeviceContext->ucActiveConfigNum = 0; // This is the defaul configuration number
	
	if(!(pDeviceContext->bIsMultiplInterface))
	{//single interface
		CyGetActiveAltInterfaceConfig(pDeviceContext);
	}
	else
	{// Multiple interface
		cyGetMultipleInterfaceConfig(pDeviceContext);
	}

	pDeviceContext->ucNumberOfInterfaceCompositUSB30Only = 0; // intialize
    // Check if device is USB3.0 then parse the device configuration and store SS EP information
	if(pDeviceContext->UsbDeviceDescriptor.bcdUSB == USB30MAJORVER)
	{
		/*
		RTL_OSVERSIONINFOW lpVersionInformation= {0};
		//WORKAROUND : WIN 8 based xHCI does not provide USBDI version of xHCI driver stack, so adding this works around to differenciate OS
		// Get the OS version if it is WIN 8 then we do not need to parse the descriptor table to get the MaxBurst other paramter as it is being added to MaxPacketSize in Windows 9
		if(RtlGetVersion(&lpVersionInformation)==STATUS_SUCCESS)
		{
			CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP,"Operting System Version information\n");      
			CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP,"dwMajorVersion:%x dwMinorVersion:%x dwBuildNumber:%x\n",lpVersionInformation.dwMajorVersion
																								  ,lpVersionInformation.dwMinorVersion
																								  ,lpVersionInformation.dwBuildNumber);      
		}
		if(!((lpVersionInformation.dwMajorVersion==6) && (lpVersionInformation.dwMinorVersion ==2)))*/
			CyGetAndParseUSB30DeviceConfiguration(pDeviceContext);
	}

    CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP,"End CySelectInterfaces\n");      

   return NTStatus;
}
NTSTATUS
CyEvtDeviceD0Entry(
    WDFDEVICE Device,
    WDF_POWER_DEVICE_STATE PreviousState
    )
/*++

Routine Description:

    EvtDeviceD0Entry event callback must perform any operations that are
    necessary before the specified device is used.  It will be called every
    time the hardware needs to be (re-)initialized.

    This function is not marked pageable because this function is in the
    device power up path. When a function is marked pagable and the code
    section is paged out, it will generate a page fault which could impact
    the fast resume behavior because the client driver will have to wait
    until the system drivers can service this page fault.

    This function runs at PASSIVE_LEVEL, even though it is not paged.  A
    driver can optionally make this function pageable if DO_POWER_PAGABLE
    is set.  Even if DO_POWER_PAGABLE isn't set, this function still runs
    at PASSIVE_LEVEL.  In this case, though, the function absolutely must
    not do anything that will cause a page fault.

Arguments:

    Device - Handle to a framework device object.

    PreviousState - Device power state which the device was in most recently.
        If the device is being newly started, this will be
        PowerDeviceUnspecified.

Return Value:

    NTSTATUS

--*/
{
    PDEVICE_CONTEXT         pDeviceContext;
    NTSTATUS                NTStatus=STATUS_SUCCESS;

    pDeviceContext = CyGetDeviceContext(Device);

    CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_POWER,
                "Start CyEvtDeviceD0Entry previous PowerState %s\n",
				  GetDevicePowerString(PreviousState));

	pDeviceContext->WdfPowerState = WDFD0_ENTRY;
    //NTStatus = WdfIoTargetStart(WdfUsbTargetPipeGetIoTarget(pDeviceContext->InterruptPipe));

    CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_POWER, "End CyEvtDeviceD0Entry\n");

    return NTStatus;
}


NTSTATUS
CyEvtDeviceD0Exit(
    WDFDEVICE Device,
    WDF_POWER_DEVICE_STATE TargetState
    )
/*++

Routine Description:

    This routine undoes anything done in EvtDeviceD0Entry.  It is called
    whenever the device leaves the D0 state, which happens when the device is
    stopped, when it is removed, and when it is powered off.

    The device is still in D0 when this callback is invoked, which means that
    the driver can still touch hardware in this routine.


    EvtDeviceD0Exit event callback must perform any operations that are
    necessary before the specified device is moved out of the D0 state.  If the
    driver needs to save hardware state before the device is powered down, then
    that should be done here.

    This function runs at PASSIVE_LEVEL, though it is generally not paged.  A
    driver can optionally make this function pageable if DO_POWER_PAGABLE is set.

    Even if DO_POWER_PAGABLE isn't set, this function still runs at
    PASSIVE_LEVEL.  In this case, though, the function absolutely must not do
    anything that will cause a page fault.

Arguments:

    Device - Handle to a framework device object.

    TargetState - Device power state which the device will be put in once this
        callback is complete.

Return Value:

    Success implies that the device can be used.  Failure will result in the
    device stack being torn down.

--*/
{
    PDEVICE_CONTEXT         pDeviceContext;

    PAGED_CODE();

    CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_POWER,
          "Start CyDeviceD0Exit TargetPowerState %s\n",
          GetDevicePowerString(TargetState));

    pDeviceContext = CyGetDeviceContext(Device);

	pDeviceContext->WdfPowerState = WDFD0_EXIT;
    //WdfIoTargetStop(WdfUsbTargetPipeGetIoTarget(pDeviceContext->InterruptPipe),   WdfIoTargetCancelSentIo);
    CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_POWER, "End CyDeviceD0Exi\n");

    return STATUS_SUCCESS;
}
__drv_requiresIRQL(PASSIVE_LEVEL)
PCHAR
GetDevicePowerString(
    __in WDF_POWER_DEVICE_STATE PState
    )
{
    switch (PState)
    {
    case WdfPowerDeviceInvalid:
        return "WdfPowerDeviceInvalid";
    case WdfPowerDeviceD0:
        return "WdfPowerDeviceD0";
    case PowerDeviceD1:
        return "WdfPowerDeviceD1";
    case WdfPowerDeviceD2:
        return "WdfPowerDeviceD2";
    case WdfPowerDeviceD3:
        return "WdfPowerDeviceD3";
    case WdfPowerDeviceD3Final:
        return "WdfPowerDeviceD3Final";
    case WdfPowerDevicePrepareForHibernation:
        return "WdfPowerDevicePrepareForHibernation";
    case WdfPowerDeviceMaximum:
        return "PowerDeviceMaximum";
    default:
        return "UnKnown Device Power State";
    }
}


__drv_requiresIRQL(PASSIVE_LEVEL)
NTSTATUS
CySetPowerPolicy(
    __in WDFDEVICE Device
    )
{
    WDF_DEVICE_POWER_POLICY_IDLE_SETTINGS PowerIdleSettings;
    WDF_DEVICE_POWER_POLICY_WAKE_SETTINGS PowerWakeSettings;
    NTSTATUS    NTStatus = STATUS_SUCCESS;
//#define _HX3_HUB
#ifdef _HX3_HUB
    return NTStatus;
#endif

    PAGED_CODE();
    
    WDF_DEVICE_POWER_POLICY_IDLE_SETTINGS_INIT(&PowerIdleSettings, IdleCanWakeFromS0/*IdleUsbSelectiveSuspend*/);

    PowerIdleSettings.IdleTimeout = 10000; // 10-sec
    NTStatus = WdfDeviceAssignS0IdleSettings(Device, &PowerIdleSettings);
    if ( !NT_SUCCESS(NTStatus))
	{
        CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP,
                "WdfDeviceSetPowerPolicyS0IdlePolicy failed %x\n", NTStatus);
        return NTStatus;
    }
    
    WDF_DEVICE_POWER_POLICY_WAKE_SETTINGS_INIT(&PowerWakeSettings);

    NTStatus = WdfDeviceAssignSxWakeSettings(Device, &PowerWakeSettings);
    if (!NT_SUCCESS(NTStatus)) {
        CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP,
            "WdfDeviceAssignSxWakeSettings failed %x\n", NTStatus);
        return NTStatus;
    }

    return NTStatus;
}
__drv_requiresIRQL(PASSIVE_LEVEL)
VOID InitInterfacePair(__in WDFUSBDEVICE UsbDevice,
					   __in PWDF_USB_INTERFACE_SETTING_PAIR pUsbInterfacePair,
					   __in UCHAR ucNumberOfInterface)
{

	UCHAR iIndex;

	for(iIndex=0;iIndex<ucNumberOfInterface;iIndex++)
	{
		pUsbInterfacePair[iIndex].SettingIndex =0;
		pUsbInterfacePair[iIndex].UsbInterface = WdfUsbTargetDeviceGetInterface(
                                        UsbDevice,
                                        iIndex);
	}
}
// Get number of configure pipe and store all pipe handle
__drv_requiresIRQL(PASSIVE_LEVEL)
VOID CyGetActiveAltInterfaceConfig(__in PDEVICE_CONTEXT pDevContext)
{
	WDFUSBPIPE                          UsbPipe;
    WDF_USB_PIPE_INFORMATION            UsbPipeInfo;
    UCHAR                               ucIndex;
    UCHAR                               ucNumberConfiguredPipes;
	
	ucNumberConfiguredPipes = WdfUsbInterfaceGetNumConfiguredPipes(pDevContext->UsbInterfaceConfig.Types.SingleInterface.ConfiguredUsbInterface);	
	pDevContext->ucActiveNumOfPipe = ucNumberConfiguredPipes; /* Update the number of cofigured pipe */
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP,"Number of configured pipe 0x%x\n", pDevContext->ucActiveNumOfPipe); 

	pDevContext->ucActiveInterruptInPipe = 0; // Initialize
	for(ucIndex=0; ucIndex < ucNumberConfiguredPipes; ucIndex++) 
	{
		WDF_USB_PIPE_INFORMATION_INIT(&UsbPipeInfo);

		UsbPipe = WdfUsbInterfaceGetConfiguredPipe(
			pDevContext->UsbInterfaceConfig.Types.SingleInterface.ConfiguredUsbInterface,
			ucIndex,
			&UsbPipeInfo
			);        
		WdfUsbTargetPipeSetNoMaximumPacketSizeCheck(UsbPipe); /* disable check for the multiple of maximum packet size for read/write buffer */
		pDevContext->WdfUsbPipeArray[ucIndex] = UsbPipe; /* Store pipe handle */
		/* display information */		
		if(WdfUsbPipeTypeInterrupt == UsbPipeInfo.PipeType && (WdfUsbTargetPipeIsInEndpoint(UsbPipe)))
		{
			//Update the interrupt IN endpoint information
			pDevContext->WdfUsbInterruptInPipeArray[pDevContext->ucActiveInterruptInPipe]=UsbPipe;
			pDevContext->ucActiveInterruptInPipe++;

			CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP,
					"Interrupt Pipe is 0x%p\n", UsbPipe);  
			CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP,
					"Interrupt Pipe\n"); 					
		}
		if(WdfUsbPipeTypeBulk == UsbPipeInfo.PipeType) {
			CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP,
					"Bulk Pipe is 0x%p\n", UsbPipe);            // && WdfUsbTargetPipeIsInEndpoint(pipe
			CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP,
					"Bulk Pipe\n");			
		}        

		if(WdfUsbPipeTypeIsochronous == UsbPipeInfo.PipeType &&
				WdfUsbTargetPipeIsOutEndpoint(UsbPipe)) 
		{
			CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP,
					"Isochronous Pipe is 0x%p\n", UsbPipe);            
			CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP,
					"Isochronous Pipe\n");			
		}
		CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP,
				" MaximumPacketSize :%x\n", UsbPipeInfo.MaximumPacketSize);
		CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP,
				" EndpointAddress  :%x\n", UsbPipeInfo.EndpointAddress);
		CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP,
				" Interval   :%x\n", UsbPipeInfo.Interval);
		CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP,
				" SettingIndex    :%x\n", UsbPipeInfo.SettingIndex);  
	}

}
VOID cyGetMultipleInterfaceConfig(PDEVICE_CONTEXT pDevContext)
{
	WDFUSBPIPE                          UsbPipe;
    WDF_USB_PIPE_INFORMATION            UsbPipeInfo;
    UCHAR                               ucIndex,ucItfIndex;
    UCHAR                               ucNumberConfiguredPipes;
	WDF_USB_DEVICE_SELECT_CONFIG_PARAMS UsbConfigParams;	
	

	UsbConfigParams =  pDevContext->UsbInterfaceConfig;		

	for(ucItfIndex=0; ucItfIndex<UsbConfigParams.Types.MultiInterface.NumberOfConfiguredInterfaces  ;ucItfIndex++)
	{
		// usb interface handle
		WDFUSBINTERFACE  UsbInterface = UsbConfigParams.Types.MultiInterface.Pairs[ucItfIndex].UsbInterface;

		CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_PNP,
						"Alternate setting  is 0x%x\n", UsbConfigParams.Types.MultiInterface.Pairs[ucItfIndex].SettingIndex );  

		// TODO : Add getting the number of configured pipe
		
	}

} 
__drv_requiresIRQL(PASSIVE_LEVEL)
WDF_USB_PIPE_TYPE CyFindUsbPipeType(__in UCHAR ucEndpointAddress,__in PDEVICE_CONTEXT pDevContext,__out WDFUSBPIPE* UsbPipeHandle)
{
	WDFUSBPIPE                          UsbPipe;
    WDF_USB_PIPE_INFORMATION            UsbPipeInfo;    	
	UCHAR								ucIndex;

	for(ucIndex=0;ucIndex<pDevContext->ucActiveNumOfPipe;ucIndex++)
	{
		WDF_USB_PIPE_INFORMATION_INIT(&UsbPipeInfo);

		UsbPipe = WdfUsbInterfaceGetConfiguredPipe(
			pDevContext->UsbInterfaceConfig.Types.SingleInterface.ConfiguredUsbInterface,
			ucIndex,
			&UsbPipeInfo
			);        
		if(ucEndpointAddress == UsbPipeInfo.EndpointAddress )
		{
			*UsbPipeHandle =  UsbPipe;
			CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Endpoint:0x%x found\n",ucEndpointAddress);
			return  UsbPipeInfo.PipeType; 
		}
	}
	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "Endpoint 0x%x does not exist \n",ucEndpointAddress);
	return WdfUsbPipeTypeInvalid;
}
 __drv_requiresIRQL(PASSIVE_LEVEL)
NTSTATUS CyGetAndParseUSB30DeviceConfiguration(__in PDEVICE_CONTEXT pDevContext)
 {
	 NTSTATUS NtStatus = STATUS_SUCCESS;
	 WDFMEMORY pUsb30DeviceConfig =NULL;
	 PVOID pUsb30DeviceConfigBuf =NULL;
	 size_t szUsb30DeviceConfigBufSize =0;
	 // Get Device configuration.
	 NtStatus = CyGetUSB30DeviceConfiguration(pDevContext,&pUsb30DeviceConfig);
	 if (NT_SUCCESS(NtStatus) && pUsb30DeviceConfig) 
	 {
		 pUsb30DeviceConfigBuf = WdfMemoryGetBuffer(pUsb30DeviceConfig,&szUsb30DeviceConfigBufSize);
		 //Parse and store the Enpoint companion descriptor
		 CyParseAndStoreUSB30DeviceConfiguration(pDevContext,pUsb30DeviceConfigBuf,szUsb30DeviceConfigBufSize);

		 // Delete the device configuration memory object as it's no longer needed
	     WdfObjectDelete(pUsb30DeviceConfig);
	 }
	 
	 return NtStatus;
 }
__drv_requiresIRQL(PASSIVE_LEVEL)
static NTSTATUS CyGetUSB30DeviceConfiguration(__in PDEVICE_CONTEXT pDevContext,WDFMEMORY *pUsb30DeviceConfig)
{/* WdfUsbTargetDeviceRetrieveConfigDescriptor function return the selected interface detail while the 
    WdfUsbTargetDeviceFormatRequestForControlTransfer function return the device whole configuration(including the multiple interface)
    which is we don't want, adding specific implementation for the getting configuration descriptor here*/						
	USHORT  ConfigLen = 0;
	PUSB_CONFIGURATION_DESCRIPTOR  configurationDescriptor = NULL;
	WDF_OBJECT_ATTRIBUTES  objectAttribs;	
	NTSTATUS NtStatus =STATUS_SUCCESS;
	WDF_USB_CONTROL_SETUP_PACKET  controlSetupPacket;
	WDF_MEMORY_DESCRIPTOR  tmpmemoryDescriptor;
	WDF_MEMORY_DESCRIPTOR  tmpmemoryDescriptor1;
    USB_CONFIGURATION_DESCRIPTOR  UsbConfigDec;


	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "Start CyGetUSB30DeviceConfiguration\n");
	
	// first get the configuration length
	//Initialze the buffer
	WDF_MEMORY_DESCRIPTOR_INIT_BUFFER(&tmpmemoryDescriptor,
                                  (PVOID) &UsbConfigDec,
                                  sizeof(USB_CONFIGURATION_DESCRIPTOR));

	//Initialize control setup packet to get total configuration  length.
	controlSetupPacket.Packet.bm.Request.Dir = BmRequestDeviceToHost ;
	controlSetupPacket.Packet.bm.Request.Type = BmRequestStandard;
	controlSetupPacket.Packet.bm.Request.Recipient = BmRequestToDevice;
	controlSetupPacket.Packet.bRequest = USB_REQUEST_GET_DESCRIPTOR;
	controlSetupPacket.Packet.wIndex.Bytes.HiByte = 0;
	controlSetupPacket.Packet.wIndex.Bytes.LowByte = 0;
	controlSetupPacket.Packet.wValue.Bytes.HiByte = USB_CONFIGURATION_DESCRIPTOR_TYPE;
	controlSetupPacket.Packet.wValue.Bytes.LowByte =0;
	controlSetupPacket.Packet.wLength  = sizeof(USB_CONFIGURATION_DESCRIPTOR);

    NtStatus = WdfUsbTargetDeviceSendControlTransferSynchronously(
                                         pDevContext->CyUsbDevice,
                                         WDF_NO_HANDLE,
                                         NULL,
                                         &controlSetupPacket,
                                         &tmpmemoryDescriptor,
                                         NULL
                                         );
	if (!NT_SUCCESS(NtStatus)) 
	{
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "WdfUsbTargetDeviceSendControlTransferSynchronously failed:%x \n",NtStatus);		
		CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "End CyGetUSB30DeviceConfiguration\n");
		return NtStatus;
	}
	// allocate memory to get the device configuration
	WDF_OBJECT_ATTRIBUTES_INIT(&objectAttribs);
	objectAttribs.ParentObject = pDevContext->CyUsbDevice;
	// This object will be deleted after 
	NtStatus = WdfMemoryCreate(
							   &objectAttribs,
							   NonPagedPool,
							   CYMEM_TAG,
							   UsbConfigDec.wTotalLength,
							   pUsb30DeviceConfig,
							   (PVOID)&configurationDescriptor
							   );
	if (!NT_SUCCESS(NtStatus)) {
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "WdfMemoryCreate failed:%x \n",NtStatus);		
		CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "End CyGetUSB30DeviceConfiguration\n");
		return NtStatus;
	}
	WDF_MEMORY_DESCRIPTOR_INIT_HANDLE(&tmpmemoryDescriptor1,
                                  *pUsb30DeviceConfig,
                                  NULL);


	//Get whole device configuration using the wTotalLenght od configuration descriptor
	//Initialize control setup packet to get total configuration  length.
	controlSetupPacket.Packet.bm.Request.Dir = BmRequestDeviceToHost ;
	controlSetupPacket.Packet.bm.Request.Type = BmRequestStandard;
	controlSetupPacket.Packet.bm.Request.Recipient = BmRequestToDevice ;
	controlSetupPacket.Packet.bRequest = USB_REQUEST_GET_DESCRIPTOR;
	controlSetupPacket.Packet.wIndex.Bytes.HiByte = 0;
	controlSetupPacket.Packet.wIndex.Bytes.LowByte = 0;
	controlSetupPacket.Packet.wValue.Bytes.HiByte = USB_CONFIGURATION_DESCRIPTOR_TYPE;
	controlSetupPacket.Packet.wValue.Bytes.LowByte =0;
	controlSetupPacket.Packet.wLength  = UsbConfigDec.wTotalLength;

    NtStatus = WdfUsbTargetDeviceSendControlTransferSynchronously(
                                         pDevContext->CyUsbDevice,
                                         WDF_NO_HANDLE,
                                         NULL,
                                         &controlSetupPacket,
                                         &tmpmemoryDescriptor1,
                                         NULL
                                         );
	if (!NT_SUCCESS(NtStatus)) 
	{
		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_PNP, "WdfUsbTargetDeviceSendControlTransferSynchronously failed:%x \n",NtStatus);		
		CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "End CyGetUSB30DeviceConfiguration\n");
		return NtStatus;
	}

	CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_PNP, "End CyGetUSB30DeviceConfiguration\n");
	return NtStatus;
}
__drv_requiresIRQL(PASSIVE_LEVEL)
static void CyParseAndStoreUSB30DeviceConfiguration(__in PDEVICE_CONTEXT pDevContext,PVOID pUsb30DevConfigBuf,size_t Usb30DevConfigBufSize)
{
   // Parse the USB30 device configuration and store into a internal storage.
   int i,j;
   size_t ByteParsed = 0;
   PUCHAR tmpConfigBufptr = (PUCHAR) pUsb30DevConfigBuf;  

   PUSB_CONFIGURATION_DESCRIPTOR  pConfig = (PUSB_CONFIGURATION_DESCRIPTOR)tmpConfigBufptr;
   ByteParsed = sizeof(USB_CONFIGURATION_DESCRIPTOR);
   tmpConfigBufptr +=ByteParsed;
   
   //Initialize the SS_CUSTOMER_INFC
   RtlZeroMemory(pDevContext->SS_Custom_Infc,sizeof(pDevContext->SS_Custom_Infc));
   //Store the USB3.0 device number of interface information.
   pDevContext->ucNumberOfInterfaceCompositUSB30Only = pConfig->bNumInterfaces;
   CyTraceEvents(TRACE_LEVEL_INFORMATION, DBG_IOCTL, "pConfig->bNumInterfaces :%d\n",pConfig->bNumInterfaces);					    
   
   for(i=0;i<pConfig->bNumInterfaces;i++)
   {
	   if(ByteParsed>=Usb30DevConfigBufSize)
		   return;	   

	   do
	   {	   
		   PUSB_INTERFACE_DESCRIPTOR pIntfc = (PUSB_INTERFACE_DESCRIPTOR)tmpConfigBufptr;
		   PUSB_INTERFACE_DESCRIPTOR pIntfctmp = NULL;
		   ByteParsed +=sizeof(USB_INTERFACE_DESCRIPTOR);
	       tmpConfigBufptr += sizeof(USB_INTERFACE_DESCRIPTOR);
		   if(ByteParsed>=Usb30DevConfigBufSize)
				return;
		   if(pIntfc->bInterfaceClass == 0x08/*MassStorage*/) // This is condition to check if device configuration has MassStorage, if it is then allow user to read configuration from the device.
				pDevContext->ucNumberOfInterfaceCompositUSB30Only = 1;

		   for(j=0;j<pIntfc->bNumEndpoints ;j++)
		   {
			 PUSB_ENDPOINT_DESCRIPTOR pUsbEp = (PUSB_ENDPOINT_DESCRIPTOR)tmpConfigBufptr;
			 PUSB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR pSSEp = NULL;
			 pDevContext->SS_Custom_Infc[pIntfc->bInterfaceNumber][pIntfc->bAlternateSetting].SS_CustomerEp[j].bEndpointAddress = pUsbEp->bEndpointAddress;
			 pDevContext->SS_Custom_Infc[pIntfc->bInterfaceNumber][pIntfc->bAlternateSetting].SS_CustomerEp[j].wMaxPacketSize = pUsbEp->wMaxPacketSize;
			 ByteParsed+=sizeof(USB_ENDPOINT_DESCRIPTOR);
			 tmpConfigBufptr += sizeof(USB_ENDPOINT_DESCRIPTOR);
			 if(ByteParsed>=Usb30DevConfigBufSize)
				return;

			 //Check for SS Companion descriptor
			 pSSEp = (PUSB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR) tmpConfigBufptr;
			 if(pSSEp && (pSSEp->bLength ==0x6) && (pSSEp->bDescriptorType==0x30))
			 {	
				pDevContext->SS_Custom_Infc[pIntfc->bInterfaceNumber][pIntfc->bAlternateSetting].SS_CustomerEp[j].bMaxBurst = (pSSEp->bMaxBurst+1); //Adding one because Max burst start index is 0.
#ifdef WIN7_DDK
				pDevContext->SS_Custom_Infc[pIntfc->bInterfaceNumber][pIntfc->bAlternateSetting].SS_CustomerEp[j].bmAttributes = pSSEp->bmAttributes;
				pDevContext->SS_Custom_Infc[pIntfc->bInterfaceNumber][pIntfc->bAlternateSetting].SS_CustomerEp[j].bBytesPerInterval = pSSEp->bBytesPerInterval;
#else
				pDevContext->SS_Custom_Infc[pIntfc->bInterfaceNumber][pIntfc->bAlternateSetting].SS_CustomerEp[j].bmAttributes = pSSEp->bmAttributes.AsUchar;
				pDevContext->SS_Custom_Infc[pIntfc->bInterfaceNumber][pIntfc->bAlternateSetting].SS_CustomerEp[j].bBytesPerInterval = pSSEp->wBytesPerInterval;
#endif				

				ByteParsed+=sizeof(USB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR);
				tmpConfigBufptr += sizeof(USB_SUPERSPEED_ENDPOINT_COMPANION_DESCRIPTOR);
				if(ByteParsed>=Usb30DevConfigBufSize)
					return;
			 }
		   }
		   //Check for Alternate interface
		   pIntfctmp = (PUSB_INTERFACE_DESCRIPTOR)tmpConfigBufptr;
		   if(pIntfctmp &&(pIntfctmp->bDescriptorType==USB_INTERFACE_DESCRIPTOR_TYPE))
		   {
			   if((pIntfc->bInterfaceNumber != pIntfctmp->bInterfaceNumber)&& (pIntfctmp->bAlternateSetting ==pIntfc->bAlternateSetting )) 
			   {
					break; // break the do/while loop 
			   }
		   }
	   }while(1);
	   
   }
}
