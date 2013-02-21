/*****************************************************************
|
|   Neptune - Datagram Packets
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

#ifndef _NPT_DATA_BUFFER_H_
#define _NPT_DATA_BUFFER_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptTypes.h"
#include "NptConstants.h"

/*----------------------------------------------------------------------
|   NPT_DataBuffer
+---------------------------------------------------------------------*/
class NPT_DataBuffer 
{
 public:
    // constructors & destructor
    NPT_DataBuffer();              // size unknown until first set 
    NPT_DataBuffer(NPT_Size size); // initial size specified
    NPT_DataBuffer(const void* data, NPT_Size size, bool copy = true); // initial data and size specified
    NPT_DataBuffer(const NPT_DataBuffer& other);
    virtual ~NPT_DataBuffer();

    // operators
    NPT_DataBuffer& operator=(const NPT_DataBuffer& copy);
    bool            operator==(const NPT_DataBuffer& other) const;

    // data buffer handling methods
    virtual NPT_Result SetBuffer(NPT_Byte* buffer, NPT_Size bufferSize);
    virtual NPT_Result SetBufferSize(NPT_Size bufferSize);
    virtual NPT_Size   GetBufferSize() const { return m_BufferSize; }
    virtual NPT_Result Reserve(NPT_Size size);
    virtual NPT_Result Clear();

    // data handling methods
    virtual const NPT_Byte* GetData() const { return m_Buffer; }
    virtual NPT_Byte*       UseData() { return m_Buffer; };
    virtual NPT_Size        GetDataSize() const { return m_DataSize; }
    virtual NPT_Result      SetDataSize(NPT_Size size);
    virtual NPT_Result      SetData(const NPT_Byte* data, NPT_Size dataSize);

 protected:
    // members
    bool      m_BufferIsLocal;
    NPT_Byte* m_Buffer;
    NPT_Size  m_BufferSize;
    NPT_Size  m_DataSize;

    // methods
    NPT_Result ReallocateBuffer(NPT_Size size);
};

#endif // _NPT_DATA_BUFFER_H_
