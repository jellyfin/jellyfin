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

#ifndef _NPT_HASH_H_
#define _NPT_HASH_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptTypes.h"
#include "NptResults.h"

/*----------------------------------------------------------------------
|    Fowler/Noll/Vo FNV-1a hash functions
+---------------------------------------------------------------------*/
const NPT_UInt32 NPT_FNV1A_32_INIT = ((NPT_UInt32)0x811c9dc5);
NPT_UInt32 NPT_Fnv1aHash32(const NPT_UInt8* data, NPT_Size data_size, NPT_UInt32 hash_init=NPT_FNV1A_32_INIT);
NPT_UInt32 NPT_Fnv1aHashStr32(const char* data, NPT_UInt32 hash_init=NPT_FNV1A_32_INIT);
const NPT_UInt64 NPT_FNV1A_64_INIT = ((NPT_UInt64)0xcbf29ce484222325ULL);
NPT_UInt64 NPT_Fnv1aHash64(const NPT_UInt8* data, NPT_Size data_size, NPT_UInt64 hash_init=NPT_FNV1A_64_INIT);
NPT_UInt64 NPT_Fnv1aHashStr64(const char* data, NPT_UInt64 hash_init=NPT_FNV1A_64_INIT);

/*----------------------------------------------------------------------
|   NPT_Hash
+---------------------------------------------------------------------*/
template <typename K>
struct NPT_Hash
{
};

template <>
struct NPT_Hash<const char*>
{
    NPT_UInt32 operator()(const char* s) const { return NPT_Fnv1aHashStr32(s); }
};

template <>
struct NPT_Hash<char*>
{
    NPT_UInt32 operator()(char* s) const { return NPT_Fnv1aHashStr32(s); }
};

template <>
struct NPT_Hash<int>
{
    NPT_UInt32 operator()(int i) const { return NPT_Fnv1aHash32(reinterpret_cast<const NPT_UInt8*>(&i), sizeof(int)); }
};

template <>
struct NPT_Hash<unsigned int>
{
    NPT_UInt32 operator()(unsigned int i) const { return NPT_Fnv1aHash32(reinterpret_cast<const NPT_UInt8*>(&i), sizeof(int)); }
};

#endif // _NPT_HASH_H_
