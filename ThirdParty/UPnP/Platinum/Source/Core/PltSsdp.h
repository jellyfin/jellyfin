/*****************************************************************
|
|   Platinum - SSDP
|
| Copyright (c) 2004-2010, Plutinosoft, LLC.
| All rights reserved.
| http://www.plutinosoft.com
|
| This program is free software; you can redistribute it and/or
| modify it under the terms of the GNU General Public License
| as published by the Free Software Foundation; either version 2
| of the License, or (at your option) any later version.
|
| OEMs, ISVs, VARs and other distributors that combine and 
| distribute commercially licensed software with Platinum software
| and do not wish to distribute the source code for the commercially
| licensed software under version 2, or (at your option) any later
| version, of the GNU General Public License (the "GPL") must enter
| into a commercial license agreement with Plutinosoft, LLC.
| licensing@plutinosoft.com
|  
| This program is distributed in the hope that it will be useful,
| but WITHOUT ANY WARRANTY; without even the implied warranty of
| MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
| GNU General Public License for more details.
|
| You should have received a copy of the GNU General Public License
| along with this program; see the file LICENSE.txt. If not, write to
| the Free Software Foundation, Inc., 
| 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
| http://www.gnu.org/licenses/gpl-2.0.html
|
****************************************************************/

/** @file
 UPnP SSDP
 */

#ifndef _PLT_SSDP_H_
#define _PLT_SSDP_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "PltThreadTask.h"
#include "PltHttpServerTask.h"

/*----------------------------------------------------------------------
|   forward declarations
+---------------------------------------------------------------------*/
class PLT_DeviceHost;

/*----------------------------------------------------------------------
|   PLT_SsdpPacketListener class
+---------------------------------------------------------------------*/
/**
 The PLT_SsdpPacketListener class is an interface for handling SSDP packets
 (M-SEARCH and NOTIFY).
 */
class PLT_SsdpPacketListener
{
public:
    virtual ~PLT_SsdpPacketListener() {}
    virtual NPT_Result OnSsdpPacket(const NPT_HttpRequest&        request, 
                                    const NPT_HttpRequestContext& context) = 0;
};

/*----------------------------------------------------------------------
|   PLT_SsdpSearchResponseListener class
+---------------------------------------------------------------------*/
/**
 The PLT_SsdpSearchResponseListener class is an interface for handling SSDP M-SEARCH 
 responses.
 */
class PLT_SsdpSearchResponseListener
{
public:
    virtual ~PLT_SsdpSearchResponseListener() {}
    virtual NPT_Result ProcessSsdpSearchResponse(NPT_Result                    res,  
                                                 const NPT_HttpRequestContext& context,
                                                 NPT_HttpResponse*             response) = 0;
};

/*----------------------------------------------------------------------
|   PLT_SsdpSender class
+---------------------------------------------------------------------*/
/**
 The PLT_SsdpSender class provides a mechanism to format and send SSDP packets.
 */
class PLT_SsdpSender
{
public:
    static NPT_Result SendSsdp(NPT_HttpRequest&   request, 
                               const char*        usn,
                               const char*        nt,
                               NPT_UdpSocket&     socket,
                               bool               notify,
                               const NPT_SocketAddress* addr = NULL);
     
    static NPT_Result SendSsdp(NPT_HttpResponse&  response,
                               const char*        usn,
                               const char*        nt,
                               NPT_UdpSocket&     socket,
                               bool               notify, 
                               const NPT_SocketAddress* addr = NULL);

private:
    static NPT_Result FormatPacket(NPT_HttpMessage&   message,
                                   const char*        usn,
                                   const char*        nt,
                                   NPT_UdpSocket&     socket,
                                   bool               notify);
};

/*----------------------------------------------------------------------
|   PLT_SsdpDeviceSearchResponseInterfaceIterator class
+---------------------------------------------------------------------*/
/**
 The PLT_SsdpDeviceSearchResponseInterfaceIterator class looks for the best network
 interface to use then sends a SSDP M-SEARCH response.
 */
class PLT_SsdpDeviceSearchResponseInterfaceIterator
{
public:
    PLT_SsdpDeviceSearchResponseInterfaceIterator(PLT_DeviceHost*   device, 
                                                  NPT_SocketAddress remote_addr,
                                                  const char*       st) :
        m_Device(device), m_RemoteAddr(remote_addr), m_ST(st)  {}
    virtual ~PLT_SsdpDeviceSearchResponseInterfaceIterator() {}
      
    NPT_Result operator()(NPT_NetworkInterface*& if_addr) const;

private:
    PLT_DeviceHost*   m_Device;
    NPT_SocketAddress m_RemoteAddr;
    NPT_String        m_ST;
};

/*----------------------------------------------------------------------
|   PLT_SsdpDeviceSearchResponseTask class
+---------------------------------------------------------------------*/
/**
 The PLT_SsdpDeviceSearchResponseTask class is used by a PLT_DeviceHost to respond
 to SSDP M-SEARCH requests from UPnP ControlPoints.
 */
class PLT_SsdpDeviceSearchResponseTask : public PLT_ThreadTask
{
public:
    PLT_SsdpDeviceSearchResponseTask(PLT_DeviceHost*   device, 
                                     NPT_SocketAddress remote_addr,
                                     const char*       st) : 
        m_Device(device), m_RemoteAddr(remote_addr), m_ST(st) {}

protected:
    virtual ~PLT_SsdpDeviceSearchResponseTask() {}

    // PLT_ThreadTask methods
    virtual void DoRun();
    
protected:
    PLT_DeviceHost*     m_Device;
    NPT_SocketAddress   m_RemoteAddr;
    NPT_String          m_ST;
};

/*----------------------------------------------------------------------
|   PLT_SsdpAnnounceInterfaceIterator class
+---------------------------------------------------------------------*/
/**
 The PLT_SsdpAnnounceInterfaceIterator class is used to send SSDP announcements 
 given a list of network interaces. 
 */
class PLT_SsdpAnnounceInterfaceIterator
{
public:
    PLT_SsdpAnnounceInterfaceIterator(PLT_DeviceHost* device, bool is_byebye = false, bool broadcast = false) :
        m_Device(device), m_IsByeBye(is_byebye), m_Broadcast(broadcast) {}
      
    NPT_Result operator()(NPT_NetworkInterface*& if_addr) const;
    
private:
    PLT_DeviceHost* m_Device;
    bool            m_IsByeBye;
    bool            m_Broadcast;
};

/*----------------------------------------------------------------------
|   PLT_SsdpInitMulticastIterator class
+---------------------------------------------------------------------*/
/** 
 The PLT_SsdpInitMulticastIterator class is used to join a multicast group 
 given a list of IP addresses.
 */
class PLT_SsdpInitMulticastIterator
{
public:
    PLT_SsdpInitMulticastIterator(NPT_UdpMulticastSocket* socket) :
        m_Socket(socket) {}

    NPT_Result operator()(NPT_IpAddress& if_addr) const {
        NPT_IpAddress addr;
        addr.ResolveName("239.255.255.250");
        // OSX bug, since we're reusing the socket, we need to leave group first
        // before joining it
        m_Socket->LeaveGroup(addr, if_addr);
        return m_Socket->JoinGroup(addr, if_addr);
    }

private:
    NPT_UdpMulticastSocket* m_Socket;
};

/*----------------------------------------------------------------------
|   PLT_SsdpDeviceAnnounceTask class
+---------------------------------------------------------------------*/
/**
 The PLT_SsdpDeviceAnnounceTask class is a task to send UPnP Device SSDP announcements
 (alive or byebye). It can be setup to automatically repeat after an interval.
 */
class PLT_SsdpDeviceAnnounceTask : public PLT_ThreadTask
{
public:
    PLT_SsdpDeviceAnnounceTask(PLT_DeviceHost*  device, 
                               NPT_TimeInterval repeat,
                               bool             is_byebye_first = false,
                               bool             extra_broadcast = false) : 
        m_Device(device), 
        m_Repeat(repeat), m_IsByeByeFirst(is_byebye_first), 
        m_ExtraBroadcast(extra_broadcast) {}

protected:
    virtual ~PLT_SsdpDeviceAnnounceTask() {}

    // PLT_ThreadTask methods
    virtual void DoRun();

protected:
    PLT_DeviceHost*             m_Device;
    NPT_TimeInterval            m_Repeat;
    bool                        m_IsByeByeFirst;
    bool                        m_ExtraBroadcast;
};

/*----------------------------------------------------------------------
|   PLT_NetworkInterfaceAddressSearchIterator class
+---------------------------------------------------------------------*/
/**
 The PLT_NetworkInterfaceAddressSearchIterator class returns the network interface
 given an IP address.
 */
class PLT_NetworkInterfaceAddressSearchIterator
{
public:
    PLT_NetworkInterfaceAddressSearchIterator(NPT_String ip) : m_Ip(ip)  {}
    virtual ~PLT_NetworkInterfaceAddressSearchIterator() {}

    NPT_Result operator()(NPT_NetworkInterface*& addr) const {
        NPT_List<NPT_NetworkInterfaceAddress>::Iterator niaddr = addr->GetAddresses().GetFirstItem();
        if (!niaddr) return NPT_FAILURE;

        return (m_Ip.Compare((*niaddr).GetPrimaryAddress().ToString(), true) == 0) ? NPT_SUCCESS : NPT_FAILURE;
    }

private:
    NPT_String m_Ip;
};

/*----------------------------------------------------------------------
|   PLT_SsdpPacketListenerIterator class
+---------------------------------------------------------------------*/
/**
 The PLT_SsdpPacketListenerIterator class iterates through a list of 
 PLT_SsdpPacketListener instances to notify of a new SSDP incoming packet.
 */
class PLT_SsdpPacketListenerIterator
{
public:
    PLT_SsdpPacketListenerIterator(NPT_HttpRequest&              request, 
                                   const NPT_HttpRequestContext& context) :
      m_Request(request), m_Context(context) {}

    NPT_Result operator()(PLT_SsdpPacketListener*& listener) const {
        return listener->OnSsdpPacket(m_Request, m_Context);
    }

private:
    NPT_HttpRequest&              m_Request;
    const NPT_HttpRequestContext& m_Context;
};

/*----------------------------------------------------------------------
|   PLT_SsdpListenTask class
+---------------------------------------------------------------------*/
/**
 The PLT_SsdpListenTask class is used to listen for incoming SSDP packets and 
 keep track of a list of PLT_SsdpPacketListener listeners to notify when a new 
 SSDP packet has arrived.
 */
class PLT_SsdpListenTask : public PLT_HttpServerSocketTask
{
public:
    PLT_SsdpListenTask(NPT_Socket* socket) : 
        PLT_HttpServerSocketTask(socket, true) {
        // Change read time out for UDP because iPhone 3.0 seems to hang
        // after reading everything from the socket even though
        // more stuff arrived
#if defined(TARGET_OS_IPHONE) && TARGET_OS_IPHONE
        m_Socket->SetReadTimeout(10000);
#endif
    }

    NPT_Result AddListener(PLT_SsdpPacketListener* listener) {
        NPT_AutoLock lock(m_Mutex);
        m_Listeners.Add(listener);
        return NPT_SUCCESS;
    }

    NPT_Result RemoveListener(PLT_SsdpPacketListener* listener) {
        NPT_AutoLock lock(m_Mutex);
        m_Listeners.Remove(listener);
        return NPT_SUCCESS;
    }
    
    // PLT_Task methods
    void DoAbort();

protected:
    virtual ~PLT_SsdpListenTask() {}

    // PLT_HttpServerSocketTask methods
    NPT_Result GetInputStream(NPT_InputStreamReference& stream);
    NPT_Result GetInfo(NPT_SocketInfo& info);
    NPT_Result SetupResponse(NPT_HttpRequest&              request, 
                             const NPT_HttpRequestContext& context,
                             NPT_HttpResponse&             response);

protected:
    PLT_InputDatagramStreamReference  m_Datagram;
    NPT_List<PLT_SsdpPacketListener*> m_Listeners;
    NPT_Mutex                         m_Mutex;
};

/*----------------------------------------------------------------------
|   PLT_SsdpSearchTask class
+---------------------------------------------------------------------*/
/**
 The PLT_SsdpSearchTask class is a task used by a PLT_CtrlPoint to issue a SSDP
 M-SEARCH request. It can be set to repeat at a certain frequencey.
 */
class PLT_SsdpSearchTask : public PLT_ThreadTask
{
public:
    PLT_SsdpSearchTask(NPT_UdpSocket*                  socket,
                       PLT_SsdpSearchResponseListener* listener, 
                       NPT_HttpRequest*                request,
                       NPT_TimeInterval                frequency = NPT_TimeInterval(0.)); // pass 0 for one time

protected:
    virtual ~PLT_SsdpSearchTask();

    // PLT_ThreadTask methods
    virtual void DoAbort();
    virtual void DoRun();

    virtual NPT_Result ProcessResponse(NPT_Result                    res, 
                                       const NPT_HttpRequest&        request,  
                                       const NPT_HttpRequestContext& context,
                                       NPT_HttpResponse*             response);

private:
    PLT_SsdpSearchResponseListener* m_Listener;
    NPT_HttpRequest*                m_Request;
    NPT_TimeInterval                m_Frequency;
    bool                            m_Repeat;
    NPT_UdpSocket*                  m_Socket;
};

#endif /* _PLT_SSDP_H_ */
