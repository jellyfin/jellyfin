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

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "PltMetadataHandler.h"

NPT_SET_LOCAL_LOGGER("platinum.core.metadata.handler")

/*----------------------------------------------------------------------
|   PLT_MetadataHandler::Load
+---------------------------------------------------------------------*/
NPT_Result
PLT_MetadataHandler::Load(const char* filename)
{
    NPT_File file(filename);
    NPT_Result res = file.Open(NPT_FILE_OPEN_MODE_READ);
    if (res != NPT_SUCCESS) {
        NPT_LOG_SEVERE_2("error %d opening : %s\n", res, filename);
    } else {
        NPT_InputStreamReference stream;
        res = file.GetInputStream(stream);
        if (NPT_FAILED(res)) {
            NPT_LOG_SEVERE_2("error %d getting input stream: %s\n", res, filename);
        }
        res = Load(*stream);
        if (NPT_FAILED(res)) {
            NPT_LOG_SEVERE_2("error %d reading header: %s\n", res, filename);
        }
    }

    return res;
}

/*----------------------------------------------------------------------
|   PLT_MetadataHandler::Save
+---------------------------------------------------------------------*/
NPT_Result
PLT_MetadataHandler::Save(const char* filename)
{
    NPT_File file(filename);
    NPT_Result res = file.Open(NPT_FILE_OPEN_MODE_WRITE);
    if (res != NPT_SUCCESS) {
        NPT_LOG_SEVERE_2("error %d opening : %s\n", res, filename);
    } else {
        NPT_OutputStreamReference stream;
        res = file.GetOutputStream(stream);
        if (NPT_FAILED(res)) {
            NPT_LOG_SEVERE_2("error %d getting output stream: %s\n", res, filename);
        }
        res = Save(*stream);
        if (NPT_FAILED(res)) {
            NPT_LOG_SEVERE_2("error %d writing header: %s\n", res, filename);
        }
    }

    return res;
}
