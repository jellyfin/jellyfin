/*****************************************************************
|
|   Neptune - System :: WinRT Implementation
|
|   (c) 2001-2012 Gilles Boccon-Gibod
|   Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptWinRtPch.h"
#include "NptConfig.h"
#include "NptTypes.h"
#include "NptSystem.h"
#include "NptResults.h"
#include "NptDebug.h"

using namespace Windows::Security::Cryptography;

/*----------------------------------------------------------------------
|   NPT_WinRtSystemInitializer
+---------------------------------------------------------------------*/
class NPT_WinRtSystem {
public:
    static NPT_WinRtSystem Global;
    ~NPT_WinRtSystem() {
		CloseHandle(m_WaitEvent);
	}
	HANDLE m_WaitEvent;
    
private:
    NPT_WinRtSystem() {
		m_WaitEvent = CreateEventExW(NULL, L"", 0, EVENT_ALL_ACCESS);
	}
};
NPT_WinRtSystem NPT_WinRtSystem::Global;

/*----------------------------------------------------------------------
|   NPT_System::GetProcessId
+---------------------------------------------------------------------*/
NPT_Result
NPT_System::GetProcessId(NPT_UInt32& id)
{
	id = GetCurrentProcessId();
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_System::GetCurrentTimeStamp
+---------------------------------------------------------------------*/
NPT_Result
NPT_System::GetCurrentTimeStamp(NPT_TimeStamp& now)
{
	FILETIME time;
	GetSystemTimeAsFileTime(&time);
	ULARGE_INTEGER ltime;
	ltime.LowPart = time.dwLowDateTime;
	ltime.HighPart = time.dwHighDateTime;
	
	/* convert to 64-bits 100-nanoseconds value */
	ULONGLONG time64 = ltime.QuadPart;
    time64 -= 116444736000000000; /* convert from the Windows epoch (Jan. 1, 1601) to the 
                                   * Unix epoch (Jan. 1, 1970) */
	now.FromNanos(time64*100);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_System::Sleep
+---------------------------------------------------------------------*/
NPT_Result
NPT_System::Sleep(const NPT_TimeInterval& duration)
{
	DWORD timeout = (DWORD)duration.ToMillis();
	WaitForSingleObjectEx(NPT_WinRtSystem::Global.m_WaitEvent, timeout, FALSE);
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
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_System::NPT_System
+---------------------------------------------------------------------*/
NPT_UInt32 
NPT_System::GetRandomInteger()
{
    return CryptographicBuffer::GenerateRandomNumber();
}

