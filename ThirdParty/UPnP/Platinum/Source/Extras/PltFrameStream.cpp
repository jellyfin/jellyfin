/*****************************************************************
|
|   Platinum - Frame Stream
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
#include "PltFrameStream.h"

NPT_SET_LOCAL_LOGGER("platinum.core.framestream")

/*----------------------------------------------------------------------
|   PLT_InputFrameStream::PLT_InputFrameStream
+---------------------------------------------------------------------*/
PLT_InputFrameStream::PLT_InputFrameStream(NPT_Reference<PLT_FrameBuffer>& frame_buffer,
                                           const char*                     boundary) : 
    m_FrameBuffer(frame_buffer),
    m_LastFrameIndex(0),
    m_Boundary(boundary),
    m_Eos(false)
{
    m_FrameBuffer->AddReader();
}

/*----------------------------------------------------------------------
|   PLT_InputFrameStream::~PLT_InputFrameStream
+---------------------------------------------------------------------*/
PLT_InputFrameStream::~PLT_InputFrameStream()
{
    m_FrameBuffer->RemoveReader();
}

/*----------------------------------------------------------------------
|   PLT_InputFrameStream::GetAvailable
+---------------------------------------------------------------------*/
NPT_Result 
PLT_InputFrameStream::GetAvailable(NPT_LargeSize& available) 
{ 
    NPT_CHECK_WARNING(m_Part.GetAvailable(available));

    if (available == 0 && !m_Eos) {
        NPT_CHECK_WARNING(FillBuffer());
        NPT_CHECK_WARNING(m_Part.GetAvailable(available));
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_InputFrameStream::FillBuffer
+---------------------------------------------------------------------*/
NPT_Result 
PLT_InputFrameStream::FillBuffer()
{
    // reset memorystream
    m_Part.SetDataSize(0);
    
    // fetch next frame
    NPT_DataBuffer frame;
    NPT_Result result = m_FrameBuffer->GetNextFrame(m_LastFrameIndex, frame);

    // error (EOS) or empty frame means we're done
    if (NPT_FAILED(result) || frame.GetDataSize() == 0) {
        m_Part.WriteLine("--" + m_Boundary + "--");
        m_Eos = true;
        return NPT_SUCCESS;
    }

    m_Part.WriteLine("--" + m_Boundary);
    m_Part.WriteLine("Content-Type: " + NPT_String(m_FrameBuffer->GetMimeType()));
    m_Part.WriteLine("Content-Length: "+NPT_String::FromInteger(frame.GetDataSize()));
    m_Part.WriteLine("");
    m_Part.Write(frame.GetData(), frame.GetDataSize());
    m_Part.WriteLine("");
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_InputFrameStream::Read
+---------------------------------------------------------------------*/
NPT_Result 
PLT_InputFrameStream::Read(void*     buffer, 
                           NPT_Size  bytes_to_read, 
                           NPT_Size* bytes_read /*= 0*/)
{

    if (bytes_read) *bytes_read = 0;

    if (bytes_to_read == 0) {
        return NPT_SUCCESS;
    }

    // make sure we have data
    NPT_LargeSize available;
    NPT_CHECK_WARNING(GetAvailable(available));

    return m_Part.Read(buffer, bytes_to_read, bytes_read);
}
