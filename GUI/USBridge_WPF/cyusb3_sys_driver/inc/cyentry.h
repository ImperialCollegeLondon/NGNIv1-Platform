/*
 ## Cypress CyUSB3 driver header file (cyentry.h)
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
#ifndef _CYENTRY_H
#define _CYENTRY_H

#include <ntddk.h>
#include <wdf.h>
#include "cydevice.h"

DRIVER_INITIALIZE DriverEntry; /* Driver entry point function*/
EVT_WDF_OBJECT_CONTEXT_CLEANUP  CyEvtDriverContextCleanup;  /* driver context cleanup */
  
#endif /*_CYENTRY_H*/