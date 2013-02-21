/*****************************************************************
|
|      Queue Test Program 1
|
|      (c) 2008 Gilles Boccon-Gibod
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
        printf("TEST FAILED line %d\n", __LINE__);  \
        NPT_ASSERT(0);                              \
    }                                               \
}

/*----------------------------------------------------------------------
|       Item
+---------------------------------------------------------------------*/
class Item
{
public:
    typedef enum {
        MSG_INCREMENT_COUNTER,
        MSG_CHANGE_TIMEOUT,
        MSG_TERMINATE
    } Message;
    
    Item(Message msg) : m_Message(msg) {}
    
    Message m_Message;
};

/*----------------------------------------------------------------------
|       WriterThread
+---------------------------------------------------------------------*/
class WriterThread : public NPT_Thread
{
public:
    WriterThread(NPT_Queue<Item>& queue, const char* name) : 
        m_Queue(queue), m_Name(name), m_Counter(0) {}
    
    void Run() {
        NPT_Debug("WRITER %s starting ++++++++++++++++++++++++\n", (const char*)m_Name);

        for (int i=0; i<1000; i++) {
            if (i%5 == 0) {
                NPT_Debug("WRITER %s post: change timeout\n", (const char*)m_Name);
                m_Queue.Push(new Item(Item::MSG_CHANGE_TIMEOUT));
            } 
            m_Queue.Push(new Item(Item::MSG_INCREMENT_COUNTER));
            if (i%3 == 0) {
                NPT_Debug("WRITER %s sleeping\n", (const char*)m_Name);
                NPT_System::Sleep(NPT_TimeInterval(0.01f));
            }
        }

        NPT_Debug("WRITER %s terminating ----------------------\n", (const char*)m_Name);
    }
    
    NPT_Queue<Item>& m_Queue;
    NPT_String       m_Name;
    unsigned int     m_Counter;
};

/*----------------------------------------------------------------------
|       ReaderThread
+---------------------------------------------------------------------*/
class ReaderThread : public NPT_Thread
{
public:
    ReaderThread(NPT_Queue<Item>& queue, const char* name, NPT_TimeInterval sleep_time) : 
        m_Queue(queue), m_Name(name), m_Counter(0), m_NbTimeouts(0), m_SleepTime(sleep_time) {}
    
    void Run() {
        NPT_Debug("READER %s starting =====================\n", (const char*)m_Name);
        NPT_Timeout timeout = NPT_TIMEOUT_INFINITE;
        for (;;) {
            if (m_SleepTime.ToNanos() != 0) {
                NPT_Debug("READER %s sleeping...\n", (const char*)m_Name);
                NPT_System::Sleep(m_SleepTime);
                NPT_Debug("READER %s woke up!\n", (const char*)m_Name);
            }
            
            Item* item = NULL;
            NPT_Result result = m_Queue.Pop(item, timeout);
            if (NPT_SUCCEEDED(result)) {
                CHECK(item != NULL);
                Item::Message msg = item->m_Message;
                delete item;
                switch (msg) {
                    case Item::MSG_INCREMENT_COUNTER:
                        ++m_Counter;
                        NPT_Debug("READER %s new counter=%d\n", (const char*)m_Name, m_Counter);
                        break;
                        
                    case Item::MSG_CHANGE_TIMEOUT:
                        if (timeout == 0) {
                            timeout = 15;
                        } else if (timeout == 15) {
                            timeout = NPT_TIMEOUT_INFINITE;
                        } else {
                            timeout = 0;
                        }
                        NPT_Debug("READER %s new timeout=%d\n", (const char*)m_Name, timeout);
                        break;
                        
                    case Item::MSG_TERMINATE:
                        NPT_Debug("READER %s terminating #######################\n", (const char*)m_Name);
                        return;
                }
            } else {
                NPT_Debug("READER %s pop returned %d\n", (const char*)m_Name, result);
                if (timeout == 0) {
                    CHECK(result == NPT_ERROR_LIST_EMPTY);
                    NPT_System::Sleep(0.01f);
                } else if (timeout != NPT_TIMEOUT_INFINITE) {
                    CHECK(result == NPT_ERROR_TIMEOUT);
                    ++m_NbTimeouts;
                } else {
                    NPT_ASSERT(0);
                }
            }
        }
    }

    NPT_Queue<Item>& m_Queue;
    NPT_String       m_Name;
    unsigned int     m_Counter;
    unsigned int     m_NbTimeouts;
    NPT_TimeInterval m_SleepTime;
};

/*----------------------------------------------------------------------
|       Test1
+---------------------------------------------------------------------*/
static void
Test1()
{
    // create a queue
    NPT_Queue<Item> queue;
    
    // create 2 writers
    NPT_Debug("creating writer 1\n");
    WriterThread writer1(queue, "1"); writer1.Start();
    NPT_System::Sleep(NPT_TimeInterval(0.3f));

    NPT_Debug("creating writer 2\n");
    WriterThread writer2(queue, "2"); writer2.Start();
    NPT_System::Sleep(NPT_TimeInterval(0.3f));

    // create 4 readers
    NPT_Debug("creating reader 1\n");
    ReaderThread reader1(queue, "1", NPT_TimeInterval(0.0f)); reader1.Start();
    NPT_System::Sleep(NPT_TimeInterval(0.3f));

    NPT_Debug("creating reader 2\n");
    ReaderThread reader2(queue, "2", NPT_TimeInterval(0.0f)); reader2.Start();
    NPT_System::Sleep(NPT_TimeInterval(0.3f));

    NPT_Debug("creating reader 3\n");
    ReaderThread reader3(queue, "3", NPT_TimeInterval(0.0f)); reader3.Start();
    NPT_System::Sleep(NPT_TimeInterval(0.3f));

    NPT_Debug("creating reader 4\n");
    ReaderThread reader4(queue, "4", NPT_TimeInterval(0.0f)); reader4.Start();
    NPT_System::Sleep(NPT_TimeInterval(0.3f));


    // wait for the writers
    NPT_Result result;
    NPT_Debug("Waiting for Writer 1 *********************\n");
    result = writer1.Wait();
    NPT_Debug("Writer 1 done *********************\n");
    CHECK(result == NPT_SUCCESS);
    NPT_Debug("Waiting for Writer 2 *********************\n");
    result = writer2.Wait();
    NPT_Debug("Writer 1 done *********************\n");

    // post 4 termination messages
    queue.Push(new Item(Item::MSG_TERMINATE));
    queue.Push(new Item(Item::MSG_TERMINATE));
    queue.Push(new Item(Item::MSG_TERMINATE));
    queue.Push(new Item(Item::MSG_TERMINATE));

    // wait for the readers
    CHECK(result == NPT_SUCCESS);
    NPT_Debug("Waiting for Reader 1 *********************\n");
    result = reader1.Wait();
    NPT_Debug("Reader 1 done *********************\n");
    CHECK(result == NPT_SUCCESS);
    NPT_Debug("Waiting for Reader 2 *********************\n");
    result = reader2.Wait();
    NPT_Debug("Reader 2 done *********************\n");
    CHECK(result == NPT_SUCCESS);
    NPT_Debug("Waiting for Reader 3 *********************\n");
    result = reader3.Wait();
    NPT_Debug("Reader 3 done *********************\n");
    CHECK(result == NPT_SUCCESS);
    NPT_Debug("Waiting for Reader 4 *********************\n");
    result = reader4.Wait();    
    NPT_Debug("Reader 4 done *********************\n");
    CHECK(result == NPT_SUCCESS);
    
    // check counters
    unsigned int total = reader1.m_Counter+reader2.m_Counter+reader3.m_Counter+reader4.m_Counter;
    CHECK(total == 2*1000);
}

/*----------------------------------------------------------------------
|       Test2
+---------------------------------------------------------------------*/
static void
Test2()
{
    // create a queue
    NPT_Queue<Item> queue;
    
    // create 2 readers
    NPT_Debug("creating reader 1\n");
    ReaderThread reader1(queue, "1", NPT_TimeInterval(0.05f)); reader1.Start();

    NPT_Debug("creating reader 2\n");
    ReaderThread reader2(queue, "2", NPT_TimeInterval(0.065f)); reader2.Start();

    for (int i=0; i<30; i++) {
        queue.Push(new Item(Item::MSG_INCREMENT_COUNTER));
    }
    queue.Push(new Item(Item::MSG_TERMINATE));
    for (int i=0; i<30; i++) {
        queue.Push(new Item(Item::MSG_INCREMENT_COUNTER));
    }
    queue.Push(new Item(Item::MSG_TERMINATE));
    
    NPT_Result result;
    NPT_Debug("Waiting for Reader 1 *********************\n");
    result = reader1.Wait();
    NPT_Debug("Reader 1 done *********************\n");
    CHECK(result == NPT_SUCCESS);
    NPT_Debug("Waiting for Reader 1 *********************\n");
    result = reader2.Wait();
    NPT_Debug("Reader 2 done *********************\n");
    CHECK(result == NPT_SUCCESS);
    
    // check counters
    unsigned int total = reader1.m_Counter+reader2.m_Counter;
    CHECK(total == 60);
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

    Test1();
    Test2();
    
    NPT_Debug("- program done -\n");

    return 0;
}






