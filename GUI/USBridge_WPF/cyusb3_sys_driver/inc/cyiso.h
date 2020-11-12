/*
 ## Cypress CyUSB3 driver header file (cyiso.h)
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
#ifndef _CYISO_H_
#define _CYISO_H_

#include "..\inc\cydevice.h"
#include "..\inc\cyusbif.h"

NTSTATUS CyIsoReadWrite(WDFREQUEST Request,PDEVICE_CONTEXT pDevContext,WDFUSBPIPE UsbPipeHandle,BOOLEAN bIsDirectMethod);
EVT_WDF_REQUEST_CANCEL CyEvtRequestCancel;
EVT_WDF_REQUEST_COMPLETION_ROUTINE CySubRequestCompletionRoutine;

#endif /*_CYISO_H_*/