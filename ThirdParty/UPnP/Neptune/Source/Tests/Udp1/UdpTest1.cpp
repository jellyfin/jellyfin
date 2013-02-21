/*****************************************************************
|
|      Neptune Tests - UDP
|
|      (c) 2001-2002 Gilles Boccon-Gibod
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
|       Receive
+---------------------------------------------------------------------*/
static void
Receive()
{
    printf("==== Receive\n");

    NPT_UdpSocket receiver;
    NPT_DataBuffer buffer(4096);
    buffer.SetDataSize(4096);
    NPT_Result result = receiver.Bind(NPT_SocketAddress(NPT_IpAddress::Any, 9123));
    if (NPT_FAILED(result)) {
        fprintf(stderr, "Bind() failed (%d)\n", result);
        return;
    }
    NPT_SocketAddress address;
    result = receiver.Receive(buffer, &address);
    if (NPT_FAILED(result)) {
        fprintf(stderr, "Receive failed(%d)\n", result);
        return;
    }
    NPT_String addr_string = address.GetIpAddress().ToString();
    printf("received packet, size=%d, from %s:%d\n",
           (int)buffer.GetDataSize(),
           (const char*)addr_string,
           (int)address.GetPort());
}

/*----------------------------------------------------------------------
|       Send
+---------------------------------------------------------------------*/
static void
Send()
{
    printf("==== Send\n");

    NPT_UdpSocket sender;
    NPT_DataBuffer buffer(1024);
    buffer.SetDataSize(1024);
    NPT_IpAddress address;
    address.ResolveName("localhost");
    NPT_SocketAddress socket_address(address, 9123);
    NPT_Result result = sender.Send(buffer, &socket_address);
    if (NPT_FAILED(result)) {
        fprintf(stderr, "Send() failed(%d)\n", result);
        return;
    }
}

/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int argc, char** argv)
{
    if (argc >= 2) {
        if (NPT_StringsEqual(argv[1], "send")) {
            Receive();
        } else {
            Send();
        }
    } else {
    }
    return 0;
}
