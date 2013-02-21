/*****************************************************************
|
|   Platinum - Thread Tasks
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

/** @file
 Runnable Task
 */

#ifndef _PLT_THREADTASK_H_
#define _PLT_THREADTASK_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "PltTaskManager.h"

/*----------------------------------------------------------------------
|   PLT_ThreadTask class
+---------------------------------------------------------------------*/
/**
 The PLT_ThreadTask class is a base class for executing a given task in a worker
 thread. A PLT_ThreadTask is usually always associated to a PLT_TaskManager 
 which maintains a list to stop and destroy tasks when finished.
 */
class PLT_ThreadTask : public NPT_Runnable
{
public:
    friend class PLT_TaskManager;

    /**
     When a task is not managed by a PLT_TaskManager, the owner must call
     this to stop and destroy it.
     */
    NPT_Result Kill();
    
protected:
    /**
     Return whether this task is in the process of stopping.
     @param timeout number of milliseconds to wait
     @return boolean indicating if the task is stopping
     */
    virtual bool IsAborting(NPT_Timeout timeout) {
        return NPT_SUCCEEDED(m_Abort.WaitUntilEquals(1, timeout));
    }
    
    /**
     Start a task by associating it with a task manager.
     @param task_manager PLT_TaskManager pointer
     @param delay optional time interval to wait before launching the new task
     @param auto_destroy a flag to indicate if the task is owned by someone else
     and thus should not destroy itself when done.
     */
    NPT_Result Start(PLT_TaskManager*  task_manager = NULL, 
                     NPT_TimeInterval* delay = NULL,
                     bool              auto_destroy = true);
    /**
     Stop the task. This is either called by a task manager or the Kill method.
     @param blocking Whether the method should block until the task has finished.
     */
    NPT_Result Stop(bool blocking = true);
    
    /**
     This method to override in derived classes is called when the task is about
     to start. 
     */
    virtual void DoInit()    {}
    
    /**
     This method to override in derived classes is called when the task is about
     to stop.
     */
    virtual void DoAbort()   {}
    
    /**
     This method to override in derived classes is the main task loop.
     */
    virtual void DoRun()     {}
    
    /**
     A PLT_ThreadTask base class is never instantiated directly.
     */
    PLT_ThreadTask();
    
    /**
     The task manager will destroy the task when finished if m_AutoDestroy is 
     true otherwise the owner of this task must use the Kill method.
     */
    virtual ~PLT_ThreadTask();
    
private:    
    NPT_Result StartThread();
    
    // NPT_Thread methods
    void Run();

protected:
    // members
    PLT_TaskManager*    m_TaskManager;

private:
    // members
    NPT_SharedVariable  m_Started;
    NPT_SharedVariable  m_Abort;
    NPT_Thread*         m_Thread;
    bool                m_AutoDestroy;
    NPT_TimeInterval    m_Delay;
};

#endif /* _PLT_THREADTASK_H_ */
