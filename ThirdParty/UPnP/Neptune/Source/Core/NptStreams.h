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

#ifndef _NPT_STREAMS_H_
#define _NPT_STREAMS_H_

/*----------------------------------------------------------------------
|    includes
+---------------------------------------------------------------------*/
#include "NptTypes.h"
#include "NptReferences.h"
#include "NptConstants.h"
#include "NptResults.h"
#include "NptDataBuffer.h"
#include "NptStrings.h"

/*----------------------------------------------------------------------
|    class references
+---------------------------------------------------------------------*/
class NPT_String;

/*----------------------------------------------------------------------
|    constants
+---------------------------------------------------------------------*/
const int NPT_ERROR_READ_FAILED  = NPT_ERROR_BASE_IO - 0;
const int NPT_ERROR_WRITE_FAILED = NPT_ERROR_BASE_IO - 1;
const int NPT_ERROR_EOS          = NPT_ERROR_BASE_IO - 2;

/*----------------------------------------------------------------------
|    NPT_InputStream
+---------------------------------------------------------------------*/
class NPT_InputStream
{
 public:
    // constructor and destructor
    virtual ~NPT_InputStream() {};

    // methods
    virtual NPT_Result Load(NPT_DataBuffer& buffer, NPT_Size max_read = 0);
    virtual NPT_Result Read(void*     buffer, 
                            NPT_Size  bytes_to_read, 
                            NPT_Size* bytes_read = NULL) = 0;
    virtual NPT_Result ReadFully(void*     buffer, 
                                 NPT_Size  bytes_to_read);
    virtual NPT_Result Seek(NPT_Position offset) = 0;
    virtual NPT_Result Skip(NPT_Size offset);
    virtual NPT_Result Tell(NPT_Position& offset) = 0;
    virtual NPT_Result GetSize(NPT_LargeSize& size) = 0;
    virtual NPT_Result GetAvailable(NPT_LargeSize& available) = 0;
    
    // data access methods
    NPT_Result ReadUI64(NPT_UInt64& value);
    NPT_Result ReadUI32(NPT_UInt32& value);
    NPT_Result ReadUI24(NPT_UInt32& value);
    NPT_Result ReadUI16(NPT_UInt16& value);
    NPT_Result ReadUI08(NPT_UInt8&  value);    
};

typedef NPT_Reference<NPT_InputStream> NPT_InputStreamReference;

/*----------------------------------------------------------------------
|    NPT_OutputStream
+---------------------------------------------------------------------*/
class NPT_OutputStream
{
public:
    // constructor and destructor
    virtual ~NPT_OutputStream() {};

    // methods
    virtual NPT_Result Write(const void* buffer, 
                             NPT_Size    bytes_to_write, 
                             NPT_Size*   bytes_written = NULL) = 0;
    virtual NPT_Result WriteFully(const void* buffer, 
                                  NPT_Size    bytes_to_write);
    virtual NPT_Result WriteString(const char* string_buffer);
    virtual NPT_Result WriteLine(const char* line_buffer);
    virtual NPT_Result Seek(NPT_Position offset) = 0;
    virtual NPT_Result Tell(NPT_Position& offset) = 0;
    virtual NPT_Result Flush() { return NPT_SUCCESS; }
    
    // data access methods
    NPT_Result WriteUI64(NPT_UInt64 value);
    NPT_Result WriteUI32(NPT_UInt32 value);
    NPT_Result WriteUI24(NPT_UInt32 value);
    NPT_Result WriteUI16(NPT_UInt16 value);
    NPT_Result WriteUI08(NPT_UInt8  value);    
};

typedef NPT_Reference<NPT_OutputStream> NPT_OutputStreamReference;

/*----------------------------------------------------------------------
|    NPT_StreamToStreamCopy
+---------------------------------------------------------------------*/
NPT_Result NPT_StreamToStreamCopy(NPT_InputStream&  from, 
                                  NPT_OutputStream& to,
                                  NPT_Position      offset = 0,
                                  NPT_LargeSize     size   = 0, /* 0 means the entire stream */
                                  NPT_LargeSize*    bytes_written = NULL);

/*----------------------------------------------------------------------
|    NPT_DelegatingInputStream
|
|    Use this class as a base class if you need to inherit both from
|    NPT_InputStream and NPT_OutputStream which share the Seek and Tell
|    method. In this case, you override the  base-specific version of 
|    those methods, InputSeek, InputTell, instead of the Seek and Tell 
|    methods.
+---------------------------------------------------------------------*/
class NPT_DelegatingInputStream : public NPT_InputStream
{
public:
    // NPT_InputStream methods
    NPT_Result Seek(NPT_Position offset) {
        return InputSeek(offset);
    }
    NPT_Result Tell(NPT_Position& offset) {
        return InputTell(offset);
    }

private:
    // methods
    virtual NPT_Result InputSeek(NPT_Position  offset) = 0;
    virtual NPT_Result InputTell(NPT_Position& offset) = 0;
};

/*----------------------------------------------------------------------
|    NPT_DelegatingOutputStream
|
|    Use this class as a base class if you need to inherit both from
|    NPT_InputStream and NPT_OutputStream which share the Seek and Tell
|    method. In this case, you override the  base-specific version of 
|    those methods, OutputSeek and OutputTell, instead of the Seek and 
|    Tell methods.
+---------------------------------------------------------------------*/
class NPT_DelegatingOutputStream : public NPT_OutputStream
{
public:
    // NPT_OutputStream methods
    NPT_Result Seek(NPT_Position offset) {
        return OutputSeek(offset);
    }
    NPT_Result Tell(NPT_Position& offset) {
        return OutputTell(offset);
    }

private:
    // methods
    virtual NPT_Result OutputSeek(NPT_Position  offset) = 0;
    virtual NPT_Result OutputTell(NPT_Position& offset) = 0;
};

/*----------------------------------------------------------------------
|    NPT_MemoryStream
+---------------------------------------------------------------------*/
class NPT_MemoryStream : 
    public NPT_DelegatingInputStream,
    public NPT_DelegatingOutputStream
{
public:
    // constructor and destructor
    NPT_MemoryStream(NPT_Size initial_capacity = 0);
    NPT_MemoryStream(const void* data, NPT_Size size);
    virtual ~NPT_MemoryStream() {}

    // accessors
    const NPT_DataBuffer& GetBuffer() const { return m_Buffer; }

    // NPT_InputStream methods
    NPT_Result Read(void*     buffer, 
                    NPT_Size  bytes_to_read, 
                    NPT_Size* bytes_read = NULL);
    NPT_Result GetSize(NPT_LargeSize& size)  { 
        size = m_Buffer.GetDataSize();    
        return NPT_SUCCESS;
    }
    NPT_Result GetAvailable(NPT_LargeSize& available) { 
        available = (NPT_LargeSize)m_Buffer.GetDataSize()-m_ReadOffset; 
        return NPT_SUCCESS;
    }

    // NPT_OutputStream methods
    NPT_Result Write(const void* buffer, 
                     NPT_Size    bytes_to_write, 
                     NPT_Size*   bytes_written = NULL);

    // methods delegated to m_Buffer
    const NPT_Byte* GetData() const { return m_Buffer.GetData(); }
    NPT_Byte*       UseData()       { return m_Buffer.UseData(); }
    NPT_Size        GetDataSize() const   { return m_Buffer.GetDataSize(); }
    NPT_Size        GetBufferSize() const { return m_Buffer.GetBufferSize();}

    // methods
    NPT_Result SetDataSize(NPT_Size size);

private:
    // NPT_DelegatingInputStream methods
    NPT_Result InputSeek(NPT_Position offset);
    NPT_Result InputTell(NPT_Position& offset) { 
        offset = m_ReadOffset; 
        return NPT_SUCCESS;
    }

    // NPT_DelegatingOutputStream methods
    NPT_Result OutputSeek(NPT_Position offset);
    NPT_Result OutputTell(NPT_Position& offset) {
        offset = m_WriteOffset; 
        return NPT_SUCCESS;
    }

protected:
    // members
    NPT_DataBuffer m_Buffer;
    NPT_Size       m_ReadOffset;
    NPT_Size       m_WriteOffset;
};

typedef NPT_Reference<NPT_MemoryStream> NPT_MemoryStreamReference;

/*----------------------------------------------------------------------
|   NPT_StringOutputStream
+---------------------------------------------------------------------*/
class NPT_StringOutputStream : public NPT_OutputStream
{
public:
    // methods
    NPT_StringOutputStream(NPT_Size size = 4096);
    NPT_StringOutputStream(NPT_String* storage);
    virtual ~NPT_StringOutputStream() ;

    const NPT_String& GetString() const { return *m_String; }
    NPT_Result Reset() { if (m_String) m_String->SetLength(0); return NPT_SUCCESS; }

    // NPT_OutputStream methods
    NPT_Result Write(const void* buffer, NPT_Size bytes_to_write, NPT_Size* bytes_written = NULL);

    NPT_Result Seek(NPT_Position /*offset*/)  { return NPT_ERROR_NOT_SUPPORTED;   }
    NPT_Result Tell(NPT_Position& offset) { offset = m_String->GetLength(); return NPT_SUCCESS; }

protected:
    NPT_String* m_String;
    bool        m_StringIsOwned;
};

typedef NPT_Reference<NPT_StringOutputStream> NPT_StringOutputStreamReference;

/*----------------------------------------------------------------------
|   NPT_SubInputStream
+---------------------------------------------------------------------*/
class NPT_SubInputStream : public NPT_InputStream
{
public:
    // constructor and destructor
    NPT_SubInputStream(NPT_InputStreamReference& source, 
                       NPT_Position              start,
                       NPT_LargeSize             size); 

    // methods
    virtual NPT_Result Read(void*     buffer, 
                            NPT_Size  bytes_to_read, 
                            NPT_Size* bytes_read = NULL) = 0;
    virtual NPT_Result Seek(NPT_Position offset) = 0;
    virtual NPT_Result Tell(NPT_Position& offset) = 0;
    virtual NPT_Result GetSize(NPT_LargeSize& size) = 0;
    virtual NPT_Result GetAvailable(NPT_LargeSize& available) = 0;

private:
    NPT_InputStreamReference m_Source;
    NPT_Position             m_Position;
    NPT_Position             m_Start;
    NPT_LargeSize            m_Size;
};

/*----------------------------------------------------------------------
|   NPT_NullOutputStream
+---------------------------------------------------------------------*/
class NPT_NullOutputStream : public NPT_OutputStream
{
public:
    // methods
    NPT_NullOutputStream() {}
    virtual ~NPT_NullOutputStream() {}

    // NPT_OutputStream methods
    NPT_Result Write(const void* buffer, NPT_Size bytes_to_write, NPT_Size* bytes_written = NULL);

    NPT_Result Seek(NPT_Position /*offset*/)  { return NPT_ERROR_NOT_SUPPORTED;   }
    NPT_Result Tell(NPT_Position& /*offset*/)  { return NPT_ERROR_NOT_SUPPORTED;   }
};

typedef NPT_Reference<NPT_NullOutputStream> NPT_NullOutputStreamReference;

#endif // _NPT_STREAMS_H_
