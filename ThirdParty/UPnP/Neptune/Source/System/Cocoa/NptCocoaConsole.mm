/*****************************************************************
|
|      Neptune - Console Support: Cocoa Implementation
|
|      (c) 2002-2006 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include <stdio.h>
#import <Foundation/Foundation.h>
#include "NptConfig.h"
#include "NptConsole.h"
#include "NptUtils.h"

/*----------------------------------------------------------------------
|       NPT_Console::Output
+---------------------------------------------------------------------*/
void
NPT_Console::Output(const char* message)
{
    // trim extra \r\n
    char *msg = (char *)message;
    msg[NPT_StringLength(message)-2] = 0;
    
    NSLog(@"%s", msg);
}

