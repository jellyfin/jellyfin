/*****************************************************************
|
|   Platinum - Control/Event
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
#include "PltTaskManager.h"
#include "PltEvent.h"
#include "PltService.h"
#include "PltUPnP.h"
#include "PltDeviceData.h"
#include "PltUtilities.h"
#include "PltCtrlPointTask.h"

NPT_SET_LOCAL_LOGGER("platinum.core.event")

/*----------------------------------------------------------------------
|   PLT_EventNotification::PLT_EventNotification
+---------------------------------------------------------------------*/
PLT_EventNotification*
PLT_EventNotification::Parse(const NPT_HttpRequest&        request,
                             const NPT_HttpRequestContext& context,
                             NPT_HttpResponse&             response)
{
    NPT_COMPILER_UNUSED(context);

    PLT_LOG_HTTP_MESSAGE(NPT_LOG_LEVEL_FINER, "PLT_CtrlPoint::ProcessHttpNotify:", request);

    PLT_EventNotification *notification = new PLT_EventNotification();
    notification->m_RequestUrl = request.GetUrl();
    
    const NPT_String* sid = PLT_UPnPMessageHelper::GetSID(request);
    const NPT_String* nt  = PLT_UPnPMessageHelper::GetNT(request);
    const NPT_String* nts = PLT_UPnPMessageHelper::GetNTS(request);

    if (!sid || sid->GetLength() == 0) {
        NPT_CHECK_LABEL_WARNING(NPT_FAILURE, bad_request);
    }
    notification->m_SID = *sid;

    if (!nt  || nt->GetLength()  == 0 || !nts || nts->GetLength() == 0) {
        response.SetStatus(400, "Bad request");
        NPT_CHECK_LABEL_WARNING(NPT_FAILURE, bad_request);
    }

    if (nt->Compare("upnp:event", true) || nts->Compare("upnp:propchange", true)) {
        NPT_CHECK_LABEL_WARNING(NPT_FAILURE, bad_request);
    }

    // if the sequence number is less than our current one, we got it out of order
    // so we disregard it
    PLT_UPnPMessageHelper::GetSeq(request, notification->m_EventKey);

    // parse body
    if (NPT_FAILED(PLT_HttpHelper::GetBody(request, notification->m_XmlBody))) {
        NPT_CHECK_LABEL_WARNING(NPT_FAILURE, bad_request);
    }

    return notification;

bad_request:
    NPT_LOG_SEVERE("CtrlPoint received bad event notify request\r\n");
    if (response.GetStatusCode() == 200) {
        response.SetStatus(412, "Precondition Failed");
    }
    delete notification;
    return NULL;
}

/*----------------------------------------------------------------------
|   PLT_EventSubscriber::PLT_EventSubscriber
+---------------------------------------------------------------------*/
PLT_EventSubscriber::PLT_EventSubscriber(PLT_TaskManager* task_manager, 
                                         PLT_Service*     service,
                                         const char*      sid,
                                         NPT_Timeout      timeout_secs /* = -1 */) : 
    m_TaskManager(task_manager), 
    m_Service(service), 
    m_EventKey(0),
    m_SubscriberTask(NULL),
    m_SID(sid)
{
    NPT_LOG_FINE_1("Creating new subscriber (%s)", m_SID.GetChars());
    SetTimeout(timeout_secs);
}

/*----------------------------------------------------------------------
|   PLT_EventSubscriber::~PLT_EventSubscriber
+---------------------------------------------------------------------*/
PLT_EventSubscriber::~PLT_EventSubscriber() 
{
    NPT_LOG_FINE_1("Deleting subscriber (%s)", m_SID.GetChars());
    if (m_SubscriberTask) {
        m_SubscriberTask->Kill();
        m_SubscriberTask = NULL;
    }
}

/*----------------------------------------------------------------------
|   PLT_EventSubscriber::GetService
+---------------------------------------------------------------------*/
PLT_Service*
PLT_EventSubscriber::GetService() 
{
    return m_Service;
}

/*----------------------------------------------------------------------
|   PLT_EventSubscriber::GetEventKey
+---------------------------------------------------------------------*/
NPT_Ordinal
PLT_EventSubscriber::GetEventKey() 
{
    return m_EventKey;
}

/*----------------------------------------------------------------------
|   PLT_EventSubscriber::SetEventKey
+---------------------------------------------------------------------*/
NPT_Result
PLT_EventSubscriber::SetEventKey(NPT_Ordinal value) 
{
    m_EventKey = value;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_EventSubscriber::GetLocalIf
+---------------------------------------------------------------------*/
NPT_SocketAddress
PLT_EventSubscriber::GetLocalIf() 
{
    return m_LocalIf;
}

/*----------------------------------------------------------------------
|   PLT_EventSubscriber::SetLocalIf
+---------------------------------------------------------------------*/
NPT_Result
PLT_EventSubscriber::SetLocalIf(NPT_SocketAddress value) 
{
    m_LocalIf = value;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_EventSubscriber::GetExpirationTime
+---------------------------------------------------------------------*/
// a TimeStamp of 0 means no expiration
NPT_TimeStamp
PLT_EventSubscriber::GetExpirationTime()
{
    return m_ExpirationTime;
}

/*----------------------------------------------------------------------
|   PLT_EventSubscriber::SetExpirationTime
+---------------------------------------------------------------------*/
NPT_Result
PLT_EventSubscriber::SetTimeout(NPT_Timeout seconds) 
{
    NPT_LOG_FINE_2("subscriber (%s) expiring in %d seconds",
        m_SID.GetChars(),
        seconds);

    // -1 means infinite but we default to 300 secs
    if (seconds == -1) seconds = 300;
    
    NPT_System::GetCurrentTimeStamp(m_ExpirationTime);
    m_ExpirationTime += NPT_TimeInterval((double)seconds);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_EventSubscriber::FindCallbackURL
+---------------------------------------------------------------------*/
NPT_Result
PLT_EventSubscriber::FindCallbackURL(const char* callback_url) 
{
    NPT_String res;
    return NPT_ContainerFind(m_CallbackURLs, 
                             NPT_StringFinder(callback_url), 
                             res);
}

/*----------------------------------------------------------------------
|   PLT_EventSubscriber::AddCallbackURL
+---------------------------------------------------------------------*/
NPT_Result
PLT_EventSubscriber::AddCallbackURL(const char* callback_url) 
{
    NPT_CHECK_POINTER_FATAL(callback_url);

    NPT_LOG_FINE_2("Adding callback \"%s\" to subscriber %s", 
        callback_url, 
        m_SID.GetChars());
    return m_CallbackURLs.Add(callback_url);
}

/*----------------------------------------------------------------------
|   PLT_EventSubscriber::Notify
+---------------------------------------------------------------------*/
NPT_Result
PLT_EventSubscriber::Notify(NPT_List<PLT_StateVariable*>& vars)
{
    // verify we have eventable variables
    bool foundVars = false;
    NPT_XmlElementNode* propertyset = 
        new NPT_XmlElementNode("e", "propertyset");
    NPT_CHECK_SEVERE(propertyset->SetNamespaceUri(
        "e", 
        "urn:schemas-upnp-org:event-1-0"));

    NPT_List<PLT_StateVariable*>::Iterator var = vars.GetFirstItem();
    while (var) {
        if ((*var)->IsSendingEvents()) {
            NPT_XmlElementNode* property = 
                new NPT_XmlElementNode("e", "property");
            propertyset->AddChild(property);
            PLT_XmlHelper::AddChildText(property, 
                                        (*var)->GetName(), 
                                        (*var)->GetValue());
            foundVars = true;
        }
        ++var;
    }

    // no eventable state variables found!
    if (foundVars == false) {
        delete propertyset;
        return NPT_FAILURE;
    }

    // format the body with the xml
    NPT_String xml;
    if (NPT_FAILED(PLT_XmlHelper::Serialize(*propertyset, xml))) {
        delete propertyset;
        NPT_CHECK_FATAL(NPT_FAILURE);
    }
    delete propertyset;

    // parse the callback url
    NPT_HttpUrl url(m_CallbackURLs[0]);
    if (!url.IsValid()) {
        NPT_CHECK_FATAL(NPT_FAILURE);
    }
    // format request
    NPT_HttpRequest* request = 
        new NPT_HttpRequest(url,
                            "NOTIFY",
                            NPT_HTTP_PROTOCOL_1_1);
    NPT_HttpEntity* entity;
    PLT_HttpHelper::SetBody(*request, xml, &entity);

    // add the extra headers
    entity->SetContentType("text/xml; charset=\"utf-8\"");
    PLT_UPnPMessageHelper::SetNT(*request, "upnp:event");
    PLT_UPnPMessageHelper::SetNTS(*request, "upnp:propchange");
    PLT_UPnPMessageHelper::SetSID(*request, m_SID);
    PLT_UPnPMessageHelper::SetSeq(*request, m_EventKey);

    // wrap around sequence to 1
    if (++m_EventKey == 0) m_EventKey = 1;

    // start the task now if not started already
    if (!m_SubscriberTask) {
        // TODO: the subscriber task should inform subscriber if
        // a notification failed to be received so it can be removed
        // from the list of subscribers inside the device host
        m_SubscriberTask = new PLT_HttpClientSocketTask(request, true);
        
        // short connection time out in case subscriber is not alive
        NPT_HttpClient::Config config;
        config.m_ConnectionTimeout = 2000;
        m_SubscriberTask->SetHttpClientConfig(config);
        
        // add initial delay to make sure ctrlpoint receives response to subscription
        // before our first NOTIFY. Also make sure task is not auto-destroy
        // since we want to destroy it manually when the subscriber goes away.
        NPT_TimeInterval delay(0.05f);
        NPT_CHECK_FATAL(m_TaskManager->StartTask(m_SubscriberTask, NULL /*&delay*/, false));
    } else {
        m_SubscriberTask->AddRequest(request);
    }
     
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_EventSubscriberFinderByService::operator()
+---------------------------------------------------------------------*/
bool 
PLT_EventSubscriberFinderByService::operator()(PLT_EventSubscriberReference const & eventSub) const
{
    return (m_Service == eventSub->GetService());
}
