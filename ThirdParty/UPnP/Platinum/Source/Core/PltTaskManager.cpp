/*****************************************************************
|
|   Platinum - Task Manager
|
| Copyright (c) 2004-2010, Plutinosoft, LLC.
| All rights reserved.
| http://www.plutinosoft.com
|
| This program is free software; you can redistribute it and/or
| modify it under the terms of the GNU General Public License
| as published by the Free Software Foundation; either version 2
| of the License, or (at your option) any later version.
|
| OEMs, ISVs, VARs and other distributors that combine and 
| distribute commercially licensed software with Platinum software
| and do not wish to distribute the source code for the commercially
| licensed software under version 2, or (at your option) any later
| version, of the GNU General Public License (the "GPL") must enter
| into a commercial license agreement with Plutinosoft, LLC.
| licensing@plutinosoft.com
|  
| This program is distributed in the hope that it will be useful,
| but WITHOUT ANY WARRANTY; without even the implied warranty of
| MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
| GNU General Public License for more details.
|
| You should have received a copy of the GNU General Public License
| along with this program; see the file LICENSE.txt. If not, write to
| the Free Software Foundation, Inc., 
| 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
| http://www.gnu.org/licenses/gpl-2.0.html
|
****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "PltTaskManager.h"
#include "PltThreadTask.h"

NPT_SET_LOCAL_LOGGER("platinum.core.taskmanager")

/*----------------------------------------------------------------------
|   PLT_TaskManager::PLT_TaskManager
+---------------------------------------------------------------------*/
PLT_TaskManager::PLT_TaskManager(NPT_Cardinal max_items /* = 0 */) :
    m_Queue(NULL),
    m_MaxTasks(max_items),
    m_RunningTasks(0),
    m_Stopping(false)
{
}

/*----------------------------------------------------------------------
|   PLT_TaskManager::~PLT_TaskManager
+---------------------------------------------------------------------*/
PLT_TaskManager::~PLT_TaskManager()
{    
    StopAllTasks();
}

/*----------------------------------------------------------------------
|   PLT_TaskManager::StartTask
+---------------------------------------------------------------------*/
NPT_Result 
PLT_TaskManager::StartTask(PLT_ThreadTask*   task, 
                           NPT_TimeInterval* delay /* = NULL*/,
                           bool              auto_destroy /* = true */)
{
    NPT_CHECK_POINTER_SEVERE(task);
    return task->Start(this, delay, auto_destroy);
}

/*----------------------------------------------------------------------
|   PLT_TaskManager::StopAllTasks
+---------------------------------------------------------------------*/
NPT_Result
PLT_TaskManager::StopAllTasks()
{
    NPT_Cardinal num_running_tasks;
    
    do {
        {
            NPT_AutoLock lock(m_TasksLock);
            
            m_Stopping = true;
            
            // unblock the queue if any by deleting it
            if (m_Queue) {
                NPT_Queue<int>* queue = m_Queue;
                m_Queue = NULL;
                delete queue;
            }
        }

        // abort all running tasks
        {
            NPT_AutoLock lock(m_TasksLock);
        
            NPT_List<PLT_ThreadTask*>::Iterator task = m_Tasks.GetFirstItem();
            while (task) {
                // stop task if it's not already stopping
                if (!(*task)->IsAborting(0)) {
                    (*task)->Stop(false);
                }
                ++task;
            }
            
            num_running_tasks = m_Tasks.GetItemCount();
        }

        if (num_running_tasks == 0) 
            break; 
        
        NPT_System::Sleep(NPT_TimeInterval(0.05));
    } while (1);
    
    m_Stopping = false;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_TaskManager::AddTask
+---------------------------------------------------------------------*/
NPT_Result 
PLT_TaskManager::AddTask(PLT_ThreadTask* task) 
{
    NPT_Result result = NPT_SUCCESS;

    // verify we're not stopping or maxed out number of running tasks
    do {
        m_TasksLock.Lock();
        
        // returning an error if we're stopping
        // NOTE: this could leak the task if not handled by caller properly
        if (m_Stopping) {
            m_TasksLock.Unlock();
            NPT_CHECK_WARNING(NPT_ERROR_INTERRUPTED);
        }
        
        if (m_MaxTasks) {
            if (!m_Queue) m_Queue = new NPT_Queue<int>(m_MaxTasks);

            // try to add to queue but don't block forever if queue is full
            result = m_Queue->Push(new int, 100);
            if (NPT_SUCCEEDED(result)) break;

            // release lock if it's a failure
            // this gives a chance for the taskmanager
            // to abort the queue if full
            m_TasksLock.Unlock();

            // if it failed due to something other than a timeout
            // it probably means the queue is aborting
            if (result != NPT_ERROR_TIMEOUT) {
                NPT_CHECK_WARNING(result);
            }
        }
    } while (result == NPT_ERROR_TIMEOUT);

    // start task now
    if (NPT_FAILED(result = task->StartThread())) {
        m_TasksLock.Unlock();
        NPT_CHECK_WARNING(result);
    }

    NPT_LOG_FINER_3("[TaskManager 0x%08x] %d/%d running tasks", this, ++m_RunningTasks, m_MaxTasks);

    // keep track of running task
    result = m_Tasks.Add(task);

    m_TasksLock.Unlock();
    return result;
}

/*----------------------------------------------------------------------
|   PLT_TaskManager::RemoveTask
+---------------------------------------------------------------------*/
// called by a PLT_ThreadTask::Run when done
NPT_Result
PLT_TaskManager::RemoveTask(PLT_ThreadTask* task)
{
    NPT_Result result = NPT_SUCCESS;
    
    {
        NPT_AutoLock lock(m_TasksLock);
        
        if (m_Queue) {
            int* val = NULL;
            result = m_Queue->Pop(val, 100);
            
            // if for some reason the queue is empty, don't block forever
            if (NPT_SUCCEEDED(result)) {
                delete val;
            } else {
                NPT_LOG_WARNING_1("Failed to pop task from queue %d", result);
            }
        }
        
        NPT_LOG_FINER_3("[TaskManager 0x%08x] %d/%d running tasks", this, --m_RunningTasks, m_MaxTasks);
        m_Tasks.Remove(task);
    }
    
    // cleanup task only if auto-destroy flag was set
    // otherwise it's the owner's responsability to
    // clean it up
    if (task->m_AutoDestroy) delete task;

    return NPT_SUCCESS;
}
