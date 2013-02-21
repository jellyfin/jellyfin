/*****************************************************************
|
|      Neptune - System :: PS3 Implementation
|
|      (c) 2001-2006 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include <stdio.h>
#include <stdlib.h>
#include <errno.h>
#include <time.h>
#include <sys/timer.h>
#include <sys/sys_time.h>
#include <unistd.h>

#include "NptConfig.h"
#include "NptTypes.h"
#include "NptSystem.h"
#include "NptResults.h"
#include "NptDebug.h"

/*----------------------------------------------------------------------
|   NPT_System::GetProcessId
+---------------------------------------------------------------------*/
NPT_Result
NPT_System::GetProcessId(NPT_Integer& id)
{
    id = 0;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_System::GetCurrentTimeStamp
+---------------------------------------------------------------------*/
NPT_Result
NPT_System::GetCurrentTimeStamp(NPT_TimeStamp& now)
{
    sys_time_sec_t  sec;
    sys_time_nsec_t nsec;

    int result = sys_time_get_current_time(&sec, &nsec);
    if (result != CELL_OK){
        now.m_Seconds     = 0;
        now.m_NanoSeconds = 0;   
        return NPT_FAILURE;
    }

    /* convert format */
    now.m_Seconds     = sec;
    now.m_NanoSeconds = nsec;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_System::Sleep
+---------------------------------------------------------------------*/
NPT_Result
NPT_System::Sleep(const NPT_TimeInterval& duration)
{
    unsigned long usecs = 1000000*duration.m_Seconds + duration.m_NanoSeconds/1000;
    sys_timer_usleep(usecs);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_System::SleepUntil
+---------------------------------------------------------------------*/
NPT_Result
NPT_System::SleepUntil(const NPT_TimeStamp& when)
{
    NPT_TimeStamp now;
    GetCurrentTimeStamp(now);
    if (when > now) {
        NPT_TimeInterval duration = when-now;
        return NPT_System::Sleep(duration);
    } else {
        return NPT_SUCCESS;
    }
}

/*----------------------------------------------------------------------
|   NPT_System::SetRandomSeed
+---------------------------------------------------------------------*/
NPT_Result  
NPT_System::SetRandomSeed(unsigned int seed)
{
    srand(seed);
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_System::GetRandomInteger
+---------------------------------------------------------------------*/
NPT_Integer 
NPT_System::GetRandomInteger()
{
    static bool seeded = false;
    if (!seeded) {
        NPT_TimeStamp now;
        GetCurrentTimeStamp(now);
        srand(now.m_NanoSeconds);
        seeded = true;
    }

    return rand();
}
