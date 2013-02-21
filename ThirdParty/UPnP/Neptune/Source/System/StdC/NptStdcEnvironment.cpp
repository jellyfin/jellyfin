/*****************************************************************
|
|      Neptune - Environment variables: StdC Implementation
|
|      (c) 2002-2006 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include <stdlib.h>

#include "NptConfig.h"
#include "NptUtils.h"
#include "NptResults.h"

/*----------------------------------------------------------------------
|   NPT_Environment::Get
+---------------------------------------------------------------------*/
NPT_Result 
NPT_Environment::Get(const char* name, NPT_String& value)
{
    char* env;

    /* default value */
    value.SetLength(0);

#if defined(NPT_CONFIG_HAVE_GETENV)
    env = getenv(name);
    if (env) {
        value = env;
        return NPT_SUCCESS;
    } else {
        return NPT_ERROR_NO_SUCH_ITEM;
    }
#elif defined(NPT_CONFIG_HAVE_DUPENV_S)
    if (dupenv_s(&env, NULL, name) != 0) {
        return NPT_FAILURE;
    } else if (env != NULL) {
        value = env;
        free(env);
        return NPT_SUCCESS;
    } else {
        return NPT_ERROR_NO_SUCH_ITEM;
    }
#else
    return NPT_ERROR_NOT_SUPPORTED;
#endif
}

/*----------------------------------------------------------------------
|   NPT_Environment::Set
+---------------------------------------------------------------------*/
NPT_Result 
NPT_Environment::Set(const char* name, const char* value)
{
    int result = 0;
    if (value) {
#if defined(NPT_CONFIG_HAVE_SETENV)
        // set the variable
        setenv(name, value, 1); // ignore return value (some platforms have this function as void)
#elif defined(NPT_CONFIG_HAVE_PUTENV_S)
        result = putenv_s(name, value);
#else
        return NPT_ERROR_NOT_SUPPORTED;
#endif
    } else {
        // remove the variable
#if defined(NPT_CONFIG_HAVE_UNSETENV)
        unsetenv(name); // ignore return value (some platforms have this function as void)
#elif defined(NPT_CONFIG_HAVE_PUTENV_S)
        result = putenv_s(name, "");
#else
        return NPT_ERROR_NOT_SUPPORTED;
#endif
    }
    return result==0?NPT_SUCCESS:NPT_FAILURE;
}
