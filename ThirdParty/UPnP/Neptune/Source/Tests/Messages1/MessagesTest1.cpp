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
#include <stdio.h>
#include "Neptune.h"

/*----------------------------------------------------------------------
|       FooServerMessageHandler
+---------------------------------------------------------------------*/
class FooServerMessageHandler
{
public:
    NPT_IMPLEMENT_DYNAMIC_CAST(FooServerMessageHandler)
    
    // destructor
    virtual ~FooServerMessageHandler() {}
    
    // methods
    virtual void OnBarCmd1(NPT_MessageReceiver* /*receiver*/, int /*info*/) {}
    virtual void OnBarCmd2(NPT_MessageReceiver* /*receiver*/, 
                           int /*info1*/, int /*info2*/) { }
    virtual void OnBarCmd3(NPT_MessageReceiver* /*receiver*/, 
                           int /*info1*/, int /*info2*/, int /*info3*/) {}
};
NPT_DEFINE_DYNAMIC_CAST_ANCHOR(FooServerMessageHandler)

/*----------------------------------------------------------------------
|       FooServerMessage
+---------------------------------------------------------------------*/
class FooServerMessage : public NPT_Message
{
public:
    static NPT_Message::Type MessageType;
    NPT_Message::Type GetType() {
        return MessageType;
    }
    virtual NPT_Result Deliver(FooServerMessageHandler* handler) = 0;
    virtual NPT_Result Dispatch(NPT_MessageHandler* handler) {
        FooServerMessageHandler* specific = NPT_DYNAMIC_CAST(FooServerMessageHandler, handler);
        if (specific) {
            return Deliver(specific);
        } else {
            return DefaultDeliver(handler);
        }
    }
};

NPT_Message::Type FooServerMessage::MessageType = "FooServer Message";

/*----------------------------------------------------------------------
|       FooServerBarCmd1Message
+---------------------------------------------------------------------*/
class FooServerBarCmd1Message : public FooServerMessage
{
public:
    FooServerBarCmd1Message(NPT_MessageReceiver* receiver, int info) : 
        m_Receiver(receiver), m_Info(info) {}
    NPT_Result Deliver(FooServerMessageHandler* handler) {
        handler->OnBarCmd1(m_Receiver, m_Info);
        return NPT_SUCCESS;
    }
     
private:
    NPT_MessageReceiver* m_Receiver;
    int                  m_Info;
};

/*----------------------------------------------------------------------
|       FooServerBarCmd2Message
+---------------------------------------------------------------------*/
class FooServerBarCmd2Message : public FooServerMessage
{
public:
    FooServerBarCmd2Message(NPT_MessageReceiver* receiver, 
                            int info1, int info2) : 
        m_Receiver(receiver), m_Info1(info1), m_Info2(info2) {}
    NPT_Result Deliver(FooServerMessageHandler* handler) {
        handler->OnBarCmd2(m_Receiver, m_Info1, m_Info2);
        return NPT_SUCCESS;
    }
     
private:
    NPT_MessageReceiver* m_Receiver;
    int                  m_Info1;
    int                  m_Info2;
};

/*----------------------------------------------------------------------
|       FooServerBarCmd3Message
+---------------------------------------------------------------------*/
class FooServerBarCmd3Message : public FooServerMessage
{
public:
    FooServerBarCmd3Message(NPT_MessageReceiver* receiver, 
                            int info1, int info2, int info3) : 
        m_Receiver(receiver), m_Info1(info1), m_Info2(info2), m_Info3(info3) {}
    NPT_Result Deliver(FooServerMessageHandler* handler) {
        handler->OnBarCmd3(m_Receiver, m_Info1, m_Info2, m_Info3);
        return NPT_SUCCESS;
    }
     
private:
    NPT_MessageReceiver* m_Receiver;
    int                  m_Info1;
    int                  m_Info2;
    int                  m_Info3;
};

/*----------------------------------------------------------------------
|       FooServerBarCmd4Message
+---------------------------------------------------------------------*/
class FooServerBarCmd4Message : public NPT_Message
{
public:
    static NPT_Message::Type MessageType;
    NPT_Message::Type GetType() {
        return MessageType;
    }
    FooServerBarCmd4Message() {}
};
NPT_Message::Type FooServerBarCmd4Message::MessageType = "FooServerBarCmd4 Message";

/*----------------------------------------------------------------------
|       FooClientMessageHandler
+---------------------------------------------------------------------*/
class FooClientMessageHandler
{
public:
    NPT_IMPLEMENT_DYNAMIC_CAST(FooClientMessageHandler)

    // destructor
    virtual ~FooClientMessageHandler() {}
    
    // methods
    virtual void OnBarNotification1(int /*info*/) {}
    virtual void OnBarNotification2(int /*info1*/, int /*info2*/) {}
};
NPT_DEFINE_DYNAMIC_CAST_ANCHOR(FooClientMessageHandler)

/*----------------------------------------------------------------------
|       FooClientMessage
+---------------------------------------------------------------------*/
class FooClientMessage : public NPT_Message
{
public:
    static NPT_Message::Type MessageType;
    NPT_Message::Type GetType() {
        return MessageType;
    }
    virtual NPT_Result Deliver(FooClientMessageHandler* handler) = 0;
    virtual NPT_Result Dispatch(NPT_MessageHandler* handler) {
        FooClientMessageHandler* specific = NPT_DYNAMIC_CAST(FooClientMessageHandler, handler);
        if (specific) {
            return Deliver(specific);
        } else {
            return DefaultDeliver(handler);
        }
    }
};
NPT_Message::Type FooClientMessage::MessageType = "FooClient Message";

/*----------------------------------------------------------------------
|       FooClientBarNotification1Message
+---------------------------------------------------------------------*/
class FooClientBarNotification1Message : public FooClientMessage
{
public:
    FooClientBarNotification1Message(int info) : m_Info(info) {}
    NPT_Result Deliver(FooClientMessageHandler* handler) {
        handler->OnBarNotification1(m_Info);
        return NPT_SUCCESS;
    }

private:
    int m_Info;
};

/*----------------------------------------------------------------------
|       FooServer
+---------------------------------------------------------------------*/
class FooServer : public NPT_Thread,
                  public NPT_MessageReceiver,
                  public NPT_MessageHandler,
                  public FooServerMessageHandler
{
public:
    FooServer();
    
    // message posting wrappers
    NPT_Result DoBarCmd1(NPT_MessageReceiver* receiver, int info);
    NPT_Result DoBarCmd2(NPT_MessageReceiver* receiver, int info1, int info2);
    NPT_Result DoBarCmd3(NPT_MessageReceiver* receiver, 
                        int info1, int info2, int info3);
    NPT_Result DoBarCmd4();

    // NPT_Runnable methods (from NPT_Thread)
    void Run();

    // NPT_MessageHandler methods
    void OnMessage(NPT_Message* message);
    NPT_Result HandleMessage(NPT_Message* message);
    
    // NPT_FooServerMessageHandler methods
    void OnBarCmd1(NPT_MessageReceiver* receiver, int info);
    void OnBarCmd2(NPT_MessageReceiver* receiver, int info1, int info2);

private:
    // members
    NPT_SimpleMessageQueue* m_MessageQueue;
};

/*----------------------------------------------------------------------
|       FooServer::FooServer
+---------------------------------------------------------------------*/
FooServer::FooServer()
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
|       FooServer::Run
+---------------------------------------------------------------------*/
void
FooServer::Run()
{
    printf("FooServer::Run - begin\n");
    while (m_MessageQueue->PumpMessage() == NPT_SUCCESS) {};
    printf("FooServer::Run - end\n");
}

/*----------------------------------------------------------------------
|       FooServer::HandleMessage
+---------------------------------------------------------------------*/
NPT_Result
FooServer::HandleMessage(NPT_Message* message)
{
    // a handler typically does not implement this method unless it
    // needs to catch all messages before they are dispatched
    printf("FooServer::HandleMessage (%s)\n", message->GetType());
    return NPT_MessageHandler::HandleMessage(message);
}

/*----------------------------------------------------------------------
|       FooServer::OnMessage
+---------------------------------------------------------------------*/
void
FooServer::OnMessage(NPT_Message* message)
{
    printf("FooServer::OnMessage (%s)\n", message->GetType());
}

/*----------------------------------------------------------------------
|       FooServer::OnBarCmd1
+---------------------------------------------------------------------*/
void 
FooServer::OnBarCmd1(NPT_MessageReceiver* receiver, int info)
{
    printf("FooServer::OnBarCmd1 %d\n", info);
    receiver->PostMessage(new FooClientBarNotification1Message(7));
}

/*----------------------------------------------------------------------
|       FooServer::OnBarCmd2
+---------------------------------------------------------------------*/
void 
FooServer::OnBarCmd2(NPT_MessageReceiver* /*receiver*/, int info1, int info2)
{
    printf("FooServer::OnBarCmd2 %d %d\n", info1, info2);
}

/*----------------------------------------------------------------------
|       FooServer::DoBarCmd1
+---------------------------------------------------------------------*/
NPT_Result
FooServer::DoBarCmd1(NPT_MessageReceiver* receiver, int info)
{
    return PostMessage(new FooServerBarCmd1Message(receiver, info));
}

/*----------------------------------------------------------------------
|       FooServer::DoBarCmd2
+---------------------------------------------------------------------*/
NPT_Result
FooServer::DoBarCmd2(NPT_MessageReceiver* receiver, int info1, int info2)
{
    return PostMessage(new FooServerBarCmd2Message(receiver, info1,info2));
}

/*----------------------------------------------------------------------
|       FooServer::DoBarCmd3
+---------------------------------------------------------------------*/
NPT_Result
FooServer::DoBarCmd3(NPT_MessageReceiver* receiver, 
                     int info1, int info2, int info3)
{
    return PostMessage(new FooServerBarCmd3Message(receiver, 
                                                   info1, info2, info3));
}

/*----------------------------------------------------------------------
|       FooServer::DoBarCmd4
+---------------------------------------------------------------------*/
NPT_Result
FooServer::DoBarCmd4()
{
    return PostMessage(new FooServerBarCmd4Message());
}

/*----------------------------------------------------------------------
|       FooClient
+---------------------------------------------------------------------*/
class FooClient : public NPT_MessageReceiver,
                  public NPT_MessageHandler,
                  public FooClientMessageHandler
{
public:
    FooClient(FooServer* server, int id);
    
    // NPT_MessageHandler methods
    //void OnMessage(NPT_Message* message);
    
    // NPT_FooServerMessageHandler methods
    void OnBarNotification1(int info);
    void OnBarNotification2(int info1, int info2);

private:
    // members
    FooServer* m_Server;
    int        m_Id;
};

/*----------------------------------------------------------------------
|       FooClient::FooClient
+---------------------------------------------------------------------*/
FooClient::FooClient(FooServer* server, int id) :
    m_Server(server), m_Id(id)
{
    // set ourself as the message handler
    SetHandler(this);
    
    // send commands to server
    server->DoBarCmd1(this, 1);
    server->DoBarCmd2(this, 1, 2);
    server->DoBarCmd3(this, 1, 2, 3);
    server->DoBarCmd4();
}

/*----------------------------------------------------------------------
|       FooClient::OnBarNotification1
+---------------------------------------------------------------------*/
void
FooClient::OnBarNotification1(int info)
{
    printf("FooClient::OnBarNotification1 (client=%d) %d\n", m_Id, info);
}

/*----------------------------------------------------------------------
|       FooClient::OnBarNotification2
+---------------------------------------------------------------------*/
void
FooClient::OnBarNotification2(int info1, int info2)
{
    printf("FooClient::OnBarNotification2 (client=%d) %d %d\n", m_Id, info1, info2);
}

/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int /*argc*/, char** /*argv*/) 
{
    printf("MessagesTest1:: start\n");

    FooServer* server  = new FooServer();
    FooClient* client1 = new FooClient(server, 1);
    FooClient* client2 = new FooClient(server, 2);
    NPT_MessageQueue* queue = new NPT_SimpleMessageQueue();

    client1->SetQueue(queue);
    client2->SetQueue(queue);

    while (queue->PumpMessage() == NPT_SUCCESS) {}

    delete client1;
    delete client2;
    delete server;
    delete queue;
    
    printf("MessagesTest1:: end\n");
}

