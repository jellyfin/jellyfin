/*****************************************************************
|
|   Platinum - DeviceHost
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
#pragma once

#include "DeviceData.h"

namespace Platinum
{

/*----------------------------------------------------------------------
|   DeviceHost
+---------------------------------------------------------------------*/
public ref class DeviceHost : public DeviceData
{
private:

	PLT_DeviceHostReference* m_pHostHandle;

public:

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj == nullptr)
            return false;

        if (!this->GetType()->IsInstanceOfType(obj))
            return false;

        return (*m_pHandle == ((DeviceHost^)obj)->Handle);
    }

internal:

	property PLT_DeviceHostReference& Host
	{
		PLT_DeviceHostReference& get()
		{
			return *m_pHostHandle;
		}
	}

internal:

	DeviceHost(PLT_DeviceHostReference& devHost) : 
		m_pHostHandle(new PLT_DeviceHostReference(devHost)),
		DeviceData((PLT_DeviceDataReference&)*m_pHostHandle)
	{
    }

    DeviceHost(PLT_DeviceHost& devHost) : 
		m_pHostHandle(new PLT_DeviceHostReference(&devHost)),  
		DeviceData((PLT_DeviceDataReference&)*m_pHostHandle) // we must make sure to pass our newly created ref object
	{
    }

public:

    void setLeaseTime(TimeSpan^ lease)
    {
        (*m_pHostHandle)->SetLeaseTime(NPT_TimeInterval((double)lease->TotalSeconds));
    }

	NPT_Result AddIcon(DeviceIcon^ icon, array<Byte>^ data)
	{
		pin_ptr<Byte> pinnedBuffer = &data[0];
		return (*m_pHostHandle)->AddIcon(icon->Handle, (const void*)pinnedBuffer, data->Length, true);
	}

    ~DeviceHost()
    {
        // clean-up managed

        // clean-up unmanaged
        this->!DeviceHost();
    }

    !DeviceHost()
    {
        // clean-up unmanaged
		if (m_pHostHandle != 0)
		{
			delete m_pHostHandle;

			m_pHostHandle = 0;
		}
    }

};

}

// marshal wrapper
PLATINUM_MANAGED_MARSHAL_AS(Platinum::DeviceHost, PLT_DeviceHost);
PLATINUM_MANAGED_MARSHAL_AS(Platinum::DeviceHost, PLT_DeviceHostReference);