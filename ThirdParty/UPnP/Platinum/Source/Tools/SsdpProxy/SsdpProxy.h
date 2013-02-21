/*****************************************************************
|
|   Platinum - Ssdp Proxy tool
|
| Copyright (c) 2004-2008, Plutinosoft, LLC.
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

#ifndef _PLT_SSDP_PROXY_H_
#define _PLT_SSDP_PROXY_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "PltTaskManager.h"
#include "PltSsdp.h"

/*----------------------------------------------------------------------
|   forward declarations
+---------------------------------------------------------------------*/
class PLT_SsdpUnicastListener;

/*----------------------------------------------------------------------
|   PLT_SsdpProxy class
+---------------------------------------------------------------------*/
class PLT_SsdpProxy : public PLT_TaskManager,
                      public PLT_SsdpPacketListener
{
public:
    PLT_SsdpProxy();
    ~PLT_SsdpProxy();

    NPT_Result Start(NPT_UInt32 port);

    // PLT_SsdpPacketListener method
    virtual NPT_Result OnSsdpPacket(NPT_HttpRequest&              request, 
                                    const NPT_HttpRequestContext& context);

    // PLT_SsdpUnicastListener redirect
    virtual NPT_Result OnUnicastSsdpPacket(NPT_HttpRequest&              request, 
                                           const NPT_HttpRequestContext& context);

private:
    PLT_SsdpUnicastListener* m_UnicastListener;
};

/*----------------------------------------------------------------------
|   PLT_SsdpUnicastListener class
+---------------------------------------------------------------------*/
class PLT_SsdpUnicastListener :  public PLT_SsdpPacketListener
{
public:
    PLT_SsdpUnicastListener(PLT_SsdpProxy* proxy) : m_Proxy(proxy) {}

    // PLT_SsdpPacketListener method
    NPT_Result OnSsdpPacket(NPT_HttpRequest&              request, 
                            const NPT_HttpRequestContext& context);

private:
    PLT_SsdpProxy* m_Proxy;
};

/*----------------------------------------------------------------------
|   PLT_SsdpProxySearchResponseListener class
+---------------------------------------------------------------------*/
class PLT_SsdpProxyForwardTask : public PLT_SsdpSearchTask 
{
public:
    PLT_SsdpProxyForwardTask(NPT_UdpSocket*           socket,
                             NPT_HttpRequest*         request, 
                             NPT_Timeout              timeout,
                             const NPT_SocketAddress& forward_address);

    NPT_Result ProcessResponse(NPT_Result                    res, 
                               NPT_HttpRequest*              request, 
                               const NPT_HttpRequestContext& context,
                               NPT_HttpResponse*             response);

private:
    NPT_SocketAddress m_ForwardAddress;
};

#endif // _PLT_SSDP_PROXY_H_
