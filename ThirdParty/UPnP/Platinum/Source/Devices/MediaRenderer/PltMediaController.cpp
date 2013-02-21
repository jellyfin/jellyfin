/*****************************************************************
|
|   Platinum - AV Media Controller (Media Renderer Control Point)
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
#include "PltMediaController.h"
#include "PltDidl.h"
#include "PltDeviceData.h"
#include "PltUtilities.h"

NPT_SET_LOCAL_LOGGER("platinum.media.renderer.controller")

/*----------------------------------------------------------------------
|   PLT_MediaController::PLT_MediaController
+---------------------------------------------------------------------*/
PLT_MediaController::PLT_MediaController(PLT_CtrlPointReference&      ctrl_point, 
                                         PLT_MediaControllerDelegate* delegate /* = NULL */) :
    m_CtrlPoint(ctrl_point),
    m_Delegate(delegate)
{
    m_CtrlPoint->AddListener(this);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::~PLT_MediaController
+---------------------------------------------------------------------*/
PLT_MediaController::~PLT_MediaController()
{
    m_CtrlPoint->RemoveListener(this);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::OnDeviceAdded
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaController::OnDeviceAdded(PLT_DeviceDataReference& device)
{
    // verify the device implements the function we need
    PLT_Service* serviceAVT = NULL;
    PLT_Service* serviceCMR;
	PLT_Service* serviceRC;
    NPT_String   type;
    
    if (!device->GetType().StartsWith("urn:schemas-upnp-org:device:MediaRenderer"))
        return NPT_FAILURE;

    // optional service
    type = "urn:schemas-upnp-org:service:AVTransport:*";
    if (NPT_SUCCEEDED(device->FindServiceByType(type, serviceAVT))) {
        // in case it's a newer upnp implementation, force to 1
        NPT_LOG_FINE_1("Service %s found", (const char*)type);
        serviceAVT->ForceVersion(1);
    }
    
    // required services
    type = "urn:schemas-upnp-org:service:ConnectionManager:*";
    if (NPT_FAILED(device->FindServiceByType(type, serviceCMR))) {
        NPT_LOG_FINE_1("Service %s not found", (const char*)type);
        return NPT_FAILURE;
    } else {
        // in case it's a newer upnp implementation, force to 1
        serviceCMR->ForceVersion(1);
    }

	type = "urn:schemas-upnp-org:service:RenderingControl:*";
    if (NPT_FAILED(device->FindServiceByType(type, serviceRC))) {
        NPT_LOG_FINE_1("Service %s not found", (const char*)type);
        return NPT_FAILURE;
    } else {
        // in case it's a newer upnp implementation, force to 1
        serviceRC->ForceVersion(1);
    }

    {
        NPT_AutoLock lock(m_MediaRenderers);

        PLT_DeviceDataReference data;
        NPT_String uuid = device->GetUUID();
        
        // is it a new device?
        if (NPT_SUCCEEDED(NPT_ContainerFind(m_MediaRenderers, 
                                            PLT_DeviceDataFinder(uuid), data))) {
            NPT_LOG_WARNING_1("Device (%s) is already in our list!", (const char*)uuid);
            return NPT_FAILURE;
        }

        NPT_LOG_FINE_1("Device Found: %s", (const char*)*device);

        m_MediaRenderers.Add(device);
    }
    
    if (m_Delegate && m_Delegate->OnMRAdded(device)) {
        // subscribe to services eventing only if delegate wants it
        if (serviceAVT) m_CtrlPoint->Subscribe(serviceAVT);

        // subscribe to required services
		m_CtrlPoint->Subscribe(serviceCMR);
		m_CtrlPoint->Subscribe(serviceRC);
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_MediaController::OnDeviceRemoved
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::OnDeviceRemoved(PLT_DeviceDataReference& device)
{   
    if (!device->GetType().StartsWith("urn:schemas-upnp-org:device:MediaRenderer"))
        return NPT_FAILURE;

    {
        NPT_AutoLock lock(m_MediaRenderers);

        // only release if we have kept it around
        PLT_DeviceDataReference data;
        NPT_String uuid = device->GetUUID();

        // Have we seen that device?
        if (NPT_FAILED(NPT_ContainerFind(m_MediaRenderers, PLT_DeviceDataFinder(uuid), data))) {
            NPT_LOG_WARNING_1("Device (%s) not found in our list!", (const char*)uuid);
            return NPT_FAILURE;
        }

        NPT_LOG_FINE_1("Device Removed: %s", (const char*)*device);

        m_MediaRenderers.Remove(device);
    }

    if (m_Delegate) {
        m_Delegate->OnMRRemoved(device);
    }
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_MediaController::FindRenderer
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaController::FindRenderer(const char* uuid, PLT_DeviceDataReference& device)
{
    NPT_AutoLock lock(m_MediaRenderers);

    if (NPT_FAILED(NPT_ContainerFind(m_MediaRenderers, PLT_DeviceDataFinder(uuid), device))) {
        NPT_LOG_FINE_1("Device (%s) not found in our list of renderers", (const char*)uuid);
        return NPT_FAILURE;
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_MediaController::GetProtocolInfoSink
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::GetProtocolInfoSink(const NPT_String&     device_uuid, 
                                         NPT_List<NPT_String>& sinks)
{
    PLT_DeviceDataReference renderer;
    NPT_CHECK_WARNING(FindRenderer(device_uuid, renderer));

    // look for ConnectionManager service
    PLT_Service* serviceCMR;
    NPT_CHECK_SEVERE(renderer->FindServiceByType("urn:schemas-upnp-org:service:ConnectionManager:*", 
                                                 serviceCMR));

    NPT_String value;
    NPT_CHECK_SEVERE(serviceCMR->GetStateVariableValue("SinkProtocolInfo", 
                                                       value));

    sinks = value.Split(",");
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_MediaController::GetTransportState
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::GetTransportState(const NPT_String&  device_uuid, 
                                       NPT_String&        state)
{
    PLT_DeviceDataReference renderer;
    NPT_CHECK_WARNING(FindRenderer(device_uuid, renderer));
    
    // look for AVTransport service
    PLT_Service* serviceAVT;
    NPT_CHECK_SEVERE(renderer->FindServiceByType("urn:schemas-upnp-org:service:AVTransport:*", 
                                                 serviceAVT));
    
    NPT_CHECK_SEVERE(serviceAVT->GetStateVariableValue("TransportState", 
                                                       state));
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_MediaController::GetVolumeState
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::GetVolumeState(const NPT_String&  device_uuid, 
                                    NPT_UInt32&        volume)
{
    PLT_DeviceDataReference renderer;
    NPT_CHECK_WARNING(FindRenderer(device_uuid, renderer));
    
    // look for RenderingControl service
    PLT_Service* serviceRC;
    NPT_CHECK_SEVERE(renderer->FindServiceByType("urn:schemas-upnp-org:service:RenderingControl:*", 
                                                 serviceRC));
    
    NPT_String value;
    NPT_CHECK_SEVERE(serviceRC->GetStateVariableValue("Volume", 
                                                      value));
    
    return value.ToInteger32(volume);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::FindMatchingProtocolInfo
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaController::FindMatchingProtocolInfo(NPT_List<NPT_String>& sinks,
                                              const char*           protocol_info)
{
    PLT_ProtocolInfo protocol(protocol_info);
    for (NPT_List<NPT_String>::Iterator iter = sinks.GetFirstItem();
         iter;
         iter++) {
        PLT_ProtocolInfo sink(*iter);
        if (sink.Match(protocol)) {
            return NPT_SUCCESS;
        }
    }

    return NPT_ERROR_NO_SUCH_ITEM;
}

/*----------------------------------------------------------------------
|   PLT_MediaController::FindBestResource
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::FindBestResource(PLT_DeviceDataReference& device, 
                                      PLT_MediaObject&         item, 
                                      NPT_Cardinal&            resource_index)
{
    if (item.m_Resources.GetItemCount() <= 0) return NPT_ERROR_INVALID_PARAMETERS;

    NPT_List<NPT_String> sinks;
    NPT_CHECK_SEVERE(GetProtocolInfoSink(device->GetUUID(), sinks));

    // look for best resource
    for (NPT_Cardinal i=0; i< item.m_Resources.GetItemCount(); i++) {
        if (NPT_SUCCEEDED(FindMatchingProtocolInfo(
                sinks, 
                item.m_Resources[i].m_ProtocolInfo.ToString()))) {
            resource_index = i;
            return NPT_SUCCESS;
        }
    }

    return NPT_ERROR_NO_SUCH_ITEM;
}

/*----------------------------------------------------------------------
|   PLT_MediaController::InvokeActionWithInstance
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::InvokeActionWithInstance(PLT_ActionReference& action,
                                              NPT_UInt32           instance_id,
                                              void*                userdata)
{
    // Set the object id
    NPT_CHECK_SEVERE(action->SetArgumentValue(
        "InstanceID", 
        NPT_String::FromInteger(instance_id)));

    // set the arguments on the action, this will check the argument values
    return m_CtrlPoint->InvokeAction(action, userdata);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::GetCurrentTransportActions
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::GetCurrentTransportActions(PLT_DeviceDataReference& device, 
                                                NPT_UInt32               instance_id,
                                                void*                    userdata)
{
    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(
        device, 
        "urn:schemas-upnp-org:service:AVTransport:1", 
        "GetCurrentTransportActions", 
        action));
    return InvokeActionWithInstance(action, instance_id, userdata);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::GetDeviceCapabilities
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::GetDeviceCapabilities(PLT_DeviceDataReference& device, 
                                           NPT_UInt32               instance_id,
                                           void*                    userdata)
{
    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(
        device, 
        "urn:schemas-upnp-org:service:AVTransport:1", 
        "GetDeviceCapabilities", 
        action));
    return InvokeActionWithInstance(action, instance_id, userdata);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::GetMediaInfo
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::GetMediaInfo(PLT_DeviceDataReference& device, 
                                  NPT_UInt32               instance_id,
                                  void*                    userdata)
{
    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(
        device, 
        "urn:schemas-upnp-org:service:AVTransport:1", 
        "GetMediaInfo", 
        action));
    return InvokeActionWithInstance(action, instance_id, userdata);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::GetPositionInfo
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::GetPositionInfo(PLT_DeviceDataReference& device, 
                                     NPT_UInt32               instance_id,
                                     void*                    userdata)
{
    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(
        device, 
        "urn:schemas-upnp-org:service:AVTransport:1", 
        "GetPositionInfo", 
        action));
    return InvokeActionWithInstance(action, instance_id, userdata);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::GetTransportInfo
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::GetTransportInfo(PLT_DeviceDataReference& device, 
                                      NPT_UInt32               instance_id,
                                      void*                    userdata)
{
    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(
        device, 
        "urn:schemas-upnp-org:service:AVTransport:1", 
        "GetTransportInfo", 
        action));
    return InvokeActionWithInstance(action, instance_id, userdata);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::GetTransportSettings
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::GetTransportSettings(PLT_DeviceDataReference&  device, 
                                          NPT_UInt32                instance_id,
                                          void*                     userdata)
{
    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(
        device, 
        "urn:schemas-upnp-org:service:AVTransport:1", 
        "GetTransportSettings", 
        action));
    return InvokeActionWithInstance(action, instance_id, userdata);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::Next
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::Next(PLT_DeviceDataReference& device, 
                          NPT_UInt32               instance_id,
                          void*                    userdata)
{
    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(
        device, 
        "urn:schemas-upnp-org:service:AVTransport:1", 
        "Next", 
        action));
    return InvokeActionWithInstance(action, instance_id, userdata);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::Pause
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::Pause(PLT_DeviceDataReference& device, 
                           NPT_UInt32               instance_id,
                           void*                    userdata)
{
    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(
        device, 
        "urn:schemas-upnp-org:service:AVTransport:1", 
        "Pause", 
        action));
    return InvokeActionWithInstance(action, instance_id, userdata);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::Play
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::Play(PLT_DeviceDataReference& device, 
                          NPT_UInt32               instance_id,
                          NPT_String               speed,
                          void*                    userdata)
{
    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(
        device, 
        "urn:schemas-upnp-org:service:AVTransport:1", 
        "Play", 
        action));

    // Set the speed
    if (NPT_FAILED(action->SetArgumentValue("Speed", speed))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    return InvokeActionWithInstance(action, instance_id, userdata);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::Previous
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::Previous(PLT_DeviceDataReference& device, 
                              NPT_UInt32               instance_id,
                              void*                    userdata)
{
    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(
        device, 
        "urn:schemas-upnp-org:service:AVTransport:1", 
        "Previous", 
        action));
    return InvokeActionWithInstance(action, instance_id, userdata);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::Seek
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::Seek(PLT_DeviceDataReference& device, 
                          NPT_UInt32               instance_id,
                          NPT_String               unit,
                          NPT_String               target,
                          void*                    userdata)
{
    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(
        device, 
        "urn:schemas-upnp-org:service:AVTransport:1", 
        "Seek", 
        action));

    // Set the unit
    if (NPT_FAILED(action->SetArgumentValue("Unit", unit))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // Set the target
    if (NPT_FAILED(action->SetArgumentValue("Target", target))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    return InvokeActionWithInstance(action, instance_id, userdata);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::CanSetNextAVTransportURI
+---------------------------------------------------------------------*/
bool
PLT_MediaController::CanSetNextAVTransportURI(PLT_DeviceDataReference &device)
{
    if (device.IsNull()) return NPT_ERROR_INVALID_PARAMETERS;

    PLT_ActionDesc* action_desc;
    NPT_Result result = m_CtrlPoint->FindActionDesc(device,
                                                    "urn:schemas-upnp-org:service:AVTransport:1",
                                                    "SetNextAVTransportURI",
                                                    action_desc);
    return (result == NPT_SUCCESS);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::SetAVTransportURI
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::SetAVTransportURI(PLT_DeviceDataReference& device, 
                                       NPT_UInt32               instance_id, 
                                       const char*              uri,
                                       const char*              metadata,
                                       void*                    userdata)
{
    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(
        device, 
        "urn:schemas-upnp-org:service:AVTransport:1", 
        "SetAVTransportURI", 
        action));

    // set the uri
    if (NPT_FAILED(action->SetArgumentValue("CurrentURI", uri))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // set the uri metadata
    if (NPT_FAILED(action->SetArgumentValue("CurrentURIMetaData", metadata))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    return InvokeActionWithInstance(action, instance_id, userdata);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::SetNextAVTransportURI
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::SetNextAVTransportURI(PLT_DeviceDataReference& device, 
                                           NPT_UInt32               instance_id, 
                                           const char*              next_uri,
                                           const char*              next_metadata,
                                           void*                    userdata)
{
    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(device, 
                                               "urn:schemas-upnp-org:service:AVTransport:1", 
                                               "SetNextAVTransportURI", 
                                               action));
    
    // set the uri
    if (NPT_FAILED(action->SetArgumentValue("NextURI", next_uri))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }
    
    // set the uri metadata
    if (NPT_FAILED(action->SetArgumentValue("NextURIMetaData", next_metadata))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }
    
    return InvokeActionWithInstance(action, instance_id, userdata);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::SetPlayMode
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::SetPlayMode(PLT_DeviceDataReference& device, 
                                 NPT_UInt32               instance_id,
                                 NPT_String               new_play_mode,
                                 void*                    userdata)
{
    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(
        device, 
        "urn:schemas-upnp-org:service:AVTransport:1", 
        "SetPlayMode", 
        action));

    // set the New PlayMode
    if (NPT_FAILED(action->SetArgumentValue("NewPlayMode", new_play_mode))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    return InvokeActionWithInstance(action, instance_id, userdata);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::Stop
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::Stop(PLT_DeviceDataReference& device, 
                          NPT_UInt32               instance_id,
                          void*                    userdata)
{
    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(
        device, 
        "urn:schemas-upnp-org:service:AVTransport:1", 
        "Stop", 
        action));
    return InvokeActionWithInstance(action, instance_id, userdata);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::GetCurrentConnectionIDs
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::GetCurrentConnectionIDs(PLT_DeviceDataReference& device, 
                                             void*                    userdata)
{
    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(
        device, 
        "urn:schemas-upnp-org:service:ConnectionManager:1", 
        "GetCurrentConnectionIDs", 
        action));

    // set the arguments on the action, this will check the argument values
    if (NPT_FAILED(m_CtrlPoint->InvokeAction(action, userdata))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_MediaController::GetCurrentConnectionInfo
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::GetCurrentConnectionInfo(PLT_DeviceDataReference& device, 
                                              NPT_UInt32               connection_id,
                                              void*                    userdata)
{
    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(
        device, 
        "urn:schemas-upnp-org:service:ConnectionManager:1", 
        "GetCurrentConnectionInfo", 
        action));

    // set the New PlayMode
    if (NPT_FAILED(action->SetArgumentValue("ConnectionID", 
                                            NPT_String::FromInteger(connection_id)))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // set the arguments on the action, this will check the argument values
    if (NPT_FAILED(m_CtrlPoint->InvokeAction(action, userdata))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_MediaController::GetProtocolInfo
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::GetProtocolInfo(PLT_DeviceDataReference& device, 
                                     void*                    userdata)
{
    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(
        device, 
        "urn:schemas-upnp-org:service:ConnectionManager:1", 
        "GetProtocolInfo", 
        action));

    // set the arguments on the action, this will check the argument values
    if (NPT_FAILED(m_CtrlPoint->InvokeAction(action, userdata))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_MediaController::SetMute
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::SetMute(PLT_DeviceDataReference& device, 
                             NPT_UInt32               instance_id,
                             const char*              channel,
                             bool                     mute,
                             void*                    userdata)
{
    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(
        device, 
        "urn:schemas-upnp-org:service:RenderingControl:1", 
        "SetMute", 
        action));
    
    // set the channel
    if (NPT_FAILED(action->SetArgumentValue("Channel", channel))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }
    
    // set the channel
    if (NPT_FAILED(action->SetArgumentValue("DesiredMute", 
                                            mute?"1":"0"))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    return InvokeActionWithInstance(action, instance_id, userdata);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::SetVolume
+---------------------------------------------------------------------*/
NPT_Result PLT_MediaController::SetVolume(PLT_DeviceDataReference&  device,
										  NPT_UInt32				instance_id, 
										  const char*               channel,
										  int						volume, 
										  void*						userdata) 
{

    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(
        device, 
        "urn:schemas-upnp-org:service:RenderingControl:1", 
        "SetVolume", 
        action));

	    // set the channel
    if (NPT_FAILED(action->SetArgumentValue("Channel", channel))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

	if (NPT_FAILED(action->SetArgumentValue("DesiredVolume",  
											NPT_String::FromInteger(volume)))) {
		return NPT_ERROR_INVALID_PARAMETERS;
	}

    return InvokeActionWithInstance(action, instance_id, userdata);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::GetMute
+---------------------------------------------------------------------*/
NPT_Result 
PLT_MediaController::GetMute(PLT_DeviceDataReference& device, 
                             NPT_UInt32               instance_id,
                             const char*              channel,
                             void*                    userdata)
{
    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(
        device, 
        "urn:schemas-upnp-org:service:RenderingControl:1", 
        "GetMute", 
        action));
    
    // set the channel
    if (NPT_FAILED(action->SetArgumentValue("Channel", channel))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }
    
    return InvokeActionWithInstance(action, instance_id, userdata);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::GetVolume
+---------------------------------------------------------------------*/
NPT_Result PLT_MediaController::GetVolume(PLT_DeviceDataReference&  device, 
										  NPT_UInt32				instance_id, 
										  const char*				channel,
										  void*						userdata) 
{
    PLT_ActionReference action;
    NPT_CHECK_SEVERE(m_CtrlPoint->CreateAction(
        device, 
        "urn:schemas-upnp-org:service:RenderingControl:1", 
        "GetVolume", 
        action));

	    // set the channel
    if (NPT_FAILED(action->SetArgumentValue("Channel", channel))) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    return InvokeActionWithInstance(action, instance_id, userdata);
}

/*----------------------------------------------------------------------
|   PLT_MediaController::OnActionResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaController::OnActionResponse(NPT_Result           res, 
                                      PLT_ActionReference& action, 
                                      void*                userdata)
{
    if (m_Delegate == NULL) return NPT_SUCCESS;

    PLT_DeviceDataReference device;
    NPT_String uuid = action->GetActionDesc().GetService()->GetDevice()->GetUUID();
           
    /* extract action name */
    NPT_String actionName = action->GetActionDesc().GetName();

    /* AVTransport response ? */
    if (actionName.Compare("GetCurrentTransportActions", true) == 0) {
        if (NPT_FAILED(FindRenderer(uuid, device))) res = NPT_FAILURE;
        return OnGetCurrentTransportActionsResponse(res, device, action, userdata);
    }
    else if (actionName.Compare("GetDeviceCapabilities", true) == 0) {
        if (NPT_FAILED(FindRenderer(uuid, device))) res = NPT_FAILURE;
        return OnGetDeviceCapabilitiesResponse(res, device, action, userdata);
    }
    else if (actionName.Compare("GetMediaInfo", true) == 0) {
        if (NPT_FAILED(FindRenderer(uuid, device))) res = NPT_FAILURE;
        return OnGetMediaInfoResponse(res, device, action, userdata);
    }
    else if (actionName.Compare("GetPositionInfo", true) == 0) {
        if (NPT_FAILED(FindRenderer(uuid, device))) res = NPT_FAILURE;
        return OnGetPositionInfoResponse(res, device, action, userdata);
    }
    else if (actionName.Compare("GetTransportInfo", true) == 0) {
        if (NPT_FAILED(FindRenderer(uuid, device))) res = NPT_FAILURE;
        return OnGetTransportInfoResponse(res, device, action, userdata);
    }
    else if (actionName.Compare("GetTransportSettings", true) == 0) {
        if (NPT_FAILED(FindRenderer(uuid, device))) res = NPT_FAILURE;
        return OnGetTransportSettingsResponse(res, device, action, userdata);
    }
    else if (actionName.Compare("Next", true) == 0) {
        if (NPT_FAILED(FindRenderer(uuid, device))) res = NPT_FAILURE;
        m_Delegate->OnNextResult(res, device, userdata);
    }
    else if (actionName.Compare("Pause", true) == 0) {
        if (NPT_FAILED(FindRenderer(uuid, device))) res = NPT_FAILURE;
        m_Delegate->OnPauseResult(res, device, userdata);
    }
    else if (actionName.Compare("Play", true) == 0) {
        if (NPT_FAILED(FindRenderer(uuid, device))) res = NPT_FAILURE;
        m_Delegate->OnPlayResult(res, device, userdata);
    }
    else if (actionName.Compare("Previous", true) == 0) {
        if (NPT_FAILED(FindRenderer(uuid, device))) res = NPT_FAILURE;
        m_Delegate->OnPreviousResult(res, device, userdata);
    }
    else if (actionName.Compare("Seek", true) == 0) {
        if (NPT_FAILED(FindRenderer(uuid, device))) res = NPT_FAILURE;
        m_Delegate->OnSeekResult(res, device, userdata);
    }
    else if (actionName.Compare("SetAVTransportURI", true) == 0) {
        if (NPT_FAILED(FindRenderer(uuid, device))) res = NPT_FAILURE;
        m_Delegate->OnSetAVTransportURIResult(res, device, userdata);
    }
    else if (actionName.Compare("SetPlayMode", true) == 0) {
        if (NPT_FAILED(FindRenderer(uuid, device))) res = NPT_FAILURE;
        m_Delegate->OnSetPlayModeResult(res, device, userdata);
    }
    else if (actionName.Compare("Stop", true) == 0) {
        if (NPT_FAILED(FindRenderer(uuid, device))) res = NPT_FAILURE;
        m_Delegate->OnStopResult(res, device, userdata);
    }
    else if (actionName.Compare("GetCurrentConnectionIDs", true) == 0) {
        if (NPT_FAILED(FindRenderer(uuid, device))) res = NPT_FAILURE;
        return OnGetCurrentConnectionIDsResponse(res, device, action, userdata);
    }
    else if (actionName.Compare("GetCurrentConnectionInfo", true) == 0) {
        if (NPT_FAILED(FindRenderer(uuid, device))) res = NPT_FAILURE;
        return OnGetCurrentConnectionInfoResponse(res, device, action, userdata);
    }
    else if (actionName.Compare("GetProtocolInfo", true) == 0) {
        if (NPT_FAILED(FindRenderer(uuid, device))) res = NPT_FAILURE;
        return OnGetProtocolInfoResponse(res, device, action, userdata);
    }
    else if (actionName.Compare("SetMute", true) == 0) {
        if (NPT_FAILED(FindRenderer(uuid, device))) res = NPT_FAILURE;
        m_Delegate->OnSetMuteResult(res, device, userdata);
    }
    else if (actionName.Compare("GetMute", true) == 0) {
        if (NPT_FAILED(FindRenderer(uuid, device))) res = NPT_FAILURE;
        return OnGetMuteResponse(res, device, action, userdata);
    }
	else if (actionName.Compare("SetVolume", true) == 0) { 
        if (NPT_FAILED(FindRenderer(uuid, device))) res = NPT_FAILURE;
        m_Delegate->OnSetVolumeResult(res, device, userdata);
    }
    else if (actionName.Compare("GetVolume", true) == 0) {
        if (NPT_FAILED(FindRenderer(uuid, device))) res = NPT_FAILURE;
        return OnGetVolumeResponse(res, device, action, userdata);
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_MediaController::OnGetCurrentTransportActionsResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaController::OnGetCurrentTransportActionsResponse(NPT_Result               res, 
                                                          PLT_DeviceDataReference& device, 
                                                          PLT_ActionReference&     action, 
                                                          void*                    userdata)
{
    NPT_String actions;
    PLT_StringList values;

    if (NPT_FAILED(res) || action->GetErrorCode() != 0) {
        goto bad_action;
    }

    if (NPT_FAILED(action->GetArgumentValue("Actions", actions))) {
        goto bad_action;
    }

    // parse the list of actions and return a list to listener
    ParseCSV(actions, values);

    m_Delegate->OnGetCurrentTransportActionsResult(NPT_SUCCESS, device, &values, userdata);
    return NPT_SUCCESS;

bad_action:
    m_Delegate->OnGetCurrentTransportActionsResult(NPT_FAILURE, device, NULL, userdata);
    return NPT_FAILURE;
}

/*----------------------------------------------------------------------
|   PLT_MediaController::OnGetDeviceCapabilitiesResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaController::OnGetDeviceCapabilitiesResponse(NPT_Result               res, 
                                                     PLT_DeviceDataReference& device, 
                                                     PLT_ActionReference&     action, 
                                                     void*                    userdata)
{
    NPT_String value;
    PLT_DeviceCapabilities capabilities;

    if (NPT_FAILED(res) || action->GetErrorCode() != 0) {
        goto bad_action;
    }

    if (NPT_FAILED(action->GetArgumentValue("PlayMedia", value))) {
        goto bad_action;
    }
    // parse the list of medias and return a list to listener
    ParseCSV(value, capabilities.play_media);

    if (NPT_FAILED(action->GetArgumentValue("RecMedia", value))) {
        goto bad_action;
    }
    // parse the list of rec and return a list to listener
    ParseCSV(value, capabilities.rec_media);

    if (NPT_FAILED(action->GetArgumentValue("RecQualityModes", value))) {
        goto bad_action;
    }
    // parse the list of modes and return a list to listener
    ParseCSV(value, capabilities.rec_quality_modes);

    m_Delegate->OnGetDeviceCapabilitiesResult(NPT_SUCCESS, device, &capabilities, userdata);
    return NPT_SUCCESS;

bad_action:
    m_Delegate->OnGetDeviceCapabilitiesResult(NPT_FAILURE, device, NULL, userdata);
    return NPT_FAILURE;
}

/*----------------------------------------------------------------------
|   PLT_MediaController::OnGetMediaInfoResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaController::OnGetMediaInfoResponse(NPT_Result               res, 
                                            PLT_DeviceDataReference& device, 
                                            PLT_ActionReference&     action, 
                                            void*                    userdata)
{
    NPT_String      value;
    PLT_MediaInfo   info;

    if (NPT_FAILED(res) || action->GetErrorCode() != 0) {
        goto bad_action;
    }

    if (NPT_FAILED(action->GetArgumentValue("NrTracks", info.num_tracks))) {
        goto bad_action;
    }
    if (NPT_FAILED(action->GetArgumentValue("MediaDuration", value))) {
        goto bad_action;
    }
    if (NPT_FAILED(PLT_Didl::ParseTimeStamp(value, info.media_duration))) {
        goto bad_action;
    }

    if (NPT_FAILED(action->GetArgumentValue("CurrentURI", info.cur_uri))) {
        goto bad_action;
    }
    if (NPT_FAILED(action->GetArgumentValue("CurrentURIMetaData", info.cur_metadata))) {
        goto bad_action;
    }
    if (NPT_FAILED(action->GetArgumentValue("NextURI", info.next_uri))) {
        goto bad_action;
    }
    if (NPT_FAILED(action->GetArgumentValue("NextURIMetaData",  info.next_metadata))) {
        goto bad_action;
    }
    if (NPT_FAILED(action->GetArgumentValue("PlayMedium", info.play_medium))) {
        goto bad_action;
    }
    if (NPT_FAILED(action->GetArgumentValue("RecordMedium", info.rec_medium))) {
        goto bad_action;
    }
    if (NPT_FAILED(action->GetArgumentValue("WriteStatus", info.write_status))) {
        goto bad_action;
    }

    m_Delegate->OnGetMediaInfoResult(NPT_SUCCESS, device, &info, userdata);
    return NPT_SUCCESS;

bad_action:
    m_Delegate->OnGetMediaInfoResult(NPT_FAILURE, device, NULL, userdata);
    return NPT_FAILURE;
}

/*----------------------------------------------------------------------
|   PLT_MediaController::OnGetPositionInfoResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaController::OnGetPositionInfoResponse(NPT_Result               res, 
                                               PLT_DeviceDataReference& device, 
                                               PLT_ActionReference&     action, 
                                               void*                    userdata)
{
    NPT_String       value;
    PLT_PositionInfo info;

    if (NPT_FAILED(res) || action->GetErrorCode() != 0) {
        goto bad_action;
    }

    if (NPT_FAILED(action->GetArgumentValue("Track", info.track))) {
        goto bad_action;
    }

    if (NPT_FAILED(action->GetArgumentValue("TrackDuration", value))) {
        goto bad_action;
    }
    if (NPT_FAILED(PLT_Didl::ParseTimeStamp(value, info.track_duration))) {
         // some renderers return garbage sometimes
		info.track_duration = NPT_TimeStamp(0.);
    }

    if (NPT_FAILED(action->GetArgumentValue("TrackMetaData", info.track_metadata))) {
        goto bad_action;
    }    
    
    if (NPT_FAILED(action->GetArgumentValue("TrackURI", info.track_uri))) {
        goto bad_action;
    }

    if (NPT_FAILED(action->GetArgumentValue("RelTime", value))) {
        goto bad_action;
    }

	// NOT_IMPLEMENTED is a valid value according to spec
	if (value != "NOT_IMPLEMENTED" && NPT_FAILED(PLT_Didl::ParseTimeStamp(value, info.rel_time))) {
		// some dogy renderers return garbage sometimes
		info.rel_time = NPT_TimeStamp(-1.0f);
    }

    if (NPT_FAILED(action->GetArgumentValue("AbsTime", value))) {
        goto bad_action;
    }
    
	// NOT_IMPLEMENTED is a valid value according to spec
	if (value != "NOT_IMPLEMENTED" && NPT_FAILED(PLT_Didl::ParseTimeStamp(value, info.abs_time))) {
		// some dogy renderers return garbage sometimes
		info.abs_time = NPT_TimeStamp(-1.0f);
    }

    if (NPT_FAILED(action->GetArgumentValue("RelCount", info.rel_count))) {
        goto bad_action;
    }    
    if (NPT_FAILED(action->GetArgumentValue("AbsCount", info.abs_count))) {
        goto bad_action;
    }

    m_Delegate->OnGetPositionInfoResult(NPT_SUCCESS, device, &info, userdata);
    return NPT_SUCCESS;

bad_action:
    m_Delegate->OnGetPositionInfoResult(NPT_FAILURE, device, NULL, userdata);
    return NPT_FAILURE;
}

/*----------------------------------------------------------------------
|   PLT_MediaController::OnGetTransportInfoResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaController::OnGetTransportInfoResponse(NPT_Result               res, 
                                                PLT_DeviceDataReference& device, 
                                                PLT_ActionReference&     action, 
                                                void*                    userdata)
{
    PLT_TransportInfo info;

    if (NPT_FAILED(res) || action->GetErrorCode() != 0) {
        goto bad_action;
    }

    if (NPT_FAILED(action->GetArgumentValue("CurrentTransportState", info.cur_transport_state))) {
        goto bad_action;
    }    
    if (NPT_FAILED(action->GetArgumentValue("CurrentTransportStatus", info.cur_transport_status))) {
        goto bad_action;
    }    
    if (NPT_FAILED(action->GetArgumentValue("CurrentSpeed", info.cur_speed))) {
        goto bad_action;
    }    

    m_Delegate->OnGetTransportInfoResult(NPT_SUCCESS, device, &info, userdata);
    return NPT_SUCCESS;

bad_action:
    m_Delegate->OnGetTransportInfoResult(NPT_FAILURE, device, NULL, userdata);
    return NPT_FAILURE;
}

/*----------------------------------------------------------------------
|   PLT_MediaController::OnGetTransportSettingsResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaController::OnGetTransportSettingsResponse(NPT_Result               res, 
                                                    PLT_DeviceDataReference& device, 
                                                    PLT_ActionReference&     action, 
                                                    void*                    userdata)
{
    PLT_TransportSettings settings;

    if (NPT_FAILED(res) || action->GetErrorCode() != 0) {
        goto bad_action;
    }

    if (NPT_FAILED(action->GetArgumentValue("PlayMode", settings.play_mode))) {
        goto bad_action;
    }    
    if (NPT_FAILED(action->GetArgumentValue("RecQualityMode", settings.rec_quality_mode))) {
        goto bad_action;
    }    

    m_Delegate->OnGetTransportSettingsResult(NPT_SUCCESS, device, &settings, userdata);
    return NPT_SUCCESS;

bad_action:
    m_Delegate->OnGetTransportSettingsResult(NPT_FAILURE, device, NULL, userdata);
    return NPT_FAILURE;
}

/*----------------------------------------------------------------------
|   PLT_MediaController::OnGetCurrentConnectionIDsResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaController::OnGetCurrentConnectionIDsResponse(NPT_Result               res, 
                                                       PLT_DeviceDataReference& device, 
                                                       PLT_ActionReference&     action, 
                                                       void*                    userdata)
{
    NPT_String value;
    PLT_StringList IDs;

    if (NPT_FAILED(res) || action->GetErrorCode() != 0) {
        goto bad_action;
    }

    if (NPT_FAILED(action->GetArgumentValue("ConnectionIDs", value))) {
        goto bad_action;
    }
    // parse the list of medias and return a list to listener
    ParseCSV(value, IDs);

    m_Delegate->OnGetCurrentConnectionIDsResult(NPT_SUCCESS, device, &IDs, userdata);
    return NPT_SUCCESS;

bad_action:
    m_Delegate->OnGetCurrentConnectionIDsResult(NPT_FAILURE, device, NULL, userdata);
    return NPT_FAILURE;
}

/*----------------------------------------------------------------------
|   PLT_MediaController::OnGetCurrentConnectionInfoResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaController::OnGetCurrentConnectionInfoResponse(NPT_Result               res, 
                                                        PLT_DeviceDataReference& device, 
                                                        PLT_ActionReference&     action, 
                                                        void*                    userdata)
{
    NPT_String value;
    PLT_ConnectionInfo info;

    if (NPT_FAILED(res) || action->GetErrorCode() != 0) {
        goto bad_action;
    }

    if (NPT_FAILED(action->GetArgumentValue("RcsID", info.rcs_id))) {
        goto bad_action;
    }
    if (NPT_FAILED(action->GetArgumentValue("AVTransportID", info.avtransport_id))) {
        goto bad_action;
    }
    if (NPT_FAILED(action->GetArgumentValue("ProtocolInfo", info.protocol_info))) {
        goto bad_action;
    }
    if (NPT_FAILED(action->GetArgumentValue("PeerConnectionManager", info.peer_connection_mgr))) {
        goto bad_action;
    }
    if (NPT_FAILED(action->GetArgumentValue("PeerConnectionID", info.peer_connection_id))) {
        goto bad_action;
    }
    if (NPT_FAILED(action->GetArgumentValue("Direction", info.direction))) {
        goto bad_action;
    }
    if (NPT_FAILED(action->GetArgumentValue("Status", info.status))) {
        goto bad_action;
    }
    m_Delegate->OnGetCurrentConnectionInfoResult(NPT_SUCCESS, device, &info, userdata);
    return NPT_SUCCESS;

bad_action:
    m_Delegate->OnGetCurrentConnectionInfoResult(NPT_FAILURE, device, NULL, userdata);
    return NPT_FAILURE;
}

/*----------------------------------------------------------------------
|   PLT_MediaController::OnGetProtocolInfoResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaController::OnGetProtocolInfoResponse(NPT_Result               res, 
                                               PLT_DeviceDataReference& device, 
                                               PLT_ActionReference&     action, 
                                               void*                    userdata)
{
    NPT_String     source_info, sink_info;
    PLT_StringList sources, sinks;

    if (NPT_FAILED(res) || action->GetErrorCode() != 0) {
        goto bad_action;
    }

    if (NPT_FAILED(action->GetArgumentValue("Source", source_info))) {
        goto bad_action;
    }
    ParseCSV(source_info, sources);

    if (NPT_FAILED(action->GetArgumentValue("Sink", sink_info))) {
        goto bad_action;
    }
    ParseCSV(sink_info, sinks);

    m_Delegate->OnGetProtocolInfoResult(NPT_SUCCESS, device, &sources, &sinks, userdata);
    return NPT_SUCCESS;

bad_action:
    m_Delegate->OnGetProtocolInfoResult(NPT_FAILURE, device, NULL, NULL, userdata);
    return NPT_FAILURE;
}

/*----------------------------------------------------------------------
|   PLT_MediaController::OnGetMuteResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaController::OnGetMuteResponse(NPT_Result               res, 
                                       PLT_DeviceDataReference& device, 
                                       PLT_ActionReference&     action, 
                                       void*                    userdata)
{
    NPT_String channel, mute;

    if (NPT_FAILED(res) || action->GetErrorCode() != 0) {
        goto bad_action;
    }

    if (NPT_FAILED(action->GetArgumentValue("Channel", channel))) {
        goto bad_action;
    }

    if (NPT_FAILED(action->GetArgumentValue("CurrentMute", mute))) {
        goto bad_action;
    }

    m_Delegate->OnGetMuteResult(
        NPT_SUCCESS, 
        device, 
        channel, 
        PLT_Service::IsTrue(mute)?true:false, 
        userdata);
    return NPT_SUCCESS;

bad_action:
    m_Delegate->OnGetMuteResult(NPT_FAILURE, device, "", false, userdata);
    return NPT_FAILURE;
}

/*----------------------------------------------------------------------
|   PLT_MediaController::OnGetVolumeResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaController::OnGetVolumeResponse(NPT_Result               res, 
										 PLT_DeviceDataReference& device, 
										 PLT_ActionReference&	  action, 
										 void*                    userdata) 
{
	NPT_String channel;
	NPT_String current_volume;
	NPT_UInt32 volume;
	
	if (NPT_FAILED(res) || action->GetErrorCode() != 0) {
        goto bad_action;
    }

	if (NPT_FAILED(action->GetArgumentValue("Channel", channel))) {
        goto bad_action;
    }

	if (NPT_FAILED(action->GetArgumentValue("CurrentVolume", current_volume))) {
        goto bad_action;
    }

	if (NPT_FAILED(current_volume.ToInteger(volume))) {
		  goto bad_action;
	}

	m_Delegate->OnGetVolumeResult(NPT_SUCCESS, device, channel, volume, userdata);
	return NPT_SUCCESS;

bad_action:
    m_Delegate->OnGetVolumeResult(NPT_FAILURE, device, "", 0, userdata);
    return NPT_FAILURE;
}

/*----------------------------------------------------------------------
|   PLT_MediaController::OnEventNotify
+---------------------------------------------------------------------*/
NPT_Result
PLT_MediaController::OnEventNotify(PLT_Service*                  service, 
                                   NPT_List<PLT_StateVariable*>* vars)
{   
    if (!service->GetDevice()->GetType().StartsWith("urn:schemas-upnp-org:device:MediaRenderer"))
        return NPT_FAILURE;

    if (!m_Delegate) return NPT_SUCCESS;

    /* make sure device associated to service is still around */
    PLT_DeviceDataReference data;
    NPT_CHECK_WARNING(FindRenderer(service->GetDevice()->GetUUID(), data));
    
    m_Delegate->OnMRStateVariablesChanged(service, vars);
    return NPT_SUCCESS;
}
