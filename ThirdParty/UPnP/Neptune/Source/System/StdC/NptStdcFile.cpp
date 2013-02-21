/*****************************************************************
|
|   Neptune - Files :: Standard C Implementation
|
|   (c) 2001-2008 Gilles Boccon-Gibod
|   Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#define _LARGEFILE_SOURCE
#define _LARGEFILE_SOURCE64
#define _FILE_OFFSET_BITS 64

#include <stdio.h>
#if !defined(_WIN32_WCE)
#include <string.h>
#include <sys/stat.h>
#include <errno.h>
#else
#include <stdio.h>
#define errno GetLastError()
#endif

#include "NptConfig.h"
#include "NptUtils.h"
#include "NptFile.h"
#include "NptThreads.h"
#include "NptInterfaces.h"
#include "NptStrings.h"
#include "NptDebug.h"

#if defined(NPT_CONFIG_HAVE_SHARE_H)
#include <share.h>
#endif

#if defined(_WIN32)
extern FILE *NPT_fsopen_utf8(const char* path, const char* mode, int sh_flags);
extern FILE *NPT_fopen_utf8(const char* path, const char* mode);
#define fopen   NPT_fopen_utf8
#define fopen_s NPT_fopen_s_utf8
#define _fsopen NPT_fsopen_utf8
#endif

/*----------------------------------------------------------------------
|   compatibility wrappers
+---------------------------------------------------------------------*/
#if !defined(NPT_CONFIG_HAVE_FOPEN_S)
static int fopen_s(FILE**      file,
                   const char* filename,
                   const char* mode)
{
    *file = fopen(filename, mode);

#if defined(_WIN32_WCE)
    if (*file == NULL) return ENOENT;
#else
    if (*file == NULL) return errno;
#endif
    return 0;
}
#endif // defined(NPT_CONFIG_HAVE_FOPEN_S

/*----------------------------------------------------------------------
|   MapErrno
+---------------------------------------------------------------------*/
static NPT_Result
MapErrno(int err) {
    switch (err) {
      case EACCES:       return NPT_ERROR_PERMISSION_DENIED;
      case EPERM:        return NPT_ERROR_PERMISSION_DENIED;
      case ENOENT:       return NPT_ERROR_NO_SUCH_FILE;
#if defined(ENAMETOOLONG)
      case ENAMETOOLONG: return NPT_ERROR_INVALID_PARAMETERS;
#endif
      case EBUSY:        return NPT_ERROR_FILE_BUSY;
      case EROFS:        return NPT_ERROR_FILE_NOT_WRITABLE;
      case ENOTDIR:      return NPT_ERROR_FILE_NOT_DIRECTORY;
      default:           return NPT_ERROR_ERRNO(err);
    }
}

/*----------------------------------------------------------------------
|   NPT_StdcFileWrapper
+---------------------------------------------------------------------*/
class NPT_StdcFileWrapper
{
public:
    // constructors and destructor
    NPT_StdcFileWrapper(FILE* file, const char* name) : m_File(file), m_Name(name) {}
    ~NPT_StdcFileWrapper() {
        if (m_File != NULL && 
            m_File != stdin && 
            m_File != stdout && 
            m_File != stderr) {
            fclose(m_File);
        }
    }

    // members
    FILE*      m_File;
    NPT_String m_Name;
};

typedef NPT_Reference<NPT_StdcFileWrapper> NPT_StdcFileReference;

/*----------------------------------------------------------------------
|   NPT_StdcFileStream
+---------------------------------------------------------------------*/
class NPT_StdcFileStream
{
public:
    // constructors and destructor
    NPT_StdcFileStream(NPT_StdcFileReference file) :
      m_FileReference(file) {}

    // NPT_FileInterface methods
    NPT_Result Seek(NPT_Position offset);
    NPT_Result Tell(NPT_Position& offset);
    NPT_Result Flush();

protected:
    // constructors and destructors
    virtual ~NPT_StdcFileStream() {}

    // members
    NPT_StdcFileReference m_FileReference;
};

/*----------------------------------------------------------------------
|   NPT_StdcFileStream::Seek
+---------------------------------------------------------------------*/
NPT_Result
NPT_StdcFileStream::Seek(NPT_Position offset)
{
    size_t result;

    result = NPT_fseek(m_FileReference->m_File, offset, SEEK_SET);
    if (result == 0) {
        return NPT_SUCCESS;
    } else {
        return NPT_FAILURE;
    }
}

/*----------------------------------------------------------------------
|   NPT_StdcFileStream::Tell
+---------------------------------------------------------------------*/
NPT_Result
NPT_StdcFileStream::Tell(NPT_Position& offset)
{
    offset = 0;

    NPT_Int64 pos = NPT_ftell(m_FileReference->m_File);
    if (pos < 0) return NPT_FAILURE;

    offset = pos;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_StdcFileStream::Flush
+---------------------------------------------------------------------*/
NPT_Result
NPT_StdcFileStream::Flush()
{
    fflush(m_FileReference->m_File);
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_StdcFileInputStream
+---------------------------------------------------------------------*/
class NPT_StdcFileInputStream : public NPT_InputStream,
                                private NPT_StdcFileStream
                                
{
public:
    // constructors and destructor
    NPT_StdcFileInputStream(NPT_StdcFileReference& file) :
        NPT_StdcFileStream(file) {}

    // NPT_InputStream methods
    NPT_Result Read(void*     buffer, 
                    NPT_Size  bytes_to_read, 
                    NPT_Size* bytes_read);
    NPT_Result Seek(NPT_Position offset) {
        return NPT_StdcFileStream::Seek(offset);
    }
    NPT_Result Tell(NPT_Position& offset) {
        return NPT_StdcFileStream::Tell(offset);
    }
    NPT_Result GetSize(NPT_LargeSize& size);
    NPT_Result GetAvailable(NPT_LargeSize& available);
};

/*----------------------------------------------------------------------
|   NPT_StdcFileInputStream::Read
+---------------------------------------------------------------------*/
NPT_Result
NPT_StdcFileInputStream::Read(void*     buffer, 
                              NPT_Size  bytes_to_read, 
                              NPT_Size* bytes_read)
{
    size_t nb_read;

    // check the parameters
    if (buffer == NULL) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // read from the file
    nb_read = fread(buffer, 1, bytes_to_read, m_FileReference->m_File);
    if (nb_read > 0) {
        if (bytes_read) *bytes_read = (NPT_Size)nb_read;
        return NPT_SUCCESS;
    } else if (feof(m_FileReference->m_File)) {
        if (bytes_read) *bytes_read = 0;
        return NPT_ERROR_EOS;
    } else {
        if (bytes_read) *bytes_read = 0;
        return MapErrno(errno);
    }
}

/*----------------------------------------------------------------------
|   NPT_StdcFileInputStream::GetSize
+---------------------------------------------------------------------*/
NPT_Result
NPT_StdcFileInputStream::GetSize(NPT_LargeSize& size)
{
    NPT_FileInfo file_info;
    NPT_Result result = NPT_File::GetInfo(m_FileReference->m_Name, &file_info);
    if (NPT_FAILED(result)) return result;
    size = file_info.m_Size;
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_StdcFileInputStream::GetAvailable
+---------------------------------------------------------------------*/
NPT_Result
NPT_StdcFileInputStream::GetAvailable(NPT_LargeSize& available)
{
    NPT_Int64     offset = NPT_ftell(m_FileReference->m_File);
    NPT_LargeSize size = 0;
    
    if (NPT_SUCCEEDED(GetSize(size)) && offset >= 0 && (NPT_LargeSize)offset <= size) {
        available = size - offset;
        return NPT_SUCCESS;
    } else {
        available = 0;
        return NPT_FAILURE;
    }
}

/*----------------------------------------------------------------------
|   NPT_StdcFileOutputStream
+---------------------------------------------------------------------*/
class NPT_StdcFileOutputStream : public NPT_OutputStream,
                                 private NPT_StdcFileStream
{
public:
    // constructors and destructor
    NPT_StdcFileOutputStream(NPT_StdcFileReference& file) :
        NPT_StdcFileStream(file) {}

    // NPT_InputStream methods
    NPT_Result Write(const void* buffer, 
                     NPT_Size    bytes_to_write, 
                     NPT_Size*   bytes_written);
    NPT_Result Seek(NPT_Position offset) {
        return NPT_StdcFileStream::Seek(offset);
    }
    NPT_Result Tell(NPT_Position& offset) {
        return NPT_StdcFileStream::Tell(offset);
    }
    NPT_Result Flush() {
        return NPT_StdcFileStream::Flush();
    }
};

/*----------------------------------------------------------------------
|   NPT_StdcFileOutputStream::Write
+---------------------------------------------------------------------*/
NPT_Result
NPT_StdcFileOutputStream::Write(const void* buffer, 
                                NPT_Size    bytes_to_write, 
                                NPT_Size*   bytes_written)
{
    size_t nb_written;

    nb_written = fwrite(buffer, 1, bytes_to_write, m_FileReference->m_File);

    if (nb_written > 0) {
        if (bytes_written) *bytes_written = (NPT_Size)nb_written;
        return NPT_SUCCESS;
    } else {
        if (bytes_written) *bytes_written = 0;
        return NPT_ERROR_WRITE_FAILED;
    }
}

/*----------------------------------------------------------------------
|   NPT_StdcFile
+---------------------------------------------------------------------*/
class NPT_StdcFile: public NPT_FileInterface
{
public:
    // constructors and destructor
    NPT_StdcFile(NPT_File& delegator);
   ~NPT_StdcFile();

    // NPT_FileInterface methods
    NPT_Result Open(OpenMode mode);
    NPT_Result Close();
    NPT_Result GetInputStream(NPT_InputStreamReference& stream);
    NPT_Result GetOutputStream(NPT_OutputStreamReference& stream);

private:
    // members
    NPT_File&             m_Delegator;
    OpenMode              m_Mode;
    NPT_StdcFileReference m_FileReference;
};

/*----------------------------------------------------------------------
|   NPT_StdcFile::NPT_StdcFile
+---------------------------------------------------------------------*/
NPT_StdcFile::NPT_StdcFile(NPT_File& delegator) :
    m_Delegator(delegator),
    m_Mode(0)
{
}

/*----------------------------------------------------------------------
|   NPT_StdcFile::~NPT_StdcFile
+---------------------------------------------------------------------*/
NPT_StdcFile::~NPT_StdcFile()
{
    Close();
}

/*----------------------------------------------------------------------
|   NPT_StdcFile::Open
+---------------------------------------------------------------------*/
NPT_Result
NPT_StdcFile::Open(NPT_File::OpenMode mode)
{
    FILE* file = NULL;
    
    // check if we're already open
    if (!m_FileReference.IsNull()) {
        return NPT_ERROR_FILE_ALREADY_OPEN;
    }

    // store the mode
    m_Mode = mode;

    // check for special names
    const char* name = (const char*)m_Delegator.GetPath();
    if (NPT_StringsEqual(name, NPT_FILE_STANDARD_INPUT)) {
        file = stdin;
    } else if (NPT_StringsEqual(name, NPT_FILE_STANDARD_OUTPUT)) {
        file = stdout;
    } else if (NPT_StringsEqual(name, NPT_FILE_STANDARD_ERROR)) {
        file = stderr;
    } else {
        // compute mode
        const char* fmode = "";
        if (mode & NPT_FILE_OPEN_MODE_WRITE) {
            if (mode & NPT_FILE_OPEN_MODE_APPEND) {
                /* write, read, create, append */
                /* (append implies create)     */
                fmode = "a+b";
            } else {
                if ((mode & NPT_FILE_OPEN_MODE_CREATE) || (mode & NPT_FILE_OPEN_MODE_TRUNCATE)) {
                    /* write, read, create, truncate                      */
                    /* (truncate implies create, create implies truncate) */
                    fmode = "w+b";
                } else {
                    /* write, read */
                    fmode = "r+b";
                }
            }
        } else {
            /* read only */
            fmode = "rb";
        }

        // open the file
#if defined(NPT_CONFIG_HAVE_FSOPEN)
        file = _fsopen(name, fmode, _SH_DENYNO);
        int open_result = file == NULL ? ENOENT : 0; 
#else
        int open_result = fopen_s(&file, name, fmode);
#endif

        // test the result of the open
        if (open_result != 0) return MapErrno(open_result);
    }

    // unbuffer the file if needed 
    if ((mode & NPT_FILE_OPEN_MODE_UNBUFFERED) && file != NULL) {
#if !defined(_WIN32_WCE)
        setvbuf(file, NULL, _IONBF, 0);
#endif
    }   
    
    // create a reference to the FILE object
    m_FileReference = new NPT_StdcFileWrapper(file, name);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_StdcFile::Close
+---------------------------------------------------------------------*/
NPT_Result
NPT_StdcFile::Close()
{
    // release the file reference
    m_FileReference = NULL;

    // reset the mode
    m_Mode = 0;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_StdcFile::GetInputStream
+---------------------------------------------------------------------*/
NPT_Result 
NPT_StdcFile::GetInputStream(NPT_InputStreamReference& stream)
{
    // default value
    stream = NULL;

    // check that the file is open
    if (m_FileReference.IsNull()) return NPT_ERROR_FILE_NOT_OPEN;

    // check that the mode is compatible
    if (!(m_Mode & NPT_FILE_OPEN_MODE_READ)) {
        return NPT_ERROR_FILE_NOT_READABLE;
    }

    // create a stream
    stream = new NPT_StdcFileInputStream(m_FileReference);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_StdcFile::GetOutputStream
+---------------------------------------------------------------------*/
NPT_Result 
NPT_StdcFile::GetOutputStream(NPT_OutputStreamReference& stream)
{
    // default value
    stream = NULL;

    // check that the file is open
    if (m_FileReference.IsNull()) return NPT_ERROR_FILE_NOT_OPEN;

    // check that the mode is compatible
    if (!(m_Mode & NPT_FILE_OPEN_MODE_WRITE)) {
        return NPT_ERROR_FILE_NOT_WRITABLE;
    }
    
    // create a stream
    stream = new NPT_StdcFileOutputStream(m_FileReference);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_File::NPT_File
+---------------------------------------------------------------------*/
NPT_File::NPT_File(const char* path) : m_Path(path), m_IsSpecial(false)
{
    m_Delegate = new NPT_StdcFile(*this);
    
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
        m_Delegate  = new NPT_StdcFile(*this);
    }
    return *this;
}

