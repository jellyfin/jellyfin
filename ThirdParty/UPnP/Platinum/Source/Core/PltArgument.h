/*****************************************************************
|
|   Platinum - Action Argument
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
| licensing@plutinosoft.com
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

/** @file
 UPnP Service Action Argument
 */

#ifndef _PLT_ARGUMENT_H_
#define _PLT_ARGUMENT_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"

/*----------------------------------------------------------------------
|   forward declarations
+---------------------------------------------------------------------*/
class PLT_StateVariable;
class PLT_Argument;
class PLT_ActionDesc;
typedef NPT_Array<PLT_Argument*> PLT_Arguments;

/*----------------------------------------------------------------------
|   PLT_ArgumentDesc
+---------------------------------------------------------------------*/
/**
 The PLT_ArgumentDesc class provides information about a given argument of a 
 UPnP Service given action.
 It has a name, a position, a direction (in/out), a PLT_StateVariable state 
 variable association and whether it is the return value of the action or not.
 */
class PLT_ArgumentDesc
{
public:
    PLT_ArgumentDesc(const char*        name,
                     NPT_Ordinal        position,
                     const char*        direction = "in",
                     PLT_StateVariable* variable = NULL, 
                     bool               has_ret = false);

    // accessor methods
    NPT_Result         GetSCPDXML(NPT_XmlElementNode* node);
    const NPT_String&  GetName() const { return m_Name; }
    const NPT_String&  GetDirection() const { return m_Direction; }
    NPT_Ordinal        GetPosition() { return m_Position; }
    PLT_StateVariable* GetRelatedStateVariable() { return m_RelatedStateVariable; }
    bool               HasReturnValue() { return m_HasReturnValue; }

protected:
    NPT_String         m_Name;
    NPT_Ordinal        m_Position;
    NPT_String         m_Direction;
    PLT_StateVariable* m_RelatedStateVariable;
    bool               m_HasReturnValue;
};

/*----------------------------------------------------------------------
|   PLT_Argument
+---------------------------------------------------------------------*/
/** 
 The PLT_Argument class provides a mechanism to set or verify the validity of a
 specific UPNP service action argument.
 Typically, only a PLT_Action uses this class. Since an argument can be
 associated to a state variable, the argument is automatically updated when
 the associated state variable is changed 
 */
class PLT_Argument
{
public:
    PLT_Argument(PLT_ArgumentDesc& arg_desc);

    // class methods
    static NPT_Result CreateArgument(PLT_ActionDesc& action_desc, 
                                     const char*     arg_name, 
                                     const char*     arg_value,
                                     PLT_Argument*&  arg);

    // accessor methods
    PLT_ArgumentDesc&  GetDesc() { return m_ArgDesc; }
    NPT_Ordinal        GetPosition() { return m_ArgDesc.GetPosition(); }
    NPT_Result         SetValue(const char* value);
    const NPT_String&  GetValue();

private:
    NPT_Result         ValidateValue(const char* value);
    
protected:
    PLT_ArgumentDesc&  m_ArgDesc;
    NPT_String         m_Value;
};

/*----------------------------------------------------------------------
|   PLT_ArgumentNameFinder
+---------------------------------------------------------------------*/
/** 
 The PLT_ArgumentNameFinder class provides a mechanism to find a PLT_Argument given
 an argument name.
 */
class PLT_ArgumentNameFinder
{
public:
    // methods
    PLT_ArgumentNameFinder(const char* name) : m_Name(name) {}

    bool operator()(PLT_Argument* const & argument) const {
        return argument->GetDesc().GetName().Compare(m_Name, true) ? false : true;
    }

private:
    // members
    NPT_String m_Name;
};

/*----------------------------------------------------------------------
|   PLT_ArgumentDescNameFinder
+---------------------------------------------------------------------*/
/** 
 The PLT_ArgumentDescNameFinder class provides a mechanism to find a PLT_ArgumentDesc given
 an argument name.
 */
class PLT_ArgumentDescNameFinder
{
public:
    // methods
    PLT_ArgumentDescNameFinder(const char* name) : m_Name(name) {}

    bool operator()(PLT_ArgumentDesc* const & arg_desc) const {
        return arg_desc->GetName().Compare(m_Name, true) ? false : true;
    }

private:
    // members
    NPT_String m_Name;
};

#endif /* _PLT_ARGUMENT_H_ */
