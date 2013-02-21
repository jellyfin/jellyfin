/*****************************************************************
|
|      Neptune - Cocoa Message Queue
|
|      (c) 2001-2008 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
****************************************************************/

#ifndef _NPT_COCOA_MESSAGE_QUEUE_
#define _NPT_COCOA_MESSAGE_QUEUE_

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include "NptStrings.h"
#include "NptMessaging.h"
#include "NptSimpleMessageQueue.h"

/*----------------------------------------------------------------------
|       NPT_CocoaMessageQueue
+---------------------------------------------------------------------*/
class NPT_CocoaMessageQueue : public NPT_MessageQueue
{
public:
    NPT_CocoaMessageQueue();
    ~NPT_CocoaMessageQueue();

    // NPT_MessageQueue methods
    virtual NPT_Result QueueMessage(NPT_Message*        message,
                                    NPT_MessageHandler* handler);
    virtual NPT_Result PumpMessage(NPT_Timeout timeout = NPT_TIMEOUT_INFINITE);

    // methods
    NPT_Result HandleMessage(NPT_Message* message, NPT_MessageHandler* handler);
};

#endif /* _NPT_COCOA_MESSAGE_QUEUE_ */

