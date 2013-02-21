/*****************************************************************
|
|   Platinum - Managed ActionArgumentDescription
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

/*----------------------------------------------------------------------
|   ActionArgumentDirection
+---------------------------------------------------------------------*/
public enum class ActionArgumentDirection
{
	In,
	Out,
	InOut
};

ref class StateVariable;

/*----------------------------------------------------------------------
|   ActionArgumentDescription
+---------------------------------------------------------------------*/
public ref class ActionArgumentDescription
{
private:

	PLT_ArgumentDesc* m_pHandle;

public:

	property String^ Name
	{
		String^ get()
		{
			return gcnew String(m_pHandle->GetName());
		}
	}

	property ActionArgumentDirection Direction
	{
		ActionArgumentDirection get()
		{
			return ParseArgumentDirection(m_pHandle->GetDirection());
		}
	}

	property Boolean HasReturnValue
	{
		Boolean get()
		{
			return Boolean(m_pHandle->HasReturnValue());
		}
	}

	property StateVariable^ RelatedStateVariable
	{
		StateVariable^ get();
	}

private:

	static ActionArgumentDirection ParseArgumentDirection(const NPT_String& dir)
	{
		NPT_String s (dir);

		s.MakeLowercase();

		if (s == "in")
			return ActionArgumentDirection::In;

		if (s == "out")
			return ActionArgumentDirection::Out;

		if (s == "inout")
			return ActionArgumentDirection::InOut;

		if (s == "io")
			return ActionArgumentDirection::InOut;

		throw gcnew ArgumentException("unknown direction");
	}

public:

	virtual Boolean Equals(Object^ obj) override
	{
		if (obj == nullptr)
			return false;

		if (!this->GetType()->IsInstanceOfType(obj))
			return false;

		return (m_pHandle == ((ActionArgumentDescription^)obj)->m_pHandle);
	}

internal:

	ActionArgumentDescription(PLT_ArgumentDesc& devData)
	{
		m_pHandle = &devData;
	}

public:

	~ActionArgumentDescription()
	{
		// clean-up managed

		// clean-up unmanaged
		this->!ActionArgumentDescription();
	}

	!ActionArgumentDescription()
	{
		// clean-up unmanaged
	}

};

}

// marshal wrapper
PLATINUM_MANAGED_MARSHAL_AS(Platinum::ActionArgumentDescription, PLT_ArgumentDesc);
