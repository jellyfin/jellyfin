/*****************************************************************
|
|   Platinum - Stream Pump
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

#ifndef _PLT_STREAM_PUMP_H_
#define _PLT_STREAM_PUMP_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptStreams.h"
#include "NptRingBuffer.h"

/*----------------------------------------------------------------------
|   PLT_PipeInputStream
+---------------------------------------------------------------------*/
class PLT_PipeInputStream
{
public:
    // constructor and destructor
    virtual ~PLT_PipeInputStream() {};

    // methods
    virtual NPT_Result Receive(NPT_InputStream& stream, NPT_Size max_bytes_to_read, NPT_Size* bytes_read = 0) = 0;
};

typedef NPT_Reference<PLT_PipeInputStream> PLT_PipeInputStreamReference;

/*----------------------------------------------------------------------
|   PLT_PipeOutputStream
+---------------------------------------------------------------------*/
class PLT_PipeOutputStream
{
public:
    // constructor and destructor
    virtual ~PLT_PipeOutputStream() {};

    // methods
    virtual NPT_Result Transmit(NPT_OutputStream& stream) = 0;
};

typedef NPT_Reference<PLT_PipeOutputStream> PLT_PipeOutputStreamReference;

/*----------------------------------------------------------------------
|   PLT_StreamPump class
+---------------------------------------------------------------------*/
class PLT_StreamPump
{
public:
    virtual ~PLT_StreamPump();

protected:
    // methods
    PLT_StreamPump(NPT_Size size = 65535);
    NPT_Result PullData(NPT_InputStream& input, NPT_Size max_bytes_to_read);
    NPT_Result PushData(NPT_OutputStream& output, NPT_Size& bytes_written);

    // members
    NPT_RingBuffer*     m_RingBuffer;
    NPT_Offset          m_TotalBytesRead;
    NPT_Offset          m_TotalBytesWritten;
};

/*----------------------------------------------------------------------
|   PLT_PipeInputStreamPump class
+---------------------------------------------------------------------*/
class PLT_PipeInputStreamPump : public PLT_StreamPump,
                                public PLT_PipeInputStream
{
public:
    PLT_PipeInputStreamPump(NPT_OutputStreamReference& output, NPT_Size size = 65535);
    virtual ~PLT_PipeInputStreamPump();

    NPT_Result Receive(NPT_InputStream& input, NPT_Size max_bytes_to_read, NPT_Size* bytes_read);

protected:
    NPT_OutputStreamReference   m_Output;
    NPT_Result                  m_LastRes;
};

/*----------------------------------------------------------------------
|   PLT_PipeInputStreamPump class
+---------------------------------------------------------------------*/
class PLT_PipeOutputStreamPump : public PLT_StreamPump,
                                 public PLT_PipeOutputStream
{
public:
    PLT_PipeOutputStreamPump(NPT_InputStreamReference& input, 
                             NPT_Size                  size = 65535,
                             NPT_Size                  max_bytes_to_read = 0);
    virtual ~PLT_PipeOutputStreamPump();

    NPT_Result Transmit(NPT_OutputStream& output);

protected:
    NPT_InputStreamReference    m_Input;
    NPT_Size                    m_MaxBytesToRead;
    NPT_Result                  m_LastRes;
};


#endif // _PLT_STREAM_PUMP_H_ 
