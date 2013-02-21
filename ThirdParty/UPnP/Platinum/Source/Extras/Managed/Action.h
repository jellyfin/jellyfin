/*****************************************************************
|
|   Platinum - Managed Action
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

ref class ActionArgumentDescription;
ref class ActionDescription;
ref class Service;

/*----------------------------------------------------------------------
|   Action
+---------------------------------------------------------------------*/
public ref class Action
{
public:

	delegate void ActionResponseDelegate(NeptuneException^ error);

private:

	PLT_ActionReference* m_pHandle;

internal:

	property PLT_ActionReference& Handle
	{
		PLT_ActionReference& get()
		{
			return *m_pHandle;
		}
	}

public:

	property String^ Name
	{
		String^ get()
		{
			return gcnew String((*m_pHandle)->GetActionDesc().GetName());
		}
	}

	property IEnumerable<ActionArgumentDescription^>^ Arguments
	{
		IEnumerable<ActionArgumentDescription^>^ get();
	}

	property Service^ ParentService
	{
		Service^ get();
	}

	property ActionDescription^ Description
	{
		ActionDescription^ get();
	}

public:

	event ActionResponseDelegate^ ActionResponse;

public:

	ActionArgumentDescription^ GetArgument(String^ name);
    Int32 SetArgumentValue(String^ name, String^ value);

internal:

	void HandleActionResponse(NeptuneException^ error, Action^ action);

public:

	virtual Boolean Equals(Object^ obj) override
	{
		if (obj == nullptr)
			return false;

		if (!this->GetType()->IsInstanceOfType(obj))
			return false;

		Action^ a = (Action^)obj;

        return (m_pHandle->AsPointer() == a->m_pHandle->AsPointer());

		/*if (m_pHandle->AsPointer() == a->m_pHandle->AsPointer())
			return true;

		return ((*m_pHandle)->GetActionDesc() == (*a->m_pHandle)->GetActionDesc());*/
	}

internal:

	Action(PLT_ActionReference& action)
	{
		if (action.IsNull())
			throw gcnew ArgumentNullException("action");

		m_pHandle = new PLT_ActionReference(action);
	}

public:

	~Action()
	{
		// clean-up managed

		// clean-up unmanaged
		this->!Action();
	}

	!Action()
	{
		// clean-up unmanaged
		if (m_pHandle != 0)
		{
			delete m_pHandle;

			m_pHandle = 0;
		}
	}

};

}

// marshal wrapper
PLATINUM_MANAGED_MARSHAL_AS(Platinum::Action, PLT_ActionReference);