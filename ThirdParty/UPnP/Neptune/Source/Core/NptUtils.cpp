/*****************************************************************
|
|   Neptune - Utils
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

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include <math.h>

#include "NptConfig.h"
#include "NptTypes.h"
#include "NptDebug.h"
#include "NptUtils.h"
#include "NptResults.h"

#if defined(NPT_CONFIG_HAVE_LIMITS_H)
#include <limits.h>
#endif

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
const unsigned int NPT_FORMAT_LOCAL_BUFFER_SIZE = 1024;
const unsigned int NPT_FORMAT_BUFFER_INCREMENT  = 4096;
const unsigned int NPT_FORMAT_BUFFER_MAX_SIZE   = 65536;

/*----------------------------------------------------------------------
|   NPT_BytesToInt64Be
+---------------------------------------------------------------------*/
NPT_UInt64 
NPT_BytesToInt64Be(const unsigned char* bytes)
{
    return 
        ( ((NPT_UInt64)bytes[0])<<56 ) |
        ( ((NPT_UInt64)bytes[1])<<48 ) |
        ( ((NPT_UInt64)bytes[2])<<40 ) |
        ( ((NPT_UInt64)bytes[3])<<32 ) |
        ( ((NPT_UInt64)bytes[4])<<24 ) |
        ( ((NPT_UInt64)bytes[5])<<16 ) |
        ( ((NPT_UInt64)bytes[6])<<8  ) |
        ( ((NPT_UInt64)bytes[7])     );    
}

/*----------------------------------------------------------------------
|   NPT_BytesToInt32Be
+---------------------------------------------------------------------*/
NPT_UInt32 
NPT_BytesToInt32Be(const unsigned char* bytes)
{
    return 
        ( ((NPT_UInt32)bytes[0])<<24 ) |
        ( ((NPT_UInt32)bytes[1])<<16 ) |
        ( ((NPT_UInt32)bytes[2])<<8  ) |
        ( ((NPT_UInt32)bytes[3])     );    
}

/*----------------------------------------------------------------------
|   NPT_BytesToInt24Be
+---------------------------------------------------------------------*/
NPT_UInt32 
NPT_BytesToInt24Be(const unsigned char* bytes)
{
    return 
        ( ((NPT_UInt32)bytes[0])<<16 ) |
        ( ((NPT_UInt32)bytes[1])<<8  ) |
        ( ((NPT_UInt32)bytes[2])     );    
}

/*----------------------------------------------------------------------
|   NPT_BytesToInt16Be
+---------------------------------------------------------------------*/
NPT_UInt16
NPT_BytesToInt16Be(const unsigned char* bytes)
{
    return 
        ( ((NPT_UInt16)bytes[0])<<8  ) |
        ( ((NPT_UInt16)bytes[1])     );    
}

/*----------------------------------------------------------------------
|   NPT_BytesToInt64Le
+---------------------------------------------------------------------*/
NPT_UInt64 
NPT_BytesToInt64Le(const unsigned char* bytes)
{
    return 
        ( ((NPT_UInt64)bytes[7])<<56 ) |
        ( ((NPT_UInt64)bytes[6])<<48 ) |
        ( ((NPT_UInt64)bytes[5])<<40 ) |
        ( ((NPT_UInt64)bytes[4])<<32 ) |
        ( ((NPT_UInt64)bytes[3])<<24 ) |
        ( ((NPT_UInt64)bytes[2])<<16 ) |
        ( ((NPT_UInt64)bytes[1])<<8  ) |
        ( ((NPT_UInt64)bytes[0])     );    
}

/*----------------------------------------------------------------------
|   NPT_BytesToInt32Le
+---------------------------------------------------------------------*/
NPT_UInt32 
NPT_BytesToInt32Le(const unsigned char* bytes)
{
    return 
        ( ((NPT_UInt32)bytes[3])<<24 ) |
        ( ((NPT_UInt32)bytes[2])<<16 ) |
        ( ((NPT_UInt32)bytes[1])<<8  ) |
        ( ((NPT_UInt32)bytes[0])     );    
}

/*----------------------------------------------------------------------
|   NPT_BytesToInt24Le
+---------------------------------------------------------------------*/
NPT_UInt32 
NPT_BytesToInt24Le(const unsigned char* bytes)
{
    return 
        ( ((NPT_UInt32)bytes[2])<<16 ) |
        ( ((NPT_UInt32)bytes[1])<<8  ) |
        ( ((NPT_UInt32)bytes[0])     );    
}

/*----------------------------------------------------------------------
|   NPT_BytesToInt16Le
+---------------------------------------------------------------------*/
NPT_UInt16
NPT_BytesToInt16Le(const unsigned char* bytes)
{
    return 
        ( ((NPT_UInt16)bytes[1])<<8  ) |
        ( ((NPT_UInt16)bytes[0])     );    
}

/*----------------------------------------------------------------------
|    NPT_BytesFromInt64Be
+---------------------------------------------------------------------*/
void 
NPT_BytesFromInt64Be(unsigned char* buffer, NPT_UInt64 value)
{
    buffer[0] = (unsigned char)(value>>56) & 0xFF;
    buffer[1] = (unsigned char)(value>>48) & 0xFF;
    buffer[2] = (unsigned char)(value>>40) & 0xFF;
    buffer[3] = (unsigned char)(value>>32) & 0xFF;
    buffer[4] = (unsigned char)(value>>24) & 0xFF;
    buffer[5] = (unsigned char)(value>>16) & 0xFF;
    buffer[6] = (unsigned char)(value>> 8) & 0xFF;
    buffer[7] = (unsigned char)(value    ) & 0xFF;
}

/*----------------------------------------------------------------------
|    NPT_BytesFromInt32Be
+---------------------------------------------------------------------*/
void 
NPT_BytesFromInt32Be(unsigned char* buffer, NPT_UInt32 value)
{
    buffer[0] = (unsigned char)(value>>24) & 0xFF;
    buffer[1] = (unsigned char)(value>>16) & 0xFF;
    buffer[2] = (unsigned char)(value>> 8) & 0xFF;
    buffer[3] = (unsigned char)(value    ) & 0xFF;
}

/*----------------------------------------------------------------------
|    NPT_BytesFromInt24Be
+---------------------------------------------------------------------*/
void 
NPT_BytesFromInt24Be(unsigned char* buffer, NPT_UInt32 value)
{
    buffer[0] = (unsigned char)(value>>16) & 0xFF;
    buffer[1] = (unsigned char)(value>> 8) & 0xFF;
    buffer[2] = (unsigned char)(value    ) & 0xFF;
}

/*----------------------------------------------------------------------
|    NPT_BytesFromInt16Be
+---------------------------------------------------------------------*/
void 
NPT_BytesFromInt16Be(unsigned char* buffer, NPT_UInt16 value)
{
    buffer[0] = (unsigned char)((value>> 8) & 0xFF);
    buffer[1] = (unsigned char)((value    ) & 0xFF);
}

/*----------------------------------------------------------------------
|    NPT_BytesFromInt64Le
+---------------------------------------------------------------------*/
void 
NPT_BytesFromInt64Le(unsigned char* buffer, NPT_UInt64 value)
{
    buffer[7] = (unsigned char)(value>>56) & 0xFF;
    buffer[6] = (unsigned char)(value>>48) & 0xFF;
    buffer[5] = (unsigned char)(value>>40) & 0xFF;
    buffer[4] = (unsigned char)(value>>32) & 0xFF;
    buffer[3] = (unsigned char)(value>>24) & 0xFF;
    buffer[2] = (unsigned char)(value>>16) & 0xFF;
    buffer[1] = (unsigned char)(value>> 8) & 0xFF;
    buffer[0] = (unsigned char)(value    ) & 0xFF;
}

/*----------------------------------------------------------------------
|    NPT_BytesFromInt32Le
+---------------------------------------------------------------------*/
void 
NPT_BytesFromInt32Le(unsigned char* buffer, NPT_UInt32 value)
{
    buffer[3] = (unsigned char)(value>>24) & 0xFF;
    buffer[2] = (unsigned char)(value>>16) & 0xFF;
    buffer[1] = (unsigned char)(value>> 8) & 0xFF;
    buffer[0] = (unsigned char)(value    ) & 0xFF;
}

/*----------------------------------------------------------------------
|    NPT_BytesFromInt24Le
+---------------------------------------------------------------------*/
void 
NPT_BytesFromInt24Le(unsigned char* buffer, NPT_UInt32 value)
{
    buffer[2] = (unsigned char)(value>>16) & 0xFF;
    buffer[1] = (unsigned char)(value>> 8) & 0xFF;
    buffer[0] = (unsigned char)(value    ) & 0xFF;
}

/*----------------------------------------------------------------------
|    NPT_BytesFromInt16Le
+---------------------------------------------------------------------*/
void 
NPT_BytesFromInt16Le(unsigned char* buffer, NPT_UInt16 value)
{
    buffer[1] = (unsigned char)((value>> 8) & 0xFF);
    buffer[0] = (unsigned char)((value    ) & 0xFF);
}

#if !defined(NPT_CONFIG_HAVE_SNPRINTF)
/*----------------------------------------------------------------------
|   NPT_FormatString
+---------------------------------------------------------------------*/
int 
NPT_FormatString(char* /*str*/, NPT_Size /*size*/, const char* /*format*/, ...)
{
    NPT_ASSERT(0); // not implemented yet
    return 0;
}
#endif // NPT_CONFIG_HAVE_SNPRINTF

/*----------------------------------------------------------------------
|   NPT_NibbleToHex
+---------------------------------------------------------------------*/
char 
NPT_NibbleToHex(unsigned int nibble, bool uppercase /* = true */)
{
    NPT_ASSERT(nibble < 16);
    if (uppercase) {
        return (nibble < 10) ? ('0' + nibble) : ('A' + (nibble-10));
    } else {
        return (nibble < 10) ? ('0' + nibble) : ('a' + (nibble-10));
    }
    return (nibble < 10) ? ('0' + nibble) : ('A' + (nibble-10));
}

/*----------------------------------------------------------------------
|   NPT_HexToNibble
+---------------------------------------------------------------------*/
int 
NPT_HexToNibble(char hex)
{
    if (hex >= 'a' && hex <= 'f') {
        return ((hex - 'a') + 10);
    } else if (hex >= 'A' && hex <= 'F') {
        return ((hex - 'A') + 10);
    } else if (hex >= '0' && hex <= '9') {
        return (hex - '0');
    } else {
        return -1;
    }
}

/*----------------------------------------------------------------------
|   NPT_ByteToHex
+---------------------------------------------------------------------*/
void
NPT_ByteToHex(NPT_Byte b, char* buffer, bool uppercase)
{
    buffer[0] = NPT_NibbleToHex((b>>4) & 0x0F, uppercase);
    buffer[1] = NPT_NibbleToHex(b      & 0x0F, uppercase);
}

/*----------------------------------------------------------------------
|   NPT_HexToByte
+---------------------------------------------------------------------*/
NPT_Result
NPT_HexToByte(const char* buffer, NPT_Byte& b)
{
    int nibble_0 = NPT_HexToNibble(buffer[0]);
    if (nibble_0 < 0) return NPT_ERROR_INVALID_SYNTAX;
    
    int nibble_1 = NPT_HexToNibble(buffer[1]);
    if (nibble_1 < 0) return NPT_ERROR_INVALID_SYNTAX;

    b = (nibble_0 << 4) | nibble_1;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_HexToBytes
+---------------------------------------------------------------------*/
NPT_Result
NPT_HexToBytes(const char*    hex,
              NPT_DataBuffer& bytes)
{
    // check the size
    NPT_Size len = NPT_StringLength(hex);
    if ((len%2) != 0) return NPT_ERROR_INVALID_PARAMETERS;
    NPT_Size bytes_size = len / 2;
    NPT_Result result = bytes.SetDataSize(bytes_size);
    if (NPT_FAILED(result)) return result;
    
    // decode
    for (NPT_Ordinal i=0; i<bytes_size; i++) {
        result = NPT_HexToByte(hex+(i*2), *(bytes.UseData()+i));
        if (NPT_FAILED(result)) return result;
    }
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_HexString
+---------------------------------------------------------------------*/
NPT_String 
NPT_HexString(const unsigned char* data,
              NPT_Size             data_size,
              const char*          separator,
              bool                 uppercase)
{
    NPT_String result;
    
    // quick check 
    if (data == NULL || data_size == 0) return result;
        
    // set the result size
    NPT_Size separator_length = separator?NPT_StringLength(separator):0;
    result.SetLength(data_size*2+(data_size-1)*separator_length);
    
    // build the string
    const unsigned char* src = data;
    char* dst = result.UseChars();
    while (data_size--) {
        NPT_ByteToHex(*src++, dst, uppercase);
        dst += 2;
        if (data_size) {
            NPT_CopyMemory(dst, separator, separator_length);
            dst += separator_length;
        }
    }
    
    return result;
}

/*----------------------------------------------------------------------
|    NPT_ParseFloat
+---------------------------------------------------------------------*/
NPT_Result 
NPT_ParseFloat(const char* str, float& result, bool relaxed)
{
    // safe default value 
    result = 0.0f;

    // check params
    if (str == NULL || *str == '\0') {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // ignore leading whitespace
    if (relaxed) {
        while (*str == ' ' || *str == '\t') {
            str++;
        }
    }
    if (*str == '\0') {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // check for sign
    bool negative = false;
    if (*str == '-') {
        // negative number
        negative = true; 
        str++;
    } else if (*str == '+') {
        // skip the + sign
        str++;
    }

    // parse the digits
    bool  after_radix = false;
    bool  empty = true;
    float value = 0.0f;
    float decimal = 10.0f;
    char  c;
    while ((c = *str++)) {
        if (c == '.') {
            if (after_radix || (*str < '0' || *str > '9')) {
                return NPT_ERROR_INVALID_PARAMETERS;
            } else {
                after_radix = true;
            }
        } else if (c >= '0' && c <= '9') {
            empty = false;
            if (after_radix) {
                value += (float)(c-'0')/decimal;
                decimal *= 10.0f;
            } else {
                value = 10.0f*value + (float)(c-'0');
            }
        } else if (c == 'e' || c == 'E') {
            // exponent
            if (*str == '+' || *str == '-' || (*str >= '0' && *str <= '9')) {
                int exponent = 0;
                if (NPT_SUCCEEDED(NPT_ParseInteger(str, exponent, relaxed))) {
                    value *= (float)pow(10.0f, (float)exponent);
                    break;
                } else {
                    return NPT_ERROR_INVALID_PARAMETERS;
                }
            } else {
                return NPT_ERROR_INVALID_PARAMETERS;
            }
        } else {
            if (relaxed) {
                break;
            } else {
                return NPT_ERROR_INVALID_PARAMETERS;
            }
        } 
    }

    // check that the value was non empty
    if (empty) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // return the result
    result = negative ? -value : value;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|    NPT_ParseInteger64
+---------------------------------------------------------------------*/
NPT_Result 
NPT_ParseInteger64(const char* str, NPT_Int64& result, bool relaxed, NPT_Cardinal* chars_used)
{
    // safe default value
    result = 0;
    if (chars_used) *chars_used = 0;

    if (str == NULL) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // ignore leading whitespace
    if (relaxed) {
        while (*str == ' ' || *str == '\t') {
            str++;
            if (chars_used) (*chars_used)++;
        }
    }
    if (*str == '\0') {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // check for sign
    bool negative = false;
    if (*str == '-') {
        // negative number
        negative = true; 
        str++;
        if (chars_used) (*chars_used)++;
    } else if (*str == '+') {
        // skip the + sign
        str++;
        if (chars_used) (*chars_used)++;
    }

    // check for overflows
    NPT_Int64 max = NPT_INT64_MAX/10;

    // adjust the max for overflows when the value is negative
    if (negative && ((NPT_INT64_MAX%10) == 9)) ++max;

    // parse the digits
    bool      empty = true;
    NPT_Int64 value = 0;
    char c;
    while ((c = *str++)) {
        if (c >= '0' && c <= '9') {
            if (value < 0 || value > max) return NPT_ERROR_OVERFLOW;
            value = 10*value + (c-'0');
            if (value < 0 && (!negative || value != NPT_INT64_MIN)) return NPT_ERROR_OVERFLOW;
            empty = false;
            if (chars_used) (*chars_used)++;
        } else {
            if (relaxed) {
                break;
            } else {
                return NPT_ERROR_INVALID_PARAMETERS;
            }
        } 
    }

    // check that the value was non empty
    if (empty) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // return the result
    result = negative ? -value : value;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|    NPT_ParseInteger64
+---------------------------------------------------------------------*/
NPT_Result 
NPT_ParseInteger64(const char* str, NPT_UInt64& result, bool relaxed, NPT_Cardinal* chars_used)
{
    // safe default value
    result = 0;
    if (chars_used) *chars_used = 0;

    if (str == NULL) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // ignore leading whitespace
    if (relaxed) {
        while (*str == ' ' || *str == '\t') {
            str++;
            if (chars_used) (*chars_used)++;
        }
    }
    if (*str == '\0') {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // parse the digits
    bool       empty = true;
    NPT_UInt64 value = 0;
    char c;
    while ((c = *str++)) {
        if (c >= '0' && c <= '9') {
            NPT_UInt64 new_value;
            if (value > NPT_UINT64_MAX/10)  return NPT_ERROR_OVERFLOW;
            new_value = 10*value + (c-'0');
            if (new_value < value) return NPT_ERROR_OVERFLOW;
            value = new_value;
            empty = false;
            if (chars_used) (*chars_used)++;
        } else {
            if (relaxed) {
                break;
            } else {
                return NPT_ERROR_INVALID_PARAMETERS;
            }
        } 
    }

    // check that the value was non empty
    if (empty) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // return the result
    result = value;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|    NPT_ParseInteger32
+---------------------------------------------------------------------*/
NPT_Result 
NPT_ParseInteger32(const char* str, NPT_Int32& value, bool relaxed, NPT_Cardinal* chars_used)
{
    NPT_Int64 value_64;
    NPT_Result result = NPT_ParseInteger64(str, value_64, relaxed, chars_used);
    value = 0;
    if (NPT_SUCCEEDED(result)) {
        if (value_64 < NPT_INT32_MIN || value_64 > NPT_INT32_MAX) {
            return NPT_ERROR_OVERFLOW;
        }
        value = (NPT_Int32)value_64;
    }
    return result;
}

/*----------------------------------------------------------------------
|     NPT_ParseInteger32
+---------------------------------------------------------------------*/
NPT_Result 
NPT_ParseInteger32(const char* str, NPT_UInt32& value, bool relaxed, NPT_Cardinal* chars_used)
{
    NPT_UInt64 value_64;
    NPT_Result result = NPT_ParseInteger64(str, value_64, relaxed, chars_used);
    value = 0;
    if (NPT_SUCCEEDED(result)) {
        if (value_64 > (NPT_UInt64)NPT_UINT32_MAX) return NPT_ERROR_OVERFLOW;
        value = (NPT_UInt32)value_64;
    }
    return result;
}

/*----------------------------------------------------------------------
|    NPT_ParseInteger
+---------------------------------------------------------------------*/
NPT_Result 
NPT_ParseInteger(const char* str, long& value, bool relaxed, NPT_Cardinal* chars_used)
{
    NPT_Int64 value_64;
    NPT_Result result = NPT_ParseInteger64(str, value_64, relaxed, chars_used);
    value = 0;
    if (NPT_SUCCEEDED(result)) {
        if (value_64 < NPT_LONG_MIN || value_64 > NPT_LONG_MAX) {
            return NPT_ERROR_OVERFLOW;
        }
        value = (long)value_64;
    }
    return result;
}

/*----------------------------------------------------------------------
|    NPT_ParseInteger
+---------------------------------------------------------------------*/
NPT_Result 
NPT_ParseInteger(const char* str, unsigned long& value, bool relaxed, NPT_Cardinal* chars_used)
{
    NPT_UInt64 value_64;
    NPT_Result result = NPT_ParseInteger64(str, value_64, relaxed, chars_used);
    value = 0;
    if (NPT_SUCCEEDED(result)) {
        if (value_64 > NPT_ULONG_MAX) {
            return NPT_ERROR_OVERFLOW;
        }
        value = (unsigned long)value_64;
    }
    return result;
}

/*----------------------------------------------------------------------
|    NPT_ParseInteger
+---------------------------------------------------------------------*/
NPT_Result 
NPT_ParseInteger(const char* str, int& value, bool relaxed, NPT_Cardinal* chars_used)
{
    NPT_Int64 value_64;
    NPT_Result result = NPT_ParseInteger64(str, value_64, relaxed, chars_used);
    value = 0;
    if (NPT_SUCCEEDED(result)) {
        if (value_64 < NPT_INT_MIN || value_64 > NPT_INT_MAX) {
            return NPT_ERROR_OVERFLOW;
        }
        value = (int)value_64;
    }
    return result;
}

/*----------------------------------------------------------------------
|    NPT_ParseInteger
+---------------------------------------------------------------------*/
NPT_Result 
NPT_ParseInteger(const char* str, unsigned int& value, bool relaxed, NPT_Cardinal* chars_used)
{
    NPT_UInt64 value_64;
    NPT_Result result = NPT_ParseInteger64(str, value_64, relaxed, chars_used);
    value = 0;
    if (NPT_SUCCEEDED(result)) {
        if (value_64 > NPT_UINT_MAX) {
            return NPT_ERROR_OVERFLOW;
        }
        value = (unsigned int)value_64;
    }
    return result;
}

#if !defined(NPT_CONFIG_HAVE_STRCPY)
/*----------------------------------------------------------------------
|    NPT_CopyString
+---------------------------------------------------------------------*/
void
NPT_CopyString(char* dst, const char* src)
{
    while(*dst++ = *src++);
}
#endif

/*----------------------------------------------------------------------
|   NPT_FormatOutput
+---------------------------------------------------------------------*/
void
NPT_FormatOutput(void        (*function)(void* parameter, const char* message),
                 void*       function_parameter,
                 const char* format, 
                 va_list     args)
{
    char         local_buffer[NPT_FORMAT_LOCAL_BUFFER_SIZE];
    unsigned int buffer_size = NPT_FORMAT_LOCAL_BUFFER_SIZE;
    char*        buffer = local_buffer;

    for(;;) {
        int result;

        /* try to format the message (it might not fit) */
        result = NPT_FormatStringVN(buffer, buffer_size-1, format, args);
        buffer[buffer_size-1] = 0; /* force a NULL termination */
        if (result >= 0) break;

        /* the buffer was too small, try something bigger */
        buffer_size = (buffer_size+NPT_FORMAT_BUFFER_INCREMENT)*2;
        if (buffer_size > NPT_FORMAT_BUFFER_MAX_SIZE) break;
        if (buffer != local_buffer) delete[] buffer;
        buffer = new char[buffer_size];
        if (buffer == NULL) return;
    }

    (*function)(function_parameter, buffer);
    if (buffer != local_buffer) delete[] buffer;
}

/*----------------------------------------------------------------------
|   local types
+---------------------------------------------------------------------*/
typedef enum {
    NPT_MIME_PARAMETER_PARSER_STATE_NEED_NAME,
    NPT_MIME_PARAMETER_PARSER_STATE_IN_NAME,
    NPT_MIME_PARAMETER_PARSER_STATE_NEED_EQUALS,
    NPT_MIME_PARAMETER_PARSER_STATE_NEED_VALUE,
    NPT_MIME_PARAMETER_PARSER_STATE_IN_VALUE,
    NPT_MIME_PARAMETER_PARSER_STATE_IN_QUOTED_VALUE,
    NPT_MIME_PARAMETER_PARSER_STATE_NEED_SEPARATOR
} NPT_MimeParameterParserState;

/*----------------------------------------------------------------------
|   NPT_ParseMimeParameters
|
|     From RFC 822 and RFC 2045
|
|                                                 ; (  Octal, Decimal.)
|     CHAR        =  <any ASCII character>        ; (  0-177,  0.-127.)
|     ALPHA       =  <any ASCII alphabetic character>
|                                                 ; (101-132, 65.- 90.)
|                                                 ; (141-172, 97.-122.)
|     DIGIT       =  <any ASCII decimal digit>    ; ( 60- 71, 48.- 57.)
|     CTL         =  <any ASCII control           ; (  0- 37,  0.- 31.)
|                     character and DEL>          ; (    177,     127.)
|     CR          =  <ASCII CR, carriage return>  ; (     15,      13.)
|     LF          =  <ASCII LF, linefeed>         ; (     12,      10.)
|     SPACE       =  <ASCII SP, space>            ; (     40,      32.)
|     HTAB        =  <ASCII HT, horizontal-tab>   ; (     11,       9.)
|     <">         =  <ASCII quote mark>           ; (     42,      34.)
|     CRLF        =  CR LF
|
|     LWSP-char   =  SPACE / HTAB                 ; semantics = SPACE
|
|     linear-white-space =  1*([CRLF] LWSP-char)  ; semantics = SPACE
|                                                 ; CRLF => folding
|
|     parameter := attribute "=" value
|
|     attribute := token
|                  ; Matching of attributes
|                  ; is ALWAYS case-insensitive.
|
|     value := token / quoted-string
|
|     token := 1*<any (US-ASCII) CHAR except SPACE, CTLs, or tspecials>
|
|     tspecials :=  "(" / ")" / "<" / ">" / "@" /
|                   "," / ";" / ":" / "\" / <">
|                   "/" / "[" / "]" / "?" / "="
|
|     quoted-string = <"> *(qtext/quoted-pair) <">; Regular qtext or
|                                                 ;   quoted chars.
|
|     qtext       =  <any CHAR excepting <">,     ; => may be folded
|                     "\" & CR, and including
|                     linear-white-space>
|
|     quoted-pair =  "\" CHAR                     ; may quote any char
|
+---------------------------------------------------------------------*/
NPT_Result 
NPT_ParseMimeParameters(const char*                      encoded,
                        NPT_Map<NPT_String, NPT_String>& parameters)
{
    // check parameters
    if (encoded == NULL) return NPT_ERROR_INVALID_PARAMETERS;
    
    // reserve some space 
    NPT_String param_name;
    NPT_String param_value;
    param_name.Reserve(64);
    param_value.Reserve(64);

    NPT_MimeParameterParserState state = NPT_MIME_PARAMETER_PARSER_STATE_NEED_NAME;
    bool quoted_char = false;
    for (;;) {
        char c = *encoded++;
        if (!quoted_char && (c == 0x0A || c == 0x0D)) continue; // ignore EOL chars
        switch (state) {
            case NPT_MIME_PARAMETER_PARSER_STATE_NEED_NAME:
                if (c == '\0') break; // END
                if (c == ' ' || c == '\t') continue; // ignore leading whitespace
                if (c <  ' ') return NPT_ERROR_INVALID_SYNTAX; // CTLs are invalid
                param_name += c; // we're not strict: accept all other chars
                state = NPT_MIME_PARAMETER_PARSER_STATE_IN_NAME;
                break;
                
            case NPT_MIME_PARAMETER_PARSER_STATE_IN_NAME:
                if (c <  ' ') return NPT_ERROR_INVALID_SYNTAX; // END or CTLs are invalid
                if (c == ' ') {
                    state = NPT_MIME_PARAMETER_PARSER_STATE_NEED_EQUALS;
                } else if (c == '=') {
                    state = NPT_MIME_PARAMETER_PARSER_STATE_NEED_VALUE;
                } else {
                    param_name += c; // we're not strict: accept all other chars
                }
                break;
                
            case NPT_MIME_PARAMETER_PARSER_STATE_NEED_EQUALS:
                if (c <  ' ') return NPT_ERROR_INVALID_SYNTAX; // END or CTLs are invalid
                if (c == ' ' || c == '\t') continue; // ignore leading whitespace
                if (c != '=') return NPT_ERROR_INVALID_SYNTAX;
                state = NPT_MIME_PARAMETER_PARSER_STATE_NEED_VALUE;
                break;

            case NPT_MIME_PARAMETER_PARSER_STATE_NEED_VALUE:
                if (c <  ' ') return NPT_ERROR_INVALID_SYNTAX; // END or CTLs are invalid
                if (c == ' ' || c == '\t') continue; // ignore leading whitespace
                if (c == '"') {
                    state = NPT_MIME_PARAMETER_PARSER_STATE_IN_QUOTED_VALUE;
                } else {
                    param_value += c; // we're not strict: accept all other chars
                    state = NPT_MIME_PARAMETER_PARSER_STATE_IN_VALUE;
                }
                break;
                
            case NPT_MIME_PARAMETER_PARSER_STATE_IN_QUOTED_VALUE:
                if (quoted_char) {
                    quoted_char = false;
                    if (c == '\0') return NPT_ERROR_INVALID_SYNTAX;
                    param_value += c; // accept all chars
                    break;
                } else if (c == '\\') {
                    quoted_char = true;
                    break;
                } else if (c == '"') {
                    // add the parameter to the map
                    param_name.TrimRight();
                    param_value.TrimRight();
                    parameters[param_name] = param_value;
                    param_name.SetLength(0);
                    param_value.SetLength(0);
                    state = NPT_MIME_PARAMETER_PARSER_STATE_NEED_SEPARATOR;
                } else if (c <  ' ') {
                    return NPT_ERROR_INVALID_SYNTAX; // END or CTLs are invalid
                } else {
                    param_value += c; // we're not strict: accept all other chars
                }
                break;
                
            case NPT_MIME_PARAMETER_PARSER_STATE_IN_VALUE:
                if (c == '\0' || c == ';') {
                    // add the parameter to the map
                    param_name.TrimRight();
                    param_value.TrimRight();
                    parameters[param_name] = param_value;
                    param_name.SetLength(0);
                    param_value.SetLength(0);
                    state = NPT_MIME_PARAMETER_PARSER_STATE_NEED_NAME;
                } else if (c < ' ') {
                    // CTLs are invalid
                    return NPT_ERROR_INVALID_SYNTAX;
                } else {
                    param_value += c; // we're not strict: accept all other chars
                }
                break;

            case NPT_MIME_PARAMETER_PARSER_STATE_NEED_SEPARATOR:
                if (c == '\0') break;
                if (c <  ' ') return NPT_ERROR_INVALID_SYNTAX; // CTLs are invalid
                if (c == ' ' || c == '\t') continue; // ignore whitespace
                if (c != ';') return NPT_ERROR_INVALID_SYNTAX;
                state = NPT_MIME_PARAMETER_PARSER_STATE_NEED_NAME;
                break;
        }
        if (c == '\0') break; // end of buffer
    }
    
    return NPT_SUCCESS;
}

