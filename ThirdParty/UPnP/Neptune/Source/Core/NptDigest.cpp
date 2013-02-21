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

/*
 Portions of this code are based on the code of LibTomCrypt
 that was released into public domain by Tom St Denis. 
*/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptDigest.h"
#include "NptUtils.h"

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
#define NPT_BASIC_DIGEST_BLOCK_SIZE 64

/*----------------------------------------------------------------------
|   macros
+---------------------------------------------------------------------*/
#define NPT_Digest_ROL(x, y) \
( (((NPT_UInt32)(x) << (y)) | (((NPT_UInt32)(x) & 0xFFFFFFFFUL) >> (32 - (y)))) & 0xFFFFFFFFUL)
#define NPT_Digest_ROR(x, y) \
( ((((NPT_UInt32)(x)&0xFFFFFFFFUL)>>(NPT_UInt32)((y)&31)) | ((NPT_UInt32)(x)<<(NPT_UInt32)(32-((y)&31)))) & 0xFFFFFFFFUL)

#define NPT_Sha1_F0(x,y,z)  (z ^ (x & (y ^ z)))
#define NPT_Sha1_F1(x,y,z)  (x ^ y ^ z)
#define NPT_Sha1_F2(x,y,z)  ((x & y) | (z & (x | y)))
#define NPT_Sha1_F3(x,y,z)  (x ^ y ^ z)

#define NPT_Sha1_FF0(a,b,c,d,e,i) e = (NPT_Digest_ROL(a, 5) + NPT_Sha1_F0(b,c,d) + e + W[i] + 0x5a827999UL); b = NPT_Digest_ROL(b, 30);
#define NPT_Sha1_FF1(a,b,c,d,e,i) e = (NPT_Digest_ROL(a, 5) + NPT_Sha1_F1(b,c,d) + e + W[i] + 0x6ed9eba1UL); b = NPT_Digest_ROL(b, 30);
#define NPT_Sha1_FF2(a,b,c,d,e,i) e = (NPT_Digest_ROL(a, 5) + NPT_Sha1_F2(b,c,d) + e + W[i] + 0x8f1bbcdcUL); b = NPT_Digest_ROL(b, 30);
#define NPT_Sha1_FF3(a,b,c,d,e,i) e = (NPT_Digest_ROL(a, 5) + NPT_Sha1_F3(b,c,d) + e + W[i] + 0xca62c1d6UL); b = NPT_Digest_ROL(b, 30);

#define NPT_Sha256_Ch(x,y,z)       (z ^ (x & (y ^ z)))
#define NPT_Sha256_Maj(x,y,z)      (((x | y) & z) | (x & y)) 
#define NPT_Sha256_S(x, n)         NPT_Digest_ROR((x),(n))
#define NPT_Sha256_R(x, n)         (((x)&0xFFFFFFFFUL)>>(n))
#define NPT_Sha256_Sigma0(x)       (NPT_Sha256_S(x,  2) ^ NPT_Sha256_S(x, 13) ^ NPT_Sha256_S(x, 22))
#define NPT_Sha256_Sigma1(x)       (NPT_Sha256_S(x,  6) ^ NPT_Sha256_S(x, 11) ^ NPT_Sha256_S(x, 25))
#define NPT_Sha256_Gamma0(x)       (NPT_Sha256_S(x,  7) ^ NPT_Sha256_S(x, 18) ^ NPT_Sha256_R(x,  3))
#define NPT_Sha256_Gamma1(x)       (NPT_Sha256_S(x, 17) ^ NPT_Sha256_S(x, 19) ^ NPT_Sha256_R(x, 10))


#define NPT_Md5_F(x,y,z)  (z ^ (x & (y ^ z)))
#define NPT_Md5_G(x,y,z)  (y ^ (z & (y ^ x)))
#define NPT_Md5_H(x,y,z)  (x ^ y ^ z)
#define NPT_Md5_I(x,y,z)  (y ^ (x | (~z)))

#define NPT_Md5_FF(a,b,c,d,M,s,t) \
    a = (a + NPT_Md5_F(b,c,d) + M + t); a = NPT_Digest_ROL(a, s) + b;

#define NPT_Md5_GG(a,b,c,d,M,s,t) \
    a = (a + NPT_Md5_G(b,c,d) + M + t); a = NPT_Digest_ROL(a, s) + b;

#define NPT_Md5_HH(a,b,c,d,M,s,t) \
    a = (a + NPT_Md5_H(b,c,d) + M + t); a = NPT_Digest_ROL(a, s) + b;

#define NPT_Md5_II(a,b,c,d,M,s,t) \
    a = (a + NPT_Md5_I(b,c,d) + M + t); a = NPT_Digest_ROL(a, s) + b;

/*----------------------------------------------------------------------
|   NPT_BasicDigest
+---------------------------------------------------------------------*/
class NPT_BasicDigest : public NPT_Digest
{
public:
    NPT_BasicDigest();
    
    // NPT_Digest methods
    virtual NPT_Result Update(const NPT_UInt8* data, NPT_Size data_size);
    
protected:
    // methods
    NPT_Result   ComputeDigest(NPT_UInt32*     state, 
                               NPT_Cardinal    state_count, 
                               bool            big_endian,
                               NPT_DataBuffer& digest);
    virtual void CompressBlock(const NPT_UInt8* block) = 0;
    
    // members
    NPT_UInt64 m_Length;
    NPT_UInt32 m_Pending;
    NPT_UInt8  m_Buffer[NPT_BASIC_DIGEST_BLOCK_SIZE];
};

/*----------------------------------------------------------------------
|   NPT_BasicDigest::NPT_BasicDigest
+---------------------------------------------------------------------*/
NPT_BasicDigest::NPT_BasicDigest() :
    m_Length(0),
    m_Pending(0)
{
}

/*----------------------------------------------------------------------
|   NPT_BasicDigest::Update
+---------------------------------------------------------------------*/
NPT_Result
NPT_BasicDigest::Update(const NPT_UInt8* data, NPT_Size data_size)
{
    while (data_size > 0) {
        if (m_Pending == 0 && data_size >= NPT_BASIC_DIGEST_BLOCK_SIZE) {
            CompressBlock(data);
            m_Length  += NPT_BASIC_DIGEST_BLOCK_SIZE * 8;
            data      += NPT_BASIC_DIGEST_BLOCK_SIZE;
            data_size -= NPT_BASIC_DIGEST_BLOCK_SIZE;
        } else {
            unsigned int chunk = data_size;
            if (chunk > (NPT_BASIC_DIGEST_BLOCK_SIZE - m_Pending)) {
                chunk = NPT_BASIC_DIGEST_BLOCK_SIZE - m_Pending;
            }
            NPT_CopyMemory(&m_Buffer[m_Pending], data, chunk);
            m_Pending += chunk;
            data      += chunk;
            data_size -= chunk;
            if (m_Pending == NPT_BASIC_DIGEST_BLOCK_SIZE) {
                CompressBlock(m_Buffer);
                m_Length += 8 * NPT_BASIC_DIGEST_BLOCK_SIZE;
                m_Pending = 0;
            }
        }
    }
    
    return NPT_SUCCESS;
}


/*----------------------------------------------------------------------
|   NPT_BasicDigest::ComputeDigest
+---------------------------------------------------------------------*/
NPT_Result
NPT_BasicDigest::ComputeDigest(NPT_UInt32*     state,
                               NPT_Cardinal    state_count,
                               bool            big_endian,
                               NPT_DataBuffer& digest)
{
    // increase the length of the message
    m_Length += m_Pending * 8;

    // append the '1' bit
    m_Buffer[m_Pending++] = 0x80;

    // if there isn't enough space left for the size (8 bytes), then compress.
    // then we can fall back to padding zeros and length encoding as normal. 
    if (m_Pending > NPT_BASIC_DIGEST_BLOCK_SIZE-8) {
        while (m_Pending < NPT_BASIC_DIGEST_BLOCK_SIZE) {
            m_Buffer[m_Pending++] = 0;
        }
        CompressBlock(m_Buffer);
        m_Pending = 0;
    }

    // pad with zeroes up until the length
    while (m_Pending < NPT_BASIC_DIGEST_BLOCK_SIZE-8) {
        m_Buffer[m_Pending++] = 0;
    }

    // store length
    if (big_endian) {
        NPT_BytesFromInt64Be(&m_Buffer[NPT_BASIC_DIGEST_BLOCK_SIZE-8], m_Length);
    } else {
        NPT_BytesFromInt64Le(&m_Buffer[NPT_BASIC_DIGEST_BLOCK_SIZE-8], m_Length);
    }
    CompressBlock(m_Buffer);

    // copy output
    digest.SetDataSize(4*state_count);
    NPT_UInt8* out = digest.UseData();
    if (big_endian) {
        for (unsigned int i = 0; i < state_count; i++) {
            NPT_BytesFromInt32Be(out, state[i]);
            out += 4;
        }
    } else {
        for (unsigned int i = 0; i < state_count; i++) {
            NPT_BytesFromInt32Le(out, state[i]);
            out += 4;
        }
    }
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Sha1Digest
+---------------------------------------------------------------------*/
class NPT_Sha1Digest : public NPT_BasicDigest
{
public:
    NPT_Sha1Digest();

    // NPT_Digest methods
    virtual NPT_Result   GetDigest(NPT_DataBuffer& digest);
    virtual unsigned int GetSize() { return 20; }
    
private:
    // methods
    virtual void CompressBlock(const NPT_UInt8* block);
    
    // members
    NPT_UInt32 m_State[5];
};

/*----------------------------------------------------------------------
|   NPT_Sha1Digest::NPT_Sha1Digest
+---------------------------------------------------------------------*/
NPT_Sha1Digest::NPT_Sha1Digest()
{
    m_State[0] = 0x67452301UL;
    m_State[1] = 0xefcdab89UL;
    m_State[2] = 0x98badcfeUL;
    m_State[3] = 0x10325476UL;
    m_State[4] = 0xc3d2e1f0UL;
}

/*----------------------------------------------------------------------
|   NPT_Sha1Digest::CompressBlock
+---------------------------------------------------------------------*/
void
NPT_Sha1Digest::CompressBlock(const NPT_UInt8* block)
{
    NPT_UInt32 a,b,c,d,e,t,W[80];

    // copy the 512-bit block into W[0..15]
    for (unsigned int i = 0; i < 16; i++) {
        W[i] = NPT_BytesToInt32Be(&block[4*i]);
    }
    
    // copy the state to local variables
    a = m_State[0];
    b = m_State[1];
    c = m_State[2];
    d = m_State[3];
    e = m_State[4];
    
    // expand it
    unsigned int i;
    for (i = 16; i < 80; i++) {
        W[i] = NPT_Digest_ROL(W[i-3] ^ W[i-8] ^ W[i-14] ^ W[i-16], 1); 
    }
    
    // compress
    for (i = 0; i < 20; ) {
       NPT_Sha1_FF0(a,b,c,d,e,i++); t = e; e = d; d = c; c = b; b = a; a = t;
    }

    for (; i < 40; ) {
       NPT_Sha1_FF1(a,b,c,d,e,i++); t = e; e = d; d = c; c = b; b = a; a = t;
    }

    for (; i < 60; ) {
       NPT_Sha1_FF2(a,b,c,d,e,i++); t = e; e = d; d = c; c = b; b = a; a = t;
    }

    for (; i < 80; ) {
       NPT_Sha1_FF3(a,b,c,d,e,i++); t = e; e = d; d = c; c = b; b = a; a = t;
    }
    
    // store the variables back into the state
    m_State[0] += a;
    m_State[1] += b;
    m_State[2] += c;
    m_State[3] += d;
    m_State[4] += e;
}

/*----------------------------------------------------------------------
|   NPT_Sha1Digest::GetDigest
+---------------------------------------------------------------------*/
NPT_Result
NPT_Sha1Digest::GetDigest(NPT_DataBuffer& digest)
{
    return ComputeDigest(m_State, 5, true, digest);
}

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
static const NPT_UInt32 NPT_Sha256_K[64] = {
    0x428a2f98UL, 0x71374491UL, 0xb5c0fbcfUL, 0xe9b5dba5UL, 0x3956c25bUL,
    0x59f111f1UL, 0x923f82a4UL, 0xab1c5ed5UL, 0xd807aa98UL, 0x12835b01UL,
    0x243185beUL, 0x550c7dc3UL, 0x72be5d74UL, 0x80deb1feUL, 0x9bdc06a7UL,
    0xc19bf174UL, 0xe49b69c1UL, 0xefbe4786UL, 0x0fc19dc6UL, 0x240ca1ccUL,
    0x2de92c6fUL, 0x4a7484aaUL, 0x5cb0a9dcUL, 0x76f988daUL, 0x983e5152UL,
    0xa831c66dUL, 0xb00327c8UL, 0xbf597fc7UL, 0xc6e00bf3UL, 0xd5a79147UL,
    0x06ca6351UL, 0x14292967UL, 0x27b70a85UL, 0x2e1b2138UL, 0x4d2c6dfcUL,
    0x53380d13UL, 0x650a7354UL, 0x766a0abbUL, 0x81c2c92eUL, 0x92722c85UL,
    0xa2bfe8a1UL, 0xa81a664bUL, 0xc24b8b70UL, 0xc76c51a3UL, 0xd192e819UL,
    0xd6990624UL, 0xf40e3585UL, 0x106aa070UL, 0x19a4c116UL, 0x1e376c08UL,
    0x2748774cUL, 0x34b0bcb5UL, 0x391c0cb3UL, 0x4ed8aa4aUL, 0x5b9cca4fUL,
    0x682e6ff3UL, 0x748f82eeUL, 0x78a5636fUL, 0x84c87814UL, 0x8cc70208UL,
    0x90befffaUL, 0xa4506cebUL, 0xbef9a3f7UL, 0xc67178f2UL
};

/*----------------------------------------------------------------------
|   NPT_Sha256Digest
+---------------------------------------------------------------------*/
class NPT_Sha256Digest : public NPT_BasicDigest
{
public:
    NPT_Sha256Digest();

    // NPT_Digest methods
    virtual NPT_Result   GetDigest(NPT_DataBuffer& digest);
    virtual unsigned int GetSize() { return 32; }

private:
    // methods
    virtual void CompressBlock(const NPT_UInt8* block);
    
    // members
    NPT_UInt32 m_State[8];
};

/*----------------------------------------------------------------------
|   NPT_Sha256Digest::NPT_Sha256Digest
+---------------------------------------------------------------------*/
NPT_Sha256Digest::NPT_Sha256Digest()
{
    m_State[0] = 0x6A09E667UL;
    m_State[1] = 0xBB67AE85UL;
    m_State[2] = 0x3C6EF372UL;
    m_State[3] = 0xA54FF53AUL;
    m_State[4] = 0x510E527FUL;
    m_State[5] = 0x9B05688CUL;
    m_State[6] = 0x1F83D9ABUL;
    m_State[7] = 0x5BE0CD19UL;
}

/*----------------------------------------------------------------------
|   NPT_Sha256Digest::CompressBlock
+---------------------------------------------------------------------*/
void
NPT_Sha256Digest::CompressBlock(const NPT_UInt8* block)
{
    NPT_UInt32 S[8], W[64];
    
    // copy the state into the local workspace
    for (unsigned int i = 0; i < 8; i++) {
        S[i] = m_State[i];
    }
    
    // copy the 512-bit block into W[0..15]
    for (unsigned int i = 0; i < 16; i++) {
        W[i] = NPT_BytesToInt32Be(&block[4*i]);
    }
    
    // fill W[16..63]
    for (unsigned int i = 16; i < 64; i++) {
        W[i] = NPT_Sha256_Gamma1(W[i - 2]) + W[i - 7] + NPT_Sha256_Gamma0(W[i - 15]) + W[i - 16];
    }        
    
    // compress
     for (unsigned int i = 0; i < 64; ++i) {
         NPT_UInt32 t0 = 
            S[7] + 
            NPT_Sha256_Sigma1(S[4]) + 
            NPT_Sha256_Ch(S[4], S[5], S[6]) + 
            NPT_Sha256_K[i] + 
            W[i];
         NPT_UInt32 t1 = NPT_Sha256_Sigma0(S[0]) + NPT_Sha256_Maj(S[0], S[1], S[2]);
         S[3] += t0;
         S[7]  = t0 + t1;

         NPT_UInt32 t = S[7]; S[7] = S[6]; S[6] = S[5]; S[5] = S[4]; 
         S[4] = S[3]; S[3] = S[2]; S[2] = S[1]; S[1] = S[0]; S[0] = t;
     }  

    // store the local variables back into the state
    for (unsigned i = 0; i < 8; i++) {
        m_State[i] += S[i];
    }    
}

/*----------------------------------------------------------------------
|   NPT_Sha256Digest::GetDigest
+---------------------------------------------------------------------*/
NPT_Result
NPT_Sha256Digest::GetDigest(NPT_DataBuffer& digest)
{
    return ComputeDigest(m_State, 8, true, digest);
}

/*----------------------------------------------------------------------
|   NPT_Md5Digest
+---------------------------------------------------------------------*/
class NPT_Md5Digest : public NPT_BasicDigest
{
public:
    NPT_Md5Digest();
    
    // NPT_Digest methods
    virtual NPT_Result   GetDigest(NPT_DataBuffer& digest);
    virtual unsigned int GetSize() { return 16; }
    
protected:
    // methods
    virtual void CompressBlock(const NPT_UInt8* block);
    
    // members
    NPT_UInt32 m_State[4];
};

/*----------------------------------------------------------------------
|   NPT_Md5Digest::NPT_Md5Digest
+---------------------------------------------------------------------*/
NPT_Md5Digest::NPT_Md5Digest()
{
    m_State[0] = 0x67452301UL;
    m_State[1] = 0xefcdab89UL;
    m_State[2] = 0x98badcfeUL;
    m_State[3] = 0x10325476UL;
}

/*----------------------------------------------------------------------
|   NPT_Md5Digest::CompressBlock
+---------------------------------------------------------------------*/
void
NPT_Md5Digest::CompressBlock(const NPT_UInt8* block)
{
    NPT_UInt32 a,b,c,d,W[16];

    // copy the 512-bit block into W[0..15]
    unsigned int i;
    for (i = 0; i < 16; i++) {
        W[i] = NPT_BytesToInt32Le(&block[4*i]);
    }
    
    // copy the state to local variables
    a = m_State[0];
    b = m_State[1];
    c = m_State[2];
    d = m_State[3];
        
    // round 1
    NPT_Md5_FF(a,b,c,d,W[ 0], 7,0xd76aa478UL)
    NPT_Md5_FF(d,a,b,c,W[ 1],12,0xe8c7b756UL)
    NPT_Md5_FF(c,d,a,b,W[ 2],17,0x242070dbUL)
    NPT_Md5_FF(b,c,d,a,W[ 3],22,0xc1bdceeeUL)
    NPT_Md5_FF(a,b,c,d,W[ 4], 7,0xf57c0fafUL)
    NPT_Md5_FF(d,a,b,c,W[ 5],12,0x4787c62aUL)
    NPT_Md5_FF(c,d,a,b,W[ 6],17,0xa8304613UL)
    NPT_Md5_FF(b,c,d,a,W[ 7],22,0xfd469501UL)
    NPT_Md5_FF(a,b,c,d,W[ 8], 7,0x698098d8UL)
    NPT_Md5_FF(d,a,b,c,W[ 9],12,0x8b44f7afUL)
    NPT_Md5_FF(c,d,a,b,W[10],17,0xffff5bb1UL)
    NPT_Md5_FF(b,c,d,a,W[11],22,0x895cd7beUL)
    NPT_Md5_FF(a,b,c,d,W[12], 7,0x6b901122UL)
    NPT_Md5_FF(d,a,b,c,W[13],12,0xfd987193UL)
    NPT_Md5_FF(c,d,a,b,W[14],17,0xa679438eUL)
    NPT_Md5_FF(b,c,d,a,W[15],22,0x49b40821UL)

    // round 2
    NPT_Md5_GG(a,b,c,d,W[ 1], 5,0xf61e2562UL)
    NPT_Md5_GG(d,a,b,c,W[ 6], 9,0xc040b340UL)
    NPT_Md5_GG(c,d,a,b,W[11],14,0x265e5a51UL)
    NPT_Md5_GG(b,c,d,a,W[ 0],20,0xe9b6c7aaUL)
    NPT_Md5_GG(a,b,c,d,W[ 5], 5,0xd62f105dUL)
    NPT_Md5_GG(d,a,b,c,W[10], 9,0x02441453UL)
    NPT_Md5_GG(c,d,a,b,W[15],14,0xd8a1e681UL)
    NPT_Md5_GG(b,c,d,a,W[ 4],20,0xe7d3fbc8UL)
    NPT_Md5_GG(a,b,c,d,W[ 9], 5,0x21e1cde6UL)
    NPT_Md5_GG(d,a,b,c,W[14], 9,0xc33707d6UL)
    NPT_Md5_GG(c,d,a,b,W[ 3],14,0xf4d50d87UL)
    NPT_Md5_GG(b,c,d,a,W[ 8],20,0x455a14edUL)
    NPT_Md5_GG(a,b,c,d,W[13], 5,0xa9e3e905UL)
    NPT_Md5_GG(d,a,b,c,W[ 2], 9,0xfcefa3f8UL)
    NPT_Md5_GG(c,d,a,b,W[ 7],14,0x676f02d9UL)
    NPT_Md5_GG(b,c,d,a,W[12],20,0x8d2a4c8aUL)

    // round 3
    NPT_Md5_HH(a,b,c,d,W[ 5], 4,0xfffa3942UL)
    NPT_Md5_HH(d,a,b,c,W[ 8],11,0x8771f681UL)
    NPT_Md5_HH(c,d,a,b,W[11],16,0x6d9d6122UL)
    NPT_Md5_HH(b,c,d,a,W[14],23,0xfde5380cUL)
    NPT_Md5_HH(a,b,c,d,W[ 1], 4,0xa4beea44UL)
    NPT_Md5_HH(d,a,b,c,W[ 4],11,0x4bdecfa9UL)
    NPT_Md5_HH(c,d,a,b,W[ 7],16,0xf6bb4b60UL)
    NPT_Md5_HH(b,c,d,a,W[10],23,0xbebfbc70UL)
    NPT_Md5_HH(a,b,c,d,W[13], 4,0x289b7ec6UL)
    NPT_Md5_HH(d,a,b,c,W[ 0],11,0xeaa127faUL)
    NPT_Md5_HH(c,d,a,b,W[ 3],16,0xd4ef3085UL)
    NPT_Md5_HH(b,c,d,a,W[ 6],23,0x04881d05UL)
    NPT_Md5_HH(a,b,c,d,W[ 9], 4,0xd9d4d039UL)
    NPT_Md5_HH(d,a,b,c,W[12],11,0xe6db99e5UL)
    NPT_Md5_HH(c,d,a,b,W[15],16,0x1fa27cf8UL)
    NPT_Md5_HH(b,c,d,a,W[ 2],23,0xc4ac5665UL)

    // round 4
    NPT_Md5_II(a,b,c,d,W[ 0], 6,0xf4292244UL)
    NPT_Md5_II(d,a,b,c,W[ 7],10,0x432aff97UL)
    NPT_Md5_II(c,d,a,b,W[14],15,0xab9423a7UL)
    NPT_Md5_II(b,c,d,a,W[ 5],21,0xfc93a039UL)
    NPT_Md5_II(a,b,c,d,W[12], 6,0x655b59c3UL)
    NPT_Md5_II(d,a,b,c,W[ 3],10,0x8f0ccc92UL)
    NPT_Md5_II(c,d,a,b,W[10],15,0xffeff47dUL)
    NPT_Md5_II(b,c,d,a,W[ 1],21,0x85845dd1UL)
    NPT_Md5_II(a,b,c,d,W[ 8], 6,0x6fa87e4fUL)
    NPT_Md5_II(d,a,b,c,W[15],10,0xfe2ce6e0UL)
    NPT_Md5_II(c,d,a,b,W[ 6],15,0xa3014314UL)
    NPT_Md5_II(b,c,d,a,W[13],21,0x4e0811a1UL)
    NPT_Md5_II(a,b,c,d,W[ 4], 6,0xf7537e82UL)
    NPT_Md5_II(d,a,b,c,W[11],10,0xbd3af235UL)
    NPT_Md5_II(c,d,a,b,W[ 2],15,0x2ad7d2bbUL)
    NPT_Md5_II(b,c,d,a,W[ 9],21,0xeb86d391UL)
    
    // store the variables back into the state
    m_State[0] += a;
    m_State[1] += b;
    m_State[2] += c;
    m_State[3] += d;
}

/*----------------------------------------------------------------------
|   NPT_Md5Digest::GetDigest
+---------------------------------------------------------------------*/
NPT_Result
NPT_Md5Digest::GetDigest(NPT_DataBuffer& digest)
{
    return ComputeDigest(m_State, 4, false, digest);
}

/*----------------------------------------------------------------------
|   NPT_HmacDigest
|
|   compute Digest(key XOR opad, Digest(key XOR ipad, data))
|   key is the MAC key
|   ipad is the byte 0x36 repeated 64 times
|   opad is the byte 0x5c repeated 64 times
|   and data is the data to authenticate
|
+---------------------------------------------------------------------*/
class NPT_HmacDigest : public NPT_Digest
{
public:
    NPT_HmacDigest(NPT_Digest::Algorithm algorithm,
                   const NPT_UInt8*      key, 
                   NPT_Size              key_size);
   ~NPT_HmacDigest();
   
    // NPT_Digest methods
    virtual NPT_Result Update(const NPT_UInt8* data, NPT_Size data_size) {
        return m_InnerDigest->Update(data, data_size);
    }
    virtual NPT_Result GetDigest(NPT_DataBuffer& buffer);
    virtual unsigned int GetSize() { return m_InnerDigest->GetSize(); }
    
private:
    NPT_Digest* m_InnerDigest;
    NPT_Digest* m_OuterDigest;
};

/*----------------------------------------------------------------------
|   NPT_HmacDigest::NPT_HmacDigest
+---------------------------------------------------------------------*/
NPT_HmacDigest::NPT_HmacDigest(NPT_Digest::Algorithm algorithm,
                               const NPT_UInt8*      key, 
                               NPT_Size              key_size)
{
    NPT_Digest::Create(algorithm, m_InnerDigest);
    NPT_Digest::Create(algorithm, m_OuterDigest);
    
    NPT_UInt8 workspace[NPT_BASIC_DIGEST_BLOCK_SIZE];
    
    // if the key is larger than the block size, use a digest of the key
    if (key_size > NPT_BASIC_DIGEST_BLOCK_SIZE) {
        NPT_Digest* key_digest = NULL;
        NPT_Digest::Create(algorithm, key_digest);
        key_digest->Update(key, key_size);
        NPT_DataBuffer hk;
        key_digest->GetDigest(hk);
        key = hk.GetData();
        key_size = hk.GetDataSize();
        delete key_digest;
    }

    // compute key XOR ipad
    for (unsigned int i = 0; i < key_size; i++) {
        workspace[i] = key[i] ^ 0x36;
    }
    for (unsigned int i = key_size; i < NPT_BASIC_DIGEST_BLOCK_SIZE; i++) {
        workspace[i] = 0x36;
    }
    
    // start the inner digest with (key XOR ipad)
    m_InnerDigest->Update(workspace, NPT_BASIC_DIGEST_BLOCK_SIZE);

    // compute key XOR opad
    for (unsigned int i = 0; i < key_size; i++) {
        workspace[i] = key[i] ^ 0x5c;
    }
    for (unsigned int i = key_size; i < NPT_BASIC_DIGEST_BLOCK_SIZE; i++) {
        workspace[i] = 0x5c;
    }
    
    // start the outer digest with (key XOR opad)
    m_OuterDigest->Update(workspace, NPT_BASIC_DIGEST_BLOCK_SIZE);
}

/*----------------------------------------------------------------------
|   NPT_HmacDigest::~NPT_HmacDigest
+---------------------------------------------------------------------*/
NPT_HmacDigest::~NPT_HmacDigest()
{
    delete m_InnerDigest;
    delete m_OuterDigest;
}

/*----------------------------------------------------------------------
|   NPT_HmacDigest::GetDigest
+---------------------------------------------------------------------*/
NPT_Result
NPT_HmacDigest::GetDigest(NPT_DataBuffer& mac)
{
    // finish the outer digest with the value of the inner digest
    NPT_DataBuffer inner;
    m_InnerDigest->GetDigest(inner);
    m_OuterDigest->Update(inner.GetData(), inner.GetDataSize());
    
    // return the value of the outer digest
    return m_OuterDigest->GetDigest(mac);
}

/*----------------------------------------------------------------------
|   NPT_Digest::Create
+---------------------------------------------------------------------*/
NPT_Result
NPT_Digest::Create(Algorithm algorithm, NPT_Digest*& digest)
{
    switch (algorithm) {
        case ALGORITHM_SHA1:   digest = new NPT_Sha1Digest();   return NPT_SUCCESS;
        case ALGORITHM_SHA256: digest = new NPT_Sha256Digest(); return NPT_SUCCESS;
        case ALGORITHM_MD5:    digest = new NPT_Md5Digest();    return NPT_SUCCESS;
        default: return NPT_ERROR_NOT_SUPPORTED;
    }
}

/*----------------------------------------------------------------------
|   NPT_Hmac::Create
+---------------------------------------------------------------------*/
NPT_Result
NPT_Hmac::Create(NPT_Digest::Algorithm algorithm, 
                 const NPT_UInt8*      key,
                 NPT_Size              key_size,
                 NPT_Digest*&          digest)
{
    switch (algorithm) {
        case NPT_Digest::ALGORITHM_SHA1: 
        case NPT_Digest::ALGORITHM_MD5:
            digest = new NPT_HmacDigest(algorithm, key, key_size); 
            return NPT_SUCCESS;
        default: return NPT_ERROR_NOT_SUPPORTED;
    }
}
