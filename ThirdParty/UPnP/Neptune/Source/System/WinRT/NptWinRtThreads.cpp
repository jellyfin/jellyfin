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
#include "NptWinRtPch.h"

using namespace Platform;
using namespace Windows::System::Threading;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Concurrency;

#include "NptConfig.h"
#include "NptTypes.h"
#include "NptConstants.h"
#include "NptThreads.h"
#include "NptDebug.h"
#include "NptResults.h"
#include "NptWinRtThreads.h"
#include "NptTime.h"
#include "NptSystem.h"
#include "NptLogging.h"

/*----------------------------------------------------------------------
|   logging
+---------------------------------------------------------------------*/
NPT_SET_LOCAL_LOGGER("neptune.threads.winrt")

/*----------------------------------------------------------------------
|   NPT_WinRtMutex::NPT_WinRtMutex
+---------------------------------------------------------------------*/
NPT_WinRtMutex::NPT_WinRtMutex()
{
    m_Handle = CreateMutexExW(NULL, L"", FALSE, MUTEX_ALL_ACCESS);
}

/*----------------------------------------------------------------------
|   NPT_WinRtMutex::~NPT_WinRtMutex
+---------------------------------------------------------------------*/
NPT_WinRtMutex::~NPT_WinRtMutex()
{
    CloseHandle(m_Handle);
}

/*----------------------------------------------------------------------
|   NPT_WinRtMutex::Lock
+---------------------------------------------------------------------*/
NPT_Result
NPT_WinRtMutex::Lock()
{
    DWORD result = WaitForSingleObjectEx(m_Handle, INFINITE, FALSE);
    if (result == WAIT_OBJECT_0) {
        return NPT_SUCCESS;
    } else {
        return NPT_FAILURE;
    }
}

/*----------------------------------------------------------------------
|   NPT_WinRtMutex::Unlock
+---------------------------------------------------------------------*/
NPT_Result
NPT_WinRtMutex::Unlock()
{
    ReleaseMutex(m_Handle);
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Mutex::NPT_Mutex
+---------------------------------------------------------------------*/
NPT_Mutex::NPT_Mutex()
{
    m_Delegate = new NPT_WinRtMutex();
}

/*----------------------------------------------------------------------
|   NPT_WinRtCriticalSection::NPT_WinRtCriticalSection
+---------------------------------------------------------------------*/
NPT_WinRtCriticalSection::NPT_WinRtCriticalSection()
{
    InitializeCriticalSectionEx(&m_CriticalSection, 0, 0);
}

/*----------------------------------------------------------------------
|   NPT_WinRtCriticalSection::~NPT_WinRtCriticalSection
+---------------------------------------------------------------------*/
NPT_WinRtCriticalSection::~NPT_WinRtCriticalSection()
{
    DeleteCriticalSection(&m_CriticalSection);
}

/*----------------------------------------------------------------------
|   NPT_WinRtCriticalSection::Lock
+---------------------------------------------------------------------*/
NPT_Result
NPT_WinRtCriticalSection::Lock()
{
    EnterCriticalSection(&m_CriticalSection);
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_WinRtCriticalSection::Unlock
+---------------------------------------------------------------------*/
NPT_Result
NPT_WinRtCriticalSection::Unlock()
{
    LeaveCriticalSection(&m_CriticalSection);
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_WinRtEvent::NPT_WinRtEvent
+---------------------------------------------------------------------*/
NPT_WinRtEvent::NPT_WinRtEvent(bool manual /* = false */, bool initial /* = false */)
{
	DWORD flags = 0;
	if (manual)  flags |= CREATE_EVENT_MANUAL_RESET;
	if (initial) flags |= CREATE_EVENT_INITIAL_SET;
    m_Event = CreateEventExW(NULL, L"", flags, EVENT_ALL_ACCESS);
}

/*----------------------------------------------------------------------
|   NPT_WinRtEvent::~NPT_WinRtEvent
+---------------------------------------------------------------------*/
NPT_WinRtEvent::~NPT_WinRtEvent()
{
    CloseHandle(m_Event);
}

/*----------------------------------------------------------------------
|   NPT_WinRtEvent::Wait
+---------------------------------------------------------------------*/
NPT_Result
NPT_WinRtEvent::Wait(NPT_Timeout timeout)
{
    if (m_Event) {
        DWORD result = WaitForSingleObjectEx(m_Event, timeout==NPT_TIMEOUT_INFINITE?INFINITE:timeout, FALSE);
        if (result == WAIT_TIMEOUT) {
            return NPT_ERROR_TIMEOUT;
        }
        if (result != WAIT_OBJECT_0 && result != WAIT_ABANDONED) {
            return NPT_FAILURE;
        }
    }
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_WinRtEvent::Signal
+---------------------------------------------------------------------*/
void
NPT_WinRtEvent::Signal()
{
    SetEvent(m_Event);
}

/*----------------------------------------------------------------------
|   NPT_WinRtEvent::Reset
+---------------------------------------------------------------------*/
void
NPT_WinRtEvent::Reset()
{
    ResetEvent(m_Event);
}

/*----------------------------------------------------------------------
|       NPT_WinRtSharedVariable
+---------------------------------------------------------------------*/
class NPT_WinRtSharedVariable : public NPT_SharedVariableInterface
{
public:
    // methods
               NPT_WinRtSharedVariable(int value);
              ~NPT_WinRtSharedVariable();
    void       SetValue(int value);
    int        GetValue();
    NPT_Result WaitUntilEquals(int value, NPT_Timeout timeout);
    NPT_Result WaitWhileEquals(int value, NPT_Timeout timeout);

 private:
    // members
    volatile int       m_Value;
    CRITICAL_SECTION   m_Mutex;
    CONDITION_VARIABLE m_Condition;
};

/*----------------------------------------------------------------------
|       NPT_WinRtSharedVariable::NPT_WinRtSharedVariable
+---------------------------------------------------------------------*/
NPT_WinRtSharedVariable::NPT_WinRtSharedVariable(int value) : 
    m_Value(value)
{
    InitializeCriticalSectionEx(&m_Mutex, 0, 0);
    InitializeConditionVariable(&m_Condition);
}

/*----------------------------------------------------------------------
|       NPT_WinRtSharedVariable::~NPT_WinRtSharedVariable
+---------------------------------------------------------------------*/
NPT_WinRtSharedVariable::~NPT_WinRtSharedVariable()
{
    DeleteCriticalSection(&m_Mutex);
}

/*----------------------------------------------------------------------
|       NPT_WinRtSharedVariable::SetValue
+---------------------------------------------------------------------*/
void
NPT_WinRtSharedVariable::SetValue(int value)
{
    EnterCriticalSection(&m_Mutex);
    m_Value = value;
    WakeAllConditionVariable(&m_Condition);
    LeaveCriticalSection(&m_Mutex);
}

/*----------------------------------------------------------------------
|       NPT_WinRtSharedVariable::GetValue
+---------------------------------------------------------------------*/
int
NPT_WinRtSharedVariable::GetValue()
{
    // we assume that int read/write are atomic on the platform
    return m_Value;
}

/*----------------------------------------------------------------------
|       NPT_WinRtSharedVariable::WaitUntilEquals
+---------------------------------------------------------------------*/
NPT_Result
NPT_WinRtSharedVariable::WaitUntilEquals(int value, NPT_Timeout timeout)
{
    NPT_Result result = NPT_SUCCESS;
    
    EnterCriticalSection(&m_Mutex);
    while (value != m_Value) {
        if (!SleepConditionVariableCS(&m_Condition, &m_Mutex, timeout==NPT_TIMEOUT_INFINITE?INFINITE:timeout)) {
            DWORD error = GetLastError();
            if (error == ERROR_TIMEOUT) {
                result = NPT_ERROR_TIMEOUT;
            } else {
                result = NPT_FAILURE;
            }
        }
    }
    LeaveCriticalSection(&m_Mutex);
    
    return result;
}

/*----------------------------------------------------------------------
|       NPT_WinRtSharedVariable::WaitWhileEquals
+---------------------------------------------------------------------*/
NPT_Result
NPT_WinRtSharedVariable::WaitWhileEquals(int value, NPT_Timeout timeout)
{
    NPT_Result result = NPT_SUCCESS;

    EnterCriticalSection(&m_Mutex);
    while (value == m_Value) {
        if (!SleepConditionVariableCS(&m_Condition, &m_Mutex, timeout==NPT_TIMEOUT_INFINITE?INFINITE:timeout)) {
            DWORD error = GetLastError();
            if (error == ERROR_TIMEOUT) {
                result = NPT_ERROR_TIMEOUT;
            } else {
                result = NPT_FAILURE;
            }
        }
    }
    LeaveCriticalSection(&m_Mutex);
    
    return result;
}

/*----------------------------------------------------------------------
|       NPT_SharedVariable::NPT_SharedVariable
+---------------------------------------------------------------------*/
NPT_SharedVariable::NPT_SharedVariable(int value)
{
    m_Delegate = new NPT_WinRtSharedVariable(value);
}

#if 0
/*----------------------------------------------------------------------
|   NPT_WinRtSharedVariable
+---------------------------------------------------------------------*/
class NPT_WinRtSharedVariable : public NPT_SharedVariableInterface
{
 public:
    // methods
               NPT_WinRtSharedVariable(int value);
              ~NPT_WinRtSharedVariable();
    void       SetValue(int value);
    int        GetValue();
    NPT_Result WaitUntilEquals(int value, NPT_Timeout timeout = NPT_TIMEOUT_INFINITE);
    NPT_Result WaitWhileEquals(int value, NPT_Timeout timeout = NPT_TIMEOUT_INFINITE);

 private:
    // members
    volatile int   m_Value;
    NPT_Mutex      m_Lock;
    NPT_WinRtEvent m_Event;
};

/*----------------------------------------------------------------------
|   NPT_WinRtSharedVariable::NPT_WinRtSharedVariable
+---------------------------------------------------------------------*/
NPT_WinRtSharedVariable::NPT_WinRtSharedVariable(int value) : 
    m_Value(value)
{
}

/*----------------------------------------------------------------------
|   NPT_WinRtSharedVariable::~NPT_WinRtSharedVariable
+---------------------------------------------------------------------*/
NPT_WinRtSharedVariable::~NPT_WinRtSharedVariable()
{
}

/*----------------------------------------------------------------------
|   NPT_WinRtSharedVariable::SetValue
+---------------------------------------------------------------------*/
void
NPT_WinRtSharedVariable::SetValue(int value)
{
    m_Lock.Lock();
    if (value != m_Value) {
        m_Value = value;
        m_Event.Signal();
    }
    m_Lock.Unlock();
}

/*----------------------------------------------------------------------
|   NPT_WinRtSharedVariable::GetValue
+---------------------------------------------------------------------*/
int
NPT_WinRtSharedVariable::GetValue()
{
    // reading an integer should be atomic on all WinRt platforms
    return m_Value;
}

/*----------------------------------------------------------------------
|   NPT_WinRtSharedVariable::WaitUntilEquals
+---------------------------------------------------------------------*/
NPT_Result
NPT_WinRtSharedVariable::WaitUntilEquals(int value, NPT_Timeout timeout)
{
    do {
        m_Lock.Lock();
        if (m_Value == value) {
            break;
        }
        m_Lock.Unlock();
        {
             NPT_Result result = m_Event.Wait(timeout);
             if (NPT_FAILED(result)) return result;
        }
    } while (1);

    m_Lock.Unlock();
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_WinRtSharedVariable::WaitWhileEquals
+---------------------------------------------------------------------*/
NPT_Result
NPT_WinRtSharedVariable::WaitWhileEquals(int value, NPT_Timeout timeout)
{
    do {
        m_Lock.Lock();
        if (m_Value != value) {
            break;
        }
        m_Lock.Unlock();
        {
             NPT_Result result = m_Event.Wait(timeout);
             if (NPT_FAILED(result)) return result;
        }
    } while (1);

    m_Lock.Unlock();
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_SharedVariable::NPT_SharedVariable
+---------------------------------------------------------------------*/
NPT_SharedVariable::NPT_SharedVariable(int value)
{
    m_Delegate = new NPT_WinRtSharedVariable(value);
}
#endif

/*----------------------------------------------------------------------
|   NPT_WinRtAtomicVariable
+---------------------------------------------------------------------*/
class NPT_WinRtAtomicVariable : public NPT_AtomicVariableInterface
{
 public:
    // methods
                NPT_WinRtAtomicVariable(int value);
               ~NPT_WinRtAtomicVariable();
    int  Increment(); 
    int  Decrement();
    void SetValue(int value);
    int  GetValue();

 private:
    // members
    volatile LONG m_Value;
};

/*----------------------------------------------------------------------
|   NPT_WinRtAtomicVariable::NPT_WinRtAtomicVariable
+---------------------------------------------------------------------*/
NPT_WinRtAtomicVariable::NPT_WinRtAtomicVariable(int value) : 
    m_Value(value)
{
}

/*----------------------------------------------------------------------
|   NPT_WinRtAtomicVariable::~NPT_WinRtAtomicVariable
+---------------------------------------------------------------------*/
NPT_WinRtAtomicVariable::~NPT_WinRtAtomicVariable()
{
}

/*----------------------------------------------------------------------
|   NPT_WinRtAtomicVariable::Increment
+---------------------------------------------------------------------*/
int
NPT_WinRtAtomicVariable::Increment()
{
    return InterlockedIncrement(const_cast<LONG*>(&m_Value));
}

/*----------------------------------------------------------------------
|   NPT_WinRtAtomicVariable::Decrement
+---------------------------------------------------------------------*/
int
NPT_WinRtAtomicVariable::Decrement()
{
    return InterlockedDecrement(const_cast<LONG*>(&m_Value));
}

/*----------------------------------------------------------------------
|   NPT_WinRtAtomicVariable::SetValue
+---------------------------------------------------------------------*/
void
NPT_WinRtAtomicVariable::SetValue(int value)
{
    m_Value = value;
}

/*----------------------------------------------------------------------
|   NPT_WinRtAtomicVariable::GetValue
+---------------------------------------------------------------------*/
int
NPT_WinRtAtomicVariable::GetValue()
{
    return m_Value;
}

/*----------------------------------------------------------------------
|   NPT_AtomicVariable::NPT_AtomicVariable
+---------------------------------------------------------------------*/
NPT_AtomicVariable::NPT_AtomicVariable(int value)
{
    m_Delegate = new NPT_WinRtAtomicVariable(value);
}

/*----------------------------------------------------------------------
|   NPT_WinRtThread
+---------------------------------------------------------------------*/
class NPT_WinRtThread : public NPT_ThreadInterface
{
 public:
	// class methods
	static NPT_Result SetThreadPriority(HANDLE thread, int priority);

    // methods
                NPT_WinRtThread(NPT_Thread*   delegator,
                                NPT_Runnable& target,
                                bool          detached);
               ~NPT_WinRtThread();
    NPT_Result  Start(); 
    NPT_Result  Wait(NPT_Timeout timeout = NPT_TIMEOUT_INFINITE);
	NPT_Result  SetPriority(int priority);

 private:
    // methods
    static unsigned int __stdcall EntryPoint(void* argument);

    // NPT_Runnable methods
    void Run();

    // NPT_Interruptible methods
    NPT_Result Interrupt() { return NPT_ERROR_NOT_IMPLEMENTED; } 

    // members
    NPT_Thread*   m_Delegator;
    NPT_Runnable& m_Target;
    bool          m_Detached;
    HANDLE        m_ThreadHandle;
};

/*----------------------------------------------------------------------
|   NPT_WinRtThread::NPT_WinRtThread
+---------------------------------------------------------------------*/
NPT_WinRtThread::NPT_WinRtThread(NPT_Thread*   delegator,
                                 NPT_Runnable& target,
                                 bool          detached) : 
    m_Delegator(delegator),
    m_Target(target),
    m_Detached(detached),
    m_ThreadHandle(0)
{
}

/*----------------------------------------------------------------------
|   NPT_WinRtThread::~NPT_WinRtThread
+---------------------------------------------------------------------*/
NPT_WinRtThread::~NPT_WinRtThread()
{
    if (!m_Detached) {
        // we're not detached, and not in the Run() method, so we need to 
        // wait until the thread is done
        Wait();
    }

    // close the thread handle
    if (m_ThreadHandle) {
        CloseHandle(m_ThreadHandle);
    }
}

/*----------------------------------------------------------------------
|   NPT_WinRtThread::SetThreadPriority
+---------------------------------------------------------------------*/
NPT_Result
NPT_WinRtThread::SetThreadPriority(HANDLE thread, int priority)
{
	int WinRt_priority;
	if (priority < NPT_THREAD_PRIORITY_LOWEST) {
		WinRt_priority = THREAD_PRIORITY_IDLE;
	} else if (priority < NPT_THREAD_PRIORITY_BELOW_NORMAL) {
		WinRt_priority = THREAD_PRIORITY_LOWEST;
	} else if (priority < NPT_THREAD_PRIORITY_NORMAL) {
		WinRt_priority = THREAD_PRIORITY_BELOW_NORMAL;
	} else if (priority < NPT_THREAD_PRIORITY_ABOVE_NORMAL) {
		WinRt_priority = THREAD_PRIORITY_NORMAL;
	} else if (priority < NPT_THREAD_PRIORITY_HIGHEST) {
		WinRt_priority = THREAD_PRIORITY_ABOVE_NORMAL;
	} else if (priority < NPT_THREAD_PRIORITY_TIME_CRITICAL) {
		WinRt_priority = THREAD_PRIORITY_HIGHEST;
	} else {
		WinRt_priority = THREAD_PRIORITY_TIME_CRITICAL;
	}
#if 0
	BOOL result = ::SetThreadPriority(thread, WinRt_priority);
	if (!result) {
		NPT_LOG_WARNING_1("SetThreadPriority failed (%x)", GetLastError());
		return NPT_FAILURE;
	}
#endif

	return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_WinRtThread::Start
+---------------------------------------------------------------------*/
NPT_Result
NPT_WinRtThread::Start()
{
	if (m_ThreadHandle > 0) {
        // failed
        NPT_LOG_WARNING("thread already started !");
        return NPT_ERROR_INVALID_STATE;
    }

    NPT_LOG_FINER("creating thread");

	// create a stack local variable 'detached', as this object
    // may already be deleted when the thread creation returns and
    // before we get to call detach on the given thread
    bool detached = m_Detached;

    HANDLE thread_handle = CreateEventExW(NULL, NULL, CREATE_EVENT_MANUAL_RESET, EVENT_ALL_ACCESS);

    auto handler = ref new WorkItemHandler([=](IAsyncAction^)
    {
        // run the thread routine
        NPT_LOG_FINE("+++ thread routine start +++");
        try {
            Run();
        } catch(...) {
            NPT_LOG_FINE("*** exception caught during thread routine ***");
        }
        NPT_LOG_FINE("--- thread routine done +++");
        
        // signal that we're done
        SetEvent(thread_handle);

        // if the thread is detached, delete it
        if (detached) {
            delete m_Delegator;
            CloseHandle(thread_handle);
        }
    });

    // remember the handle unless we're detached
    if (!detached) {
        m_ThreadHandle = thread_handle;
    }

    // run the thread
    ThreadPool::RunAsync(handler, WorkItemPriority::Normal, WorkItemOptions::TimeSliced);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_WinRtThread::Run
+---------------------------------------------------------------------*/
void
NPT_WinRtThread::Run()
{
    m_Target.Run();
}

/*----------------------------------------------------------------------
|   NPT_WinRtThread::SetPriority
+---------------------------------------------------------------------*/
NPT_Result 
NPT_WinRtThread::SetPriority(int priority)
{
	if (m_ThreadHandle == 0) return NPT_ERROR_INVALID_STATE;
	return NPT_WinRtThread::SetThreadPriority(m_ThreadHandle, priority);
}

/*----------------------------------------------------------------------
|   NPT_WinRtThread::Wait
+---------------------------------------------------------------------*/
NPT_Result
NPT_WinRtThread::Wait(NPT_Timeout timeout /* = NPT_TIMEOUT_INFINITE */)
{
    // check that we're not detached
    if (m_ThreadHandle == 0 || m_Detached) {
        return NPT_FAILURE;
    }

    // wait for the thread to finish
    // Logging here will cause a crash on exit because LogManager may already be destroyed
    DWORD result = WaitForSingleObjectEx(m_ThreadHandle, 
                                         timeout==NPT_TIMEOUT_INFINITE?INFINITE:timeout,
										 FALSE);
    if (result != WAIT_OBJECT_0) {
        return NPT_ERROR_TIMEOUT;
    } else {
        return NPT_SUCCESS;
    }
}

/*----------------------------------------------------------------------
|   NPT_Thread::GetCurrentThreadId
+---------------------------------------------------------------------*/
NPT_Thread::ThreadId 
NPT_Thread::GetCurrentThreadId()
{
    return ::GetCurrentThreadId();
}

/*----------------------------------------------------------------------
|   NPT_Thread::SetCurrentThreadPriority
+---------------------------------------------------------------------*/
NPT_Result 
NPT_Thread::SetCurrentThreadPriority(int priority)
{
	return NPT_WinRtThread::SetThreadPriority(::GetCurrentThread(), priority);
}

/*----------------------------------------------------------------------
|   NPT_Thread::NPT_Thread
+---------------------------------------------------------------------*/
NPT_Thread::NPT_Thread(bool detached)
{
    m_Delegate = new NPT_WinRtThread(this, *this, detached);
}

/*----------------------------------------------------------------------
|   NPT_Thread::NPT_Thread
+---------------------------------------------------------------------*/
NPT_Thread::NPT_Thread(NPT_Runnable& target, bool detached)
{
    m_Delegate = new NPT_WinRtThread(this, target, detached);
}
