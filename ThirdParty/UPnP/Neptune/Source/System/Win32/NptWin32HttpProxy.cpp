/*****************************************************************
|
|      Neptune - HTTP Proxy :: WinHttp Implementation
|
|      (c) 2001-2007 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
//#include <Winhttp.h>
#include "NptResults.h"
#include "NptHttp.h"
#include "NptThreads.h"

#if 0 // not implemented yet
HINTERNET NPT_Win32HttpHandle = INVALID_HANDLE_VALUE;
NPT_Lock NPT_Win32HttpLock;

/*----------------------------------------------------------------------
|       NPT_HttpProxySelector::GetSystemDefault
+---------------------------------------------------------------------*/
NPT_HttpProxySelector*
NPT_HttpProxySelector::GetSystemDefault()
{
    NPT_AutoLock lock(NPT_Win32HttpLock);

    if (NPT_Win32HttpHandle == INVALID_HANDLE_VALUE) {
        WINHTTP_CURRENT_USER_IE_PROXY_CONFIG config;
        BOOL result = WinHttpGetIEProxyConfigForCurrentUser(&config);
    }

    return NULL;
}
#else
NPT_HttpProxySelector*
NPT_HttpProxySelector::GetSystemDefault()
{
    return NULL;
}
#endif
