/*****************************************************************
|
|   Platinum - Stream Handler
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

#ifndef _PLT_STREAM_HANDLER_H_
#define _PLT_STREAM_HANDLER_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptTypes.h"
#include "NptStreams.h"
#include "NptStrings.h"
#include "NptTime.h"

/*----------------------------------------------------------------------
|   forward declarations
+---------------------------------------------------------------------*/
class CMediaCrawler;

/*----------------------------------------------------------------------
|   CStreamHandler
+---------------------------------------------------------------------*/
class CStreamHandler
{
public:
    CStreamHandler(CMediaCrawler* crawler) : m_MediaCrawler(crawler) {}
    virtual ~CStreamHandler() {}

    // overridables
    virtual bool       HandleResource(const char* prot_info, const char* url) = 0;
    virtual NPT_Result ModifyResource(NPT_XmlElementNode* resource, NPT_SocketInfo* info = NULL) = 0;
    virtual NPT_Result ProcessFileRequest(NPT_HttpRequest& request, NPT_HttpResponse*& response) = 0;

protected:
    CMediaCrawler* m_MediaCrawler;
};

/*----------------------------------------------------------------------
|   CStreamHandlerFinder
+---------------------------------------------------------------------*/
class CStreamHandlerFinder
{
public:
    // methods
    CStreamHandlerFinder(const char* prot_info, const char* url) : m_ProtInfo(prot_info), m_Url(url) {}
    bool operator()(CStreamHandler* const & handler) const {
        return handler->HandleResource(m_ProtInfo, m_Url);
    }

private:
    // members
    NPT_String m_ProtInfo;
    NPT_String m_Url;
};

/*----------------------------------------------------------------------
|   CPassThroughStreamHandler
+---------------------------------------------------------------------*/
class CPassThroughStreamHandler : public CStreamHandler
{
public:
    CPassThroughStreamHandler(CMediaCrawler* crawler) : CStreamHandler(crawler) {}
    virtual ~CPassThroughStreamHandler() {}

    // overridables
    virtual bool HandleResource(const char* /*prot_info*/, const char* /*url*/) {
        return true;
    }

    virtual NPT_Result ModifyResource(NPT_XmlElementNode* resource, 
                                      NPT_SocketInfo*     info = NULL) {
        NPT_COMPILER_UNUSED(resource);
        NPT_COMPILER_UNUSED(info);
        return NPT_SUCCESS;
    }

    virtual NPT_Result ProcessFileRequest(NPT_HttpRequest&   request, 
                                          NPT_HttpResponse*& response) {
        NPT_HttpClient client;
        return client.SendRequest(request, response);
    }

};

#endif /* _PLT_STREAM_HANDLER_H_ */
