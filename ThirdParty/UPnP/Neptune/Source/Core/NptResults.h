/*****************************************************************
|
|   Neptune - Result Codes
|
| Copyright (c) 2002-2008, Axiomatic Systems, LLC.
| All rights reserved.
|
| Redistribution and use in source and binary forms, with or without
| modification, are permitted provided that the following conditions are met:
|     * Redistributions of source code must retain the above copyright
|       notice, this list of conditions and the following disclaimer.
|     * Redistributions in binary form must reproduce the above copyright
|       notice, this list of conditions and the following disclaimer in the
|       documentation and/or other materials provided with the distribution.
|     * Neither the name of Axiomatic Systems nor the
|       names of its contributors may be used to endorse or promote products
|       derived from this software without specific prior written permission.
|
| THIS SOFTWARE IS PROVIDED BY AXIOMATIC SYSTEMS ''AS IS'' AND ANY
| EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
| WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
| DISCLAIMED. IN NO EVENT SHALL AXIOMATIC SYSTEMS BE LIABLE FOR ANY
| DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
| (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
| LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
| ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
| (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
| SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
|
 ****************************************************************/

#ifndef _NPT_RESULTS_H_
#define _NPT_RESULTS_H_

/*----------------------------------------------------------------------
|   macros
+---------------------------------------------------------------------*/
#if defined(NPT_DEBUG)
#include "NptDebug.h"
#define NPT_CHECK(_x)               \
do {                                \
    NPT_Result _result = (_x);      \
    if (_result != NPT_SUCCESS) {   \
        NPT_Debug("%s(%d): @@@ NPT_CHECK failed, result=%d (%s)\n", __FILE__, __LINE__, _result, NPT_ResultText(_result)); \
        return _result;             \
    }                               \
} while(0)
#define NPT_CHECK_POINTER(_p)                 \
do {                                          \
    if ((_p) == NULL) {                       \
        NPT_Debug("%s(%d): @@@ NULL pointer parameter\n", __FILE__, __LINE__); \
        return NPT_ERROR_INVALID_PARAMETERS;  \
    }                                         \
} while(0)
#define NPT_CHECK_LABEL(_x, label)  \
do {                                \
    NPT_Result _result = (_x);      \
    if (_result != NPT_SUCCESS) {   \
        NPT_Debug("%s(%d): @@@ NPT_CHECK failed, result=%d (%s)\n", __FILE__, __LINE__, _result, NPT_ResultText(_result)); \
        goto label;                 \
    }                               \
} while(0)
#define NPT_CHECK_POINTER_LABEL(_p, label)   \
do {                                         \
    if (_p == NULL) {                        \
        NPT_Debug("%s(%d): @@@ NULL pointer parameter\n", __FILE__, __LINE__); \
        goto label;                          \
    }                                        \
} while(0)
#else
#define NPT_CHECK(_x)               \
do {                                \
    NPT_Result _result = (_x);      \
    if (_result != NPT_SUCCESS) {   \
        return _result;             \
    }                               \
} while(0)
#define NPT_CHECK_POINTER(_p)                               \
do {                                                        \
    if ((_p) == NULL) return NPT_ERROR_INVALID_PARAMETERS;  \
} while(0)
#define NPT_CHECK_LABEL(_x, label)  \
do {                                \
    NPT_Result _result = (_x);      \
    if (_result != NPT_SUCCESS) {   \
        goto label;                 \
    }                               \
} while(0)
#define NPT_CHECK_POINTER_LABEL(_p, label)   \
do {                                         \
    if ((_p) == NULL) {                      \
        goto label;                          \
    }                                        \
} while(0)
#endif

#define NPT_FAILED(result)              ((result) != NPT_SUCCESS)
#define NPT_SUCCEEDED(result)           ((result) == NPT_SUCCESS)

/*----------------------------------------------------------------------
|   result codes
+---------------------------------------------------------------------*/
/** Result indicating that the operation or call succeeded */
#define NPT_SUCCESS                     0

/** Result indicating an unspecififed failure condition */
#define NPT_FAILURE                     (-1)

#if !defined(NPT_ERROR_BASE)
#define NPT_ERROR_BASE -20000
#endif

// error bases
#define NPT_ERROR_BASE_GENERAL        (NPT_ERROR_BASE-0)
#define NPT_ERROR_BASE_LIST           (NPT_ERROR_BASE-100)
#define NPT_ERROR_BASE_FILE           (NPT_ERROR_BASE-200)
#define NPT_ERROR_BASE_IO             (NPT_ERROR_BASE-300)
#define NPT_ERROR_BASE_SOCKET         (NPT_ERROR_BASE-400)
#define NPT_ERROR_BASE_INTERFACES     (NPT_ERROR_BASE-500)
#define NPT_ERROR_BASE_XML            (NPT_ERROR_BASE-600)
#define NPT_ERROR_BASE_UNIX           (NPT_ERROR_BASE-700)
#define NPT_ERROR_BASE_HTTP           (NPT_ERROR_BASE-800)
#define NPT_ERROR_BASE_THREADS        (NPT_ERROR_BASE-900)
#define NPT_ERROR_BASE_SERIAL_PORT    (NPT_ERROR_BASE-1000)
#define NPT_ERROR_BASE_TLS            (NPT_ERROR_BASE-1100)

// general errors
#define NPT_ERROR_INVALID_PARAMETERS  (NPT_ERROR_BASE_GENERAL - 0)
#define NPT_ERROR_PERMISSION_DENIED   (NPT_ERROR_BASE_GENERAL - 1)
#define NPT_ERROR_OUT_OF_MEMORY       (NPT_ERROR_BASE_GENERAL - 2)
#define NPT_ERROR_NO_SUCH_NAME        (NPT_ERROR_BASE_GENERAL - 3)
#define NPT_ERROR_NO_SUCH_PROPERTY    (NPT_ERROR_BASE_GENERAL - 4)
#define NPT_ERROR_NO_SUCH_ITEM        (NPT_ERROR_BASE_GENERAL - 5)
#define NPT_ERROR_NO_SUCH_CLASS       (NPT_ERROR_BASE_GENERAL - 6)
#define NPT_ERROR_OVERFLOW            (NPT_ERROR_BASE_GENERAL - 7)
#define NPT_ERROR_INTERNAL            (NPT_ERROR_BASE_GENERAL - 8)
#define NPT_ERROR_INVALID_STATE       (NPT_ERROR_BASE_GENERAL - 9)
#define NPT_ERROR_INVALID_FORMAT      (NPT_ERROR_BASE_GENERAL - 10)
#define NPT_ERROR_INVALID_SYNTAX      (NPT_ERROR_BASE_GENERAL - 11)
#define NPT_ERROR_NOT_IMPLEMENTED     (NPT_ERROR_BASE_GENERAL - 12)
#define NPT_ERROR_NOT_SUPPORTED       (NPT_ERROR_BASE_GENERAL - 13)
#define NPT_ERROR_TIMEOUT             (NPT_ERROR_BASE_GENERAL - 14)
#define NPT_ERROR_WOULD_BLOCK         (NPT_ERROR_BASE_GENERAL - 15)
#define NPT_ERROR_TERMINATED          (NPT_ERROR_BASE_GENERAL - 16)
#define NPT_ERROR_OUT_OF_RANGE        (NPT_ERROR_BASE_GENERAL - 17)
#define NPT_ERROR_OUT_OF_RESOURCES    (NPT_ERROR_BASE_GENERAL - 18)
#define NPT_ERROR_NOT_ENOUGH_SPACE    (NPT_ERROR_BASE_GENERAL - 19)
#define NPT_ERROR_INTERRUPTED         (NPT_ERROR_BASE_GENERAL - 20)
#define NPT_ERROR_CANCELLED           (NPT_ERROR_BASE_GENERAL - 21)

/* standard error codes                                  */
/* these are special codes to convey an errno            */
/* the error code is (SHI_ERROR_BASE_ERRNO - errno)      */
/* where errno is the positive integer from errno.h      */
#define NPT_ERROR_BASE_ERRNO          (NPT_ERROR_BASE-2000)
#define NPT_ERROR_ERRNO(e)            (NPT_ERROR_BASE_ERRNO - (e))

/*----------------------------------------------------------------------
|   functions
+---------------------------------------------------------------------*/
const char* NPT_ResultText(int result);

#endif // _NPT_RESULTS_H_
