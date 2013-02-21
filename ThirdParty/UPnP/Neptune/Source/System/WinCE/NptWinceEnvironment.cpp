/*****************************************************************
|
|      Neptune - Environment variables: Windows CE Implementation
|
|      (c) 2002-2006 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include <windows.h>

#include "NptConfig.h"
#include "NptUtils.h"
#include "NptResults.h"

/*----------------------------------------------------------------------
|   NPT_GetEnvironment
+---------------------------------------------------------------------*/
NPT_Result 
NPT_GetEnvironment(const char* name, NPT_String& value)
{
    HKEY       key = NULL; 
    DWORD      type;
    WCHAR*     name_w;
    DWORD      name_length;
    DWORD      value_length;
    NPT_Result result;

    // default value
    value.SetLength(0);

    // convert name to unicode
    name_length = NPT_StringLength(name);
    name_w = new WCHAR[(name_length+1)];
    MultiByteToWideChar(CP_UTF8, 0, name, -1, name_w, name_length+1);

    if (RegOpenKeyEx(HKEY_CURRENT_USER, 
                     _T("Software\\Axiomatic\\Neptune\\Environment"), 
                     0, KEY_ALL_ACCESS, &key) == ERROR_SUCCESS) { 
        if (RegQueryValueEx(key, name_w, 0, &type, (PBYTE)NULL, &value_length ) == ERROR_SUCCESS) { 
            // convert to UTF-8

            WCHAR* value_w = new WCHAR[(value_length+1)];
            int    value_size = 4*value_length+1;
            value.Reserve(value_size);

            if (RegQueryValueEx(key, name_w, 0, &type, (PBYTE)value_w, &value_length ) == ERROR_SUCCESS) {
                value_size = WideCharToMultiByte(CP_UTF8, 0, value_w, value_length, value.UseChars(), value_size, NULL, FALSE);
                value.SetLength(value_size);
            }

            delete[] value_w;
            result = NPT_SUCCESS;
        }
    }

    delete[] name_w;

    return result;
}
