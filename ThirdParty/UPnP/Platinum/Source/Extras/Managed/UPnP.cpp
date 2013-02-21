/*****************************************************************
|
|   Platinum - Managed UPnP
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
#include "StdAfx.h"
#include "UPnP.h"
#include "ControlPoint.h"
#include "DeviceHost.h"
#include "NeptuneLoggingBridge.h"

void Platinum::UPnP::AddControlPoint( ControlPoint^ cp )
{
	Helpers::ThrowOnError(m_pHandle->AddCtrlPoint(cp->Handle));
}

void Platinum::UPnP::RemoveControlPoint( ControlPoint^ cp )
{
	Helpers::ThrowOnError(m_pHandle->RemoveCtrlPoint(cp->Handle));
}

void Platinum::UPnP::AddDeviceHost( DeviceHost^ host )
{
    Helpers::ThrowOnError(m_pHandle->AddDevice((PLT_DeviceHostReference)host->Host));
}

void Platinum::UPnP::RemoveDeviceHost( DeviceHost^ host )
{
    Helpers::ThrowOnError(m_pHandle->RemoveDevice((PLT_DeviceHostReference)host->Host));
}

static Platinum::UPnP::UPnP()
{
	NeptuneLoggingBridge::Configure();
}

List<String^>^ Platinum::UPnP::GetIpAddresses(bool include_localhost)
{
    NPT_List<NPT_IpAddress> ips;
    PLT_UPnPMessageHelper::GetIPAddresses(ips, include_localhost); // TODO: Throw on Error?

    List<String^>^ _ips = gcnew List<String^>();
    NPT_List<NPT_IpAddress>::Iterator ip = ips.GetFirstItem();
    while (ip) {
        _ips->Add(gcnew String((*ip).ToString()));
        ++ip;
    }

    return _ips;
}