/*****************************************************************
|
|   Platinum - Managed Helpers
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
#include "NeptuneException.h"
#include "Clix.h"

using namespace clix;

namespace Platinum
{

/*----------------------------------------------------------------------
|   Helpers
+---------------------------------------------------------------------*/
private ref class Helpers
{
public:
	
	static void ThrowOnError(NPT_Result r)
	{
		if (NPT_FAILED(r))
		{
			throw gcnew NeptuneException(r);
		}
	}

	static void ThrowOnErrorButNoSuchItem(NPT_Result r)
	{
		if (NPT_FAILED(r) && (r != NPT_ERROR_NO_SUCH_ITEM))
		{
			throw gcnew NeptuneException(r);
		}
	}

	// this code was part of RouterControl.IO (http://routercontrol.codeplex.com)
	// for more details see:
	// http://www.knopflerfish.org/releases/current/docs/jars/upnp/upnp_api-2.0.0/src/org/osgi/service/upnp/UPnPStateVariable.java
	static Type^ ParseType(const NPT_String& upnpType)
	{
		NPT_String s (upnpType);

		s.MakeLowercase();

		if (s == "string")
			return String::typeid;

		if (s == "char")
			return Char::typeid;

		if (s == "boolean")
			return Boolean::typeid;

		if (s == "ui1")
			return Byte::typeid;

		if (s == "ui2")
			return UInt16::typeid;

		if (s == "ui4")
			return UInt32::typeid;

		if (s == "i1")
			return SByte::typeid;

		if (s == "i2")
			return Int16::typeid;

		if ((s == "i4") || (s == "int"))
			return Int32::typeid;

		if ((s == "r4") || (s == "float"))
			return Single::typeid;

		if ((s == "r8") || (s == "number") || (s == "fixed.14.4"))
			return Double::typeid;

		if ((s == "date") || (s == "datetime") || (s == "datetime.tz"))
			return DateTime::typeid;

		if ((s == "time") || (s == "time.tz"))	// milliseconds since midnight
			return UInt64::typeid;

		if ((s == "bin.base64") || (s == "bin.hex"))
			return array<Byte>::typeid;

		if (s == "uri")
			return Uri::typeid;

		if (s == "uuid")
			return Guid::typeid;

		throw gcnew ArgumentException("unknown type", "upnpType");
	}

	static Object^ ConvertValue(const NPT_String& targetType, const NPT_String& val)
	{
		return ConvertValue(ParseType(targetType), val);
	}

	static Object^ ConvertValue(Type^ targetType, const NPT_String& val)
	{
		String^ strVal = gcnew String(val);

		if (targetType == String::typeid)
			return strVal;

		if (targetType == Char::typeid)
		{
			if (val.IsEmpty())
				throw gcnew ArgumentException("character value is empty", "val");

			return Char(val[0]);
		}

		if (targetType == Boolean::typeid)
			return Convert::ToBoolean(strVal);

		if (targetType == Byte::typeid)
			return Convert::ToByte(strVal);

		if (targetType == SByte::typeid)
			return Convert::ToSByte(strVal);

		if (targetType == UInt16::typeid)
			return Convert::ToUInt16(strVal);

		if (targetType == UInt32::typeid)
			return Convert::ToUInt32(strVal);

		if (targetType == UInt64::typeid)
			return Convert::ToUInt64(strVal);

		if (targetType == Int16::typeid)
			return Convert::ToInt16(strVal);

		if (targetType == Int32::typeid)
			return Convert::ToInt32(strVal);

		if (targetType == Int64::typeid)
			return Convert::ToInt64(strVal);

		if (targetType == Single::typeid)
			return Convert::ToSingle(strVal);

		if (targetType == Double::typeid)
			return Convert::ToDouble(strVal);

		if (targetType == DateTime::typeid)
			return Convert::ToDateTime(strVal);

		if (targetType == array<Byte>::typeid)
			return Convert::FromBase64String(strVal);

		if (targetType == Uri::typeid)
			return marshal_as<Uri^>(val);

		if (targetType == Guid::typeid)
			return gcnew Guid(strVal);

		throw gcnew ArgumentException("unsupported type", "targetType");
	}

};

/*----------------------------------------------------------------------
|   StringConv
+---------------------------------------------------------------------*/
struct StringConv
{
    gcroot<msclr::interop::marshal_context^> c;
    //const char* szAnsi;
    std::string szAnsi;
    
    StringConv(System::String^ s) :
        //c(gcnew msclr::interop::marshal_context),
        //szAnsi(c->marshal_as<E_UTF8>(s))
        szAnsi(marshalString<E_UTF8>(s))
    {}

    ~StringConv()
    {}

    operator const char*() const
    {
        //return szAnsi;
        return szAnsi.c_str();
    }
};

/*----------------------------------------------------------------------
|   StringConvA
+---------------------------------------------------------------------*/
struct StringConvA
{
    char* szAnsi;

    StringConvA(System::String^ s) :
        szAnsi(static_cast<char*>(System::Runtime::InteropServices::Marshal::StringToHGlobalAnsi(s).ToPointer()))
    {}

    ~StringConvA()
    {

        System::Runtime::InteropServices::Marshal::FreeHGlobal(IntPtr(szAnsi));
    }

    operator LPCSTR() const
    {
        return szAnsi;
    }
};

/*----------------------------------------------------------------------
|   StringConvW
+---------------------------------------------------------------------*/
struct StringConvW
{
    wchar_t* szUnicode;

    StringConvW(System::String^ s) :
        szUnicode(static_cast<wchar_t*>(System::Runtime::InteropServices::Marshal::StringToHGlobalUni(s).ToPointer()))
    {}

    ~StringConvW()
    {

        System::Runtime::InteropServices::Marshal::FreeHGlobal(IntPtr(szUnicode));
    }

    operator LPCWSTR() const
    {
        return szUnicode;
    }
};

/*----------------------------------------------------------------------
|   ManagedWrapper
+---------------------------------------------------------------------*/
template<typename T_NativeType>
public ref class ManagedWrapper
{
protected:

    T_NativeType* m_pHandle;
	bool		  m_Owned;

internal:

    property T_NativeType& Handle
    {
        T_NativeType& get()
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

        return (m_pHandle == ((ManagedWrapper^)obj)->m_pHandle);
    }

internal:

	ManagedWrapper() : m_Owned(true)
    {
        m_pHandle = new T_NativeType();
    }

    /*ManagedWrapper(ManagedWrapper^ obj)
    {
        m_pHandle = new T_NativeType(obj->Handle);
    }

	ManagedWrapper(T_NativeType object_class) : m_Owned(true)
    {
        m_pHandle = new T_NativeType(object_class.Handle);
    }*/

	ManagedWrapper(T_NativeType& object_class) : m_Owned(false)
    {
		// IMPORTANT: we're keeping a reference of the native pointer
		// so passing a reference to a local variable allocated on the stack is not OK
        m_pHandle = &object_class;
    }

public:

    ~ManagedWrapper()
    {
        this->!ManagedWrapper();
    }

    !ManagedWrapper()
    {
        if (m_pHandle != 0 && m_Owned)
        {
            delete m_pHandle;
		}

		m_pHandle = 0;
    }
};

}

#define PLATINUM_MANAGED_IMPLEMENT_PROPERTY(propertyType, propertyName, nativeVar, nativePtr) \
property propertyType propertyName {						             \
	propertyType get() {	                                             \
		return (nativePtr##->##nativeVar);	                             \
	}																     \
	void set(propertyType var) {	                                     \
        nativePtr##->##nativeVar = (var);                                \
	}																     \
}

#define PLATINUM_MANAGED_IMPLEMENT_STRING_PROPERTY(propertyType, propertyName, nativeVar, nativePtr) \
property propertyType propertyName {						             \
	propertyType get() {	                                             \
		return marshal_as<propertyType>(nativePtr##->##nativeVar);		 \
	}																     \
	void set(propertyType var) {	                                     \
        std::string s = marshalString<E_UTF8>(var);                      \
        nativePtr##->##nativeVar = s.c_str();                            \
	}																     \
}

#define PLATINUM_MANAGED_IMPLEMENT_OBJECT_PROPERTY(propertyType, propertyName, nativeVar, nativePtr) \
property propertyType propertyName {						             \
	propertyType get() {	                                             \
		return marshal_as<propertyType>(nativePtr##->##nativeVar);	     \
	}																     \
	void set(propertyType var) {                                         \
        nativePtr##->##nativeVar = var->Handle;                          \
	}																     \
}

