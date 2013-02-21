/*****************************************************************
|
|   Neptune - Buffered Byte Stream
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

#ifndef _NPT_BUFFERED_STREAMS_H_
#define _NPT_BUFFERED_STREAMS_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptStreams.h"
#include "NptTypes.h"
#include "NptConstants.h"
#include "NptStrings.h"
#include "NptDebug.h"

/*----------------------------------------------------------------------
|   NPT_BufferedStream
+---------------------------------------------------------------------*/
const NPT_Size NPT_BUFFERED_BYTE_STREAM_DEFAULT_SIZE = 4096;

/*----------------------------------------------------------------------
|   NPT_BufferedInputStream
+---------------------------------------------------------------------*/
class NPT_BufferedInputStream : public NPT_InputStream
{
public:
    // constructors and destructor
    NPT_BufferedInputStream(NPT_InputStreamReference& stream,
                            NPT_Size buffer_size = NPT_BUFFERED_BYTE_STREAM_DEFAULT_SIZE);
    virtual ~NPT_BufferedInputStream();

    // methods
    virtual NPT_Result ReadLine(NPT_String& line,
                                NPT_Size    max_chars = 4096,
                                bool        break_on_cr = false);
    virtual NPT_Result ReadLine(char*     buffer, 
                                NPT_Size  buffer_size,
                                NPT_Size* chars_read = NULL,
                                bool      break_on_cr = false);
    virtual NPT_Result SetBufferSize(NPT_Size size, bool force = false);
    virtual NPT_Result Peek(void*     buffer, 
                            NPT_Size  bytes_to_read, 
                            NPT_Size* bytes_read);
                              
    // NPT_InputStream methods
    NPT_Result Read(void*     buffer, 
                    NPT_Size  bytes_to_read, 
                    NPT_Size* bytes_read = NULL);
    NPT_Result Seek(NPT_Position offset);
    NPT_Result Tell(NPT_Position& offset);
    NPT_Result GetSize(NPT_LargeSize& size);
    NPT_Result GetAvailable(NPT_LargeSize& available);

protected:
    // members
    NPT_InputStreamReference m_Source;
    bool                     m_SkipNewline;
    bool                     m_Eos;
    struct {
        NPT_Byte* data;
        NPT_Size  offset;
        NPT_Size  valid;
        NPT_Size  size;
    } m_Buffer;

    // methods
    virtual NPT_Result FillBuffer();
    virtual NPT_Result ReleaseBuffer();
};

typedef NPT_Reference<NPT_BufferedInputStream> NPT_BufferedInputStreamReference;

#endif // _NPT_BUFFERED_STREAMS_H_
