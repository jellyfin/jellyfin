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

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "PltHttpServerTask.h"
#include "PltHttp.h"
#include "PltVersion.h"

NPT_SET_LOCAL_LOGGER("platinum.core.http.servertask")

/*----------------------------------------------------------------------
|   external references
+---------------------------------------------------------------------*/
extern NPT_String HttpServerHeader;
const char* const PLT_HTTP_DEFAULT_403_HTML = "<html><head><title>403 Forbidden</title></head><body><h1>Forbidden</h1><p>Access to this URL is forbidden.</p></html>";
const char* const PLT_HTTP_DEFAULT_404_HTML = "<html><head><title>404 Not Found</title></head><body><h1>Not Found</h1><p>The requested URL was not found on this server.</p></html>";
const char* const PLT_HTTP_DEFAULT_500_HTML = "<html><head><title>500 Internal Error</title></head><body><h1>Internal Error</h1><p>The server encountered an unexpected condition which prevented it from fulfilling the request.</p></html>";

/*----------------------------------------------------------------------
|   PLT_HttpServerSocketTask::PLT_HttpServerSocketTask
+---------------------------------------------------------------------*/
PLT_HttpServerSocketTask::PLT_HttpServerSocketTask(NPT_Socket* socket, 
                                                   bool        stay_alive_forever) :
    m_Socket(socket),
    m_StayAliveForever(stay_alive_forever)
{
    // needed for PS3 that is some case will request data every 35 secs and 
    // won't like it if server disconnected too early
    m_Socket->SetReadTimeout(60000);
    m_Socket->SetWriteTimeout(60000);
}

/*----------------------------------------------------------------------
|   PLT_HttpServerSocketTask::~PLT_HttpServerSocketTask
+---------------------------------------------------------------------*/
PLT_HttpServerSocketTask::~PLT_HttpServerSocketTask() 
{
    if (m_Socket) delete m_Socket;
}

/*----------------------------------------------------------------------
|   PLT_HttpServerSocketTask::DoRun
+---------------------------------------------------------------------*/
void
PLT_HttpServerSocketTask::DoRun()
{
    NPT_BufferedInputStreamReference buffered_input_stream;
    NPT_HttpRequestContext           context;
    NPT_Result                       res = NPT_SUCCESS;
    bool                             headers_only;
    bool                             keep_alive = false;

    // create a buffered input stream to parse HTTP request
    NPT_InputStreamReference input_stream;
    NPT_CHECK_LABEL_SEVERE(GetInputStream(input_stream), done);
    NPT_CHECK_POINTER_LABEL_FATAL(input_stream.AsPointer(), done);
    buffered_input_stream = new NPT_BufferedInputStream(input_stream);

    while (!IsAborting(0)) {
        NPT_HttpRequest*  request = NULL;
        NPT_HttpResponse* response = NULL;

        // reset keep-alive to exit task on read failure
        keep_alive = false;

        // wait for a request
        res = Read(buffered_input_stream, request, &context);
        if (NPT_FAILED(res) || (request == NULL)) 
            goto cleanup;
        
        // process request and setup response
        res = RespondToClient(*request, context, response);
        if (NPT_FAILED(res) || (response == NULL)) 
            goto cleanup;

        // check if client requested keep-alive
        keep_alive = PLT_HttpHelper::IsConnectionKeepAlive(*request);
        headers_only = request->GetMethod() == NPT_HTTP_METHOD_HEAD;

        // send response, pass keep-alive request from client
        // (it can be overridden if response handler did not allow it)
        res = Write(response, keep_alive, headers_only);

        // on write error, reset keep_alive so we can close this connection
        if (NPT_FAILED(res)) keep_alive = false;

cleanup:
        // cleanup
        delete request;
        delete response;

        if (!keep_alive && !m_StayAliveForever) {
            return;
        }
    }
done:
    return;
}

/*----------------------------------------------------------------------
|   PLT_HttpServerSocketTask::GetInputStream
+---------------------------------------------------------------------*/
NPT_Result
PLT_HttpServerSocketTask::GetInputStream(NPT_InputStreamReference& stream)
{
    return m_Socket->GetInputStream(stream);
}

/*----------------------------------------------------------------------
|   PLT_HttpServerSocketTask::GetInfo
+---------------------------------------------------------------------*/
NPT_Result
PLT_HttpServerSocketTask::GetInfo(NPT_SocketInfo& info)
{
    return m_Socket->GetInfo(info);
}

/*----------------------------------------------------------------------
|   PLT_HttpServerSocketTask::Read
+---------------------------------------------------------------------*/
NPT_Result
PLT_HttpServerSocketTask::Read(NPT_BufferedInputStreamReference& buffered_input_stream, 
                               NPT_HttpRequest*&                 request,
                               NPT_HttpRequestContext*           context) 
{
    NPT_SocketInfo info;
    GetInfo(info);
    
    // update context with socket info if needed
    if (context) {
        context->SetLocalAddress(info.local_address);
        context->SetRemoteAddress(info.remote_address);
    }

    // put back in buffered mode to be able to parse HTTP request properly
    buffered_input_stream->SetBufferSize(NPT_BUFFERED_BYTE_STREAM_DEFAULT_SIZE);

    // parse request
    NPT_Result res = NPT_HttpRequest::Parse(*buffered_input_stream, &info.local_address, request);
    if (NPT_FAILED(res) || !request) {
        // only log if not timeout
        res = NPT_FAILED(res)?res:NPT_FAILURE;
        if (res != NPT_ERROR_TIMEOUT && res != NPT_ERROR_EOS) NPT_CHECK_WARNING(res);
        return res;
    }
    
    // update context with socket info again 
    // to refresh the remote address in case it was a non connected udp socket
    GetInfo(info);
    if (context) {
        context->SetLocalAddress(info.local_address);
        context->SetRemoteAddress(info.remote_address);
    }

    // return right away if no body is expected
    if (request->GetMethod() == NPT_HTTP_METHOD_GET || 
        request->GetMethod() == NPT_HTTP_METHOD_HEAD) {
        return NPT_SUCCESS;
    }

    // create an entity
    NPT_HttpEntity* request_entity = new NPT_HttpEntity(request->GetHeaders());
    request->SetEntity(request_entity);

    NPT_MemoryStream* body_stream = new NPT_MemoryStream();
    request_entity->SetInputStream((NPT_InputStreamReference)body_stream);

    // unbuffer the stream to read body fast
    buffered_input_stream->SetBufferSize(0);

    // check for chunked Transfer-Encoding
    if (request_entity->GetTransferEncoding() == "chunked") {
        NPT_CHECK_SEVERE(NPT_StreamToStreamCopy(
            *NPT_InputStreamReference(new NPT_HttpChunkedInputStream(buffered_input_stream)).AsPointer(), 
            *body_stream));

        request_entity->SetTransferEncoding(NULL);
    } else if (request_entity->GetContentLength()) {
        // a request with a body must always have a content length if not chunked
        NPT_CHECK_SEVERE(NPT_StreamToStreamCopy(
            *buffered_input_stream.AsPointer(), 
            *body_stream, 
            0, 
            request_entity->GetContentLength()));
    } else {
        request->SetEntity(NULL);
    }

    // rebuffer the stream
    buffered_input_stream->SetBufferSize(NPT_BUFFERED_BYTE_STREAM_DEFAULT_SIZE);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_HttpServerSocketTask::RespondToClient
+---------------------------------------------------------------------*/
NPT_Result 
PLT_HttpServerSocketTask::RespondToClient(NPT_HttpRequest&              request, 
                                          const NPT_HttpRequestContext& context,
                                          NPT_HttpResponse*&            response)
{
    NPT_Result result   = NPT_ERROR_NO_SUCH_ITEM;
    
    // reset output params first
    response = NULL;
    
    // prepare the response body
    NPT_HttpEntity* body = new NPT_HttpEntity();
    response = new NPT_HttpResponse(200, "OK", NPT_HTTP_PROTOCOL_1_1);
    response->SetEntity(body);

    // ask to setup the response
    result = SetupResponse(request, context, *response);

    // handle result
    if (result == NPT_ERROR_NO_SUCH_ITEM) {
        body->SetInputStream(PLT_HTTP_DEFAULT_404_HTML);
        body->SetContentType("text/html");
        response->SetStatus(404, "Not Found");
    } else if (result == NPT_ERROR_PERMISSION_DENIED) {
        body->SetInputStream(PLT_HTTP_DEFAULT_403_HTML);
        body->SetContentType("text/html");
        response->SetStatus(403, "Forbidden");
    } else if (result == NPT_ERROR_TERMINATED) {
        // mark that we want to exit
        delete response;
        response = NULL;
    } else if (NPT_FAILED(result)) {
        body->SetInputStream(PLT_HTTP_DEFAULT_500_HTML);
        body->SetContentType("text/html");
        response->SetStatus(500, "Internal Error");
    }
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_HttpServerSocketTask::SendResponseHeaders
+---------------------------------------------------------------------*/
NPT_Result
PLT_HttpServerSocketTask::SendResponseHeaders(NPT_HttpResponse* response,
                                              NPT_OutputStream& output_stream,
                                              bool&             keep_alive)
{
    // add any headers that may be missing
    NPT_HttpHeaders& headers = response->GetHeaders();

    // get the request entity to set additional headers
    NPT_InputStreamReference body_stream;
    NPT_HttpEntity* entity = response->GetEntity();
    if (entity && NPT_SUCCEEDED(entity->GetInputStream(body_stream))) {
        // set the content length if known
        if (entity->ContentLengthIsKnown()) {
            headers.SetHeader(NPT_HTTP_HEADER_CONTENT_LENGTH,
                              NPT_String::FromIntegerU(entity->GetContentLength()));
        }

        // content type
        NPT_String content_type = entity->GetContentType();
        if (!content_type.IsEmpty()) {
            headers.SetHeader(NPT_HTTP_HEADER_CONTENT_TYPE, content_type);
        }

        // content encoding
        NPT_String content_encoding = entity->GetContentEncoding();
        if (!content_encoding.IsEmpty()) {
            headers.SetHeader(NPT_HTTP_HEADER_CONTENT_ENCODING, content_encoding);
        }
        
        // transfer encoding
        const NPT_String& transfer_encoding = entity->GetTransferEncoding();
        if (!transfer_encoding.IsEmpty()) {
            headers.SetHeader(NPT_HTTP_HEADER_TRANSFER_ENCODING, transfer_encoding);
        }
        
    } else if (!headers.GetHeader(NPT_HTTP_HEADER_CONTENT_LENGTH)) {
        // force content length to 0 if there is no message body
		// (necessary for 1.1 or 1.0 with keep-alive connections)
        headers.SetHeader(NPT_HTTP_HEADER_CONTENT_LENGTH, "0");
    }

    const NPT_String* content_length  = headers.GetHeaderValue(NPT_HTTP_HEADER_CONTENT_LENGTH);
    const NPT_String* transfer_encoding  = headers.GetHeaderValue(NPT_HTTP_HEADER_TRANSFER_ENCODING);
    const NPT_String* connection_header  = headers.GetHeaderValue(NPT_HTTP_HEADER_CONNECTION);
    if (keep_alive) {
        if (connection_header && connection_header->Compare("close") == 0) {
            keep_alive = false;
        } else {
            // the request says client supports keep-alive
            // but override if response has content-length header or
            // transfer chunked encoding
            keep_alive = content_length ||
                (transfer_encoding && transfer_encoding->Compare(NPT_HTTP_TRANSFER_ENCODING_CHUNKED) == 0);
        }
    }

    // only write keep-alive header for 1.1 if it's close
    NPT_String protocol = response->GetProtocol();
    if (protocol.Compare(NPT_HTTP_PROTOCOL_1_0, true) == 0 || !keep_alive) {
        headers.SetHeader(NPT_HTTP_HEADER_CONNECTION, keep_alive?"keep-alive":"close", true);
    }
    headers.SetHeader(NPT_HTTP_HEADER_SERVER, PLT_HTTP_DEFAULT_SERVER, false); // set but don't replace

    PLT_LOG_HTTP_MESSAGE(NPT_LOG_LEVEL_FINE, "PLT_HttpServerSocketTask::Write", response);

    // create a memory stream to buffer the headers
    NPT_MemoryStream header_stream;
    response->Emit(header_stream);

    // send the headers
    NPT_CHECK_WARNING(output_stream.WriteFully(header_stream.GetData(), header_stream.GetDataSize()));

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_HttpServerSocketTask::SendResponseBody
+---------------------------------------------------------------------*/
NPT_Result
PLT_HttpServerSocketTask::SendResponseBody(NPT_HttpResponse* response,
                                           NPT_OutputStream& output_stream)
{
    NPT_HttpEntity* entity = response->GetEntity();
    if (!entity) return NPT_SUCCESS;

    NPT_InputStreamReference body_stream;
    entity->GetInputStream(body_stream);
    if (body_stream.IsNull()) return NPT_SUCCESS;

    // check for chunked transfer encoding
    NPT_OutputStream* dest = &output_stream;
    if (entity->GetTransferEncoding() == NPT_HTTP_TRANSFER_ENCODING_CHUNKED) {
        dest = new NPT_HttpChunkedOutputStream(output_stream);
    }

    // send body
    NPT_LOG_FINE_1("sending body stream, %lld bytes", entity->GetContentLength());
    NPT_LargeSize bytes_written = 0;
    NPT_Result result = NPT_StreamToStreamCopy(*body_stream, *dest, 0, entity->GetContentLength(), &bytes_written); /* passing 0 if content length is unknown will read until nothing is left */
    if (NPT_FAILED(result)) {
        NPT_LOG_FINE_3("body stream only partially sent, %lld bytes (%d:%s)",
                       bytes_written,
                       result,
                       NPT_ResultText(result));
    }

    // flush to write out any buffered data left in chunked output if used
    dest->Flush();

    // cleanup (this will send zero size chunk followed by CRLF)
    if (dest != &output_stream) delete dest;

    return result;
}

/*----------------------------------------------------------------------
|   PLT_HttpServerSocketTask::Write
+---------------------------------------------------------------------*/
NPT_Result
PLT_HttpServerSocketTask::Write(NPT_HttpResponse* response, 
                                bool&             keep_alive, 
                                bool              headers_only /* = false */) 
{
    // get the socket output stream
    NPT_OutputStreamReference output_stream;
    NPT_CHECK_WARNING(m_Socket->GetOutputStream(output_stream));

    // send headers
    NPT_CHECK_WARNING(SendResponseHeaders(response, *output_stream, keep_alive));

    // send the body
    if (!headers_only) {
        NPT_CHECK_WARNING(SendResponseBody(response, *output_stream));
    }
    
    // flush
    output_stream->Flush();

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_HttpListenTask::DoRun
+---------------------------------------------------------------------*/
void 
PLT_HttpListenTask::DoRun() 
{
    while (!IsAborting(0)) {
        NPT_Socket* client = NULL;
        NPT_Result  result = m_Socket->WaitForNewClient(client, 5000);
        if (NPT_FAILED(result)) {
            // cleanup just in case
            if (client) delete client;
            
            // normal error
            if (result == NPT_ERROR_TIMEOUT) continue;
            
            // exit on other errors ?
            NPT_LOG_WARNING_2("PLT_HttpListenTask exiting with %d (%s)", result, NPT_ResultText(result));
            break;
        } else {
            PLT_ThreadTask* task = new PLT_HttpServerTask(m_Handler, client);
            if (NPT_FAILED(m_TaskManager->StartTask(task))) {
                task->Kill();
                delete client;
            }
        }
    }
}
