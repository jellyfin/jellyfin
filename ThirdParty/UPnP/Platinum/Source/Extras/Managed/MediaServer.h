/*****************************************************************
|
|   Platinum - Managed MediaServer
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
#pragma once

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "DeviceHost.h"
#include "Http.h"

class PLT_MediaServer;

namespace Platinum
{

ref class DeviceHost;
ref class Action;
class MediaServerEventBridge;

/*----------------------------------------------------------------------
|   MediaServer
+---------------------------------------------------------------------*/
public ref class MediaServer : public DeviceHost
{
public:

    delegate Int32 BrowseMetadataDelegate(Action^ action, String^ object_id, String^ filter, Int32 starting_index, Int32 requested_count, String^ sort_criteria, HttpRequestContext^ context);
    delegate Int32 BrowseDirectChildrenDelegate(Action^ action, String^ object_id, String^ filter, Int32 starting_index, Int32 requested_count, String^ sort_criteria, HttpRequestContext^ context);
    delegate Int32 SearchContainerDelegate(Action^ action, String^ object_id, String^ searchCriteria, String^ filter, Int32 starting_index, Int32 requested_count, String^ sort_criteria, HttpRequestContext^ context);
    delegate Int32 ProcessFileRequestDelegate(HttpRequestContext^ context, HttpResponse^ response);

private:

    MediaServerEventBridge* m_pBridge;

public:

    // properties

private:

    void RegisterEvents();

public:

    event BrowseMetadataDelegate^ BrowseMetadata;
    event BrowseDirectChildrenDelegate^ BrowseDirectChildren;
    event SearchContainerDelegate^ SearchContainer;
    event ProcessFileRequestDelegate^ ProcessFileRequest;


internal:

    Int32 OnBrowseMetadataDelegate(Action^ action, String^ object_id, String^ filter, Int32 starting_index, Int32 requested_count, String^ sort_criteria, HttpRequestContext^ context)
    {
        // handle events
        return this->BrowseMetadata(action, object_id, filter, starting_index, requested_count, sort_criteria, context);
    }

    Int32 OnBrowseDirectChildrenDelegate(Action^ action, String^ object_id, String^ filter, Int32 starting_index, Int32 requested_count, String^ sort_criteria, HttpRequestContext^ context)
    {
        // handle events
        return this->BrowseDirectChildren(action, object_id, filter, starting_index, requested_count, sort_criteria, context);
    }

    Int32 OnSearchContainerDelegate(Action^ action, String^ object_id, String^ searchCriteria, String^ filter, Int32 starting_index, Int32 requested_count, String^ sort_criteria, HttpRequestContext^ context)
    {
        // handle events
        return this->SearchContainer(action, object_id, searchCriteria, filter, starting_index, requested_count, sort_criteria, context);
    }

    Int32 OnProcessFileRequestDelegate(HttpRequestContext^ context, HttpResponse^ response)
    {
        return this->ProcessFileRequest(context, response);
    }

public:

    MediaServer(String^ friendlyName);
    MediaServer(String^ friendlyName, String^ uuid);
    MediaServer(PLT_MediaServer* server);
    
    void UpdateSystemUpdateID(Int32 update);
    void UpdateContainerUpdateID(String^ id, Int32 update);

    ~MediaServer()
    {
        // clean-up managed

        // clean-up unmanaged
        this->!MediaServer();
    }

    !MediaServer();


    static Int32 SetResponseFilePath(HttpRequestContext^ context, HttpResponse^ response, String^ filepath);
    static Int32 SetResponseData(HttpRequestContext^ context, HttpResponse^ response, array<Byte>^ data);
};

}