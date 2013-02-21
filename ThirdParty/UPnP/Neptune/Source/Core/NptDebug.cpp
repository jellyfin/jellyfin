/*****************************************************************
|
|   Neptune - Debug Utilities
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

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include <stdarg.h>
#include "NptUtils.h"
#include "NptDebug.h"

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
#define NPT_DEBUG_LOCAL_BUFFER_SIZE 1024
#define NPT_DEBUG_BUFFER_INCREMENT  4096
#define NPT_DEBUG_BUFFER_MAX_SIZE   65536

/*----------------------------------------------------------------------
|   NPT_Debug
+---------------------------------------------------------------------*/
void
NPT_Debug(const char* format, ...)
{
#if defined(NPT_DEBUG)
    char         local_buffer[NPT_DEBUG_LOCAL_BUFFER_SIZE];
    unsigned int buffer_size = NPT_DEBUG_LOCAL_BUFFER_SIZE;
    char*        buffer = local_buffer;
    va_list      args;

    va_start(args, format);

    for(;;) {
        int result;

        /* try to format the message (it might not fit) */
        result = NPT_FormatStringVN(buffer, buffer_size-1, format, args);
        buffer[buffer_size-1] = 0; /* force a NULL termination */
        if (result >= 0) break;

        /* the buffer was too small, try something bigger */
        buffer_size = (buffer_size+NPT_DEBUG_BUFFER_INCREMENT)*2;
        if (buffer_size > NPT_DEBUG_BUFFER_MAX_SIZE) break;
        if (buffer != local_buffer) delete[] buffer;
        buffer = new char[buffer_size];
        if (buffer == NULL) return;
    }

    NPT_DebugOutput(buffer);
    if (buffer != local_buffer) delete[] buffer;

    va_end(args);
#else
    NPT_COMPILER_UNUSED(format);
#endif
}
