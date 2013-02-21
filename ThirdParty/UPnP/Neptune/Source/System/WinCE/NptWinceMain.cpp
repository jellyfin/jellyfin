/*****************************************************************
|
|   Neptune - Utils : WinCE Implementation
|
|   (c) 2002-2006 Gilles Boccon-Gibod
|   Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include <windows.h>

/*----------------------------------------------------------------------
|   _tmain
+---------------------------------------------------------------------*/
extern int main(int argc, char** argv);

int
_tmain(int argc, wchar_t** argv, wchar_t** envp)
{
    char** argv_utf8 = new char*[1+argc];
    int i;
    int result;

    // allocate and convert args
    for (i=0; i<argc; i++) {
        unsigned int arg_length = wcslen(argv[i]);
        argv_utf8[i] = new char[4*arg_length+1];
        WideCharToMultiByte(CP_UTF8, 0, argv[i], -1, argv_utf8[i], 4*arg_length+1, 0, 0);
    }

    // terminate the array
    argv_utf8[argc] = NULL;

    // call the real main
    result = main(argc, argv_utf8);

    // cleanup
    for (i=0; i<argc; i++) {
        delete [] argv_utf8[i];
    }
    delete[] argv_utf8;

    return result;
}
