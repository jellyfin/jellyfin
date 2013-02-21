/*****************************************************************
|
|   Platinum - Managed ControlPointEventBridge
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
#include "ControlPointEventBridge.h"
#include "ControlPoint.h"
#include "DeviceData.h"
#include "Action.h"
#include "Service.h"
#include "EnumerableNptList.h"
#include "StateVariable.h"

NPT_Result Platinum::ControlPointEventBridge::OnDeviceAdded( PLT_DeviceDataReference& device )
{
	m_pControlPoint->OnDeviceAdded(gcnew DeviceData(device));

	return NPT_SUCCESS;
}

NPT_Result Platinum::ControlPointEventBridge::OnDeviceRemoved( PLT_DeviceDataReference& device )
{
	m_pControlPoint->OnDeviceRemoved(gcnew DeviceData(device));

	return NPT_SUCCESS;
}

NPT_Result Platinum::ControlPointEventBridge::OnActionResponse( NPT_Result res, PLT_ActionReference& action, void* userdata )
{
	if (NPT_FAILED(res))
	{
		m_pControlPoint->OnActionResponse(
			gcnew NeptuneException(res),
			gcnew Action(action)
			);
	}
	else
	{
		m_pControlPoint->OnActionResponse(
			nullptr,
			gcnew Action(action)
			);
	}

	return NPT_SUCCESS;
}

NPT_Result Platinum::ControlPointEventBridge::OnEventNotify( PLT_Service* service, NPT_List<PLT_StateVariable*>* vars )
{
	m_pControlPoint->OnEventNotify(
		gcnew Service(*service),
		gcnew Enumerables::EnumerableNptList<StateVariable^, PLT_StateVariable*>(*vars)
		);

	return NPT_SUCCESS;
}
