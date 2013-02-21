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

/** @file
 UPnP AV Media Controller implementation.
 */

#ifndef _PLT_MEDIA_BROWSER_H_
#define _PLT_MEDIA_BROWSER_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "PltCtrlPoint.h"
#include "PltMediaItem.h"

/*----------------------------------------------------------------------
|   PLT_BrowseInfo
+---------------------------------------------------------------------*/
/**
 The PLT_BrowseInfo struct is used to marshall Browse or Search action 
 response results across different threads of execution.
 */
typedef struct {
    NPT_String                   object_id;
    PLT_MediaObjectListReference items;
    NPT_UInt32                   si;
    NPT_UInt32                   nr;
    NPT_UInt32                   tm;
    NPT_UInt32                   uid;
} PLT_BrowseInfo;

/*----------------------------------------------------------------------
|   PLT_MediaBrowserDelegate
+---------------------------------------------------------------------*/
/**
 The PLT_MediaBrowserDelegate class is an interface for receiving PLT_MediaBrowser
 events or action responses.
 */
class PLT_MediaBrowserDelegate
{
public:
    virtual ~PLT_MediaBrowserDelegate() {}
    
    virtual bool OnMSAdded(PLT_DeviceDataReference& /* device */) { return true; }
    virtual void OnMSRemoved(PLT_DeviceDataReference& /* device */) {}
    virtual void OnMSStateVariablesChanged(
        PLT_Service*                  /*service*/, 
        NPT_List<PLT_StateVariable*>* /*vars*/) {}

    // ContentDirectory
    virtual void OnBrowseResult(
        NPT_Result               /*res*/, 
        PLT_DeviceDataReference& /*device*/, 
        PLT_BrowseInfo*          /*info*/, 
        void*                    /*userdata*/) {}

	virtual void OnSearchResult(
        NPT_Result               /*res*/, 
        PLT_DeviceDataReference& /*device*/, 
        PLT_BrowseInfo*          /*info*/, 
        void*                    /*userdata*/) {}
};

/*----------------------------------------------------------------------
|   PLT_MediaBrowser
+---------------------------------------------------------------------*/
/**
 The PLT_MediaBrowser class implements a UPnP AV Media Server control point.
 */
class PLT_MediaBrowser : public PLT_CtrlPointListener
{
public:
    PLT_MediaBrowser(PLT_CtrlPointReference&   ctrl_point,
                     PLT_MediaBrowserDelegate* delegate = NULL);
    virtual ~PLT_MediaBrowser();

    // ContentDirectory service
    virtual NPT_Result Browse(PLT_DeviceDataReference& device, 
                              const char*              object_id, 
                              NPT_UInt32               start_index,
                              NPT_UInt32               count = 30, // DLNA recommendations
                              bool                     browse_metadata = false,
                              const char*              filter = "dc:date,upnp:genre,res,res@duration,res@size,upnp:albumArtURI,upnp:originalTrackNumber,upnp:album,upnp:artist,upnp:author", // explicitely specify res otherwise WMP won't return a URL!
                              const char*              sort_criteria = "",
                              void*                    userdata = NULL);

	virtual NPT_Result Search(PLT_DeviceDataReference& device, 
		                      const char*              container_id,
							  const char*              search_criteria,
				              NPT_UInt32               start_index,
					          NPT_UInt32               count = 30, // DLNA recommendations
                              const char*              filter = "dc:date,upnp:genre,res,res@duration,res@size,upnp:albumArtURI,upnp:originalTrackNumber,upnp:album,upnp:artist,upnp:author", // explicitely specify res otherwise WMP won't return a URL!
						  	  void*                    userdata = NULL);

    // methods
    virtual const NPT_Lock<PLT_DeviceDataReferenceList>& GetMediaServers() { return m_MediaServers; }
    virtual NPT_Result FindServer(const char* uuid, PLT_DeviceDataReference& device);    
    virtual void SetDelegate(PLT_MediaBrowserDelegate* delegate) { m_Delegate = delegate; }

protected:
    // PLT_CtrlPointListener methods
    virtual NPT_Result OnDeviceAdded(PLT_DeviceDataReference& device);
    virtual NPT_Result OnDeviceRemoved(PLT_DeviceDataReference& device);
    virtual NPT_Result OnActionResponse(NPT_Result res, PLT_ActionReference& action, void* userdata);
    virtual NPT_Result OnEventNotify(PLT_Service* service, NPT_List<PLT_StateVariable*>* vars);
    
    // ContentDirectory service responses
    virtual NPT_Result OnBrowseResponse(NPT_Result               res, 
                                        PLT_DeviceDataReference& device, 
                                        PLT_ActionReference&     action, 
                                        void*                    userdata);

	virtual NPT_Result OnSearchResponse(NPT_Result               res, 
                                        PLT_DeviceDataReference& device, 
                                        PLT_ActionReference&     action, 
                                        void*                    userdata);
    
protected:
    PLT_CtrlPointReference                m_CtrlPoint;
    PLT_MediaBrowserDelegate*             m_Delegate;
    NPT_Lock<PLT_DeviceDataReferenceList> m_MediaServers;
};

#endif /* _PLT_MEDIA_BROWSER_H_ */
