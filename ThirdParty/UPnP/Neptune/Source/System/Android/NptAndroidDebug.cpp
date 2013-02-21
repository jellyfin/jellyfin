/*****************************************************************
|
|      File: NptStdcDebug.c
|
|      Atomix - Debug Support: Android Implementation
|
|      (c) 2002-2009 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include <android/log.h>
#include <stdio.h>

#include "NptConfig.h"
#include "NptDefs.h"
#include "NptTypes.h"
#include "NptDebug.h"

/*----------------------------------------------------------------------
|       NPT_DebugOuput
+---------------------------------------------------------------------*/
void
NPT_DebugOutput(const char* message)
{
    __android_log_write(ANDROID_LOG_DEBUG, "Neptune", message);
    printf("%s", message);
}
