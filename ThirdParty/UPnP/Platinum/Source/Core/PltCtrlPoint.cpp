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

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "PltCtrlPoint.h"
#include "PltUPnP.h"
#include "PltDeviceData.h"
#include "PltUtilities.h"
#include "PltCtrlPointTask.h"
#include "PltSsdp.h"
#include "PltHttpServer.h"
#include "PltConstants.h"

NPT_SET_LOCAL_LOGGER("platinum.core.ctrlpoint")

/*----------------------------------------------------------------------
|   PLT_CtrlPointListenerOnDeviceAddedIterator class
+---------------------------------------------------------------------*/
class PLT_CtrlPointListenerOnDeviceAddedIterator
{
public:
    PLT_CtrlPointListenerOnDeviceAddedIterator(PLT_DeviceDataReference& device) :
        m_Device(device) {}

    NPT_Result operator()(PLT_CtrlPointListener*& listener) const {
        return listener->OnDeviceAdded(m_Device);
    }

private:
    PLT_DeviceDataReference& m_Device;
};

/*----------------------------------------------------------------------
|   PLT_CtrlPointListenerOnDeviceRemovedIterator class
+---------------------------------------------------------------------*/
class PLT_CtrlPointListenerOnDeviceRemovedIterator
{
public:
    PLT_CtrlPointListenerOnDeviceRemovedIterator(PLT_DeviceDataReference& device) :
        m_Device(device) {}

    NPT_Result operator()(PLT_CtrlPointListener*& listener) const {
        return listener->OnDeviceRemoved(m_Device);
    }

private:
    PLT_DeviceDataReference& m_Device;
};

/*----------------------------------------------------------------------
|   PLT_CtrlPointListenerOnActionResponseIterator class
+---------------------------------------------------------------------*/
class PLT_CtrlPointListenerOnActionResponseIterator
{
public:
    PLT_CtrlPointListenerOnActionResponseIterator(NPT_Result           res, 
                                                  PLT_ActionReference& action, 
                                                  void*                userdata) :
        m_Res(res), m_Action(action), m_Userdata(userdata) {}

    NPT_Result operator()(PLT_CtrlPointListener*& listener) const {
        return listener->OnActionResponse(m_Res, m_Action, m_Userdata);
    }

private:
    NPT_Result           m_Res;
    PLT_ActionReference& m_Action;
    void*                m_Userdata;
};

/*----------------------------------------------------------------------
|   PLT_CtrlPointListenerOnEventNotifyIterator class
+---------------------------------------------------------------------*/
class PLT_CtrlPointListenerOnEventNotifyIterator
{
public:
    PLT_CtrlPointListenerOnEventNotifyIterator(PLT_Service*                  service, 
                                               NPT_List<PLT_StateVariable*>* vars) :
        m_Service(service), m_Vars(vars) {}

    NPT_Result operator()(PLT_CtrlPointListener*& listener) const {
        return listener->OnEventNotify(m_Service, m_Vars);
    }

private:
    PLT_Service*                  m_Service;
    NPT_List<PLT_StateVariable*>* m_Vars;
};

/*----------------------------------------------------------------------
|   PLT_AddGetSCPDRequestIterator class
+---------------------------------------------------------------------*/
class PLT_AddGetSCPDRequestIterator
{
public:
    PLT_AddGetSCPDRequestIterator(PLT_CtrlPointGetSCPDsTask& task,
                                  PLT_DeviceDataReference&   device) :
        m_Task(task), m_Device(device) {}

    NPT_Result operator()(PLT_Service*& service) const {
        // look for the host and port of the device
        NPT_String scpd_url = service->GetSCPDURL(true);

        NPT_LOG_FINER_3("Queueing SCPD request for service \"%s\" of device \"%s\" @ %s", 
            (const char*)service->GetServiceID(),
            (const char*)service->GetDevice()->GetFriendlyName(),
            (const char*)scpd_url);

        // verify url before queuing just in case
        NPT_HttpUrl url(scpd_url);
        if (!url.IsValid()) {
            NPT_LOG_SEVERE_3("Invalid SCPD url \"%s\" for service \"%s\" of device \"%s\"!",
                (const char*)scpd_url, 
                (const char*)service->GetServiceID(),
                (const char*)service->GetDevice()->GetFriendlyName());
            return NPT_ERROR_INVALID_SYNTAX;
        }

        // Create request and attach service to it
        PLT_CtrlPointGetSCPDRequest* request = 
            new PLT_CtrlPointGetSCPDRequest((PLT_DeviceDataReference&)m_Device, scpd_url, "GET", NPT_HTTP_PROTOCOL_1_1);
        return m_Task.AddSCPDRequest(request);
    }

private:
    PLT_CtrlPointGetSCPDsTask& m_Task;
    PLT_DeviceDataReference    m_Device;
};

/*----------------------------------------------------------------------
|   PLT_EventSubscriberRemoverIterator class
+---------------------------------------------------------------------*/
// Note: The PLT_CtrlPoint::m_Lock must be acquired prior to using any 
// function such as Apply on this iterator
class PLT_EventSubscriberRemoverIterator
{
public:
    PLT_EventSubscriberRemoverIterator(PLT_CtrlPoint* ctrl_point) : 
        m_CtrlPoint(ctrl_point) {}
    ~PLT_EventSubscriberRemoverIterator() {}

    NPT_Result operator()(PLT_Service*& service) const {
        PLT_EventSubscriberReference sub;
        if (NPT_SUCCEEDED(NPT_ContainerFind(m_CtrlPoint->m_Subscribers, 
                                            PLT_EventSubscriberFinderByService(service), sub))) {
            NPT_LOG_INFO_1("Removed subscriber \"%s\"", (const char*)sub->GetSID());
            m_CtrlPoint->m_Subscribers.Remove(sub);
        }

        return NPT_SUCCESS;
    }

private:
    PLT_CtrlPoint* m_CtrlPoint;
};

/*----------------------------------------------------------------------
|   PLT_ServiceReadyIterator class
+---------------------------------------------------------------------*/
class PLT_ServiceReadyIterator
{
public:
    PLT_ServiceReadyIterator() {}

    NPT_Result operator()(PLT_Service*& service) const {
        return service->IsValid()?NPT_SUCCESS:NPT_FAILURE;
    }
};

/*----------------------------------------------------------------------
|   PLT_DeviceReadyIterator class
+---------------------------------------------------------------------*/
class PLT_DeviceReadyIterator
{
public:
    PLT_DeviceReadyIterator() {}
    NPT_Result operator()(PLT_DeviceDataReference& device) const {
        NPT_Result res = device->m_Services.ApplyUntil(
            PLT_ServiceReadyIterator(), 
            NPT_UntilResultNotEquals(NPT_SUCCESS));
        if (NPT_FAILED(res)) return res;

        res = device->m_EmbeddedDevices.ApplyUntil(
            PLT_DeviceReadyIterator(), 
            NPT_UntilResultNotEquals(NPT_SUCCESS));
        if (NPT_FAILED(res)) return res;

        // a device must have at least one service or embedded device 
        // otherwise it's not ready
        if (device->m_Services.GetItemCount() == 0 &&
            device->m_EmbeddedDevices.GetItemCount() == 0) {
            return NPT_FAILURE;
        }
        
        return NPT_SUCCESS;
    }
};

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::PLT_CtrlPoint
+---------------------------------------------------------------------*/
PLT_CtrlPoint::PLT_CtrlPoint(const char* search_criteria /* = "upnp:rootdevice" */) :
    m_EventHttpServer(NULL),
    m_SearchCriteria(search_criteria),
    m_Aborted(false)
{
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::~PLT_CtrlPoint
+---------------------------------------------------------------------*/
PLT_CtrlPoint::~PLT_CtrlPoint()
{
    delete m_EventHttpServer;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::IgnoreUUID
+---------------------------------------------------------------------*/
void
PLT_CtrlPoint::IgnoreUUID(const char* uuid)
{
    if (!m_UUIDsToIgnore.Find(NPT_StringFinder(uuid))) {
        m_UUIDsToIgnore.Add(uuid);
    }
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::Start
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::Start(PLT_SsdpListenTask* task)
{
    m_Aborted = false;
    
    m_EventHttpServer = new PLT_HttpServer();
    m_EventHttpServer->AddRequestHandler(new PLT_HttpRequestHandler(this), "/", true, true);
    m_EventHttpServer->Start();

    // house keeping task
    m_TaskManager.StartTask(new PLT_CtrlPointHouseKeepingTask(this));

    // add ourselves as an listener to SSDP multicast advertisements
    task->AddListener(this);

    //    
    // use next line instead for DLNA testing, faster frequency for M-SEARCH
    //return m_SearchCriteria.GetLength()?Search(NPT_HttpUrl("239.255.255.250", 1900, "*"), m_SearchCriteria, 1, 5000):NPT_SUCCESS;
    // 
    
    return m_SearchCriteria.GetLength()?Search(NPT_HttpUrl("239.255.255.250", 1900, "*"), m_SearchCriteria):NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::GetPort
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::GetPort(NPT_UInt16& port)
{
    if (m_Aborted) return NPT_FAILURE;
    
    port = m_EventHttpServer->GetPort();
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::Stop
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::Stop(PLT_SsdpListenTask* task)
{
    m_Aborted = true;
    task->RemoveListener(this);

    m_TaskManager.StopAllTasks();

    // force remove all devices
    NPT_List<PLT_DeviceDataReference>::Iterator iter = m_RootDevices.GetFirstItem();
    while (iter) {
        NotifyDeviceRemoved(*iter);
        ++iter;
    }
    
    if (m_EventHttpServer) {
        m_EventHttpServer->Stop();
        delete m_EventHttpServer;
        m_EventHttpServer = NULL;
    }
    
    // we can safely clear everything without a lock
    // as there are no more tasks pending
    m_RootDevices.Clear();
    m_Subscribers.Clear();

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::AddListener
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::AddListener(PLT_CtrlPointListener* listener) 
{
    NPT_AutoLock lock(m_Lock);
    if (!m_ListenerList.Contains(listener)) {
        m_ListenerList.Add(listener);
    }
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::RemoveListener
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::RemoveListener(PLT_CtrlPointListener* listener)
{
    NPT_AutoLock lock(m_Lock);
    m_ListenerList.Remove(listener);
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::CreateSearchTask
+---------------------------------------------------------------------*/
PLT_SsdpSearchTask*
PLT_CtrlPoint::CreateSearchTask(const NPT_HttpUrl&   url, 
                                const char*          target, 
                                NPT_Cardinal         mx, 
                                NPT_TimeInterval     frequency,
                                const NPT_IpAddress& address)
{
    // make sure mx is at least 1
    if (mx<1) mx=1;

    // create socket
    NPT_UdpMulticastSocket* socket = new NPT_UdpMulticastSocket();
    socket->SetInterface(address);
    socket->SetTimeToLive(PLT_Constants::GetInstance().GetSearchMulticastTimeToLive());

    // bind to something > 1024 and different than 1900
    int retries = 20;
    do {    
        int random = NPT_System::GetRandomInteger();
        int port = (unsigned short)(1024 + (random % 15000));
        if (port == 1900) continue;

        if (NPT_SUCCEEDED(socket->Bind(
            NPT_SocketAddress(NPT_IpAddress::Any, port), 
            false)))
            break;

    } while (--retries);

    if (retries == 0) {
        NPT_LOG_SEVERE("Couldn't bind socket for Search Task");
        return NULL;
    }

    // create request
    NPT_HttpRequest* request = new NPT_HttpRequest(url, "M-SEARCH", NPT_HTTP_PROTOCOL_1_1);
    PLT_UPnPMessageHelper::SetMX(*request, mx);
    PLT_UPnPMessageHelper::SetST(*request, target);
    PLT_UPnPMessageHelper::SetMAN(*request, "\"ssdp:discover\"");
    request->GetHeaders().SetHeader(NPT_HTTP_HEADER_USER_AGENT, *PLT_Constants::GetInstance().GetDefaultUserAgent());

    // create task
    PLT_SsdpSearchTask* task = new PLT_SsdpSearchTask(
        socket,
        this, 
        request,
        (frequency.ToMillis()>0 && frequency.ToMillis()<5000)?NPT_TimeInterval(5.):frequency);  /* repeat no less than every 5 secs */
    return task;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::Search
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::Search(const NPT_HttpUrl& url, 
                      const char*        target, 
                      NPT_Cardinal       mx /* = 5 */,
                      NPT_TimeInterval   frequency /* = NPT_TimeInterval(50.) */,
                      NPT_TimeInterval   initial_delay /* = NPT_TimeInterval(0.) */)
{
    if (m_Aborted) NPT_CHECK_WARNING(NPT_ERROR_INVALID_STATE);
    
    NPT_List<NPT_NetworkInterface*> if_list;
    NPT_List<NPT_NetworkInterface*>::Iterator net_if;
    NPT_List<NPT_NetworkInterfaceAddress>::Iterator net_if_addr;

    NPT_CHECK_SEVERE(PLT_UPnPMessageHelper::GetNetworkInterfaces(if_list, true));

    for (net_if = if_list.GetFirstItem(); 
         net_if; 
         net_if++) {
        // make sure the interface is at least broadcast or multicast
        if (!((*net_if)->GetFlags() & NPT_NETWORK_INTERFACE_FLAG_MULTICAST) &&
            !((*net_if)->GetFlags() & NPT_NETWORK_INTERFACE_FLAG_BROADCAST)) {
            continue;
        }       
            
        for (net_if_addr = (*net_if)->GetAddresses().GetFirstItem(); 
             net_if_addr; 
             net_if_addr++) {
            // create task
            PLT_SsdpSearchTask* task = CreateSearchTask(url, 
                target, 
                mx, 
                frequency,
                (*net_if_addr).GetPrimaryAddress());
            m_TaskManager.StartTask(task, &initial_delay);
        }
    }

    if_list.Apply(NPT_ObjectDeleter<NPT_NetworkInterface>());
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::Discover
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::Discover(const NPT_HttpUrl& url, 
                        const char*        target, 
                        NPT_Cardinal       mx, /* = 5 */
                        NPT_TimeInterval   frequency /* = NPT_TimeInterval(50.) */,
                        NPT_TimeInterval   initial_delay /* = NPT_TimeInterval(0.) */)
{
    if (m_Aborted) NPT_CHECK_WARNING(NPT_ERROR_INVALID_STATE);

    // make sure mx is at least 1
    if (mx<1) mx = 1;

    // create socket
    NPT_UdpSocket* socket = new NPT_UdpSocket();

    // create request
    NPT_HttpRequest* request = new NPT_HttpRequest(url, "M-SEARCH", NPT_HTTP_PROTOCOL_1_1);
    PLT_UPnPMessageHelper::SetMX(*request, mx);
    PLT_UPnPMessageHelper::SetST(*request, target);
    PLT_UPnPMessageHelper::SetMAN(*request, "\"ssdp:discover\"");
    request->GetHeaders().SetHeader(NPT_HTTP_HEADER_USER_AGENT, *PLT_Constants::GetInstance().GetDefaultUserAgent());

    // force HOST to be the regular multicast address:port
    // Some servers do care (like WMC) otherwise they won't respond to us
    request->GetHeaders().SetHeader(NPT_HTTP_HEADER_HOST, "239.255.255.250:1900");

    // create task
    PLT_ThreadTask* task = new PLT_SsdpSearchTask(
        socket,
        this, 
        request,
        (frequency.ToMillis()>0 && frequency.ToMillis()<5000)?NPT_TimeInterval(5.):frequency);  /* repeat no less than every 5 secs */
    return m_TaskManager.StartTask(task, &initial_delay);
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::DoHouseKeeping
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::DoHouseKeeping()
{
    NPT_List<PLT_DeviceDataReference> devices_to_remove;
    
    // remove expired devices
    {
        NPT_AutoLock lock(m_Lock);

        PLT_DeviceDataReference head, device;
        while (NPT_SUCCEEDED(m_RootDevices.PopHead(device))) {
            NPT_TimeStamp    last_update = device->GetLeaseTimeLastUpdate();
            NPT_TimeInterval lease_time  = device->GetLeaseTime();

            // check if device lease time has expired or if failed to renew subscribers 
            // TODO: UDA 1.1 says that root device and all embedded devices must have expired
            // before we can assume they're all no longer unavailable (we may have missed the root device renew)
            NPT_TimeStamp now;
            NPT_System::GetCurrentTimeStamp(now);
            if (now > last_update + NPT_TimeInterval((double)lease_time*2)) {
                devices_to_remove.Add(device);
            } else {
                // add the device back to our list since it is still alive
                m_RootDevices.Add(device);

                // keep track of first device added back to list
                // to know we checked all devices in initial list
                if (head.IsNull()) head = device;
            }
            
            // have we exhausted initial list?
            if (!head.IsNull() && head == *m_RootDevices.GetFirstItem())
                break;
        };
    }

    // remove old devices
    {
        NPT_AutoLock lock(m_Lock);

        for (NPT_List<PLT_DeviceDataReference>::Iterator device =
             devices_to_remove.GetFirstItem();
             device;
             device++) {
             RemoveDevice(*device);
        }
    }

    // renew subscribers of subscribed device services
    NPT_List<PLT_ThreadTask*> tasks;
    {
        NPT_AutoLock lock(m_Lock);

        NPT_List<PLT_EventSubscriberReference>::Iterator sub = m_Subscribers.GetFirstItem();
        while (sub) {
            NPT_TimeStamp now;
            NPT_System::GetCurrentTimeStamp(now);

            // time to renew if within 90 secs of expiration
            if (now > (*sub)->GetExpirationTime() - NPT_TimeStamp(90.)) {
                PLT_ThreadTask* task = RenewSubscriber(*sub);
                if (task) tasks.Add(task);
            }
            sub++;
        }
    }

    // Queue up all tasks now outside of lock, in case they
    // block because the task manager has maxed out number of running tasks
    // and to avoid a deadlock with tasks trying to acquire the lock in the response
    NPT_List<PLT_ThreadTask*>::Iterator task = tasks.GetFirstItem();
    while (task) {
        m_TaskManager.StartTask(*task);
        task++;
    }
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::FindDevice
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::FindDevice(const char*              uuid, 
                          PLT_DeviceDataReference& device,
                          bool                     return_root /* = false */) 
{
    NPT_List<PLT_DeviceDataReference>::Iterator iter = m_RootDevices.GetFirstItem();
    while (iter) {
        // device uuid found immediately as root device
        if ((*iter)->GetUUID().Compare(uuid) == 0) {
            device = *iter;
            return NPT_SUCCESS;
        } else if (NPT_SUCCEEDED((*iter)->FindEmbeddedDevice(uuid, device))) {
            // we found the uuid as an embedded device of this root
            // return root if told, otherwise return found embedded device
            if (return_root) device = (*iter);
            return NPT_SUCCESS;
        }
        ++iter;
    }

    return NPT_ERROR_NO_SUCH_ITEM;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::FindActionDesc
+---------------------------------------------------------------------*/
NPT_Result 
PLT_CtrlPoint::FindActionDesc(PLT_DeviceDataReference& device, 
                              const char*              service_type,
                              const char*              action_name,
                              PLT_ActionDesc*&         action_desc)
{
    if (device.IsNull()) return NPT_ERROR_INVALID_PARAMETERS;
    
    // look for the service
    PLT_Service* service;
    if (NPT_FAILED(device->FindServiceByType(service_type, service))) {
        NPT_LOG_FINE_1("Service %s not found", (const char*)service_type);
        return NPT_FAILURE;
    }

    action_desc = service->FindActionDesc(action_name);
    if (action_desc == NULL) {
        NPT_LOG_FINE_1("Action %s not found in service", action_name);
        return NPT_FAILURE;
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::CreateAction
+---------------------------------------------------------------------*/
NPT_Result 
PLT_CtrlPoint::CreateAction(PLT_DeviceDataReference& device, 
                            const char*              service_type,
                            const char*              action_name,
                            PLT_ActionReference&     action)
{
    if (device.IsNull()) return NPT_ERROR_INVALID_PARAMETERS;

    NPT_AutoLock lock(m_Lock);
    
    PLT_ActionDesc* action_desc;
    NPT_CHECK_SEVERE(FindActionDesc(device, 
        service_type, 
        action_name, 
        action_desc));

    PLT_DeviceDataReference root_device;
    NPT_CHECK_SEVERE(FindDevice(device->GetUUID(), root_device, true));

    action = new PLT_Action(*action_desc, root_device);
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::SetupResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::SetupResponse(NPT_HttpRequest&              request,
                             const NPT_HttpRequestContext& context,
                             NPT_HttpResponse&             response)
{
    NPT_COMPILER_UNUSED(context);
    
    if (request.GetMethod().Compare("NOTIFY") == 0) {
        return ProcessHttpNotify(request, context, response);
    }

    NPT_LOG_SEVERE("CtrlPoint received bad http request\r\n");
    response.SetStatus(412, "Precondition Failed");
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::DecomposeLastChangeVar
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::DecomposeLastChangeVar(NPT_List<PLT_StateVariable*>& vars)
{
    // parse LastChange var into smaller vars
    PLT_StateVariable* lastChangeVar = NULL;
    if (NPT_SUCCEEDED(NPT_ContainerFind(vars, 
                                        PLT_StateVariableNameFinder("LastChange"), 
                                        lastChangeVar))) {
        vars.Remove(lastChangeVar);
        PLT_Service* var_service = lastChangeVar->GetService();
        NPT_String text = lastChangeVar->GetValue();
        
        NPT_XmlNode* xml = NULL;
        NPT_XmlParser parser;
        if (NPT_FAILED(parser.Parse(text, xml)) || !xml || !xml->AsElementNode()) {
            delete xml;
            return NPT_ERROR_INVALID_FORMAT;
        }
        
        NPT_XmlElementNode* node = xml->AsElementNode();
        if (!node->GetTag().Compare("Event", true)) {
            // look for the instance with attribute id = 0
            NPT_XmlElementNode* instance = NULL;
            for (NPT_Cardinal i=0; i<node->GetChildren().GetItemCount(); i++) {
                NPT_XmlElementNode* child;
                if (NPT_FAILED(PLT_XmlHelper::GetChild(node, child, i)))
                    continue;
                
                if (!child->GetTag().Compare("InstanceID", true)) {
                    // extract the "val" attribute value
                    NPT_String value;
                    if (NPT_SUCCEEDED(PLT_XmlHelper::GetAttribute(child, "val", value)) &&
                        !value.Compare("0")) {
                        instance = child;
                        break;
                    }
                }
            }
            
            // did we find an instance with id = 0 ?
            if (instance != NULL) {
                // all the children of the Instance node are state variables
                for (NPT_Cardinal j=0; j<instance->GetChildren().GetItemCount(); j++) {
                    NPT_XmlElementNode* var_node;
                    if (NPT_FAILED(PLT_XmlHelper::GetChild(instance, var_node, j)))
                        continue;
                    
                    // look for the state variable in this service
                    const NPT_String* value = var_node->GetAttribute("val");
                    PLT_StateVariable* var = var_service->FindStateVariable(var_node->GetTag());
                    if (value != NULL && var != NULL) {
                        // get the value and set the state variable
                        // if it succeeded, add it to the list of vars we'll event
                        if (NPT_SUCCEEDED(var->SetValue(*value))) {
                            vars.Add(var);
                            NPT_LOG_FINE_2("LastChange var change for (%s): %s", 
                                           (const char*)var->GetName(), 
                                           (const char*)var->GetValue());
                        }
                    }
                }
            }
        }
        delete xml;
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::ProcessEventNotification
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::ProcessEventNotification(PLT_EventSubscriberReference subscriber,
                                        PLT_EventNotification*       notification,
                                        NPT_List<PLT_StateVariable*> &vars)
{
    NPT_XmlElementNode* xml = NULL;
    PLT_Service* service = subscriber->GetService();
    PLT_DeviceData* device  = service->GetDevice();

    NPT_String uuid = device->GetUUID();
    NPT_String service_id = service->GetServiceID();

    // callback uri for this sub
    NPT_String callback_uri = "/" + uuid + "/" + service_id;

    if (notification->m_RequestUrl.GetPath().Compare(callback_uri, true)) {
        NPT_CHECK_LABEL_WARNING(NPT_FAILURE, failure);
    }

    // if the sequence number is less than our current one, we got it out of order
    // so we disregard it
    if (subscriber->GetEventKey() && notification->m_EventKey < subscriber->GetEventKey()) {
        NPT_CHECK_LABEL_WARNING(NPT_FAILURE, failure);
    }

    // parse body
    if (NPT_FAILED(PLT_XmlHelper::Parse(notification->m_XmlBody, xml))) {
        NPT_CHECK_LABEL_WARNING(NPT_FAILURE, failure);
    }

    // check envelope
    if (xml->GetTag().Compare("propertyset", true)) {
        NPT_CHECK_LABEL_WARNING(NPT_FAILURE, failure);
    }

    // check property set
    // keep a vector of the state variables that changed
    NPT_XmlElementNode* property;
    PLT_StateVariable*  var;
    for (NPT_List<NPT_XmlNode*>::Iterator children = xml->GetChildren().GetFirstItem();
         children;
         children++) {
        NPT_XmlElementNode* child = (*children)->AsElementNode();
        if (!child) continue;

        // check property
        if (child->GetTag().Compare("property", true)) continue;

        if (NPT_FAILED(PLT_XmlHelper::GetChild(child, property))) {
            NPT_CHECK_LABEL_WARNING(NPT_FAILURE, failure);
        }

        var = service->FindStateVariable(property->GetTag());
        if (var == NULL) continue;

        if (NPT_FAILED(var->SetValue(property->GetText()?*property->GetText():""))) {
            NPT_CHECK_LABEL_WARNING(NPT_FAILURE, failure);
        }

        vars.Add(var);
    }

    // update sequence
    subscriber->SetEventKey(notification->m_EventKey);

    // Look if a state variable LastChange was received and decompose it into
    // independent state variable updates
    DecomposeLastChangeVar(vars);
    
    return NPT_SUCCESS;

failure:
    NPT_LOG_SEVERE("CtrlPoint failed to process event notification");
    delete xml;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::AddPendingEventNotification
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::AddPendingEventNotification(PLT_EventNotification *notification)
{
    // Only keep a maximum of 20 pending notifications
    while (m_PendingNotifications.GetItemCount() > 20) {
        PLT_EventNotification *garbage = NULL;
        m_PendingNotifications.PopHead(garbage);
        delete garbage;
    }

    m_PendingNotifications.Add(notification);
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::ProcessPendingEventNotifications
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::ProcessPendingEventNotifications()
{
    NPT_Cardinal count = m_PendingNotifications.GetItemCount();
    while (count--) {
        NPT_List<PLT_StateVariable*> vars;
        PLT_Service *service = NULL;
        PLT_EventNotification *notification;

        if (NPT_SUCCEEDED(m_PendingNotifications.PopHead(notification))) {
            PLT_EventSubscriberReference sub;

            // look for the subscriber with that sid
            if (NPT_FAILED(NPT_ContainerFind(m_Subscribers,
                                             PLT_EventSubscriberFinderBySID(notification->m_SID),
                                             sub))) {
                m_PendingNotifications.Add(notification);
                continue;
            }

            // keep track of service for listeners later
            service = sub->GetService();

            // Reprocess notification
            NPT_LOG_WARNING_1("Reprocessing delayed notification for subscriber", (const char*)notification->m_SID);
            NPT_Result result = ProcessEventNotification(sub, notification, vars);
            delete notification;
            
            if (NPT_FAILED(result)) continue;
        }
        
        // notify listeners
        if (service && vars.GetItemCount()) {
            m_ListenerList.Apply(PLT_CtrlPointListenerOnEventNotifyIterator(service, &vars));
        }
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::ProcessHttpNotify
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::ProcessHttpNotify(const NPT_HttpRequest&        request,
                                 const NPT_HttpRequestContext& context,
                                 NPT_HttpResponse&             response)
{
    NPT_COMPILER_UNUSED(context);

    NPT_AutoLock lock(m_Lock);
    
    NPT_List<PLT_StateVariable*> vars;
    PLT_Service* service = NULL;
    PLT_EventSubscriberReference sub;
    NPT_Result result;

    PLT_LOG_HTTP_MESSAGE(NPT_LOG_LEVEL_FINER, "PLT_CtrlPoint::ProcessHttpNotify:", request);

    // Create notification from request
    PLT_EventNotification* notification = PLT_EventNotification::Parse(request, context, response);
    NPT_CHECK_POINTER_LABEL_WARNING(notification, bad_request);

    // Give a last change to process pending notifications before throwing them out
    // by AddPendingNotification
    ProcessPendingEventNotifications();

    // look for the subscriber with that sid
    if (NPT_FAILED(NPT_ContainerFind(m_Subscribers,
                                     PLT_EventSubscriberFinderBySID(notification->m_SID),
                                     sub))) {
        NPT_LOG_WARNING_1("Subscriber %s not found, delaying notification process.\n", (const char*)notification->m_SID);
        AddPendingEventNotification(notification);
        return NPT_SUCCESS;
    }

    // Process notification for subscriber
    service = sub->GetService();
    result  = ProcessEventNotification(sub, notification, vars);
    delete notification;
    
    NPT_CHECK_LABEL_WARNING(result, bad_request);

    // Notify listeners
    if (vars.GetItemCount()) {
        m_ListenerList.Apply(PLT_CtrlPointListenerOnEventNotifyIterator(service, &vars));
    }

    return NPT_SUCCESS;

bad_request:
    NPT_LOG_SEVERE("CtrlPoint received bad event notify request\r\n");
    if (response.GetStatusCode() == 200) {
        response.SetStatus(412, "Precondition Failed");
    }
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::ProcessSsdpSearchResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::ProcessSsdpSearchResponse(NPT_Result                    res, 
                                         const NPT_HttpRequestContext& context, 
                                         NPT_HttpResponse*             response)
{
    NPT_CHECK_SEVERE(res);
    NPT_CHECK_POINTER_SEVERE(response);

    NPT_String ip_address = context.GetRemoteAddress().GetIpAddress().ToString();
    NPT_String protocol   = response->GetProtocol();
    
    NPT_String prefix = NPT_String::Format("PLT_CtrlPoint::ProcessSsdpSearchResponse from %s:%d",
        (const char*)context.GetRemoteAddress().GetIpAddress().ToString() , 
        context.GetRemoteAddress().GetPort());
    PLT_LOG_HTTP_MESSAGE(NPT_LOG_LEVEL_FINER, prefix, response);
    
    // any 2xx responses are ok
    if (response->GetStatusCode()/100 == 2) {
        const NPT_String* st  = response->GetHeaders().GetHeaderValue("st");
        const NPT_String* usn = response->GetHeaders().GetHeaderValue("usn");
        const NPT_String* ext = response->GetHeaders().GetHeaderValue("ext");
        NPT_CHECK_POINTER_SEVERE(st);
        NPT_CHECK_POINTER_SEVERE(usn);
        NPT_CHECK_POINTER_SEVERE(ext);
        
        NPT_String uuid;
        // if we get an advertisement other than uuid
        // verify it's formatted properly
        if (usn != st) {
            char tmp_uuid[200];
            char tmp_st[200];
            int  ret;
            // FIXME: We can't use sscanf directly!
            ret = sscanf(((const char*)*usn)+5, "%199[^::]::%199s",
                tmp_uuid, 
                tmp_st);
            if (ret != 2)
                return NPT_FAILURE;
            
            if (st->Compare(tmp_st, true))
                return NPT_FAILURE;
            
            uuid = tmp_uuid;
        } else {
            uuid = ((const char*)*usn)+5;
        }
        
        if (m_UUIDsToIgnore.Find(NPT_StringFinder(uuid))) {
            NPT_LOG_FINE_1("CtrlPoint received a search response from ourselves (%s)\n", (const char*)uuid);
            return NPT_SUCCESS;
        }

        return ProcessSsdpMessage(*response, context, uuid);    
    }
    
    return NPT_FAILURE;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::OnSsdpPacket
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::OnSsdpPacket(const NPT_HttpRequest&        request,
                            const NPT_HttpRequestContext& context)
{
    return ProcessSsdpNotify(request, context);
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::ProcessSsdpNotify
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::ProcessSsdpNotify(const NPT_HttpRequest&        request, 
                                 const NPT_HttpRequestContext& context)
{
    // get the address of who sent us some data back
    NPT_String ip_address = context.GetRemoteAddress().GetIpAddress().ToString();
    NPT_String method     = request.GetMethod();
    NPT_String uri        = request.GetUrl().GetPath(true);
    NPT_String protocol   = request.GetProtocol();

    if (method.Compare("NOTIFY") == 0) {
        const NPT_String* nts = PLT_UPnPMessageHelper::GetNTS(request);
        const NPT_String* nt  = PLT_UPnPMessageHelper::GetNT(request);
        const NPT_String* usn = PLT_UPnPMessageHelper::GetUSN(request);

        NPT_String prefix = NPT_String::Format("PLT_CtrlPoint::ProcessSsdpNotify from %s:%d (%s)",
            context.GetRemoteAddress().GetIpAddress().ToString().GetChars(), 
            context.GetRemoteAddress().GetPort(),
            usn?usn->GetChars():"unknown");
        PLT_LOG_HTTP_MESSAGE(NPT_LOG_LEVEL_FINER, prefix, request);

        if ((uri.Compare("*") != 0) || (protocol.Compare("HTTP/1.1") != 0))
            return NPT_FAILURE;
        
        NPT_CHECK_POINTER_SEVERE(nts);
        NPT_CHECK_POINTER_SEVERE(nt);
        NPT_CHECK_POINTER_SEVERE(usn);

        NPT_String uuid;
        // if we get an advertisement other than uuid
        // verify it's formatted properly
        if (*usn != *nt) {
            char tmp_uuid[200];
            char tmp_nt[200];
            int  ret;
            //FIXME: no sscanf!
            ret = sscanf(((const char*)*usn)+5, "%199[^::]::%199s",
                tmp_uuid, 
                tmp_nt);
            if (ret != 2)
                return NPT_FAILURE;
            
            if (nt->Compare(tmp_nt, true))
                return NPT_FAILURE;
            
            uuid = tmp_uuid;
        } else {
            uuid = ((const char*)*usn)+5;
        }

        if (m_UUIDsToIgnore.Find(NPT_StringFinder(uuid))) {
            NPT_LOG_FINE_1("Received a NOTIFY request from ourselves (%s)\n", (const char*)uuid);
            return NPT_SUCCESS;
        }

        // if it's a byebye, remove the device and return right away
        if (nts->Compare("ssdp:byebye", true) == 0) {
            NPT_LOG_INFO_1("Received a byebye NOTIFY request from %s\n", (const char*)uuid);

            NPT_AutoLock lock(m_Lock);

            // look for root device
            PLT_DeviceDataReference root_device;
            FindDevice(uuid, root_device, true);
                
            if (!root_device.IsNull()) RemoveDevice(root_device);
            return NPT_SUCCESS;
        }
        
        return ProcessSsdpMessage(request, context, uuid);
    }
    
    return NPT_FAILURE;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::AddDevice
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::AddDevice(PLT_DeviceDataReference& data)
{
    NPT_AutoLock lock(m_Lock);

    return NotifyDeviceReady(data);
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::NotifyDeviceReady
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::NotifyDeviceReady(PLT_DeviceDataReference& data)
{
    m_ListenerList.Apply(PLT_CtrlPointListenerOnDeviceAddedIterator(data));

    /* recursively add embedded devices */
    NPT_Array<PLT_DeviceDataReference> embedded_devices = 
        data->GetEmbeddedDevices();
    for (NPT_Cardinal i=0;i<embedded_devices.GetItemCount();i++) {
        NotifyDeviceReady(embedded_devices[i]);
    }
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::RemoveDevice
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::RemoveDevice(PLT_DeviceDataReference& data)
{
    NPT_AutoLock lock(m_Lock);

    NotifyDeviceRemoved(data);
    CleanupDevice(data);
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::NotifyDeviceRemoved
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::NotifyDeviceRemoved(PLT_DeviceDataReference& data)
{
    m_ListenerList.Apply(PLT_CtrlPointListenerOnDeviceRemovedIterator(data));

    /* recursively add embedded devices */
    NPT_Array<PLT_DeviceDataReference> embedded_devices = 
        data->GetEmbeddedDevices();
    for (NPT_Cardinal i=0;i<embedded_devices.GetItemCount();i++) {
        NotifyDeviceRemoved(embedded_devices[i]);
    }
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::CleanupDevice
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::CleanupDevice(PLT_DeviceDataReference& data)
{
    if (data.IsNull()) return NPT_ERROR_INVALID_PARAMETERS;
    
    NPT_LOG_INFO_1("Removing %s from device list\n", (const char*)data->GetUUID());
    
    // Note: This must take the lock prior to being called
    // we can't take the lock here because this function
    // will be recursively called if device contains embedded devices
    
    /* recursively remove embedded devices */
    NPT_Array<PLT_DeviceDataReference> embedded_devices = data->GetEmbeddedDevices();
    for (NPT_Cardinal i=0;i<embedded_devices.GetItemCount();i++) {
        CleanupDevice(embedded_devices[i]);
    }

    /* remove from list */
    m_RootDevices.Remove(data);

    /* unsubscribe from services */
    data->m_Services.Apply(PLT_EventSubscriberRemoverIterator(this));

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::ProcessSsdpMessage
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::ProcessSsdpMessage(const NPT_HttpMessage&        message, 
                                  const NPT_HttpRequestContext& context,
                                  NPT_String&                   uuid)
{
    NPT_COMPILER_UNUSED(context);

    NPT_AutoLock lock(m_Lock);

    // check if we should ignore our own UUID
    if (m_UUIDsToIgnore.Find(NPT_StringFinder(uuid))) return NPT_SUCCESS;

    const NPT_String* url = PLT_UPnPMessageHelper::GetLocation(message);
    NPT_CHECK_POINTER_SEVERE(url);

    // Fix for Connect360 which uses localhost in device description url
    NPT_HttpUrl location(*url);
    if (location.GetHost().ToLowercase() == "localhost" ||
        location.GetHost().ToLowercase() == "127.0.0.1") {
        location.SetHost(context.GetRemoteAddress().GetIpAddress().ToString());
    }
    
    // be nice and assume a default lease time if not found even though it's required
    NPT_TimeInterval leasetime;
    if (NPT_FAILED(PLT_UPnPMessageHelper::GetLeaseTime(message, leasetime))) {
        leasetime = *PLT_Constants::GetInstance().GetDefaultSubscribeLease();
    }
    
    // check if device (or embedded device) is already known
    PLT_DeviceDataReference data;
    if (NPT_SUCCEEDED(FindDevice(uuid, data))) {  
        
//        // in case we missed the byebye and the device description has changed (ip or port)
//        // reset base and assumes device is the same (same number of services and embedded devices)
//        // FIXME: The right way is to remove the device and rescan it though but how do we know it changed?
//        PLT_DeviceReadyIterator device_tester;
//        if (NPT_SUCCEEDED(device_tester(data)) && data->GetDescriptionUrl().Compare(location.ToString(), true)) {
//            NPT_LOG_INFO_2("Old device \"%s\" detected @ new location %s", 
//                (const char*)data->GetFriendlyName(), 
//                (const char*)location.ToString());
//            data->SetURLBase(location);
//        }

        // renew expiration time
        data->SetLeaseTime(leasetime);
        NPT_LOG_FINE_1("Device \"%s\" expiration time renewed..", 
            (const char*)data->GetFriendlyName());

        return NPT_SUCCESS;
    }

    // start inspection
    return InspectDevice(location, uuid, leasetime);
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::InspectDevice
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::InspectDevice(const NPT_HttpUrl& location, 
                             const char*        uuid, 
                             NPT_TimeInterval   leasetime)
{
    NPT_AutoLock lock(m_Lock);

    // check if already inspecting device
    NPT_String pending_uuid;
    if (NPT_SUCCEEDED(NPT_ContainerFind(m_PendingInspections,
                                        NPT_StringFinder(uuid),
                                        pending_uuid))) {
        return NPT_SUCCESS;
    }
    
    NPT_LOG_INFO_2("Inspecting device \"%s\" detected @ %s", 
        uuid, 
        (const char*)location.ToString());

    if (!location.IsValid()) {
        NPT_LOG_INFO_1("Invalid device description url: %s", 
            (const char*) location.ToString());
        return NPT_FAILURE;
    }

    // remember that we're now inspecting the device
    m_PendingInspections.Add(uuid);
        
    // Start a task to retrieve the description
    PLT_CtrlPointGetDescriptionTask* task = new PLT_CtrlPointGetDescriptionTask(
        location,
        this,
        leasetime,
        uuid);

    // Add a delay to make sure that we received late NOTIFY bye-bye
    NPT_TimeInterval delay(.5f);
    m_TaskManager.StartTask(task, &delay);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::FetchDeviceSCPDs
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::FetchDeviceSCPDs(PLT_CtrlPointGetSCPDsTask* task,
                                PLT_DeviceDataReference&   device, 
                                NPT_Cardinal               level)
{
    if (level == 5 && device->m_EmbeddedDevices.GetItemCount()) {
        NPT_LOG_FATAL("Too many embedded devices depth! ");
        return NPT_FAILURE;
    }

    ++level;

    // fetch embedded devices services scpds first
    for (NPT_Cardinal i = 0;
         i<device->m_EmbeddedDevices.GetItemCount();
         i++) {
         NPT_CHECK_SEVERE(FetchDeviceSCPDs(task, device->m_EmbeddedDevices[i], level));
    }

    // Get SCPD of device services now and bail right away if one fails
    return device->m_Services.ApplyUntil(
        PLT_AddGetSCPDRequestIterator(*task, device),
        NPT_UntilResultNotEquals(NPT_SUCCESS));
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::ProcessGetDescriptionResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::ProcessGetDescriptionResponse(NPT_Result                    res, 
                                             const NPT_HttpRequest&        request,
                                             const NPT_HttpRequestContext& context,
                                             NPT_HttpResponse*             response,
                                             NPT_TimeInterval              leasetime,
                                             NPT_String                    uuid)
{    
    NPT_COMPILER_UNUSED(request);

    NPT_AutoLock lock(m_Lock);
    
    PLT_CtrlPointGetSCPDsTask* task = NULL;
    NPT_String desc;
    PLT_DeviceDataReference root_device;
    PLT_DeviceDataReference device;

    // Add a delay, some devices need it (aka Rhapsody)
    NPT_TimeInterval delay(0.1f);

    NPT_String prefix = NPT_String::Format("PLT_CtrlPoint::ProcessGetDescriptionResponse @ %s (result = %d, status = %d)",
        (const char*)request.GetUrl().ToString(),
        res,
        response?response->GetStatusCode():0);

    // Remove pending inspection
    m_PendingInspections.Remove(uuid);

    // verify response was ok
    NPT_CHECK_LABEL_FATAL(res, bad_response);
    NPT_CHECK_POINTER_LABEL_FATAL(response, bad_response);

    PLT_LOG_HTTP_MESSAGE(NPT_LOG_LEVEL_FINER, prefix, response);

    // get response body
    res = PLT_HttpHelper::GetBody(*response, desc);
    NPT_CHECK_LABEL_SEVERE(res, bad_response);

    // create new root device
    NPT_CHECK_FATAL(PLT_DeviceData::SetDescription(root_device, leasetime, request.GetUrl(), desc, context));

    // make sure root device was not previously queried
    if (NPT_FAILED(FindDevice(root_device->GetUUID(), device))) {
        m_RootDevices.Add(root_device);
            
        NPT_LOG_INFO_2("Device \"%s\" is now known as \"%s\"", 
            (const char*)root_device->GetUUID(), 
            (const char*)root_device->GetFriendlyName());

        // create one single task to fetch all scpds one after the other
        task = new PLT_CtrlPointGetSCPDsTask(this, root_device);
        NPT_CHECK_LABEL_SEVERE(FetchDeviceSCPDs(task, root_device, 0),
                               bad_response);

        // if device has embedded devices, we want to delay fetching scpds
        // just in case there's a chance all the initial NOTIFY bye-bye have
        // not all been received yet which would cause to remove the devices
        // as we're adding them
        if (root_device->m_EmbeddedDevices.GetItemCount() > 0) delay = 1.f;
        m_TaskManager.StartTask(task, &delay);
    }

    return NPT_SUCCESS;

bad_response:
    NPT_LOG_SEVERE_2("Bad Description response @ %s: %s", 
        (const char*)request.GetUrl().ToString(),
        (const char*)desc);

    if (task) delete task;
    return res;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::ProcessGetSCPDResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::ProcessGetSCPDResponse(NPT_Result                    res, 
                                      const NPT_HttpRequest&        request,
                                      const NPT_HttpRequestContext& context,
                                      NPT_HttpResponse*             response,
                                      PLT_DeviceDataReference&      device)
{
    NPT_COMPILER_UNUSED(context);

    NPT_AutoLock lock(m_Lock);
    
    PLT_DeviceReadyIterator device_tester;
    NPT_String              scpd;
    PLT_DeviceDataReference root_device;
    PLT_Service*            service;

    NPT_String prefix = NPT_String::Format("PLT_CtrlPoint::ProcessGetSCPDResponse for a service of device \"%s\" @ %s (result = %d, status = %d)", 
        (const char*)device->GetFriendlyName(), 
        (const char*)request.GetUrl().ToString(),
        res,
        response?response->GetStatusCode():0);

    // verify response was ok
    NPT_CHECK_LABEL_FATAL(res, bad_response);
    NPT_CHECK_POINTER_LABEL_FATAL(response, bad_response);

    PLT_LOG_HTTP_MESSAGE(NPT_LOG_LEVEL_FINER, prefix, response);

    // make sure root device hasn't disappeared
    NPT_CHECK_LABEL_WARNING(FindDevice(device->GetUUID(), root_device, true),
                            bad_response);

    res = device->FindServiceBySCPDURL(request.GetUrl().ToRequestString(), service);
    NPT_CHECK_LABEL_SEVERE(res, bad_response);

    // get response body
    res = PLT_HttpHelper::GetBody(*response, scpd);
    NPT_CHECK_LABEL_FATAL(res, bad_response);
        
    // set the service scpd
    res = service->SetSCPDXML(scpd);
    NPT_CHECK_LABEL_SEVERE(res, bad_response);

    // if root device is ready, notify listeners about it and embedded devices
    if (NPT_SUCCEEDED(device_tester(root_device))) {
        AddDevice(root_device);
    }
    
    return NPT_SUCCESS;

bad_response:
    NPT_LOG_SEVERE_2("Bad SCPD response for device \"%s\":%s", 
        (const char*)device->GetFriendlyName(),
        (const char*)scpd);

    if (!root_device.IsNull()) RemoveDevice(root_device);
    return res;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::RenewSubscriber
+---------------------------------------------------------------------*/
PLT_ThreadTask*
PLT_CtrlPoint::RenewSubscriber(PLT_EventSubscriberReference subscriber)
{
    NPT_AutoLock lock(m_Lock);

    PLT_DeviceDataReference root_device;
    if (NPT_FAILED(FindDevice(subscriber->GetService()->GetDevice()->GetUUID(),
                              root_device,
                              true))) {
        return NULL;
    }

    NPT_LOG_FINE_3("Renewing subscriber \"%s\" for service \"%s\" of device \"%s\"", 
        (const char*)subscriber->GetSID(),
        (const char*)subscriber->GetService()->GetServiceID(),
        (const char*)subscriber->GetService()->GetDevice()->GetFriendlyName());

    // create the request
    NPT_HttpRequest* request = new NPT_HttpRequest(
        subscriber->GetService()->GetEventSubURL(true),
        "SUBSCRIBE", 
        NPT_HTTP_PROTOCOL_1_1);

    PLT_UPnPMessageHelper::SetSID(*request, subscriber->GetSID());
    PLT_UPnPMessageHelper::SetTimeOut(*request, 
        (NPT_Int32)PLT_Constants::GetInstance().GetDefaultSubscribeLease()->ToSeconds());

    // Prepare the request
    // create a task to post the request
    return new PLT_CtrlPointSubscribeEventTask(
        request,
        this, 
        root_device,
        subscriber->GetService());
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::Subscribe
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::Subscribe(PLT_Service* service, 
                         bool         cancel, 
                         void*        userdata)
{
    NPT_AutoLock lock(m_Lock);
    
    if (m_Aborted) NPT_CHECK_WARNING(NPT_ERROR_INVALID_STATE);

    NPT_HttpRequest* request = NULL;

    // make sure service is subscribable
    if (!service->IsSubscribable()) return NPT_FAILURE;

    // event url
    NPT_HttpUrl url(service->GetEventSubURL(true));

    // look for the corresponding root device & sub
    PLT_DeviceDataReference root_device;
    PLT_EventSubscriberReference sub;
    NPT_CHECK_WARNING(FindDevice(service->GetDevice()->GetUUID(),
                                 root_device,
                                 true));

    // look for the subscriber with that service to decide if it's a renewal or not
    NPT_ContainerFind(m_Subscribers, 
                      PLT_EventSubscriberFinderByService(service), 
                      sub);

    if (cancel == false) {
        // renewal?
        if (!sub.IsNull()) {
            PLT_ThreadTask* task = RenewSubscriber(sub);
            return m_TaskManager.StartTask(task);
        }

        NPT_LOG_INFO_2("Subscribing to service \"%s\" of device \"%s\"",
            (const char*)service->GetServiceID(),
            (const char*)service->GetDevice()->GetFriendlyName());

        // prepare the callback url
        NPT_String uuid         = service->GetDevice()->GetUUID();
        NPT_String service_id   = service->GetServiceID();
        NPT_String callback_uri = "/" + uuid + "/" + service_id;

        // create the request
        request = new NPT_HttpRequest(url, "SUBSCRIBE", NPT_HTTP_PROTOCOL_1_1);
        // specify callback url using ip of interface used when 
        // retrieving device description
        NPT_HttpUrl callbackUrl(
            service->GetDevice()->m_LocalIfaceIp.ToString(), 
            m_EventHttpServer->GetPort(), 
            callback_uri);

        // set the required headers for a new subscription
        PLT_UPnPMessageHelper::SetNT(*request, "upnp:event");
        PLT_UPnPMessageHelper::SetCallbacks(*request, 
            "<" + callbackUrl.ToString() + ">");
        PLT_UPnPMessageHelper::SetTimeOut(*request, 
            (NPT_Int32)PLT_Constants::GetInstance().GetDefaultSubscribeLease()->ToSeconds());
    } else {
        NPT_LOG_INFO_3("Unsubscribing subscriber \"%s\" for service \"%s\" of device \"%s\"",
            (const char*)(!sub.IsNull()?sub->GetSID().GetChars():"unknown"),
            (const char*)service->GetServiceID(),
            (const char*)service->GetDevice()->GetFriendlyName());        
        
        // cancellation
        if (sub.IsNull()) return NPT_FAILURE;

        // create the request
        request = new NPT_HttpRequest(url, "UNSUBSCRIBE", NPT_HTTP_PROTOCOL_1_1);
        PLT_UPnPMessageHelper::SetSID(*request, sub->GetSID());

        // remove from list now
        m_Subscribers.Remove(sub, true);
    }

    // verify we have request to send just in case
    NPT_CHECK_POINTER_FATAL(request);

    // Prepare the request
    // create a task to post the request
    PLT_ThreadTask* task = new PLT_CtrlPointSubscribeEventTask(
        request,
        this, 
		root_device,
        service, 
        userdata);
    m_TaskManager.StartTask(task);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::ProcessSubscribeResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::ProcessSubscribeResponse(NPT_Result                    res, 
                                        const NPT_HttpRequest&        request, 
                                        const NPT_HttpRequestContext& context,
                                        NPT_HttpResponse*             response,
                                        PLT_Service*                  service,
                                        void*                  /* userdata */)
{
    NPT_COMPILER_UNUSED(context);

    NPT_AutoLock lock(m_Lock);
    
    const NPT_String*    sid = NULL;
    NPT_Int32            seconds;
    PLT_EventSubscriberReference sub;
    bool                 subscription = (request.GetMethod().ToUppercase() == "SUBSCRIBE");

    NPT_String prefix = NPT_String::Format("PLT_CtrlPoint::ProcessSubscribeResponse %ubscribe for service \"%s\" (result = %d, status code = %d)", 
        (const char*)subscription?"S":"Uns",
        (const char*)service->GetServiceID(),
        res,
        response?response->GetStatusCode():0);
    PLT_LOG_HTTP_MESSAGE(NPT_LOG_LEVEL_FINER, prefix, response);

    // if there's a failure or it's a response to a cancellation
    // we get out (any 2xx status code ok)
    if (NPT_FAILED(res) || response == NULL || response->GetStatusCode()/100 != 2) {
        goto failure;
    }
        
    if (subscription) {
        if (!(sid = PLT_UPnPMessageHelper::GetSID(*response)) || 
            NPT_FAILED(PLT_UPnPMessageHelper::GetTimeOut(*response, seconds))) {
            NPT_CHECK_LABEL_SEVERE(res = NPT_ERROR_INVALID_SYNTAX, failure);
        }

        // Look for subscriber
        NPT_ContainerFind(m_Subscribers, 
            PLT_EventSubscriberFinderBySID(*sid), 
            sub);
        
        NPT_LOG_INFO_5("%s subscriber \"%s\" for service \"%s\" of device \"%s\" (timeout = %d)",
                       !sub.IsNull()?"Updating timeout for":"Creating new",
                       (const char*)*sid,
                       (const char*)service->GetServiceID(),
                       (const char*)service->GetDevice()->GetFriendlyName(),
                       seconds);
    
        // create new subscriber if sid never seen before
        // or update subscriber expiration otherwise
        if (sub.IsNull()) {
            sub = new PLT_EventSubscriber(&m_TaskManager, service, *sid, seconds);
            m_Subscribers.Add(sub);
        } else {
            sub->SetTimeout(seconds);
        }

        // Process any pending notifcations for that subscriber we got a bit too early
        ProcessPendingEventNotifications();
        
        return NPT_SUCCESS;
    }

    goto remove_sub;

failure:
    NPT_LOG_SEVERE_4("%subscription failed of sub \"%s\" for service \"%s\" of device \"%s\"", 
        (const char*)subscription?"S":"Uns",
        (const char*)(sid?*sid:"Unknown"),
        (const char*)service->GetServiceID(),
        (const char*)service->GetDevice()->GetFriendlyName());
    res = NPT_FAILED(res)?res:NPT_FAILURE;

remove_sub:
    // in case it was a renewal look for the subscriber with that service and remove it from the list
    if (NPT_SUCCEEDED(NPT_ContainerFind(m_Subscribers, 
                                        PLT_EventSubscriberFinderByService(service), 
                                        sub))) {
        m_Subscribers.Remove(sub);
    }

    return res;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::InvokeAction
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::InvokeAction(PLT_ActionReference& action, 
                            void*                userdata)
{
    if (m_Aborted) NPT_CHECK_WARNING(NPT_ERROR_INVALID_STATE);
    
    PLT_Service* service = action->GetActionDesc().GetService();
    
    // create the request
    NPT_HttpUrl url(service->GetControlURL(true));
    NPT_HttpRequest* request = new NPT_HttpRequest(url, "POST", NPT_HTTP_PROTOCOL_1_1);
    
    // create a memory stream for our request body
    NPT_MemoryStreamReference stream(new NPT_MemoryStream);
    action->FormatSoapRequest(*stream);

    // set the request body
    NPT_HttpEntity* entity = NULL;
    PLT_HttpHelper::SetBody(*request, (NPT_InputStreamReference)stream, &entity);

    entity->SetContentType("text/xml; charset=\"utf-8\"");
    NPT_String service_type = service->GetServiceType();
    NPT_String action_name  = action->GetActionDesc().GetName();
    request->GetHeaders().SetHeader("SOAPAction", "\"" + service_type + "#" + action_name + "\"");

    // create a task to post the request
    PLT_CtrlPointInvokeActionTask* task = new PLT_CtrlPointInvokeActionTask(
        request,
        this, 
        action, 
        userdata);

    // queue the request
    m_TaskManager.StartTask(task);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::ProcessActionResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::ProcessActionResponse(NPT_Result                    res,
                                     const NPT_HttpRequest&        request,
                                     const NPT_HttpRequestContext& /*context*/,
                                     NPT_HttpResponse*             response,
                                     PLT_ActionReference&          action,
                                     void*                         userdata)
{
    NPT_String          service_type;
    NPT_String          str;
    NPT_XmlElementNode* xml = NULL;
    NPT_String          name;
    NPT_String          soap_action_name;
    NPT_XmlElementNode* soap_action_response;
    NPT_XmlElementNode* soap_body;
    NPT_XmlElementNode* fault;
    const NPT_String*   attr = NULL;
    PLT_ActionDesc&     action_desc = action->GetActionDesc();

    // reset the error code and desc
    action->SetError(0, "");

    // check context validity
    if (NPT_FAILED(res) || response == NULL) {
        PLT_Service* service = action_desc.GetService();
        NPT_LOG_WARNING_4("Failed to reach %s for %s.%s (%d)",
                          request.GetUrl().ToString().GetChars(),
                          service->GetDevice()->GetUUID().GetChars(),
                          service->GetServiceName().GetChars(),
                          res);
        goto failure;
    }

    PLT_LOG_HTTP_MESSAGE(NPT_LOG_LEVEL_FINER, "PLT_CtrlPoint::ProcessActionResponse:", response);

    NPT_LOG_FINER("Reading/Parsing Action Response Body...");
    if (NPT_FAILED(PLT_HttpHelper::ParseBody(*response, xml))) {
        goto failure;
    }

    NPT_LOG_FINER("Analyzing Action Response Body...");

    // read envelope
    if (xml->GetTag().Compare("Envelope", true))
        goto failure;

    // check namespace
    if (!xml->GetNamespace() || xml->GetNamespace()->Compare("http://schemas.xmlsoap.org/soap/envelope/"))
        goto failure;

    // check encoding
    attr = xml->GetAttribute("encodingStyle", "http://schemas.xmlsoap.org/soap/envelope/");
    if (!attr || attr->Compare("http://schemas.xmlsoap.org/soap/encoding/"))
        goto failure;

    // read action
    soap_body = PLT_XmlHelper::GetChild(xml, "Body");
    if (soap_body == NULL)
        goto failure;

    // check if an error occurred
    fault = PLT_XmlHelper::GetChild(soap_body, "Fault");
    if (fault != NULL) {
        // we have an error
        ParseFault(action, fault);
        goto failure;
    }

    if (NPT_FAILED(PLT_XmlHelper::GetChild(soap_body, soap_action_response)))
        goto failure;

    // verify action name is identical to SOAPACTION header
    if (soap_action_response->GetTag().Compare(action_desc.GetName() + "Response", true))
        goto failure;

    // verify namespace
    if (!soap_action_response->GetNamespace() ||
         soap_action_response->GetNamespace()->Compare(action_desc.GetService()->GetServiceType()))
         goto failure;

    // read all the arguments if any
    for (NPT_List<NPT_XmlNode*>::Iterator args = soap_action_response->GetChildren().GetFirstItem(); 
         args; 
         args++) {
        NPT_XmlElementNode* child = (*args)->AsElementNode();
        if (!child) continue;

        action->SetArgumentValue(child->GetTag(), child->GetText()?*child->GetText():"");
        if (NPT_FAILED(res)) goto failure; 
    }

    // create a buffer for our response body and call the service
    res = action->VerifyArguments(false);
    if (NPT_FAILED(res)) goto failure; 

    goto cleanup;

failure:
    // override res with failure if necessary
    if (NPT_SUCCEEDED(res)) res = NPT_FAILURE;
    // fallthrough

cleanup:
    {
        NPT_AutoLock lock(m_Lock);
        m_ListenerList.Apply(PLT_CtrlPointListenerOnActionResponseIterator(res, action, userdata));
    }
    
    delete xml;
    return res;
}

/*----------------------------------------------------------------------
|   PLT_CtrlPoint::ParseFault
+---------------------------------------------------------------------*/
NPT_Result
PLT_CtrlPoint::ParseFault(PLT_ActionReference& action,
                          NPT_XmlElementNode*  fault)
{
    NPT_XmlElementNode* detail = fault->GetChild("detail");
    if (detail == NULL) return NPT_FAILURE;

    NPT_XmlElementNode *upnp_error, *error_code, *error_desc;
    upnp_error = detail->GetChild("upnp_error");
	
	// WMP12 Hack
	if (upnp_error == NULL) {
        upnp_error = detail->GetChild("UPnPError", NPT_XML_ANY_NAMESPACE);
        if (upnp_error == NULL) return NPT_FAILURE;
	}

    error_code = upnp_error->GetChild("errorCode", NPT_XML_ANY_NAMESPACE);
    error_desc = upnp_error->GetChild("errorDescription", NPT_XML_ANY_NAMESPACE);
    NPT_Int32  code = 501;    
    NPT_String desc;
    if (error_code && error_code->GetText()) {
        NPT_String value = *error_code->GetText();
        value.ToInteger(code);
    }
    if (error_desc && error_desc->GetText()) {
        desc = *error_desc->GetText();
    }
    action->SetError(code, desc);
    return NPT_SUCCESS;
}
