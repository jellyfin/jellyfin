/*****************************************************************
|
|      Neptune - Win32 Message Queue
|
|      (c) 2001-2008 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
****************************************************************/

#ifndef _NPT_WIN32_MESSAGE_QUEUE_
#define _NPT_WIN32_MESSAGE_QUEUE_

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include <windows.h>
#include <TCHAR.h>
#include "NptStrings.h"
#include "NptMessaging.h"
#include "NptSimpleMessageQueue.h"

/*----------------------------------------------------------------------
|       NPT_Win32WindowMessageQueue
+---------------------------------------------------------------------*/
class NPT_Win32WindowMessageQueue : public NPT_MessageQueue
{
public:
    NPT_Win32WindowMessageQueue();
    ~NPT_Win32WindowMessageQueue();

    // NPT_MessageQueue methods
    virtual NPT_Result PumpMessage(NPT_Timeout timeout = NPT_TIMEOUT_INFINITE);
    virtual NPT_Result QueueMessage(NPT_Message*        message,
                                    NPT_MessageHandler* handler);

    NPT_Result HandleMessage(NPT_Message* message, NPT_MessageHandler* handler);

private:
    static LRESULT CALLBACK WindowProcedure(HWND   window, 
                                            UINT   message,
                                            WPARAM wparam, 
                                            LPARAM lparam);
    HWND        m_WindowHandle;
    TCHAR       m_ClassName[16];
    HINSTANCE   m_hInstance;
};

#endif // _NPT_WIN32_MESSAGE_QUEUE_

