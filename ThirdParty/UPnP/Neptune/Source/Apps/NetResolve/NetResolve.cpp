/*****************************************************************
|
|      Neptune Utilities - Network Resolver Example
|
|      (c) 2001-2011 Gilles Boccon-Gibod
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
            "usage: NetResolve <hostname>\n");
    exit(1);
}

/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int argc, char** argv)
{
    // check command line
    if (argc != 2) {
        PrintUsageAndExit();
    }

    NPT_List<NPT_IpAddress> addresses;
    NPT_Result result = NPT_NetworkNameResolver::Resolve(argv[1], addresses);
    if (NPT_FAILED(result)) {
        fprintf(stderr, "ERROR: resolver failed (%d)\n", result);
        return 1;
    }
    
    printf("found %d addresses:\n", addresses.GetItemCount());
    unsigned int i=0;
    for (NPT_List<NPT_IpAddress>::Iterator address = addresses.GetFirstItem();
                                           address;
                                         ++address) {
        NPT_String addr_str = (*address).ToString();
        printf("[%02d] %s\n", i, addr_str.GetChars());
        ++i;
    }
    return 0;
}




