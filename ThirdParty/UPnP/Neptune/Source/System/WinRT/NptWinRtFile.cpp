/*****************************************************************
|
|   Neptune - Files :: WinRT Implementation
|
|   (c) 2001-2012 Gilles Boccon-Gibod
|   Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptConfig.h"
#include "NptUtils.h"
#include "NptFile.h"
#include "NptThreads.h"
#include "NptInterfaces.h"
#include "NptStrings.h"
#include "NptDebug.h"

/*----------------------------------------------------------------------
|   NPT_WinRtFile
+---------------------------------------------------------------------*/
class NPT_WinRtFile: public NPT_FileInterface
{
public:
    // constructors and destructor
    NPT_WinRtFile(NPT_File& delegator);
   ~NPT_WinRtFile();

    // NPT_FileInterface methods
    NPT_Result Open(OpenMode mode);
    NPT_Result Close();
    NPT_Result GetInputStream(NPT_InputStreamReference& stream);
    NPT_Result GetOutputStream(NPT_OutputStreamReference& stream);

private:
    // members
    NPT_File&             m_Delegator;
    OpenMode              m_Mode;
};

/*----------------------------------------------------------------------
|   NPT_WinRtFile::NPT_WinRtFile
+---------------------------------------------------------------------*/
NPT_WinRtFile::NPT_WinRtFile(NPT_File& delegator) :
    m_Delegator(delegator),
    m_Mode(0)
{
}

/*----------------------------------------------------------------------
|   NPT_WinRtFile::~NPT_WinRtFile
+---------------------------------------------------------------------*/
NPT_WinRtFile::~NPT_WinRtFile()
{
    Close();
}

/*----------------------------------------------------------------------
|   NPT_WinRtFile::Open
+---------------------------------------------------------------------*/
NPT_Result
NPT_WinRtFile::Open(NPT_File::OpenMode /*mode*/)
{
    return NPT_ERROR_NOT_IMPLEMENTED;
}

/*----------------------------------------------------------------------
|   NPT_WinRtFile::Close
+---------------------------------------------------------------------*/
NPT_Result
NPT_WinRtFile::Close()
{
    // reset the mode
    m_Mode = 0;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_WinRtFile::GetInputStream
+---------------------------------------------------------------------*/
NPT_Result 
NPT_WinRtFile::GetInputStream(NPT_InputStreamReference& stream)
{
    // default value
    stream = NULL;

    return NPT_ERROR_NOT_IMPLEMENTED;
}

/*----------------------------------------------------------------------
|   NPT_WinRtFile::GetOutputStream
+---------------------------------------------------------------------*/
NPT_Result 
NPT_WinRtFile::GetOutputStream(NPT_OutputStreamReference& stream)
{
    // default value
    stream = NULL;

    return NPT_ERROR_NOT_IMPLEMENTED;
}

/*----------------------------------------------------------------------
|   NPT_File::NPT_File
+---------------------------------------------------------------------*/
NPT_File::NPT_File(const char* path) : m_Path(path), m_IsSpecial(false)
{
    m_Delegate = new NPT_WinRtFile(*this);

    if (NPT_StringsEqual(path, NPT_FILE_STANDARD_INPUT)  ||
        NPT_StringsEqual(path, NPT_FILE_STANDARD_OUTPUT) ||
        NPT_StringsEqual(path, NPT_FILE_STANDARD_ERROR)) {
        m_IsSpecial = true;
    } 
}

/*----------------------------------------------------------------------
|   NPT_File::operator=
+---------------------------------------------------------------------*/
NPT_File& 
NPT_File::operator=(const NPT_File& file)
{
    if (this != &file) {
        delete m_Delegate;
        m_Path      = file.m_Path;
        m_IsSpecial = file.m_IsSpecial;
        m_Delegate  = new NPT_WinRtFile(*this);;
    }
    return *this;
}

/*----------------------------------------------------------------------
|   NPT_FilePath::Separator
+---------------------------------------------------------------------*/
const char* const NPT_FilePath::Separator = "\\";

/*----------------------------------------------------------------------
|   NPT_File::GetRoots
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::GetRoots(NPT_List<NPT_String>& roots)
{
    roots.Clear();
    return NPT_ERROR_NOT_IMPLEMENTED;
}

/*----------------------------------------------------------------------
|   NPT_File::GetWorkingDir
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::GetWorkingDir(NPT_String& path)
{
    path.SetLength(0);
    return NPT_ERROR_NOT_IMPLEMENTED;
}

/*----------------------------------------------------------------------
|   NPT_File::GetInfo
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::GetInfo(const char* path, NPT_FileInfo* info)
{
    return NPT_ERROR_NOT_IMPLEMENTED;
}

/*----------------------------------------------------------------------
|   NPT_File::CreateDir
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::CreateDir(const char* path)
{
    return NPT_ERROR_NOT_IMPLEMENTED;
}

/*----------------------------------------------------------------------
|   NPT_File::RemoveFile
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::RemoveFile(const char* path)
{
    return NPT_ERROR_NOT_IMPLEMENTED;
}

/*----------------------------------------------------------------------
|   NPT_File::RemoveDir
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::RemoveDir(const char* path)
{
    return NPT_ERROR_NOT_IMPLEMENTED;
}

/*----------------------------------------------------------------------
|   NPT_File::Rename
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::Rename(const char* from_path, const char* to_path)
{
    return NPT_ERROR_NOT_IMPLEMENTED;
}

/*----------------------------------------------------------------------
|   NPT_File::ListDir
+---------------------------------------------------------------------*/
NPT_Result 
NPT_File::ListDir(const char*           path, 
                  NPT_List<NPT_String>& entries, 
                  NPT_Ordinal           start /* = 0 */, 
                  NPT_Cardinal          max   /* = 0 */)
{
    return NPT_ERROR_NOT_IMPLEMENTED;
}
