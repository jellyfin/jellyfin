/*****************************************************************
|
|   Neptune - Network :: Winsock Implementation
|
|   (c) 2001-2006 Gilles Boccon-Gibod
|   Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#define STRICT
#include <winsock2.h>
#include <ws2tcpip.h>
#include "NptNetwork.h"
#include "NptWin32Network.h"

/*----------------------------------------------------------------------
|   static initializer
+---------------------------------------------------------------------*/
NPT_WinsockSystem::NPT_WinsockSystem() {
    WORD    wVersionRequested;
    WSADATA wsaData;
    wVersionRequested = MAKEWORD(2, 2);
    /*wVersionRequested = MAKEWORD(1, 1);*/
    WSAStartup( wVersionRequested, &wsaData );
}
NPT_WinsockSystem::~NPT_WinsockSystem() {
    WSACleanup();
}
NPT_WinsockSystem NPT_WinsockSystem::Initializer;

#if defined(_WIN32_WCE)
// don't use the SIO_GET_INTERFACE_LIST on Windows CE, it is
// hopelessly broken, and will crash your application.
#define NPT_NETWORK_USE_IP_HELPER_API
#else
#define NPT_NETWORK_USE_SIO_GET_INTERFACE_LIST
#endif

#if defined(NPT_NETWORK_USE_SIO_GET_INTERFACE_LIST)
/*----------------------------------------------------------------------
|   NPT_NetworkInterface::GetNetworkInterfaces
+---------------------------------------------------------------------*/
NPT_Result
NPT_NetworkInterface::GetNetworkInterfaces(NPT_List<NPT_NetworkInterface*>& interfaces)
{
    // create a socket to talk to the TCP/IP stack
    SOCKET net;
    if((net = WSASocket(AF_INET, SOCK_DGRAM, IPPROTO_UDP, NULL, 0, 0)) == INVALID_SOCKET) {
        return NPT_FAILURE;
    }

    // get a list of interfaces
    INTERFACE_INFO query[32];  // get up to 32 interfaces 
    DWORD bytes_returned;
    int io_result = WSAIoctl(net, 
                             SIO_GET_INTERFACE_LIST, 
                             NULL, 0, 
                             &query, sizeof(query), 
                             &bytes_returned, 
                             NULL, NULL);
    if (io_result == SOCKET_ERROR) {
        closesocket(net);
        return NPT_FAILURE;
    }

    // we don't need the socket anymore
    closesocket(net);

    // Display interface information
    int interface_count = (bytes_returned/sizeof(INTERFACE_INFO));
    unsigned int iface_index = 0;
    for (int i=0; i<interface_count; i++) {
        SOCKADDR_IN* address;
        NPT_Flags    flags = 0;

        // primary address
        address = (SOCKADDR_IN*)&query[i].iiAddress;
        NPT_IpAddress primary_address(ntohl(address->sin_addr.s_addr));

        // netmask
        address = (SOCKADDR_IN*)&query[i].iiNetmask;
        NPT_IpAddress netmask(ntohl(address->sin_addr.s_addr));

        // broadcast address
        address = (SOCKADDR_IN*)&query[i].iiBroadcastAddress;
        NPT_IpAddress broadcast_address(ntohl(address->sin_addr.s_addr));

        {
            // broadcast address is incorrect
            unsigned char addr[4];
            for(int i=0; i<4; i++) {
                addr[i] = (primary_address.AsBytes()[i] & netmask.AsBytes()[i]) | 
                    ~netmask.AsBytes()[i];
            }
            broadcast_address.Set(addr);
        }

        // ignore interfaces that are not up
        if (!(query[i].iiFlags & IFF_UP)) {
            continue;
        }
        if (query[i].iiFlags & IFF_BROADCAST) {
            flags |= NPT_NETWORK_INTERFACE_FLAG_BROADCAST;
        }
        if (query[i].iiFlags & IFF_MULTICAST) {
            flags |= NPT_NETWORK_INTERFACE_FLAG_MULTICAST;
        }
        if (query[i].iiFlags & IFF_LOOPBACK) {
            flags |= NPT_NETWORK_INTERFACE_FLAG_LOOPBACK;
        }
        if (query[i].iiFlags & IFF_POINTTOPOINT) {
            flags |= NPT_NETWORK_INTERFACE_FLAG_POINT_TO_POINT;
        }

        // mac address (no support for this for now)
        NPT_MacAddress mac;

        // create an interface object
        char iface_name[5];
        iface_name[0] = 'i';
        iface_name[1] = 'f';
        iface_name[2] = '0'+(iface_index/10);
        iface_name[3] = '0'+(iface_index%10);
        iface_name[4] = '\0';
        NPT_NetworkInterface* iface = new NPT_NetworkInterface(iface_name, mac, flags);

        // set the interface address
        NPT_NetworkInterfaceAddress iface_address(
            primary_address,
            broadcast_address,
            NPT_IpAddress::Any,
            netmask);
        iface->AddAddress(iface_address);  
         
        // add the interface to the list
        interfaces.Add(iface);   

        // increment the index (used for generating the name
        iface_index++;
    }

    return NPT_SUCCESS;
}
#elif defined(NPT_NETWORK_USE_IP_HELPER_API)
// Use the IP Helper API
#include <iphlpapi.h>

/*----------------------------------------------------------------------
|   NPT_NetworkInterface::GetNetworkInterfaces
+---------------------------------------------------------------------*/
NPT_Result
NPT_NetworkInterface::GetNetworkInterfaces(NPT_List<NPT_NetworkInterface*>& interfaces)
{
    IP_ADAPTER_ADDRESSES* iface_list = NULL;
    ULONG                 size = sizeof(IP_ADAPTER_INFO);

    // get the interface table
    for(;;) {
        iface_list = (IP_ADAPTER_ADDRESSES*)malloc(size);
        DWORD result = GetAdaptersAddresses(AF_INET,
                                            0,
                                            NULL,
                                            iface_list, &size);
        if (result == NO_ERROR) {
            break;
        } else {
            // free and try again
            free(iface_list);
            if (result != ERROR_BUFFER_OVERFLOW) {
                return NPT_FAILURE;
            }
        }
    }

    // iterate over the interfaces
    for (IP_ADAPTER_ADDRESSES* iface = iface_list; iface; iface = iface->Next) {
        // skip this interface if it is not up
        if (iface->OperStatus != IfOperStatusUp) continue;

        // get the interface type and mac address
        NPT_MacAddress::Type mac_type;
        switch (iface->IfType) {
            case IF_TYPE_ETHERNET_CSMACD:   mac_type = NPT_MacAddress::TYPE_ETHERNET; break;
            case IF_TYPE_SOFTWARE_LOOPBACK: mac_type = NPT_MacAddress::TYPE_LOOPBACK; break;
            case IF_TYPE_PPP:               mac_type = NPT_MacAddress::TYPE_PPP;      break;
            default:                        mac_type = NPT_MacAddress::TYPE_UNKNOWN;  break;
        }
        NPT_MacAddress mac(mac_type, iface->PhysicalAddress, iface->PhysicalAddressLength);

        // compute interface flags
        NPT_Flags flags = 0;
        if (!(iface->Flags & IP_ADAPTER_NO_MULTICAST)) flags |= NPT_NETWORK_INTERFACE_FLAG_MULTICAST;
        if (iface->IfType == IF_TYPE_SOFTWARE_LOOPBACK) flags |= NPT_NETWORK_INTERFACE_FLAG_LOOPBACK;
        if (iface->IfType == IF_TYPE_PPP) flags |= NPT_NETWORK_INTERFACE_FLAG_POINT_TO_POINT;

        // compute the unicast address (only the first one is supported for now)
        NPT_IpAddress primary_address;
        if (iface->FirstUnicastAddress) {
            if (iface->FirstUnicastAddress->Address.lpSockaddr == NULL) continue;
            if (iface->FirstUnicastAddress->Address.iSockaddrLength != sizeof(SOCKADDR_IN)) continue;
            SOCKADDR_IN* address = (SOCKADDR_IN*)iface->FirstUnicastAddress->Address.lpSockaddr;
            if (address->sin_family != AF_INET) continue;
            primary_address.Set(ntohl(address->sin_addr.s_addr));
        }
        NPT_IpAddress broadcast_address; // not supported yet
        NPT_IpAddress netmask;           // not supported yet

        // convert the interface name to UTF-8
        // BUG in Wine: FriendlyName is NULL
        unsigned int iface_name_length = (unsigned int)iface->FriendlyName?wcslen(iface->FriendlyName):0;
        char* iface_name = new char[4*iface_name_length+1];
        int result = WideCharToMultiByte(
            CP_UTF8, 0, iface->FriendlyName, iface_name_length,
            iface_name, 4*iface_name_length+1,
            NULL, NULL);
        if (result > 0) {
            iface_name[result] = '\0';
        } else {
            iface_name[0] = '\0';
        }

        // create an interface descriptor
        NPT_NetworkInterface* iface_object = new NPT_NetworkInterface(iface_name, mac, flags);
        NPT_NetworkInterfaceAddress iface_address(
            primary_address,
            broadcast_address,
            NPT_IpAddress::Any,
            netmask);
        iface_object->AddAddress(iface_address);  
    
        // cleanup 
        delete[] iface_name;
         
        // add the interface to the list
        interfaces.Add(iface_object);   
    }

    free(iface_list);
    return NPT_SUCCESS;
}
#else
/*----------------------------------------------------------------------
|   NPT_NetworkInterface::GetNetworkInterfaces
+---------------------------------------------------------------------*/
NPT_Result
NPT_NetworkInterface::GetNetworkInterfaces(NPT_List<NPT_NetworkInterface*>&)
{
    return NPT_SUCCESS;
}
#endif
