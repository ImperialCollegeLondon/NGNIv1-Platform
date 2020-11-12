/*
 ## Cypress CyUSB3 driver source file (cyentry.c)
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
#include "..\inc\cyentry.h"
#include "..\inc\cytrace.h"
#if defined(EVENT_TRACING)
#include "cyentry.tmh"
#endif

#if DBG
#include <stdarg.h>
#include "Ntstrsafe.h"
#endif

#ifdef ALLOC_PRAGMA
#pragma alloc_text(INIT, DriverEntry)
#endif


__drv_requiresIRQL(PASSIVE_LEVEL)
NTSTATUS
DriverEntry(
    __in PDRIVER_OBJECT  DriverObject,
    __in PUNICODE_STRING RegistryPath
    )
/*++

Routine Description:
    DriverEntry initializes the driver and is the first routine called by the
    system after the driver is loaded.

Parameters Description:

    DriverObject - represents the instance of the function driver that is loaded
    into memory. DriverEntry must initialize members of DriverObject before it
    returns to the caller. DriverObject is allocated by the system before the
    driver is loaded, and it is released by the system after the system unloads
    the function driver from memory.

    RegistryPath - represents the driver specific path in the Registry.
    The function driver can use the path to store driver related data between
    reboots. The path does not store hardware instance specific data.

Return Value:

    STATUS_SUCCESS if successful,
    STATUS_UNSUCCESSFUL otherwise.

--*/
{
    WDF_DRIVER_CONFIG       config;
    NTSTATUS                status;
    WDF_OBJECT_ATTRIBUTES   attributes;

	CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_INIT, "Start DriverEntry\n");
    //
    // Initialize WPP Tracing
    WPP_INIT_TRACING( DriverObject, RegistryPath );
	
    WDF_DRIVER_CONFIG_INIT(
        &config,
        CyEvtDeviceAdd
        );

    //
    // Register a cleanup callback so that we can call WPP_CLEANUP when
    // the framework driver object is deleted during driver unload.
    //
    WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
    attributes.EvtCleanupCallback = CyEvtDriverContextCleanup;

    //
    // Create a framework driver object to represent our driver.
    //
    status = WdfDriverCreate(
        DriverObject,
        RegistryPath,
        &attributes, // Driver Object Attributes
        &config,          // Driver Config Info
        WDF_NO_HANDLE // hDriver
        );

    if (!NT_SUCCESS(status)) {

		CyTraceEvents(TRACE_LEVEL_ERROR, DBG_INIT, "WdfDriverCreate failed :%x\n",status);
        //
        // Cleanup tracing here because DriverContextCleanup will not be called
        // as we have failed to create WDFDRIVER object itself.
        // Please note that if your return failure from DriverEntry after the
        // WDFDRIVER object is created successfully, you don't have to
        // call WPP cleanup because in those cases DriverContextCleanup
        // will be executed when the framework deletes the DriverObject.
        
		
        WPP_CLEANUP(DriverObject);
		//Windows 2000 support : Get pointer to device object
		//WPP_CLEANUP(DriverObject->DeviceObject);

    }

	CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_INIT, "End DriverEntry\n");
    return status;
}
VOID
CyEvtDriverContextCleanup(
    WDFDRIVER Driver
    )
/*++
Routine Description:

    Free resources allocated in DriverEntry that are automatically
    cleaned up framework.

Arguments:

    Driver - handle to a WDF Driver object.

Return Value:

    VOID.

--*/
{
	CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_INIT, "Start CyEvtDriverContextCleanup\n");

    //PAGED_CODE ();

    UNREFERENCED_PARAMETER(Driver);


    WPP_CLEANUP( WdfDriverWdmGetDriverObject( Driver ));
    //Windows 2000 support : Get pointer to device object
    //WPP_CLEANUP(DriverObject->DeviceObject);

	//CyTraceEvents(TRACE_LEVEL_VERBOSE, DBG_INIT, "End CyEvtDriverContextCleanup\n");
}
#if !defined(EVENT_TRACING)

VOID
CyTraceEvents (
    __in ULONG DebugPrintLevel,
    __in ULONG DebugPrintFlag,
    __drv_formatString(printf)
    __in PCSTR DebugMessage,
    ...
    )

/*++

Routine Description:

    Debug print for the sample driver.

Arguments:

    TraceEventsLevel - print level between 0 and 3, with 3 the most verbose

Return Value:

    None.

 --*/
 {
#if DBG_CHK
#define     TEMP_BUFFER_SIZE        1024
    va_list    list;
    CHAR       debugMessageBuffer[TEMP_BUFFER_SIZE];
    NTSTATUS   status;

    va_start(list, DebugMessage);

    if (DebugMessage) {

        //
        // Using new safe string functions instead of _vsnprintf.
        // This function takes care of NULL terminating if the message
        // is longer than the buffer.
        //
        status = RtlStringCbVPrintfA( debugMessageBuffer,
                                      sizeof(debugMessageBuffer),
                                      DebugMessage,
                                      list );
        if(!NT_SUCCESS(status)) {

            DbgPrint (_DRIVER_NAME_": RtlStringCbVPrintfA failed 0x%x\n", status);
            return;
        }
        if (DebugPrintLevel <= TRACE_LEVEL_ERROR ||
            (DebugPrintLevel <= DebugLevel &&
             ((DebugPrintFlag & DebugFlag) == DebugPrintFlag))) {
            DbgPrint("%s %s", _DRIVER_NAME_, debugMessageBuffer);
        }
    }
    va_end(list);

    return;
#else
	/* Disabling the logging for the free build of driver
    UNREFERENCED_PARAMETER(TraceEventsLevel);
    UNREFERENCED_PARAMETER(TraceEventsFlag);
    UNREFERENCED_PARAMETER(DebugMessage);
	*/
#endif
}

#endif
