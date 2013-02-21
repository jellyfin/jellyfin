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

#ifndef _NPT_FILE_H_
#define _NPT_FILE_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptTypes.h"
#include "NptStreams.h"
#include "NptTime.h"

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
const int NPT_ERROR_NO_SUCH_FILE          = NPT_ERROR_BASE_FILE - 0;
const int NPT_ERROR_FILE_NOT_OPEN         = NPT_ERROR_BASE_FILE - 1;
const int NPT_ERROR_FILE_BUSY             = NPT_ERROR_BASE_FILE - 2;
const int NPT_ERROR_FILE_ALREADY_OPEN     = NPT_ERROR_BASE_FILE - 3;
const int NPT_ERROR_FILE_NOT_READABLE     = NPT_ERROR_BASE_FILE - 4;
const int NPT_ERROR_FILE_NOT_WRITABLE     = NPT_ERROR_BASE_FILE - 5;
const int NPT_ERROR_FILE_NOT_DIRECTORY    = NPT_ERROR_BASE_FILE - 6;
const int NPT_ERROR_FILE_ALREADY_EXISTS   = NPT_ERROR_BASE_FILE - 7;
const int NPT_ERROR_FILE_NOT_ENOUGH_SPACE = NPT_ERROR_BASE_FILE - 8;
const int NPT_ERROR_DIRECTORY_NOT_EMPTY   = NPT_ERROR_BASE_FILE - 9;

/**
 * File open modes.
 * Use a combination of these flags to indicate how a file should be opened 
 * Note all combinations of flags are valid or meaningful:
 * If NPT_FILE_OPEN_MODE_WRITE is not set, then NPT_FILE_OPEN_MODE_CREATE, 
 * NPT_FILE_OPEN_MODE_TRUNCATE and NPT_FILE_OPEN_MODE_APPEND are ignored.
 * If NPT_FILE_OPEN_MODE_APPEND is set, then NPT_FILE_OPEN_MODE_CREATE is
 * automatically implied whether it is set or not.
 * NPT_FILE_OPEN_MODE_CREATE and NPT_FILE_OPEN_MODE_TRUNCATE imply each
 * other (if one is set, the other one is automatically implied)
 */
const unsigned int NPT_FILE_OPEN_MODE_READ       = 0x01;
const unsigned int NPT_FILE_OPEN_MODE_WRITE      = 0x02;
const unsigned int NPT_FILE_OPEN_MODE_CREATE     = 0x04;
const unsigned int NPT_FILE_OPEN_MODE_TRUNCATE   = 0x08;
const unsigned int NPT_FILE_OPEN_MODE_UNBUFFERED = 0x10;
const unsigned int NPT_FILE_OPEN_MODE_APPEND     = 0x20;

const unsigned int NPT_FILE_ATTRIBUTE_READ_ONLY = 0x01;
const unsigned int NPT_FILE_ATTRIBUTE_LINK      = 0x02;

#define NPT_FILE_STANDARD_INPUT  "@STDIN"
#define NPT_FILE_STANDARD_OUTPUT "@STDOUT"
#define NPT_FILE_STANDARD_ERROR  "@STDERR"

/*----------------------------------------------------------------------
|   class references
+---------------------------------------------------------------------*/
class NPT_DataBuffer;

/*----------------------------------------------------------------------
|   NPT_FileInfo
+---------------------------------------------------------------------*/
struct NPT_FileInfo
{
    // types
    typedef enum {
        FILE_TYPE_NONE,
        FILE_TYPE_REGULAR,
        FILE_TYPE_DIRECTORY,
        FILE_TYPE_SPECIAL,
        FILE_TYPE_OTHER
    } FileType;
    
    // constructor
    NPT_FileInfo() : m_Type(FILE_TYPE_NONE), m_Size(0), m_AttributesMask(0), m_Attributes(0) {}
    
    // members
    FileType      m_Type;
    NPT_UInt64    m_Size;
    NPT_Flags     m_AttributesMask;
    NPT_Flags     m_Attributes;
    NPT_TimeStamp m_CreationTime;
    NPT_TimeStamp m_ModificationTime;
};

/*----------------------------------------------------------------------
|   NPT_FilePath
+---------------------------------------------------------------------*/
class NPT_FilePath
{
public:
    // class members
    static const char* const Separator;

    // class methods
    static NPT_String BaseName(const char* path, bool with_extension = true);
    static NPT_String DirName(const char* path);
    static NPT_String FileExtension(const char* path);
    static NPT_String Create(const char* directory, const char* base);
    
private:
    NPT_FilePath() {} // this class can't have instances
};

/*----------------------------------------------------------------------
|   NPT_FileInterface
+---------------------------------------------------------------------*/
class NPT_FileInterface
{
public:
    // types
    typedef unsigned int OpenMode;

    // constructors and destructor
    virtual ~NPT_FileInterface() {}

    // methods
    virtual NPT_Result Open(OpenMode mode) = 0;
    virtual NPT_Result Close() = 0;
    virtual NPT_Result GetInputStream(NPT_InputStreamReference& stream) = 0;
    virtual NPT_Result GetOutputStream(NPT_OutputStreamReference& stream) = 0;
};

/*----------------------------------------------------------------------
|   NPT_File
+---------------------------------------------------------------------*/
class NPT_File : public NPT_FileInterface
{
public:
    // class methods
    static NPT_Result GetRoots(NPT_List<NPT_String>& roots);
    static NPT_Result GetSize(const char* path, NPT_LargeSize &size);
    static NPT_Result GetInfo(const char* path, NPT_FileInfo* info = NULL);
    static bool       Exists(const char* path) { return NPT_SUCCEEDED(GetInfo(path)); }
    static NPT_Result Remove(const char* path, bool recurse = false);
    static NPT_Result RemoveFile(const char* path);
    static NPT_Result RemoveDir(const char* path);
    static NPT_Result RemoveDir(const char* path, bool force_if_not_empty);
    static NPT_Result Rename(const char* from_path, const char* to_path);
    static NPT_Result ListDir(const char* path, NPT_List<NPT_String>& entries, NPT_Ordinal start = 0, NPT_Cardinal count = 0);
    static NPT_Result CreateDir(const char* path);
    static NPT_Result CreateDir(const char* path, bool create_intermediate_dirs);
    static NPT_Result GetWorkingDir(NPT_String& path);
    static NPT_Result Load(const char* path, NPT_DataBuffer& buffer, NPT_FileInterface::OpenMode mode = NPT_FILE_OPEN_MODE_READ);
    static NPT_Result Load(const char* path, NPT_String& data, NPT_FileInterface::OpenMode mode = NPT_FILE_OPEN_MODE_READ);
    static NPT_Result Save(const char* path, NPT_String& data);
    static NPT_Result Save(const char* path, const NPT_DataBuffer& buffer);
    
    // constructors and destructor
    NPT_File(const char* path);
   ~NPT_File() { delete m_Delegate; }

    // methods
    NPT_Result          Load(NPT_DataBuffer& buffer);
    NPT_Result          Save(const NPT_DataBuffer& buffer);
    const NPT_String&   GetPath() { return m_Path; }
    NPT_Result          GetSize(NPT_LargeSize &size);
    NPT_Result          GetInfo(NPT_FileInfo& info);
    NPT_Result          ListDir(NPT_List<NPT_String>& entries);
    NPT_Result          Rename(const char* path);
    
    // NPT_FileInterface methods
    NPT_Result Open(OpenMode mode) {
        return m_Delegate->Open(mode);
    }
    NPT_Result Close() {
        return m_Delegate->Close();
    }
    NPT_Result GetInputStream(NPT_InputStreamReference& stream) {
        return m_Delegate->GetInputStream(stream);
    }
    NPT_Result GetOutputStream(NPT_OutputStreamReference& stream) {
        return m_Delegate->GetOutputStream(stream);
    }

    // operators
    NPT_File& operator=(const NPT_File& file);

protected:
    // members
    NPT_FileInterface* m_Delegate;
    NPT_String         m_Path;
    bool               m_IsSpecial;
};

/*----------------------------------------------------------------------
|   NPT_FileDateComparator
+---------------------------------------------------------------------*/
class NPT_FileDateComparator {
public: 
    NPT_FileDateComparator(const char* directory) : m_Directory(directory) {}
    NPT_Int32 operator()(const NPT_String& file1, const NPT_String& file2) const {
        NPT_FileInfo info1, info2;
        if (NPT_FAILED(NPT_File::GetInfo(NPT_FilePath::Create(m_Directory, file1), &info1))) return -1;
        if (NPT_FAILED(NPT_File::GetInfo(NPT_FilePath::Create(m_Directory, file2), &info2))) return -1;
        return (info1.m_ModificationTime == info2.m_ModificationTime) ? 0 : (info1.m_ModificationTime < info2.m_ModificationTime ? -1 : 1);
    }
    
private:
    NPT_String m_Directory;
};

#endif // _NPT_FILE_H_ 
