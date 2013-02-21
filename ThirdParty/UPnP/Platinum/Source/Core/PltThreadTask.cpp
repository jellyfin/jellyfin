/*****************************************************************
|
|   Platinum - Tasks
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
#include "PltThreadTask.h"
#include "PltTaskManager.h" 

NPT_SET_LOCAL_LOGGER("platinum.core.threadtask")

/*----------------------------------------------------------------------
|   PLT_ThreadTask::PLT_ThreadTask
+---------------------------------------------------------------------*/
PLT_ThreadTask::PLT_ThreadTask() :
    m_TaskManager(NULL),
    m_Thread(NULL),
    m_AutoDestroy(false)
{
}

/*----------------------------------------------------------------------
|   PLT_ThreadTask::~PLT_ThreadTask
+---------------------------------------------------------------------*/
PLT_ThreadTask::~PLT_ThreadTask()
{
    if (!m_AutoDestroy) delete m_Thread;
}

/*----------------------------------------------------------------------
|   PLT_ThreadTask::Start
+---------------------------------------------------------------------*/
NPT_Result
PLT_ThreadTask::Start(PLT_TaskManager*  task_manager,/* = NULL */
                      NPT_TimeInterval* delay,       /* = NULL */
                      bool              auto_destroy /* = true */)
{
    m_Abort.SetValue(0);
    m_AutoDestroy = auto_destroy;
    m_Delay       = delay?*delay:NPT_TimeStamp(0.);
    m_TaskManager = task_manager;
    
    if (m_TaskManager) {
        NPT_CHECK_SEVERE(m_TaskManager->AddTask(this));
        return NPT_SUCCESS;
    } else {
        return StartThread();
    }
}

/*----------------------------------------------------------------------
|   PLT_ThreadTask::StartThread
+---------------------------------------------------------------------*/
NPT_Result
PLT_ThreadTask::StartThread()
{
    m_Started.SetValue(0);
    
    m_Thread = new NPT_Thread((NPT_Runnable&)*this, m_AutoDestroy);
    NPT_CHECK_SEVERE(m_Thread->Start());
    
    return m_Started.WaitUntilEquals(1, NPT_TIMEOUT_INFINITE);
}

/*----------------------------------------------------------------------
|   PLT_ThreadTask::Stop
+---------------------------------------------------------------------*/
NPT_Result
PLT_ThreadTask::Stop(bool blocking /* = true */)
{
    // keep variable around in case
    // we get destroyed
    bool auto_destroy = m_AutoDestroy;
    
    // tell thread we want to die
    m_Abort.SetValue(1);
    DoAbort();
    
    // return without waiting if non blocking or not started
    if (!blocking || !m_Thread) return NPT_SUCCESS;

    // if auto-destroy, the thread may be already dead by now 
    // so we can't wait on m_Thread.
    // only Task Manager will know when task is finished
    return auto_destroy?NPT_FAILURE:m_Thread->Wait();
}

/*----------------------------------------------------------------------
|   PLT_ThreadTask::Kill
+---------------------------------------------------------------------*/
NPT_Result
PLT_ThreadTask::Kill()
{
    Stop();

    // A task can only be destroyed manually
    // when the m_AutoDestroy is false
    // otherwise the Task Manager takes
    // care of deleting it when the thread exits
    NPT_ASSERT(m_AutoDestroy == false);
    if (!m_AutoDestroy) delete this;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_ThreadTask::Run
+---------------------------------------------------------------------*/
void
PLT_ThreadTask::Run() 
{
    m_Started.SetValue(1);
    
    // wait before starting task if necessary
    if ((float)m_Delay > 0.f) {
        // more than 100ms, loop so we can abort it
        if ((float)m_Delay > 0.1f) {
            NPT_TimeStamp start, now;
            NPT_System::GetCurrentTimeStamp(start);
            do {
                NPT_System::GetCurrentTimeStamp(now);
                if (now >= start + m_Delay) break;
            } while (!IsAborting(100));
        } else {
            NPT_System::Sleep(m_Delay);
        }
    }

    // loop
    if (!IsAborting(0))  {
        DoInit();
        DoRun();
    }

    // notify the Task Manager we're done
    // it will destroy us if m_AutoDestroy is true
    if (m_TaskManager) {
        m_TaskManager->RemoveTask(this);
    } else if (m_AutoDestroy) {
        // destroy ourselves otherwise
        delete this;
    }
}
