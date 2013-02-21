/*****************************************************************
|
|   Platinum - Synchronous Media Browser
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
 UPnP AV Media Controller synchronous implementation.
 */

#ifndef _PLT_SYNC_MEDIA_BROWSER_
#define _PLT_SYNC_MEDIA_BROWSER_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "PltCtrlPoint.h"
#include "PltMediaBrowser.h"
#include "PltMediaCache.h"

/*----------------------------------------------------------------------
|   types
+---------------------------------------------------------------------*/
typedef NPT_Map<NPT_String, PLT_DeviceDataReference>         PLT_DeviceMap;
typedef NPT_Map<NPT_String, PLT_DeviceDataReference>::Entry  PLT_DeviceMapEntry;

typedef struct PLT_BrowseData {
    NPT_SharedVariable shared_var;
    NPT_Result         res;
    PLT_BrowseInfo     info;
} PLT_BrowseData;

typedef NPT_Reference<PLT_BrowseData> PLT_BrowseDataReference;

/*----------------------------------------------------------------------
|   PLT_MediaContainerListener
+---------------------------------------------------------------------*/
class PLT_MediaContainerChangesListener
{
public:
    virtual ~PLT_MediaContainerChangesListener() {}
    virtual void OnContainerChanged(PLT_DeviceDataReference& device, 
                                    const char*              item_id, 
                                    const char*              update_id) = 0;
};

/*----------------------------------------------------------------------
|   PLT_SyncMediaBrowser
+---------------------------------------------------------------------*/
class PLT_SyncMediaBrowser : public PLT_MediaBrowser,
                             public PLT_MediaBrowserDelegate
{
public:
    PLT_SyncMediaBrowser(PLT_CtrlPointReference&            ctrlPoint, 
                         bool                               use_cache = false, 
                         PLT_MediaContainerChangesListener* listener = NULL);
    virtual ~PLT_SyncMediaBrowser();

    // PLT_MediaBrowser methods
    virtual NPT_Result OnDeviceAdded(PLT_DeviceDataReference& device);
    virtual NPT_Result OnDeviceRemoved(PLT_DeviceDataReference& device);

    // PLT_MediaBrowserDelegate methods
    virtual void OnMSStateVariablesChanged(PLT_Service*                  service, 
                                           NPT_List<PLT_StateVariable*>* vars);
    virtual void OnBrowseResult(NPT_Result               res, 
                                PLT_DeviceDataReference& device, 
                                PLT_BrowseInfo*          info, 
                                void*                    userdata);

    // methods
    void       SetContainerListener(PLT_MediaContainerChangesListener* listener) {
        m_ContainerListener = listener;
    }
    NPT_Result BrowseSync(PLT_DeviceDataReference&      device, 
                          const char*                   id, 
                          PLT_MediaObjectListReference& list,
                          bool                          metadata = false,
                          NPT_Int32                     start = 0,
                          NPT_Cardinal                  max_results = 0); // 0 means all

    const NPT_Lock<PLT_DeviceMap>& GetMediaServersMap() const { return m_MediaServers; }
    bool IsCached(const char* uuid, const char* object_id);

protected:
    NPT_Result BrowseSync(PLT_BrowseDataReference& browse_data,
                          PLT_DeviceDataReference& device, 
                          const char*              object_id,
                          NPT_Int32                index, 
                          NPT_Int32                count,
                          bool                     browse_metadata = false,
                          const char*              filter = "dc:date,upnp:genre,res,res@duration,res@size,upnp:albumArtURI,upnp:album,upnp:artist,upnp:author,searchable,childCount", // explicitely specify res otherwise WMP won't return a URL!
                          const char*              sort = "");
private:
    NPT_Result Find(const char* ip, PLT_DeviceDataReference& device);
    NPT_Result WaitForResponse(NPT_SharedVariable& shared_var);

private:
    NPT_Lock<PLT_DeviceMap>              m_MediaServers;
    PLT_MediaContainerChangesListener*   m_ContainerListener;
    bool                                 m_UseCache;
    PLT_MediaCache<PLT_MediaObjectListReference,NPT_String> m_Cache;
};

/*----------------------------------------------------------------------
|   PLT_DeviceMapFinderByIp
+---------------------------------------------------------------------*/
class PLT_DeviceMapFinderByIp
{
public:
    // methods
    PLT_DeviceMapFinderByIp(const char* ip) : m_IP(ip) {}

    bool operator()(const PLT_DeviceMapEntry* const& entry) const {
        PLT_DeviceDataReference device = entry->GetValue();
        return (device->GetURLBase().GetHost() == m_IP);
    }

private:
    // members
    NPT_String m_IP;
};

/*----------------------------------------------------------------------
|   PLT_DeviceFinderByUUID
+---------------------------------------------------------------------*/
class PLT_DeviceMapFinderByUUID
{
public:
    // methods
    PLT_DeviceMapFinderByUUID(const char* uuid) : m_UUID(uuid) {}

    bool operator()(const PLT_DeviceMapEntry* const& entry) const {
        PLT_DeviceDataReference device = entry->GetValue();
        return device->GetUUID() == m_UUID;
    }

private:
    // members
    NPT_String m_UUID;
};

#endif /* _PLT_SYNC_MEDIA_BROWSER_ */

