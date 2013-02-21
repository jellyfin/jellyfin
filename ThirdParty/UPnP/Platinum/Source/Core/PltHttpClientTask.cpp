/*****************************************************************
|
|   Platinum - HTTP Client Tasks
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
#include "PltHttpClientTask.h"
#include "PltConstants.h"

NPT_SET_LOCAL_LOGGER("platinum.core.http.clienttask")

/*----------------------------------------------------------------------
|   PLT_HttpClientSocketTask::PLT_HttpClientSocketTask
+---------------------------------------------------------------------*/
PLT_HttpClientSocketTask::PLT_HttpClientSocketTask(NPT_HttpRequest* request /* = NULL */,
                                                   bool             wait_forever /* = false */) :
    m_WaitForever(wait_forever)
{
    m_Client.SetUserAgent(*PLT_Constants::GetInstance().GetDefaultUserAgent());
    m_Client.SetTimeouts(60000, 60000, 60000);
    if (request) m_Requests.Push(request);
}

/*----------------------------------------------------------------------
|   PLT_HttpClientSocketTask::~PLT_HttpClientSocketTask
+---------------------------------------------------------------------*/
PLT_HttpClientSocketTask::~PLT_HttpClientSocketTask()
{
    // delete any outstanding requests
    NPT_HttpRequest* request;
    while (NPT_SUCCEEDED(m_Requests.Pop(request, false))) {
        delete request;
    }
}

/*----------------------------------------------------------------------
|   PLT_HttpClientSocketTask::SetHttpClientConfig
+---------------------------------------------------------------------*/
NPT_Result 
PLT_HttpClientSocketTask::SetHttpClientConfig(const NPT_HttpClient::Config& config)
{
    return m_Client.SetConfig(config);
}

/*----------------------------------------------------------------------
|   PLT_HttpClientSocketTask::AddRequest
+---------------------------------------------------------------------*/
NPT_Result
PLT_HttpClientSocketTask::AddRequest(NPT_HttpRequest* request)
{
    return m_Requests.Push(request);
}

/*----------------------------------------------------------------------
|   PLT_HttpClientSocketTask::GetNextRequest
+---------------------------------------------------------------------*/
NPT_Result
PLT_HttpClientSocketTask::GetNextRequest(NPT_HttpRequest*& request, NPT_Timeout timeout_ms)
{
    return m_Requests.Pop(request, timeout_ms);
}

/*----------------------------------------------------------------------
|   PLT_HttpClientSocketTask::DoAbort
+---------------------------------------------------------------------*/
void
PLT_HttpClientSocketTask::DoAbort()
{
    m_Client.Abort();
}

/*----------------------------------------------------------------------
|   PLT_HttpClientSocketTask::DoRun
+---------------------------------------------------------------------*/
void
PLT_HttpClientSocketTask::DoRun()
{
    NPT_HttpRequest*       request = NULL;
    NPT_HttpResponse*      response = NULL;
    NPT_HttpRequestContext context;
    NPT_Result             res;
    NPT_TimeStamp          watchdog;

    NPT_System::GetCurrentTimeStamp(watchdog);

    do {
        // pop next request or wait for one for 100ms
        while (NPT_SUCCEEDED(GetNextRequest(request, 100))) {
            response = NULL;
            
            if (IsAborting(0)) goto abort;

            // send request
            res = m_Client.SendRequest(*request, response, &context);

            NPT_String prefix = NPT_String::Format("PLT_HttpClientSocketTask::DoRun (res = %d):", res);
            PLT_LOG_HTTP_MESSAGE(NPT_LOG_LEVEL_FINER, prefix, response);

            // process response
            ProcessResponse(res, *request, context, response);

            // cleanup
            delete response;
            response = NULL;
            delete request;
            request = NULL;
        }

        // DLNA requires that we abort unanswered/unused sockets after 60 secs
        NPT_TimeStamp now;
        NPT_System::GetCurrentTimeStamp(now);
        if (now > watchdog + NPT_TimeInterval(60.)) {
            NPT_HttpConnectionManager::GetInstance()->Recycle(NULL);
            watchdog = now;
        }

    } while (m_WaitForever && !IsAborting(0));
    
abort:
    if (request) delete request;
    if (response) delete response;
}

/*----------------------------------------------------------------------
|   PLT_HttpClientSocketTask::ProcessResponse
+---------------------------------------------------------------------*/
NPT_Result 
PLT_HttpClientSocketTask::ProcessResponse(NPT_Result                    res, 
                                          const NPT_HttpRequest&        request,  
                                          const NPT_HttpRequestContext& context, 
                                          NPT_HttpResponse*             response) 
{
    NPT_COMPILER_UNUSED(request);
    NPT_COMPILER_UNUSED(context);

    NPT_LOG_FINE_1("PLT_HttpClientSocketTask::ProcessResponse (result=%d)", res);
    NPT_CHECK_WARNING(res);

    NPT_CHECK_POINTER_WARNING(response);

    // check if there's a body to read
    NPT_HttpEntity* entity;
    NPT_InputStreamReference body;
    if (!(entity = response->GetEntity()) || 
        NPT_FAILED(entity->GetInputStream(body)) ||
        body.IsNull()) {
        return NPT_SUCCESS;
    }

    // dump body into ether  
    // (if no content-length specified, read until disconnected)
    NPT_NullOutputStream output;
    NPT_CHECK_SEVERE(NPT_StreamToStreamCopy(*body, 
                                            output,
                                            0, 
                                            entity->GetContentLength()));

    return NPT_SUCCESS;
}
