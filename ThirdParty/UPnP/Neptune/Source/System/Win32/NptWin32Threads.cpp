/*****************************************************************
|
|   Neptune - Threads :: Win32 Implementation
|
|   (c) 2001-2008 Gilles Boccon-Gibod
|   Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#if defined(_XBOX)
#include <xtl.h>
#else
#include <windows.h>
#if !defined(_WIN32_WCE)
#include <process.h>
#endif
#endif

#include "NptConfig.h"
#include "NptTypes.h"
#include "NptConstants.h"
#include "NptThreads.h"
#include "NptDebug.h"
#include "NptResults.h"
#include "NptWin32Threads.h"
#include "NptTime.h"
#include "NptSystem.h"
#include "NptLogging.h"

/*----------------------------------------------------------------------
|   logging
+---------------------------------------------------------------------*/
NPT_SET_LOCAL_LOGGER("neptune.threads.win32")

/*----------------------------------------------------------------------
|   configuration macros
+---------------------------------------------------------------------*/
#if defined(_WIN32_WCE) || defined(_XBOX)
#define NPT_WIN32_USE_CREATE_THREAD
#endif

#if defined(NPT_WIN32_USE_CREATE_THREAD)
#define _beginthreadex(security, stack_size, start_proc, arg, flags, pid) \
CreateThread(security, stack_size, (LPTHREAD_START_ROUTINE) start_proc,   \
             arg, flags, (LPDWORD)pid)
#define _endthreadex ExitThread
#endif

/*----------------------------------------------------------------------
|   NPT_Win32Mutex::NPT_Win32Mutex
+---------------------------------------------------------------------*/
NPT_Win32Mutex::NPT_Win32Mutex()
{
    m_Handle = CreateMutex(NULL, FALSE, NULL);
}

/*----------------------------------------------------------------------
|   NPT_Win32Mutex::~NPT_Win32Mutex
+---------------------------------------------------------------------*/
NPT_Win32Mutex::~NPT_Win32Mutex()
{
    CloseHandle(m_Handle);
}

/*----------------------------------------------------------------------
|   NPT_Win32Mutex::Lock
+---------------------------------------------------------------------*/
NPT_Result
NPT_Win32Mutex::Lock()
{
    DWORD result = WaitForSingleObject(m_Handle, INFINITE);
    if (result == WAIT_OBJECT_0) {
        return NPT_SUCCESS;
    } else {
        return NPT_FAILURE;
    }
}

/*----------------------------------------------------------------------
|   NPT_Win32Mutex::Unlock
+---------------------------------------------------------------------*/
NPT_Result
NPT_Win32Mutex::Unlock()
{
    ReleaseMutex(m_Handle);
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Mutex::NPT_Mutex
+---------------------------------------------------------------------*/
NPT_Mutex::NPT_Mutex()
{
    m_Delegate = new NPT_Win32Mutex();
}

/*----------------------------------------------------------------------
|   NPT_Win32CriticalSection::NPT_Win32CriticalSection
+---------------------------------------------------------------------*/
NPT_Win32CriticalSection::NPT_Win32CriticalSection()
{
    InitializeCriticalSection(&m_CriticalSection);
}

/*----------------------------------------------------------------------
|   NPT_Win32CriticalSection::~NPT_Win32CriticalSection
+---------------------------------------------------------------------*/
NPT_Win32CriticalSection::~NPT_Win32CriticalSection()
{
    DeleteCriticalSection(&m_CriticalSection);
}

/*----------------------------------------------------------------------
|   NPT_Win32CriticalSection::Lock
+---------------------------------------------------------------------*/
NPT_Result
NPT_Win32CriticalSection::Lock()
{
    EnterCriticalSection(&m_CriticalSection);
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Win32CriticalSection::Unlock
+---------------------------------------------------------------------*/
NPT_Result
NPT_Win32CriticalSection::Unlock()
{
    LeaveCriticalSection(&m_CriticalSection);
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Win32Event::NPT_Win32Event
+---------------------------------------------------------------------*/
NPT_Win32Event::NPT_Win32Event(bool manual /* = false */, bool initial /* = false */)
{
    m_Event = CreateEvent(NULL, (manual==true)?TRUE:FALSE, (initial==true)?TRUE:FALSE, NULL);
}

/*----------------------------------------------------------------------
|   NPT_Win32Event::~NPT_Win32Event
+---------------------------------------------------------------------*/
NPT_Win32Event::~NPT_Win32Event()
{
    CloseHandle(m_Event);
}

/*----------------------------------------------------------------------
|   NPT_Win32Event::Wait
+---------------------------------------------------------------------*/
NPT_Result
NPT_Win32Event::Wait(NPT_Timeout timeout)
{
    if (m_Event) {
        DWORD result = WaitForSingleObject(m_Event, timeout==NPT_TIMEOUT_INFINITE?INFINITE:timeout);
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
|   NPT_Win32Event::Signal
+---------------------------------------------------------------------*/
void
NPT_Win32Event::Signal()
{
    SetEvent(m_Event);
}

/*----------------------------------------------------------------------
|   NPT_Win32Event::Reset
+---------------------------------------------------------------------*/
void
NPT_Win32Event::Reset()
{
    ResetEvent(m_Event);
}

/*----------------------------------------------------------------------
|   NPT_Win32SharedVariable
+---------------------------------------------------------------------*/
class NPT_Win32SharedVariable : public NPT_SharedVariableInterface
{
 public:
    // methods
               NPT_Win32SharedVariable(int value);
              ~NPT_Win32SharedVariable();
    void       SetValue(int value);
    int        GetValue();
    NPT_Result WaitUntilEquals(int value, NPT_Timeout timeout = NPT_TIMEOUT_INFINITE);
    NPT_Result WaitWhileEquals(int value, NPT_Timeout timeout = NPT_TIMEOUT_INFINITE);

 private:
    // members
    volatile int   m_Value;
    NPT_Mutex      m_Lock;
    NPT_Win32Event m_Event;
};

/*----------------------------------------------------------------------
|   NPT_Win32SharedVariable::NPT_Win32SharedVariable
+---------------------------------------------------------------------*/
NPT_Win32SharedVariable::NPT_Win32SharedVariable(int value) : 
    m_Value(value)
{
}

/*----------------------------------------------------------------------
|   NPT_Win32SharedVariable::~NPT_Win32SharedVariable
+---------------------------------------------------------------------*/
NPT_Win32SharedVariable::~NPT_Win32SharedVariable()
{
}

/*----------------------------------------------------------------------
|   NPT_Win32SharedVariable::SetValue
+---------------------------------------------------------------------*/
void
NPT_Win32SharedVariable::SetValue(int value)
{
    m_Lock.Lock();
    if (value != m_Value) {
        m_Value = value;
        m_Event.Signal();
    }
    m_Lock.Unlock();
}

/*----------------------------------------------------------------------
|   NPT_Win32SharedVariable::GetValue
+---------------------------------------------------------------------*/
int
NPT_Win32SharedVariable::GetValue()
{
    // reading an integer should be atomic on all Win32 platforms
    return m_Value;
}

/*----------------------------------------------------------------------
|   NPT_Win32SharedVariable::WaitUntilEquals
+---------------------------------------------------------------------*/
NPT_Result
NPT_Win32SharedVariable::WaitUntilEquals(int value, NPT_Timeout timeout)
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
|   NPT_Win32SharedVariable::WaitWhileEquals
+---------------------------------------------------------------------*/
NPT_Result
NPT_Win32SharedVariable::WaitWhileEquals(int value, NPT_Timeout timeout)
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
    m_Delegate = new NPT_Win32SharedVariable(value);
}

/*----------------------------------------------------------------------
|   NPT_Win32AtomicVariable
+---------------------------------------------------------------------*/
class NPT_Win32AtomicVariable : public NPT_AtomicVariableInterface
{
 public:
    // methods
                NPT_Win32AtomicVariable(int value);
               ~NPT_Win32AtomicVariable();
    int  Increment(); 
    int  Decrement();
    void SetValue(int value);
    int  GetValue();

 private:
    // members
    volatile LONG m_Value;
};

/*----------------------------------------------------------------------
|   NPT_Win32AtomicVariable::NPT_Win32AtomicVariable
+---------------------------------------------------------------------*/
NPT_Win32AtomicVariable::NPT_Win32AtomicVariable(int value) : 
    m_Value(value)
{
}

/*----------------------------------------------------------------------
|   NPT_Win32AtomicVariable::~NPT_Win32AtomicVariable
+---------------------------------------------------------------------*/
NPT_Win32AtomicVariable::~NPT_Win32AtomicVariable()
{
}

/*----------------------------------------------------------------------
|   NPT_Win32AtomicVariable::Increment
+---------------------------------------------------------------------*/
int
NPT_Win32AtomicVariable::Increment()
{
    return InterlockedIncrement(const_cast<LONG*>(&m_Value));
}

/*----------------------------------------------------------------------
|   NPT_Win32AtomicVariable::Decrement
+---------------------------------------------------------------------*/
int
NPT_Win32AtomicVariable::Decrement()
{
    return InterlockedDecrement(const_cast<LONG*>(&m_Value));
}

/*----------------------------------------------------------------------
|   NPT_Win32AtomicVariable::SetValue
+---------------------------------------------------------------------*/
void
NPT_Win32AtomicVariable::SetValue(int value)
{
    m_Value = value;
}

/*----------------------------------------------------------------------
|   NPT_Win32AtomicVariable::GetValue
+---------------------------------------------------------------------*/
int
NPT_Win32AtomicVariable::GetValue()
{
    return m_Value;
}

/*----------------------------------------------------------------------
|   NPT_AtomicVariable::NPT_AtomicVariable
+---------------------------------------------------------------------*/
NPT_AtomicVariable::NPT_AtomicVariable(int value)
{
    m_Delegate = new NPT_Win32AtomicVariable(value);
}

/*----------------------------------------------------------------------
|   NPT_Win32Thread
+---------------------------------------------------------------------*/
class NPT_Win32Thread : public NPT_ThreadInterface
{
 public:
	// class methods
	static NPT_Result SetThreadPriority(HANDLE thread, int priority);
	static NPT_Result GetThreadPriority(HANDLE thread, int& priority);

    // methods
                NPT_Win32Thread(NPT_Thread*   delegator,
                                NPT_Runnable& target,
                                bool          detached);
               ~NPT_Win32Thread();
    NPT_Result  Start(); 
    NPT_Result  Wait(NPT_Timeout timeout = NPT_TIMEOUT_INFINITE);
    NPT_Result  GetPriority(int& priority);
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
|   NPT_Win32Thread::NPT_Win32Thread
+---------------------------------------------------------------------*/
NPT_Win32Thread::NPT_Win32Thread(NPT_Thread*   delegator,
                                 NPT_Runnable& target,
                                 bool          detached) : 
    m_Delegator(delegator),
    m_Target(target),
    m_Detached(detached),
    m_ThreadHandle(0)
{
}

/*----------------------------------------------------------------------
|   NPT_Win32Thread::~NPT_Win32Thread
+---------------------------------------------------------------------*/
NPT_Win32Thread::~NPT_Win32Thread()
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
|   NPT_Win32Thread::SetThreadPriority
+---------------------------------------------------------------------*/
NPT_Result
NPT_Win32Thread::SetThreadPriority(HANDLE thread, int priority)
{
	int win32_priority;
	if (priority < NPT_THREAD_PRIORITY_LOWEST) {
		win32_priority = THREAD_PRIORITY_IDLE;
	} else if (priority < NPT_THREAD_PRIORITY_BELOW_NORMAL) {
		win32_priority = THREAD_PRIORITY_LOWEST;
	} else if (priority < NPT_THREAD_PRIORITY_NORMAL) {
		win32_priority = THREAD_PRIORITY_BELOW_NORMAL;
	} else if (priority < NPT_THREAD_PRIORITY_ABOVE_NORMAL) {
		win32_priority = THREAD_PRIORITY_NORMAL;
	} else if (priority < NPT_THREAD_PRIORITY_HIGHEST) {
		win32_priority = THREAD_PRIORITY_ABOVE_NORMAL;
	} else if (priority < NPT_THREAD_PRIORITY_TIME_CRITICAL) {
		win32_priority = THREAD_PRIORITY_HIGHEST;
	} else {
		win32_priority = THREAD_PRIORITY_TIME_CRITICAL;
	}
	BOOL result = ::SetThreadPriority(thread, win32_priority);
	if (!result) {
		NPT_LOG_WARNING_1("SetThreadPriority failed (%x)", GetLastError());
		return NPT_FAILURE;
	}

	return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Win32Thread::GetThreadPriority
+---------------------------------------------------------------------*/
NPT_Result
NPT_Win32Thread::GetThreadPriority(HANDLE thread, int& priority)
{
    int win32_priority = ::GetThreadPriority(thread);
	if (win32_priority == THREAD_PRIORITY_ERROR_RETURN) {
		NPT_LOG_WARNING_1("GetThreadPriority failed (%x)", GetLastError());
		return NPT_FAILURE;
	}

    priority = win32_priority;
	return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Win32Thread::EntryPoint
+---------------------------------------------------------------------*/
unsigned int __stdcall
NPT_Win32Thread::EntryPoint(void* argument)
{
    NPT_Win32Thread* thread = reinterpret_cast<NPT_Win32Thread*>(argument);

    NPT_LOG_FINER("thread in =======================");

    // set random seed per thread
    NPT_TimeStamp now;
    NPT_System::GetCurrentTimeStamp(now);
    NPT_System::SetRandomSeed((NPT_UInt32)(now.ToNanos()) + ::GetCurrentThreadId());

    // run the thread 
    thread->Run();
    
    // if the thread is detached, delete it
    if (thread->m_Detached) {
        delete thread->m_Delegator;
    }

    // done
    return 0;
}

/*----------------------------------------------------------------------
|   NPT_Win32Thread::Start
+---------------------------------------------------------------------*/
NPT_Result
NPT_Win32Thread::Start()
{
    if (m_ThreadHandle > 0) {
        // failed
        NPT_LOG_WARNING("thread already started !");
        return NPT_ERROR_INVALID_STATE;
    }

    NPT_LOG_FINER("creating thread");

    // create the native thread
#if defined(_WIN32_WCE)
    DWORD thread_id;
#else
    unsigned int thread_id;
#endif
    // create a stack local variable 'detached', as this object
    // may already be deleted when _beginthreadex returns and
    // before we get to call detach on the given thread
    bool detached = m_Detached;

    HANDLE thread_handle = (HANDLE)
        _beginthreadex(NULL, 
                       NPT_CONFIG_THREAD_STACK_SIZE, 
                       EntryPoint, 
                       reinterpret_cast<void*>(this), 
                       0, 
                       &thread_id);
    if (thread_handle == 0) {
        // failed
        return NPT_FAILURE;
    }

    if (detached) {
        CloseHandle(thread_handle);
    } else {
        m_ThreadHandle = thread_handle;
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Win32Thread::Run
+---------------------------------------------------------------------*/
void
NPT_Win32Thread::Run()
{
    m_Target.Run();
}

/*----------------------------------------------------------------------
|   NPT_Win32Thread::SetPriority
+---------------------------------------------------------------------*/
NPT_Result 
NPT_Win32Thread::SetPriority(int priority)
{
	if (m_ThreadHandle == 0) return NPT_ERROR_INVALID_STATE;
	return NPT_Win32Thread::SetThreadPriority(m_ThreadHandle, priority);
}

/*----------------------------------------------------------------------
|   NPT_Win32Thread::GetPriority
+---------------------------------------------------------------------*/
NPT_Result 
NPT_Win32Thread::GetPriority(int& priority)
{
	if (m_ThreadHandle == 0) return NPT_ERROR_INVALID_STATE;
	return NPT_Win32Thread::GetThreadPriority(m_ThreadHandle, priority);
}

/*----------------------------------------------------------------------
|   NPT_Win32Thread::Wait
+---------------------------------------------------------------------*/
NPT_Result
NPT_Win32Thread::Wait(NPT_Timeout timeout /* = NPT_TIMEOUT_INFINITE */)
{
    // check that we're not detached
    if (m_ThreadHandle == 0 || m_Detached) {
        return NPT_FAILURE;
    }

    // wait for the thread to finish
    // Logging here will cause a crash on exit because LogManager may already be destroyed
    DWORD result = WaitForSingleObject(m_ThreadHandle, 
                                       timeout==NPT_TIMEOUT_INFINITE?INFINITE:timeout);
    if (result != WAIT_OBJECT_0) {
        return NPT_FAILURE;
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
	return NPT_Win32Thread::SetThreadPriority(::GetCurrentThread(), priority);
}

/*----------------------------------------------------------------------
|   NPT_Thread::NPT_Thread
+---------------------------------------------------------------------*/
NPT_Thread::NPT_Thread(bool detached)
{
    m_Delegate = new NPT_Win32Thread(this, *this, detached);
}

/*----------------------------------------------------------------------
|   NPT_Thread::NPT_Thread
+---------------------------------------------------------------------*/
NPT_Thread::NPT_Thread(NPT_Runnable& target, bool detached)
{
    m_Delegate = new NPT_Win32Thread(this, target, detached);
}
