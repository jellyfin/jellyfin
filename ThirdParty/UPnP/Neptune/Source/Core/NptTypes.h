/*****************************************************************
|
|   Neptune - Types
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

#ifndef _NPT_TYPES_H_
#define _NPT_TYPES_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptConfig.h"

/*----------------------------------------------------------------------
|   sized types (this assumes that ints are 32 bits)
+---------------------------------------------------------------------*/
typedef NPT_CONFIG_INT64_TYPE          NPT_Int64;
typedef unsigned NPT_CONFIG_INT64_TYPE NPT_UInt64;
typedef unsigned int                   NPT_UInt32;
typedef int                            NPT_Int32;
typedef unsigned short                 NPT_UInt16;
typedef short                          NPT_Int16;
typedef unsigned char                  NPT_UInt8;
typedef char                           NPT_Int8;
typedef float                          NPT_Float;

/*----------------------------------------------------------------------
|   named types       
+---------------------------------------------------------------------*/
typedef int           NPT_Result;
typedef unsigned int  NPT_Cardinal;
typedef unsigned int  NPT_Ordinal;
typedef NPT_UInt32    NPT_Size;
typedef NPT_UInt64    NPT_LargeSize;
typedef NPT_Int32     NPT_Offset;
typedef NPT_UInt64    NPT_Position;
typedef NPT_Int32     NPT_Timeout;
typedef void          NPT_Interface;
typedef NPT_UInt8     NPT_Byte;
typedef NPT_UInt32    NPT_Flags;
typedef void*         NPT_Any;
typedef const void*   NPT_AnyConst;

/*----------------------------------------------------------------------
|   limits       
+---------------------------------------------------------------------*/
#if defined(NPT_CONFIG_HAVE_LIMITS_H)
#include <limits.h>
#endif

#if !defined(NPT_INT_MIN)
#if defined(NPT_CONFIG_HAVE_INT_MIN)
#define NPT_INT_MIN INT_MIN
#endif
#endif

#if !defined(NPT_INT_MAX)
#if defined(NPT_CONFIG_HAVE_INT_MAX)
#define NPT_INT_MAX INT_MAX
#endif
#endif

#if !defined(NPT_UINT_MAX)
#if defined(NPT_CONFIG_HAVE_UINT_MAX)
#define NPT_UINT_MAX UINT_MAX
#endif
#endif

#if !defined(NPT_LONG_MIN)
#if defined(NPT_CONFIG_HAVE_LONG_MIN)
#define NPT_LONG_MIN LONG_MIN
#endif
#endif

#if !defined(NPT_LONG_MAX)
#if defined(NPT_CONFIG_HAVE_LONG_MAX)
#define NPT_LONG_MAX LONG_MAX
#endif
#endif

#if !defined(NPT_ULONG_MAX)
#if defined(NPT_CONFIG_HAVE_ULONG_MAX)
#define NPT_ULONG_MAX ULONG_MAX
#endif
#endif

#if !defined(NPT_INT32_MAX)
#define NPT_INT32_MAX 0x7FFFFFFF
#endif

#if !defined(NPT_INT32_MIN)
#define NPT_INT32_MIN (-NPT_INT32_MAX - 1) 
#endif

#if !defined(NPT_UINT32_MAX)
#define NPT_UINT32_MAX 0xFFFFFFFF
#endif

#if !defined(NPT_INT64_MAX)
#if defined(NPT_CONFIG_HAVE_LLONG_MAX)
#define NPT_INT64_MAX LLONG_MAX
#else
#define NPT_INT64_MAX 0x7FFFFFFFFFFFFFFFLL
#endif
#endif

#if !defined(NPT_INT64_MIN)
#if defined(NPT_CONFIG_HAVE_LLONG_MIN)
#define NPT_INT64_MIN LLONG_MIN
#else
#define NPT_INT64_MIN (-NPT_INT64_MAX - 1LL) 
#endif
#endif

#if !defined(NPT_UINT64_MAX)
#if defined(NPT_CONFIG_HAVE_ULLONG_MAX)
#define NPT_UINT64_MAX ULLONG_MAX
#else
#define NPT_UINT64_MAX 0xFFFFFFFFFFFFFFFFULL
#endif
#endif

#endif // _NPT_TYPES_H_
