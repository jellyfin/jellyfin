/*****************************************************************
|
|   Platinum - Event
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
 UPnP Eventing
 */

#ifndef _PLT_EVENT_H_
#define _PLT_EVENT_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "PltHttpClientTask.h"

/*----------------------------------------------------------------------
|   forward declarations
+---------------------------------------------------------------------*/
class PLT_StateVariable;
class PLT_DeviceData;
class PLT_Service;
class PLT_TaskManager;
class PLT_CtrlPoint;

/*----------------------------------------------------------------------
|   PLT_EventNotification class
+---------------------------------------------------------------------*/
/**
 The PLT_EventNotification class represents an event notification for a given
 service to a given subscriber 
 */
class PLT_EventNotification
{
public:
    ~PLT_EventNotification() {}

    static PLT_EventNotification* Parse(const NPT_HttpRequest&        request,
                                        const NPT_HttpRequestContext& context,
                                        NPT_HttpResponse&             response);


    NPT_TimeStamp                 m_ReceptionTime;
    NPT_HttpUrl                   m_RequestUrl;
    NPT_String                    m_SID;
    NPT_Ordinal                   m_EventKey;
    NPT_String                    m_XmlBody;
    
protected:
    PLT_EventNotification() {}
};

/*----------------------------------------------------------------------
|   PLT_EventSubscriber class
+---------------------------------------------------------------------*/
/**
 The PLT_EventSubscriber class represents an event subscription for a given service
 from a UPnP ControlPoint.
 */
class PLT_EventSubscriber
{
public:
    PLT_EventSubscriber(PLT_TaskManager* task_manager, 
                        PLT_Service*     service,
                        const char*      sid,
                        NPT_Timeout      timeout_secs = -1);
    ~PLT_EventSubscriber();

    PLT_Service*      GetService();
    NPT_Ordinal       GetEventKey();
    NPT_Result        SetEventKey(NPT_Ordinal value);
    NPT_SocketAddress GetLocalIf();
    NPT_Result        SetLocalIf(NPT_SocketAddress value);
    NPT_TimeStamp     GetExpirationTime();
    NPT_Result        SetTimeout(NPT_Timeout seconds = -1);
    const NPT_String& GetSID() const { return m_SID; }
    NPT_Result        FindCallbackURL(const char* callback_url);
    NPT_Result        AddCallbackURL(const char* callback_url);
    NPT_Result        Notify(NPT_List<PLT_StateVariable*>& vars);
    
protected:
    //members
    PLT_TaskManager*          m_TaskManager;
    PLT_Service*              m_Service;
    NPT_Ordinal               m_EventKey;
    PLT_HttpClientSocketTask* m_SubscriberTask;
    NPT_String                m_SID;
    NPT_SocketAddress         m_LocalIf;
    NPT_Array<NPT_String>     m_CallbackURLs;
    NPT_TimeStamp             m_ExpirationTime;
};

typedef NPT_Reference<PLT_EventSubscriber> PLT_EventSubscriberReference;

/*----------------------------------------------------------------------
|   PLT_EventSubscriberFinderBySID
+---------------------------------------------------------------------*/
/**
 The PLT_EventSubscriberFinderBySID class returns an instance of a PLT_EventSubscriber
 given its subscriber ID.
 */
class PLT_EventSubscriberFinderBySID
{
public:
    // methods
    PLT_EventSubscriberFinderBySID(const char* sid) : m_SID(sid) {}

    bool operator()(PLT_EventSubscriberReference const & sub) const {
        return m_SID.Compare(sub->GetSID(), true) ? false : true;
    }

private:
    // members
    NPT_String m_SID;
};

/*----------------------------------------------------------------------
|   PLT_EventSubscriberFinderByCallbackURL
+---------------------------------------------------------------------*/
/**
 The PLT_EventSubscriberFinderByCallbackURL class returns an instance of a 
 PLT_EventSubscriber given its subscriber callback url.
 */
class PLT_EventSubscriberFinderByCallbackURL
{
public:
    // methods
    PLT_EventSubscriberFinderByCallbackURL(const char* callback_url) : 
      m_CallbackURL(callback_url) {}

    bool operator()(PLT_EventSubscriberReference const & sub) const {
        return NPT_SUCCEEDED(sub->FindCallbackURL(m_CallbackURL));
    }

private:
    // members
    NPT_String m_CallbackURL;
};

/*----------------------------------------------------------------------
|   PLT_EventSubscriberFinderByService
+---------------------------------------------------------------------*/
/**
 The PLT_EventSubscriberFinderByService class returns an instance of a 
 PLT_EventSubscriber given a UPnP service.
 */
class PLT_EventSubscriberFinderByService
{
public:
    // methods
    PLT_EventSubscriberFinderByService(PLT_Service* service) : m_Service(service) {}
    virtual ~PLT_EventSubscriberFinderByService() {}
    bool operator()(PLT_EventSubscriberReference const & eventSub) const;

private:
    // members
    PLT_Service* m_Service;
};

#endif /* _PLT_EVENT_H_ */
