/*****************************************************************
|
|   Platinum - Frame Server
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
#include "PltFrameStream.h"
#include "PltFrameServer.h"
#include "PltUtilities.h"

NPT_SET_LOCAL_LOGGER("platinum.media.server.frame")

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
#define BOUNDARY "BOUNDARYGOAWAY"

/*----------------------------------------------------------------------
|   PLT_SocketPolicyServer
+---------------------------------------------------------------------*/
class PLT_SocketPolicyServer : public NPT_Thread
{
public:
    PLT_SocketPolicyServer(const char* policy, 
                           NPT_IpPort  port = 0,
                           const char* authorized_ports = "5900") :
        m_Policy(policy),
        m_Port(port),
        m_AuthorizedPorts(authorized_ports),
        m_Aborted(false) {}
        
    ~PLT_SocketPolicyServer() {
        Stop();
    }
        
    NPT_Result Start() {
        NPT_Result result = NPT_FAILURE;
        
        // bind
        // randomly try a port for our http server
        int retries = 100;
        do {    
            int random = NPT_System::GetRandomInteger();
            NPT_IpPort port = (unsigned short)(50000 + (random % 15000));
                        
            result = m_Socket.Bind(
                NPT_SocketAddress(NPT_IpAddress::Any, m_Port?m_Port:port), 
                false);
                
            if (NPT_SUCCEEDED(result) || m_Port)
                break;
        } while (--retries > 0);

        if (NPT_FAILED(result) || retries == 0) return NPT_FAILURE;

        // remember that we're bound
        NPT_SocketInfo info;
        m_Socket.GetInfo(info);
        m_Port = info.local_address.GetPort();
        
        return NPT_Thread::Start();
    }
    
    NPT_Result Stop() {
        m_Aborted = true;
        m_Socket.Cancel();
        
        return Wait();
    }
    
    void Run() {
        do {
            // wait for a connection
            NPT_Socket* client = NULL;
            NPT_LOG_FINE_1("waiting for connection on port %d...", m_Port);
            NPT_Result result = m_Socket.WaitForNewClient(client, NPT_TIMEOUT_INFINITE);
            if (NPT_FAILED(result) || client == NULL) return;
                    
            NPT_SocketInfo client_info;
            client->GetInfo(client_info);
            NPT_LOG_FINE_2("client connected (%s)",
                client_info.local_address.ToString().GetChars(),
                client_info.remote_address.ToString().GetChars());

            // get the output stream
            NPT_OutputStreamReference output;
            client->GetOutputStream(output);

            // generate policy based on our current IP
            NPT_String policy = "<cross-domain-policy>";
            policy += "<allow-access-from domain=\""+client_info.local_address.GetIpAddress().ToString()+"\" to-ports=\""+m_AuthorizedPorts+"\"/>";
            policy += "<allow-access-from domain=\""+client_info.remote_address.GetIpAddress().ToString()+"\" to-ports=\""+m_AuthorizedPorts+"\"/>";
            policy += "</cross-domain-policy>";

            NPT_MemoryStream* mem_input = new NPT_MemoryStream();
            mem_input->Write(policy.GetChars(), policy.GetLength());
            NPT_InputStreamReference input(mem_input);
            
            NPT_StreamToStreamCopy(*input, *output);
            
            
            delete client;
        } while (!m_Aborted);
    }
    
    NPT_TcpServerSocket m_Socket;
    NPT_String          m_Policy;
    NPT_IpPort          m_Port;
    NPT_String          m_AuthorizedPorts;
    bool                m_Aborted;
};

/*----------------------------------------------------------------------
|   PLT_HttpStreamRequestHandler::SetupResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_HttpStreamRequestHandler::SetupResponse(NPT_HttpRequest&              request, 
                                            const NPT_HttpRequestContext& context,
                                            NPT_HttpResponse&             response)
{
    PLT_LOG_HTTP_MESSAGE(NPT_LOG_LEVEL_FINE, "PLT_HttpStreamRequestHandler::SetupResponse:", &request);

    if (request.GetMethod().Compare("GET") && 
        request.GetMethod().Compare("HEAD")) {
        return NPT_FAILURE;
    }

    NPT_Reference<PLT_FrameBuffer> buffer;
    if (!m_StreamValidator.OnNewRequestAccept(request, context, response, buffer)) {
        return NPT_ERROR_NO_SUCH_ITEM;
    }

    response.SetProtocol(NPT_HTTP_PROTOCOL_1_0);
    response.GetHeaders().SetHeader(NPT_HTTP_HEADER_CONNECTION, "close");
    response.GetHeaders().SetHeader("Cache-Control", "no-store, no-cache, must-revalidate, pre-check=0, post-check=0, max-age=0");
    response.GetHeaders().SetHeader("Pragma", "no-cache");
    response.GetHeaders().SetHeader("Expires", "Tue, 4 Jan 2000 02:43:05 GMT");

    // HEAD request has no entity or if status code is not 2xx
    if (!request.GetMethod().Compare("HEAD") || response.GetStatusCode()/100 != 2) 
        return NPT_SUCCESS;
    
    NPT_HttpEntity* entity = response.GetEntity();
    NPT_CHECK_POINTER_FATAL(entity);
    entity->SetContentType("multipart/x-mixed-replace;boundary=" BOUNDARY);

    NPT_InputStreamReference body(new PLT_InputFrameStream(buffer, BOUNDARY));
    entity->SetInputStream(body, false);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_FrameServer::PLT_FrameServer
+---------------------------------------------------------------------*/
PLT_FrameServer::PLT_FrameServer(const char*          resource_name,
                                 PLT_StreamValidator& stream_validator,
                                 NPT_IpAddress        address,
                                 NPT_UInt16           port,
                                 bool                 policy_server_enabled) :	
    PLT_HttpServer(address, port, false),
    m_PolicyServer(NULL),
    m_StreamValidator(stream_validator),
    m_PolicyServerEnabled(policy_server_enabled)
{
    NPT_String resource(resource_name);
    resource.Trim("/\\");
    AddRequestHandler(
        new PLT_HttpStreamRequestHandler(stream_validator), 
        "/" + resource, 
        true,
        true);
}

/*----------------------------------------------------------------------
|   PLT_FrameServer::~PLT_FrameServer
+---------------------------------------------------------------------*/
PLT_FrameServer::~PLT_FrameServer()
{
    delete m_PolicyServer;
}

/*----------------------------------------------------------------------
|   PLT_FrameServer::Start
+---------------------------------------------------------------------*/
NPT_Result
PLT_FrameServer::Start()
{
    // start main server so we can get the listening port
    NPT_CHECK_SEVERE(PLT_HttpServer::Start());
    
    // start the xml socket policy server for flash
    if (m_PolicyServerEnabled) {
        m_PolicyServer = new PLT_SocketPolicyServer(
            "", 
            8989, 
            "5900,"+NPT_String::FromInteger(GetPort()));
        NPT_CHECK_SEVERE(m_PolicyServer->Start());
    }
    
    return NPT_SUCCESS;
}


