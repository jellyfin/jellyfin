/*****************************************************************
|
|   Neptune - TLS/SSL Support
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

#ifndef _NPT_TLS_H_
#define _NPT_TLS_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptConfig.h"
#include "NptStreams.h"
#include "NptTime.h"
#include "NptHttp.h"

/*----------------------------------------------------------------------
|   error codes
+---------------------------------------------------------------------*/
const NPT_Result NPT_ERROR_INVALID_PASSWORD                     = (NPT_ERROR_BASE_TLS-1);
const NPT_Result NPT_ERROR_TLS_INVALID_HANDSHAKE                = (NPT_ERROR_BASE_TLS-2);
const NPT_Result NPT_ERROR_TLS_INVALID_PROTOCOL_MESSAGE         = (NPT_ERROR_BASE_TLS-3);
const NPT_Result NPT_ERROR_TLS_INVALID_HMAC                     = (NPT_ERROR_BASE_TLS-4);
const NPT_Result NPT_ERROR_TLS_INVALID_VERSION                  = (NPT_ERROR_BASE_TLS-5);
const NPT_Result NPT_ERROR_TLS_INVALID_SESSION                  = (NPT_ERROR_BASE_TLS-6);
const NPT_Result NPT_ERROR_TLS_NO_CIPHER                        = (NPT_ERROR_BASE_TLS-7);
const NPT_Result NPT_ERROR_TLS_BAD_CERTIFICATE                  = (NPT_ERROR_BASE_TLS-8);
const NPT_Result NPT_ERROR_TLS_INVALID_KEY                      = (NPT_ERROR_BASE_TLS-9);
const NPT_Result NPT_ERROR_TLS_NO_CLIENT_RENEGOTIATION          = (NPT_ERROR_BASE_TLS-10);
const NPT_Result NPT_ERROR_TLS_INVALID_FINISHED_MESSAGE         = (NPT_ERROR_BASE_TLS-11);
const NPT_Result NPT_ERROR_TLS_NO_CERTIFICATE_DEFINED           = (NPT_ERROR_BASE_TLS-12);
const NPT_Result NPT_ERROR_TLS_ALERT_HANDSHAKE_FAILED           = (NPT_ERROR_BASE_TLS-13); 
const NPT_Result NPT_ERROR_TLS_ALERT_BAD_CERTIFICATE            = (NPT_ERROR_BASE_TLS-14);
const NPT_Result NPT_ERROR_TLS_ALERT_INVALID_VERSION            = (NPT_ERROR_BASE_TLS-15);
const NPT_Result NPT_ERROR_TLS_ALERT_BAD_RECORD_MAC             = (NPT_ERROR_BASE_TLS-16);
const NPT_Result NPT_ERROR_TLS_ALERT_DECODE_ERROR               = (NPT_ERROR_BASE_TLS-17);
const NPT_Result NPT_ERROR_TLS_ALERT_DECRYPT_ERROR              = (NPT_ERROR_BASE_TLS-18);
const NPT_Result NPT_ERROR_TLS_ALERT_ILLEGAL_PARAMETER          = (NPT_ERROR_BASE_TLS-19);
const NPT_Result NPT_ERROR_TLS_ALERT_UNEXPECTED_MESSAGE         = (NPT_ERROR_BASE_TLS-20);
const NPT_Result NPT_ERROR_TLS_CERTIFICATE_FAILURE              = (NPT_ERROR_BASE_TLS-21);
const NPT_Result NPT_ERROR_TLS_CERTIFICATE_NO_TRUST_ANCHOR      = (NPT_ERROR_BASE_TLS-22);
const NPT_Result NPT_ERROR_TLS_CERTIFICATE_BAD_SIGNATURE        = (NPT_ERROR_BASE_TLS-23);      
const NPT_Result NPT_ERROR_TLS_CERTIFICATE_NOT_YET_VALID        = (NPT_ERROR_BASE_TLS-24);
const NPT_Result NPT_ERROR_TLS_CERTIFICATE_EXPIRED              = (NPT_ERROR_BASE_TLS-25);
const NPT_Result NPT_ERROR_TLS_CERTIFICATE_SELF_SIGNED          = (NPT_ERROR_BASE_TLS-26);
const NPT_Result NPT_ERROR_TLS_CERTIFICATE_INVALID_CHAIN        = (NPT_ERROR_BASE_TLS-27);
const NPT_Result NPT_ERROR_TLS_CERTIFICATE_UNSUPPORTED_DIGEST   = (NPT_ERROR_BASE_TLS-28);
const NPT_Result NPT_ERROR_TLS_CERTIFICATE_INVALID_PRIVATE_KEY  = (NPT_ERROR_BASE_TLS-29);
const NPT_Result NPT_ERROR_TLS_DNS_NAME_MISMATCH                = (NPT_ERROR_BASE_TLS-30);

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
const unsigned int NPT_TLS_NULL_WITH_NULL_NULL      = 0x00;
const unsigned int NPT_TLS_RSA_WITH_RC4_128_MD5     = 0x04;
const unsigned int NPT_TLS_RSA_WITH_RC4_128_SHA     = 0x05;
const unsigned int NPT_TLS_RSA_WITH_AES_128_CBC_SHA = 0x2F;
const unsigned int NPT_TLS_RSA_WITH_AES_256_CBC_SHA = 0x35;

/*----------------------------------------------------------------------
|   class references
+---------------------------------------------------------------------*/
class NPT_TlsContextImpl;
class NPT_TlsSessionImpl;

/*----------------------------------------------------------------------
|   types
+---------------------------------------------------------------------*/
typedef enum {
    NPT_TLS_KEY_FORMAT_RSA_PRIVATE,
    NPT_TLS_KEY_FORMAT_PKCS8,
    NPT_TLS_KEY_FORMAT_PKCS12
} NPT_TlsKeyFormat;

struct NPT_TlsTrustAnchorData {
    const unsigned char* cert_data;
    unsigned int         cert_size;
};

/*----------------------------------------------------------------------
|   NPT_Tls
+---------------------------------------------------------------------*/
class NPT_Tls
{
public:
    static const NPT_TlsTrustAnchorData* GetDefaultTrustAnchors(NPT_Ordinal indx=0);
    static bool MatchDnsNames(const char*                 hostname,
                              const NPT_List<NPT_String>& dns_names);
    static bool MatchDnsName(const char* hostname, const char* dns_name);
                              
private:
    NPT_Tls() {}; // don't instantiate 
};

/*----------------------------------------------------------------------
|   NPT_TlsContext
+---------------------------------------------------------------------*/
class NPT_TlsContext
{
public:
    enum {
        OPTION_VERIFY_LATER               = 1,
        OPTION_REQUIRE_CLIENT_CERTIFICATE = 2,
        OPTION_ADD_DEFAULT_TRUST_ANCHORS  = 4
    };
    NPT_TlsContext(NPT_Flags options=0);
   ~NPT_TlsContext();
   
    // methods
    NPT_Result LoadKey(NPT_TlsKeyFormat     key_format, 
                       const unsigned char* key_data,
                       NPT_Size             key_data_size,
                       const char*          password);
      
    NPT_Result SelfSignCertificate(const char* common_name,
                                   const char* organization,
                                   const char* organizational_name);
                                   
    /** 
     * Add one trust anchor
     */
    NPT_Result AddTrustAnchor(const unsigned char* ta_data,
                              NPT_Size             ta_data_size);
    /**
     * Add one or more trust anchors, from a list
     * @param anchors Array of trust anchor data
     * @param anchor_count Number of anchors in the array, or 0 if the array
     * is terminated by a 'sentinel' (an anchor data with the field cert_data set
     * to NULL and the field cert_size set to 0).
     */
    NPT_Result AddTrustAnchors(const NPT_TlsTrustAnchorData* anchors, 
                               NPT_Cardinal                  anchor_count = 0);

protected:
    NPT_TlsContextImpl* m_Impl;
    
    // friends
    friend class NPT_TlsSession;
    friend class NPT_TlsClientSession;
    friend class NPT_TlsServerSession;
};

/*----------------------------------------------------------------------
|   NPT_TlsCertificateInfo
+---------------------------------------------------------------------*/
struct NPT_TlsCertificateInfo
{
    struct _subject {
        NPT_String common_name;
        NPT_String organization;
        NPT_String organizational_name;
    } subject;
    struct _issuer {
        NPT_String common_name;
        NPT_String organization;
        NPT_String organizational_name;
    } issuer;
    struct _fingerprint {
        unsigned char sha1[20];
        unsigned char md5[16];
    } fingerprint;
    NPT_DateTime issue_date;
    NPT_DateTime expiration_date;
    NPT_List<NPT_String> alternate_names;
};

/*----------------------------------------------------------------------
|   NPT_TlsSession
+---------------------------------------------------------------------*/
class NPT_TlsSession
{
public:
    virtual ~NPT_TlsSession();
    virtual NPT_Result Handshake();
    virtual NPT_Result GetHandshakeStatus();
    virtual NPT_Result GetPeerCertificateInfo(NPT_TlsCertificateInfo& info, NPT_Ordinal position=0);
    virtual NPT_Result VerifyPeerCertificate();
    virtual NPT_Result VerifyDnsNameMatch(const char* hostname);
    virtual NPT_Result GetSessionId(NPT_DataBuffer& session_id);
    virtual NPT_UInt32 GetCipherSuiteId();
    virtual NPT_Result GetInputStream(NPT_InputStreamReference& stream);
    virtual NPT_Result GetOutputStream(NPT_OutputStreamReference& stream);
    
protected:
    NPT_TlsSession(NPT_TlsContext&     context, 
                   NPT_TlsSessionImpl* impl);

    NPT_TlsContext&                   m_Context;
    NPT_Reference<NPT_TlsSessionImpl> m_Impl;
    NPT_InputStreamReference          m_InputStream;
    NPT_OutputStreamReference         m_OutputStream;
};

/*----------------------------------------------------------------------
|   NPT_TlsClientSession
+---------------------------------------------------------------------*/
class NPT_TlsClientSession : public NPT_TlsSession
{
public:
    NPT_TlsClientSession(NPT_TlsContext&            context,
                         NPT_InputStreamReference&  input,
                         NPT_OutputStreamReference& output);
};

/*----------------------------------------------------------------------
|   NPT_TlsServerSession
+---------------------------------------------------------------------*/
class NPT_TlsServerSession : public NPT_TlsSession
{
public:
    NPT_TlsServerSession(NPT_TlsContext&            context,
                         NPT_InputStreamReference&  input,
                         NPT_OutputStreamReference& output);
};

/*----------------------------------------------------------------------
|   NPT_HttpTlsConnector
+---------------------------------------------------------------------*/
class NPT_HttpTlsConnector : public NPT_HttpClient::Connector
{
public:
    enum {
        OPTION_ACCEPT_SELF_SIGNED_CERTS = 1,
        OPTION_ACCEPT_HOSTNAME_MISMATCH = 2
    };
    NPT_HttpTlsConnector(NPT_Flags options = 0);
    NPT_HttpTlsConnector(NPT_TlsContext& tls_context, NPT_Flags options = 0);
    virtual ~NPT_HttpTlsConnector() {}
    NPT_TlsContext& GetTlsContext() { return m_TlsContext; }
    
    virtual NPT_Result VerifyPeer(NPT_TlsClientSession& session,
                                  const char*           hostname);
    
    //  NPT_HttpClient::Connector methods
    virtual NPT_Result Connect(const NPT_HttpUrl&           url,
                               NPT_HttpClient&              client,
                               const NPT_HttpProxyAddress*  proxy,
                               bool                         reuse,
                               NPT_HttpClient::Connection*& connection);
    virtual NPT_Result Abort() { return NPT_ERROR_NOT_IMPLEMENTED; }

private:
    // singleton management
    class Cleanup {
        static Cleanup AutomaticCleaner;
        ~Cleanup() {
            if (DefaultTlsContext) {
                delete DefaultTlsContext;
                DefaultTlsContext = NULL;
            }
        }
    };
    
    // class methods
    static NPT_TlsContext& GetDefaultTlsContext();
    
    // class members
    static NPT_TlsContext* DefaultTlsContext;
    
    // members
    NPT_TlsContext& m_TlsContext;
    NPT_Flags       m_Options;
};

/*----------------------------------------------------------------------
|   Trust Anchors
+-----------------------------------------------------------------*/
/** 
 * Arrays of trust anchors (each array element is of type NPT_TlsTrustAnchorData
 * and the last element is a terminator element: the cert_data field is NULL
 * and the cert_size field is 0
 */ 
#include "NptTlsDefaultTrustAnchorsBase.h"
#include "NptTlsDefaultTrustAnchorsExtended.h"

#endif // _NPT_TLS_H_
