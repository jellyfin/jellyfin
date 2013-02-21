/*****************************************************************
|
|      Lists Test Program 1
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
        printf("A::A()\n");
        A_Count++;
    }
    A(int a, char b) : _a(a), _b(b), _c(&_a) {
        printf("A::A(%d, %d)\n", a, b);
        A_Count++;
    }
    A(const A& other) : _a(other._a), _b(other._b), _c(&_a) {
        printf("A::A(copy: a=%d, b=%d)\n", _a, _b);
        A_Count++;
    }
    ~A() {
        printf("A::~A(), a=%d, b=%d\n", _a, _b);
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

static int ApplyCounter = 0;
class Test1 {
public: 
    NPT_Result operator()(const A& a) const {
        ApplyCounter++;
        A aa(3,4);
        if (a == aa) return NPT_ERROR_OUT_OF_MEMORY;
        return NPT_SUCCESS;
    }
};

#define CHECK(x) {                                  \
    if (!(x)) {                                     \
        printf("TEST FAILED line %d\n", __LINE__);  \
        return 1;                                   \
    }                                               \
}

/*----------------------------------------------------------------------
|       compare
+---------------------------------------------------------------------*/
static int
compare(NPT_UInt32 a, NPT_UInt32 b)
{
    return (a==b)?0:(a<b)?-1:1;
}

/*----------------------------------------------------------------------
|       SortTest
+---------------------------------------------------------------------*/
static int
SortTest()
{
    for (unsigned int i=0; i<10000; i++) {
        unsigned int list_size = 1+NPT_System::GetRandomInteger()%100;
        NPT_List<NPT_UInt32> list;
        for (unsigned int j=0; j<list_size; j++) {
            list.Add(NPT_System::GetRandomInteger()%(2*list_size));
        }
        CHECK(NPT_SUCCEEDED(list.Sort(compare)));
        NPT_UInt32 value = 0;
        for (unsigned int j=0; j<list_size; j++) {
            NPT_UInt32 cursor = 0;
            list.Get(j, cursor);
            CHECK(cursor >= value);
            value = cursor;
        }
    }
    
    return 0;
}

/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int /*argc*/, char** /*argv*/)
{
    if (SortTest()) return 1;
    
    NPT_List<A> a_list;
    a_list.Add(A(1,2));
    a_list.Add(A(2,3));
    a_list.Add(A(3,4));
    CHECK(a_list.GetItemCount() == 3);
    CHECK(a_list.Contains(A(2,3)));
    CHECK(!a_list.Contains(A(7,8)));
    A a;
    CHECK(NPT_SUCCEEDED(a_list.PopHead(a)));
    CHECK(a == A(1,2));
    CHECK(a_list.GetItemCount() == 2);
    CHECK(NPT_SUCCEEDED(a_list.Get(0, a)));
    CHECK(a == A(2,3));
    A* pa = NULL;
    CHECK(NPT_SUCCEEDED(a_list.Get(0,pa)));
    CHECK(pa != NULL);
    CHECK(*pa == A(2,3));
    CHECK(a_list.GetItem(1) == ++a_list.GetFirstItem());

    a_list.Clear();
    CHECK(a_list.GetItemCount() == 0);
    a_list.Insert(a_list.GetFirstItem(), A(7,9));
    CHECK(a_list.GetItemCount() == 1);
    CHECK(*a_list.GetFirstItem() == A(7,9));

    a_list.Add(A(1, 2));
    CHECK(a_list.GetItemCount() == 2);
    CHECK(A_Count == 3);
    CHECK(*a_list.GetFirstItem() == A(7,9));
    CHECK(*a_list.GetLastItem()  == A(1,2));
    
    a_list.Insert(a_list.GetLastItem(), A(3,4));
    CHECK(a_list.GetItemCount() == 3);
    CHECK(*a_list.GetLastItem() == A(1,2));

    // test ApplyUntil 
    ApplyCounter = 0;
    bool applied;
    NPT_Result result = a_list.ApplyUntil(Test1(), NPT_UntilResultEquals(NPT_ERROR_OUT_OF_MEMORY), &applied);
    CHECK(applied == true);
    CHECK(result == NPT_SUCCESS);
    CHECK(ApplyCounter == 2);

    ApplyCounter = 0;
    result = a_list.ApplyUntil(Test1(), NPT_UntilResultNotEquals(NPT_SUCCESS), &applied);
    CHECK(applied == true);
    CHECK(result == NPT_ERROR_OUT_OF_MEMORY);
    CHECK(ApplyCounter == 2);

    ApplyCounter = 0;
    result = a_list.ApplyUntil(Test1(), NPT_UntilResultEquals(NPT_FAILURE), &applied);
    CHECK(applied == false);
    CHECK(result == NPT_SUCCESS);
    CHECK(ApplyCounter == 3);

    a_list.Insert(NPT_List<A>::Iterator(NULL), A(3,4));
    CHECK(a_list.GetItemCount() == 4);
    CHECK(*a_list.GetLastItem() == A(3,4));

    a_list.Insert(a_list.GetFirstItem(), A(7,8));
    CHECK(a_list.GetItemCount() == 5);
    CHECK(*a_list.GetFirstItem() == A(7,8));

    a_list.Insert(a_list.GetItem(2), A(9,10));
    CHECK(a_list.GetItemCount() == 6);
    CHECK(*a_list.GetItem(2) == A(9,10));

    a_list.Erase(a_list.GetItem(1));
    CHECK(a_list.GetItemCount() == 5);
    CHECK(*a_list.GetItem(1) == A(9,10));
    CHECK(A_Count == 1+a_list.GetItemCount());

    NPT_List<int> i1_list;
    NPT_List<int> i2_list;
    CHECK(i1_list == i2_list);
    i1_list.Add(3);
    CHECK(i1_list != i2_list);
    CHECK(!(i1_list == i2_list));
    i2_list.Add(3);
    CHECK(i1_list == i2_list);
    i2_list.Add(4);
    CHECK(i1_list != i2_list);
    i1_list.Add(4);
    i1_list.Add(5);
    i2_list.Add(6);
    CHECK(i1_list != i2_list);
  

    // NPT_Stack test
    NPT_Stack<int> i_stack;
    int i=0;
    CHECK(NPT_FAILED(i_stack.Pop(i)));
    CHECK(NPT_FAILED(i_stack.Peek(i)));
    CHECK(NPT_SUCCEEDED(i_stack.Push(4)));
    CHECK(NPT_SUCCEEDED(i_stack.Push(5)));
    CHECK(NPT_SUCCEEDED(i_stack.Push(6)));
    CHECK(NPT_SUCCEEDED(i_stack.Pop(i)));
    CHECK(i == 6);
    CHECK(NPT_SUCCEEDED(i_stack.Peek(i)));
    CHECK(i == 5);
    CHECK(NPT_SUCCEEDED(i_stack.Pop(i)));
    CHECK(i == 5);
    CHECK(NPT_SUCCEEDED(i_stack.Pop(i)));
    CHECK(i == 4);

    return 0;
}
