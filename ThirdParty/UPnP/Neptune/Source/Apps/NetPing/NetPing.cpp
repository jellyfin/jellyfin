/*****************************************************************
|
|      Neptune Utilities - Network Ping Client
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
    CLIENT_TYPE_UNKNOWN,
    CLIENT_TYPE_UDP,
    CLIENT_TYPE_TCP
} ClientType;

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
            "usage: NetPing udp|tcp <hostname> <port>\n");
    exit(1);
}

/*----------------------------------------------------------------------
|       UdpPing
+---------------------------------------------------------------------*/
static void
UdpPing(const char* hostname, int port)
{
    NPT_UdpSocket sender;
    NPT_Result    result;

    // build ping packet
    NPT_DataBuffer packet;
    const char* packet_data = "PING";
    packet.SetData((NPT_Byte*)packet_data, sizeof(packet_data));

    // resolve hostname
    NPT_IpAddress ip_address;
    result = ip_address.ResolveName(hostname);
    if (NPT_FAILED(result)) {
        fprintf(stderr, "ERROR: failed to resolve name\n");
        return;
    }

    // get the current timestamp
    NPT_TimeStamp before;
    NPT_System::GetCurrentTimeStamp(before);

    // send the packet
    sender.Connect(NPT_SocketAddress(ip_address, port));
    sender.Send(packet);
    //NPT_SocketAddress destination_address(ip_address, port);
    //sender.Send(packet, &destination_address);
    NPT_SocketInfo socket_info;
    sender.GetInfo(socket_info);
    NPT_Debug("sent from %s:%d to %s:%d\n", 
        socket_info.local_address.GetIpAddress().ToString().GetChars(),
        socket_info.local_address.GetPort(),
        socket_info.remote_address.GetIpAddress().ToString().GetChars(),
        socket_info.remote_address.GetPort());
    //sender.Connect(NPT_SocketAddress(ip_address, port));
    NPT_SocketAddress destination_address(ip_address, port);
    sender.Send(packet, &destination_address);
    //NPT_SocketInfo socket_info;
    //sender.GetInfo(socket_info);
    //NPT_Debug("sent from %s:%d to %s:%d\n", 
    //    socket_info.local_address.GetIpAddress().ToString().GetChars(),
    //    socket_info.local_address.GetPort(),
    //    socket_info.remote_address.GetIpAddress().ToString().GetChars(),
    //    socket_info.remote_address.GetPort());
    NPT_Debug("send %d bytes\n", 4);

    // receive response
    NPT_DataBuffer response(32768);
    result = sender.Receive(response);
    if (NPT_FAILED(result)) {
        fprintf(stderr, "ERROR: failed to receive response (%d)\n", result);
        return;
    }
    sender.GetInfo(socket_info);
    printf("RESPONSE: %d bytes from %s:%d\n", 
           response.GetDataSize(),
           socket_info.remote_address.GetIpAddress().ToString().GetChars(),
           socket_info.remote_address.GetPort());

    NPT_TimeStamp after;
    NPT_System::GetCurrentTimeStamp(after);
    NPT_TimeInterval i = after-before;
    printf("RTT: %f ms\n", ((float)i)*1000.0f);
}

/*----------------------------------------------------------------------
|       TcpPing
+---------------------------------------------------------------------*/
static void
TcpPing(const char* hostname, int port)
{
    NPT_TcpClientSocket sender;
    NPT_Result          result;

    // resolve name
    NPT_IpAddress ip_address;
    result = ip_address.ResolveName(hostname);
    if (NPT_FAILED(result)) {
        fprintf(stderr, "ERROR: failed to resolve name\n");
        return;
    }
    
    // connect to server
    sender.Connect(NPT_SocketAddress(ip_address, port));
    NPT_OutputStreamReference output;

    // get the current timestamp
    NPT_TimeStamp before;
    NPT_System::GetCurrentTimeStamp(before);

    // send the ping
    sender.GetOutputStream(output);
    output->Write("PING", 4);

    // receive response
    NPT_InputStreamReference input;
    sender.GetInputStream(input);
    char buffer[1024];

    NPT_Size bytes_read;
    result = input->Read(buffer, sizeof(buffer), &bytes_read);
    if (NPT_SUCCEEDED(result)) {
       NPT_Debug("read %ld bytes\n", bytes_read);
    }

    NPT_TimeStamp after;
    NPT_System::GetCurrentTimeStamp(after);
    NPT_TimeInterval i = after-before;
    printf("RTT: %f ms\n", ((float)i)*1000.0f);
}

/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int argc, char** argv)
{
    // check command line
    if (argc != 4) {
        PrintUsageAndExit();
    }

    // init options
    Options.verbose = false;
    ClientType client_type = CLIENT_TYPE_UNKNOWN;
    int port = -1;
    char* hostname = NULL;

    // parse command line
    if (!strcmp(argv[1], "udp")) {
        client_type = CLIENT_TYPE_UDP;
    } else if (!strcmp(argv[1], "tcp")) {
        client_type = CLIENT_TYPE_TCP;
    } else {
        fprintf(stderr, "ERROR: unknown client type\n");
        exit(1);
    }

    hostname = argv[2];
    port = strtoul(argv[3], NULL, 10);

    switch (client_type) {
        case CLIENT_TYPE_TCP: TcpPing(hostname, port); break;
        case CLIENT_TYPE_UDP: UdpPing(hostname, port); break;
        default: break;
    }

    return 0;
}




