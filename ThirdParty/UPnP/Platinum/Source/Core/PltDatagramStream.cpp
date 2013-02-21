/*****************************************************************
|
|   Platinum - Datagram Stream
|
| Copyright (c) 2004-2010, Plutinosoft, LLC.
| All rights reserved.
| http://www.plutinosoft.com
|
| This program is free software; you can redistribute it and/or
| modify it under the terms of the GNU General Public License
| as published by the Free Software Foundation; either version 2
| of the License, or (at your option) any later version.
|
| OEMs, ISVs, VARs and other distributors that combine and 
| distribute commercially licensed software with Platinum software
| and do not wish to distribute the source code for the commercially
| licensed software under version 2, or (at your option) any later
| version, of the GNU General Public License (the "GPL") must enter
| into a commercial license agreement with Plutinosoft, LLC.
| licensing@plutinosoft.com
|  
| This program is distributed in the hope that it will be useful,
| but WITHOUT ANY WARRANTY; without even the implied warranty of
| MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
| GNU General Public License for more details.
|
| You should have received a copy of the GNU General Public License
| along with this program; see the file LICENSE.txt. If not, write to
| the Free Software Foundation, Inc., 
| 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
| http://www.gnu.org/licenses/gpl-2.0.html
|
****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "PltDatagramStream.h"

/*----------------------------------------------------------------------
|   PLT_InputDatagramStream::PLT_InputDatagramStream
+---------------------------------------------------------------------*/
PLT_InputDatagramStream::PLT_InputDatagramStream(NPT_UdpSocket* socket,
                                                 NPT_Size       buffer_size) : 
    m_Socket(socket),
    m_BufferOffset(0)
{
    m_Buffer.SetBufferSize(buffer_size);
}

/*----------------------------------------------------------------------
|   PLT_InputDatagramStream::~PLT_InputDatagramStream
+---------------------------------------------------------------------*/
PLT_InputDatagramStream::~PLT_InputDatagramStream()
{
}

/*----------------------------------------------------------------------
|   PLT_InputDatagramStream::Read
+---------------------------------------------------------------------*/
NPT_Result 
PLT_InputDatagramStream::Read(void*     buffer, 
                              NPT_Size  bytes_to_read, 
                              NPT_Size* bytes_read /*= 0*/)
{
    NPT_Result res = NPT_SUCCESS;

    if (bytes_read) *bytes_read = 0;

    // always try to read from socket if needed even if bytes_to_read is 0
    if (m_Buffer.GetDataSize() == 0) {        
        // read data into it now
        NPT_SocketAddress addr;
        res = m_Socket->Receive(m_Buffer, &addr);
        
        // update info
        m_Socket->GetInfo(m_Info);
        m_Info.remote_address = addr;
    }
        
    if (bytes_to_read == 0) return res;
    
    if (NPT_SUCCEEDED(res)) {
        NPT_Size available = m_Buffer.GetDataSize()-(NPT_Size)m_BufferOffset;
        NPT_Size _bytes_to_read = bytes_to_read<available?bytes_to_read:available;
        NPT_CopyMemory(buffer, m_Buffer.UseData()+m_BufferOffset, _bytes_to_read);
        m_BufferOffset += _bytes_to_read;
        
        if (bytes_read) *bytes_read = _bytes_to_read;
        
        // read buffer entirety, reset for next time
        if (m_BufferOffset == m_Buffer.GetDataSize()) {
            m_BufferOffset = 0;
            m_Buffer.SetDataSize(0);
        }
    }

    return res;
}

/*----------------------------------------------------------------------
|   PLT_OutputDatagramStream::GetInfo
+---------------------------------------------------------------------*/
NPT_Result 
PLT_InputDatagramStream::GetInfo(NPT_SocketInfo& info)
{
    info = m_Info;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_OutputDatagramStream::PLT_OutputDatagramStream
+---------------------------------------------------------------------*/
PLT_OutputDatagramStream::PLT_OutputDatagramStream(NPT_UdpSocket*   socket, 
                                                   NPT_Size         size, 
                                                   const NPT_SocketAddress* address) : 
    m_Socket(socket),
    m_Address(address?new NPT_SocketAddress(address->GetIpAddress(), address->GetPort()):NULL)
{
    m_Buffer.SetBufferSize(size);
}

/*----------------------------------------------------------------------
|   PLT_OutputDatagramStream::~PLT_OutputDatagramStream
+---------------------------------------------------------------------*/
PLT_OutputDatagramStream::~PLT_OutputDatagramStream()
{
    delete m_Address;
}

/*----------------------------------------------------------------------
|   PLT_OutputDatagramStream::Write
+---------------------------------------------------------------------*/
NPT_Result 
PLT_OutputDatagramStream::Write(const void* buffer, NPT_Size bytes_to_write, NPT_Size* bytes_written /* = NULL */)
{
    // calculate if we need to increase the buffer
    NPT_Int32 overflow = bytes_to_write - m_Buffer.GetBufferSize() + m_Buffer.GetDataSize();
    if (overflow > 0) {
        m_Buffer.Reserve(m_Buffer.GetBufferSize() + overflow);
    }
    // copy data in place at the end of what we have there already
    NPT_CopyMemory(m_Buffer.UseData() + m_Buffer.GetDataSize(), buffer, bytes_to_write);
    m_Buffer.SetDataSize(m_Buffer.GetDataSize() + bytes_to_write);

    if (bytes_written) *bytes_written = bytes_to_write;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_OutputDatagramStream::Flush
+---------------------------------------------------------------------*/
NPT_Result 
PLT_OutputDatagramStream::Flush()
{
    // send buffer now
    m_Socket->Send(m_Buffer, m_Address);

    // reset buffer
    m_Buffer.SetDataSize(0);
    return NPT_SUCCESS;
}
