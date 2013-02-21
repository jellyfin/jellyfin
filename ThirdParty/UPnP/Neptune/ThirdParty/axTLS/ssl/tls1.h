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
 * @file tls1.h
 *
 * @brief The definitions for the TLS library.
 */
#ifndef HEADER_SSL_LIB_H
#define HEADER_SSL_LIB_H

#ifdef __cplusplus
extern "C" {
#endif

#include "version.h"
#include "crypto.h"
#include "crypto_misc.h"

#define SSL_PROTOCOL_MIN_VERSION    0x31   /* TLS v1.0 */
#define SSL_PROTOCOL_MINOR_VERSION  0x02   /* TLS v1.1 */
#define SSL_PROTOCOL_VERSION_MAX    0x32   /* TLS v1.1 */
#define SSL_PROTOCOL_VERSION1_1     0x32   /* TLS v1.1 */
#define SSL_RANDOM_SIZE             32
#define SSL_SECRET_SIZE             48
#define SSL_FINISHED_HASH_SIZE      12
#define SSL_RECORD_SIZE             5
#define SSL_SERVER_READ             0
#define SSL_SERVER_WRITE            1
#define SSL_CLIENT_READ             2
#define SSL_CLIENT_WRITE            3
#define SSL_HS_HDR_SIZE             4

/* the flags we use while establishing a connection */
#define SSL_NEED_RECORD             0x0001
#define SSL_TX_ENCRYPTED            0x0002 
#define SSL_RX_ENCRYPTED            0x0004
#define SSL_SESSION_RESUME          0x0008
#define SSL_IS_CLIENT               0x0010
#define SSL_HAS_CERT_REQ            0x0020
#define SSL_SENT_CLOSE_NOTIFY       0x0040

/* some macros to muck around with flag bits */
#define SET_SSL_FLAG(A)             (ssl->flag |= A)
#define CLR_SSL_FLAG(A)             (ssl->flag &= ~A)
#define IS_SET_SSL_FLAG(A)          (ssl->flag & A)

#define MAX_KEY_BYTE_SIZE           512     /* for a 4096 bit key */
#define RT_MAX_PLAIN_LENGTH         16384
#define RT_EXTRA                    1024
#define BM_RECORD_OFFSET            5

#ifdef CONFIG_SSL_SKELETON_MODE
#define NUM_PROTOCOLS               1
#else
#define NUM_PROTOCOLS               4
#endif

#define PARANOIA_CHECK(A, B)        if (A < B) { \
    ret = SSL_ERROR_INVALID_HANDSHAKE; goto error; }

/* protocol types */
enum
{
    PT_CHANGE_CIPHER_SPEC = 20,
    PT_ALERT_PROTOCOL,
    PT_HANDSHAKE_PROTOCOL,
    PT_APP_PROTOCOL_DATA
};

/* handshaking types */
enum
{
    HS_HELLO_REQUEST,
    HS_CLIENT_HELLO,
    HS_SERVER_HELLO,
    HS_CERTIFICATE = 11,
    HS_SERVER_KEY_XCHG,
    HS_CERT_REQ,
    HS_SERVER_HELLO_DONE,
    HS_CERT_VERIFY,
    HS_CLIENT_KEY_XCHG,
    HS_FINISHED = 20
};

typedef struct 
{
    uint8_t cipher;
    uint8_t key_size;
    uint8_t iv_size;
    uint8_t key_block_size;
    uint8_t padding_size;
    uint8_t digest_size;
    hmac_func hmac;
    crypt_func encrypt;
    crypt_func decrypt;
} cipher_info_t;

struct _SSLObjLoader 
{
    uint8_t *buf;
    int len;
};

typedef struct _SSLObjLoader SSLObjLoader;

typedef struct 
{
    time_t conn_time;
    uint8_t session_id[SSL_SESSION_ID_SIZE];
    uint8_t master_secret[SSL_SECRET_SIZE];
} SSL_SESSION;

typedef struct _SSL_CERT /* GBG: added */
{
    uint8_t *buf;
    int size;
    struct _SSL_CERT* next; /* GBG: added */
} SSL_CERT;

typedef X509_CTX SSL_X509_CERT;

typedef struct
{
    MD5_CTX md5_ctx;
    SHA1_CTX sha1_ctx;
    uint8_t final_finish_mac[SSL_FINISHED_HASH_SIZE];
    uint8_t *key_block;
    uint8_t master_secret[SSL_SECRET_SIZE];
    uint8_t client_random[SSL_RANDOM_SIZE]; /* client's random sequence */
    uint8_t server_random[SSL_RANDOM_SIZE]; /* server's random sequence */
    uint16_t bm_proc_index;
} DISPOSABLE_CTX;

struct _SSL
{
    uint32_t flag;
    uint16_t need_bytes;
    uint16_t got_bytes;
    uint8_t record_type;
    uint8_t cipher;
    uint8_t sess_id_size;
    uint8_t version;
    uint8_t client_version;
    int16_t next_state;
    int16_t hs_status;
    DISPOSABLE_CTX *dc;         /* temporary data which we'll get rid of soon */
    SSL_SOCKET* client_fd;
    const cipher_info_t *cipher_info;
    void *encrypt_ctx;
    void *decrypt_ctx;
    uint8_t bm_all_data[RT_MAX_PLAIN_LENGTH+RT_EXTRA];
    uint8_t *bm_data;
    uint16_t bm_index;
    uint16_t bm_read_index;
    struct _SSL *next;                  /* doubly linked list */
    struct _SSL *prev;
    struct _SSL_CTX *ssl_ctx;           /* back reference to a clnt/svr ctx */
#ifndef CONFIG_SSL_SKELETON_MODE
    uint16_t session_index;
    SSL_SESSION *session;
#endif
#ifdef CONFIG_SSL_CERT_VERIFICATION
    X509_CTX *x509_ctx;
#endif

    uint8_t session_id[SSL_SESSION_ID_SIZE]; 
    uint8_t client_mac[SHA1_SIZE];  /* for HMAC verification */
    uint8_t server_mac[SHA1_SIZE];  /* for HMAC verification */
    uint8_t read_sequence[8];       /* 64 bit sequence number */
    uint8_t write_sequence[8];      /* 64 bit sequence number */
    uint8_t hmac_header[SSL_RECORD_SIZE];    /* rx hmac */
};

typedef struct _SSL SSL;

struct _SSL_CTX
{
    uint32_t options;
    /* GBG: removed - uint8_t chain_length; */
    RSA_CTX *rsa_ctx;
#ifdef CONFIG_SSL_CERT_VERIFICATION
    /* GBG: removed CA_CERT_CTX *ca_cert_ctx; */
    X509_CTX* ca_certs; /* GBG: added */
#endif
    SSL *head;
    SSL *tail;
    SSL_CERT* certs; /* GBG: modified */
#ifndef CONFIG_SSL_SKELETON_MODE
    uint16_t num_sessions;
    SSL_SESSION **ssl_sessions;
#endif
#ifdef CONFIG_SSL_CTX_MUTEXING
    SSL_CTX_MUTEX_TYPE mutex;
#endif
#ifdef CONFIG_OPENSSL_COMPATIBLE
    void *bonus_attr;
#endif
};

typedef struct _SSL_CTX SSL_CTX;

/* backwards compatibility */
typedef struct _SSL_CTX SSLCTX;

extern const uint8_t ssl_prot_prefs[NUM_PROTOCOLS];

SSL *ssl_new(SSL_CTX *ssl_ctx, void* client_fd);
void disposable_new(SSL *ssl);
void disposable_free(SSL *ssl);
int send_packet(SSL *ssl, uint8_t protocol, 
        const uint8_t *in, int length);
int do_svr_handshake(SSL *ssl, int handshake_type, uint8_t *buf, int hs_len);
int do_clnt_handshake(SSL *ssl, int handshake_type, uint8_t *buf, int hs_len);
int process_finished(SSL *ssl, uint8_t *buf, int hs_len);
int process_sslv23_client_hello(SSL *ssl);
int send_alert(SSL *ssl, int error_code);
int send_finished(SSL *ssl);
int send_certificate(SSL *ssl);
int basic_read(SSL *ssl, uint8_t **in_data);
int send_change_cipher_spec(SSL *ssl);
void finished_digest(SSL *ssl, const char *label, uint8_t *digest);
void generate_master_secret(SSL *ssl, const uint8_t *premaster_secret);
void add_packet(SSL *ssl, const uint8_t *pkt, int len);
int add_cert(SSL_CTX *ssl_ctx, const uint8_t *buf, int len);
int add_private_key(SSL_CTX *ssl_ctx, SSLObjLoader *ssl_obj);
void ssl_obj_free(SSLObjLoader *ssl_obj);
int pkcs8_decode(SSL_CTX *ssl_ctx, SSLObjLoader *ssl_obj, const char *password);
int pkcs12_decode(SSL_CTX *ssl_ctx, SSLObjLoader *ssl_obj, const char *password);
int load_key_certs(SSL_CTX *ssl_ctx);
#ifdef CONFIG_SSL_CERT_VERIFICATION
int add_cert_auth(SSL_CTX *ssl_ctx, const uint8_t *buf, int len);
/* GBG: removed - void remove_ca_certs(CA_CERT_CTX *ca_cert_ctx); */
#endif
#ifdef CONFIG_SSL_ENABLE_CLIENT
int do_client_connect(SSL *ssl);
#endif

#ifdef CONFIG_SSL_FULL_MODE
void DISPLAY_STATE(SSL *ssl, int is_send, uint8_t state, int not_ok);
void DISPLAY_BYTES(SSL *ssl, const char *format, 
        const uint8_t *data, int size, ...);
void DISPLAY_CERT(SSL *ssl, const X509_CTX *x509_ctx);
void DISPLAY_RSA(SSL *ssl,  const RSA_CTX *rsa_ctx);
void DISPLAY_ALERT(SSL *ssl, int alert);
#else
#define DISPLAY_STATE(A,B,C,D)
#define DISPLAY_CERT(A,B)
#define DISPLAY_RSA(A,B)
#define DISPLAY_ALERT(A, B)
#ifdef WIN32
void DISPLAY_BYTES(SSL *ssl, const char *format,/* win32 has no variadic macros */
        const uint8_t *data, int size, ...);
#else
void DISPLAY_BYTES(SSL *ssl, const char *format,/* win32 has no variadic macros */
        const uint8_t *data, int size, ...);
#endif
#endif

#ifdef CONFIG_SSL_CERT_VERIFICATION
int process_certificate(SSL *ssl, X509_CTX **x509_ctx);
#endif

SSL_SESSION *ssl_session_update(int max_sessions, 
        SSL_SESSION *ssl_sessions[], SSL *ssl,
        const uint8_t *session_id);
void kill_ssl_session(SSL_SESSION **ssl_sessions, SSL *ssl);

#ifdef __cplusplus
}
#endif

#endif 
