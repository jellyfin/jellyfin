/*****************************************************************
|
|   Platinum - XBox 360
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
#include "Neptune.h"
#include "PltXbox360.h"
#include "PltService.h"
#include "PltUtilities.h"
#include "PltSsdp.h"

/*----------------------------------------------------------------------
|   externals
+---------------------------------------------------------------------*/
extern NPT_UInt8 Platinum_48x48_png[4681];

/*----------------------------------------------------------------------
|   PLT_Xbox360::PLT_Xbox360
+---------------------------------------------------------------------*/
PLT_Xbox360::PLT_Xbox360(const char*  uuid        /* = NULL */, 
                         unsigned int port        /* = 0 */,
                         bool         port_rebind /* = false */) :
    PLT_MediaRenderer("Xbox 360", false, uuid, port, port_rebind)
{
}

/*----------------------------------------------------------------------
|   PLT_Xbox360::~PLT_Xbox360
+---------------------------------------------------------------------*/
PLT_Xbox360::~PLT_Xbox360()
{
}

/*----------------------------------------------------------------------
|   PLT_Xbox360::SetupServices
+---------------------------------------------------------------------*/
NPT_Result
PLT_Xbox360::SetupServices()
{
    NPT_CHECK(PLT_MediaRenderer::SetupServices());

    m_ModelDescription = "Xbox 360";
    m_ModelName = "Xbox 360";
    m_ModelURL = "http://www.xbox.com";
    m_Manufacturer = "Microsoft Corporation";
    m_ManufacturerURL = "http://www.microsoft.com";

    NPT_Array<PLT_Service*>::Iterator service;

    if (NPT_SUCCEEDED(NPT_ContainerFind(
            m_Services, 
            PLT_ServiceTypeFinder("urn:schemas-upnp-org:service:RenderingControl:1"), 
            service))) {
        InitServiceURLs(*service, "RenderingControl");
    }

    if (NPT_SUCCEEDED(NPT_ContainerFind(
            m_Services, 
            PLT_ServiceTypeFinder("urn:schemas-upnp-org:service:ConnectionManager:1"), 
            service))) {
        InitServiceURLs(*service, "ConnectionManager");
    }

    // remove AVTransport
    if (NPT_SUCCEEDED(NPT_ContainerFind(
            m_Services, 
            PLT_ServiceTypeFinder("urn:schemas-upnp-org:service:AVTransport:1"), 
            service))) {
        m_Services.Erase(service);
    }
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_Service::InitServiceURLs
+---------------------------------------------------------------------*/
NPT_Result
PLT_Xbox360::InitServiceURLs(PLT_Service* service, const char* service_name)
{
    service->SetSCPDURL("/Content/" + NPT_String(service_name));
    service->SetControlURL("/Control/" + NPT_String(service_name));
    service->SetEventSubURL("/Event/" + NPT_String(service_name));

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_Xbox360::GetDescription
+---------------------------------------------------------------------*/
NPT_Result
PLT_Xbox360::GetDescription(NPT_XmlElementNode* root, NPT_XmlElementNode** device_out)
{
    // if no device out passed, pass one
    NPT_XmlElementNode* device_out_local;
    if (!device_out) device_out = &device_out_local;

    NPT_CHECK(PLT_MediaRenderer::GetDescription(root, device_out));

    // add extra stuff
    root->SetNamespaceUri("ms", " urn:microsoft-com:wmc-1-0");
    root->SetNamespaceUri("microsoft", "urn-schemas-microsoft-com:WMPNSS-1-0");
    
    if (*device_out) {
        (*device_out)->SetAttribute("ms", "X_MS_SupportsWMDRM", "true");
        NPT_XmlElementNode* device_caps = new NPT_XmlElementNode("microsoft", "X_DeviceCaps");
        device_caps->AddText("4754");
        (*device_out)->AddChild(device_caps);

        NPT_XmlElementNode* handshake = new NPT_XmlElementNode("microsoft", "HandshakeFlags");
        handshake->AddText("1");
        (*device_out)->AddChild(handshake);
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_Xbox360::SetupIcons
+---------------------------------------------------------------------*/
NPT_Result
PLT_Xbox360::SetupIcons()
{
    AddIcon(
        PLT_DeviceIcon("image/png", 48, 48, 32, "/xbox360.png"),
        Platinum_48x48_png, sizeof(Platinum_48x48_png), false);
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_DeviceHost::Announce
+---------------------------------------------------------------------*/
NPT_Result
PLT_Xbox360::Announce(PLT_DeviceData*  device, 
                      NPT_HttpRequest& req, 
                      NPT_UdpSocket&   socket, 
                      bool             byebye)
{
    AnnouncePresence(socket, "");

    return PLT_MediaRenderer::Announce(device, req, socket, byebye);
}

/*----------------------------------------------------------------------
|   PLT_DeviceHost::AnnouncePresence
+---------------------------------------------------------------------*/
NPT_Result
PLT_Xbox360::AnnouncePresence(NPT_UdpSocket& socket, 
                              const char*    serial_number)
{
    NPT_COMPILER_UNUSED(serial_number);

    NPT_HttpRequest req(
        NPT_HttpUrl("239.255.255.250", 1900, "*"), 
        "NOTIFY", 
        NPT_HTTP_PROTOCOL_1_1);
    PLT_HttpHelper::SetHost(req, "239.255.255.250:1900");

    NPT_Result res = NPT_SUCCESS;
    // get location URL based on ip address of interface
    PLT_UPnPMessageHelper::SetNTS(req, "ssdp:alive");
    PLT_UPnPMessageHelper::SetLeaseTime(req, NPT_TimeInterval(4.));
    PLT_UPnPMessageHelper::SetServer(req, "dashboard/1.0 UpnP/1.0 xbox/2.0", true);
    req.GetHeaders().SetHeader("AL", 
        "<urn:schemas-microsoft-com:nhed:attributes?type=X02&firmwarever=8955.0&udn=uuid:10000000-0000-0000-0200-00125A8FEFAC>");
    PLT_UPnPMessageHelper::SetLocation(req, "*");

    // target address
    NPT_IpAddress ip;
    if (NPT_FAILED(res = ip.ResolveName(req.GetUrl().GetHost()))) {
        return res;
    }
    NPT_SocketAddress addr(ip, req.GetUrl().GetPort());

    PLT_SsdpSender::SendSsdp(req,
            "uuid:00000000-0000-0000-0200-00125A8FEFAC::urn:schemas-microsoft-com:nhed:presence:1", 
            "urn:schemas-microsoft-com:nhed:presence:1",
            socket,
            true,
            &addr);

    return NPT_SUCCESS;
}
