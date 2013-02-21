/*****************************************************************
|
|   Platinum - Managed MediaServer
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
#include "StdAfx.h"
#include "PltMimeType.h"
#include "MediaServer.h"
#include "DeviceHost.h"
#include "MediaServerEventBridge.h"

Platinum::MediaServer::MediaServer(PLT_MediaServer* server) : 
    DeviceHost(*(server))
{
    RegisterEvents();
}

Platinum::MediaServer::MediaServer(String^ friendlyName) : 
    DeviceHost(*(new PLT_MediaServer(StringConv(friendlyName))))
{
    RegisterEvents();
}

Platinum::MediaServer::MediaServer(String^ friendlyName, String^ uuid) : 
    DeviceHost(*(new PLT_MediaServer(StringConv(friendlyName), false, StringConv(uuid))))
{
    RegisterEvents();
}

void Platinum::MediaServer::RegisterEvents()
{
    if (!m_pBridge)
    {
        m_pBridge = new MediaServerEventBridge(this);
    }

    PLT_MediaServer* server = (PLT_MediaServer*)(Handle.AsPointer());
    server->SetDelegate(m_pBridge);
}

Platinum::MediaServer::!MediaServer()
{

}

void Platinum::MediaServer::UpdateSystemUpdateID(Int32 update)
{
    PLT_MediaServer* server = (PLT_MediaServer*)(Handle.AsPointer());
    server->UpdateSystemUpdateID(update);
}


void Platinum::MediaServer::UpdateContainerUpdateID(String^ id, Int32 update)
{
    PLT_MediaServer* server = (PLT_MediaServer*)(Handle.AsPointer());
    server->UpdateContainerUpdateID(StringConv(id), update);
}

Int32 Platinum::MediaServer::SetResponseFilePath(HttpRequestContext^ context, HttpResponse^ response, String^ filepath)
{
    NPT_CHECK_WARNING(PLT_HttpServer::ServeFile(context->Request->Handle, 
                                                context->Handle,
                                                response->Handle, 
                                                NPT_String(StringConv(filepath))));

    /* Update content type header according to file and context */
    NPT_HttpEntity* entity = response->Handle.GetEntity();
    if (entity) entity->SetContentType(
        PLT_MimeType::GetMimeType(NPT_String(StringConv(filepath)), 
                                  &context->Handle));

    /* streaming header for DLNA */
    response->Handle.GetHeaders().SetHeader("transferMode.dlna.org", "Streaming");
    return NPT_SUCCESS;
}


Int32 Platinum::MediaServer::SetResponseData(HttpRequestContext^ context, HttpResponse^ response, array<Byte>^ data)
{
    NPT_HttpEntity* entity = response->Handle.GetEntity();
	if (entity) 
	{
		pin_ptr<Byte> pinnedBuffer = &data[0];
		entity->SetInputStream((const void*)pinnedBuffer, data->Length);
	}
	
    /* interactive header for DLNA ?*/
    response->Handle.GetHeaders().SetHeader("transferMode.dlna.org", "Interactive");
	return NPT_SUCCESS;
}