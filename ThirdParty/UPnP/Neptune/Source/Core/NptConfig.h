/*****************************************************************
|
|   Neptune - Configuration
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

#ifndef _NPT_CONFIG_H_
#define _NPT_CONFIG_H_

/*----------------------------------------------------------------------
|   defaults
+---------------------------------------------------------------------*/
#define NPT_CONFIG_HAVE_ASSERT_H
#define NPT_CONFIG_HAVE_STD_C
#define NPT_CONFIG_HAVE_POSIX_TIME
#define NPT_CONFIG_HAVE_ASSERT_H
#define NPT_CONFIG_HAVE_STDLIB_H
#define NPT_CONFIG_HAVE_STDIO_H
#define NPT_CONFIG_HAVE_STDARG_H
#define NPT_CONFIG_HAVE_STDINT_H
#define NPT_CONFIG_HAVE_STRING_H
#define NPT_CONFIG_HAVE_LIMITS_H

/*----------------------------------------------------------------------
|   standard C runtime
+---------------------------------------------------------------------*/
#if defined(NPT_CONFIG_HAVE_STD_C)
#define NPT_CONFIG_HAVE_MALLOC
#define NPT_CONFIG_HAVE_CALLOC
#define NPT_CONFIG_HAVE_REALLOC
#define NPT_CONFIG_HAVE_FREE
#define NPT_CONFIG_HAVE_MEMCPY
#define NPT_CONFIG_HAVE_MEMSET
#define NPT_CONFIG_HAVE_MEMCMP
#define NPT_CONFIG_HAVE_GETENV
#define NPT_CONFIG_HAVE_SETENV
#define NPT_CONFIG_HAVE_UNSETENV
#define NPT_CONFIG_HAVE_READDIR_R
#endif /* NPT_CONFIG_HAS_STD_C */

#if defined(NPT_CONFIG_HAVE_POSIX_TIME)
#define NPT_CONFIG_HAVE_GMTIME
#define NPT_CONFIG_HAVE_GMTIME_R
#define NPT_CONFIG_HAVE_LOCALTIME
#define NPT_CONFIG_HAVE_LOCALTIME_R
#endif

#if defined(NPT_CONFIG_HAVE_STRING_H)
#define NPT_CONFIG_HAVE_STRCMP
#define NPT_CONFIG_HAVE_STRNCMP
#define NPT_CONFIG_HAVE_STRDUP
#define NPT_CONFIG_HAVE_STRLEN
#define NPT_CONFIG_HAVE_STRCPY
#define NPT_CONFIG_HAVE_STRNCPY
#endif /* NPT_CONFIG_HAVE_STRING_H */

#if defined(NPT_CONFIG_HAVE_STDIO_H)
#define NPT_CONFIG_HAVE_SPRINTF
#define NPT_CONFIG_HAVE_SNPRINTF
#define NPT_CONFIG_HAVE_VSPRINTF
#define NPT_CONFIG_HAVE_VSNPRINTF
#endif /* NPT_CONFIG_HAVE_STDIO_H */

#if defined(NPT_CONFIG_HAVE_LIMITS_H)
#define NPT_CONFIG_HAVE_INT_MIN
#define NPT_CONFIG_HAVE_INT_MAX
#define NPT_CONFIG_HAVE_UINT_MAX
#define NPT_CONFIG_HAVE_LONG_MIN
#define NPT_CONFIG_HAVE_LONG_MAX
#define NPT_CONFIG_HAVE_ULONG_MAX
#endif

/*----------------------------------------------------------------------
|   standard C++ runtime
+---------------------------------------------------------------------*/
#define NPT_CONFIG_HAVE_NEW_H

/*----------------------------------------------------------------------
|   sockets
+---------------------------------------------------------------------*/
#define NPT_CONFIG_HAVE_SOCKADDR_SA_LEN

/*----------------------------------------------------------------------
|   platform specifics
+---------------------------------------------------------------------*/
/* Windows 32 */
#if defined(_WIN32) || defined(_XBOX)
#if !defined(STRICT)
#define STRICT
#endif
#endif

/* XBox */
#if defined(_XBOX)
#define NPT_CONFIG_THREAD_STACK_SIZE 0x10000
#endif

/* QNX */
#if defined(__QNX__)
#endif

/* cygwin */
#if defined(__CYGWIN__)
#undef NPT_CONFIG_HAVE_SOCKADDR_SA_LEN
#endif

/* linux */
#if defined(__linux__)
#define NPT_CONFIG_HAVE_GETADDRINFO
#undef NPT_CONFIG_HAVE_SOCKADDR_SA_LEN
#endif

/* symbian */
#if defined(__SYMBIAN32__)
/* If defined, specify the stack size of each NPT_Thread. */
#define NPT_CONFIG_THREAD_STACK_SIZE   0x14000
#endif

/* android */
#if defined(ANDROID)
#define NPT_CONFIG_HAVE_GETADDRINFO
#undef NPT_CONFIG_HAVE_SOCKADDR_SA_LEN
#endif

/* OSX and iOS */
#if defined(__APPLE__)
#define NPT_CONFIG_HAVE_GETADDRINFO
#define NPT_CONFIG_HAVE_AUTORELEASE_POOL
#define NPT_CONFIG_HAVE_SYSTEM_LOG_CONFIG
#endif

/*----------------------------------------------------------------------
|   compiler specifics
+---------------------------------------------------------------------*/
/* GCC */
#if defined(__GNUC__)
#define NPT_LocalFunctionName __FUNCTION__
#define NPT_COMPILER_UNUSED(p) (void)p
#else
#define NPT_COMPILER_UNUSED(p) 
#endif

/* TriMedia C/C++ Compiler */
#if defined(__TCS__)
#undef NPT_CONFIG_HAVE_ASSERT_H
#undef NPT_CONFIG_HAVE_SNPRINTF
#undef NPT_CONFIG_HAVE_VSNPRINTF
#endif

/* palmos compiler */
#if defined(__PALMOS__)
#if __PALMOS__ <= 0x05000000
#undef NPT_CONFIG_HAVE_ASSERT_H
#undef NPT_CONFIG_HAVE_SNPRINTF
#undef NPT_CONFIG_HAVE_VSNPRINTF
#endif
#endif

/* Microsoft C/C++ Compiler */
#if defined(_MSC_VER)
#undef NPT_CONFIG_HAVE_STDINT_H
#define NPT_CONFIG_HAVE_GETADDRINFO
#define NPT_CONFIG_STAT_ST_CTIME_IS_ST_BIRTHTIME
#define NPT_FORMAT_64 "I64"
#define NPT_CONFIG_INT64_TYPE __int64
#define NPT_INT64_MIN _I64_MIN
#define NPT_INT64_MAX _I64_MAX
#define NPT_UINT64_MAX _UI64_MAX
#define NPT_INT64_C(_x) _x##i64
#define NPT_UINT64_C(_x) _x##ui64
#define NPT_LocalFunctionName __FUNCTION__
#if !defined(_WIN32_WCE)
#define NPT_fseek _fseeki64
#define NPT_ftell _ftelli64
#else
#define NPT_fseek(a,b,c) fseek((a),(long)(b), (c))
#define NPT_ftell ftell
#endif
#define NPT_stat  NPT_stat_utf8
#define NPT_stat_struct struct __stat64
#if defined(_WIN64)
typedef __int64 NPT_PointerLong;
#else
#if _MSC_VER >= 1400
typedef __w64 long NPT_PointerLong;
#else
typedef long NPT_PointerLong;
#endif
#endif
#define NPT_POINTER_TO_LONG(_p) ((NPT_PointerLong) (_p) )
#if _MSC_VER >= 1400 && !defined(_WIN32_WCE)
#define gmtime_r(a,b) gmtime_s(a,b)
#define localtime_r(a,b) localtime_s(b,a)
#define NPT_CONFIG_HAVE_FOPEN_S
#define NPT_CONFIG_HAVE_FSOPEN
#define NPT_CONFIG_HAVE_SHARE_H
#define NPT_vsnprintf(s,c,f,a)  _vsnprintf_s(s,c,_TRUNCATE,f,a)
#define NPT_snprintf(s,c,f,...) _snprintf_s(s,c,_TRUNCATE,f,__VA_ARGS__)
#define NPT_strncpy(d,s,c)       strncpy_s(d,c+1,s,c)
#define NPT_strcpy(d,s)          strcpy_s(d,strlen(s)+1,s)
#undef NPT_CONFIG_HAVE_GETENV
#define NPT_CONFIG_HAVE_DUPENV_S
#define dupenv_s _dupenv_s
#undef NPT_CONFIG_HAVE_SETENV
#undef NPT_CONFIG_HAVE_UNSETENV
#define NPT_CONFIG_HAVE_PUTENV_S
#define putenv_s _putenv_s
#else
#undef NPT_CONFIG_HAVE_GMTIME_R
#undef NPT_CONFIG_HAVE_LOCALTIME_R
#define NPT_vsnprintf  _vsnprintf
#define NPT_snprintf   _snprintf
#endif
#if defined(_DEBUG)
#define _CRTDBG_MAP_ALLOC
#endif
#endif

/* Windows CE */
#if defined(_WIN32_WCE)
#if defined(NPT_CONFIG_HAVE_FOPEN_S)
#undef NPT_CONFIG_HAVE_FOPEN_S
#endif
#endif

/* Symbian */
#if defined(__SYMBIAN32__)
#undef NPT_CONFIG_HAVE_NEW_H
#include "e32std.h"
#define explicit
#define NPT_fseek fseek  // no fseeko ?
#define NPT_ftell ftell  // no ftello ?
#endif

/* Android */
#if defined(ANDROID)
#define NPT_CONFIG_NO_RTTI
#endif

/* OSX and iOS */
#if defined(__APPLE__)
#include <TargetConditionals.h>
#include <AvailabilityMacros.h>
#define NPT_CONFIG_HAVE_NET_IF_DL_H
#define NPT_CONFIG_HAVE_SOCKADDR_DL
#if !defined(TARGET_OS_IPHONE) || !TARGET_OS_IPHONE
#define NPT_CONFIG_HAVE_NET_IF_TYPES_H
#if defined(MAC_OS_X_VERSION_10_6) && (MAC_OS_X_VERSION_MIN_REQUIRED >= MAC_OS_X_VERSION_10_6)
#define NPT_CONFIG_HAVE_STAT_ST_BIRTHTIME
#endif
#endif
#endif

/*----------------------------------------------------------------------
|   defaults
+---------------------------------------------------------------------*/
#if !defined(NPT_FORMAT_64)
#define NPT_FORMAT_64 "ll"
#endif

#if !defined(NPT_POINTER_TO_LONG)
#define NPT_POINTER_TO_LONG(_p) ((long)(_p))
#endif

#if !defined(NPT_CONFIG_INT64_TYPE)
#define NPT_CONFIG_INT64_TYPE long long
#endif

#if !defined(NPT_INT64_C)
#define NPT_INT64_C(_x) _x##LL
#endif

#if !defined(NPT_UINT64_C)
#define NPT_UINT64_C(_x) _x##ULL
#endif

#if !defined(NPT_snprintf)
#define NPT_snprintf snprintf
#endif

#if !defined(NPT_strcpy)
#define NPT_strcpy strcpy
#endif

#if !defined(NPT_strncpy)
#define NPT_strncpy strncpy
#endif

#if !defined(NPT_vsnprintf)
#define NPT_vsnprintf vsnprintf
#endif

#if !defined(NPT_LocalFunctionName)
#define NPT_LocalFunctionName (NULL)
#endif

#if !defined(NPT_CONFIG_THREAD_STACK_SIZE)
#define NPT_CONFIG_THREAD_STACK_SIZE 0
#endif

#if !defined(NPT_fseek)
#define NPT_fseek fseeko
#endif

#if !defined(NPT_ftell)
#define NPT_ftell ftello
#endif

#if !defined(NPT_stat)
#define NPT_stat stat
#endif

#if !defined(NPT_stat_struct)
#define NPT_stat_struct struct stat
#endif

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#if defined(DMALLOC)
#include <dmalloc.h>
#endif

#endif // _NPT_CONFIG_H_ 
