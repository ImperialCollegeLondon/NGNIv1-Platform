/*
 ## Cypress CyUSB3 driver header file (cyinterruptep.h)
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
#ifndef _CYINTERRUPTEP_H_
#define _CYINTERRUPTEP_H_

#include "cydevice.h"
#include <wdf.h>

#define MAX_INTERRUPT_REDEAR_EP 10

__drv_requiresIRQL(PASSIVE_LEVEL)
NTSTATUS
 CyCheckAndConfigureInterruptInEp(
 __in PDEVICE_CONTEXT pDevContext
    );

__drv_requiresIRQL(PASSIVE_LEVEL)
NTSTATUS
CyConfigInterruptINepReader(
    __in PDEVICE_CONTEXT pDevContext,
	__in WDFUSBPIPE  IntUsbPipe
    );
VOID
CyCompleteIoctlRequest(
    __in WDFDEVICE WdfDevice
    );

EVT_WDF_USB_READER_COMPLETION_ROUTINE CyEvtInterruptINepReaderComplete;
#endif /*_CYINTERRUPTEP_H_*/