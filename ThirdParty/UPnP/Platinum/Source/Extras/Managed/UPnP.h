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
#pragma once

namespace Platinum
{

ref class ControlPoint;
ref class DeviceHost;
class NeptuneLoggingBridge;

/*----------------------------------------------------------------------
|   UPnP
+---------------------------------------------------------------------*/
public ref class UPnP
{
public:

	static const unsigned short DefaultPort = 1900;

private:

	PLT_UPnP* m_pHandle;

public:

	void Start()
	{
		Helpers::ThrowOnError(m_pHandle->Start());
	}

	void Stop()
	{
		if (m_pHandle) m_pHandle->Stop();
	}

	void AddControlPoint(ControlPoint^ cp);
    void RemoveControlPoint(ControlPoint^ cp);

    void AddDeviceHost(DeviceHost^ host);
    void RemoveDeviceHost(DeviceHost^ host);

    static List<String^>^ GetIpAddresses()
    {
        return GetIpAddresses(false);
    }
    static List<String^>^ GetIpAddresses(bool include_localhost);

    property bool Running
    {
        bool get()
        {
            return m_pHandle->IsRunning();
        }
    }

public:

	virtual Boolean Equals(Object^ obj) override
	{
		if (obj == nullptr)
			return false;

		if (!this->GetType()->IsInstanceOfType(obj))
			return false;

		return (m_pHandle == ((UPnP^)obj)->m_pHandle);
	}

public:

	static UPnP();

	UPnP()
	{
		m_pHandle = new PLT_UPnP();
	}

	~UPnP()
	{
		// clean-up managed

		// clean-up unmanaged
		this->!UPnP();
	}

	!UPnP()
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

