/*****************************************************************
|
|   Platinum - Managed StateVariable
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

ref class Service;

/*----------------------------------------------------------------------
|   StateVariable
+---------------------------------------------------------------------*/
public ref class StateVariable
{
private:

	PLT_StateVariable* m_pHandle;

public:

	property String^ Name
	{
		String^ get()
		{
			return gcnew String(m_pHandle->GetName());
		}
	}

	property Service^ ParentService
	{
		Service^ get();
	}

	property Type^ DataType
	{
		Type^ get()
		{
			return Helpers::ParseType(m_pHandle->GetDataType());
		}
	}

	property String^ DataTypeString
	{
		String^ get()
		{
			return gcnew String(m_pHandle->GetDataType());
		}
	}

	property Object^ Value
	{
		Object^ get()
		{
			return Helpers::ConvertValue(
				m_pHandle->GetDataType(),
				m_pHandle->GetValue()
				);
		}
	}

	property String^ ValueString
	{
		String^ get()
		{
			return gcnew String(m_pHandle->GetValue());
		}

		void set (String^ value)
		{
			if (value == nullptr)
				throw gcnew ArgumentNullException("value");

			marshal_context c;

			Helpers::ThrowOnError(m_pHandle->SetValue(
				c.marshal_as<const char*>(value)
				));
		}
	}

public:

	void ValidateValue(String^ value)
	{
		if (value == nullptr)
			throw gcnew ArgumentNullException("value");

		marshal_context c;

		Helpers::ThrowOnError(m_pHandle->ValidateValue(
			c.marshal_as<const char*>(value)
			));
	}

public:

	virtual Boolean Equals(Object^ obj) override
	{
		if (obj == nullptr)
			return false;

		if (!this->GetType()->IsInstanceOfType(obj))
			return false;

		return (m_pHandle == ((StateVariable^)obj)->m_pHandle);
	}

internal:

	StateVariable(PLT_StateVariable& devData)
	{
		m_pHandle = &devData;
	}

public:

	~StateVariable()
	{
		// clean-up managed

		// clean-up unmanaged
		this->!StateVariable();
	}

	!StateVariable()
	{
        // clean-up unmanaged
	}

};

}

// marshal wrapper
PLATINUM_MANAGED_MARSHAL_AS(Platinum::StateVariable, PLT_StateVariable);
