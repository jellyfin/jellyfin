/*****************************************************************
|
|   Platinum - Simple Device
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
#include "NptTypes.h"
#include "Platinum.h"
#include "PltSimple.h"

NPT_SET_LOCAL_LOGGER("platinum.test.simple")

/*----------------------------------------------------------------------
|   forward references
+---------------------------------------------------------------------*/
extern const char* SCPDXML_SIMPLE;

/*----------------------------------------------------------------------
|   PLT_Simple::PLT_Simple
+---------------------------------------------------------------------*/
PLT_Simple::PLT_Simple(const char* FriendlyName, const char* UUID) :	
    PLT_DeviceHost("/", UUID, "urn:schemas-upnp-org:device:Simple:1", FriendlyName)
{
}

/*----------------------------------------------------------------------
|   PLT_Simple::~PLT_Simple
+---------------------------------------------------------------------*/
PLT_Simple::~PLT_Simple()
{
}

/*----------------------------------------------------------------------
|   PLT_Simple::SetupServices
+---------------------------------------------------------------------*/
NPT_Result
PLT_Simple::SetupServices()
{
    PLT_Service* service = new PLT_Service(
        this,
        "urn:schemas-upnp-org:service:Test:1", 
        "urn:upnp-org:serviceId:Test.001",
        "Test");
    NPT_CHECK_FATAL(service->SetSCPDXML((const char*)SCPDXML_SIMPLE));
    NPT_CHECK_FATAL(AddService(service));

    service->SetStateVariable("Status", "True");

    return NPT_SUCCESS;
}

const char* SCPDXML_SIMPLE =
    "<?xml version=\"1.0\" ?>"
    "  <scpd xmlns=\"urn:schemas-upnp-org:service-1-0\">"
    "    <specVersion>"
    "       <major>1</major>"
    "	    <minor>0</minor>"
    "	 </specVersion>"
    "    <serviceStateTable>"
    "      <stateVariable sendEvents=\"yes\">"
    "        <name>LastChange</name>"
    "        <dataType>string</dataType>"
    "        <defaultValue></defaultValue>"
    "      </stateVariable>"
    "      <stateVariable sendEvents=\"yes\">"
    "        <name>PresetNameList</name>"
    "        <dataType>string</dataType>"
    "        <defaultValue></defaultValue>"
    "      </stateVariable>"
    "    </serviceStateTable>"
    "    <intel_nmpr:X_INTEL_NMPR xmlns:intel_nmpr=\"udn:schemas-intel-com:device-1-0\">2.1</intel_nmpr:X_INTEL_NMPR>"
    "    <dlna:X_DLNADOC xmlns:dlna=\"udn:schemas-dlna-org:device-1-0\">DMP 1.00</dlna:X_DLNADOC>"
    "  </scpd>";

