/*****************************************************************
|
|   Neptune - Windows CE Utils
|
|   (c) 2001-2006 Gilles Boccon-Gibod
|   Author: Gilles Boccon-Gibod (bok@bok.net)
|
****************************************************************/


#ifndef _NPT_WINCE_UTILS_H_
#define _NPT_WINCE_UTILS_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include <windows.h>

/*----------------------------------------------------------------------
|   fix windows macros
+---------------------------------------------------------------------*/
#if defined(CreateDirectory)
#undef CreateDirectory
#endif

#if defined(DeleteFile)
#undef DeleteFile
#endif

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptTypes.h"

/*----------------------------------------------------------------------
|   A2WHelper
+---------------------------------------------------------------------*/
static LPWSTR A2WHelper(LPWSTR lpw, LPCSTR lpa, int nChars, UINT acp)
{
    int ret;

    assert(lpa != NULL);
    assert(lpw != NULL);
    if (lpw == NULL || lpa == NULL) return NULL;

    lpw[0] = '\0';
    ret = MultiByteToWideChar(acp, 0, lpa, -1, lpw, nChars);
    if (ret == 0) {
        assert(0);
        return NULL;
    }        
    return lpw;
}

/*----------------------------------------------------------------------
|   W2AHelper
+---------------------------------------------------------------------*/
static LPSTR W2AHelper(LPSTR lpa, LPCWSTR lpw, int nChars, UINT acp)
{
    int ret;

    assert(lpw != NULL);
    assert(lpa != NULL);
    if (lpa == NULL || lpw == NULL) return NULL;

    lpa[0] = '\0';
    ret = WideCharToMultiByte(acp, 0, lpw, -1, lpa, nChars, NULL, NULL);
    if (ret == 0) {
        assert(0);
        return NULL;
    }
    return lpa;
}

/*----------------------------------------------------------------------
|   macros
+---------------------------------------------------------------------*/
#define USES_CONVERSION int _convert = 0; LPCWSTR _lpw = NULL; LPCSTR _lpa = NULL

#define A2W(lpa) (\
    ((_lpa = lpa) == NULL) ? NULL : (\
    _convert = (strlen(_lpa)+1),\
    (INT_MAX/2<_convert)? NULL :  \
    A2WHelper((LPWSTR) alloca(_convert*sizeof(WCHAR)), _lpa, _convert, CP_UTF8)))

#define W2A(lpw) (\
    ((_lpw = lpw) == NULL) ? NULL : (\
    (_convert = (lstrlenW(_lpw)+1), \
    (_convert>INT_MAX/2) ? NULL : \
    W2AHelper((LPSTR) alloca(_convert*sizeof(WCHAR)), _lpw, _convert*sizeof(WCHAR), CP_UTF8))))

#endif /* _NPT_WINCE_UTILS_H_ */