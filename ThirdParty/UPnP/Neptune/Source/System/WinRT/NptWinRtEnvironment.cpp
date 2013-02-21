/*****************************************************************
|
|      Neptune - Environment variables: WinRT Implementation
|
|      (c) 2002-2012 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include "NptConfig.h"
#include "NptUtils.h"
#include "NptResults.h"

/*----------------------------------------------------------------------
|   NPT_Environment::Get
+---------------------------------------------------------------------*/
NPT_Result 
NPT_Environment::Get(const char* name, NPT_String& value)
{
    /* default value */
    value.SetLength(0);

    return NPT_ERROR_NOT_SUPPORTED;
}

/*----------------------------------------------------------------------
|   NPT_Environment::Set
+---------------------------------------------------------------------*/
NPT_Result 
NPT_Environment::Set(const char* name, const char* value)
{
    return NPT_ERROR_NOT_SUPPORTED;
}
