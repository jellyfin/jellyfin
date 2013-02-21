/*****************************************************************
|
|   Platinum - HTTP Server Tasks
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
 HTTP Server Tasks
 */

#ifndef _PLT_HTTP_SERVER_TASK_H_
#define _PLT_HTTP_SERVER_TASK_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "PltHttp.h"
#include "PltDatagramStream.h"
#include "PltThreadTask.h"

/*----------------------------------------------------------------------
|   PLT_HttpServerSocketTask class
+---------------------------------------------------------------------*/
/** 
 The PLT_HttpServerSocketTask class is a task used for handling one or more HTTP 
 requests from a client. It is created by a PLT_HttpListenTask instance upon
 receiving a connection request. A PLT_HttpServer will handle the delegation for
 setting up the HTTP response.
 */
class PLT_HttpServerSocketTask : public PLT_ThreadTask
{
    friend class PLT_ThreadTask;

public:
    PLT_HttpServerSocketTask(NPT_Socket* socket, bool stay_alive_forever = false);

protected:
    virtual ~PLT_HttpServerSocketTask();

protected:
    // Request callback handler
    virtual NPT_Result SetupResponse(NPT_HttpRequest&              request, 
                                     const NPT_HttpRequestContext& context,
                                     NPT_HttpResponse&             response) = 0;

    // overridables
    virtual NPT_Result GetInputStream(NPT_InputStreamReference& stream);
    virtual NPT_Result GetInfo(NPT_SocketInfo& info);

    // PLT_ThreadTask methods
    virtual void DoAbort() { if (m_Socket) m_Socket->Cancel(); }
    virtual void DoRun();

private:
    virtual NPT_Result Read(NPT_BufferedInputStreamReference& buffered_input_stream, 
                            NPT_HttpRequest*&                 request,
                            NPT_HttpRequestContext*           context = NULL);
    virtual NPT_Result Write(NPT_HttpResponse* response, 
                             bool&             keep_alive, 
                             bool              headers_only = false);
    virtual NPT_Result RespondToClient(NPT_HttpRequest&              request, 
                                       const NPT_HttpRequestContext& context,
                                       NPT_HttpResponse*&            response);
    virtual NPT_Result SendResponseHeaders(NPT_HttpResponse* response,
                                           NPT_OutputStream& output_stream,
                                           bool&             keep_alive);
    virtual NPT_Result SendResponseBody(NPT_HttpResponse* response,
                                        NPT_OutputStream& output_stream);

protected:
    NPT_Socket*         m_Socket;
    bool                m_StayAliveForever;
};

/*----------------------------------------------------------------------
|   PLT_HttpServerTask class
+---------------------------------------------------------------------*/
/**
 The PLT_HttpServerTask class is a version of PLT_HttpServerSocketTask that supports 
 delegation of HTTP request handling.
 */
class PLT_HttpServerTask : public PLT_HttpServerSocketTask
{
public:
    PLT_HttpServerTask(NPT_HttpRequestHandler* handler, 
                       NPT_Socket*             socket, 
                       bool                    keep_alive = false) : 
        PLT_HttpServerSocketTask(socket, keep_alive), m_Handler(handler) {}

protected:
    virtual ~PLT_HttpServerTask() {}

    NPT_Result SetupResponse(NPT_HttpRequest&              request, 
                             const NPT_HttpRequestContext& context,
                             NPT_HttpResponse&             response) {
        return m_Handler->SetupResponse(request, context, response);
    }

protected:
    NPT_HttpRequestHandler* m_Handler;
};

/*----------------------------------------------------------------------
|   PLT_HttpListenTask class
+---------------------------------------------------------------------*/
/**
 The PLT_HttpListenTask class is used by a PLT_HttpServer to listen for incoming
 connections and spawn a new task for handling each request.
 */
class PLT_HttpListenTask : public PLT_ThreadTask
{
public:
    PLT_HttpListenTask(NPT_HttpRequestHandler* handler, 
                       NPT_TcpServerSocket*    socket, 
                       bool                    owns_socket = true) : 
        m_Handler(handler), m_Socket(socket), m_OwnsSocket(owns_socket) {}

protected:
    virtual ~PLT_HttpListenTask() { 
        if (m_OwnsSocket && m_Socket) delete m_Socket;
    }

protected:
    // PLT_ThreadTask methods
    virtual void DoAbort() { if (m_Socket) m_Socket->Cancel(); }
    virtual void DoRun();

protected:
    NPT_HttpRequestHandler* m_Handler;
    NPT_TcpServerSocket*    m_Socket;
    bool                    m_OwnsSocket;
};

#endif /* _PLT_HTTP_SERVER_TASK_H_ */
