/*****************************************************************
|
|      RingBuffer Test Program 1
|
|      (c) 2001-2005 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include <stdlib.h>

#if defined(WIN32)
#include <crtdbg.h>
#endif

const unsigned int BUFFER_SIZE = 17;

/*----------------------------------------------------------------------
|       ReadChunk
+---------------------------------------------------------------------*/
static NPT_Result
ReadChunk(NPT_RingBuffer& buffer)
{
    static unsigned int total_read = 0;
    unsigned int chunk = rand()%BUFFER_SIZE;
    unsigned int can_read = buffer.GetAvailable();
    if (chunk > can_read) chunk = can_read;
    if (chunk == 0) return NPT_SUCCESS;

    // read a chunk
    unsigned char bytes[BUFFER_SIZE];
    NPT_CHECK(buffer.Read(bytes, chunk));

    // check values
    for (unsigned int i=0; i<chunk; i++) {
        unsigned int index = total_read+i;
        unsigned char expected = index & 0xFF;
        if (bytes[i] != expected) {
            printf("unexpected byte at index %d (expected %d, got %d)\n", 
                   index, expected, bytes[i]);
            return NPT_FAILURE;
        }
    }
    total_read += chunk;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|       WriteChunk
+---------------------------------------------------------------------*/
static NPT_Result
WriteChunk(NPT_RingBuffer& buffer)
{
    static unsigned int total_written = 0;
    unsigned int chunk = rand()%BUFFER_SIZE;
    unsigned int can_write = buffer.GetSpace();
    if (chunk > can_write) chunk = can_write;
    if (chunk == 0) return NPT_SUCCESS;

    // generate buffer
    unsigned char bytes[BUFFER_SIZE];
    for (unsigned int i=0; i<chunk; i++) {
        unsigned int index = total_written+i;
        bytes[i] = index&0xFF;
    }

    // write chunk
    NPT_CHECK(buffer.Write(bytes, chunk));
    total_written += chunk;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int /*argc*/, char** /*argv*/)
{
    // setup debugging
#if defined(WIN32) && defined(_DEBUG)
    int flags = _crtDbgFlag       | 
        _CRTDBG_ALLOC_MEM_DF      |
        _CRTDBG_DELAY_FREE_MEM_DF |
        _CRTDBG_CHECK_ALWAYS_DF;

    _CrtSetDbgFlag(flags);
    //AllocConsole();
    //freopen("CONOUT$", "w", stdout);
#endif 

    NPT_RingBuffer buffer(BUFFER_SIZE);
    
    for (int i=0; i<100000000; i++) {
        if (NPT_FAILED(WriteChunk(buffer))) {
            printf("WriteChunk failed\n");
            return 1;
        }
        if (NPT_FAILED(ReadChunk(buffer))) {
            printf("ReadChunk failed\n");
            return 1;
        }
    }

    printf("RingBufferTest1 passed\n");

    return 0;
}
