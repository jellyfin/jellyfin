/*****************************************************************
|
|   Neptune - Stack
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

#ifndef _NPT_STACK_H_
#define _NPT_STACK_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptResults.h"
#include "NptTypes.h"
#include "NptList.h"

/*----------------------------------------------------------------------
|   NPT_Stack
+---------------------------------------------------------------------*/
template <typename T> 
class NPT_Stack : public NPT_List<T>
{
public:
    // methods
    NPT_Result Push(const T& value) {
        return this->Add(value);
    }

    NPT_Result Peek(T& value) {
        // NOTE: we must use the this-> accessor here because the standard
        // requires it when the member to look up is in a parent template
        if (this->m_ItemCount == 0) return NPT_ERROR_NO_SUCH_ITEM;
        value = this->m_Tail->m_Data;
        return NPT_SUCCESS;
    }

    NPT_Result Pop(T& value) {
        // NOTE: we must use the this-> accessor here because the standard
        // requires it when the member to look up is in a parent template
        if (this->m_ItemCount == 0) return NPT_ERROR_NO_SUCH_ITEM;
        typename NPT_List<T>::Iterator tail = this->GetLastItem();
        value = *tail;
        return this->Erase(tail);
    }
};

#endif // _NPT_STACK_H_
