/*****************************************************************
|
|      Neptune Utilities - Network Configuration Dump
|
|      (c) 2001-2005 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include "NptConfig.h"
#include "Neptune.h"
#include "NptDebug.h"

#if defined(NPT_CONFIG_HAVE_STDLIB_H)
#include <stdlib.h>
#endif

#if defined(NPT_CONFIG_HAVE_STRING_H)
#include <string.h>
#endif

#if defined(NPT_CONFIG_HAVE_STDIO_H)
#include <stdio.h>
#endif


/*----------------------------------------------------------------------
|       globals
+---------------------------------------------------------------------*/

/*----------------------------------------------------------------------
|       PrintUsageAndExit
+---------------------------------------------------------------------*/
static void
PrintUsageAndExit(void)
{
    fprintf(stderr, 
            "usage: NetConfig\n");
    exit(1);
}

/*----------------------------------------------------------------------
|       PrintFlags
+---------------------------------------------------------------------*/
static void
PrintFlags(NPT_Flags flags)
{
    if (flags & NPT_NETWORK_INTERFACE_FLAG_LOOPBACK) {
        printf("LOOPBACK ");
    }
    if (flags & NPT_NETWORK_INTERFACE_FLAG_PROMISCUOUS) {
        printf("PROMISCUOUS ");
    }
    if (flags & NPT_NETWORK_INTERFACE_FLAG_BROADCAST) {
        printf("BROADCAST ");
    }
    if (flags & NPT_NETWORK_INTERFACE_FLAG_MULTICAST) {
        printf("MULTICAST ");
    } 
    if (flags & NPT_NETWORK_INTERFACE_FLAG_POINT_TO_POINT) {
        printf("POINT-TO-POINT ");
    }
}

/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int argc, char**)
{
    // check command line
    if (argc < 1) {
        PrintUsageAndExit();
    }

    NPT_List<NPT_NetworkInterface*> interfaces;
    NPT_Result result = NPT_NetworkInterface::GetNetworkInterfaces(interfaces);
    if (NPT_FAILED(result)) {
        printf("GetNetworkInterfaces() failed\n");
        return 0;
    }
    NPT_List<NPT_NetworkInterface*>::Iterator iface = interfaces.GetFirstItem();
    unsigned int index = 0;
    while (iface) {
        printf("Interface %d: -------------------------------------\n", index);
        printf("  name  = %s\n", (*iface)->GetName().GetChars());
        printf("  flags = %x [ ", (*iface)->GetFlags());
        PrintFlags((*iface)->GetFlags());
        printf("]\n");
        printf("  mac   = %s (type=%d)\n", (*iface)->GetMacAddress().ToString().GetChars(), (*iface)->GetMacAddress().GetType());
        
        // print all addresses
        NPT_List<NPT_NetworkInterfaceAddress>::Iterator nwifaddr = 
            (*iface)->GetAddresses().GetFirstItem();
        unsigned int addr_index = 0;
        while (nwifaddr) {
            printf("  address %d:\n", addr_index);
            printf("    primary address     = ");
            printf("%s\n", nwifaddr->GetPrimaryAddress().ToString().GetChars());
            if ((*iface)->GetFlags() & NPT_NETWORK_INTERFACE_FLAG_BROADCAST) {
                printf("    broadcast address   = ");
                printf("%s\n", nwifaddr->GetBroadcastAddress().ToString().GetChars());
            }
            if ((*iface)->GetFlags() & NPT_NETWORK_INTERFACE_FLAG_POINT_TO_POINT) {
                printf("    destination address = ");
                printf("%s\n", nwifaddr->GetDestinationAddress().ToString().GetChars());
            }
            printf("    netmask             = ");
            printf("%s\n", nwifaddr->GetNetMask().ToString().GetChars());
            ++nwifaddr;
            ++addr_index;
        }
        
        ++iface;
        ++index;
    }
    
    return 0;
}




