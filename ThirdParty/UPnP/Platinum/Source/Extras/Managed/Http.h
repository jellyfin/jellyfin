/*****************************************************************
|
|   Platinum - Managed Http
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

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/

namespace Platinum
{

/*----------------------------------------------------------------------
|   HttpRequest
+---------------------------------------------------------------------*/
public ref class HttpRequest
{
protected:

    NPT_HttpRequest* m_pHandle;

internal:

    property NPT_HttpRequest& Handle
    {
        NPT_HttpRequest& get()
        {
            return *m_pHandle;
        }
    }

public:

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj == nullptr)
            return false;

        if (!this->GetType()->IsInstanceOfType(obj))
            return false;

        return (m_pHandle == ((HttpRequest^)obj)->m_pHandle);
    }

public:

    // properties
    virtual property Uri^ URI 
    {
        Uri^ get()
        {
            return marshal_as<Uri^>(m_pHandle->GetUrl());
        }
    }

internal:

    // Note: Because a NPT_HttpRequest cannot do a deep copy, 
    // this only refers to the original native object
    HttpRequest(NPT_HttpRequest& native) :
        m_pHandle(&native)
    {
    }
            
public:

    ~HttpRequest()
    {
        // clean-up managed

        // clean-up unmanaged
        this->!HttpRequest();
    }

    !HttpRequest()
    {
        // clean-up unmanaged
    }
};

}

// marshal wrapper
PLATINUM_MANAGED_MARSHAL_AS(Platinum::HttpRequest, NPT_HttpRequest);

namespace Platinum
{

/*----------------------------------------------------------------------
|   HttpRequest
+---------------------------------------------------------------------*/
public ref class HttpResponse
{
protected:

    NPT_HttpResponse* m_pHandle;

internal:

    property NPT_HttpResponse& Handle
    {
        NPT_HttpResponse& get()
        {
            return *m_pHandle;
        }
    }

public:

    virtual Boolean Equals(Object^ obj) override
    {
        if (obj == nullptr)
            return false;

        if (!this->GetType()->IsInstanceOfType(obj))
            return false;

        return (m_pHandle == ((HttpResponse^)obj)->m_pHandle);
    }

internal:

    // Note: Because a NPT_HttpResponse cannot do a deep copy, 
    // this only refers to the original native object
    HttpResponse(NPT_HttpResponse& native) :
        m_pHandle(&native)
    {
    }
         
public:

    ~HttpResponse()
    {
        // clean-up managed

        // clean-up unmanaged
        this->!HttpResponse();
    }

    !HttpResponse()
    {
        // clean-up unmanaged
    }
};

}

// marshal wrapper
PLATINUM_MANAGED_MARSHAL_AS(Platinum::HttpResponse, NPT_HttpResponse);

namespace Platinum
{

/*----------------------------------------------------------------------
|   DeviceSignature
+---------------------------------------------------------------------*/
public enum class DeviceSignature
{
	Unknown,
	XBox,
	PS3,
	WMP,
	Sonos,
	Mac,
	Windows,
	Vlc
};

/*----------------------------------------------------------------------
|   HttpRequestContext
+---------------------------------------------------------------------*/
public ref class HttpRequestContext : ManagedWrapper<PLT_HttpRequestContext>
{

public:

    ref struct SocketAddress {
        String^ ip;
        UInt32  port;
    };

    virtual property SocketAddress^ LocalAddress 
    {
        SocketAddress^ get()
        {
            SocketAddress^ local = gcnew SocketAddress;
            local->ip = marshal_as<String^>(Handle.GetLocalAddress().GetIpAddress().ToString());
            local->port = Handle.GetLocalAddress().GetPort();
            return local;
        }
    }

    virtual property SocketAddress^ RemoteAddress 
    {
        SocketAddress^ get() 
        {
            SocketAddress^ local = gcnew SocketAddress;
            local->ip = marshal_as<String^>(Handle.GetRemoteAddress().GetIpAddress().ToString());
            local->port = Handle.GetRemoteAddress().GetPort();
            return local;
        }
    }

    virtual property HttpRequest^ Request
    {
        HttpRequest^ get()
        {
            return marshal_as<HttpRequest^>(m_pHandle->GetRequest());
        }
    }

	virtual property DeviceSignature Signature
	{
		DeviceSignature get()
		{
			return ParseDeviceSignature(m_pHandle->GetDeviceSignature());
		}
	}
private:

	static DeviceSignature ParseDeviceSignature(PLT_DeviceSignature signature)
	{
		switch (signature)
		{
		case PLT_DEVICE_XBOX:
			return DeviceSignature::XBox;
		case PLT_DEVICE_PS3:
			return DeviceSignature::PS3;
		case PLT_DEVICE_WMP:
			return DeviceSignature::WMP;
		case PLT_DEVICE_SONOS:
			return DeviceSignature::Sonos;
		case PLT_DEVICE_MAC:
			return DeviceSignature::Mac;
		case PLT_DEVICE_WINDOWS:
			return DeviceSignature::Windows;
		case PLT_DEVICE_VLC:
			return DeviceSignature::Vlc;
		default:
			return DeviceSignature::Unknown;
		}
	}


internal:

    HttpRequestContext(PLT_HttpRequestContext& native) :
        ManagedWrapper<PLT_HttpRequestContext>(native)
    {
    }
            
    ~HttpRequestContext()
    {
        // clean-up managed

        // clean-up unmanaged
        this->!HttpRequestContext();
    }

    !HttpRequestContext()
    {
        // clean-up unmanaged
    }
};

}

// marshal wrapper
PLATINUM_MANAGED_MARSHAL_AS(Platinum::HttpRequestContext, PLT_HttpRequestContext);
