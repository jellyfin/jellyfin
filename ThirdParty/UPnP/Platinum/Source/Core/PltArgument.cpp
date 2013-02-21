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

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "PltArgument.h"
#include "PltStateVariable.h"
#include "PltUtilities.h"
#include "PltAction.h"

NPT_SET_LOCAL_LOGGER("platinum.core.argument")

/*----------------------------------------------------------------------
|   PLT_ArgumentDesc::PLT_ArgumentDesc
+---------------------------------------------------------------------*/
PLT_ArgumentDesc::PLT_ArgumentDesc(const char*        name, 
                                   NPT_Ordinal        position,
                                   const char*        direction,
                                   PLT_StateVariable* variable, 
                                   bool               has_ret) :
    m_Name(name),
    m_Position(position),
    m_Direction(direction),
    m_RelatedStateVariable(variable),
    m_HasReturnValue(has_ret)
{
}

/*----------------------------------------------------------------------
|   PLT_ArgumentDesc::GetSCPDXML
+---------------------------------------------------------------------*/
NPT_Result
PLT_ArgumentDesc::GetSCPDXML(NPT_XmlElementNode* node)
{
    NPT_XmlElementNode* argument = new NPT_XmlElementNode("argument");
    NPT_CHECK_SEVERE(node->AddChild(argument));
    NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(argument, "name", m_Name));
    NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(argument, "direction", m_Direction));
    NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(argument, "relatedStateVariable", m_RelatedStateVariable->GetName()));

    if (m_HasReturnValue) {
        NPT_CHECK_SEVERE(argument->AddChild(new NPT_XmlElementNode("retval")));
    }
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_Argument::CreateArgument
+---------------------------------------------------------------------*/
NPT_Result
PLT_Argument::CreateArgument(PLT_ActionDesc& action_desc, 
                             const char*     name, 
                             const char*     value, 
                             PLT_Argument*&  arg)
{
    // reset output params first
    arg = NULL;

    PLT_ArgumentDesc* arg_desc = action_desc.GetArgumentDesc(name);
	if (!arg_desc) {
		NPT_LOG_WARNING_2("Invalid argument %s for action %s", 
			name, 
			(const char*)action_desc.GetName());
		return NPT_ERROR_NO_SUCH_NAME;
	}

	NPT_Result    res;
    PLT_Argument* new_arg = new PLT_Argument(*arg_desc);
    if (NPT_FAILED(res = new_arg->SetValue(value))) {
        delete new_arg;

		NPT_LOG_WARNING_3("Invalid value of %s for argument %s of action %s", 
			value,
			name, 
			(const char*)action_desc.GetName());
        return res;
    }

    arg = new_arg;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_Argument::PLT_Argument
+---------------------------------------------------------------------*/
PLT_Argument::PLT_Argument(PLT_ArgumentDesc& arg_desc) :
    m_ArgDesc(arg_desc)
{

}

/*----------------------------------------------------------------------
|   PLT_Argument::SetValue
+---------------------------------------------------------------------*/
NPT_Result
PLT_Argument::SetValue(const char* value) 
{
    NPT_CHECK_SEVERE(ValidateValue(value));

    m_Value = value;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_Argument::GetValue
+---------------------------------------------------------------------*/
const NPT_String&
PLT_Argument::GetValue() 
{
    return m_Value;
}

/*----------------------------------------------------------------------
|   PLT_Argument::ValidateValue
+---------------------------------------------------------------------*/
NPT_Result
PLT_Argument::ValidateValue(const char* value)
{
    if (m_ArgDesc.GetRelatedStateVariable()) {
        return m_ArgDesc.GetRelatedStateVariable()->ValidateValue(value);
    }
    return NPT_SUCCESS;    
}
