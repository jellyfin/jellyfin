/*****************************************************************
|
|      Sockets Test Program 1
|
|      (c) 2001-2010 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "NptDebug.h"

#if defined(WIN32) && defined(_DEBUG)
#include <crtdbg.h>
#endif

#define CHECK(x) {                                  \
    if (!(x)) {                                     \
        printf("TEST FAILED line %d\n", __LINE__);  \
        return 1;                                   \
    }                                               \
}

NPT_IpAddress RemoteIpAddress;

/*----------------------------------------------------------------------
|   TcpServerThread
+---------------------------------------------------------------------*/
class TcpServerThread : public NPT_Thread
{
public:
    TcpServerThread() : m_Socket(NPT_SOCKET_FLAG_CANCELLABLE), m_Interrupted(false) {}
    
    void Run() {
        NPT_Console::Output("{02} waiting for connection on port 10000\n");
        NPT_SocketAddress address(NPT_IpAddress::Any, 10000);
        NPT_Result result = m_Socket.Bind(address, true);
        m_Ready.SetValue(1);
        if (NPT_FAILED(result)) {
            NPT_Console::OutputF("bind failed (%d) (%s)\n", result, NPT_ResultText(result));
            return;
        }
        NPT_Socket* client = NULL;
        result = m_Socket.WaitForNewClient(client);
        NPT_Console::Output("{02} client connected\n");
        for (;;) {
            NPT_System::Sleep(1.0);
            if (m_Interrupted) {
                NPT_Console::Output("{02} thread interrupted\n");
                break;
            }
        }
        delete client;
        NPT_Console::Output("{02} tcp server thread done\n");
    }

    NPT_TcpServerSocket m_Socket;
    NPT_SharedVariable  m_Ready;
    bool                m_Interrupted;
};

/*----------------------------------------------------------------------
|   CancellerThread
+---------------------------------------------------------------------*/
class CancellerThread : public NPT_Thread
{
public:
    CancellerThread(NPT_Socket* socket, float delay, bool shutdown) :
        m_Socket(socket),
        m_Delay(delay),
        m_Shutdown(shutdown) {
        Start();
        m_Ready.WaitUntilEquals(1);
    }
    
    void Run() {
        NPT_Console::OutputF("[XX] will cancel socket in %f\n", m_Delay);
        m_Ready.SetValue(1);
        NPT_System::Sleep(m_Delay);
        NPT_Console::OutputF("[XX] cancelling socket (shutdown=%s)\n", m_Shutdown?"yes":"no");
        m_Socket->Cancel(m_Shutdown);
    }

    NPT_Socket*        m_Socket;
    float              m_Delay;
    bool               m_Shutdown;
    NPT_SharedVariable m_Ready;
};

/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int /*argc*/, char** /*argv*/)
{
    // setup debugging
#if defined(WIN32) && defined(_DEBUG)
    int flags = _crtDbgFlag       | 
        _CRTDBG_ALLOC_MEM_DF      |
        _CRTDBG_DELAY_FREE_MEM_DF |
        _CRTDBG_CHECK_ALWAYS_DF;

    _CrtSetDbgFlag(flags);
    //AllocConsole();
    //freopen("CONOUT$", "w", stdout);
#endif 
    
    NPT_Result result;
    TcpServerThread*     server_thread = NULL;
    NPT_TcpClientSocket* tcp_client = NULL;
    NPT_TcpServerSocket* tcp_server = NULL;
    CancellerThread*     canceller  = NULL;
    
    NPT_SocketAddress address(NPT_IpAddress(127,0,0,1), 10000);
    result = RemoteIpAddress.ResolveName("www.google.com");
    CHECK(result == NPT_SUCCESS);

    NPT_Console::Output("--- test for immediate connection\n");
    NPT_Console::Output("[01] starting write server thread\n");
    server_thread = new TcpServerThread();
    server_thread->Start();
    NPT_Console::Output("[01] waiting for server to be ready...\n");
    server_thread->m_Ready.WaitUntilEquals(1);
    NPT_Console::Output("[01] server thread ready\n");
    NPT_Console::Output("[01] waiting a while...\n");
    NPT_System::Sleep(3.0);
    tcp_client = new NPT_TcpClientSocket();
    NPT_Console::Output("[01] connection to 127.0.0.1:10000\n");
    result = tcp_client->Connect(address);
    NPT_Console::OutputF("[01] connect returns %d : %s\n", result, NPT_ResultText(result));
    CHECK(result == NPT_SUCCESS);
    delete tcp_client;
    NPT_Console::Output("[01] terminating server\n");
    server_thread->m_Interrupted = true;
    server_thread->Wait();
    delete server_thread;
    
    NPT_Console::Output("\n--- test for refused local connection\n");
    address.SetPort(89);
    tcp_client = new NPT_TcpClientSocket();
    NPT_Console::Output("[01] connecting to 127.0.0.1:89\n");
    result = tcp_client->Connect(address);
    NPT_Console::OutputF("[01] connect returns %d : %s\n", result, NPT_ResultText(result));
    CHECK(result == NPT_ERROR_CONNECTION_REFUSED);
    delete tcp_client;

    /*NPT_Console::Output("\n--- test for refused remote connection\n");
    address.SetIpAddress(RemoteIpAddress);
    address.SetPort(81);
    tcp_client = new NPT_TcpClientSocket();
    NPT_Console::Output("[01] connecting to www.google.com:81\n");
    result = tcp_client->Connect(address);
    NPT_Console::OutputF("[01] connect returns %d : %s\n", result, NPT_ResultText(result));
    CHECK(result == NPT_ERROR_CONNECTION_REFUSED);
    delete tcp_client;*/

    NPT_Console::Output("\n--- test for connection timeout\n");
    address.SetIpAddress(NPT_IpAddress(1,1,1,1));
    NPT_Console::Output("[01] connecting to 1.1.1.1:89\n");
    tcp_client = new NPT_TcpClientSocket();
    result = tcp_client->Connect(address, 3000);
    NPT_Console::OutputF("[01] connect returns %d : %s\n", result, NPT_ResultText(result));
    CHECK(result == NPT_ERROR_TIMEOUT);
    delete tcp_client;
    
    NPT_Console::Output("\n--- test for remote connection\n");
    address.SetIpAddress(RemoteIpAddress);
    address.SetPort(80);
    NPT_Console::Output("[01] connecting to www.google.com:80\n");
    tcp_client = new NPT_TcpClientSocket();
    result = tcp_client->Connect(address);
    NPT_Console::OutputF("[01] connect returns %d : %s\n", result, NPT_ResultText(result));
    CHECK(result == NPT_SUCCESS);
    delete tcp_client;
    
    for (int i=0; i<2; i++) {
        NPT_Console::OutputF("\n--- test for cancelled connection, shutdown=%d\n", i);
        address.SetIpAddress(NPT_IpAddress(1,1,1,1));
        address.SetPort(89);
        NPT_Console::Output("[01] connecting to 1.1.1.1:89\n");
        tcp_client = new NPT_TcpClientSocket(NPT_SOCKET_FLAG_CANCELLABLE);
        canceller = new CancellerThread(tcp_client, 3.0f, i==1);
        result = tcp_client->Connect(address);
        NPT_Console::OutputF("[01] connect returns %d : %s\n", result, NPT_ResultText(result));
        CHECK(result == NPT_ERROR_CANCELLED);
        canceller->Wait();
        delete canceller;
        delete tcp_client;
    }
    
    for (int i=0; i<2; i++) {
        NPT_Console::OutputF("\n--- testing read cancellation, shutdown=%d\n", i);
        address.SetIpAddress(RemoteIpAddress);
        address.SetPort(80);
        NPT_Console::Output("[01] connecting to www.google.com:80\n");
        tcp_client = new NPT_TcpClientSocket(NPT_SOCKET_FLAG_CANCELLABLE);
        result = tcp_client->Connect(address);
        NPT_Console::OutputF("[01] connect returns %d : %s\n", result, NPT_ResultText(result));
        CHECK(result == NPT_SUCCESS);    
        canceller = new CancellerThread(tcp_client, 3.0f, i==1);
        NPT_InputStreamReference input;
        tcp_client->GetInputStream(input);
        unsigned char buffer[4096];
        NPT_SetMemory(buffer, 0, sizeof(buffer));
        result = input->Read(buffer, 4096);
        NPT_Console::OutputF("{00} read returned %d (%s)\n", result, NPT_ResultText(result));
        CHECK(result == NPT_ERROR_CANCELLED);
        delete tcp_client;
        canceller->Wait();
        delete canceller;
    }
    
    for (int i=0; i<2; i++) {
        NPT_Console::OutputF("\n--- testing write cancellation, shutdown=%d\n", i);
        server_thread = new TcpServerThread();
        server_thread->Start();
        NPT_Console::Output("[01] waiting for server to be ready...\n");
        server_thread->m_Ready.WaitUntilEquals(1);
        NPT_Console::Output("[01] server thread ready\n");
        NPT_Console::Output("[01] waiting a while...\n");
        NPT_System::Sleep(3.0);
        address.SetIpAddress(NPT_IpAddress(127,0,0,1));
        address.SetPort(10000);
        NPT_Console::Output("[01] connecting to localhost:10000\n");
        tcp_client = new NPT_TcpClientSocket(NPT_SOCKET_FLAG_CANCELLABLE);
        result = tcp_client->Connect(address);
        NPT_Console::OutputF("[01] connect returns %d : %s\n", result, NPT_ResultText(result));
        CHECK(result == NPT_SUCCESS);    
        canceller = new CancellerThread(tcp_client, 3.0f, i==1);
        NPT_OutputStreamReference output;
        tcp_client->GetOutputStream(output);
        NPT_Size total_written = 0;
        unsigned char buffer[4096];
        NPT_SetMemory(buffer, 0, sizeof(buffer));
        do {
            NPT_Size bytes_written = 0;
            result = output->Write(buffer, 4096, &bytes_written);
            if (NPT_SUCCEEDED(result)) {
                total_written += bytes_written;
            }
        } while (NPT_SUCCEEDED(result));
        output = NULL;
        NPT_Console::OutputF("{01} write returned %d (%s)\n", result, NPT_ResultText(result));
        NPT_Console::OutputF("{01} wrote %d bytes total\n", total_written);
        CHECK(result == NPT_ERROR_CANCELLED);
        delete tcp_client;
        canceller->Wait();
        delete canceller;
        server_thread->m_Interrupted = true;
        server_thread->Wait();
        delete server_thread;
    }

    for (int i=0; i<2; i++) {
        NPT_Console::OutputF("\n--- testing accept cancellation, shutdown=%d\n", i);
        NPT_Console::Output("{03} waiting for connection on port 10000\n");
        address.SetIpAddress(NPT_IpAddress(127,0,0,1));
        address.SetPort(10000);
        tcp_server = new NPT_TcpServerSocket(NPT_SOCKET_FLAG_CANCELLABLE);
        result = tcp_server->Bind(address, true);
        CHECK(result == NPT_SUCCESS);
        canceller = new CancellerThread(tcp_server, 3.0f, i==1);
        NPT_Socket* new_client = NULL;
        result = tcp_server->WaitForNewClient(new_client);
        NPT_Console::OutputF("{03} WaitForNewClient returned %d (%s)\n", result, NPT_ResultText(result));
        CHECK(result == NPT_ERROR_CANCELLED);
        canceller->Wait();
        delete canceller;
        delete tcp_server;
    }

    NPT_Console::Output("------------\n");
    NPT_Console::Output("bye bye\n");

#if defined(WIN32) && defined(_DEBUG)
    _CrtDumpMemoryLeaks();
#endif

    return 0;
}
