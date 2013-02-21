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

/** @file
   UPnP Service Action
 */

#ifndef _PLT_ACTION_H_
#define _PLT_ACTION_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "PltArgument.h"
#include "PltDeviceData.h"

/*----------------------------------------------------------------------
|   forward declarations
+---------------------------------------------------------------------*/
class PLT_Service;

/*----------------------------------------------------------------------
|   PLT_ActionDesc
+---------------------------------------------------------------------*/
/**
 The PLT_ActionDesc class provides information about a UPnP Service given action.
 This description has a name, a set of arguments and is associated to a service.
 */
class PLT_ActionDesc
{
public:
    /**
     Constructor
     @param name the action name
     @param service the UPnP service the action is associated with
     */
    PLT_ActionDesc(const char* name, PLT_Service* service);
   ~PLT_ActionDesc();

    /**
     Return an array of arguments
     @return array of arguments
     */
    NPT_Array<PLT_ArgumentDesc*>& GetArgumentDescs() { 
        return m_ArgumentDescs; 
    }

    /**
     Return the action name.
     @return action name.
     */
    const NPT_String& GetName() const { return m_Name;}
    
    /**
     Look for an argument given a name.
     @param name argument name
     @return PLT_ArgumentDesc pointer
     */
    PLT_ArgumentDesc* GetArgumentDesc(const char* name);
    
    /**
     Serialize action information to xml into an existing xml tree
     @param node the xml Element to serialize action information
     */
    NPT_Result        GetSCPDXML(NPT_XmlElementNode* node);
    
    /**
     Return the service the action is associated with
     */
    PLT_Service*      GetService();

protected:
    //members
    NPT_String                   m_Name;
    PLT_Service*                 m_Service;
    NPT_Array<PLT_ArgumentDesc*> m_ArgumentDescs;
};

/*----------------------------------------------------------------------
|   PLT_Action
+---------------------------------------------------------------------*/
/** 
 The PLT_Action class provides a mechanism to call or verify the validity of a
 specific UPNP service action.
 Given a service, a UPnP Control Point would use this class to serialize a soap
 request. On the other side, a UPnP Device would use this class to verify
 a soap request and the validity of the action arguments.
 */
class PLT_Action
{
public:
    /**
     Constructor
     @param action_desc the action description
     If you intend to send an action, you need to use the second constructor
     and pass the root device of the device you wish to control.
     */
    PLT_Action(PLT_ActionDesc& action_desc);
    
    /**
     Constructor
     @param action_desc the action description
     @param root_device a reference to the root device of the service the action
     is associated with. This insures that the device won't be deleted if it goes 
     away while we're waiting for a response for this action. This is important because
     we only keep a reference to the PLT_ActionDesc which is own by the service operated
     by the device (or embedded device).
     */
    PLT_Action(PLT_ActionDesc& action_desc, PLT_DeviceDataReference& root_device);
    ~PLT_Action();

    /**
     Return the action description
     @return the action description
     */
    PLT_ActionDesc& GetActionDesc() { return m_ActionDesc; }
    
    /**
     Retrieve the string value of an argument given an argument name.
     @param name the argument name
     @param value the string value to retrieve
     @return error if the argument is not found or if the type does not correspond.
     */
    NPT_Result GetArgumentValue(const char* name, NPT_String& value);
    
    /**
     Retrieve the value of an argument given an argument name.
     @param name the argument name
     @param value the unsigned int value to retrieve
     @return error if the argument is not found or if the type does not correspond.
     */
    NPT_Result GetArgumentValue(const char* name, NPT_UInt32& value);
    
    /**
     Retrieve the value of an argument given an argument name.
     @param name the argument name
     @param value the int value to retrieve
     @return error if the argument is not found or if the type does not correspond.
     */
    NPT_Result GetArgumentValue(const char* name, NPT_Int32& value);
    
    /**
     Retrieve the value of an argument given an argument name.
     @param name the argument name
     @param value the bool value to retrieve
     @return error if the argument is not found or if the type does not correspond.
     */
	NPT_Result GetArgumentValue(const char* name, bool& value);
    
    /**
     Verify a value is valid for a given argument.
     @param name the argument name
     @param value the value to verify
     */
    NPT_Result VerifyArgumentValue(const char* name, const char* value);
    
    /**
     Verify that all required arguments are set.
     @param input boolean indicating whether input or output parameters 
     should be verified
     */
    NPT_Result VerifyArguments(bool input);
    
    /**
     Set the output argument value from the associated current state variable value.
     @param name the state variable name
     */
    NPT_Result SetArgumentOutFromStateVariable(const char* name);
    
    /**
     Set all the output argument values associated with state variables.
     */
    NPT_Result SetArgumentsOutFromStateVariable();
    
    /**
     Set an argument value
     @param name the argument name
     @param value the argument value
     */
    NPT_Result SetArgumentValue(const char* name, const char* value);

    /** 
     Set the error code and description in case of failure.
     @param code the code for the error
     @param description a short description
     */
    NPT_Result SetError(unsigned int code, const char* description);
    
    /**
     Return the error description and code for the failed action.
     @param code optional pointer to receive the code
     @return the error short description
     */
    const char* GetError(unsigned int* code = NULL);
    
    /**
     Return the error code for the failed action.
     @return the error code.
     */
    unsigned int GetErrorCode();
    
    /**
     Called by a control point when serializing an action.
     @param stream the stream to serialize the action to
     */
    NPT_Result    FormatSoapRequest(NPT_OutputStream& stream);
    
    /**
     Called by a device when serializing a response to an action.
     @param stream the stream to serialize the action to
     */
    NPT_Result    FormatSoapResponse(NPT_OutputStream& stream);

    /**
     Helper method for a device to serialize an action invocation error.
     @param code optional pointer to receive the code
     @param desc the error short description
     @param stream the stream to serialize to
     */
    static NPT_Result FormatSoapError(unsigned int      code, 
                                      NPT_String        desc, 
                                      NPT_OutputStream& stream);

private:
    // methods
    NPT_Result    SetArgumentOutFromStateVariable(PLT_ArgumentDesc* arg_desc);
    PLT_Argument* GetArgument(const char* name);

protected:
    // members
    PLT_ActionDesc&         m_ActionDesc;
    PLT_Arguments           m_Arguments;
    unsigned int            m_ErrorCode;
    NPT_String              m_ErrorDescription;
    
    // keep reference of service root device to prevent it 
    // from being released during action lifetime
	PLT_DeviceDataReference m_RootDevice;
};

typedef NPT_Reference<PLT_Action> PLT_ActionReference;

/*----------------------------------------------------------------------
|   PLT_GetSCPDXMLIterator
+---------------------------------------------------------------------*/
/**
 The PLT_GetSCPDXMLIterator class provides a recursive way to serialize 
 an SCPD into an xml tree.
 */
template <class T>
class PLT_GetSCPDXMLIterator
{
public:
    PLT_GetSCPDXMLIterator<T>(NPT_XmlElementNode* node) :
        m_Node(node) {}
      
    NPT_Result operator()(T* const & data) const {
        return data->GetSCPDXML(m_Node);
    }

private:
    NPT_XmlElementNode* m_Node;
};

/*----------------------------------------------------------------------
|   PLT_ActionDescNameFinder
+---------------------------------------------------------------------*/
/** 
 The PLT_ActionDescNameFinder class provides a mechanism to find a PLT_ActionDesc 
 given a PLT_Service and an action name.
 */
class PLT_ActionDescNameFinder
{
public:
    // methods
    PLT_ActionDescNameFinder(PLT_Service* service, const char* name) : 
        m_Service(service), m_Name(name) {}
    virtual ~PLT_ActionDescNameFinder() {}

    bool operator()(const PLT_ActionDesc* const & action_desc) const {
        return action_desc->GetName().Compare(m_Name, true) ? false : true;
    }

private:
    // members
    PLT_Service* m_Service;
    NPT_String   m_Name;
};

#endif /* _PLT_ACTION_H_ */
