/*****************************************************************
|
|   Neptune - System :: Win32 Implementation
|
|   (c) 2001-2006 Gilles Boccon-Gibod
|   Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "e32cmn.h"
#include "e32math.h"
#include "sys/time.h"

#include "NptConfig.h"
#include "NptTypes.h"
#include "NptSystem.h"
#include "NptResults.h"
#include "NptDebug.h"


/*----------------------------------------------------------------------
|   globals
+---------------------------------------------------------------------*/
static TInt64 NPT_System_RandomGeneratorSeed = 0;


/*----------------------------------------------------------------------
|   NPT_System::GetProcessId
+---------------------------------------------------------------------*/
NPT_Result
NPT_System::GetProcessId(NPT_UInt32& id)
{
    //id = getpid();
    id = 0;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_System::GetCurrentTimeStamp
+---------------------------------------------------------------------*/
NPT_Result
NPT_System::GetCurrentTimeStamp(NPT_TimeStamp& now)
{
    struct timeval now_tv;

    /* get current time from system */
    if (gettimeofday(&now_tv, NULL)) {
        now.m_Seconds     = 0;
        now.m_NanoSeconds = 0;
        return NPT_FAILURE;
    }

    /* convert format */
    now.m_Seconds     = now_tv.tv_sec;
    now.m_NanoSeconds = now_tv.tv_usec * 1000;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_System::Sleep
+---------------------------------------------------------------------*/
NPT_Result
NPT_System::Sleep(const NPT_TimeInterval& duration)
{
    TTimeIntervalMicroSeconds32  milliseconds = 1000*duration.m_Seconds + duration.m_NanoSeconds/1000000;
    User::After(milliseconds); /* FIXME: this doesn't behave like a normal sleep() where the processor idles. Need to use CTimer much more complicated logic. */

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
        return Sleep(duration);
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
    NPT_System_RandomGeneratorSeed = seed;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_System::NPT_System
+---------------------------------------------------------------------*/
NPT_UInt32 
NPT_System::GetRandomInteger()
{
    if (!NPT_System_RandomGeneratorSeed) {
        TTime time;
        time.HomeTime();
        
        NPT_System::SetRandomSeed(time.Int64());
    }

    return Math::Rand(NPT_System_RandomGeneratorSeed);
}

