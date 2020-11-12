/*
 ## Cypress CyUSB3 driver header file (cyguid.h)
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
#ifndef _CYGUID_H_
#define _CYGUID_H_

 #include <initguid.h>

// {AE18AA60-7F6A-11d4-97DD-00010229B959}
//DEFINE_GUID(DEFAULT_WDF_CYUSB_GUID, 
//0xae18aa60, 0x7f6a, 0x11d4, 0x97, 0xdd, 0x0, 0x1, 0x2, 0x29, 0xb9, 0x59);

GUID    DEFAULT_WDF_CYUSB_GUID_APP_INTERFACE = { 0xae18aa60, 0x7f6a, 0x11d4, { 0x97, 0xdd, 0x0, 0x1, 0x2, 0x29, 0xb9, 0x59 } };
#define GUID_BUFFER_SIZE (sizeof(WCHAR)*256)
#define CYREG_GUID_PARAMETER_NAME L"DriverGUID"

#endif /*_CYGUID_H_*/