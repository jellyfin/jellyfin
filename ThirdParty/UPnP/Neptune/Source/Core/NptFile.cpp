/*****************************************************************
|
|   Neptune - Files
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

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptFile.h"
#include "NptUtils.h"
#include "NptConstants.h"
#include "NptStreams.h"
#include "NptDataBuffer.h"
#include "NptLogging.h"

/*----------------------------------------------------------------------
|   logging
+---------------------------------------------------------------------*/
NPT_SET_LOCAL_LOGGER("neptune.file")

/*----------------------------------------------------------------------
|   NPT_FilePath::BaseName
+---------------------------------------------------------------------*/
NPT_String 
NPT_FilePath::BaseName(const char* path, bool with_extension /* = true */)
{
    NPT_String result = path;
    int separator = result.ReverseFind(Separator);
    if (separator >= 0) {
        result = path+separator+NPT_StringLength(Separator);
    } 

    if (!with_extension) {
        int dot = result.ReverseFind('.');
        if (dot >= 0) {
            result.SetLength(dot);
        }
    }

    return result;
}

/*----------------------------------------------------------------------
|   NPT_FilePath::DirName
+---------------------------------------------------------------------*/
NPT_String 
NPT_FilePath::DirName(const char* path)
{
    NPT_String result = path;
    int separator = result.ReverseFind(Separator);
    if (separator >= 0) {
        if (separator == 0) {
            result.SetLength(NPT_StringLength(Separator));
        } else {
            result.SetLength(separator);
        }
    } else {
        result.SetLength(0);
    } 

    return result;
}

/*----------------------------------------------------------------------
|   NPT_FilePath::FileExtension
+---------------------------------------------------------------------*/
NPT_String 
NPT_FilePath::FileExtension(const char* path)
{
    NPT_String result = path;
    int separator = result.ReverseFind('.');
    if (separator >= 0) {
        result = path+separator;
    } else {
        result.SetLength(0);
    }

    return result;
}

/*----------------------------------------------------------------------
|   NPT_FilePath::Create
+---------------------------------------------------------------------*/
NPT_String 
NPT_FilePath::Create(const char* directory, const char* basename)
{
    if (!directory || NPT_StringLength(directory) == 0) return basename;
    if (!basename || NPT_StringLength(basename) == 0) return directory;

    NPT_String result = directory;
    if (!result.EndsWith(Separator) && basename[0] != Separator[0]) {
        result += Separator;
    }
    result += basename;

    return result;
}

/*----------------------------------------------------------------------
|   NPT_File::CreateDir
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::CreateDir(const char* path, bool create_intermediate_dirs)
{
    NPT_String full_path = path;

    // normalize path separators
    full_path.Replace((NPT_FilePath::Separator[0] == '/')?'\\':'/', NPT_FilePath::Separator);

    // remove superfluous delimiters at the end
    full_path.TrimRight(NPT_FilePath::Separator);

    // create intermediate directories if needed
    if (create_intermediate_dirs) {
        NPT_String dir_path;

        // look for the next path separator
        int separator = full_path.Find(NPT_FilePath::Separator, 1);
        while (separator > 0) {
            // copy the path up to the separator
            dir_path = full_path.SubString(0, separator);

            // create the directory non recursively
            NPT_CHECK_WARNING(NPT_File::CreateDir(dir_path, false));

            // look for the next delimiter
            separator = full_path.Find(NPT_FilePath::Separator, separator + 1);
        }
    }

    // create the final directory
    NPT_Result result = NPT_File::CreateDir(full_path);

    // return error only if file didn't exist
    if (NPT_FAILED(result) && result != NPT_ERROR_FILE_ALREADY_EXISTS) {
        return result;
    }
    
    return NPT_SUCCESS;
}


/*----------------------------------------------------------------------
|   NPT_File::RemoveDir
+---------------------------------------------------------------------*/
NPT_Result 
NPT_File::RemoveDir(const char* path, bool force_if_not_empty)
{
    NPT_String root_path = path;

    // normalize path separators
    root_path.Replace((NPT_FilePath::Separator[0] == '/')?'\\':'/', NPT_FilePath::Separator);
    
    // remove superfluous delimiters at the end
    root_path.TrimRight(NPT_FilePath::Separator);

    // remove all entries in the directory if required
    if (force_if_not_empty) {
        // enumerate all entries
        NPT_File dir(root_path);
        NPT_List<NPT_String> entries;
        NPT_CHECK_WARNING(dir.ListDir(entries));
        for (NPT_List<NPT_String>::Iterator it = entries.GetFirstItem(); it; ++it) {
            NPT_File::Remove(NPT_FilePath::Create(root_path, *it), true);
        }
    }

    // remove the (now empty) directory
    return NPT_File::RemoveDir(root_path);
}

/*----------------------------------------------------------------------
|   NPT_File::Load
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::Load(const char* path, NPT_DataBuffer& buffer, NPT_FileInterface::OpenMode mode)
{
    // create and open the file
    NPT_File file(path);
    NPT_Result result = file.Open(mode);
    if (NPT_FAILED(result)) return result;
    
    // load the file
    result = file.Load(buffer);

    // close the file
    file.Close();

    return result;
}

/*----------------------------------------------------------------------
|   NPT_File::Load
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::Load(const char* path, NPT_String& data, NPT_FileInterface::OpenMode mode)
{
    NPT_DataBuffer buffer;

    // reset ouput params
    data = "";

    // create and open the file
    NPT_File file(path);
    NPT_Result result = file.Open(mode);
    if (NPT_FAILED(result)) return result;
    
    // load the file
    result = file.Load(buffer);

    if (NPT_SUCCEEDED(result) && buffer.GetDataSize() > 0) {
        data.Assign((const char*)buffer.GetData(), buffer.GetDataSize());
        data.SetLength(buffer.GetDataSize());
    }

    // close the file
    file.Close();

    return result;
}

/*----------------------------------------------------------------------
|   NPT_File::Save
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::Save(const char* filename, NPT_String& data)
{
    NPT_DataBuffer buffer(data.GetChars(), data.GetLength());
    return NPT_File::Save(filename, buffer);
}

/*----------------------------------------------------------------------
|   NPT_File::Save
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::Save(const char* filename, const NPT_DataBuffer& buffer)
{
    // create and open the file
    NPT_File file(filename);
    NPT_Result result = file.Open(NPT_FILE_OPEN_MODE_WRITE | NPT_FILE_OPEN_MODE_CREATE | NPT_FILE_OPEN_MODE_TRUNCATE);
    if (NPT_FAILED(result)) return result;

    // load the file
    result = file.Save(buffer);

    // close the file
    file.Close();

    return result;
}

/*----------------------------------------------------------------------
|   NPT_File::Load
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::Load(NPT_DataBuffer& buffer)
{
    NPT_InputStreamReference input;

    // get the input stream for the file
    NPT_CHECK_WARNING(GetInputStream(input));

    // read the stream
    return input->Load(buffer);
}

/*----------------------------------------------------------------------
|   NPT_File::Save
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::Save(const NPT_DataBuffer& buffer)
{
    NPT_OutputStreamReference output;

    // get the output stream for the file
    NPT_CHECK_WARNING(GetOutputStream(output));

    // write to the stream
    return output->WriteFully(buffer.GetData(), buffer.GetDataSize());
}

/*----------------------------------------------------------------------
|   NPT_File::GetInfo
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::GetInfo(NPT_FileInfo& info)
{
    if (m_IsSpecial) {
        info.m_Type           = NPT_FileInfo::FILE_TYPE_SPECIAL;
        info.m_Size           = 0;
        info.m_Attributes     = 0;
        info.m_AttributesMask = 0;
        return NPT_SUCCESS;
    }
    return GetInfo(m_Path.GetChars(), &info);
}

/*----------------------------------------------------------------------
|   NPT_File::GetSize
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::GetSize(NPT_LargeSize& size)
{
    // default value
    size = 0;
    
    // get the file info
    NPT_FileInfo info;
    GetInfo(info);
    
    switch (info.m_Type) {
        case NPT_FileInfo::FILE_TYPE_DIRECTORY: {
            NPT_List<NPT_String> entries;
            NPT_CHECK_WARNING(ListDir(entries));
            size = entries.GetItemCount();
            break;
        }
        
        case NPT_FileInfo::FILE_TYPE_REGULAR:
        case NPT_FileInfo::FILE_TYPE_OTHER:
            size = info.m_Size;
            return NPT_SUCCESS;
            
        default:
            break;
    } 
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_File::GetSize
+---------------------------------------------------------------------*/
NPT_Result        
NPT_File::GetSize(const char* path, NPT_LargeSize& size)
{
    NPT_File file(path);
    return file.GetSize(size);
}

/*----------------------------------------------------------------------
|   NPT_File::Remove
+---------------------------------------------------------------------*/
NPT_Result 
NPT_File::Remove(const char* path, bool recurse /* = false */)
{
    NPT_FileInfo info;

    // make sure the path exists
    NPT_CHECK_WARNING(GetInfo(path, &info));

    if (info.m_Type == NPT_FileInfo::FILE_TYPE_DIRECTORY) {
        return RemoveDir(path, recurse);
    } else {
        return RemoveFile(path);
    }
}

/*----------------------------------------------------------------------
|   NPT_File::Rename
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::Rename(const char* path)
{
    NPT_Result result = Rename(m_Path.GetChars(), path);
    if (NPT_SUCCEEDED(result)) {
        m_Path = path;
    }
    return result;
}

/*----------------------------------------------------------------------
|   NPT_File::ListDir
+---------------------------------------------------------------------*/
NPT_Result        
NPT_File::ListDir(NPT_List<NPT_String>& entries)
{
    entries.Clear();
    return ListDir(m_Path.GetChars(), entries);
}
