/*****************************************************************
|
|      Neptune - Queue :: Posix Implementation
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
#include <time.h>
#include <sys/time.h>
#include <errno.h>

#include "NptConfig.h"
#include "NptTypes.h"
#include "NptQueue.h"
#include "NptThreads.h"
#include "NptList.h"
#include "NptLogging.h"

/*----------------------------------------------------------------------
|       logging
+---------------------------------------------------------------------*/
NPT_SET_LOCAL_LOGGER("neptune.queue.posix")

/*----------------------------------------------------------------------
|       NPT_PosixQueue
+---------------------------------------------------------------------*/
class NPT_PosixQueue : public NPT_GenericQueue
{
public:
    // methods
               NPT_PosixQueue(NPT_Cardinal max_items);
              ~NPT_PosixQueue();
    NPT_Result Push(NPT_QueueItem* item, NPT_Timeout timeout); 
    NPT_Result Pop(NPT_QueueItem*& item, NPT_Timeout timeout);
    NPT_Result Peek(NPT_QueueItem*& item, NPT_Timeout timeout);

private:
    void       Abort();
    NPT_Result GetTimeOut(NPT_Timeout timeout, struct timespec& timed);
    
private:
    // members
    NPT_Cardinal             m_MaxItems;
    pthread_mutex_t          m_Mutex;
    pthread_cond_t           m_CanPushCondition;
    pthread_cond_t           m_CanPopCondition;
    NPT_Cardinal             m_PushersWaitingCount;
    NPT_Cardinal             m_PoppersWaitingCount;
    NPT_List<NPT_QueueItem*> m_Items;
    bool                     m_Aborting;
};

/*----------------------------------------------------------------------
|       NPT_PosixQueue::NPT_PosixQueue
+---------------------------------------------------------------------*/
NPT_PosixQueue::NPT_PosixQueue(NPT_Cardinal max_items) : 
    m_MaxItems(max_items), 
    m_PushersWaitingCount(0),
    m_PoppersWaitingCount(0),
    m_Aborting(false)
{
    pthread_mutex_init(&m_Mutex, NULL);
    pthread_cond_init(&m_CanPushCondition, NULL);
    pthread_cond_init(&m_CanPopCondition, NULL);
}

/*----------------------------------------------------------------------
|       NPT_PosixQueue::~NPT_PosixQueue()
+---------------------------------------------------------------------*/
NPT_PosixQueue::~NPT_PosixQueue()
{
    Abort();
    
    // destroy resources
    pthread_cond_destroy(&m_CanPushCondition);
    pthread_cond_destroy(&m_CanPopCondition);
    pthread_mutex_destroy(&m_Mutex);
}

/*----------------------------------------------------------------------
|       NPT_PosixQueue::Abort
+---------------------------------------------------------------------*/
void
NPT_PosixQueue::Abort()
{
    pthread_cond_t abort_condition;
    pthread_cond_init(&abort_condition, NULL);

    struct timespec timed;
    GetTimeOut(20, timed);

    // acquire mutex
    if (pthread_mutex_lock(&m_Mutex)) {
        return;
    }

    // tell other threads that they should exit immediately
    m_Aborting = true;

    // notify clients
    pthread_cond_broadcast(&m_CanPopCondition);
    pthread_cond_broadcast(&m_CanPushCondition);

    // wait for all waiters to exit
    while (m_PoppersWaitingCount > 0 && m_PushersWaitingCount > 0) {
        pthread_cond_timedwait(&abort_condition,
                               &m_Mutex,
                               &timed);
    }
    
    pthread_mutex_unlock(&m_Mutex);
}

/*----------------------------------------------------------------------
|       NPT_PosixQueue::GetTimeOut
+---------------------------------------------------------------------*/
NPT_Result
NPT_PosixQueue::GetTimeOut(NPT_Timeout timeout, struct timespec& timed)
{
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
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|       NPT_PosixQueue::Push
+---------------------------------------------------------------------*/
NPT_Result
NPT_PosixQueue::Push(NPT_QueueItem* item, NPT_Timeout timeout)
{
    struct timespec timed;
    if (timeout != NPT_TIMEOUT_INFINITE) {
        NPT_CHECK(GetTimeOut(timeout, timed));
    }

    // lock the mutex that protects the list
    if (pthread_mutex_lock(&m_Mutex)) {
        return NPT_FAILURE;
    }

    NPT_Result result = NPT_SUCCESS;
    // check that we have not exceeded the max
    if (m_MaxItems) {
        while (m_Items.GetItemCount() >= m_MaxItems) {
            // wait until we can push
            ++m_PushersWaitingCount;
            if (timeout == NPT_TIMEOUT_INFINITE) {
                pthread_cond_wait(&m_CanPushCondition, &m_Mutex);
                --m_PushersWaitingCount;
            } else {
                int wait_res = pthread_cond_timedwait(&m_CanPushCondition, 
                                                      &m_Mutex, 
                                                      &timed);
                --m_PushersWaitingCount;
                if (wait_res == ETIMEDOUT) {
                    result = NPT_ERROR_TIMEOUT;
                    break;
                }
            }

            if (m_Aborting) {
                result = NPT_ERROR_INTERRUPTED;
                break;
            }
        }
    }

    // add the item to the list
    if (result == NPT_SUCCESS) {
        m_Items.Add(item);

        // wake up any thread that may be waiting to pop
        if (m_PoppersWaitingCount) { 
            pthread_cond_broadcast(&m_CanPopCondition);
        }
    }

    // unlock the mutex
    pthread_mutex_unlock(&m_Mutex);

    return result;
}

/*----------------------------------------------------------------------
|       NPT_PosixQueue::Pop
+---------------------------------------------------------------------*/
NPT_Result
NPT_PosixQueue::Pop(NPT_QueueItem*& item, NPT_Timeout timeout)
{
    struct timespec timed;
    if (timeout != NPT_TIMEOUT_INFINITE) {
        NPT_CHECK(GetTimeOut(timeout, timed));
    }

    // lock the mutex that protects the list
    if (pthread_mutex_lock(&m_Mutex)) {
        return NPT_FAILURE;
    }

    NPT_Result result;
    if (timeout) {
        while ((result = m_Items.PopHead(item)) == NPT_ERROR_LIST_EMPTY) {
            // no item in the list, wait for one
            ++m_PoppersWaitingCount;
            if (timeout == NPT_TIMEOUT_INFINITE) {
                pthread_cond_wait(&m_CanPopCondition, &m_Mutex);
                --m_PoppersWaitingCount;
            } else {
                int wait_res = pthread_cond_timedwait(&m_CanPopCondition, 
                                                      &m_Mutex, 
                                                      &timed);
                --m_PoppersWaitingCount;
                if (wait_res == ETIMEDOUT) {
                    result = NPT_ERROR_TIMEOUT;
                    break;
                }
            }
            
            if (m_Aborting) {
                result = NPT_ERROR_INTERRUPTED;
                break;
            }
        }
    } else {
        result = m_Items.PopHead(item);
    }
    
    // wake up any thread that my be waiting to push
    if (m_MaxItems && (result == NPT_SUCCESS) && m_PushersWaitingCount) {
        pthread_cond_broadcast(&m_CanPushCondition);
    }

    // unlock the mutex
    pthread_mutex_unlock(&m_Mutex);
 
    return result;
}

/*----------------------------------------------------------------------
|       NPT_PosixQueue::Peek
+---------------------------------------------------------------------*/
NPT_Result
NPT_PosixQueue::Peek(NPT_QueueItem*& item, NPT_Timeout timeout)
{
    struct timespec timed;
    if (timeout != NPT_TIMEOUT_INFINITE) {
        NPT_CHECK(GetTimeOut(timeout, timed));
    }

    // lock the mutex that protects the list
    if (pthread_mutex_lock(&m_Mutex)) {
        return NPT_FAILURE;
    }

    NPT_Result result = NPT_SUCCESS;
    NPT_List<NPT_QueueItem*>::Iterator head = m_Items.GetFirstItem();
    if (timeout) {
        while (!head) {
            // no item in the list, wait for one
            ++m_PoppersWaitingCount;
            if (timeout == NPT_TIMEOUT_INFINITE) {
                pthread_cond_wait(&m_CanPopCondition, &m_Mutex);
                --m_PoppersWaitingCount;
            } else {
                int wait_res = pthread_cond_timedwait(&m_CanPopCondition, 
                                                      &m_Mutex, 
                                                      &timed);
                --m_PoppersWaitingCount;
                if (wait_res == ETIMEDOUT) {
                    result = NPT_ERROR_TIMEOUT;
                    break;
                }
            }

            if (m_Aborting) {
                result = NPT_ERROR_INTERRUPTED;
                break;
            }

            head = m_Items.GetFirstItem();
        }
    } else {
        if (!head) result = NPT_ERROR_LIST_EMPTY;
    }

    item = head?*head:NULL;

    // unlock the mutex
    pthread_mutex_unlock(&m_Mutex);

    return result;
}

/*----------------------------------------------------------------------
|       NPT_GenericQueue::CreateInstance
+---------------------------------------------------------------------*/
NPT_GenericQueue*
NPT_GenericQueue::CreateInstance(NPT_Cardinal max_items)
{
    NPT_LOG_FINER_1("queue max_items = %ld", max_items);
    return new NPT_PosixQueue(max_items);
}




