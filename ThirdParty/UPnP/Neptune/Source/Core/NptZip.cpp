/*****************************************************************
|
|   Neptune - Zip Support
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

#if defined(NPT_CONFIG_ENABLE_ZIP)

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptConfig.h"
#include "NptZip.h"
#include "NptLogging.h"
#include "NptUtils.h"

#include "zlib.h"

/*----------------------------------------------------------------------
|   logging
+---------------------------------------------------------------------*/
NPT_SET_LOCAL_LOGGER("neptune.zip")

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
const unsigned int NPT_ZIP_DEFAULT_BUFFER_SIZE = 4096;

/*----------------------------------------------------------------------
|   NPT_Zip::MapError
+---------------------------------------------------------------------*/
NPT_Result
NPT_Zip::MapError(int err)
{
    switch (err) {
        case Z_OK:            return NPT_SUCCESS;           
        case Z_STREAM_END:    return NPT_ERROR_EOS;
        case Z_DATA_ERROR:
        case Z_STREAM_ERROR:  return NPT_ERROR_INVALID_FORMAT;
        case Z_MEM_ERROR:     return NPT_ERROR_OUT_OF_MEMORY;
        case Z_VERSION_ERROR: return NPT_ERROR_INTERNAL;
        case Z_NEED_DICT:     return NPT_ERROR_NOT_SUPPORTED;
        default:              return NPT_FAILURE;
    }
}

/*----------------------------------------------------------------------
|   NPT_ZipInflateState
+---------------------------------------------------------------------*/
class NPT_ZipInflateState {
public:
    NPT_ZipInflateState();
   ~NPT_ZipInflateState();
    z_stream m_Stream;
};

/*----------------------------------------------------------------------
|   NPT_ZipInflateState::NPT_ZipInflateState
+---------------------------------------------------------------------*/
NPT_ZipInflateState::NPT_ZipInflateState()
{
    // initialize the state
    NPT_SetMemory(&m_Stream, 0, sizeof(m_Stream));

    // initialize the decompressor
    inflateInit2(&m_Stream, 15+32); // 15 = default window bits, +32 = automatic header
}

/*----------------------------------------------------------------------
|   NPT_ZipInflateState::~NPT_ZipInflateState
+---------------------------------------------------------------------*/
NPT_ZipInflateState::~NPT_ZipInflateState()
{
    inflateEnd(&m_Stream);
}

/*----------------------------------------------------------------------
|   NPT_ZipDeflateState
+---------------------------------------------------------------------*/
class NPT_ZipDeflateState {
public:
    NPT_ZipDeflateState(int             compression_level,
                        NPT_Zip::Format format);
   ~NPT_ZipDeflateState();
    z_stream m_Stream;
};

/*----------------------------------------------------------------------
|   NPT_ZipDeflateState::NPT_ZipDeflateState
+---------------------------------------------------------------------*/
NPT_ZipDeflateState::NPT_ZipDeflateState(int             compression_level,
                                         NPT_Zip::Format format)
{
    // check parameters
    if (compression_level < NPT_ZIP_COMPRESSION_LEVEL_DEFAULT ||
        compression_level > NPT_ZIP_COMPRESSION_LEVEL_MAX) {
        compression_level = NPT_ZIP_COMPRESSION_LEVEL_DEFAULT;
    }

    // initialize the state
    NPT_SetMemory(&m_Stream, 0, sizeof(m_Stream));

    // initialize the compressor
    deflateInit2(&m_Stream, 
                compression_level,
                Z_DEFLATED,
                15 + (format == NPT_Zip::GZIP ? 16 : 0),
                8,
                Z_DEFAULT_STRATEGY);
}

/*----------------------------------------------------------------------
|   NPT_ZipDeflateState::~NPT_ZipDeflateState
+---------------------------------------------------------------------*/
NPT_ZipDeflateState::~NPT_ZipDeflateState()
{
    deflateEnd(&m_Stream);
}

/*----------------------------------------------------------------------
|   NPT_ZipInflatingInputStream::NPT_ZipInflatingInputStream
+---------------------------------------------------------------------*/
NPT_ZipInflatingInputStream::NPT_ZipInflatingInputStream(NPT_InputStreamReference& source) :
    m_Source(source),
    m_Position(0),
    m_State(new NPT_ZipInflateState()),
    m_Buffer(NPT_ZIP_DEFAULT_BUFFER_SIZE)
{
}

/*----------------------------------------------------------------------
|   NPT_ZipInflatingInputStream::~NPT_ZipInflatingInputStream
+---------------------------------------------------------------------*/
NPT_ZipInflatingInputStream::~NPT_ZipInflatingInputStream()
{
    delete m_State;
}

/*----------------------------------------------------------------------
|   NPT_ZipInflatingInputStream::Read
+---------------------------------------------------------------------*/
NPT_Result 
NPT_ZipInflatingInputStream::Read(void*     buffer, 
                                  NPT_Size  bytes_to_read, 
                                  NPT_Size* bytes_read)
{
    // check state and parameters
    if (m_State == NULL) return NPT_ERROR_INVALID_STATE;
    if (buffer == NULL) return NPT_ERROR_INVALID_PARAMETERS;
    if (bytes_to_read == 0) return NPT_SUCCESS;
    
    // default values
    if (bytes_read) *bytes_read = 0;
    
    // setup the output buffer
    m_State->m_Stream.next_out  = (Bytef*)buffer;
    m_State->m_Stream.avail_out = (uInt)bytes_to_read;
    
    while (m_State->m_Stream.avail_out) {
        // decompress what we can
        int err = inflate(&m_State->m_Stream, Z_NO_FLUSH);
        
        if (err == Z_STREAM_END) {
            // we decompressed everything
            break;
        } else if (err == Z_OK) {
            // we got something
            continue;
        } else if (err == Z_BUF_ERROR) {
            // we need more input data
            NPT_Size   input_bytes_read = 0;
            NPT_Result result = m_Source->Read(m_Buffer.UseData(), m_Buffer.GetBufferSize(), &input_bytes_read);
            if (NPT_FAILED(result)) return result;

            // setup the input buffer
            m_Buffer.SetDataSize(input_bytes_read);
            m_State->m_Stream.next_in = m_Buffer.UseData();
            m_State->m_Stream.avail_in = m_Buffer.GetDataSize();
        
        } else {
            return NPT_Zip::MapError(err);
        }
    }
    
    // report how much we could decompress
    NPT_Size progress = bytes_to_read - m_State->m_Stream.avail_out;
    if (bytes_read) {
        *bytes_read = progress;
    }
    m_Position += progress;
    
    return progress == 0 ? NPT_ERROR_EOS:NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_ZipInflatingInputStream::Seek
+---------------------------------------------------------------------*/
NPT_Result 
NPT_ZipInflatingInputStream::Seek(NPT_Position /* offset */)
{
    // we can't seek
    return NPT_ERROR_NOT_SUPPORTED;
}

/*----------------------------------------------------------------------
|   NPT_ZipInflatingInputStream::Tell
+---------------------------------------------------------------------*/
NPT_Result 
NPT_ZipInflatingInputStream::Tell(NPT_Position& offset)
{
    offset = m_Position;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_ZipInflatingInputStream::GetSize
+---------------------------------------------------------------------*/
NPT_Result 
NPT_ZipInflatingInputStream::GetSize(NPT_LargeSize& size)
{
    // the size is not predictable
    size = 0;
    return NPT_ERROR_NOT_SUPPORTED;
}

/*----------------------------------------------------------------------
|   NPT_ZipInflatingInputStream::GetAvailable
+---------------------------------------------------------------------*/
NPT_Result 
NPT_ZipInflatingInputStream::GetAvailable(NPT_LargeSize& available)
{
    // we don't know
    available = 0;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_ZipDeflatingInputStream::NPT_ZipDeflatingInputStream
+---------------------------------------------------------------------*/
NPT_ZipDeflatingInputStream::NPT_ZipDeflatingInputStream(
    NPT_InputStreamReference& source,
    int                       compression_level,
    NPT_Zip::Format           format) :
    m_Source(source),
    m_Position(0),
    m_Eos(false),
    m_State(new NPT_ZipDeflateState(compression_level, format)),
    m_Buffer(NPT_ZIP_DEFAULT_BUFFER_SIZE)
{
}

/*----------------------------------------------------------------------
|   NPT_ZipDeflatingInputStream::~NPT_ZipDeflatingInputStream
+---------------------------------------------------------------------*/
NPT_ZipDeflatingInputStream::~NPT_ZipDeflatingInputStream()
{
    delete m_State;
}

/*----------------------------------------------------------------------
|   NPT_ZipDeflatingInputStream::Read
+---------------------------------------------------------------------*/
NPT_Result 
NPT_ZipDeflatingInputStream::Read(void*     buffer, 
                                  NPT_Size  bytes_to_read, 
                                  NPT_Size* bytes_read)
{
    // check state and parameters
    if (m_State == NULL) return NPT_ERROR_INVALID_STATE;
    if (buffer == NULL) return NPT_ERROR_INVALID_PARAMETERS;
    if (bytes_to_read == 0) return NPT_SUCCESS;
    
    // default values
    if (bytes_read) *bytes_read = 0;
    
    // setup the output buffer
    m_State->m_Stream.next_out  = (Bytef*)buffer;
    m_State->m_Stream.avail_out = (uInt)bytes_to_read;
    
    while (m_State->m_Stream.avail_out) {
        // compress what we can
        int err = deflate(&m_State->m_Stream, m_Eos?Z_FINISH:Z_NO_FLUSH);
        
        if (err == Z_STREAM_END) {
            // we compressed everything
            break;
        } else if (err == Z_OK) {
            // we got something
            continue;
        } else if (err == Z_BUF_ERROR) {
            // we need more input data
            NPT_Size   input_bytes_read = 0;
            NPT_Result result = m_Source->Read(m_Buffer.UseData(), m_Buffer.GetBufferSize(), &input_bytes_read);
            if (result == NPT_ERROR_EOS) {
                m_Eos = true;
            } else {
                if (NPT_FAILED(result)) return result;
            }
            
            // setup the input buffer
            m_Buffer.SetDataSize(input_bytes_read);
            m_State->m_Stream.next_in = m_Buffer.UseData();
            m_State->m_Stream.avail_in = m_Buffer.GetDataSize();
        
        } else {
            return NPT_Zip::MapError(err);
        }
    }
    
    // report how much we could compress
    NPT_Size progress = bytes_to_read - m_State->m_Stream.avail_out;
    if (bytes_read) {
        *bytes_read = progress;
    }
    m_Position += progress;
    
    return progress == 0 ? NPT_ERROR_EOS:NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_ZipDeflatingInputStream::Seek
+---------------------------------------------------------------------*/
NPT_Result 
NPT_ZipDeflatingInputStream::Seek(NPT_Position /* offset */)
{
    // we can't seek
    return NPT_ERROR_NOT_SUPPORTED;
}

/*----------------------------------------------------------------------
|   NPT_ZipDeflatingInputStream::Tell
+---------------------------------------------------------------------*/
NPT_Result 
NPT_ZipDeflatingInputStream::Tell(NPT_Position& offset)
{
    offset = m_Position;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_ZipDeflatingInputStream::GetSize
+---------------------------------------------------------------------*/
NPT_Result 
NPT_ZipDeflatingInputStream::GetSize(NPT_LargeSize& size)
{
    // the size is not predictable
    size = 0;
    return NPT_ERROR_NOT_SUPPORTED;
}

/*----------------------------------------------------------------------
|   NPT_ZipDeflatingInputStream::GetAvailable
+---------------------------------------------------------------------*/
NPT_Result 
NPT_ZipDeflatingInputStream::GetAvailable(NPT_LargeSize& available)
{
    // we don't know
    available = 0;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Zip::Deflate
+---------------------------------------------------------------------*/
NPT_Result 
NPT_Zip::Deflate(const NPT_DataBuffer& in,
                 NPT_DataBuffer&       out,
                 int                   compression_level,
                 Format                format /* = ZLIB */)
{
    // default return state
    out.SetDataSize(0);
    
    // check parameters
    if (compression_level < NPT_ZIP_COMPRESSION_LEVEL_DEFAULT ||
        compression_level > NPT_ZIP_COMPRESSION_LEVEL_MAX) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }
                
    // setup the stream
    z_stream stream;
    NPT_SetMemory(&stream, 0, sizeof(stream));
    stream.next_in   = (Bytef*)in.GetData();
    stream.avail_in  = (uInt)in.GetDataSize();
    
    // setup the memory functions
    stream.zalloc = (alloc_func)0;
    stream.zfree = (free_func)0;
    stream.opaque = (voidpf)0;

    // initialize the compressor
    int err = deflateInit2(&stream, 
                           compression_level,
                           Z_DEFLATED,
                           15 + (format == GZIP ? 16 : 0),
                           8,
                           Z_DEFAULT_STRATEGY);
    if (err != Z_OK) return MapError(err);

    // reserve an output buffer known to be large enough
    out.Reserve(deflateBound(&stream, stream.avail_in) + (format==GZIP?10:0));
    stream.next_out  = out.UseData();
    stream.avail_out = out.GetBufferSize();

    // decompress
    err = deflate(&stream, Z_FINISH);
    if (err != Z_STREAM_END) {
        deflateEnd(&stream);
        return MapError(err);
    }
    
    // update the output size
    out.SetDataSize(stream.total_out);

    // cleanup
    err = deflateEnd(&stream);
    return MapError(err);
}
                              
/*----------------------------------------------------------------------
|   NPT_Zip::Inflate
+---------------------------------------------------------------------*/                              
NPT_Result 
NPT_Zip::Inflate(const NPT_DataBuffer& in,
                 NPT_DataBuffer&       out)
{
    // assume an output buffer twice the size of the input plus a bit
    NPT_CHECK_WARNING(out.Reserve(32+2*in.GetDataSize()));
    
    // setup the stream
    z_stream stream;
    stream.next_in   = (Bytef*)in.GetData();
    stream.avail_in  = (uInt)in.GetDataSize();
    stream.next_out  = out.UseData();
    stream.avail_out = (uInt)out.GetBufferSize();

    // setup the memory functions
    stream.zalloc = (alloc_func)0;
    stream.zfree = (free_func)0;
    stream.opaque = (voidpf)0;

    // initialize the decompressor
    int err = inflateInit2(&stream, 15+32); // 15 = default window bits, +32 = automatic header
    if (err != Z_OK) return MapError(err);
    
    // decompress until the end
    do {
        err = inflate(&stream, Z_SYNC_FLUSH);
        if (err == Z_STREAM_END || err == Z_OK || err == Z_BUF_ERROR) {
            out.SetDataSize(stream.total_out);
            if ((err == Z_OK && stream.avail_out == 0) || err == Z_BUF_ERROR) {
                // grow the output buffer
                out.Reserve(out.GetBufferSize()*2);
                stream.next_out = out.UseData()+stream.total_out;
                stream.avail_out = out.GetBufferSize()-stream.total_out;
            }
        }
    } while (err == Z_OK);
    
    // check for errors
    if (err != Z_STREAM_END) {
        inflateEnd(&stream);
        return MapError(err);
    }
    
    // cleanup
    err = inflateEnd(&stream);
    return MapError(err);
}


/*----------------------------------------------------------------------
|   NPT_Zip::Deflate
+---------------------------------------------------------------------*/
NPT_Result 
NPT_Zip::Deflate(NPT_File& in,
                 NPT_File& out,
                 int       compression_level,
                 Format    format /* = ZLIB */)
{
    // check parameters
    if (compression_level < NPT_ZIP_COMPRESSION_LEVEL_DEFAULT ||
        compression_level > NPT_ZIP_COMPRESSION_LEVEL_MAX) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }
    
    NPT_InputStreamReference input;
    NPT_CHECK(in.GetInputStream(input));
    NPT_OutputStreamReference output;
    NPT_CHECK(out.GetOutputStream(output));
    
    NPT_ZipDeflatingInputStream deflating_stream(input, compression_level, format);
    return NPT_StreamToStreamCopy(deflating_stream, *output.AsPointer());
}

#endif // NPT_CONFIG_ENABLE_ZIP
