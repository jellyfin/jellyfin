/*****************************************************************
|
|   Platinum - Frame Buffer
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
#include "PltFrameBuffer.h"

NPT_SET_LOCAL_LOGGER("platinum.core.framebuffer")

/*----------------------------------------------------------------------
|   PLT_FrameBuffer::PLT_FrameBuffer
+---------------------------------------------------------------------*/
PLT_FrameBuffer::PLT_FrameBuffer(const char* mime_type) :
    m_MimeType(mime_type),
    m_Aborted(false)
{
}

/*----------------------------------------------------------------------
|   PLT_FrameBuffer::~PLT_FrameBuffer
+---------------------------------------------------------------------*/
PLT_FrameBuffer::~PLT_FrameBuffer()
{
}

/*----------------------------------------------------------------------
|   PLT_FrameBuffer::Reset
+---------------------------------------------------------------------*/
void
PLT_FrameBuffer::Reset()
{
    NPT_AutoLock lock(m_FrameLock);
    
    // mark that we're planning to receive more frames
    m_Aborted = false;
    
    // reset to 0 for new readers to
    m_FrameIndex.SetValue(0);
}

/*----------------------------------------------------------------------
|   PLT_FrameBuffer::Abort
+---------------------------------------------------------------------*/
void
PLT_FrameBuffer::Abort()
{
    NPT_AutoLock lock(m_FrameLock);
    
    // mark that we're not planning to receive more frames
    m_Aborted = true;
    
    // increment to unlock readers
    m_FrameIndex.SetValue(m_FrameIndex.GetValue()+1);
}

/*----------------------------------------------------------------------
|   PLT_FrameBuffer::SetNextFrame
+---------------------------------------------------------------------*/
NPT_Result
PLT_FrameBuffer::SetNextFrame(const NPT_Byte* data, NPT_Size size)
{
    NPT_AutoLock lock(m_FrameLock);

    m_Frame.SetData(data, size);
    m_FrameIndex.SetValue(m_FrameIndex.GetValue()+1);

    NPT_LOG_INFO_1("Set frame %d", m_FrameIndex.GetValue());
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_FrameBuffer::GetNextFrame
+---------------------------------------------------------------------*/
NPT_Result
PLT_FrameBuffer::GetNextFrame(NPT_UInt32&     last_frame_index, 
                              NPT_DataBuffer& buffer, 
                              NPT_Timeout     timeout)
{
    NPT_CHECK_WARNING(m_FrameIndex.WaitWhileEquals(last_frame_index, timeout));

    {
        NPT_AutoLock lock(m_FrameLock);
        
        // if we're aborted or we reseted, disconnect
        if (m_Aborted || last_frame_index > (NPT_UInt32)m_FrameIndex.GetValue()) 
            return NPT_ERROR_EOS;
        
        buffer.SetData(m_Frame.GetData(), m_Frame.GetDataSize());

        // update current frame index
        last_frame_index = m_FrameIndex.GetValue();
        NPT_LOG_INFO_1("Retrieved frame %d", last_frame_index);
    }

    return NPT_SUCCESS;
}
