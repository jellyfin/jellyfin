/*****************************************************************
|
|   Neptune - Common Definitions
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

#ifndef _NPT_COMMON_H_
#define _NPT_COMMON_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptTypes.h"
#include "NptResults.h"

/*----------------------------------------------------------------------
|   NPT_ObjectDeleter
+---------------------------------------------------------------------*/
template <class T>
class NPT_ObjectDeleter {
public:
    void operator()(T* object) const {
        delete object;
    }
};

/*----------------------------------------------------------------------
|   NPT_ObjectComparator
+---------------------------------------------------------------------*/
template <class T>
class NPT_ObjectComparator {
public:
    NPT_ObjectComparator(T& object) : m_Object(object) {}
    bool operator()(const T& object) const {
        return object == m_Object;
    }
private:
    T& m_Object;
};

/*----------------------------------------------------------------------
|   NPT_ContainerFind
+---------------------------------------------------------------------*/
template <typename T, typename P>
NPT_Result NPT_ContainerFind(T&                   container, 
                             const P&             predicate, 
                             typename T::Element& item, 
                             NPT_Ordinal          n=0) 
{
    typename T::Iterator found = container.Find(predicate, n);
    if (found) {
        item = *found;
        return NPT_SUCCESS;
    } else {
        return NPT_ERROR_NO_SUCH_ITEM;
    }
}

/*----------------------------------------------------------------------
|   NPT_ContainerFind
+---------------------------------------------------------------------*/
template <typename T, typename P>
NPT_Result NPT_ContainerFind(T&                    container, 
                             const P&              predicate, 
                             typename T::Iterator& iter, 
                             NPT_Ordinal           n=0) 
{
    iter = container.Find(predicate, n);
    return iter?NPT_SUCCESS:NPT_ERROR_NO_SUCH_ITEM;
}

/*----------------------------------------------------------------------
|   NPT_UntilResultEquals
+---------------------------------------------------------------------*/
class NPT_UntilResultEquals
{
public:
    // methods
    NPT_UntilResultEquals(NPT_Result condition_result, 
                          NPT_Result return_value = NPT_SUCCESS) :
      m_ConditionResult(condition_result),
      m_ReturnValue(return_value) {}
    bool operator()(NPT_Result result, NPT_Result& return_value) const {
        if (result == m_ConditionResult) {
            return_value = m_ReturnValue;
            return true;
        } else {
            return false;
        }
    }

private:
    // members
    NPT_Result m_ConditionResult;
    NPT_Result m_ReturnValue;
};

/*----------------------------------------------------------------------
|   NPT_UntilResultNotEquals
+---------------------------------------------------------------------*/
class NPT_UntilResultNotEquals
{
public:
    // methods
    NPT_UntilResultNotEquals(NPT_Result condition_result) :
      m_ConditionResult(condition_result) {}
    bool operator()(NPT_Result result, NPT_Result& return_value) const {
        if (result != m_ConditionResult) {
            return_value = result;
            return true;
        } else {
            return false;
        }
    }

private:
    // members
    NPT_Result m_ConditionResult;
};

/*----------------------------------------------------------------------
|   NPT_PropertyValue
+---------------------------------------------------------------------*/
class NPT_PropertyValue
{
 public:
    // typedefs
    typedef enum {UNKNOWN, INTEGER, STRING} Type;

    // methods
    NPT_PropertyValue() : m_Type(UNKNOWN), m_Integer(0) {}
    NPT_PropertyValue(int value)         : m_Type(INTEGER), m_Integer(value) {}
    NPT_PropertyValue(const char* value) : m_Type(STRING),  m_String(value)  {}

    // members
    Type m_Type;
    union {
        int         m_Integer;
        const char* m_String;
    };
};

#endif // _NPT_COMMON_H_

