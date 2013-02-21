/*****************************************************************
|
|   Neptune - Simple Message Queue
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

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptSimpleMessageQueue.h"
#include "NptDebug.h"
#include "NptLogging.h"

/*----------------------------------------------------------------------
|   logging
+---------------------------------------------------------------------*/
NPT_SET_LOCAL_LOGGER("neptune.message-queue")

/*----------------------------------------------------------------------
|   NPT_SimpleMessageCapsule
+---------------------------------------------------------------------*/
class NPT_SimpleMessageCapsule
{
public:
    NPT_SimpleMessageCapsule(NPT_Message* message, 
                             NPT_MessageHandler* handler) :
        m_Message(message), m_Handler(handler), m_Proxy(NULL) {
        m_Proxy = NPT_DYNAMIC_CAST(NPT_MessageHandlerProxy, m_Handler);
        if (m_Proxy) m_Proxy->AddReference();
    }
    virtual ~NPT_SimpleMessageCapsule() { if (m_Proxy) m_Proxy->Release(); }

    NPT_Message*             m_Message;
    NPT_MessageHandler*      m_Handler;
    NPT_MessageHandlerProxy* m_Proxy;
};

/*----------------------------------------------------------------------
|   NPT_SimpleMessageQueue::NPT_SimpleMessageQueue
+---------------------------------------------------------------------*/
NPT_SimpleMessageQueue::NPT_SimpleMessageQueue()
{
}

/*----------------------------------------------------------------------
|   NPT_SimpleMessageQueue::~NPT_SimpleMessageQueue
+---------------------------------------------------------------------*/
NPT_SimpleMessageQueue::~NPT_SimpleMessageQueue()
{
    // empty the queue
    // TBD
}

/*----------------------------------------------------------------------
|   NPT_SimpleMessageQueue::QueueMessage
+---------------------------------------------------------------------*/
NPT_Result
NPT_SimpleMessageQueue::QueueMessage(NPT_Message*        message, 
                                     NPT_MessageHandler* handler)
{
    // push the message on the queue, with the handler reference
    NPT_SimpleMessageCapsule* capsule = new NPT_SimpleMessageCapsule(message, handler);
    NPT_Result result = m_Queue.Push(capsule);
    if (NPT_FAILED(result)) delete capsule;
    return result;
}

/*----------------------------------------------------------------------
|   NPT_SimpleMessageQueue::PumpMessage
+---------------------------------------------------------------------*/
NPT_Result
NPT_SimpleMessageQueue::PumpMessage(NPT_Timeout timeout /* = NPT_TIMEOUT_INFINITE */)
{
    NPT_SimpleMessageCapsule* capsule;
    
    NPT_LOG_FINEST_1("popping message from queue, timeout=%d", timeout);
    NPT_Result result = m_Queue.Pop(capsule, timeout);
    if (NPT_SUCCEEDED(result) && capsule) {
        if (capsule->m_Handler && capsule->m_Message) {
            result = capsule->m_Handler->HandleMessage(capsule->m_Message);
        }
        delete capsule->m_Message;
        delete capsule;
    } else {
        NPT_LOG_FINEST_1("m_Queue.Pop() returned %d", result);
    }

    return result;
}
