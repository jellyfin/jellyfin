/*
 * Copyright (c) 2007, Cameron Rich
 * 
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without 
 * modification, are permitted provided that the following conditions are met:
 *
 * * Redistributions of source code must retain the above copyright notice, 
 *   this list of conditions and the following disclaimer.
 * * Redistributions in binary form must reproduce the above copyright notice, 
 *   this list of conditions and the following disclaimer in the documentation 
 *   and/or other materials provided with the distribution.
 * * Neither the name of the axTLS project nor the names of its contributors 
 *   may be used to endorse or promote products derived from this software 
 *   without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

/**
 * @mainpage axTLS API
 *
 * @image html axolotl.jpg
 *
 * The axTLS library has features such as:
 * - The TLSv1 SSL client/server protocol
 * - No requirement to use any openssl libraries.
 * - A choice between AES block (128/256 bit) and RC4 (128 bit) stream ciphers.
 * - RSA encryption/decryption with variable sized keys (up to 4096 bits).
 * - Certificate chaining and peer authentication.
 * - Session resumption, session renegotiation.
 * - ASN.1, X.509, PKCS#8, PKCS#12 keys/certificates with DER/PEM encoding.
 * - Highly configurable compile time options.
 * - Portable across many platforms (written in ANSI C), and has language
 * bindings in C, C#, VB.NET, Java, Perl and Lua.
 * - Partial openssl API compatibility (via a wrapper).
 * - A very small footprint (around 50-60kB for the library in 'server-only' 
 *   mode).
 * - No dependencies on sockets - can use serial connections for example.
 * - A very simple API - ~ 20 functions/methods.
 *
 * A list of these functions/methods are described below.
 *
 *  @ref c_api 
 *
 *  @ref bigint_api 
 *
 *  @ref csharp_api 
 *
 *  @ref java_api 
 */
#ifndef HEADER_SSL_H
#define HEADER_SSL_H

#ifdef __cplusplus
extern "C" {
#endif

#include <time.h>
#include "os_port.h"

/* need to predefine before ssl_lib.h gets to it */
#define SSL_SESSION_ID_SIZE                     32

#include "tls1.h"

/* The optional parameters that can be given to the client/server SSL engine */
#define SSL_CLIENT_AUTHENTICATION               0x00010000
#define SSL_SERVER_VERIFY_LATER                 0x00020000
#define SSL_NO_DEFAULT_KEY                      0x00040000
#define SSL_DISPLAY_STATES                      0x00080000
#define SSL_DISPLAY_BYTES                       0x00100000
#define SSL_DISPLAY_CERTS                       0x00200000
#define SSL_DISPLAY_RSA                         0x00400000
#define SSL_CONNECT_IN_PARTS                    0x00800000

/* errors that can be generated */
#define SSL_OK                                  0
#define SSL_NOT_OK                              -1
#define SSL_ERROR_DEAD                          -2
#define SSL_CLOSE_NOTIFY                        -3
#define SSL_ERROR_CONN_LOST                     -256
#define SSL_ERROR_SOCK_SETUP_FAILURE            -258
#define SSL_ERROR_INVALID_HANDSHAKE             -260
#define SSL_ERROR_INVALID_PROT_MSG              -261
#define SSL_ERROR_INVALID_HMAC                  -262
#define SSL_ERROR_INVALID_VERSION               -263
#define SSL_ERROR_INVALID_SESSION               -265
#define SSL_ERROR_NO_CIPHER                     -266
#define SSL_ERROR_BAD_CERTIFICATE               -268
#define SSL_ERROR_INVALID_KEY                   -269
#define SSL_ERROR_FINISHED_INVALID              -271
#define SSL_ERROR_NO_CERT_DEFINED               -272
#define SSL_ERROR_NO_CLIENT_RENOG               -273
#define SSL_ERROR_NOT_SUPPORTED                 -274
#define SSL_ERROR_TIMEOUT                       -275 /* GBG */
#define SSL_ERROR_EOS                           -276 /* GBG */
#define SSL_X509_OFFSET                         -512
#define SSL_X509_ERROR(A)                       (SSL_X509_OFFSET+A)

/* alert types that are recognized */
#define SSL_ALERT_TYPE_WARNING                  1
#define SLL_ALERT_TYPE_FATAL                    2

/* these are all the alerts that are recognized */
#define SSL_ALERT_CLOSE_NOTIFY                  0
#define SSL_ALERT_UNEXPECTED_MESSAGE            10
#define SSL_ALERT_BAD_RECORD_MAC                20
#define SSL_ALERT_HANDSHAKE_FAILURE             40
#define SSL_ALERT_BAD_CERTIFICATE               42
#define SSL_ALERT_ILLEGAL_PARAMETER             47
#define SSL_ALERT_DECODE_ERROR                  50
#define SSL_ALERT_DECRYPT_ERROR                 51
#define SSL_ALERT_INVALID_VERSION               70
#define SSL_ALERT_NO_RENEGOTIATION              100

/* The ciphers that are supported */
#define SSL_AES128_SHA                          0x2f
#define SSL_AES256_SHA                          0x35
#define SSL_RC4_128_SHA                         0x05
#define SSL_RC4_128_MD5                         0x04

/* build mode ids' */
#define SSL_BUILD_SKELETON_MODE                 0x01
#define SSL_BUILD_SERVER_ONLY                   0x02
#define SSL_BUILD_ENABLE_VERIFICATION           0x03
#define SSL_BUILD_ENABLE_CLIENT                 0x04
#define SSL_BUILD_FULL_MODE                     0x05

/* offsets to retrieve configuration information */
#define SSL_BUILD_MODE                          0
#define SSL_MAX_CERT_CFG_OFFSET                 1
#define SSL_MAX_CA_CERT_CFG_OFFSET              2
#define SSL_HAS_PEM                             3

/* default session sizes */
#define SSL_DEFAULT_SVR_SESS                    5
#define SSL_DEFAULT_CLNT_SESS                   1

/* X.509/X.520 distinguished name types */
#define SSL_X509_CERT_COMMON_NAME               0
#define SSL_X509_CERT_ORGANIZATION              1
#define SSL_X509_CERT_ORGANIZATIONAL_NAME       2
#define SSL_X509_CA_CERT_COMMON_NAME            3
#define SSL_X509_CA_CERT_ORGANIZATION           4
#define SSL_X509_CA_CERT_ORGANIZATIONAL_NAME    5

/* SSL object loader types */
#define SSL_OBJ_X509_CERT                       1
#define SSL_OBJ_X509_CACERT                     2
#define SSL_OBJ_RSA_KEY                         3
#define SSL_OBJ_PKCS8                           4
#define SSL_OBJ_PKCS12                          5

/**
 * @defgroup c_api Standard C API
 * @brief The standard interface in C.
 * @{
 */

/**
 * @brief Establish a new client/server context.
 *
 * This function is called before any client/server SSL connections are made. 
 *
 * Each new connection will use the this context's private key and 
 * certificate chain. If a different certificate chain is required, then a 
 * different context needs to be be used.
 *
 * There are two threading models supported - a single thread with one
 * SSL_CTX can support any number of SSL connections - and multiple threads can 
 * support one SSL_CTX object each (the default). But if a single SSL_CTX 
 * object uses many SSL objects in individual threads, then the 
 * CONFIG_SSL_CTX_MUTEXING option needs to be configured.
 *
 * @param options [in]  Any particular options. At present the options
 * supported are:
 * - SSL_SERVER_VERIFY_LATER (client only): Don't stop a handshake if the server
 * authentication fails. The certificate can be authenticated later with a
 * call to ssl_verify_cert().
 * - SSL_CLIENT_AUTHENTICATION (server only): Enforce client authentication
 * i.e. each handshake will include a "certificate request" message from the
 * server. Only available if verification has been enabled.
 * - SSL_DISPLAY_BYTES (full mode build only): Display the byte sequences
 * during the handshake.
 * - SSL_DISPLAY_STATES (full mode build only): Display the state changes
 * during the handshake.
 * - SSL_DISPLAY_CERTS (full mode build only): Display the certificates that
 * are passed during a handshake.
 * - SSL_DISPLAY_RSA (full mode build only): Display the RSA key details that
 * are passed during a handshake.
 * - SSL_CONNECT_IN_PARTS (client only): To use a non-blocking version of 
 * ssl_client_new().
 * @param num_sessions [in] The number of sessions to be used for session
 * caching. If this value is 0, then there is no session caching. This option
 * is not used in skeleton mode.
 * @return A client/server context.
 */
EXP_FUNC SSL_CTX * STDCALL ssl_ctx_new(uint32_t options, int num_sessions);

/**
 * @brief Remove a client/server context.
 *
 * Frees any used resources used by this context. Each connection will be 
 * sent a "Close Notify" alert (if possible).
 * @param ssl_ctx [in] The client/server context.
 */
EXP_FUNC void STDCALL ssl_ctx_free(SSL_CTX *ssl_ctx);

/**
 * @brief (server only) Establish a new SSL connection to an SSL client.
 *
 * It is up to the application to establish the logical connection (whether it
 * is  a socket, serial connection etc).
 * @param ssl_ctx [in] The server context.
 * @param client_fd [in] The client's file descriptor. 
 * @return An SSL object reference.
 */
EXP_FUNC SSL * STDCALL ssl_server_new(SSL_CTX *ssl_ctx, SSL_SOCKET* client_fd);

/**
 * @brief (client only) Establish a new SSL connection to an SSL server.
 *
 * It is up to the application to establish the initial logical connection 
 * (whether it is  a socket, serial connection etc).
 *
 * This is a normally a blocking call - it will finish when the handshake is 
 * complete (or has failed). To use in non-blocking mode, set 
 * SSL_CONNECT_IN_PARTS in ssl_ctx_new().
 * @param ssl_ctx [in] The client context.
 * @param client_fd [in] The client's file descriptor.
 * @param session_id [in] A 32 byte session id for session resumption. This 
 * can be null if no session resumption is being used or required. This option
 * is not used in skeleton mode.
 * @param sess_id_size The size of the session id (max 32)
 * @return An SSL object reference. Use ssl_handshake_status() to check 
 * if a handshake succeeded.
 */
EXP_FUNC SSL * STDCALL ssl_client_new(SSL_CTX *ssl_ctx, SSL_SOCKET* client_fd, const uint8_t *session_id, uint8_t sess_id_size);

/**
 * @brief Free any used resources on this connection. 
 
 * A "Close Notify" message is sent on this connection (if possible). It is up 
 * to the application to close the socket or file descriptor.
 * @param ssl [in] The ssl object reference.
 */
EXP_FUNC void STDCALL ssl_free(SSL *ssl);

/**
 * @brief Read the SSL data stream.
 * If the socket is non-blocking and data is blocked then SSO_OK will be
 * returned.
 * @param ssl [in] An SSL object reference.
 * @param in_data [out] If the read was successful, a pointer to the read
 * buffer will be here. Do NOT ever free this memory as this buffer is used in
 * sucessive calls. If the call was unsuccessful, this value will be null.
 * @return The number of decrypted bytes:
 * - if > 0, then the handshaking is complete and we are returning the number 
 *   of decrypted bytes. 
 * - SSL_OK if the handshaking stage is successful (but not yet complete).  
 * - < 0 if an error.
 * @see ssl.h for the error code list.
 * @note Use in_data before doing any successive ssl calls.
 */
EXP_FUNC int STDCALL ssl_read(SSL *ssl, uint8_t **in_data);

/**
 * @brief Write to the SSL data stream. 
 * if the socket is non-blocking and data is blocked then a check is made
 * to ensure that all data is sent (i.e. blocked mode is forced).
 * @param ssl [in] An SSL obect reference.
 * @param out_data [in] The data to be written
 * @param out_len [in] The number of bytes to be written.
 * @return The number of bytes sent, or if < 0 if an error.
 * @see ssl.h for the error code list.
 */
EXP_FUNC int STDCALL ssl_write(SSL *ssl, const uint8_t *out_data, int out_len);

/**
 * @brief Find an ssl object based on a file descriptor.
 *
 * Goes through the list of SSL objects maintained in a client/server context
 * to look for a file descriptor match.
 * @param ssl_ctx [in] The client/server context.
 * @param client_fd [in]  The file descriptor.
 * @return A reference to the SSL object. Returns null if the object could not 
 * be found.
 */
EXP_FUNC SSL * STDCALL ssl_find(SSL_CTX *ssl_ctx, SSL_SOCKET* client_fd);

/**
 * @brief Get the session id for a handshake. 
 * 
 * This will be a 32 byte sequence and is available after the first
 * handshaking messages are sent.
 * @param ssl [in] An SSL object reference.
 * @return The session id as a 32 byte sequence.
 * @note A SSLv23 handshake may have only 16 valid bytes.
 */
EXP_FUNC const uint8_t * STDCALL ssl_get_session_id(const SSL *ssl);

/**
 * @brief Get the session id size for a handshake. 
 * 
 * This will normally be 32 but could be 0 (no session id) or something else.
 * @param ssl [in] An SSL object reference.
 * @return The size of the session id.
 */
EXP_FUNC uint8_t STDCALL ssl_get_session_id_size(const SSL *ssl);

/**
 * @brief Return the cipher id (in the SSL form).
 * @param ssl [in] An SSL object reference.
 * @return The cipher id. This will be one of the following:
 * - SSL_AES128_SHA (0x2f)
 * - SSL_AES256_SHA (0x35)
 * - SSL_RC4_128_SHA (0x05)
 * - SSL_RC4_128_MD5 (0x04)
 */
EXP_FUNC uint8_t STDCALL ssl_get_cipher_id(const SSL *ssl);

/**
 * @brief Return the status of the handshake.
 * @param ssl [in] An SSL object reference.
 * @return SSL_OK if the handshake is complete and ok. 
 * @see ssl.h for the error code list.
 */
EXP_FUNC int STDCALL ssl_handshake_status(const SSL *ssl);

/**
 * @brief Retrieve various parameters about the axTLS engine.
 * @param offset [in] The configuration offset. It will be one of the following:
 * - SSL_BUILD_MODE The build mode. This will be one of the following:
 *   - SSL_BUILD_SERVER_ONLY            (basic server mode)
 *   - SSL_BUILD_ENABLE_VERIFICATION    (server can do client authentication)
 *   - SSL_BUILD_ENABLE_CLIENT          (client/server capabilties)
 *   - SSL_BUILD_FULL_MODE              (client/server with diagnostics)
 *   - SSL_BUILD_SKELETON_MODE          (skeleton mode)
 * - SSL_MAX_CERT_CFG_OFFSET The maximum number of certificates allowed.
 * - SSL_MAX_CA_CERT_CFG_OFFSET The maximum number of CA certificates allowed.
 * - SSL_HAS_PEM                        1 if supported
 * @return The value of the requested parameter.
 */
EXP_FUNC int STDCALL ssl_get_config(int offset);

/**
 * @brief Display why the handshake failed.
 *
 * This call is only useful in a 'full mode' build. The output is to stdout.
 * @param error_code [in] An error code.
 * @see ssl.h for the error code list.
 */
EXP_FUNC void STDCALL ssl_display_error(int error_code);

/**
 * @brief Authenticate a received certificate.
 * 
 * This call is usually made by a client after a handshake is complete and the
 * context is in SSL_SERVER_VERIFY_LATER mode.
 * @param ssl [in] An SSL object reference.
 * @return SSL_OK if the certificate is verified.
 */
EXP_FUNC int STDCALL ssl_verify_cert(const SSL *ssl);

/**
 * @brief Retrieve an X.509 distinguished name component.
 * 
 * When a handshake is complete and a certificate has been exchanged, then the
 * details of the remote certificate can be retrieved.
 *
 * This will usually be used by a client to check that the server's common 
 * name matches the URL.
 *
 * @param ssl [in] An SSL_X509_CERT object reference. [GBG: modified]
 * @param component [in] one of:
 * - SSL_X509_CERT_COMMON_NAME
 * - SSL_X509_CERT_ORGANIZATION
 * - SSL_X509_CERT_ORGANIZATIONAL_NAME
 * - SSL_X509_CA_CERT_COMMON_NAME
 * - SSL_X509_CA_CERT_ORGANIZATION
 * - SSL_X509_CA_CERT_ORGANIZATIONAL_NAME
 * @return The appropriate string (or null if not defined)
 * @note Verification build mode must be enabled.
 */
/* GBG: modified */
EXP_FUNC const char * STDCALL ssl_cert_get_dn(const SSL_X509_CERT *cert, int component);

/**
 * @brief Retrieve a Subject Alternative DNSName
 *
 * When a handshake is complete and a certificate has been exchanged, then the
 * details of the remote certificate can be retrieved.
 *
 * This will usually be used by a client to check that the server's DNS  
 * name matches the URL.
 *
 * @param ssl [in] An SSL_X509_CERT object reference. [GBG: modified]
 * @param dnsindex [in] The index of the DNS name to retrieve.
 * @return The appropriate string (or null if not defined)
 * @note Verification build mode must be enabled.
 */
/* GBG: modified */
EXP_FUNC const char * STDCALL ssl_cert_get_subject_alt_dnsname(const SSL_X509_CERT *cert, int dnsindex);

/* GBG added */
EXP_FUNC const SSL_X509_CERT* ssl_get_peer_cert(const SSL* ssl, unsigned int position);
EXP_FUNC void ssl_cert_get_fingerprints(const SSL_X509_CERT *cert, unsigned char* md5, unsigned char* sha1);
EXP_FUNC void ssl_cert_get_validity_dates(const SSL_X509_CERT *cert, SSL_DateTime* not_before, SSL_DateTime* not_after);

/**
 * @brief Force the client to perform its handshake again.
 *
 * For a client this involves sending another "client hello" message.
 * For the server is means sending a "hello request" message.
 *
 * This is a blocking call on the client (until the handshake completes).
 *
 * @param ssl [in] An SSL object reference.
 * @return SSL_OK if renegotiation instantiation was ok
 */
EXP_FUNC int STDCALL ssl_renegotiate(SSL *ssl);

/**
 * @brief Process a file that is in binary DER or ASCII PEM format.
 *
 * These are temporary objects that are used to load private keys,
 * certificates etc into memory.
 * @param ssl_ctx [in] The client/server context.
 * @param obj_type [in] The format of the file. Can be one of:
 * - SSL_OBJ_X509_CERT (no password required)
 * - SSL_OBJ_X509_CACERT (no password required)
 * - SSL_OBJ_RSA_KEY (AES128/AES256 PEM encryption supported)
 * - SSL_OBJ_PKCS8 (RC4-128 encrypted data supported)
 * - SSL_OBJ_PKCS12 (RC4-128 encrypted data supported)
 *
 * PEM files are automatically detected (if supported). The object type is
 * also detected, and so is not relevant for these types of files.
 * @param filename [in] The location of a file in DER/PEM format.
 * @param password [in] The password used. Can be null if not required.
 * @return SSL_OK if all ok
 * @note Not available in skeleton build mode.
 */
EXP_FUNC int STDCALL ssl_obj_load(SSL_CTX *ssl_ctx, int obj_type, const char *filename, const char *password);

/**
 * @brief Process binary data.
 *
 * These are temporary objects that are used to load private keys,
 * certificates etc into memory.
 * @param ssl_ctx [in] The client/server context.
 * @param obj_type [in] The format of the memory data.
 * @param data [in] The binary data to be loaded.
 * @param len [in] The amount of data to be loaded.
 * @param password [in] The password used. Can be null if not required.
 * @return SSL_OK if all ok
 * @see ssl_obj_load for more details on obj_type.
 */
EXP_FUNC int STDCALL ssl_obj_memory_load(SSL_CTX *ssl_ctx, int obj_type, const uint8_t *data, int len, const char *password);

#ifdef CONFIG_SSL_GENERATE_X509_CERT
/**
 * @brief Create an X.509 certificate. 
 * 
 * This certificate is a self-signed v1 cert with a fixed start/stop validity 
 * times. It is signed with an internal private key in ssl_ctx.
 *
 * @param ssl_ctx [in] The client/server context.
 * @param options [in] Not used yet.
 * @param dn [in] An array of distinguished name strings. The array is defined
 * by:
 * - SSL_X509_CERT_COMMON_NAME (0)
 *      - If SSL_X509_CERT_COMMON_NAME is empty or not defined, then the 
 *        hostname will be used.
 * - SSL_X509_CERT_ORGANIZATION (1)
 *      - If SSL_X509_CERT_ORGANIZATION is empty or not defined, then $USERNAME 
 *        will be used.
 * - SSL_X509_CERT_ORGANIZATIONAL_NAME (2)
 *      - SSL_X509_CERT_ORGANIZATIONAL_NAME is optional.
 * @param cert_data [out] The certificate as a sequence of bytes.
 * @return < 0 if an error, or the size of the certificate in bytes.
 * @note cert_data must be freed when there is no more need for it.
 */
EXP_FUNC int STDCALL ssl_x509_create(SSL_CTX *ssl_ctx, uint32_t options, const char * dn[], uint8_t **cert_data);
#endif

/**
 * @brief Return the axTLS library version as a string.
 */
EXP_FUNC const char * STDCALL ssl_version(void);

EXP_FUNC void ssl_mem_free(void* mem); /* GBG */

/** @} */

#ifdef __cplusplus
}
#endif

#endif
