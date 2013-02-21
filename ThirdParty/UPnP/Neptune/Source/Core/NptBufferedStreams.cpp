/*****************************************************************
|
|   Neptune - Buffered Streams
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
#include "NptTypes.h"
#include "NptInterfaces.h"
#include "NptConstants.h"
#include "NptBufferedStreams.h"
#include "NptUtils.h"

#define NPT_CHECK_NOLOGTIMEOUT(_x)   \
do {                                 \
    NPT_Result __result = (_x);      \
    if (__result != NPT_SUCCESS) {   \
        if (__result != NPT_ERROR_TIMEOUT && __result != NPT_ERROR_EOS) { \
            NPT_CHECK(__result);     \
        }                            \
        return __result;             \
    }                                \
} while(0)

/*----------------------------------------------------------------------
|   NPT_BufferedInputStream::NPT_BufferedInputStream
+---------------------------------------------------------------------*/
NPT_BufferedInputStream::NPT_BufferedInputStream(NPT_InputStreamReference& source, NPT_Size buffer_size) :
    m_Source(source),
    m_SkipNewline(false),
    m_Eos(false)
{
    // setup the read buffer
    m_Buffer.data     = NULL;
    m_Buffer.offset   = 0;
    m_Buffer.valid    = 0;
    m_Buffer.size     = buffer_size;
}

/*----------------------------------------------------------------------
|   NPT_BufferedInputStream::~NPT_BufferedInputStream
+---------------------------------------------------------------------*/
NPT_BufferedInputStream::~NPT_BufferedInputStream()
{
    // release the buffer
    delete[] m_Buffer.data;
}

/*----------------------------------------------------------------------
|   NPT_BufferedInputStream::SetBufferSize
+---------------------------------------------------------------------*/
NPT_Result
NPT_BufferedInputStream::SetBufferSize(NPT_Size size, bool force /* = false */)
{
    if (m_Buffer.data != NULL) {
        // we already have a buffer
        if (m_Buffer.size < size || force) {
            // the current buffer is too small or we want to move
            // existing data to the beginning of the buffer, reallocate
            NPT_Byte* buffer = new NPT_Byte[size];
            if (buffer == NULL) return NPT_ERROR_OUT_OF_MEMORY;

            // copy existing data
            NPT_Size need_to_copy = m_Buffer.valid - m_Buffer.offset;
            if (need_to_copy) {
                NPT_CopyMemory((void*)buffer, 
                               m_Buffer.data+m_Buffer.offset,
                               need_to_copy);
            }

            // use the new buffer
            delete[] m_Buffer.data;
            m_Buffer.data = buffer;
            m_Buffer.valid -= m_Buffer.offset;
            m_Buffer.offset = 0;
        }
    }
    m_Buffer.size = size;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_BufferedInputStream::FillBuffer
+---------------------------------------------------------------------*/
NPT_Result
NPT_BufferedInputStream::FillBuffer()
{
    // shortcut
    if (m_Eos) return NPT_ERROR_EOS;

    // check that there is nothing left in the buffer and the buffer
    // size is not 0
    NPT_ASSERT(m_Buffer.valid == m_Buffer.offset);
    NPT_ASSERT(m_Buffer.size != 0);

    // allocate the read buffer if it has not been done yet
    if (m_Buffer.data == NULL) {
        m_Buffer.data = new NPT_Byte[m_Buffer.size];
        if (m_Buffer.data == NULL) return NPT_ERROR_OUT_OF_MEMORY;
    }

    // refill the buffer
    m_Buffer.offset = 0;
    NPT_Result result = m_Source->Read(m_Buffer.data, m_Buffer.size, &m_Buffer.valid);
    if (NPT_FAILED(result)) m_Buffer.valid = 0;
    return result;
}

/*----------------------------------------------------------------------
|   NPT_BufferedInputStream::ReleaseBuffer
+---------------------------------------------------------------------*/
NPT_Result
NPT_BufferedInputStream::ReleaseBuffer()
{
    NPT_ASSERT(m_Buffer.size == 0);
    NPT_ASSERT(m_Buffer.offset == m_Buffer.valid);

    delete[] m_Buffer.data;
    m_Buffer.data = NULL;
    m_Buffer.offset = 0;
    m_Buffer.valid = 0;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_BufferedInputStream::ReadLine
+---------------------------------------------------------------------*/
NPT_Result
NPT_BufferedInputStream::ReadLine(char*     buffer, 
                                  NPT_Size  size, 
                                  NPT_Size* chars_read,
                                  bool      break_on_cr)
{
    NPT_Result result = NPT_SUCCESS;
    char*      buffer_start = buffer;
    char*      buffer_end   = buffer_start+size-1;
    bool       skip_newline = false;

    // check parameters
    if (buffer == NULL || size < 1) {
        if (chars_read) *chars_read = 0;
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // read until EOF or newline
    for (;;) {
        while (m_Buffer.offset != m_Buffer.valid) {
            // there is some data left in the buffer
            NPT_Byte c = m_Buffer.data[m_Buffer.offset++];
            if (c == '\r') {
                if (break_on_cr) {
                    skip_newline = true;
                    goto done;
                }
            } else if (c == '\n') {
                if (m_SkipNewline && (buffer == buffer_start)) {
                    continue;
                }
                goto done;
            } else {
                if (buffer == buffer_end) {
                    result = NPT_ERROR_NOT_ENOUGH_SPACE;
                    goto done;
                }
                *buffer++ = c;
            }
        }

        if (m_Buffer.size == 0 && !m_Eos) {
            // unbuffered mode
            if (m_Buffer.data != NULL) ReleaseBuffer();
            while (NPT_SUCCEEDED(result = m_Source->Read(buffer, 1, NULL))) {
                if (*buffer == '\r') {
                    if (break_on_cr) {
                        skip_newline = true;
                        goto done;
                    }
                } else if (*buffer == '\n') {
                    goto done;
                } else {
                    if (buffer == buffer_end) {
                        result = NPT_ERROR_NOT_ENOUGH_SPACE;
                        goto done;
                    }
                    ++buffer;
                }
            }
        } else {
            // refill the buffer
            result = FillBuffer();
        }
        if (NPT_FAILED(result)) goto done;
    }

done:
    // update the newline skipping state
    m_SkipNewline = skip_newline;

    // NULL-terminate the line
    *buffer = '\0';

    // return what we have
    if (chars_read) *chars_read = (NPT_Size)(buffer-buffer_start);
    if (result == NPT_ERROR_EOS) {
        m_Eos = true;
        if (buffer != buffer_start) {
            // we have reached the end of the stream, but we have read
            // some chars, so do not return EOS now
            return NPT_SUCCESS;
        }
    }
    return result;
}

/*----------------------------------------------------------------------
|   NPT_BufferedInputStream::ReadLine
+---------------------------------------------------------------------*/
NPT_Result
NPT_BufferedInputStream::ReadLine(NPT_String& line,
                                  NPT_Size    max_chars,
                                  bool        break_on_cr)
{
    // clear the line
    line.SetLength(0);

    // reserve space for the chars
    line.Reserve(max_chars);

    // read the line
    NPT_Size chars_read = 0;
    NPT_CHECK_NOLOGTIMEOUT(ReadLine(line.UseChars(), max_chars, &chars_read, break_on_cr));

    // adjust the length of the string object
    line.SetLength(chars_read);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_BufferedInputStream::Read
+---------------------------------------------------------------------*/
NPT_Result 
NPT_BufferedInputStream::Read(void*     buffer, 
                              NPT_Size  bytes_to_read, 
                              NPT_Size* bytes_read)
{
    NPT_Result result = NPT_SUCCESS;
    NPT_Size   total_read = 0;
    NPT_Size   buffered;

    // check for a possible shortcut
    if (bytes_to_read == 0) return NPT_SUCCESS;

    // skip a newline char if needed
    if (m_SkipNewline) {
        m_SkipNewline = false;
        result = Read(buffer, 1, NULL);
        if (NPT_FAILED(result)) goto done;
        NPT_Byte c = *(NPT_Byte*)buffer;
        if (c != '\n') {
            buffer = (void*)((NPT_Byte*)buffer+1);
            --bytes_to_read;
            total_read = 1;
        }
    }

    // compute how much is buffered
    buffered = m_Buffer.valid-m_Buffer.offset;
    if (bytes_to_read > buffered) {
        // there is not enough in the buffer, take what's there
        if (buffered) {
            NPT_CopyMemory(buffer, 
                           m_Buffer.data + m_Buffer.offset,
                           buffered);
            m_Buffer.offset += buffered;
            total_read += buffered;
            goto done;
        }
        
        // read the rest from the source
        if (m_Buffer.size == 0) {
            // unbuffered mode, read directly into the supplied buffer
            if (m_Buffer.data != NULL) ReleaseBuffer(); // cleanup if necessary
            NPT_Size local_read = 0;
            result = m_Source->Read(buffer, bytes_to_read, &local_read);
            if (NPT_SUCCEEDED(result)) {
                total_read += local_read; 
            }
            goto done;
        } else {
            // refill the buffer
            result = FillBuffer();
            if (NPT_FAILED(result)) goto done;
            buffered = m_Buffer.valid;
            if (bytes_to_read > buffered) bytes_to_read = buffered;
        }
    }

    // get what we can from the buffer
    if (bytes_to_read) {
        NPT_CopyMemory(buffer, 
                       m_Buffer.data + m_Buffer.offset,
                       bytes_to_read);
        m_Buffer.offset += bytes_to_read;
        total_read += bytes_to_read;
    }
    
done:
    if (bytes_read) *bytes_read = total_read;
    if (result == NPT_ERROR_EOS) { 
        m_Eos = true;
        if (total_read != 0) {
            // we have reached the end of the stream, but we have read
            // some chars, so do not return EOS now
            return NPT_SUCCESS;
        }
    }
    return result;
}

/*----------------------------------------------------------------------
|   NPT_BufferedInputStream::Peek
+---------------------------------------------------------------------*/
NPT_Result 
NPT_BufferedInputStream::Peek(void*     buffer, 
                              NPT_Size  bytes_to_read, 
                              NPT_Size* bytes_read)
{
    NPT_Result result = NPT_SUCCESS;
    NPT_Size   buffered;
    NPT_Size   new_size = m_Buffer.size?m_Buffer.size:NPT_BUFFERED_BYTE_STREAM_DEFAULT_SIZE;
    
    // check for a possible shortcut
    if (bytes_to_read == 0) return NPT_SUCCESS;
    
    // compute how much is buffered
    buffered = m_Buffer.valid-m_Buffer.offset;
    if (bytes_to_read > buffered && buffered < new_size && !m_Eos) {
        // we need more data than what we have          
        // switch to unbuffered mode and resize to force relocation
        // of data to the beginning of the buffer
        SetBufferSize(new_size, true);
        // fill up the end of the buffer
        result = FillBuffer();
        // continue even if it failed
        buffered = m_Buffer.valid;
    }
    
    // make sure we're returning what we can
    if (bytes_to_read > buffered) bytes_to_read = buffered;
    
    // get what we can from the buffer
    NPT_CopyMemory(buffer, 
                   m_Buffer.data + m_Buffer.offset,
                   bytes_to_read);

    if (bytes_read) *bytes_read = bytes_to_read;
    if (result == NPT_ERROR_EOS) { 
        m_Eos = true;
        if (bytes_to_read != 0) {
            // we have reached the end of the stream, but we have read
            // some chars, so do not return EOS now
            return NPT_SUCCESS;
        }
    }
    return result;    
}

/*----------------------------------------------------------------------
|   NPT_BufferedInputStream::Seek
+---------------------------------------------------------------------*/
NPT_Result 
NPT_BufferedInputStream::Seek(NPT_Position /*offset*/)
{
    // not implemented yet
    return NPT_ERROR_NOT_IMPLEMENTED;
}

/*----------------------------------------------------------------------
|   NPT_BufferedInputStream::Tell
+---------------------------------------------------------------------*/
NPT_Result 
NPT_BufferedInputStream::Tell(NPT_Position& offset)
{
    // not implemented yet
    offset = 0;
    return NPT_ERROR_NOT_IMPLEMENTED;
}

/*----------------------------------------------------------------------
|   NPT_BufferedInputStream::GetSize
+---------------------------------------------------------------------*/
NPT_Result 
NPT_BufferedInputStream::GetSize(NPT_LargeSize& size)
{
    return m_Source->GetSize(size);
}

/*----------------------------------------------------------------------
|   NPT_BufferedInputStream::GetAvailable
+---------------------------------------------------------------------*/
NPT_Result 
NPT_BufferedInputStream::GetAvailable(NPT_LargeSize& available)
{
    NPT_LargeSize source_available = 0;
    NPT_Result    result = m_Source->GetAvailable(source_available);
    if (NPT_SUCCEEDED(result)) {
        available = m_Buffer.valid-m_Buffer.offset + source_available;
        return NPT_SUCCESS;
    } else {
        available = m_Buffer.valid-m_Buffer.offset;
        return available?NPT_SUCCESS:result;
    }
}
