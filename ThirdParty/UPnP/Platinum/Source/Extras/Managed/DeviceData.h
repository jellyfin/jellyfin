/*****************************************************************
|
|   Platinum - Managed DeviceData
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
|   DeviceIcon
+---------------------------------------------------------------------*/
public ref class DeviceIcon : public ManagedWrapper<PLT_DeviceIcon>
{  
public:

    // properties

    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, MimeType, m_MimeType, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_PROPERTY(Int32, Width, m_Width, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_PROPERTY(Int32, Height, m_Height, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_PROPERTY(Int32, Depth, m_Depth, m_pHandle);
    PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(String^, UrlPath, m_UrlPath, m_pHandle);

internal:

    DeviceIcon(PLT_DeviceIcon& native) :
        ManagedWrapper<PLT_DeviceIcon>(native)
    {}

public:
    DeviceIcon() :
        ManagedWrapper<PLT_DeviceIcon>()
    {}

	DeviceIcon(String^ mimeType, Int32 width, Int32 height, Int32 depth, String^ urlPath) :
        ManagedWrapper<PLT_DeviceIcon>()
    {
		MimeType = mimeType;
		Width = width;
		Height = height;
		Depth = depth;
		UrlPath = urlPath;
	}
};

}

// marshal wrapper
PLATINUM_MANAGED_MARSHAL_AS(Platinum::DeviceIcon, PLT_DeviceIcon);

namespace Platinum
{

ref class Service;

/*----------------------------------------------------------------------
|   DeviceData
+---------------------------------------------------------------------*/
public ref class DeviceData
{
protected:

	PLT_DeviceDataReference* m_pHandle;

public:

	property String^ Description
	{
		String^ get()
		{
			NPT_String s;

			Helpers::ThrowOnError((*m_pHandle)->GetDescription(s));

			return gcnew String(s);
		}
	}

	property Uri^ DescriptionUrl
	{
		Uri^ get()
		{
			return marshal_as<Uri^>((*m_pHandle)->GetDescriptionUrl());
		}
	}

	property Uri^ UrlBase
	{
		Uri^ get()
		{
			return marshal_as<Uri^>((*m_pHandle)->GetURLBase());
		}
	}

	property Uri^ IconUrl
	{
		Uri^ get()
		{
			return marshal_as<Uri^>((*m_pHandle)->GetIconUrl());
		}
	}

	property TimeSpan^ LeaseTime
	{
		TimeSpan^ get()
		{
			return marshal_as<TimeSpan>((*m_pHandle)->GetLeaseTime());
		}
	}

	property String^ UUID
	{
		String^ get()
		{
			return gcnew String((*m_pHandle)->GetUUID());
		}
	}

	property String^ FriendlyName
	{
		String^ get()
		{
			return gcnew String((*m_pHandle)->GetFriendlyName());
		}
	}

	property String^ TypeName
	{
		String^ get()
		{
			return gcnew String((*m_pHandle)->GetType());
		}
	}

	property String^ ModelDescription
	{
		String^ get()
		{
			return gcnew String((*m_pHandle)->GetModelDescription());
		}
	}

	property String^ ParentUUID
	{
		String^ get()
		{
			return gcnew String((*m_pHandle)->GetParentUUID());
		}
	}

	property IEnumerable<Service^>^ Services
	{
		IEnumerable<Service^>^ get();
	}

	property IEnumerable<DeviceData^>^ EmbeddedDevices
	{
		IEnumerable<DeviceData^>^ get();
	}

internal:

    property PLT_DeviceDataReference& Handle
    {
        PLT_DeviceDataReference& get()
        {
            return *m_pHandle;
        }
    }

public:

	DeviceData^ FindEmbeddedDeviceByType(String^ type);
	Service^    FindServiceById(String^ serviceId);
	Service^    FindServiceByType(String^ type);
	Service^    FindServiceBySCPDURL(Uri^ url);
	Service^    FindServiceByControlURL(Uri^ url);
	Service^    FindServiceByEventSubURL(Uri^ url);

public:

	virtual Boolean Equals(Object^ obj) override
	{
		if (obj == nullptr)
			return false;

		if (!this->GetType()->IsInstanceOfType(obj))
			return false;

		return (*m_pHandle == *((DeviceData^)obj)->m_pHandle);
	}

internal:

	DeviceData(PLT_DeviceDataReference& devData)
	{
		if (devData.IsNull())
			throw gcnew ArgumentNullException("devData");

		m_pHandle = new PLT_DeviceDataReference(devData);
	}

	DeviceData(PLT_DeviceData& devData)
	{
		m_pHandle = new PLT_DeviceDataReference(&devData);
	}

public:

	~DeviceData()
	{
        // clean-up managed

        // clean-up unmanaged
		this->!DeviceData();
	}

	!DeviceData()
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
PLATINUM_MANAGED_MARSHAL_AS(Platinum::DeviceData, PLT_DeviceData);
PLATINUM_MANAGED_MARSHAL_AS(Platinum::DeviceData, PLT_DeviceDataReference);
