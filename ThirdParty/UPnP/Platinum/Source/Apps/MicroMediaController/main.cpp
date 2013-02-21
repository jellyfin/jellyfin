/*****************************************************************
|
|   Platinum - main
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
#include "PltMicroMediaController.h"
#include "PltFileMediaServer.h"
#include "PltMediaRenderer.h"
#include "PltXbox360.h"
#include "PltVersion.h"

#include <stdio.h>
#include <string.h>
#include <stdlib.h>

//#define HAS_RENDERER 1
//#define HAS_SERVER 1
//#define SIMULATE_XBOX_360 1
//#define SIMULATE_PS3 1
//#define BROADCAST_EXTRA 1

/*----------------------------------------------------------------------
|   main
+---------------------------------------------------------------------*/
int main(void)
{
    // setup Neptune logging
    NPT_LogManager::GetDefault().Configure("plist:.level=INFO;.handlers=ConsoleHandler;.ConsoleHandler.colors=off;.ConsoleHandler.filter=63");

    // Create upnp engine
    PLT_UPnP upnp;
    
#ifdef SIMULATE_XBOX_360
    // override default headers
    NPT_HttpClient::m_UserAgentHeader = "Xbox/2.0.8955.0 UPnP/1.0 Xbox/2.0.8955.0";
    NPT_HttpServer::m_ServerHeader    = "Xbox/2.0.8955.0 UPnP/1.0 Xbox/2.0.8955.0";
#endif

#ifdef SIMULATE_PS3
    // TODO: We need a way to add an extra header to all HTTP requests
    //X-AV-Client-Info: av=5.0; cn="Sony Computer Entertainment Inc."; mn="PLAYSTATION 3"; mv="1.0";
#endif

    // Create control point
    PLT_CtrlPointReference ctrlPoint(new PLT_CtrlPoint());

    // Create controller
    PLT_MicroMediaController controller(ctrlPoint);

#ifdef HAS_SERVER
    // create device
    PLT_DeviceHostReference server(
        new PLT_FileMediaServer("C:\\Music", 
                                "Platinum UPnP Media Server"));

    server->m_ModelDescription = "Platinum File Media Server";
    server->m_ModelURL = "http://www.plutinosoft.com/";
    server->m_ModelNumber = "1.0";
    server->m_ModelName = "Platinum File Media Server";
    server->m_Manufacturer = "Plutinosoft";
    server->m_ManufacturerURL = "http://www.plutinosoft.com/";

    // add device
    upnp.AddDevice(server);

    // remove device uuid from ctrlpoint
    ctrlPoint->IgnoreUUID(server->GetUUID());
#endif

    // add control point to upnp engine and start it
    upnp.AddCtrlPoint(ctrlPoint);
    upnp.Start();

#ifdef BROADCAST_EXTRA
    // tell control point to perform extra broadcast discover every 6 secs
    // in case our device doesn't support multicast
    ctrlPoint->Discover(NPT_HttpUrl("255.255.255.255", 1900, "*"), "upnp:rootdevice", 1, 6000);
    ctrlPoint->Discover(NPT_HttpUrl("239.255.255.250", 1900, "*"), "upnp:rootdevice", 1, 6000);
#endif

#ifdef SIMULATE_XBOX_360
    // create device
    PLT_DeviceHostReference xbox(new PLT_Xbox360("30848576-1775-2000-0000-00125a8fefad"));
    xbox->SetByeByeFirst(false);
    xbox->m_SerialNumber = "308485761776";

    // add device
    upnp.AddDevice(xbox);
    ctrlPoint->IgnoreUUID(xbox->GetUUID());

    // xbox issues a search for the content directory service
    // 10 secs after announcing itself to make sure 
    // it got detected and inspected first

    ctrlPoint->Search(
        NPT_HttpUrl("239.255.255.250", 1900, "*"), 
        "urn:schemas-microsoft-com:service:MSContentDirectory:1", 2, 10000, NPT_TimeInterval(10, 0));
    ctrlPoint->Search(
        NPT_HttpUrl("239.255.255.250", 1900, "*"), 
        "urn:schemas-upnp-org:service:ContentDirectory:1", 2, 10000, NPT_TimeInterval(10, 0));
    
#endif

    // start to process commands 
    controller.ProcessCommandLoop();

    // stop everything
    upnp.Stop();

    return 0;
}
