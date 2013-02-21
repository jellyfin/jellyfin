/*****************************************************************
|
|      Arrays Test Program 1
|
|      (c) 2005 Gilles Boccon-Gibod
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
        //printf("A::A()\n");
        A_Count++;
    }
    A(int a, char b) : _a(a), _b(b), _c(&_a) {
        //printf("A::A(%d, %d)\n", a, b);
        A_Count++;
    }
    A(const A& other) : _a(other._a), _b(other._b), _c(&_a) {
        //printf("A::A(copy: a=%d, b=%d)\n", _a, _b);
        A_Count++;
    }
    ~A() {
        //printf("A::~A(), a=%d, b=%d\n", _a, _b);
        A_Count--;
    }
    bool Check() { return _c == &_a; }
    bool operator==(const A& other) {
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
|       main
+---------------------------------------------------------------------*/
int
main(int /*argc*/, char** /*argv*/)
{
    NPT_Result res;
    NPT_Array<int> a;
    a.Add(7);
    CHECK(a[0] == 7);

    NPT_Array<A> a_array;
    a_array.Add(A(1,2));
    a_array.Add(A(3,4));
    a_array.Reserve(100);
    a_array.Add(A(4,5));

    CHECK(A_Count == 3);
    NPT_Array<A> b_array = a_array;
    CHECK(A_Count == 6);
    CHECK(b_array.GetItemCount() == a_array.GetItemCount());
    CHECK(b_array == a_array);
    CHECK(a_array[0] == b_array[0]);
    b_array[0] = A(7,8);
    CHECK(A_Count == 6);
    CHECK(!(a_array[0] == b_array[0]));

    a_array.Resize(2);
    CHECK(A_Count == 5);
    CHECK(a_array.GetItemCount() == 2);
    b_array.Resize(5);
    CHECK(A_Count == 7);
    CHECK(b_array[4]._a == 0);
    CHECK(b_array[4]._b == 0);
    
    a_array.Resize(6, A(9,10));
    CHECK(A_Count == 11);
    CHECK(a_array.GetItemCount() == 6);
    CHECK(a_array[5] == A(9,10));

    for (NPT_Ordinal i=0; i<a_array.GetItemCount(); i++) {
        a_array[i].Check();
    }
    for (NPT_Ordinal i=0; i<b_array.GetItemCount(); i++) {
        b_array[i].Check();
    }

    res = a_array.Erase(&a_array[6]);
    CHECK(res != NPT_SUCCESS);
    a_array.Erase(&a_array[2]);
    CHECK(a_array.GetItemCount() == 5);
    CHECK(A_Count == 10);
    CHECK(a_array[4] == A(9,10));

    a_array.Insert(a_array.GetItem(1), A(3, 110), 1);
    CHECK(a_array.GetItemCount() == 6);
    CHECK(A_Count == 11);
    CHECK(a_array[1] == A(3,110));
    CHECK(a_array[5] == A(9,10));

    a_array.Erase(1, 3);
    CHECK(a_array.GetItemCount() == 3);
    CHECK(A_Count == 8);
    CHECK(a_array[2] == A(9,10));

    a_array.Insert(a_array.GetFirstItem(), A(34, 0), 4);
    CHECK(a_array.GetItemCount() == 7);
    CHECK(A_Count == 12);
    CHECK(a_array[6] == A(9,10));

    a_array.Insert(a_array.GetItem(5), A(116, 'e'), 200);
    CHECK(a_array.GetItemCount() == 207);
    CHECK(a_array[206] == A(9,10));

    a_array.Clear();
    a_array.Insert(a_array.GetFirstItem(), A(1, 'c'));
    CHECK(a_array.GetItemCount() == 1);
    CHECK(a_array[0] == A(1,'c'));
    
    a_array.Insert(a_array.GetItem(1), A(2, 'd'));
    CHECK(a_array.GetItemCount() == 2);
    CHECK(a_array[0] == A(1,'c'));
    CHECK(a_array[1] == A(2,'d'));

    NPT_Array<int>* int_array = new NPT_Array<int>(100);
    CHECK(int_array->GetItemCount() == 0);
    int_array->Add(1);
    int_array->Add(2);
    CHECK(int_array->GetItemCount() == 2);
    CHECK((*int_array)[0] == 1);
    CHECK((*int_array)[1] == 2);
    int_array->Clear();
    CHECK(int_array->GetItemCount() == 0);
    delete int_array;

    NPT_Array<A*> c_array;
    A* o = new A(3, 2);
    c_array.Add(o);
    CHECK(c_array.GetItemCount() == 1);
    for (int i=0; i<4; i++) {
        c_array.Insert(0,new A(55,'a'));
    }

    CHECK(c_array.Contains(o));
    A* a66 = new A(66, 'b');
    CHECK(!c_array.Contains(a66));
    delete a66;

    A** ai = c_array.Find(NPT_ObjectComparator<A*>(o));
    CHECK(ai);
    CHECK(**ai == *o);
    c_array.Erase(ai);
    delete o;
    CHECK(c_array.GetItemCount() == 4);

    c_array.Apply(NPT_ObjectDeleter<A>());

    NPT_Array<int> i_array;
    CHECK(NPT_SUCCEEDED(i_array.Resize(4, 0)));
    CHECK(i_array.GetItemCount() == 4);
    i_array[0] = 3;
    i_array[1] = 7;
    i_array[2] = 9;
    i_array[3] = 12;
    
    NPT_Array<int> j_array = i_array;
    CHECK(i_array == j_array);
    i_array[2] = 7;
    CHECK(i_array != j_array);
    CHECK(!(i_array == j_array));
    i_array[2] = 9;
    CHECK(i_array == j_array);
    j_array.Add(12);
    CHECK(i_array != j_array);
    CHECK(!(i_array == j_array));

    NPT_Array<int> k_array;
    k_array.Add(1);
    k_array = i_array;
    CHECK(k_array == i_array);
}
