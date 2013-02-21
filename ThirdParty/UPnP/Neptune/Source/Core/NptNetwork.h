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

#ifndef _NPT_NETWORK_H_
#define _NPT_NETWORK_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptTypes.h"
#include "NptConstants.h"
#include "NptStrings.h"
#include "NptList.h"

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
const unsigned int NPT_NETWORK_MAX_MAC_ADDRESS_LENGTH  = 8;

/*----------------------------------------------------------------------
|   flags
+---------------------------------------------------------------------*/
#define NPT_NETWORK_INTERFACE_FLAG_LOOPBACK       0x01
#define NPT_NETWORK_INTERFACE_FLAG_PROMISCUOUS    0x02
#define NPT_NETWORK_INTERFACE_FLAG_BROADCAST      0x04
#define NPT_NETWORK_INTERFACE_FLAG_MULTICAST      0x08
#define NPT_NETWORK_INTERFACE_FLAG_POINT_TO_POINT 0x10

/*----------------------------------------------------------------------
|   workarounds
+---------------------------------------------------------------------*/
#if defined(_WIN32)
#if defined(SetPort)
#undef SetPort
#endif
#endif

/*----------------------------------------------------------------------
|   types
+---------------------------------------------------------------------*/
typedef unsigned int NPT_IpPort;

/*----------------------------------------------------------------------
|   NPT_IpAddress
+---------------------------------------------------------------------*/
class NPT_IpAddress
{
public:
    // class members
    static const NPT_IpAddress Any;

    // constructors and destructor
    NPT_IpAddress();
    NPT_IpAddress(unsigned long address);
    NPT_IpAddress(unsigned char a, unsigned char b, unsigned char c, unsigned char d);

    // methods
    NPT_Result       ResolveName(const char* name, 
                                 NPT_Timeout timeout = NPT_TIMEOUT_INFINITE);
    NPT_Result       Parse(const char* name);
    NPT_Result       Set(unsigned long address);
    NPT_Result       Set(const unsigned char bytes[4]);
    const unsigned char* AsBytes() const;
    unsigned long    AsLong() const;
    NPT_String       ToString() const;
    
    // operators
    bool             operator==(const NPT_IpAddress& other) const;
    
    // FIXME: temporary
    NPT_String       m_HostName;

private:
    // members
    unsigned char m_Address[4];
};

/*----------------------------------------------------------------------
|   NPT_MacAddress
+---------------------------------------------------------------------*/
class NPT_MacAddress
{
public:
    // typedef enum
    typedef enum {
        TYPE_UNKNOWN,
        TYPE_LOOPBACK,
        TYPE_ETHERNET,
        TYPE_PPP,
        TYPE_IEEE_802_11
    } Type;
    
    // constructors and destructor
    NPT_MacAddress() : m_Type(TYPE_UNKNOWN), m_Length(0) {}
    NPT_MacAddress(Type           type,
                   const unsigned char* addr, 
                   unsigned int   length);
    
    // methods
    void                 SetAddress(Type type, const unsigned char* addr,
                                    unsigned int length);
    Type                 GetType() const    { return m_Type; }
    const unsigned char* GetAddress() const { return m_Address; }
    unsigned int         GetLength() const  { return m_Length; }
    NPT_String           ToString() const;
    
private:
    // members
    Type          m_Type;
    unsigned char m_Address[NPT_NETWORK_MAX_MAC_ADDRESS_LENGTH];
    unsigned int  m_Length;
};

/*----------------------------------------------------------------------
|   NPT_NetworkInterfaceAddress
+---------------------------------------------------------------------*/
class NPT_NetworkInterfaceAddress
{
public:
    // constructors and destructor
    NPT_NetworkInterfaceAddress(const NPT_IpAddress& primary,
                                const NPT_IpAddress& broadcast,
                                const NPT_IpAddress& destination,
                                const NPT_IpAddress& netmask) :
        m_PrimaryAddress(primary),
        m_BroadcastAddress(broadcast),
        m_DestinationAddress(destination),
        m_NetMask(netmask) {}

    // methods
    const NPT_IpAddress& GetPrimaryAddress() const {
        return m_PrimaryAddress;
    }
    const NPT_IpAddress& GetBroadcastAddress() const {
        return m_BroadcastAddress;
    }
    const NPT_IpAddress& GetDestinationAddress() const {
        return m_DestinationAddress;
    }
    const NPT_IpAddress& GetNetMask() const {
        return m_NetMask;
    }
    
    bool IsAddressInNetwork(const NPT_IpAddress& address) {
        if (m_PrimaryAddress.AsLong() == address.AsLong()) return true;
        if (m_NetMask.AsLong() == 0) return false;
        return (m_PrimaryAddress.AsLong() & m_NetMask.AsLong()) == (address.AsLong() & m_NetMask.AsLong());
    }

private:
    // members
    NPT_IpAddress m_PrimaryAddress;
    NPT_IpAddress m_BroadcastAddress;
    NPT_IpAddress m_DestinationAddress;
    NPT_IpAddress m_NetMask;
};

/*----------------------------------------------------------------------
|   NPT_NetworkInterface
+---------------------------------------------------------------------*/
class NPT_NetworkInterface
{
public:
    // class methods
    static NPT_Result GetNetworkInterfaces(NPT_List<NPT_NetworkInterface*>& interfaces);

    // constructors and destructor
    NPT_NetworkInterface(const char*           name,
                         const NPT_MacAddress& mac,
                         NPT_Flags             flags);
    NPT_NetworkInterface(const char*           name,
                         NPT_Flags             flags);
   ~NPT_NetworkInterface() {}

    // methods
    NPT_Result AddAddress(const NPT_NetworkInterfaceAddress& address);
    const NPT_String& GetName() const {
        return m_Name;
    }
    const NPT_MacAddress& GetMacAddress() const {
        return m_MacAddress;
    }
    void SetMacAddress(NPT_MacAddress::Type type,
                       const unsigned char* addr, 
                       unsigned int         length) {
        m_MacAddress.SetAddress(type, addr, length);
    }
    NPT_Flags GetFlags() const { return m_Flags; }
    const NPT_List<NPT_NetworkInterfaceAddress>& GetAddresses() const {
        return m_Addresses;
    }    
    
    bool IsAddressInNetwork(const NPT_IpAddress& address) {
        NPT_List<NPT_NetworkInterfaceAddress>::Iterator iter = m_Addresses.GetFirstItem();
        while (iter) {
            if ((*iter).IsAddressInNetwork(address)) return true;
           ++iter;
        }
        return false;
    }
    
private:
    // members
    NPT_String                            m_Name;
    NPT_MacAddress                        m_MacAddress;
    NPT_Flags                             m_Flags;
    NPT_List<NPT_NetworkInterfaceAddress> m_Addresses;
};

/*----------------------------------------------------------------------
|   NPT_NetworkNameResolver
+---------------------------------------------------------------------*/
class NPT_NetworkNameResolver
{
public:
    // class methods
    static NPT_Result Resolve(const char*              name, 
                              NPT_List<NPT_IpAddress>& addresses,
                              NPT_Timeout              timeout = NPT_TIMEOUT_INFINITE);
};

#endif // _NPT_NETWORK_H_
