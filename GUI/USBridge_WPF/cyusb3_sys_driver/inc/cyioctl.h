/*
 ## Cypress CyUSB3 driver header file (cyioctl.h)
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
#ifndef _CYIOCTL_H_
#define _CYIOCTL_H_

#include <ntddk.h>
#include <wdf.h>


EVT_WDF_REQUEST_COMPLETION_ROUTINE  EvtControlTransferCompletionRoutine;
EVT_WDF_REQUEST_COMPLETION_ROUTINE  EvtBulkRWCompletionRoutine;
EVT_WDF_REQUEST_COMPLETION_ROUTINE  EvtBulkRWURBCompletionRoutine;
EVT_WDF_REQUEST_COMPLETION_ROUTINE  EvtBulkRWBufferedIOCompletionRoutine;


#define BULK_STAGESIZE 0x400000 // 1Mbyte
#define INTERRUPT_STAGESIZE 0x400000 // 1Mbyte

#define FUNCTION_FROM_CTL_CODE(ctrlCode)     (((ULONG)(ctrlCode & 0x00003FFC)) >> 2)

typedef struct _CY_PIPE_CONTEXT {
  BOOLEAN bVariable; /* dummy variable */
} CY_PIPE_CONTEXT, *PCY_PIPE_CONTEXT;

WDF_DECLARE_CONTEXT_TYPE(CY_PIPE_CONTEXT)


#define IOCTL_ADAPT_INDEX 0x0000

// Get the driver version
#define IOCTL_ADAPT_GET_DRIVER_VERSION         CTL_CODE(FILE_DEVICE_UNKNOWN, IOCTL_ADAPT_INDEX, METHOD_BUFFERED, FILE_ANY_ACCESS)

// Get the current USBDI version
#define IOCTL_ADAPT_GET_USBDI_VERSION         CTL_CODE(FILE_DEVICE_UNKNOWN, IOCTL_ADAPT_INDEX+1, METHOD_BUFFERED, FILE_ANY_ACCESS)

// Get the current device alt interface settings from driver
#define IOCTL_ADAPT_GET_ALT_INTERFACE_SETTING CTL_CODE(FILE_DEVICE_UNKNOWN, IOCTL_ADAPT_INDEX+2, METHOD_BUFFERED, FILE_ANY_ACCESS)

// Set the device interface and alt interface setting
#define IOCTL_ADAPT_SELECT_INTERFACE          CTL_CODE(FILE_DEVICE_UNKNOWN, IOCTL_ADAPT_INDEX+3, METHOD_BUFFERED, FILE_ANY_ACCESS)

// Get device address from driver
#define IOCTL_ADAPT_GET_ADDRESS               CTL_CODE(FILE_DEVICE_UNKNOWN, IOCTL_ADAPT_INDEX+4, METHOD_BUFFERED, FILE_ANY_ACCESS)

// Get number of endpoints for current interface and alt interface setting from driver
#define IOCTL_ADAPT_GET_NUMBER_ENDPOINTS      CTL_CODE(FILE_DEVICE_UNKNOWN, IOCTL_ADAPT_INDEX+5, METHOD_BUFFERED, FILE_ANY_ACCESS)

// Get the current device power state
#define IOCTL_ADAPT_GET_DEVICE_POWER_STATE    CTL_CODE(FILE_DEVICE_UNKNOWN, IOCTL_ADAPT_INDEX+6,   METHOD_BUFFERED, FILE_ANY_ACCESS)

// Set the device power state
#define IOCTL_ADAPT_SET_DEVICE_POWER_STATE    CTL_CODE(FILE_DEVICE_UNKNOWN, IOCTL_ADAPT_INDEX+7,   METHOD_BUFFERED, FILE_ANY_ACCESS)

// Send a raw packet to endpoint 0
#define IOCTL_ADAPT_SEND_EP0_CONTROL_TRANSFER CTL_CODE(FILE_DEVICE_UNKNOWN, IOCTL_ADAPT_INDEX+8, METHOD_BUFFERED, FILE_ANY_ACCESS)

// Send/receive data to/from nonep0
#define IOCTL_ADAPT_SEND_NON_EP0_TRANSFER     CTL_CODE(FILE_DEVICE_UNKNOWN, IOCTL_ADAPT_INDEX+9, METHOD_BUFFERED, FILE_ANY_ACCESS)

// Simulate a disconnect/reconnect
#define IOCTL_ADAPT_CYCLE_PORT                CTL_CODE(FILE_DEVICE_UNKNOWN, IOCTL_ADAPT_INDEX+10, METHOD_BUFFERED, FILE_ANY_ACCESS)

// Reset the pipe
#define IOCTL_ADAPT_RESET_PIPE                CTL_CODE(FILE_DEVICE_UNKNOWN, IOCTL_ADAPT_INDEX+11, METHOD_BUFFERED, FILE_ANY_ACCESS)

// Reset the device
#define IOCTL_ADAPT_RESET_PARENT_PORT         CTL_CODE(FILE_DEVICE_UNKNOWN, IOCTL_ADAPT_INDEX+12, METHOD_BUFFERED, FILE_ANY_ACCESS)

// Get the current transfer size of an endpoint (in number of bytes)
#define IOCTL_ADAPT_GET_TRANSFER_SIZE         CTL_CODE(FILE_DEVICE_UNKNOWN, IOCTL_ADAPT_INDEX+13, METHOD_BUFFERED, FILE_ANY_ACCESS)

// Set the transfer size of an endpoint (in number of bytes)
#define IOCTL_ADAPT_SET_TRANSFER_SIZE         CTL_CODE(FILE_DEVICE_UNKNOWN, IOCTL_ADAPT_INDEX+14, METHOD_BUFFERED, FILE_ANY_ACCESS)

// Return the name of the device
#define IOCTL_ADAPT_GET_DEVICE_NAME           CTL_CODE(FILE_DEVICE_UNKNOWN, IOCTL_ADAPT_INDEX+15, METHOD_BUFFERED, FILE_ANY_ACCESS)

// Return the "Friendly Name" of the device
#define IOCTL_ADAPT_GET_FRIENDLY_NAME         CTL_CODE(FILE_DEVICE_UNKNOWN, IOCTL_ADAPT_INDEX+16, METHOD_BUFFERED, FILE_ANY_ACCESS)

// Abort all outstanding transfers on the pipe
#define IOCTL_ADAPT_ABORT_PIPE                CTL_CODE(FILE_DEVICE_UNKNOWN, IOCTL_ADAPT_INDEX+17, METHOD_BUFFERED, FILE_ANY_ACCESS)

// Send/receive data to/from nonep0 w/ direct buffer acccess (no buffering)
#define IOCTL_ADAPT_SEND_NON_EP0_DIRECT       CTL_CODE(FILE_DEVICE_UNKNOWN, IOCTL_ADAPT_INDEX+18, METHOD_NEITHER, FILE_ANY_ACCESS)

// Return device speed
#define IOCTL_ADAPT_GET_DEVICE_SPEED          CTL_CODE(FILE_DEVICE_UNKNOWN, IOCTL_ADAPT_INDEX+19, METHOD_BUFFERED, FILE_ANY_ACCESS)

// Get the current USB frame number
#define IOCTL_ADAPT_GET_CURRENT_FRAME         CTL_CODE(FILE_DEVICE_UNKNOWN, IOCTL_ADAPT_INDEX+20, METHOD_BUFFERED, FILE_ANY_ACCESS)

#define NUMBER_OF_ADAPT_IOCTLS 21 // Last IOCTL_ADAPT_INDEX + 1

typedef
__drv_functionClass(CY_DEVICE_CONTROL_HANDLER)
__drv_sameIRQL
__drv_maxIRQL(DISPATCH_LEVEL)
VOID
CY_DEVICE_CONTROL_HANDLER(
    __in
    WDFQUEUE Queue,
    __in
    WDFREQUEST Request,
    __in
    size_t OutputBufferLength,
    __in
    size_t InputBufferLength  
    );

typedef CY_DEVICE_CONTROL_HANDLER *PCY_DEVICE_CONTROL_HANDLER;




/*Cy Internal API - IOCTL Handler */
CY_DEVICE_CONTROL_HANDLER CyIoctlHandler_GetCyUSBDriverVersion;
CY_DEVICE_CONTROL_HANDLER CyIoctlHandler_GetUSBDIVersion;
CY_DEVICE_CONTROL_HANDLER CyIoctlHandler_GetAltIntrfSetting;
CY_DEVICE_CONTROL_HANDLER CyIoctlHandler_SetInterface;
CY_DEVICE_CONTROL_HANDLER CyIoctlHandler_GetDeviceAddress;
CY_DEVICE_CONTROL_HANDLER CyIoctlHandler_GetNoOfEndpoint;
CY_DEVICE_CONTROL_HANDLER CyIoctlHandler_GetPowerState;
CY_DEVICE_CONTROL_HANDLER CyIoctlHandler_SetPowerState;
CY_DEVICE_CONTROL_HANDLER CyIoctlHandler_ControlTransfer;
CY_DEVICE_CONTROL_HANDLER CyIoctlHandler_BulkInterruptIsoOperation;
CY_DEVICE_CONTROL_HANDLER CyIoctlHandler_CyclePort;
CY_DEVICE_CONTROL_HANDLER CyIoctlHandler_ResetPipe;
CY_DEVICE_CONTROL_HANDLER CyIoctlHandler_ResetParentPort;
CY_DEVICE_CONTROL_HANDLER CyIoctlHandler_GetTransferSize;
CY_DEVICE_CONTROL_HANDLER CyIoctlHandler_SetTransferSize;
CY_DEVICE_CONTROL_HANDLER CyIoctlHandler_GetDeviceName;
CY_DEVICE_CONTROL_HANDLER CyIoctlHandler_GetFriendlyName;
CY_DEVICE_CONTROL_HANDLER CyIoctlHandler_AbortPipe;
CY_DEVICE_CONTROL_HANDLER CyIoctlHandler_BulkIntIsoDirectTransfer;
CY_DEVICE_CONTROL_HANDLER CyIoctlHandler_GetDeviceSpeed;
CY_DEVICE_CONTROL_HANDLER CyIoctlHandler_GetCurrentFrame;

/* Event call back */
EVT_WDF_IO_IN_CALLER_CONTEXT  CyEvtIoInCallerContextNeither; /* Neither method preprocessing context callback */

#endif /* _CYIOCTL_H_*/