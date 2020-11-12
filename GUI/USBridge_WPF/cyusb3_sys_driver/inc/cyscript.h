/*
 ## Cypress CyUSB3 driver header file (cyscript.h)
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
#ifndef _CYSCRIPT_H_
#define _CYSCRIPT_H_

#define CYSCRIPT_SIGNATURE          'TPSC'


typedef struct _SCRIPT_HEADER
{
    ULONG  Signature;
    ULONG  RecordSize;
    USHORT HeaderSize;

    UCHAR  Tag;

    UCHAR  ConfigNum;
    UCHAR  IntfcNum;
    UCHAR  AltIntfc;

    UCHAR  EndPtAddr;

    UCHAR  ReqType;
    UCHAR  bRequest;
    UCHAR  reserved0;
    USHORT wValue;
    USHORT wIndex;

    ULONG  Timeout;
    ULONG  DataLen;
} SCRIPT_HEADER, *PSCRIPT_HEADER;


NTSTATUS
CyExecuteScriptFile(
    IN WDFDEVICE  Device,
    IN PWSTR ScriptFileName
    );

#endif // _CYSCRIPT_H_
