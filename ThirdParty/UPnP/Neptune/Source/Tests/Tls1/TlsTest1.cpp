/*****************************************************************
|
|      TLS Test Program 1
|
|      (c) 2001-2006 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include "Neptune.h"
#include "NptDebug.h"

#include "TlsClientPrivate1.h"
#include "TlsClientPrivate2.h"

#if defined(WIN32) && defined(_DEBUG)
#include <crtdbg.h>
#endif

#define CHECK(x)                                        \
    do {                                                \
      if (!(x)) {                                       \
        fprintf(stderr, "ERROR line %d \n", __LINE__);  \
      }                                                 \
    } while(0)

const char*
GetCipherSuiteName(unsigned int id)
{
    switch (id) {
        case 0: return "NOT SET";
        case NPT_TLS_RSA_WITH_RC4_128_MD5:     return "RSA-WITH-RC4-128-MD5";
        case NPT_TLS_RSA_WITH_RC4_128_SHA:     return "RSA-WITH-RC4-128-SHA";
        case NPT_TLS_RSA_WITH_AES_128_CBC_SHA: return "RSA-WITH-AES-128-CBC-SHA";
        case NPT_TLS_RSA_WITH_AES_256_CBC_SHA: return "RSA-WITH-AES-256-CBC-SHA";
        default: return "UNKNOWN";
    }
}

static const char* EquifaxCA = 
"MIIDIDCCAomgAwIBAgIENd70zzANBgkqhkiG9w0BAQUFADBOMQswCQYDVQQGEwJV\n"
"UzEQMA4GA1UEChMHRXF1aWZheDEtMCsGA1UECxMkRXF1aWZheCBTZWN1cmUgQ2Vy\n"
"dGlmaWNhdGUgQXV0aG9yaXR5MB4XDTk4MDgyMjE2NDE1MVoXDTE4MDgyMjE2NDE1\n"
"MVowTjELMAkGA1UEBhMCVVMxEDAOBgNVBAoTB0VxdWlmYXgxLTArBgNVBAsTJEVx\n"
"dWlmYXggU2VjdXJlIENlcnRpZmljYXRlIEF1dGhvcml0eTCBnzANBgkqhkiG9w0B\n"
"AQEFAAOBjQAwgYkCgYEAwV2xWGcIYu6gmi0fCG2RFGiYCh7+2gRvE4RiIcPRfM6f\n"
"BeC4AfBONOziipUEZKzxa1NfBbPLZ4C/QgKO/t0BCezhABRP/PvwDN1Dulsr4R+A\n"
"cJkVV5MW8Q+XarfCaCMczE1ZMKxRHjuvK9buY0V7xdlfUNLjUA86iOe/FP3gx7kC\n"
"AwEAAaOCAQkwggEFMHAGA1UdHwRpMGcwZaBjoGGkXzBdMQswCQYDVQQGEwJVUzEQ\n"
"MA4GA1UEChMHRXF1aWZheDEtMCsGA1UECxMkRXF1aWZheCBTZWN1cmUgQ2VydGlm\n"
"aWNhdGUgQXV0aG9yaXR5MQ0wCwYDVQQDEwRDUkwxMBoGA1UdEAQTMBGBDzIwMTgw\n"
"ODIyMTY0MTUxWjALBgNVHQ8EBAMCAQYwHwYDVR0jBBgwFoAUSOZo+SvSspXXR9gj\n"
"IBBPM5iQn9QwHQYDVR0OBBYEFEjmaPkr0rKV10fYIyAQTzOYkJ/UMAwGA1UdEwQF\n"
"MAMBAf8wGgYJKoZIhvZ9B0EABA0wCxsFVjMuMGMDAgbAMA0GCSqGSIb3DQEBBQUA\n"
"A4GBAFjOKer89961zgK5F7WF0bnj4JXMJTENAKaSbn+2kmOeUJXRmm/kEd5jhW6Y\n"
"7qj/WsjTVbJmcVfewCHrPSqnI0kBBIZCe/zuf6IWUrVnZ9NA2zsmWLIodz2uFHdh\n"
"1voqZiegDfqnc1zqcPGUIWVEX/r87yloqaKHee9570+sB3c4\n";

static void
PrintCertificateInfo(NPT_TlsCertificateInfo& cert_info)
{
    printf("[6] Fingerprints:\n");
    printf("MD5: %s\n", NPT_HexString(cert_info.fingerprint.md5, sizeof(cert_info.fingerprint.md5), ":").GetChars());
    printf("SHA1: %s\n", NPT_HexString(cert_info.fingerprint.sha1, sizeof(cert_info.fingerprint.sha1), ":").GetChars());
    printf("Subject Certificate:\n");
    printf("  Common Name         = %s\n", cert_info.subject.common_name.GetChars());
    printf("  Organization        = %s\n", cert_info.subject.organization.GetChars());
    printf("  Organizational Name = %s\n", cert_info.subject.organizational_name.GetChars());
    printf("Issuer Certificate:\n");
    printf("  Common Name         = %s\n", cert_info.issuer.common_name.GetChars());
    printf("  Organization        = %s\n", cert_info.issuer.organization.GetChars());
    printf("  Organizational Name = %s\n", cert_info.issuer.organizational_name.GetChars());
    printf("Issue Date:      %d/%d/%d %02d:%02d:%02d\n", cert_info.issue_date.m_Year,
                                                    cert_info.issue_date.m_Month,
                                                    cert_info.issue_date.m_Day,
                                                    cert_info.issue_date.m_Hours,
                                                    cert_info.issue_date.m_Minutes,
                                                    cert_info.issue_date.m_Seconds);
    printf("Expiration Date: %d/%d/%d %02d:%02d:%02d\n", cert_info.expiration_date.m_Year,
                                                    cert_info.expiration_date.m_Month,
                                                    cert_info.expiration_date.m_Day,
                                                    cert_info.expiration_date.m_Hours,
                                                    cert_info.expiration_date.m_Minutes,
                                                    cert_info.expiration_date.m_Seconds);
    for (NPT_List<NPT_String>::Iterator i = cert_info.alternate_names.GetFirstItem();
                                        i;
                                      ++i) {
        printf("DNS Name:        %s\n", (*i).GetChars()); 
    }
    printf("\n");
}

static void
PrintCertificateChain(NPT_TlsSession& session)
{
    NPT_Ordinal position = 0;
    NPT_Result result;
    
    do {
        NPT_TlsCertificateInfo info;
        result = session.GetPeerCertificateInfo(info, position++);
        if (NPT_SUCCEEDED(result)) {
            PrintCertificateInfo(info);
        }
    } while (NPT_SUCCEEDED(result));
}

static void
PrintSessionInfo(NPT_TlsSession& session)
{
    NPT_Result result;
    
    NPT_DataBuffer session_id;
    result = session.GetSessionId(session_id);
    CHECK(result == NPT_SUCCESS);
    //CHECK(session_id.GetDataSize() > 0);
    printf("[5] Session ID: ");
    printf("%s", NPT_HexString(session_id.GetData(), session_id.GetDataSize()).GetChars());
    printf("\n");
    
    PrintCertificateChain(session);
    
    printf("[7] Cipher Type = %d (%s)\n", session.GetCipherSuiteId(), GetCipherSuiteName(session.GetCipherSuiteId()));
}

static int
TestRemoteServer(const char* hostname, unsigned int port, bool verify_cert, NPT_Result expected_cert_verif_result, bool client_key)
{
    printf("[1] Connecting to %s...\n", hostname);
    NPT_Socket* client_socket = new NPT_TcpClientSocket();
    NPT_IpAddress server_ip;
    NPT_Result result = server_ip.ResolveName(hostname);
    if (NPT_FAILED(result)) {
        printf("!ERROR cannot resolve hostname\n");
        return 1;
    }
    NPT_SocketAddress server_addr(server_ip, port);
    result = client_socket->Connect(server_addr);
    printf("[2] Connection result = %d (%s)\n", result, NPT_ResultText(result));
    if (NPT_FAILED(result)) {
        printf("!ERROR cannot connect\n");
        return 1;
    }
    
    NPT_InputStreamReference input;
    NPT_OutputStreamReference output;
    client_socket->GetInputStream(input);
    client_socket->GetOutputStream(output);
    delete client_socket;
    NPT_TlsContext context(NPT_TlsContext::OPTION_VERIFY_LATER);
    
    NPT_DataBuffer ta_data;
    NPT_Base64::Decode(EquifaxCA, NPT_StringLength(EquifaxCA), ta_data);
    result = context.AddTrustAnchor(ta_data.GetData(), ta_data.GetDataSize());
    if (NPT_FAILED(result)) {
        printf("!ERROR: context->AddTrustAnchor() \n");
        return 1;
    }
    result = context.AddTrustAnchors(NPT_Tls::GetDefaultTrustAnchors(0));
    if (NPT_FAILED(result)) {
        printf("!ERROR: context->AddTrustAnchors() \n");
        return 1;
    }

    if (client_key) {
        /* self-signed cert */
        result = context.LoadKey(NPT_TLS_KEY_FORMAT_PKCS8, TestClient_p8_1, TestClient_p8_1_len, "neptune");
        CHECK(result == NPT_SUCCESS);
        result = context.SelfSignCertificate("MyClientCommonName", "MyClientOrganization", "MyClientOrganizationalName");
    }
    
    NPT_TlsClientSession session(context, input, output);
    printf("[3] Performing Handshake\n");
    result = session.Handshake();
    printf("[4] Handshake Result = %d (%s)\n", result, NPT_ResultText(result));
    if (NPT_FAILED(result)) {
        printf("!ERROR handshake failed\n");
        return 1;
    }

    PrintSessionInfo(session);
    if (verify_cert) {
        result = session.VerifyPeerCertificate();
        printf("[9] Certificate Verification Result = %d (%s)\n", result, NPT_ResultText(result));
        if (result != expected_cert_verif_result) {
            printf("!ERROR, cert verification expected %d, got %d\n", expected_cert_verif_result, result);
            return 1;
        }
    }
    
    NPT_InputStreamReference  ssl_input;
    NPT_OutputStreamReference ssl_output;
    session.GetInputStream(ssl_input);
    session.GetOutputStream(ssl_output);
    
    printf("[10] Getting / Document\n");
    ssl_output->WriteString("GET / HTTP/1.0\n\n");
    for (;;) {
        unsigned char buffer[1];
        NPT_Size bytes_read = 0;
        result = ssl_input->Read(&buffer[0], 1, &bytes_read);
        if (NPT_SUCCEEDED(result)) {
            CHECK(bytes_read == 1);
            printf("%c", buffer[0]);
        } else {
            if (result != NPT_ERROR_EOS && result != NPT_ERROR_CONNECTION_ABORTED) {
                printf("!ERROR: Read() returned %d (%s)\n", result, NPT_ResultText(result)); 
            }
            break;
        }
    }
    printf("[9] SUCCESS\n");
    
    return 0;
}

class TlsTestServer : public NPT_Thread
{
    void Run();
    
public:
    TlsTestServer(int mode) : m_Mode(mode) {}
    
    int                m_Mode;
    NPT_SharedVariable m_Ready;
    NPT_SocketInfo     m_SocketInfo;
};

void
TlsTestServer::Run()
{
    printf("@@@ starting TLS server\n");
    NPT_TcpServerSocket socket;
    NPT_SocketAddress address(NPT_IpAddress::Any, 0);
    NPT_Result result = socket.Bind(address);
    if (NPT_FAILED(result)) {
        fprintf(stderr, "@@@ Bind failed (%d)\n", result);
        return;
    }
    result = socket.GetInfo(m_SocketInfo);
    if (NPT_FAILED(result)) {
        fprintf(stderr, "@@@ GetInfo failed (%d)\n", result);
        return;
    }
    socket.Listen(5);
    m_Ready.SetValue(1);
    
    printf("@@@ Waiting for connection\n");
    NPT_Socket* client = NULL;
    socket.WaitForNewClient(client);
    printf("@@@ Client connected\n");
    
    NPT_TlsContext tls_context(m_Mode?(NPT_TlsContext::OPTION_REQUIRE_CLIENT_CERTIFICATE | NPT_TlsContext::OPTION_VERIFY_LATER):0);
    /* self-signed cert */
    result = tls_context.LoadKey(NPT_TLS_KEY_FORMAT_PKCS8, TestClient_p8_1, TestClient_p8_1_len, "neptune");
    CHECK(result == NPT_SUCCESS);
    result = tls_context.SelfSignCertificate("MyServerCommonName", "MyServerOrganization", "MyServerOrganizationalName");
    
    NPT_InputStreamReference  socket_input;
    NPT_OutputStreamReference socket_output;
    client->GetInputStream(socket_input);
    client->GetOutputStream(socket_output);
    NPT_TlsServerSession session(tls_context, socket_input, socket_output);
    delete client;
    
    result = session.Handshake();
    if (m_Mode == 1) {
        /* expect a self-signed client cert */
        result = session.VerifyPeerCertificate();
        printf("@@@ Certificate Verification Result = %d (%s)\n", result, NPT_ResultText(result));
        if (result != NPT_ERROR_TLS_CERTIFICATE_SELF_SIGNED) {
            printf("!ERROR, cert verification expected %d, got %d\n", NPT_ERROR_TLS_CERTIFICATE_SELF_SIGNED, result);
            return;
        }

        PrintCertificateChain(session);
    } else {
        if (NPT_FAILED(result)) {
            fprintf(stderr, "@@@ Handshake failed (%d : %s)\n", result, NPT_ResultText(result));
            return;
        }
    }
    
    NPT_OutputStreamReference tls_output;
    session.GetOutputStream(tls_output);
    tls_output->WriteString("Hello, Client\n");
    
    printf("@@@ TLS server done\n");
    //NPT_System::Sleep(1.0);
}

static void
TestLocalServer()
{
    TlsTestServer* server = new TlsTestServer(0);
    server->Start();
    
    server->m_Ready.WaitUntilEquals(1);
    TestRemoteServer("127.0.0.1", server->m_SocketInfo.local_address.GetPort(), true, NPT_ERROR_TLS_CERTIFICATE_SELF_SIGNED, true);
    server->Wait();
    delete server;

    server = new TlsTestServer(1);
    server->Start();
    
    server->m_Ready.WaitUntilEquals(1);
    TestRemoteServer("127.0.0.1", server->m_SocketInfo.local_address.GetPort(), true, NPT_ERROR_TLS_CERTIFICATE_SELF_SIGNED, true);
    server->Wait();
    delete server;
}

static void 
TestPrivateKeys()
{
    NPT_TlsContext context;
    NPT_Result     result;

    NPT_DataBuffer key_data;
    NPT_Base64::Decode(TestClient_rsa_priv_base64_1, NPT_StringLength(TestClient_rsa_priv_base64_1), key_data);
    
    result = context.LoadKey(NPT_TLS_KEY_FORMAT_RSA_PRIVATE, key_data.GetData(), key_data.GetDataSize(), NULL);
    CHECK(result == NPT_SUCCESS);

    result = context.LoadKey(NPT_TLS_KEY_FORMAT_PKCS8, TestClient_p8_1, TestClient_p8_1_len, NULL);
    CHECK(result != NPT_SUCCESS);
    result = context.LoadKey(NPT_TLS_KEY_FORMAT_PKCS8, TestClient_p8_1, TestClient_p8_1_len, "neptune");
    CHECK(result == NPT_SUCCESS);
}

class TestTlsConnector : public NPT_HttpTlsConnector
{
public:
    virtual NPT_Result VerifyPeer(NPT_TlsClientSession& session,
                                  const char*           hostname) {
        printf("+++ Verifying Peer (hostname=%s)\n", hostname);
        PrintSessionInfo(session);
        return NPT_HttpTlsConnector::VerifyPeer(session, hostname);
    }
};

static void
TestHttpConnector(const char* hostname)
{
    TestTlsConnector connector;
    NPT_HttpClient client(&connector, false);
    NPT_String url_string = "https://";
    url_string += hostname;
    url_string += "/index.html";
    NPT_HttpUrl url(url_string);
    NPT_HttpRequest request(url, NPT_HTTP_METHOD_GET);
    NPT_HttpResponse* response = NULL;
    NPT_Result result = client.SendRequest(request, response);
    CHECK(result == NPT_SUCCESS);
    if (NPT_SUCCEEDED(result)) {
        CHECK(response->GetEntity() != NULL);
        if (response->GetEntity()) {
            printf("+++ HTTP Response: code=%d, type=%s, len=%d\n", 
                   response->GetStatusCode(), 
                   response->GetEntity()->GetContentType().GetChars(),
                   (int)response->GetEntity()->GetContentLength());
        }
    } else {
        printf("!ERROR: SendRequest returns %d (%s)\n", result, NPT_ResultText(result));
    }
    delete response;
}

static void
TestDnsNameMatch()
{
    CHECK(!NPT_Tls::MatchDnsName(NULL, NULL));
    CHECK(!NPT_Tls::MatchDnsName(NULL, ""));
    CHECK(!NPT_Tls::MatchDnsName(NULL, "a"));
    CHECK(!NPT_Tls::MatchDnsName(NULL, "a.com"));
    CHECK(!NPT_Tls::MatchDnsName(NULL, "*"));
    CHECK(!NPT_Tls::MatchDnsName("", NULL));
    CHECK(!NPT_Tls::MatchDnsName("", ""));
    CHECK(!NPT_Tls::MatchDnsName("", "a"));
    CHECK(!NPT_Tls::MatchDnsName("", "a.com"));
    CHECK(!NPT_Tls::MatchDnsName("", "*"));
    CHECK(!NPT_Tls::MatchDnsName("*", "*"));
    CHECK(!NPT_Tls::MatchDnsName("a", "*"));
    CHECK(!NPT_Tls::MatchDnsName("a.com", "*"));
    CHECK(!NPT_Tls::MatchDnsName("a.com", "b.com"));
    CHECK(!NPT_Tls::MatchDnsName("a.com", "*.a.com"));

    CHECK(NPT_Tls::MatchDnsName("a.com", "a.com"));
    CHECK(NPT_Tls::MatchDnsName("b.a.com", "*.a.com"));
    CHECK(NPT_Tls::MatchDnsName("a.com", "A.com"));
    CHECK(NPT_Tls::MatchDnsName("a.com", "a.COM"));
}

int 
main(int argc, char** argv)
{
    /* test dns name matching */
    TestDnsNameMatch();
    
    /* test private keys */
    TestPrivateKeys();
    
    /* test a local connection */
    TestLocalServer();
    
    /* test a connection */
    const char* hostname = argc==2?argv[1]:"zebulon.bok.net";
    TestRemoteServer(hostname, 443, true, NPT_SUCCESS, false);

    /* test using the http connector */
    TestHttpConnector(hostname);
}
