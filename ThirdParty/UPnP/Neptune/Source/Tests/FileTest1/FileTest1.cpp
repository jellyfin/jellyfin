/*****************************************************************
|
|      File Test Program 1
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

#define CHECK(x) \
do {\
  if (!(x)) {\
    fprintf(stderr, "ERROR line %d\n", __LINE__);\
    return NPT_FAILURE;\
  }\
} while(0)\

/*----------------------------------------------------------------------
|    CreateNewFile
+---------------------------------------------------------------------*/
NPT_Result
CreateNewFile(const char* filename, NPT_Size chunk_count, NPT_Size chunk_size=1)
{
    NPT_File file(filename);
    NPT_CHECK(file.Open(NPT_FILE_OPEN_MODE_CREATE|NPT_FILE_OPEN_MODE_WRITE|NPT_FILE_OPEN_MODE_TRUNCATE));
    NPT_OutputStreamReference out;
    file.GetOutputStream(out);
    unsigned char* chunk_buffer = new unsigned char[chunk_size];
    for (unsigned int i=0; i<chunk_size; i++) {
        chunk_buffer[i] = (unsigned char)i;
    }
    for (unsigned int i=0; i<chunk_count; i++) {
        CHECK(NPT_SUCCEEDED(out->WriteFully(chunk_buffer, chunk_size))); 
    }
    delete[] chunk_buffer;
    file.Close();
    out = NULL;
    
    NPT_FileInfo info;
    NPT_Result result = NPT_File::GetInfo(filename, &info);
    CHECK(NPT_SUCCEEDED(result));
    CHECK(info.m_Size == (NPT_LargeSize)chunk_count*(NPT_LargeSize)chunk_size);
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int argc, char** argv)
{
    NPT_Result result;
    NPT_FileInfo info;
    
    CHECK(NPT_File::GetInfo("foobar.doesnotexist", NULL) == NPT_ERROR_NO_SUCH_FILE);
    CHECK(!NPT_File::Exists("foobar.doesnotexist"));
    
    // test special names
    NPT_File file(NPT_FILE_STANDARD_INPUT);
    CHECK(NPT_SUCCEEDED(file.GetInfo(info)));
    CHECK(info.m_Type == NPT_FileInfo::FILE_TYPE_SPECIAL);
    file = NPT_File(NPT_FILE_STANDARD_OUTPUT);
    
    CHECK(NPT_SUCCEEDED(file.GetInfo(info)));
    CHECK(info.m_Type == NPT_FileInfo::FILE_TYPE_SPECIAL);
    file = NPT_File(NPT_FILE_STANDARD_ERROR);
    CHECK(NPT_SUCCEEDED(file.GetInfo(info)));
    CHECK(info.m_Type == NPT_FileInfo::FILE_TYPE_SPECIAL);

    if (NPT_File::Exists("foobar.file1")) {
        result = NPT_File::RemoveFile("foobar.file1");
        CHECK(NPT_SUCCEEDED(result));
    }
    
    result = CreateNewFile("foobar.file1", 9);
    CHECK(NPT_SUCCEEDED(result));
    CHECK(NPT_File::Exists("foobar.file1"));
    result = NPT_File::GetInfo("foobar.file1", &info);
    CHECK(NPT_SUCCEEDED(result));
    CHECK(info.m_Type == NPT_FileInfo::FILE_TYPE_REGULAR);
    CHECK(info.m_Size == 9);
    
    {
        NPT_File f1("foobar.file1");
        result = f1.GetInfo(info);
        CHECK(NPT_SUCCEEDED(result));
        CHECK(info.m_Type == NPT_FileInfo::FILE_TYPE_REGULAR);
        CHECK(info.m_Size == 9);
    }
    {
        NPT_File f1("foobar.file1");
        NPT_LargeSize size;
        result = f1.GetSize(size);
        CHECK(NPT_SUCCEEDED(result));
        CHECK(size == 9);
    }
    
    {
        NPT_File f1("foobar.file1");
        NPT_File::Remove("foobar.file1-r");
        result = f1.Rename("foobar.file1-r");
        CHECK(NPT_SUCCEEDED(result));
        CHECK(f1.GetPath() == "foobar.file1-r");
    }
    CHECK(NPT_File::Exists("foobar.file1-r"));
    result = NPT_File::GetInfo("foobar.file1-r", &info);
    CHECK(NPT_SUCCEEDED(result));
    CHECK(info.m_Type == NPT_FileInfo::FILE_TYPE_REGULAR);
    CHECK(info.m_Size == 9);

    // dirs
    NPT_File::RemoveDir("foobar.dir", true);
    CHECK(!NPT_File::Exists("foobar.dir"));
    result = NPT_File::CreateDir("foobar.dir");
    CHECK(NPT_SUCCEEDED(result));
    result = NPT_File::GetInfo("foobar.dir", &info);
    CHECK(NPT_SUCCEEDED(result));
    CHECK(info.m_Type == NPT_FileInfo::FILE_TYPE_DIRECTORY);
    {
        NPT_File f1("foobar.dir");
        result = f1.GetInfo(info);
        CHECK(NPT_SUCCEEDED(result));
        CHECK(info.m_Type == NPT_FileInfo::FILE_TYPE_DIRECTORY);
    }

    NPT_String dirname = "foobar.dir";
    NPT_String fname;
    fname = dirname;
    fname += NPT_FilePath::Separator;
    fname += "file1";
    result = CreateNewFile(fname, 1);
    CHECK(NPT_SUCCEEDED(result));
    fname = dirname;
    fname += NPT_FilePath::Separator;
    fname += "file2";
    result = CreateNewFile(fname, 2);
    CHECK(NPT_SUCCEEDED(result));
    fname = dirname;
    fname += NPT_FilePath::Separator;
    fname += "file3";
    result = CreateNewFile(fname, 3);
    CHECK(NPT_SUCCEEDED(result));

    NPT_List<NPT_String> entries;
    result = NPT_File::ListDir("foobar.dir", entries);
    CHECK(NPT_SUCCEEDED(result));
    CHECK(entries.GetItemCount() == 3);
    NPT_LargeSize dir_size;
    result = NPT_File::GetSize("foobar.dir", dir_size);
    CHECK(NPT_SUCCEEDED(result));
    CHECK(dir_size == 3);
    
    result = NPT_File::RemoveFile("foobar.dir");
    CHECK(NPT_FAILED(result));
    result = NPT_File::RemoveDir("foobar.dir");
    CHECK(result == NPT_ERROR_DIRECTORY_NOT_EMPTY);
    
    result = NPT_File::Rename("foobar.dir", "foobar.dir-r");
    CHECK(NPT_SUCCEEDED(result));
    
    dirname = "foobar.dir-r";
    fname = dirname;
    fname += NPT_FilePath::Separator;
    fname += "file1";
    result = NPT_File::RemoveFile(fname);
    CHECK(NPT_SUCCEEDED(result));
    fname = dirname;
    fname += NPT_FilePath::Separator;
    fname += "file2";
    result = NPT_File::RemoveFile(fname);
    CHECK(NPT_SUCCEEDED(result));
    fname = dirname;
    fname += NPT_FilePath::Separator;
    fname += "file3";
    result = NPT_File::RemoveFile(fname);
    CHECK(NPT_SUCCEEDED(result));

    result = NPT_File::RemoveDir("foobar.dir-r");
    CHECK(NPT_SUCCEEDED(result));
    CHECK(!NPT_File::Exists("foobar.dir-r"));

    // dir creation with intermediates
    result = NPT_File::CreateDir("foobar.dir/a/b/c/d", false);
    CHECK(NPT_FAILED(result));
    result = NPT_File::CreateDir("foobar.dir/a/b/c/d", true);
    CHECK(NPT_SUCCEEDED(result));
    CreateNewFile("foobar.dir/a/a.bin", 1, 1);
    CreateNewFile("foobar.dir/a/b.bin", 1, 1);
    CreateNewFile("foobar.dir/a/b/a.bin", 1, 1);
    CreateNewFile("foobar.dir/a/b/c/d/foo.bin", 1, 1);
    result = NPT_File::RemoveDir("foobar.dir", false);
    CHECK(NPT_FAILED(result));
    result = NPT_File::RemoveDir("foobar.dir", true);
    CHECK(NPT_SUCCEEDED(result));
    
    // paths
    NPT_String test;
    test = NPT_FilePath::BaseName("");
    CHECK(test == "");
    test = NPT_FilePath::BaseName("a");
    CHECK(test == "a");
    test = NPT_FilePath::BaseName("a"+NPT_String(NPT_FilePath::Separator)+"b");
    CHECK(test == "b");
    test = NPT_FilePath::BaseName("a"+NPT_String(NPT_FilePath::Separator)+"b"+NPT_FilePath::Separator);
    CHECK(test == "");
    test = NPT_FilePath::BaseName(NPT_String(NPT_FilePath::Separator)+"a");
    CHECK(test == "a");
    test = NPT_FilePath::BaseName(NPT_FilePath::Separator);
    CHECK(test == "");

    test = NPT_FilePath::DirName("");
    CHECK(test == "");
    test = NPT_FilePath::DirName("a");
    CHECK(test == "");
    test = NPT_FilePath::DirName("a"+NPT_String(NPT_FilePath::Separator)+"b");
    CHECK(test == "a");
    test = NPT_FilePath::DirName("a"+NPT_String(NPT_FilePath::Separator)+"b"+NPT_FilePath::Separator);
    CHECK(test == "a"+NPT_String(NPT_FilePath::Separator)+"b");
    test = NPT_FilePath::DirName(NPT_String(NPT_FilePath::Separator)+"a");
    CHECK(test == NPT_FilePath::Separator);
    test = NPT_FilePath::DirName(NPT_FilePath::Separator);
    CHECK(test == NPT_FilePath::Separator);
    
    // small files
    result = CreateNewFile("small.bin", 0x100, 0x107);
    CHECK(NPT_SUCCEEDED(result));

    file = NPT_File("small.bin");
    result = file.Open(NPT_FILE_OPEN_MODE_READ);
    CHECK(NPT_SUCCEEDED(result));
    NPT_InputStreamReference input;
    file.GetInputStream(input);
    NPT_Position position;
    result = input->Tell(position);
    CHECK(NPT_SUCCEEDED(result));
    CHECK(position == 0);
    NPT_LargeSize large_size = (NPT_LargeSize)0x107 * (NPT_LargeSize)0x100;
    result = input->Seek(large_size-0x107);
    CHECK(NPT_SUCCEEDED(result));
    result = input->Tell(position);
    CHECK(NPT_SUCCEEDED(result));
    CHECK(position == large_size-0x107);        
    unsigned char* buffer = new unsigned char[0x107];
    result = input->ReadFully(buffer, 0x107);
    CHECK(NPT_SUCCEEDED(result));
    result = input->Tell(position);
    CHECK(NPT_SUCCEEDED(result));
    CHECK(position == large_size);
    for (unsigned int i=0; i<0x107; i++) {
        CHECK(buffer[i] == (unsigned char)i);
    }        
    delete[] buffer;
    file.Close();
    NPT_File::RemoveFile(file.GetPath());

    // large files
    if (argc == 2) {
        result = CreateNewFile(argv[1], 0x10000, 0x10007);
        CHECK(NPT_SUCCEEDED(result));

        NPT_String new_name = argv[1];
        new_name += ".renamed";
        result = NPT_File::Rename(argv[1], new_name);
        CHECK(NPT_SUCCEEDED(result));
        file = NPT_File(new_name);
        result = file.Open(NPT_FILE_OPEN_MODE_READ);
        CHECK(NPT_SUCCEEDED(result));
        file.GetInputStream(input);
        result = input->Tell(position);
        CHECK(NPT_SUCCEEDED(result));
        CHECK(position == 0);
        large_size = (NPT_LargeSize)0x10007 * (NPT_LargeSize)0x10000;
        result = input->Seek(large_size-0x10007);
        CHECK(NPT_SUCCEEDED(result));
        result = input->Tell(position);
        CHECK(NPT_SUCCEEDED(result));
        CHECK(position == large_size-0x10007);        
        buffer = new unsigned char[0x10007];
        result = input->ReadFully(buffer, 0x10007);
        CHECK(NPT_SUCCEEDED(result));
        result = input->Tell(position);
        CHECK(NPT_SUCCEEDED(result));
        CHECK(position == large_size);
        for (unsigned int i=0; i<0x10007; i++) {
            CHECK(buffer[i] == (unsigned char)i);
        }        
        delete[] buffer;
        file.Close();
        NPT_File::RemoveFile(new_name);
    }
    
    // test dynamic size
    unsigned char             buff[16] = {0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f};  
    const char*               filename = "pi.\xCF\x80.test";
    NPT_TimeInterval          wait(2.0f);
    
    if (argc > 1) {
        filename = argv[1];
    }
    
    NPT_File                  file1(filename);
    NPT_OutputStreamReference output;
    NPT_LargeSize             size;

    CHECK(NPT_SUCCEEDED(file1.Open(NPT_FILE_OPEN_MODE_CREATE | NPT_FILE_OPEN_MODE_WRITE | NPT_FILE_OPEN_MODE_READ | NPT_FILE_OPEN_MODE_TRUNCATE)));
    CHECK(NPT_SUCCEEDED(file1.GetSize(size)));
    CHECK(size == 0);
    CHECK(NPT_SUCCEEDED(file1.GetOutputStream(output)));
    CHECK(NPT_SUCCEEDED(file1.GetInputStream(input)));
    CHECK(NPT_SUCCEEDED(output->Tell(position)));
    CHECK(position == 0);
    CHECK(NPT_SUCCEEDED(input->Tell(position)));
    CHECK(position == 0);
    CHECK(NPT_SUCCEEDED(output->WriteFully(buff, 16)));
    output->Flush();
    NPT_System::Sleep(wait);
    CHECK(NPT_SUCCEEDED(file1.GetSize(size)));
    CHECK(size == 16);
    CHECK(NPT_SUCCEEDED(output->Tell(position)));
    CHECK(NPT_SUCCEEDED(input->GetSize(size)));
    CHECK(size == 16);
    CHECK(position == 16);
    CHECK(NPT_SUCCEEDED(input->Tell(position)));
    CHECK(position == 16);
    CHECK(NPT_SUCCEEDED(output->Seek(8)));
    CHECK(NPT_SUCCEEDED(output->Tell(position)));
    CHECK(position == 8);
    
    NPT_File                 file2(filename);
    NPT_InputStreamReference input2;

    CHECK(NPT_SUCCEEDED(file2.Open(NPT_FILE_OPEN_MODE_READ)));
    CHECK(NPT_SUCCEEDED(file2.GetSize(size)));
    CHECK(size == 16);
    CHECK(NPT_SUCCEEDED(file2.GetInputStream(input2)));
    CHECK(NPT_SUCCEEDED(input2->GetSize(size)));
    CHECK(size == 16);
    CHECK(NPT_SUCCEEDED(input2->Tell(position)));
    CHECK(position == 0);
    CHECK(NPT_SUCCEEDED(input2->Seek(8)));
    CHECK(NPT_SUCCEEDED(input2->Tell(position)));
    CHECK(position == 8);
    
    CHECK(NPT_SUCCEEDED(output->WriteFully(buff, 16)));
    output->Flush();
    NPT_System::Sleep(wait);
    CHECK(NPT_SUCCEEDED(file2.GetSize(size)));
    CHECK(size == 24);
    CHECK(NPT_SUCCEEDED(output->Tell(position)));
    CHECK(position == 24);
    CHECK(NPT_SUCCEEDED(input->Tell(position)));
    CHECK(position == 24);
    
    CHECK(NPT_SUCCEEDED(input2->GetSize(size)));
    CHECK(size == 24);
    CHECK(NPT_SUCCEEDED(input2->Seek(20)));
    CHECK(NPT_SUCCEEDED(input2->Read(buff, 4, NULL)));
    
    return 0;
}
