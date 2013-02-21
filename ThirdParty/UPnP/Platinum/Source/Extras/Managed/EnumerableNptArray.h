/*****************************************************************
|
|   Platinum - Managed EnumerableNptArray
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

namespace Enumerables
{

/*----------------------------------------------------------------------
|   EnumerableNptArray
+---------------------------------------------------------------------*/
template<typename T_DotNetType, typename T_NativeType>
private ref class EnumerableNptArray : public IEnumerable<T_DotNetType>
{
private:

	ref class EnumerableNptArrayIterator : public IEnumerator<T_DotNetType>
	{
	private:

		const NPT_Array<T_NativeType>* m_pArray;
		NPT_Ordinal m_Index;

	public:

		virtual property T_DotNetType Current
		{
			T_DotNetType get()
			{
                return marshal_as<T_DotNetType>(*(*m_pArray)[m_Index]);
			}
		}

	private:

		virtual property Object^ Current2
		{
			Object^ get() sealed = System::Collections::IEnumerator::Current::get
			{
				return marshal_as<T_DotNetType>(*(*m_pArray)[m_Index]); // FIXME: This is a problem when T_NativeType is not a pointer (like PLT_DeviceDataReference for example)
			}
		}

	public:

		virtual bool MoveNext()
		{
			if (m_Index < m_pArray->GetItemCount() - 1)
			{
				m_Index++;
			}

			return false;
		}

		virtual void Reset()
		{ 
			m_Index = -1;
		}

	public:

        EnumerableNptArrayIterator(const NPT_Array<T_NativeType>& array)
		{
			m_Index = -1;
			m_pArray = &array;
		}

		~EnumerableNptArrayIterator()
		{
		}
		
	};

private:

    const NPT_Array<T_NativeType>* m_pArray;

public:

	virtual IEnumerator<T_DotNetType>^ GetEnumerator()
	{
		return gcnew EnumerableNptArrayIterator(*m_pArray);
	}

private:

	virtual System::Collections::IEnumerator^ GetEnumerator2() sealed = System::Collections::IEnumerable::GetEnumerator
	{
		return gcnew EnumerableNptArrayIterator(*m_pArray);
	}

public:

    EnumerableNptArray(const NPT_Array<T_NativeType>& array)
	{
		m_pArray = &array;
	}
};

/*----------------------------------------------------------------------
|   EnumerableNptArrayRef
+---------------------------------------------------------------------*/
template<typename T_DotNetType, typename T_NativeType>
private ref class EnumerableNptArrayRef : public IEnumerable<T_DotNetType>
{
private:

    ref class EnumerableNptArrayRefIterator : public IEnumerator<T_DotNetType>
    {
    private:

        const NPT_Array<T_NativeType>* m_pArray;
        NPT_Ordinal m_Index;

    public:

        virtual property T_DotNetType Current
        {
            T_DotNetType get()
            {
                return marshal_as<T_DotNetType>((*m_pArray)[m_Index]);
            }
        }

    private:

        virtual property Object^ Current2
        {
            Object^ get() sealed = System::Collections::IEnumerator::Current::get
            {
                return marshal_as<T_DotNetType>((*m_pArray)[m_Index]); // FIXME: This is a problem when T_NativeType is not a pointer (like PLT_DeviceDataReference for example)
            }
        }

    public:

        virtual bool MoveNext()
        {
            if (m_Index < m_pArray->GetItemCount() - 1)
            {
                m_Index++;
            }

            return false;
        }

        virtual void Reset()
        { 
            m_Index = -1;
        }

    public:

        EnumerableNptArrayRefIterator(const NPT_Array<T_NativeType>& array)
          {
              m_Index = -1;
              m_pArray = &array;
          }

          ~EnumerableNptArrayRefIterator()
          {
          }

    };

private:

    const NPT_Array<T_NativeType>* m_pArray;

public:

    virtual IEnumerator<T_DotNetType>^ GetEnumerator()
    {
        return gcnew EnumerableNptArrayRefIterator(*m_pArray);
    }

private:

    virtual System::Collections::IEnumerator^ GetEnumerator2() sealed = System::Collections::IEnumerable::GetEnumerator
    {
        return gcnew EnumerableNptArrayRefIterator(*m_pArray);
    }

public:

      EnumerableNptArrayRef(const NPT_Array<T_NativeType>& array)
      {
          m_pArray = &array;
      }
};

}
}
