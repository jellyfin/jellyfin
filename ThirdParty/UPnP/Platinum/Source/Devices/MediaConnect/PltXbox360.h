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

#ifndef _PLT_XBOX360_H_
#define _PLT_XBOX360_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "PltMediaRenderer.h"

class PLT_Xbox360 : public PLT_MediaRenderer
{
public:
    PLT_Xbox360(const char*  uuid        = NULL, 
                unsigned int port        = 0,
                bool         port_rebind = false);

protected:
    // PLT_DeviceHost methods
    virtual NPT_Result SetupServices();
    virtual NPT_Result SetupIcons();
    virtual NPT_Result InitServiceURLs(PLT_Service* service, const char* service_name);
    
    virtual NPT_Result Announce(PLT_DeviceData*  device, 
                                NPT_HttpRequest& request, 
                                NPT_UdpSocket&   socket, 
                                bool             byebye);

    // PLT_DeviceData methods
    virtual NPT_Result GetDescription(NPT_String& desc) { return PLT_MediaRenderer::GetDescription(desc); }
    virtual NPT_Result GetDescription(NPT_XmlElementNode*  parent, 
                                      NPT_XmlElementNode** device = NULL);

protected:
    virtual ~PLT_Xbox360();

    virtual NPT_Result AnnouncePresence(NPT_UdpSocket& socket, 
                                        const char*    serial_number);
};

#endif /* _PLT_XBOX360_H_ */
