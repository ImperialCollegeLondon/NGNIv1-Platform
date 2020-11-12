/*
 ## Cypress CyUSB3 driver header file (cypnppower.h)
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
#ifndef _CYPNP_H_
#define  _CYPNP_H_


#include <ntddk.h>
#include <wdf.h>
#include <usbdi.h>
#include <usbdlib.h>
#include <wdfusb.h>
#include "cydevice.h"


/*wdf framwork event callback */
EVT_WDF_DEVICE_PREPARE_HARDWARE CyEvtDevicePrepareHardware;
EVT_WDF_DEVICE_RELEASE_HARDWARE CyEvtDeviceReleaseHardware;
EVT_WDF_DEVICE_D0_ENTRY CyEvtDeviceD0Entry;
EVT_WDF_DEVICE_D0_EXIT CyEvtDeviceD0Exit;
/*cy driver defined internal API */
__drv_requiresIRQL(PASSIVE_LEVEL)
NTSTATUS
CySelectInterfaces(
    __in WDFDEVICE Device
    );
__drv_requiresIRQL(PASSIVE_LEVEL)
PCHAR
GetDevicePowerString(
    __in WDF_POWER_DEVICE_STATE PState
    );
__drv_requiresIRQL(PASSIVE_LEVEL)
NTSTATUS
CySetPowerPolicy(
    __in WDFDEVICE Device
    );
__drv_requiresIRQL(PASSIVE_LEVEL)
VOID 
InitInterfacePair(__in WDFUSBDEVICE UsbDevice,
				  __in PWDF_USB_INTERFACE_SETTING_PAIR pUsbInterfacePair,
				  __in UCHAR ucNumberOfInterface);
__drv_requiresIRQL(PASSIVE_LEVEL)
VOID CyGetActiveAltInterfaceConfig(__in PDEVICE_CONTEXT pDevContext);
__drv_requiresIRQL(PASSIVE_LEVEL)
WDF_USB_PIPE_TYPE CyFindUsbPipeType(__in UCHAR ucEndpointAddress,__in PDEVICE_CONTEXT pDevContext,__out WDFUSBPIPE* UsbPipeHandle);
#endif /* _CYPNP_H */