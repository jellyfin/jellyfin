/*****************************************************************
|
|      Neptune - Selectable Message Queue
|
|      (c) 2001-2003 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include <unistd.h>

#include "NptSelectableMessageQueue.h"
#include "NptDebug.h"

/*----------------------------------------------------------------------
|    NPT_SelectableMessageQueue::NPT_SelectableMessageQueue
+---------------------------------------------------------------------*/
NPT_SelectableMessageQueue::NPT_SelectableMessageQueue()
{
    if (pipe(m_Pipe) != 0) {
        m_Pipe[0] = -1;
        m_Pipe[1] = -1;
    }
}
 
/*----------------------------------------------------------------------
|    NPT_SelectableMessageQueue::~NPT_SelectableMessageQueue
+---------------------------------------------------------------------*/
NPT_SelectableMessageQueue::~NPT_SelectableMessageQueue()
{
    close(m_Pipe[0]);
    close(m_Pipe[1]);
}
 
/*----------------------------------------------------------------------
|    NPT_SelectableMessageQueue::QueueMessage
+---------------------------------------------------------------------*/
NPT_Result
NPT_SelectableMessageQueue::QueueMessage(NPT_Message*        message,
                                         NPT_MessageHandler* handler)
{
    // first, queue the message
    NPT_Result result = NPT_SimpleMessageQueue::QueueMessage(message, handler);

    // then write a byte on the pipe to signal that there is a message
    if (write(m_Pipe[1], "\0", 1) != 1) {
        result = NPT_FAILURE;
    }

    return result;
}

/*----------------------------------------------------------------------
|    NPT_SelectableMessageQueue::PumpMessage
+---------------------------------------------------------------------*/
NPT_Result
NPT_SelectableMessageQueue::PumpMessage(NPT_Timeout timeout)
{
    NPT_Result result = NPT_SimpleMessageQueue::PumpMessage(timeout);
    if (NPT_SUCCEEDED(result)) {
        // flush the event
        FlushEvent();
    }
    return result;
}

/*----------------------------------------------------------------------
|    NPT_SelectableMessageQueue::FlushEvent
+---------------------------------------------------------------------*/
NPT_Result
NPT_SelectableMessageQueue::FlushEvent()
{
    char buffer[1];
    if (read(m_Pipe[0], buffer, 1) != 1) {
        return NPT_FAILURE;
    }

    return NPT_SUCCESS;
}
 

