/*****************************************************************
|
|   Neptune - Network
|
| Copyright (c) 2002-2008, Axiomatic Systems, LLC.
| All rights reserved.
|
| Redistribution and use in source and binary forms, with or without
| modification, are permitted provided that the following conditions are met:
|     * Redistributions of source code must retain the above copyright
|       notice, this list of conditions and the following disclaimer.
|     * Redistributions in binary form must reproduce the above copyright
|       notice, this list of conditions and the following disclaimer in the
|       documentation and/or other materials provided with the distribution.
|     * Neither the name of Axiomatic Systems nor the
|       names of its contributors may be used to endorse or promote products
|       derived from this software without specific prior written permission.
|
| THIS SOFTWARE IS PROVIDED BY AXIOMATIC SYSTEMS ''AS IS'' AND ANY
| EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
| WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
| DISCLAIMED. IN NO EVENT SHALL AXIOMATIC SYSTEMS BE LIABLE FOR ANY
| DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
| (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
| LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
| ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
| (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
| SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
|
 ****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptSockets.h"
#include "NptUtils.h"

/*----------------------------------------------------------------------
|   NPT_IpAddress::Any
+---------------------------------------------------------------------*/
const NPT_IpAddress NPT_IpAddress::Any;

/*----------------------------------------------------------------------
|   NPT_IpAddress::NPT_IpAddress
+---------------------------------------------------------------------*/
NPT_IpAddress::NPT_IpAddress()
{
    m_Address[0] = m_Address[1] = m_Address[2] = m_Address[3] = 0;
}

/*----------------------------------------------------------------------
|   NPT_IpAddress::NPT_IpAddress
+---------------------------------------------------------------------*/
NPT_IpAddress::NPT_IpAddress(unsigned long address)
{
    Set(address);
}

/*----------------------------------------------------------------------
|   NPT_IpAddress::NPT_IpAddress
+---------------------------------------------------------------------*/
NPT_IpAddress::NPT_IpAddress(unsigned char a, 
                             unsigned char b, 
                             unsigned char c, 
                             unsigned char d)
{
    m_Address[0] = a;
    m_Address[1] = b;
    m_Address[2] = c;
    m_Address[3] = d;
}

/*----------------------------------------------------------------------
|   NPT_IpAddress::Parse
+---------------------------------------------------------------------*/
NPT_Result
NPT_IpAddress::Parse(const char* name)
{
    // check the name
    if (name == NULL) return NPT_ERROR_INVALID_PARAMETERS;

    // clear the address
    m_Address[0] = m_Address[1] = m_Address[2] = m_Address[3] = 0;

    // parse
    unsigned int  fragment;
    bool          fragment_empty = true;
    unsigned char address[4];
    unsigned int  accumulator;
    for (fragment = 0, accumulator = 0; fragment < 4; ++name) {
        if (*name == '\0' || *name == '.') {
            // fragment terminator
            if (fragment_empty) return NPT_ERROR_INVALID_SYNTAX;
            address[fragment++] = accumulator;
            if (*name == '\0') break;
            accumulator = 0;
            fragment_empty = true;
        } else if (*name >= '0' && *name <= '9') {
            // numerical character
            accumulator = accumulator*10 + (*name - '0');
            if (accumulator > 255) return NPT_ERROR_INVALID_SYNTAX;
            fragment_empty = false; 
        } else {
            // invalid character
            return NPT_ERROR_INVALID_SYNTAX;
        }
    }

    if (fragment == 4 && *name == '\0' && !fragment_empty) {
        m_Address[0] = address[0];
        m_Address[1] = address[1];
        m_Address[2] = address[2];
        m_Address[3] = address[3];
        return NPT_SUCCESS;
    } else {
        return NPT_ERROR_INVALID_SYNTAX;
    }
}

/*----------------------------------------------------------------------
|   NPT_IpAddress::AsLong
+---------------------------------------------------------------------*/
unsigned long
NPT_IpAddress::AsLong() const
{
    return 
        (((unsigned long)m_Address[0])<<24) |
        (((unsigned long)m_Address[1])<<16) |
        (((unsigned long)m_Address[2])<< 8) |
        (((unsigned long)m_Address[3]));
}

/*----------------------------------------------------------------------
|   NPT_IpAddress::AsBytes
+---------------------------------------------------------------------*/
const unsigned char* 
NPT_IpAddress::AsBytes() const
{
    return m_Address;
}

/*----------------------------------------------------------------------
|   NPT_IpAddress::ToString
+---------------------------------------------------------------------*/
NPT_String
NPT_IpAddress::ToString() const
{
    NPT_String address;
    address.Reserve(16);
    address += NPT_String::FromInteger(m_Address[0]);
    address += '.';
    address += NPT_String::FromInteger(m_Address[1]);
    address += '.';
    address += NPT_String::FromInteger(m_Address[2]);
    address += '.';
    address += NPT_String::FromInteger(m_Address[3]);

    return address;
}

/*----------------------------------------------------------------------
|   NPT_IpAddress::Set
+---------------------------------------------------------------------*/
NPT_Result    
NPT_IpAddress::Set(const unsigned char bytes[4])
{
    m_Address[0] = bytes[0];
    m_Address[1] = bytes[1];
    m_Address[2] = bytes[2];
    m_Address[3] = bytes[3];

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_IpAddress::Set
+---------------------------------------------------------------------*/
NPT_Result    
NPT_IpAddress::Set(unsigned long address)
{
    m_Address[0] = (unsigned char)((address >> 24) & 0xFF);
    m_Address[1] = (unsigned char)((address >> 16) & 0xFF);
    m_Address[2] = (unsigned char)((address >>  8) & 0xFF);
    m_Address[3] = (unsigned char)((address      ) & 0xFF);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_IpAddress::operator==
+---------------------------------------------------------------------*/
bool
NPT_IpAddress::operator==(const NPT_IpAddress& other) const
{
    return other.AsLong() == AsLong();
}

/*----------------------------------------------------------------------
|   NPT_MacAddress::NPT_MacAddress
+---------------------------------------------------------------------*/
NPT_MacAddress::NPT_MacAddress(Type                  type,
                               const unsigned char*  address, 
                               unsigned int          length)
{
    SetAddress(type, address, length);
}

/*----------------------------------------------------------------------
|   NPT_MacAddress::SetAddress
+---------------------------------------------------------------------*/
void
NPT_MacAddress::SetAddress(Type                 type,
                           const unsigned char* address, 
                           unsigned int         length)
{
    m_Type = type;
    if (length > NPT_NETWORK_MAX_MAC_ADDRESS_LENGTH) {
        length = NPT_NETWORK_MAX_MAC_ADDRESS_LENGTH;
    }
    m_Length = length;
    for (unsigned int i=0; i<length; i++) {
        m_Address[i] = address[i];
    }
}

/*----------------------------------------------------------------------
|   NPT_MacAddress::ToString
+---------------------------------------------------------------------*/
NPT_String
NPT_MacAddress::ToString() const
{
    NPT_String result;
 
    if (m_Length) {
        char s[3*NPT_NETWORK_MAX_MAC_ADDRESS_LENGTH];
        const char hex[17] = "0123456789abcdef";
        for (unsigned int i=0; i<m_Length; i++) {
            s[i*3  ] = hex[m_Address[i]>>4];
            s[i*3+1] = hex[m_Address[i]&0xf];
            s[i*3+2] = ':';
        }
        s[3*m_Length-1] = '\0';
        result = s;
    }

    return result;
}

/*----------------------------------------------------------------------
|   NPT_NetworkInterface::NPT_NetworkInterface
+---------------------------------------------------------------------*/ 
NPT_NetworkInterface::NPT_NetworkInterface(const char*           name,
                                           const NPT_MacAddress& mac,
                                           NPT_Flags             flags) :
    m_Name(name),
    m_MacAddress(mac),
    m_Flags(flags)
{
}

/*----------------------------------------------------------------------
|   NPT_NetworkInterface::NPT_NetworkInterface
+---------------------------------------------------------------------*/ 
NPT_NetworkInterface::NPT_NetworkInterface(const char* name,
                                           NPT_Flags   flags) :
    m_Name(name),
    m_Flags(flags)
{
}

/*----------------------------------------------------------------------
|   NPT_NetworkInterface::AddAddress
+---------------------------------------------------------------------*/ 
NPT_Result
NPT_NetworkInterface::AddAddress(const NPT_NetworkInterfaceAddress& address)
{
    return m_Addresses.Add(address);
}


