/*****************************************************************
|
|   Platinum - HTTP Server
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
 HTTP Server
 */

#ifndef _PLT_HTTP_SERVER_H_
#define _PLT_HTTP_SERVER_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "PltHttpServerTask.h"

/*----------------------------------------------------------------------
|   PLT_HttpServer class
+---------------------------------------------------------------------*/
/**
 The PLT_HttpServer class provides an asynchronous way to handle multiple HTTP requests
 concurrently. Pipelining requests and keep-alive connections are supported.
 */
class PLT_HttpServer : public NPT_HttpRequestHandler,
                       public NPT_HttpServer
{
public:
    PLT_HttpServer(NPT_IpAddress address = NPT_IpAddress::Any,
                   NPT_IpPort    port = 0,
                   bool          allow_random_port_on_bind_failure = false,
                   NPT_Cardinal  max_clients = 50,
                   bool          reuse_address = false);
    virtual ~PLT_HttpServer();
    
    // class methods
    static NPT_Result ServeFile(const NPT_HttpRequest&        request, 
                                const NPT_HttpRequestContext& context,
                                NPT_HttpResponse&             response, 
                                NPT_String                    file_path);
    static NPT_Result ServeStream(const NPT_HttpRequest&        request, 
                                  const NPT_HttpRequestContext& context,
                                  NPT_HttpResponse&             response,
                                  NPT_InputStreamReference&     stream, 
                                  const char*                   content_type);

    // NPT_HttpRequestHandler methods
    virtual NPT_Result SetupResponse(NPT_HttpRequest&              request,
                                     const NPT_HttpRequestContext& context,
                                     NPT_HttpResponse&             response);

    // methods
    virtual NPT_Result   Start();
    virtual NPT_Result   Stop();
    virtual unsigned int GetPort() { return m_Port; }

private:
    PLT_TaskManager*    m_TaskManager;
    NPT_IpAddress       m_Address;
    NPT_IpPort          m_Port;
    bool                m_AllowRandomPortOnBindFailure;
    bool                m_ReuseAddress;
    PLT_HttpListenTask* m_HttpListenTask;
    bool                m_Aborted;
};

#endif /* _PLT_HTTP_SERVER_H_ */
