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

#ifndef _NPT_BASE64_H_
#define _NPT_BASE64_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptDataBuffer.h"
#include "NptStrings.h"

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
const NPT_Cardinal NPT_BASE64_MIME_BLOCKS_PER_LINE = 19;
const NPT_Cardinal NPT_BASE64_PEM_BLOCKS_PER_LINE  = 16;

/*----------------------------------------------------------------------
|   NPT_Base64
+---------------------------------------------------------------------*/
class NPT_Base64 {
public:
    // class methods
    static NPT_Result Decode(const char*     base64, 
                             NPT_Size        size,
                             NPT_DataBuffer& data,
                             bool            url_safe = false);
    static NPT_Result Encode(const NPT_Byte* data, 
                             NPT_Size        size, 
                             NPT_String&     base64, 
                             NPT_Cardinal    max_blocks_per_line = 0, 
                             bool            url_safe = false);

private: 
    // this class is purely static
    NPT_Base64();
};

#endif // _NPT_BASE64_H_
