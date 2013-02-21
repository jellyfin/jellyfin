/*****************************************************************
|
|   Neptune - Arrays
|
| Copyright (c) 2002-2008, Axiomatic Systems, LLC.
| All rights reserved.
|
| Redistribution and use in source and binary forms, with or without
| modification, are permitted provided that the following conditions are met:
|     * Redistributions of source code must retain the above copyright
|       notice, this list of conditions and the following disclaimer.
|     * Redistributions in binary form must reproduce the above copyright
|       notice, this list of conditions and the following disclaimer in the
|       documentation and/or other materials provided with the distribution.
|     * Neither the name of Axiomatic Systems nor the
|       names of its contributors may be used to endorse or promote products
|       derived from this software without specific prior written permission.
|
| THIS SOFTWARE IS PROVIDED BY AXIOMATIC SYSTEMS ''AS IS'' AND ANY
| EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
| WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
| DISCLAIMED. IN NO EVENT SHALL AXIOMATIC SYSTEMS BE LIABLE FOR ANY
| DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
| (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
| LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
| ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
| (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
| SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
|
****************************************************************/

#ifndef _NPT_ARRAY_H_
#define _NPT_ARRAY_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptConfig.h"
#if defined(NPT_CONFIG_HAVE_NEW_H)
#include <new>
#endif
#include "NptTypes.h"
#include "NptResults.h"

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
const int NPT_ARRAY_INITIAL_MAX_SIZE = 128; // bytes

/*----------------------------------------------------------------------
|   NPT_Array
+---------------------------------------------------------------------*/
template <typename T> 
class NPT_Array 
{
public:
    // types
    typedef T Element;
    typedef T* Iterator;

    // methods
    NPT_Array<T>(): m_Capacity(0), m_ItemCount(0), m_Items(0) {}
    explicit NPT_Array<T>(NPT_Cardinal count);
    NPT_Array<T>(NPT_Cardinal count, const T& item);
    NPT_Array<T>(const T* items, NPT_Cardinal item_count);
   ~NPT_Array<T>();
    NPT_Array<T>(const NPT_Array<T>& copy);
    NPT_Array<T>& operator=(const NPT_Array<T>& copy);
    bool          operator==(const NPT_Array<T>& other) const;
    bool          operator!=(const NPT_Array<T>& other) const;
    NPT_Cardinal GetItemCount() const { return m_ItemCount; }
    NPT_Result   Add(const T& item);
    T& operator[](NPT_Ordinal pos)             { return m_Items[pos]; }
    const T& operator[](NPT_Ordinal pos) const { return m_Items[pos]; }
    NPT_Result   Erase(Iterator which);
    NPT_Result   Erase(NPT_Ordinal which) { return Erase(&m_Items[which]); }
    NPT_Result   Erase(Iterator first, Iterator last);
    NPT_Result   Erase(NPT_Ordinal first, NPT_Ordinal last) { return Erase(&m_Items[first], &m_Items[last]); }
    NPT_Result   Insert(Iterator where, const T& item, NPT_Cardinal count = 1);
    NPT_Result   Reserve(NPT_Cardinal count);
    NPT_Cardinal GetCapacity() const { return m_Capacity; }
    NPT_Result   Resize(NPT_Cardinal count);
    NPT_Result   Resize(NPT_Cardinal count, const T& fill);
    NPT_Result   Clear();
    bool         Contains(const T& data) const;
    Iterator     GetFirstItem() const { return m_ItemCount?&m_Items[0]:NULL; }
    Iterator     GetLastItem() const  { return m_ItemCount?&m_Items[m_ItemCount-1]:NULL; }
    Iterator     GetItem(NPT_Ordinal n) { return n<m_ItemCount?&m_Items[n]:NULL; }

    // template list operations
    // keep these template members defined here because MSV6 does not let
    // us define them later
    template <typename X> 
    NPT_Result Apply(const X& function) const
    {                                  
        for (unsigned int i=0; i<m_ItemCount; i++) function(m_Items[i]);
        return NPT_SUCCESS;
    }

    template <typename X, typename P>
    NPT_Result ApplyUntil(const X& function, const P& predicate, bool* match = NULL) const
    {                                  
        for (unsigned int i=0; i<m_ItemCount; i++) {
            NPT_Result return_value;
            if (predicate(function(m_Items[i]), return_value)) {
                if (match) *match = true;
                return return_value;
            }
        }
        if (match) *match = false;
        return NPT_SUCCESS;
    }

    template <typename X> 
    T* Find(const X& predicate, NPT_Ordinal n=0, NPT_Ordinal* pos = NULL) const
    {
        if (pos) *pos = -1;

        for (unsigned int i=0; i<m_ItemCount; i++) {
            if (predicate(m_Items[i])) {
                if (pos) *pos = i;
                if (n == 0) return &m_Items[i];
                --n;
            }
        }
        return NULL;
    }

protected:
    // methods
    T* Allocate(NPT_Cardinal count, NPT_Cardinal& allocated);

    // members
    NPT_Cardinal m_Capacity;
    NPT_Cardinal m_ItemCount;
    T*           m_Items;
};

/*----------------------------------------------------------------------
|   NPT_Array<T>::NPT_Array<T>
+---------------------------------------------------------------------*/
template <typename T>
inline
NPT_Array<T>::NPT_Array(NPT_Cardinal count) :
    m_Capacity(0),
    m_ItemCount(0),
    m_Items(0)
{
    Reserve(count);
}

/*----------------------------------------------------------------------
|   NPT_Array<T>::NPT_Array<T>
+---------------------------------------------------------------------*/
template <typename T>
inline
NPT_Array<T>::NPT_Array(const NPT_Array<T>& copy) :
    m_Capacity(0),
    m_ItemCount(0),
    m_Items(0)
{
    Reserve(copy.GetItemCount());
    for (NPT_Ordinal i=0; i<copy.m_ItemCount; i++) {
        new ((void*)&m_Items[i]) T(copy.m_Items[i]);
    }
    m_ItemCount = copy.m_ItemCount;
}

/*----------------------------------------------------------------------
|   NPT_Array<T>::NPT_Array<T>
+---------------------------------------------------------------------*/
template <typename T>
inline
NPT_Array<T>::NPT_Array(NPT_Cardinal count, const T& item) :
    m_Capacity(0),
    m_ItemCount(count),
    m_Items(0)    
{
    Reserve(count);
    for (NPT_Ordinal i=0; i<count; i++) {
        new ((void*)&m_Items[i]) T(item);
    }
}

/*----------------------------------------------------------------------
|   NPT_Array<T>::NPT_Array<T>
+---------------------------------------------------------------------*/
template <typename T>
inline
NPT_Array<T>::NPT_Array(const T* items, NPT_Cardinal item_count) :
    m_Capacity(0),
    m_ItemCount(item_count),
    m_Items(0)    
{
    Reserve(item_count);
    for (NPT_Ordinal i=0; i<item_count; i++) {
        new ((void*)&m_Items[i]) T(items[i]);
    }
}

/*----------------------------------------------------------------------
|   NPT_Array<T>::~NPT_Array<T>
+---------------------------------------------------------------------*/
template <typename T>
inline
NPT_Array<T>::~NPT_Array()
{
    // remove all items
    Clear();

    // free the memory
    ::operator delete((void*)m_Items);
}

/*----------------------------------------------------------------------
|   NPT_Array<T>::operator=
+---------------------------------------------------------------------*/
template <typename T>
NPT_Array<T>&
NPT_Array<T>::operator=(const NPT_Array<T>& copy)
{
    // do nothing if we're assigning to ourselves
    if (this == &copy) return *this;

    // destroy all elements
    Clear();

    // copy all elements from the other object
    Reserve(copy.GetItemCount());
    m_ItemCount = copy.m_ItemCount;
    for (NPT_Ordinal i=0; i<copy.m_ItemCount; i++) {
        new ((void*)&m_Items[i]) T(copy.m_Items[i]);
    }

    return *this;
}

/*----------------------------------------------------------------------
|   NPT_Array<T>::Clear
+---------------------------------------------------------------------*/
template <typename T>
NPT_Result
NPT_Array<T>::Clear()
{
    // destroy all items
    for (NPT_Ordinal i=0; i<m_ItemCount; i++) {
        m_Items[i].~T();
    }

    m_ItemCount = 0;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Array<T>::Allocate
+---------------------------------------------------------------------*/
template <typename T>
T*
NPT_Array<T>::Allocate(NPT_Cardinal count, NPT_Cardinal& allocated) 
{
    if (m_Capacity) {
        allocated = 2*m_Capacity;
    } else {
        // start with just enough elements to fill 
        // NPT_ARRAY_INITIAL_MAX_SIZE worth of memory
        allocated = NPT_ARRAY_INITIAL_MAX_SIZE/sizeof(T);
        if (allocated == 0) allocated = 1;
    }
    if (allocated < count) allocated = count;

    // allocate the items
    return (T*)::operator new(allocated*sizeof(T));
}

/*----------------------------------------------------------------------
|   NPT_Array<T>::Reserve
+---------------------------------------------------------------------*/
template <typename T>
NPT_Result
NPT_Array<T>::Reserve(NPT_Cardinal count)
{
    if (count <= m_Capacity) return NPT_SUCCESS;

    // (re)allocate the items
    NPT_Cardinal new_capacity;
    T* new_items = Allocate(count, new_capacity);
    if (new_items == NULL) {
        return NPT_ERROR_OUT_OF_MEMORY;
    }
    if (m_ItemCount && m_Items) {
        for (unsigned int i=0; i<m_ItemCount; i++) {
            // construct the copy
            new ((void*)&new_items[i])T(m_Items[i]);

            // destroy the item
            m_Items[i].~T();
        }
    }
    ::operator delete((void*)m_Items);
    m_Items = new_items;
    m_Capacity = new_capacity;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Array<T>::Add
+---------------------------------------------------------------------*/
template <typename T>
inline
NPT_Result
NPT_Array<T>::Add(const T& item)
{
    // ensure capacity
    NPT_Result result = Reserve(m_ItemCount+1);
    if (result != NPT_SUCCESS) return result;

    // store the item
    new ((void*)&m_Items[m_ItemCount++]) T(item);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Array<T>::Erase
+---------------------------------------------------------------------*/
template <typename T>
inline
NPT_Result
NPT_Array<T>::Erase(Iterator which)
{
    return Erase(which, which);
}

/*----------------------------------------------------------------------
|   NPT_Array<T>::Erase
+---------------------------------------------------------------------*/
template <typename T>
NPT_Result
NPT_Array<T>::Erase(Iterator first, Iterator last)
{
    // check parameters
    if (first == NULL || last == NULL) return NPT_ERROR_INVALID_PARAMETERS;

    // check the bounds
    NPT_Ordinal first_index = (NPT_Ordinal)(NPT_POINTER_TO_LONG(first-m_Items));
    NPT_Ordinal last_index  = (NPT_Ordinal)(NPT_POINTER_TO_LONG(last-m_Items));
    if (first_index >= m_ItemCount ||
        last_index  >= m_ItemCount ||
        first_index > last_index) {
        return NPT_ERROR_INVALID_PARAMETERS;
    }

    // shift items to the left
    NPT_Cardinal interval = last_index-first_index+1;
    NPT_Cardinal shifted = m_ItemCount-last_index-1;
    for (NPT_Ordinal i=first_index; i<first_index+shifted; i++) {
        m_Items[i] = m_Items[i+interval];
    }

    // destruct the remaining items
    for (NPT_Ordinal i=first_index+shifted; i<m_ItemCount; i++) {
        m_Items[i].~T();
    }

    // update the item count
    m_ItemCount -= interval;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Array<T>::Insert
+---------------------------------------------------------------------*/
template <typename T>
NPT_Result
NPT_Array<T>::Insert(Iterator where, const T& item, NPT_Cardinal repeat)
{
    // check bounds
    NPT_Ordinal where_index = where?((NPT_Ordinal)NPT_POINTER_TO_LONG(where-m_Items)):m_ItemCount;
    if (where > &m_Items[m_ItemCount] || repeat == 0) return NPT_ERROR_INVALID_PARAMETERS;

    NPT_Cardinal needed = m_ItemCount+repeat;
    if (needed > m_Capacity) {
        // allocate more memory
        NPT_Cardinal new_capacity;
        T* new_items = Allocate(needed, new_capacity);
        if (new_items == NULL) return NPT_ERROR_OUT_OF_MEMORY;
        m_Capacity = new_capacity;

        // move the items before the insertion point
        for (NPT_Ordinal i=0; i<where_index; i++) {
            new((void*)&new_items[i])T(m_Items[i]);
            m_Items[i].~T();
        }

        // move the items after the insertion point
        for (NPT_Ordinal i=where_index; i<m_ItemCount; i++) {
            new((void*)&new_items[i+repeat])T(m_Items[i]);
            m_Items[i].~T();
        }

        // use the new items instead of the current ones
        ::operator delete((void*)m_Items);
        m_Items = new_items;
    } else {
        // shift items after the insertion point to the right
        for (NPT_Ordinal i=m_ItemCount; i>where_index; i--) {
            new((void*)&m_Items[i+repeat-1])T(m_Items[i-1]);
            m_Items[i-1].~T();
        }
    }

    // insert the new items
    for (NPT_Cardinal i=where_index; i<where_index+repeat; i++) {
        new((void*)&m_Items[i])T(item);
    }

    // update the item count
    m_ItemCount += repeat;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Array<T>::Resize
+---------------------------------------------------------------------*/
template <typename T>
NPT_Result
NPT_Array<T>::Resize(NPT_Cardinal size)
{
    if (size < m_ItemCount) {
        // shrink
        for (NPT_Ordinal i=size; i<m_ItemCount; i++) {
            m_Items[i].~T();
        }
        m_ItemCount = size;
    } else if (size > m_ItemCount) {
        return Resize(size, T());
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Array<T>::Resize
+---------------------------------------------------------------------*/
template <typename T>
NPT_Result
NPT_Array<T>::Resize(NPT_Cardinal size, const T& fill)
{
    if (size < m_ItemCount) {
        return Resize(size);
    } else if (size > m_ItemCount) {
        Reserve(size);
        for (NPT_Ordinal i=m_ItemCount; i<size; i++) {
            new ((void*)&m_Items[i]) T(fill);
        }
        m_ItemCount = size;
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Array<T>::Contains
+---------------------------------------------------------------------*/
template <typename T>
bool
NPT_Array<T>::Contains(const T& data) const
{
    for (NPT_Ordinal i=0; i<m_ItemCount; i++) {
        if (m_Items[i] == data) return true;
    }

    return false;
}

/*----------------------------------------------------------------------
|   NPT_Array<T>::operator==
+---------------------------------------------------------------------*/
template <typename T>
bool
NPT_Array<T>::operator==(const NPT_Array<T>& other) const
{
    // we need the same number of items
    if (other.m_ItemCount != m_ItemCount) return false;

    // compare all items
    for (NPT_Ordinal i=0; i<m_ItemCount; i++) {
        if (!(m_Items[i] == other.m_Items[i])) return false;
    }

    return true;
}

/*----------------------------------------------------------------------
|   NPT_Array<T>::operator!=
+---------------------------------------------------------------------*/
template <typename T>
inline
bool
NPT_Array<T>::operator!=(const NPT_Array<T>& other) const
{
    return !(*this == other);
}

#endif // _NPT_ARRAY_H_













