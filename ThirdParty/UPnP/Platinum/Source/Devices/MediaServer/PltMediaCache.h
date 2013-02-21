/*****************************************************************
|
|   Platinum - AV Media Cache
|
| Copyright (c) 2004-2010, Plutinosoft, LLC.
| All rights reserved.
| http://www.plutinosoft.com
|
| This program is free software; you can redistribute it and/or
| modify it under the terms of the GNU General Public License
| as published by the Free Software Foundation; either version 2
| of the License, or (at your option) any later version.
|
| OEMs, ISVs, VARs and other distributors that combine and 
| distribute commercially licensed software with Platinum software
| and do not wish to distribute the source code for the commercially
| licensed software under version 2, or (at your option) any later
| version, of the GNU General Public License (the "GPL") must enter
| into a commercial license agreement with Plutinosoft, LLC.
| licensing@plutinosoft.com
|  
| This program is distributed in the hope that it will be useful,
| but WITHOUT ANY WARRANTY; without even the implied warranty of
| MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
| GNU General Public License for more details.
|
| You should have received a copy of the GNU General Public License
| along with this program; see the file LICENSE.txt. If not, write to
| the Free Software Foundation, Inc., 
| 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
| http://www.gnu.org/licenses/gpl-2.0.html
|
****************************************************************/

/** @file
 Simple Object Caching utility.
 */

#ifndef _PLT_MEDIA_CACHE_H_
#define _PLT_MEDIA_CACHE_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"

/*----------------------------------------------------------------------
|   PLT_MediaCache
+---------------------------------------------------------------------*/
/**
 The PLT_MediaCache template provides a way to hold references to object in 
 memory. 
 */ 
template <typename T, typename U>
class PLT_MediaCache
{
public:
    typedef typename NPT_Map<NPT_String,T>::Entry ElementEntry;
    typedef typename NPT_List<ElementEntry*>::Iterator ElementIterator;

    PLT_MediaCache<T,U>() {}
    virtual ~PLT_MediaCache<T,U>() {}

    NPT_Result Put(const char* root, const char* key, T& value, U* tag = NULL);
    NPT_Result Get(const char* root, const char* key, T& value, U* tag = NULL);
    NPT_Result Clear(const char* root, const char* key);
    NPT_Result Clear(const char* root = NULL);

private:
    // methods
    NPT_String GenerateKey(const char* root, const char* key);

private:
    // members
    NPT_Mutex              m_Mutex;
    NPT_Map<NPT_String, T> m_Items;
    NPT_Map<NPT_String, U> m_Tags;
};

/*----------------------------------------------------------------------
|   PLT_MediaCache::GenerateKey
+---------------------------------------------------------------------*/
template <typename T, typename U>
inline
NPT_String
PLT_MediaCache<T,U>::GenerateKey(const char* root, const char* key)
{
    // TODO: There could be collision
    NPT_String result = root;
    result += "/";
    result += key;
    return result;
}

/*----------------------------------------------------------------------
|   PLT_MediaCache::Put
+---------------------------------------------------------------------*/
template <typename T, typename U>
inline
NPT_Result
PLT_MediaCache<T,U>::Put(const char* root,
                         const char* key, 
                         T&          value,
                         U*          tag)
{
    NPT_AutoLock lock(m_Mutex);

    NPT_String fullkey = GenerateKey(root, key);
    if (fullkey.GetLength() == 0) return NPT_ERROR_INVALID_PARAMETERS;

    m_Items.Erase(fullkey);
    NPT_CHECK(m_Items.Put(fullkey, value));
    
    if (tag) NPT_CHECK(m_Tags.Put(fullkey, *tag));
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_MediaCache::Get
+---------------------------------------------------------------------*/
template <typename T, typename U>
inline
NPT_Result
PLT_MediaCache<T,U>::Get(const char* root,
                       const char* key, 
                       T&          value,
                       U*          tag /* = NULL */)
{
    NPT_AutoLock lock(m_Mutex);

    NPT_String fullkey = GenerateKey(root, key);
    if (fullkey.GetLength() == 0) return NPT_ERROR_INVALID_PARAMETERS;
    
    T* _value = NULL;
    NPT_CHECK(m_Items.Get(fullkey, _value));
    
    U* _tag;
    if (tag) {
        m_Tags.Get(fullkey, _tag);
        if (_tag) *tag = *_tag;
    }

    value = *_value;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_MediaCache::Clear
+---------------------------------------------------------------------*/
template <typename T, typename U>
inline
NPT_Result 
PLT_MediaCache<T,U>::Clear(const char* root, const char* key) 
{
    NPT_AutoLock lock(m_Mutex);

    NPT_String fullkey = GenerateKey(root, key);
    if (fullkey.GetLength() == 0) return NPT_ERROR_INVALID_PARAMETERS;

    ElementIterator entries = m_Items.GetEntries().GetFirstItem();
    ElementIterator entry;
    while (entries) {
        entry = entries++;
        if ((*entry)->GetKey() == (fullkey)) {
            m_Items.Erase(fullkey);
            m_Tags.Erase(fullkey);
            return NPT_SUCCESS;
        }
    }

    return NPT_ERROR_NO_SUCH_ITEM;
}

/*----------------------------------------------------------------------
|   PLT_MediaCache::Clear
+---------------------------------------------------------------------*/
template <typename T, typename U>
inline
NPT_Result
PLT_MediaCache<T,U>::Clear(const char* root)
{
    NPT_AutoLock lock(m_Mutex);

    if (!root || root[0]=='\0') 
        return m_Items.Clear();

    NPT_String key = GenerateKey(root, "");
    ElementIterator entries = m_Items.GetEntries().GetFirstItem();
    ElementIterator entry;
    while (entries) {
        entry = entries++;
        NPT_String entry_key = (*entry)->GetKey();
        if (entry_key.StartsWith(key)) {
            m_Items.Erase(entry_key);
            m_Tags.Erase(entry_key);
        }
    }

    return NPT_SUCCESS;
}

#endif /* _PLT_MEDIA_CACHE_H_ */
