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

/** @file
 Datagram Input/Output Neptune streams
 */

#ifndef _PLT_DATAGRAM_H_
#define _PLT_DATAGRAM_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"

/*----------------------------------------------------------------------
|   PLT_InputDatagramStream
+---------------------------------------------------------------------*/
/**
 The PLT_InputDatagramStream class is a simple buffered input stream 
 used when reading SSDP packets on a UDP socket. It allows to use Neptune
 HTTP parsing as if reading on a TCP socket.
 */
class PLT_InputDatagramStream : public NPT_InputStream
{
public:
    // methods
    PLT_InputDatagramStream(NPT_UdpSocket* socket,
                            NPT_Size       buffer_size = 2000);
    virtual ~PLT_InputDatagramStream();
    
    NPT_Result GetInfo(NPT_SocketInfo& info);

    // NPT_InputStream methods
    NPT_Result Read(void*     buffer, 
                    NPT_Size  bytes_to_read, 
                    NPT_Size* bytes_read = 0);

    NPT_Result Seek(NPT_Position offset) { NPT_COMPILER_UNUSED(offset); return NPT_FAILURE; }
    NPT_Result Skip(NPT_Size offset) { NPT_COMPILER_UNUSED(offset); return NPT_FAILURE; }
    NPT_Result Tell(NPT_Position& offset){ NPT_COMPILER_UNUSED(offset); return NPT_FAILURE; }
    NPT_Result GetSize(NPT_LargeSize& size)   { NPT_COMPILER_UNUSED(size); return NPT_FAILURE; }
    NPT_Result GetAvailable(NPT_LargeSize& available) { NPT_COMPILER_UNUSED(available); return NPT_FAILURE; }
        
protected:
    NPT_UdpSocket*      m_Socket;
    NPT_SocketInfo      m_Info;
    NPT_DataBuffer      m_Buffer;
    NPT_Position        m_BufferOffset;
};

typedef NPT_Reference<PLT_InputDatagramStream> PLT_InputDatagramStreamReference;

/*----------------------------------------------------------------------
|   PLT_OutputDatagramStream
+---------------------------------------------------------------------*/
/**
 The PLT_OutputDatagramStream class is a simple buffered output stream 
 used when writing SSDP packets on a UDP socket. It allows to use Neptune
 HTTP client as if writing on a TCP socket.
 */
class PLT_OutputDatagramStream : public NPT_OutputStream
{
public:
    // methods
    PLT_OutputDatagramStream(NPT_UdpSocket*           socket, 
                             NPT_Size                 size = 4096,
                             const NPT_SocketAddress* address = NULL);
    virtual ~PLT_OutputDatagramStream();

    // NPT_OutputStream methods
    NPT_Result Write(const void* buffer, NPT_Size bytes_to_write, NPT_Size* bytes_written = NULL);
    NPT_Result Flush();

    NPT_Result Seek(NPT_Position offset)  { NPT_COMPILER_UNUSED(offset); return NPT_FAILURE; }
    NPT_Result Tell(NPT_Position& offset) { NPT_COMPILER_UNUSED(offset); return NPT_FAILURE; }

protected:
    NPT_UdpSocket*     m_Socket;
    NPT_DataBuffer     m_Buffer;
    NPT_SocketAddress* m_Address;
};

typedef NPT_Reference<PLT_OutputDatagramStream> PLT_OutputDatagramStreamReference;

#endif /* _PLT_DATAGRAM_H_ */
