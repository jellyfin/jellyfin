/*****************************************************************
|
|      Maps Test Program 1
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

/*----------------------------------------------------------------------
|       globals
+---------------------------------------------------------------------*/
static unsigned int A_Count = 0;

/*----------------------------------------------------------------------
|       types
+---------------------------------------------------------------------*/
class A {
public:
    A() : _a(0), _b(0), _c(&_a) {
        A_Count++;
    }
    A(int a, char b) : _a(a), _b(b), _c(&_a) {
        A_Count++;
    }
    A(const A& other) : _a(other._a), _b(other._b), _c(&_a) {
        A_Count++;
    }
    ~A() {
        A_Count--;
    }
    bool Check() { return _c == &_a; }
    bool operator==(const A& other) const {
        return _a == other._a && _b == other._b;
    }
    int _a;
    char _b;
    int* _c;
};

#define CHECK(x) {                                  \
    if (!(x)) {                                     \
        printf("TEST FAILED line %d\n", __LINE__);  \
        return 1;                                   \
    }                                               \
}

/*----------------------------------------------------------------------
|   TestPerformance
+---------------------------------------------------------------------*/
static void
TestPerformance()
{
    for (unsigned int i=1; i<10000; i += 1000) {
        NPT_TimeStamp before;
        NPT_System::GetCurrentTimeStamp(before);
        for (unsigned int j=0; j<10; j++) {
            NPT_Map<NPT_String, NPT_String> map;
            for (unsigned int k=0; k<i; k++) {
                char key[64] = "blablabliblibloublou";
                unsigned int run = NPT_System::GetRandomInteger()%8;
                for (unsigned int x=0; x<run; x++) {
                    key[x] = 'A'+NPT_System::GetRandomInteger()%32;
                }
                map[key] = "hello";
            }
        }
        NPT_TimeStamp after;
        NPT_System::GetCurrentTimeStamp(after);
        NPT_UInt64 duration = (after.ToNanos()-before.ToNanos())/10;
        printf("LinearMap insert: %d\t%d ns\t\t%d ns/item\n", i, (NPT_UInt32)duration, (NPT_UInt32)(duration/i));
    }

    for (unsigned int i=1; i<10000; i += 1000) {
        NPT_TimeStamp before;
        NPT_System::GetCurrentTimeStamp(before);
        for (unsigned int j=0; j<100; j++) {
            NPT_HashMap<NPT_String, NPT_String> map;
            for (unsigned int k=0; k<i; k++) {
                char key[64] = "blablabliblibloublou";
                unsigned int run = NPT_System::GetRandomInteger()%8;
                for (unsigned int x=0; x<run; x++) {
                    key[x] = 'A'+NPT_System::GetRandomInteger()%32;
                }
                map[key] = "hello";
            }
        }
        NPT_TimeStamp after;
        NPT_System::GetCurrentTimeStamp(after);
        NPT_UInt64 duration = (after.ToNanos()-before.ToNanos())/100;
        printf("HashMap insert: %d\t%d ns\t\t%d ns/item\n", i, (NPT_UInt32)duration, (NPT_UInt32)(duration/i));
    }

    for (unsigned int i=1; i<10000; i += 1000) {
        NPT_TimeStamp before;
        NPT_System::GetCurrentTimeStamp(before);
        for (unsigned int j=0; j<100; j++) {
            NPT_HashMap<NPT_String, NPT_String> map;
            for (unsigned int k=0; k<i; k++) {
                char key[64] = "blablabliblibloublou";
                unsigned int run = NPT_System::GetRandomInteger()%8;
                for (unsigned int x=0; x<run; x++) {
                    key[x] = 'A'+NPT_System::GetRandomInteger()%32;
                }
                map[key] = "hello";
            }
            for (unsigned int k=0; k<i; k++) {
                char key[64] = "blablabliblibloublou";
                unsigned int run = NPT_System::GetRandomInteger()%8;
                for (unsigned int x=0; x<run; x++) {
                    key[x] = 'A'+NPT_System::GetRandomInteger()%32;
                }
                map.Erase(key);
            }
        }
        NPT_TimeStamp after;
        NPT_System::GetCurrentTimeStamp(after);
        NPT_UInt64 duration = (after.ToNanos()-before.ToNanos())/100;
        printf("HashMap insert+erase: %d\t%d ns\t\t%d ns/item\n", i, (NPT_UInt32)duration, (NPT_UInt32)(duration/i));
    }
}

/*----------------------------------------------------------------------
|   TestMap
+---------------------------------------------------------------------*/
static int
TestMap()
{
    NPT_Map<NPT_String,A> a_map;
    A* a = NULL;

    CHECK(a_map.GetEntryCount() == 0);
    CHECK(a_map.HasKey("hello") == false);
    CHECK(!a_map.HasValue(A(1,2)));
    CHECK(NPT_FAILED(a_map.Get("bla", a)));
    CHECK(a == NULL);

    a_map.Put("hello", A(1,2));
    CHECK(a_map.GetEntryCount() == 1);
    CHECK(NPT_SUCCEEDED(a_map.Get("hello", a)));
    CHECK(*a == A(1,2));
    CHECK(a_map.HasKey("hello"));
    CHECK(a_map.HasValue(A(1,2)));
    CHECK(a_map["hello"] == A(1,2));
    
    CHECK(a_map["bla"] == A());
    CHECK(a_map.GetEntryCount() == 2);
    a_map["bla"] = A(3,4);
    CHECK(a_map["bla"] == A(3,4));
    CHECK(a_map.GetEntryCount() == 2);

    NPT_Map<NPT_String,A> b_map;
    b_map["hello"] = A(1,2);
    b_map["bla"] = A(3,4);
    CHECK(a_map == b_map);

    NPT_Map<NPT_String,A> c_map = a_map;
    CHECK(c_map["hello"] == a_map["hello"]);
    CHECK(c_map["bla"] == a_map["bla"]);

    CHECK(NPT_SUCCEEDED(a_map.Put("bla", A(5,6))));
    CHECK(NPT_SUCCEEDED(a_map.Get("bla", a)));
    CHECK(*a == A(5,6));
    CHECK(NPT_FAILED(a_map.Get("youyou", a)));

    b_map.Clear();
    CHECK(b_map.GetEntryCount() == 0);

    a_map["youyou"] = A(6,7);
    CHECK(NPT_FAILED(a_map.Erase("coucou")));
    CHECK(NPT_SUCCEEDED(a_map.Erase("bla")));
    CHECK(!a_map.HasKey("bla"));

    CHECK(!(a_map == c_map));
    CHECK(c_map != a_map);

    c_map = a_map;
    NPT_Map<NPT_String,A> d_map(c_map);
    CHECK(d_map == c_map);

    NPT_Map<int,int> i_map;
    i_map[5] = 6;
    i_map[6] = 7;
    i_map[9] = 0;
    CHECK(i_map[0] == 0 || i_map[0] != 0); // unknown value (will cause a valgrind warning)
    CHECK(i_map.GetEntryCount() == 4);

    NPT_Map<NPT_String,A> a1_map;
    NPT_Map<NPT_String,A> a2_map;
    a1_map["hello"] = A(1,2);
    a1_map["bla"]   = A(2,3);
    a1_map["youyou"]= A(3,4);
    a2_map["bla"]   = A(2,3);
    a2_map["youyou"]= A(3,4);
    a2_map["hello"] = A(1,2);
    CHECK(a1_map == a2_map);
    a1_map["foo"] = A(0,0);
    CHECK(a1_map != a2_map);
    a2_map["foo"] = A(0,0);
    CHECK(a1_map == a2_map);
    a2_map["foo"] = A(7,8);
    CHECK(a1_map != a2_map);
    a2_map["foo"] = A(0,0);
    a1_map["bir"] = A(0,0);
    a2_map["bar"] = A(0,0);
    CHECK(a1_map.GetEntryCount() == a2_map.GetEntryCount());
    CHECK(a1_map != a2_map);
    CHECK(!(a1_map == a2_map));
    
    NPT_Map<NPT_String, NPT_String*> p_map;
    p_map["1"] = new NPT_String("hello");
    p_map["2"] = new NPT_String("good bye");
    p_map.GetEntries().Apply(NPT_MapEntryValueDeleter<NPT_Map<NPT_String, NPT_String*>::Entry>());
    
    return 0;
}

struct Hasher {
    NPT_UInt32 operator()(const NPT_String& /*key*/) const { return 0; }
};

/*----------------------------------------------------------------------
|   TestHashMap
+---------------------------------------------------------------------*/
static int
TestHashMap()
{
    NPT_HashMap<NPT_String,A> a_map;
    A* a = NULL;

    CHECK(a_map.GetEntryCount() == 0);
    CHECK(a_map.HasKey("hello") == false);
    CHECK(!a_map.HasValue(A(1,2)));
    CHECK(NPT_FAILED(a_map.Get("bla", a)));
    CHECK(a == NULL);

    a_map.Put("hello", A(1,2));
    CHECK(a_map.GetEntryCount() == 1);
    CHECK(NPT_SUCCEEDED(a_map.Get("hello", a)));
    CHECK(*a == A(1,2));
    CHECK(a_map.HasKey("hello"));
    CHECK(a_map.HasValue(A(1,2)));
    CHECK(a_map["hello"] == A(1,2));
    
    CHECK(a_map["bla"] == A());
    CHECK(a_map.GetEntryCount() == 2);
    a_map["bla"] = A(3,4);
    CHECK(a_map["bla"] == A(3,4));
    CHECK(a_map.GetEntryCount() == 2);

    NPT_HashMap<NPT_String,A> b_map;
    b_map["hello"] = A(1,2);
    b_map["bla"] = A(3,4);
    CHECK(a_map == b_map);

    NPT_HashMap<NPT_String,A> c_map = a_map;
    CHECK(c_map["hello"] == a_map["hello"]);
    CHECK(c_map["bla"] == a_map["bla"]);

    CHECK(NPT_SUCCEEDED(a_map.Put("bla", A(5,6))));
    CHECK(NPT_SUCCEEDED(a_map.Get("bla", a)));
    CHECK(*a == A(5,6));
    CHECK(NPT_FAILED(a_map.Get("youyou", a)));

    b_map.Clear();
    CHECK(b_map.GetEntryCount() == 0);

    a_map["youyou"] = A(6,7);
    CHECK(NPT_FAILED(a_map.Erase("coucou")));
    CHECK(NPT_SUCCEEDED(a_map.Erase("bla")));
    CHECK(!a_map.HasKey("bla"));

    CHECK(!(a_map == c_map));
    CHECK(c_map != a_map);

    c_map = a_map;
    NPT_HashMap<NPT_String,A> d_map(c_map);
    CHECK(d_map == c_map);

    NPT_HashMap<int,int> i_map;
    i_map[5] = 6;
    i_map[6] = 7;
    i_map[9] = 0;
    CHECK(i_map[0] == 0 || i_map[0] != 0); // unknown value (will cause a valgrind warning)
    CHECK(i_map.GetEntryCount() == 4);

    NPT_HashMap<NPT_String,A> a1_map;
    NPT_HashMap<NPT_String,A> a2_map;
    a1_map["hello"] = A(1,2);
    a1_map["bla"]   = A(2,3);
    a1_map["youyou"]= A(3,4);
    a2_map["bla"]   = A(2,3);
    a2_map["youyou"]= A(3,4);
    a2_map["hello"] = A(1,2);
    CHECK(a1_map == a2_map);
    a1_map["foo"] = A(0,0);
    CHECK(a1_map != a2_map);
    a2_map["foo"] = A(0,0);
    CHECK(a1_map == a2_map);
    a2_map["foo"] = A(7,8);
    CHECK(a1_map != a2_map);
    a2_map["foo"] = A(0,0);
    a1_map["bir"] = A(0,0);
    a2_map["bar"] = A(0,0);
    CHECK(a1_map.GetEntryCount() == a2_map.GetEntryCount());
    CHECK(a1_map != a2_map);
    CHECK(!(a1_map == a2_map));
    
    NPT_HashMap<NPT_String, NPT_String> smap;
    for (unsigned int i=0; i<24; i++) {
        NPT_String s = NPT_String::Format("blabla%d", i);
        smap[s] = "1234";
        CHECK(smap[s] == "1234");
    }
    for (unsigned int i=0; i<24; i++) {
        NPT_String s = NPT_String::Format("blabla%d", i);
        CHECK(smap[s] == "1234");
    }
    for (unsigned int i=0; i<24; i++) {
        NPT_String s = NPT_String::Format("blabla%d", i);
        CHECK(NPT_SUCCEEDED(smap.Erase(s)));
        CHECK(!smap.HasKey(s));
    }
    CHECK(smap.GetEntryCount() == 0);
    
    Hasher hasher;
    NPT_HashMap<NPT_String, int, Hasher> zmap(hasher);
    for (unsigned int i=0; i<1024; i++) {
        NPT_String s = NPT_String::Format("blabla%d", i);
        zmap[s] = 1234;
        CHECK(zmap[s] == 1234);
    }
    for (unsigned int i=0; i<1024; i++) {
        NPT_String s = NPT_String::Format("blabla%d", i);
        CHECK(zmap[s] == 1234);
    }
    for (unsigned int i=0; i<1024; i++) {
        NPT_String s = NPT_String::Format("blabla%d", i);
        CHECK(NPT_SUCCEEDED(zmap.Erase(s)));
        CHECK(!zmap.HasKey(s));
    }
    CHECK(zmap.GetEntryCount() == 0);
    
    NPT_HashMap<NPT_String, int> imap;
    for (int i=0; i<1024; i++) {
        NPT_String s = NPT_String::Format("blabla%d", i);
        imap[s] = i;
        CHECK(imap[s] == i);
    }
    unsigned int zz = 1024;
    for (NPT_HashMap<NPT_String, int>::Iterator it = imap.GetEntries();
                                                it;
                                                ++it) {
        CHECK(imap.HasKey((*it).GetKey()));
        CHECK(imap.HasValue((*it).GetValue()));
        --zz;
    }
    CHECK(zz==0);
    
    NPT_HashMap<NPT_String, NPT_String*> p_map;
    p_map["1"] = new NPT_String("hello");
    p_map["2"] = new NPT_String("good bye");
    p_map.Apply(NPT_MapEntryValueDeleter<NPT_HashMap<NPT_String, NPT_String*>::Entry>());

    return 0;
}

/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int /*argc*/, char** /*argv*/)
{

    int result;
    
    result = TestMap();
    if (result) return result;

    result = TestHashMap();
    if (result) return result;
    
    TestPerformance();
    
    return 0;
}
