/*****************************************************************
|
|      Platinum - Frame Streamer
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
|       includes
+---------------------------------------------------------------------*/
#include "Platinum.h"
#include "PltFrameBuffer.h"
#include "PltFrameStream.h"
#include "PltFrameServer.h"

#include <stdlib.h>

NPT_SET_LOCAL_LOGGER("platinum.core.framestreamer")

/*----------------------------------------------------------------------
|   globals
+---------------------------------------------------------------------*/
struct Options {
    const char* path;
} Options;

/*----------------------------------------------------------------------
|   StreamValidator:
+---------------------------------------------------------------------*/
class StreamValidator : public PLT_StreamValidator
{
public:
    StreamValidator(NPT_Reference<PLT_FrameBuffer>& buffer) : m_Buffer(buffer) {}
    virtual ~StreamValidator() {}
    
    // PLT_StreamValidator methods
    bool OnNewRequestAccept(const NPT_HttpRequest&          request, 
                            const NPT_HttpRequestContext&   context,
                            NPT_HttpResponse&               response, 
                            NPT_Reference<PLT_FrameBuffer>& buffer) {
        NPT_COMPILER_UNUSED(request);
        NPT_COMPILER_UNUSED(response);
        NPT_COMPILER_UNUSED(context);
        // TODO: should compare HTTP Header Accept and buffer mimetype
        buffer = m_Buffer;
        return true;
    }
    
    NPT_Reference<PLT_FrameBuffer> m_Buffer;
};

/*----------------------------------------------------------------------
|   FrameWriter
+---------------------------------------------------------------------*/
class FrameWriter : public NPT_Thread
{
public:
    FrameWriter(NPT_Reference<PLT_FrameBuffer>& frame_buffer,
                const char*                     frame_folder) : 
        m_FrameBuffer(frame_buffer),
        m_Aborted(false),
        m_Folder(frame_folder)
        {}

    const char* GetPath(NPT_List<NPT_String>::Iterator& entry) {
        if (!entry) return NULL;

        if (!entry->EndsWith(".jpg", true)) {
            return GetPath(++entry);
        }

        return *entry;
    }

    void Run() {
        NPT_List<NPT_String> entries;
        const char* frame_path = NULL;
        NPT_DataBuffer frame;
        NPT_List<NPT_String>::Iterator entry;
        
        while (!m_Aborted) {
            // has number of images changed since last time?
            NPT_LargeSize count;
            NPT_File::GetSize(m_Folder, count);
            
            if (entries.GetItemCount() == 0 || entries.GetItemCount() != count) {
                NPT_File::ListDir(m_Folder, entries);
                entry = entries.GetFirstItem();
                if (!entry) {
                    // Wait a bit before continuing
                    NPT_System::Sleep(NPT_TimeInterval(0.2f));
                    continue;
                }
                
                // set delay based on number of files if necessary
                m_Delay = NPT_TimeInterval((float)1.f/entries.GetItemCount());
            }
            
            // look for path to next image
            if (!(frame_path = GetPath(entry))) {
                // loop back if necessary
                entry = entries.GetFirstItem();
                continue;
            }
            
            if (NPT_FAILED(NPT_File::Load(NPT_FilePath::Create(m_Folder, frame_path), frame))) {
                NPT_LOG_SEVERE_1("Image \"%s\" not found!", frame_path?frame_path:"none");
                // clear previously loaded names so we reload entire set
                entries.Clear();
                continue;
            }

            if (NPT_FAILED(m_FrameBuffer->SetNextFrame(frame.GetData(), 
                                                       frame.GetDataSize()))) {
                NPT_LOG_SEVERE_1("Failed to set next frame %s", frame_path);
                goto failure;
            }

            // Wait before loading next frame
            NPT_System::Sleep(m_Delay);

            // look for next entry
            ++entry;
        }

failure:
        // one more time to unblock any pending readers
        m_FrameBuffer->Abort();
    }

    NPT_Reference<PLT_FrameBuffer> m_FrameBuffer;
    bool                           m_Aborted;
    NPT_String                     m_Folder;
    NPT_TimeInterval               m_Delay;
};

/*----------------------------------------------------------------------
|   PrintUsageAndExit
+---------------------------------------------------------------------*/
static void
PrintUsageAndExit(char** args)
{
    fprintf(stderr, "usage: %s <images path>\n", args[0]);
    fprintf(stderr, "<path> : local path to serve images from\n");
    exit(1);
}

/*----------------------------------------------------------------------
|   ParseCommandLine
+---------------------------------------------------------------------*/
static void
ParseCommandLine(char** args)
{
    char** _args = args++;
    const char* arg;

    /* default values */
    Options.path = NULL;
    
    while ((arg = *args++)) {
        if (Options.path == NULL) {
            Options.path = arg;
        } else {
            fprintf(stderr, "ERROR: too many arguments\n");
            PrintUsageAndExit(_args);
        }
    }

    /* check args */
    if (Options.path == NULL) {
        fprintf(stderr, "ERROR: path missing\n");
        PrintUsageAndExit(_args);
    }
}

/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int argc, char** argv)
{
    NPT_COMPILER_UNUSED(argc);
    
    /* parse command line */
    ParseCommandLine(argv);
    
    // frame buffer 
    NPT_Reference<PLT_FrameBuffer> frame_buffer(new PLT_FrameBuffer("image/jpeg"));
    
    // A Framewriter reading images from a folder and writing them
    // into frame buffer in a loop
    FrameWriter writer(frame_buffer, Options.path);
    writer.Start();

    // stream request validation
    StreamValidator validator(frame_buffer);
    
    // frame server receiving requests and serving frames 
    // read from frame buffer
    NPT_Reference<PLT_FrameServer> device( 
        new PLT_FrameServer(
            "frame",
            validator,
            NPT_IpAddress::Any,
            8099));

    if (NPT_FAILED(device->Start()))
        return 1;

    char buf[256];
    while (gets(buf))
    {
        if (*buf == 'q')
        {
            break;
        }
    }

    writer.m_Aborted = true;

    return 0;
}
