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

#ifndef _PLT_FRAME_SERVER_H_
#define _PLT_FRAME_SERVER_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "PltHttpServer.h"
#include "PltFrameBuffer.h"

/*----------------------------------------------------------------------
|   forward declarations
+---------------------------------------------------------------------*/
class PLT_SocketPolicyServer;

/*----------------------------------------------------------------------
|   PLT_StreamValidator class
+---------------------------------------------------------------------*/
class PLT_StreamValidator
{
public:
    virtual ~PLT_StreamValidator() {}
    virtual bool OnNewRequestAccept(const NPT_HttpRequest&          request, 
                                    const NPT_HttpRequestContext&   context,
                                    NPT_HttpResponse&               response, 
                                    NPT_Reference<PLT_FrameBuffer>& buffer) = 0;
};

/*----------------------------------------------------------------------
|   PLT_HttpStreamRequestHandler
+---------------------------------------------------------------------*/
class PLT_HttpStreamRequestHandler : public NPT_HttpRequestHandler
{
public:
    // constructor
    PLT_HttpStreamRequestHandler(PLT_StreamValidator& stream_validator) :
        m_StreamValidator(stream_validator) {}

    // NPT_HttpRequestHandler methods
    virtual NPT_Result SetupResponse(NPT_HttpRequest&              request, 
                                     const NPT_HttpRequestContext& context,
                                     NPT_HttpResponse&             response);

private:
    PLT_StreamValidator& m_StreamValidator;
};

/*----------------------------------------------------------------------
|   PLT_FrameServer class
+---------------------------------------------------------------------*/
class PLT_FrameServer : public PLT_HttpServer
{
public:
    PLT_FrameServer(const char*          resource_name,
                    PLT_StreamValidator& stream_validator,
                    NPT_IpAddress        address = NPT_IpAddress::Any,
                    NPT_UInt16           port = 0,
                    bool                 policy_server_enabled = false);
    virtual ~PLT_FrameServer();
    
    virtual NPT_Result Start();

protected:
    PLT_SocketPolicyServer* m_PolicyServer;
    PLT_StreamValidator&    m_StreamValidator;
    bool                    m_PolicyServerEnabled;
};

#endif /* _PLT_FRAME_SERVER_H_ */
