/*****************************************************************
|
|   Neptune - Toplevel Include
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

#ifndef _NEPTUNE_H_
#define _NEPTUNE_H_

/*----------------------------------------------------------------------
|   flags
+---------------------------------------------------------------------*/
#define NPT_EXTERNAL_USE /* do not expose internal definitions */

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptConfig.h"
#include "NptCommon.h"
#include "NptResults.h"
#include "NptTypes.h"
#include "NptConstants.h"
#include "NptReferences.h"
#include "NptStreams.h"
#include "NptBufferedStreams.h"
#include "NptFile.h"
#include "NptNetwork.h"
#include "NptSockets.h"
#include "NptTime.h"
#include "NptThreads.h"
#include "NptSystem.h"
#include "NptMessaging.h"
#include "NptQueue.h"
#include "NptSimpleMessageQueue.h"
#include "NptSelectableMessageQueue.h"
#include "NptXml.h"
#include "NptStrings.h"
#include "NptArray.h"
#include "NptList.h"
#include "NptMap.h"
#include "NptStack.h"
#include "NptUri.h"
#include "NptHttp.h"
#include "NptDataBuffer.h"
#include "NptUtils.h"
#include "NptRingBuffer.h"
#include "NptBase64.h"
#include "NptConsole.h"
#include "NptLogging.h"
#include "NptSerialPort.h"
#include "NptVersion.h"
#include "NptDynamicLibraries.h"
#include "NptDynamicCast.h"
#include "NptDigest.h"
#include "NptCrypto.h"

// optional modules
#include "NptZip.h"
#include "NptTls.h"

#endif // _NEPTUNE_H_
