/*****************************************************************
|
|      Crypto Test Program 1
|
|      (c) 2005-2010 Axiomatic Systems, LLC.
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include <stdlib.h>
#include <stdio.h>
#include "Neptune.h"

/*----------------------------------------------------------------------
|       macros
+---------------------------------------------------------------------*/
#define SHOULD_SUCCEED(r)                                   \
    do {                                                    \
        if (NPT_FAILED(r)) {                                \
            NPT_Console::OutputF("failed line %d (%d)\n", __LINE__, r);\
            return 1;                                       \
        }                                                   \
    } while(0)                                         

#define SHOULD_EQUAL(a,b)                                   \
    do {                                                    \
        if ((a) != (b)) {                                   \
            NPT_Console::OutputF("failed line %d (%d != %d)\n", __LINE__, a,b);\
            return 1;                                       \
        }                                                   \
    } while(0)                                         

#define SHOULD_EQUAL_MEM(a,b,s)                                         \
    do {                                                                \
        for (unsigned int x=0; x<s; x++) {                              \
            if (a[x] != b[x]) {                                         \
                NPT_Console::OutputF("failed line %d (byte %d)\n", __LINE__, x);   \
                return 1;                                               \
            }                                                           \
        }                                                               \
    } while(0)                                         

/*----------------------------------------------------------------------
|   TestBlockCiphers
+---------------------------------------------------------------------*/
static int
TestBlockCiphers()
{
    NPT_BlockCipher* cipher = NULL;
    NPT_UInt8        b0[16];
    NPT_UInt8        k1[16] =  
        { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 
          0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f };
    NPT_UInt8        pt1[16] =       
        { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
          0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff };
    NPT_UInt8        ct1[16] =    
        { 0x69, 0xc4, 0xe0, 0xd8, 0x6a, 0x7b, 0x04, 0x30, 
          0xd8, 0xcd, 0xb7, 0x80, 0x70, 0xb4, 0xc5, 0x5a };
    
    NPT_Result result;
    result = NPT_BlockCipher::Create(NPT_BlockCipher::AES_128, 
                                     NPT_BlockCipher::ENCRYPT,
                                     k1,
                                     16,
                                     cipher);
    SHOULD_SUCCEED(result);
    SHOULD_EQUAL(cipher->GetBlockSize(), 16);
    result = cipher->ProcessBlock(pt1, b0);
    SHOULD_SUCCEED(result);
    SHOULD_EQUAL_MEM(b0, ct1, 16);
    delete cipher;
    
    result = NPT_BlockCipher::Create(NPT_BlockCipher::AES_128, 
                                     NPT_BlockCipher::DECRYPT,
                                     k1,
                                     16,
                                     cipher);
    SHOULD_SUCCEED(result);
    SHOULD_EQUAL(cipher->GetBlockSize(), 16);
    result = cipher->ProcessBlock(ct1, b0);
    SHOULD_SUCCEED(result);
    SHOULD_EQUAL_MEM(b0, pt1, 16);
    delete cipher;

    NPT_UInt8 key[16];
    NPT_CopyMemory(key, k1, 16);
    for (unsigned int i=0; i<100; i++) {
        NPT_BlockCipher* encrypter;
        NPT_BlockCipher* decrypter;
        result = NPT_BlockCipher::Create(NPT_BlockCipher::AES_128, 
                                         NPT_BlockCipher::ENCRYPT,
                                         key,
                                         16,
                                         encrypter);
        result = NPT_BlockCipher::Create(NPT_BlockCipher::AES_128, 
                                         NPT_BlockCipher::DECRYPT,
                                         key,
                                         16,
                                         decrypter);
        NPT_UInt8 mem1[16];
        NPT_UInt8 mem2[16];
        NPT_UInt8 mem3[16];
        NPT_SetMemory(mem1, 0, 16);
        for (unsigned int j=0; j<1000; j++) {
            encrypter->ProcessBlock(mem1, mem2);
            decrypter->ProcessBlock(mem2, mem3);
            SHOULD_EQUAL_MEM(mem1, mem3, 16);
            NPT_CopyMemory(mem1, mem2, 16);
        }
        delete encrypter;
        delete decrypter;
        NPT_CopyMemory(key, mem1, 16);
    }
    
    return 0;
}

/*----------------------------------------------------------------------
 |   TestBenchmark
 +---------------------------------------------------------------------*/
static int
TestBenchmark()
{
    unsigned char* data = new unsigned char[16*1024];
    NPT_SetMemory(data, 0, sizeof(16*1024));
    NPT_TimeStamp before;
    NPT_TimeStamp after;
    float         elapsed;
    unsigned char key[16] = { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 
                              0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f };
    NPT_BlockCipher* cipher;
    NPT_BlockCipher::Create(NPT_BlockCipher::AES_128, 
                            NPT_BlockCipher::ENCRYPT,
                            key,
                            16,
                            cipher);
    NPT_System::GetCurrentTimeStamp(before);
    unsigned int block_count = 0;
    do {
        unsigned char out[16];
        for (unsigned int i=0; i<1024; i++) {
            cipher->ProcessBlock(data+16*i, out);
        }
        block_count += 1024;
        NPT_System::GetCurrentTimeStamp(after);
        elapsed  = after.ToSeconds()-before.ToSeconds();
    } while (elapsed < 10.0f);
    NPT_Console::OutputF("AES: %d blocks in 10 seconds: %f MB/s\n", block_count, ((block_count*16.0f)/1000000.0f)/elapsed);
    delete[] data;
    
    return 0;
}

/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int /*argc*/, char** /*argv*/)
{
    TestBlockCiphers();
    TestBenchmark();
    return 0;
}
