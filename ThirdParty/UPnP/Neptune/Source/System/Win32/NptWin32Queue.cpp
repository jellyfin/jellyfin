/*****************************************************************
|
|   Neptune - Queue :: Win32 Implementation
|
|   (c) 2001-2002 Gilles Boccon-Gibod
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
#endif

#include "NptConfig.h"
#include "NptTypes.h"
#include "NptQueue.h"
#include "NptThreads.h"
#include "NptList.h"
#include "NptDebug.h"
#include "NptWin32Threads.h"
#include "NptLogging.h"

/*----------------------------------------------------------------------
|   logging
+---------------------------------------------------------------------*/
//NPT_SET_LOCAL_LOGGER("neptune.queue.win32")

/*----------------------------------------------------------------------
|   NPT_Win32Queue
+---------------------------------------------------------------------*/
class NPT_Win32Queue : public NPT_GenericQueue
{
public:
    // methods
               NPT_Win32Queue(NPT_Cardinal max_items);
              ~NPT_Win32Queue();
    NPT_Result Push(NPT_QueueItem* item, NPT_Timeout timeout); 
    NPT_Result Pop(NPT_QueueItem*& item, NPT_Timeout timeout);
    NPT_Result Peek(NPT_QueueItem*& item, NPT_Timeout timeout);


private:
    // members
    NPT_Cardinal             m_MaxItems;
    NPT_Win32CriticalSection m_Mutex;
    NPT_Win32Event*          m_CanPushCondition;
    NPT_Win32Event*          m_CanPopCondition;
    NPT_List<NPT_QueueItem*> m_Items; // should be volatile ?
};

/*----------------------------------------------------------------------
|   NPT_Win32Queue::NPT_Win32Queue
+---------------------------------------------------------------------*/
NPT_Win32Queue::NPT_Win32Queue(NPT_Cardinal max_items) : 
    m_MaxItems(max_items)
{
    m_CanPushCondition = new NPT_Win32Event(true, true);
    m_CanPopCondition  = new NPT_Win32Event(true, false);
}

/*----------------------------------------------------------------------
|   NPT_Win32Queue::~NPT_Win32Queue()
+---------------------------------------------------------------------*/
NPT_Win32Queue::~NPT_Win32Queue()
{
    // destroy resources
    delete m_CanPushCondition;
    delete m_CanPopCondition;
}

/*----------------------------------------------------------------------
|   NPT_Win32Queue::Push
+---------------------------------------------------------------------*/
NPT_Result
NPT_Win32Queue::Push(NPT_QueueItem* item, NPT_Timeout timeout)
{
    // lock the mutex that protects the list
    NPT_CHECK(m_Mutex.Lock());

    // check that we have not exceeded the max
    if (m_MaxItems) {
        while (m_Items.GetItemCount() >= m_MaxItems) {
            // we must wait until some items have been removed

            // reset the condition to indicate that the queue is full
            m_CanPushCondition->Reset();

            // unlock the mutex so that another thread can pop
            m_Mutex.Unlock();

            // wait for the condition to signal that we can push
            NPT_Result result = m_CanPushCondition->Wait(timeout);
            if (NPT_FAILED(result)) return result;

            // relock the mutex so that we can check the list again
            NPT_CHECK(m_Mutex.Lock());
        }
    }

    // add the item to the list
    m_Items.Add(item);

    // wake up the threads waiting to pop
    m_CanPopCondition->Signal();

    // unlock the mutex
    m_Mutex.Unlock();

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Win32Queue::Pop
+---------------------------------------------------------------------*/
NPT_Result
NPT_Win32Queue::Pop(NPT_QueueItem*& item, NPT_Timeout timeout)
{
    // default value
    item = NULL;
    
    // lock the mutex that protects the list
    NPT_CHECK(m_Mutex.Lock());

    NPT_Result result;
    if (timeout) {
        while ((result = m_Items.PopHead(item)) == NPT_ERROR_LIST_EMPTY) {
            // no item in the list, wait for one

            // reset the condition to indicate that the queue is empty
            m_CanPopCondition->Reset();

            // unlock the mutex so that another thread can push
            m_Mutex.Unlock();

            // wait for the condition to signal that we can pop
            NPT_Result result = m_CanPopCondition->Wait(timeout);
            if (NPT_FAILED(result)) return result;

            // relock the mutex so that we can check the list again
            NPT_CHECK(m_Mutex.Lock());
        }
    } else {
        result = m_Items.PopHead(item);
    }
    
    if (m_MaxItems && (result == NPT_SUCCESS)) {
        // wake up the threads waiting to push
        m_CanPushCondition->Signal();
    }

    // unlock the mutex
    m_Mutex.Unlock();
 
    return result;
}

/*----------------------------------------------------------------------
|   NPT_Win32Queue::Peek
+---------------------------------------------------------------------*/
NPT_Result
NPT_Win32Queue::Peek(NPT_QueueItem*& item, NPT_Timeout timeout)
{
    // default value
    item = NULL;
    
    // lock the mutex that protects the list
    NPT_CHECK(m_Mutex.Lock());

    NPT_Result result = NPT_SUCCESS;
    NPT_List<NPT_QueueItem*>::Iterator head = m_Items.GetFirstItem();
    if (timeout) {
        while (!head) {
            // no item in the list, wait for one

            // reset the condition to indicate that the queue is empty
            m_CanPopCondition->Reset();

            // unlock the mutex so that another thread can push
            m_Mutex.Unlock();

            // wait for the condition to signal that we can pop
            NPT_Result result = m_CanPopCondition->Wait(timeout);
            if (NPT_FAILED(result)) return result;

            // relock the mutex so that we can check the list again
            NPT_CHECK(m_Mutex.Lock());

            // try again
            head = m_Items.GetFirstItem();
        }
    } else {
        if (!head) result = NPT_ERROR_LIST_EMPTY;
    }

    if (head) item = *head;

    // unlock the mutex
    m_Mutex.Unlock();

    return result;
}

/*----------------------------------------------------------------------
|   NPT_GenericQueue::CreateInstance
+---------------------------------------------------------------------*/
NPT_GenericQueue*
NPT_GenericQueue::CreateInstance(NPT_Cardinal max_items)
{
    return new NPT_Win32Queue(max_items);
}

