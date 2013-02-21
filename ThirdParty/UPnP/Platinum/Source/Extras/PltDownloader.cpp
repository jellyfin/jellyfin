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

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "PltDownloader.h"
#include "PltTaskManager.h"

/*----------------------------------------------------------------------
|   PLT_Downloader::PLT_Downloader
+---------------------------------------------------------------------*/
PLT_Downloader::PLT_Downloader(PLT_TaskManager*           task_manager,
                               NPT_HttpUrl&               url, 
                               NPT_OutputStreamReference& output) :
    m_URL(url),
    m_Output(output),
    m_TaskManager(task_manager),
    m_Task(NULL),
    m_State(PLT_DOWNLOADER_IDLE)
{
}
    
/*----------------------------------------------------------------------
|   PLT_Downloader::~PLT_Downloader
+---------------------------------------------------------------------*/
PLT_Downloader::~PLT_Downloader()
{
    Stop();
}

/*----------------------------------------------------------------------
|   PLT_Downloader::Start
+---------------------------------------------------------------------*/
NPT_Result
PLT_Downloader::Start()
{
    Stop();

    m_Task = new PLT_HttpDownloadTask(m_URL, this);
    NPT_Result res = m_TaskManager->StartTask(m_Task, NULL, false);
    if (NPT_FAILED(res)) {
        m_State = PLT_DOWNLOADER_ERROR;
        return res;
    }

    m_State = PLT_DOWNLOADER_STARTED;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_Downloader::Stop
+---------------------------------------------------------------------*/
NPT_Result
PLT_Downloader::Stop()
{
    if (m_Task) {
        m_Task->Kill();
        m_Task = NULL;
    }

    m_State = PLT_DOWNLOADER_IDLE;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_Downloader::ProcessResponse
+---------------------------------------------------------------------*/
NPT_Result 
PLT_Downloader::ProcessResponse(NPT_Result                    res, 
                                const NPT_HttpRequest&        request, 
                                const NPT_HttpRequestContext& context, 
                                NPT_HttpResponse*             response)
{
    NPT_COMPILER_UNUSED(request);
    NPT_COMPILER_UNUSED(context);

    if (NPT_FAILED(res)) {
        m_State = PLT_DOWNLOADER_ERROR;
        return res;
    }

    m_State = PLT_DOWNLOADER_DOWNLOADING;

    NPT_HttpEntity* entity;
    NPT_InputStreamReference body;
    if (!response || 
        !(entity = response->GetEntity()) || 
        NPT_FAILED(entity->GetInputStream(body)) || 
        body.IsNull()) {
        m_State = PLT_DOWNLOADER_ERROR;
        return NPT_FAILURE;
    }

    // Read body (no content length means until socket is closed)
    res = NPT_StreamToStreamCopy(*body.AsPointer(), 
        *m_Output.AsPointer(), 
        0, 
        entity->GetContentLength());

    if (NPT_FAILED(res)) {
        m_State = PLT_DOWNLOADER_ERROR;
        return res;
    }

    m_State = PLT_DOWNLOADER_SUCCESS;
    return NPT_SUCCESS;
}
