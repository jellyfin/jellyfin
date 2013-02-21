/*****************************************************************
|
|      URL Test Program 1
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
      }                                                 \
    } while(0)

/*----------------------------------------------------------------------
|       Parse Test Vectors
+---------------------------------------------------------------------*/
typedef struct {
    const char* url;
    bool        expected_to_be_valid;
    const char* expected_scheme;
    const char* expected_host;
    int         expected_port;
    const char* expected_path;
    const char* expected_query;
    const char* expected_fragment;
    const char* expected_string;
} ParseTestVector;

static ParseTestVector ParseTestVectors[] = {
  {"",                                                                       false, NULL,   NULL,      0,   NULL,                  NULL,                            NULL,       NULL},
  {"http",                                                                   false, NULL,   NULL,      0,   NULL,                  NULL,                            NULL,       NULL},
  {"http:",                                                                  false, NULL,   NULL,      0,   NULL,                  NULL,                            NULL,       NULL},
  {"http:/",                                                                 false, NULL,   NULL,      0,   NULL,                  NULL,                            NULL,       NULL},
  {"http://",                                                                false, NULL,   NULL,      0,   NULL,                  NULL,                            NULL,       NULL},
  {"http://a",                                                               true,  "http", "a",       80,  "/",                   NULL,                            NULL,       "http://a/"},
  {"http://foo.bar",                                                         true,  "http", "foo.bar", 80,  "/",                   NULL,                            NULL,       "http://foo.bar/"},
  {"http://foo.bar:",                                                        false, NULL,   NULL,      0,   NULL,                  NULL,                            NULL,       NULL},
  {"http://foo.bar:156",                                                     true,  "http", "foo.bar", 156, "/",                   NULL,                            NULL,       "http://foo.bar:156/"},
  {"http://foo.bar:176899",                                                  false, NULL,   NULL,      0,   NULL,                  NULL,                            NULL,       NULL},
  {"http://foo.bar:176a",                                                    false, NULL,   NULL,      0,   NULL,                  NULL,                            NULL,       NULL},
  {"http://foo.bar:176/",                                                    true,  "http", "foo.bar", 176, "/",                   NULL,                            NULL,       "http://foo.bar:176/"},
  {"http://foo.bar:176/blabla",                                              true,  "http", "foo.bar", 176, "/blabla",             NULL,                            NULL,       "http://foo.bar:176/blabla"},
  {"http://foo.bar/blabla/blibli",                                           true,  "http", "foo.bar", 80,  "/blabla/blibli",      NULL,                            NULL,       "http://foo.bar/blabla/blibli"},
  {"http://foo.bar/blabla/blibli",                                           true,  "http", "foo.bar", 80,  "/blabla/blibli",      NULL,                            NULL,       "http://foo.bar/blabla/blibli"},
  {"http://foo.bar:176/blabla/blibli/",                                      true,  "http", "foo.bar", 176, "/blabla/blibli/",     NULL,                            NULL,       "http://foo.bar:176/blabla/blibli/"},
  {"http://foo.bar/",                                                        true,  "http", "foo.bar", 80,  "/",                   NULL,                            NULL,       "http://foo.bar/"},
  {"http://foo.bar/blabla/blibli/?query",                                    true,  "http", "foo.bar", 80,  "/blabla/blibli/",     "query",                         NULL,       "http://foo.bar/blabla/blibli/?query"},
  {"http://foo.bar/blabla/blibli/?query=1&bla=%20&slash=/&foo=a#fragment",   true,  "http", "foo.bar", 80,  "/blabla/blibli/",     "query=1&bla=%20&slash=/&foo=a", "fragment", "http://foo.bar/blabla/blibli/?query=1&bla=%20&slash=/&foo=a#fragment"},
  {"http://foo.bar/blabla%20foo/blibli/?query=1&bla=2&slash=/&foo=a#fragment", true,  "http", "foo.bar", 80,  "/blabla%20foo/blibli/", "query=1&bla=2&slash=/&foo=a","fragment", "http://foo.bar/blabla%20foo/blibli/?query=1&bla=2&slash=/&foo=a#fragment"},
  {"http://foo.bar?query",                                                   true,  "http", "foo.bar", 80,  NULL,                  "query",                         NULL,       "http://foo.bar/?query"},
  {"http://foo.bar#fragment",                                                true,  "http", "foo.bar", 80,  NULL,                   NULL,                           "fragment", "http://foo.bar/#fragment"}
};
 
typedef struct {
    char* scheme;
    char* host;
    int   port;
    char* qery;
    char* fragment;
    char* expected_uri;
} ConstructTestVector;

typedef struct {
    char* in;
    char* out;
    bool  do_percent;
} EncodeTestVector;

typedef struct {
    char* in;
    char* out;
} DecodeTestVector;

/*----------------------------------------------------------------------
|       TestParse
+---------------------------------------------------------------------*/
static void
TestParse(ParseTestVector* vector, int test_index)
{
    NPT_HttpUrl url(vector->url);
    if (url.IsValid() != vector->expected_to_be_valid) {
        fprintf(stderr, "TEST %02d: expected IsValid() to return %s, got %s\n", test_index, vector->expected_to_be_valid?"true":"false", url.IsValid()?"true":"false");
        return;
    }
    if (!vector->expected_to_be_valid) return;
    if (vector->expected_scheme) {
        if (url.GetScheme() != vector->expected_scheme) {
            fprintf(stderr, "TEST %02d: expected GetScheme() to return %s, got %s\n", test_index, vector->expected_scheme, url.GetScheme().GetChars());
            return;
        }
    }
    if (vector->expected_host) {
        if (url.GetHost() != vector->expected_host) {
            fprintf(stderr, "TEST %02d: expected GetHost() to return %s, got %s\n", test_index, vector->expected_host, url.GetHost().GetChars());
            return;
        }
    }
    if (url.GetPort() != vector->expected_port) {
        fprintf(stderr, "TEST %02d: expected GetPort() to return %d, got %d\n", test_index, vector->expected_port, url.GetPort());
        return;
    }
    if (vector->expected_path) {
        if (url.GetPath() != vector->expected_path) {
            fprintf(stderr, "TEST %02d: expected GetPath() to return %s, got %s\n", test_index, vector->expected_path, url.GetPath().GetChars());
            return;
        }
    }    
    if (url.HasQuery() != (vector->expected_query != NULL)) {
        fprintf(stderr, "TEST %02d: expected a query, did not get one\n", test_index);
        return;
    }
    if (vector->expected_query) {
        if (url.GetQuery() != vector->expected_query) {
            fprintf(stderr, "TEST %02d: expected GetQuery() to return %s, got %s\n", test_index, vector->expected_query, url.GetQuery().GetChars());
            return;
        }
    } 
    if (url.HasFragment() != (vector->expected_fragment != NULL)) {
        fprintf(stderr, "TEST %02d: expected a fragment, did not get one\n", test_index);
        return;
    }
    if (vector->expected_fragment) {
        if (url.GetFragment() != vector->expected_fragment) {
            fprintf(stderr, "TEST %02d: expected GetFragment() to return %s, got %s\n", test_index, vector->expected_fragment, url.GetFragment().GetChars());
            return;
        }
    } 
    
    NPT_String url_string = url.ToString();
    if (url_string != vector->expected_string) {
        fprintf(stderr, "TEST %02d: expected ToString() to return %s, got %s\n", test_index, vector->expected_string, url_string.GetChars());
        return;
    }
    NPT_HttpUrl url2(url_string);
    if (url2.ToString() != url_string) {
        fprintf(stderr, "TEST %02d: url ToString() does not parse to same url\n", test_index);
        return;
    }
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

    printf("--- test starting\n");
    
    // parsing test vectors
    for (unsigned int i=0; i<sizeof(ParseTestVectors)/sizeof(ParseTestVectors[0]); i++) {
        ParseTestVector* vector = &ParseTestVectors[i];
        TestParse(vector, i);
    }
    
    // test URL parsing, special cases
    NPT_HttpUrl url;
    CHECK(!url.IsValid());

    url = "http://foo.bar/blabla%20foo/blibli/?query=1&bla=2&slash=/&foo=a#fragment";
    CHECK(url.IsValid());
    CHECK(url.GetHost() == "foo.bar");
    CHECK(url.GetPort() == 80);
    CHECK(url.GetPath() == "/blabla%20foo/blibli/");
    CHECK(url.GetQuery() == "query=1&bla=2&slash=/&foo=a");
    CHECK(url.GetFragment() == "fragment");
    CHECK(url.ToString(false) == "http://foo.bar/blabla%20foo/blibli/?query=1&bla=2&slash=/&foo=a");

    url = NPT_HttpUrl("http://foo.bar/blabla%20foo/blibli/?query=1&bla=2&slash=/&foo=a#fragment");
    CHECK(url.IsValid());
    CHECK(url.GetHost() == "foo.bar");
    CHECK(url.GetPort() == 80);
    CHECK(url.GetPath() == "/blabla%20foo/blibli/");
    CHECK(url.GetQuery() == "query=1&bla=2&slash=/&foo=a");
    CHECK(url.GetFragment() == "fragment");
    CHECK(url.ToRequestString() == "/blabla%20foo/blibli/?query=1&bla=2&slash=/&foo=a");

    url.ParsePathPlus("/bla/foo?query=bar");
    url.SetHost("bar.com:8080");
    CHECK(url.IsValid());
    CHECK(url.GetHost() == "bar.com");
    CHECK(url.GetPort() == 8080);
    CHECK(url.GetPath() == "/bla/foo");
    CHECK(url.GetQuery() == "query=bar");
    
    url.ParsePathPlus("bla/foo?query=bar");
    url.SetHost("bar.com:8080");
    CHECK(url.IsValid());
    CHECK(url.GetHost() == "bar.com");
    CHECK(url.GetPort() == 8080);
    CHECK(url.GetPath() == "bla/foo");
    CHECK(url.GetQuery() == "query=bar");

    url.ParsePathPlus("*");
    CHECK(url.IsValid());
    CHECK(url.GetPath() == "*");

    url = NPT_HttpUrl("http://foo/?query=1&bla=2&slash=/&foo=a#fragment");
    CHECK(url.IsValid());
    CHECK(url.GetHost() == "foo");
    CHECK(url.GetPort() == 80);
    CHECK(url.GetPath() == "/");
    CHECK(url.GetQuery() == "query=1&bla=2&slash=/&foo=a");
    CHECK(url.GetFragment() == "fragment");
    CHECK(url.ToRequestString() == "/?query=1&bla=2&slash=/&foo=a");

    // url form encoding
    NPT_UrlQuery query;
    query.AddField("url1","http://foo.bar/foo?q=3&bar=+7/3&boo=a%3Db&bli=a b");
    query.AddField("url2","(1234+456 789)");
    CHECK(query.ToString() == "url1=http%3A%2F%2Ffoo.bar%2Ffoo%3Fq%3D3%26bar%3D%2B7%2F3%26boo%3Da%253Db%26bli%3Da+b&url2=(1234%2B456+789)");

    query = "url1=http%3A%2F%2Ffoo.bar%2Ffoo%3Fq%3D3%26bar%3D%2B7%2F3&url2=12+34";
    CHECK(query.ToString() == "url1=http%3A%2F%2Ffoo.bar%2Ffoo%3Fq%3D3%26bar%3D%2B7%2F3&url2=12+34");

    // url query decoding
    NPT_UrlQuery query2("a=1+2+3&b=http%3A%2F%2Ffoo.bar%2Ffoo%3Fq%3D3%26bar%3D%2B7%2F3%26boo%3Da%3Db%26bli%3Da+b");
    const char* a_field = query2.GetField("a");
    const char* b_field = query2.GetField("b");
    const char* c_field = query2.GetField("c");
    CHECK(a_field != NULL);
    CHECK(NPT_StringsEqual(a_field, "1+2+3"));
    CHECK(NPT_UrlQuery::UrlDecode(a_field) == "1 2 3");
    CHECK(b_field != NULL);
    CHECK(NPT_StringsEqual(b_field, "http%3A%2F%2Ffoo.bar%2Ffoo%3Fq%3D3%26bar%3D%2B7%2F3%26boo%3Da%3Db%26bli%3Da+b"));
    CHECK(NPT_UrlQuery::UrlDecode(b_field) == "http://foo.bar/foo?q=3&bar= 7/3&boo=a=b&bli=a b");
    CHECK(c_field == NULL);
    
    // url query misc
    NPT_UrlQuery query3;
    query3.SetField("a b", "c&3", false);
    query3.AddField("a b", "c&4 b&6", false);
    query3.SetField("c d", "c&5", false);
    query3.SetField("a+b", "c_3", true);
    const char* field1 = query3.GetField("a b");
    const char* field2 = query3.GetField("c d");
    CHECK(field1 != NULL);
    CHECK(NPT_UrlQuery::UrlDecode(field1) == "c_3");
    CHECK(field2 != NULL);
    CHECK(NPT_UrlQuery::UrlDecode(field2) == "c&5");

    // url query with empty values
    NPT_UrlQuery query4("a=1&b&c=");
    a_field = query4.GetField("a");
    b_field = query4.GetField("b");
    c_field = query4.GetField("c");
    CHECK(NPT_StringsEqual(a_field, "1"));
    CHECK(NPT_StringsEqual(b_field, ""));
    CHECK(NPT_StringsEqual(c_field, ""));

    printf("--- test done\n");
    
    return 0;
}
