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

/** @file
 UPnP AV Media MimeType.
 */

#ifndef _PLT_MIMETYPE_H_
#define _PLT_MIMETYPE_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "PltHttp.h"
/*----------------------------------------------------------------------
|   forward declarations
+---------------------------------------------------------------------*/
class PLT_HttpRequestContext;

/*----------------------------------------------------------------------
|   PLT_MimeType
+---------------------------------------------------------------------*/
class PLT_MimeType
{
public:
    virtual ~PLT_MimeType() {}

    static const char* GetMimeType(const NPT_String&             filename, 
                                   const PLT_HttpRequestContext* context = NULL);
    static const char* GetMimeType(const NPT_String&   filename,
                                   PLT_DeviceSignature signature = PLT_DEVICE_UNKNOWN);
    
    static const char* GetMimeTypeFromExtension(const NPT_String&             extension, 
                                                const PLT_HttpRequestContext* context = NULL);
    static const char* GetMimeTypeFromExtension(const NPT_String&   extension,
                                                PLT_DeviceSignature signature = PLT_DEVICE_UNKNOWN);

private:
    PLT_MimeType() {}

};

#endif /* _PLT_MIMETYPE_H_ */
