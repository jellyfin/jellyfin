/*****************************************************************
|
|   Platinum - Media Crawler
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

#ifndef _CRAWLER_H_
#define _CRAWLER_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Platinum.h"
#include "PltMediaConnect.h"
#include "PltSyncMediaBrowser.h"
#include "StreamHandler.h"

/*----------------------------------------------------------------------
|   CMediaCrawler
+---------------------------------------------------------------------*/
class CMediaCrawler : public PLT_MediaBrowser,
                      public PLT_MediaServer

{
public:
    CMediaCrawler(PLT_CtrlPointReference& ctrlPoint,
                  const char*             friendly_name = "Platinum Crawler",
                  bool                    show_ip = false,
                  const char*             udn = NULL,
                  unsigned int            port = 0);
    virtual ~CMediaCrawler();

    NPT_Result AddStreamHandler(CStreamHandler* handler);

protected:
    // PLT_MediaServer methods
    NPT_Result OnBrowse(PLT_ActionReference&          action, 
                        const PLT_HttpRequestContext& context);

    // PLT_MediaBrowser methods
    NPT_Result OnBrowseResponse(NPT_Result               res, 
                                PLT_DeviceDataReference& device, 
                                PLT_ActionReference&     action, 
                                void*                    userdata);
    
    // File Server Listener methods
    NPT_Result ProcessFileRequest(NPT_HttpRequest&              request, 
                                  const NPT_HttpRequestContext& context,
                                  NPT_HttpResponse&             response);

private:
    // methods
    NPT_Result OnBrowseRoot(PLT_ActionReference& action);
    NPT_Result OnBrowseDevice(PLT_ActionReference&          action, 
                              const char*                   server_uuid, 
                              const char*                   server_object_id, 
                              const NPT_HttpRequestContext& context);

    NPT_Result SplitObjectId(const NPT_String& object_id, 
                             NPT_String&       server_uuid, 
                             NPT_String&       server_object_id);
    NPT_String FormatObjectId(const NPT_String& server_uuid, 
                              const NPT_String& server_object_id);

    NPT_String UpdateDidl(const char*              server_uuid, 
                          const NPT_String&        didl, 
                          const NPT_SocketAddress* req_local_address = NULL);

    // members
    NPT_List<CStreamHandler*> m_StreamHandlers;
};

/*----------------------------------------------------------------------
|   CMediaCrawlerBrowseInfo
+---------------------------------------------------------------------*/
struct CMediaCrawlerBrowseInfo {
    NPT_SharedVariable shared_var;
    NPT_Result         res;
    int                code;
    NPT_String         object_id;
    NPT_String         didl;
    NPT_String         nr;
    NPT_String         tm;
    NPT_String         uid;
};

typedef NPT_Reference<CMediaCrawlerBrowseInfo> CMediaCrawlerBrowseInfoReference;

#endif /* _CRAWLER_H_ */

