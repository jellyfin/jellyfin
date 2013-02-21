/*****************************************************************
|
|      Neptune - Console Support: StdC Implementation
|
|      (c) 2002-2006 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include <android/log.h>
#include <stdio.h>

#include "NptConfig.h"
#include "NptConsole.h"

/*----------------------------------------------------------------------
|       NPT_Console::Output
+---------------------------------------------------------------------*/
void
NPT_Console::Output(const char* message)
{
    __android_log_write(ANDROID_LOG_DEBUG, "Neptune", message);
}
