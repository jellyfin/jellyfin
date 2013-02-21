/*****************************************************************
|
|      Messages Test Program 1
|
|      (c) 2001-2008 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/
/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
/*----------------------------------------------------------------------
|       TestServerMessageHandler
+---------------------------------------------------------------------*/
class TestServerMessageHandler
{
public:
    NPT_IMPLEMENT_DYNAMIC_CAST(TestServerMessageHandler)
    
    // destructor
    virtual ~TestServerMessageHandler() {}
    
    // methods
    virtual void OnTestCommand(NPT_MessageReceiver* /*receiver*/, int /*id*/) {}
};
NPT_DEFINE_DYNAMIC_CAST_ANCHOR(TestServerMessageHandler)

/*----------------------------------------------------------------------
|       TestServerMessage
+---------------------------------------------------------------------*/
class TestServerMessage : public NPT_Message
{
public:
    static NPT_Message::Type MessageType;
    NPT_Message::Type GetType() {
        return MessageType;
    }
    virtual NPT_Result Deliver(TestServerMessageHandler* handler) = 0;
    virtual NPT_Result Dispatch(NPT_MessageHandler* handler) {
        TestServerMessageHandler* specific = NPT_DYNAMIC_CAST(TestServerMessageHandler, handler);
        if (specific) {
            return Deliver(specific);
        } else {
            return DefaultDeliver(handler);
        }
    }
};
NPT_Message::Type TestServerMessage::MessageType = "TestServer Message";
/*----------------------------------------------------------------------
|       TestServerTestCommandMessage
+---------------------------------------------------------------------*/
class TestServerTestCommandMessage : public TestServerMessage
{
public:
    TestServerTestCommandMessage(NPT_MessageReceiver* receiver, int id) : 
        m_Receiver(receiver), m_Id(id) {}
    NPT_Result Deliver(TestServerMessageHandler* handler) {
        handler->OnTestCommand(m_Receiver, m_Id);
        return NPT_SUCCESS;
    }
     
private:
    NPT_MessageReceiver* m_Receiver;
    int                  m_Id;
};
/*----------------------------------------------------------------------
|       TestClientMessageHandler
+---------------------------------------------------------------------*/
class TestClientMessageHandler
{
public:
    NPT_IMPLEMENT_DYNAMIC_CAST(TestClientMessageHandler)
    
    // destructor
    virtual ~TestClientMessageHandler() {}
    
    // methods
    virtual void OnReply(int /*id*/) {}
};
NPT_DEFINE_DYNAMIC_CAST_ANCHOR(TestClientMessageHandler)

/*----------------------------------------------------------------------
|       TestClientMessage
+---------------------------------------------------------------------*/
class TestClientMessage : public NPT_Message
{
public:
    static NPT_Message::Type MessageType;
    NPT_Message::Type GetType() {
        return MessageType;
    }
    virtual NPT_Result Deliver(TestClientMessageHandler* handler) = 0;
    virtual NPT_Result Dispatch(NPT_MessageHandler* handler) {
        TestClientMessageHandler* specific = NPT_DYNAMIC_CAST(TestClientMessageHandler, handler);
        if (specific) {
            return Deliver(specific);
        } else {
            return DefaultDeliver(handler);
        }
    }
};
NPT_Message::Type TestClientMessage::MessageType = "TestClient Message";
/*----------------------------------------------------------------------
|       TestClientReplyMessage
+---------------------------------------------------------------------*/
class TestClientReplyMessage : public TestClientMessage
{
public:
    TestClientReplyMessage(int id) : m_Id(id) {}
    NPT_Result Deliver(TestClientMessageHandler* handler) {
        handler->OnReply(m_Id);
        return NPT_SUCCESS;
    }
private:
    int m_Id;
};
/*----------------------------------------------------------------------
|       TestServer
+---------------------------------------------------------------------*/
class TestServer : public NPT_Thread,
                   public NPT_MessageReceiver,
                   public NPT_MessageHandler,
                   public TestServerMessageHandler
{
public:
    TestServer();
    
    // message posting wrappers
    NPT_Result DoTestCommand(NPT_MessageReceiver* receiver, int id);
    // NPT_Runnable methods (from NPT_Thread)
    void Run();
    // NPT_TestServerMessageHandler methods
    void OnTestCommand(NPT_MessageReceiver* receiver, int id);
private:
    // members
    NPT_SimpleMessageQueue* m_MessageQueue;
};
/*----------------------------------------------------------------------
|       TestServer::TestServer
+---------------------------------------------------------------------*/
TestServer::TestServer()
{
    // create the message queue
    m_MessageQueue = new NPT_SimpleMessageQueue();
    
    // attach to the message queue
    SetQueue(m_MessageQueue);
    SetHandler(this);
    // start the thread
    Start();
}
/*----------------------------------------------------------------------
|       TestServer::Run
+---------------------------------------------------------------------*/
void
TestServer::Run()
{
    printf("TestServer::Run - begin\n");
    while (m_MessageQueue->PumpMessage() == NPT_SUCCESS) {};
    printf("TestServer::Run - end\n");
}
/*----------------------------------------------------------------------
|       TestServer::DoTestCommand
+---------------------------------------------------------------------*/
NPT_Result 
TestServer::DoTestCommand(NPT_MessageReceiver* receiver, int id)
{
    return this->PostMessage(new TestServerTestCommandMessage(receiver, id));
}
/*----------------------------------------------------------------------
|       TestServer::OnTestCommand
+---------------------------------------------------------------------*/
void 
TestServer::OnTestCommand(NPT_MessageReceiver* receiver, int id)
{
    printf("TestServer::OnTestCommand %d\n", id);
    receiver->PostMessage(new TestClientReplyMessage(id+10000));
}
/*----------------------------------------------------------------------
|       TestClient
+---------------------------------------------------------------------*/
class TestClient : public NPT_MessageReceiver,
                   public NPT_MessageHandler,
                   public TestClientMessageHandler
{
public:
    TestClient(TestServer* server, int id);
    
    // NPT_TestServerMessageHandler methods
    void OnReply(int id);
private:
    // members
    TestServer* m_Server;
    int         m_Id;
};
/*----------------------------------------------------------------------
|       TestClient::TestClient
+---------------------------------------------------------------------*/
TestClient::TestClient(TestServer* server, int id) :
    m_Server(server), m_Id(id)
{
    // set ourself as the message handler
    SetHandler(this);
    
    // send commands to server
    server->DoTestCommand(this, 1);
    server->DoTestCommand(this, 2);
    server->DoTestCommand(this, 3);
    server->DoTestCommand(this, 4);
}
/*----------------------------------------------------------------------
|       TestClient::OnReply
+---------------------------------------------------------------------*/
void
TestClient::OnReply(int id)
{
    printf("TestClient::OnReply (client=%d) %d\n", m_Id, id);
}
/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int argc, char** argv) 
{
    NPT_COMPILER_UNUSED(argc);
    NPT_COMPILER_UNUSED(argv);
    printf("MessagesTest2:: start\n");
    TestServer* server  = new TestServer();
    TestClient* client1 = new TestClient(server, 1);
    TestClient* client2 = new TestClient(server, 2);
    NPT_MessageQueue* queue = new NPT_SimpleMessageQueue();
    client1->SetQueue(queue);
    client2->SetQueue(queue);

    server->Wait();

    delete client1;
    delete client2;
    delete server;
    delete queue;
    printf("MessagesTest2:: end\n");
}
