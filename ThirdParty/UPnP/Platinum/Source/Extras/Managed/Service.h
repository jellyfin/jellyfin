/*****************************************************************
|
|   Platinum - Managed Service
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

namespace Platinum
{

ref class DeviceData;
ref class ActionDescription;
ref class StateVariable;

/*----------------------------------------------------------------------
|   Service
+---------------------------------------------------------------------*/
public ref class Service
{
private:

	PLT_Service* m_pHandle;

internal:

	property PLT_Service& Handle
	{
		PLT_Service& get()
		{
			return *m_pHandle;
		}
	}

public:

	property Uri^ SCPDURL
	{
		Uri^ get()
		{
			return marshal_as<Uri^>(m_pHandle->GetSCPDURL());
		}
	}

	property String^ ServiceID
	{
		String^ get()
		{
			return gcnew String(m_pHandle->GetServiceID());
		}
	}

	property String^ ServiceType
	{
		String^ get()
		{
			return gcnew String(m_pHandle->GetServiceType());
		}
	}

	property DeviceData^ ParentDevice
	{
		DeviceData^ get();
	}

	property IEnumerable<StateVariable^>^ StateVariables
	{
		IEnumerable<StateVariable^>^ get();
	}

	property IEnumerable<ActionDescription^>^ Actions
	{
		IEnumerable<ActionDescription^>^ get();
	}

public:

	ActionDescription^ FindAction(String^ name);
    StateVariable^ FindStateVariable(String^ name);

public:

	virtual Boolean Equals(Object^ obj) override
	{
		if (obj == nullptr)
			return false;

		if (!this->GetType()->IsInstanceOfType(obj))
			return false;

		return (m_pHandle == ((Service^)obj)->m_pHandle);
	}

internal:

	Service(PLT_Service& devData)
	{
		m_pHandle = &devData;
	}

public:

	~Service()
	{
		// clean-up managed

		// clean-up unmanaged
		this->!Service();
	}

	!Service()
	{
		// clean-up unmanaged
	}

};

}

// marshal wrapper
PLATINUM_MANAGED_MARSHAL_AS(Platinum::Service, PLT_Service);
