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

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "StdAfx.h"
#include "DeviceData.h"
#include "Service.h"
#include "EnumerableNptArray.h"

IEnumerable<Platinum::Service^>^ Platinum::DeviceData::Services::get()
{
	return gcnew Enumerables::EnumerableNptArray<Service^, PLT_Service*>(
		(*m_pHandle)->GetServices()
		);
}

IEnumerable<Platinum::DeviceData^>^ Platinum::DeviceData::EmbeddedDevices::get()
{
	return gcnew Enumerables::EnumerableNptArrayRef<DeviceData^, PLT_DeviceDataReference>(
		(*m_pHandle)->GetEmbeddedDevices()
		);
}

Platinum::DeviceData^ Platinum::DeviceData::FindEmbeddedDeviceByType( String^ type )
{
	if (String::IsNullOrEmpty(type))
		throw gcnew ArgumentException("null or empty", "type");

	PLT_DeviceDataReference r;
	marshal_context c;

	Helpers::ThrowOnErrorButNoSuchItem((*m_pHandle)->FindEmbeddedDeviceByType(
		c.marshal_as<const char*>(type),
		r
		));

	if (r.IsNull())
		return nullptr;

	return marshal_as<DeviceData^>(r);
}

Platinum::Service^ Platinum::DeviceData::FindServiceById( String^ id )
{
	if (String::IsNullOrEmpty(id))
		throw gcnew ArgumentException("null or empty", "id");

	PLT_Service* r = 0;
	marshal_context c;

	Helpers::ThrowOnErrorButNoSuchItem((*m_pHandle)->FindServiceById(
		c.marshal_as<const char*>(id),
		r
		));

	if (!r)
		return nullptr;

	return marshal_as<Service^>(*r);
}

Platinum::Service^ Platinum::DeviceData::FindServiceByType( String^ type )
{
	if (String::IsNullOrEmpty(type))
		throw gcnew ArgumentException("null or empty", "type");

	PLT_Service* r = 0;
	marshal_context c;

	Helpers::ThrowOnErrorButNoSuchItem((*m_pHandle)->FindServiceByType(
		c.marshal_as<const char*>(type),
		r
		));

	if (!r)
		return nullptr;

	return marshal_as<Service^>(*r);
}

Platinum::Service^ Platinum::DeviceData::FindServiceBySCPDURL( Uri^ url )
{
	if (url == nullptr)
		throw gcnew ArgumentNullException("url");

	PLT_Service* r = 0;
	marshal_context c;

	Helpers::ThrowOnErrorButNoSuchItem((*m_pHandle)->FindServiceBySCPDURL(
		c.marshal_as<const char*>(url->ToString()),
		r
		));

	if (!r)
		return nullptr;

	return marshal_as<Service^>(*r);
}

Platinum::Service^ Platinum::DeviceData::FindServiceByControlURL( Uri^ url )
{
	if (url == nullptr)
		throw gcnew ArgumentNullException("url");

	PLT_Service* r = 0;
	marshal_context c;

	Helpers::ThrowOnErrorButNoSuchItem((*m_pHandle)->FindServiceByControlURL(
		c.marshal_as<const char*>(url->ToString()),
		r
		));

	if (!r)
		return nullptr;

	return marshal_as<Service^>(*r);
}

Platinum::Service^ Platinum::DeviceData::FindServiceByEventSubURL( Uri^ url )
{
	if (url == nullptr)
		throw gcnew ArgumentNullException("url");

	PLT_Service* r = 0;
	marshal_context c;

	Helpers::ThrowOnErrorButNoSuchItem((*m_pHandle)->FindServiceByEventSubURL(
		c.marshal_as<const char*>(url->ToString()),
		r
		));

	if (!r)
		return nullptr;

	return marshal_as<Service^>(*r);
}
