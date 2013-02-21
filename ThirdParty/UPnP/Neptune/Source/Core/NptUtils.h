/*****************************************************************
|
|   Neptune Utils
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

#ifndef _NPT_UTILS_H_
#define _NPT_UTILS_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptConfig.h"
#include "NptTypes.h"
#include "NptStrings.h"
#include "NptMap.h"
#include "NptDataBuffer.h"
#include "NptHash.h"

#if defined (NPT_CONFIG_HAVE_STDIO_H)
#include <stdio.h>
#endif

#if defined (NPT_CONFIG_HAVE_STRING_H)
#include <string.h>
#endif

#if defined(NPT_CONFIG_HAVE_STDARG_H)
#include <stdarg.h>
#endif

/*----------------------------------------------------------------------
|   macros
+---------------------------------------------------------------------*/
#define NPT_ARRAY_SIZE(_a) (sizeof(_a)/sizeof((_a)[0]))

/*----------------------------------------------------------------------
|   byte I/O
+---------------------------------------------------------------------*/
extern void NPT_BytesFromInt64Be(unsigned char* buffer, NPT_UInt64 value);
extern void NPT_BytesFromInt32Be(unsigned char* buffer, NPT_UInt32 value);
extern void NPT_BytesFromInt24Be(unsigned char* buffer, NPT_UInt32 value);
extern void NPT_BytesFromInt16Be(unsigned char* buffer, NPT_UInt16 value);
extern NPT_UInt64 NPT_BytesToInt64Be(const unsigned char* buffer);
extern NPT_UInt32 NPT_BytesToInt32Be(const unsigned char* buffer);
extern NPT_UInt32 NPT_BytesToInt24Be(const unsigned char* buffer);
extern NPT_UInt16 NPT_BytesToInt16Be(const unsigned char* buffer);

extern void NPT_BytesFromInt64Le(unsigned char* buffer, NPT_UInt64 value);
extern void NPT_BytesFromInt32Le(unsigned char* buffer, NPT_UInt32 value);
extern void NPT_BytesFromInt24Le(unsigned char* buffer, NPT_UInt32 value);
extern void NPT_BytesFromInt16Le(unsigned char* buffer, NPT_UInt16 value);
extern NPT_UInt64 NPT_BytesToInt64Le(const unsigned char* buffer);
extern NPT_UInt32 NPT_BytesToInt32Le(const unsigned char* buffer);
extern NPT_UInt32 NPT_BytesToInt24Le(const unsigned char* buffer);
extern NPT_UInt16 NPT_BytesToInt16Le(const unsigned char* buffer);

/*----------------------------------------------------------------------
|    conversion utilities
+---------------------------------------------------------------------*/
extern NPT_Result 
NPT_ParseFloat(const char* str, float& result, bool relaxed = true);

extern NPT_Result 
NPT_ParseInteger(const char* str, long& result, bool relaxed = true, NPT_Cardinal* chars_used = 0);

extern NPT_Result 
NPT_ParseInteger(const char* str, unsigned long& result, bool relaxed = true, NPT_Cardinal* chars_used = 0);

extern NPT_Result 
NPT_ParseInteger(const char* str, int& result, bool relaxed = true, NPT_Cardinal* chars_used = 0);

extern NPT_Result 
NPT_ParseInteger(const char* str, unsigned int& result, bool relaxed = true, NPT_Cardinal* chars_used = 0);

extern NPT_Result 
NPT_ParseInteger32(const char* str, NPT_Int32& result, bool relaxed = true, NPT_Cardinal* chars_used = 0);

extern NPT_Result 
NPT_ParseInteger32(const char* str, NPT_UInt32& result, bool relaxed = true, NPT_Cardinal* chars_used = 0);

extern NPT_Result 
NPT_ParseInteger64(const char* str, NPT_Int64& result, bool relaxed = true, NPT_Cardinal* chars_used = 0);

extern NPT_Result 
NPT_ParseInteger64(const char* str, NPT_UInt64& result, bool relaxed = true, NPT_Cardinal* chars_used = 0);

/*----------------------------------------------------------------------
|    formatting
+---------------------------------------------------------------------*/
void
NPT_FormatOutput(void        (*function)(void* parameter, const char* message),
                 void*       function_parameter,
                 const char* format, 
                 va_list     args);

void NPT_ByteToHex(NPT_Byte b, char* buffer, bool uppercase=false);
NPT_Result NPT_HexToByte(const char* buffer, NPT_Byte& b);
NPT_Result NPT_HexToBytes(const char* hex, NPT_DataBuffer& bytes);
NPT_String NPT_HexString(const unsigned char* data, 
                         NPT_Size             data_size,
                         const char*          separator = NULL,
                         bool                 uppercase=false);
char NPT_NibbleToHex(unsigned int nibble, bool uppercase = true);
int NPT_HexToNibble(char hex);

/*----------------------------------------------------------------------
|    parsing
+---------------------------------------------------------------------*/
NPT_Result 
NPT_ParseMimeParameters(const char*                      encoded,
                        NPT_Map<NPT_String, NPT_String>& parameters);

/*----------------------------------------------------------------------
|    environment variables
+---------------------------------------------------------------------*/
class NPT_Environment {
public:
    static NPT_Result Get(const char* name, NPT_String& value);
    static NPT_Result Set(const char* name, const char* value);
};
// compat for older APIs
#define NPT_GetEnvironment(_x,_y) NPT_Environment::Get(_x,_y)

/*----------------------------------------------------------------------
|   string utils
+---------------------------------------------------------------------*/
#if defined (NPT_CONFIG_HAVE_STDIO_H)
#include <stdio.h>
#endif

#if defined (NPT_CONFIG_HAVE_STRING_H)
#include <string.h>
#endif

#if defined (NPT_CONFIG_HAVE_SNPRINTF)
#define NPT_FormatString NPT_snprintf
#else
int NPT_FormatString(char* str, NPT_Size size, const char* format, ...);
#endif

#if defined(NPT_CONFIG_HAVE_VSNPRINTF)
#define NPT_FormatStringVN(s,c,f,a) NPT_vsnprintf(s,c,f,a)
#else
extern int NPT_FormatStringVN(char *buffer, size_t count, const char *format, va_list argptr);
#endif

#if defined(NPT_CONFIG_HAVE_MEMCPY)
#define NPT_CopyMemory memcpy
#else
extern void NPT_CopyMemory(void* dest, void* src, NPT_Size size);
#endif

#if defined(NPT_CONFIG_HAVE_STRCMP)
#define NPT_StringsEqual(s1, s2) (strcmp((s1), (s2)) == 0)
#else
extern int NPT_StringsEqual(const char* s1, const char* s2);
#endif

#if defined(NPT_CONFIG_HAVE_STRNCMP)
#define NPT_StringsEqualN(s1, s2, n) (strncmp((s1), (s2), (n)) == 0)
#else
extern int NPT_StringsEqualN(const char* s1, const char* s2, unsigned long size);
#endif

#if defined(NPT_CONFIG_HAVE_STRLEN)
#define NPT_StringLength(s) (NPT_Size)(strlen(s))
#else
extern unsigned long NPT_StringLength(const char* s);
#endif

#if defined(NPT_CONFIG_HAVE_STRCPY)
#define NPT_CopyString(dst, src) ((void)NPT_strcpy((dst), (src)))
#else
extern void NPT_CopyString(char* dst, const char* src);
#endif

/**
 * Copy up to n characters from src to dst.
 * The destination buffer will be null-terminated, so it must
 * have enough space for n+1 characters (n from the source plus
 * the null terminator).
 */
#if defined(NPT_CONFIG_HAVE_STRNCPY)
#define NPT_CopyStringN(dst, src, n) \
do { ((void)NPT_strncpy((dst), (src), n)); (dst)[(n)] = '\0'; } while(0)
#else
extern int NPT_CopyStringN(char* dst, const char* src, unsigned long n);
#endif

#if defined(NPT_CONFIG_HAVE_MEMSET)
#define NPT_SetMemory memset
#else
extern void NPT_SetMemory(void* dest, int c, NPT_Size size);
#endif

#if defined(NPT_CONFIG_HAVE_MEMCMP)
#define NPT_MemoryEqual(s1, s2, n) (memcmp((s1), (s2), (n)) == 0) 
#else 
extern int NPT_MemoryEqual(const void* s1, const void* s2, unsigned long n); 
#endif

#endif // _NPT_UTILS_H_
