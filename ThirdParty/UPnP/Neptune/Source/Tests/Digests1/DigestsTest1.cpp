/*****************************************************************
|
|      Digests Test Program 1
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
|   TestDigests
+---------------------------------------------------------------------*/
static int
TestDigests()
{
    NPT_Digest* sha1   = NULL;
    NPT_Digest* sha256 = NULL;
    NPT_Digest* md5    = NULL;
    NPT_Result result;
    
    result = NPT_Digest::Create(NPT_Digest::ALGORITHM_SHA1, sha1);
    SHOULD_SUCCEED(result);
    NPT_String data = "hello";
    result = sha1->Update((const NPT_UInt8*)data.GetChars(), data.GetLength());
    SHOULD_SUCCEED(result);
    NPT_DataBuffer digest;
    result = sha1->GetDigest(digest);
    SHOULD_SUCCEED(result);
    SHOULD_EQUAL(sha1->GetSize(), 20);
    SHOULD_EQUAL(digest.GetDataSize(), 20);
    NPT_UInt8 digest1[] = {0xaa, 0xf4, 0xc6, 0x1d, 0xdc, 0xc5, 0xe8, 0xa2, 0xda, 0xbe, 0xde, 0x0f, 0x3b, 0x48, 0x2c, 0xd9, 0xae, 0xa9, 0x43, 0x4d};
    SHOULD_EQUAL_MEM(digest.GetData(), digest1, 20);
    delete sha1;
    
    result = NPT_Digest::Create(NPT_Digest::ALGORITHM_SHA1, sha1);
    SHOULD_SUCCEED(result);
    data = "Hello, this is a test for the Neptune digest functionality. Blablabla. Bliblibli";
    result = sha1->Update((const NPT_UInt8*)data.GetChars(), data.GetLength());
    SHOULD_SUCCEED(result);
    result = sha1->GetDigest(digest);
    SHOULD_SUCCEED(result);
    SHOULD_EQUAL(digest.GetDataSize(), 20);
    NPT_UInt8 digest2[] = {0x92, 0x6a, 0xd8, 0x38, 0xbf, 0x91, 0x51, 0x3b, 0xa6, 0xf9, 0x75, 0x6f, 0x8a, 0xa3, 0xcb, 0xe2, 0xe4, 0x5a, 0x95, 0xbd};
    SHOULD_EQUAL_MEM(digest.GetData(), digest2, 20);
    delete sha1;
    
    result = NPT_Digest::Create(NPT_Digest::ALGORITHM_SHA1, sha1);
    SHOULD_SUCCEED(result);
    data = "0123456789";
    for (unsigned int a=0; a<6; a++) {
        result = sha1->Update((const NPT_UInt8*)data.GetChars(), data.GetLength());
        SHOULD_SUCCEED(result);
    }
    result = sha1->GetDigest(digest);
    SHOULD_SUCCEED(result);
    SHOULD_EQUAL(digest.GetDataSize(), 20);
    NPT_UInt8 digest3[] = {0xf5, 0x2e, 0x3c, 0x27, 0x32, 0xde, 0x7b, 0xea, 0x28, 0xf2, 0x16, 0xd8, 0x77, 0xd7, 0x8d, 0xae, 0x1a, 0xa1, 0xac, 0x6a};
    SHOULD_EQUAL_MEM(digest.GetData(), digest3, 20);
    delete sha1;

    result = NPT_Digest::Create(NPT_Digest::ALGORITHM_SHA256, sha256);
    SHOULD_SUCCEED(result);
    data = "hello";
    result = sha256->Update((const NPT_UInt8*)data.GetChars(), data.GetLength());
    SHOULD_SUCCEED(result);
    result = sha256->GetDigest(digest);
    SHOULD_SUCCEED(result);
    SHOULD_EQUAL(sha256->GetSize(), 32);
    SHOULD_EQUAL(digest.GetDataSize(), 32);
    NPT_UInt8 digest4[] = {0x2c, 0xf2, 0x4d, 0xba, 0x5f, 0xb0, 0xa3, 0x0e, 0x26, 0xe8, 0x3b, 0x2a, 0xc5, 0xb9, 0xe2, 0x9e, 0x1b, 0x16, 0x1e, 0x5c, 0x1f, 0xa7, 0x42, 0x5e, 0x73, 0x04, 0x33, 0x62, 0x93, 0x8b, 0x98, 0x24};
    SHOULD_EQUAL_MEM(digest.GetData(), digest4, 32);
    delete sha256;
    
    result = NPT_Digest::Create(NPT_Digest::ALGORITHM_SHA256, sha256);
    SHOULD_SUCCEED(result);
    data = "Hello, this is a test for the Neptune digest functionality. Blablabla. Bliblibli";
    result = sha256->Update((const NPT_UInt8*)data.GetChars(), data.GetLength());
    SHOULD_SUCCEED(result);
    result = sha256->GetDigest(digest);
    SHOULD_SUCCEED(result);
    SHOULD_EQUAL(digest.GetDataSize(), 32);
    NPT_UInt8 digest5[] = {0xed, 0x9a, 0xee, 0xd7, 0x7e, 0x07, 0x1d, 0x3d, 0x24, 0x99, 0xc9, 0x11, 0xf5, 0x56, 0x89, 0x5a, 0x90, 0x22, 0x99, 0x59, 0xee, 0x51, 0x83, 0x4b, 0x17, 0x8b, 0xa1, 0x7e, 0x4b, 0x50, 0x32, 0x7e};
    SHOULD_EQUAL_MEM(digest.GetData(), digest5, 32);
    delete sha256;
    
    result = NPT_Digest::Create(NPT_Digest::ALGORITHM_SHA256, sha256);
    SHOULD_SUCCEED(result);
    data = "0123456789";
    for (unsigned int a=0; a<6; a++) {
        result = sha256->Update((const NPT_UInt8*)data.GetChars(), data.GetLength());
        SHOULD_SUCCEED(result);
    }
    result = sha256->GetDigest(digest);
    SHOULD_SUCCEED(result);
    SHOULD_EQUAL(digest.GetDataSize(), 32);
    NPT_UInt8 digest6[] = {0x5e, 0x43, 0xc8, 0x70, 0x4a, 0xc8, 0x1f, 0x33, 0xd7, 0x01, 0xc1, 0xac, 0xe0, 0x46, 0xba, 0x9f, 0x25, 0x70, 0x62, 0xb4, 0xd1, 0x7e, 0x78, 0xf3, 0x25, 0x4c, 0xbf, 0x24, 0x31, 0x77, 0xe4, 0xf2};
    SHOULD_EQUAL_MEM(digest.GetData(), digest6, 32);
    delete sha256;
    
    result = NPT_Digest::Create(NPT_Digest::ALGORITHM_MD5, md5);
    SHOULD_SUCCEED(result);
    data = "hello";
    result = md5->Update((const NPT_UInt8*)data.GetChars(), data.GetLength());
    SHOULD_SUCCEED(result);
    result = md5->GetDigest(digest);
    SHOULD_SUCCEED(result);
    SHOULD_EQUAL(digest.GetDataSize(), 16);
    NPT_UInt8 digest11[] = {0x5d, 0x41, 0x40, 0x2a, 0xbc, 0x4b, 0x2a, 0x76, 0xb9, 0x71, 0x9d, 0x91, 0x10, 0x17, 0xc5, 0x92};
    SHOULD_EQUAL_MEM(digest.GetData(), digest11, 16);
    delete md5;
    
    result = NPT_Digest::Create(NPT_Digest::ALGORITHM_MD5, md5);
    SHOULD_SUCCEED(result);
    data = "Hello, this is a test for the Neptune digest functionality. Blablabla. Bliblibli";
    result = md5->Update((const NPT_UInt8*)data.GetChars(), data.GetLength());
    SHOULD_SUCCEED(result);
    result = md5->GetDigest(digest);
    SHOULD_SUCCEED(result);
    SHOULD_EQUAL(digest.GetDataSize(), 16);
    NPT_UInt8 digest12[] = {0x32, 0x90, 0x77, 0xc4, 0xfe, 0xa9, 0x00, 0xf5, 0x6b, 0xb2, 0x62, 0x7f, 0xe3, 0xc0, 0x93, 0x51};
    SHOULD_EQUAL_MEM(digest.GetData(), digest12, 16);
    delete md5;
    
    result = NPT_Digest::Create(NPT_Digest::ALGORITHM_MD5, md5);
    SHOULD_SUCCEED(result);
    data = "0123456789";
    for (unsigned int a=0; a<6; a++) {
        result = md5->Update((const NPT_UInt8*)data.GetChars(), data.GetLength());
        SHOULD_SUCCEED(result);
    }
    result = md5->GetDigest(digest);
    SHOULD_SUCCEED(result);
    SHOULD_EQUAL(digest.GetDataSize(), 16);
    NPT_UInt8 digest13[] = {0x1c, 0xed, 0x81, 0x1a, 0xf4, 0x7e, 0xad, 0x37, 0x48, 0x72, 0xfc, 0xca, 0x9d, 0x73, 0xdd, 0x71};
    SHOULD_EQUAL_MEM(digest.GetData(), digest13, 16);
    delete md5;

    return 0;
}

/*----------------------------------------------------------------------
|   TestHmac
+---------------------------------------------------------------------*/
static int
TestHmac()
{
    NPT_Digest*  hmac = NULL;
    NPT_Result     result;
    NPT_String     data;
    NPT_DataBuffer mac;
    
    char key1[] = "hello";
    int  key1_size = 5;
    result = NPT_Hmac::Create(NPT_Digest::ALGORITHM_MD5, (const NPT_UInt8*)key1, key1_size, hmac);
    SHOULD_SUCCEED(result);
    data = "Hello, this is a test for the Neptune digest functionality. Blablabla. Bliblibli";
    result = hmac->Update((const NPT_UInt8*)data.GetChars(), data.GetLength());
    SHOULD_SUCCEED(result);
    result = hmac->GetDigest(mac);
    SHOULD_SUCCEED(result);
    SHOULD_EQUAL(mac.GetDataSize(), 16);
    NPT_UInt8 mac1[] = {0xb2, 0x4f, 0x2f, 0x05, 0x76, 0xcf, 0x5a, 0xa9, 0xa6, 0x05, 0xe3, 0x21, 0x6e, 0x70, 0xb6, 0x84};
    SHOULD_EQUAL_MEM(mac.GetData(), mac1, 16);
    delete hmac;
    
    char key2[] = "hello-this-is-a-long-key";
    int  key2_size = 24;
    result = NPT_Hmac::Create(NPT_Digest::ALGORITHM_MD5, (const NPT_UInt8*)key2, key2_size, hmac);
    SHOULD_SUCCEED(result);
    data = "Hello, this is a test for the Neptune digest functionality. Blablabla. Bliblibli";
    result = hmac->Update((const NPT_UInt8*)data.GetChars(), data.GetLength());
    SHOULD_SUCCEED(result);
    result = hmac->GetDigest(mac);
    SHOULD_SUCCEED(result);
    SHOULD_EQUAL(mac.GetDataSize(), 16);
    NPT_UInt8 mac2[] = {0x35, 0xaf, 0x0f, 0x44, 0x63, 0x33, 0xdb, 0x44, 0x1c, 0x09, 0x25, 0xbb, 0xab, 0x95, 0xfb, 0x87};
    SHOULD_EQUAL_MEM(mac.GetData(), mac2, 16);
    delete hmac;

    return 0;
}

/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int /*argc*/, char** /*argv*/)
{
    TestDigests();
    TestHmac();
    return 0;
}
