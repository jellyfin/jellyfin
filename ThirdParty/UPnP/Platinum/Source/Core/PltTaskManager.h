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

/** @file
 Runnable Tasks Manager
 */

#ifndef _PLT_TASKMANAGER_H_
#define _PLT_TASKMANAGER_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"

/*----------------------------------------------------------------------
|   forward declarations
+---------------------------------------------------------------------*/
class PLT_ThreadTask;

/*----------------------------------------------------------------------
|   PLT_TaskManager class
+---------------------------------------------------------------------*/
/**
 The PLT_TaskManager class maintains a list of runnable tasks. During shutdown, it
 can stop all running tasks. Additionally, it can limit the number of
 tasks that can run at any given time.
 */
class PLT_TaskManager
{
public:
    /**
     Create a new Task Manager.
     @param max_items Maximum number of concurrent tasks that the task manager
     will allow. When the value is reached, a thread calling AddTask will block until
     a task has finished.
     */
	PLT_TaskManager(NPT_Cardinal max_tasks = 0);
	virtual ~PLT_TaskManager();

    /**
     Start a new new task and associates it with this task manager.
     @param task new task
     @param delay optional time interval to wait before launching the new task
     @param auto_destroy a flag to indicate if the task is owned by someone else
     and thus should not destroy itself when done.
     */
    virtual NPT_Result StartTask(PLT_ThreadTask*   task, 
                                 NPT_TimeInterval* delay = NULL,
                                 bool              auto_destroy = true);

    /**
     Stop all tasks associated with this task manager.
     */
    NPT_Result StopAllTasks();

    /**
     Returns the max number of concurrent tasks allowed. 0 for no limit.
     */
    NPT_Cardinal GetMaxTasks() { return m_MaxTasks; }

private:
    friend class PLT_ThreadTask;

    // called by PLT_ThreadTask
    NPT_Result AddTask(PLT_ThreadTask* task);
    NPT_Result RemoveTask(PLT_ThreadTask* task);

private:
    NPT_List<PLT_ThreadTask*>  m_Tasks;
    NPT_Mutex                  m_TasksLock;
    NPT_Mutex                  m_CallbackLock;
    NPT_Queue<int>*            m_Queue;
    NPT_Cardinal               m_MaxTasks;
    NPT_Cardinal               m_RunningTasks;
    bool                       m_Stopping;
};

#endif /* _PLT_TASKMANAGER_H_ */
