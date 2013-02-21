/*****************************************************************
|
|   Platinum - AV Media Browser (Media Server Control Point)
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
#include "PltMediaBrowser.h"
#include "PltDidl.h"

NPT_SET_LOCAL_LOGGER("platinum.media.server.browser")

/*----------------------------------------------------------------------
|   PLT_MediaBrowser::PLT_MediaBrowser
+---------------------------------------------------------------------*/
PLT_MediaBrowser::PLT_MediaBrowser(PLT_CtrlPointReference&   ctrl_point,
                                   PLT_MediaBrowserDelegate* delegate /* = NULL */) :
    m_CtrlPoint(ctrl_point),
    m_Delegate(delegate)
{
    m_CtrlPoint->AddListener(this);
}

/*----------------------------------------------------------------------
|   PLT_MediaBrowser::~PLT_MediaBrowser
+---------------------------------------------------------------------*/
PLT_MediaBrowser::~PLT_MediaBrowser()
{
    m_CtrlPoint->RemoveListener(this);
}

/*----------------------------------------------------------------------
|   PLT_MediaBrowser::OnDeviceAdded
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaBrowser::OnDeviceAdded(PLT_DeviceDataReference& device)
{
    // verify the device implements the function we need
    PLT_Service* serviceCDS;
    PLT_Service* serviceCMR;
    NPT_String   type;

    if (!device->GetType().StartsWith("urn:schemas-upnp-org:device:MediaServer"))
        return NPT_FAILURE;
    
    type = "urn:schemas-upnp-org:service:ContentDirectory:*";
    if (NPT_FAILED(device->FindServiceByType(type, serviceCDS))) {
        NPT_LOG_WARNING_2("Service %s not found in device \"%s\"", 
            type.GetChars(),
            device->GetFriendlyName().GetChars());
        return NPT_FAILURE;
    } else {
        // in case it's a newer upnp implementation, force to 1
        serviceCDS->ForceVersion(1);
    }
    
    type = "urn:schemas-upnp-org:service:ConnectionManager:*";
    if (NPT_FAILED(device->FindServiceByType(type, serviceCMR))) {
        NPT_LOG_WARNING_2("Service %s not found in device \"%s\"", 
            type.GetChars(), 
            device->GetFriendlyName().GetChars());
        return NPT_FAILURE;
    } else {
        // in case it's a newer upnp implementation, force to 1
        serviceCMR->ForceVersion(1);
    }
    
    {
        NPT_AutoLock lock(m_MediaServers);

        PLT_DeviceDataReference data;
        NPT_String uuid = device->GetUUID();
        
        // is it a new device?
        if (NPT_SUCCEEDED(NPT_ContainerFind(m_MediaServers, PLT_DeviceDataFinder(uuid), data))) {
            NPT_LOG_WARNING_1("Device (%s) is already in our list!", (const char*)uuid);
            return NPT_FAILURE;
        }

        NPT_LOG_FINE_1("Device Found: %s", (const char*)*device);

        m_MediaServers.Add(device);
    }

    if (m_Delegate && m_Delegate->OnMSAdded(device)) {
        m_CtrlPoint->Subscribe(serviceCDS);
        m_CtrlPoint->Subscribe(serviceCMR);
    }
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_MediaBrowser::OnDeviceRemoved
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaBrowser::OnDeviceRemoved(PLT_DeviceDataReference& device)
{
    if (!device->GetType().StartsWith("urn:schemas-upnp-org:device:MediaServer"))
        return NPT_FAILURE;

    {
        NPT_AutoLock lock(m_MediaServers);

        // only release if we have kept it around
        PLT_DeviceDataReference data;
        NPT_String uuid = device->GetUUID();

        // Have we seen that device?
        if (NPT_FAILED(NPT_ContainerFind(m_MediaServers, PLT_DeviceDataFinder(uuid), data))) {
            NPT_LOG_WARNING_1("Device (%s) not found in our list!", (const char*)uuid);
            return NPT_FAILURE;
        }

        NPT_LOG_FINE_1("Device Removed: %s", (const char*)*device);

        m_MediaServers.Remove(device);
    }
    
    if (m_Delegate) {
        m_Delegate->OnMSRemoved(device);
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_MediaBrowser::FindServer
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaBrowser::FindServer(const char* uuid, PLT_DeviceDataReference& device)
{
    NPT_AutoLock lock(m_MediaServers);

    if (NPT_FAILED(NPT_ContainerFind(m_MediaServers, PLT_DeviceDataFinder(uuid), device))) {
        NPT_LOG_FINE_1("Device (%s) not found in our list of servers", (const char*)uuid);
        return NPT_FAILURE;
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_MediaBrowser::Search
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaBrowser::Search(PLT_DeviceDataReference& device, 
                         const char*              container_id,
						 const char*              search_criteria,
                         NPT_UInt32               start_index,
                         NPT_UInt32               count,
                         const char*              filter,
                         void*                    userdata)
{
    // verify device still in our list
    PLT_DeviceDataReference device_data;
    NPT_CHECK_WARNING(FindServer(device->GetUUID(), device_data));

    // create action
    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(
        device, 
        "urn:schemas-upnp-org:service:ContentDirectory:1",
        "Search",
        action));

    // Set the container id
    PLT_Arguments args;
    if (NPT_FAILED(action->SetArgumentValue("ContainerID", container_id))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // set the Search Criteria
    if (NPT_FAILED(action->SetArgumentValue("SearchCriteria", search_criteria))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

 
    // set the Filter
    if (NPT_FAILED(action->SetArgumentValue("Filter", filter))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // set the Starting Index
    if (NPT_FAILED(action->SetArgumentValue("StartingIndex", 
                                            NPT_String::FromInteger(start_index)))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // set the Requested Count
    if (NPT_FAILED(action->SetArgumentValue("RequestedCount", 
                                            NPT_String::FromInteger(count)))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // set the Requested Count
    if (NPT_FAILED(action->SetArgumentValue("SortCriteria", ""))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // invoke the action
    if (NPT_FAILED(m_CtrlPoint->InvokeAction(action, userdata))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_MediaBrowser::Browse
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaBrowser::Browse(PLT_DeviceDataReference& device, 
                         const char*              obj_id,
                         NPT_UInt32               start_index,
                         NPT_UInt32               count,
                         bool                     browse_metadata,
                         const char*              filter,
                         const char*              sort_criteria,
                         void*                    userdata)
{
    // verify device still in our list
    PLT_DeviceDataReference device_data;
    NPT_CHECK_WARNING(FindServer(device->GetUUID(), device_data));

    // create action
    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(
        device, 
        "urn:schemas-upnp-org:service:ContentDirectory:1",
        "Browse",
        action));

    // Set the object id
    PLT_Arguments args;
    if (NPT_FAILED(action->SetArgumentValue("ObjectID", obj_id))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // set the browse_flag
    if (NPT_FAILED(action->SetArgumentValue("BrowseFlag", 
                                            browse_metadata?"BrowseMetadata":"BrowseDirectChildren"))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }
 
    // set the Filter
    if (NPT_FAILED(action->SetArgumentValue("Filter", filter))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // set the Starting Index
    if (NPT_FAILED(action->SetArgumentValue("StartingIndex", 
                                            NPT_String::FromInteger(start_index)))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // set the Requested Count
    if (NPT_FAILED(action->SetArgumentValue("RequestedCount", 
                                            NPT_String::FromInteger(count)))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // set the Requested Count
    if (NPT_FAILED(action->SetArgumentValue("SortCriteria", sort_criteria))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // invoke the action
    if (NPT_FAILED(m_CtrlPoint->InvokeAction(action, userdata))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_MediaBrowser::OnActionResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaBrowser::OnActionResponse(NPT_Result           res, 
                                   PLT_ActionReference& action, 
                                   void*                userdata)
{
    // look for device in our list first
    PLT_DeviceDataReference device;
    NPT_String uuid = action->GetActionDesc().GetService()->GetDevice()->GetUUID();
    if (NPT_FAILED(FindServer(uuid, device))) res = NPT_FAILURE;

    NPT_String actionName = action->GetActionDesc().GetName();
    if (actionName.Compare("Browse", true) == 0) {
        return OnBrowseResponse(res, device, action, userdata);
    } else if (actionName.Compare("Search", true) == 0) {
        return OnSearchResponse(res, device, action, userdata);
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_MediaBrowser::OnBrowseResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaBrowser::OnBrowseResponse(NPT_Result               res, 
                                   PLT_DeviceDataReference& device, 
                                   PLT_ActionReference&     action, 
                                   void*                    userdata)
{
    NPT_String     value;
    PLT_BrowseInfo info;
    NPT_String     unescaped;

    if (!m_Delegate) return NPT_SUCCESS;

    if (NPT_FAILED(res) || action->GetErrorCode() != 0) {
        goto bad_action;
    }

    if (NPT_FAILED(action->GetArgumentValue("ObjectID", info.object_id)))  {
        goto bad_action;
    }
    if (NPT_FAILED(action->GetArgumentValue("UpdateID", value)) || 
        value.GetLength() == 0 || 
        NPT_FAILED(value.ToInteger(info.uid))) {
        goto bad_action;
    }
    if (NPT_FAILED(action->GetArgumentValue("NumberReturned", value)) || 
        value.GetLength() == 0 || 
        NPT_FAILED(value.ToInteger(info.nr))) {
        goto bad_action;
    }
    if (NPT_FAILED(action->GetArgumentValue("TotalMatches", value)) || 
        value.GetLength() == 0 || 
        NPT_FAILED(value.ToInteger(info.tm))) {
        goto bad_action;
    }
    if (NPT_FAILED(action->GetArgumentValue("Result", value)) || 
        value.GetLength() == 0) {
        goto bad_action;
    }
    
    if (NPT_FAILED(PLT_Didl::FromDidl(value, info.items))) {
        goto bad_action;
    }

    m_Delegate->OnBrowseResult(NPT_SUCCESS, device, &info, userdata);
    return NPT_SUCCESS;

bad_action:
    m_Delegate->OnBrowseResult(NPT_FAILURE, device, NULL, userdata);
    return NPT_FAILURE;
}

/*----------------------------------------------------------------------
|   PLT_MediaBrowser::OnSearchResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaBrowser::OnSearchResponse(NPT_Result               res, 
                                   PLT_DeviceDataReference& device, 
                                   PLT_ActionReference&     action, 
                                   void*                    userdata)
{
    NPT_String     value;
    PLT_BrowseInfo info;
    NPT_String     unescaped;

    if (!m_Delegate) return NPT_SUCCESS;

    if (NPT_FAILED(res) || action->GetErrorCode() != 0) {
        goto bad_action;
    }

    if (NPT_FAILED(action->GetArgumentValue("ContainerId", info.object_id)))  {
        goto bad_action;
    }
    if (NPT_FAILED(action->GetArgumentValue("UpdateID", value)) || 
        value.GetLength() == 0 || 
        NPT_FAILED(value.ToInteger(info.uid))) {
        goto bad_action;
    }
    if (NPT_FAILED(action->GetArgumentValue("NumberReturned", value)) || 
        value.GetLength() == 0 || 
        NPT_FAILED(value.ToInteger(info.nr))) {
        goto bad_action;
    }
    if (NPT_FAILED(action->GetArgumentValue("TotalMatches", value)) || 
        value.GetLength() == 0 || 
        NPT_FAILED(value.ToInteger(info.tm))) {
        goto bad_action;
    }
    if (NPT_FAILED(action->GetArgumentValue("Result", value)) || 
        value.GetLength() == 0) {
        goto bad_action;
    }
    
    if (NPT_FAILED(PLT_Didl::FromDidl(value, info.items))) {
        goto bad_action;
    }

    m_Delegate->OnSearchResult(NPT_SUCCESS, device, &info, userdata);
    return NPT_SUCCESS;

bad_action:
    m_Delegate->OnSearchResult(NPT_FAILURE, device, NULL, userdata);
    return NPT_FAILURE;
}

/*----------------------------------------------------------------------
|   PLT_MediaBrowser::OnEventNotify
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaBrowser::OnEventNotify(PLT_Service* service, NPT_List<PLT_StateVariable*>* vars)
{
    if (!service->GetDevice()->GetType().StartsWith("urn:schemas-upnp-org:device:MediaServer"))
        return NPT_FAILURE;

    if (!m_Delegate) return NPT_SUCCESS;

    /* make sure device associated to service is still around */
    PLT_DeviceDataReference data;
    NPT_CHECK_WARNING(FindServer(service->GetDevice()->GetUUID(), data));

    m_Delegate->OnMSStateVariablesChanged(service, vars);
    return NPT_SUCCESS;
}
