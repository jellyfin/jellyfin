/*****************************************************************
|
|      Neptune - Cocoa Message Queue
|
|      (c) 2001-2008 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptConfig.h"

#import <Foundation/Foundation.h>
#include "NptCocoaMessageQueue.h"

/*----------------------------------------------------------------------
|   NPT_CocoaMessageWrapper
+---------------------------------------------------------------------*/
@interface NPT_CocoaMessageCapsule : NSObject
{
    NPT_Message*             message;
    NPT_MessageHandler*      handler;
    NPT_MessageHandlerProxy* proxy;
}
-(id)   initWithMessage: (NPT_Message*) message andHandler: (NPT_MessageHandler*) handler;
-(void) handle;
@end

@implementation NPT_CocoaMessageCapsule
-(id) initWithMessage: (NPT_Message*) aMessage andHandler: (NPT_MessageHandler*) aHandler
{
    if ((self = [super init])) {
        message = aMessage;
        handler = aHandler;
        proxy   = NPT_DYNAMIC_CAST(NPT_MessageHandlerProxy, aHandler);
        if (proxy) proxy->AddReference();
    }
    return self;
}

-(void) dealloc
{
    delete message;
    if (proxy) proxy->Release();
    [super dealloc];
}

-(void) handle 
{
    if (handler && message) {
        handler->HandleMessage(message);
    }
}
@end

/*----------------------------------------------------------------------
|   NPT_CocoaMessageQueue::NPT_CocoaMessageQueue
+---------------------------------------------------------------------*/
NPT_CocoaMessageQueue::NPT_CocoaMessageQueue()
{
}

/*----------------------------------------------------------------------
|   NPT_CocoaMessageQueue::~NPT_CocoaMessageQueue
+---------------------------------------------------------------------*/
NPT_CocoaMessageQueue::~NPT_CocoaMessageQueue() 
{
}

/*----------------------------------------------------------------------
|   NPT_CocoaMessageQueue::PumpMessage
+---------------------------------------------------------------------*/
NPT_Result
NPT_CocoaMessageQueue::PumpMessage(NPT_Timeout)
{
    // you cannot pump messages on this type of queue, since they will
    // be pumped by the main application message loop 
    return NPT_ERROR_NOT_SUPPORTED; 
}

/*----------------------------------------------------------------------
|   NPT_CocoaMessageQueue::QueueMessage
+---------------------------------------------------------------------*/
NPT_Result
NPT_CocoaMessageQueue::QueueMessage(NPT_Message*        message,
                                    NPT_MessageHandler* handler)
{
    // create a capsule to represent the message and handler
    NPT_CocoaMessageCapsule* capsule = [NPT_CocoaMessageCapsule alloc];
    [capsule initWithMessage: message andHandler: handler];
    
    // trigger the handling of the message on the main thread
    [capsule performSelectorOnMainThread: @selector(handle)
                              withObject: nil
                           waitUntilDone: FALSE];
     
    // we no longer hold a reference to the capsule (it will be released
    // by the receiving thread)
    [capsule release];
    
    return NPT_SUCCESS;
}
