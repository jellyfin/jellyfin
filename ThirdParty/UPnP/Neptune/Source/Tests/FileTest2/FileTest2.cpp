/*****************************************************************
|
|      File Test Program 2
|
|      (c) 2005-2008 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include "Neptune.h"
#include "NptDebug.h"

/*----------------------------------------------------------------------
|   FileTypeName
+---------------------------------------------------------------------*/
static const char*
FileTypeName(NPT_FileInfo::FileType type) 
{
    switch (type) {
        case NPT_FileInfo::FILE_TYPE_NONE:      return "NONE";
        case NPT_FileInfo::FILE_TYPE_REGULAR:   return "REGULAR";
        case NPT_FileInfo::FILE_TYPE_DIRECTORY: return "DIRECTORY";
        case NPT_FileInfo::FILE_TYPE_SPECIAL:   return "SPECIAL";
        case NPT_FileInfo::FILE_TYPE_OTHER:     return "OTHER";
        default:                                return "??";
    }
}

/*----------------------------------------------------------------------
|   WalkDir
+---------------------------------------------------------------------*/
static NPT_Result
WalkDir(const char* path, unsigned int indent)
{
    NPT_FileInfo info;
    NPT_Result result = NPT_File::GetInfo(path, &info);
    if (NPT_FAILED(result)) {
        fprintf(stderr, "ERROR: NPT_File::GetInfo(\"%s\") returned %d (%s)\n",
                path, result, NPT_ResultText(result));
        return result;
    }
    if (info.m_Type != NPT_FileInfo::FILE_TYPE_DIRECTORY) {
        fprintf(stderr, "WARNING: %s is not a directory\n", path);
        return NPT_SUCCESS; // not fatal
    }

    NPT_List<NPT_String> entries;
    result = NPT_File::ListDir(path, entries);
    if (NPT_FAILED(result)) {
        fprintf(stderr, "WARNING: NPT_File::ListDirectory returned %d (%s)\n",
                result, NPT_ResultText(result));
        return NPT_SUCCESS; // not fatal
    }

    for (NPT_List<NPT_String>::Iterator entries_iterator = entries.GetFirstItem();
         entries_iterator;
         ++entries_iterator) {
         if (*entries_iterator == "." || *entries_iterator == "..") continue;

         NPT_String child = path;
         child += NPT_FilePath::Separator;
         child += *entries_iterator;
         result = NPT_File::GetInfo(child, &info);
         if (NPT_FAILED(result)) {
             fprintf(stderr, "WARNING: NPT_File::GetInfo(%s) returned %d (%s)\n",
                     child.GetChars(), result, NPT_ResultText(result));
             continue;
         }
         for (unsigned int i=0; i<indent; i++) {
             printf("  ");
         }
         printf("%s: type=%s", child.GetChars(), FileTypeName(info.m_Type));
         if (info.m_Type != NPT_FileInfo::FILE_TYPE_DIRECTORY) printf(" size=%lld", info.m_Size);
         if (info.m_AttributesMask & NPT_FILE_ATTRIBUTE_READ_ONLY &&
             info.m_Attributes     & NPT_FILE_ATTRIBUTE_READ_ONLY) {
            printf(" RO");
         }
         if (info.m_AttributesMask & NPT_FILE_ATTRIBUTE_LINK &&
             info.m_Attributes     & NPT_FILE_ATTRIBUTE_LINK) {
                 printf(" LINK");
         }
         printf("\n");
         NPT_String cre_time = NPT_DateTime(info.m_CreationTime, true).ToString(NPT_DateTime::FORMAT_RFC_1123);
         for (unsigned int i=0; i<indent; i++) { printf("  "); }
         printf("created: %s\n", cre_time.GetChars());
         NPT_String mod_time = NPT_DateTime(info.m_ModificationTime, true).ToString(NPT_DateTime::FORMAT_RFC_1123);
         for (unsigned int i=0; i<indent; i++) { printf("  "); }
         printf("modified: %s\n", mod_time.GetChars());
         
         if (info.m_Type == NPT_FileInfo::FILE_TYPE_DIRECTORY) {
             result = WalkDir(child, indent+1);
             if (NPT_FAILED(result)) return result;
         } 
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   main
+---------------------------------------------------------------------*/
int
main(int argc, char** argv)
{
    if (argc != 2) {
        fprintf(stderr, 
                "ERROR: root directory name not specified\n"
                "usage: filetest2 <root_path>\n");
        return 1;
    }

    WalkDir(argv[1], 0);

    return 0;
}
