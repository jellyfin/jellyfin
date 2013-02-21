/*****************************************************************
|
|   Neptune - Messaging System
|
| Copyright (c) 2002-2008, Axiomatic Systems, LLC.
| All rights reserved.
|
| Redistribution and use in source and binary forms, with or without
| modification, are permitted provided that the following conditions are met:
|     * Redistributions of source code must retain the above copyright
|       notice, this list of conditions and the following disclaimer.
|     * Redistributions in binary form must reproduce the above copyright
|       notice, this list of conditions and the following disclaimer in the
|       documentation and/or other materials provided with the distribution.
|     * Neither the name of Axiomatic Systems nor the
|       names of its contributors may be used to endorse or promote products
|       derived from this software without specific prior written permission.
|
| THIS SOFTWARE IS PROVIDED BY AXIOMATIC SYSTEMS ''AS IS'' AND ANY
| EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
| WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
| DISCLAIMED. IN NO EVENT SHALL AXIOMATIC SYSTEMS BE LIABLE FOR ANY
| DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
| (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
| LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
| ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
| (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
| SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
|
 ****************************************************************/

#ifndef _NPT_MESSAGING_H_
#define _NPT_MESSAGING_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptConstants.h"
#include "NptTypes.h"
#include "NptResults.h"
#include "NptList.h"
#include "NptThreads.h"
#include "NptDynamicCast.h"

/*----------------------------------------------------------------------
|   forward references
+---------------------------------------------------------------------*/
class NPT_Message;

/*----------------------------------------------------------------------
|   NPT_MessageHandler
+---------------------------------------------------------------------*/
class NPT_MessageHandler
{
public:
    NPT_IMPLEMENT_DYNAMIC_CAST(NPT_MessageHandler)
    
    // methods
    virtual ~NPT_MessageHandler() {}

    // default message handler
    virtual void OnMessage(NPT_Message*) {}

    // this method is a central point of handling for received messages.
    // it can be overloaded by subclasses that wish to process all 
    // incoming messages
    virtual NPT_Result HandleMessage(NPT_Message* message);
};

/*----------------------------------------------------------------------
|   NPT_MessageHandlerProxy
+---------------------------------------------------------------------*/
class NPT_MessageHandlerProxy : public NPT_MessageHandler
{
public:
    NPT_IMPLEMENT_DYNAMIC_CAST_D(NPT_MessageHandlerProxy, NPT_MessageHandler)

    /**
     * Create a proxy for a message handler.
     * All calls to HandleMessage() and OnMessage() on the proxy
     * are automatically forwarded to the handler.
     * This class is useful in cases where a handler is passed
     * asynchronously (for example in a message queue) and one wishes
     * to guarantee right away that no more calls to the handler will be 
     * made (because, for example, the handler needs to be deleted).
     *
     * The proxy object keeps a pointer to the handler, but does not own it.
     */
    NPT_MessageHandlerProxy(NPT_MessageHandler* handler);
    
    // destructor
    virtual ~NPT_MessageHandlerProxy();

    // NPT_MessageHandler methods
    virtual void OnMessage(NPT_Message*);
    virtual NPT_Result HandleMessage(NPT_Message* message);
    
    /**
     * Detach the proxy from the handler implementation.
     * After this call returns, calls will no longer be
     * forwarded to the handler object. It is then safe, for example,
     * to delete the handler.
     */
    void DetachHandler();
    
    /**
     * Increment the reference count
     */
    void AddReference();

    /**
     * Decrement the reference count and delete if 0
     */
    void Release();
    
private:
    // members
    NPT_MessageHandler* m_Handler;
    NPT_Cardinal        m_ReferenceCount;
    NPT_Mutex           m_Lock;
};

/*----------------------------------------------------------------------
|   NPT_Messsage
+---------------------------------------------------------------------*/
class NPT_Message
{
public:
    // types
    typedef const char* Type;

    // static members
    static Type const MessageType;

    // methods
    virtual           ~NPT_Message() {}
    virtual Type       GetType() { return MessageType; }
    virtual NPT_Result Dispatch(NPT_MessageHandler* handler) {
        return DefaultDeliver(handler);
    }
    // this method should really be called 'Deliver', but this would
    // cause a problem when subclasses overload it 
    virtual NPT_Result DefaultDeliver(NPT_MessageHandler* handler) {
        handler->OnMessage(this);
        return NPT_SUCCESS;
    }
};

/*----------------------------------------------------------------------
|   NPT_TerminateMesssage
+---------------------------------------------------------------------*/
class NPT_TerminateMessage : public NPT_Message
{
public:
    // methods
    NPT_Result Dispatch(NPT_MessageHandler* /*handler*/) {
        return NPT_ERROR_TERMINATED;
    }
};

/*----------------------------------------------------------------------
|   NPT_MessageQueue
+---------------------------------------------------------------------*/
class NPT_MessageQueue
{
public:
    // methods
    virtual           ~NPT_MessageQueue() {}
    virtual NPT_Result PumpMessage(NPT_Timeout timeout = NPT_TIMEOUT_INFINITE) = 0;
    virtual NPT_Result QueueMessage(NPT_Message*        message, 
                                    NPT_MessageHandler* handler) = 0;
};

/*----------------------------------------------------------------------
|   NPT_MessageReceiver
+---------------------------------------------------------------------*/
class NPT_MessageReceiver
{
public:
    // methods
    NPT_MessageReceiver() : m_Queue(NULL), m_Handler(NULL) {}
    NPT_MessageReceiver(NPT_MessageHandler* handler) : 
        m_Queue(NULL), m_Handler(handler) {}
    NPT_MessageReceiver(NPT_MessageQueue* queue) : 
        m_Queue(queue), m_Handler(NULL) {}
    NPT_MessageReceiver(NPT_MessageHandler* handler, 
                        NPT_MessageQueue*   queue) : 
        m_Queue(queue), m_Handler(handler) {}
    virtual ~NPT_MessageReceiver() {}
    NPT_Result SetQueue(NPT_MessageQueue* queue) {
        m_Queue = queue;
        return NPT_SUCCESS;
    }
    NPT_Result SetHandler(NPT_MessageHandler* handler) {
        m_Handler = handler;
        return NPT_SUCCESS;
    }
    virtual NPT_Result PostMessage(NPT_Message* message) {
        if (m_Queue) {
            return m_Queue->QueueMessage(message, m_Handler);
        } else {
            return NPT_FAILURE;
        }
    }

protected:
    // members
    NPT_MessageQueue*   m_Queue;
    NPT_MessageHandler* m_Handler;
};

/*----------------------------------------------------------------------
|   NPT_MessageBroadcaster
+---------------------------------------------------------------------*/
class NPT_MessageBroadcaster
{
public:
    // methods
    NPT_MessageBroadcaster(NPT_Message* message) : m_Message(message) {}
    NPT_Result operator()(NPT_MessageReceiver*& receiver) const {
        receiver->PostMessage(m_Message);
        return NPT_SUCCESS;
    }

private:
    // members
    NPT_Message* m_Message;
};

#endif // _NPT_MESSAGING_H_
