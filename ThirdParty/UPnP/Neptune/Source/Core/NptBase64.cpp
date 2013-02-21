/*****************************************************************
|
|   Neptune - Base64
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
#include "NptBase64.h"
#include "NptUtils.h"
#include "NptResults.h"

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
static const signed char NPT_Base64_Bytes[128] = {
      -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
      -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
      -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1, 0x3E,   -1,   -1,   -1, 0x3F,
    0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D,   -1,   -1,   -1, 0x7F,   -1,   -1,
      -1, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 
    0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19,   -1,   -1,   -1,   -1,   -1,
      -1, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 
    0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F, 0x30, 0x31, 0x32, 0x33,   -1,   -1,   -1,   -1,   -1
};

static const char NPT_Base64_Chars[] = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
const char NPT_BASE64_PAD_CHAR = '=';
const char NPT_BASE64_PAD_BYTE = 0x7F;

/*----------------------------------------------------------------------
|   NPT_Base64::Decode
+---------------------------------------------------------------------*/
NPT_Result
NPT_Base64::Decode(const char*     base64, 
                   NPT_Size        size,
                   NPT_DataBuffer& data,
                   bool            url_safe /* = false */)
{
    // estimate the data size
    data.SetBufferSize(size);

    // reset the buffer
    data.SetDataSize(0);

    // keep a pointer to the buffer
    unsigned char* buffer = data.UseData();
    NPT_Size       data_size = 0;

    // iterate over all characters
    unsigned char codes[4];
    unsigned int code_count = 0;
    while (size--) {
        unsigned char c = *base64++;
        if (c >= NPT_ARRAY_SIZE(NPT_Base64_Bytes)) continue;
        if (url_safe) {
            // remap some characters
            if (c == '-') {
                c = '+';
            } else if (c == '_') {
                c = '/';
            }
        }
        signed char code = NPT_Base64_Bytes[c];
        if (code >= 0) {
            // valid code
            codes[code_count++] = code;
            if (code_count == 4) {
                // group complete
                if (codes[0] == NPT_BASE64_PAD_BYTE || codes[1] == NPT_BASE64_PAD_BYTE) {
                    return NPT_ERROR_INVALID_FORMAT;
                }
                if (codes[2] == NPT_BASE64_PAD_BYTE) {
                    // pad at char 3
                    if (codes[3] == NPT_BASE64_PAD_BYTE) {
                        // double padding
                        unsigned int packed = (codes[0]<<2)|(codes[1]>>4);
                        buffer[data_size++] = (unsigned char)packed;
                    } else {
                        // invalid padding
                        return NPT_ERROR_INVALID_FORMAT;
                    }
                } else if (codes[3] == NPT_BASE64_PAD_BYTE) {
                    // single padding
                    unsigned int packed = (codes[0]<<10)|(codes[1]<<4)|(codes[2]>>2);
                    buffer[data_size++] = (unsigned char)(packed >> 8);
                    buffer[data_size++] = (unsigned char)(packed     );
                } else {
                    // no padding
                    unsigned int packed = (codes[0]<<18)|(codes[1]<<12)|(codes[2]<<6)|codes[3];
                    buffer[data_size++] = (unsigned char)(packed >> 16);
                    buffer[data_size++] = (unsigned char)(packed >>  8);
                    buffer[data_size++] = (unsigned char)(packed      );
                }
                code_count = 0;
            }
        }
    }

    if (code_count) return NPT_ERROR_INVALID_FORMAT;

    // update the data size
    data.SetDataSize(data_size);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Base64::Encode
+---------------------------------------------------------------------*/
NPT_Result
NPT_Base64::Encode(const NPT_Byte* data, 
                   NPT_Size        size, 
                   NPT_String&     base64, 
                   NPT_Cardinal    max_blocks_per_line /* = 0 */, 
                   bool            url_safe /* = false */)
{
    unsigned int block_count = 0;
    unsigned int           i = 0;

    // reserve space for the string
    base64.Reserve(4*((size+3)/3) + 2*(max_blocks_per_line?(size/(3*max_blocks_per_line)):0));
    char* buffer = base64.UseChars();

    // encode each byte
    while (size >= 3) {
        // output a block
        *buffer++ = NPT_Base64_Chars[ (data[i  ] >> 2) & 0x3F];
        *buffer++ = NPT_Base64_Chars[((data[i  ] & 0x03) << 4) | ((data[i+1] >> 4) & 0x0F)];
        *buffer++ = NPT_Base64_Chars[((data[i+1] & 0x0F) << 2) | ((data[i+2] >> 6) & 0x03)];
        *buffer++ = NPT_Base64_Chars[  data[i+2] & 0x3F];

        size -= 3;
        i += 3;
        if (++block_count == max_blocks_per_line) {
            *buffer++ = '\r';
            *buffer++ = '\n';
            block_count = 0;
        }
    }

    // deal with the tail
    if (size == 2) {
        *buffer++ = NPT_Base64_Chars[ (data[i  ] >> 2) & 0x3F];
        *buffer++ = NPT_Base64_Chars[((data[i  ] & 0x03) << 4) | ((data[i+1] >> 4) & 0x0F)];
        *buffer++ = NPT_Base64_Chars[ (data[i+1] & 0x0F) << 2];
        *buffer++ = NPT_BASE64_PAD_CHAR;
    } else if (size == 1) {
        *buffer++ = NPT_Base64_Chars[(data[i] >> 2) & 0x3F];
        *buffer++ = NPT_Base64_Chars[(data[i] & 0x03) << 4];
        *buffer++ = NPT_BASE64_PAD_CHAR;
        *buffer++ = NPT_BASE64_PAD_CHAR;
    }

    // update the string size
    NPT_ASSERT((NPT_Size)(buffer-base64.GetChars()) <= base64.GetCapacity());
    base64.SetLength((NPT_Size)(buffer-base64.GetChars()));

    // deal with url safe remapping
    if (url_safe) {
        base64.Replace('+','-');
        base64.Replace('/','_');
    }

    return NPT_SUCCESS;
}
