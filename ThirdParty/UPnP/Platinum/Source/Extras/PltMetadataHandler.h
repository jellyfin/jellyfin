/*****************************************************************
|
|   Platinum - Metadata Handler
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

#ifndef _PLT_METADATA_HANDLER_H_
#define _PLT_METADATA_HANDLER_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"

/*----------------------------------------------------------------------
|   PLT_MetadataHandler class
+---------------------------------------------------------------------*/
class PLT_MetadataHandler
{
public:
    virtual ~PLT_MetadataHandler() {}

    // metadata overridables
    virtual bool HandleExtension(const char* extension) = 0;
    virtual NPT_Result  Load(NPT_InputStream&  stream, 
                             NPT_TimeInterval  sleeptime = NPT_TimeInterval(.01), 
                             NPT_TimeInterval  timeout = NPT_TimeInterval(30.)) = 0;
    virtual NPT_Result  Save(NPT_OutputStream& stream,
                             NPT_TimeInterval  sleeptime = NPT_TimeInterval(.01), 
                             NPT_TimeInterval  timeout = NPT_TimeInterval(30.)) = 0;

    virtual const char* GetLicenseData(NPT_String& licenseData) = 0;
    virtual NPT_Result  GetCoverArtData(char*& caData, int& len) = 0;
    virtual const char* GetContentID(NPT_String& value) = 0;
    virtual const char* GetTitle(NPT_String& value) = 0;
    virtual const char* GetDescription(NPT_String& value) = 0;
    virtual NPT_Result  GetDuration(NPT_UInt32& seconds) = 0;
    virtual const char* GetProtection(NPT_String& protection) = 0;
    virtual NPT_Result  GetYear(NPT_Size& year) = 0;
    
    // helper functions
    virtual NPT_Result  Load(const char* filename);
    virtual NPT_Result  Save(const char* filename);
};

/*----------------------------------------------------------------------
|   PLT_MetadataHandlerFinder
+---------------------------------------------------------------------*/
class PLT_MetadataHandlerFinder
{
public:
    // methods
    PLT_MetadataHandlerFinder(const char* extension) : m_Extension(extension) {}
    bool operator()(PLT_MetadataHandler* const & handler) const {
        return handler->HandleExtension(m_Extension) ? true : false;
    }

private:
    // members
    NPT_String m_Extension;
};

#endif /* _PLT_METADATA_HANDLER_H_ */
