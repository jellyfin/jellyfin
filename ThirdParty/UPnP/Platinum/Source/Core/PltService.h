/*****************************************************************
|
|   Platinum - Service
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
 UPnP Service
 */

#ifndef _PLT_SERVICE_H_
#define _PLT_SERVICE_H_

/*----------------------------------------------------------------------
|    includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "PltEvent.h"
#include "PltArgument.h"
#include "PltStateVariable.h"
#include "PltAction.h"

/*----------------------------------------------------------------------
|    forward declarations
+---------------------------------------------------------------------*/
class PLT_DeviceData;

/*----------------------------------------------------------------------
|    PLT_Service class
+---------------------------------------------------------------------*/
/**
 UPnP Service.
 The PLT_Service class holds information about a UPnP service of a given device.
 It maintains a list of actions and state variables. A PLT_DeviceData instance can own 
 one or more PLT_Service instances. When a PLT_Service is advertised as part of a
 a UPnP Device (PLT_DeviceHost), it also maintains a list of subscribers to nofify when
 state variables change.
 */
class PLT_Service
{
public:
    // methods
    /**
     Create an instance of a UPnP Service either hosted or discovered.
     @param device Pointer to the PLT_DeviceData the service is associated to
     @param type String representing the UPnP service type
     @param id String representing the UPnP service id
     @param name A String to create unique service SCPD, control and eventing urls
     @param last_change_namespace A String for the LastChange state variable namespace if any
     */
    PLT_Service(PLT_DeviceData* device,
                const char*     type, 
                const char*     id,
                const char*     name,
                const char*     last_change_namespace = NULL);
    virtual ~PLT_Service();
    
    // methods
    /**
     When service is hosted by a PLT_DeviceHost, this setups the SCPD, control and event urls.
     @param service_name the service name used to format unique urls
     */
    NPT_Result InitURLs(const char* service_name);
    
    /**
     Verify the service has been properly initialized or is a valid discovered service.
     @return true if valid.
     */
    bool IsValid() {  return (m_ActionDescs.GetItemCount() > 0); }
    
    /**
     When a PLT_DeviceHost needs to change more than one state variables at a time
     but would rather send only one event with all state variable changes, this can be 
     used to pause and resume the automatic eventing.
     @param pause Flag to indicate if eventing should be paused or resumed 
     */
    NPT_Result PauseEventing(bool pause = true);

    // class methods
    static bool IsTrue(const NPT_String& value) {
        if (value.Compare("1", true)    && 
            value.Compare("true", true) && 
            value.Compare("yes", true)) {
            return false;
        }
        return true;
    }

    // accessor methods
    /**
     Set the SCPD url for control points to be able to fetch the SCPD xml document.
     @param url relative path of SCPD url
     */
    NPT_Result SetSCPDURL(const char* url)     { m_SCPDURL = url; return NPT_SUCCESS; }
    
    /*
     Set the Service Control url for control points to be able to invoke actions.
     @param url relative path of control url
     */
    NPT_Result SetControlURL(const char* url)  { m_ControlURL = url; return NPT_SUCCESS; };
    
    /**
     Set the Service Event subscription url for control points to be able to subscribe
     to events.
     @param url relative path of even url
     */
    NPT_Result SetEventSubURL(const char* url) { m_EventSubURL = url; return NPT_SUCCESS; };
    
    /**
     Return the SCPD url associated with this service.
     @param absolute flag to indicate if absolute url including ip and port should
     be returned
     @return SCPD url
     */
    NPT_String GetSCPDURL(bool absolute = false);
    
    /**
     Return the Control url associated with this service.
     @param absolute flag to indicate if absolute url including ip and port should
     be returned
     @return Control url
     */
    NPT_String GetControlURL(bool absolute = false);
    
    /**
     Return the Event subscription url associated with this service.
     @param absolute flag to indicate if absolute url including ip and port should
     be returned
     @return Event url
     */
    NPT_String GetEventSubURL(bool absolute = false);
    
    /**
     Return the service id.
     @return service id
     */
    const NPT_String& GetServiceID() const { return m_ServiceID;   }
    
    /**
     Return the service type.
     @return service type
     */
    const NPT_String& GetServiceType() const { return m_ServiceType; }

    /**
     Return the service friendly name.
     @return service name
     */
    const NPT_String& GetServiceName() const { return m_ServiceName; } 

    /**
     Return the PLT_DeviceData* the service is associated with.
     @return PLT_DeviceData pointer
     */
    PLT_DeviceData* GetDevice() { return m_Device;      }
    
    /**
     When a control point discover a new service with a higher version number
     than it can work with, a lower version can be set to force backward 
     compatibility.
     @param version Integer specifying the version to use
     */
    NPT_Result ForceVersion(NPT_Cardinal version);

    /**
     Return the service SCPD xml document.
     @param xml String to receive document
     */
    NPT_Result GetSCPDXML(NPT_String& xml);
    
    /**
     Set the service SCPD xml document.
     @param xml String SCPD xml document
     */
    NPT_Result SetSCPDXML(const char* xml);
    
    /**
     Populate the UPnP Device description document with service information.
     @param parent XML Element where to insert the service XML Element
     @param service Pointer to service XML Element node newly created so it can be
     extended with additional non standard information.
     */
    NPT_Result GetDescription(NPT_XmlElementNode* parent, NPT_XmlElementNode** service = NULL);

    /**
     Set a new value for a given state variable. The service keeps track of which
     state variables have changed and events are being triggered by a PLT_ServiceEventTask
     when necessary.
     @param name state variable name
     @param value new State Variable value.
     */
    NPT_Result SetStateVariable(const char* name, const char* value);
    
    /**
     Certain state variables notifications must not be sent faster than a certain 
     rate according to the UPnP specs. This sets the rate for a given state variable.
     @param name state variable name
     @param rate a time interval specifying the minimum interval allowed between
     notifications.
     */
    NPT_Result SetStateVariableRate(const char* name, NPT_TimeInterval rate);
    
    /**
     Certain state variables require extra xml attributes when serialized.
     @param name state variable name
     @param key the attribute name
     @param value the attribute value
     */
	NPT_Result SetStateVariableExtraAttribute(const char* name, const char* key, const char* value);
    
    /**
     Helper function to increment a state variable representing a number.
     @param name state variable name
     */
    NPT_Result IncStateVariable(const char* name);
    
    /**
     Return the PLT_StateVariable pointer given a state variable name.
     @param name state variable name
     @return PLT_StateVariable pointer
     */
    PLT_StateVariable* FindStateVariable(const char* name);
    
    /**
     Return the state variable value given a state variable name.
     @param name state variable name
     @param value state variable value output
     */
    NPT_Result GetStateVariableValue(const char* name, NPT_String& value);
    
    /**
     Return whether a service is capable of sending events.
     @return true if sending events
     */
    bool IsSubscribable();
    
    /**
     Return the list of state variables.
     @return list of state variable pointers.
     */
    const NPT_List<PLT_StateVariable*>& GetStateVariables() const { return m_StateVars; }

    /**
     Return the PLT_ActionDesc given an action name
     @param name action name
     @return PLT_ActioDesc pointer
     */
    PLT_ActionDesc* FindActionDesc(const char* name);
    
    /**
     Return an array of actions descriptions PLT_ActionDesc.
     @return array of PLT_ActionDesc pointers.
     */
    const NPT_Array<PLT_ActionDesc*>& GetActionDescs() const { return m_ActionDescs; }

private:    
    /**
     A task to send events.
     The PLT_ServiceEventTask is started when receiving a first subscription. It
     monitors if some state variables have changed and sends events to all
     subscribers if so.
     */
    class PLT_ServiceEventTask : public PLT_ThreadTask {
    public:
        PLT_ServiceEventTask(PLT_Service* service) : m_Service(service) {}
        
        void DoRun() { 
            while (!IsAborting(100)) m_Service->NotifyChanged();
        }
        
    private:
        PLT_Service* m_Service;
    };
    
    // methods
    void Cleanup();
    
    /**
     Called by a PLT_StateVariable to keep track of what events need to be 
     sent by the PLT_ServiceEventTask task.
     @param var PLT_StateVariable pointer
     */
    NPT_Result AddChanged(PLT_StateVariable* var);
    
    /**
     Certain UPnP services combine state variable changes into one single
     state variable called "LastChange". This function updates the LastChange
     state variable by looking through the list passed for state variables that
     are not individually evented.
     */
    NPT_Result UpdateLastChange(NPT_List<PLT_StateVariable*>& vars);
    
    /**
     Send state variable change events to all subscribers.
     */
    NPT_Result NotifyChanged();

    // Events
    /**
     Called by PLT_DeviceHost when it receives a request for a new subscription.
     */
    NPT_Result ProcessNewSubscription(
        PLT_TaskManager*         task_manager,
        const NPT_SocketAddress& addr, 
        const NPT_String&        callback_urls, 
        int                      timeout, 
        NPT_HttpResponse&        response);
    
    /**
     Called by PLT_DeviceHost when it receives a request renewing an existing
     subscription.
     */
    NPT_Result ProcessRenewSubscription(
        const NPT_SocketAddress& addr, 
        const NPT_String&        sid, 
        int                      timeout,
        NPT_HttpResponse&        response);
    
    /**
     Called by PLT_DeviceHost when it receives a request to cancel an existing
     subscription.
     */
    NPT_Result ProcessCancelSubscription(
        const NPT_SocketAddress& addr, 
        const NPT_String&        sid, 
        NPT_HttpResponse&        response);


protected:
    // friends that need to call private functions
    friend class PLT_StateVariable; // AddChanged
    friend class PLT_DeviceHost;    // ProcessXXSubscription
    
    //members
    PLT_DeviceData*                         m_Device;
    NPT_String                              m_ServiceType;
    NPT_String                              m_ServiceID;
	NPT_String                              m_ServiceName;
    NPT_String                              m_SCPDURL;
    NPT_String                              m_ControlURL;
    NPT_String                              m_EventSubURL;
    PLT_ServiceEventTask*                   m_EventTask;
    NPT_Array<PLT_ActionDesc*>              m_ActionDescs;
    NPT_List<PLT_StateVariable*>            m_StateVars;
    NPT_Mutex                               m_Lock;
    NPT_List<PLT_StateVariable*>            m_StateVarsChanged;
    NPT_List<PLT_StateVariable*>            m_StateVarsToPublish;
    NPT_List<PLT_EventSubscriberReference>  m_Subscribers;
    bool                                    m_EventingPaused;
    NPT_String                              m_LastChangeNamespace;
};

/*----------------------------------------------------------------------
|    PLT_ServiceSCPDURLFinder
+---------------------------------------------------------------------*/
/** 
 The PLT_ServiceSCPDURLFinder class returns an instance of a PLT_Service given a 
 service SCPD url.
 */
class PLT_ServiceSCPDURLFinder
{
public:
    // methods
    PLT_ServiceSCPDURLFinder(const char* url) : m_URL(url) {}
    virtual ~PLT_ServiceSCPDURLFinder() {}
    bool operator()(PLT_Service* const & service) const;

private:
    // members
    NPT_String m_URL;
};

/*----------------------------------------------------------------------
|    PLT_ServiceControlURLFinder
+---------------------------------------------------------------------*/
/** 
 The PLT_ServiceControlURLFinder class returns an instance of a PLT_Service 
 given a service control url.
 */
class PLT_ServiceControlURLFinder
{
public:
    // methods
    PLT_ServiceControlURLFinder(const char* url) : m_URL(url) {}
    virtual ~PLT_ServiceControlURLFinder() {}
    bool operator()(PLT_Service* const & service) const;

private:
    // members
    NPT_String m_URL;
};

/*----------------------------------------------------------------------
|    PLT_ServiceEventSubURLFinder
+---------------------------------------------------------------------*/
/** 
 The PLT_ServiceEventSubURLFinder class returns an instance of a PLT_Service 
 given a service event subscription url.
 */
class PLT_ServiceEventSubURLFinder
{
public:
    // methods
    PLT_ServiceEventSubURLFinder(const char* url) : m_URL(url) {}
    virtual ~PLT_ServiceEventSubURLFinder() {}
    bool operator()(PLT_Service* const & service) const;

private:
    // members
    NPT_String m_URL;
};

/*----------------------------------------------------------------------
|    PLT_ServiceIDFinder
+---------------------------------------------------------------------*/
/** 
 The PLT_ServiceIDFinder class returns an instance of a PLT_Service given a 
 service id.
 */
class PLT_ServiceIDFinder
{
public:
    // methods
    PLT_ServiceIDFinder(const char* id) : m_Id(id) {}
    virtual ~PLT_ServiceIDFinder() {}
    bool operator()(PLT_Service* const & service) const;

private:
    // members
    NPT_String m_Id;
};

/*----------------------------------------------------------------------
|    PLT_ServiceTypeFinder
+---------------------------------------------------------------------*/
/** 
 The PLT_ServiceTypeFinder class returns an instance of a PLT_Service given a 
 service type.
 */
class PLT_ServiceTypeFinder
{
public:
    // methods
    PLT_ServiceTypeFinder(const char* type) : m_Type(type) {}
    virtual ~PLT_ServiceTypeFinder() {}
    bool operator()(PLT_Service* const & service) const;

private:
    // members
    NPT_String m_Type;
};

/*----------------------------------------------------------------------
|    PLT_ServiceNameFinder
+---------------------------------------------------------------------*/
/** 
 The PLT_ServiceNameFinder class returns an instance of a PLT_Service given a 
 service name.
 */
class PLT_ServiceNameFinder
{
public:
    // methods
    PLT_ServiceNameFinder(const char* name) : m_Name(name) {}
    virtual ~PLT_ServiceNameFinder() {}
    bool operator()(PLT_Service* const & service) const;

private:
    // members
    NPT_String m_Name;
};

/*----------------------------------------------------------------------
|    PLT_LastChangeXMLIterator
+---------------------------------------------------------------------*/
/**
 The PLT_LastChangeXMLIterator class is used to serialize the LastChange variable
 changes into xml given a list of state variables.
 */
class PLT_LastChangeXMLIterator
{
public:
    // methods
    PLT_LastChangeXMLIterator(NPT_XmlElementNode* node) : m_Node(node) {}
    virtual ~PLT_LastChangeXMLIterator() {}

    NPT_Result operator()(PLT_StateVariable* const & var) const;

private:
    NPT_XmlElementNode* m_Node;
};

#endif /* _PLT_SERVICE_H_ */
