/*****************************************************************
|
|      Neptune - Time: Posix Implementation
|
|      (c) 2002-2009 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include <time.h>
#include <errno.h>

#if defined(__CYGWIN__)
extern time_t timezone;
#endif

#include "NptTime.h"
#include "NptResults.h"
#include "NptLogging.h"
#include "NptSystem.h"
#include "NptUtils.h"

/*----------------------------------------------------------------------
|   logging
+---------------------------------------------------------------------*/
//NPT_SET_LOCAL_LOGGER("neptune.system.posix.time")

/*----------------------------------------------------------------------
|   compatibility wrappers
+---------------------------------------------------------------------*/
#if defined(NPT_CONFIG_HAVE_GMTIME) && !defined(NPT_CONFIG_HAVE_GMTIME_R)
static int gmtime_r(struct tm* _tm, time_t* time)
{
    struct tm* _gmt = gmtime(time);

#if defined(_WIN32_WCE)
    if (_gmt == NULL) return ENOENT;
#else
    if (_gmt== NULL) return errno;
#endif

    *_tm  = *_gmt;
    return 0;
}
#endif // defined(NPT_CONFIG_HAVE_GMTIME_S

#if defined(NPT_CONFIG_HAVE_LOCALTIME) && !defined(NPT_CONFIG_HAVE_LOCALTIME_R)
static int localtime_r(struct tm* _tm, time_t* time)
{   
    struct tm* _local = localtime(time);

#if defined(_WIN32_WCE)
    if (_local == NULL) return ENOENT;
#else
    if (_local== NULL) return errno;
#endif

    *_tm  = *_local;
    return 0;
}
#endif // defined(NPT_CONFIG_HAVE_LOCALTIME_S

/*----------------------------------------------------------------------
|   NPT_DateTime::GetTimeZone
+---------------------------------------------------------------------*/
NPT_Int32
NPT_DateTime::GetLocalTimeZone()
{
    struct tm tm_local;
    time_t epoch = 0;
    
    NPT_SetMemory(&tm_local, 0, sizeof(tm_local));

    localtime_r(&epoch, &tm_local);

#if defined(__CYGWIN__)
     return (int)timezone/60;
#else
     return tm_local.tm_gmtoff/60;
#endif
}
