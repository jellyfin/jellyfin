/*****************************************************************
|
|   Neptune - Interfaces
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

#ifndef _NPT_INTERFACES_H_
#define _NPT_INTERFACES_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptTypes.h"
#include "NptCommon.h"
#include "NptResults.h"
#include "NptConfig.h"

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
const int NPT_ERROR_NO_SUCH_INTERFACE = NPT_ERROR_BASE_INTERFACES - 0;

/*----------------------------------------------------------------------
|   NPT_InterfaceId
+---------------------------------------------------------------------*/
class NPT_InterfaceId
{
 public:
    // methods
    bool operator==(const NPT_InterfaceId& id) const {
        return ((id.m_Id == m_Id) && (id.m_Version == m_Version));
    }

    // members
    unsigned long m_Id;
    unsigned long m_Version;
};

/*----------------------------------------------------------------------
|   NPT_Polymorphic
+---------------------------------------------------------------------*/
class NPT_Polymorphic
{
public:
    // destructor
    virtual ~NPT_Polymorphic() {}
     
    // methods
    virtual NPT_Result GetInterface(const NPT_InterfaceId& id, 
                                    NPT_Interface*&        iface) = 0;
};

/*----------------------------------------------------------------------
|   NPT_Interruptible
+---------------------------------------------------------------------*/
class NPT_Interruptible
{
public:
    // destructor
    virtual ~NPT_Interruptible() {}

    // methods
    virtual NPT_Result Interrupt() = 0;
};

/*----------------------------------------------------------------------
|   NPT_Configurable
+---------------------------------------------------------------------*/
class NPT_Configurable
{
public:
    // destructor
    virtual ~NPT_Configurable() {}
     
    // methods
    virtual NPT_Result SetProperty(const char* /*name*/, 
                                   const char* /*value*/) { 
        return NPT_ERROR_NO_SUCH_PROPERTY;
    }
    virtual NPT_Result SetProperty(const char* /*name*/, 
                                   int         /*value*/) { 
        return NPT_ERROR_NO_SUCH_PROPERTY;
    }
    virtual NPT_Result GetProperty(const char*        /*name*/, 
                                   NPT_PropertyValue& /*value*/) {
        return NPT_ERROR_NO_SUCH_PROPERTY;
    }
};

#endif // _NPT_INTERFACES_H_
