/*****************************************************************
|
|   Neptune - Network Sockets
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

#ifndef _NPT_SOCKETS_H_
#define _NPT_SOCKETS_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptTypes.h"
#include "NptConstants.h"
#include "NptStreams.h"
#include "NptStrings.h"
#include "NptDataBuffer.h"
#include "NptNetwork.h"

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
const int NPT_ERROR_CONNECTION_RESET      = NPT_ERROR_BASE_SOCKET - 0;
const int NPT_ERROR_CONNECTION_ABORTED    = NPT_ERROR_BASE_SOCKET - 1;
const int NPT_ERROR_CONNECTION_REFUSED    = NPT_ERROR_BASE_SOCKET - 2;
const int NPT_ERROR_CONNECTION_FAILED     = NPT_ERROR_BASE_SOCKET - 3;
const int NPT_ERROR_HOST_UNKNOWN          = NPT_ERROR_BASE_SOCKET - 4;
const int NPT_ERROR_SOCKET_FAILED         = NPT_ERROR_BASE_SOCKET - 5;
const int NPT_ERROR_GETSOCKOPT_FAILED     = NPT_ERROR_BASE_SOCKET - 6;
const int NPT_ERROR_SETSOCKOPT_FAILED     = NPT_ERROR_BASE_SOCKET - 7;
const int NPT_ERROR_SOCKET_CONTROL_FAILED = NPT_ERROR_BASE_SOCKET - 8;
const int NPT_ERROR_BIND_FAILED           = NPT_ERROR_BASE_SOCKET - 9;
const int NPT_ERROR_LISTEN_FAILED         = NPT_ERROR_BASE_SOCKET - 10;
const int NPT_ERROR_ACCEPT_FAILED         = NPT_ERROR_BASE_SOCKET - 11;
const int NPT_ERROR_ADDRESS_IN_USE        = NPT_ERROR_BASE_SOCKET - 12;
const int NPT_ERROR_NETWORK_DOWN          = NPT_ERROR_BASE_SOCKET - 13;
const int NPT_ERROR_NETWORK_UNREACHABLE   = NPT_ERROR_BASE_SOCKET - 14;
const int NPT_ERROR_NOT_CONNECTED         = NPT_ERROR_BASE_SOCKET - 15;

const unsigned int NPT_SOCKET_FLAG_CANCELLABLE = 1; // make the socket cancellable

/*----------------------------------------------------------------------
|   forward references
+---------------------------------------------------------------------*/
class NPT_Socket;

/*----------------------------------------------------------------------
|   NPT_SocketAddress
+---------------------------------------------------------------------*/
class NPT_SocketAddress 
{
public:
    // constructors and destructor
    NPT_SocketAddress() : m_Port(0) {}
    NPT_SocketAddress(const NPT_IpAddress& address, NPT_IpPort port) :
        m_IpAddress(address),
        m_Port(port) {}

    // methods
    NPT_Result SetIpAddress(const NPT_IpAddress& address) {
        m_IpAddress = address;
        return NPT_SUCCESS;
    }
    const NPT_IpAddress& GetIpAddress() const { 
        return m_IpAddress; 
    }
    NPT_Result SetPort(NPT_IpPort port) { 
        m_Port = port; 
        return NPT_SUCCESS; 
    }
    NPT_IpPort GetPort() const { 
        return m_Port; 
    }
    NPT_String ToString() const;

    // operators
    bool operator==(const NPT_SocketAddress& other) const;

private:
    // members
    NPT_IpAddress m_IpAddress;
    NPT_IpPort    m_Port;
};

/*----------------------------------------------------------------------
|   NPT_SocketInfo
+---------------------------------------------------------------------*/
typedef struct {
    NPT_SocketAddress local_address;
    NPT_SocketAddress remote_address;
} NPT_SocketInfo;

/*----------------------------------------------------------------------
|   NPT_SocketInterface
+---------------------------------------------------------------------*/
class NPT_SocketInterface
{
 public:
    virtual ~NPT_SocketInterface() {}

    // interface methods
    virtual NPT_Result Bind(const NPT_SocketAddress& address, bool reuse_address = true) = 0;
    virtual NPT_Result Connect(const NPT_SocketAddress& address, NPT_Timeout timeout) = 0;
    virtual NPT_Result WaitForConnection(NPT_Timeout timeout) = 0;
    virtual NPT_Result GetInputStream(NPT_InputStreamReference& stream) = 0;
    virtual NPT_Result GetOutputStream(NPT_OutputStreamReference& stream) = 0;
    virtual NPT_Result GetInfo(NPT_SocketInfo& info) = 0;
    virtual NPT_Result SetReadTimeout(NPT_Timeout timeout) = 0;
    virtual NPT_Result SetWriteTimeout(NPT_Timeout timeout) = 0;
    virtual NPT_Result Cancel(bool shutdown=true) = 0;
};

/*----------------------------------------------------------------------
|   NPT_UdpSocketInterface
+---------------------------------------------------------------------*/
class NPT_UdpSocketInterface
{
 public:
    virtual ~NPT_UdpSocketInterface() {}

    // methods
    virtual NPT_Result Send(const NPT_DataBuffer&    packet, 
                            const NPT_SocketAddress* address = NULL) = 0;
    virtual NPT_Result Receive(NPT_DataBuffer&    packet, 
                               NPT_SocketAddress* address = NULL) = 0;
};

/*----------------------------------------------------------------------
|   NPT_UdpMulticastSocketInterface
+---------------------------------------------------------------------*/
class NPT_UdpMulticastSocketInterface
{
 public:
    virtual ~NPT_UdpMulticastSocketInterface() {}

    // methods
    virtual NPT_Result JoinGroup(const NPT_IpAddress& group, 
                                 const NPT_IpAddress& iface) = 0;
    virtual NPT_Result LeaveGroup(const NPT_IpAddress& group,
                                  const NPT_IpAddress& iface) = 0;
    virtual NPT_Result SetTimeToLive(unsigned char ttl) = 0;
    virtual NPT_Result SetInterface(const NPT_IpAddress& iface) = 0;
};

/*----------------------------------------------------------------------
|   NPT_TcpServerSocketInterface
+---------------------------------------------------------------------*/
class NPT_TcpServerSocketInterface
{
 public:
    virtual ~NPT_TcpServerSocketInterface() {}

    // interface methods
    virtual NPT_Result Listen(unsigned int max_clients) = 0;
    virtual NPT_Result WaitForNewClient(NPT_Socket*& client, 
                                        NPT_Timeout  timeout,
                                        NPT_Flags    flags) = 0;
};

/*----------------------------------------------------------------------
|   NPT_Socket
+---------------------------------------------------------------------*/
class NPT_Socket : public NPT_SocketInterface
{
public:
    // constructor and destructor
    explicit NPT_Socket(NPT_SocketInterface* delegate) : m_SocketDelegate(delegate) {}
    virtual ~NPT_Socket();

    // delegate NPT_SocketInterface methods
    NPT_Result Bind(const NPT_SocketAddress& address, bool reuse_address = true) {             
        return m_SocketDelegate->Bind(address, reuse_address);                            
    }                                                               
    NPT_Result Connect(const NPT_SocketAddress& address,            
                       NPT_Timeout timeout = NPT_TIMEOUT_INFINITE) {
       return m_SocketDelegate->Connect(address, timeout);                 
    }                                                               
    NPT_Result WaitForConnection(NPT_Timeout timeout = NPT_TIMEOUT_INFINITE) {
        return m_SocketDelegate->WaitForConnection(timeout);                 
    } 
    NPT_Result GetInputStream(NPT_InputStreamReference& stream) {   
        return m_SocketDelegate->GetInputStream(stream);                   
    }                                                               
    NPT_Result GetOutputStream(NPT_OutputStreamReference& stream) { 
    return m_SocketDelegate->GetOutputStream(stream);                      
    }                                                               
    NPT_Result GetInfo(NPT_SocketInfo& info) {                      
        return m_SocketDelegate->GetInfo(info);                            
    }                                                               
    NPT_Result SetReadTimeout(NPT_Timeout timeout) {                      
        return m_SocketDelegate->SetReadTimeout(timeout);                            
    }                                                          
    NPT_Result SetWriteTimeout(NPT_Timeout timeout) {                      
        return m_SocketDelegate->SetWriteTimeout(timeout);                            
    }                                                          
    NPT_Result Cancel(bool shutdown=true) {                      
        return m_SocketDelegate->Cancel(shutdown);                            
    }                                                          

protected:
    // constructor
    NPT_Socket() {}

    // members
    NPT_SocketInterface* m_SocketDelegate;
};

typedef NPT_Reference<NPT_Socket> NPT_SocketReference;

/*----------------------------------------------------------------------
|   NPT_UdpSocket
+---------------------------------------------------------------------*/
class NPT_UdpSocket : public NPT_Socket,
                      public NPT_UdpSocketInterface
{
 public:
    // constructor and destructor
             NPT_UdpSocket(NPT_Flags flags=NPT_SOCKET_FLAG_CANCELLABLE);
    virtual ~NPT_UdpSocket();

    // delegate NPT_UdpSocketInterface methods
    NPT_Result Send(const NPT_DataBuffer&    packet,           
                    const NPT_SocketAddress* address = NULL) {
        return m_UdpSocketDelegate->Send(packet, address);              
    }                                                         
    NPT_Result Receive(NPT_DataBuffer&     packet,            
                       NPT_SocketAddress*  address = NULL) {  
        return m_UdpSocketDelegate->Receive(packet, address);           
    }

protected:
    // constructor
    NPT_UdpSocket(NPT_UdpSocketInterface* delegate);

    // members
    NPT_UdpSocketInterface* m_UdpSocketDelegate;
};

/*----------------------------------------------------------------------
|   NPT_UdpMulticastSocket
+---------------------------------------------------------------------*/
class NPT_UdpMulticastSocket : public NPT_UdpSocket, 
                               public NPT_UdpMulticastSocketInterface
{
public:
    // constructor and destructor
             NPT_UdpMulticastSocket(NPT_Flags flags=NPT_SOCKET_FLAG_CANCELLABLE);
    virtual ~NPT_UdpMulticastSocket();

    // delegate NPT_UdpMulticastSocketInterface methods
    NPT_Result JoinGroup(const NPT_IpAddress& group,            
                         const NPT_IpAddress& iface =           
                         NPT_IpAddress::Any) {                  
        return m_UdpMulticastSocketDelegate->JoinGroup(group, iface);
    }                                                           
    NPT_Result LeaveGroup(const NPT_IpAddress& group,           
                          const NPT_IpAddress& iface =          
                          NPT_IpAddress::Any) {                 
        return m_UdpMulticastSocketDelegate->LeaveGroup(group, iface);
    }                                                          
    NPT_Result SetTimeToLive(unsigned char ttl) {     
        return m_UdpMulticastSocketDelegate->SetTimeToLive(ttl); 
    }
    NPT_Result SetInterface(const NPT_IpAddress& iface) {
        return m_UdpMulticastSocketDelegate->SetInterface(iface);
    }

protected:
    // members
    NPT_UdpMulticastSocketInterface* m_UdpMulticastSocketDelegate;
};

/*----------------------------------------------------------------------
|   NPT_TcpClientSocket
+---------------------------------------------------------------------*/
class NPT_TcpClientSocket : public NPT_Socket
{
public:
    // constructors and destructor
             NPT_TcpClientSocket(NPT_Flags flags=NPT_SOCKET_FLAG_CANCELLABLE);
    virtual ~NPT_TcpClientSocket();
};

/*----------------------------------------------------------------------
|   NPT_TcpServerSocket
+---------------------------------------------------------------------*/
class NPT_TcpServerSocket : public NPT_Socket,
                            public NPT_TcpServerSocketInterface
{
public:
    // constructors and destructor
             NPT_TcpServerSocket(NPT_Flags flags=NPT_SOCKET_FLAG_CANCELLABLE);
    virtual ~NPT_TcpServerSocket();

    // delegate NPT_TcpServerSocketInterface methods
    NPT_Result Listen(unsigned int max_clients) {   
        return m_TcpServerSocketDelegate->Listen(max_clients);
    }
    NPT_Result WaitForNewClient(NPT_Socket*& client, 
                                NPT_Timeout  timeout = NPT_TIMEOUT_INFINITE,
                                NPT_Flags    flags = 0) {
        return m_TcpServerSocketDelegate->WaitForNewClient(client, timeout, flags);
    }

protected:
    // members
    NPT_TcpServerSocketInterface* m_TcpServerSocketDelegate;
};

#endif // _NPT_SOCKETS_H_
