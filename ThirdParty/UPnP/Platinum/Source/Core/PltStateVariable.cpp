/*****************************************************************
|
|   Platinum - Service State Variable
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
#include "PltStateVariable.h"
#include "PltService.h"
#include "PltUtilities.h"
#include "PltUPnP.h"

NPT_SET_LOCAL_LOGGER("platinum.core.statevariable")

/*----------------------------------------------------------------------
|   PLT_StateVariable::PLT_StateVariable
+---------------------------------------------------------------------*/
PLT_StateVariable::PLT_StateVariable(PLT_Service* service) : 
    m_Service(service), 
    m_AllowedValueRange(NULL),
    m_IsSendingEventsIndirectly(true)
{
}

/*----------------------------------------------------------------------
|   PLT_StateVariable::~PLT_StateVariable
+---------------------------------------------------------------------*/
PLT_StateVariable::~PLT_StateVariable() 
{
    m_AllowedValues.Apply(NPT_ObjectDeleter<NPT_String>());
    if (m_AllowedValueRange) delete m_AllowedValueRange;
}

/*----------------------------------------------------------------------
|   PLT_StateVariable::GetSCPDXML
+---------------------------------------------------------------------*/
NPT_Result
PLT_StateVariable::GetSCPDXML(NPT_XmlElementNode* node)
{
    NPT_XmlElementNode* variable = new NPT_XmlElementNode("stateVariable");
    NPT_CHECK_SEVERE(node->AddChild(variable));

    NPT_CHECK_SEVERE(variable->SetAttribute("sendEvents", m_IsSendingEvents?"yes":"no"));
    NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(variable, "name", m_Name));
    NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(variable, "dataType", m_DataType));
    if (m_DefaultValue.GetLength()) {
        NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(variable, "defaultValue", m_DefaultValue));
    }

    if (m_AllowedValues.GetItemCount()) {
        NPT_XmlElementNode* allowedValueList = new NPT_XmlElementNode("allowedValueList");
        NPT_CHECK_SEVERE(variable->AddChild(allowedValueList));
	    for( int l = 0 ; l < (int)m_AllowedValues.GetItemCount(); l++) {
            NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(allowedValueList, "allowedValue", (*m_AllowedValues[l])));
        }
    } else if (m_AllowedValueRange) {
        NPT_XmlElementNode* range = new NPT_XmlElementNode("allowedValueRange");
        NPT_CHECK_SEVERE(variable->AddChild(range));
        NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(range, "minimum", NPT_String::FromInteger(m_AllowedValueRange->min_value)));
        NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(range, "maximum", NPT_String::FromInteger(m_AllowedValueRange->max_value)));
        if (m_AllowedValueRange->step != -1) {
            NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(range, "step",    NPT_String::FromInteger(m_AllowedValueRange->step)));
        }
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_StateVariable::GetService
+---------------------------------------------------------------------*/
PLT_Service* 
PLT_StateVariable::GetService() 
{
    return m_Service;
}

/*----------------------------------------------------------------------
|   PLT_StateVariable::IsSendingEvents
+---------------------------------------------------------------------*/
bool 
PLT_StateVariable::IsSendingEvents(bool indirectly /* = false */) 
{
    if (indirectly) {
        return (!m_IsSendingEvents && 
                !m_Name.StartsWith("A_ARG_TYPE_") && 
                m_IsSendingEventsIndirectly);
    }

    return m_IsSendingEvents;
}

/*----------------------------------------------------------------------
|   PLT_StateVariable::DisableIndirectEventing
+---------------------------------------------------------------------*/
void
PLT_StateVariable::DisableIndirectEventing()
{
    m_IsSendingEventsIndirectly = false;
}

/*----------------------------------------------------------------------
|   PLT_StateVariable::SetRate
+---------------------------------------------------------------------*/
NPT_Result
PLT_StateVariable::SetRate(NPT_TimeInterval rate)
{
    if (!IsSendingEvents()) return NPT_FAILURE;

    m_Rate = rate;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_StateVariable::SetValue
+---------------------------------------------------------------------*/
NPT_Result
PLT_StateVariable::SetValue(const char* value)
{
    if (value == NULL) {
        return NPT_FAILURE;
    }

    // update only if it's different
    if (m_Value != value) {
        NPT_Result res = ValidateValue(value);
        if (NPT_FAILED(res)) {
            return res;
        }

        m_Value = value;
        m_Service->AddChanged(this); 
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_StateVariable::IsReadyToPublish
+---------------------------------------------------------------------*/
bool
PLT_StateVariable::IsReadyToPublish()
{
    NPT_TimeStamp now;
    NPT_System::GetCurrentTimeStamp(now);

    if (m_Rate == NPT_TimeStamp() || m_LastEvent + m_Rate <= now ) {
        m_LastEvent = now;
        return true;
    }

    return false;
}

/*----------------------------------------------------------------------
|   PLT_StateVariable::ValidateValue
+---------------------------------------------------------------------*/
NPT_Result
PLT_StateVariable::ValidateValue(const char* value)
{
    if (m_DataType.Compare("string", true) == 0) {
        // if we have a value allowed restriction, make sure the value is in our list
        if (m_AllowedValues.GetItemCount()) {
            // look for a comma separated list
            NPT_String _value = value;
            NPT_List<NPT_String> values = _value.Split(",");
            NPT_List<NPT_String>::Iterator val = values.GetFirstItem();
            while (val) {
                val->Trim(" ");
				if (!m_AllowedValues.Find(NPT_StringFinder(*val))) {
					NPT_LOG_WARNING_2("Invalid value of %s for state variable %s", 
						(const char*)*val,
						(const char*)m_Name);
                    return NPT_ERROR_INVALID_PARAMETERS;
				}
                ++val;
            }
        }
    }

    // TODO: there are more to it than allowed values, we need to test for range, etc..
    return NPT_SUCCESS;    
}

/*----------------------------------------------------------------------
|   PLT_StateVariable::Find
+---------------------------------------------------------------------*/
PLT_StateVariable*
PLT_StateVariable::Find(NPT_List<PLT_StateVariable*>& vars, const char* name)
{
    PLT_StateVariable* stateVariable = NULL;
    NPT_ContainerFind(vars, PLT_StateVariableNameFinder(name), stateVariable);
    return stateVariable;
}

/*----------------------------------------------------------------------
|   PLT_StateVariable::SetExtraAttribute
+---------------------------------------------------------------------*/
NPT_Result
PLT_StateVariable::SetExtraAttribute(const char* name, const char* value)
{
	return m_ExtraAttributes.Put(NPT_String(name), NPT_String(value));
}

/*----------------------------------------------------------------------
|   PLT_StateVariable::Serialize
+---------------------------------------------------------------------*/
NPT_Result
PLT_StateVariable::Serialize(NPT_XmlElementNode& node)
{
    NPT_List<NPT_Map<NPT_String, NPT_String>::Entry*>::Iterator entry = 
        m_ExtraAttributes.GetEntries().GetFirstItem();
    while (entry) {
        const NPT_String& key   = (*entry)->GetKey();
        const NPT_String& value = (*entry)->GetValue();
		node.SetAttribute(key, value);
        ++entry;
    }
    return node.SetAttribute("val", GetValue());
}
