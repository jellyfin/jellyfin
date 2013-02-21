/*****************************************************************
|
|   Neptune - Crypto
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

#ifndef _NPT_CRYPTO_H_
#define _NPT_CRYPTO_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptTypes.h"
#include "NptDataBuffer.h"

/*----------------------------------------------------------------------
|   NPT_BlockCipher
+---------------------------------------------------------------------*/
class NPT_BlockCipher {
public:
    // types
    typedef enum {
        AES_128
    } Algorithm;
    
    typedef enum {
        ENCRYPT,
        DECRYPT
    } Direction;
    
    // factory
    static NPT_Result Create(Algorithm         algorithm, 
                             Direction         direction,
                             const NPT_UInt8*  key,
                             NPT_Size          key_size,
                             NPT_BlockCipher*& cipher);
    
    // methods
    virtual           ~NPT_BlockCipher() {}
    virtual NPT_Size   GetBlockSize() = 0;
    virtual Direction  GetDirection() = 0;
    virtual Algorithm  GetAlgorithm() = 0;
    virtual NPT_Result ProcessBlock(const NPT_UInt8* input, NPT_UInt8* output) = 0;
    /**
     * @param iv Initial vector (same size as cipher block size), or NULL for an IV made up of all zeros.
     */
    virtual NPT_Result ProcessCbc(const NPT_UInt8* input, NPT_Size input_size, const NPT_UInt8* iv, NPT_DataBuffer& output);
    
protected:
    NPT_BlockCipher() {} // don't instantiate directly
};

#endif // _NPT_CRYPTO_H_
