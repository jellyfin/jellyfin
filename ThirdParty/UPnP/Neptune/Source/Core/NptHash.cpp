/*****************************************************************
|
|   Neptune - Hashing
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

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptTypes.h"
#include "NptResults.h"
#include "NptHash.h"

/*----------------------------------------------------------------------
|   local constants
+---------------------------------------------------------------------*/
// 32 bit magic FNV-1a prime
const NPT_UInt32 NPT_FNV_32_PRIME = 0x01000193;

/*----------------------------------------------------------------------
|   NPT_Fnv1aHash32
+---------------------------------------------------------------------*/
NPT_UInt32
NPT_Fnv1aHash32(const NPT_UInt8* data, NPT_Size data_size, NPT_UInt32 hash_init)
{
    const NPT_UInt8* data_end = data + data_size;
    NPT_UInt32       hash_value = hash_init;
    
    while (data < data_end) {
        hash_value ^= (NPT_UInt32)*data++;

#if defined(NPT_CONFIG_FNV_HASH_USE_SHIFT_MUL)
        hash_value += (hash_value<<1) + (hash_value<<4) + (hash_value<<7) + (hash_value<<8) + (hash_value<<24);
#else
        hash_value *= NPT_FNV_32_PRIME;
#endif
    }

    return hash_value;
}


/*----------------------------------------------------------------------
|   NPT_Fnv1aHashStr32
+---------------------------------------------------------------------*/
NPT_UInt32
NPT_Fnv1aHashStr32(const char* data, NPT_UInt32 hash_init)
{
    NPT_UInt32 hash_value = hash_init;
    
    while (*data) {
        hash_value ^= (NPT_UInt32)*data++;

#if defined(NPT_CONFIG_FNV_HASH_USE_SHIFT_MUL)
        hash_value += (hash_value<<1) + (hash_value<<4) + (hash_value<<7) + (hash_value<<8) + (hash_value<<24);
#else
        hash_value *= NPT_FNV_32_PRIME;
#endif
    }

    return hash_value;
}

/*----------------------------------------------------------------------
|   NPT_FnvHash32
+---------------------------------------------------------------------*/
// 64 bit magic FNV-1a prime
const NPT_UInt64 NPT_FNV_64_PRIME = 0x100000001b3ULL;

/*----------------------------------------------------------------------
|   NPT_Fnv1aHash64
+---------------------------------------------------------------------*/
NPT_UInt64
NPT_Fnv1aHash64(const NPT_UInt8* data, NPT_Size data_size, NPT_UInt64 hash_init)
{
    const NPT_UInt8* data_end = data + data_size;	
    NPT_UInt64       hash_value = hash_init;
    
    while (data < data_end) {
        hash_value ^= (NPT_UInt64)*data++;

#if defined(NPT_CONFIG_FNV_HASH_USE_SHIFT_MUL)
        hash_value += (hash_value << 1) + (hash_value << 4) + (hash_value << 5) + (hash_value << 7) + (hash_value << 8) + (hash_value << 40);
#else
        hash_value *= NPT_FNV_64_PRIME;
#endif
    }

    return hash_value;
}


/*----------------------------------------------------------------------
|   NPT_Fnv1aHashStr64
+---------------------------------------------------------------------*/
NPT_UInt64
NPT_Fnv1aHashStr64(const char* data, NPT_UInt64 hash_init)
{
    NPT_UInt64 hash_value = hash_init;
    
    while (*data) {
        hash_value ^= (NPT_UInt64)*data++;

#if defined(NPT_CONFIG_FNV_HASH_USE_SHIFT_MUL)
        hash_value += (hash_value << 1) + (hash_value << 4) + (hash_value << 5) + (hash_value << 7) + (hash_value << 8) + (hash_value << 40);
#else
        hash_value *= NPT_FNV_64_PRIME;
#endif
    }

    return hash_value;
}
