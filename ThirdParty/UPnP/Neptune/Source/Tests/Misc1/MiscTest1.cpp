/*****************************************************************
|
|      Misc Test Program 1
|
|      (c) 2005-2006 Gilles Boccon-Gibod
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
#include "NptUtils.h"
#include "NptTypes.h"
#include "NptDynamicCast.h"
#include "NptHash.h"

/*----------------------------------------------------------------------
|       macros
+---------------------------------------------------------------------*/
#define SHOULD_SUCCEED(r)                                   \
    do {                                                    \
        if (NPT_FAILED(r)) {                                \
            NPT_Debug("failed line %d (%d)\n", __LINE__, r);\
            exit(1);                                        \
        }                                                   \
    } while(0)                                         

#define SHOULD_FAIL(r)                                                  \
    do {                                                                \
        if (NPT_SUCCEEDED(r)) {                                         \
            NPT_Debug("should have failed line %d (%d)\n", __LINE__, r);\
            exit(1);                                                    \
        }                                                               \
    } while(0)                                  

#define SHOULD_EQUAL_I(a, b)                                           \
    do {                                                               \
        if ((a) != (b)) {                                              \
            NPT_Debug("got %l, expected %l line %d\n", a, b, __LINE__);\
            exit(1);                                                   \
        }                                                              \
    } while(0)                                  

#define SHOULD_EQUAL_F(a, b)                                           \
    do {                                                               \
        if ((a) != (b)) {                                              \
            NPT_Debug("got %f, expected %f line %d\n", a, b, __LINE__);\
            exit(1);                                                   \
        }                                                              \
    } while(0)                                  

#define SHOULD_EQUAL_S(a, b)                                           \
    do {                                                               \
        if (!NPT_StringsEqual(a,b)) {                                  \
            NPT_Debug("got %s, expected %s line %d\n", a, b, __LINE__);\
            exit(1);                                                   \
        }                                                              \
    } while(0)                                  


class BarA 
{
public:
    NPT_IMPLEMENT_DYNAMIC_CAST(BarA)
    virtual ~BarA() {}
    virtual int bar() { return 1; }
};
NPT_DEFINE_DYNAMIC_CAST_ANCHOR(BarA)

class FooA 
{
public:
    NPT_IMPLEMENT_DYNAMIC_CAST(FooA)
    virtual ~FooA() {}
    virtual int foo() { return 2; }
};
NPT_DEFINE_DYNAMIC_CAST_ANCHOR(FooA)

class FooB : public FooA 
{
public:
    NPT_IMPLEMENT_DYNAMIC_CAST_D(FooB, FooA)
    virtual int foo() { return 3; }
};
NPT_DEFINE_DYNAMIC_CAST_ANCHOR(FooB)

class FooC : public FooB, public BarA
{
public:
    NPT_IMPLEMENT_DYNAMIC_CAST_D2(FooC, FooB, BarA)
    virtual int foo() { return 4; }
    virtual int bar() { return 5; }
};
NPT_DEFINE_DYNAMIC_CAST_ANCHOR(FooC)

/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int /*argc*/, char** /*argv*/)
{
    NPT_Result result;

    // dynamic cast
    BarA* bar_a = new BarA();
    NPT_ASSERT(bar_a != NULL);
    NPT_ASSERT(NPT_DYNAMIC_CAST(BarA, bar_a) == bar_a);
    NPT_ASSERT(NPT_DYNAMIC_CAST(FooA, bar_a) == NULL);
    NPT_ASSERT(bar_a->bar() == 1);
    delete bar_a;

    FooA* foo_a = new FooA();
    NPT_ASSERT(foo_a != NULL);
    NPT_ASSERT(NPT_DYNAMIC_CAST(FooA, foo_a) == foo_a);
    NPT_ASSERT(NPT_DYNAMIC_CAST(FooB, foo_a) == NULL);
    NPT_ASSERT(foo_a->foo() == 2);
    delete foo_a;

    FooB* foo_b = new FooB();
    NPT_ASSERT(foo_b != NULL);
    foo_a = NPT_DYNAMIC_CAST(FooA, foo_b);
    NPT_ASSERT(NPT_DYNAMIC_CAST(FooB, foo_b) == foo_b);
    NPT_ASSERT(NPT_DYNAMIC_CAST(FooA, foo_b) != NULL);
    NPT_ASSERT(NPT_DYNAMIC_CAST(FooC, foo_b) == NULL);
    NPT_ASSERT(foo_a->foo() == 3);
    delete foo_b;

    FooC* foo_c = new FooC();
    NPT_ASSERT(foo_c != NULL);
    foo_a = NPT_DYNAMIC_CAST(FooA, foo_c);
    foo_b = NPT_DYNAMIC_CAST(FooB, foo_c);
    bar_a = NPT_DYNAMIC_CAST(BarA, foo_c);
    NPT_ASSERT(NPT_DYNAMIC_CAST(FooC, foo_c) == foo_c);
    NPT_ASSERT(foo_a != NULL);
    NPT_ASSERT(foo_b != NULL);
    NPT_ASSERT(bar_a != NULL);
    NPT_ASSERT(foo_a->foo() == 4);
    NPT_ASSERT(foo_b->foo() == 4);
    NPT_ASSERT(foo_c->foo() == 4);
    NPT_ASSERT(bar_a->bar() == 5);
    delete foo_c;

    // misc type tests
    signed long   sl;
    unsigned long ul;
    signed int    si;
    unsigned int  ui;
    NPT_Int64     si64;
    NPT_UInt64    ui64;
    
    NPT_ASSERT(sizeof(NPT_UInt32) == sizeof(NPT_Int32));
    NPT_ASSERT(sizeof(NPT_Int32) >= 4);
    NPT_ASSERT(sizeof(NPT_UInt64) == sizeof(NPT_Int64));
    NPT_ASSERT(sizeof(NPT_Int64) >= 8);
    sl = NPT_LONG_MAX;
    sl += 1;
    NPT_ASSERT(sl == NPT_LONG_MIN);
    si = NPT_INT_MAX;
    si += 1;
    NPT_ASSERT(si == NPT_INT_MIN);
    si64 = NPT_INT64_MAX;
    si64 += 1;
    NPT_ASSERT(si64 == NPT_INT64_MIN);
    ul = NPT_ULONG_MAX;
    ul += 1;
    NPT_ASSERT(ul == 0);
    ui = NPT_UINT_MAX;
    ui += 1;
    NPT_ASSERT(ui == 0);
    ui64 = NPT_UINT64_MAX;
    ui64 += 1;
    NPT_ASSERT(ui64 == 0);
    
    // base64
    NPT_String t = "hello";
    NPT_String base64;
    NPT_DataBuffer data;
    result = NPT_Base64::Encode((const NPT_Byte*)t.GetChars(), t.GetLength(), base64);
    NPT_ASSERT(NPT_SUCCEEDED(result));
    NPT_ASSERT(base64 == "aGVsbG8=");
    result = NPT_Base64::Decode(base64.GetChars(), base64.GetLength(), data);
    NPT_ASSERT(NPT_SUCCEEDED(result));
    NPT_ASSERT(data.GetDataSize() == t.GetLength());
    NPT_String tt((const char*)data.GetData(), data.GetDataSize());
    NPT_ASSERT(tt == t);

    t = "hello!";
    result = NPT_Base64::Encode((const NPT_Byte*)t.GetChars(), t.GetLength(), base64);
    NPT_ASSERT(NPT_SUCCEEDED(result));
    NPT_ASSERT(base64 == "aGVsbG8h");
    result = NPT_Base64::Decode(base64.GetChars(), base64.GetLength(), data);
    NPT_ASSERT(NPT_SUCCEEDED(result));
    NPT_ASSERT(data.GetDataSize() == t.GetLength());
    tt.Assign((const char*)data.GetData(), data.GetDataSize());
    NPT_ASSERT(tt == t);

    t = "hello!!";
    result = NPT_Base64::Encode((const NPT_Byte*)t.GetChars(), t.GetLength(), base64);
    NPT_ASSERT(NPT_SUCCEEDED(result));
    NPT_ASSERT(base64 == "aGVsbG8hIQ==");
    result = NPT_Base64::Decode(base64.GetChars(), base64.GetLength(), data);
    NPT_ASSERT(NPT_SUCCEEDED(result));
    NPT_ASSERT(data.GetDataSize() == t.GetLength());
    tt.Assign((const char*)data.GetData(), data.GetDataSize());
    NPT_ASSERT(tt == t);
    
    unsigned char r256_bin[] = {
        0x7d, 0x5f, 0xd0, 0xf4, 0x6a, 0xa8, 0xae, 0x34, 0x6e, 0x32, 0x1d, 0xa1,
        0xef, 0x66, 0xdd, 0x82, 0x76, 0xa6, 0xfd, 0x8c, 0x75, 0x97, 0xa0, 0x01,
        0x00, 0xde, 0x52, 0xef, 0xdf, 0xb6, 0x3e, 0xe4, 0x7b, 0x45, 0xdd, 0x2b,
        0xa1, 0x9c, 0xb0, 0x6d, 0x2c, 0x75, 0xb1, 0x87, 0x43, 0x0f, 0xea, 0x24,
        0x36, 0x11, 0x7e, 0xee, 0xd1, 0x91, 0x7f, 0x7b, 0x02, 0xea, 0x9a, 0x2a,
        0x25, 0xc0, 0xac, 0x99, 0xa4, 0x89, 0x55, 0x5b, 0x82, 0xdf, 0xb0, 0x7e,
        0xa1, 0x78, 0x0f, 0xdf, 0x25, 0x5f, 0x3d, 0xba, 0xcb, 0xbc, 0x35, 0x04,
        0xc3, 0xf4, 0xb8, 0xc0, 0x17, 0x8e, 0x75, 0x01, 0xe6, 0x2f, 0x88, 0x2c,
        0x76, 0x0a, 0x8c, 0x3f, 0x83, 0xd4, 0x10, 0xa8, 0x00, 0xfc, 0xa0, 0x92,
        0x7b, 0xae, 0xa3, 0x8c, 0x47, 0xea, 0x25, 0xf9, 0x29, 0x81, 0x1c, 0x21,
        0xf2, 0xf4, 0xfe, 0x07, 0x7e, 0x4b, 0x01, 0x79, 0x41, 0x3a, 0xb6, 0x71,
        0x0b, 0x75, 0xa7, 0x9d, 0x1b, 0x12, 0xc4, 0x46, 0x06, 0xf3, 0x5f, 0x00,
        0x05, 0x2a, 0x1b, 0x34, 0xd6, 0x87, 0xc4, 0x70, 0xcc, 0xc3, 0x9e, 0xa8,
        0x24, 0x2c, 0x97, 0x4e, 0xfc, 0x91, 0x70, 0x1c, 0x29, 0x66, 0xc3, 0x23,
        0xbf, 0xd7, 0x4d, 0x35, 0x51, 0xff, 0xeb, 0xde, 0x45, 0xbd, 0x8d, 0x80,
        0x44, 0x2a, 0x8d, 0xc0, 0xe8, 0x6a, 0xe2, 0x86, 0x46, 0x9f, 0xf2, 0x3c,
        0x93, 0x0d, 0x27, 0x02, 0xe4, 0x79, 0xa1, 0x21, 0xf4, 0x43, 0xcd, 0x4c,
        0x22, 0x25, 0x9e, 0x93, 0xeb, 0x77, 0x8e, 0x1e, 0x57, 0x1e, 0x9b, 0xcb,
        0x91, 0x86, 0xcf, 0x15, 0xaf, 0xd5, 0x03, 0x0f, 0x70, 0xbe, 0x6e, 0x37,
        0xea, 0x37, 0xdd, 0xf6, 0xa1, 0xb1, 0xf7, 0x05, 0xbc, 0x2d, 0x44, 0x60,
        0x35, 0xa4, 0x05, 0x0b, 0x22, 0x7d, 0x7a, 0x71, 0xe5, 0x1d, 0x8e, 0xcb,
        0xc3, 0xb8, 0x3a, 0xe1
    };
    NPT_String b64;
    NPT_Base64::Encode(r256_bin, sizeof(r256_bin), b64);
    NPT_DataBuffer r256_out;
    NPT_Base64::Decode(b64.GetChars(), b64.GetLength(), r256_out);
    NPT_ASSERT(r256_out.GetDataSize() == sizeof(r256_bin));
    NPT_ASSERT(r256_bin[sizeof(r256_bin)-1] == r256_out.GetData()[sizeof(r256_bin)-1]);

    unsigned char random_bytes[] = {
        0xc7, 0xee, 0x49, 0x9e, 0x2c, 0x8b, 0x1c, 0x16, 0x9e, 0x7f, 0x30, 0xd0,
        0xc6, 0x12, 0x30, 0x80, 0x81, 0xcd, 0x20, 0x20, 0x26, 0xaf, 0x4f, 0xd6,
        0xfc, 0x86, 0x2e, 0x85, 0xf3, 0x10, 0x38, 0x2b, 0x0e, 0xbb, 0x80, 0x68,
        0xbe, 0xff, 0x1c, 0xdc, 0x72, 0xb5, 0x0d, 0x8f, 0x8e, 0x6c, 0x09, 0x63,
        0xba, 0x21, 0x23, 0xb2, 0x24, 0x17, 0xd3, 0x17, 0x69, 0x44, 0x77, 0x11,
        0x36, 0x6a, 0x6e, 0xf2, 0x44, 0x87, 0xa1, 0xd3, 0xf3, 0x1f, 0x6c, 0x38,
        0x22, 0x4a, 0x44, 0x70, 0x66, 0xef, 0x8c, 0x3a, 0x51, 0xc8, 0xee, 0x85,
        0x00, 0x25, 0x93, 0x10, 0x2e, 0x0b, 0x1b, 0x03, 0x94, 0x47, 0x05, 0x22,
        0xd0, 0xc4, 0xec, 0x2e, 0xcc, 0xbc, 0xbb, 0x67, 0xfd, 0xec, 0x0e, 0xb1,
        0x3f, 0xbc, 0x82, 0xe0, 0xa7, 0x9c, 0xf3, 0xae, 0xbd, 0xb7, 0xab, 0x02,
        0xf1, 0xd9, 0x17, 0x4c, 0x9d, 0xeb, 0xe2, 0x00, 0x1e, 0x19, 0x6e, 0xb3,
        0xfd, 0x7d, 0xea, 0x49, 0x85, 0x43, 0x2f, 0x56, 0x81, 0x89, 0xba, 0x71,
        0x37, 0x10, 0xb5, 0x74, 0xab, 0x90, 0x4d, 0xc4, 0xd1, 0x0d, 0x8d, 0x6f,
        0x01, 0xf5, 0x2c, 0xc9, 0x1a, 0x79, 0xa1, 0x41, 0x71, 0x2b, 0xfb, 0xf3,
        0xd5, 0xe4, 0x2a, 0xf5, 0xad, 0x80, 0x7a, 0x03, 0xff, 0x5f, 0x45, 0x8c,
        0xec, 0x6a, 0x4b, 0x05, 0xe3, 0x65, 0x19, 0x70, 0x05, 0xad, 0xc4, 0xb8,
        0x4e, 0x9e, 0x9a, 0x36, 0x4a, 0x86, 0x9d, 0xf5, 0x99, 0xcb, 0x00, 0xb8,
        0xb9, 0xa7, 0x86, 0x18, 0xfc, 0x9a, 0xe7, 0x00, 0x6a, 0x67, 0xfa, 0x42,
        0x9d, 0xff, 0x4d, 0x7a, 0xe4, 0xe8, 0x03, 0x88, 0xff, 0x60, 0xe1, 0x8d,
        0x09, 0x5f, 0x6f, 0xde, 0x6b
    };
    NPT_Array<unsigned char> random(random_bytes, NPT_ARRAY_SIZE(random_bytes));

    t = "x+5JniyLHBaefzDQxhIwgIHNICAmr0/W/IYuhfMQOCsOu4Bovv8c3HK1DY+ObAlj\r\n"
        "uiEjsiQX0xdpRHcRNmpu8kSHodPzH2w4IkpEcGbvjDpRyO6FACWTEC4LGwOURwUi\r\n"
        "0MTsLsy8u2f97A6xP7yC4Kec8669t6sC8dkXTJ3r4gAeGW6z/X3qSYVDL1aBibpx\r\n"
        "NxC1dKuQTcTRDY1vAfUsyRp5oUFxK/vz1eQq9a2AegP/X0WM7GpLBeNlGXAFrcS4\r\n"
        "Tp6aNkqGnfWZywC4uaeGGPya5wBqZ/pCnf9NeuToA4j/YOGNCV9v3ms=";
    result = NPT_Base64::Decode(t.GetChars(), t.GetLength(), data);
    NPT_ASSERT(NPT_SUCCEEDED(result));
    NPT_ASSERT(data.GetDataSize() == 233);
    NPT_Array<unsigned char> verif(data.GetData(), data.GetDataSize());
    NPT_ASSERT(verif == random);

    result = NPT_Base64::Encode(&random[0], random.GetItemCount(), base64, NPT_BASE64_PEM_BLOCKS_PER_LINE);
    NPT_ASSERT(NPT_SUCCEEDED(result));
    NPT_ASSERT(base64 == t);

    NPT_String t_url = t;
    t.Replace('/', '_');
    t.Replace('+', '-');
    result = NPT_Base64::Encode(&random[0], random.GetItemCount(), base64, NPT_BASE64_PEM_BLOCKS_PER_LINE, true);
    NPT_ASSERT(NPT_SUCCEEDED(result));
    NPT_ASSERT(base64 == t);
    
    t = "76768484767685839";
    result = NPT_Base64::Decode(t.GetChars(), t.GetLength(), data);
    NPT_ASSERT(result == NPT_ERROR_INVALID_FORMAT);

    t = "76869=978686";
    result = NPT_Base64::Decode(t.GetChars(), t.GetLength(), data);
    NPT_ASSERT(result == NPT_ERROR_INVALID_FORMAT);

    t = "7686=8978686";
    result = NPT_Base64::Decode(t.GetChars(), t.GetLength(), data);
    NPT_ASSERT(result == NPT_ERROR_INVALID_FORMAT);

    t = "7686==978686";
    result = NPT_Base64::Decode(t.GetChars(), t.GetLength(), data);
    NPT_ASSERT(result == NPT_ERROR_INVALID_FORMAT);

    // test IP address parsing
    NPT_IpAddress ip;
    NPT_ASSERT(NPT_FAILED(ip.Parse("")));
    NPT_ASSERT(NPT_FAILED(ip.Parse("a.b.c.d")));
    NPT_ASSERT(NPT_FAILED(ip.Parse("1.2.3.4.5")));
    NPT_ASSERT(NPT_FAILED(ip.Parse("1")));
    NPT_ASSERT(NPT_FAILED(ip.Parse("1.2.3.4.")));
    NPT_ASSERT(NPT_FAILED(ip.Parse("1.2.3.4f")));
    NPT_ASSERT(NPT_FAILED(ip.Parse("1.g.3.4")));
    NPT_ASSERT(NPT_FAILED(ip.Parse("1.2..3.4")));
    NPT_ASSERT(NPT_FAILED(ip.Parse("1.2.300.4")));
    NPT_ASSERT(NPT_SUCCEEDED(ip.Parse("1.2.3.4")));
    NPT_ASSERT(ip.AsBytes()[0] == 1);
    NPT_ASSERT(ip.AsBytes()[1] == 2);
    NPT_ASSERT(ip.AsBytes()[2] == 3);
    NPT_ASSERT(ip.AsBytes()[3] == 4);
    NPT_ASSERT(NPT_SUCCEEDED(ip.Parse("255.255.0.1")));
    NPT_ASSERT(ip.AsBytes()[0] == 255);
    NPT_ASSERT(ip.AsBytes()[1] == 255);
    NPT_ASSERT(ip.AsBytes()[2] == 0);
    NPT_ASSERT(ip.AsBytes()[3] == 1);
    NPT_ASSERT(NPT_SUCCEEDED(ip.Parse("0.0.0.0")));
    NPT_ASSERT(ip.AsBytes()[0] == 0);
    NPT_ASSERT(ip.AsBytes()[1] == 0);
    NPT_ASSERT(ip.AsBytes()[2] == 0);
    NPT_ASSERT(ip.AsBytes()[3] == 0);

    // MIME parameter parser
    NPT_Map<NPT_String,NPT_String> params;
    result = NPT_ParseMimeParameters(NULL, params);
    NPT_ASSERT(result == NPT_ERROR_INVALID_PARAMETERS);
    
    result = NPT_ParseMimeParameters("", params);
    NPT_ASSERT(NPT_SUCCEEDED(result));
    NPT_ASSERT(params.GetEntryCount() == 0);
        
    result = NPT_ParseMimeParameters("foo=bar", params);
    NPT_ASSERT(NPT_SUCCEEDED(result));
    NPT_ASSERT(params.GetEntryCount() == 1);
    NPT_ASSERT(params["foo"] == "bar");
    params.Clear();

    result = NPT_ParseMimeParameters(" foo =bar", params);
    NPT_ASSERT(NPT_SUCCEEDED(result));
    NPT_ASSERT(params.GetEntryCount() == 1);
    NPT_ASSERT(params["foo"] == "bar");
    params.Clear();

    result = NPT_ParseMimeParameters(" foo= bar", params);
    NPT_ASSERT(NPT_SUCCEEDED(result));
    NPT_ASSERT(params.GetEntryCount() == 1);
    NPT_ASSERT(params["foo"] == "bar");
    params.Clear();
    
    result = NPT_ParseMimeParameters(" foo= bar;", params);
    NPT_ASSERT(NPT_SUCCEEDED(result));
    NPT_ASSERT(params.GetEntryCount() == 1);
    NPT_ASSERT(params["foo"] == "bar");
    params.Clear();

    result = NPT_ParseMimeParameters("foo=\"bar\"", params);
    NPT_ASSERT(NPT_SUCCEEDED(result));
    NPT_ASSERT(params.GetEntryCount() == 1);
    NPT_ASSERT(params["foo"] == "bar");
    params.Clear();

    result = NPT_ParseMimeParameters("foo=\"ba\"r\"", params);
    NPT_ASSERT(result == NPT_ERROR_INVALID_SYNTAX);
    params.Clear();

    result = NPT_ParseMimeParameters("foo=\"ba\\\"r\"", params);
    NPT_ASSERT(NPT_SUCCEEDED(result));
    NPT_ASSERT(params.GetEntryCount() == 1);
    NPT_ASSERT(params["foo"] == "ba\"r");
    params.Clear();

    result = NPT_ParseMimeParameters("foo=\"bar\\\"\"", params);
    NPT_ASSERT(NPT_SUCCEEDED(result));
    NPT_ASSERT(params.GetEntryCount() == 1);
    NPT_ASSERT(params["foo"] == "bar\"");
    params.Clear();

    result = NPT_ParseMimeParameters("foo=\"bar\\\\\"", params);
    NPT_ASSERT(NPT_SUCCEEDED(result));
    NPT_ASSERT(params.GetEntryCount() == 1);
    NPT_ASSERT(params["foo"] == "bar\\");
    params.Clear();

    result = NPT_ParseMimeParameters("a=1;b=2; c=3; d=4 ; e=\"\\;\"; f=\";\"", params);
    NPT_ASSERT(NPT_SUCCEEDED(result));
    NPT_ASSERT(params.GetEntryCount() == 6);
    NPT_ASSERT(params["a"] == "1");
    NPT_ASSERT(params["b"] == "2");
    NPT_ASSERT(params["c"] == "3");
    NPT_ASSERT(params["d"] == "4");
    NPT_ASSERT(params["e"] == ";");
    NPT_ASSERT(params["f"] == ";");
    params.Clear();

    // number parsing
    float      f;
    int        i;
    int        l;
    NPT_Int32  si32;
    NPT_UInt32 ui32;

    SHOULD_FAIL(NPT_ParseInteger("ssdfsdf", i, false));
    SHOULD_FAIL(NPT_ParseInteger("", i, false));
    SHOULD_FAIL(NPT_ParseInteger(NULL, i, false));
    SHOULD_FAIL(NPT_ParseInteger("123a", i, false));
    SHOULD_FAIL(NPT_ParseInteger("a123", i, false));
    SHOULD_FAIL(NPT_ParseInteger(" 123", i, false));
    SHOULD_FAIL(NPT_ParseInteger("a 123", i, true));
    SHOULD_FAIL(NPT_ParseInteger(" a123", i, true));

    SHOULD_SUCCEED(NPT_ParseInteger("+1", i, false));
    SHOULD_EQUAL_I(i, 1);
    SHOULD_SUCCEED(NPT_ParseInteger("+123", i, false));
    SHOULD_EQUAL_I(i, 123);
    SHOULD_SUCCEED(NPT_ParseInteger("-1", i, false));
    SHOULD_EQUAL_I(i, -1);
    SHOULD_SUCCEED(NPT_ParseInteger("-123", i, false));
    SHOULD_EQUAL_I(i, -123);
    SHOULD_SUCCEED(NPT_ParseInteger("-123fgs", i, true));
    SHOULD_EQUAL_I(i, -123);
    SHOULD_SUCCEED(NPT_ParseInteger("  -123fgs", i, true));
    SHOULD_EQUAL_I(i, -123);
    SHOULD_SUCCEED(NPT_ParseInteger("0", i, true));
    SHOULD_EQUAL_I(i, 0);
    SHOULD_SUCCEED(NPT_ParseInteger("7768", i, true));
    SHOULD_EQUAL_I(i, 7768);

    SHOULD_SUCCEED(NPT_ParseInteger("+1", l, false));
    SHOULD_EQUAL_I(l, 1);
    SHOULD_SUCCEED(NPT_ParseInteger("+123", l, false));
    SHOULD_EQUAL_I(l, 123);
    SHOULD_SUCCEED(NPT_ParseInteger("-1", l, false));
    SHOULD_EQUAL_I(l, -1);
    SHOULD_SUCCEED(NPT_ParseInteger("-123", l, false));
    SHOULD_EQUAL_I(l, -123);
    SHOULD_SUCCEED(NPT_ParseInteger("-123fgs", l, true));
    SHOULD_EQUAL_I(l, -123);
    SHOULD_SUCCEED(NPT_ParseInteger("  -123fgs", l, true));
    SHOULD_EQUAL_I(l, -123);
    SHOULD_SUCCEED(NPT_ParseInteger("0", l, true));
    SHOULD_EQUAL_I(l, 0);
    SHOULD_SUCCEED(NPT_ParseInteger("7768", l, true));
    SHOULD_EQUAL_I(l, 7768);

    SHOULD_SUCCEED(NPT_ParseInteger32("2147483647", si32, false));
    SHOULD_EQUAL_I(si32, 2147483647);
    SHOULD_SUCCEED(NPT_ParseInteger32("-2147483647", si32, false));
    SHOULD_EQUAL_I(si32, -2147483647);
    SHOULD_SUCCEED(NPT_ParseInteger32("-2147483648", si32, false));
    SHOULD_EQUAL_I(si32, (-2147483647 - 1));
    SHOULD_FAIL(NPT_ParseInteger32("2147483648", si32, false));
    SHOULD_FAIL(NPT_ParseInteger32("-2147483649", si32, false));
    SHOULD_FAIL(NPT_ParseInteger32("-21474836480", si32, false));
    SHOULD_FAIL(NPT_ParseInteger32("21474836470", si32, false));

    SHOULD_SUCCEED(NPT_ParseInteger32("4294967295", ui32, false));
    SHOULD_EQUAL_I(ui32, 4294967295U);
    SHOULD_FAIL(NPT_ParseInteger32("4294967296", ui32, false));
    SHOULD_FAIL(NPT_ParseInteger32("-1", ui32, false));

    SHOULD_SUCCEED(NPT_ParseInteger64("9223372036854775807", si64, false));
    SHOULD_EQUAL_I(si64, NPT_INT64_C(9223372036854775807));
    SHOULD_SUCCEED(NPT_ParseInteger64("-9223372036854775807", si64, false));
    SHOULD_EQUAL_I(si64, NPT_INT64_C(-9223372036854775807));
    SHOULD_SUCCEED(NPT_ParseInteger64("-9223372036854775808", si64, false));
    SHOULD_EQUAL_I(si64, (NPT_INT64_C(-9223372036854775807) - NPT_INT64_C(1)));
    SHOULD_FAIL(NPT_ParseInteger64("9223372036854775808", si64, false));
    SHOULD_FAIL(NPT_ParseInteger64("-9223372036854775809", si64, false));
    SHOULD_FAIL(NPT_ParseInteger64("-9223372036854775897", si64, false));
    SHOULD_FAIL(NPT_ParseInteger64("9223372036854775897", si64, false));

    SHOULD_SUCCEED(NPT_ParseInteger64("18446744073709551615", ui64, false));
    SHOULD_EQUAL_I(ui64, NPT_UINT64_C(18446744073709551615));
    SHOULD_FAIL(NPT_ParseInteger64("18446744073709551616", ui64, false));
    SHOULD_FAIL(NPT_ParseInteger64("-1", ui64, false));

    SHOULD_FAIL(NPT_ParseFloat("ssdfsdf", f, false));
    SHOULD_FAIL(NPT_ParseFloat("", f, false));
    SHOULD_FAIL(NPT_ParseFloat(NULL, f, false));
    SHOULD_FAIL(NPT_ParseFloat("123.", f, false));
    SHOULD_FAIL(NPT_ParseFloat("a123", f, false));
    SHOULD_FAIL(NPT_ParseFloat(" 123", f, false));
    SHOULD_FAIL(NPT_ParseFloat(" 127.89E5ff", f, false));

    SHOULD_SUCCEED(NPT_ParseFloat("+1.0", f, false));
    SHOULD_EQUAL_F(f, 1.0f);
    SHOULD_SUCCEED(NPT_ParseFloat("+123", f, false));
    SHOULD_EQUAL_F(f, 123.0f);
    SHOULD_SUCCEED(NPT_ParseFloat("-0.1", f, false));
    SHOULD_EQUAL_F(f, -0.1f);
    SHOULD_SUCCEED(NPT_ParseFloat("0.23e-13", f, false));
    SHOULD_EQUAL_F(f, 0.23e-13f);
    SHOULD_SUCCEED(NPT_ParseFloat(" 127.89E5ff", f, true));
    SHOULD_EQUAL_F(f, 127.89E5f);
    SHOULD_SUCCEED(NPT_ParseFloat("+0.3db", f, true));
    SHOULD_EQUAL_F(f, 0.3f);
    SHOULD_SUCCEED(NPT_ParseFloat("+.3db", f, true));
    SHOULD_EQUAL_F(f, 0.3f);
    SHOULD_SUCCEED(NPT_ParseFloat("-.3db", f, true));
    SHOULD_EQUAL_F(f, -0.3f);
    SHOULD_SUCCEED(NPT_ParseFloat(".3db", f, true));
    SHOULD_EQUAL_F(f, .3f);

    // FNV hash
    NPT_UInt32 h32 = NPT_Fnv1aHash32((const NPT_UInt8*)"curds and whey", 14);
    SHOULD_EQUAL_I(h32, 0x22d5344e);
    h32 = NPT_Fnv1aHashStr32("curds and whey");
    SHOULD_EQUAL_I(h32, 0x22d5344e);
    h32 = NPT_Hash<const char*>()("curds and whey");
    SHOULD_EQUAL_I(h32, 0x22d5344e);
    
    NPT_UInt64 h64 = NPT_Fnv1aHash64((const NPT_UInt8*)"curds and whey", 14);
    SHOULD_EQUAL_I(h64, 0x23e520e2751bb46eULL);
    h64 = NPT_Fnv1aHashStr64("curds and whey");
    SHOULD_EQUAL_I(h64, 0x23e520e2751bb46eULL);
    
    return 0;
}
