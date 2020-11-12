/*
 ## Cypress CyUSB3 driver header file (cydevice.h)
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
#ifndef _CYDEVICE_H_
#define _CYDEVICE_H_


#include <ntddk.h>
#include <usbdi.h>
#include <usbdlib.h>

#include <wdf.h>
#include <wdfusb.h>
#include "cyioctl.h"
#include "cyusbif.h"


#define CYMEM_TAG 'CYU3'
EVT_WDF_DRIVER_DEVICE_ADD  CyEvtDeviceAdd; /* Add device callback*/


//
// This context is associated with every open handle.
//
typedef struct _FILE_CONTEXT {

    WDFUSBPIPE Pipe;

} FILE_CONTEXT, *PFILE_CONTEXT;

WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(FILE_CONTEXT,CyGetFileContext)



typedef struct _CYUSB_MULTIPLE_INTERFACE
{	 
	 UCHAR   ucAciveNumConfiguredPipes;	 
}CYUSB_MULTIPLE_INTERFACE,*PCYUSB_MULTIPLE_INTERFACE;
typedef struct _SS_CUSTOM_EP
{
	UCHAR  bEndpointAddress; 
	UCHAR  bMaxBurst;
    UCHAR  bmAttributes;        
    USHORT bBytesPerInterval; 
	USHORT wMaxPacketSize;
}SS_CUSTOM_EP,*PSS_CUSTOM_EP;
typedef struct _SS_CUSTOMER_INFC
{
	SS_CUSTOM_EP SS_CustomerEp[MAX_USB_ENDPOINT];
}SS_CUSTOMER_INFC,*PSS_CUSTOMER_INFC;

typedef enum _CYWDF_POWERSTATE{WDFD0_ENTRY=0,WDFD0_ENTRY1,WDFD0_ENTRY2, WDFD0_EXIT}CYWDF_POWERSTATE;
/* Device context store device configuration and current state*/
typedef struct _DEVICE_CONTEXT {

	WDFUSBDEVICE                    CyUsbDevice;    /* Handle of a usb device and used for all lower usb stack communication */

    USB_DEVICE_DESCRIPTOR           UsbDeviceDescriptor;

    PUSB_CONFIGURATION_DESCRIPTOR   UsbConfigurationDescriptor;   	

	ULONG							ulUSBDeviceTrait;  /* A set of bit flags that identify device traits :Remote Wakeup, Self Powered/BusPowered and Device Speed  */

    ULONG                           ulWaitWakeEnable;    

	ULONG							ulUSBDIVersion;

	BOOLEAN							bIsMultiplInterface;

	UCHAR                           ucNumberOfInterface; /* Number of interface */

    UCHAR                           ucNumberOfInterfaceCompositUSB30Only; /* Number of interface - This will tell number of interface as per device configuration if device is composite */

    USHORT                          ucDeviceInstaceNumber;

	UCHAR							ucActiveConfigNum; /*Store active configuration number */

	UCHAR                           ucNumAltSettigns; /*Number of alternate settings */

	UCHAR							ucActiveAltSettings; /* Active alternate settings */

	UCHAR							ucActiveNumOfPipe; /* Active number of pipe in current alternate setting*/

	UCHAR                           ucActiveInterruptInPipe; /* Active Intrrupt IN Endpoint */

	UCHAR                           ucIntInData; /* store interrupt in data*/

	WDFQUEUE                        IntInMsgQ; /* intrrupt in message queue for continous reader */

	CYWDF_POWERSTATE				WdfPowerState;

	WDFUSBPIPE						WdfUsbInterruptInPipeArray[MAX_USB_ENDPOINT]; /* Array of active USB Interrupt IN Pipe handle in current selected alternate interface settings */

	WDFUSBPIPE						WdfUsbPipeArray[MAX_USB_ENDPOINT]; /* Array of active all transfer USB Pipe handle in current selected alternate interface settings */

	WDF_USB_DEVICE_SELECT_CONFIG_PARAMS UsbInterfaceConfig; /* Interface configuration */

	PWDF_USB_INTERFACE_SETTING_PAIR     MultipleInterfacePair; /* multiple interface pair */

	WDFREQUEST   WdfSubRequest; // This request object will be used to send sub request when the transfer size more than the kernal page memory.
	
	PCY_DEVICE_CONTROL_HANDLER      CyDispatchIoctl[NUMBER_OF_ADAPT_IOCTLS]; /* Array of IOCTL handler function pointer */

	WDFQUEUE                        hQueue;

	SS_CUSTOMER_INFC                SS_Custom_Infc[MAX_USB_INTERFACE][MAX_USB_ALTERNATESETTIGNS]; // Store only required USB3.0 device configuration.



} DEVICE_CONTEXT, *PDEVICE_CONTEXT;

typedef struct _REQUEST_CONTEXT {

    WDFMEMORY         UrbMemory;
    PMDL              Mdl;
    ULONG             Length;         // remaining to xfer
    ULONG             Numxfer;        // cumulate xfer
    ULONG_PTR         VirtualAddress; // va for next segment of xfer.
    WDFCOLLECTION     SubRequestCollection; // used for doing Isoch
    WDFSPINLOCK       SubRequestCollectionLock; // used to sync access to collection at DISPATCH_LEVEL
    BOOLEAN           Read; // TRUE if Read
	WDFMEMORY InputMemoryBufferWrite;  // Valid for Neither IO buffer request
	WDFMEMORY OutputMemoryBufferWrite; // Valid for Neither IO buffer request
	WDFMEMORY InputMemoryBufferRead;  // Valid for Neither IO buffer request
	WDFMEMORY OutputMemoryBufferRead; // Valid for Neither IO buffer request
	ULONG     ulszInputMemoryBuffer; // input buffer size
	ULONG     ulszOutputMemoryBuffer; // output buffer size
	BOOLEAN   IsNeitherIO; // Buffering method used for the Request 
	ULONG     ulLastIsoPktIndex; //ISOCHRONOUS SPECIFC - Store the last iso packet index in iso completion routine
	ULONG	  ulNoOfIsoUserRequestPkt; //ISOCHRONOUS SPECIFC - It will check wether user provided buffer is sufficient to hold all packets, this is to avaoid crash.
	PMDL      pSubMdl;
	//
	//Adding paramter to send sub request to cary out large transfer.	
	ULONG ulNumberOfSubRequest; // This will store the number of sub request required to finish the main request.
	ULONG ulRemainingSubRequest; // This will store remaining subrequests for the transfer.
	ULONG ulMemBufferOffset;  // This will store buffer offset
	ULONG ulLastRequestSize;
	ULONG ulBytesReadWrite; //This will store total number of bytes read/write for the request
	WDFUSBPIPE UsbPipeHandle;
	PDEVICE_CONTEXT  pDevContext;
	PVOID pMainBufPtr;
	WDFMEMORY WdfMemoryBufferIo; // This will be used by the buffered IO ioctl only.
	//
} REQUEST_CONTEXT, * PREQUEST_CONTEXT;

WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(REQUEST_CONTEXT,CyGetRequestContext)

typedef struct _SUB_REQUEST_CONTEXT {

    WDFREQUEST  UserRequest;
    PURB        SubUrb;
    PMDL        SubMdl;
    LIST_ENTRY  ListEntry; // used in CancelRoutine

} SUB_REQUEST_CONTEXT, *PSUB_REQUEST_CONTEXT ;
WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(SUB_REQUEST_CONTEXT, CyGetSubRequestContext)

WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(DEVICE_CONTEXT,CyGetDeviceContext)

/* Cy internal API */
__drv_requiresIRQL(PASSIVE_LEVEL)
void 
CyInitIoctlDispatcher(
    __in WDFDEVICE Device
    );

BOOLEAN GetRegistryKey(IN WDFDEVICE wdfDevice,					   
					   IN PWSTR SubkeyName OPTIONAL,
					   IN ULONG RegType,
					   IN PWSTR ParameterName,
					   IN OUT PVOID ParameterValue,
					   IN OUT PULONG ParameterValuelen);

#endif /*_CYDEVICE_H_*/