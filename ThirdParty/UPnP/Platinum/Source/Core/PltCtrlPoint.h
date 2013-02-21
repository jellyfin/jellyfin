/*****************************************************************
|
|   Platinum - Control Point
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
 UPnP ControlPoint
 */

#ifndef _PLT_CONTROL_POINT_H_
#define _PLT_CONTROL_POINT_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "PltService.h"
#include "PltSsdp.h"
#include "PltDeviceData.h"

/*----------------------------------------------------------------------
|   forward declarations
+---------------------------------------------------------------------*/
class PLT_HttpServer;
class PLT_CtrlPointHouseKeepingTask;
class PLT_SsdpSearchTask;
class PLT_SsdpListenTask;
class PLT_CtrlPointGetSCPDsTask;
class PLT_CtrlPointGetSCPDRequest;

/*----------------------------------------------------------------------
|   PLT_CtrlPointListener class
+---------------------------------------------------------------------*/
/**
 The PLT_CtrlPointListener class is an interface used to receive notifications when
 devices are found or removed from the network, actions responses and events
 are being received.
 */
class PLT_CtrlPointListener
{
public:
    virtual ~PLT_CtrlPointListener() {}

    virtual NPT_Result OnDeviceAdded(PLT_DeviceDataReference& device) = 0;
    virtual NPT_Result OnDeviceRemoved(PLT_DeviceDataReference& device) = 0;
    virtual NPT_Result OnActionResponse(NPT_Result res, PLT_ActionReference& action, void* userdata) = 0;
    virtual NPT_Result OnEventNotify(PLT_Service* service, NPT_List<PLT_StateVariable*>* vars) = 0;
};

typedef NPT_List<PLT_CtrlPointListener*> PLT_CtrlPointListenerList;

/*----------------------------------------------------------------------
|   PLT_CtrlPoint class
+---------------------------------------------------------------------*/
/**
 The PLT_CtrlPoint class implements the base functionality of a UPnP ControlPoint.
 It searches and inpects devices, invoke actions on services and subscribes to 
 events. 
 */
class PLT_CtrlPoint : public PLT_SsdpPacketListener,
                      public PLT_SsdpSearchResponseListener,
                      public NPT_HttpRequestHandler
{
public:
    PLT_CtrlPoint(const char* search_criteria = "upnp:rootdevice"); // pass NULL to prevent repeated automatic search
    virtual ~PLT_CtrlPoint();
    
    /**
     Returns the port used by the internal HTTP server for all incoming event notifications.
     @return port
     */
    NPT_Result GetPort(NPT_UInt16& port);
    
    // delegation
    NPT_Result AddListener(PLT_CtrlPointListener* listener);
    NPT_Result RemoveListener(PLT_CtrlPointListener* listener);

    // discovery
    void       IgnoreUUID(const char* uuid);
    NPT_Result Search(const NPT_HttpUrl& url = NPT_HttpUrl("239.255.255.250", 1900, "*"), 
                      const char*        target = "upnp:rootdevice", 
                      NPT_Cardinal       mx = 5,
                      NPT_TimeInterval   frequency = NPT_TimeInterval(50.), // pass NPT_TimeInterval(0.) for one time only
                      NPT_TimeInterval   initial_delay = NPT_TimeInterval(0.));
    NPT_Result Discover(const NPT_HttpUrl& url = NPT_HttpUrl("239.255.255.250", 1900, "*"), 
                        const char*        target = "ssdp:all", 
                        NPT_Cardinal       mx = 5,
                        NPT_TimeInterval   frequency = NPT_TimeInterval(50.), // pass NPT_TimeInterval(0.) for one time only
                        NPT_TimeInterval   initial_delay = NPT_TimeInterval(0.));
    NPT_Result InspectDevice(const NPT_HttpUrl& location,
                             const char*        uuid,
                             NPT_TimeInterval   leasetime = *PLT_Constants::GetInstance().GetDefaultDeviceLease());

    // actions
    NPT_Result FindActionDesc(PLT_DeviceDataReference& device, 
                              const char*              service_type,
                              const char*              action_name,
                              PLT_ActionDesc*&         action_desc);
    NPT_Result CreateAction(PLT_DeviceDataReference& device, 
                            const char*              service_type,
                            const char*              action_name,
                            PLT_ActionReference&     action);
    NPT_Result InvokeAction(PLT_ActionReference& action, void* userdata = NULL);

    // events
    NPT_Result Subscribe(PLT_Service* service, 
                         bool         cancel = false, 
                         void*        userdata = NULL);

    // NPT_HttpRequestHandler methods
    virtual NPT_Result SetupResponse(NPT_HttpRequest&              request,
                                     const NPT_HttpRequestContext& context,
                                     NPT_HttpResponse&             response);

    // PLT_SsdpSearchResponseListener methods
    virtual NPT_Result ProcessSsdpSearchResponse(NPT_Result                    res, 
                                                 const NPT_HttpRequestContext& context,
                                                 NPT_HttpResponse*             response);
    // PLT_SsdpPacketListener method
    virtual NPT_Result OnSsdpPacket(const NPT_HttpRequest&        request, 
                                    const NPT_HttpRequestContext& context);

protected:

    // State Variable Handling
    virtual NPT_Result DecomposeLastChangeVar(NPT_List<PLT_StateVariable*>& vars);

    // methods
    
    NPT_Result   Start(PLT_SsdpListenTask* task);
    NPT_Result   Stop(PLT_SsdpListenTask* task);

    // SSDP & HTTP Notifications handling
    NPT_Result   ProcessSsdpNotify(const NPT_HttpRequest&        request, 
                                   const NPT_HttpRequestContext& context);
    NPT_Result   ProcessSsdpMessage(const NPT_HttpMessage&        message, 
                                    const NPT_HttpRequestContext& context,  
                                    NPT_String&                   uuid);
    NPT_Result   ProcessGetDescriptionResponse(NPT_Result                    res, 
                                               const NPT_HttpRequest&        request, 
                                               const NPT_HttpRequestContext& context,
                                               NPT_HttpResponse*             response,
                                               NPT_TimeInterval              leasetime,
                                               NPT_String                    uuid);
    NPT_Result   ProcessGetSCPDResponse(NPT_Result                    res, 
                                        const NPT_HttpRequest&        request,
                                        const NPT_HttpRequestContext& context,
                                        NPT_HttpResponse*             response,
                                        PLT_DeviceDataReference&      device);
    NPT_Result   ProcessActionResponse(NPT_Result                    res,
                                       const NPT_HttpRequest&        request,
                                       const NPT_HttpRequestContext& context,
                                       NPT_HttpResponse*             response,
                                       PLT_ActionReference&          action,
                                       void*                         userdata);
    NPT_Result   ProcessSubscribeResponse(NPT_Result                    res, 
                                          const NPT_HttpRequest&        request, 
                                          const NPT_HttpRequestContext& context, 
                                          NPT_HttpResponse*             response,
                                          PLT_Service*                  service,
                                          void*                         userdata);
    NPT_Result   ProcessHttpNotify(const NPT_HttpRequest&        request,
                                   const NPT_HttpRequestContext& context,
                                   NPT_HttpResponse&             response);

    // Device management
    NPT_Result   AddDevice(PLT_DeviceDataReference& data);
    NPT_Result   RemoveDevice(PLT_DeviceDataReference& data);
    
private:
    // methods
    NPT_Result RenewSubscribers();
    PLT_ThreadTask* RenewSubscriber(PLT_EventSubscriberReference subscriber);
    NPT_Result AddPendingEventNotification(PLT_EventNotification *notification);
    NPT_Result ProcessPendingEventNotifications();
    NPT_Result ProcessEventNotification(PLT_EventSubscriberReference subscriber,
                                        PLT_EventNotification*       notification,
                                        NPT_List<PLT_StateVariable*> &vars);
    
    NPT_Result DoHouseKeeping();
    NPT_Result FetchDeviceSCPDs(PLT_CtrlPointGetSCPDsTask* task,
                                PLT_DeviceDataReference&   device, 
                                NPT_Cardinal               level);

    // Device management
    NPT_Result FindDevice(const char* uuid, PLT_DeviceDataReference& device, bool return_root = false);
    NPT_Result NotifyDeviceReady(PLT_DeviceDataReference& data);
    NPT_Result NotifyDeviceRemoved(PLT_DeviceDataReference& data);
    NPT_Result CleanupDevice(PLT_DeviceDataReference& data);
    
    NPT_Result ParseFault(PLT_ActionReference& action, NPT_XmlElementNode* fault);
    PLT_SsdpSearchTask* CreateSearchTask(const NPT_HttpUrl&   url, 
                                         const char*          target, 
                                         NPT_Cardinal         mx, 
                                         NPT_TimeInterval     frequency,
                                         const NPT_IpAddress& address);
    
private:
    friend class NPT_Reference<PLT_CtrlPoint>;
    friend class PLT_UPnP;
    friend class PLT_UPnP_CtrlPointStartIterator;
    friend class PLT_UPnP_CtrlPointStopIterator;
    friend class PLT_EventSubscriberRemoverIterator;
    friend class PLT_CtrlPointGetDescriptionTask;
    friend class PLT_CtrlPointGetSCPDsTask;
    friend class PLT_CtrlPointInvokeActionTask;
    friend class PLT_CtrlPointHouseKeepingTask;
    friend class PLT_CtrlPointSubscribeEventTask;

    NPT_List<NPT_String>                         m_UUIDsToIgnore;
    PLT_CtrlPointListenerList                    m_ListenerList;
    PLT_HttpServer*                              m_EventHttpServer;
    PLT_TaskManager                              m_TaskManager;
    NPT_Mutex                                    m_Lock;
    NPT_List<PLT_DeviceDataReference>            m_RootDevices;
    NPT_List<PLT_EventSubscriberReference>       m_Subscribers;
    NPT_String                                   m_SearchCriteria;
    bool                                         m_Aborted;
    NPT_List<PLT_EventNotification *>            m_PendingNotifications;
    NPT_List<NPT_String>                         m_PendingInspections;
};

typedef NPT_Reference<PLT_CtrlPoint> PLT_CtrlPointReference;

#endif /* _PLT_CONTROL_POINT_H_ */
