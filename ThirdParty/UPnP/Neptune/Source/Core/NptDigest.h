/*****************************************************************
|
|   Neptune - Message Digests
|
| Copyright (c) 2002-2010, Axiomatic Systems, LLC.
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

#ifndef _NPT_DIGEST_H_
#define _NPT_DIGEST_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptTypes.h"
#include "NptDataBuffer.h"

/*----------------------------------------------------------------------
|   NPT_Digest
+---------------------------------------------------------------------*/
class NPT_Digest {
public:
    // types
    typedef enum {
        ALGORITHM_SHA1,
        ALGORITHM_SHA256,
        ALGORITHM_MD5
    } Algorithm;
    
    // factory
    static NPT_Result Create(Algorithm algorithm, NPT_Digest*& digest);
    
    // methods
    virtual             ~NPT_Digest() {}
    virtual unsigned int GetSize() = 0;
    virtual NPT_Result   Update(const NPT_UInt8* data, NPT_Size data_size) = 0;
    virtual NPT_Result   GetDigest(NPT_DataBuffer& digest) = 0;

protected:
    NPT_Digest() {} // don't instantiate directly
};

class NPT_Hmac {
public:
    static NPT_Result Create(NPT_Digest::Algorithm algorithm,
                             const NPT_UInt8*      key,
                             NPT_Size              key_size, 
                             NPT_Digest*&          digest);

private:
    // methods
    NPT_Hmac() {} // don't instantiate
};

#endif // _NPT_DIGEST_H_
