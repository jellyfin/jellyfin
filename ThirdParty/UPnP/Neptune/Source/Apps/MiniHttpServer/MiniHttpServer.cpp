/*****************************************************************
|
|      Mini HTTP Server
|
|      (c) 2001-2009 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include "Neptune.h"


/*----------------------------------------------------------------------
|   MiniServer
+---------------------------------------------------------------------*/
static void
MiniServer(const char* root, unsigned int port, bool verbose)
{
    NPT_HttpServer            server(port);
    NPT_InputStreamReference  input;
    NPT_OutputStreamReference output;
    NPT_HttpRequestContext    context;

    NPT_HttpFileRequestHandler* file_handler = new NPT_HttpFileRequestHandler("/", root, true);
    server.AddRequestHandler(file_handler, "/", true);

    for (;;) {
        if (verbose) NPT_Console::Output("waiting for connection...\n");
        NPT_Result result = server.WaitForNewClient(input, 
                                                    output,
                                                    &context);
        if (verbose) NPT_Console::OutputF("WaitForNewClient returned %d (%s)\n", result, NPT_ResultText(result));
        if (NPT_FAILED(result)) return;

        result = server.RespondToClient(input, output, context);
        if (verbose) NPT_Console::OutputF("RespondToClient returned %d (%s)\n", result, NPT_ResultText(result));
        
        input = NULL;
        output = NULL;
    } 

    delete file_handler;
}

/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int /*argc*/, char** argv)
{
    NPT_String   file_root;
    unsigned int port    = 8000;
    bool         verbose = false;
    
    while (const char* arg = *++argv) {
        if (NPT_StringsEqual(arg, "--help") ||
            NPT_StringsEqual(arg, "-h")) {
            NPT_Console::Output("usage: minihttpserver [--file-root <dir>] [--port <port>] [--verbose]\n");
            return 0;
        } else if (NPT_StringsEqual(arg, "--file-root")) {
            arg = *++argv;
            if (arg == NULL) {
                NPT_Console::Output("ERROR: missing argument for --root option\n");
                return 1;
            }
            file_root = arg;
        } else if (NPT_StringsEqual(arg, "--port")) {
            arg = *++argv;
            if (arg == NULL) {
                NPT_Console::Output("ERROR: missing argument for --port option\n");
                return 1;
            }
            NPT_ParseInteger(arg, port, true);
        } else if (NPT_StringsEqual(arg, "--verbose")) {
            verbose = true;
        }
    }
    
    if (file_root.GetLength() == 0) {
        NPT_File::GetWorkingDir(file_root);
    }
    
    if (verbose) {
        NPT_Console::OutputF("Starting server on port %d, root=%s\n", port, file_root.GetChars());
    }
    MiniServer(file_root, port, verbose);
    
    return 0;
}
