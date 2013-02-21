/*****************************************************************
|
|      Neptune - Win32 Message Queue
|
|      (c) 2001-2008 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include "NptWin32MessageQueue.h"

/*----------------------------------------------------------------------
|       platform adaptation
+---------------------------------------------------------------------*/
#if defined(_WIN32_WCE)
#define GetWindowLongPtr GetWindowLong
#define SetWindowLongPtr SetWindowLong
#endif

/*----------------------------------------------------------------------
|       constants
+---------------------------------------------------------------------*/
const int NPT_WIN32_MESSAGE_ID_BASE = WM_USER + 9200;

/*----------------------------------------------------------------------
|       NPT_Win32WindowMessageQueue
+---------------------------------------------------------------------*/
NPT_Win32WindowMessageQueue::NPT_Win32WindowMessageQueue()
{
    // create a hidden window to process our incoming messages
    WNDCLASS wclass;

    // compute a unique class name
    m_ClassName[0] = 'N';
    m_ClassName[1] = 'P';
    m_ClassName[2] = 'T';
    m_ClassName[3] = 'H';
    m_ClassName[4] = 'W';
    NPT_String tid = NPT_String::FromInteger(GetCurrentThreadId());
    for (unsigned int i=0; i<=tid.GetLength(); i++) {
        m_ClassName[5+i] = tid.GetChars()[i];        
    }

    // register a window class
    wclass.style         = 0;
    wclass.lpfnWndProc   = NPT_Win32WindowMessageQueue::WindowProcedure;
    wclass.cbClsExtra    = 0;
    wclass.cbWndExtra    = 0;
    wclass.hInstance     = GetModuleHandle(NULL);
    wclass.hIcon         = NULL;
    wclass.hCursor       = NULL;
    wclass.hbrBackground = NULL;
    wclass.lpszMenuName  = NULL;
    wclass.lpszClassName = m_ClassName;

    // register the class and ignore any error because we might
    // be registering the class more than once                  
    RegisterClass(&wclass);

    // create the hidden window
    m_WindowHandle = CreateWindow(
        wclass.lpszClassName, // pointer to registered class name 
        TEXT(""),             // pointer to window name 
        0,                    // window style 
        0,                    // horizontal position of window 
        0,                    // vertical position of window 
        0,                    // window width 
        0,                    // window height 
        NULL,                 // handle to parent or owner window 
        NULL,                 // handle to menu or child-window identifier 
        wclass.hInstance,     // handle to application instance 
        NULL);

    // set a pointer to ourself as user data */
#if defined(_MSC_VER)
#pragma warning( push )
#pragma warning( disable: 4244) // we have to test for this because SetWindowLongPtr
                                // is incorrectly defined, so we'll get a C4244 warning
#endif // _MSC_VER
    if (m_WindowHandle) {
        SetWindowLongPtr(m_WindowHandle, GWL_USERDATA, NPT_POINTER_TO_LONG(this));
    }
#if defined(_MSC_VER)
#pragma warning( pop )
#endif // _MSC_VER
    m_hInstance = wclass.hInstance;
}

/*----------------------------------------------------------------------
|       NPT_Win32WindowMessageQueue
+---------------------------------------------------------------------*/
NPT_Win32WindowMessageQueue::~NPT_Win32WindowMessageQueue() 
{
    // remove ourself as user data to ensure we're not called anymore
    SetWindowLongPtr(m_WindowHandle, GWL_USERDATA, 0);

    // destroy the hidden window
    DestroyWindow(m_WindowHandle);

    // unregister the window class
    UnregisterClass(m_ClassName, m_hInstance);
}

/*----------------------------------------------------------------------
|       NPT_Win32WindowMessageQueue::WindowProcedure
+---------------------------------------------------------------------*/
LRESULT CALLBACK 
NPT_Win32WindowMessageQueue::WindowProcedure(HWND   window, 
                                             UINT   message,
                                             WPARAM wparam, 
                                             LPARAM lparam)
{
    // if it is a windows message, just pass it along
    if (message != (UINT) NPT_WIN32_MESSAGE_ID_BASE) {
        return DefWindowProc(window, message, wparam, lparam);
    }

    // dispatch the message to the handler
#if defined(_MSC_VER)
#pragma warning( push )
#pragma warning( disable: 4312) // we have to test for this because GetWindowLongPtr
                                // is incorrectly defined, so we'll get a C4244 warning
#endif // _MSC_VER
    NPT_Win32WindowMessageQueue* queue = reinterpret_cast<NPT_Win32WindowMessageQueue *>(GetWindowLongPtr(window, GWL_USERDATA));
#if defined(_MSC_VER)
#pragma warning( pop )  
#endif // _MSC_VER
    if (queue == NULL) {
        return 0; 
    }
    queue->HandleMessage(reinterpret_cast<NPT_Message*>(lparam),
        reinterpret_cast<NPT_MessageHandler*>(wparam));
    return 0;
}

/*----------------------------------------------------------------------
|       NPT_Win32WindowMessageQueue::PumpMessage
+---------------------------------------------------------------------*/
NPT_Result
NPT_Win32WindowMessageQueue::PumpMessage(NPT_Timeout)
{
    // you cannot pump messages on this type of queue, since they will
    // be pumped by the main windows message loop 
    return NPT_ERROR_NOT_SUPPORTED; 
}

/*----------------------------------------------------------------------
|       NPT_Win32WindowMessageQueue::QueueMessage
+---------------------------------------------------------------------*/
NPT_Result
NPT_Win32WindowMessageQueue::QueueMessage(NPT_Message*        message,
                                          NPT_MessageHandler* handler)
{
    int result;

    result = ::PostMessage(m_WindowHandle, 
        NPT_WIN32_MESSAGE_ID_BASE, 
        (WPARAM)handler, 
        (LPARAM)message);

    if (result == 0) return NPT_FAILURE;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|       NPT_Win32WindowMessageQueue::HandleMessage
+---------------------------------------------------------------------*/
NPT_Result
NPT_Win32WindowMessageQueue::HandleMessage(NPT_Message*        message,
                                           NPT_MessageHandler* handler)
{
    NPT_Result result = NPT_FAILURE;

    if (message && handler) {
        result = handler->HandleMessage(message);
    }
    delete message;
    return result;
}

