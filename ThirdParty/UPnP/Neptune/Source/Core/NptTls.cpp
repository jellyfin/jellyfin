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

#if defined(NPT_CONFIG_ENABLE_TLS)

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptConfig.h"
#include "NptTls.h"
#include "NptLogging.h"
#include "NptUtils.h"
#include "NptSockets.h"
#include "NptSystem.h"
#include "NptDigest.h"

#include "ssl.h"

/*----------------------------------------------------------------------
|   logging
+---------------------------------------------------------------------*/
NPT_SET_LOCAL_LOGGER("neptune.tls")

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
const unsigned int NPT_TLS_CONTEXT_DEFAULT_SESSION_CACHE = 16;
const unsigned int NPT_HTTP_TLS_CONNECTOR_MAX_PROXY_RESPONSE_SIZE = (16*1024);

/*----------------------------------------------------------------------
|   types
+---------------------------------------------------------------------*/
typedef NPT_Reference<NPT_TlsSessionImpl> NPT_TlsSessionImplReference;

/*----------------------------------------------------------------------
|   SSL_DateTime_Before
+---------------------------------------------------------------------*/
int
SSL_DateTime_Before(const SSL_DateTime* t1, const SSL_DateTime* t2)
{
    if (t1->year == t2->year) {
        if (t1->month == t2->month) {
            if (t1->day == t2->day) {
                return t1->hours*3600+t1->minutes*60+t1->seconds < 
                       t2->hours*3600+t2->minutes*60+t2->seconds ? 1 : 0;
            } else {
                return t1->day < t2->day ? 1 : 0;
            }
        } else {
            return t1->month < t2->month ? 1 : 0;
        }
    } else {
        return t1->year < t2->year ? 1 : 0;
    }
}

/*----------------------------------------------------------------------
|   SSL_DateTime_Now
+---------------------------------------------------------------------*/
void
SSL_DateTime_Now(SSL_DateTime* now)
{
    NPT_TimeStamp ts;
    NPT_System::GetCurrentTimeStamp(ts);
    NPT_DateTime dt;
    dt.FromTimeStamp(ts);
    now->year    = dt.m_Year;
    now->month   = dt.m_Month;
    now->day     = dt.m_Day;
    now->hours   = dt.m_Hours;
    now->minutes = dt.m_Minutes;
    now->seconds = dt.m_Seconds;
}

/*----------------------------------------------------------------------
|   SSL_GetRandomSeed
+---------------------------------------------------------------------*/
uint64_t
SSL_GetRandomSeed()
{
    NPT_TimeStamp ts;
    NPT_System::GetCurrentTimeStamp(ts);
    return ts.ToNanos();
}

/*----------------------------------------------------------------------
|   SSL_Mutex_Create
+---------------------------------------------------------------------*/
void 
SSL_Mutex_Create(NPT_Mutex** mutex)
{
    *mutex = new NPT_Mutex();
}

/*----------------------------------------------------------------------
|   SSL_Mutex_Destroy
+---------------------------------------------------------------------*/
void 
SSL_Mutex_Destroy(NPT_Mutex* mutex)
{
    delete mutex;
}

/*----------------------------------------------------------------------
|   SSL_Mutex_Lock
+---------------------------------------------------------------------*/
void 
SSL_Mutex_Lock(NPT_Mutex* mutex)
{
    mutex->Lock();
}

/*----------------------------------------------------------------------
|   SSL_Mutex_Lock
+---------------------------------------------------------------------*/
void 
SSL_Mutex_Unlock(NPT_Mutex* mutex)
{
    mutex->Unlock();
}

/*----------------------------------------------------------------------
|   SSL_Sha256_ComputeDigest
+---------------------------------------------------------------------*/
void
SSL_Sha256_ComputeDigest(const unsigned char* data, 
                         unsigned int         data_size,
                         unsigned char*       digest_value)
{
    NPT_Digest* digest = NULL;
    NPT_Digest::Create(NPT_Digest::ALGORITHM_SHA256, digest);
    digest->Update(data, data_size);
    NPT_DataBuffer buffer;
    digest->GetDigest(buffer);
    NPT_CopyMemory(digest_value, buffer.GetData(), 32);
    delete digest;
}

/*----------------------------------------------------------------------
|   NPT_Tls::MatchDnsName
+---------------------------------------------------------------------*/
bool 
NPT_Tls::MatchDnsName(const char* hostname, const char* dns_name)
{
    // NULL or empty names don't match anything
    if (hostname == NULL || *hostname == '\0') return false;
    if (dns_name == NULL || *dns_name == '\0') return false;

    // check for wildcards */
    if (dns_name[0] == '*') {
        // wildcard match, expect '*.' at the start, we don't match '*foo.com'
        if (dns_name[1] != '.') return false;
        
        // skip the first component of the hostname
        while (hostname[0] != '\0' && hostname[0] != '.') {
            ++hostname;
        }
        if (hostname[0] == '.') ++hostname;
        
        // compare the tails
        return NPT_String::Compare(hostname, dns_name+2, true) == 0;
    } else {
        // full match
        return NPT_String::Compare(hostname, dns_name, true) == 0;    
    }
}

/*----------------------------------------------------------------------
|   NPT_Tls::MatchDnsNames
+---------------------------------------------------------------------*/
bool 
NPT_Tls::MatchDnsNames(const char*                 hostname,
                       const NPT_List<NPT_String>& dns_names)
{
    // NULL or empty names don't match anything
    if (hostname == NULL || *hostname == '\0') return false;
    
    // check the dns names
    for (NPT_List<NPT_String>::Iterator i = dns_names.GetFirstItem();
                                        i;
                                      ++i) {
        if (MatchDnsName(hostname, (*i).GetChars())) return true;
    }
    
    // no match
    return false;
}

/*----------------------------------------------------------------------
|   NPT_Tls_MapResult
+---------------------------------------------------------------------*/
static NPT_Result
NPT_Tls_MapResult(int err)
{
    switch (err) {
        case SSL_ERROR_CONN_LOST:           return NPT_ERROR_CONNECTION_ABORTED;
        case SSL_ERROR_TIMEOUT:             return NPT_ERROR_TIMEOUT;
        case SSL_ERROR_EOS:                 return NPT_ERROR_EOS;
        case SSL_ERROR_NOT_SUPPORTED:       return NPT_ERROR_NOT_SUPPORTED;
        case SSL_ERROR_INVALID_HANDSHAKE:   return NPT_ERROR_TLS_INVALID_HANDSHAKE;
        case SSL_ERROR_INVALID_PROT_MSG:    return NPT_ERROR_TLS_INVALID_PROTOCOL_MESSAGE;
        case SSL_ERROR_INVALID_HMAC:        return NPT_ERROR_TLS_INVALID_HMAC;
        case SSL_ERROR_INVALID_VERSION:     return NPT_ERROR_TLS_INVALID_VERSION;
        case SSL_ERROR_INVALID_SESSION:     return NPT_ERROR_TLS_INVALID_SESSION;
        case SSL_ERROR_NO_CIPHER:           return NPT_ERROR_TLS_NO_CIPHER;
        case SSL_ERROR_BAD_CERTIFICATE:     return NPT_ERROR_TLS_BAD_CERTIFICATE;
        case SSL_ERROR_INVALID_KEY:         return NPT_ERROR_TLS_INVALID_KEY;
        case SSL_ERROR_FINISHED_INVALID:    return NPT_ERROR_TLS_INVALID_FINISHED_MESSAGE;
        case SSL_ERROR_NO_CERT_DEFINED:     return NPT_ERROR_TLS_NO_CERTIFICATE_DEFINED;
        case SSL_ERROR_NO_CLIENT_RENOG:     return NPT_ERROR_TLS_NO_CLIENT_RENEGOTIATION;
        case -SSL_ALERT_HANDSHAKE_FAILURE:  return NPT_ERROR_TLS_ALERT_HANDSHAKE_FAILED;
        case -SSL_ALERT_BAD_CERTIFICATE:    return NPT_ERROR_TLS_ALERT_BAD_CERTIFICATE;
        case -SSL_ALERT_INVALID_VERSION:    return NPT_ERROR_TLS_ALERT_INVALID_VERSION;
        case -SSL_ALERT_BAD_RECORD_MAC:     return NPT_ERROR_TLS_ALERT_BAD_RECORD_MAC;
        case -SSL_ALERT_DECODE_ERROR:       return NPT_ERROR_TLS_ALERT_DECODE_ERROR;
        case -SSL_ALERT_DECRYPT_ERROR:      return NPT_ERROR_TLS_ALERT_DECRYPT_ERROR;
        case -SSL_ALERT_ILLEGAL_PARAMETER:  return NPT_ERROR_TLS_ALERT_ILLEGAL_PARAMETER;
        case -SSL_ALERT_UNEXPECTED_MESSAGE: return NPT_ERROR_TLS_ALERT_UNEXPECTED_MESSAGE;
        case SSL_X509_ERROR(X509_NOT_OK):                       return NPT_ERROR_TLS_CERTIFICATE_FAILURE;
        case SSL_X509_ERROR(X509_VFY_ERROR_NO_TRUSTED_CERT):    return NPT_ERROR_TLS_CERTIFICATE_NO_TRUST_ANCHOR;
        case SSL_X509_ERROR(X509_VFY_ERROR_BAD_SIGNATURE):      return NPT_ERROR_TLS_CERTIFICATE_BAD_SIGNATURE;      
        case SSL_X509_ERROR(X509_VFY_ERROR_NOT_YET_VALID):      return NPT_ERROR_TLS_CERTIFICATE_NOT_YET_VALID;
        case SSL_X509_ERROR(X509_VFY_ERROR_EXPIRED):            return NPT_ERROR_TLS_CERTIFICATE_EXPIRED;
        case SSL_X509_ERROR(X509_VFY_ERROR_SELF_SIGNED):        return NPT_ERROR_TLS_CERTIFICATE_SELF_SIGNED;
        case SSL_X509_ERROR(X509_VFY_ERROR_INVALID_CHAIN):      return NPT_ERROR_TLS_CERTIFICATE_INVALID_CHAIN;
        case SSL_X509_ERROR(X509_VFY_ERROR_UNSUPPORTED_DIGEST): return NPT_ERROR_TLS_CERTIFICATE_UNSUPPORTED_DIGEST;
        case SSL_X509_ERROR(X509_INVALID_PRIV_KEY):             return NPT_ERROR_TLS_CERTIFICATE_INVALID_PRIVATE_KEY;
        case 0:                           return NPT_SUCCESS;
        default:                          return NPT_FAILURE;
    }
}

/*----------------------------------------------------------------------
|   NPT_TlsContextImpl
+---------------------------------------------------------------------*/
class NPT_TlsContextImpl {
public:
    NPT_TlsContextImpl(NPT_Flags options) :
        m_SSL_CTX(ssl_ctx_new(((options & NPT_TlsContext::OPTION_VERIFY_LATER)?SSL_SERVER_VERIFY_LATER:0) |
                              ((options & NPT_TlsContext::OPTION_REQUIRE_CLIENT_CERTIFICATE)?SSL_CLIENT_AUTHENTICATION:0),
                              NPT_TLS_CONTEXT_DEFAULT_SESSION_CACHE)) {}
    ~NPT_TlsContextImpl() { ssl_ctx_free(m_SSL_CTX); }
    
    NPT_Result LoadKey(NPT_TlsKeyFormat     key_format, 
                       const unsigned char* key_data,
                       NPT_Size             key_data_size,
                       const char*          password);
    NPT_Result SelfSignCertificate(const char* common_name,
                                   const char* organization,
                                   const char* organizational_name);                               
    NPT_Result AddTrustAnchor(const unsigned char* ta_data,
                              NPT_Size             ta_data_size);
    
    SSL_CTX* m_SSL_CTX;
};

/*----------------------------------------------------------------------
|   NPT_TlsContextImpl::LoadKey
+---------------------------------------------------------------------*/
NPT_Result 
NPT_TlsContextImpl::LoadKey(NPT_TlsKeyFormat     key_format, 
                            const unsigned char* key_data,
                            NPT_Size             key_data_size,
                            const char*          password)
{
    int object_type;
    switch (key_format) {
        case NPT_TLS_KEY_FORMAT_RSA_PRIVATE: object_type = SSL_OBJ_RSA_KEY; break;
        case NPT_TLS_KEY_FORMAT_PKCS8:       object_type = SSL_OBJ_PKCS8;   break;
        case NPT_TLS_KEY_FORMAT_PKCS12:      object_type = SSL_OBJ_PKCS12;  break;
        default: return NPT_ERROR_INVALID_PARAMETERS;
    }
    
    int result = ssl_obj_memory_load(m_SSL_CTX, object_type, key_data, key_data_size, password);
    return NPT_Tls_MapResult(result);
}

/*----------------------------------------------------------------------
|   NPT_TlsContextImpl::SelfSignCertificate
+---------------------------------------------------------------------*/
NPT_Result 
NPT_TlsContextImpl::SelfSignCertificate(const char* common_name,
                                        const char* organization,
                                        const char* organizational_name)
{
    const char* dn[3] = {common_name, organization, organizational_name};
    uint8_t* certificate = NULL;
    int result = ssl_x509_create(m_SSL_CTX, 0, dn, &certificate);
    if (result <= 0) {
        return NPT_Tls_MapResult(result);
    }
    result = ssl_obj_memory_load(m_SSL_CTX, SSL_OBJ_X509_CERT, certificate, result, NULL);
    ssl_mem_free(certificate);
    
    return NPT_Tls_MapResult(result);
}

/*----------------------------------------------------------------------
|   NPT_TlsContextImpl::AddTrustAnchor
+---------------------------------------------------------------------*/
NPT_Result 
NPT_TlsContextImpl::AddTrustAnchor(const unsigned char* ta_data,
                                   NPT_Size             ta_data_size)
{
    int result = ssl_obj_memory_load(m_SSL_CTX, SSL_OBJ_X509_CACERT, ta_data, ta_data_size, NULL);
    return NPT_Tls_MapResult(result);
}

/*----------------------------------------------------------------------
|   NPT_TlsStreamAdapter
+---------------------------------------------------------------------*/
struct NPT_TlsStreamAdapter
{
    static int Read(SSL_SOCKET* _self, void* buffer, unsigned int size) {
        NPT_TlsStreamAdapter* self = (NPT_TlsStreamAdapter*)_self;
        NPT_Size bytes_read = 0;
        NPT_Result result = self->m_Input->Read(buffer, size, &bytes_read);
        if (NPT_FAILED(result)) {
            switch (result) {
                case NPT_ERROR_EOS:     return SSL_ERROR_EOS;
                case NPT_ERROR_TIMEOUT: return SSL_ERROR_TIMEOUT;
                default:                return SSL_ERROR_CONN_LOST;
            }
        }
        return bytes_read;
    }

    static int Write(SSL_SOCKET* _self, const void* buffer, unsigned int size) {
        NPT_TlsStreamAdapter* self = (NPT_TlsStreamAdapter*)_self;
        NPT_Size bytes_written = 0;
        NPT_Result result = self->m_Output->Write(buffer, size, &bytes_written);
        if (NPT_FAILED(result)) {
            switch (result) {
                case NPT_ERROR_EOS:     return SSL_ERROR_EOS;
                case NPT_ERROR_TIMEOUT: return SSL_ERROR_TIMEOUT;
                default:                return SSL_ERROR_CONN_LOST;
            }
        }
        return bytes_written;
    }
    
    NPT_TlsStreamAdapter(NPT_InputStreamReference  input, 
                         NPT_OutputStreamReference output) :
        m_Input(input), m_Output(output) {
        m_Base.Read  = Read;
        m_Base.Write = Write;
    }
    
    SSL_SOCKET                m_Base;
    NPT_InputStreamReference  m_Input;
    NPT_OutputStreamReference m_Output;
};


/*----------------------------------------------------------------------
|   NPT_TlsSessionImpl
+---------------------------------------------------------------------*/
class NPT_TlsSessionImpl {
public:
    NPT_TlsSessionImpl(SSL_CTX*                   context,
                       NPT_InputStreamReference&  input,
                       NPT_OutputStreamReference& output) :
        m_SSL_CTX(context),
        m_SSL(NULL),
        m_StreamAdapter(input, output) {}
    virtual ~NPT_TlsSessionImpl() { ssl_free(m_SSL); }
    
    // methods
    virtual NPT_Result Handshake() = 0;
    virtual NPT_Result GetHandshakeStatus();
    virtual NPT_Result VerifyPeerCertificate();
    virtual NPT_Result VerifyDnsNameMatch(const char* hostname);
    virtual NPT_Result GetSessionId(NPT_DataBuffer& session_id);
    virtual NPT_UInt32 GetCipherSuiteId();
    virtual NPT_Result GetPeerCertificateInfo(NPT_TlsCertificateInfo& cert_info,
                                              NPT_Ordinal             position);
    
    // members
    SSL_CTX*             m_SSL_CTX;
    SSL*                 m_SSL;
    NPT_TlsStreamAdapter m_StreamAdapter;
};

/*----------------------------------------------------------------------
|   NPT_TlsSessionImpl::GetHandshakeStatus
+---------------------------------------------------------------------*/
NPT_Result
NPT_TlsSessionImpl::GetHandshakeStatus()
{
    int status;
    if (m_SSL == NULL || (status = ssl_handshake_status(m_SSL)) == SSL_NOT_OK) {
        // no handshake done
        return NPT_ERROR_INVALID_STATE;
    }

    return NPT_Tls_MapResult(status);
}

/*----------------------------------------------------------------------
|   NPT_TlsSessionImpl::VerifyPeerCertificate
+---------------------------------------------------------------------*/
NPT_Result
NPT_TlsSessionImpl::VerifyPeerCertificate()
{
    if (m_SSL == NULL || ssl_handshake_status(m_SSL) == SSL_NOT_OK) {
        // no handshake done
        return NPT_ERROR_INVALID_STATE;
    }

    int result = ssl_verify_cert(m_SSL);
    return NPT_Tls_MapResult(result);
}

/*----------------------------------------------------------------------
|   NPT_TlsSessionImpl::VerifyDnsNameMatch
+---------------------------------------------------------------------*/
NPT_Result
NPT_TlsSessionImpl::VerifyDnsNameMatch(const char* hostname)
{
    if (hostname == NULL) return NPT_ERROR_INVALID_PARAMETERS;
    if (m_SSL == NULL || ssl_handshake_status(m_SSL) == SSL_NOT_OK) {
        // no handshake done
        return NPT_ERROR_INVALID_STATE;
    }

    // get the peer certificate
    const SSL_X509_CERT* cert = ssl_get_peer_cert(m_SSL, 0);
    if (cert == NULL) return NPT_ERROR_NO_SUCH_ITEM;
    
    // try the common name 
    const char* common_name = ssl_cert_get_dn(cert, SSL_X509_CERT_COMMON_NAME);
    if (common_name && NPT_Tls::MatchDnsName(hostname, common_name)) return NPT_SUCCESS;
    
    // try all the alt DNS names
    const char* alt_name = NULL;
    for (unsigned int i=0; (alt_name=ssl_cert_get_subject_alt_dnsname(cert, i)); i++) {
        if (NPT_Tls::MatchDnsName(hostname, alt_name)) return NPT_SUCCESS;
    }
    
    return NPT_FAILURE;
}

/*----------------------------------------------------------------------
|   NPT_TlsSessionImpl::GetSessionId
+---------------------------------------------------------------------*/
NPT_Result
NPT_TlsSessionImpl::GetSessionId(NPT_DataBuffer& session_id)
{
    if (m_SSL == NULL || ssl_handshake_status(m_SSL) == SSL_NOT_OK) {
        // no handshake done
        session_id.SetDataSize(0);
        return NPT_ERROR_INVALID_STATE;
    }
    
    // return the session id
    session_id.SetData(ssl_get_session_id(m_SSL),
                       ssl_get_session_id_size(m_SSL));
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_TlsSessionImpl::GetCipherSuiteId
+---------------------------------------------------------------------*/
NPT_UInt32
NPT_TlsSessionImpl::GetCipherSuiteId()
{
    if (m_SSL == NULL || ssl_handshake_status(m_SSL) == SSL_NOT_OK) {
        // no handshake done
        return 0;
    }
    
    return ssl_get_cipher_id(m_SSL);
}

/*----------------------------------------------------------------------
|   NPT_TlsSessionImpl::GetPeerCertificateInfo
+---------------------------------------------------------------------*/
NPT_Result
NPT_TlsSessionImpl::GetPeerCertificateInfo(NPT_TlsCertificateInfo& cert_info,
                                           NPT_Ordinal             position)
{
    if (m_SSL == NULL || ssl_handshake_status(m_SSL) == SSL_NOT_OK) {
        // no handshake done
        return NPT_ERROR_INVALID_STATE;
    }

    // find the certificate at the requested index
    const SSL_X509_CERT* cert = ssl_get_peer_cert(m_SSL, position);
    if (cert == NULL) return NPT_ERROR_NO_SUCH_ITEM;
    
    // get the certificate fields 
    cert_info.subject.common_name         = ssl_cert_get_dn(cert, SSL_X509_CERT_COMMON_NAME);
    cert_info.subject.organization        = ssl_cert_get_dn(cert, SSL_X509_CERT_ORGANIZATION);
    cert_info.subject.organizational_name = ssl_cert_get_dn(cert, SSL_X509_CERT_ORGANIZATIONAL_NAME);
    cert_info.issuer.common_name          = ssl_cert_get_dn(cert, SSL_X509_CA_CERT_COMMON_NAME);
    cert_info.issuer.organization         = ssl_cert_get_dn(cert, SSL_X509_CA_CERT_ORGANIZATION);
    cert_info.issuer.organizational_name  = ssl_cert_get_dn(cert, SSL_X509_CA_CERT_ORGANIZATIONAL_NAME);
    
    ssl_cert_get_fingerprints(cert, cert_info.fingerprint.md5, cert_info.fingerprint.sha1);
    SSL_DateTime not_before, not_after;
    ssl_cert_get_validity_dates(cert, &not_before, &not_after);
    cert_info.issue_date.m_Year        = not_before.year;
    cert_info.issue_date.m_Month       = not_before.month;
    cert_info.issue_date.m_Day         = not_before.day;
    cert_info.issue_date.m_Hours       = not_before.hours;
    cert_info.issue_date.m_Minutes     = not_before.minutes;
    cert_info.issue_date.m_Seconds     = not_before.seconds;
    cert_info.issue_date.m_NanoSeconds = 0;
    cert_info.issue_date.m_TimeZone    = 0;
    cert_info.expiration_date.m_Year        = not_after.year;
    cert_info.expiration_date.m_Month       = not_after.month;
    cert_info.expiration_date.m_Day         = not_after.day;
    cert_info.expiration_date.m_Hours       = not_after.hours;
    cert_info.expiration_date.m_Minutes     = not_after.minutes;
    cert_info.expiration_date.m_Seconds     = not_after.seconds;
    cert_info.expiration_date.m_NanoSeconds = 0;
    cert_info.expiration_date.m_TimeZone    = 0;

    // alternate names
    cert_info.alternate_names.Clear();
    const char* alt_name = NULL;
    for (unsigned int i=0; (alt_name=ssl_cert_get_subject_alt_dnsname(cert, i)); i++) {
        cert_info.alternate_names.Add(NPT_String(alt_name));
    }
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_TlsClientSessionImpl
+---------------------------------------------------------------------*/
class NPT_TlsClientSessionImpl : public NPT_TlsSessionImpl {
public:
    NPT_TlsClientSessionImpl(SSL_CTX*                   context,
                             NPT_InputStreamReference&  input,
                             NPT_OutputStreamReference& output) :
        NPT_TlsSessionImpl(context, input, output) {}

    // methods
    virtual NPT_Result Handshake();
};

/*----------------------------------------------------------------------
|   NPT_TlsClientSessionImpl::Handshake
+---------------------------------------------------------------------*/
NPT_Result
NPT_TlsClientSessionImpl::Handshake()
{
    if (m_SSL == NULL) {
        // we have not created the client object yet
        m_SSL = ssl_client_new(m_SSL_CTX, &m_StreamAdapter.m_Base, NULL, 0);
    }
    
    int result = ssl_handshake_status(m_SSL);
    return NPT_Tls_MapResult(result);
}

/*----------------------------------------------------------------------
|   NPT_TlsServerSessionImpl
+---------------------------------------------------------------------*/
class NPT_TlsServerSessionImpl : public NPT_TlsSessionImpl {
public:
    NPT_TlsServerSessionImpl(SSL_CTX*                   context,
                             NPT_InputStreamReference&  input,
                             NPT_OutputStreamReference& output) :
        NPT_TlsSessionImpl(context, input, output) {}

    // methods
    virtual NPT_Result Handshake();
};

/*----------------------------------------------------------------------
|   NPT_TlsServerSessionImpl::Handshake
+---------------------------------------------------------------------*/
NPT_Result
NPT_TlsServerSessionImpl::Handshake()
{
    if (m_SSL == NULL) {
        // we have not created the server object yet 
        m_SSL = ssl_server_new(m_SSL_CTX, &m_StreamAdapter.m_Base);
    }
    
    uint8_t* data = NULL;
    int result;
    while ((result = ssl_handshake_status(m_SSL)) == SSL_NOT_OK) {
        result = ssl_read(m_SSL, &data);
        if (result != SSL_OK) break;
        if (data != NULL) {
            NPT_LOG_WARNING("got data during handshake???");
            return NPT_ERROR_INTERNAL;
        }
    }
    return NPT_Tls_MapResult(result);
}

/*----------------------------------------------------------------------
|   NPT_TlsInputStream
+---------------------------------------------------------------------*/
class NPT_TlsInputStream : public NPT_InputStream {
public:
    NPT_TlsInputStream(NPT_TlsSessionImplReference& session) :
        m_Session(session),
        m_Position(0),
        m_RecordCacheData(NULL),
        m_RecordCacheSize(0) {}
    
    // NPT_InputStream methods
    virtual NPT_Result Read(void*     buffer, 
                            NPT_Size  bytes_to_read, 
                            NPT_Size* bytes_read = NULL);
    virtual NPT_Result Seek(NPT_Position)           { return NPT_ERROR_NOT_SUPPORTED; }
    virtual NPT_Result Tell(NPT_Position& offset)   { offset = m_Position; return NPT_SUCCESS; }
    virtual NPT_Result GetSize(NPT_LargeSize& size) { size=0; return NPT_ERROR_NOT_SUPPORTED; }
    virtual NPT_Result GetAvailable(NPT_LargeSize& available);

private:
    NPT_TlsSessionImplReference m_Session;
    NPT_Position                m_Position;
    uint8_t*                    m_RecordCacheData;
    NPT_Size                    m_RecordCacheSize;
};

/*----------------------------------------------------------------------
|   NPT_TlsInputStream::Read
+---------------------------------------------------------------------*/
NPT_Result 
NPT_TlsInputStream::Read(void*     buffer, 
                         NPT_Size  bytes_to_read, 
                         NPT_Size* bytes_read)
{
    // setup default values
    if (bytes_read) *bytes_read = 0;
    
    // quick check for edge case
    if (bytes_to_read == 0) return NPT_SUCCESS;
    
    // read a new record if we don't have one cached
    if (m_RecordCacheData == NULL) {
        int ssl_result;
        do {
            ssl_result = ssl_read(m_Session->m_SSL, &m_RecordCacheData);
        } while (ssl_result == 0);
        if (ssl_result < 0) {
            return NPT_Tls_MapResult(ssl_result);
        } 
        m_RecordCacheSize = ssl_result;
    }
    
    // we now have data in cache
    if (bytes_to_read > m_RecordCacheSize) {
        // read at most what's in the cache
        bytes_to_read = m_RecordCacheSize;
    }
    NPT_CopyMemory(buffer, m_RecordCacheData, bytes_to_read);
    if (bytes_read) *bytes_read = bytes_to_read;

    // update the record cache
    m_RecordCacheSize -= bytes_to_read;
    if (m_RecordCacheSize == 0) {
        // nothing left in the cache
        m_RecordCacheData = NULL;
    } else {
        // move the cache pointer
        m_RecordCacheData += bytes_to_read;
    }
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_TlsInputStream::GetAvailable
+---------------------------------------------------------------------*/
NPT_Result 
NPT_TlsInputStream::GetAvailable(NPT_LargeSize& /*available*/)
{
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_TlsOutputStream
+---------------------------------------------------------------------*/
class NPT_TlsOutputStream : public NPT_OutputStream {
public:
    NPT_TlsOutputStream(NPT_TlsSessionImplReference& session) :
        m_Session(session),
        m_Position(0) {}
    
    // NPT_OutputStream methods
    virtual NPT_Result Write(const void* buffer, 
                             NPT_Size    bytes_to_write, 
                             NPT_Size*   bytes_written = NULL);
    virtual NPT_Result Seek(NPT_Position) { return NPT_ERROR_NOT_SUPPORTED; }
    virtual NPT_Result Tell(NPT_Position& offset) { offset = m_Position; return NPT_SUCCESS; }

private:
    NPT_TlsSessionImplReference m_Session;
    NPT_Position                m_Position;
};

/*----------------------------------------------------------------------
|   NPT_TlsOutputStream::Write
+---------------------------------------------------------------------*/
NPT_Result 
NPT_TlsOutputStream::Write(const void* buffer, 
                           NPT_Size    bytes_to_write, 
                           NPT_Size*   bytes_written)
{
    // setup default values
    if (bytes_written) *bytes_written = 0;
    
    // quick check for edge case 
    if (bytes_to_write == 0) return NPT_SUCCESS;
    
    // write some data
    int ssl_result;
    do {
        ssl_result = ssl_write(m_Session->m_SSL, (const uint8_t*)buffer, bytes_to_write);
    } while (ssl_result == 0);
    if (ssl_result < 0) {
        return NPT_Tls_MapResult(ssl_result);
    }
    m_Position += ssl_result;
    if (bytes_written) *bytes_written = (NPT_Size)ssl_result;
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Tls::GetDefaultTrustAnchors
+---------------------------------------------------------------------*/
const NPT_TlsTrustAnchorData*
NPT_Tls::GetDefaultTrustAnchors(NPT_Ordinal indx)
{
    if (indx == 0) {
        return NptTlsDefaultTrustAnchorsBase;
    } else if (indx == 2) {
        return NptTlsDefaultTrustAnchorsExtended;
    } else {
        return NULL;
    }
}

/*----------------------------------------------------------------------
|   NPT_TlsContext::NPT_TlsContext
+---------------------------------------------------------------------*/
NPT_TlsContext::NPT_TlsContext(NPT_Flags options) :
    m_Impl(new NPT_TlsContextImpl(options))
{
    if (options & OPTION_ADD_DEFAULT_TRUST_ANCHORS) {
        const NPT_TlsTrustAnchorData* ta = NPT_Tls::GetDefaultTrustAnchors(0);
        if (ta) {
            AddTrustAnchors(ta);
        }
    }
}

/*----------------------------------------------------------------------
|   NPT_TlsContext::~NPT_TlsContext
+---------------------------------------------------------------------*/
NPT_TlsContext::~NPT_TlsContext()
{
    delete m_Impl;
}

/*----------------------------------------------------------------------
|   NPT_TlsContext::LoadKey
+---------------------------------------------------------------------*/
NPT_Result 
NPT_TlsContext::LoadKey(NPT_TlsKeyFormat     key_format, 
                        const unsigned char* key_data,
                        NPT_Size             key_data_size,
                        const char*          password)
{
    return m_Impl->LoadKey(key_format, key_data, key_data_size, password);
}

/*----------------------------------------------------------------------
|   NPT_TlsContext::SelfSignCertificate
+---------------------------------------------------------------------*/
NPT_Result 
NPT_TlsContext::SelfSignCertificate(const char* common_name,
                                    const char* organization,
                                    const char* organizational_name)
{
    return m_Impl->SelfSignCertificate(common_name, organization, organizational_name);
}

/*----------------------------------------------------------------------
|   NPT_TlsContext::AddTrustAnchor
+---------------------------------------------------------------------*/
NPT_Result 
NPT_TlsContext::AddTrustAnchor(const unsigned char* ta_data,
                               NPT_Size             ta_data_size)
{
    return m_Impl->AddTrustAnchor(ta_data, ta_data_size);
}

/*----------------------------------------------------------------------
|   NPT_TlsContext::AddTrustAnchors
+---------------------------------------------------------------------*/
NPT_Result 
NPT_TlsContext::AddTrustAnchors(const NPT_TlsTrustAnchorData* anchors,
                                NPT_Cardinal                  anchor_count)
{
    if (anchors == NULL) return NPT_SUCCESS;
    for (unsigned int i=0; 
         anchor_count     ? 
         (i<anchor_count) :
         (anchors[i].cert_data && anchors[i].cert_size); 
         i++) {
         // add the trust anchor and ignore the error 
         m_Impl->AddTrustAnchor(anchors[i].cert_data, anchors[i].cert_size);
    }
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_TlsSession::NPT_TlsSession
+---------------------------------------------------------------------*/
NPT_TlsSession::NPT_TlsSession(NPT_TlsContext&     context, 
                               NPT_TlsSessionImpl* impl) :
    m_Context(context),
    m_Impl(impl),
    m_InputStream(new NPT_TlsInputStream(m_Impl)),
    m_OutputStream(new NPT_TlsOutputStream(m_Impl))
{
}

/*----------------------------------------------------------------------
|   NPT_TlsSession::~NPT_TlsSession
+---------------------------------------------------------------------*/
NPT_TlsSession::~NPT_TlsSession()
{
}

/*----------------------------------------------------------------------
|   NPT_TlsSession::Handshake
+---------------------------------------------------------------------*/
NPT_Result 
NPT_TlsSession::Handshake()
{
    return m_Impl->Handshake();
}

/*----------------------------------------------------------------------
|   NPT_TlsSession::GetHandshakeStatus
+---------------------------------------------------------------------*/
NPT_Result 
NPT_TlsSession::GetHandshakeStatus()
{
    return m_Impl->GetHandshakeStatus();
}

/*----------------------------------------------------------------------
|   NPT_TlsSession::VerifyPeerCertificate
+---------------------------------------------------------------------*/
NPT_Result 
NPT_TlsSession::VerifyPeerCertificate()
{
    return m_Impl->VerifyPeerCertificate();
}

/*----------------------------------------------------------------------
|   NPT_TlsSession::VerifyDnsNameMatch
+---------------------------------------------------------------------*/
NPT_Result
NPT_TlsSession::VerifyDnsNameMatch(const char* hostname)
{
    return m_Impl->VerifyDnsNameMatch(hostname);
}

/*----------------------------------------------------------------------
|   NPT_TlsClientSession::GetSessionId
+---------------------------------------------------------------------*/
NPT_Result 
NPT_TlsSession::GetSessionId(NPT_DataBuffer& session_id)
{
    return m_Impl->GetSessionId(session_id);
}

/*----------------------------------------------------------------------
|   NPT_TlsSession::GetCipherSuiteId
+---------------------------------------------------------------------*/
NPT_UInt32
NPT_TlsSession::GetCipherSuiteId()
{
    return m_Impl->GetCipherSuiteId();
}
              
/*----------------------------------------------------------------------
|   NPT_TlsSession::GetPeerCertificateInfo
+---------------------------------------------------------------------*/
NPT_Result
NPT_TlsSession::GetPeerCertificateInfo(NPT_TlsCertificateInfo& cert_info,
                                       unsigned int            position)
{
    return m_Impl->GetPeerCertificateInfo(cert_info, position);
}

/*----------------------------------------------------------------------
|   NPT_TlsSession::GetInputStream
+---------------------------------------------------------------------*/
NPT_Result 
NPT_TlsSession::GetInputStream(NPT_InputStreamReference& stream)
{
    stream = m_InputStream;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_TlsSession::GetOutputStream
+---------------------------------------------------------------------*/
NPT_Result 
NPT_TlsSession::GetOutputStream(NPT_OutputStreamReference& stream)
{
    stream = m_OutputStream;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_TlsClientSession::NPT_TlsClientSession
+---------------------------------------------------------------------*/
NPT_TlsClientSession::NPT_TlsClientSession(NPT_TlsContext&            context,
                                           NPT_InputStreamReference&  input,
                                           NPT_OutputStreamReference& output) :
    NPT_TlsSession(context, new NPT_TlsClientSessionImpl(context.m_Impl->m_SSL_CTX, input, output))
{
}

/*----------------------------------------------------------------------
|   NPT_TlsServerSession::NPT_TlsServerSession
+---------------------------------------------------------------------*/
NPT_TlsServerSession::NPT_TlsServerSession(NPT_TlsContext&            context,
                                           NPT_InputStreamReference&  input,
                                           NPT_OutputStreamReference& output) :
    NPT_TlsSession(context, new NPT_TlsServerSessionImpl(context.m_Impl->m_SSL_CTX, input, output))
{
}

/*----------------------------------------------------------------------
|   NPT_HttpTlsConnector::DefaultTlsContext
+---------------------------------------------------------------------*/
NPT_HttpTlsConnector::Cleanup NPT_HttpTlsConnector::Cleanup::AutomaticCleaner;
NPT_TlsContext* NPT_HttpTlsConnector::DefaultTlsContext = NULL;

/*----------------------------------------------------------------------
|   NPT_HttpTlsConnector::NPT_HttpTlsConnector
+---------------------------------------------------------------------*/
NPT_HttpTlsConnector::NPT_HttpTlsConnector(NPT_Flags options) :
    m_TlsContext(GetDefaultTlsContext()),
    m_Options(options)
{
}

/*----------------------------------------------------------------------
|   NPT_HttpTlsConnector::NPT_HttpTlsConnector
+---------------------------------------------------------------------*/
NPT_HttpTlsConnector::NPT_HttpTlsConnector(NPT_TlsContext& tls_context, NPT_Flags options) :
    m_TlsContext(tls_context),
    m_Options(options)
{
}

/*----------------------------------------------------------------------
|   NPT_HttpTlsConnector::GetDefaultTlsContext
+---------------------------------------------------------------------*/
NPT_TlsContext&
NPT_HttpTlsConnector::GetDefaultTlsContext()
{
    if (DefaultTlsContext == NULL) {
        NPT_SingletonLock::GetInstance().Lock();
        if (DefaultTlsContext == NULL) {
            DefaultTlsContext = new NPT_TlsContext(NPT_TlsContext::OPTION_VERIFY_LATER | 
                                                   NPT_TlsContext::OPTION_ADD_DEFAULT_TRUST_ANCHORS);
        }
        NPT_SingletonLock::GetInstance().Unlock();
    }
    
    return *DefaultTlsContext;
}

/*----------------------------------------------------------------------
|   NPT_HttpTlsConnector::VerifyPeer
+---------------------------------------------------------------------*/
NPT_Result
NPT_HttpTlsConnector::VerifyPeer(NPT_TlsClientSession& session, const char* hostname) 
{
    // verify the certificate
    NPT_Result result = session.VerifyPeerCertificate();
    if (NPT_FAILED(result)) {
        if (result == NPT_ERROR_TLS_CERTIFICATE_SELF_SIGNED) {
            if (!m_Options && OPTION_ACCEPT_SELF_SIGNED_CERTS) {
                // self-signed certs are not acceptable
                NPT_LOG_FINE("accepting self-signed certificate");
                return result;
            }
        }
        NPT_LOG_WARNING_2("TLS certificate verification failed (%d:%s)", result, NPT_ResultText(result));
        return result;
    }

    // chech the DNS name
    if (!(m_Options & OPTION_ACCEPT_HOSTNAME_MISMATCH)) {
        // check the hostname
        result = session.VerifyDnsNameMatch(hostname);
        if (NPT_FAILED(result)) {
            NPT_LOG_WARNING_2("TLS certificate does not match DNS name (%d:%s)", result, NPT_ResultText(result));
            return NPT_ERROR_TLS_DNS_NAME_MISMATCH;
        }
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_HttpSimpleTlsConnection
+---------------------------------------------------------------------*/
class NPT_HttpSimpleTlsConnection : public NPT_HttpClient::Connection
{
public:
    virtual NPT_InputStreamReference&  GetInputStream() {
        return m_InputStream;
    }
    virtual NPT_OutputStreamReference& GetOutputStream() {
        return m_OutputStream;
    }
    
    // members
    NPT_InputStreamReference  m_InputStream;
    NPT_OutputStreamReference m_OutputStream;
};

/*----------------------------------------------------------------------
|   NPT_HttpTlsConnector::Connect
+---------------------------------------------------------------------*/
NPT_Result
NPT_HttpTlsConnector::Connect(const NPT_HttpUrl&           url,
                              NPT_HttpClient&              client,
                              const NPT_HttpProxyAddress*  proxy,
                              bool                         reuse,
                              NPT_HttpClient::Connection*& connection)
{
    // default values
    connection = NULL;
    
    // local reference holders
    NPT_InputStreamReference  input_stream;
    NPT_OutputStreamReference output_stream;
    
    // decide which server we need to connect to
    const char* peer_hostname = (const char*)url.GetHost();
    const char* server_hostname;
    NPT_UInt16  server_port;
    if (proxy) {
        // the proxy is set
        server_hostname = (const char*)proxy->GetHostName();
        server_port     = proxy->GetPort();
    } else {
        // no proxy: connect directly
        server_hostname = peer_hostname;
        server_port     = url.GetPort();
    }
    
    // resolve the server address
    NPT_IpAddress ip_address;
    NPT_CHECK_FINE(ip_address.ResolveName(server_hostname, client.GetConfig().m_NameResolverTimeout));

    // check if we can reuse a connection
    // TODO: with this we don't yet support reusing a connection to a proxy
    NPT_SocketAddress socket_address(ip_address, server_port);
    NPT_HttpConnectionManager* connection_manager = NPT_HttpConnectionManager::GetInstance();
    if (!proxy && reuse) {
        NPT_LOG_FINE("looking for a connection to reuse");
        connection = connection_manager->FindConnection(socket_address);
        if (connection) {
            NPT_LOG_FINE("reusing connection");
            return NPT_SUCCESS;
        }
    }
    
    // connect to the server
    NPT_LOG_FINE_2("TLS connector will connect to %s:%d", server_hostname, server_port);
    NPT_TcpClientSocket* tcp_socket = new NPT_TcpClientSocket();
    NPT_SocketReference socket(tcp_socket);
    tcp_socket->SetReadTimeout(client.GetConfig().m_IoTimeout);
    tcp_socket->SetWriteTimeout(client.GetConfig().m_IoTimeout);
    NPT_CHECK_FINE(tcp_socket->Connect(socket_address, client.GetConfig().m_ConnectionTimeout));

    // get the streams
    NPT_InputStreamReference  raw_input;
    NPT_OutputStreamReference raw_output;
    NPT_CHECK_FINE(tcp_socket->GetInputStream(raw_input));
    NPT_CHECK_FINE(tcp_socket->GetOutputStream(raw_output));
    
    if (url.GetSchemeId() == NPT_Url::SCHEME_ID_HTTPS) {
        if (proxy) {
            // RFC 2817 CONNECT
            NPT_String connect_host = url.GetHost() + ":" + NPT_String::FromInteger(url.GetPort());
            NPT_String connect = "CONNECT " + connect_host + " HTTP/1.1\r\n"
                                 "Host: " + connect_host + "\r\n\r\n";
            NPT_Result result = raw_output->WriteFully(connect.GetChars(), connect.GetLength());
            if (NPT_FAILED(result)) return result;
            NPT_String connect_response;
            connect_response.Reserve(1024);
            bool connect_ok = false;
            for (unsigned int x=0; x<NPT_HTTP_TLS_CONNECTOR_MAX_PROXY_RESPONSE_SIZE; x++) {
                connect_response.Reserve(x+1);
                result = raw_input->Read(connect_response.UseChars()+x, 1);
                if (NPT_FAILED(result)) return result;
                if (connect_response.GetChars()[x] == '\n') {
                    connect_response.SetLength(x+1);
                    if (!connect_ok) {
                        // check the connection result
                        NPT_LOG_FINE_1("proxy response: %s", connect_response.GetChars());
                        if (connect_response.GetLength() < 12) {
                            return NPT_ERROR_HTTP_INVALID_RESPONSE_LINE;
                        }
                        if (!connect_response.StartsWith("HTTP/1.")) {
                            return NPT_ERROR_HTTP_INVALID_RESPONSE_LINE;
                        }
                        if (connect_response[8] != ' ') {
                            return NPT_ERROR_HTTP_INVALID_RESPONSE_LINE;
                        }
                        NPT_String status_code = connect_response.SubString(9, 3);
                        if (status_code != "200") {
                            NPT_LOG_WARNING_1("proxy response is not 200 (%s)", status_code.GetChars());
                            return NPT_ERROR_HTTP_INVALID_RESPONSE_LINE;
                        }
                        connect_ok = true;
                    } else {
                        if (connect_response.EndsWith("\r\n\r\n")) {
                            // this is the end, my friend
                            break;
                        }
                    }
                }
            }
            if (!connect_ok) {
                return NPT_ERROR_HTTP_INVALID_RESPONSE_LINE;
            }
        }
        
        // setup the TLS connection
        NPT_TlsClientSession tls_session(m_TlsContext, raw_input, raw_output);
        NPT_Result result = tls_session.Handshake();
        if (NPT_FAILED(result)) {
            NPT_LOG_WARNING_2("TLS handshake failed (%d:%s)", result, NPT_ResultText(result));
            return result;
        }
        result = VerifyPeer(tls_session, peer_hostname);
        if (NPT_FAILED(result)) {
            NPT_LOG_WARNING_2("VerifyPeer failed (%d:%s)", result, NPT_ResultText(result));
            return result;
        }
    
        // return the TLS streams
        tls_session.GetInputStream(input_stream);
        tls_session.GetOutputStream(output_stream);
    } else {
        input_stream  = raw_input;
        output_stream = raw_output;
    }

    // create a connection object for the streams
    connection = new NPT_HttpConnectionManager::Connection(*connection_manager,
                                                           socket,
                                                           input_stream,
                                                           output_stream);
    
    return NPT_SUCCESS;
}

#endif // NPT_CONFIG_ENABLE_TLS
