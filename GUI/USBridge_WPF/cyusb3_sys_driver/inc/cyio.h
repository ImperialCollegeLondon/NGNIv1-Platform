/*
 ## Cypress CyUSB3 driver header file (cyio.h)
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
#ifndef _CYIO_H_
#define _CYIO_H_

#include <ntddk.h>
#include <wdf.h>

EVT_WDF_DEVICE_FILE_CREATE          CyEvtDeviceIoCreate; /*File Open */
EVT_WDF_FILE_CLOSE                  CyEvtDeviceIoClose; /*File Close*/
EVT_WDF_IO_QUEUE_IO_READ            CyEvtIoRead; /*File Read*/
EVT_WDF_IO_QUEUE_IO_WRITE           CyEvtIoWrite; /* File Wite */
EVT_WDF_IO_QUEUE_IO_DEVICE_CONTROL  CyEvtIoDeviceControl; /*Device IOCTL handler*/
EVT_WDF_IO_QUEUE_IO_STOP            CyEvtIoStop;
EVT_WDF_IO_QUEUE_IO_RESUME          CyEvtIoResume;

#endif /*_CYIO_H_*/
