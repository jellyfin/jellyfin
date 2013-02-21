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

/** @file
 UPnP State Variable
 */

#ifndef _PLT_STATE_VARIABLE_H_
#define _PLT_STATE_VARIABLE_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "Neptune.h"

/*----------------------------------------------------------------------
|   forward declarations
+---------------------------------------------------------------------*/
class PLT_Argument;
class PLT_Service;

/*----------------------------------------------------------------------
|   NPT_AllowedValueRange struct
+---------------------------------------------------------------------*/
/**
 The NPT_AllowedValueRange struct holds the min, max and step value allowed of
 a UPnP Service state variable.
 */
typedef struct {
    NPT_Int32 min_value;
    NPT_Int32 max_value;
    NPT_Int32 step;
} NPT_AllowedValueRange;

/*----------------------------------------------------------------------
|   PLT_StateVariable class
+---------------------------------------------------------------------*/
/**
 The PLT_StateVariable class maintains the state of a UPnP Service state variable. 
 It is used by a PLT_DeviceHost instance to notify subscribers of a change or by a
 subscriber (PLT_CtrlPoint) when a service state variable change notification
 has been received.
 */
class PLT_StateVariable
{
public:
    PLT_StateVariable(PLT_Service* service);
    ~PLT_StateVariable();

    /**
     Populate the SCPD xml document with state variable information.
     @param node XML Element where to insert the state variable XML Element
     */
    NPT_Result GetSCPDXML(NPT_XmlElementNode* node);
    
    /**
     Return the PLT_Service that this state variable is associated with.
     @return PLT_Service pointer.
     */
    PLT_Service* GetService();
    
    /**
     Return whether the state variable is eventable directly or indirectly. A state
     variable sends events indirectly when part of the "LastChange" state variable.
     @param indirectly Boolean to test if the state variable is sending events indirectly
     @return Whether the state variable sends events according to the input flag specified.
     */
    bool IsSendingEvents(bool indirectly = false);
    
    /**
     Force the state variable to send events directly.
     */
    void DisableIndirectEventing();
    
    /**
     Certain state variables notifications must not be sent faster than a certain 
     rate according to the UPnP specs. This sets the rate for a given state variable.
     @param rate time interval to respect between notifications.
     */
    NPT_Result SetRate(NPT_TimeInterval rate);
    
    /**
     Set the state variable value. The value is first validated to make sure
     it is an allowed value. Once the value is validated, it is marked for eventing by
     calling the PLT_Service AddChanged function.
     @param value new state variable value. Can be a comma separated list of values.
     */
    NPT_Result SetValue(const char* value);
    
    /**
     Validate the new value of the state variable.
     @param value new state variable value. Can be a comma separated list of values.
     */
    NPT_Result ValidateValue(const char* value);
    
    /**
     Certain state variables require extra xml attributes when serialized.
     @param name the attribute name
     @param value the attribute value
     */
	NPT_Result SetExtraAttribute(const char* name, const char* value);

    /**
     Return the state variable name.
     @return state variable name.
     */
    const NPT_String& GetName()     const { return m_Name;     }
    
    /**
     Return the current state variable value.
     @return state variable current value.
     */
    const NPT_String& GetValue()    const { return m_Value;    }
    
    /**
     Return the state variable data type.
     @return state variable data type.
     */
    const NPT_String& GetDataType() const { return m_DataType; }
    
    /**
     Return the state variable allowed value range if any.
     @return state variable value range pointer or null if none.
     */
	const NPT_AllowedValueRange* GetAllowedValueRange() const { return m_AllowedValueRange; }

    /**
     Helper function to return a state variable given a list of state variables
     and a state variable name.
     @param vars list of state variables
     @param name state variable name to look for
     @return PLT_StateVariable pointer.
     */
    static PLT_StateVariable* Find(NPT_List<PLT_StateVariable*>& vars, 
                                   const char*                   name);

protected:
    /**
     Return whether the state variable value changed and subscribers need to
     be notified.
     */
    bool IsReadyToPublish();
    
    /**
     Serialize the state variable into xml.
     */
	NPT_Result Serialize(NPT_XmlElementNode& node);

protected:
    friend class PLT_Service;
    friend class PLT_LastChangeXMLIterator;

    //members
    PLT_Service*            m_Service;
    NPT_AllowedValueRange*  m_AllowedValueRange;
    NPT_String              m_Name;
    NPT_String              m_DataType;
    NPT_String              m_DefaultValue;
    bool                    m_IsSendingEvents;
    bool                    m_IsSendingEventsIndirectly;
    NPT_TimeInterval        m_Rate;
    NPT_TimeStamp           m_LastEvent;
    NPT_Array<NPT_String*>  m_AllowedValues;
    NPT_String              m_Value;

	NPT_Map<NPT_String,NPT_String> m_ExtraAttributes;
};

/*----------------------------------------------------------------------
|   PLT_StateVariableNameFinder
+---------------------------------------------------------------------*/
/**
 The PLT_StateVariableNameFinder class returns the PLT_StateVariable instance
 given a state variable name.
 */
class PLT_StateVariableNameFinder 
{
public:
    // methods
    PLT_StateVariableNameFinder(const char* name) : m_Name(name) {}
    virtual ~PLT_StateVariableNameFinder() {}

    bool operator()(const PLT_StateVariable* const & state_variable) const {
        return state_variable->GetName().Compare(m_Name, true) ? false : true;
    }

private:
    // members
    NPT_String   m_Name;
};

#endif /* _PLT_STATE_VARIABLE_H_ */
