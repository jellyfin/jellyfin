/*****************************************************************
|
|   Platinum - Managed Marshalling
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

namespace msclr { 
namespace interop {

// -------------------------- NPT_IpAddress -------

template<> 
inline String^ marshal_as<String^, NPT_IpAddress> (const NPT_IpAddress& from)
{
	return gcnew String(from.ToString());
}

// -------------------------- NPT_HttpUrl -------

template<> 
inline Uri^ marshal_as<Uri^, NPT_HttpUrl> (const NPT_HttpUrl& from)
{
	if (!from.IsValid())
		return nullptr;

	return gcnew Uri(gcnew String(from.ToString()));
}

// -------------------------- NPT_String -------

template<> 
inline Uri^ marshal_as<Uri^, NPT_String> (const NPT_String& from)
{
	if (from.IsEmpty())
		return nullptr;

	return gcnew Uri(gcnew String(from));
}

template<> 
inline String^ marshal_as<String^, NPT_String> (const NPT_String& from)
{
    return gcnew String(from);
}

// -------------------------- PLT_ProtocolInfo -------

template<> 
inline String^ marshal_as<String^, PLT_ProtocolInfo> (const PLT_ProtocolInfo& from)
{
    return gcnew String(from.ToString());
}

// -------------------------- NPT_TimeInterval -------

template<> 
inline TimeSpan marshal_as<TimeSpan, NPT_TimeInterval> (const NPT_TimeInterval& from)
{
	return TimeSpan(from.ToSeconds() * TimeSpan::TicksPerSecond);
}

// ---------------------------------------------


}}


// -------------------------- generic wrapper marshal macro -------

#define PLATINUM_MANAGED_MARSHAL_AS(dotNetType,nativeType) \
	namespace msclr { namespace interop { \
		template<> \
		inline dotNetType^ marshal_as<dotNetType^, nativeType> (const nativeType& from) \
		{ return gcnew dotNetType(const_cast<nativeType&>(from)); } \
	}}

