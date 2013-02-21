/*****************************************************************
|
|      Threads Test Program 1
|
|      (c) 2001-2002 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include "Neptune.h"

#if defined(WIN32) && defined(_DEBUG)
#include <crtdbg.h>
#endif

#define CHECK(x) {                                  \
    if (!(x)) {                                     \
        NPT_Console::OutputF("TEST FAILED line %d\n", __LINE__);  \
        NPT_ASSERT(0);                              \
    }                                               \
}

/*----------------------------------------------------------------------
|       Thread1
+---------------------------------------------------------------------*/
class Thread1 : public NPT_Thread
{
public:
    virtual ~Thread1() { 
        NPT_Debug("~Thread1\n"); 
    }
    void Run() {
        NPT_Debug("Thread1::Run - start\n");

        // sleep a while
        NPT_TimeInterval duration(1.2f);
        NPT_System::Sleep(duration);       

        NPT_Debug("Thread1::Run - end\n");
    }
};

/*----------------------------------------------------------------------
|       Thread2
+---------------------------------------------------------------------*/
class Thread2 : public NPT_Runnable
{
public:
    Thread2(NPT_SharedVariable* variable) : m_SharedVariable(variable) {}
    virtual ~Thread2() { NPT_Debug("~Thread2\n"); }
    void Run() {
        NPT_Debug("Thread2::Run - start\n");

        // sleep a while
        NPT_TimeInterval duration(2.1f);
        NPT_System::Sleep(duration);

        NPT_Debug("Thread2::Run - waiting for variable == 3\n");
        m_SharedVariable->WaitUntilEquals(3);
        NPT_Debug("Thread2::Run - end\n");
        NPT_Debug("Thread2::Run - deleting myself\n");
        //FIXME: This causes a crash
        //delete this;
    }
    NPT_SharedVariable* m_SharedVariable;
};

/*----------------------------------------------------------------------
|       Thread3
+---------------------------------------------------------------------*/
class Thread3 : public NPT_Thread
{
public:
    Thread3(NPT_SharedVariable* variable) : NPT_Thread(false),
                                           m_SharedVariable(variable) {}
    virtual ~Thread3() { NPT_Debug("~Thread3\n"); }
    Thread3() : NPT_Thread(false) {}
    void Run() {
        NPT_Debug("Thread3::Run - start\n");

        NPT_Thread::SetCurrentThreadPriority(NPT_THREAD_PRIORITY_ABOVE_NORMAL);

        // sleep a while
        NPT_TimeInterval duration(3.1f);
        NPT_System::Sleep(duration);

        NPT_Debug("Thread3::Run - setting shared var to 1\n");
        m_SharedVariable->SetValue(1);

         // sleep a while
        NPT_System::Sleep(duration);

        NPT_Debug("Thread3::Run - setting shared var to 2\n");
        m_SharedVariable->SetValue(2);

         // sleep a while
        NPT_System::Sleep(duration);      

        NPT_Debug("Thread3::Run - setting shared var to 3\n");
        m_SharedVariable->SetValue(3);
        NPT_Debug("Thread3::Run - end\n");
    }
    NPT_SharedVariable* m_SharedVariable;
};

/*----------------------------------------------------------------------
|       Thread4
+---------------------------------------------------------------------*/
class Thread4 : public NPT_Runnable
{
public:
    virtual ~Thread4() { 
        NPT_Debug("~Thread4\n"); 
    }
    void Run() {
        NPT_Debug("Thread4::Run - start\n");

        // change the prio
        NPT_Thread::SetCurrentThreadPriority(NPT_THREAD_PRIORITY_BELOW_NORMAL);
        
        // sleep a while
        NPT_TimeInterval duration(4.3f);
        NPT_System::Sleep(duration);

        NPT_Debug("Thread4::Run - end\n");
    }
};

class T1 : public NPT_Runnable
{
    void Run() {
        NPT_Debug("*** T1 running ***\n");
        NPT_TimeInterval duration(1.0f);
        NPT_Debug("*** T1 sleeping ***\n");
        NPT_System::Sleep(duration);
        NPT_Debug("*** T1 done ***\n");
    }
};

/*----------------------------------------------------------------------
|       TestPrio
+---------------------------------------------------------------------*/
class PrioThread : public NPT_Runnable
{
public:
    PrioThread(int prio) : m_Prio(prio), m_Counter(0) {}
    void Run() {
        NPT_Thread::SetCurrentThreadPriority(m_Prio);
        NPT_TimeStamp now;
        NPT_TimeStamp then;
        NPT_System::GetCurrentTimeStamp(now);
        do {
            for (unsigned int i=0; i<10000; i++) {
                m_Counter++;
            }
            for (unsigned int i=0; i<10000; i++) {
                m_Counter--;
            }
            m_Counter++;
            NPT_System::GetCurrentTimeStamp(then);
        } while (then.ToMillis()-now.ToMillis() < 30000);
    }
    
    int        m_Prio;
    NPT_UInt64 m_Counter;
};
static void
TestPrio()
{
    PrioThread p1(NPT_THREAD_PRIORITY_NORMAL);
    PrioThread p2(NPT_THREAD_PRIORITY_BELOW_NORMAL);
    PrioThread p3(NPT_THREAD_PRIORITY_ABOVE_NORMAL);
    NPT_Thread t1(p1);
    NPT_Thread t2(p2);
    NPT_Thread t3(p3);
    t1.Start();
    t2.Start();
    t3.Start();
    t1.Wait();
    t2.Wait();
    t3.Wait();
    NPT_Debug("### Prio NORMAL       -> %lld iterations\n", p1.m_Counter);
    NPT_Debug("### Prio BELOW NORMAL -> %lld iterations\n", p2.m_Counter);
    NPT_Debug("### Prio ABOVE NORMAL -> %lld iterations\n", p3.m_Counter);
}

/*----------------------------------------------------------------------
|       Test1
+---------------------------------------------------------------------*/
static void
Test1()
{
    NPT_Debug("--- Test1 Start ---\n");
    
    T1 runnable;

    NPT_Debug("+++ creating non-detached thread +++\n");
    NPT_Thread* thread1 = new NPT_Thread(runnable); // not detached
    NPT_Debug("+++ starting non-detached thread +++\n");
    thread1->Start();
    NPT_Debug("+++ waiting for non-detached thread +++\n");
    NPT_Result result = thread1->Wait();
    CHECK(NPT_SUCCEEDED(result));
    NPT_Debug("+++ deleting non-detached thread +++\n");
    delete thread1;
    NPT_Debug("+++ done with non-detached thread +++\n");

    NPT_Debug("+++ creating detached thread +++\n");
    thread1 = new NPT_Thread(runnable, true); // detached
    NPT_Debug("+++ starting detached thread +++\n");
    thread1->Start();
    NPT_Debug("+++ waiting for detached thread +++\n");
    NPT_System::Sleep(NPT_TimeInterval(3.0f));
    //delete thread1;
    NPT_Debug("+++ done with detached thread +++\n");

    NPT_Debug("+++ creating non-detached thread +++\n");
    thread1 = new NPT_Thread(runnable); // not detached
    NPT_Debug("+++ starting non-detached thread +++\n");
    thread1->Start();
    NPT_Debug("+++ deleting non-detached thread +++\n");
    delete thread1;
    NPT_Debug("+++ done with non-detached thread +++\n");

    NPT_Debug("+++ creating detached thread +++\n");
    thread1 = new NPT_Thread(runnable, true); // detached
    NPT_Debug("+++ starting detached thread +++\n");
    thread1->Start();
    NPT_Debug("+++ deleting for detached thread +++\n");
    delete thread1;
    NPT_Debug("+++ done with detached thread +++\n");
}

/*----------------------------------------------------------------------
|       Test2
+---------------------------------------------------------------------*/
static void
Test2()
{
    NPT_Debug("--- Test2 Start ---\n");

    NPT_SharedVariable shv1(0);
    NPT_Thread* thread1 = new Thread1();
    Thread2 t2(&shv1);
    NPT_Thread* thread2 = new NPT_Thread(t2, true);
    NPT_Thread* thread3 = new Thread3(&shv1);
    Thread4 t4;
    NPT_Thread* thread4 = new NPT_Thread(t4, false);

    NPT_Debug("starting thread1...\n");
    thread1->Start();

    NPT_Debug("starting thread2...\n");
    thread2->Start();

    NPT_Debug("starting thread3\n");
    thread3->Start();
    NPT_Debug("releasing thread3\n");
    delete thread3;

    NPT_Debug("starting thread4\n");
    thread4->Start();
    NPT_Debug("deleting thread4\n");
    delete thread4;

    NPT_Debug("deleting thread1...\n");
    delete thread1;
    NPT_Debug("...done\n");

    // sleep a while
    NPT_TimeInterval duration(15.0);
    NPT_System::Sleep(duration);

    NPT_Debug("--- Test2 End ---\n");
}

typedef struct {
    volatile int* var;
    int           var_i;
} _CB_T;

static int _count_CBR[3] = {0,0,0};

class CBR : public NPT_Runnable, public NPT_ThreadCallbackReceiver
{
public:
    CBR(NPT_ThreadCallbackSlot& slot, int var, int cycles, float sleep_time) : m_Slot(slot), m_Var(var), m_FlipFlop(false), m_Cycles(cycles), m_Sleep(sleep_time) {}

    void OnCallback(void* args) {
        _CB_T* t_args = (_CB_T*)args;
        CHECK(*t_args->var == -1);
        (*t_args->var)+= t_args->var_i;
        _count_CBR[m_Var]++;
        m_FlipFlop = true;
        if (m_Sleep != 0.0f) {
            NPT_Debug(".CBR [%d] - on callback (%d)\n", m_Var, _count_CBR[m_Var]);
        }
    }

    void Run() {
        for (int i=0; i<m_Cycles;) {
            if (m_Sleep != 0.0f) {
                NPT_Debug(".CBR [%d] - processing\n", m_Var);
            }
            NPT_Result result = m_Slot.ReceiveCallback(*this);
            if (result == NPT_ERROR_CALLBACK_NOTHING_PENDING) {
                if (m_Sleep != 0.0f) {
                    NPT_Debug(".CBR [%d] - nothing pending\n", m_Var);
                }
            } else {
                CHECK(result == NPT_SUCCESS);
                CHECK(m_FlipFlop == true);
                m_FlipFlop = false;
                i++;
            }
            if (m_Sleep != 0.0f) {
                NPT_Debug(".CBR [%d] - sleeping\n", m_Var);
            }
            if (m_Sleep != 0.0f) NPT_System::Sleep(NPT_TimeInterval(m_Sleep));
        }
    }

private:
    NPT_ThreadCallbackSlot& m_Slot;
    int                     m_Var;
    bool                    m_FlipFlop;
    int                     m_Cycles;
    float                   m_Sleep;
};

class CBW : public NPT_Runnable
{
public:
    CBW(NPT_ThreadCallbackSlot& slot, int var, int cycles, float sleep_time) : m_Slot(slot), m_Var(var), m_Cycles(cycles), m_Sleep(sleep_time) {}

    void Run() {
        volatile int res = -1;
        for (int i=0; i<m_Cycles; i++) {
            _CB_T args = {&res, m_Var+1};
            if (m_Sleep != 0.0f) {
                NPT_Debug("@CBR [%d] - calling back\n", m_Var);
            }
            NPT_Result result = m_Slot.SendCallback(&args);
            if (result == NPT_ERROR_CALLBACK_HANDLER_SHUTDOWN) {
                NPT_Debug("SHUTDOWN\n");
                return;
            }
            CHECK(res == m_Var);
            res -= (m_Var+1);
            if (m_Sleep != 0.0f) {
                NPT_Debug("@CBR [%d] - sleeping\n", m_Var);
            }
            if (m_Sleep != 0.0f) NPT_System::Sleep(NPT_TimeInterval(m_Sleep));
        }
    }

private:
    NPT_ThreadCallbackSlot& m_Slot;
    int                     m_Var;
    int                     m_Cycles;
    float                   m_Sleep;
};

/*----------------------------------------------------------------------
|       Test3
+---------------------------------------------------------------------*/
static void
Test3(int cycles, float r_sleep, float w_sleep)
{
    NPT_Debug("--- Test3 Start ---\n");

    NPT_ThreadCallbackSlot slot;

    CBR cbr0(slot, 0, cycles, r_sleep);
    CBR cbr1(slot, 1, cycles, r_sleep);
    CBR cbr2(slot, 2, cycles, r_sleep);
    NPT_Thread* rt1 = new NPT_Thread(cbr0);
    NPT_Thread* rt2 = new NPT_Thread(cbr1);
    NPT_Thread* rt3 = new NPT_Thread(cbr2);
    CBW cbw0(slot, 0, cycles, w_sleep);
    CBW cbw1(slot, 1, cycles, w_sleep);
    CBW cbw2(slot, 2, cycles, w_sleep);
    NPT_Thread* wt1 = new NPT_Thread(cbw0);
    NPT_Thread* wt2 = new NPT_Thread(cbw1);
    NPT_Thread* wt3 = new NPT_Thread(cbw2);

    rt1->Start();
    rt2->Start();
    rt3->Start();
    wt1->Start();
    wt2->Start();
    wt3->Start();

    delete rt1;
    delete rt2;
    delete rt3;
    delete wt1;
    delete wt2;
    delete wt3;

    NPT_Debug("--- Test3: %d %d %d\n", _count_CBR[0], _count_CBR[1], _count_CBR[2]);
    NPT_Debug("--- Test3 End ---\n");
}

class CBR2 : public NPT_Runnable, public NPT_ThreadCallbackReceiver
{
public:
    CBR2(NPT_ThreadCallbackSlot& slot) : m_Slot(slot) {}

    void OnCallback(void*) {
        NPT_Debug("CBR2 - on callback\n");
    }

    void Run() {
        for (int i=0; i<10;) {
            NPT_Debug("CBR2: processing [%d]\n", i);
            NPT_Result result = m_Slot.ReceiveCallback(*this);
            if (result == NPT_ERROR_CALLBACK_NOTHING_PENDING) {
            } else {
                i++;
            }
            NPT_Debug("CBR2: sleeping\n");
            NPT_System::Sleep(NPT_TimeInterval(0.2f));
        }
        NPT_Debug("CBR2: shutting down\n");
        m_Slot.Shutdown();
    }

private:
    NPT_ThreadCallbackSlot& m_Slot;
};

/*----------------------------------------------------------------------
|       Test4
+---------------------------------------------------------------------*/
static void
Test4()
{
    NPT_Debug("--- Test4 Start ---\n");

    NPT_ThreadCallbackSlot slot;

    CBR2 cbr(slot);
    NPT_Thread* t = new NPT_Thread(cbr);
    t->Start();

    for (int i=0; i<20; i++) {
        NPT_Debug("Test4: calling back [%d]\n", i);
        NPT_Result result = slot.SendCallback(NULL);
        if (NPT_FAILED(result)) {
            CHECK(result == NPT_ERROR_CALLBACK_HANDLER_SHUTDOWN);
            CHECK(i >= 10);
            NPT_Debug("Test4: slot shutdown\n");
        }
    }
    delete t;

    NPT_Debug("--- Test4 End ---\n");
}

/*----------------------------------------------------------------------
|       TestSharedVariables
+---------------------------------------------------------------------*/
class SharedVarThread : public NPT_Thread {
public:
    SharedVarThread(int target, NPT_SharedVariable& shared) : m_Target(target), m_Shared(shared), m_Result(NPT_FAILURE) {}
    void Run() {
        m_Result = m_Shared.WaitUntilEquals(m_Target, 10000);
    }
    
    int                 m_Target;
    NPT_SharedVariable& m_Shared;
    NPT_Result          m_Result;
};

static void
TestSharedVariables()
{
    NPT_SharedVariable shared;
    SharedVarThread t1(1, shared);
    SharedVarThread t2(2, shared);
    SharedVarThread t3(2, shared);
    
    t1.Start();
    t2.Start();
    t3.Start();
    NPT_System::Sleep(3.0);
    shared.SetValue(1);
    NPT_System::Sleep(2.0);
    shared.SetValue(2);
    
    NPT_Result result = t1.Wait(20000);
    CHECK(result == NPT_SUCCESS);
    CHECK(t1.m_Result == NPT_SUCCESS);
    result = t2.Wait(20000);
    CHECK(result == NPT_SUCCESS);
    CHECK(t2.m_Result == NPT_SUCCESS);
    result = t3.Wait(20000);
    CHECK(result == NPT_SUCCESS);
    CHECK(t3.m_Result == NPT_SUCCESS);
}

#if defined(WIN32) && defined(_DEBUG)
static int AllocHook( int allocType, void *userData, size_t size, int blockType, 
                     long requestNumber, const unsigned char *filename, int lineNumber)
{
    (void)allocType;
    (void)userData;
    (void)size;
    (void)blockType;
    (void)requestNumber;
    (void)lineNumber;
    (void)filename;
    return 1;
}
#endif

/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int argc, char** argv) 
{
    NPT_COMPILER_UNUSED(argc);
    NPT_COMPILER_UNUSED(argv);

#if defined(WIN32) && defined(_DEBUG)
    _CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF    |
                   _CRTDBG_CHECK_ALWAYS_DF |
                   _CRTDBG_LEAK_CHECK_DF);
    _CrtSetAllocHook(AllocHook);
#endif
    
    TestSharedVariables();
    TestPrio();
    Test3(100000, 0.0f, 0.0f);
    Test3(300, 0.1f, 0.0f);
    Test3(100, 0.5f, 0.4f);
    Test4();
    Test1();
    Test2();

    NPT_Debug("- program done -\n");

    return 0;
}






