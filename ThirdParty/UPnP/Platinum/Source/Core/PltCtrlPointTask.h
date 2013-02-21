/*****************************************************************
|
|   Platinum - Control Point Tasks
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
 UPnP ControlPoint Tasks
 */

#ifndef _PLT_CONTROL_POINT_TASK_H_
#define _PLT_CONTROL_POINT_TASK_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "PltHttpClientTask.h"
#include "PltDatagramStream.h"
#include "PltDeviceData.h"
#include "PltCtrlPoint.h"

/*----------------------------------------------------------------------
|   forward declarations
+---------------------------------------------------------------------*/
class PLT_Action;

/*----------------------------------------------------------------------
|   PLT_CtrlPointGetDescriptionTask class
+---------------------------------------------------------------------*/
/**
 The PLT_CtrlPointGetDescriptionTask class fetches the description xml document
 from a UPnP device
 */
class PLT_CtrlPointGetDescriptionTask : public PLT_HttpClientSocketTask
{
public:
    PLT_CtrlPointGetDescriptionTask(const NPT_HttpUrl& url,
                                    PLT_CtrlPoint*     ctrl_point,
                                    NPT_TimeInterval   leasetime,
                                    NPT_String         uuid);
    virtual ~PLT_CtrlPointGetDescriptionTask();

protected:
    // PLT_HttpClientSocketTask methods
    NPT_Result ProcessResponse(NPT_Result                    res, 
                               const NPT_HttpRequest&        request, 
                               const NPT_HttpRequestContext& context, 
                               NPT_HttpResponse*             response);

protected:
    PLT_CtrlPoint*   m_CtrlPoint;
    NPT_TimeInterval m_LeaseTime;
    NPT_String       m_UUID;
};

/*----------------------------------------------------------------------
|   PLT_CtrlPointGetSCPDRequest class
+---------------------------------------------------------------------*/
/**
 The PLT_CtrlPointGetSCPDRequest class is used by a PLT_CtrlPointGetSCPDsTask task
 to fetch a specific SCPD xml document for a given service of a given device.
 */
class PLT_CtrlPointGetSCPDRequest : public NPT_HttpRequest
{
public:
    PLT_CtrlPointGetSCPDRequest(PLT_DeviceDataReference& device,
                                const char*              url,
                                const char*              method = "GET",
                                const char*              protocol = NPT_HTTP_PROTOCOL_1_1) : // 1.1 for pipelining
        NPT_HttpRequest(url, method, protocol), m_Device(device) {}
    virtual ~PLT_CtrlPointGetSCPDRequest() {}

    // members
    PLT_DeviceDataReference m_Device;
};

/*----------------------------------------------------------------------
|   PLT_CtrlPointGetSCPDsTask class
+---------------------------------------------------------------------*/
/**
 The PLT_CtrlPointGetSCPDsTask class fetches the SCPD xml document of one or more
 services for a given device. 
 */
class PLT_CtrlPointGetSCPDsTask : public PLT_HttpClientSocketTask
{
public:
    PLT_CtrlPointGetSCPDsTask(PLT_CtrlPoint* ctrl_point, PLT_DeviceDataReference& root_device);
    virtual ~PLT_CtrlPointGetSCPDsTask();

    NPT_Result AddSCPDRequest(PLT_CtrlPointGetSCPDRequest* request) {
        return PLT_HttpClientSocketTask::AddRequest((NPT_HttpRequest*)request);
    }

    // override to prevent calling this directly
    NPT_Result AddRequest(NPT_HttpRequest*) {
        // only queuing PLT_CtrlPointGetSCPDRequest allowed
        return NPT_ERROR_NOT_SUPPORTED;
    }

protected:
    // PLT_HttpClientSocketTask methods
    NPT_Result ProcessResponse(NPT_Result                    res, 
                               const NPT_HttpRequest&        request, 
                               const NPT_HttpRequestContext& context, 
                               NPT_HttpResponse*             response);   

protected:
    PLT_CtrlPoint*          m_CtrlPoint;
    PLT_DeviceDataReference m_RootDevice;
};

/*----------------------------------------------------------------------
|   PLT_CtrlPointInvokeActionTask class
+---------------------------------------------------------------------*/
/**
 The PLT_CtrlPointInvokeActionTask class is used by a PLT_CtrlPoint to invoke
 a specific action of a given service for a given device.
 */
class PLT_CtrlPointInvokeActionTask : public PLT_HttpClientSocketTask
{
public:
    PLT_CtrlPointInvokeActionTask(NPT_HttpRequest*     request,
                                  PLT_CtrlPoint*       ctrl_point, 
                                  PLT_ActionReference& action,
                                  void*                userdata);
    virtual ~PLT_CtrlPointInvokeActionTask();

protected:
    // PLT_HttpClientSocketTask methods
    NPT_Result ProcessResponse(NPT_Result                    res, 
                               const NPT_HttpRequest&        request, 
                               const NPT_HttpRequestContext& context, 
                               NPT_HttpResponse*             response);   

protected:
    PLT_CtrlPoint*      m_CtrlPoint;
    PLT_ActionReference m_Action;
    void*               m_Userdata;
};

/*----------------------------------------------------------------------
|   PLT_CtrlPointHouseKeepingTask class
+---------------------------------------------------------------------*/
/**
 The PLT_CtrlPointHouseKeepingTask class is used by a PLT_CtrlPoint to keep 
 track of expired devices and autmatically renew event subscribers. 
 */
class PLT_CtrlPointHouseKeepingTask : public PLT_ThreadTask
{
public:
    PLT_CtrlPointHouseKeepingTask(PLT_CtrlPoint*   ctrl_point, 
                                  NPT_TimeInterval timer = NPT_TimeInterval(5.));

protected:
    ~PLT_CtrlPointHouseKeepingTask() {}

    // PLT_ThreadTask methods
    virtual void DoRun();

protected:
    PLT_CtrlPoint*   m_CtrlPoint;
    NPT_TimeInterval m_Timer;
};

/*----------------------------------------------------------------------
|   PLT_CtrlPointSubscribeEventTask class
+---------------------------------------------------------------------*/
/**
 The PLT_CtrlPointSubscribeEventTask class is used to subscribe, renew or cancel
 a subscription for a given service of a given device.
 */
class PLT_CtrlPointSubscribeEventTask : public PLT_HttpClientSocketTask
{
public:
    PLT_CtrlPointSubscribeEventTask(NPT_HttpRequest*         request,
                                    PLT_CtrlPoint*           ctrl_point, 
									PLT_DeviceDataReference& device,
                                    PLT_Service*             service,
                                    void*                    userdata = NULL);
    virtual ~PLT_CtrlPointSubscribeEventTask();
    
protected:
    // PLT_HttpClientSocketTask methods
    NPT_Result ProcessResponse(NPT_Result                    res, 
                               const NPT_HttpRequest&        request, 
                               const NPT_HttpRequestContext& context, 
                               NPT_HttpResponse*             response);

protected:
    PLT_CtrlPoint*          m_CtrlPoint;
    PLT_Service*            m_Service;
	PLT_DeviceDataReference m_Device; // force to keep a reference to device owning m_Service
    void*                   m_Userdata;
};

#endif /* _PLT_CONTROL_POINT_TASK_H_ */
