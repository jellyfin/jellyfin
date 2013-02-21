extern "C" {
#include <sys/system_properties.h>
}
#include "NptLogging.h"

NPT_Result
NPT_GetSystemLogConfig(NPT_String& config)
{
    char android_npt_config[PROP_VALUE_MAX];
    android_npt_config[0] = 0;
    int prop_len = __system_property_get("persist.neptune_log_config", 
                                         android_npt_config);
    if (prop_len > 0) {
        config = android_npt_config;
        return NPT_SUCCESS;
    } else {
        return NPT_ERROR_NO_SUCH_PROPERTY;
    }
}
