/*****************************************************************
|
|      Neptune - Threads :: Posix Implementation
|
|      (c) 2001-2002 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#if defined(__SYMBIAN32__)
#include <stdio.h>
#endif
#include <pthread.h>
#include <unistd.h>
#include <time.h>
#include <sys/time.h>
#include <errno.h>

#include "NptConfig.h"
#include "NptTypes.h"
#include "NptThreads.h"
#include "NptLogging.h"
#include "NptTime.h"
#include "NptSystem.h"
#include "NptUtils.h"
#include "NptAutoreleasePool.h"

/*----------------------------------------------------------------------
|       logging
+---------------------------------------------------------------------*/
NPT_SET_LOCAL_LOGGER("neptune.threads.posix")

/*----------------------------------------------------------------------
|       NPT_PosixMutex
+---------------------------------------------------------------------*/
class NPT_PosixMutex : public NPT_MutexInterface
{
public:
    // methods
             NPT_PosixMutex();
    virtual ~NPT_PosixMutex();

    // NPT_Mutex methods
    virtual NPT_Result Lock();
    virtual NPT_Result Unlock();

private:
    // members
    pthread_mutex_t m_Mutex;
};

/*----------------------------------------------------------------------
|       NPT_PosixMutex::NPT_PosixMutex
+---------------------------------------------------------------------*/
NPT_PosixMutex::NPT_PosixMutex()
{
    // Recursive by default
    pthread_mutexattr_t attr;
    pthread_mutexattr_init(&attr);
    pthread_mutexattr_settype(&attr, PTHREAD_MUTEX_RECURSIVE);

    pthread_mutex_init(&m_Mutex, &attr);
}

/*----------------------------------------------------------------------
|       NPT_PosixMutex::~NPT_PosixMutex
+---------------------------------------------------------------------*/
NPT_PosixMutex::~NPT_PosixMutex()
{
    pthread_mutex_destroy(&m_Mutex);
}

/*----------------------------------------------------------------------
|       NPT_PosixMutex::Lock
+---------------------------------------------------------------------*/
NPT_Result
NPT_PosixMutex::Lock()
{
    pthread_mutex_lock(&m_Mutex);
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|       NPT_PosixMutex::Unlock
+---------------------------------------------------------------------*/
NPT_Result
NPT_PosixMutex::Unlock()
{
    pthread_mutex_unlock(&m_Mutex);
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|       NPT_Mutex::NPT_Mutex
+---------------------------------------------------------------------*/
NPT_Mutex::NPT_Mutex()
{
    m_Delegate = new NPT_PosixMutex();
}

/*----------------------------------------------------------------------
|       NPT_PosixSharedVariable
+---------------------------------------------------------------------*/
class NPT_PosixSharedVariable : public NPT_SharedVariableInterface
{
public:
    // methods
               NPT_PosixSharedVariable(int value);
              ~NPT_PosixSharedVariable();
    void       SetValue(int value);
    int        GetValue();
    NPT_Result WaitUntilEquals(int value, NPT_Timeout timeout);
    NPT_Result WaitWhileEquals(int value, NPT_Timeout timeout);

 private:
    // members
    volatile int    m_Value;
    pthread_mutex_t m_Mutex;
    pthread_cond_t  m_Condition;
};

/*----------------------------------------------------------------------
|       NPT_PosixSharedVariable::NPT_PosixSharedVariable
+---------------------------------------------------------------------*/
NPT_PosixSharedVariable::NPT_PosixSharedVariable(int value) : 
    m_Value(value)
{
    pthread_mutex_init(&m_Mutex, NULL);
    pthread_cond_init(&m_Condition, NULL);
}

/*----------------------------------------------------------------------
|       NPT_PosixSharedVariable::~NPT_PosixSharedVariable
+---------------------------------------------------------------------*/
NPT_PosixSharedVariable::~NPT_PosixSharedVariable()
{
    pthread_cond_destroy(&m_Condition);
    pthread_mutex_destroy(&m_Mutex);
}

/*----------------------------------------------------------------------
|       NPT_PosixSharedVariable::SetValue
+---------------------------------------------------------------------*/
void
NPT_PosixSharedVariable::SetValue(int value)
{
    pthread_mutex_lock(&m_Mutex);
    m_Value = value;
    pthread_cond_broadcast(&m_Condition);
    pthread_mutex_unlock(&m_Mutex);
}

/*----------------------------------------------------------------------
|       NPT_PosixSharedVariable::GetValue
+---------------------------------------------------------------------*/
int
NPT_PosixSharedVariable::GetValue()
{
    // we assume that int read/write are atomic on the platform
    return m_Value;
}

/*----------------------------------------------------------------------
|       NPT_PosixSharedVariable::WaitUntilEquals
+---------------------------------------------------------------------*/
NPT_Result
NPT_PosixSharedVariable::WaitUntilEquals(int value, NPT_Timeout timeout)
{
    NPT_Result result = NPT_SUCCESS;
    struct     timespec timed;

    if (timeout != NPT_TIMEOUT_INFINITE) {
        // get current time from system
        struct timeval now;
        if (gettimeofday(&now, NULL)) {
            return NPT_FAILURE;
        }

        now.tv_usec += timeout * 1000;
        if (now.tv_usec >= 1000000) {
            now.tv_sec += now.tv_usec / 1000000;
            now.tv_usec = now.tv_usec % 1000000;
        }

        // setup timeout
        timed.tv_sec  = now.tv_sec;
        timed.tv_nsec = now.tv_usec * 1000;
    }
    
    pthread_mutex_lock(&m_Mutex);
    while (value != m_Value) {
        if (timeout == NPT_TIMEOUT_INFINITE) {
            pthread_cond_wait(&m_Condition, &m_Mutex);
        } else {
            int wait_res = pthread_cond_timedwait(&m_Condition, &m_Mutex, &timed);
            if (wait_res == ETIMEDOUT) {
                result = NPT_ERROR_TIMEOUT;
                break;
            }
        }
    }
    pthread_mutex_unlock(&m_Mutex);
    
    return result;
}

/*----------------------------------------------------------------------
|       NPT_PosixSharedVariable::WaitWhileEquals
+---------------------------------------------------------------------*/
NPT_Result
NPT_PosixSharedVariable::WaitWhileEquals(int value, NPT_Timeout timeout)
{
    NPT_Result result = NPT_SUCCESS;
    struct     timespec timed;

    if (timeout != NPT_TIMEOUT_INFINITE) {
        // get current time from system
        struct timeval now;
        if (gettimeofday(&now, NULL)) {
            return NPT_FAILURE;
        }

        now.tv_usec += timeout * 1000;
        if (now.tv_usec >= 1000000) {
            now.tv_sec += now.tv_usec / 1000000;
            now.tv_usec = now.tv_usec % 1000000;
        }

        // setup timeout
        timed.tv_sec  = now.tv_sec;
        timed.tv_nsec = now.tv_usec * 1000;
    }
    
    pthread_mutex_lock(&m_Mutex);
    while (value == m_Value) {
        if (timeout == NPT_TIMEOUT_INFINITE) {
            pthread_cond_wait(&m_Condition, &m_Mutex);
        } else {
            int wait_res = pthread_cond_timedwait(&m_Condition, &m_Mutex, &timed);
            if (wait_res == ETIMEDOUT) {
                result = NPT_ERROR_TIMEOUT;
                break;
            }
        }
    }
    pthread_mutex_unlock(&m_Mutex);
    
    return result;
}

/*----------------------------------------------------------------------
|       NPT_SharedVariable::NPT_SharedVariable
+---------------------------------------------------------------------*/
NPT_SharedVariable::NPT_SharedVariable(int value)
{
    m_Delegate = new NPT_PosixSharedVariable(value);
}

/*----------------------------------------------------------------------
|       NPT_PosixAtomicVariable
+---------------------------------------------------------------------*/
class NPT_PosixAtomicVariable : public NPT_AtomicVariableInterface
{
 public:
    // methods
         NPT_PosixAtomicVariable(int value);
        ~NPT_PosixAtomicVariable();
    int  Increment(); 
    int  Decrement();
    int  GetValue();
    void SetValue(int value);

 private:
    // members
    volatile int    m_Value;
    pthread_mutex_t m_Mutex;
};

/*----------------------------------------------------------------------
|       NPT_PosixAtomicVariable::NPT_PosixAtomicVariable
+---------------------------------------------------------------------*/
NPT_PosixAtomicVariable::NPT_PosixAtomicVariable(int value) : 
    m_Value(value)
{
    pthread_mutex_init(&m_Mutex, NULL);
}

/*----------------------------------------------------------------------
|       NPT_PosixAtomicVariable::~NPT_PosixAtomicVariable
+---------------------------------------------------------------------*/
NPT_PosixAtomicVariable::~NPT_PosixAtomicVariable()
{
    pthread_mutex_destroy(&m_Mutex);
}

/*----------------------------------------------------------------------
|       NPT_PosixAtomicVariable::Increment
+---------------------------------------------------------------------*/
int
NPT_PosixAtomicVariable::Increment()
{
    int value;

    pthread_mutex_lock(&m_Mutex);
    value = ++m_Value;
    pthread_mutex_unlock(&m_Mutex);
    
    return value;
}

/*----------------------------------------------------------------------
|       NPT_PosixAtomicVariable::Decrement
+---------------------------------------------------------------------*/
int
NPT_PosixAtomicVariable::Decrement()
{
    int value;

    pthread_mutex_lock(&m_Mutex);
    value = --m_Value;
    pthread_mutex_unlock(&m_Mutex);
    
    return value;
}

/*----------------------------------------------------------------------
|       NPT_PosixAtomicVariable::GetValue
+---------------------------------------------------------------------*/
int
NPT_PosixAtomicVariable::GetValue()
{
    // we assume that int read/write are atomic on the platform
    return m_Value;
}

/*----------------------------------------------------------------------
|       NPT_PosixAtomicVariable::SetValue
+---------------------------------------------------------------------*/
void
NPT_PosixAtomicVariable::SetValue(int value)
{
    pthread_mutex_lock(&m_Mutex);
    m_Value = value;
    pthread_mutex_unlock(&m_Mutex);
}

/*----------------------------------------------------------------------
|       NPT_AtomicVariable::NPT_AtomicVariable
+---------------------------------------------------------------------*/
NPT_AtomicVariable::NPT_AtomicVariable(int value)
{
    m_Delegate = new NPT_PosixAtomicVariable(value);
}

/*----------------------------------------------------------------------
|       NPT_PosixThread
+---------------------------------------------------------------------*/
class NPT_PosixThread : public NPT_ThreadInterface
{
 public:
    // methods
                NPT_PosixThread(NPT_Thread*   delegator,
                                NPT_Runnable& target,
                                bool          detached);
               ~NPT_PosixThread();
    NPT_Result  Start(); 
    NPT_Result  Wait(NPT_Timeout timeout = NPT_TIMEOUT_INFINITE);
    NPT_Result  SetPriority(int priority);
    NPT_Result  GetPriority(int& priority);
    
    // class methods
    static NPT_Result GetPriority(NPT_Thread::ThreadId thread_id, int& priority);
    static NPT_Result SetPriority(NPT_Thread::ThreadId thread_id, int priority);

 private:
    // class methods
    static void* EntryPoint(void* argument);

    // NPT_Runnable methods
    void Run();

    // NPT_Interruptible methods
    NPT_Result Interrupt() { return NPT_ERROR_NOT_IMPLEMENTED; }

    // members
    NPT_Thread*        m_Delegator;
    NPT_Runnable&      m_Target;
    bool               m_Detached;
    pthread_t          m_ThreadId;
    bool               m_Joined;
    NPT_PosixMutex     m_JoinLock;
    NPT_SharedVariable m_Done;
};

/*----------------------------------------------------------------------
|       NPT_PosixThread::NPT_PosixThread
+---------------------------------------------------------------------*/
NPT_PosixThread::NPT_PosixThread(NPT_Thread*   delegator,
                                 NPT_Runnable& target,
                                 bool          detached) : 
    m_Delegator(delegator),
    m_Target(target),
    m_Detached(detached),
    m_ThreadId(0),
    m_Joined(false)
{
    NPT_LOG_FINE("NPT_PosixThread::NPT_PosixThread");
    m_Done.SetValue(0);
}

/*----------------------------------------------------------------------
|       NPT_PosixThread::~NPT_PosixThread
+---------------------------------------------------------------------*/
NPT_PosixThread::~NPT_PosixThread()
{
    //NPT_Debug("NPT_PosixThread::~NPT_PosixThread %d\n", m_ThreadId);

    if (!m_Detached) {
        // we're not detached, and not in the Run() method, so we need to 
        // wait until the thread is done
        Wait();
    }
}

/*----------------------------------------------------------------------
|   NPT_Thread::GetCurrentThreadId
+---------------------------------------------------------------------*/
NPT_Thread::ThreadId 
NPT_Thread::GetCurrentThreadId()
{
    pthread_t pid = pthread_self();
    return (NPT_Thread::ThreadId)((void*)pid);
}

/*----------------------------------------------------------------------
|   NPT_Thread::SetCurrentThreadPriority
+---------------------------------------------------------------------*/
NPT_Result
NPT_Thread::SetCurrentThreadPriority(int priority)
{
    return NPT_PosixThread::SetPriority(GetCurrentThreadId(), priority);
}

/*----------------------------------------------------------------------
|   NPT_Thread::GetCurrentThreadPriority
+---------------------------------------------------------------------*/
NPT_Result
NPT_Thread::GetCurrentThreadPriority(int& priority)
{
    return NPT_PosixThread::GetPriority(GetCurrentThreadId(), priority);
}

/*----------------------------------------------------------------------
|       NPT_PosixThread::EntryPoint
+---------------------------------------------------------------------*/
void*
NPT_PosixThread::EntryPoint(void* argument)
{
    NPT_PosixThread* thread = reinterpret_cast<NPT_PosixThread*>(argument);

    NPT_LOG_FINER("NPT_PosixThread::EntryPoint - in =======================");

#if defined(NPT_CONFIG_HAVE_AUTORELEASE_POOL)
    // ensure there is the top level autorelease pool for each thread
    NPT_AutoreleasePool pool;
#endif
    
    // get the thread ID from this context, because m_ThreadId may not yet
    // have been set by the parent thread in the Start() method
    thread->m_ThreadId = pthread_self();
    
    // set random seed per thread
    NPT_TimeStamp now;
    NPT_System::GetCurrentTimeStamp(now);
    NPT_System::SetRandomSeed((unsigned int)(now.ToNanos() + (long)thread->m_ThreadId));

    // run the thread 
    thread->Run();
    
    // Logging here will cause a crash on exit because LogManager may already be destroyed    
    //NPT_LOG_FINE("NPT_PosixThread::EntryPoint - out ======================");

    // we're done with the thread object
    // if we're detached, we need to delete ourselves
    if (thread->m_Detached) {
        delete thread->m_Delegator;
    } else {
        // notify we're done
        thread->m_Done.SetValue(1);
    }

    // done
    return NULL;
}

/*----------------------------------------------------------------------
|       NPT_PosixThread::Start
+---------------------------------------------------------------------*/
NPT_Result
NPT_PosixThread::Start()
{
    NPT_LOG_FINER("---- Creating thread");
    
    // reset values
    m_Joined = false;
    m_ThreadId = 0;
    m_Done.SetValue(0);

    pthread_attr_t *attributes = NULL;

#if defined(NPT_CONFIG_THREAD_STACK_SIZE)
    pthread_attr_t stack_size_attributes;
    pthread_attr_init(&stack_size_attributes);
    pthread_attr_setstacksize(&stack_size_attributes, NPT_CONFIG_THREAD_STACK_SIZE);
    attributes = &stack_size_attributes;
#endif

    // use local copies of some of the object's members, because for
    // detached threads, the object instance may have deleted itself
    // before the pthread_create() function returns
    bool detached = m_Detached;

    // create the native thread
    pthread_t thread_id;
    int result = pthread_create(&thread_id, attributes, EntryPoint, 
                                static_cast<NPT_PosixThread*>(this));
    NPT_LOG_FINER_2("---- Thread Created: id = %d, res=%d", 
                   thread_id, result);
    if (result != 0) {
        // failed
        return NPT_ERROR_ERRNO(result);
    } else {
        // detach the thread if we're not joinable
        if (detached) {
            pthread_detach(thread_id);
        } else {
            // store the thread ID (NOTE: this is also done by the thread Run() method
            // but it is necessary to do it from both contexts, because we don't know
            // which one will need it first.)
            m_ThreadId = thread_id;        
        } 
        return NPT_SUCCESS;
    }
}

/*----------------------------------------------------------------------
|       NPT_PosixThread::Run
+---------------------------------------------------------------------*/
void
NPT_PosixThread::Run()
{
    m_Target.Run();
}

/*----------------------------------------------------------------------
|       NPT_PosixThread::Wait
+---------------------------------------------------------------------*/
NPT_Result
NPT_PosixThread::Wait(NPT_Timeout timeout /* = NPT_TIMEOUT_INFINITE */)
{
    void* return_value;
    int   result;

    // Note: Logging here will cause a crash on exit because LogManager may already be destroyed
    // if this object is global or static as well
    //NPT_LOG_FINE_1("NPT_PosixThread::Wait - waiting for id %d", m_ThreadId);

    // check that we're not detached
    if (m_ThreadId == 0 || m_Detached) {
        return NPT_FAILURE;
    }

    // wait for the thread to finish
    m_JoinLock.Lock();
    if (m_Joined) {
        result = 0;
    } else {
        if (timeout != NPT_TIMEOUT_INFINITE) {
            if (NPT_FAILED(m_Done.WaitUntilEquals(1, timeout))) {
                result = -1;
                goto timedout;
            }
        }

        result = pthread_join(m_ThreadId, &return_value);
        m_Joined = true;
    }

timedout:
    m_JoinLock.Unlock();
    if (result != 0) {
        return NPT_FAILURE;
    } else {
        return NPT_SUCCESS;
    }
}

/*----------------------------------------------------------------------
|       NPT_PosixThread::SetPriority
+---------------------------------------------------------------------*/
NPT_Result
NPT_PosixThread::SetPriority(int priority)
{
    // check that we're started
    if (m_ThreadId == 0) {
        return NPT_FAILURE;
    }
    
    return NPT_PosixThread::SetPriority((NPT_Thread::ThreadId)m_ThreadId, priority);
}

/*----------------------------------------------------------------------
|       NPT_PosixThread::SetPriority
+---------------------------------------------------------------------*/
NPT_Result
NPT_PosixThread::SetPriority(NPT_Thread::ThreadId thread_id, int priority)
{
    // check that we're started
    if (thread_id == 0) {
        return NPT_FAILURE;
    }
    
    /* sched_priority will be the priority of the thread */
    struct sched_param sp;
    int policy;
    int result = pthread_getschedparam((pthread_t)thread_id, &policy, &sp);
    
    NPT_LOG_FINER_3("Current thread policy: %d, priority: %d, new priority: %d", 
                   policy, sp.sched_priority, priority);
    NPT_LOG_FINER_4("Thread max(SCHED_OTHER): %d, max(SCHED_RR): %d \
                   min(SCHED_OTHER): %d, min(SCHED_RR): %d",
                   sched_get_priority_max(SCHED_OTHER),
                   sched_get_priority_max(SCHED_RR),
                   sched_get_priority_min(SCHED_OTHER),
                   sched_get_priority_min(SCHED_RR));
    
    sp.sched_priority = priority;
    
    /*
    if (sp.sched_priority <= 0)
        sp.sched_priority += sched_get_priority_max (policy = SCHED_OTHER);
    else
        sp.sched_priority += sched_get_priority_min (policy = SCHED_RR);
     */
    
    /* scheduling parameters of target thread */
    result = pthread_setschedparam((pthread_t)thread_id, policy, &sp);
    
    return (result==0)?NPT_SUCCESS:NPT_ERROR_ERRNO(result);
}

/*----------------------------------------------------------------------
|       NPT_PosixThread::GetPriority
+---------------------------------------------------------------------*/
NPT_Result
NPT_PosixThread::GetPriority(int& priority)
{
    // check that we're started
    if (m_ThreadId == 0) {
        return NPT_FAILURE;
    }
    
    return NPT_PosixThread::GetPriority((NPT_Thread::ThreadId)m_ThreadId, priority);
}

/*----------------------------------------------------------------------
|       NPT_PosixThread::GetPriority
+---------------------------------------------------------------------*/
NPT_Result
NPT_PosixThread::GetPriority(NPT_Thread::ThreadId thread_id, int& priority)
{
    // check that we're started
    if (thread_id == 0) {
        return NPT_FAILURE;
    }
 
    struct sched_param sp;
    int policy;

    int result = pthread_getschedparam((pthread_t)thread_id, &policy, &sp);
    NPT_LOG_FINER_1("Current thread priority: %d", sp.sched_priority);
    
    priority = sp.sched_priority;
    return (result==0)?NPT_SUCCESS:NPT_ERROR_ERRNO(result);
}

/*----------------------------------------------------------------------
|       NPT_Thread::NPT_Thread
+---------------------------------------------------------------------*/
NPT_Thread::NPT_Thread(bool detached)
{
    m_Delegate = new NPT_PosixThread(this, *this, detached);
}

/*----------------------------------------------------------------------
|       NPT_Thread::NPT_Thread
+---------------------------------------------------------------------*/
NPT_Thread::NPT_Thread(NPT_Runnable& target, bool detached)
{
    m_Delegate = new NPT_PosixThread(this, target, detached);
}

