/*****************************************************************
|
|   Neptune - Threads
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

#ifndef _NPT_THREADS_H_
#define _NPT_THREADS_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptTypes.h"
#include "NptConstants.h"
#include "NptInterfaces.h"

/*----------------------------------------------------------------------
|   error codes
+---------------------------------------------------------------------*/
const int NPT_ERROR_CALLBACK_HANDLER_SHUTDOWN = NPT_ERROR_BASE_THREADS-0;
const int NPT_ERROR_CALLBACK_NOTHING_PENDING  = NPT_ERROR_BASE_THREADS-1;

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
const int NPT_THREAD_PRIORITY_MIN           = -15;
const int NPT_THREAD_PRIORITY_IDLE          = -15;
const int NPT_THREAD_PRIORITY_LOWEST        =  -2;
const int NPT_THREAD_PRIORITY_BELOW_NORMAL  =  -1;
const int NPT_THREAD_PRIORITY_NORMAL        =   0;
const int NPT_THREAD_PRIORITY_ABOVE_NORMAL  =   1;
const int NPT_THREAD_PRIORITY_HIGHEST       =   2;
const int NPT_THREAD_PRIORITY_TIME_CRITICAL =  15;
const int NPT_THREAD_PRIORITY_MAX           =  15;

/*----------------------------------------------------------------------
|   NPT_MutexInterface
+---------------------------------------------------------------------*/
class NPT_MutexInterface
{
 public:
    // methods
    virtual           ~NPT_MutexInterface() {}
    virtual NPT_Result Lock()   = 0;
    virtual NPT_Result Unlock() = 0;
};

/*----------------------------------------------------------------------
|   NPT_Mutex
+---------------------------------------------------------------------*/
class NPT_Mutex : public NPT_MutexInterface
{
 public:
    // methods
               NPT_Mutex();
              ~NPT_Mutex() { delete m_Delegate; }
    NPT_Result Lock()   { return m_Delegate->Lock();   }
    NPT_Result Unlock() { return m_Delegate->Unlock(); }

 private:
    // members
    NPT_MutexInterface* m_Delegate;
};

/*----------------------------------------------------------------------
|   NPT_AutoLock
+---------------------------------------------------------------------*/
class NPT_AutoLock
{
 public:
    // methods
     NPT_AutoLock(NPT_Mutex &mutex) : m_Mutex(mutex)   {
        m_Mutex.Lock();
    }
    ~NPT_AutoLock() {
        m_Mutex.Unlock(); 
    }
        
 private:
    // members
    NPT_Mutex& m_Mutex;
};

/*----------------------------------------------------------------------
|   NPT_Lock
+---------------------------------------------------------------------*/
template <typename T> 
class NPT_Lock : public T,
                 public NPT_Mutex
{
};

/*----------------------------------------------------------------------
|   NPT_SingletonLock
+---------------------------------------------------------------------*/
class NPT_SingletonLock
{
public:
    static NPT_Mutex& GetInstance() {
        return Instance;
    }
    
private:
    static NPT_Mutex Instance;
};

/*----------------------------------------------------------------------
|   NPT_SharedVariableInterface
+---------------------------------------------------------------------*/
class NPT_SharedVariableInterface
{
 public:
    // methods
    virtual           ~NPT_SharedVariableInterface() {}
    virtual void       SetValue(int value)= 0;
    virtual int        GetValue()         = 0;
    virtual NPT_Result WaitUntilEquals(int value, NPT_Timeout timeout = NPT_TIMEOUT_INFINITE) = 0;
    virtual NPT_Result WaitWhileEquals(int value, NPT_Timeout timeout = NPT_TIMEOUT_INFINITE) = 0;
};

/*----------------------------------------------------------------------
|   NPT_SharedVariable
+---------------------------------------------------------------------*/
class NPT_SharedVariable : public NPT_SharedVariableInterface
{
 public:
    // methods
               NPT_SharedVariable(int value = 0);
              ~NPT_SharedVariable() { delete m_Delegate; }
    void SetValue(int value) { 
        m_Delegate->SetValue(value); 
    }
    int GetValue() { 
        return m_Delegate->GetValue(); 
    }
    NPT_Result WaitUntilEquals(int value, NPT_Timeout timeout = NPT_TIMEOUT_INFINITE) { 
        return m_Delegate->WaitUntilEquals(value, timeout); 
    }
    NPT_Result WaitWhileEquals(int value, NPT_Timeout timeout = NPT_TIMEOUT_INFINITE) { 
        return m_Delegate->WaitWhileEquals(value, timeout); 
    }

 private:
    // members
    NPT_SharedVariableInterface* m_Delegate;
};

/*----------------------------------------------------------------------
|   NPT_AtomicVariableInterface
+---------------------------------------------------------------------*/
class NPT_AtomicVariableInterface
{
 public:
    // methods
    virtual      ~NPT_AtomicVariableInterface() {}
    virtual  int  Increment() = 0;
    virtual  int  Decrement() = 0;
    virtual  int  GetValue()  = 0;
    virtual  void SetValue(int value)  = 0;
};

/*----------------------------------------------------------------------
|   NPT_AtomicVariable
+---------------------------------------------------------------------*/
class NPT_AtomicVariable : public NPT_AtomicVariableInterface
{
 public:
    // methods
         NPT_AtomicVariable(int value = 0);
        ~NPT_AtomicVariable() { delete m_Delegate;             }
    int  Increment()          { return m_Delegate->Increment();}
    int  Decrement()          { return m_Delegate->Decrement();}
    void SetValue(int value)  { m_Delegate->SetValue(value);   }
    int  GetValue()           { return m_Delegate->GetValue(); }

 private:
    // members
    NPT_AtomicVariableInterface* m_Delegate;
};

/*----------------------------------------------------------------------
|   NPT_Runnable
+---------------------------------------------------------------------*/
class NPT_Runnable
{
public:
    virtual ~NPT_Runnable() {}  
    virtual void Run() = 0;
};

/*----------------------------------------------------------------------
|   NPT_ThreadInterface
+---------------------------------------------------------------------*/
class NPT_ThreadInterface: public NPT_Runnable, public NPT_Interruptible
{
 public:
    // methods
    virtual           ~NPT_ThreadInterface() {}
    virtual NPT_Result Start() = 0;
    virtual NPT_Result Wait(NPT_Timeout timeout = NPT_TIMEOUT_INFINITE) = 0;
    virtual NPT_Result SetPriority(int /*priority*/) { return NPT_SUCCESS; } 
    virtual NPT_Result GetPriority(int& priority) = 0;
};

/*----------------------------------------------------------------------
|   NPT_Thread
+---------------------------------------------------------------------*/
class NPT_Thread : public NPT_ThreadInterface
{
 public:
    // types
    typedef unsigned long ThreadId;

    // class methods
    static ThreadId GetCurrentThreadId();
    static NPT_Result SetCurrentThreadPriority(int priority);
    static NPT_Result GetCurrentThreadPriority(int& priority);

    // methods
    explicit NPT_Thread(bool detached = false);
    explicit NPT_Thread(NPT_Runnable& target, bool detached = false);
   ~NPT_Thread() { delete m_Delegate; }

    // NPT_ThreadInterface methods
    NPT_Result Start() { 
        return m_Delegate->Start(); 
    } 
    NPT_Result Wait(NPT_Timeout timeout = NPT_TIMEOUT_INFINITE)  { 
        return m_Delegate->Wait(timeout);  
    }
    NPT_Result SetPriority(int priority) {
        return m_Delegate->SetPriority(priority);
    }    
    NPT_Result GetPriority(int& priority) {
        return m_Delegate->GetPriority(priority);
    }

    // NPT_Runnable methods
    virtual void Run() {}

    // NPT_Interruptible methods
    virtual NPT_Result Interrupt() { return m_Delegate->Interrupt(); }

 private:
    // members
    NPT_ThreadInterface* m_Delegate;
};


/*----------------------------------------------------------------------
|   NPT_ThreadCallbackReceiver
+---------------------------------------------------------------------*/
class NPT_ThreadCallbackReceiver
{
public:
    virtual ~NPT_ThreadCallbackReceiver() {}
    virtual void OnCallback(void* args) = 0;
};

/*----------------------------------------------------------------------
|   NPT_ThreadCallbackSlot
+---------------------------------------------------------------------*/
class NPT_ThreadCallbackSlot
{
public:
    // types
    class NotificationHelper {
    public:
        virtual ~NotificationHelper() {};
        virtual void Notify(void) = 0;
    };

    // constructor
    NPT_ThreadCallbackSlot();

    // methods
    NPT_Result ReceiveCallback(NPT_ThreadCallbackReceiver& receiver, NPT_Timeout timeout = 0);
    NPT_Result SendCallback(void* args);
    NPT_Result SetNotificationHelper(NotificationHelper* helper);
    NPT_Result Shutdown();

protected:
    // members
    volatile void*      m_CallbackArgs;
    volatile bool       m_Shutdown;
    NPT_SharedVariable  m_Pending;
    NPT_SharedVariable  m_Ack;
    NPT_Mutex           m_ReadLock;
    NPT_Mutex           m_WriteLock;
    NotificationHelper* m_NotificationHelper;
};

#endif // _NPT_THREADS_H_
