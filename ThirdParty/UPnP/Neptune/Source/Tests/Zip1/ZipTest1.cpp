/*****************************************************************
|
|      Zip Test Program 1
|
|      (c) 2001-2006 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "NptDebug.h"

#if defined(WIN32) && defined(_DEBUG)
#include <crtdbg.h>
#endif

#define CHECK(x)                                        \
    do {                                                \
      if (!(x)) {                                       \
        fprintf(stderr, "ERROR line %d \n", __LINE__);  \
        error_hook();                                   \
        return -1;                                      \
      }                                                 \
    } while(0)

/*----------------------------------------------------------------------
|       test vectors
+---------------------------------------------------------------------*/
extern unsigned char t1[];
extern unsigned int  t1_len;
extern unsigned char t1_gz[];
extern unsigned int  t1_gz_len;
extern unsigned int  t1_gz_header_len;

extern unsigned char t2[];
extern unsigned int  t2_len;
extern unsigned char t2_gz[];
extern unsigned int  t2_gz_len;
extern unsigned int  t2_gz_header_len;

typedef struct {
    unsigned char* uncompressed;
    unsigned int   uncompressed_len;
    unsigned char* compressed;
    unsigned int   compressed_len;
    unsigned int   compressed_header_len;
} TestVector;
TestVector TestVectors[] = {
    {t1, t1_len, t1_gz, t1_gz_len, t1_gz_header_len},
    {t2, t2_len, t2_gz, t2_gz_len, t2_gz_header_len},
};

/*----------------------------------------------------------------------
|       error_hook
+---------------------------------------------------------------------*/
static void
error_hook() 
{
    fprintf(stderr, "STOPPING\n");
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

for (unsigned int t=0; t<sizeof(TestVectors)/sizeof(TestVectors[0]); t++) {
    TestVector* v = &TestVectors[t];
    NPT_DataBuffer in1(v->compressed, v->compressed_len);
    NPT_DataBuffer out1;
    NPT_Result result = NPT_Zip::Inflate(in1, out1);
    CHECK(result == NPT_SUCCESS);
    CHECK(out1.GetDataSize() == v->uncompressed_len);
    CHECK(NPT_MemoryEqual(out1.GetData(), v->uncompressed, v->uncompressed_len));
    
    NPT_DataBuffer in2(v->uncompressed, v->uncompressed_len);
    NPT_DataBuffer out2;
    NPT_DataBuffer out2_check;
    result = NPT_Zip::Deflate(in2, out2, NPT_ZIP_COMPRESSION_LEVEL_MAX, NPT_Zip::GZIP);
    CHECK(result == NPT_SUCCESS);
    result = NPT_Zip::Inflate(out2, out2_check);
    CHECK(result == NPT_SUCCESS);
    CHECK(out2_check.GetDataSize() == in2.GetDataSize());
    CHECK(NPT_MemoryEqual(v->uncompressed, out2_check.GetData(), in2.GetDataSize()));
    
    // try with random data
    NPT_DataBuffer in3(300000);
    unsigned char* in3_p = in3.UseData();
    for (int i=0; i<300000; i++) {
        *in3_p++ = NPT_System::GetRandomInteger();
    }
    in3.SetDataSize(300000);
    NPT_DataBuffer out3;
    result = NPT_Zip::Deflate(in3, out3);
    CHECK(result == NPT_SUCCESS);
    NPT_DataBuffer out3_check;
    result = NPT_Zip::Inflate(out3, out3_check);
    CHECK(result == NPT_SUCCESS);
    CHECK(in3 == out3_check);

    // try with redundant data
    in3_p = in3.UseData();
    for (int i=0; i<200000; i+=4) {
        *in3_p++ = NPT_System::GetRandomInteger();
        *in3_p++ = 0;
        *in3_p++ = 0;
        *in3_p++ = 0;
    }
    result = NPT_Zip::Deflate(in3, out3);
    CHECK(result == NPT_SUCCESS);
    result = NPT_Zip::Inflate(out3, out3_check);
    CHECK(result == NPT_SUCCESS);
    CHECK(in3 == out3_check);

    // streams
    for (unsigned int x=0; x<1000; x++) {
        NPT_MemoryStream* ms_gz = new NPT_MemoryStream(v->compressed, v->compressed_len);
        NPT_InputStreamReference ms_gz_ref(ms_gz);
        NPT_ZipInflatingInputStream ziis(ms_gz_ref);
        NPT_DataBuffer buffer;
        NPT_Position position = 0;
        bool expect_eos = false;
        for (;;) {
            NPT_Size chunk = NPT_System::GetRandomInteger()%40000;
            buffer.SetDataSize(chunk);
            NPT_Size bytes_read = 0;
            result = ziis.Read(buffer.UseData(), chunk, &bytes_read);
            if (expect_eos) {
                CHECK(result == NPT_ERROR_EOS);
                break;
            }
            if (result == NPT_ERROR_EOS) {
                CHECK(position == v->uncompressed_len);
            } else {
                CHECK(result == NPT_SUCCESS);
            }
            CHECK(bytes_read <= chunk);
            if (bytes_read != chunk) expect_eos = true;
            CHECK(NPT_MemoryEqual(v->uncompressed+position, 
                                  buffer.GetData(),
                                  bytes_read));
            position += bytes_read;
        }
        CHECK(position == v->uncompressed_len);
    }

    for (unsigned int x=0; x<1000; x++) {
        NPT_MemoryStream* ms = new NPT_MemoryStream(v->uncompressed, v->uncompressed_len);
        NPT_InputStreamReference ms_ref(ms);
        NPT_ZipDeflatingInputStream zdis(ms_ref, NPT_ZIP_COMPRESSION_LEVEL_MAX, NPT_Zip::GZIP);
        NPT_DataBuffer buffer;
        NPT_Position position = 0;
        bool expect_eos = false;
        for (;;) {
            NPT_Size chunk = NPT_System::GetRandomInteger()%40000;
            buffer.Reserve(buffer.GetDataSize()+chunk);
            NPT_Size bytes_read = 0;
            result = zdis.Read(buffer.UseData()+buffer.GetDataSize(), chunk, &bytes_read);
            if (expect_eos) {
                CHECK(result == NPT_ERROR_EOS);
                break;
            }
            CHECK(result == NPT_SUCCESS);
            CHECK(bytes_read <= chunk);
            if (bytes_read != chunk) expect_eos = true;
            position += bytes_read;
            buffer.SetDataSize(buffer.GetDataSize()+bytes_read);
        }
        NPT_DataBuffer out;
        NPT_DataBuffer check(v->uncompressed, v->uncompressed_len);
        CHECK(NPT_Zip::Inflate(buffer, out) == NPT_SUCCESS);
        CHECK(out == check);
    }
}

    return 0;
}
