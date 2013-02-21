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

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "PltTaskManager.h"
#include "PltHttpServer.h"
#include "PltHttp.h"
#include "PltVersion.h"
#include "PltUtilities.h"
#include "PltProtocolInfo.h"
#include "PltMimeType.h"

NPT_SET_LOCAL_LOGGER("platinum.core.http.server")

/*----------------------------------------------------------------------
|   PLT_HttpServer::PLT_HttpServer
+---------------------------------------------------------------------*/
PLT_HttpServer::PLT_HttpServer(NPT_IpAddress address,
                               NPT_IpPort    port,
                               bool          allow_random_port_on_bind_failure,   /* = false */
                               NPT_Cardinal  max_clients,                         /* = 50 */
                               bool          reuse_address) :                     /* = false */
    m_TaskManager(new PLT_TaskManager(max_clients)),
    m_Address(address),
    m_Port(port),
    m_AllowRandomPortOnBindFailure(allow_random_port_on_bind_failure),
    m_ReuseAddress(reuse_address),
    m_HttpListenTask(NULL),
    m_Aborted(false)
{
}

/*----------------------------------------------------------------------
|   PLT_HttpServer::~PLT_HttpServer
+---------------------------------------------------------------------*/
PLT_HttpServer::~PLT_HttpServer()
{ 
    Stop();
    delete m_TaskManager;
}

/*----------------------------------------------------------------------
|   PLT_HttpServer::Start
+---------------------------------------------------------------------*/
NPT_Result
PLT_HttpServer::Start()
{
    NPT_Result res = NPT_FAILURE;
    
    // we can't restart an aborted server
    if (m_Aborted) return NPT_ERROR_INVALID_STATE;
    
    // if we're given a port for our http server, try it
    if (m_Port) {
        res = SetListenPort(m_Port, m_ReuseAddress);
        // return right away if failed and not allowed to try again randomly
        if (NPT_FAILED(res) && !m_AllowRandomPortOnBindFailure) {
            NPT_CHECK_SEVERE(res);
        }
    }
    
    // try random port now
    if (!m_Port || NPT_FAILED(res)) {
        int retries = 100;
        do {    
            int random = NPT_System::GetRandomInteger();
            int port = (unsigned short)(1024 + (random % 1024));
            if (NPT_SUCCEEDED(SetListenPort(port, m_ReuseAddress))) {
                break;
            }
        } while (--retries > 0);

        if (retries == 0) NPT_CHECK_SEVERE(NPT_FAILURE);
    }

    // keep track of port server has successfully bound
    m_Port = m_BoundPort;

    // Tell server to try to listen to more incoming sockets
    // (this could fail silently)
    if (m_TaskManager->GetMaxTasks() > 20) {
        m_Socket.Listen(m_TaskManager->GetMaxTasks());
    }
    
    // start a task to listen for incoming connections
    // and keep it around so we can abort the server
    m_HttpListenTask = new PLT_HttpListenTask(this, &m_Socket, false);
    m_TaskManager->StartTask(m_HttpListenTask, NULL, false);

    NPT_SocketInfo info;
    m_Socket.GetInfo(info);
    NPT_LOG_INFO_2("HttpServer listening on %s:%d", 
        (const char*)info.local_address.GetIpAddress().ToString(), 
        m_Port);
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_HttpServer::Stop
+---------------------------------------------------------------------*/
NPT_Result
PLT_HttpServer::Stop()
{
    m_Aborted = true;
    
    if (m_HttpListenTask) {
        m_HttpListenTask->Kill();
        m_HttpListenTask = NULL;
    }

    // stop all other pending tasks 
    m_TaskManager->StopAllTasks();
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_HttpServer::SetupResponse
+---------------------------------------------------------------------*/
NPT_Result 
PLT_HttpServer::SetupResponse(NPT_HttpRequest&              request, 
                              const NPT_HttpRequestContext& context,
                              NPT_HttpResponse&             response) 
{
    NPT_String prefix = NPT_String::Format("PLT_HttpServer::SetupResponse %s request from %s for \"%s\"", 
        (const char*) request.GetMethod(),
        (const char*) context.GetRemoteAddress().ToString(),
        (const char*) request.GetUrl().ToString());
    PLT_LOG_HTTP_MESSAGE(NPT_LOG_LEVEL_FINE, prefix, &request);

    NPT_List<NPT_HttpRequestHandler*> handlers = FindRequestHandlers(request);
    if (handlers.GetItemCount() == 0) return NPT_ERROR_NO_SUCH_ITEM;

    // ask the handler to setup the response
    NPT_Result result = (*handlers.GetFirstItem())->SetupResponse(request, context, response);
    
    // DLNA compliance
    PLT_UPnPMessageHelper::SetDate(response);
    if (request.GetHeaders().GetHeader("Accept-Language")) {
        response.GetHeaders().SetHeader("Content-Language", "en");
    }
    return result;
}

/*----------------------------------------------------------------------
|   PLT_HttpServer::ServeFile
+---------------------------------------------------------------------*/
NPT_Result 
PLT_HttpServer::ServeFile(const NPT_HttpRequest&        request, 
                          const NPT_HttpRequestContext& context,
                          NPT_HttpResponse&             response,
                          NPT_String                    file_path) 
{
    NPT_InputStreamReference stream;
    NPT_File                 file(file_path);
    NPT_FileInfo             file_info;
    
    // prevent hackers from accessing files outside of our root
    if ((file_path.Find("/..") >= 0) || (file_path.Find("\\..") >= 0) ||
        NPT_FAILED(NPT_File::GetInfo(file_path, &file_info))) {
        return NPT_ERROR_NO_SUCH_ITEM;
    }
    
    // check for range requests
    const NPT_String* range_spec = request.GetHeaders().GetHeaderValue(NPT_HTTP_HEADER_RANGE);
    
    // handle potential 304 only if range header not set
    NPT_DateTime  date;
    NPT_TimeStamp timestamp;
    if (NPT_SUCCEEDED(PLT_UPnPMessageHelper::GetIfModifiedSince((NPT_HttpMessage&)request, date)) &&
        !range_spec) {
        date.ToTimeStamp(timestamp);
        
        NPT_LOG_INFO_5("File %s timestamps: request=%d (%s) vs file=%d (%s)", 
                       (const char*)request.GetUrl().GetPath(),
                       (NPT_UInt32)timestamp.ToSeconds(),
                       (const char*)date.ToString(),
                       (NPT_UInt32)file_info.m_ModificationTime,
                       (const char*)NPT_DateTime(file_info.m_ModificationTime).ToString());
        
        if (timestamp >= file_info.m_ModificationTime) {
            // it's a match
            NPT_LOG_FINE_1("Returning 304 for %s", request.GetUrl().GetPath().GetChars());
            response.SetStatus(304, "Not Modified", NPT_HTTP_PROTOCOL_1_1);
            return NPT_SUCCESS;
        }
    }
    
    // open file
    if (NPT_FAILED(file.Open(NPT_FILE_OPEN_MODE_READ)) || 
        NPT_FAILED(file.GetInputStream(stream))        ||
        stream.IsNull()) {
        return NPT_ERROR_NO_SUCH_ITEM;
    }
    
    // set Last-Modified and Cache-Control headers
    if (file_info.m_ModificationTime) {
        NPT_DateTime last_modified = NPT_DateTime(file_info.m_ModificationTime);
        response.GetHeaders().SetHeader("Last-Modified", last_modified.ToString(NPT_DateTime::FORMAT_RFC_1123), true);
        response.GetHeaders().SetHeader("Cache-Control", "max-age=0,must-revalidate", true);
        //response.GetHeaders().SetHeader("Cache-Control", "max-age=1800", true);
    }
    
    PLT_HttpRequestContext tmp_context(request, context);
    return ServeStream(request, context, response, stream, PLT_MimeType::GetMimeType(file_path, &tmp_context));
}

/*----------------------------------------------------------------------
|   PLT_HttpServer::ServeStream
+---------------------------------------------------------------------*/
NPT_Result 
PLT_HttpServer::ServeStream(const NPT_HttpRequest&        request, 
                            const NPT_HttpRequestContext& context,
                            NPT_HttpResponse&             response,
                            NPT_InputStreamReference&     body, 
                            const char*                   content_type) 
{    
    if (body.IsNull()) return NPT_FAILURE;
    
    // set date
    NPT_TimeStamp now;
    NPT_System::GetCurrentTimeStamp(now);
    response.GetHeaders().SetHeader("Date", NPT_DateTime(now).ToString(NPT_DateTime::FORMAT_RFC_1123), true);
    
    // get entity
    NPT_HttpEntity* entity = response.GetEntity();
    NPT_CHECK_POINTER_FATAL(entity);
    
    // set the content type
    entity->SetContentType(content_type);
    
    // check for range requests
    const NPT_String* range_spec = request.GetHeaders().GetHeaderValue(NPT_HTTP_HEADER_RANGE);
    
    // setup entity body
    NPT_CHECK(NPT_HttpFileRequestHandler::SetupResponseBody(response, body, range_spec));
              
    // set some default headers
    if (response.GetEntity()->GetTransferEncoding() != NPT_HTTP_TRANSFER_ENCODING_CHUNKED) {
        // set but don't replace Accept-Range header only if body is seekable
        NPT_Position offset;
        if (NPT_SUCCEEDED(body->Tell(offset)) && NPT_SUCCEEDED(body->Seek(offset))) {
            response.GetHeaders().SetHeader(NPT_HTTP_HEADER_ACCEPT_RANGES, "bytes", false); 
        }
    }
    
    // set getcontentFeatures.dlna.org
    const NPT_String* value = request.GetHeaders().GetHeaderValue("getcontentFeatures.dlna.org");
    if (value) {
        PLT_HttpRequestContext tmp_context(request, context);
        const char* dlna = PLT_ProtocolInfo::GetDlnaExtension(entity->GetContentType(),
                                                              &tmp_context);
        if (dlna) response.GetHeaders().SetHeader("ContentFeatures.DLNA.ORG", dlna, false);
    }
    
    // transferMode.dlna.org
    value = request.GetHeaders().GetHeaderValue("transferMode.dlna.org");
    if (value) {
        // Interactive mode not supported?
        /*if (value->Compare("Interactive", true) == 0) {
            response.SetStatus(406, "Not Acceptable");
            return NPT_SUCCESS;
        }*/
        
        response.GetHeaders().SetHeader("TransferMode.DLNA.ORG", value->GetChars(), false);
    } else {
        response.GetHeaders().SetHeader("TransferMode.DLNA.ORG", "Streaming", false);
    }
    
    if (request.GetHeaders().GetHeaderValue("TimeSeekRange.dlna.org")) {
        response.SetStatus(406, "Not Acceptable");
        return NPT_SUCCESS;
    }
    
    return NPT_SUCCESS;
}
