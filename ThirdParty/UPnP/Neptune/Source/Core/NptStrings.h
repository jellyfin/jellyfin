/*****************************************************************
|
|   Neptune - String Objects
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

#ifndef _NPT_STRINGS_H_
#define _NPT_STRINGS_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptConfig.h"
#if defined(NPT_CONFIG_HAVE_NEW_H)
#include <new>
#endif
#include "NptTypes.h"
#include "NptConstants.h"
#include "NptList.h"
#include "NptArray.h"
#include "NptDebug.h"
#include "NptHash.h"

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
const int NPT_STRING_SEARCH_FAILED = -1;

/*----------------------------------------------------------------------
|   NPT_String
+---------------------------------------------------------------------*/
class NPT_String
{
public:
    // factories
    static NPT_String FromInteger(NPT_Int64 value);
    static NPT_String FromIntegerU(NPT_UInt64 value);
    static NPT_String Format(const char* format, ...);
    
    // constructors
    NPT_String(const NPT_String& str);
    NPT_String(const char* str);
    NPT_String(const char* str, NPT_Size length);
    NPT_String(char c, NPT_Cardinal repeat = 1);
    NPT_String() : m_Chars(NULL) {}
   ~NPT_String() { if (m_Chars) GetBuffer()->Destroy(); }

    // string info and manipulations
    bool       IsEmpty() const { return m_Chars == NULL || GetBuffer()->GetLength() == 0; }
    NPT_Size   GetLength()   const { return m_Chars ? GetBuffer()->GetLength() : 0;    }
    NPT_Size   GetCapacity() const { return m_Chars ? GetBuffer()->GetAllocated() : 0; }
    NPT_Result SetLength(NPT_Size length, bool pad = false);
    void       Assign(const char* chars, NPT_Size size);
    void       Append(const char* chars, NPT_Size size);
    void       Append(const char* s) { Append(s, StringLength(s)); }
    int        Compare(const char* s, bool ignore_case = false) const;
    static int Compare(const char* s1, const char* s2, bool ignore_case = false);
    int        CompareN(const char* s, NPT_Size count, bool ignore_case = false) const;
    static int CompareN(const char* s1, const char* s2, NPT_Size count, bool ignore_case = false);

    // substrings
    NPT_String SubString(NPT_Ordinal first, NPT_Size length) const;
    NPT_String SubString(NPT_Ordinal first) const {
        return SubString(first, GetLength());
    }
    NPT_String Left(NPT_Size length) const {
        return SubString(0, length);
    }
    NPT_String Right(NPT_Size length) const {
        return length >= GetLength() ? 
               *this : 
               SubString(GetLength()-length, length);
    }
    NPT_List<NPT_String> Split(const char* separator) const;
    NPT_Array<NPT_String> SplitAny(const char* separator) const;
    static NPT_String Join(NPT_List<NPT_String>& args, const char* separator);
    
    // buffer management
    void       Reserve(NPT_Size length);

    // hashing
    NPT_UInt32 GetHash32() const;
    NPT_UInt64 GetHash64() const;

    // conversions
    NPT_String ToLowercase() const;
    NPT_String ToUppercase() const;
    NPT_Result ToInteger(int& value, bool relaxed = true) const;
    NPT_Result ToInteger(unsigned int& value, bool relaxed = true) const;
    NPT_Result ToInteger(long& value, bool relaxed = true) const;
    NPT_Result ToInteger(unsigned long& value, bool relaxed = true) const;
    NPT_Result ToInteger32(NPT_Int32& value, bool relaxed = true) const;
    NPT_Result ToInteger32(NPT_UInt32& value, bool relaxed = true) const;
    NPT_Result ToInteger64(NPT_Int64& value, bool relaxed = true) const;
    NPT_Result ToInteger64(NPT_UInt64& value, bool relaxed = true) const;
    NPT_Result ToFloat(float& value, bool relaxed = true) const;
    
    // processing
    void MakeLowercase();
    void MakeUppercase();
    const NPT_String& Replace(char a, char b);
    const NPT_String& Replace(char a, const char* b);

    // search
    int  Find(char c, NPT_Ordinal start = 0, bool ignore_case = false) const;
    int  Find(const char* s, NPT_Ordinal start = 0, bool ignore_case = false) const;
    int  FindAny(const char* s, NPT_Ordinal start, bool ignore_case = false) const;
    int  ReverseFind(char c, NPT_Ordinal start = 0, bool ignore_case = false) const;
    int  ReverseFind(const char* s, NPT_Ordinal start = 0, bool ignore_case = false) const;
    bool StartsWith(const char* s, bool ignore_case = false) const;
    bool EndsWith(const char* s, bool ignore_case = false) const;

    // editing
    const NPT_String& Insert(const char* s, NPT_Ordinal where = 0);
    const NPT_String& Erase(NPT_Ordinal start, NPT_Cardinal count = 1);
    const NPT_String& Replace(const char* before, const char* after);
    // void Replace(NPT_Ordinal start, NPT_Cardinal count, const char* s);
    const NPT_String& TrimLeft();
    const NPT_String& TrimLeft(char c);
    const NPT_String& TrimLeft(const char* chars);
    const NPT_String& TrimRight();
    const NPT_String& TrimRight(char c);
    const NPT_String& TrimRight(const char* chars);
    const NPT_String& Trim();
    const NPT_String& Trim(char c);
    const NPT_String& Trim(const char* chars);

    // type casting
    operator char*() const        { return m_Chars ? m_Chars: &EmptyString; }
    operator const char* () const { return m_Chars ? m_Chars: &EmptyString; }
    const char* GetChars() const  { return m_Chars ? m_Chars: &EmptyString; }
    char*       UseChars()        { return m_Chars ? m_Chars: &EmptyString; }

    // operator overloading
    NPT_String& operator=(const char* str);
    NPT_String& operator=(const NPT_String& str);
    NPT_String& operator=(char c);
    const NPT_String& operator+=(const NPT_String& s) {
        Append(s.GetChars(), s.GetLength());
        return *this;
    }
    const NPT_String& operator+=(const char* s) {
        Append(s);
        return *this;
    }
    const NPT_String& operator+=(char c) {
        Append(&c, 1);
        return *this;
    }
    char operator[](int index) const {
        NPT_ASSERT((unsigned int)index < GetLength());
        return GetChars()[index];
    }
    char& operator[](int index) {
        NPT_ASSERT((unsigned int)index < GetLength());
        return UseChars()[index];
    }

    // friend operators
    friend NPT_String operator+(const NPT_String& s1, const NPT_String& s2) {
        return s1+s2.GetChars();
    }
    friend NPT_String operator+(const NPT_String& s1, const char* s2);
    friend NPT_String operator+(const char* s1, const NPT_String& s2);
    friend NPT_String operator+(const NPT_String& s, char c);
    friend NPT_String operator+(char c, const NPT_String& s);

protected:
    // inner classes
    class Buffer {
    public:
        // class methods
        static Buffer* Allocate(NPT_Size allocated, NPT_Size length) {
            void* mem = ::operator new(sizeof(Buffer)+allocated+1);
            return new(mem) Buffer(allocated, length);
        }
        static char* Create(NPT_Size allocated, NPT_Size length=0) {
            Buffer* shared = Allocate(allocated, length);
            return shared->GetChars();
        }
        static char* Create(const char* copy) {
            NPT_Size length = StringLength(copy);
            Buffer* shared = Allocate(length, length);
            CopyString(shared->GetChars(), copy);
            return shared->GetChars();
        }
        static char* Create(const char* copy, NPT_Size length) {
            Buffer* shared = Allocate(length, length);
            CopyBuffer(shared->GetChars(), copy, length);
            shared->GetChars()[length] = '\0';
            return shared->GetChars();
        }
        static char* Create(char c, NPT_Cardinal repeat) {
            Buffer* shared = Allocate(repeat, repeat);
            char* s = shared->GetChars();
            while (repeat--) {
                *s++ = c;
            }
            *s = '\0';
            return shared->GetChars();
        }

        // methods
        char* GetChars() { 
            // return a pointer to the first char
            return reinterpret_cast<char*>(this+1); 
        }
        NPT_Size GetLength() const      { return m_Length; }
        void SetLength(NPT_Size length) { m_Length = length; }
        NPT_Size GetAllocated() const   { return m_Allocated; }
        void Destroy() { ::operator delete((void*)this); }
        
    private:
        // methods
        Buffer(NPT_Size allocated, NPT_Size length = 0) : 
            m_Length(length),
            m_Allocated(allocated) {}
        
        // members
        NPT_Cardinal m_Length;
        NPT_Cardinal m_Allocated;
        // the actual string data follows

    };
    
    // members
    char* m_Chars;

private:
    // friends
    friend class Buffer;

    // static members
    static char EmptyString;

    // methods
    Buffer* GetBuffer() const { 
        return reinterpret_cast<Buffer*>(m_Chars)-1;
    }
    void Reset() { 
        if (m_Chars != NULL) {
            delete GetBuffer(); 
            m_Chars = NULL;
        }
    }
    char* PrepareToWrite(NPT_Size length);
    void PrepareToAppend(NPT_Size length, NPT_Size allocate);

    // static methods
    static void CopyString(char* dst, const char* src) {
        while ((*dst++ = *src++)){}
    }
    
    static void CopyBuffer(char* dst, const char* src, NPT_Size size) {
        while (size--) *dst++ = *src++;
    }
    
    static NPT_Size StringLength(const char* str) {
        NPT_Size length = 0;
        while (*str++) length++;
        return length;
    }
};

/*----------------------------------------------------------------------
|   external operators
+---------------------------------------------------------------------*/
inline bool operator==(const NPT_String& s1, const NPT_String& s2) { 
    return s1.Compare(s2) == 0; 
}
inline bool operator==(const NPT_String& s1, const char* s2) {
    return s1.Compare(s2) == 0; 
}
inline bool operator==(const char* s1, const NPT_String& s2) {
    return s2.Compare(s1) == 0; 
}
inline bool operator!=(const NPT_String& s1, const NPT_String& s2) {
    return s1.Compare(s2) != 0; 
}
inline bool operator!=(const NPT_String& s1, const char* s2) {
    return s1.Compare(s2) != 0; 
}
inline bool operator!=(const char* s1, const NPT_String& s2) {
    return s2.Compare(s1) != 0; 
}
inline bool operator<(const NPT_String& s1, const NPT_String& s2) {
    return s1.Compare(s2) < 0; 
}
inline bool operator<(const NPT_String& s1, const char* s2) {
    return s1.Compare(s2) < 0; 
}
inline bool operator<(const char* s1, const NPT_String& s2) {
    return s2.Compare(s1) > 0; 
}
inline bool operator>(const NPT_String& s1, const NPT_String& s2) {
    return s1.Compare(s2) > 0; 
}
inline bool operator>(const NPT_String& s1, const char* s2) {
    return s1.Compare(s2) > 0; 
}
inline bool operator>(const char* s1, const NPT_String& s2) {
    return s2.Compare(s1) < 0; 
}
inline bool operator<=(const NPT_String& s1, const NPT_String& s2) {
    return s1.Compare(s2) <= 0; 
}
inline bool operator<=(const NPT_String& s1, const char* s2) {
    return s1.Compare(s2) <= 0; 
}
inline bool operator<=(const char* s1, const NPT_String& s2) {
    return s2.Compare(s1) >= 0; 
}
inline bool operator>=(const NPT_String& s1, const NPT_String& s2) {
    return s1.Compare(s2) >= 0; 
}
inline bool operator>=(const NPT_String& s1, const char* s2) {
    return s1.Compare(s2) >= 0; 
}
inline bool operator>=(const char* s1, const NPT_String& s2) {
    return s2.Compare(s1) <= 0; 
}

/*----------------------------------------------------------------------
|   hashing
+---------------------------------------------------------------------*/
template <>
struct NPT_Hash<NPT_String>
{
    NPT_UInt32 operator()(const NPT_String& s) const { return s.GetHash32(); }
};


#endif // _NPT_STRINGS_H_
