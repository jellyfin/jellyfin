/*****************************************************************
|
|   abstraction layer for axTLS
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

#ifndef _OS_PORT_H_
#define _OS_PORT_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#if defined(WIN32)
#include <windows.h>
#include <malloc.h>
typedef UINT8 uint8_t;
typedef INT8 int8_t;
typedef UINT16 uint16_t;
typedef INT16 int16_t;
typedef UINT32 uint32_t;
typedef INT32 int32_t;
typedef UINT64 uint64_t;
typedef INT64 int64_t;
#else
#include <stdint.h>
#endif

/*----------------------------------------------------------------------
|   types
+---------------------------------------------------------------------*/
typedef struct SSL_SOCKET SSL_SOCKET;

struct SSL_SOCKET {
    int (*Read)(SSL_SOCKET* self, void* buffer, unsigned int size);
    int (*Write)(SSL_SOCKET* self, const void* buffer, unsigned int size);
};

typedef struct {
    uint32_t year;        /* year                      */
    uint32_t month;       /* month of the year (1-12)  */
    uint32_t day;         /* day of the month (1-31)   */
    uint32_t hours;       /* hours (0-23)              */
    uint32_t minutes;     /* minutes (0-59)            */
    uint32_t seconds;     /* seconds (0-59)            */
} SSL_DateTime;
int SSL_DateTime_Before(const SSL_DateTime* t1, const SSL_DateTime* t2);
void SSL_DateTime_Now(SSL_DateTime* now);
uint64_t SSL_GetRandomSeed();

#define STDCALL
#define EXP_FUNC

#if defined(__cplusplus) 
typedef class NPT_Mutex* SSL_CTX_MUTEX_TYPE;
#else
typedef void* SSL_CTX_MUTEX_TYPE;
#endif
void SSL_Mutex_Create(SSL_CTX_MUTEX_TYPE* mutex);
void SSL_Mutex_Destroy(SSL_CTX_MUTEX_TYPE mutex);
void SSL_Mutex_Lock(SSL_CTX_MUTEX_TYPE mutex);
void SSL_Mutex_Unlock(SSL_CTX_MUTEX_TYPE mutex);
#define SSL_CTX_MUTEX_INIT(_mutex) SSL_Mutex_Create(&_mutex)
#define SSL_CTX_MUTEX_DESTROY(_mutex) do {SSL_Mutex_Destroy(_mutex); _mutex = NULL; } while(0)
#define SSL_CTX_LOCK(_mutex) SSL_Mutex_Lock(_mutex)
#define SSL_CTX_UNLOCK(_mutex) SSL_Mutex_Unlock(_mutex)

#define TTY_FLUSH()
#define SOCKET_BLOCK(X)
#define SOCKET_READ(s,b,z) (s)->Read((s), (b), (z))
#define SOCKET_WRITE(s,b,z) (s)->Write((s), (b), (z))

void SSL_Sha256_ComputeDigest(const unsigned char* buffer, unsigned int buffer_length, unsigned char* digest);

#endif /* _OS_PORT_H_ */
