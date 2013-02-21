/*****************************************************************
|
|   Neptune - Lists
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

#ifndef _NPT_LIST_H_
#define _NPT_LIST_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptResults.h"
#include "NptTypes.h"
#include "NptConstants.h"
#include "NptCommon.h"

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
const int NPT_ERROR_LIST_EMPTY              = NPT_ERROR_BASE_LIST - 0;
const int NPT_ERROR_LIST_OPERATION_ABORTED  = NPT_ERROR_BASE_LIST - 1;
const int NPT_ERROR_LIST_OPERATION_CONTINUE = NPT_ERROR_BASE_LIST - 2;

/*----------------------------------------------------------------------
|   NPT_List
+---------------------------------------------------------------------*/
template <typename T> 
class NPT_List 
{
protected:
    class Item;

public:
    // types
    typedef T Element;

    class Iterator {
    public:
        Iterator() : m_Item(NULL) {}
        explicit Iterator(Item* item) : m_Item(item) {}
        Iterator(const Iterator& copy) : m_Item(copy.m_Item) {}
        T&  operator*()  const { return m_Item->m_Data; }
        T*  operator->() const { return &m_Item->m_Data;}
        Iterator& operator++()  { // prefix
            m_Item = m_Item->m_Next;
            return (*this); 
        }
        Iterator operator++(int) { // postfix
            Iterator saved_this = *this;
            m_Item = m_Item->m_Next;
            return saved_this;
        }
        Iterator& operator--() { // prefix
            m_Item = m_Item->m_Prev;
            return (*this); 
        }
        Iterator operator--(int) { // postfix
            Iterator saved_this = *this;
            m_Item = m_Item->m_Prev;
            return saved_this;
        }
        operator bool() const {
            return m_Item != NULL;
        }
        bool operator==(const Iterator& other) const {
            return m_Item == other.m_Item;
        }
        bool operator!=(const Iterator& other) const {
            return m_Item != other.m_Item;
        }
        void operator=(const Iterator& other) {
            m_Item = other.m_Item;
        }
        void operator=(Item* item) {
            m_Item = item;
        }

    private:
        Item* m_Item;

        // friends
        friend class NPT_List<T>;
    };

    // methods
                 NPT_List<T>();
                 NPT_List<T>(const NPT_List<T>& list);
                ~NPT_List<T>();
    NPT_Result   Add(const T& data);
    NPT_Result   Insert(const Iterator where, const T& data);
    NPT_Result   Remove(const T& data, bool all=false);
    NPT_Result   Erase(const Iterator position);
    NPT_Result   PopHead(T& data);
    bool         Contains(const T& data) const;
    NPT_Result   Clear();
    NPT_Result   Get(NPT_Ordinal index, T& data) const;
    NPT_Result   Get(NPT_Ordinal index, T*& data) const;
    NPT_Cardinal GetItemCount() const { return m_ItemCount; }
    Iterator     GetFirstItem() const { return Iterator(m_Head); }
    Iterator     GetLastItem() const  { return Iterator(m_Tail); }
    Iterator     GetItem(NPT_Ordinal index) const;

    // list manipulation
    NPT_Result   Add(NPT_List<T>& list);
    NPT_Result   Remove(const NPT_List<T>& list, bool all=false);
    NPT_Result   Cut(NPT_Cardinal keep, NPT_List<T>& cut);
    
    // item manipulation
    NPT_Result   Add(Item& item);
    NPT_Result   Detach(Item& item);
    NPT_Result   Insert(const Iterator where, Item& item);

    // list operations
    // keep these template members defined here because MSV6 does not let
    // us define them later
    template <typename X> 
    NPT_Result Apply(const X& function) const
    {                          
        Item* item = m_Head;
        while (item) {
            function(item->m_Data);
            item = item->m_Next;
        }

        return NPT_SUCCESS;
    }

    template <typename X, typename P> 
    NPT_Result ApplyUntil(const X& function, const P& predicate, bool* match = NULL) const
    {                          
        Item* item = m_Head;
        while (item) {
            NPT_Result return_value;
            if (predicate(function(item->m_Data), return_value)) {
                if (match) *match = true;
                return return_value;
            }
            item = item->m_Next;
        }
        
        if (match) *match = false;
        return NPT_SUCCESS;
    }

    template <typename P> 
    Iterator Find(const P& predicate, NPT_Ordinal n=0) const
    {
        Item* item = m_Head;
        while (item) {
            if (predicate(item->m_Data)) {
                if (n == 0) {
                    return Iterator(item);
                }
                --n;
            }
            item = item->m_Next;
        }

        return Iterator(NULL);
    }

    // Merge sort algorithm
    // http://en.wikipedia.org/wiki/Mergesort
    template <typename X> 
    NPT_Result Sort(const X& function)
    {   
        if (GetItemCount() <= 1) return NPT_SUCCESS;
        
        NPT_List<T> right;
        NPT_CHECK(Cut(GetItemCount() >> 1, right));
        
        // Sort ourselves again
        Sort(function);
        
        // sort the right side
        right.Sort(function);
        
        // merge the two back inline
        if (function(m_Tail->m_Data, right.m_Head->m_Data) > 0) {
            Merge(right, function);
        } else {
            // append right
            right.m_Head->m_Prev = m_Tail;
            if (m_Tail) m_Tail->m_Next = right.m_Head;
            if (!m_Head) m_Head = right.m_Head;
            m_Tail = right.m_Tail;
            m_ItemCount += right.m_ItemCount;
            
            right.m_ItemCount = 0;
            right.m_Head = right.m_Tail = NULL;
        }
        
        return NPT_SUCCESS;
    }

    template <typename X> 
    NPT_Result Merge(NPT_List<T>& other, const X& function) 
    {
        Iterator left = GetFirstItem();
        Iterator right;
        while (left && other.m_Head) {
            if (function(*left, other.m_Head->m_Data) <= 0) {
                ++left;
            } else {
                // remove head and insert it
                Item* head = other.m_Head;
                other.Detach(*head);
                Insert(left, *head);
            }
        }
        
        // add what's left of other if any
        if (other.m_Head) {
            other.m_Head->m_Prev = m_Tail;
            if (m_Tail) m_Tail->m_Next = other.m_Head;
            m_Tail = other.m_Tail;
            if (!m_Head) m_Head = other.m_Head;
            other.m_Head = other.m_Tail = NULL;
        }
        m_ItemCount += other.m_ItemCount;
        other.m_ItemCount = 0;
        return NPT_SUCCESS;
    }

    // operators
    void operator=(const NPT_List<T>& other);
    bool operator==(const NPT_List<T>& other) const;
    bool operator!=(const NPT_List<T>& other) const;

protected:
    // types
    class Item 
    {
    public:
        // methods
        Item(const T& data) : m_Next(0), m_Prev(0), m_Data(data) {}

        // members
        Item* m_Next;
        Item* m_Prev;
        T     m_Data;

        // friends
        //friend class NPT_List<T>;
        //friend class NPT_List<T>::Iterator;
    };

    // members
    NPT_Cardinal m_ItemCount;
    Item*        m_Head;
    Item*        m_Tail;
};

/*----------------------------------------------------------------------
|   NPT_List<T>::NPT_List
+---------------------------------------------------------------------*/
template <typename T>
inline
NPT_List<T>::NPT_List() : m_ItemCount(0), m_Head(0), m_Tail(0) 
{
}

/*----------------------------------------------------------------------
|   NPT_List<T>::NPT_List
+---------------------------------------------------------------------*/
template <typename T>
inline
NPT_List<T>::NPT_List(const NPT_List<T>& list) : m_ItemCount(0), m_Head(0), m_Tail(0) 
{
    *this = list;
}

/*----------------------------------------------------------------------
|   NPT_List<T>::~NPT_List<T>
+---------------------------------------------------------------------*/
template <typename T>
inline
NPT_List<T>::~NPT_List()
{
    Clear();
}
 
/*----------------------------------------------------------------------
|   NPT_List<T>::operator=
+---------------------------------------------------------------------*/
template <typename T>
void
NPT_List<T>::operator=(const NPT_List<T>& list)
{
    // cleanup
    Clear();

    // copy the new list
    Item* item = list.m_Head;
    while (item) {
        Add(item->m_Data);
        item = item->m_Next;
    }
}

/*----------------------------------------------------------------------
|   NPT_List<T>::operator==
+---------------------------------------------------------------------*/
template <typename T>
bool
NPT_List<T>::operator==(const NPT_List<T>& other) const
{
    // quick test
    if (m_ItemCount != other.m_ItemCount) return false;

    // compare all elements one by one
    Item* our_item = m_Head;
    Item* their_item = other.m_Head;
    while (our_item && their_item) {
        if (our_item->m_Data != their_item->m_Data) return false;
        our_item   = our_item->m_Next;
        their_item = their_item->m_Next;
    }
    
    return our_item == NULL && their_item == NULL;
}

/*----------------------------------------------------------------------
|   NPT_List<T>::operator!=
+---------------------------------------------------------------------*/
template <typename T>
inline
bool
NPT_List<T>::operator!=(const NPT_List<T>& other) const
{
    return !(*this == other);
}

/*----------------------------------------------------------------------
|   NPT_List<T>::Clear
+---------------------------------------------------------------------*/
template <typename T>
NPT_Result
NPT_List<T>::Clear()
{
    // delete all items
    Item* item = m_Head;
    while (item) {
        Item* next = item->m_Next;
        delete item;
        item = next;
    }

    m_ItemCount = 0;
    m_Head      = NULL;
    m_Tail      = NULL;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_List<T>::Add
+---------------------------------------------------------------------*/
template <typename T>
NPT_Result
NPT_List<T>::Add(Item& item)
{
    // add element at the tail
    if (m_Tail) {
        item.m_Prev = m_Tail;
        item.m_Next = NULL;
        m_Tail->m_Next = &item;
        m_Tail = &item;
    } else {
        m_Head = &item;
        m_Tail = &item;
        item.m_Next = NULL;
        item.m_Prev = NULL;
    }

    // one more item in the list now
    ++m_ItemCount;
 
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_List<T>::Add
+---------------------------------------------------------------------*/
template <typename T>
NPT_Result
NPT_List<T>::Add(NPT_List<T>& list)
{
    // copy the new list
    Item* item = list.m_Head;
    while (item) {
        Add(item->m_Data);
        item = item->m_Next;
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_List<T>::Add
+---------------------------------------------------------------------*/
template <typename T>
inline
NPT_Result
NPT_List<T>::Add(const T& data)
{
    return Add(*new Item(data));
}

/*----------------------------------------------------------------------
|   NPT_List<T>::GetItem
+---------------------------------------------------------------------*/
template <typename T>
typename NPT_List<T>::Iterator
NPT_List<T>::GetItem(NPT_Ordinal n) const
{
    Iterator result;
    if (n >= m_ItemCount) return result;
    
    result = m_Head;
    for (unsigned int i=0; i<n; i++) {
        ++result;
    }

    return result;
}

/*----------------------------------------------------------------------
|   NPT_List<T>::Insert
+---------------------------------------------------------------------*/
template <typename T>
inline 
NPT_Result
NPT_List<T>::Insert(Iterator where, const T&data)
{
    return Insert(where, *new Item(data));
}

/*----------------------------------------------------------------------
|   NPT_List<T>::Insert
+---------------------------------------------------------------------*/
template <typename T>
NPT_Result
NPT_List<T>::Insert(Iterator where, Item& item)
{
    // insert the item in the list
    Item* position = where.m_Item;
    if (position) {
        // insert at position
        item.m_Next = position;
        item.m_Prev = position->m_Prev;
        position->m_Prev = &item;
        if (item.m_Prev) {
            item.m_Prev->m_Next = &item;
        } else {
            // this is the new head
            m_Head = &item;
        }

        // one more item in the list now
        ++m_ItemCount;
    } else {
        // insert at tail
        return Add(item);
    }
 
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_List<T>::Erase
+---------------------------------------------------------------------*/
template <typename T>
NPT_Result
NPT_List<T>::Erase(Iterator position) 
{
    if (!position) return NPT_ERROR_NO_SUCH_ITEM;
    Detach(*position.m_Item);
    delete position.m_Item;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_List<T>::Remove
+---------------------------------------------------------------------*/
template <typename T>
NPT_Result
NPT_List<T>::Remove(const T& data, bool all)
{
    Item* item = m_Head;
    NPT_Cardinal matches = 0;

    while (item) {
        Item* next = item->m_Next;
        if (item->m_Data == data) {
            // we found a match
            ++matches;

            // detach item
            Detach(*item);
            
            // destroy the item
            delete item;

            if (!all) return NPT_SUCCESS;
        }
        item = next;
    }
 
    return matches?NPT_SUCCESS:NPT_ERROR_NO_SUCH_ITEM;
}

/*----------------------------------------------------------------------
|   NPT_List<T>::Remove
+---------------------------------------------------------------------*/
template <typename T>
NPT_Result
NPT_List<T>::Remove(const NPT_List<T>& list, bool all)
{
    Item* item = list.m_Head;
    while (item) {
        Remove(item->m_Data, all);
        item = item->m_Next;
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_List<T>::Detach
+---------------------------------------------------------------------*/
template <typename T>
NPT_Result
NPT_List<T>::Detach(Item& item)
{
    // remove item
    if (item.m_Prev) {
        // item is not the head
        if (item.m_Next) {
            // item is not the tail
            item.m_Next->m_Prev = item.m_Prev;
            item.m_Prev->m_Next = item.m_Next;
        } else {
            // item is the tail
            m_Tail = item.m_Prev;
            m_Tail->m_Next = NULL;
        }
    } else {
        // item is the head
        m_Head = item.m_Next;
        if (m_Head) {
            // item is not the tail
            m_Head->m_Prev = NULL;
        } else {
            // item is also the tail
            m_Tail = NULL;
        }
    }

    // one less item in the list now
    --m_ItemCount;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_List<T>::Get
+---------------------------------------------------------------------*/
template <typename T>
NPT_Result
NPT_List<T>::Get(NPT_Ordinal index, T& data) const
{
    T* data_pointer;
    NPT_CHECK(Get(index, data_pointer));
    data = *data_pointer;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_List<T>::Get
+---------------------------------------------------------------------*/
template <typename T>
NPT_Result
NPT_List<T>::Get(NPT_Ordinal index, T*& data) const
{
    Item* item = m_Head;

    if (index < m_ItemCount) {
        while (index--) item = item->m_Next;
        data = &item->m_Data;
        return NPT_SUCCESS;
    } else {
        data = NULL;
        return NPT_ERROR_NO_SUCH_ITEM;
    }
}

/*----------------------------------------------------------------------
|   NPT_List<T>::PopHead
+---------------------------------------------------------------------*/
template <typename T>
NPT_Result
NPT_List<T>::PopHead(T& data)
{
    // check that we have an element
    if (m_Head == NULL) return NPT_ERROR_LIST_EMPTY;

    // copy the head item's data
    data = m_Head->m_Data;

    // discard the head item
    Item* head = m_Head;
    m_Head = m_Head->m_Next;
    if (m_Head) {
        m_Head->m_Prev = NULL;
    } else {
        m_Tail = NULL;
    }
    delete head;

    // update the count
    --m_ItemCount;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_List<T>::Contains
+---------------------------------------------------------------------*/
template <typename T>
bool
NPT_List<T>::Contains(const T& data) const
{
    Item* item = m_Head;
    while (item) {
        if (item->m_Data == data) return true;
        item = item->m_Next;
    }

    return false;
}

/*----------------------------------------------------------------------
|   NPT_List<T>::Cut
+---------------------------------------------------------------------*/
template <typename T> 
NPT_Result 
NPT_List<T>::Cut(NPT_Cardinal keep, NPT_List<T>& cut) 
{
    cut.Clear();
    
    // shortcut
    if (keep >= GetItemCount()) return NPT_SUCCESS;
    
    // update new counts first
    cut.m_ItemCount = m_ItemCount-keep;
    m_ItemCount = keep;
    
    // look for the cut-point item
    Item* item = m_Head;
    while (keep--) { item = item->m_Next;}
    
    // the cut list goes from the cut-point item to the tail
    cut.m_Head = item;
    cut.m_Tail = m_Tail;
    
    // update the portion of the list we keep
    if (item == m_Head) m_Head = NULL;
    m_Tail = item->m_Prev;
    
    // update the cut list
    if (item->m_Prev) item->m_Prev->m_Next = NULL;
    item->m_Prev = NULL;
    
    return NPT_SUCCESS;
}

#endif // _NPT_LIST_H_
