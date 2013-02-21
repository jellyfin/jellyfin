/*****************************************************************
|
|      HTTP Client Test Program 2
|
|      (c) 2001-2011 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "NptDebug.h"

#define LOG_FORMAT "%30s,%3d,%8d, %8d, %8d, [%30s], %s\n"

/*----------------------------------------------------------------------
|       TestHttpGet
+---------------------------------------------------------------------*/
static void 
TestHttpGet(const char* arg, bool use_http_1_1, int verbosity)
{
    NPT_HttpUrl url(arg);
    NPT_HttpRequest request(url, NPT_HTTP_METHOD_GET);
    NPT_HttpClient client;
    NPT_HttpResponse* response;

    if (!url.IsValid()) return;
    if (use_http_1_1) request.SetProtocol(NPT_HTTP_PROTOCOL_1_1);

    NPT_TimeStamp before;
    NPT_System::GetCurrentTimeStamp(before);
    NPT_Result result = client.SendRequest(request, response);
    NPT_TimeStamp after;
    NPT_System::GetCurrentTimeStamp(after);
    NPT_UInt64 elapsed = (after-before).ToMillis();
    if (NPT_FAILED(result)) {
        if (verbosity >= 1) printf(LOG_FORMAT, NPT_ResultText(result), 0, 0, 0, (int)elapsed, "", arg);
        return;
    } 
    NPT_DataBuffer payload;
    result = response->GetEntity()->Load(payload);
    int loaded = -1;
    if (NPT_SUCCEEDED(result))  {
        loaded = (int)payload.GetDataSize();
    }
    const NPT_String* server = response->GetHeaders().GetHeaderValue("Server");
    if (verbosity >= 1) printf(LOG_FORMAT, "NPT_SUCCESS", response->GetStatusCode(), loaded, (int)response->GetEntity()->GetContentLength(), (int)elapsed, server?server->GetChars():"", arg);

    delete response;
}

/*----------------------------------------------------------------------
|       TestHttpPost
+---------------------------------------------------------------------*/
static void 
TestHttpPost(const char* arg, bool use_http_1_1, unsigned int verbosity)
{
    NPT_HttpUrl url(arg);
    NPT_HttpRequest request(url, NPT_HTTP_METHOD_POST);
    NPT_HttpClient client;
    NPT_HttpResponse* response;

    if (!url.IsValid()) return;
    if (use_http_1_1) request.SetProtocol(NPT_HTTP_PROTOCOL_1_1);

    NPT_HttpEntity* entity = new NPT_HttpEntity();
    entity->SetInputStream("blabla");
    request.SetEntity(entity);
    request.GetHeaders().SetHeader("Expect", "100-continue");
    
    NPT_TimeStamp before;
    NPT_System::GetCurrentTimeStamp(before);
    NPT_Result result = client.SendRequest(request, response);
    NPT_TimeStamp after;
    NPT_System::GetCurrentTimeStamp(after);
    NPT_UInt64 elapsed = (after-before).ToMillis();
    if (NPT_FAILED(result)) {
        if (verbosity >= 1) printf(LOG_FORMAT, NPT_ResultText(result), 0, 0, 0, (int)elapsed, "", arg);
        return;
    } 
    NPT_DataBuffer payload;
    result = response->GetEntity()->Load(payload);
    int loaded = -1;
    if (NPT_SUCCEEDED(result))  {
        loaded = (int)payload.GetDataSize();
    }
    const NPT_String* server = response->GetHeaders().GetHeaderValue("Server");
    if (verbosity >= 1) printf(LOG_FORMAT, "NPT_SUCCESS", response->GetStatusCode(), loaded, (int)response->GetEntity()->GetContentLength(), (int)elapsed, server?server->GetChars():"", arg);

    delete response;
}

/*----------------------------------------------------------------------
|   ClientThread
+---------------------------------------------------------------------*/
class ClientThread : public NPT_Thread
{
public:
    ClientThread(const char*  name,
                 const char*  playlist,
                 bool         use_http_1_1,
                 unsigned int loops,
                 bool         random,
                 bool         post,
                 unsigned int sleep,
                 unsigned int verbosity) :
        m_Name(name),
        m_Playlist(playlist),
        m_UseHttp_1_1(use_http_1_1),
        m_Loops(loops),
        m_Random(random),
        m_Post(post),
        m_Sleep(sleep),
        m_Verbosity(verbosity) {}
        
    virtual void Run();
    
private:
    NPT_String   m_Name;
    NPT_String   m_Playlist;
    bool         m_UseHttp_1_1;
    unsigned int m_Loops;
    bool         m_Random;
    bool         m_Post;
    unsigned int m_Sleep;
    unsigned int m_Verbosity;
};

/*----------------------------------------------------------------------
|   ClientThread::Run
+---------------------------------------------------------------------*/
void
ClientThread::Run()
{
    NPT_DataBuffer list_buffer;
    NPT_String list_string;
    if (NPT_File::Exists(m_Playlist)) {
        NPT_File::Load(m_Playlist, list_buffer);
        list_string.Assign((const char*)list_buffer.GetData(), list_buffer.GetDataSize());
    } else {
        list_string = m_Playlist;
    }
    NPT_List<NPT_String> urls = list_string.Split("\n");
    if (m_Verbosity >= 2) printf("urls: %d\n", urls.GetItemCount());
    if (m_Verbosity >= 2) printf("loops: %d, random: %s, sleep: %d ms\n", m_Loops, m_Random?"true":"false", m_Sleep);
    for (unsigned int i=0; i<m_Loops; i++) {
        NPT_TimeStamp before;
        NPT_System::GetCurrentTimeStamp(before);
        for (unsigned int j=0; j<urls.GetItemCount(); j++) {
            unsigned int choice = j;
            if (m_Random) {
                choice = NPT_System::GetRandomInteger()%urls.GetItemCount();
            }
            if (m_Post) {
                TestHttpPost((*urls.GetItem(choice)).GetChars(), m_UseHttp_1_1, m_Verbosity);
            } else {
                TestHttpGet((*urls.GetItem(choice)).GetChars(), m_UseHttp_1_1, m_Verbosity);
            }

            if (m_Sleep) {
                NPT_System::Sleep(NPT_TimeStamp(((float)m_Sleep)/1000.0f));
            }
        }
        NPT_TimeStamp after;
        NPT_System::GetCurrentTimeStamp(after);
        float elapsed = (float)(after-before);
        if (m_Verbosity >= 1) printf("%s [%04d] TOTAL time elapsed = %d ms\n", m_Name.GetChars(), i, (int)(elapsed*1000.0));
    }
}

/*----------------------------------------------------------------------
|   main
+---------------------------------------------------------------------*/
int
main(int argc, char** argv)
{
    // parse args
    --argc; ++argv;
    bool         use_http_1_1 = false;
    unsigned int loops        = 1;
    bool         random       = false;
    bool         post         = false;
    unsigned int sleep        = 0;
    unsigned int threads      = 1;
    unsigned int verbosity    = 1;
    while (*argv) {
        if (NPT_StringsEqual(*argv, "--http-1-1")) {
            use_http_1_1 = true;
        } else if (NPT_StringsEqual(*argv, "--loops")) {
            NPT_ParseInteger(*++argv, loops);
        } else if (NPT_StringsEqual(*argv, "--post")) {
            post = true;
        } else if (NPT_StringsEqual(*argv, "--random")) {
            random = true;
        } else if (NPT_StringsEqual(*argv, "--sleep")) {
            NPT_ParseInteger(*++argv, sleep);
        } else if (NPT_StringsEqual(*argv, "--verbosity")) {
            NPT_ParseInteger(*++argv, verbosity);
        } else if (NPT_StringsEqual(*argv, "--threads")) {
            NPT_ParseInteger(*++argv, threads);
        } else {
            break;
        }
        ++argv;
    }
    if (*argv == NULL) {
        fprintf(stderr, "ERROR: missing URL or list filename\n");
        return 1;
    }
     
    NPT_Array<ClientThread*> cthreads;
    cthreads.Resize(threads);
    for (unsigned int i=0; i<threads; i++) {
        NPT_String name = "THREAD ";
        name += NPT_String::FromInteger(i);
        ClientThread* thread = new ClientThread(name, *argv, use_http_1_1, loops, random, post, sleep, verbosity);
        cthreads[i] = thread;
        thread->Start();
    }

    for (unsigned int i=0; i<threads; i++) {
        cthreads[i]->Wait();
        delete cthreads[i];
    }
    
    return 0;
}
