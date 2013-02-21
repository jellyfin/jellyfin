/*****************************************************************
|
|   Platinum - HTTP tests
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
#include "NptUtils.h"
#include "Neptune.h"
#include "NptLogging.h"
#include "PltTaskManager.h"
#include "PltHttpServer.h"
#include "PltDownloader.h"
#include "PltRingBufferStream.h"

#include <stdio.h>
#include <string.h>
#include <stdlib.h>

NPT_SET_LOCAL_LOGGER("platinum.core.http.test")

//#define TEST1
//#define TEST2
//#define TEST3
//#define TEST4
#define TEST5

/*----------------------------------------------------------------------
|   globals
+---------------------------------------------------------------------*/
struct Options {
    NPT_UInt32  port;
    NPT_String  path;
} Options;

/*----------------------------------------------------------------------
|   PLT_HttpCustomRequestHandler
+---------------------------------------------------------------------*/
class PLT_HttpCustomRequestHandler : public NPT_HttpRequestHandler
{
public:
    // constructors
    PLT_HttpCustomRequestHandler(NPT_InputStreamReference& body, 
                                 const char*               mime_type,
                                 bool                      update_content_length = false) :
        m_Body(body),
        m_MimeType(mime_type),
        m_UpdateContentLength(update_content_length) {}

    // NPT_HttpRequetsHandler methods
    virtual NPT_Result SetupResponse(NPT_HttpRequest&              request, 
                                     const NPT_HttpRequestContext& context,
                                     NPT_HttpResponse&             response) {
        NPT_COMPILER_UNUSED(request);
        NPT_COMPILER_UNUSED(context);

        NPT_HttpEntity* entity = response.GetEntity();
        if (entity == NULL) return NPT_ERROR_INVALID_STATE;

        entity->SetContentType(m_MimeType);
        entity->SetInputStream(m_Body, m_UpdateContentLength);

        return NPT_SUCCESS;
    }

private:
    NPT_InputStreamReference m_Body;
    NPT_String               m_MimeType;
    bool                     m_UpdateContentLength;
};


#ifdef TEST1
/*----------------------------------------------------------------------
|   Test1
+---------------------------------------------------------------------*/
static bool
Test1(PLT_TaskManager* task_manager, NPT_HttpUrl url, NPT_Size& size)
{
    NPT_LOG_INFO("########### TEST 1 ######################");

    NPT_MemoryStreamReference memory_stream(new NPT_MemoryStream());
    NPT_OutputStreamReference output_stream(memory_stream);
    PLT_Downloader downloader(task_manager, url, output_stream);
    downloader.Start();

    while (1) {
        switch(downloader.GetState()) {
            case PLT_DOWNLOADER_SUCCESS: {
                size = memory_stream->GetDataSize();
                return true;
            }

            case PLT_DOWNLOADER_ERROR:
                return false;

            default:
                NPT_System::Sleep(NPT_TimeInterval(.1f));
                break;
        }
    };

    return false;
}
#endif

#ifdef TEST2
/*----------------------------------------------------------------------
|   DumpBody
+---------------------------------------------------------------------*/
static NPT_Result 
ReadBody(PLT_Downloader& downloader, NPT_InputStreamReference& stream, NPT_Size& size)
{
    NPT_LargeSize avail;
    char buffer[2048];
    NPT_Result ret = NPT_ERROR_WOULD_BLOCK;

    /* reset output param first */
    size = 0;

    /*
       we test for availability first to avoid
       getting stuck in Read forever in case blocking is true
       and the download is done writing to the stream
    */
    NPT_CHECK(stream->GetAvailable(avail));

    if (avail) {
         ret = stream->Read(buffer, 2048, &size);
         NPT_LOG_FINER_2("Read %d bytes (result = %d)\n", size, ret);
         return ret;
     } else {
         Plt_DowloaderState state = downloader.GetState();
         switch (state) {
             case PLT_DOWNLOADER_ERROR:
                 return NPT_FAILURE;

             case PLT_DOWNLOADER_SUCCESS:
                 /* no more data expected */
                 return NPT_ERROR_EOS;

             default:
                 NPT_System::Sleep(NPT_TimeInterval(.1f));
                 break;
         }
     }
 
     return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   Test2
+---------------------------------------------------------------------*/
static bool
Test2(PLT_TaskManager* task_manager, NPT_HttpUrl url, NPT_Size& size)
{
    NPT_LOG_INFO("########### TEST 2 ######################");

    /* reset output param first */
    size = 0;

    PLT_RingBufferStreamReference ringbuffer_stream(new PLT_RingBufferStream());
    NPT_OutputStreamReference output_stream(ringbuffer_stream);
    NPT_InputStreamReference  input_stream(ringbuffer_stream);
    PLT_Downloader downloader(task_manager, url, output_stream);
    downloader.Start();

    while (1) {
        switch(downloader.GetState()) {
            case PLT_DOWNLOADER_SUCCESS:
                ringbuffer_stream->SetEOS();
                /* fallthrough */

            case PLT_DOWNLOADER_DOWNLOADING: {
                    NPT_Size bytes_read;
                    NPT_Result res = ReadBody(downloader, input_stream, bytes_read);
                    if (NPT_FAILED(res)) {
                        return (res==NPT_ERROR_EOS)?true:false;
                    }
                    size += bytes_read;
                }
                break;

            case PLT_DOWNLOADER_ERROR:
                return false;

            default:
                NPT_System::Sleep(NPT_TimeInterval(.1f));
                break;
        }
    };

    return false;
}
#endif


#ifdef TEST3
class RingBufferWriterTask : public PLT_ThreadTask 
{
public:
    RingBufferWriterTask(PLT_RingBufferStreamReference& ringbuffer_stream) 
    : m_RingBufferStream(ringbuffer_stream) {}
    
    // PLT_ThreadTask methods
    virtual void DoRun() { 
        char buffer[32768];
        m_RingBufferStream->WriteFully(buffer, 32768);
        
        /* mark as done */
        m_RingBufferStream->SetEOS();
    }
    
private:
    PLT_RingBufferStreamReference m_RingBufferStream;
};

/*----------------------------------------------------------------------
|   Test3
+---------------------------------------------------------------------*/
static bool
Test3(PLT_TaskManager* task_manager, NPT_HttpUrl url, PLT_RingBufferStreamReference& ringbuffer_stream, NPT_Size& size)
{
    NPT_LOG_INFO("########### TEST 3 ######################");

    /* reset output param first */
    size = 0;

    NPT_MemoryStreamReference memory_stream(new NPT_MemoryStream());
    NPT_OutputStreamReference output_stream(memory_stream);
    PLT_Downloader downloader(task_manager, url, output_stream);
    downloader.Start();

    /* asynchronously write onto ring buffer stream */
    task_manager->StartTask(new RingBufferWriterTask(ringbuffer_stream));

    /* start pulling data */
    while (1) {
        switch(downloader.GetState()) {
            case PLT_DOWNLOADER_SUCCESS:
                size = memory_stream->GetDataSize();
                return true;

            case PLT_DOWNLOADER_ERROR:
                return false;

            default:
                NPT_System::Sleep(NPT_TimeInterval(.1f));
                break;
        }
    };

    return false;
}
#endif

#ifdef TEST4
class ClientStuckTask : public PLT_ThreadTask 
{
public:
    ClientStuckTask(NPT_HttpUrl& url, NPT_HttpClient& client) 
    : m_Url(url), m_Client(client) {}
    
    // PLT_ThreadTask methods
    virtual void DoRun() { 
        NPT_HttpRequest request(m_Url, NPT_HTTP_METHOD_GET);
        NPT_HttpResponse* response = NULL;
        m_Client.SendRequest(request, response);
    }
    
    virtual void DoAbort()   {
        m_Client.Abort();
    }
    
private:
    NPT_HttpUrl     m_Url;
    NPT_HttpClient& m_Client;
    bool            m_Aborted;
};
/*----------------------------------------------------------------------
|   Test4
+---------------------------------------------------------------------*/
static bool
Test4(PLT_TaskManager* task_manager, NPT_HttpUrl url, NPT_TimeInterval wait_before_kill)
{
    NPT_LOG_INFO("########### TEST 4 ######################");
        
    NPT_HttpClient client;
    
    /* start task to asynchronously fetch url */
    ClientStuckTask* task = new ClientStuckTask(url, client);
    task_manager->StartTask(task, NULL, false);
    
    /* wait a bit and abort client */
    NPT_System::Sleep(wait_before_kill);
    
    task->Kill();
    return true;
}
#endif

#ifdef TEST5
/*----------------------------------------------------------------------
|   Test5
+---------------------------------------------------------------------*/
static bool
Test5(NPT_HttpUrl url)
{
    NPT_LOG_INFO("########### TEST 5 ######################");
    
    NPT_HttpClient client;
    
    // first request
    NPT_HttpRequest request(url, NPT_HTTP_METHOD_POST, NPT_HTTP_PROTOCOL_1_1);
    NPT_HttpEntity* request_entity = new NPT_HttpEntity();
    request_entity->SetInputStream("Testing");
    request.SetEntity(request_entity);
    
    NPT_HttpResponse* response = NULL;
    client.SendRequest(request, response);
    NPT_HttpEntity* entity = NULL;
    if (response && (entity = response->GetEntity())) {
        NPT_DataBuffer buffer;
        if (NPT_FAILED(entity->Load(buffer))) return false;
    }
    
    // try again
    delete response;
    response = NULL;
    request_entity = new NPT_HttpEntity();
    request_entity->SetInputStream("Testing2");
    request.SetEntity(request_entity);
    client.SendRequest(request, response);
    entity = NULL;
    if (response && (entity = response->GetEntity())) {
        NPT_DataBuffer buffer;
        if (NPT_FAILED(entity->Load(buffer))) return false;
    }
    
    return true;
}
#endif

/*----------------------------------------------------------------------
|   PrintUsageAndExit
+---------------------------------------------------------------------*/
static void
PrintUsageAndExit(char** args)
{
    fprintf(stderr, "usage: %s [-p <port>] [-f <filepath>]\n", args[0]);
    fprintf(stderr, "-p : optional server port\n");
    fprintf(stderr, "-f : optional local filepath to serve\n");
    exit(1);
}

/*----------------------------------------------------------------------
|   ParseCommandLine
+---------------------------------------------------------------------*/
static void
ParseCommandLine(char** args)
{
    const char* arg;
    char**      tmp = args+1;

    /* default values */
    Options.port = 0;
    Options.path = "";

    while ((arg = *tmp++)) {
        if (Options.port == 0 && !strcmp(arg, "-p")) {
            NPT_UInt32 port;
            if (NPT_FAILED(NPT_ParseInteger32(*tmp++, port, false))) {
                fprintf(stderr, "ERROR: invalid port\n");
                exit(1);
            }
            Options.port = port;
        } else if (Options.path.IsEmpty() && !strcmp(arg, "-f")) {
            Options.path = *tmp++;
        } else {
            fprintf(stderr, "ERROR: too many arguments\n");
            PrintUsageAndExit(args);
        }
    }
}

/*----------------------------------------------------------------------
|   main
+---------------------------------------------------------------------*/
int
main(int argc, char** argv)
{
    NPT_COMPILER_UNUSED(argc);
    
    NPT_HttpRequestHandler* handler;
    NPT_Reference<NPT_DataBuffer> buffer;
    bool result;

    /* parse command line */
    ParseCommandLine(argv);

    /* create http server */
    PLT_HttpServer http_server(Options.port?Options.port:8089);
    NPT_String url;

    if (!Options.path.IsEmpty()) {
        /* extract folder path */
        int index1 = Options.path.ReverseFind('\\');
        int index2 = Options.path.ReverseFind('/');
        if (index1 <= 0 && index2 <=0) {
            fprintf(stderr, "ERROR: invalid path\n");
            exit(1);
        }

        NPT_FileInfo info;
        NPT_CHECK_SEVERE(NPT_File::GetInfo(Options.path, &info));

        /* add file request handler */
        handler = new NPT_HttpFileRequestHandler(
            Options.path.Left(index1>index2?index1:index2), 
            "/");
        http_server.AddRequestHandler(handler, "/", true);

        /* build url */
        url = "/" + Options.path.SubString((index1>index2?index1:index2)+1);
    } else {
        /* create random garbage data */
        buffer = new NPT_DataBuffer(32768);
        buffer->SetDataSize(32768);

        /* add static handler */
        handler = new NPT_HttpStaticRequestHandler(buffer->GetData(),
            buffer->GetDataSize(),
            "application/octet-stream");
        http_server.AddRequestHandler(handler, "/test");

        /* build url */
        url = "/test";
    }

    /* add custom handler */
    PLT_RingBufferStreamReference ringbuffer_stream(new PLT_RingBufferStream());
    NPT_InputStreamReference stream(ringbuffer_stream);
    NPT_HttpRequestHandler* custom_handler = new PLT_HttpCustomRequestHandler(stream, "text/xml");
    http_server.AddRequestHandler(custom_handler, "/custom");
    
    /* start server */
    NPT_CHECK_SEVERE(http_server.Start());

    /* a task manager for the tests downloader */
    PLT_TaskManager task_manager;

    /* small delay to let the server start */
    NPT_System::Sleep(NPT_TimeInterval(1.f));
    
    /* execute tests */
    NPT_Size size;
    NPT_COMPILER_UNUSED(size);
    
#ifdef TEST1
    result = Test1(&task_manager, NPT_HttpUrl("127.0.0.1", http_server.GetPort(), url), size);
    if (!result) return -1;
#endif
    
#ifdef TEST2
    result = Test2(&task_manager, NPT_HttpUrl("127.0.0.1", http_server.GetPort(), url), size);
    if (!result) return -1;
#endif
    
#ifdef TEST3
    result = Test3(&task_manager, NPT_HttpUrl("127.0.0.1", http_server.GetPort(), "/custom"), ringbuffer_stream, size);
    if (!result) return -1;
#endif
    
#ifdef TEST4
    result = Test4(&task_manager, NPT_HttpUrl("127.0.0.1", http_server.GetPort(), "/custom"), NPT_TimeInterval(.1f));
    if (!result) return -1;
    
    result = Test4(&task_manager, NPT_HttpUrl("127.0.0.1", http_server.GetPort(), "/custom"), NPT_TimeInterval(1.f));
    if (!result) return -1;
    
    result = Test4(&task_manager, NPT_HttpUrl("127.0.0.1", http_server.GetPort(), "/custom"), NPT_TimeInterval(2.f));
    if (!result) return -1;
#endif
    
#ifdef TEST5
    result = Test5(NPT_HttpUrl("127.0.0.1", http_server.GetPort(), "/test"));
    if (!result) return -1;
#endif
    
    NPT_System::Sleep(NPT_TimeInterval(1.f));
    
    // abort server tasks that are waiting on ring buffer stream
    ringbuffer_stream->Abort();
    
    http_server.Stop();
    
    return 0;
}
