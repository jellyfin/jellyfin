/*****************************************************************
|
|   Neptune - Threads :: WinRT Implementation
|
|   (c) 2001-2012 Gilles Boccon-Gibod
|   Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptConfig.h"
#include "NptTypes.h"
#include "NptThreads.h"
#include "NptDebug.h"

/*----------------------------------------------------------------------
|   NPT_WinRtMutex
+---------------------------------------------------------------------*/
class NPT_WinRtMutex : public NPT_MutexInterface
{
public:
    // methods
             NPT_WinRtMutex();
    virtual ~NPT_WinRtMutex();

    // NPT_Mutex methods
    virtual NPT_Result Lock();
    virtual NPT_Result Unlock();

private:
    // members
    HANDLE m_Handle;
};

/*----------------------------------------------------------------------
|   NPT_WinRtEvent
+---------------------------------------------------------------------*/
class NPT_WinRtEvent
{
public:
    // methods
             NPT_WinRtEvent(bool manual = false, bool initial = false);
    virtual ~NPT_WinRtEvent();

    virtual NPT_Result Wait(NPT_Timeout timeout = NPT_TIMEOUT_INFINITE);
    virtual void       Signal();
    virtual void       Reset();

private:
    // members
    HANDLE m_Event;
};

/*----------------------------------------------------------------------
|   NPT_WinRtCriticalSection
+---------------------------------------------------------------------*/
class NPT_WinRtCriticalSection
{
public:
    // methods
    NPT_WinRtCriticalSection();
   ~NPT_WinRtCriticalSection();

    // NPT_Mutex methods
    NPT_Result Lock();
    NPT_Result Unlock();

private:
    // members
    CRITICAL_SECTION m_CriticalSection;
};
