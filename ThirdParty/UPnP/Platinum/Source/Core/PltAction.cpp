/*****************************************************************
|
|   Platinum - Service Action
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
#include "PltAction.h"
#include "PltService.h"
#include "PltUtilities.h"

#define REMOVE_WMP_DATATYPE_EXTENSION

NPT_SET_LOCAL_LOGGER("platinum.core.action")

/*----------------------------------------------------------------------
|   PLT_ActionDesc::PLT_ActionDesc
+---------------------------------------------------------------------*/
PLT_ActionDesc::PLT_ActionDesc(const char* name, PLT_Service* service) : 
    m_Name(name), 
    m_Service(service)
{
}

/*----------------------------------------------------------------------
|   PLT_ActionDesc::~PLT_ActionDesc
+---------------------------------------------------------------------*/
PLT_ActionDesc::~PLT_ActionDesc() 
{
    m_ArgumentDescs.Apply(NPT_ObjectDeleter<PLT_ArgumentDesc>());
}

/*----------------------------------------------------------------------
|   PLT_ActionDesc::GetSCPDXML
+---------------------------------------------------------------------*/
NPT_Result
PLT_ActionDesc::GetSCPDXML(NPT_XmlElementNode* node)
{
    NPT_XmlElementNode* action = new NPT_XmlElementNode("action");
    NPT_CHECK_SEVERE(node->AddChild(action));
    NPT_CHECK_SEVERE(PLT_XmlHelper::AddChildText(action, "name", m_Name));

    NPT_XmlElementNode* argumentList = new NPT_XmlElementNode("argumentList");
    NPT_CHECK_SEVERE(action->AddChild(argumentList));

    // no arguments is ok
    if (!m_ArgumentDescs.GetItemCount()) return NPT_SUCCESS;

    return m_ArgumentDescs.ApplyUntil(
        PLT_GetSCPDXMLIterator<PLT_ArgumentDesc>(argumentList), 
        NPT_UntilResultNotEquals(NPT_SUCCESS));
}

/*----------------------------------------------------------------------
|   PLT_ActionDesc::GetService
+---------------------------------------------------------------------*/
PLT_Service*
PLT_ActionDesc::GetService() 
{
    return m_Service;
}

/*----------------------------------------------------------------------
|   PLT_ActionDesc::GetArgumentDesc
+---------------------------------------------------------------------*/
PLT_ArgumentDesc*
PLT_ActionDesc::GetArgumentDesc(const char* name)
{
    PLT_ArgumentDesc* arg_desc = NULL;
    NPT_ContainerFind(m_ArgumentDescs, PLT_ArgumentDescNameFinder(name), arg_desc);
    return arg_desc;
}

/*----------------------------------------------------------------------
|   PLT_Action::PLT_Action
+---------------------------------------------------------------------*/
PLT_Action::PLT_Action(PLT_ActionDesc& action_desc) :
    m_ActionDesc(action_desc),
    m_ErrorCode(0)
{
}

/*----------------------------------------------------------------------
|   PLT_Action::PLT_Action
+---------------------------------------------------------------------*/
PLT_Action::PLT_Action(PLT_ActionDesc&          action_desc, 
                       PLT_DeviceDataReference& root_device) :
    m_ActionDesc(action_desc),
    m_ErrorCode(0),
	m_RootDevice(root_device)
{
}

/*----------------------------------------------------------------------
|   PLT_Action::~PLT_Action
+---------------------------------------------------------------------*/
PLT_Action::~PLT_Action() 
{
    m_Arguments.Apply(NPT_ObjectDeleter<PLT_Argument>());
}

/*----------------------------------------------------------------------
|   PLT_Action::GetArgumentValue
+---------------------------------------------------------------------*/
NPT_Result 
PLT_Action::GetArgumentValue(const char* name, NPT_String& value) 
{
    PLT_Argument* arg = GetArgument(name);
    if (arg == NULL) {
        return NPT_FAILURE;
    }
    value = arg->GetValue();
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_Action::GetArgumentValue
+---------------------------------------------------------------------*/
NPT_Result 
PLT_Action::GetArgumentValue(const char* name, NPT_UInt32& value) 
{
    NPT_String tmp_value;
    NPT_CHECK_WARNING(GetArgumentValue(name, tmp_value));
    return tmp_value.ToInteger(value);
}

/*----------------------------------------------------------------------
|   PLT_Action::GetArgumentValue
+---------------------------------------------------------------------*/
NPT_Result 
PLT_Action::GetArgumentValue(const char* name, NPT_Int32& value) 
{
    NPT_String tmp_value;
    NPT_CHECK_WARNING(GetArgumentValue(name, tmp_value));
    return tmp_value.ToInteger(value);
}

/*----------------------------------------------------------------------
|   PLT_Action::GetArgumentValue
+---------------------------------------------------------------------*/
NPT_Result 
PLT_Action::GetArgumentValue(const char* name, bool& value) 
{
    NPT_String tmp_value;
    NPT_CHECK_WARNING(GetArgumentValue(name, tmp_value));
	if (tmp_value == "1" || 
		!tmp_value.Compare("TRUE", true) || 
		!tmp_value.Compare("YES", true)) {
		value = true;
	} else if (tmp_value == "0" ||
		!tmp_value.Compare("FALSE", true) ||
		!tmp_value.Compare("NO", true)) {
		value = false;
	} else {
		return NPT_FAILURE;
	}
	return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_Action::GetArgument
+---------------------------------------------------------------------*/
PLT_Argument*
PLT_Action::GetArgument(const char* name)
{
    PLT_Argument* argument = NULL;
    NPT_ContainerFind(m_Arguments, PLT_ArgumentNameFinder(name), argument);
    return argument;
}

/*----------------------------------------------------------------------
|   PLT_Action::SetArgumentValue
+---------------------------------------------------------------------*/
NPT_Result
PLT_Action::SetArgumentValue(const char* name,
                             const char* value)
{
    // look for this argument in our argument list
    // and replace the value if we found it 
	PLT_Arguments::Iterator iter = NULL;
    if (NPT_SUCCEEDED(NPT_ContainerFind(m_Arguments, PLT_ArgumentNameFinder(name), iter))) {
        NPT_Result res = (*iter)->SetValue(value);

		// remove argument from list if failed
		// so that when we verify arguments later, 
		// we don't use a previously set value
		if (NPT_FAILED(res)) m_Arguments.Erase(iter);
		return res;
    }

    // since we didn't find it, create a clone 
	PLT_Argument* arg;
    NPT_CHECK_SEVERE(PLT_Argument::CreateArgument(m_ActionDesc, name, value, arg));

    // insert it at the right position
    for (NPT_Cardinal i=0;
         i<m_Arguments.GetItemCount();
         i++) {
        iter = m_Arguments.GetItem(i);
        if ((*iter)->GetPosition() > arg->GetPosition()) {
            return m_Arguments.Insert(iter, arg);
        }
    }

    return m_Arguments.Add(arg);
}

/*----------------------------------------------------------------------
|   PLT_Action::VerifyArgumentValue
+---------------------------------------------------------------------*/
NPT_Result
PLT_Action::VerifyArgumentValue(const char* name, const char* value)
{
    NPT_String str;
    NPT_CHECK_SEVERE(GetArgumentValue(name, str));

    return str.Compare(value, true)?NPT_FAILURE:NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_Action::VerifyArguments
+---------------------------------------------------------------------*/
NPT_Result
PLT_Action::VerifyArguments(bool input)
{
    NPT_Cardinal count = 0;

    // Check we have all the required parameters (in or out)
    for(unsigned int i=0; i<m_ActionDesc.GetArgumentDescs().GetItemCount(); i++) {
        PLT_ArgumentDesc* arg_desc = m_ActionDesc.GetArgumentDescs()[i];

        // only input arguments are needed
        if (arg_desc->GetDirection().Compare(input?"in":"out", true))
            continue;

        // look for this argument in the list we received
        PLT_Argument* arg = NULL;
        if (NPT_FAILED(NPT_ContainerFind(m_Arguments, PLT_ArgumentNameFinder(arg_desc->GetName()), arg))) {
			NPT_LOG_WARNING_2("Argument %s for action %s not found", 
				(const char*) arg_desc->GetName(), 
				(const char*) m_ActionDesc.GetName());
            return NPT_FAILURE;
        }
        ++count;
    }

    SetError(0, "");
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_Action::SetArgumentOutFromStateVariable
+---------------------------------------------------------------------*/
NPT_Result
PLT_Action::SetArgumentOutFromStateVariable(PLT_ArgumentDesc* arg_desc)
{
    // only output arguments can use a state variable
    if (arg_desc->GetDirection().Compare("out", true)) {
        return NPT_FAILURE;
    }

    PLT_StateVariable* variable = arg_desc->GetRelatedStateVariable();
    if (!variable) return NPT_FAILURE;

    // assign the value to an argument
    NPT_CHECK_SEVERE(SetArgumentValue(arg_desc->GetName(), variable->GetValue()));
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_Action::SetArgumentOutFromStateVariable
+---------------------------------------------------------------------*/
NPT_Result
PLT_Action::SetArgumentOutFromStateVariable(const char* name)
{
    // look for this argument in the action list of arguments
    PLT_ArgumentDesc* arg_desc = NULL;
    NPT_CHECK_SEVERE(NPT_ContainerFind(m_ActionDesc.GetArgumentDescs(), 
        PLT_ArgumentDescNameFinder(name), arg_desc));

    return SetArgumentOutFromStateVariable(arg_desc);
}

/*----------------------------------------------------------------------
|   PLT_Action::SetArgumentsOutFromStateVariable
+---------------------------------------------------------------------*/
NPT_Result
PLT_Action::SetArgumentsOutFromStateVariable()
{
    // go through the list of the action output arguments
    for(unsigned int i=0; i<m_ActionDesc.GetArgumentDescs().GetItemCount(); i++) {
        PLT_ArgumentDesc* arg_desc = m_ActionDesc.GetArgumentDescs()[i];

        // only output arguments are needed
        if (arg_desc->GetDirection().Compare("out", true))
            continue;

        NPT_CHECK_SEVERE(SetArgumentOutFromStateVariable(arg_desc));
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_Action::SetError
+---------------------------------------------------------------------*/
NPT_Result 
PLT_Action::SetError(unsigned int code, const char* description) 
{
    m_ErrorCode = code;
    m_ErrorDescription = description;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   PLT_Action::GetError
+---------------------------------------------------------------------*/
const char* 
PLT_Action::GetError(unsigned int* code /* = NULL */) 
{
    if (code) *code = m_ErrorCode;
    return m_ErrorDescription;
}

/*----------------------------------------------------------------------
|   PLT_Action::GetErrorCode
+---------------------------------------------------------------------*/
unsigned int 
PLT_Action::GetErrorCode() 
{
    return m_ErrorCode;
}

/*----------------------------------------------------------------------
|   PLT_Action::FormatSoapRequest
+---------------------------------------------------------------------*/
NPT_Result
PLT_Action::FormatSoapRequest(NPT_OutputStream& stream)
{
    NPT_String str;
    NPT_Result res;
    NPT_XmlElementNode* body = NULL;
    NPT_XmlElementNode* request = NULL;
    NPT_XmlElementNode* envelope = new NPT_XmlElementNode("s", "Envelope");

    NPT_CHECK_LABEL_SEVERE(res = envelope->SetNamespaceUri("s", "http://schemas.xmlsoap.org/soap/envelope/"), cleanup);
    NPT_CHECK_LABEL_SEVERE(res = envelope->SetAttribute("s", "encodingStyle", "http://schemas.xmlsoap.org/soap/encoding/"), cleanup);

    body = new NPT_XmlElementNode("s", "Body");
    NPT_CHECK_LABEL_SEVERE(res = envelope->AddChild(body), cleanup);

    request = new NPT_XmlElementNode("u", m_ActionDesc.GetName());
    NPT_CHECK_LABEL_SEVERE(res = request->SetNamespaceUri("u", m_ActionDesc.GetService()->GetServiceType()), cleanup);
    NPT_CHECK_LABEL_SEVERE(res = body->AddChild(request), cleanup);

    for(unsigned int i=0; i<m_Arguments.GetItemCount(); i++) {
        PLT_Argument* argument = m_Arguments[i];
        if (argument->GetDesc().GetDirection().Compare("in", true) == 0) {
            NPT_CHECK_LABEL_SEVERE(res = PLT_XmlHelper::AddChildText(
                request, 
                argument->GetDesc().GetName(), 
                argument->GetValue()), cleanup);
        }
    }

    NPT_CHECK_LABEL_SEVERE(res = PLT_XmlHelper::Serialize(*envelope, str), cleanup);
    delete envelope;

    return stream.Write((const char*)str, str.GetLength());

cleanup:
    delete envelope;
    return res;
}

/*----------------------------------------------------------------------
|   PLT_Action::FormatSoapResponse
+---------------------------------------------------------------------*/
NPT_Result
PLT_Action::FormatSoapResponse(NPT_OutputStream& stream)
{
    if (m_ErrorCode) {
        return FormatSoapError(m_ErrorCode, m_ErrorDescription, stream);
    }

    NPT_String str;
    NPT_Result res;
    NPT_XmlElementNode* body = NULL;
    NPT_XmlElementNode* response = NULL;
    NPT_XmlElementNode* node = NULL;
    NPT_XmlElementNode* envelope = new NPT_XmlElementNode("s", "Envelope");

    NPT_CHECK_LABEL_SEVERE(res = envelope->SetNamespaceUri("s", "http://schemas.xmlsoap.org/soap/envelope/"), cleanup);
    NPT_CHECK_LABEL_SEVERE(res = envelope->SetAttribute("s", "encodingStyle", "http://schemas.xmlsoap.org/soap/encoding/"), cleanup);

    body = new NPT_XmlElementNode("s", "Body");
    NPT_CHECK_LABEL_SEVERE(res = envelope->AddChild(body), cleanup);        

    response = new NPT_XmlElementNode("u", m_ActionDesc.GetName() + "Response");
    NPT_CHECK_LABEL_SEVERE(response->SetNamespaceUri("u", m_ActionDesc.GetService()->GetServiceType()), cleanup);
    NPT_CHECK_LABEL_SEVERE(res = body->AddChild(response), cleanup);    

    for(unsigned int i=0; i<m_Arguments.GetItemCount(); i++) {
        PLT_Argument* argument = m_Arguments[i];
        if (argument->GetDesc().GetDirection().Compare("out", true) == 0) {
            node = new NPT_XmlElementNode(argument->GetDesc().GetName());
            NPT_CHECK_LABEL_SEVERE(res = node->AddText(argument->GetValue()), cleanup);
            NPT_CHECK_LABEL_SEVERE(res = response->AddChild(node), cleanup);

#ifndef REMOVE_WMP_DATATYPE_EXTENSION
            PLT_StateVariable* var = argument->GetDesc().GetRelatedStateVariable();
            if (var) {
                node->SetNamespaceUri("dt", "urn:schemas-microsoft-com:datatypes");
                node->SetAttribute("dt", "dt", var->GetDataType());
            }
#endif
        }
    }

    // this will xmlescape any values that contain xml characters
    NPT_CHECK_LABEL_SEVERE(PLT_XmlHelper::Serialize(*envelope, str), cleanup);
    delete envelope;

    return stream.Write((const char*)str, str.GetLength());

cleanup:
    delete envelope;
    return res;
}

/*----------------------------------------------------------------------
|   PLT_Action::FormatSoapError
+---------------------------------------------------------------------*/
NPT_Result
PLT_Action::FormatSoapError(unsigned int code, NPT_String desc, NPT_OutputStream& stream)
{
    NPT_String str;
    NPT_Result res;
    NPT_XmlElementNode* body = NULL;
    NPT_XmlElementNode* fault = NULL;
    NPT_XmlElementNode* detail = NULL;
    NPT_XmlElementNode* UPnPError = NULL;
    NPT_XmlElementNode* envelope = new NPT_XmlElementNode("s", "Envelope");

    NPT_CHECK_LABEL_SEVERE(res = envelope->SetNamespaceUri("s", "http://schemas.xmlsoap.org/soap/envelope/"), cleanup);
    NPT_CHECK_LABEL_SEVERE(res = envelope->SetAttribute("s", "encodingStyle", "http://schemas.xmlsoap.org/soap/encoding/"), cleanup);

    body = new NPT_XmlElementNode("s", "Body");
    NPT_CHECK_LABEL_SEVERE(res = envelope->AddChild(body), cleanup);   

    fault = new NPT_XmlElementNode("s", "Fault");
    NPT_CHECK_LABEL_SEVERE(res = body->AddChild(fault), cleanup);

    NPT_CHECK_LABEL_SEVERE(res = PLT_XmlHelper::AddChildText(fault, "faultcode", "s:Client"), cleanup);
    NPT_CHECK_LABEL_SEVERE(res = PLT_XmlHelper::AddChildText(fault, "faultstring", "UPnPError"), cleanup);

    detail = new NPT_XmlElementNode("detail");
    NPT_CHECK_LABEL_SEVERE(res = fault->AddChild(detail), cleanup);

    UPnPError = new NPT_XmlElementNode("UPnPError");
    NPT_CHECK_LABEL_SEVERE(res = UPnPError->SetNamespaceUri("", "urn:schemas-upnp-org:control-1-0"), cleanup);
    NPT_CHECK_LABEL_SEVERE(res = detail->AddChild(UPnPError), cleanup);

    NPT_CHECK_LABEL_SEVERE(res = PLT_XmlHelper::AddChildText(UPnPError, "errorCode", NPT_String::FromInteger(code)), cleanup);
    NPT_CHECK_LABEL_SEVERE(res = PLT_XmlHelper::AddChildText(UPnPError, "errorDescription", desc), cleanup);

    NPT_CHECK_LABEL_SEVERE(res = PLT_XmlHelper::Serialize(*envelope, str), cleanup);
    delete envelope;

    return stream.Write((const char*)str, str.GetLength());

cleanup:
    delete envelope;
    return res;
}
