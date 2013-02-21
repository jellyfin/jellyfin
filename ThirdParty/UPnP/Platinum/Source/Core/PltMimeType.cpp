/*****************************************************************
|
|   Platinum - AV Media MimeType
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

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "PltMimeType.h"
#include "PltHttp.h"
//#include "NptLogging.h"

//NPT_SET_LOCAL_LOGGER("platinum.media.server.mimetype")

/*----------------------------------------------------------------------
|   globals
+---------------------------------------------------------------------*/
const NPT_HttpFileRequestHandler_FileTypeMapEntry 
PLT_HttpFileRequestHandler_DefaultFileTypeMap[] = {
    {"l16",  "audio/L16;rate=44100;channels=2"},
    {"l16m",  "audio/L16;rate=44100;channels=1"},
    
    {"wav",  "audio/wav"},
    {"wavm", "audio/wav"},
    {"alac", "audio/x-alac"},
    //{"wavm",  "audio/x-wav"},
    {"l16m32",  "audio/L16;rate=32000;channels=1"}
};

const NPT_HttpFileRequestHandler_FileTypeMapEntry 
PLT_HttpFileRequestHandler_360FileTypeMap[] = {
    {"l16",  "audio/L16"},
    {"l16m", "audio/L16"},
    {"l16m32",  "audio/L16"},
    {"avi",  "video/avi"},
    {"divx", "video/avi"},
    {"xvid", "video/avi"},
    {"mov",  "video/quicktime"}
};

const NPT_HttpFileRequestHandler_FileTypeMapEntry 
PLT_HttpFileRequestHandler_PS3FileTypeMap[] = {
    {"avi",  "video/x-msvideo"},
    {"divx", "video/x-msvideo"},
    {"xvid", "video/x-msvideo"},
    {"mov",  "video/mp4"}
};

const NPT_HttpFileRequestHandler_FileTypeMapEntry 
PLT_HttpFileRequestHandler_SonosFileTypeMap[] = {
    {"wav", "audio/wav"},
};

/*----------------------------------------------------------------------
 |   PLT_MimeType::GetMimeType
 +---------------------------------------------------------------------*/
const char* 
PLT_MimeType::GetMimeType(const NPT_String&             filename,
                          const PLT_HttpRequestContext* context /* = NULL */)
{
    return GetMimeType(filename, context?PLT_HttpHelper::GetDeviceSignature(context->GetRequest()):PLT_DEVICE_UNKNOWN);
}

/*----------------------------------------------------------------------
|   PLT_MimeType::GetMimeType
+---------------------------------------------------------------------*/
const char* 
PLT_MimeType::GetMimeType(const NPT_String&   filename,
                          PLT_DeviceSignature signature /* = PLT_DEVICE_UNKNOWN */)
{
    int last_dot = filename.ReverseFind('.');
    if (last_dot >= 0) { // passing just the extension is ok (ex .mp3)
        NPT_String extension = filename.GetChars()+last_dot+1;
        return GetMimeTypeFromExtension(extension, signature);
    }

    return "application/octet-stream";
}

/*----------------------------------------------------------------------
 |   PLT_MimeType::GetMimeTypeFromExtension
 +---------------------------------------------------------------------*/
const char* 
PLT_MimeType::GetMimeTypeFromExtension(const NPT_String&             extension,
                                       const PLT_HttpRequestContext* context /* = NULL */)
{
    return GetMimeTypeFromExtension(extension, context?PLT_HttpHelper::GetDeviceSignature(context->GetRequest()):PLT_DEVICE_UNKNOWN);
}

/*----------------------------------------------------------------------
|   PLT_MimeType::GetMimeTypeFromExtension
+---------------------------------------------------------------------*/
const char* 
PLT_MimeType::GetMimeTypeFromExtension(const NPT_String&   extension,
                                       PLT_DeviceSignature signature /* = PLT_DEVICE_UNKNOWN */)
{
    if (signature != PLT_DEVICE_UNKNOWN) {
        // look for special case for 360
        if (signature == PLT_DEVICE_XBOX || signature == PLT_DEVICE_WMP ) {
			for (unsigned int i=0; i<NPT_ARRAY_SIZE(PLT_HttpFileRequestHandler_360FileTypeMap); i++) {
                if (extension.Compare(PLT_HttpFileRequestHandler_360FileTypeMap[i].extension, true) == 0) {
                    return PLT_HttpFileRequestHandler_360FileTypeMap[i].mime_type;
                }
            }

            // fallback to default if not found
		} else if (signature == PLT_DEVICE_PS3) {
            for (unsigned int i=0; i<NPT_ARRAY_SIZE(PLT_HttpFileRequestHandler_PS3FileTypeMap); i++) {
                if (extension.Compare(PLT_HttpFileRequestHandler_PS3FileTypeMap[i].extension, true) == 0) {
                    return PLT_HttpFileRequestHandler_PS3FileTypeMap[i].mime_type;
                }
            }

            // fallback to default if not found
        } else if (signature == PLT_DEVICE_SONOS) {
            for (unsigned int i=0; i<NPT_ARRAY_SIZE(PLT_HttpFileRequestHandler_SonosFileTypeMap); i++) {
                if (extension.Compare(PLT_HttpFileRequestHandler_SonosFileTypeMap[i].extension, true) == 0) {
                    return PLT_HttpFileRequestHandler_SonosFileTypeMap[i].mime_type;
                }
            }
            
            // fallback to default if not found
        }
    }
    
    // dlna custom ones
    for (unsigned int i=0; i<NPT_ARRAY_SIZE(PLT_HttpFileRequestHandler_DefaultFileTypeMap); i++) {
        if (extension.Compare(PLT_HttpFileRequestHandler_DefaultFileTypeMap[i].extension, true) == 0) {
            return PLT_HttpFileRequestHandler_DefaultFileTypeMap[i].mime_type;
        }
    }
    
    const char* type = NPT_HttpFileRequestHandler::GetDefaultContentType(extension);
    return type?type:"application/octet-stream";
}
