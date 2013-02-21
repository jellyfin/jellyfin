/*****************************************************************
|
|      Neptune Utilities - Network Echo Server
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
|       types
+---------------------------------------------------------------------*/
typedef enum {
    SERVER_TYPE_UNKNOWN,
    SERVER_TYPE_UDP,
    SERVER_TYPE_TCP
} ServerType;

/*----------------------------------------------------------------------
|       globals
+---------------------------------------------------------------------*/
static struct {
    bool verbose;
} Options;

/*----------------------------------------------------------------------
|       PrintUsageAndExit
+---------------------------------------------------------------------*/
static void
PrintUsageAndExit(void)
{
    fprintf(stderr, 
            "usage: NetEcho udp|tcp <port>\n");
    exit(1);
}

/*----------------------------------------------------------------------
|       UdpServerLoop
+---------------------------------------------------------------------*/
static void
UdpServerLoop(int port)
{
    NPT_UdpSocket listener;

    // info
    if (Options.verbose) {
        NPT_Debug("listening on port %d\n", port);
    }

    NPT_Result result = listener.Bind(NPT_SocketAddress(NPT_IpAddress::Any, port));
    if (NPT_FAILED(result)) {
        NPT_Debug("ERROR: Bind() failed (%d : %s)\n", result, NPT_ResultText(result));
    }

    // packet loop
    NPT_DataBuffer packet(32768);
    NPT_SocketAddress address;

    do {
        result = listener.Receive(packet, &address);
        if (NPT_SUCCEEDED(result)) {
            if (Options.verbose) {
                NPT_String ip = address.GetIpAddress().ToString();
                NPT_Debug("Received %d bytes from %s:%d\n", packet.GetDataSize(), ip.GetChars(), address.GetPort());
            }

            listener.Send(packet, &address);
        }
    } while (NPT_SUCCEEDED(result));
}

/*----------------------------------------------------------------------
|       TcpServerLoop
+---------------------------------------------------------------------*/
static void
TcpServerLoop(int port)
{
    NPT_TcpServerSocket listener;

    NPT_Result result = listener.Bind(NPT_SocketAddress(NPT_IpAddress::Any, port)); 
    if (NPT_FAILED(result)) {
        NPT_Debug("ERROR: Bind() failed (%d : %s)\n", result, NPT_ResultText(result));
    }
        
    NPT_Socket* client;

    for (;;) {
        NPT_Debug("waiting for client on port %d\n", port);
        result = listener.WaitForNewClient(client);
        NPT_SocketInfo socket_info;
        client->GetInfo(socket_info);
        NPT_Debug("client connected from %s:%d\n",
            socket_info.remote_address.GetIpAddress().ToString().GetChars(),
            socket_info.remote_address.GetPort());
        NPT_InputStreamReference input;
        client->GetInputStream(input);
        NPT_OutputStreamReference output;
        client->GetOutputStream(output);
        do {
            char buffer[1024];
            NPT_Size bytes_read;
            result = input->Read(buffer, sizeof(buffer), &bytes_read);
            if (NPT_SUCCEEDED(result)) {
                NPT_Debug("read %ld bytes\n", bytes_read);
                output->Write(buffer, bytes_read);
            }
        } while (NPT_SUCCEEDED(result));
        delete client;
    }
}

/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int argc, char** argv)
{
    // check command line
    if (argc != 3) {
        PrintUsageAndExit();
    }

    // init options
    Options.verbose = true;
    ServerType server_type = SERVER_TYPE_UNKNOWN;
    int port = -1;

    // parse command line
    if (!strcmp(argv[1], "udp")) {
        server_type = SERVER_TYPE_UDP;
    } else if (!strcmp(argv[1], "tcp")) {
        server_type = SERVER_TYPE_TCP;
    } else {
        fprintf(stderr, "ERROR: unknown server type\n");
        exit(1);
    }

    port = strtoul(argv[2], NULL, 10);

    switch (server_type) {
        case SERVER_TYPE_TCP: TcpServerLoop(port); break;
        case SERVER_TYPE_UDP: UdpServerLoop(port); break;
        default: break;
    }

    return 0;
}
