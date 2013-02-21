/*****************************************************************
|
|   Platinum - Managed ControlPoint
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
#include "ControlPoint.h"
#include "ControlPointEventBridge.h"
#include "Action.h"
#include "ActionDescription.h"
#include "Service.h"

void Platinum::ControlPoint::RegisterEvents()
{
	if (!m_pBridge)
	{
		m_pBridge = new ControlPointEventBridge(this);
	}

	Helpers::ThrowOnError((*m_pHandle)->AddListener(m_pBridge));
}

Platinum::Action^ Platinum::ControlPoint::CreateAction( ActionDescription^ desc )
{
	if (desc == nullptr)
		throw gcnew ArgumentNullException("desc");

	// create action
	PLT_ActionReference r(new PLT_Action(desc->Handle));

	return gcnew Action(r);
}


void Platinum::ControlPoint::InvokeAction(Action^ action)
{
    // register events
    this->ActionResponse += gcnew ActionResponseDelegate(action, &Action::HandleActionResponse);

    Helpers::ThrowOnError(
        (*m_pHandle)->InvokeAction(action->Handle)
        );
}

void Platinum::ControlPoint::Subscribe( Service^ srv )
{
	if (srv == nullptr)
		throw gcnew ArgumentNullException("srv");

	Helpers::ThrowOnError(
		(*m_pHandle)->Subscribe(&srv->Handle, false)
		);
}

void Platinum::ControlPoint::Unsubscribe( Service^ srv )
{
	if (srv == nullptr)
		throw gcnew ArgumentNullException("srv");

	Helpers::ThrowOnError(
		(*m_pHandle)->Subscribe(&srv->Handle, true)
		);
}

Platinum::ControlPoint::!ControlPoint()
{
	// clean-up unmanaged
	if (m_pHandle != 0)
	{
        // remove listener first, is it necessary?
        if (m_pBridge) (*m_pHandle)->RemoveListener(m_pBridge);

		delete m_pHandle;

		m_pHandle = 0;
	}

	if (m_pBridge != 0)
	{
		delete m_pBridge;

		m_pBridge = 0;
	}
}
