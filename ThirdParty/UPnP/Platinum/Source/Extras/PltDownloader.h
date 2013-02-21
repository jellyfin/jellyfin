/*****************************************************************
|
|   Platinum - Downloader
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

#ifndef _PLT_DOWNLOADER_H_
#define _PLT_DOWNLOADER_H_

/*----------------------------------------------------------------------
|   Includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "PltHttpClientTask.h"

/*----------------------------------------------------------------------
|   forward declarations
+---------------------------------------------------------------------*/
class PLT_Downloader;

/*----------------------------------------------------------------------
|   types
+---------------------------------------------------------------------*/
typedef PLT_HttpClientTask<PLT_Downloader> PLT_HttpDownloadTask;

typedef enum {
    PLT_DOWNLOADER_IDLE,
    PLT_DOWNLOADER_STARTED,
    PLT_DOWNLOADER_DOWNLOADING,
    PLT_DOWNLOADER_ERROR,
    PLT_DOWNLOADER_SUCCESS
} Plt_DowloaderState;

/*----------------------------------------------------------------------
|   PLT_Downloader class
+---------------------------------------------------------------------*/
class PLT_Downloader
{
public:
    PLT_Downloader(PLT_TaskManager*           task_manager, 
                   NPT_HttpUrl&               url, 
                   NPT_OutputStreamReference& output);
    virtual ~PLT_Downloader();

    NPT_Result Start();
    NPT_Result Stop();
    Plt_DowloaderState GetState() { return m_State; }

    // PLT_HttpClientTask method
    NPT_Result ProcessResponse(NPT_Result                    res, 
                               const NPT_HttpRequest&        request, 
                               const NPT_HttpRequestContext& context, 
                               NPT_HttpResponse*             response);


private:
    // members
    NPT_HttpUrl               m_URL;
    NPT_OutputStreamReference m_Output;
    PLT_TaskManager*          m_TaskManager;
    PLT_HttpDownloadTask*     m_Task;
    Plt_DowloaderState        m_State;
};

#endif /* _PLT_DOWNLOADER_H_ */
