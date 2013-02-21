/*****************************************************************
|
|   Platinum - Top Level Include
|
| Copyright (c) 2004-2010, Plutinosoft, LLC.
| All rights reserved.
| http://www.plutinosoft.com
|
| This program is free software; you can redistribute it and/or
| modify it under the terms of the GNU General Public License
| as published by the Free Software Foundation; either version 2
| of the License, or (at your option) any later version.
|
| OEMs, ISVs, VARs and other distributors that combine and 
| distribute commercially licensed software with Platinum software
| and do not wish to distribute the source code for the commercially
| licensed software under version 2, or (at your option) any later
| version, of the GNU General Public License (the "GPL") must enter
| into a commercial license agreement with Plutinosoft, LLC.
| licensing@plutinosoft.com
| 
| This program is distributed in the hope that it will be useful,
| but WITHOUT ANY WARRANTY; without even the implied warranty of
| MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
| GNU General Public License for more details.
|
| You should have received a copy of the GNU General Public License
| along with this program; see the file LICENSE.txt. If not, write to
| the Free Software Foundation, Inc., 
| 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
| http://www.gnu.org/licenses/gpl-2.0.html
|
 ****************************************************************/
/** @file
 Master Header file included by Platinum client applications.
 
 Client Applications should only need to include this file, as it 
 includes all the more specific include files required to use the API
 */

/** 
@mainpage Platinum UPnP SDK

@section intro Introduction

The Platinum SDK contains all the software components necessary to 
build and use the Platinum UPnP Framework. This includes
the Platinum framework and the Neptune C++ runtime
library.

@section architecture Architecture

The Platinum framework consists of a core framework that implements the UPnP
core specifications including GENA, SOAP and SSDP. Building on top of that, the 
Platinum framework provides the foundation for UPnP AV Media Server and 
Media Renderer compliant implementations.
 
The Platinum framework leverages the Neptune C++ runtime library which offers an 
elegant platform abstraction layer for multithreading, file system and 
network operations. Additionally, it provides support for XML parsing, string and time
manipulation, template based linked-lists, stacks and arrays, and a configurable 
cross-platform logging system.
 
*/

#ifndef _PLATINUM_H_
#define _PLATINUM_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "PltUPnP.h"
#include "PltCtrlPoint.h"
#include "PltDeviceData.h"
#include "PltHttpServer.h"
#include "PltVersion.h"

#include "PltMimeType.h"
#include "PltProtocolInfo.h"
#include "PltAction.h"
#include "PltArgument.h"
#include "PltConstants.h"
#include "PltCtrlPointTask.h"
#include "PltDatagramStream.h"
#include "PltDeviceHost.h"
#include "PltEvent.h"
#include "PltHttp.h"
#include "PltHttpClientTask.h"
#include "PltHttpServer.h"
#include "PltHttpServerTask.h"
#include "PltService.h"
#include "PltSsdp.h"
#include "PltStateVariable.h"
#include "PltTaskManager.h"
#include "PltThreadTask.h"
#include "PltUtilities.h"

#include "PltMediaServer.h"
#include "PltMediaBrowser.h"
#include "PltMediaRenderer.h"
#include "PltMediaController.h"
#include "PltDidl.h"
#include "PltFileMediaServer.h"
#include "PltMediaCache.h"
#include "PltMediaItem.h"
#include "PltSyncMediaBrowser.h"

#include "PltXbox360.h"
#include "PltMediaConnect.h"

#include "PltDownloader.h"
#include "PltStreamPump.h"
#include "PltFrameBuffer.h"
#include "PltFrameServer.h"
#include "PltFrameStream.h"
#include "PltRingBufferStream.h"

#endif /* _PLATINUM_H_ */
