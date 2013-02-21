/*****************************************************************
|
|   Platinum - AV Media Renderer Device
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

#ifndef _PLT_MEDIA_RENDERER_H_
#define _PLT_MEDIA_RENDERER_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "PltDeviceHost.h"

/*----------------------------------------------------------------------
|   PLT_MediaRendererDelegate
+---------------------------------------------------------------------*/
class PLT_MediaRendererDelegate
{
public:
    virtual ~PLT_MediaRendererDelegate() {}

    // ConnectionManager
    virtual NPT_Result OnGetCurrentConnectionInfo(PLT_ActionReference& action) = 0;

    // AVTransport
    virtual NPT_Result OnNext(PLT_ActionReference& action) = 0;
    virtual NPT_Result OnPause(PLT_ActionReference& action) = 0;
    virtual NPT_Result OnPlay(PLT_ActionReference& action) = 0;
    virtual NPT_Result OnPrevious(PLT_ActionReference& action) = 0;
    virtual NPT_Result OnSeek(PLT_ActionReference& action) = 0;
    virtual NPT_Result OnStop(PLT_ActionReference& action) = 0;
    virtual NPT_Result OnSetAVTransportURI(PLT_ActionReference& action) = 0;
    virtual NPT_Result OnSetPlayMode(PLT_ActionReference& action) = 0;

    // RenderingControl
    virtual NPT_Result OnSetVolume(PLT_ActionReference& action) = 0;
    virtual NPT_Result OnSetVolumeDB(PLT_ActionReference& action) = 0;
    virtual NPT_Result OnGetVolumeDBRange(PLT_ActionReference& action) = 0;
    virtual NPT_Result OnSetMute(PLT_ActionReference& action) = 0;
};

/*----------------------------------------------------------------------
|   PLT_MediaRenderer
+---------------------------------------------------------------------*/
class PLT_MediaRenderer : public PLT_DeviceHost
{
public:
    PLT_MediaRenderer(const char*  friendly_name,
                      bool         show_ip = false,
                      const char*  uuid = NULL,
                      unsigned int port = 0,
                      bool         port_rebind = false);
    // methods
    virtual void SetDelegate(PLT_MediaRendererDelegate* delegate) { m_Delegate = delegate; }

    // PLT_DeviceHost methods
    virtual NPT_Result SetupServices();
    virtual NPT_Result OnAction(PLT_ActionReference&          action, 
                                const PLT_HttpRequestContext& context);

protected:
    virtual ~PLT_MediaRenderer();

    // PLT_MediaRendererInterface methods
    // ConnectionManager
    virtual NPT_Result OnGetCurrentConnectionInfo(PLT_ActionReference& action);

    // AVTransport
    virtual NPT_Result OnNext(PLT_ActionReference& action);
    virtual NPT_Result OnPause(PLT_ActionReference& action);
    virtual NPT_Result OnPlay(PLT_ActionReference& action);
    virtual NPT_Result OnPrevious(PLT_ActionReference& action);
    virtual NPT_Result OnSeek(PLT_ActionReference& action);
    virtual NPT_Result OnStop(PLT_ActionReference& action);
    virtual NPT_Result OnSetAVTransportURI(PLT_ActionReference& action);
    virtual NPT_Result OnSetPlayMode(PLT_ActionReference& action);

    // RenderingControl
    virtual NPT_Result OnSetVolume(PLT_ActionReference& action);
    virtual NPT_Result OnSetVolumeDB(PLT_ActionReference &action);
    virtual NPT_Result OnGetVolumeDBRange(PLT_ActionReference &action);
    virtual NPT_Result OnSetMute(PLT_ActionReference& action);

private:
    PLT_MediaRendererDelegate* m_Delegate;
};

#endif /* _PLT_MEDIA_RENDERER_H_ */
