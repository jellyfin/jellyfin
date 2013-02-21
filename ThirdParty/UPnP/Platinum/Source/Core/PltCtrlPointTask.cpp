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

/*----------------------------------------------------------------------
|    includes
+---------------------------------------------------------------------*/
#include "PltDeviceHost.h"
#include "PltCtrlPointTask.h"
#include "PltCtrlPoint.h"
#include "PltDatagramStream.h"

/*----------------------------------------------------------------------
|    PLT_CtrlPointGetDescriptionTask::PLT_CtrlPointGetDescriptionTask
+---------------------------------------------------------------------*/
PLT_CtrlPointGetDescriptionTask::PLT_CtrlPointGetDescriptionTask(const NPT_HttpUrl& url,
                                                                 PLT_CtrlPoint*     ctrl_point,
                                                                 NPT_TimeInterval   leasetime,
                                                                 NPT_String         uuid) :
    PLT_HttpClientSocketTask(new NPT_HttpRequest(url, "GET", NPT_HTTP_PROTOCOL_1_1)), 
    m_CtrlPoint(ctrl_point),
    m_LeaseTime(leasetime),
    m_UUID(uuid)
{
}

/*----------------------------------------------------------------------
|    PLT_CtrlPointGetDescriptionTask::~PLT_CtrlPointGetDescriptionTask
+---------------------------------------------------------------------*/
PLT_CtrlPointGetDescriptionTask::~PLT_CtrlPointGetDescriptionTask() 
{
}

/*----------------------------------------------------------------------
|    PLT_CtrlPointGetDescriptionTask::ProcessResponse
+---------------------------------------------------------------------*/
NPT_Result 
PLT_CtrlPointGetDescriptionTask::ProcessResponse(NPT_Result                    res, 
                                                 const NPT_HttpRequest&        request, 
                                                 const NPT_HttpRequestContext& context, 
                                                 NPT_HttpResponse*             response)
{
    NPT_COMPILER_UNUSED(request);
    return m_CtrlPoint->ProcessGetDescriptionResponse(
        res, 
        request, 
        context, 
        response,
        m_LeaseTime,
        m_UUID);
}

/*----------------------------------------------------------------------
|    PLT_CtrlPointGetSCPDsTask::PLT_CtrlPointGetSCPDsTask
+---------------------------------------------------------------------*/
PLT_CtrlPointGetSCPDsTask::PLT_CtrlPointGetSCPDsTask(PLT_CtrlPoint*           ctrl_point, 
                                                     PLT_DeviceDataReference& root_device) :  
    PLT_HttpClientSocketTask(), 
    m_CtrlPoint(ctrl_point),
    m_RootDevice(root_device)
{
}

/*----------------------------------------------------------------------
|    PLT_CtrlPointGetSCPDsTask::~PLT_CtrlPointGetSCPDsTask
+---------------------------------------------------------------------*/
PLT_CtrlPointGetSCPDsTask::~PLT_CtrlPointGetSCPDsTask() 
{
}

/*----------------------------------------------------------------------
|    PLT_CtrlPointGetSCPDsTask::ProcessResponse
+---------------------------------------------------------------------*/
NPT_Result 
PLT_CtrlPointGetSCPDsTask::ProcessResponse(NPT_Result                    res, 
                                          const NPT_HttpRequest&        request, 
                                          const NPT_HttpRequestContext& context, 
                                          NPT_HttpResponse*             response)
{
    NPT_COMPILER_UNUSED(context);
    return m_CtrlPoint->ProcessGetSCPDResponse(
        res, 
        request, 
        context, 
        response,
        ((PLT_CtrlPointGetSCPDRequest&)request).m_Device);
}

/*----------------------------------------------------------------------
|    PLT_CtrlPointInvokeActionTask::PLT_CtrlPointInvokeActionTask
+---------------------------------------------------------------------*/
PLT_CtrlPointInvokeActionTask::PLT_CtrlPointInvokeActionTask(NPT_HttpRequest*     request,
                                                             PLT_CtrlPoint*       ctrl_point, 
                                                             PLT_ActionReference& action,
                                                             void*                userdata) : 
    PLT_HttpClientSocketTask(request), 
    m_CtrlPoint(ctrl_point), 
    m_Action(action), 
    m_Userdata(userdata)
{
}

/*----------------------------------------------------------------------
|    PLT_CtrlPointInvokeActionTask::~PLT_CtrlPointInvokeActionTask
+---------------------------------------------------------------------*/
PLT_CtrlPointInvokeActionTask::~PLT_CtrlPointInvokeActionTask() 
{
}

/*----------------------------------------------------------------------
|    PLT_CtrlPointInvokeActionTask::ProcessResponse
+---------------------------------------------------------------------*/
NPT_Result 
PLT_CtrlPointInvokeActionTask::ProcessResponse(NPT_Result                    res, 
                                               const NPT_HttpRequest&        request, 
                                               const NPT_HttpRequestContext& context, 
                                               NPT_HttpResponse*             response)
{
    NPT_COMPILER_UNUSED(request);
    NPT_COMPILER_UNUSED(context);

    return m_CtrlPoint->ProcessActionResponse(res, request, context, response, m_Action, m_Userdata);
}

/*----------------------------------------------------------------------
|    PLT_CtrlPointHouseKeepingTask::PLT_CtrlPointHouseKeepingTask
+---------------------------------------------------------------------*/
PLT_CtrlPointHouseKeepingTask::PLT_CtrlPointHouseKeepingTask(PLT_CtrlPoint*   ctrl_point, 
                                                             NPT_TimeInterval timer) : 
    m_CtrlPoint(ctrl_point), 
    m_Timer(timer)
{
}

/*----------------------------------------------------------------------
|    PLT_CtrlPointHouseKeepingTask::DoRun
+---------------------------------------------------------------------*/
void  
PLT_CtrlPointHouseKeepingTask::DoRun() 
{
    while (!IsAborting((NPT_Timeout)m_Timer.ToSeconds()*1000)) {
        if (m_CtrlPoint) {
            m_CtrlPoint->DoHouseKeeping();
        }
    }
}

/*----------------------------------------------------------------------
|    PLT_CtrlPointSubscribeEventTask::PLT_CtrlPointSubscribeEventTask
+---------------------------------------------------------------------*/
PLT_CtrlPointSubscribeEventTask::PLT_CtrlPointSubscribeEventTask(NPT_HttpRequest*        request,
                                                                 PLT_CtrlPoint*          ctrl_point, 
																 PLT_DeviceDataReference &device,
                                                                 PLT_Service*            service,
                                                                 void*                   userdata) : 
    PLT_HttpClientSocketTask(request), 
    m_CtrlPoint(ctrl_point), 
    m_Service(service), 
	m_Device(device),
    m_Userdata(userdata) 
{
}

/*----------------------------------------------------------------------
|    PLT_CtrlPointSubscribeEventTask::~PLT_CtrlPointSubscribeEventTask
+---------------------------------------------------------------------*/
PLT_CtrlPointSubscribeEventTask::~PLT_CtrlPointSubscribeEventTask() 
{
}

/*----------------------------------------------------------------------
|    PLT_CtrlPointSubscribeEventTask::ProcessResponse
+---------------------------------------------------------------------*/
NPT_Result 
PLT_CtrlPointSubscribeEventTask::ProcessResponse(NPT_Result                    res, 
                                                 const NPT_HttpRequest&        request, 
                                                 const NPT_HttpRequestContext& context, 
                                                 NPT_HttpResponse*             response)
{
    NPT_COMPILER_UNUSED(request);
    NPT_COMPILER_UNUSED(context);

    return m_CtrlPoint->ProcessSubscribeResponse(
        res, 
        request, 
        context, 
        response, 
        m_Service, 
        m_Userdata);
}
