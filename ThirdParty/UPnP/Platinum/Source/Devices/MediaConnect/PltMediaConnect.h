/*****************************************************************
|
|      Platinum - AV Media Connect Device
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
|  licensing@plutinosoft.com
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

#ifndef _PLT_MEDIA_CONNECT_H_
#define _PLT_MEDIA_CONNECT_H_

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "PltFileMediaServer.h"

/*----------------------------------------------------------------------
|   PLT_MediaConnect
+---------------------------------------------------------------------*/
class PLT_MediaConnect : public PLT_MediaServer
{
public:
    // class methods
    static NPT_Result GetMappedObjectId(const char* object_id, 
                                        NPT_String& mapped_object_id);
    
    // constructor
    PLT_MediaConnect(const char*  friendly_name,
                     bool         add_hostname = true,
                     const char*  udn = NULL,
                     NPT_UInt16   port = 0,
                     bool         port_rebind = false);

protected:
    virtual ~PLT_MediaConnect();
    
    // PLT_DeviceHost methods
    virtual NPT_Result SetupServices();
    virtual NPT_Result OnAction(PLT_ActionReference&          action, 
                                const PLT_HttpRequestContext& context);
    virtual NPT_Result ProcessGetDescription(NPT_HttpRequest&              request,
                                             const NPT_HttpRequestContext& context,
                                             NPT_HttpResponse&             response);
    virtual NPT_Result ProcessGetSCPD(PLT_Service*                  service,
                                      NPT_HttpRequest&              request,
                                      const NPT_HttpRequestContext& context,
                                      NPT_HttpResponse&             response);

    // X_MS_MediaReceiverRegistrar
    virtual NPT_Result OnIsAuthorized(PLT_ActionReference&  action);
    virtual NPT_Result OnRegisterDevice(PLT_ActionReference&  action);
    virtual NPT_Result OnIsValidated(PLT_ActionReference&  action);

protected:
	NPT_Mutex   m_Lock;
    bool        m_AddHostname;
};

/*----------------------------------------------------------------------
 |   PLT_FileMediaConnectDelegate class
 +---------------------------------------------------------------------*/
class PLT_FileMediaConnectDelegate : public PLT_FileMediaServerDelegate
{
public:
    // constructor & destructor
    PLT_FileMediaConnectDelegate(const char* url_root, const char* file_root) :
        PLT_FileMediaServerDelegate(url_root, file_root) {}
    virtual ~PLT_FileMediaConnectDelegate() {}
    
    // PLT_FileMediaServerDelegate methods
    virtual NPT_Result GetFilePath(const char* object_id, NPT_String& filepath);
    virtual NPT_Result OnSearchContainer(PLT_ActionReference&          action, 
                                         const char*                   object_id, 
                                         const char*                   search_criteria,
                                         const char*                   filter,
                                         NPT_UInt32                    starting_index,
                                         NPT_UInt32                    requested_count,
                                         const char*                   sort_criteria, 
                                         const PLT_HttpRequestContext& context);
};

#endif /* _PLT_MEDIA_CONNECT_H_ */
