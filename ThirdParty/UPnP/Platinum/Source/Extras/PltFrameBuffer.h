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

#ifndef _PLT_FRAME_BUFFER_H_
#define _PLT_FRAME_BUFFER_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"

/*----------------------------------------------------------------------
|   PLT_FrameBuffer
+---------------------------------------------------------------------*/
class PLT_FrameBuffer 
{
 public:
    // constructor & destructor
    PLT_FrameBuffer(const char* mime_type);
    virtual ~PLT_FrameBuffer();
    
    void Reset();
    void Abort();
    void AddReader()    { m_Readers.Increment(); }
    void RemoveReader() { m_Readers.Decrement(); }
    int  GetNbReaders() { return m_Readers.GetValue(); }
    const char* GetMimeType() { return m_MimeType; }

    // data buffer handling methods
    virtual NPT_Result SetNextFrame(const NPT_Byte* buffer, NPT_Size bufferSize);
    virtual NPT_Result GetNextFrame(NPT_UInt32&     last_frame_index, 
                                    NPT_DataBuffer& buffer, 
                                    NPT_Timeout     timeout = NPT_TIMEOUT_INFINITE);

 protected:
    // members
    NPT_String         m_MimeType;
    bool               m_Aborted;
    NPT_SharedVariable m_FrameIndex;
    NPT_DataBuffer     m_Frame;
    NPT_Mutex          m_FrameLock;
    NPT_AtomicVariable m_Readers;
};

#endif // _PLT_FRAME_BUFFER_H_
