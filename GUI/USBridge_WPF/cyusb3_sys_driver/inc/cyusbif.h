/*
 ## Cypress CyUSB3 driver header file (cyusbif.h)
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
#ifndef  _CYUSBIF_H_
#define  _CYUSBIF_H_

#define MAX_USB_ENDPOINT 32 // Maximum endpoint USB device can support
#define MAX_USB_INTERFACE 16 // Maximum USB Interface
#define MAX_USB_ALTERNATESETTIGNS 32 // Maximum USB Alternate settings for Interaface

#define BCDUSBJJMASK        0xFF00//(0xJJMN JJ - Major version,M Minor version, N sub-minor vesion)
#define BCDUSB30MAJORVER    0x0300 
#define BCDUSB20MAJORVER    0x0200


#define DIR_HOST_TO_DEVICE 0
#define DIR_DEVICE_TO_HOST 1

#define RECIPIENT_DEVICE    0x00
#define RECIPIENT_INTERFACE 0x01
#define RECIPIENT_ENDPOINT  0x02
#define RECIPIENT_OTHER     0x03

#define FEATURE_REMOTE_WAKEUP 0x01
#define FEATURE_TEST_MODE     0x02

#define REMOTE_WAKEUP_MASK 0x20

#define TYPE_STANDARD 0
#define TYPE_CLASS    1
#define TYPE_VENDOR   2
#define TYPE_RESERVED 3

#define DEVICE_SPEED_UNKNOWN        0x00000000
#define DEVICE_SPEED_LOW_FULL       0x00000001
#define DEVICE_SPEED_HIGH           0x00000002
#define DEVICE_SPEED_SUPER			0x00000004	

#define USB_ISO_ID                  0x4945
#define USB_ISO_CMD_ASAP            0x8000
#define USB_ISO_CMD_CURRENT_FRAME   0x8001
#define USB_ISO_CMD_SET_FRAME       0x8002

#define LANG_ID 0x0409

#include <PSHPACK1.H>
//TODO : Please remove it once you finalize Get/Set transfer size ioctl.
typedef struct _SET_TRANSFER_SIZE_INFO {
    UCHAR EndpointAddress;
    ULONG TransferSize;
} SET_TRANSFER_SIZE_INFO, *PSET_TRANSFER_SIZE_INFO;

typedef struct _WORD_SPLIT {
    UCHAR lowByte;
    UCHAR hiByte;
} WORD_SPLIT, *PWORD_SPLIT;

typedef struct _BM_REQ_TYPE {
    UCHAR   Recipient:2;
    UCHAR   Reserved:3;
    UCHAR   Type:2;
    UCHAR   Direction:1;
} BM_REQ_TYPE, *PBM_REQ_TYPE;

typedef struct _SETUP_PACKET {
    
    union {
        BM_REQ_TYPE bmReqType;
        UCHAR bmRequest;
    };

    UCHAR bRequest;
    
    union {
        WORD_SPLIT wVal;
        USHORT wValue;
    };
    
    union {
        WORD_SPLIT wIndx;
        USHORT wIndex;
    };
    
    union {
        WORD_SPLIT wLen;
        USHORT wLength;
    };

    ULONG ulTimeOut;

} SETUP_PACKET, *PSETUP_PACKET;

typedef struct _ISO_ADV_PARAMS {
    
    USHORT isoId;
    USHORT isoCmd;

    ULONG ulParam1;
    ULONG ulParam2;

} ISO_ADV_PARAMS, *PISO_ADV_PARAMS;

typedef struct _ISO_PACKET_INFO {
    ULONG Status;
    ULONG Length;
} ISO_PACKET_INFO, *PISO_PACKET_INFO;

typedef struct _SINGLE_TRANSFER {
    union {
        SETUP_PACKET SetupPacket;
        ISO_ADV_PARAMS IsoParams;
    };

    UCHAR reserved;
    UCHAR ucEndpointAddress;
    ULONG NtStatus;
    ULONG UsbdStatus;
    ULONG IsoPacketOffset;
    ULONG IsoPacketLength;
    ULONG BufferOffset;
    ULONG BufferLength;
} SINGLE_TRANSFER, *PSINGLE_TRANSFER;
#include <POPPACK.H>

#endif /*_CYUSBIF_H_*/