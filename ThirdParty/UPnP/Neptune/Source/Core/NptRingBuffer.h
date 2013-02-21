/*****************************************************************
|
|   Neptune - Ring Buffer
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

#ifndef _NPT_RING_BUFFER_H_
#define _NPT_RING_BUFFER_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptTypes.h"
#include "NptReferences.h"

/*----------------------------------------------------------------------
|   NPT_RingBuffer
+---------------------------------------------------------------------*/
class NPT_RingBuffer
{
 public:
    // methods
                  NPT_RingBuffer(NPT_Size size);
                  NPT_RingBuffer(void* buffer, NPT_Size size);
    virtual      ~NPT_RingBuffer();
    NPT_Size      GetSpace() const;
    NPT_Size      GetContiguousSpace() const;
    NPT_Result    Write(const void* buffer, NPT_Size byte_count);
    NPT_Size      GetAvailable() const;
    NPT_Size      GetContiguousAvailable() const;
    NPT_Result    Read(void* buffer, NPT_Size byte_count);
    unsigned char ReadByte();
    unsigned char PeekByte(NPT_Position offset);
    NPT_Result    MoveIn(NPT_Position offset);
    NPT_Result    MoveOut(NPT_Position offset);
    NPT_Result    Flush();
    NPT_Result    Close();
    bool          IsClosed() { return m_Closed; }

    // accessors
    unsigned char* GetWritePointer()  { return m_In; }
    unsigned char* GetReadPointer()   { return m_Out;}

 private:
    // members
    struct {
        unsigned char* start;
        unsigned char* end;
    }              m_Data;
    unsigned char* m_In;
    unsigned char* m_Out;
    NPT_Size       m_Size;
    bool           m_BufferIsLocal;
    bool           m_Closed;
};

typedef NPT_Reference<NPT_RingBuffer> NPT_RingBufferReference;

#endif // _NPT_RING_BUFFER_H_
