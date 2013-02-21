/*****************************************************************
|
|      Neptune Utilities - Network Data Pump
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
|       types
+---------------------------------------------------------------------*/
typedef enum {
    ENDPOINT_TYPE_UDP_CLIENT,
    ENDPOINT_TYPE_UDP_SERVER,
    ENDPOINT_TYPE_TCP_CLIENT,
    ENDPOINT_TYPE_TCP_SERVER,
    ENDPOINT_TYPE_MULTICAST_CLIENT,
    ENDPOINT_TYPE_MULTICAST_SERVER,
    ENDPOINT_TYPE_FILE,
    ENDPOINT_TYPE_SERIAL_PORT
} EndPointType;

typedef enum {
    ENDPOINT_DIRECTION_IN,
    ENDPOINT_DIRECTION_OUT
} EndPointDirection;

typedef struct {
    EndPointType      type;
    EndPointDirection direction;
    union {
        struct {
            int  port;
            bool reuse_addr;
        } udp_server;
        struct {
            char* hostname;
            int   port;
        } udp_client;
        struct {
            int  port;
            bool reuse_addr;
        } tcp_server;
        struct {
            char* hostname;
            int   port;
        } tcp_client;
        struct {
            char* groupname;
            int   port;
            bool  reuse_addr;
        } multicast_server;
        struct {
            char* groupname;
            int   port;
            int   ttl;
        } multicast_client;
        struct {
            char* name;
        }         file;
        struct {
            char*        name;
            unsigned int speed;
        }         serial_port;
    }            info;
} EndPoint;

/*----------------------------------------------------------------------
|       constants
+---------------------------------------------------------------------*/
#define PUMP_DEFAULT_PACKET_SIZE 16384
#define PUMP_MAX_UNDERFLOW       5000
#define PUMP_MIN_SLEEP           10

/*----------------------------------------------------------------------
|       globals
+---------------------------------------------------------------------*/
static struct {
    bool verbose;
    bool show_progress;
} Options;

/*----------------------------------------------------------------------
|       PrintUsageAndExit
+---------------------------------------------------------------------*/
static void
PrintUsageAndExit(void)
{
    fprintf(stderr, 
            "usage: NetPump [options] <input endpoint> <output endpoint>, \n"
            "where each endpoint is:\n"
            "    udp [client <hostname> <port>]|[server <port> [-r]]\n"
            "  or\n"
            "    tcp [client <hostname> <port>]|[server <port> [-r]]\n"
            "  or\n"
            "    multicast [client <groupname> <port> <ttl>]|[server <groupname> <port> [-r]]\n"
            "  or\n"
            "    file [<filename>|" NPT_FILE_STANDARD_INPUT "|" NPT_FILE_STANDARD_OUTPUT "|" NPT_FILE_STANDARD_ERROR "\n"
            "  or\n"
            "    serial <portname> <speed>\n"
            "\n"
            "options are:\n"
            "  -r: when used for udp, tcp or multicast server endpoints, do not set reuse_address\n"
            "  --verbose: show more info\n"
            "  --show-progress: show progress\n"
            "  --packet-size <packet_size>: send by burst of <packet_size> (default: %d)"
            "\n", PUMP_DEFAULT_PACKET_SIZE);
    exit(1);
}

/*----------------------------------------------------------------------
|       GetEndPointStreams
+---------------------------------------------------------------------*/
static NPT_Result
GetEndPointStreams(EndPoint*                  endpoint, 
                   NPT_InputStreamReference*  input_stream,
                   NPT_OutputStreamReference* output_stream)
{
    // default return values
    if (input_stream) *input_stream = NULL;
    if (output_stream) *output_stream = NULL;

    switch (endpoint->type) {
      case ENDPOINT_TYPE_MULTICAST_SERVER:
      case ENDPOINT_TYPE_UDP_SERVER: 
        break; // not used with streams
        
      case ENDPOINT_TYPE_UDP_CLIENT:
        {
            NPT_UdpSocket sender;

            // info
            if (Options.verbose) {
                printf("sending to %s on port %d\n",
                        endpoint->info.udp_client.hostname,
                        endpoint->info.udp_client.port);
            }

            // resolve name
            NPT_IpAddress address;
            NPT_CHECK(address.ResolveName(endpoint->info.udp_client.hostname));

            // connect socket
            NPT_CHECK(sender.Connect(NPT_SocketAddress(address,
                                                       endpoint->info.udp_client.port)));

            // get the streams
            if (input_stream) {
                NPT_CHECK(sender.GetInputStream(*input_stream));
            }
            if (output_stream) {
                NPT_CHECK(sender.GetOutputStream(*output_stream));
            }
                        
            return NPT_SUCCESS;
        }
        break;

      case ENDPOINT_TYPE_TCP_CLIENT:
        {
            NPT_TcpClientSocket client;

            // info
            if (Options.verbose) {
                printf("connecting to %s on port %d\n", 
                        endpoint->info.tcp_client.hostname,
                        endpoint->info.tcp_client.port);
            }

            // resolve the name
            NPT_IpAddress address;
            NPT_CHECK(address.ResolveName(endpoint->info.tcp_client.hostname));

            // connect
            NPT_CHECK(client.Connect(NPT_SocketAddress(address,
                                                       endpoint->info.tcp_client.port)));

            // info
            if (Options.verbose) {
                printf("connected\n"); 
            }

            // get the streams
            if (input_stream) {
                NPT_CHECK(client.GetInputStream(*input_stream));
            }
            if (output_stream) {
                NPT_CHECK(client.GetOutputStream(*output_stream));
            }
            
            return NPT_SUCCESS;
        }
        break;

      case ENDPOINT_TYPE_MULTICAST_CLIENT:
        {
            NPT_UdpMulticastSocket sender;

            // info
            if (Options.verbose) {
                printf("sending to %s on port %d\n",
                       endpoint->info.multicast_client.groupname,
                       endpoint->info.multicast_client.port);
            }

            // set time to live
            NPT_CHECK(sender.SetTimeToLive(endpoint->info.multicast_client.ttl));

            // resolve name
            NPT_IpAddress address;
            NPT_CHECK(address.ResolveName(endpoint->info.multicast_client.groupname));

            // connect socket
            NPT_CHECK(sender.Connect(NPT_SocketAddress(address, endpoint->info.multicast_client.port)));

            // get the streams
            if (input_stream) {
                NPT_CHECK(sender.GetInputStream(*input_stream));
            }
            if (output_stream) {
                NPT_CHECK(sender.GetOutputStream(*output_stream));
            }

            return NPT_SUCCESS;
        }
        break;

      case ENDPOINT_TYPE_TCP_SERVER:
        {
            NPT_TcpServerSocket server;
            NPT_Socket*         client;

            // info
            if (Options.verbose) {
                printf("waiting for client on port %d %s\n", 
                        endpoint->info.tcp_server.port,
                        endpoint->info.tcp_server.reuse_addr?"":"(reuse_addr=false)");
            }

            // bind to the address
            NPT_CHECK(server.Bind(NPT_SocketAddress(NPT_IpAddress::Any, endpoint->info.tcp_server.port),
                                  endpoint->info.tcp_server.reuse_addr));

            // wait for connection
            NPT_CHECK(server.WaitForNewClient(client));

            // info
            if (Options.verbose) {
                printf("client connected\n");
            }

            // get the streams
            if (input_stream) {
                NPT_CHECK(client->GetInputStream(*input_stream));
            }
            if (output_stream) {
                NPT_CHECK(client->GetOutputStream(*output_stream));
            }

            delete client;
            return NPT_SUCCESS;
        }
        break;

      case ENDPOINT_TYPE_FILE:
        {
            // create a file object
            NPT_File file(endpoint->info.file.name);
            if (endpoint->direction == ENDPOINT_DIRECTION_IN) {
                NPT_CHECK(file.Open(NPT_FILE_OPEN_MODE_READ | 
                                    NPT_FILE_OPEN_MODE_UNBUFFERED));
            } else {
                NPT_CHECK(file.Open(NPT_FILE_OPEN_MODE_WRITE | 
                                    NPT_FILE_OPEN_MODE_CREATE|
                                    NPT_FILE_OPEN_MODE_UNBUFFERED));
            }

            // get the streams
            if (input_stream) {
                NPT_CHECK(file.GetInputStream(*input_stream));
            }
            if (output_stream) {
                NPT_CHECK(file.GetOutputStream(*output_stream));
            }

            return NPT_SUCCESS;
        }
        break;

      case ENDPOINT_TYPE_SERIAL_PORT:
        {
            // create a serial port object
            NPT_SerialPort serial_port(endpoint->info.serial_port.name);
            NPT_CHECK(serial_port.Open(endpoint->info.serial_port.speed));

            // get the streams
            if (input_stream) {
                NPT_CHECK(serial_port.GetInputStream(*input_stream));
            }
            if (output_stream) {
                NPT_CHECK(serial_port.GetOutputStream(*output_stream));
            }

            return NPT_SUCCESS;
        }
        break;
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|       GetEndPointUdpSocket
+---------------------------------------------------------------------*/
static NPT_Result
GetEndPointUdpSocket(EndPoint* endpoint, NPT_UdpSocket*& udp_socket)
{
    // default return values
    udp_socket = NULL;

    switch (endpoint->type) {
      case ENDPOINT_TYPE_UDP_SERVER:
        {
            udp_socket = new NPT_UdpSocket();

            // info
            if (Options.verbose) {
                printf("listening on port %d %s\n", 
                       endpoint->info.udp_server.port,
                       endpoint->info.udp_server.reuse_addr?"":"(reuse_addr=false)");
            }

            // listen on port, any addr
            return udp_socket->Bind(NPT_SocketAddress(NPT_IpAddress::Any, endpoint->info.udp_server.port), 
                                    endpoint->info.udp_server.reuse_addr);
        }
        break;

      case ENDPOINT_TYPE_MULTICAST_SERVER:
        {
            NPT_UdpMulticastSocket* udp_multicast_socket = new NPT_UdpMulticastSocket();
            udp_socket = udp_multicast_socket;

            // info
            if (Options.verbose) {
                printf("listening on port %d %s\n", 
                       endpoint->info.multicast_server.port,
                       endpoint->info.multicast_server.reuse_addr?"":"(reuse_addr=false)");
            }

            // listen on port, any addr
            NPT_CHECK(udp_socket->Bind(NPT_SocketAddress(NPT_IpAddress::Any, endpoint->info.multicast_server.port),
                                       endpoint->info.multicast_server.reuse_addr));

            // info
            if (Options.verbose) {
                printf("joining multicast group %s\n", endpoint->info.multicast_server.groupname);
            }

            // resolve name
            NPT_IpAddress address;
            NPT_CHECK(address.ResolveName(endpoint->info.multicast_server.groupname));

            // join the group
            NPT_CHECK(udp_multicast_socket->JoinGroup(address));

            return NPT_SUCCESS;
        }
        break;

      default:
          return NPT_FAILURE;
    }

    return NPT_SUCCESS;
}
/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int argc, char** argv)
{
    // check command line
    if (argc < 2) {
        PrintUsageAndExit();
    }

    // init endpoints
    EndPoint in_endpoint;
    in_endpoint.direction = ENDPOINT_DIRECTION_IN;
    EndPoint out_endpoint;
    out_endpoint.direction = ENDPOINT_DIRECTION_OUT;
    EndPoint* current_endpoint = &in_endpoint;

    // init other parameters
    unsigned int packet_size = PUMP_DEFAULT_PACKET_SIZE;

    // init options
    Options.verbose       = false;
    Options.show_progress = false;

    // parse command line
    argv++;
    char* arg;
    while ((arg = *argv++)) {
        if (current_endpoint == NULL) {
            printf("ERROR: unexpected argument (%s)\n", arg);
            exit(1);    
        }
                 
        if (NPT_StringsEqual(arg, "--packet-size")) {
            packet_size = strtoul(*argv++, NULL, 10);
            continue;
        } else if (NPT_StringsEqual(arg, "--verbose")) {
            Options.verbose = true;
            continue;
        } else if (NPT_StringsEqual(arg, "--show-progress")) {
            Options.show_progress = true;
            continue;
        } else if (NPT_StringsEqual(arg, "udp")) {
            if (argv[0] && argv[1]) {
                if (NPT_StringsEqual(argv[0], "server")) {
                    if (current_endpoint->direction == ENDPOINT_DIRECTION_OUT){
                        printf("ERROR: cannot use 'udp server' as output\n");
                        exit(1);
                    }
                    current_endpoint->type = ENDPOINT_TYPE_UDP_SERVER;
                    current_endpoint->info.udp_server.port = strtoul(argv[1], NULL, 10);
                    argv += 2;
                    if (argv[0] && NPT_StringsEqual(argv[0], "-r")) {
                        current_endpoint->info.udp_server.reuse_addr = false;
                        ++argv;
                    } else {
                        current_endpoint->info.udp_server.reuse_addr = true;
                    }
                } else if (NPT_StringsEqual(argv[0], "client")) {
                    if (current_endpoint->direction == ENDPOINT_DIRECTION_IN) {
                        printf("ERROR: cannot use 'udp client' as input\n");
                        exit(1);
                    }
                    if (argv[2]) {
                        current_endpoint->type = ENDPOINT_TYPE_UDP_CLIENT;
                        current_endpoint->info.udp_client.hostname = argv[1];
                        current_endpoint->info.udp_client.port = strtoul(argv[2], NULL, 10);
                        argv += 3;                        
                    } else {
                        printf("ERROR: missing argument for 'udp client'\n");
                        exit(1);
                    }
                }
            } else {
                printf("ERROR: missing argument for 'udp' endpoint\n");
                exit(1);
            }
         } else if (NPT_StringsEqual(arg, "multicast")) {
            if (argv[0] && argv[1]) {
                if (NPT_StringsEqual(argv[0], "server")) {
                    if (current_endpoint->direction == ENDPOINT_DIRECTION_OUT){
                        printf("ERROR: cannot use 'multicast server' as output\n");
                        exit(1);
                    }
                    if (argv[2]) {
                        current_endpoint->type = ENDPOINT_TYPE_MULTICAST_SERVER;
                        current_endpoint->info.multicast_server.groupname = argv[1];
                        current_endpoint->info.multicast_server.port = strtoul(argv[2], NULL, 10);
                        argv += 3;                        
                    } else {
                        printf("ERROR: missing argument for 'multicast server'\n");
                        exit(1);
                    }
                    if (argv[0] && NPT_StringsEqual(argv[0], "-r")) {
                        current_endpoint->info.multicast_server.reuse_addr = false;
                        ++argv;
                    } else {
                        current_endpoint->info.multicast_server.reuse_addr = true;
                    }
                } else if (NPT_StringsEqual(argv[0], "client")) {
                    if (current_endpoint->direction == ENDPOINT_DIRECTION_IN) {
                        printf("ERROR: cannot use 'udp client' as input\n");
                        exit(1);
                    }
                    if (argv[2] && argv[3]) {
                        current_endpoint->type = ENDPOINT_TYPE_MULTICAST_CLIENT;
                        current_endpoint->info.multicast_client.groupname = argv[1];
                        current_endpoint->info.multicast_client.port = strtoul(argv[2], NULL, 10);
                        current_endpoint->info.multicast_client.ttl = strtoul(argv[3], NULL, 10);
                        argv += 4;                        
                    } else {
                        printf("ERROR: missing argument for 'multicast client'\n");
                        exit(1);
                    }
                }
            } else {
                printf("ERROR: missing argument for 'multicast' endpoint\n");
                exit(1);
            }
        } else if (NPT_StringsEqual(arg, "tcp")) {
            if (argv[0] && argv[1]) {
                if (NPT_StringsEqual(argv[0], "server")) {
                    current_endpoint->type = ENDPOINT_TYPE_TCP_SERVER;
                    current_endpoint->info.tcp_server.port = strtoul(argv[1], NULL, 10);
                    argv += 2;
                    if (argv[0] && NPT_StringsEqual(argv[0], "-r")) {
                        current_endpoint->info.tcp_server.reuse_addr = false;
                        ++argv;
                    } else {
                        current_endpoint->info.tcp_server.reuse_addr = true;
                    }
                } else if (NPT_StringsEqual(argv[0], "client")) {
                    if (argv[2]) {
                        current_endpoint->type = ENDPOINT_TYPE_TCP_CLIENT;
                        current_endpoint->info.tcp_client.hostname = argv[1];
                        current_endpoint->info.tcp_client.port = strtoul(argv[2], NULL, 10);
                        argv += 3;                        
                    } else {
                        printf("ERROR: missing argument for 'tcp client'\n");
                        exit(1);
                    }
                }
            } else {
                printf("ERROR: missing argument for 'tcp' endpoint\n");
                exit(1);
            }
        } else if (NPT_StringsEqual(arg, "file")) {
            if (argv[0]) {
                current_endpoint->type = ENDPOINT_TYPE_FILE;
                current_endpoint->info.file.name = *argv++;
            } else {
                printf("ERROR: missing argument for 'file' endpoint\n");
                exit(1);
            }
        } else if (NPT_StringsEqual(arg, "serial")) {
            if (argv[0]) {
                current_endpoint->type = ENDPOINT_TYPE_SERIAL_PORT;
                current_endpoint->info.serial_port.name = *argv++;
            } else {
                printf("ERROR: missing argument for 'serial' endpoint\n");
                exit(1);
            }
            if (argv[0]) {
                int speed = 0;
                if (NPT_FAILED(NPT_ParseInteger(*argv++, speed))) {
                    printf("ERROR: invalid speed for 'serial' endpoint\n");
                    exit(1);
                } 
                current_endpoint->info.serial_port.speed = (unsigned int)speed;
            } else {
                printf("ERROR: missing argument for 'serial' endpoint\n");
                exit(1);
            }
        } else {
            printf("ERROR: invalid argument (%s)\n", arg);
            exit(1);
        }

        if (current_endpoint == &in_endpoint) {
            current_endpoint = &out_endpoint;
        } else {
            current_endpoint = NULL;
        }
    }

    if (current_endpoint) {
        printf("ERROR: missing endpoint specification\n");
        exit(1);
    }

    // data pump
    NPT_Result result;

    // allocate buffer
    unsigned char* buffer;
    buffer = (unsigned char*)malloc(packet_size);
    if (buffer == NULL) {
        printf("ERROR: out of memory\n");
        exit(1);
    }

    // get output stream
    NPT_OutputStreamReference out;
    result = GetEndPointStreams(&out_endpoint, NULL, &out);
    if (NPT_FAILED(result)) {
        printf("ERROR: failed to get stream for output (%d)\n", result);
        exit(1);
    }

    unsigned long offset = 0;
    unsigned long total  = 0;
    if (in_endpoint.type == ENDPOINT_TYPE_UDP_SERVER ||
        in_endpoint.type == ENDPOINT_TYPE_MULTICAST_SERVER) {
        NPT_UdpSocket* udp_socket;
        result = GetEndPointUdpSocket(&in_endpoint, udp_socket);
        if (NPT_FAILED(result)) {
            printf("ERROR: failed to create UDP socket (%d : %s)\n", result, NPT_ResultText(result));
            exit(1);
        }

        // packet loop
        NPT_DataBuffer packet(32768);
        NPT_SocketAddress address;

        do {
            result = udp_socket->Receive(packet, &address);
            if (NPT_SUCCEEDED(result)) {
                if (Options.verbose) {
                    NPT_String ip = address.GetIpAddress().ToString();
                    printf("Received %d bytes from %s\n", (int)packet.GetDataSize(), ip.GetChars());
                }
                result = out->Write(packet.GetData(), packet.GetDataSize(), NULL);
                offset += packet.GetDataSize();
                total  += packet.GetDataSize();
            }
        } while (NPT_SUCCEEDED(result));
    } else {
        // get the input stream
        NPT_InputStreamReference in;
        result = GetEndPointStreams(&in_endpoint, &in, NULL);
        if (NPT_FAILED(result)) {
            printf("ERROR: failed to get stream for input (%d : %s)\n", result, NPT_ResultText(result));
            exit(1);
        }

        // stream loop 
        do {
            NPT_Size bytes_read;
            NPT_Size bytes_written;

            // send 
            result = in->Read(buffer, packet_size, &bytes_read);
            if (Options.show_progress) {
                printf("[%d]\r", (int)total);
            }
            if (NPT_SUCCEEDED(result) && bytes_read) {
                result = out->Write(buffer, bytes_read, &bytes_written);
                if (Options.show_progress) {
                    printf("[%d]\r", (int)total);
                }
                offset += bytes_written;
                total  += bytes_written;
            } else {
                break;
            }
        } while (NPT_SUCCEEDED(result));
    }

    if (NPT_FAILED(result)) {
        printf("[%d] *******************\n", result);
        exit(1);
    }
    
    delete[] buffer;
    return 0;
}




