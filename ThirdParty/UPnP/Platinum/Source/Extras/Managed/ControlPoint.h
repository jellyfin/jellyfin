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
#pragma once

namespace Platinum
{

ref class Action;
ref class ActionDescription;
ref class DeviceData;
ref class Service;
ref class StateVariable;
class ControlPointEventBridge;

/*----------------------------------------------------------------------
|   ControlPoint
+---------------------------------------------------------------------*/
public ref class ControlPoint
{
public:

	delegate void DeviceAddedDelegate(DeviceData^ dev);
	delegate void DeviceRemovedDelegate(DeviceData^ dev);
	delegate void ActionResponseDelegate(NeptuneException^ error, Action^ action);
	delegate void EventNotifyDelegate(Service^ srv, IEnumerable<StateVariable^>^ vars);

private:

	PLT_CtrlPointReference* m_pHandle;
	ControlPointEventBridge* m_pBridge;
	List<DeviceData^>^ m_pDevices;

public:

	property array<DeviceData^>^ Devices
	{
		array<DeviceData^>^ get()
		{
			System::Threading::Monitor::Enter(m_pDevices);

			return m_pDevices->ToArray();

			System::Threading::Monitor::Exit(m_pDevices);
		}
	}

internal:

	property PLT_CtrlPointReference& Handle
	{
		PLT_CtrlPointReference& get()
		{
			return *m_pHandle;
		}
	}

public:

	event DeviceAddedDelegate^ DeviceAdded;
	event DeviceRemovedDelegate^ DeviceRemoved;
	event ActionResponseDelegate^ ActionResponse;
	event EventNotifyDelegate^ EventNotify;

internal:

	void OnDeviceAdded(DeviceData^ dev)
	{
		// add to list
		System::Threading::Monitor::Enter(m_pDevices);

		m_pDevices->Add(dev);

		System::Threading::Monitor::Exit(m_pDevices);

		// handle events
		this->DeviceAdded(dev);
	}

	void OnDeviceRemoved(DeviceData^ dev)
	{
		// handle events
		this->DeviceRemoved(dev);

		// remove from list
		System::Threading::Monitor::Enter(m_pDevices);

		m_pDevices->Remove(dev);

		System::Threading::Monitor::Exit(m_pDevices);
	}

	void OnActionResponse(NeptuneException^ error, Action^ action)
	{
		this->ActionResponse(error, action);
	}

	void OnEventNotify(Service^ srv, IEnumerable<StateVariable^>^ vars)
	{
		this->EventNotify(srv, vars);
	}

public:

	Action^ CreateAction(ActionDescription^ desc);
    void InvokeAction(Action^ action);

	void Subscribe(Service^ srv);
	void Unsubscribe(Service^ srv);

private:

	void RegisterEvents();

public:

	virtual Boolean Equals(Object^ obj) override
	{
		if (obj == nullptr)
			return false;

		if (!this->GetType()->IsInstanceOfType(obj))
			return false;

		return (*m_pHandle == *((ControlPoint^)obj)->m_pHandle);
	}

internal:

	ControlPoint(PLT_CtrlPointReference& ctlPoint)
	{
		if (ctlPoint.IsNull())
			throw gcnew ArgumentNullException("ctlPoint");

		m_pHandle = new PLT_CtrlPointReference(ctlPoint);

		RegisterEvents();
	}

	ControlPoint(PLT_CtrlPoint& ctlPoint)
	{
		m_pHandle = new PLT_CtrlPointReference(&ctlPoint);

		RegisterEvents();
	}

public:

	ControlPoint(String^ autoSearcheviceType)
	{
		if (String::IsNullOrEmpty(autoSearcheviceType))
		{
			throw gcnew ArgumentException("null or empty", "autoSearcheviceType");
		}

		marshal_context c;

		m_pHandle = new PLT_CtrlPointReference(
			new PLT_CtrlPoint(c.marshal_as<const char*>(autoSearcheviceType))
			);

		m_pDevices = gcnew List<DeviceData^>();

		RegisterEvents();
	}

	ControlPoint(bool autoSearch)
	{
		if (autoSearch)
		{
			m_pHandle = new PLT_CtrlPointReference(new PLT_CtrlPoint());
		}
		else
		{
			m_pHandle = new PLT_CtrlPointReference(new PLT_CtrlPoint(0));
		}

		m_pDevices = gcnew List<DeviceData^>();

		RegisterEvents();
	}

	~ControlPoint()
	{
		// clean-up managed

		// clean-up unmanaged
		this->!ControlPoint();
	}

	!ControlPoint();

};

}

// marshal wrapper
PLATINUM_MANAGED_MARSHAL_AS(Platinum::ControlPoint, PLT_CtrlPoint);
