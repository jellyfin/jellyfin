/*****************************************************************
|
|   Platinum - Managed EnumerableNptList
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
|   EnumerableNptList
+---------------------------------------------------------------------*/
template<typename T_DotNetType, typename T_NativeType>
private ref class EnumerableNptList : public IEnumerable<T_DotNetType>
{
private:

	ref class EnumerableNptListIterator : public IEnumerator<T_DotNetType>
	{
	private:

		const NPT_List<T_NativeType>* m_pList;
		typename NPT_List<T_NativeType>::Iterator* m_pIt;

	public:

		virtual property T_DotNetType Current
		{
			T_DotNetType get()
			{
				return marshal_as<T_DotNetType>(***m_pIt); // FIXME: This is a problem when T_NativeType is not a pointer (like PLT_DeviceDataReference for example)
			}
		}

	private:

		virtual property Object^ Current2
		{
			Object^ get() sealed = System::Collections::IEnumerator::Current::get
			{
				return marshal_as<T_DotNetType>(***m_pIt); // FIXME: This is a problem when T_NativeType is not a pointer (like PLT_DeviceDataReference for example)
			}
		}

	public:

		virtual bool MoveNext()
		{
			if (!m_pIt)
			{
				m_pIt = &m_pList->GetFirstItem();
			}
			else
			{
				(*m_pIt)++;
			}

			return *m_pIt;
		}

		virtual void Reset()
		{ 
			m_pIt = 0;
		}

	public:

		EnumerableNptListIterator(const NPT_List<T_NativeType>& list)
		{
			m_pIt = 0;
			m_pList = &list;
		}

		~EnumerableNptListIterator()
		{
		}
		
	};

private:

	const NPT_List<T_NativeType>* m_pList;

public:

	virtual IEnumerator<T_DotNetType>^ GetEnumerator()
	{
		return gcnew EnumerableNptListIterator(*m_pList);
	}

private:

	virtual System::Collections::IEnumerator^ GetEnumerator2() sealed = System::Collections::IEnumerable::GetEnumerator
	{
		return gcnew EnumerableNptListIterator(*m_pList);
	}

public:

	EnumerableNptList(const NPT_List<T_NativeType>& list)
	{
		m_pList = &list;
	}
};

/*----------------------------------------------------------------------
|   EnumerableNptListRef
+---------------------------------------------------------------------*/
template<typename T_DotNetType, typename T_NativeType>
private ref class EnumerableNptListRef : public IEnumerable<T_DotNetType>
{
private:

    ref class EnumerableNptListRefIterator : public IEnumerator<T_DotNetType>
    {
    private:

        const NPT_List<T_NativeType>* m_pList;
        typename NPT_List<T_NativeType>::Iterator* m_pIt;

    public:

        virtual property T_DotNetType Current
        {
            T_DotNetType get()
            {
                return marshal_as<T_DotNetType>(**m_pIt);
            }
        }

    private:

        virtual property Object^ Current2
        {
            Object^ get() sealed = System::Collections::IEnumerator::Current::get
            {
                return marshal_as<T_DotNetType>(**m_pIt);
            }
        }

    public:

        virtual bool MoveNext()
        {
            if (!m_pIt)
            {
                m_pIt = &m_pList->GetFirstItem();
            }
            else
            {
                (*m_pIt)++;
            }

            return *m_pIt;
        }

        virtual void Reset()
        { 
            m_pIt = 0;
        }

    public:

        EnumerableNptListRefIterator(const NPT_List<T_NativeType>& list)
        {
            m_pIt = 0;
            m_pList = &list;
        }

        ~EnumerableNptListRefIterator()
        {
        }

    };

private:

    const NPT_List<T_NativeType>* m_pList;

public:

    virtual IEnumerator<T_DotNetType>^ GetEnumerator()
    {
        return gcnew EnumerableNptListRefIterator(*m_pList);
    }

private:

    virtual System::Collections::IEnumerator^ GetEnumerator2() sealed = System::Collections::IEnumerable::GetEnumerator
    {
        return gcnew EnumerableNptListRefIterator(*m_pList);
    }

public:

    EnumerableNptListRef(const NPT_List<T_NativeType>& list)
    {
        m_pList = &list;
    }
};

}
}
