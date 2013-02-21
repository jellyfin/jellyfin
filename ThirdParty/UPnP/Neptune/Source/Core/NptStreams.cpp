/*****************************************************************
|
|   Neptune - Byte Streams
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
#include "NptStreams.h"
#include "NptUtils.h"
#include "NptConstants.h"
#include "NptStrings.h"
#include "NptDebug.h"

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
const NPT_Size      NPT_INPUT_STREAM_LOAD_DEFAULT_READ_CHUNK = 4096;
const NPT_LargeSize NPT_INPUT_STREAM_LOAD_MAX_SIZE           = 0x40000000; // 1GB

/*----------------------------------------------------------------------
|   NPT_InputStream::Load
+---------------------------------------------------------------------*/
NPT_Result
NPT_InputStream::Load(NPT_DataBuffer& buffer, NPT_Size max_read /* = 0 */)
{
    NPT_Result    result;
    NPT_LargeSize total_bytes_read;

    // reset the buffer
    buffer.SetDataSize(0);

    // check the limits
    if (max_read > NPT_INPUT_STREAM_LOAD_MAX_SIZE) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // try to get the stream size
    NPT_LargeSize size;
    if (NPT_SUCCEEDED(GetSize(size))) { 
        // make sure we don't read more than max_read
        if (max_read && max_read < size) size = max_read;
        if (size > NPT_INPUT_STREAM_LOAD_MAX_SIZE) {
            return NPT_ERROR_OUT_OF_RANGE;
        }
    } else {
        size = max_read;
    } 
        
    // pre-allocate the buffer
    if (size) NPT_CHECK(buffer.Reserve((NPT_Size)size));

    // read the data from the file
    total_bytes_read = 0;
    do {
        NPT_LargeSize available = 0;
        NPT_LargeSize bytes_to_read;
        NPT_Size      bytes_read;
        NPT_Byte*     data;

        // check if we know how much data is available
        result = GetAvailable(available);
        if (NPT_SUCCEEDED(result) && available) {
            // we know how much is available
            bytes_to_read = available;
        } else {
            bytes_to_read = NPT_INPUT_STREAM_LOAD_DEFAULT_READ_CHUNK;
        }

        // make sure we don't read more than what was asked
        if (size != 0 && total_bytes_read+bytes_to_read>size) {
            bytes_to_read = size-total_bytes_read;
        }

        // stop if we've read everything
        if (bytes_to_read == 0) break;

        // ensure that the buffer has enough space
        if (total_bytes_read+bytes_to_read > NPT_INPUT_STREAM_LOAD_MAX_SIZE) {
            buffer.SetBufferSize(0);
            return NPT_ERROR_OUT_OF_RANGE;
        }
        NPT_CHECK(buffer.Reserve((NPT_Size)(total_bytes_read+bytes_to_read)));

        // read the data
        data = buffer.UseData()+total_bytes_read;
        result = Read((void*)data, (NPT_Size)bytes_to_read, &bytes_read);
        if (NPT_SUCCEEDED(result) && bytes_read != 0) {
            total_bytes_read += bytes_read;
            buffer.SetDataSize((NPT_Size)total_bytes_read);
        }
    } while(NPT_SUCCEEDED(result) && (size==0 || total_bytes_read < size));

    if (result == NPT_ERROR_EOS) {
        return NPT_SUCCESS;
    } else {
        return result;
    }
}

/*----------------------------------------------------------------------
|   NPT_InputStream::ReadFully
+---------------------------------------------------------------------*/
NPT_Result
NPT_InputStream::ReadFully(void* buffer, NPT_Size bytes_to_read)
{
    // shortcut
    if (bytes_to_read == 0) return NPT_SUCCESS;

    // read until failure
    NPT_Size bytes_read;
    while (bytes_to_read) {
        NPT_Result result = Read(buffer, bytes_to_read, &bytes_read);
        if (NPT_FAILED(result)) return result;
        if (bytes_read == 0) return NPT_ERROR_INTERNAL;
        NPT_ASSERT(bytes_read <= bytes_to_read);
        bytes_to_read -= bytes_read;
        buffer = (void*)(((NPT_Byte*)buffer)+bytes_read);
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_InputStream::ReadUI64
+---------------------------------------------------------------------*/
NPT_Result
NPT_InputStream::ReadUI64(NPT_UInt64& value)
{
    unsigned char buffer[8];

    // read bytes from the stream
    NPT_Result result;
    result = ReadFully((void*)buffer, 8);
    if (NPT_FAILED(result)) {
        value = 0;
        return result;
    }

    // convert bytes to value
    value = NPT_BytesToInt64Be(buffer);
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_InputStream::ReadUI32
+---------------------------------------------------------------------*/
NPT_Result
NPT_InputStream::ReadUI32(NPT_UInt32& value)
{
    unsigned char buffer[4];

    // read bytes from the stream
    NPT_Result result;
    result = ReadFully((void*)buffer, 4);
    if (NPT_FAILED(result)) {
        value = 0;
        return result;
    }

    // convert bytes to value
    value = NPT_BytesToInt32Be(buffer);
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_InputStream::ReadUI24
+---------------------------------------------------------------------*/
NPT_Result
NPT_InputStream::ReadUI24(NPT_UInt32& value)
{
    unsigned char buffer[3];

    // read bytes from the stream
    NPT_Result result;
    result = ReadFully((void*)buffer, 3);
    if (NPT_FAILED(result)) {
        value = 0;
        return result;
    }

    // convert bytes to value
    value = NPT_BytesToInt24Be(buffer);
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_InputStream::ReadUI16
+---------------------------------------------------------------------*/
NPT_Result
NPT_InputStream::ReadUI16(NPT_UInt16& value)
{
    unsigned char buffer[2];

    // read bytes from the stream
    NPT_Result result;
    result = ReadFully((void*)buffer, 2);
    if (NPT_FAILED(result)) {
        value = 0;
        return result;
    }

    // convert bytes to value
    value = NPT_BytesToInt16Be(buffer);
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_InputStream::ReadUI08
+---------------------------------------------------------------------*/
NPT_Result
NPT_InputStream::ReadUI08(NPT_UInt8& value)
{
    unsigned char buffer[1];

    // read bytes from the stream
    NPT_Result result;
    result = ReadFully((void*)buffer, 1);
    if (NPT_FAILED(result)) {        
        value = 0;
        return result;
    }

    // convert bytes to value
    value = buffer[0];
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_InputStream::Skip
+---------------------------------------------------------------------*/
NPT_Result
NPT_InputStream::Skip(NPT_Size count)
{
    // get the current location
    NPT_Position position;
    NPT_CHECK(Tell(position));

    // seek ahead
    return Seek(position+count);
}

/*----------------------------------------------------------------------
|   NPT_OutputStream::WriteFully
+---------------------------------------------------------------------*/
NPT_Result
NPT_OutputStream::WriteFully(const void* buffer, NPT_Size bytes_to_write)
{
    // shortcut
    if (bytes_to_write == 0) return NPT_SUCCESS;

    // write until failure
    NPT_Size bytes_written;
    while (bytes_to_write) {
        NPT_Result result = Write(buffer, bytes_to_write, &bytes_written);
        if (NPT_FAILED(result)) return result;
        if (bytes_written == 0) return NPT_ERROR_INTERNAL;
        NPT_ASSERT(bytes_written <= bytes_to_write);
        bytes_to_write -= bytes_written;
        buffer = (const void*)(((const NPT_Byte*)buffer)+bytes_written);
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_OutputStream::WriteString
+---------------------------------------------------------------------*/
NPT_Result
NPT_OutputStream::WriteString(const char* buffer)
{
    // shortcut
    NPT_Size string_length;
    if (buffer == NULL || (string_length = NPT_StringLength(buffer)) == 0) {
        return NPT_SUCCESS;
    }

    // write the string
    return WriteFully((const void*)buffer, string_length);
}

/*----------------------------------------------------------------------
|   NPT_OutputStream::WriteLine
+---------------------------------------------------------------------*/
NPT_Result
NPT_OutputStream::WriteLine(const char* buffer)
{
    NPT_CHECK(WriteString(buffer));
    NPT_CHECK(WriteFully((const void*)"\r\n", 2));

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_OutputStream::WriteUI64
+---------------------------------------------------------------------*/
NPT_Result
NPT_OutputStream::WriteUI64(NPT_UInt64 value)
{
    unsigned char buffer[8];

    // convert value to bytes
    NPT_BytesFromInt64Be(buffer, value);

    // write bytes to the stream
    return WriteFully((void*)buffer, 8);
}

/*----------------------------------------------------------------------
|   NPT_OutputStream::WriteUI32
+---------------------------------------------------------------------*/
NPT_Result
NPT_OutputStream::WriteUI32(NPT_UInt32 value)
{
    unsigned char buffer[4];

    // convert value to bytes
    NPT_BytesFromInt32Be(buffer, value);

    // write bytes to the stream
    return WriteFully((void*)buffer, 4);
}

/*----------------------------------------------------------------------
|   NPT_OutputStream::WriteUI24
+---------------------------------------------------------------------*/
NPT_Result
NPT_OutputStream::WriteUI24(NPT_UInt32 value)
{
    unsigned char buffer[3];

    // convert value to bytes
    NPT_BytesFromInt24Be(buffer, value);

    // write bytes to the stream
    return WriteFully((void*)buffer, 3);
}

/*----------------------------------------------------------------------
|   NPT_OutputStream::WriteUI16
+---------------------------------------------------------------------*/
NPT_Result
NPT_OutputStream::WriteUI16(NPT_UInt16 value)
{
    unsigned char buffer[2];

    // convert value to bytes
    NPT_BytesFromInt16Be(buffer, value);

    // write bytes to the stream
    return WriteFully((void*)buffer, 2);
}

/*----------------------------------------------------------------------
|   NPT_OutputStream::WriteUI08
+---------------------------------------------------------------------*/
NPT_Result
NPT_OutputStream::WriteUI08(NPT_UInt8 value)
{
    return WriteFully((void*)&value, 1);
}

/*----------------------------------------------------------------------
|   NPT_MemoryStream::NPT_MemoryStream
+---------------------------------------------------------------------*/
NPT_MemoryStream::NPT_MemoryStream(NPT_Size initial_capacity) : 
    m_Buffer(initial_capacity),
    m_ReadOffset(0),
    m_WriteOffset(0)
{
}

/*----------------------------------------------------------------------
|   NPT_MemoryStream::NPT_MemoryStream
+---------------------------------------------------------------------*/
NPT_MemoryStream::NPT_MemoryStream(const void* data, NPT_Size size) : 
    m_Buffer(data, size),
    m_ReadOffset(0),
    m_WriteOffset(0)
{
}

/*----------------------------------------------------------------------
|   NPT_MemoryStream::Read
+---------------------------------------------------------------------*/
NPT_Result 
NPT_MemoryStream::Read(void*     buffer, 
                       NPT_Size  bytes_to_read, 
                       NPT_Size* bytes_read)
{
    // check for shortcut
    if (bytes_to_read == 0) {
        if (bytes_read) *bytes_read = 0;
        return NPT_SUCCESS;
    }

    // clip to what's available
    NPT_Size available = m_Buffer.GetDataSize();
    if (m_ReadOffset+bytes_to_read > available) {
        bytes_to_read = available-m_ReadOffset;
    }

    // copy the data
    if (bytes_to_read) {
        NPT_CopyMemory(buffer, (void*)(((char*)m_Buffer.UseData())+m_ReadOffset), bytes_to_read);
        m_ReadOffset += bytes_to_read;
    } 
    if (bytes_read) *bytes_read = bytes_to_read;

    return bytes_to_read?NPT_SUCCESS:NPT_ERROR_EOS; 
}

/*----------------------------------------------------------------------
|   NPT_MemoryStream::InputSeek
+---------------------------------------------------------------------*/
NPT_Result 
NPT_MemoryStream::InputSeek(NPT_Position offset)
{
    if (offset > m_Buffer.GetDataSize()) {
        return NPT_ERROR_OUT_OF_RANGE;
    } else {
        m_ReadOffset = (NPT_Size)offset;
        return NPT_SUCCESS;
    }
}

/*----------------------------------------------------------------------
|   NPT_MemoryStream::Write
+---------------------------------------------------------------------*/
NPT_Result 
NPT_MemoryStream::Write(const void* data, 
                        NPT_Size    bytes_to_write, 
                        NPT_Size*   bytes_written)
{
    NPT_CHECK(m_Buffer.Reserve(m_WriteOffset+bytes_to_write));

    NPT_CopyMemory(m_Buffer.UseData()+m_WriteOffset, data, bytes_to_write);
    m_WriteOffset += bytes_to_write;
    if (m_WriteOffset > m_Buffer.GetDataSize()) {
        m_Buffer.SetDataSize(m_WriteOffset);
    }
    if (bytes_written) *bytes_written = bytes_to_write;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_MemoryStream::OutputSeek
+---------------------------------------------------------------------*/
NPT_Result 
NPT_MemoryStream::OutputSeek(NPT_Position offset)
{
    if (offset <= m_Buffer.GetDataSize()) {
        m_WriteOffset = (NPT_Size)offset;
        return NPT_SUCCESS;
    } else {
        return NPT_ERROR_OUT_OF_RANGE;
    }
}

/*----------------------------------------------------------------------
|   NPT_MemoryStream::SetDataSize
+---------------------------------------------------------------------*/
NPT_Result 
NPT_MemoryStream::SetDataSize(NPT_Size size)
{
    // update data amount in buffer
    NPT_CHECK(m_Buffer.SetDataSize(size));

    // adjust the read and write offsets
    if (m_ReadOffset > size) m_ReadOffset = size;
    if (m_WriteOffset > size) m_WriteOffset = size;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_StreamToStreamCopy
+---------------------------------------------------------------------*/
const unsigned int NPT_STREAM_COPY_BUFFER_SIZE = 65536; // copy 64k at a time
NPT_Result 
NPT_StreamToStreamCopy(NPT_InputStream&  from, 
                       NPT_OutputStream& to,
                       NPT_Position      offset /* = 0 */,
                       NPT_LargeSize     size   /* = 0, 0 means the entire stream */,
                       NPT_LargeSize*    bytes_written /* = NULL */)
{
    // default values
    if (bytes_written) *bytes_written = 0;
    
    // seek into the input if required
    if (offset) {
        NPT_CHECK(from.Seek(offset));
    }

    // allocate a buffer for the transfer
    NPT_LargeSize bytes_transfered = 0;
    NPT_Byte*     buffer = new NPT_Byte[NPT_STREAM_COPY_BUFFER_SIZE];
    NPT_Result result = NPT_SUCCESS;
    if (buffer == NULL) return NPT_ERROR_OUT_OF_MEMORY;

    // copy until an error occurs or the end of stream is reached
    for (;;) {
        // read some data
        NPT_Size   bytes_to_read = NPT_STREAM_COPY_BUFFER_SIZE;
        NPT_Size   bytes_read = 0;
        if (size) {
            // a max size was specified
            if (size-bytes_transfered < NPT_STREAM_COPY_BUFFER_SIZE) {
                bytes_to_read = (NPT_Size)(size-bytes_transfered);
            }
        }
        result = from.Read(buffer, bytes_to_read, &bytes_read);
        if (NPT_FAILED(result)) {
            if (result == NPT_ERROR_EOS) result = NPT_SUCCESS;
            break;
        }
        if (bytes_read == 0) continue;
        
        NPT_Size  buffer_bytes_to_write = bytes_read;
        NPT_Byte* buffer_bytes = (NPT_Byte*)buffer;
        while (buffer_bytes_to_write) {
            NPT_Size buffer_bytes_written = 0;
            result = to.Write(buffer_bytes, buffer_bytes_to_write, &buffer_bytes_written);
            if (NPT_FAILED(result)) goto end;
            NPT_ASSERT(buffer_bytes_written <= buffer_bytes_to_write);
            buffer_bytes_to_write -= buffer_bytes_written;
            if (bytes_written) *bytes_written += buffer_bytes_written;
            buffer_bytes += buffer_bytes_written;
        }

        // update the counts
        if (size) {
            bytes_transfered += bytes_read;
            if (bytes_transfered >= size) break;
        }
    }

end:
    // free the buffer and return
    delete[] buffer;
    return result;
}

/*----------------------------------------------------------------------
|   NPT_StringOutputStream::NPT_StringOutputStream
+---------------------------------------------------------------------*/
NPT_StringOutputStream::NPT_StringOutputStream(NPT_Size size) :
    m_String(new NPT_String),
    m_StringIsOwned(true)
{
    m_String->Reserve(size);
}


/*----------------------------------------------------------------------
|   NPT_StringOutputStream::NPT_StringOutputStream
+---------------------------------------------------------------------*/
NPT_StringOutputStream::NPT_StringOutputStream(NPT_String* storage) :
    m_String(storage),
    m_StringIsOwned(false)
{
}

/*----------------------------------------------------------------------
|   NPT_StringOutputStream::~NPT_StringOutputStream
+---------------------------------------------------------------------*/
NPT_StringOutputStream::~NPT_StringOutputStream()
{
    if (m_StringIsOwned) delete m_String;
}

/*----------------------------------------------------------------------
|   NPT_StringOutputStream::Write
+---------------------------------------------------------------------*/
NPT_Result 
NPT_StringOutputStream::Write(const void* buffer, NPT_Size bytes_to_write, NPT_Size* bytes_written /* = NULL */)
{
     m_String->Append((const char*)buffer, bytes_to_write);
    if (bytes_written) *bytes_written = bytes_to_write;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_SubInputStream::NPT_SubInputStream
+---------------------------------------------------------------------*/
NPT_SubInputStream::NPT_SubInputStream(NPT_InputStreamReference& source, 
                                       NPT_Position              start,
                                       NPT_LargeSize             size) :
    m_Source(source),
    m_Position(0),
    m_Start(start),
    m_Size(size)
{
}

/*----------------------------------------------------------------------
|   NPT_SubInputStream::Read
+---------------------------------------------------------------------*/
NPT_Result 
NPT_SubInputStream::Read(void*     buffer, 
                         NPT_Size  bytes_to_read, 
                         NPT_Size* bytes_read)
{
    // default values
    if (bytes_read) *bytes_read = 0;

    // shortcut
    if (bytes_to_read == 0) {
        return NPT_SUCCESS;
    }

    // clamp to range
    if (m_Position+bytes_to_read > m_Size) {
        bytes_to_read = (NPT_Size)(m_Size - m_Position);
    }

    // check for end of substream
    if (bytes_to_read == 0) {
        return NPT_ERROR_EOS;
    }

    // seek inside the source
    NPT_Result result;
    result = m_Source->Seek(m_Start+m_Position);
    if (NPT_FAILED(result)) {
        return result;
    }

    // read from the source
    NPT_Size source_bytes_read = 0;
    result = m_Source->Read(buffer, bytes_to_read, &source_bytes_read);
    if (NPT_SUCCEEDED(result)) {
        m_Position += source_bytes_read;
        if (bytes_read) *bytes_read = source_bytes_read;
    }
    return result;
}

/*----------------------------------------------------------------------
|   NPT_SubInputStream::Seek
+---------------------------------------------------------------------*/
NPT_Result 
NPT_SubInputStream::Seek(NPT_Position position)
{
    if (position == m_Position) return NPT_SUCCESS;
    if (position > m_Size) return NPT_ERROR_OUT_OF_RANGE;
    m_Position = position;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_SubInputStream::Tell
+---------------------------------------------------------------------*/
NPT_Result 
NPT_SubInputStream::Tell(NPT_Position& position)
{
    position = m_Position;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_SubInputStream::GetSize
+---------------------------------------------------------------------*/
NPT_Result 
NPT_SubInputStream::GetSize(NPT_LargeSize& size)
{
    size = m_Size;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_SubInputStream::GetAvailable
+---------------------------------------------------------------------*/
NPT_Result 
NPT_SubInputStream::GetAvailable(NPT_LargeSize& available)
{
    available = m_Size-m_Position;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_NullOutputStream::Write
+---------------------------------------------------------------------*/
NPT_Result 
NPT_NullOutputStream::Write(const void* /*buffer*/, 
                            NPT_Size  bytes_to_write, 
                            NPT_Size* bytes_written /* = NULL */)
{
    if (bytes_written) *bytes_written = bytes_to_write;
    return NPT_SUCCESS;
}

