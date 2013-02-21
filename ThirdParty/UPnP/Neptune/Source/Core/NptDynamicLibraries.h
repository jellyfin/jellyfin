/*****************************************************************
|
|   Neptune - Dynamic Libraries
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

#ifndef _NPT_DYNAMIC_LIBRARIES_H_
#define _NPT_DYNAMIC_LIBRARIES_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptTypes.h"

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
#define NPT_DYANMIC_LIBRARY_LOAD_FLAG_NOW 1

/*----------------------------------------------------------------------
|   NPT_DynamicLibraryInterface
+---------------------------------------------------------------------*/
class NPT_DynamicLibraryInterface
{
public:
    virtual ~NPT_DynamicLibraryInterface() {}
    virtual NPT_Result FindSymbol(const char* name, void*& symbol) = 0;
    virtual NPT_Result Unload() = 0;
};

/*----------------------------------------------------------------------
|   NPT_DynamicLibrary
+---------------------------------------------------------------------*/
class NPT_DynamicLibrary : public NPT_DynamicLibraryInterface
{
public:
    // class methods
    static NPT_Result Load(const char* name, NPT_Flags flags, NPT_DynamicLibrary*& library);
    
    // destructor
    ~NPT_DynamicLibrary() { delete m_Delegate; }
    
    // NPT_DynamicLibraryInterface methods
    virtual NPT_Result FindSymbol(const char* name, void*& symbol) {
        return m_Delegate->FindSymbol(name, symbol);
    }
    virtual NPT_Result Unload() {
        return m_Delegate->Unload();
    }
    
private:
    // methods
    NPT_DynamicLibrary(NPT_DynamicLibraryInterface* delegate) : m_Delegate(delegate) {}
    
    // members
    NPT_DynamicLibraryInterface* m_Delegate;
};

#endif // _NPT_DYNAMIC_LIBRARIES_H_
