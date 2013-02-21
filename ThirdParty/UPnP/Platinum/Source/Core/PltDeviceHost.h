/*****************************************************************
|
|   Platinum - Device Host
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
 UPnP Device
 */

#ifndef _PLT_DEVICE_HOST_H_
#define _PLT_DEVICE_HOST_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "PltDeviceData.h"
#include "PltSsdp.h"
#include "PltTaskManager.h"
#include "PltAction.h"
#include "PltHttp.h"

/*----------------------------------------------------------------------
|   forward declarations
+---------------------------------------------------------------------*/
class PLT_HttpServer;
class PLT_HttpServerHandler;
class PLT_SsdpDeviceAnnounceTask;
class PLT_SsdpListenTask;

/*----------------------------------------------------------------------
|   PLT_DeviceHost class
+---------------------------------------------------------------------*/
/**
 UPnP Device Host.
 The PLT_DeviceHost class is a base class for implementing a UPnP Device. It handles 
 network announcements and responses to searches from ControlPoints. ControlPoint
 action invocations are also received and delegated to derived classes. A 
 PLT_DeviceHost also takes care of eventing when services state variables change.
 */
class PLT_DeviceHost : public PLT_DeviceData,
                       public PLT_SsdpPacketListener,
                       public NPT_HttpRequestHandler
{
public:
    /**
     Creates a new instance of UPnP Device Host.
     @param description_path Relative path for description url
     @param uuid UPnP device unique id
     @param device_type UPnP device type
     @param friendly_name Name advertised for UPnP device
     @param show_ip Flag to indicate if device IP should be appended to friendly name
     @param port local port for the device host internal HTTP server, 0 for randomly
     selected.
     @param port_rebind Flag to indicate if device host should automatically try to look 
     for another port if failing to choose the one passed.
     */
    PLT_DeviceHost(const char*  description_path = "/",
                   const char*  uuid = "",
                   const char*  device_type = "",
                   const char*  friendly_name = "",
                   bool         show_ip = false,
                   NPT_UInt16   port = 0,
                   bool         port_rebind = false);
    virtual ~PLT_DeviceHost();
    
    virtual void SetBroadcast(bool broadcast) { m_Broadcast = broadcast; }
     
    /**
     When a UPnP device comes up, the specifications require that a SSDP bye-bye
     sequence is sent to force the removal of the device in case it wasn't sent
     properly during the last shutdown.
     @param bye_bye_first Boolean to indicate that SSDP bye-bye sequence should 
     be sent first or not.
     */
    virtual void SetByeByeFirst(bool bye_bye_first) { m_ByeByeFirst = bye_bye_first; }
    
    /**
     Returns the port used by the internal HTTP server for all incoming requests.
     @return port
     */
    virtual NPT_UInt16 GetPort() { return m_Port; };
    
    /**
     Sets the lease time.
     @param lease_time Lease Time
     */
    NPT_Result SetLeaseTime(NPT_TimeInterval lease_time) { return PLT_DeviceData::SetLeaseTime(lease_time); }

protected:
    /**
     NPT_HttpRequestHandler method for setting up the response of an incoming
     HTTP request.
     @param request the request received
     @param context the context of the request
     @param response the response to set up
     */
    virtual NPT_Result SetupResponse(NPT_HttpRequest&              request,
                                     const NPT_HttpRequestContext& context,
                                     NPT_HttpResponse&             response);

    /**
     Static method similar to Announce.
     @param device the device to announce
     @param request the SSDP pre formatted request
     @param socket the network socket to use to send the request
     @param byebye boolean indicating if the announce is a SSDP bye-bye or alive.
     */
    static NPT_Result Announce(PLT_DeviceData*  device, 
                                NPT_HttpRequest& request, 
                                NPT_UdpSocket&   socket, 
                                bool             byebye);
    /**
     Called during SSDP announce. The HTTP request is already configured with
     the right method and host.
     @param request the SSDP pre formatted request
     @param socket the network socket to use to send the request
     @param byebye boolean indicating if the announce is a SSDP bye-bye or alive.
     */
    NPT_Result Announce(NPT_HttpRequest& request, 
                        NPT_UdpSocket&   socket, 
                        bool             byebye) {
        return Announce(this, request, socket, byebye);
    }

    /**
     PLT_SsdpPacketListener method called when a M-SEARCH SSDP packet is received.
     @param request SSDP packet
     @param context the context of the request
     */
    virtual NPT_Result OnSsdpPacket(const NPT_HttpRequest&        request, 
                                    const NPT_HttpRequestContext& context);

    /**
     Static method similar to SendSsdpSearchResponse.
     @param device the device to announce
     @param response the SSDP pre formatted response
     @param socket the network socket to use to send the request
     @param st the original request search target
     @param addr the remote address to send the response back to in case the socket
     is not already connected.
     */
    static NPT_Result SendSsdpSearchResponse(PLT_DeviceData*          device, 
                                             NPT_HttpResponse&        response, 
                                             NPT_UdpSocket&           socket, 
                                             const char*              st,
                                             const NPT_SocketAddress* addr  = NULL);
    /**
     Called by PLT_SsdpDeviceSearchResponseTask when responding to a M-SEARCH
     SSDP request.
     @param response the SSDP pre formatted response
     @param socket the network socket to use to send the request
     @param st the original request search target
     @param addr the remote address to send the response back to in case the socket
     is not already connected.
     */
    virtual NPT_Result SendSsdpSearchResponse(NPT_HttpResponse&        response, 
                                              NPT_UdpSocket&           socket, 
                                              const char*              st,
                                              const NPT_SocketAddress* addr = NULL) {
        return SendSsdpSearchResponse(this, response, socket, st, addr);
    }
    
public:
    /**
     Add UPnP icon information to serve from file system.
     @param icon the icon information including url path
     @param fileroot the file system root path 
     @param urlroot the url root path of the icon url to match to fileroot
     Note: As an exemple, if the icon url path is "/images/icon1.jpg", the fileroot
     is "/Users/joe/www" and the urlroot is "/", when a request is made for
     "/images/icon1.jpg", the file is expected to be found at 
     "/Users/joe/www/images/icon1.jpg". If the urlroot were "/images", the file 
     would be expected to be found at "/Users/joe/www/icon1.jpg".
     */
    virtual NPT_Result AddIcon(const PLT_DeviceIcon& icon, 
                               const char*           fileroot,
                               const char*           urlroot = "/");
    
    /**
     Add UPnP icon information to serve using static image.
     @param icon the icon information including url path
     @param data the image data
     @param size the image data size
     @param copy boolean to indicate the data should be copied internally
     */
    virtual NPT_Result AddIcon(const PLT_DeviceIcon& icon, 
                               const void*           data, 
                               NPT_Size              size, 
                               bool                  copy = true);

protected:
    /**
     Required method for setting up UPnP services of device host 
     (and any embedded). Called when device starts.
     */
    virtual NPT_Result SetupServices() = 0;
    
    /**
     Default implementation for registering device icon resources. Override to 
     use different ones. Called when device starts.
     */
    virtual NPT_Result SetupIcons();
    
    /** 
     Default implementation for setting up device host. This calls SetupServices
     and SetupIcons when device starts.
     */
    virtual NPT_Result SetupDevice();
    
    /**
     Called by PLT_TaskManager when the device is started.
     @param task the SSDP listening task to attach to for receiving 
     SSDP M-SEARCH messages.
     */
    virtual NPT_Result Start(PLT_SsdpListenTask* task);
    
    /**
     Called by PLT_TaskManager when the device is stoped.
     @param task the SSDP listening task to detach from to stop receiving 
     SSDP M-SEARCH messages.
     */
    virtual NPT_Result Stop(PLT_SsdpListenTask* task);
    
    /**
     This mehod is called when an action performed by a control point has been 
     received and needs to be answered.
     @param action the action information to answer
     @param context the context information including the HTTP request and
     local and remote socket information (IP & port).
     */
    virtual NPT_Result OnAction(PLT_ActionReference&          action, 
                                const PLT_HttpRequestContext& context);
    
    /**
     This method is called when a control point is requesting the device
     description.
     @param request the HTTP request
     @param context the context information including local and remote socket information.
     @param response the response to setup.
     */
    virtual NPT_Result ProcessGetDescription(NPT_HttpRequest&              request,
                                             const NPT_HttpRequestContext& context,
                                             NPT_HttpResponse&             response);
    
    /**
     This method is called when a control point is requesting a service SCPD.
     @param service the service
     @param request the HTTP request
     @param context the context information including local and remote socket information.
     @param response the response to setup.
     */
    virtual NPT_Result ProcessGetSCPD(PLT_Service*                  service,
                                      NPT_HttpRequest&              request,
                                      const NPT_HttpRequestContext& context,
                                      NPT_HttpResponse&             response);
    
    /**
     This method is called when a "GET" request for a resource other than the device
     description, SCPD, or icons has been received.
     @param request the HTTP request
     @param context the context information including local and remote socket information.
     @param response the response to setup.
     */
    virtual NPT_Result ProcessHttpGetRequest(NPT_HttpRequest&              request,
                                             const NPT_HttpRequestContext& context,
                                             NPT_HttpResponse&             response);
    
    /**
     This method is called when a "POST" request has been received. This is usually
     an UPnP service action invocation. This will deserialize the request and call
     the OnAction method.
     @param request the HTTP request
     @param context the context information including local and remote socket information.
     @param response the response to setup.
     */
    virtual NPT_Result ProcessHttpPostRequest(NPT_HttpRequest&              request,
                                              const NPT_HttpRequestContext& context,
                                              NPT_HttpResponse&             response);
    
    /**
     This method is called when a request from a subscriber has been received. This is
     for any new subscritions, existing subscrition renewal or cancellation.
     @param request the HTTP request
     @param context the context information including local and remote socket information.
     @param response the response to setup.
     */
    virtual NPT_Result ProcessHttpSubscriberRequest(NPT_HttpRequest&              request,
                                                    const NPT_HttpRequestContext& context,
                                                    NPT_HttpResponse&             response);

protected:
    friend class PLT_UPnP;
    friend class PLT_UPnP_DeviceStartIterator;
    friend class PLT_UPnP_DeviceStopIterator;
    friend class PLT_Service;
    friend class NPT_Reference<PLT_DeviceHost>;
    friend class PLT_SsdpDeviceSearchResponseInterfaceIterator;
    friend class PLT_SsdpDeviceSearchResponseTask;
    friend class PLT_SsdpAnnounceInterfaceIterator;

    PLT_TaskManager m_TaskManager;
    PLT_HttpServer* m_HttpServer;
    bool            m_Broadcast;
    NPT_UInt16      m_Port;
    bool            m_PortRebind;
    bool            m_ByeByeFirst;
};

typedef NPT_Reference<PLT_DeviceHost> PLT_DeviceHostReference;

#endif /* _PLT_DEVICE_HOST_H_ */
