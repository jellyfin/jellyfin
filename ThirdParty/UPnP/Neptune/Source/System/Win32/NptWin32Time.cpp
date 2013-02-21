/*****************************************************************
|
|      Neptune - Time: Win32 Implementation
|
|      (c) 2002-2009 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include <windows.h>

#include "NptTime.h"
#include "NptResults.h"

/*----------------------------------------------------------------------
|   logging
+---------------------------------------------------------------------*/
//NPT_SET_LOCAL_LOGGER("neptune.system.win32.time")

/*----------------------------------------------------------------------
|   NPT_DateTime::GetTimeZone
+---------------------------------------------------------------------*/
NPT_Int32
NPT_DateTime::GetLocalTimeZone()
{
    TIME_ZONE_INFORMATION tz_info;
    DWORD result = GetTimeZoneInformation(&tz_info);
    if (result == TIME_ZONE_ID_INVALID) return 0;
    return -tz_info.Bias;
}
