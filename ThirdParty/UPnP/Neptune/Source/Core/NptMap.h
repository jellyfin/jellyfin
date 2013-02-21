/*****************************************************************
|
|   Neptune - Maps
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

#ifndef _NPT_MAP_H_
#define _NPT_MAP_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptTypes.h"
#include "NptResults.h"
#include "NptList.h"
#include "NptHash.h"

/*----------------------------------------------------------------------
|   NPT_Map
+---------------------------------------------------------------------*/
template <typename K, typename V> 
class NPT_Map 
{
public:
    // types
    class Entry {
    public:
        // constructor
        Entry(const K& key, const V& value) : m_Key(key), m_Value(value) {}
        Entry(const K& key) : m_Key(key) {}
        
        // accessors
        const K& GetKey()   const { return m_Key;   }
        const V& GetValue() const { return m_Value; }

        // operators 
        bool operator==(const Entry& other) const {
            return m_Key == other.m_Key && m_Value == other.m_Value;
        }

    protected:
        // methods
        void SetValue(const V& value) { m_Value = value; }

        // members
        K m_Key;
        V m_Value;

        // friends
        friend class NPT_Map<K,V>;
    };

    // constructors
    NPT_Map<K,V>() {}
    NPT_Map<K,V>(const NPT_Map<K,V>& copy);

    // destructor
    ~NPT_Map<K,V>();

    // methods
    NPT_Result   Put(const K& key, const V& value);
    NPT_Result   Get(const K& key, V*& value) const; // WARNING: the second parameter is a POINTER on the value type!!!
    bool         HasKey(const K& key) const { return GetEntry(key) != NULL; }
    bool         HasValue(const V& value) const;
    NPT_Result   Erase(const K& key);
    NPT_Cardinal GetEntryCount() const         { return m_Entries.GetItemCount(); }
    const NPT_List<Entry*>& GetEntries() const { return m_Entries; }
    NPT_Result   Clear();

    // operators
    V&                  operator[](const K& key);
    const NPT_Map<K,V>& operator=(const NPT_Map<K,V>& copy);
    bool                operator==(const NPT_Map<K,V>& other) const;
    bool                operator!=(const NPT_Map<K,V>& other) const;

private:
    // types
    typedef typename NPT_List<Entry*>::Iterator ListIterator;

    // methods
    Entry* GetEntry(const K& key) const;

    // members
    NPT_List<Entry*> m_Entries;
};

/*----------------------------------------------------------------------
|   NPT_Map<K,V>::NPT_Map<K,V>
+---------------------------------------------------------------------*/
template <typename K, typename V>
NPT_Map<K,V>::NPT_Map(const NPT_Map<K,V>& copy)
{
    *this = copy;
}

/*----------------------------------------------------------------------
|   NPT_Map<K,V>::~NPT_Map<K,V>
+---------------------------------------------------------------------*/
template <typename K, typename V>
NPT_Map<K,V>::~NPT_Map()
{
    // call Clear to ensure we delete all entry objects
    Clear();
}

/*----------------------------------------------------------------------
|   NPT_Map<K,V>::Clear
+---------------------------------------------------------------------*/
template <typename K, typename V>
NPT_Result
NPT_Map<K,V>::Clear()
{
    m_Entries.Apply(NPT_ObjectDeleter<Entry>());
    m_Entries.Clear();

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Map<K,V>::GetEntry
+---------------------------------------------------------------------*/
template <typename K, typename V>
typename NPT_Map<K,V>::Entry*
NPT_Map<K,V>::GetEntry(const K& key) const
{
    typename NPT_List<Entry*>::Iterator entry = m_Entries.GetFirstItem();
    while (entry) {
        if ((*entry)->GetKey() == key) {
            return *entry;
        }
        ++entry;
    }

    return NULL;
}

/*----------------------------------------------------------------------
|   NPT_Map<K,V>::Put
+---------------------------------------------------------------------*/
template <typename K, typename V>
NPT_Result
NPT_Map<K,V>::Put(const K& key, const V& value)
{
    Entry* entry = GetEntry(key);
    if (entry == NULL) {
        // no existing entry for that key, create one
        m_Entries.Add(new Entry(key, value));
    } else {
        // replace the existing entry for that key
        entry->SetValue(value);
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Map<K,V>::Get
+---------------------------------------------------------------------*/
template <typename K, typename V>
NPT_Result
NPT_Map<K,V>::Get(const K& key, V*& value) const
{
    Entry* entry = GetEntry(key);
    if (entry == NULL) {
        // no existing entry for that key
        value = NULL;
        return NPT_ERROR_NO_SUCH_ITEM;
    } else {
        // found an entry with that key
        value = &entry->m_Value;
        return NPT_SUCCESS;
    }
}

/*----------------------------------------------------------------------
|   NPT_Map<K,V>::HasValue
+---------------------------------------------------------------------*/
template <typename K, typename V>
bool
NPT_Map<K,V>::HasValue(const V& value) const
{
    ListIterator entry = m_Entries.GetFirstItem();
    while (entry) {
        if (value == (*entry)->m_Value) {
            return true;
        }
        ++entry;
    }

    return false;
}

/*----------------------------------------------------------------------
|   NPT_Map<K,V>::operator=
+---------------------------------------------------------------------*/
template <typename K, typename V>
const NPT_Map<K,V>&
NPT_Map<K,V>::operator=(const NPT_Map<K,V>& copy)
{
    // do nothing if we're assigning to ourselves
    if (this == &copy) return copy;

    // destroy all entries
    Clear();

    // copy all entries one by one
    ListIterator entry = copy.m_Entries.GetFirstItem();
    while (entry) {
        m_Entries.Add(new Entry((*entry)->GetKey(), (*entry)->GetValue()));
        ++entry;
    }

    return *this;
}

/*----------------------------------------------------------------------
|   NPT_Map<K,V>::Erase
+---------------------------------------------------------------------*/
template <typename K, typename V>
NPT_Result
NPT_Map<K,V>::Erase(const K& key)
{
    ListIterator entry = m_Entries.GetFirstItem();
    while (entry) {
        if ((*entry)->GetKey() == key) {
            delete *entry; // do this before removing the entry from the
                           // list, because Erase() will invalidate the
                           // iterator item
            m_Entries.Erase(entry);
            return NPT_SUCCESS;
        }
        ++entry;
    }

    return NPT_ERROR_NO_SUCH_ITEM;
}

/*----------------------------------------------------------------------
|   NPT_Map<K,V>::operator==
+---------------------------------------------------------------------*/
template <typename K, typename V>
bool
NPT_Map<K,V>::operator==(const NPT_Map<K,V>& other) const
{
    // quick test
    if (m_Entries.GetItemCount() != other.m_Entries.GetItemCount()) return false;

    // compare all entries to all other entries
    ListIterator entry = m_Entries.GetFirstItem();
    while (entry) {
        V* value;
        if (NPT_SUCCEEDED(other.Get((*entry)->m_Key, value))) {
            // the other map has an entry for this key, check the value
            if (!(*value == (*entry)->m_Value)) return false;
        } else {
            // the other map does not have an entry for this key
            return false;
        }
        ++entry;
    }

    return true;
}

/*----------------------------------------------------------------------
|   NPT_Map<K,V>::operator!=
+---------------------------------------------------------------------*/
template <typename K, typename V>
bool
NPT_Map<K,V>::operator!=(const NPT_Map<K,V>& other) const
{
    return !(*this == other);
}

/*----------------------------------------------------------------------
|   NPT_Map<K,V>::operator[]
+---------------------------------------------------------------------*/
template <typename K, typename V>
V&
NPT_Map<K,V>::operator[](const K& key)
{
    Entry* entry = GetEntry(key);
    if (entry == NULL) {
        // create a new "default" entry for this key
        entry = new Entry(key);
        m_Entries.Add(entry);
    }
     
    return entry->m_Value;
}

/*----------------------------------------------------------------------
|   NPT_HashMap
+---------------------------------------------------------------------*/
template <typename K, typename V, typename HF = NPT_Hash<K> > 
class NPT_HashMap 
{
public:
    // types
    class Entry {
    public:
        // constructor
        Entry(NPT_UInt32 hash_value, const K& key, const V& value) : m_HashValue(hash_value), m_Key(key), m_Value(value) {}
        Entry(NPT_UInt32 hash_value, const K& key)                 : m_HashValue(hash_value), m_Key(key) {}
        
        // accessors
        const K&   GetKey()       const { return m_Key;   }
        const V&   GetValue()     const { return m_Value; }
        NPT_UInt32 GetHashValue() const { return m_HashValue; }
        
        // operators 
        bool operator==(const Entry& other) const {
            return m_HashValue == other.m_HashValue && m_Key == other.m_Key && m_Value == other.m_Value;
        }

    protected:
        // methods
        void SetValue(const V& value) { m_Value = value; }

        // members
        NPT_UInt32 m_HashValue;
        K          m_Key;
        V          m_Value;

        // friends
        friend class NPT_HashMap<K,V,HF>;
    };

    class Iterator {
    public:
        Iterator() : m_Entry(NULL), m_Map(NULL) {}
        Iterator(Entry** entry, const NPT_HashMap<K,V,HF>* map) : m_Entry(entry), m_Map(map) {}
        Iterator(const Iterator& copy) : m_Entry(copy.m_Entry), m_Map(copy.m_Map) {}
        const Entry&  operator*()  const { return **m_Entry; }
        Iterator& operator++()  { // prefix
            if (m_Map && m_Entry) {
                do {
                    ++m_Entry;
                    if (m_Entry >= &m_Map->m_Buckets[1<<m_Map->m_BucketCountLog]) {
                        m_Entry = NULL;
                    } else {
                        if (*m_Entry) break;
                    }
                } while (m_Entry);
            }
            return (*this); 
        }
        Iterator operator++(int) { // postfix
            Iterator saved_this = *this;
            ++(*this);
            return saved_this;
        }
        operator bool() const {
            return m_Entry != NULL;
        }
        bool operator==(const Iterator& other) const {
            return m_Map == other.m_Map && m_Entry == other.m_Entry;
        }
        bool operator!=(const Iterator& other) const {
            return !(*this == other);
        }
        void operator=(const Iterator& other) {
            m_Entry = other.m_Entry;
            m_Map   = other.m_Map;
        }

    private:
        // friends
        friend class NPT_HashMap<K,V,HF>;

        // members
        Entry**                    m_Entry;
        const NPT_HashMap<K,V,HF>* m_Map;
    };

    // constructors
    NPT_HashMap<K,V,HF>();
    NPT_HashMap<K,V,HF>(const HF& hasher);
    NPT_HashMap<K,V,HF>(const NPT_HashMap<K,V,HF>& copy);

    // destructor
    ~NPT_HashMap<K,V,HF>();

    // methods
    NPT_Result   Put(const K& key, const V& value);
    NPT_Result   Get(const K& key, V*& value) const; // WARNING: the second parameter is a POINTER on the value type!!!
    bool         HasKey(const K& key) const { return GetEntry(key) != NULL; }
    bool         HasValue(const V& value) const;
    NPT_Result   Erase(const K& key);
    NPT_Cardinal GetEntryCount() const { return m_EntryCount; }
    Iterator     GetEntries() const;
    NPT_Result   Clear();
    
    // list operations
    // keep these template members defined here because MSV6 does not let
    // us define them later
    template <typename X> 
    NPT_Result Apply(const X& function) const
    {                          
        for (int i=0; i<(1<<m_BucketCountLog); i++) {
            if (m_Buckets[i]) {
                function(m_Buckets[i]);
            }
        }
        return NPT_SUCCESS;
    }

    // operators
    V&                         operator[](const K& key);
    const NPT_HashMap<K,V,HF>& operator=(const NPT_HashMap<K,V,HF>& copy);
    bool                       operator==(const NPT_HashMap<K,V,HF>& other) const;
    bool                       operator!=(const NPT_HashMap<K,V,HF>& other) const;

private:
    // methods
    Entry*     GetEntry(const K& key, NPT_UInt32* position=NULL) const;
    NPT_Result AddEntry(Entry* entry);
    void       AllocateBuckets(unsigned int count_log);
    void       AdjustBuckets(NPT_Cardinal entry_count, bool allow_shrink=false);
    
    // members
    HF           m_Hasher;
    Entry**      m_Buckets;
    NPT_Cardinal m_BucketCountLog;
    NPT_Cardinal m_EntryCount;
};

/*----------------------------------------------------------------------
|   NPT_HashMap<K,V>::NPT_HashMap
+---------------------------------------------------------------------*/
template <typename K, typename V, typename HF>
NPT_HashMap<K,V,HF>::NPT_HashMap() :
    m_Buckets(NULL),
    m_EntryCount(0)
{
    AllocateBuckets(4);
}

/*----------------------------------------------------------------------
|   NPT_HashMap<K,V>::NPT_HashMap
+---------------------------------------------------------------------*/
template <typename K, typename V, typename HF>
NPT_HashMap<K,V,HF>::NPT_HashMap(const HF& hasher) :
    m_Hasher(hasher),
    m_Buckets(NULL),
    m_EntryCount(0)
{
    AllocateBuckets(4);
}

/*----------------------------------------------------------------------
|   NPT_HashMap<K,V>::NPT_HashMap
+---------------------------------------------------------------------*/
template <typename K, typename V, typename HF>
NPT_HashMap<K,V,HF>::NPT_HashMap(const NPT_HashMap<K,V,HF>& copy) :
    m_Buckets(NULL),
    m_BucketCountLog(0),
    m_EntryCount(0)
{
    *this = copy;
}

/*----------------------------------------------------------------------
|   NPT_MapMap<K,V,HF>::NPT_HashMap
+---------------------------------------------------------------------*/
template <typename K, typename V, typename HF>
NPT_HashMap<K,V,HF>::~NPT_HashMap()
{
    for (int i=0; i<(1<<m_BucketCountLog); i++) {
        delete m_Buckets[i];
    }
    delete[] m_Buckets;
}

/*----------------------------------------------------------------------
|   NPT_HashMap<K,V,HF>::AllocateBuckets
+---------------------------------------------------------------------*/
template <typename K, typename V, typename HF>
void
NPT_HashMap<K,V,HF>::AllocateBuckets(unsigned int count_log)
{
    m_Buckets = new Entry*[1<<count_log];
    m_BucketCountLog = count_log;
    for (int i=0; i<(1<<count_log); i++) {
        m_Buckets[i] = NULL;
    }
}

/*----------------------------------------------------------------------
|   NPT_HashMap<K,V,HF>::AdjustBuckets
+---------------------------------------------------------------------*/
template <typename K, typename V, typename HF>
void
NPT_HashMap<K,V,HF>::AdjustBuckets(NPT_Cardinal entry_count, bool allow_shrink)
{
    Entry** buckets = NULL;
    unsigned int bucket_count = 1<<m_BucketCountLog;
    if (2*entry_count >= bucket_count) {
        // we need to grow
        buckets = m_Buckets;
        AllocateBuckets(m_BucketCountLog+1);
    } else if (allow_shrink && (5*entry_count < bucket_count) && m_BucketCountLog > 4) {
        // we need to shrink
        buckets = m_Buckets;
        AllocateBuckets(m_BucketCountLog-1);
    }
    if (buckets) {
        m_EntryCount = 0;
        for (unsigned int i=0; i<bucket_count; i++) {
            if (buckets[i]) AddEntry(buckets[i]);
        }
        delete[] buckets;
    }
}

/*----------------------------------------------------------------------
|   NPT_HashMap<K,V,HF>::Clear
+---------------------------------------------------------------------*/
template <typename K, typename V, typename HF>
NPT_Result
NPT_HashMap<K,V,HF>::Clear()
{
    if (m_Buckets) {
        for (int i=0; i<(1<<m_BucketCountLog); i++) {
            delete m_Buckets[i];
        }
        delete[] m_Buckets;
    }
    m_EntryCount = 0;
    AllocateBuckets(4);
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_HashMap<K,V,HF>::GetEntries
+---------------------------------------------------------------------*/
template <typename K, typename V, typename HF>
typename NPT_HashMap<K,V,HF>::Iterator
NPT_HashMap<K,V,HF>::GetEntries() const
{
    for (int i=0; i<(1<<m_BucketCountLog); i++) {
        if (m_Buckets[i]) {
            return Iterator(&m_Buckets[i], this);
        }
    }
    return Iterator(NULL, this);
}

/*----------------------------------------------------------------------
|   NPT_HashMap<K,V,HF>::GetEntry
+---------------------------------------------------------------------*/
template <typename K, typename V, typename HF>
typename NPT_HashMap<K,V,HF>::Entry*
NPT_HashMap<K,V,HF>::GetEntry(const K& key, NPT_UInt32* position) const
{
    NPT_UInt32 hash_value = m_Hasher(key);
    NPT_UInt32 mask       = (1<<m_BucketCountLog)-1;
    NPT_UInt32 cursor     = hash_value & mask;
    while (m_Buckets[cursor]) {
        Entry* entry = m_Buckets[cursor];
        if (entry->m_HashValue == hash_value &&
            entry->m_Key       == key) {
            if (position) *position = cursor;
            return entry;
        }
        cursor = (cursor + 1) & mask;
    }
    
    return NULL;
}

/*----------------------------------------------------------------------
|   NPT_HashMap<K,V,HF>::AddEntry
+---------------------------------------------------------------------*/
template <typename K, typename V, typename HF>
NPT_Result
NPT_HashMap<K,V,HF>::AddEntry(Entry* entry)
{
    AdjustBuckets(m_EntryCount+1);

    NPT_UInt32 hash_value = entry->m_HashValue;
    NPT_UInt32 mask       = (1<<m_BucketCountLog)-1;
    NPT_UInt32 cursor     = hash_value & mask;
    while (m_Buckets[cursor]) {
        cursor = (cursor + 1) & mask;
    }
    m_Buckets[cursor] = entry;
    ++m_EntryCount;
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_HashMap<K,V,HF>::Put
+---------------------------------------------------------------------*/
template <typename K, typename V, typename HF>
NPT_Result
NPT_HashMap<K,V,HF>::Put(const K& key, const V& value)
{
    Entry* entry = GetEntry(key);
    if (entry == NULL) {
        // no existing entry for that key, create one
        return AddEntry(new Entry(m_Hasher(key), key, value));
    } else {
        // replace the existing entry for that key
        entry->SetValue(value);
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_HashMap<K,V,HF>::Get
+---------------------------------------------------------------------*/
template <typename K, typename V, typename HF>
NPT_Result
NPT_HashMap<K,V,HF>::Get(const K& key, V*& value) const
{
    Entry* entry = GetEntry(key);
    if (entry == NULL) {
        // no existing entry for that key
        value = NULL;
        return NPT_ERROR_NO_SUCH_ITEM;
    } else {
        // found an entry with that key
        value = &entry->m_Value;
        return NPT_SUCCESS;
    }
}

/*----------------------------------------------------------------------
|   NPT_HashMap<K,V,HF>::HasValue
+---------------------------------------------------------------------*/
template <typename K, typename V, typename HF>
bool
NPT_HashMap<K,V,HF>::HasValue(const V& value) const
{
    for (int i=0; i<(1<<m_BucketCountLog); i++) {
        if (m_Buckets[i] && m_Buckets[i]->m_Value == value) {
            return true;
        }
    }

    return false;
}

/*----------------------------------------------------------------------
|   NPT_HashMap<K,V,HF>::Erase
+---------------------------------------------------------------------*/
template <typename K, typename V, typename HF>
NPT_Result
NPT_HashMap<K,V,HF>::Erase(const K& key)
{
    NPT_UInt32 position;
    Entry* entry = GetEntry(key, &position);
    if (entry == NULL) {
        return NPT_ERROR_NO_SUCH_ITEM;
    }
    
    // mark the bucket as unoccupied
    m_Buckets[position] = NULL;
    
    // look for buckets that need to be relocated:
    // there should be no empty bucket between an entry's ideal hash bucket
    // and its actual bucket.
    NPT_UInt32 mask = (1<<m_BucketCountLog)-1;
    for (NPT_UInt32 cursor = (position+1) & mask; m_Buckets[cursor]; cursor = (cursor + 1) & mask) {
        NPT_UInt32 target = m_Buckets[cursor]->m_HashValue & mask;
        // check if target is between position and cursor (modulo the bucket array size)
        // |    position.target.cursor |
        // |....cursor position.target.| or |.target..cursor position...|
        if ( (position <= cursor) ?
             ((position < target) && (target <= cursor)) :
             ((position < target) || (target <= cursor)) ) {
             continue;
        }
        
        // move the bucket back
        m_Buckets[position] = m_Buckets[cursor];
        m_Buckets[cursor] = NULL;
        position = cursor;
    }
        
    // cleanup and adjust the counter and buckets
    delete entry;
    --m_EntryCount;
    AdjustBuckets(m_EntryCount, true);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_HashMap<K,V,HF>::operator=
+---------------------------------------------------------------------*/
template <typename K, typename V, typename HF>
const NPT_HashMap<K,V,HF>&
NPT_HashMap<K,V,HF>::operator=(const NPT_HashMap<K,V,HF>& copy)
{
    // do nothing if we're assigning to ourselves
    if (this == &copy) return copy;

    // destroy all entries
    Clear();

    // prepare to receive all the entries
    AdjustBuckets(copy.m_EntryCount);
    
    // copy all entries
    for (int i=0; i<1<<copy.m_BucketCountLog; i++) {
        if (copy.m_Buckets[i]) {
            AddEntry(new Entry(m_Hasher(copy.m_Buckets[i]->GetKey()),
                               copy.m_Buckets[i]->GetKey(), 
                               copy.m_Buckets[i]->GetValue()));
        }
    }
    
    return *this;
}

/*----------------------------------------------------------------------
|   NPT_HashMap<K,V,HF>::operator==
+---------------------------------------------------------------------*/
template <typename K, typename V, typename HF>
bool
NPT_HashMap<K,V,HF>::operator==(const NPT_HashMap<K,V,HF>& other) const
{
    // quick check
    if (m_EntryCount != other.m_EntryCount) return false;
    
    // compare all entries to all other entries
    for (int i=0; i<(1<<m_BucketCountLog); i++) {
        Entry* entry = m_Buckets[i];
        if (entry == NULL) continue;
        Entry* other_entry = other.GetEntry(entry->m_Key);
        if (other_entry == NULL || !(other_entry->m_Value == entry->m_Value)) {
            return false;
        }
    }
    
    return true;
}

/*----------------------------------------------------------------------
|   NPT_HashMap<K,V,HF>::operator!=
+---------------------------------------------------------------------*/
template <typename K, typename V, typename HF>
bool
NPT_HashMap<K,V,HF>::operator!=(const NPT_HashMap<K,V,HF>& other) const
{
    return !(*this == other);
}

/*----------------------------------------------------------------------
|   NPT_HashMap<K,V>::operator[]
+---------------------------------------------------------------------*/
template <typename K, typename V, typename HF>
V&
NPT_HashMap<K,V,HF>::operator[](const K& key)
{
    Entry* entry = GetEntry(key);
    if (entry == NULL) {
        // create a new "default" entry for this key
        entry = new Entry(m_Hasher(key), key);
        AddEntry(entry);
    }
     
    return entry->m_Value;
}

/*----------------------------------------------------------------------
|   NPT_MapEntryValueDeleter
+---------------------------------------------------------------------*/
template <class T>
class NPT_MapEntryValueDeleter {
public:
    void operator()(T* entry) const {
        delete entry->GetValue();
    }
};

#endif // _NPT_MAP_H_
