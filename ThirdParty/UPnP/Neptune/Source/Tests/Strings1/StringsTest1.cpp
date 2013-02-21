/*****************************************************************
|
|      Stings Test Program 1
|
|      (c) 2001-2003 Gilles Boccon-Gibod
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

/*----------------------------------------------------------------------
|       Fail
+---------------------------------------------------------------------*/
static void
Fail()
{
    printf("##################################\n");
    NPT_ASSERT(0);
    //exit(1);
}

/*----------------------------------------------------------------------
|       CompareTest
+---------------------------------------------------------------------*/
static void
CompareTest(const char* name, const char* a, const char* b, int result, int expected)
{
    printf("%s %s %s = %d [%s]\n", a, name, b, result, result == expected ? "pass" : "fail");
    if (result != expected) Fail();
}

/*----------------------------------------------------------------------
|       EqualTest
+---------------------------------------------------------------------*/
static void
EqualTest(const char* name, const char* a, const char* b, const char* expected)
{
    printf("op %s on %s, result = %s ", name, a, b);
    if (strcmp(expected, b)) {
        printf(" [fail: expected %s, got %s]\n", expected, b);
    } else {
        printf(" [pass]\n");
    }
    if (strcmp(expected, b)) Fail();
}

/*----------------------------------------------------------------------
|       StringTest
+---------------------------------------------------------------------*/
static void
StringTest(const char* name, const char* a, const char* expected)
{
    printf("%s: %s", name, a);
    if (strcmp(expected, a)) {
        printf(" [fail: expected %s, got %s]\n", expected, a);
    } else {
        printf(" [pass]\n");
    }
    if (strcmp(expected, a)) Fail();
}

/*----------------------------------------------------------------------
|       IntTest
+---------------------------------------------------------------------*/
static void
IntTest(const char* name, int a, int expected)
{
    printf("%s: %d", name, a);
    if (a != expected) {
        printf(" [fail: expected %d, got %d]\n", expected, a);
    } else {
        printf(" [pass]\n");
    }
    if (a != expected) Fail();
}

/*----------------------------------------------------------------------
|       FloatTest
+---------------------------------------------------------------------*/
static void
FloatTest(const char* name, float a, float expected)
{
    printf("%s: %f", name, a);
    if (a != expected) {
        printf(" [fail: expected %f, got %f]\n", expected, a);
    } else {
        printf(" [pass]\n");
    }
    if (a != expected) Fail();
}


/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int /*argc*/, char** /*argv*/)
{
    printf(":: testing empty string\n");
    NPT_String s;
    printf("sizeof(s)=%d, chars = '%s'\n", (int)sizeof(s), s.GetChars());

    printf(":: testing allocation, new and delete\n");
    NPT_String* n0 = new NPT_String("Hello");
    delete n0;
    NPT_String n1 = "Bye";
    n1 = "ByeBye";

    printf(":: testing factories\n");
    NPT_String f0 = NPT_String::FromInteger(0);
    StringTest("FromInteger(0)", f0, "0");
    f0 = NPT_String::FromInteger(1234567);
    StringTest("FromInteger(1234567)", f0, "1234567");
    f0 = NPT_String::FromInteger(-1234567);
    StringTest("FromInteger(-1234567)", f0, "-1234567");
    f0 = NPT_String::FromIntegerU(0xFFFFFFFF);
    StringTest("FromIntegerU(0xFFFFFFFF)", f0, "4294967295");

    printf(":: testing constructors\n");
    NPT_String s00;
    StringTest("constructor()", s00, "");
    NPT_String s01("abcdef");
    StringTest("constructor(const char*)", s01, "abcdef");
    NPT_String s02(s01);
    StringTest("constructor(const NPT_String&)", s02, "abcdef");
    NPT_String s03("abcdefgh", 3);
    StringTest("constructor(const char* s, unsigned int)", s03, "abc");
    NPT_String s04('Z');
    StringTest("constructor(char)", s04, "Z");
    NPT_String s05('Z', 7);
    StringTest("constructor(char, unsigned int)", s05, "ZZZZZZZ");
    NPT_String s06((const char*)NULL);
    StringTest("constructor(NULL)", s06, "");
    NPT_String s07(s06);
    StringTest("constructor(const NPT_String& = empty)", s07, "");
    NPT_String s08("");
    StringTest("constructor(const char* = \"\")", s08, "");
    NPT_String s09("jkhlkjh\0fgsdfg\0fgsdfg", 10);
    StringTest("NPT_String s09(\"jkhlkjh\0fgsdfg\0fgsdfg\", 0, 10)", s09, "jkhlkjh");
    NPT_String s10((const char*)NULL, 0);
    StringTest("NPT_String s10(NULL, 0)", s10, "");
    NPT_String s11(' ', 0);
    StringTest("NPT_String s11(' ', 0)", s11, "");
    
    printf(":: testing assignments\n");
    NPT_String a00 = (const char*)NULL;
    StringTest("operator=(const char* = NULL)", a00, "");
    NPT_String a01 = a00;
    StringTest("operator=(const NPT_String& = empty)", a01, "");
    NPT_String a02 = "ab";
    StringTest("operator=(const char*)", a02, "ab");
    a02 = "abc";
    StringTest("operator=(const char* = bigger string)", a02, "abc");
    a02 = "ab";
    StringTest("operator=(const char* = smaller)", a02, "ab");
    a02 = (const char*)NULL;
    StringTest("operator=(const char* = NULL)", a02, "");
    a02 = "abcd";
    NPT_String a03 = a02;
    a02 = "ab";
    StringTest("operator=(const char*) with shared buffer", a02, "ab");
    a02 = "";
    StringTest("operator=(const char* = \"\")", a02, "");
    NPT_String p2("self");
    p2 = p2;
    StringTest("self assignment", p2, "self");
    NPT_String p3 = p2;
    p2 = p2;
    StringTest("self assignment with other ref", p2, "self");

    printf(":: testing SetLength()\n");
    NPT_String sl00;
    IntTest("", sl00.SetLength(0), NPT_SUCCESS);
    IntTest("", sl00.SetLength(3, true), NPT_SUCCESS);
    StringTest("", sl00, "   ");
    sl00.Assign("blabla", 6);
    IntTest("", sl00.SetLength(7, true), NPT_SUCCESS);
    StringTest("", sl00, "blabla ");
    IntTest("", sl00.SetLength(3), NPT_SUCCESS);
    StringTest("", sl00, "bla");
    IntTest("", sl00.SetLength(0), NPT_SUCCESS);
    StringTest("", sl00, "");
    
    printf(":: testing casts\n");
    s = "hello";
    printf(":: cast to char*\n");
    StringTest("cast to char*", (char*)s, "hello");
    StringTest("cast to const char*", (const char*)s, "hello");
    
    printf(":: testing GetLength\n");
    NPT_String gl0 = "abcefg";
    IntTest("GetLength", gl0.GetLength(), 6);
    gl0 = "";
    IntTest("GetLength", gl0.GetLength(), 0);
    gl0 = "abcd";
    NPT_String gl1 = gl0;
    IntTest("GetLength", gl1.GetLength(), 4);
    gl1 += 'd';
    IntTest("GetLength", gl1.GetLength(), 5);

    printf("::testing references\n");
    NPT_String* d1;
    NPT_String d2;
    NPT_String d3;
    d1 = new NPT_String("first ref");
    d2 = *d1;
    delete d1;
    d1 = NULL;
    printf("%s", d2.GetChars());
    d3 = d2;
    d3 = "d3";
    printf("%s", d2.GetChars());
    printf("%s", d3.GetChars());

    printf("::testing Append\n");
    NPT_String l = "blabla";
    l.Append("blibliblo", 6);
    StringTest("append(const char*, int size)", l, "blablablibli");
    NPT_String a;
    a.Append("bloblo", 3);
    StringTest("append to NULL", a, "blo");

    printf("::testing Reserve\n");
    NPT_String r = "123";
    r.Reserve(100);
    IntTest("size of string not changed", 3, r.GetLength());
    r += "4";
    r += "5";
    r += "6";
    NPT_String r2 = r; // make a new reference
    r += "7";
    r += "8";
    r2 += "a";
    r2 += "b";
    StringTest("string r not changed", r, "12345678");
    StringTest("string r2 not changed", r2, "123456ab");
    NPT_String rr0 = "hello";
    rr0.Reserve(0);
    StringTest("string rr0 not changed", rr0, "hello");
    rr0.Reserve(100);
    StringTest("string rr0 not changed", rr0, "hello");

    printf(":: testing substring");
    NPT_String sup("abcdefghijklmnopqrstub");
    NPT_String sub = sup.SubString(0, 2);
    StringTest("substring [0,2] of 'abcdefghijklmnopqrstub'", sub, "ab");
    sub = sup.SubString(3, 4);
    StringTest("substring [3,4] of 'abcdefghijklmnopqrstub'", sub, "defg");
    sub = sup.SubString(100, 5);
    StringTest("substring [100,5] of 'abcdefghijklmnopqrstub'", sub, "");
    sub = sup.SubString(8,100);
    StringTest("substring [8,100] of 'abcdefghijklmnopqrstub'", sub, "ijklmnopqrstub");
    printf(":: decl NPT_String sub2(p2, 1, 2);\n");

    printf(":: testing trims");
    NPT_String trim = "*&##just this$&**";
    trim.TrimLeft('*');
    StringTest("TrimLeft('*') of '*&##just this$&**'", trim, "&##just this$&**");
    trim.TrimLeft("*&##");
    StringTest("TrimLeft('&*##')", trim, "just this$&**");
    trim.TrimRight('*');
    StringTest("TrimRight('*')", trim, "just this$&");
    trim.TrimRight("*&##");
    StringTest("TrimRight('*&##')", trim, "just this$");
    trim = "*&##just this$&**";
    trim.Trim("$&*#");
    StringTest("Trim('$&*#') of '*&##just this$&**'", trim, "just this");
    trim = "\r\njust this\t   \r\n";
    trim.Trim();
    StringTest("Trim() of '\\r\\njust this\\t   \\r\\n'", trim, "just this");
    trim = "*&##just this$&**";
    trim.Trim('*');
    StringTest("", trim, "&##just this$&");
    
    printf(":: testing operator+=(NPT_String&)\n");
    NPT_String o1 = "hello";
    NPT_String o2 = ", gilles";
    o1 += o2;
    StringTest("operator +=", o1, "hello, gilles");
    o1 += ", some more";
    StringTest("operator +=", o1, "hello, gilles, some more");

    o1 = "abc";
    o1 += '#';
    StringTest("operator+=(char)", o1, "abc#");

    o1 = "hello";
    o2 = ", gilles";
    NPT_String o3 = o1+o2;
    StringTest("operator+(NPT_String&, NPT_String&)", o3, "hello, gilles");
    o3 = o1+", gilles";
    StringTest("operator+(NPT_String&, const char*)", o3, "hello, gilles");
    o3 = "I say:"+o1;
    StringTest("operator+(const char*, NPT_String&)", o3, "I say:hello");
    o3 = NPT_String("one, ") + "two";
    StringTest("NPT_String(\"one, \") + \"two\";", o3, "one, two");

    printf(":: testing operator[]\n");
    o1 = "abcdefgh";
    IntTest("o1[0]", 'a', o1[0]);
    IntTest("o1[1]", 'b', o1[1]);
    IntTest("o1[2]", 'c', o1[2]);
    o1[0] = '7';
    IntTest("o1[0]", '7', o1[0]);

    printf(":: testing operator comparisons\n");
    CompareTest(">",  "abc", "abc", NPT_String("abc") >  "abc", 0);
    CompareTest(">=", "abc", "abc", NPT_String("abc") >= "abc", 1);
    CompareTest("==", "abc", "abc", NPT_String("abc") == "abc", 1);
    CompareTest("!=", "abc", "abc", NPT_String("abc") != "abc", 0);
    CompareTest("<",  "abc", "abc", NPT_String("abc") <  "abc", 0);
    CompareTest("<=", "abc", "abc", NPT_String("abc") <= "abc", 1);

    CompareTest(">",  "abc", "ab", NPT_String("abc") >  "ab", 1);
    CompareTest(">=", "abc", "ab", NPT_String("abc") >= "ab", 1);
    CompareTest("==", "abc", "ab", NPT_String("abc") == "ab", 0);
    CompareTest("!=", "abc", "ab", NPT_String("abc") != "ab", 1);
    CompareTest("<",  "abc", "ab", NPT_String("abc") <  "ab", 0);
    CompareTest("<=", "abc", "ab", NPT_String("abc") <= "ab", 0);

    CompareTest(">",  "ab", "abc", NPT_String("ab") >  "abc", 0);
    CompareTest(">=", "ab", "abc", NPT_String("ab") >= "abc", 0);
    CompareTest("==", "ab", "abc", NPT_String("ab") == "abc", 0);
    CompareTest("!=", "ab", "abc", NPT_String("ab") != "abc", 1);
    CompareTest("<",  "ab", "abc", NPT_String("ab") <  "abc", 1);
    CompareTest("<=", "ab", "abc", NPT_String("ab") <= "abc", 1);

    CompareTest(">",  "bc", "abc", NPT_String("bc") >  "abc", 1);
    CompareTest(">=", "bc", "abc", NPT_String("bc") >= "abc", 1);
    CompareTest("==", "bc", "abc", NPT_String("bc") == "abc", 0);
    CompareTest("!=", "bc", "abc", NPT_String("bc") != "abc", 1);
    CompareTest("<",  "bc", "abc", NPT_String("bc") <  "abc", 0);
    CompareTest("<=", "bc", "abc", NPT_String("bc") <= "abc", 0);

    CompareTest(">",  "abc", "bc", NPT_String("abc") >  "bc", 0);
    CompareTest(">=", "abc", "bc", NPT_String("abc") >= "bc", 0);
    CompareTest("==", "abc", "bc", NPT_String("abc") == "bc", 0);
    CompareTest("!=", "abc", "bc", NPT_String("abc") != "bc", 1);
    CompareTest("<",  "abc", "bc", NPT_String("abc") <  "bc", 1);
    CompareTest("<=", "abc", "bc", NPT_String("abc") <= "bc", 1);

    printf(":: testing Compare\n");
    CompareTest("cnc", "abc", "abc", NPT_String("abc").Compare("abc", true), 0);
    CompareTest("cnc", "AbC3", "aBC3", NPT_String("AbC3").Compare("aBC3", true), 0);
    CompareTest("cnc", "AbCc", "aBcD", NPT_String("AbCc").Compare("aBcD", true), -1);
    CompareTest("cnc", "AbCC", "aBcd", NPT_String("AbCC").Compare("aBcd", true), -1);
    CompareTest("cnc", "bbCc", "aBcc", NPT_String("bbCc").Compare("aBcc", true), 1);
    CompareTest("cnc", "BbCC", "aBcc", NPT_String("BbCC").Compare("aBcc", true), 1);
    CompareTest("cnc", "AbCC", "aBcd", NPT_String("AbCC").CompareN("aBcd", 4, true), -1);
    CompareTest("cnc", "AbCC", "aBcd", NPT_String("AbCC").CompareN("aBcd", 5, true), -1);
    CompareTest("cnc", "AbCC", "aBcd", NPT_String("AbCC").CompareN("aBcd", 3, true), 0);

    printf(":: testing MakeLowercase\n");
    NPT_String lower = "abcdEFGhijkl";
    lower.MakeLowercase();
    EqualTest("MakeLowercase (noref)", "abcdEFGhijkl", lower, "abcdefghijkl");
    lower = "abcdEFGhijkl";
    NPT_String lower2 = lower;
    lower2.MakeLowercase();
    EqualTest("MakeLowercase (ref)", "abcdEFGhijkl", lower2, "abcdefghijkl");

    printf(":: testing MakeUppercase\n");
    NPT_String upper = "abcdEFGhijkl";
    upper.MakeUppercase();
    EqualTest("MakeUppercase (noref)", "abcdEFGhijkl", upper, "ABCDEFGHIJKL");
    upper = "abcdEFGhijkl";
    NPT_String upper2 = upper;
    upper2.MakeUppercase();
    EqualTest("MakeUppercase (ref)", "abcdEFGhijkl", upper2, "ABCDEFGHIJKL");

    printf(":: testing ToLowercase\n");
    lower = "abcdEFGhijkl";
    EqualTest("ToLowercase", "abcdEFGhijkl", lower.ToLowercase(), "abcdefghijkl");

    printf(":: testing ToUppercase\n");
    upper = "abcdEFGhijkl";
    EqualTest("ToUppercase", "abcdEFGhijkl", lower.ToUppercase(), "ABCDEFGHIJKL");

    printf(":: testing Find (s=\"au clair de la lune\")\n");
    s = "au clair de la lune";
    int f = s.Find("au");
    IntTest("Find(\"au\")", f, 0);
    f = s.Find("clair");
    IntTest("Find(\"clair\")", f, 3);
    f = s.Find("luneb");
    IntTest("Find(\"luneb\")", f, -1);
    f = s.Find((const char*)NULL);
    IntTest("Find(NULL)", f, -1);
    f = s.Find("hello");
    IntTest("Find(\"hello\")", f, -1);
    f = s.Find("");
    IntTest("Find(\"\")", f, 0);
    f = s.Find("clair", 2);
    IntTest("Find(\"clair\", 2)", f, 3);
    f = s.Find("clair", 100);
    IntTest("Find(\"clair\", 100)", f, -1);
    f = s.Find("cloir");
    IntTest("Find(\"cloir\")", f, -1);
    f = s.Find("au clair de la lune");
    IntTest("Find(\"au clair de la lune\")", f, 0);
    f = s.Find("au clair de la lune mon ami");
    IntTest("Find(\"au clair de la lune mon ami\")", f, -1);
    f = s.Find('c');
    IntTest("Find('c')", f, 3);
    NPT_String s1;
    f = s1.Find("hello");
    IntTest("Find() in empty string", f, -1);
    f = s.Find("Clair De La Lune", 0, true);
    IntTest("s.Find(\"Clair De La Lune\"", f, 3);
    f = s.Find('z');
    IntTest("", f, -1);
    f = s.Find('a', 1);
    IntTest("", f, 5);
    f = s.Find('C', 0, true);
    IntTest("", f, 3);
    
    printf(":: testing ReverseFind\n");
    s = "aabbccaa";
    f = s.ReverseFind("a");
    IntTest("", f, 7);
    f = s.ReverseFind("a", 1);
    IntTest("", f, 6);
    f = s.ReverseFind("a", 9);
    IntTest("", f, -1);
    f = s.ReverseFind("aab");
    IntTest("", f, 0);
    f = s.ReverseFind((const char*)NULL);
    IntTest("", f, -1);
    f = s.ReverseFind("");
    IntTest("", f, -1);
    f = s.ReverseFind("aa", 1);
    IntTest("", f, 0);
    f = s.ReverseFind("aabbccaa");
    IntTest("", f, 0);
    f = s.ReverseFind("aabbccaaa");
    IntTest("", f, -1);
    f = s.ReverseFind("zz");
    IntTest("", f, -1);
    f = s.ReverseFind('z');
    IntTest("", f, -1);
    f = s.ReverseFind('b');
    IntTest("", f, 3);
    f = s.ReverseFind('a', 2);
    IntTest("", f, 1);
    f = s.ReverseFind('B', 0, true);
    IntTest("", f, 3);
    f = s.ReverseFind('B');
    IntTest("", f, -1);
    
    printf(":: testing StartsWith\n");
    bool b = s.StartsWith("");
    IntTest("", b, 1);
    b = s.StartsWith("aaba");
    IntTest("", b, 0);
    b = s.StartsWith("aabbccaaa");
    IntTest("", b, 0);
    b = s.StartsWith("aabb");
    IntTest("", b, 1);
    b = s.StartsWith("AaB", true);
    IntTest("", b, 1);
    b = s.StartsWith("AaB");
    IntTest("", b, 0);

    printf(":: testing EndsWith\n");
    b = s.EndsWith("");
    IntTest("", b, 1);
    b = s.EndsWith("aaba");
    IntTest("", b, 0);
    b = s.EndsWith("aabbccaaa");
    IntTest("", b, 0);
    b = s.EndsWith("ccaa");
    IntTest("", b, 1);
    b = s.EndsWith("CcAa", true);
    IntTest("", b, 1);
    b = s.EndsWith("CcAa");
    IntTest("", b, 0);

    printf(":: testing Replace\n");
    NPT_String r0 = "abcdefghijefe";
    r0.Replace('e','@');
    StringTest("Replace(char, char)", r0, "abcd@fghij@f@");
    NPT_String r1 = r0;
    r1.Replace('@', '#');
    StringTest("Replace(char, char)", r1, "abcd#fghij#f#");
    r2 = "blablabla";
    r2.Replace("bla", "blu");
    StringTest("Replace(str, str)", r2, "blublublu");
    r2 = "abcdefxxxxijxxxx0";
    r2.Replace("xxxx", "y");
    StringTest("Replace(str, str)", r2, "abcdefyijy0");
    r2 = "abcdefxijx0";
    r2.Replace("x", "yyyyyy");
    StringTest("Replace(str, str)", r2, "abcdefyyyyyyijyyyyyy0");
    
    
    printf(":: testing Insert\n");
    NPT_String in0;
    in0.Insert("hello", 1);
    StringTest("Insert into NULL, past end", in0, "");
    in0.Insert("hello");
    StringTest("Insert into NULL, at start", in0, "hello");
    in0.Insert("yoyo");
    StringTest("Insert at start", in0, "yoyohello");
    in0.Insert("yaya", 3);
    StringTest("Insert at 3", in0, "yoyyayaohello");

    printf(":: testing Erase\n");
    NPT_String er0;
    er0.Erase(0, 0);
    StringTest("1", er0, "");
    er0.Erase(0, 1);
    StringTest("1", er0, "");
    er0.Erase(1, 1);
    StringTest("1", er0, "");
    er0 = "hello world";
    er0.Erase(0, 1);
    StringTest("1", er0, "ello world");
    er0.Erase(4);
    StringTest("1", er0, "elloworld");
    er0.Erase(7, 3);
    StringTest("1", er0, "ellowor");
    er0.Erase(5, 2);
    StringTest("1", er0, "ellow");
    er0.Erase(0, 5);
    StringTest("1", er0, "");

    printf(":: testing ToInteger");
    NPT_String ti00("123");
    unsigned int ul00;
    int          l00;
    IntTest("", ti00.ToInteger(ul00), NPT_SUCCESS);
    IntTest("", ul00, 123);
    IntTest("", ti00.ToInteger(l00), NPT_SUCCESS);
    IntTest("", l00, 123);
    ti00 = "123ggds";
    IntTest("", ti00.ToInteger(l00, false), NPT_ERROR_INVALID_PARAMETERS);
    IntTest("", ti00.ToInteger(l00, true), NPT_SUCCESS);
    IntTest("", l00, 123);
    ti00 = "-123";
    IntTest("", ti00.ToInteger(ul00, false), NPT_ERROR_INVALID_PARAMETERS);
    IntTest("", ti00.ToInteger(l00), NPT_SUCCESS);
    IntTest("", l00, -123);
    
    printf(":: testing ToFloat");
    NPT_String tf00("-1.234flo");
    float fl00;
    IntTest("", tf00.ToFloat(fl00, true), NPT_SUCCESS);
    FloatTest("", fl00, -1.234f);
    IntTest("", tf00.ToFloat(fl00, false), NPT_ERROR_INVALID_PARAMETERS);
    
    
    NPT_List<NPT_String> sl;
    sl = NPT_String("").Split("");
    IntTest("", sl.GetItemCount(), 1);
    StringTest("", *sl.GetFirstItem(), "");
    
    sl = NPT_String("").Split("#");
    IntTest("", sl.GetItemCount(), 1);
    StringTest("", *sl.GetFirstItem(), "");

    sl = NPT_String("aaa").Split("");
    IntTest("", sl.GetItemCount(), 1);
    StringTest("", *sl.GetFirstItem(), "aaa");

    sl = NPT_String("aaa").Split("b");
    IntTest("", sl.GetItemCount(), 1);
    StringTest("", *sl.GetFirstItem(), "aaa");

    sl = NPT_String("aaa").Split("a");
    IntTest("", sl.GetItemCount(), 4);
    NPT_String* sli;
    sl.Get(0, sli);
    StringTest("", *sli, "");
    sl.Get(1, sli);
    StringTest("", *sli, "");
    sl.Get(2, sli);
    StringTest("", *sli, "");
    sl.Get(3, sli);
    StringTest("", *sli, "");
    
    sl = NPT_String("aaa").Split("aa");
    IntTest("", sl.GetItemCount(), 2);
    sl.Get(0, sli);
    StringTest("", *sli, "");
    sl.Get(1, sli);
    StringTest("", *sli, "a");

    sl = NPT_String("aaa").Split("aaa");
    IntTest("", sl.GetItemCount(), 2);
    sl.Get(0, sli);
    StringTest("", *sli, "");
    sl.Get(1, sli);
    StringTest("", *sli, "");

    sl = NPT_String("a;b;c;d;e").Split(";");    
    IntTest("", sl.GetItemCount(), 5);
    sl.Get(0, sli);
    StringTest("", *sli, "a");
    sl.Get(1, sli);
    StringTest("", *sli, "b");
    sl.Get(2, sli);
    StringTest("", *sli, "c");
    sl.Get(3, sli);
    StringTest("", *sli, "d");
    sl.Get(4, sli);
    StringTest("", *sli, "e");
    
    NPT_String sf = NPT_String::Format("%s.%d", "hello", 3);
    StringTest("", "hello.3", sf.GetChars());
    for (unsigned int i=0; i<10; i++) {
        sf = NPT_String::Format("%s%s", sf.GetChars(), sf.GetChars());
    }
    IntTest("", sf.GetLength(), (1<<10)*7);
    
    
    NPT_LargeSize lu1=2000000;
    NPT_LargeSize lu2=2000002;
    NPT_String range = NPT_String::Format("bytes=%lu-%lu", (long)lu1, (long)lu2);
    StringTest("", "bytes=2000000-2000002", range.GetChars());
    
    char s_buf[7];
    s_buf[5] = 'a';
    NPT_CopyString(s_buf, "hello");
    StringTest("", s_buf, "hello");
    s_buf[5] = 'a';
    NPT_CopyStringN(s_buf, "hello", 6);
    StringTest("", s_buf, "hello");
    s_buf[5] = 'a';
    NPT_CopyStringN(s_buf, "hello", 5);
    StringTest("", s_buf, "hello");
    s_buf[5] = 'a';
    NPT_CopyStringN(s_buf, "hello", 4);
    StringTest("", s_buf, "hell");
    
    NPT_String hs1 = "curds and whey";
    IntTest("", hs1.GetHash32(), 0x22d5344e);
    
    
    char buffer[6] = "abcde";
    NPT_String tr0(buffer, 5);
    IntTest("", tr0.GetLength(), 5);
    buffer[1] = 0;
    NPT_String tr1(buffer, 5);
    IntTest("", tr1.GetLength(), 1);
    buffer[0] = 0;
    NPT_String tr2(buffer, 5);
    IntTest("", tr2.GetLength(), 0);
    tr0.Assign(buffer, 5);
    IntTest("", tr0.GetLength(), 0);
    buffer[0] = 'a';
    tr0.Assign(buffer, 5);
    IntTest("", tr0.GetLength(), 1);
    
    printf("------------------------- done -----\n");
    return 0;
}
