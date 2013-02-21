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

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "PltService.h"
#include "PltDeviceHost.h"
#include "PltUPnP.h"
#include "PltUtilities.h"
#include "PltSsdp.h"
#include "PltHttpServer.h"
#include "PltVersion.h"

NPT_SET_LOCAL_LOGGER("platinum.core.devicehost")

/*----------------------------------------------------------------------
|   externals
+---------------------------------------------------------------------*/
extern NPT_UInt8 Platinum_120x120_jpg[16096];
extern NPT_UInt8 Platinum_120x120_png[26577];
extern NPT_UInt8 Platinum_48x48_jpg[3041];
extern NPT_UInt8 Platinum_48x48_png[4681];

/*----------------------------------------------------------------------
|   PLT_DeviceHost::PLT_DeviceHost
+---------------------------------------------------------------------*/
PLT_DeviceHost::PLT_DeviceHost(const char*  description_path /* = "/" */, 
                               const char*  uuid             /* = "" */,
                               const char*  device_type      /* = "" */,
                               const char*  friendly_name    /* = "" */,
                               bool         show_ip          /* = false */,
                               NPT_UInt16   port             /* = 0 */,
                               bool         port_rebind      /* = false */) :
    PLT_DeviceData(NPT_HttpUrl(NULL, 0, description_path), 
                   uuid, 
                   *PLT_Constants::GetInstance().GetDefaultDeviceLease(), 
                   device_type, 
                   friendly_name), 
    m_HttpServer(NULL),
    m_Broadcast(false),
    m_Port(port),
    m_PortRebind(port_rebind),
    m_ByeByeFirst(false)
{
    if (show_ip) {
        NPT_List<NPT_IpAddress> ips;
        PLT_UPnPMessageHelper::GetIPAddresses(ips);
        if (ips.GetItemCount()) {
            m_FriendlyName += " (" + ips.GetFirstItem()->ToString() + ")";
        }
    }
}
    
/*----------------------------------------------------------------------
|   PLT_DeviceHost::~PLT_DeviceHost
+---------------------------------------------------------------------*/
PLT_DeviceHost::~PLT_DeviceHost() 
{
}

/*----------------------------------------------------------------------
|   PLT_DeviceHost::AddIcon
+---------------------------------------------------------------------*/
NPT_Result 
PLT_DeviceHost::AddIcon(const PLT_DeviceIcon& icon,  
                        const char*           fileroot,
                        const char*           urlroot /* = "/" */)
{
    // verify the url of the icon starts with the url root
    if (!icon.m_UrlPath.StartsWith(urlroot)) return NPT_ERROR_INVALID_PARAMETERS;
    
    NPT_HttpFileRequestHandler* icon_handler = new NPT_HttpFileRequestHandler(urlroot, fileroot);
    m_HttpServer->AddRequestHandler(icon_handler, icon.m_UrlPath, false, true);
    return m_Icons.Add(icon);
}

/*----------------------------------------------------------------------
|   PLT_DeviceHost::AddIcon
+---------------------------------------------------------------------*/
NPT_Result 
PLT_DeviceHost::AddIcon(const PLT_DeviceIcon& icon, 
                        const void*           data, 
                        NPT_Size              size, 
                        bool                  copy /* = true */)
{
    NPT_HttpStaticRequestHandler* icon_handler = 
        new NPT_HttpStaticRequestHandler(
			data, 
			size,
			icon.m_MimeType,
			copy);
    m_HttpServer->AddRequestHandler(icon_handler, icon.m_UrlPath, false, true);
    return m_Icons.Add(icon);
}

/*----------------------------------------------------------------------
|   PLT_DeviceHost::SetupIcons
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceHost::SetupIcons()
{
	/*if (m_Icons.GetItemCount() == 0) {
		AddIcon(
			PLT_DeviceIcon("image/jpeg", 120, 120, 24, "/images/platinum-120x120.jpg"),
			Platinum_120x120_jpg, sizeof(Platinum_120x120_jpg), false);
		AddIcon(
			PLT_DeviceIcon("image/jpeg", 48, 48, 24, "/images/platinum-48x48.jpg"),
			Platinum_48x48_jpg, sizeof(Platinum_48x48_jpg), false);
		AddIcon(
			PLT_DeviceIcon("image/png", 120, 120, 24, "/images/platinum-120x120.png"),
			Platinum_120x120_png, sizeof(Platinum_120x120_png), false);
		AddIcon(
			PLT_DeviceIcon("image/png", 48, 48, 24, "/images/platinum-48x48.png"),
			Platinum_48x48_png, sizeof(Platinum_48x48_png), false);
	}*/
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_DeviceHost::SetupDevice
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceHost::SetupDevice()
{
    NPT_CHECK_FATAL(SetupServices());
    NPT_CHECK_WARNING(SetupIcons());
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_DeviceHost::Start
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceHost::Start(PLT_SsdpListenTask* task)
{
    m_HttpServer = new PLT_HttpServer(NPT_IpAddress::Any, m_Port, m_PortRebind, 100); // limit to 100 clients max  

    // start the server
    NPT_CHECK_SEVERE(m_HttpServer->Start());

    // read back assigned port in case we passed 0 to randomly select one
    m_Port = m_HttpServer->GetPort();
    m_URLDescription.SetPort(m_Port);

    // callback to initialize the device
    NPT_CHECK_FATAL(SetupDevice());

    // all other requests including description document
    // and service control are dynamically handled
    m_HttpServer->AddRequestHandler(new PLT_HttpRequestHandler(this), "/", true, true);

    // we should not advertise right away
    // spec says randomly less than 100ms
    NPT_TimeInterval delay(((NPT_Int64)NPT_System::GetRandomInteger()%100)*1000000);

    // calculate when we should send another announcement
    NPT_Size leaseTime = (NPT_Size)GetLeaseTime().ToSeconds();
    NPT_TimeInterval repeat;
    repeat.SetSeconds(leaseTime?(int)((leaseTime >> 1) - 10):30);

    PLT_ThreadTask* announce_task = new PLT_SsdpDeviceAnnounceTask(
        this, 
        repeat, 
        m_ByeByeFirst, 
        m_Broadcast);
    m_TaskManager.StartTask(announce_task, &delay);

    // register ourselves as a listener for SSDP search requests
    task->AddListener(this);
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_DeviceHost::Stop
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceHost::Stop(PLT_SsdpListenTask* task)
{    
    // unregister ourselves as a listener for ssdp requests
    task->RemoveListener(this);

    // remove all our running tasks
    m_TaskManager.StopAllTasks();

    if (m_HttpServer) {
        // stop our internal http server
        m_HttpServer->Stop();
        delete m_HttpServer;
        m_HttpServer = NULL;

        // notify we're gone
        NPT_List<NPT_NetworkInterface*> if_list;
        PLT_UPnPMessageHelper::GetNetworkInterfaces(if_list, true);
        if_list.Apply(PLT_SsdpAnnounceInterfaceIterator(this, true, m_Broadcast));
        if_list.Apply(NPT_ObjectDeleter<NPT_NetworkInterface>());
    }
    
    PLT_DeviceData::Cleanup();
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_DeviceHost::Announce
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceHost::Announce(PLT_DeviceData*  device, 
                         NPT_HttpRequest& req, 
                         NPT_UdpSocket&   socket, 
                         bool             byebye)
{
    NPT_Result res = NPT_SUCCESS;

    NPT_LOG_FINER_3("Sending SSDP NOTIFY (%s) Request to %s with location:%s", 
        byebye?"ssdp:byebye":"ssdp:alive",
        (const char*)req.GetUrl().ToString(),
        (const char*)(PLT_UPnPMessageHelper::GetLocation(req)?*PLT_UPnPMessageHelper::GetLocation(req):""));
        
    if (byebye == false) {
        // get location URL based on ip address of interface
        PLT_UPnPMessageHelper::SetNTS(req, "ssdp:alive");
        PLT_UPnPMessageHelper::SetLeaseTime(req, device->GetLeaseTime());
        PLT_UPnPMessageHelper::SetServer(req, PLT_HTTP_DEFAULT_SERVER, false);
    } else {
        PLT_UPnPMessageHelper::SetNTS(req, "ssdp:byebye");
    }

    // target address
    NPT_IpAddress ip;
    if (NPT_FAILED(res = ip.ResolveName(req.GetUrl().GetHost()))) {
        return res;
    }
    NPT_SocketAddress addr(ip, req.GetUrl().GetPort());

    // upnp:rootdevice
    if (device->m_ParentUUID.IsEmpty()) {
        PLT_SsdpSender::SendSsdp(req,
            NPT_String("uuid:" + device->m_UUID + "::upnp:rootdevice"), 
            "upnp:rootdevice",
            socket,
            true, 
            &addr);
    }
    
    // on byebye, don't sleep otherwise it hangs when we stop upnp
    if (!byebye) NPT_System::Sleep(NPT_TimeInterval(PLT_DLNA_SSDP_DELAY));

    // uuid:device-UUID
    PLT_SsdpSender::SendSsdp(req,
        "uuid:" + device->m_UUID, 
        "uuid:" + device->m_UUID, 
        socket, 
        true, 
        &addr);

    // on byebye, don't sleep otherwise it hangs when we stop upnp
    if (!byebye) NPT_System::Sleep(NPT_TimeInterval(PLT_DLNA_SSDP_DELAY));

    // uuid:device-UUID::urn:schemas-upnp-org:device:deviceType:ver
    PLT_SsdpSender::SendSsdp(req,
        NPT_String("uuid:" + device->m_UUID + "::" + device->m_DeviceType), 
        device->m_DeviceType,
        socket,
        true,
        &addr);
    
    // on byebye, don't sleep otherwise it hangs when we stop upnp
    if (!byebye) NPT_System::Sleep(NPT_TimeInterval(PLT_DLNA_SSDP_DELAY));

    // services
    for (int i=0; i < (int)device->m_Services.GetItemCount(); i++) {
        // uuid:device-UUID::urn:schemas-upnp-org:service:serviceType:ver
        PLT_SsdpSender::SendSsdp(req,
            NPT_String("uuid:" + device->m_UUID + "::" + device->m_Services[i]->GetServiceType()), 
            device->m_Services[i]->GetServiceType(),
            socket,
            true, 
            &addr); 
        
        // on byebye, don't sleep otherwise it hangs when we stop upnp
        if (!byebye) NPT_System::Sleep(NPT_TimeInterval(PLT_DLNA_SSDP_DELAY));       
    }

    // embedded devices
    for (int j=0; j < (int)device->m_EmbeddedDevices.GetItemCount(); j++) {
        Announce(device->m_EmbeddedDevices[j].AsPointer(), 
            req, 
            socket, 
            byebye);
    }

    return res;
}

/*----------------------------------------------------------------------
|   PLT_DeviceHost::SetupResponse
+---------------------------------------------------------------------*/
NPT_Result 
PLT_DeviceHost::SetupResponse(NPT_HttpRequest&              request,
                              const NPT_HttpRequestContext& context,
                              NPT_HttpResponse&             response)
{
    // get the address of who sent us some data back*/
    NPT_String ip_address = context.GetRemoteAddress().GetIpAddress().ToString();
    NPT_String method     = request.GetMethod();
    NPT_String protocol   = request.GetProtocol(); 

    PLT_LOG_HTTP_MESSAGE(NPT_LOG_LEVEL_FINER, "PLT_DeviceHost::SetupResponse:", &request);

    if (method.Compare("POST") == 0) {
        return ProcessHttpPostRequest(request, context, response);
    } else if (method.Compare("SUBSCRIBE") == 0 || method.Compare("UNSUBSCRIBE") == 0) {
        return ProcessHttpSubscriberRequest(request, context, response);
    } else if (method.Compare("GET") == 0 || method.Compare("HEAD") == 0) {
        // process SCPD requests
        PLT_Service* service;
        if (NPT_SUCCEEDED(FindServiceBySCPDURL(request.GetUrl().ToRequestString(), service, true))) {
            return ProcessGetSCPD(service, request, context, response);
        }

        // process Description document requests
        if (request.GetUrl().GetPath() == m_URLDescription.GetPath()) {
            return ProcessGetDescription(request, context, response);
        }

        // process other requests
        return ProcessHttpGetRequest(request, context, response);
    }

    response.SetStatus(405, "Bad Request");
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_DeviceHost::ProcessHttpGetRequest
+---------------------------------------------------------------------*/
NPT_Result 
PLT_DeviceHost::ProcessHttpGetRequest(NPT_HttpRequest&              request,
                                      const NPT_HttpRequestContext& context,
                                      NPT_HttpResponse&             response)
{        
    NPT_COMPILER_UNUSED(request);
    NPT_COMPILER_UNUSED(context);
    NPT_COMPILER_UNUSED(response);
    
    return NPT_ERROR_NO_SUCH_ITEM;
}

/*----------------------------------------------------------------------
|   PLT_DeviceHost::ProcessGetDescription
+---------------------------------------------------------------------*/
NPT_Result 
PLT_DeviceHost::ProcessGetDescription(NPT_HttpRequest&              /*request*/,
                                      const NPT_HttpRequestContext& context,
                                      NPT_HttpResponse&             response)
{
    NPT_COMPILER_UNUSED(context);

    NPT_String doc;
    NPT_CHECK_FATAL(GetDescription(doc));
    NPT_LOG_FINEST_2("Returning description to %s: %s", 
        (const char*)context.GetRemoteAddress().GetIpAddress().ToString(),
        (const char*)doc);

    NPT_HttpEntity* entity;
    PLT_HttpHelper::SetBody(response, doc, &entity);    
    entity->SetContentType("text/xml; charset=\"utf-8\"");
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_DeviceHost::ProcessGetSCPD
+---------------------------------------------------------------------*/
NPT_Result 
PLT_DeviceHost::ProcessGetSCPD(PLT_Service*                  service,
                               NPT_HttpRequest&              /*request*/,
                               const NPT_HttpRequestContext& context,
                               NPT_HttpResponse&             response)
{
    NPT_COMPILER_UNUSED(context);
    NPT_CHECK_POINTER_FATAL(service);

    NPT_String doc;
    NPT_CHECK_FATAL(service->GetSCPDXML(doc));
    NPT_LOG_FINEST_2("Returning SCPD to %s: %s", 
        (const char*)context.GetRemoteAddress().GetIpAddress().ToString(),
        (const char*)doc);

    NPT_HttpEntity* entity;
    PLT_HttpHelper::SetBody(response, doc, &entity);    
    entity->SetContentType("text/xml; charset=\"utf-8\"");
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_DeviceHost::ProcessPostRequest
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceHost::ProcessHttpPostRequest(NPT_HttpRequest&              request,
                                       const NPT_HttpRequestContext& context,
                                       NPT_HttpResponse&             response) 
{
    NPT_Result                res;
    NPT_String                service_type;
    NPT_String                str;
    NPT_XmlElementNode*       xml = NULL;
    NPT_String                soap_action_header;
    PLT_Service*              service;
    NPT_XmlElementNode*       soap_body;
    NPT_XmlElementNode*       soap_action;
    PLT_ActionDesc*           action_desc;
    PLT_ActionReference       action;
    NPT_MemoryStreamReference resp(new NPT_MemoryStream);
    NPT_String                ip_address  = context.GetRemoteAddress().GetIpAddress().ToString();
    NPT_String                method      = request.GetMethod();
    NPT_String                url         = request.GetUrl().ToRequestString();
    NPT_String                protocol    = request.GetProtocol();

#if defined(PLATINUM_UPNP_SPECS_STRICT)
    const NPT_String*         attr;
#endif

    if (NPT_FAILED(FindServiceByControlURL(url, service, true)))
        goto bad_request;

    if (!request.GetHeaders().GetHeaderValue("SOAPAction"))
        goto bad_request;

    // extract the soap action name from the header
    soap_action_header = *request.GetHeaders().GetHeaderValue("SOAPAction");
    soap_action_header.TrimLeft('"');
    soap_action_header.TrimRight('"');
    char prefix[200];
    char soap_action_name[100];
    int  ret;
    //FIXME: no sscanf
    ret = sscanf(soap_action_header, "%199[^#]#%99s",
                 prefix, 
                 soap_action_name);
    if (ret != 2)
        goto bad_request;

    // read the xml body and parse it
    if (NPT_FAILED(PLT_HttpHelper::ParseBody(request, xml)))
        goto bad_request;

    // check envelope
    if (xml->GetTag().Compare("Envelope", true))
        goto bad_request;

#if defined(PLATINUM_UPNP_SPECS_STRICT)
    // check namespace
    if (!xml->GetNamespace() || xml->GetNamespace()->Compare("http://schemas.xmlsoap.org/soap/envelope/"))
        goto bad_request;

    // check encoding
    attr = xml->GetAttribute("encodingStyle", "http://schemas.xmlsoap.org/soap/envelope/");
    if (!attr || attr->Compare("http://schemas.xmlsoap.org/soap/encoding/"))
        goto bad_request;
#endif

    // read action
    soap_body = PLT_XmlHelper::GetChild(xml, "Body");
    if (soap_body == NULL)
        goto bad_request;

    PLT_XmlHelper::GetChild(soap_body, soap_action);
    if (soap_action == NULL)
        goto bad_request;

    // verify action name is identical to SOAPACTION header*/
    if (soap_action->GetTag().Compare(soap_action_name, true))
        goto bad_request;

    // verify namespace
    if (!soap_action->GetNamespace() || soap_action->GetNamespace()->Compare(service->GetServiceType()))
        goto bad_request;

    // create a buffer for our response body and call the service
    if ((action_desc = service->FindActionDesc(soap_action_name)) == NULL) {
        // create a bastard soap response
        PLT_Action::FormatSoapError(401, "Invalid Action", *resp);
        goto error;
    }

    // create a new action object
    action = new PLT_Action(*action_desc);

    // read all the arguments if any
    for (NPT_List<NPT_XmlNode*>::Iterator args = soap_action->GetChildren().GetFirstItem(); 
		 args; 
		 args++) {
        NPT_XmlElementNode* child = (*args)->AsElementNode();
        if (!child) continue;

        // Total HACK for xbox360 upnp uncompliance!
        NPT_String name = child->GetTag();
        if (action_desc->GetName() == "Browse" && name == "ContainerID") {
            name = "ObjectID";
        }

        res = action->SetArgumentValue(
            name,
            child->GetText()?*child->GetText():"");

		// test if value was correct
		if (res == NPT_ERROR_INVALID_PARAMETERS) {
			action->SetError(701, "Invalid Name");
			goto error;
		}
    }

	// verify all required arguments were passed
    if (NPT_FAILED(action->VerifyArguments(true))) {
        action->SetError(402, "Invalid or Missing Args");
        goto error;
    }
    
    NPT_LOG_FINE_2("Processing action \"%s\" from %s", 
                   (const char*)action->GetActionDesc().GetName(), 
                   (const char*)context.GetRemoteAddress().GetIpAddress().ToString());
                   
    // call the virtual function, it's all good
    if (NPT_FAILED(OnAction(action, PLT_HttpRequestContext(request, context)))) {
        goto error;
    }

    // create the soap response now
    action->FormatSoapResponse(*resp);
    goto done;

error:
    if (!action.IsNull()) {
        // set the error in case it wasn't done already
        if (action->GetErrorCode() == 0) {
            action->SetError(501, "Action Failed");
        }
        NPT_LOG_WARNING_3("Error while processing action %s: %d %s",
            (const char*)action->GetActionDesc().GetName(), 
            action->GetErrorCode(),
            action->GetError());

        action->FormatSoapResponse(*resp);
    }
    
    response.SetStatus(500, "Internal Server Error");

done:
    NPT_LargeSize resp_body_size;    
    if (NPT_SUCCEEDED(resp->GetAvailable(resp_body_size))) {
        NPT_HttpEntity* entity;
        PLT_HttpHelper::SetBody(response, 
                                (NPT_InputStreamReference)resp, 
                                &entity);
        entity->SetContentType("text/xml; charset=\"utf-8\"");
        response.GetHeaders().SetHeader("Ext", ""); // should only be for M-POST but oh well
    }    
    
    delete xml;
    return NPT_SUCCESS;

bad_request:
    delete xml;
    response.SetStatus(500, "Bad Request");
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_DeviceHost::ProcessHttpSubscriberRequest
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceHost::ProcessHttpSubscriberRequest(NPT_HttpRequest&              request,
                                             const NPT_HttpRequestContext& context,
                                             NPT_HttpResponse&             response) 
{
    NPT_String  ip_address = context.GetRemoteAddress().GetIpAddress().ToString();
    NPT_String  method     = request.GetMethod();
    NPT_String  url        = request.GetUrl().ToRequestString();
    NPT_String  protocol   = request.GetProtocol();

    const NPT_String* nt            = PLT_UPnPMessageHelper::GetNT(request);
    const NPT_String* callback_urls = PLT_UPnPMessageHelper::GetCallbacks(request);
    const NPT_String* sid           = PLT_UPnPMessageHelper::GetSID(request);
    
    PLT_Service* service;
    NPT_CHECK_LABEL_WARNING(FindServiceByEventSubURL(url, service, true), cleanup);

    if (method.Compare("SUBSCRIBE") == 0) {
        // Do we have a sid ?
        if (sid) {
            // make sure we don't have a callback nor a nt
            if (nt || callback_urls) {
                goto cleanup;
            }
          
            // default lease
            NPT_Int32 timeout = *PLT_Constants::GetInstance().GetDefaultSubscribeLease().AsPointer();

            // subscription renewed
            // send the info to the service
            service->ProcessRenewSubscription(context.GetLocalAddress(), 
                                              *sid, 
                                              timeout, 
                                              response);
            return NPT_SUCCESS;
        } else {
            // new subscription ?
            // verify nt is present and valid
            if (!nt || nt->Compare("upnp:event", true)) {
                response.SetStatus(412, "Precondition failed");
                return NPT_SUCCESS;
            }
            // verify callback is present
            if (!callback_urls) {
                response.SetStatus(412, "Precondition failed");
                return NPT_SUCCESS;
            }

            // default lease time
            NPT_Int32 timeout = *PLT_Constants::GetInstance().GetDefaultSubscribeLease().AsPointer();

            // send the info to the service
            service->ProcessNewSubscription(&m_TaskManager,
                                            context.GetLocalAddress(), 
                                            *callback_urls, 
                                            timeout, 
                                            response);
            return NPT_SUCCESS;
        }
    } else if (method.Compare("UNSUBSCRIBE") == 0) {
        // Do we have a sid ?
        if (sid && sid->GetLength() > 0) {
            // make sure we don't have a callback nor a nt
            if (nt || callback_urls) {
                goto cleanup;
            }

            // subscription cancelled
            // send the info to the service
            service->ProcessCancelSubscription(context.GetLocalAddress(), 
                                               *sid, 
                                               response);
            return NPT_SUCCESS;
        }
        
        response.SetStatus(412, "Precondition failed");
        return NPT_SUCCESS;
    }

cleanup:
    response.SetStatus(400, "Bad Request");
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_DeviceHost::OnSsdpPacket
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceHost::OnSsdpPacket(const NPT_HttpRequest&        request, 
                             const NPT_HttpRequestContext& context)
{
    // get the address of who sent us some data back*/
    NPT_String ip_address  = context.GetRemoteAddress().GetIpAddress().ToString();
    NPT_String method      = request.GetMethod();
    NPT_String url         = request.GetUrl().ToRequestString(true);
    NPT_String protocol    = request.GetProtocol();
	NPT_IpPort remote_port = context.GetRemoteAddress().GetPort();
	const NPT_String* st   = PLT_UPnPMessageHelper::GetST(request);

	if (method.Compare("M-SEARCH") == 0) {
		NPT_String prefix = NPT_String::Format("PLT_DeviceHost::OnSsdpPacket M-SEARCH for %s from %s:%d", 
			st?st->GetChars():"Unknown",
			(const char*) ip_address, remote_port);
		PLT_LOG_HTTP_MESSAGE(NPT_LOG_LEVEL_FINER, prefix, request);

        /*
        // DLNA 7.2.3.5 support
        if (remote_port < 1024 || remote_port == 1900) {
            NPT_LOG_INFO_2("Ignoring M-SEARCH from %s:%d (invalid source port)", 
                (const char*) ip_address,
                remote_port);
            return NPT_FAILURE;
        }
         */

        NPT_CHECK_POINTER_SEVERE(st);

        if (url.Compare("*") || protocol.Compare("HTTP/1.1"))
            return NPT_FAILURE;

        const NPT_String* man = PLT_UPnPMessageHelper::GetMAN(request);
        if (!man || man->Compare("\"ssdp:discover\"", true))
            return NPT_FAILURE;

        NPT_UInt32 mx;
        NPT_CHECK_SEVERE(PLT_UPnPMessageHelper::GetMX(request, mx));

        // create a task to respond to the request
        NPT_TimeInterval timer((mx==0)?0.:(double)(NPT_System::GetRandomInteger()%(mx>5?5:mx)));
        PLT_SsdpDeviceSearchResponseTask* task = new PLT_SsdpDeviceSearchResponseTask(this, context.GetRemoteAddress(), *st);
        m_TaskManager.StartTask(task, &timer);
        return NPT_SUCCESS;
    }

    return NPT_FAILURE;
}

/*----------------------------------------------------------------------
|   PLT_DeviceHost::SendSsdpSearchResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceHost::SendSsdpSearchResponse(PLT_DeviceData*    device, 
                                       NPT_HttpResponse&  response, 
                                       NPT_UdpSocket&     socket, 
                                       const char*        st,
                                       const NPT_SocketAddress* addr /* = NULL */)
{    
    // ssdp:all or upnp:rootdevice
    if (NPT_String::Compare(st, "ssdp:all") == 0 || 
        NPT_String::Compare(st, "upnp:rootdevice") == 0) {

        if (device->m_ParentUUID.IsEmpty()) {
            NPT_LOG_FINE_1("Responding to a M-SEARCH request for %s", st);

           // upnp:rootdevice
           PLT_SsdpSender::SendSsdp(response, 
                    NPT_String("uuid:" + device->m_UUID + "::upnp:rootdevice"), 
                    "upnp:rootdevice",
                    socket,
                    false,
                    addr);
        }
    }

    // uuid:device-UUID
    if (NPT_String::Compare(st, "ssdp:all") == 0 || 
        NPT_String::Compare(st, (const char*)("uuid:" + device->m_UUID)) == 0) {

        NPT_LOG_FINE_1("Responding to a M-SEARCH request for %s", st);

        // uuid:device-UUID
        PLT_SsdpSender::SendSsdp(response, 
                 "uuid:" + device->m_UUID, 
                 "uuid:" + device->m_UUID, 
                 socket, 
                 false,
                 addr);
    }

    // urn:schemas-upnp-org:device:deviceType:ver
    if (NPT_String::Compare(st, "ssdp:all") == 0 || 
        NPT_String::Compare(st, (const char*)(device->m_DeviceType)) == 0) {

        NPT_LOG_FINE_1("Responding to a M-SEARCH request for %s", st);

        // uuid:device-UUID::urn:schemas-upnp-org:device:deviceType:ver
        PLT_SsdpSender::SendSsdp(response, 
                 NPT_String("uuid:" + device->m_UUID + "::" + device->m_DeviceType), 
                 device->m_DeviceType,
                 socket,
                 false,
                 addr);
    }

    // services
    for (int i=0; i < (int)device->m_Services.GetItemCount(); i++) {
        if (NPT_String::Compare(st, "ssdp:all") == 0 || 
            NPT_String::Compare(st, (const char*)(device->m_Services[i]->GetServiceType())) == 0) {

            NPT_LOG_FINE_1("Responding to a M-SEARCH request for %s", st);

            // uuid:device-UUID::urn:schemas-upnp-org:service:serviceType:ver
            PLT_SsdpSender::SendSsdp(response, 
                     NPT_String("uuid:" + device->m_UUID + "::" + device->m_Services[i]->GetServiceType()), 
                     device->m_Services[i]->GetServiceType(),
                     socket,
                     false,
                     addr);
        }
    }

    // embedded devices
    for (int j=0; j < (int)device->m_EmbeddedDevices.GetItemCount(); j++) {
        SendSsdpSearchResponse(device->m_EmbeddedDevices[j].AsPointer(), 
            response, 
            socket, 
            st, 
            addr);
    }
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_DeviceHost::OnAction
+---------------------------------------------------------------------*/
NPT_Result
PLT_DeviceHost::OnAction(PLT_ActionReference&          action, 
                         const PLT_HttpRequestContext& context)
{
    NPT_COMPILER_UNUSED(context);
    action->SetError(401, "Invalid Action");
    return NPT_FAILURE;
}

