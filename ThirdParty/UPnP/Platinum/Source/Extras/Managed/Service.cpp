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

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "StdAfx.h"
#include "Service.h"
#include "ActionDescription.h"
#include "StateVariable.h"
#include "DeviceData.h"
#include "EnumerableNptArray.h"
#include "EnumerableNptList.h"

Platinum::DeviceData^ Platinum::Service::ParentDevice::get()
{
	return marshal_as<DeviceData^>(*m_pHandle->GetDevice());
}

IEnumerable<Platinum::StateVariable^>^ Platinum::Service::StateVariables::get()
{
	return gcnew Enumerables::EnumerableNptList<StateVariable^, PLT_StateVariable*>(
		m_pHandle->GetStateVariables()
		);
}

IEnumerable<Platinum::ActionDescription^>^ Platinum::Service::Actions::get()
{
	return gcnew Enumerables::EnumerableNptArray<ActionDescription^, PLT_ActionDesc*>(
		m_pHandle->GetActionDescs()
		);
}

Platinum::ActionDescription^ Platinum::Service::FindAction( String^ name )
{
	if (String::IsNullOrEmpty(name))
		throw gcnew ArgumentException("null or empty", "name");

	marshal_context c;

	PLT_ActionDesc* d = m_pHandle->FindActionDesc(
		c.marshal_as<const char*>(name)
		);

	if (!d)
		return nullptr;

	return marshal_as<ActionDescription^>(*d);
}

Platinum::StateVariable^ Platinum::Service::FindStateVariable( String^ name )
{
	if (String::IsNullOrEmpty(name))
		throw gcnew ArgumentException("null or empty", "name");

	marshal_context c;

	PLT_StateVariable* d = m_pHandle->FindStateVariable(
		c.marshal_as<const char*>(name)
		);

	if (!d)
		return nullptr;

	return marshal_as<StateVariable^>(*d);
}

