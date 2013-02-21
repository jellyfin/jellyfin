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

/** @file
 HTTP Client tasks
 */

#ifndef _PLT_HTTP_CLIENT_TASK_H_
#define _PLT_HTTP_CLIENT_TASK_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "PltHttp.h"
#include "PltThreadTask.h"

/*----------------------------------------------------------------------
|   PLT_HttpClientSocketTask class
+---------------------------------------------------------------------*/
/**
 The PLT_HttpClientSocketTask class is the base class used to send a HTTP request
 asynchronously using a task (thread). It supports persistent connections
 and HTTP pipelining with automatic fallback and reconnection when HTTP 1.0 
 is used.
 */
class PLT_HttpClientSocketTask : public PLT_ThreadTask
{
friend class PLT_ThreadTask;

public:
    PLT_HttpClientSocketTask(NPT_HttpRequest* request = NULL, 
                             bool             wait_forever = false);

    virtual NPT_Result AddRequest(NPT_HttpRequest* request);
    virtual NPT_Result SetHttpClientConfig(const NPT_HttpClient::Config& config);

protected:
    virtual ~PLT_HttpClientSocketTask();

protected:
    // PLT_ThreadTask methods
    virtual void DoAbort();
    virtual void DoRun();

    virtual NPT_Result ProcessResponse(NPT_Result                    res, 
                                       const NPT_HttpRequest&        request, 
                                       const NPT_HttpRequestContext& context,
                                       NPT_HttpResponse*             response);

private:
    NPT_Result GetNextRequest(NPT_HttpRequest*& request, NPT_Timeout timeout_ms);

protected:
    NPT_HttpClient              m_Client;
    bool                        m_WaitForever;
    NPT_Queue<NPT_HttpRequest>  m_Requests;
};

/*----------------------------------------------------------------------
|   PLT_HttpClientTask class
+---------------------------------------------------------------------*/
/**
 The PLT_HttpClientTask class is a templatized version of PLT_HttpClientSocketTask
 to support arbitrary delegation of HTTP response handling.
 */
template <class T>
class PLT_HttpClientTask : public PLT_HttpClientSocketTask
{
public:
    PLT_HttpClientTask<T>(const NPT_HttpUrl& url, T* data) : 
        PLT_HttpClientSocketTask(new NPT_HttpRequest(url, 
                                                     "GET", 
                                                     NPT_HTTP_PROTOCOL_1_1)), 
                                 m_Data(data) {}
 protected:
    virtual ~PLT_HttpClientTask<T>() {}

protected:
    // PLT_HttpClientSocketTask method
    NPT_Result ProcessResponse(NPT_Result                    res, 
                               const NPT_HttpRequest&        request, 
                               const NPT_HttpRequestContext& context, 
                               NPT_HttpResponse*             response) {
        return m_Data->ProcessResponse(res, request, context, response);
    }

protected:
    T* m_Data;
};

#endif /* _PLT_HTTP_CLIENT_TASK_H_ */
