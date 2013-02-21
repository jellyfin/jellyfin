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

#ifndef _PLT_MEDIA_CONTROLLER_H_
#define _PLT_MEDIA_CONTROLLER_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "PltCtrlPoint.h"
#include "PltMediaItem.h"

/*----------------------------------------------------------------------
|   Defines
+---------------------------------------------------------------------*/
typedef NPT_List<NPT_String> PLT_StringList;

typedef struct {
    PLT_StringList play_media;
    PLT_StringList rec_media;
    PLT_StringList rec_quality_modes;
} PLT_DeviceCapabilities;

typedef struct {
    NPT_UInt32    num_tracks;
    NPT_TimeStamp media_duration;
    NPT_String    cur_uri;
    NPT_String    cur_metadata;
    NPT_String    next_uri;
    NPT_String    next_metadata;
    NPT_String    play_medium;
    NPT_String    rec_medium;
    NPT_String    write_status;
} PLT_MediaInfo;

typedef struct {
    NPT_UInt32    track;
    NPT_TimeStamp track_duration;
    NPT_String    track_metadata;
    NPT_String    track_uri;
    NPT_TimeStamp rel_time;
    NPT_TimeStamp abs_time;
    NPT_Int32     rel_count;
    NPT_Int32     abs_count;
} PLT_PositionInfo;

typedef struct {
    NPT_String cur_transport_state;
    NPT_String cur_transport_status;
    NPT_String cur_speed;
} PLT_TransportInfo;

typedef struct {
    NPT_String play_mode;
    NPT_String rec_quality_mode;
} PLT_TransportSettings;

typedef struct {
    NPT_UInt32 rcs_id;
    NPT_UInt32 avtransport_id;
    NPT_String protocol_info;
    NPT_String peer_connection_mgr;
    NPT_UInt32 peer_connection_id;
    NPT_String direction;
    NPT_String status;
} PLT_ConnectionInfo;

/*----------------------------------------------------------------------
|   PLT_MediaControllerDelegate
+---------------------------------------------------------------------*/
class PLT_MediaControllerDelegate
{
public:
    virtual ~PLT_MediaControllerDelegate() {}

    virtual bool OnMRAdded(PLT_DeviceDataReference& /* device */) { return true; }
    virtual void OnMRRemoved(PLT_DeviceDataReference& /* device */) {}
    virtual void OnMRStateVariablesChanged(PLT_Service*                  /* service */, 
                                           NPT_List<PLT_StateVariable*>* /* vars */) {}

    // AVTransport
    virtual void OnGetCurrentTransportActionsResult(
        NPT_Result               /* res */, 
        PLT_DeviceDataReference& /* device */,
        PLT_StringList*          /* actions */, 
        void*                    /* userdata */) {}

    virtual void OnGetDeviceCapabilitiesResult(
        NPT_Result               /* res */, 
        PLT_DeviceDataReference& /* device */,
        PLT_DeviceCapabilities*  /* capabilities */,
        void*                    /* userdata */) {}

    virtual void OnGetMediaInfoResult(
        NPT_Result               /* res */,
        PLT_DeviceDataReference& /* device */,
        PLT_MediaInfo*           /* info */,
        void*                    /* userdata */) {}

    virtual void OnGetPositionInfoResult(
        NPT_Result               /* res */,
        PLT_DeviceDataReference& /* device */,
        PLT_PositionInfo*        /* info */,
        void*                    /* userdata */) {}

    virtual void OnGetTransportInfoResult(
        NPT_Result               /* res */,
        PLT_DeviceDataReference& /* device */,
        PLT_TransportInfo*       /* info */,
        void*                    /* userdata */) {}

    virtual void OnGetTransportSettingsResult(
        NPT_Result               /* res */,
        PLT_DeviceDataReference& /* device */,
        PLT_TransportSettings*   /* settings */,
        void*                    /* userdata */) {}

    virtual void OnNextResult(
        NPT_Result               /* res */,
        PLT_DeviceDataReference& /* device */,
        void*                    /* userdata */) {}

    virtual void OnPauseResult(
        NPT_Result               /* res */,
        PLT_DeviceDataReference& /* device */,
        void*                    /* userdata */) {}  

    virtual void OnPlayResult(
        NPT_Result               /* res */,
        PLT_DeviceDataReference& /* device */,
        void*                    /* userdata */) {}

    virtual void OnPreviousResult(
        NPT_Result               /* res */,
        PLT_DeviceDataReference& /* device */,
        void*                    /* userdata */) {}

    virtual void OnSeekResult(
        NPT_Result               /* res */,
        PLT_DeviceDataReference& /* device */,
        void*                    /* userdata */) {}

    virtual void OnSetAVTransportURIResult(
        NPT_Result               /* res */,
        PLT_DeviceDataReference& /* device */,
        void*                    /* userdata */) {}

    virtual void OnSetPlayModeResult(
        NPT_Result               /* res */,
        PLT_DeviceDataReference& /* device */,
        void*                    /* userdata */) {}

    virtual void OnStopResult(
        NPT_Result               /* res */,
        PLT_DeviceDataReference& /* device */,
        void*                    /* userdata */) {}
        
    // ConnectionManager
    virtual void OnGetCurrentConnectionIDsResult(
        NPT_Result               /* res */,
        PLT_DeviceDataReference& /* device */,
        PLT_StringList*          /* ids */,
        void*                    /* userdata */) {}

    virtual void OnGetCurrentConnectionInfoResult(
        NPT_Result               /* res */,
        PLT_DeviceDataReference& /* device */,
        PLT_ConnectionInfo*      /* info */,
        void*                    /* userdata */) {}

    virtual void OnGetProtocolInfoResult(
        NPT_Result               /* res */,
        PLT_DeviceDataReference& /* device */,
        PLT_StringList*          /* sources */,
        PLT_StringList*          /* sinks */,
        void*                    /* userdata */) {}
        
    // RenderingControl
    virtual void OnSetMuteResult(
        NPT_Result               /* res */,
        PLT_DeviceDataReference& /* device */,
        void*                    /* userdata */) {}

    virtual void OnGetMuteResult(
        NPT_Result               /* res */,
        PLT_DeviceDataReference& /* device */,
        const char*              /* channel */,
        bool                     /* mute */,
        void*                    /* userdata */) {}

	virtual void OnSetVolumeResult(
        NPT_Result               /* res */,
        PLT_DeviceDataReference& /* device */,
        void*                    /* userdata */) {}

	virtual void OnGetVolumeResult(
        NPT_Result               /* res */,
        PLT_DeviceDataReference& /* device */,
		const char*              /* channel */,
    	NPT_UInt32				 /* volume */,
	    void*                    /* userdata */) {}
};

/*----------------------------------------------------------------------
|   PLT_MediaController
+---------------------------------------------------------------------*/
class PLT_MediaController : public PLT_CtrlPointListener
{
public:
    PLT_MediaController(PLT_CtrlPointReference&      ctrl_point, 
                        PLT_MediaControllerDelegate* delegate = NULL);
    virtual ~PLT_MediaController();

    // public methods
    virtual void SetDelegate(PLT_MediaControllerDelegate* delegate) {
        m_Delegate = delegate;
    }

    // PLT_CtrlPointListener methods
    virtual NPT_Result OnDeviceAdded(PLT_DeviceDataReference& device);
    virtual NPT_Result OnDeviceRemoved(PLT_DeviceDataReference& device);
    virtual NPT_Result OnActionResponse(NPT_Result res, PLT_ActionReference& action, void* userdata);
    virtual NPT_Result OnEventNotify(PLT_Service* service, NPT_List<PLT_StateVariable*>* vars);

    // AVTransport
    NPT_Result GetCurrentTransportActions(PLT_DeviceDataReference& device, NPT_UInt32 instance_id, void* userdata);
    NPT_Result GetDeviceCapabilities(PLT_DeviceDataReference& device, NPT_UInt32 instance_id, void* userdata);
    NPT_Result GetMediaInfo(PLT_DeviceDataReference& device, NPT_UInt32 instance_id, void* userdata);
    NPT_Result GetPositionInfo(PLT_DeviceDataReference& device,  NPT_UInt32 instance_id, void* userdata);
    NPT_Result GetTransportInfo(PLT_DeviceDataReference& device, NPT_UInt32 instance_id, void* userdata);
    NPT_Result GetTransportSettings(PLT_DeviceDataReference& device, NPT_UInt32 instance_id, void* userdata);
    NPT_Result Next(PLT_DeviceDataReference& device, NPT_UInt32 instance_id, void* userdata);
    NPT_Result Pause(PLT_DeviceDataReference& device, NPT_UInt32 instance_id, void* userdata);
    NPT_Result Play(PLT_DeviceDataReference&  device, NPT_UInt32 instance_id, NPT_String speed, void* userdata);
    NPT_Result Previous(PLT_DeviceDataReference& device, NPT_UInt32 instance_id, void* userdata);
    NPT_Result Seek(PLT_DeviceDataReference&  device, NPT_UInt32 instance_id, NPT_String unit, NPT_String target, void* userdata);
    bool       CanSetNextAVTransportURI(PLT_DeviceDataReference& device);
    NPT_Result SetAVTransportURI(PLT_DeviceDataReference& device, NPT_UInt32 instance_id, const char* uri, const char* metadata, void* userdata);
    NPT_Result SetNextAVTransportURI(PLT_DeviceDataReference& device, NPT_UInt32 instance_id, const char* next_uri, const char* next_metadata, void* userdata);
    NPT_Result SetPlayMode(PLT_DeviceDataReference&  device, NPT_UInt32 instance_id, NPT_String new_play_mode, void* userdata);
    NPT_Result Stop(PLT_DeviceDataReference& device, NPT_UInt32 instance_id, void* userdata);

    // ConnectionManager
    NPT_Result GetCurrentConnectionIDs(PLT_DeviceDataReference& device, void* userdata);
    NPT_Result GetCurrentConnectionInfo(PLT_DeviceDataReference& device, NPT_UInt32 connection_id, void* userdata);
    NPT_Result GetProtocolInfo(PLT_DeviceDataReference& device, void* userdata);
    
    // RenderingControl
    NPT_Result SetMute(PLT_DeviceDataReference& device, NPT_UInt32 instance_id, const char* channel, bool mute, void* userdata);
    NPT_Result GetMute(PLT_DeviceDataReference& device, NPT_UInt32 instance_id, const char* channel, void* userdata);
	NPT_Result SetVolume(PLT_DeviceDataReference& device, NPT_UInt32 instance_id, const char* channel, int volume, void* userdata);
	NPT_Result GetVolume(PLT_DeviceDataReference& device, NPT_UInt32 instance_id, const char* channel, void* userdata);	

    // VariableStates    
    virtual NPT_Result GetProtocolInfoSink(const NPT_String& device_uuid, NPT_List<NPT_String>& sinks);
    virtual NPT_Result GetTransportState(const NPT_String&  device_uuid, NPT_String& state);
    virtual NPT_Result GetVolumeState(const NPT_String&  device_uuid, NPT_UInt32& volume);
    
    // methods
    virtual NPT_Result FindRenderer(const char* uuid, PLT_DeviceDataReference& device);
    virtual NPT_Result FindMatchingProtocolInfo(NPT_List<NPT_String>& sinks, const char* protocol_info);
    virtual NPT_Result FindBestResource(PLT_DeviceDataReference& device, PLT_MediaObject& item, NPT_Cardinal& resource_index);

private:
    NPT_Result InvokeActionWithInstance(PLT_ActionReference& action, NPT_UInt32 instance_id, void* userdata = NULL);

    NPT_Result OnGetCurrentTransportActionsResponse(NPT_Result res, PLT_DeviceDataReference& device, PLT_ActionReference& action, void* userdata);
    NPT_Result OnGetDeviceCapabilitiesResponse(NPT_Result res, PLT_DeviceDataReference& device, PLT_ActionReference& action, void* userdata);
    NPT_Result OnGetMediaInfoResponse(NPT_Result res, PLT_DeviceDataReference& device, PLT_ActionReference& action, void* userdata);
    NPT_Result OnGetPositionInfoResponse(NPT_Result res, PLT_DeviceDataReference& device, PLT_ActionReference& action, void* userdata);
    NPT_Result OnGetTransportInfoResponse(NPT_Result res, PLT_DeviceDataReference& device, PLT_ActionReference& action, void* userdata);
    NPT_Result OnGetTransportSettingsResponse(NPT_Result res, PLT_DeviceDataReference& device, PLT_ActionReference& action, void* userdata);

    NPT_Result OnGetCurrentConnectionIDsResponse(NPT_Result res, PLT_DeviceDataReference& device, PLT_ActionReference& action, void* userdata);
    NPT_Result OnGetCurrentConnectionInfoResponse(NPT_Result res, PLT_DeviceDataReference& device, PLT_ActionReference& action, void* userdata);
    NPT_Result OnGetProtocolInfoResponse(NPT_Result res, PLT_DeviceDataReference& device, PLT_ActionReference& action, void* userdata);
    
    NPT_Result OnGetMuteResponse(NPT_Result res, PLT_DeviceDataReference& device, PLT_ActionReference& action, void* userdata);
	NPT_Result OnGetVolumeResponse(NPT_Result res, PLT_DeviceDataReference& device, PLT_ActionReference& action, void* userdata);

public:
    static void ParseCSV(const char* csv, PLT_StringList& values) {
        const char* start = csv;
        const char* p = start;

        // look for the , character
        while (*p) {
            if (*p == ',') {
                NPT_String val(start, (int)(p-start));
                val.Trim(' ');
                values.Add(val);
                start = p + 1;
            }
            p++;
        }

        // last one
        NPT_String last(start, (int)(p-start));
        last.Trim(' ');
        if (last.GetLength()) {
            values.Add(last);
        }
    }

private:
    PLT_CtrlPointReference                m_CtrlPoint;
    PLT_MediaControllerDelegate*          m_Delegate;
    NPT_Lock<PLT_DeviceDataReferenceList> m_MediaRenderers;
};

typedef NPT_Reference<PLT_MediaController> PLT_MediaControllerReference;

#endif /* _PLT_MEDIA_CONTROLLER_H_ */
