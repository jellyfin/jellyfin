/*****************************************************************
|
|   Platinum - AV Media Server Device
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

/** @file
 UPnP AV Media Server.
 */

#ifndef _PLT_MEDIA_SERVER_H_
#define _PLT_MEDIA_SERVER_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "PltDeviceHost.h"
#include "PltMediaItem.h"

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
#define MAX_PATH_LENGTH 1024

/*----------------------------------------------------------------------
|   PLT_MediaServerDelegate
+---------------------------------------------------------------------*/
/** 
 The PLT_MediaServerDelegate class is an interface for delegating the handling 
 of the required UPnP AV ContentDirectory service actions. It also handles 
 resource HTTP requests (downloading).
 */
class PLT_MediaServerDelegate
{
public:
    PLT_MediaServerDelegate() {}
    virtual ~PLT_MediaServerDelegate() {}
    
    virtual NPT_Result OnBrowseMetadata(PLT_ActionReference&          /*action*/, 
                                        const char*                   /*object_id*/, 
                                        const char*                   /*filter*/,
                                        NPT_UInt32                    /*starting_index*/,
                                        NPT_UInt32                    /*requested_count*/,
                                        const char*                   /*sort_criteria*/,
                                        const PLT_HttpRequestContext& /*context*/) = 0;
    virtual NPT_Result OnBrowseDirectChildren(PLT_ActionReference&          /*action*/, 
                                              const char*                   /*object_id*/, 
                                              const char*                   /*filter*/,
                                              NPT_UInt32                    /*starting_index*/,
                                              NPT_UInt32                    /*requested_count*/,
                                              const char*                   /*sort_criteria*/, 
                                              const PLT_HttpRequestContext& /*context*/) = 0;
    virtual NPT_Result OnSearchContainer(PLT_ActionReference&          /*action*/, 
                                         const char*                   /*container_id*/, 
                                         const char*                   /*search_criteria*/,
 										 const char*                   /*filter*/,
                                         NPT_UInt32                    /*starting_index*/,
                                         NPT_UInt32                    /*requested_count*/,
                                         const char*                   /*sort_criteria*/, 
                                         const PLT_HttpRequestContext& /*context*/) = 0;
    virtual NPT_Result ProcessFileRequest(NPT_HttpRequest&              /*request*/,
                                          const NPT_HttpRequestContext& /*context*/,
                                          NPT_HttpResponse&             /*response*/) = 0;
};

/*----------------------------------------------------------------------
|   PLT_MediaServer
+---------------------------------------------------------------------*/
/**
 The PLT_MediaServer class implements the base class for a UPnP AV 
 Media Server device.
 */
class PLT_MediaServer : public PLT_DeviceHost
{
public:
    /* BrowseFlags */
    enum BrowseFlags {
        BROWSEMETADATA,
        BROWSEDIRECTCHILDREN
    };
    
    // class methods
    static NPT_Result ParseBrowseFlag(const char* str, BrowseFlags& flag);
    static NPT_Result ParseSort(const NPT_String& sort, NPT_List<NPT_String>& list);

    // constructor
    PLT_MediaServer(const char*  friendly_name,
                    bool         show_ip = false,
                    const char*  uuid = NULL,
                    NPT_UInt16   port = 0,
                    bool         port_rebind = false);
    
    // methods
    virtual void SetDelegate(PLT_MediaServerDelegate* delegate) { m_Delegate = delegate; }
    PLT_MediaServerDelegate* GetDelegate() { return m_Delegate; }
    virtual void UpdateSystemUpdateID(NPT_UInt32 update);
    virtual void UpdateContainerUpdateID(const char* id, NPT_UInt32 update);
    
protected:
    virtual ~PLT_MediaServer();
    
    // PLT_DeviceHost methods
    virtual NPT_Result SetupServices();
    virtual NPT_Result OnAction(PLT_ActionReference&          action, 
                                const PLT_HttpRequestContext& context);
    virtual NPT_Result ProcessHttpGetRequest(NPT_HttpRequest&              request, 
                                             const NPT_HttpRequestContext& context,
                                             NPT_HttpResponse&             response);
    
    // ConnectionManager
    virtual NPT_Result OnGetCurrentConnectionIDs(PLT_ActionReference&          action, 
                                                 const PLT_HttpRequestContext& context);
    virtual NPT_Result OnGetProtocolInfo(PLT_ActionReference&          action, 
                                         const PLT_HttpRequestContext& context);
    virtual NPT_Result OnGetCurrentConnectionInfo(PLT_ActionReference&          action, 
                                                  const PLT_HttpRequestContext& context);

    // ContentDirectory
    virtual NPT_Result OnGetSortCapabilities(PLT_ActionReference&          action, 
                                             const PLT_HttpRequestContext& context);
    virtual NPT_Result OnGetSearchCapabilities(PLT_ActionReference&          action, 
                                               const PLT_HttpRequestContext& context);
    virtual NPT_Result OnGetSystemUpdateID(PLT_ActionReference&          action, 
                                           const PLT_HttpRequestContext& context);
    virtual NPT_Result OnBrowse(PLT_ActionReference&          action, 
                                const PLT_HttpRequestContext& context);
    virtual NPT_Result OnSearch(PLT_ActionReference&          action, 
                                const PLT_HttpRequestContext& context);

    // overridable methods
    virtual NPT_Result OnBrowseMetadata(PLT_ActionReference&          action, 
                                        const char*                   object_id, 
                                        const char*                   filter,
                                        NPT_UInt32                    starting_index,
                                        NPT_UInt32                    requested_count,
                                        const char*                   sort_criteria,
                                        const PLT_HttpRequestContext& context);
    virtual NPT_Result OnBrowseDirectChildren(PLT_ActionReference&          action, 
                                              const char*                   object_id, 
                                              const char*                   filter,
                                              NPT_UInt32                    starting_index,
                                              NPT_UInt32                    requested_count,
                                              const char*                   sort_criteria, 
                                              const PLT_HttpRequestContext& context);
    virtual NPT_Result OnSearchContainer(PLT_ActionReference&          action, 
                                         const char*                   container_id, 
                                         const char*                   search_criteria,
 										 const char*                   filter,
                                         NPT_UInt32                    starting_index,
                                         NPT_UInt32                    requested_count,
                                         const char*                   sort_criteria, 
                                         const PLT_HttpRequestContext& context);
    
private:
    PLT_MediaServerDelegate* m_Delegate;
};

#endif /* _PLT_MEDIA_SERVER_H_ */
