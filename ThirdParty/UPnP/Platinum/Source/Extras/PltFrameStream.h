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

#ifndef _PLT_FRAMESTREAM_H_
#define _PLT_FRAMESTREAM_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "PltFrameBuffer.h"

/*----------------------------------------------------------------------
|   PLT_InputFrameStream
+---------------------------------------------------------------------*/
class PLT_InputFrameStream : public NPT_InputStream
{
public:
    // methods
    PLT_InputFrameStream(NPT_Reference<PLT_FrameBuffer>& frame_buffer,
                         const char*                     boundary);
    virtual ~PLT_InputFrameStream();

    // NPT_InputStream methods
    NPT_Result Read(void*     buffer, 
                    NPT_Size  bytes_to_read, 
                    NPT_Size* bytes_read = 0);

    NPT_Result Seek(NPT_Position offset)      { NPT_COMPILER_UNUSED(offset); return NPT_FAILURE; }
    NPT_Result Skip(NPT_Size offset)          { NPT_COMPILER_UNUSED(offset); return NPT_FAILURE; }
    NPT_Result Tell(NPT_Position& offset)     { NPT_COMPILER_UNUSED(offset); return NPT_FAILURE; }
    NPT_Result GetSize(NPT_LargeSize& size)   { NPT_COMPILER_UNUSED(size);   return NPT_FAILURE; }
    NPT_Result GetAvailable(NPT_LargeSize& available);
        
private:
    NPT_Result FillBuffer();

protected:
    NPT_Reference<PLT_FrameBuffer> m_FrameBuffer;
    NPT_MemoryStream               m_Part;
    NPT_UInt32                     m_LastFrameIndex;
    NPT_String                     m_Boundary;
    bool                           m_Eos;
};

typedef NPT_Reference<PLT_InputFrameStream> PLT_InputFrameStreamReference;

#endif /* _PLT_FRAMESTREAM_H_ */
