/*****************************************************************
 |
 |      Neptune - System Support: Cocoa Implementation
 |
 |      (c) 2002-2006 Gilles Boccon-Gibod
 |      Author: Sylvain Rebaud (sylvain@rebaud.com)
 |
 ****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include "NptConfig.h"
#include "NptSystem.h"
#include "NptUtils.h"

NPT_Result
NPT_GetSystemMachineName(NPT_String& name)
{
    return NPT_GetEnvironment("USER", name);
}
