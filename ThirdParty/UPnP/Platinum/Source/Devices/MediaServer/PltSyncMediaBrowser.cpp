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

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "PltSyncMediaBrowser.h"

NPT_SET_LOCAL_LOGGER("platinum.media.server.syncbrowser")

/*----------------------------------------------------------------------
|   PLT_SyncMediaBrowser::PLT_SyncMediaBrowser
+---------------------------------------------------------------------*/
PLT_SyncMediaBrowser::PLT_SyncMediaBrowser(PLT_CtrlPointReference&            ctrlPoint,
                                           bool                               use_cache /* = false */, 
                                           PLT_MediaContainerChangesListener* listener /* = NULL */) :
    PLT_MediaBrowser(ctrlPoint),
    m_ContainerListener(listener),
    m_UseCache(use_cache)
{
    SetDelegate(this);
}

/*----------------------------------------------------------------------
|   PLT_SyncMediaBrowser::~PLT_SyncMediaBrowser
+---------------------------------------------------------------------*/
PLT_SyncMediaBrowser::~PLT_SyncMediaBrowser()
{
}

/*  Blocks forever waiting for a response from a request
 *  It is expected the request to succeed or to timeout and return an error eventually
 */
/*----------------------------------------------------------------------
|   PLT_SyncMediaBrowser::WaitForResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_SyncMediaBrowser::WaitForResponse(NPT_SharedVariable& shared_var)
{
    return shared_var.WaitUntilEquals(1, 30000);
}

/*----------------------------------------------------------------------
|   PLT_SyncMediaBrowser::OnDeviceAdded
+---------------------------------------------------------------------*/
NPT_Result
PLT_SyncMediaBrowser::OnDeviceAdded(PLT_DeviceDataReference& device)
{
    NPT_String uuid = device->GetUUID();

    // test if it's a media server
    PLT_Service* service;
    if (NPT_SUCCEEDED(device->FindServiceByType("urn:schemas-upnp-org:service:ContentDirectory:*", service))) {
        NPT_AutoLock lock(m_MediaServers);
        m_MediaServers.Put(uuid, device);
    }
    
    return PLT_MediaBrowser::OnDeviceAdded(device);
}
    
/*----------------------------------------------------------------------
|   PLT_SyncMediaBrowser::OnDeviceRemoved
+---------------------------------------------------------------------*/
NPT_Result
PLT_SyncMediaBrowser::OnDeviceRemoved(PLT_DeviceDataReference& device)
{
    NPT_String uuid = device->GetUUID();

    // Remove from our list of servers first if found
    {
        NPT_AutoLock lock(m_MediaServers);
        m_MediaServers.Erase(uuid);
    }

    // clear cache for that device
    if (m_UseCache) m_Cache.Clear(device.AsPointer()->GetUUID());
    
    return PLT_MediaBrowser::OnDeviceRemoved(device);
}

/*----------------------------------------------------------------------
|   PLT_SyncMediaBrowser::Find
+---------------------------------------------------------------------*/
NPT_Result
PLT_SyncMediaBrowser::Find(const char* ip, PLT_DeviceDataReference& device)
{
    NPT_AutoLock lock(m_MediaServers);
    const NPT_List<PLT_DeviceMapEntry*>::Iterator it = 
        m_MediaServers.GetEntries().Find(PLT_DeviceMapFinderByIp(ip));
    if (it) {
        device = (*it)->GetValue();
        return NPT_SUCCESS;
    }
    return NPT_FAILURE;
}

/*----------------------------------------------------------------------
|   PLT_SyncMediaBrowser::OnBrowseResult
+---------------------------------------------------------------------*/
void
PLT_SyncMediaBrowser::OnBrowseResult(NPT_Result               res, 
                                     PLT_DeviceDataReference& device, 
                                     PLT_BrowseInfo*          info, 
                                     void*                    userdata)
{
    NPT_COMPILER_UNUSED(device);

    if (!userdata) return;

    PLT_BrowseDataReference* data = (PLT_BrowseDataReference*) userdata;
    (*data)->res = res;
    if (NPT_SUCCEEDED(res) && info) {
        (*data)->info = *info;
    }
    (*data)->shared_var.SetValue(1);
    delete data;
}

/*----------------------------------------------------------------------
|   PLT_SyncMediaBrowser::OnMSStateVariablesChanged
+---------------------------------------------------------------------*/
void 
PLT_SyncMediaBrowser::OnMSStateVariablesChanged(PLT_Service*                  service, 
                                                NPT_List<PLT_StateVariable*>* vars)
{
    NPT_AutoLock lock(m_MediaServers);
    
    PLT_DeviceDataReference device;
    const NPT_List<PLT_DeviceMapEntry*>::Iterator it = 
        m_MediaServers.GetEntries().Find(PLT_DeviceMapFinderByUUID(service->GetDevice()->GetUUID()));
    if (!it) return; // device with this service has gone away

    device = (*it)->GetValue();
    PLT_StateVariable* var = PLT_StateVariable::Find(*vars, "ContainerUpdateIDs");
    if (var) {
        // variable found, parse value
        NPT_String value = var->GetValue();
        NPT_String item_id, update_id;
        int index;

        while (value.GetLength()) {
            // look for container id
            index = value.Find(',');
            if (index < 0) break;
            item_id = value.Left(index);
            value = value.SubString(index+1);

            // look for update id
            if (value.GetLength()) {
                index = value.Find(',');
                update_id = (index<0)?value:value.Left(index);
                value = (index<0)?"":value.SubString(index+1);

                // clear cache for that device
                if (m_UseCache) m_Cache.Clear(device->GetUUID(), item_id);

                // notify listener
                if (m_ContainerListener) m_ContainerListener->OnContainerChanged(device, item_id, update_id);
            }       
        }
    }        
}

/*----------------------------------------------------------------------
|   PLT_SyncMediaBrowser::BrowseSync
+---------------------------------------------------------------------*/
NPT_Result 
PLT_SyncMediaBrowser::BrowseSync(PLT_BrowseDataReference& browse_data,
                                 PLT_DeviceDataReference& device, 
                                 const char*              object_id, 
                                 NPT_Int32                index, 
                                 NPT_Int32                count,
                                 bool                     browse_metadata,
                                 const char*              filter, 
                                 const char*              sort)
{
    NPT_Result res;

    browse_data->shared_var.SetValue(0);
    browse_data->info.si = index;

    // send off the browse packet.  Note that this will
    // not block.  There is a call to WaitForResponse in order
    // to block until the response comes back.
    res = PLT_MediaBrowser::Browse(device,
        (const char*)object_id,
        index,
        count,
        browse_metadata,
        filter,
        sort,
        new PLT_BrowseDataReference(browse_data));		
    NPT_CHECK_SEVERE(res);

    return WaitForResponse(browse_data->shared_var);
}

/*----------------------------------------------------------------------
|   PLT_SyncMediaBrowser::BrowseSync
+---------------------------------------------------------------------*/
NPT_Result
PLT_SyncMediaBrowser::BrowseSync(PLT_DeviceDataReference&      device, 
                                 const char*                   object_id, 
                                 PLT_MediaObjectListReference& list,
                                 bool                          metadata, /* = false */
                                 NPT_Int32                     start, /* = 0 */
                                 NPT_Cardinal                  max_results /* = 0 */)
{
    NPT_Result res = NPT_FAILURE;
    NPT_Int32  index = start;
    
    // only cache metadata or if starting from 0 and asking for maximum
    bool cache = m_UseCache && (metadata || (start == 0 && max_results == 0));

    // reset output params
    list = NULL;

    // look into cache first
    if (cache && NPT_SUCCEEDED(m_Cache.Get(device->GetUUID(), object_id, list))) return NPT_SUCCESS;

    do {	
        PLT_BrowseDataReference browse_data(new PLT_BrowseData(), true);

        // send off the browse packet.  Note that this will
        // not block.  There is a call to WaitForResponse in order
        // to block until the response comes back.
        res = BrowseSync(
            browse_data,
            device,
            (const char*)object_id,
            index,
            metadata?1:30, // DLNA recommendations for browsing children is no more than 30 at a time
            metadata);		
        NPT_CHECK_LABEL_WARNING(res, done);
        
        if (NPT_FAILED(browse_data->res)) {
            res = browse_data->res;
            NPT_CHECK_LABEL_WARNING(res, done);
        }

        // server returned no more, bail now
        if (browse_data->info.items->GetItemCount() == 0)
            break;

        if (list.IsNull()) {
            list = browse_data->info.items;
        } else {
            list->Add(*browse_data->info.items);
            // clear the list items so that the data inside is not
            // cleaned up by PLT_MediaItemList dtor since we copied
            // each pointer into the new list.
            browse_data->info.items->Clear();
        }

        // stop now if our list contains exactly what the server said it had.
        // Note that the server could return 0 if it didn't know how many items were
        // available. In this case we have to continue browsing until
        // nothing is returned back by the server.
        // Unless we were told to stop after reaching a certain amount to avoid
        // length delays
        // (some servers may return a total matches out of whack at some point too)
        if ((browse_data->info.tm && browse_data->info.tm <= list->GetItemCount()) ||
            (max_results && list->GetItemCount() >= max_results))
            break;

        // ask for the next chunk of entries
        index = list->GetItemCount();
    } while(1);

done:
    // cache the result
    if (cache && NPT_SUCCEEDED(res) && !list.IsNull() && list->GetItemCount()) {
        m_Cache.Put(device->GetUUID(), object_id, list);
    }

    // clear entire cache data for device if failed, the device could be gone
    if (NPT_FAILED(res) && cache) m_Cache.Clear(device->GetUUID());
    
    return res;
}

/*----------------------------------------------------------------------
|   PLT_SyncMediaBrowser::IsCached
+---------------------------------------------------------------------*/
bool
PLT_SyncMediaBrowser::IsCached(const char* uuid, const char* object_id)
{
    NPT_AutoLock lock(m_MediaServers);
    const NPT_List<PLT_DeviceMapEntry*>::Iterator it = 
        m_MediaServers.GetEntries().Find(PLT_DeviceMapFinderByUUID(uuid));
    if (!it) {
        m_Cache.Clear(uuid);
        return false; // device with this service has gone away
    }
    
    PLT_MediaObjectListReference list;
    return NPT_SUCCEEDED(m_Cache.Get(uuid, object_id, list))?true:false;
}

