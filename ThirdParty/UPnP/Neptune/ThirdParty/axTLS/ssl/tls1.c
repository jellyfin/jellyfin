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
 * Common ssl/tlsv1 code to both the client and server implementations.
 */

#include <string.h>
#include <stdlib.h>
#include <stdio.h>
#include <stdarg.h>
#include "os_port.h"
#include "ssl.h"

/* The session expiry time */
#define SSL_EXPIRY_TIME     (CONFIG_SSL_EXPIRY_TIME*3600)

static const uint8_t g_hello_request[] = { HS_HELLO_REQUEST, 0, 0, 0 };
static const uint8_t g_chg_cipher_spec_pkt[] = { 1 };
static const char * server_finished = "server finished";
static const char * client_finished = "client finished";

static int do_handshake(SSL *ssl, uint8_t *buf, int read_len);
static int set_key_block(SSL *ssl, int is_write);
static int verify_digest(SSL *ssl, int mode, const uint8_t *buf, int read_len);
static void *crypt_new(SSL *ssl, uint8_t *key, uint8_t *iv, int is_decrypt);
static int send_raw_packet(SSL *ssl, uint8_t protocol);

/**
 * The server will pick the cipher based on the order that the order that the
 * ciphers are listed. This order is defined at compile time.
 */
#ifdef CONFIG_SSL_SKELETON_MODE
const uint8_t ssl_prot_prefs[NUM_PROTOCOLS] = 
{ SSL_RC4_128_SHA };
#else
static void session_free(SSL_SESSION *ssl_sessions[], int sess_index);

const uint8_t ssl_prot_prefs[NUM_PROTOCOLS] = 
#ifdef CONFIG_SSL_PROT_LOW                  /* low security, fast speed */
{ SSL_RC4_128_SHA, SSL_AES128_SHA, SSL_AES256_SHA, SSL_RC4_128_MD5 };
#elif CONFIG_SSL_PROT_MEDIUM                /* medium security, medium speed */
{ SSL_AES128_SHA, SSL_AES256_SHA, SSL_RC4_128_SHA, SSL_RC4_128_MD5 };    
#else /* CONFIG_SSL_PROT_HIGH */            /* high security, low speed */
{ SSL_AES256_SHA, SSL_AES128_SHA, SSL_RC4_128_SHA, SSL_RC4_128_MD5 };
#endif
#endif /* CONFIG_SSL_SKELETON_MODE */

/**
 * The cipher map containing all the essentials for each cipher.
 */
#ifdef CONFIG_SSL_SKELETON_MODE
static const cipher_info_t cipher_info[NUM_PROTOCOLS] = 
{
    {   /* RC4-SHA */
        SSL_RC4_128_SHA,                /* RC4-SHA */
        16,                             /* key size */
        0,                              /* iv size */ 
        2*(SHA1_SIZE+16),               /* key block size */
        0,                              /* no padding */
        SHA1_SIZE,                      /* digest size */
        hmac_sha1,                      /* hmac algorithm */
        (crypt_func)RC4_crypt,          /* encrypt */
        (crypt_func)RC4_crypt           /* decrypt */
    },
};
#else
static const cipher_info_t cipher_info[NUM_PROTOCOLS] = 
{
    {   /* AES128-SHA */
        SSL_AES128_SHA,                 /* AES128-SHA */
        16,                             /* key size */
        16,                             /* iv size */ 
        2*(SHA1_SIZE+16+16),            /* key block size */
        16,                             /* block padding size */
        SHA1_SIZE,                      /* digest size */
        hmac_sha1,                      /* hmac algorithm */
        (crypt_func)AES_cbc_encrypt,    /* encrypt */
        (crypt_func)AES_cbc_decrypt     /* decrypt */
    },
    {   /* AES256-SHA */
        SSL_AES256_SHA,                 /* AES256-SHA */
        32,                             /* key size */
        16,                             /* iv size */ 
        2*(SHA1_SIZE+32+16),            /* key block size */
        16,                             /* block padding size */
        SHA1_SIZE,                      /* digest size */
        hmac_sha1,                      /* hmac algorithm */
        (crypt_func)AES_cbc_encrypt,    /* encrypt */
        (crypt_func)AES_cbc_decrypt     /* decrypt */
    },       
    {   /* RC4-SHA */
        SSL_RC4_128_SHA,                /* RC4-SHA */
        16,                             /* key size */
        0,                              /* iv size */ 
        2*(SHA1_SIZE+16),               /* key block size */
        0,                              /* no padding */
        SHA1_SIZE,                      /* digest size */
        hmac_sha1,                      /* hmac algorithm */
        (crypt_func)RC4_crypt,          /* encrypt */
        (crypt_func)RC4_crypt           /* decrypt */
    },
    /*
     * This protocol is from SSLv2 days and is unlikely to be used - but was
     * useful for testing different possible digest algorithms.
     */
    {   /* RC4-MD5 */
        SSL_RC4_128_MD5,                /* RC4-MD5 */
        16,                             /* key size */
        0,                              /* iv size */ 
        2*(MD5_SIZE+16),                /* key block size */
        0,                              /* no padding */
        MD5_SIZE,                       /* digest size */
        hmac_md5,                       /* hmac algorithm */
        (crypt_func)RC4_crypt,          /* encrypt */
        (crypt_func)RC4_crypt           /* decrypt */
    },
};
#endif

static void prf(const uint8_t *sec, int sec_len, uint8_t *seed, int seed_len,
        uint8_t *out, int olen);
static const cipher_info_t *get_cipher_info(uint8_t cipher);
static void increment_read_sequence(SSL *ssl);
static void increment_write_sequence(SSL *ssl);
static void add_hmac_digest(SSL *ssl, int snd, uint8_t *hmac_header,
        const uint8_t *buf, int buf_len, uint8_t *hmac_buf);

/* win32 VC6.0 doesn't have variadic macros */
/*#if defined(WIN32) && !defined(CONFIG_SSL_FULL_MODE)*/
#if 1
void DISPLAY_BYTES(SSL *ssl, const char *format, 
        const uint8_t *data, int size, ...) {
    (void)ssl;
    (void)format;
    (void)data;
    (void)size;
}
#endif

/**
 * Establish a new client/server context.
 */
EXP_FUNC SSL_CTX *STDCALL ssl_ctx_new(uint32_t options, int num_sessions)
{
    SSL_CTX *ssl_ctx = (SSL_CTX *)calloc(1, sizeof (SSL_CTX));
    ssl_ctx->options = options;

#if 0 /* GBG: no automatic cert loading */
    if (load_key_certs(ssl_ctx) < 0)
    {
        free(ssl_ctx);  /* can't load our key/certificate pair, so die */
        return NULL;
    }
#endif

#ifndef CONFIG_SSL_SKELETON_MODE
    ssl_ctx->num_sessions = num_sessions;
#endif

    SSL_CTX_MUTEX_INIT(ssl_ctx->mutex);

#ifndef CONFIG_SSL_SKELETON_MODE
    if (num_sessions)
    {
        ssl_ctx->ssl_sessions = (SSL_SESSION **)
                        calloc(1, num_sessions*sizeof(SSL_SESSION *));
    }
#endif

    return ssl_ctx;
}

/*
 * Remove a client/server context.
 */
EXP_FUNC void STDCALL ssl_ctx_free(SSL_CTX *ssl_ctx)
{
    SSL *ssl;
    int i;

    if (ssl_ctx == NULL)
        return;

    ssl = ssl_ctx->head;

    /* clear out all the ssl entries */
    while (ssl)
    {
        SSL *next = ssl->next;
        ssl_free(ssl);
        ssl = next;
    }

#ifndef CONFIG_SSL_SKELETON_MODE
    /* clear out all the sessions */
    for (i = 0; i < ssl_ctx->num_sessions; i++)
        session_free(ssl_ctx->ssl_sessions, i);

    free(ssl_ctx->ssl_sessions);
#endif

    /* GBG: changed */
    {
        SSL_CERT* cert = ssl_ctx->certs;
        while (cert) {
            SSL_CERT* next = cert->next;
            free(cert->buf);
            free(cert);
            cert = next;
        }
    }

#ifdef CONFIG_SSL_CERT_VERIFICATION
    /* GBG: remove - remove_ca_certs(ssl_ctx->ca_cert_ctx); */
    /* GBG: added */
    if (ssl_ctx->ca_certs) x509_free(ssl_ctx->ca_certs);
    /* GBG */
#endif
    /* GBG: removed - ssl_ctx->chain_length = 0; */
    SSL_CTX_MUTEX_DESTROY(ssl_ctx->mutex);
    RSA_free(ssl_ctx->rsa_ctx);
    RNG_terminate();
    free(ssl_ctx);
}

/*
 * Free any used resources used by this connection.
 */
EXP_FUNC void STDCALL ssl_free(SSL *ssl)
{
    SSL_CTX *ssl_ctx;

    if (ssl == NULL)        /* just ignore null pointers */
        return;

    /* only notify if we weren't notified first */
    /* spec says we must notify when we are dying */
    if (!IS_SET_SSL_FLAG(SSL_SENT_CLOSE_NOTIFY))
      send_alert(ssl, SSL_ALERT_CLOSE_NOTIFY);

    ssl_ctx = ssl->ssl_ctx;

    SSL_CTX_LOCK(ssl_ctx->mutex);

    /* adjust the server SSL list */
    if (ssl->prev)
        ssl->prev->next = ssl->next;
    else
        ssl_ctx->head = ssl->next;

    if (ssl->next)
        ssl->next->prev = ssl->prev;
    else
        ssl_ctx->tail = ssl->prev;

    SSL_CTX_UNLOCK(ssl_ctx->mutex);

    /* may already be free - but be sure */
    free(ssl->encrypt_ctx);
    free(ssl->decrypt_ctx);
    disposable_free(ssl);
#ifdef CONFIG_SSL_CERT_VERIFICATION
    x509_free(ssl->x509_ctx);
#endif

    free(ssl);
}

/*
 * Read the SSL connection and send any alerts for various errors.
 */
EXP_FUNC int STDCALL ssl_read(SSL *ssl, uint8_t **in_data)
{
    int ret = basic_read(ssl, in_data);

    /* check for return code so we can send an alert */
    if (ret < SSL_OK)
    {
        if (ret != SSL_ERROR_CONN_LOST &&
            ret != SSL_CLOSE_NOTIFY    && 
            ret != SSL_ERROR_TIMEOUT   && /* GBG */
            ret != SSL_ERROR_EOS          /* GBG */ )
        {
            send_alert(ssl, ret);
#ifndef CONFIG_SSL_SKELETON_MODE
            /* something nasty happened, so get rid of this session */
            kill_ssl_session(ssl->ssl_ctx->ssl_sessions, ssl);
#endif
        }
    }

    return ret;
}

/*
 * Write application data to the client
 */
EXP_FUNC int STDCALL ssl_write(SSL *ssl, const uint8_t *out_data, int out_len)
{
    int n = out_len, nw, i, tot = 0;

    /* maximum size of a TLS packet is around 16kB, so fragment */
    do 
    {
        nw = n;

        if (nw > RT_MAX_PLAIN_LENGTH)    /* fragment if necessary */
            nw = RT_MAX_PLAIN_LENGTH;

        if ((i = send_packet(ssl, PT_APP_PROTOCOL_DATA, 
                                            &out_data[tot], nw)) <= 0)
        {
            out_len = i;    /* an error */
            break;
        }

        tot += i;
        n -= i;
    } while (n > 0);

    return out_len;
}

/**
 * Add a certificate to the certificate chain.
 */
int add_cert(SSL_CTX *ssl_ctx, const uint8_t *buf, int len)
{
    int ret = SSL_ERROR_NO_CERT_DEFINED;
    SSL_CERT *ssl_cert;
    SSL_CERT *ssl_cert_tail;
    X509_CTX *cert = NULL;
    int offset;
    if ((ret = x509_new(buf, &offset, &cert)))
        goto error;

#if defined (CONFIG_SSL_FULL_MODE)
    if (ssl_ctx->options & SSL_DISPLAY_CERTS)
        x509_print(cert, NULL);
#endif

    /* GBG: modified */
    ssl_cert = (SSL_CERT*)malloc(sizeof(SSL_CERT));
    if (ssl_cert == NULL) {
        ret = SSL_NOT_OK;
        goto error;
    }
    ssl_cert_tail = ssl_ctx->certs;
    while (ssl_cert_tail && ssl_cert_tail->next) {
        ssl_cert_tail = ssl_cert_tail->next;
    }    
    if (ssl_cert_tail == NULL) {
        ssl_ctx->certs = ssl_cert;
    } else {
        ssl_cert_tail->next = ssl_cert;
    }
    ssl_cert->next = NULL;
    ssl_cert->size = len;
    ssl_cert->buf = (uint8_t *)malloc(len);
    memcpy(ssl_cert->buf, buf, len);
    /* GBG: removed - ssl_ctx->chain_length++; */
    len -= offset;
    ret = SSL_OK;           /* ok so far */

    /* recurse? */
    if (len > 0)
    {
        ret = add_cert(ssl_ctx, &buf[offset], len);
    }

error:
    if (cert) x509_free(cert);        /* don't need anymore */
    return ret;
}

#ifdef CONFIG_SSL_CERT_VERIFICATION
#if 0 /* GBG: removed */
/**
 * Add a certificate authority.
 */
int add_cert_auth(SSL_CTX *ssl_ctx, const uint8_t *buf, int len)
{
    int ret = SSL_ERROR_NO_CERT_DEFINED;
    int i = 0;
    int offset;
    CA_CERT_CTX *ca_cert_ctx;

    if (ssl_ctx->ca_cert_ctx == NULL)
        ssl_ctx->ca_cert_ctx = (CA_CERT_CTX *)calloc(1, sizeof(CA_CERT_CTX));

    ca_cert_ctx = ssl_ctx->ca_cert_ctx;

    while (i < CONFIG_X509_MAX_CA_CERTS && ca_cert_ctx->cert[i]) 
        i++;

    if (i >= CONFIG_X509_MAX_CA_CERTS)
    {
#ifdef CONFIG_SSL_FULL_MODE
        printf("Error: maximum number of CA certs added - change of "
                "compile-time configuration required\n");
#endif
        goto error;
    }

    if ((ret = x509_new(buf, &offset, &ca_cert_ctx->cert[i]))) {
        ret = SSL_X509_ERROR(ret); /* GBG */
        goto error;
    }

    len -= offset;
    ret = SSL_OK;           /* ok so far */

    /* recurse? */
    if (len > 0)
        ret = add_cert_auth(ssl_ctx, &buf[offset], len);

error:
    return ret;
}
#else /* GBG */
/* GBG: added */
int add_cert_auth(SSL_CTX *ssl_ctx, const uint8_t *cert_data, int cert_data_size)
{
    X509_CTX* ca_cert = NULL;
    int       x509_size = cert_data_size;
    int       result;
    
    result = x509_new(cert_data, &x509_size, &ca_cert);
    if (result != SSL_OK) return SSL_X509_ERROR(result);
    ca_cert->next = ssl_ctx->ca_certs;
    ssl_ctx->ca_certs = ca_cert;
    
    return SSL_OK;
}
#endif /* GBG */

/* GBG added */
EXP_FUNC const X509_CTX* ssl_get_peer_cert(const SSL* ssl, unsigned int position)
{
    X509_CTX* cert = ssl->x509_ctx;
    
    /* look for the cert at the requested position */
    while (position && cert) {
        cert = cert->next;
        --position;
    }
    if (position) return NULL;
    
    /* past the last cert, check for a cert in the list of trust anchors */
    if (cert == NULL && ssl->x509_ctx && ssl->ssl_ctx) {
        X509_CTX* ca_cert = ssl->ssl_ctx->ca_certs;
        cert = ssl->x509_ctx;
        while (cert->next) cert = cert->next;
        while (ca_cert) {
            if (asn1_compare_dn(cert->ca_cert_dn, ca_cert->cert_dn) == 0) {
                return ca_cert;
            }
            ca_cert = ca_cert->next;
        }
    }
    
    return cert;
}

/*
 * Retrieve an X.509 distinguished name component
 */
EXP_FUNC const char * STDCALL ssl_cert_get_dn(const X509_CTX* cert, int component)
{
    if (cert == NULL) return NULL;
    switch (component)
    {
        case SSL_X509_CERT_COMMON_NAME:
            return cert->cert_dn[X509_COMMON_NAME];

        case SSL_X509_CERT_ORGANIZATION:
            return cert->cert_dn[X509_ORGANIZATION];

        case SSL_X509_CERT_ORGANIZATIONAL_NAME:       
            return cert->cert_dn[X509_ORGANIZATIONAL_UNIT];

        case SSL_X509_CA_CERT_COMMON_NAME:
            return cert->ca_cert_dn[X509_COMMON_NAME];

        case SSL_X509_CA_CERT_ORGANIZATION:
            return cert->ca_cert_dn[X509_ORGANIZATION];

        case SSL_X509_CA_CERT_ORGANIZATIONAL_NAME:       
            return cert->ca_cert_dn[X509_ORGANIZATIONAL_UNIT];

        default:
            return NULL;
    }
}

/*
 * Retrieve a "Subject Alternative Name" from a v3 certificate
 */
EXP_FUNC const char * STDCALL ssl_cert_get_subject_alt_dnsname(const X509_CTX* cert,
        int dnsindex)
{
    int i;

    if (cert == NULL || cert->subject_alt_dnsnames == NULL)
        return NULL;

    for (i = 0; i < dnsindex; ++i)
    {
        if (cert->subject_alt_dnsnames[i] == NULL)
            return NULL;
    }

    return cert->subject_alt_dnsnames[dnsindex];
}

/* GBG added */
EXP_FUNC void ssl_cert_get_fingerprints(const X509_CTX* cert, unsigned char* md5, unsigned char* sha1)
{
    if (cert == NULL) {
        memset(md5, 0, MD5_SIZE);
        memset(sha1, 0, SHA1_SIZE);
        return;
    }
    memcpy(md5,  cert->fingerprint.md5, MD5_SIZE);
    memcpy(sha1, cert->fingerprint.sha1, SHA1_SIZE);
}

EXP_FUNC void ssl_cert_get_validity_dates(const X509_CTX* cert, SSL_DateTime* not_before, SSL_DateTime* not_after)
{
    if (cert == NULL) {
        memset(not_before, 0, sizeof(SSL_DateTime));
        memset(not_after, 0, sizeof(SSL_DateTime));
        return;
    }
    *not_before = cert->not_before;
    *not_after  = cert->not_after;
}
/* /GBG added */

#endif /* CONFIG_SSL_CERT_VERIFICATION */

/*
 * Find an ssl object based on the client's file descriptor.
 */
EXP_FUNC SSL * STDCALL ssl_find(SSL_CTX *ssl_ctx, SSL_SOCKET* client_fd)
{
    SSL *ssl;

    SSL_CTX_LOCK(ssl_ctx->mutex);
    ssl = ssl_ctx->head;

    /* search through all the ssl entries */
    while (ssl)
    {
        if (ssl->client_fd == client_fd)
        {
            SSL_CTX_UNLOCK(ssl_ctx->mutex);
            return ssl;
        }

        ssl = ssl->next;
    }

    SSL_CTX_UNLOCK(ssl_ctx->mutex);
    return NULL;
}

/*
 * Force the client to perform its handshake again.
 */
EXP_FUNC int STDCALL ssl_renegotiate(SSL *ssl)
{
    int ret = SSL_OK;

    disposable_new(ssl);
#ifdef CONFIG_SSL_ENABLE_CLIENT
    if (IS_SET_SSL_FLAG(SSL_IS_CLIENT))
    {
        ret = do_client_connect(ssl);
    }
    else
#endif
    {
        send_packet(ssl, PT_HANDSHAKE_PROTOCOL, 
                g_hello_request, sizeof(g_hello_request));
        SET_SSL_FLAG(SSL_NEED_RECORD);
    }

    return ret;
}

/**
 * @brief Get what we need for key info.
 * @param cipher    [in]    The cipher information we are after
 * @param key_size  [out]   The key size for the cipher
 * @param iv_size   [out]   The iv size for the cipher
 * @return  The amount of key information we need.
 */
static const cipher_info_t *get_cipher_info(uint8_t cipher)
{
    int i;

    for (i = 0; i < NUM_PROTOCOLS; i++)
    {
        if (cipher_info[i].cipher == cipher)
        {
            return &cipher_info[i];
        }
    }

    return NULL;  /* error */
}

/*
 * Get a new ssl context for a new connection.
 */
SSL *ssl_new(SSL_CTX *ssl_ctx, void* client_fd)
{
    SSL *ssl = (SSL *)calloc(1, sizeof(SSL));
    ssl->ssl_ctx = ssl_ctx;
    ssl->need_bytes = SSL_RECORD_SIZE;      /* need a record */
    ssl->client_fd = client_fd;
    ssl->flag = SSL_NEED_RECORD;
    ssl->bm_data = ssl->bm_all_data+BM_RECORD_OFFSET; /* space at the start */
    ssl->hs_status = SSL_NOT_OK;            /* not connected */
#ifdef CONFIG_ENABLE_VERIFICATION
    ssl->ca_cert_ctx = ssl_ctx->ca_cert_ctx;
#endif
    disposable_new(ssl);

    /* a bit hacky but saves a few bytes of memory */
    ssl->flag |= ssl_ctx->options;
    SSL_CTX_LOCK(ssl_ctx->mutex);

    if (ssl_ctx->head == NULL)
    {
        ssl_ctx->head = ssl;
        ssl_ctx->tail = ssl;
    }
    else
    {
        ssl->prev = ssl_ctx->tail;
        ssl_ctx->tail->next = ssl;
        ssl_ctx->tail = ssl;
    }

    SSL_CTX_UNLOCK(ssl_ctx->mutex);
    return ssl;
}

/*
 * Add a private key to a context.
 */
int add_private_key(SSL_CTX *ssl_ctx, SSLObjLoader *ssl_obj)
{
    int ret = SSL_OK;

    /* get the private key details */
    if (asn1_get_private_key(ssl_obj->buf, ssl_obj->len, &ssl_ctx->rsa_ctx))
    {
        ret = SSL_ERROR_INVALID_KEY;
        goto error;
    }

error:
    return ret;
}

/** 
 * Increment the read sequence number (as a 64 bit endian indepenent #)
 */     
static void increment_read_sequence(SSL *ssl)
{
    int i;

    for (i = 7; i >= 0; i--) 
    {       
        if (++ssl->read_sequence[i])
            break;
    }
}
            
/**
 * Increment the read sequence number (as a 64 bit endian indepenent #)
 */      
static void increment_write_sequence(SSL *ssl)
{        
    int i;                  
         
    for (i = 7; i >= 0; i--)
    {                       
        if (++ssl->write_sequence[i])
            break;
    }                       
}

/**
 * Work out the HMAC digest in a packet.
 */
static void add_hmac_digest(SSL *ssl, int mode, uint8_t *hmac_header,
        const uint8_t *buf, int buf_len, uint8_t *hmac_buf)
{
    int hmac_len = buf_len + 8 + SSL_RECORD_SIZE;
    uint8_t *t_buf = (uint8_t *)alloca(hmac_len+10);

    memcpy(t_buf, (mode == SSL_SERVER_WRITE || mode == SSL_CLIENT_WRITE) ? 
                    ssl->write_sequence : ssl->read_sequence, 8);
    memcpy(&t_buf[8], hmac_header, SSL_RECORD_SIZE);
    memcpy(&t_buf[8+SSL_RECORD_SIZE], buf, buf_len);

    ssl->cipher_info->hmac(t_buf, hmac_len, 
            (mode == SSL_SERVER_WRITE || mode == SSL_CLIENT_READ) ? 
                ssl->server_mac : ssl->client_mac, 
            ssl->cipher_info->digest_size, hmac_buf);

#if 0
    print_blob("record", ssl->hmac_tx, SSL_RECORD_SIZE);
    print_blob("buf", buf, buf_len);
    if (mode == SSL_SERVER_WRITE || mode == SSL_CLIENT_WRITE)
    {
        print_blob("write seq", ssl->write_sequence, 8);
    }
    else
    {
        print_blob("read seq", ssl->read_sequence, 8);
    }

    if (mode == SSL_SERVER_WRITE || mode == SSL_CLIENT_READ)
    {
        print_blob("server mac", 
                ssl->server_mac, ssl->cipher_info->digest_size);
    }
    else
    {
        print_blob("client mac", 
                ssl->client_mac, ssl->cipher_info->digest_size);
    }
    print_blob("hmac", hmac_buf, SHA1_SIZE);
#endif
}

/**
 * Verify that the digest of a packet is correct.
 */
static int verify_digest(SSL *ssl, int mode, const uint8_t *buf, int read_len)
{   
    uint8_t hmac_buf[SHA1_SIZE];
    int hmac_offset;
   
    if (ssl->cipher_info->padding_size)
    {
        int last_blk_size = buf[read_len-1], i;
        hmac_offset = read_len-last_blk_size-ssl->cipher_info->digest_size-1;

        /* guard against a timing attack - make sure we do the digest */
        if (hmac_offset < 0)
        {
            hmac_offset = 0;
        }
        else
        {
            /* already looked at last byte */
            for (i = 1; i < last_blk_size; i++)
            {
                if (buf[read_len-i] != last_blk_size)
                {
                    hmac_offset = 0;
                    break;
                }
            }
        }
    }
    else /* stream cipher */
    {
        hmac_offset = read_len - ssl->cipher_info->digest_size;

        if (hmac_offset < 0)
        {
            hmac_offset = 0;
        }
    }

    /* sanity check the offset */
    ssl->hmac_header[3] = hmac_offset >> 8;      /* insert size */
    ssl->hmac_header[4] = hmac_offset & 0xff;
    add_hmac_digest(ssl, mode, ssl->hmac_header, buf, hmac_offset, hmac_buf);

    if (memcmp(hmac_buf, &buf[hmac_offset], ssl->cipher_info->digest_size))
    {
        return SSL_ERROR_INVALID_HMAC;
    }

    return hmac_offset;
}

/**
 * Add a packet to the end of our sent and received packets, so that we may use
 * it to calculate the hash at the end.
 */
void add_packet(SSL *ssl, const uint8_t *pkt, int len)
{
    MD5_Update(&ssl->dc->md5_ctx, pkt, len);
    SHA1_Update(&ssl->dc->sha1_ctx, pkt, len);
}

/**
 * Work out the MD5 PRF.
 */
static void p_hash_md5(const uint8_t *sec, int sec_len, 
        uint8_t *seed, int seed_len, uint8_t *out, int olen)
{
    uint8_t a1[128];

    /* A(1) */
    hmac_md5(seed, seed_len, sec, sec_len, a1);
    memcpy(&a1[MD5_SIZE], seed, seed_len);
    hmac_md5(a1, MD5_SIZE+seed_len, sec, sec_len, out);

    while (olen > MD5_SIZE)
    {
        uint8_t a2[MD5_SIZE];
        out += MD5_SIZE;
        olen -= MD5_SIZE;

        /* A(N) */
        hmac_md5(a1, MD5_SIZE, sec, sec_len, a2);
        memcpy(a1, a2, MD5_SIZE);

        /* work out the actual hash */
        hmac_md5(a1, MD5_SIZE+seed_len, sec, sec_len, out);
    }
}

/**
 * Work out the SHA1 PRF.
 */
static void p_hash_sha1(const uint8_t *sec, int sec_len, 
        uint8_t *seed, int seed_len, uint8_t *out, int olen)
{
    uint8_t a1[128];

    /* A(1) */
    hmac_sha1(seed, seed_len, sec, sec_len, a1);
    memcpy(&a1[SHA1_SIZE], seed, seed_len);
    hmac_sha1(a1, SHA1_SIZE+seed_len, sec, sec_len, out);

    while (olen > SHA1_SIZE)
    {
        uint8_t a2[SHA1_SIZE];
        out += SHA1_SIZE;
        olen -= SHA1_SIZE;

        /* A(N) */
        hmac_sha1(a1, SHA1_SIZE, sec, sec_len, a2);
        memcpy(a1, a2, SHA1_SIZE);

        /* work out the actual hash */
        hmac_sha1(a1, SHA1_SIZE+seed_len, sec, sec_len, out);
    }
}

/**
 * Work out the PRF.
 */
static void prf(const uint8_t *sec, int sec_len, uint8_t *seed, int seed_len,
        uint8_t *out, int olen)
{
    int len, i;
    const uint8_t *S1, *S2;
    uint8_t xbuf[256]; /* needs to be > the amount of key data */
    uint8_t ybuf[256]; /* needs to be > the amount of key data */

    len = sec_len/2;
    S1 = sec;
    S2 = &sec[len];
    len += (sec_len & 1); /* add for odd, make longer */

    p_hash_md5(S1, len, seed, seed_len, xbuf, olen);
    p_hash_sha1(S2, len, seed, seed_len, ybuf, olen);

    for (i = 0; i < olen; i++)
        out[i] = xbuf[i] ^ ybuf[i];
}

/**
 * Generate a master secret based on the client/server random data and the
 * premaster secret.
 */
void generate_master_secret(SSL *ssl, const uint8_t *premaster_secret)
{
    uint8_t buf[128];   /* needs to be > 13+32+32 in size */
    memcpy(buf, "master secret", 13);
    memcpy(&buf[13], ssl->dc->client_random, SSL_RANDOM_SIZE);
    memcpy(&buf[45], ssl->dc->server_random, SSL_RANDOM_SIZE);
    prf(premaster_secret, SSL_SECRET_SIZE, buf, 77, ssl->dc->master_secret,
            SSL_SECRET_SIZE);
}

/**
 * Generate a 'random' blob of data used for the generation of keys.
 */
static void generate_key_block(uint8_t *client_random, uint8_t *server_random,
        uint8_t *master_secret, uint8_t *key_block, int key_block_size)
{
    uint8_t buf[128];
    memcpy(buf, "key expansion", 13);
    memcpy(&buf[13], server_random, SSL_RANDOM_SIZE);
    memcpy(&buf[45], client_random, SSL_RANDOM_SIZE);
    prf(master_secret, SSL_SECRET_SIZE, buf, 77, key_block, key_block_size);
}

/** 
 * Calculate the digest used in the finished message. This function also
 * doubles up as a certificate verify function.
 */
void finished_digest(SSL *ssl, const char *label, uint8_t *digest)
{
    uint8_t mac_buf[128]; 
    uint8_t *q = mac_buf;
    MD5_CTX md5_ctx = ssl->dc->md5_ctx;
    SHA1_CTX sha1_ctx = ssl->dc->sha1_ctx;

    if (label)
    {
        memcpy(q, label, (int)strlen(label));
        q += strlen(label);
    }

    MD5_Final(q, &md5_ctx);
    q += MD5_SIZE;
    
    SHA1_Final(q, &sha1_ctx);
    q += SHA1_SIZE;

    if (label)
    {
        prf(ssl->dc->master_secret, SSL_SECRET_SIZE, mac_buf, (int)(q-mac_buf),
            digest, SSL_FINISHED_HASH_SIZE);
    }
    else    /* for use in a certificate verify */
    {
        memcpy(digest, mac_buf, MD5_SIZE + SHA1_SIZE);
    }

#if 0
    printf("label: %s\n", label);
    print_blob("master secret", ssl->dc->master_secret, 48);
    print_blob("mac_buf", mac_buf, q-mac_buf);
    print_blob("finished digest", digest, SSL_FINISHED_HASH_SIZE);
#endif
}   
    
/**
 * Retrieve (and initialise) the context of a cipher.
 */
static void *crypt_new(SSL *ssl, uint8_t *key, uint8_t *iv, int is_decrypt)
{
    switch (ssl->cipher)
    {
#ifndef CONFIG_SSL_SKELETON_MODE
        case SSL_AES128_SHA:
            {
                AES_CTX *aes_ctx = (AES_CTX *)malloc(sizeof(AES_CTX));
                AES_set_key(aes_ctx, key, iv, AES_MODE_128);

                if (is_decrypt)
                {
                    AES_convert_key(aes_ctx);
                }

                return (void *)aes_ctx;
            }

        case SSL_AES256_SHA:
            {
                AES_CTX *aes_ctx = (AES_CTX *)malloc(sizeof(AES_CTX));
                AES_set_key(aes_ctx, key, iv, AES_MODE_256);

                if (is_decrypt)
                {
                    AES_convert_key(aes_ctx);
                }

                return (void *)aes_ctx;
            }

        case SSL_RC4_128_MD5:
#endif
        case SSL_RC4_128_SHA:
            {
                RC4_CTX *rc4_ctx = (RC4_CTX *)malloc(sizeof(RC4_CTX));
                RC4_setup(rc4_ctx, key, 16);
                return (void *)rc4_ctx;
            }
    }

    return NULL;    /* its all gone wrong */
}

/**
 * Send a packet over the socket.
 */
static int send_raw_packet(SSL *ssl, uint8_t protocol)
{
    uint8_t *rec_buf = ssl->bm_all_data;
    int pkt_size = SSL_RECORD_SIZE+ssl->bm_index;
    int sent = 0;
    int ret = SSL_OK;

    rec_buf[0] = protocol;
    rec_buf[1] = 0x03;      /* version = 3.1 or higher */
    rec_buf[2] = ssl->version & 0x0f;
    rec_buf[3] = ssl->bm_index >> 8;
    rec_buf[4] = ssl->bm_index & 0xff;

    DISPLAY_BYTES(ssl, "sending %d bytes", ssl->bm_all_data, 
                             pkt_size, pkt_size);

    while (sent < pkt_size)
    {
        ret = SOCKET_WRITE(ssl->client_fd, 
                        &ssl->bm_all_data[sent], pkt_size-sent);

        if (ret >= 0)
            sent += ret;
        else
        {
            /*ret = SSL_ERROR_CONN_LOST*/;
            break;
        }

    }

    SET_SSL_FLAG(SSL_NEED_RECORD);  /* reset for next time */
    ssl->bm_index = 0;

    if (protocol != PT_APP_PROTOCOL_DATA)  
    {
        /* always return SSL_OK during handshake */   
        ret = SSL_OK;
    }

    return ret;
}

/**
 * Send an encrypted packet with padding bytes if necessary.
 */
int send_packet(SSL *ssl, uint8_t protocol, const uint8_t *in, int length)
{
    int ret, msg_length = 0;

    /* if our state is bad, don't bother */
    if (ssl->hs_status == SSL_ERROR_DEAD)
        return SSL_ERROR_CONN_LOST;

    if (in) /* has the buffer already been initialised? */
    {
        memcpy(ssl->bm_data, in, length);
    }

    msg_length += length;

    if (IS_SET_SSL_FLAG(SSL_TX_ENCRYPTED))
    {
        int mode = IS_SET_SSL_FLAG(SSL_IS_CLIENT) ? 
                            SSL_CLIENT_WRITE : SSL_SERVER_WRITE;
        uint8_t hmac_header[SSL_RECORD_SIZE]; /* GBG: modified intializer */
        hmac_header[0] = protocol;
        hmac_header[1] = 0x03; /* version = 3.1 or higher */
        hmac_header[2] = ssl->version & 0x0f;
        hmac_header[3] = msg_length >> 8;
        hmac_header[4] = msg_length & 0xff;

        if (protocol == PT_HANDSHAKE_PROTOCOL)
        {
            DISPLAY_STATE(ssl, 1, ssl->bm_data[0], 0);

            if (ssl->bm_data[0] != HS_HELLO_REQUEST)
            {
                add_packet(ssl, ssl->bm_data, msg_length);
            }
        }

        /* add the packet digest */
        add_hmac_digest(ssl, mode, hmac_header, ssl->bm_data, msg_length, 
                                                &ssl->bm_data[msg_length]);
        msg_length += ssl->cipher_info->digest_size;

        /* add padding? */
        if (ssl->cipher_info->padding_size)
        {
            int last_blk_size = msg_length%ssl->cipher_info->padding_size;
            int pad_bytes = ssl->cipher_info->padding_size - last_blk_size;

            /* ensure we always have at least 1 padding byte */
            if (pad_bytes == 0)
                pad_bytes += ssl->cipher_info->padding_size;

            memset(&ssl->bm_data[msg_length], pad_bytes-1, pad_bytes);
            msg_length += pad_bytes;
        }

        DISPLAY_BYTES(ssl, "unencrypted write", ssl->bm_data, msg_length);
        increment_write_sequence(ssl);

        /* add the explicit IV for TLS1.1 */
        if (ssl->version >= SSL_PROTOCOL_VERSION1_1 &&
                        ssl->cipher_info->iv_size)
                        
        {
            uint8_t iv_size = ssl->cipher_info->iv_size;
            uint8_t *t_buf = alloca(msg_length + iv_size);
            memcpy(t_buf + iv_size, ssl->bm_data, msg_length);
            get_random(iv_size, t_buf);
            msg_length += iv_size;
            memcpy(ssl->bm_data, t_buf, msg_length);
        }

        /* now encrypt the packet */
        ssl->cipher_info->encrypt(ssl->encrypt_ctx, ssl->bm_data, 
                                            ssl->bm_data, msg_length);
    }
    else if (protocol == PT_HANDSHAKE_PROTOCOL)
    {
        DISPLAY_STATE(ssl, 1, ssl->bm_data[0], 0);

        if (ssl->bm_data[0] != HS_HELLO_REQUEST)
        {
            add_packet(ssl, ssl->bm_data, length);
        }
    }

    ssl->bm_index = msg_length;
    if ((ret = send_raw_packet(ssl, protocol)) <= 0)
        return ret;

    return length;  /* just return what we wanted to send */
}

/**
 * Work out the cipher keys we are going to use for this session based on the
 * master secret.
 */
static int set_key_block(SSL *ssl, int is_write)
{
    const cipher_info_t *ciph_info = get_cipher_info(ssl->cipher);
    uint8_t *q;
    uint8_t client_key[32], server_key[32]; /* big enough for AES256 */
    uint8_t client_iv[16], server_iv[16];   /* big enough for AES128/256 */
    int is_client = IS_SET_SSL_FLAG(SSL_IS_CLIENT);

    if (ciph_info == NULL)
        return -1;

    /* only do once in a handshake */
    if (ssl->dc->key_block == NULL)
    {
        ssl->dc->key_block = (uint8_t *)malloc(ciph_info->key_block_size);

#if 0
        print_blob("client", ssl->dc->client_random, 32);
        print_blob("server", ssl->dc->server_random, 32);
        print_blob("master", ssl->dc->master_secret, SSL_SECRET_SIZE);
#endif
        generate_key_block(ssl->dc->client_random, ssl->dc->server_random,
            ssl->dc->master_secret, ssl->dc->key_block, 
            ciph_info->key_block_size);
#if 0
        print_blob("keyblock", ssl->key_block, ciph_info->key_block_size);
#endif
    }

    q = ssl->dc->key_block;

    if ((is_client && is_write) || (!is_client && !is_write))
    {
        memcpy(ssl->client_mac, q, ciph_info->digest_size);
    }

    q += ciph_info->digest_size;

    if ((!is_client && is_write) || (is_client && !is_write))
    {
        memcpy(ssl->server_mac, q, ciph_info->digest_size);
    }

    q += ciph_info->digest_size;
    memcpy(client_key, q, ciph_info->key_size);
    q += ciph_info->key_size;
    memcpy(server_key, q, ciph_info->key_size);
    q += ciph_info->key_size;

#ifndef CONFIG_SSL_SKELETON_MODE 
    if (ciph_info->iv_size)    /* RC4 has no IV, AES does */
    {
        memcpy(client_iv, q, ciph_info->iv_size);
        q += ciph_info->iv_size;
        memcpy(server_iv, q, ciph_info->iv_size);
        q += ciph_info->iv_size;
    }
#endif

    free(is_write ? ssl->encrypt_ctx : ssl->decrypt_ctx);

    /* now initialise the ciphers */
    if (is_client)
    {
        finished_digest(ssl, server_finished, ssl->dc->final_finish_mac);

        if (is_write)
            ssl->encrypt_ctx = crypt_new(ssl, client_key, client_iv, 0);
        else
            ssl->decrypt_ctx = crypt_new(ssl, server_key, server_iv, 1);
    }
    else
    {
        finished_digest(ssl, client_finished, ssl->dc->final_finish_mac);

        if (is_write)
            ssl->encrypt_ctx = crypt_new(ssl, server_key, server_iv, 0);
        else
            ssl->decrypt_ctx = crypt_new(ssl, client_key, client_iv, 1);
    }

    ssl->cipher_info = ciph_info;
    return 0;
}

/**
 * Read the SSL connection.
 */
int basic_read(SSL *ssl, uint8_t **in_data)
{
    int ret = SSL_OK;
    int read_len, is_client = IS_SET_SSL_FLAG(SSL_IS_CLIENT);
    uint8_t *buf = ssl->bm_data;

    read_len = SOCKET_READ(ssl->client_fd, &buf[ssl->bm_read_index], 
                            ssl->need_bytes-ssl->got_bytes);

    /* connection has gone, so die */
    if (read_len <= 0)
    {
        ret = read_len;
        ssl->hs_status = SSL_ERROR_DEAD;  /* make sure it stays dead */
        goto error;
    }

    DISPLAY_BYTES(ssl, "received %d bytes", 
            &ssl->bm_data[ssl->bm_read_index], read_len, read_len);

    ssl->got_bytes += read_len;
    ssl->bm_read_index += read_len;

    /* haven't quite got what we want, so try again later */
    if (ssl->got_bytes < ssl->need_bytes)
        return SSL_OK;

    read_len = ssl->got_bytes;
    ssl->got_bytes = 0;

    if (IS_SET_SSL_FLAG(SSL_NEED_RECORD))
    {
        /* check for sslv2 "client hello" */
        if (buf[0] & 0x80 && buf[2] == 1)
        {
#ifdef CONFIG_SSL_ENABLE_V23_HANDSHAKE
            uint8_t version = (buf[3] << 4) + buf[4];
            DISPLAY_BYTES(ssl, "ssl2 record", buf, 5);

            /* should be v3.1 (TLSv1) or better  */
            ssl->version = ssl->client_version = version;

            if (version > SSL_PROTOCOL_VERSION_MAX)
            {
                /* use client's version */
                ssl->version = SSL_PROTOCOL_VERSION_MAX;
            }
            else if (version < SSL_PROTOCOL_MIN_VERSION)  
            {
                ret = SSL_ERROR_INVALID_VERSION;
                ssl_display_error(ret);
                return ret;
            }

            add_packet(ssl, &buf[2], 3);
            ret = process_sslv23_client_hello(ssl); 
#else
            printf("Error: no SSLv23 handshaking allowed\n"); TTY_FLUSH();
            ret = SSL_ERROR_NOT_SUPPORTED;
#endif
            goto error; /* not an error - just get out of here */
        }

        /* GBG: check the header values */
        if ((buf[0] != PT_HANDSHAKE_PROTOCOL &&
             buf[0] != PT_CHANGE_CIPHER_SPEC &&
             buf[0] != PT_APP_PROTOCOL_DATA  &&
             buf[0] != PT_ALERT_PROTOCOL) ||
            (buf[1] != 3 /* version major */)) {
            ret = SSL_ERROR_INVALID_PROT_MSG;
            goto error;
        }

        ssl->need_bytes = (buf[3] << 8) + buf[4];

        /* do we violate the spec with the message size?  */
        if (ssl->need_bytes > RT_MAX_PLAIN_LENGTH+RT_EXTRA-BM_RECORD_OFFSET)
        {
            ret = SSL_ERROR_INVALID_PROT_MSG;              
            goto error;
        }

        CLR_SSL_FLAG(SSL_NEED_RECORD);
        memcpy(ssl->hmac_header, buf, 3);       /* store for hmac */
        ssl->record_type = buf[0];
        goto error;                         /* no error, we're done */
    }

    /* for next time - just do it now in case of an error */
    SET_SSL_FLAG(SSL_NEED_RECORD);
    ssl->need_bytes = SSL_RECORD_SIZE;

    /* decrypt if we need to */
    if (IS_SET_SSL_FLAG(SSL_RX_ENCRYPTED))
    {
        ssl->cipher_info->decrypt(ssl->decrypt_ctx, buf, buf, read_len);

        if (ssl->version >= SSL_PROTOCOL_VERSION1_1 &&
                        ssl->cipher_info->iv_size)
        {
            buf += ssl->cipher_info->iv_size;
            read_len -= ssl->cipher_info->iv_size;
        }

        read_len = verify_digest(ssl, 
                is_client ? SSL_CLIENT_READ : SSL_SERVER_READ, buf, read_len);

        /* does the hmac work? */
        if (read_len < 0)
        {
            ret = read_len;
            goto error;
        }

        DISPLAY_BYTES(ssl, "decrypted", buf, read_len);
        increment_read_sequence(ssl);
    }

    /* The main part of the SSL packet */
    switch (ssl->record_type)
    {
        case PT_HANDSHAKE_PROTOCOL:
            if (ssl->dc != NULL)
            {
                ssl->dc->bm_proc_index = 0;
                ret = do_handshake(ssl, buf, read_len);
            }
            else /* no client renegotiation allowed */
            {
                ret = SSL_ERROR_NO_CLIENT_RENOG;              
                goto error;
            }
            break;

        case PT_CHANGE_CIPHER_SPEC:
            if (ssl->next_state != HS_FINISHED)
            {
                ret = SSL_ERROR_INVALID_HANDSHAKE;
                goto error;
            }

            /* all encrypted from now on */
            SET_SSL_FLAG(SSL_RX_ENCRYPTED);
            if (set_key_block(ssl, 0) < 0)
            {
                ret = SSL_ERROR_INVALID_HANDSHAKE;
                goto error;
            }
            
            memset(ssl->read_sequence, 0, 8);
            break;

        case PT_APP_PROTOCOL_DATA:
            if (in_data)
            {
                *in_data = buf;   /* point to the work buffer */
                (*in_data)[read_len] = 0;  /* null terminate just in case */
            }

            ret = read_len;
            break;

        case PT_ALERT_PROTOCOL:
            /* return the alert # with alert bit set */
            if(buf[0] == SSL_ALERT_TYPE_WARNING &&
               buf[1] == SSL_ALERT_CLOSE_NOTIFY)
            {
              ret = SSL_CLOSE_NOTIFY;
              send_alert(ssl, SSL_ALERT_CLOSE_NOTIFY);
              SET_SSL_FLAG(SSL_SENT_CLOSE_NOTIFY);
            }
            else 
            {
                ret = -buf[1]; 
                DISPLAY_ALERT(ssl, buf[1]);
            }

            break;

        default:
            ret = SSL_ERROR_INVALID_PROT_MSG;
            break;
    }

error:
    ssl->bm_read_index = 0;          /* reset to go again */

    if (ret < SSL_OK && in_data)/* if all wrong, then clear this buffer ptr */
        *in_data = NULL;

    return ret;
}

/**
 * Do some basic checking of data and then perform the appropriate handshaking.
 */
static int do_handshake(SSL *ssl, uint8_t *buf, int read_len)
{
    int hs_len = (buf[2]<<8) + buf[3];
    uint8_t handshake_type = buf[0];
    int ret = SSL_OK;
    int is_client = IS_SET_SSL_FLAG(SSL_IS_CLIENT);

    /* some integrity checking on the handshake */
    PARANOIA_CHECK(read_len-SSL_HS_HDR_SIZE, hs_len);

    if (handshake_type != ssl->next_state)
    {
        /* handle a special case on the client */
        if (!is_client || handshake_type != HS_CERT_REQ ||
                        ssl->next_state != HS_SERVER_HELLO_DONE)
        {
            ret = SSL_ERROR_INVALID_HANDSHAKE;
            goto error;
        }
    }

    hs_len += SSL_HS_HDR_SIZE;  /* adjust for when adding packets */
    ssl->bm_index = hs_len;     /* store the size and check later */
    DISPLAY_STATE(ssl, 0, handshake_type, 0);

    if (handshake_type != HS_CERT_VERIFY && handshake_type != HS_HELLO_REQUEST)
        add_packet(ssl, buf, hs_len); 

#if defined(CONFIG_SSL_ENABLE_CLIENT)
    ret = is_client ? 
        do_clnt_handshake(ssl, handshake_type, buf, hs_len) :
        do_svr_handshake(ssl, handshake_type, buf, hs_len);
#else
    ret = do_svr_handshake(ssl, handshake_type, buf, hs_len);
#endif

    /* just use recursion to get the rest */
    if (hs_len < read_len && ret == SSL_OK)
        ret = do_handshake(ssl, &buf[hs_len], read_len-hs_len);

error:
    return ret;
}

/**
 * Sends the change cipher spec message. We have just read a finished message
 * from the client.
 */
int send_change_cipher_spec(SSL *ssl)
{
    int ret = send_packet(ssl, PT_CHANGE_CIPHER_SPEC, 
            g_chg_cipher_spec_pkt, sizeof(g_chg_cipher_spec_pkt));
    SET_SSL_FLAG(SSL_TX_ENCRYPTED);

    if (ret >= 0 && set_key_block(ssl, 1) < 0)
        ret = SSL_ERROR_INVALID_HANDSHAKE;

    memset(ssl->write_sequence, 0, 8);
    return ret;
}

/**
 * Send a "finished" message
 */
int send_finished(SSL *ssl)
{
    uint8_t buf[SSL_FINISHED_HASH_SIZE+4] = {
        HS_FINISHED, 0, 0, SSL_FINISHED_HASH_SIZE };

    /* now add the finished digest mac (12 bytes) */
    finished_digest(ssl, 
        IS_SET_SSL_FLAG(SSL_IS_CLIENT) ?
                    client_finished : server_finished, &buf[4]);

#ifndef CONFIG_SSL_SKELETON_MODE
    /* store in the session cache */
    if (!IS_SET_SSL_FLAG(SSL_SESSION_RESUME) && ssl->ssl_ctx->num_sessions)
    {
        memcpy(ssl->session->master_secret,
                ssl->dc->master_secret, SSL_SECRET_SIZE);
    }
#endif

    return send_packet(ssl, PT_HANDSHAKE_PROTOCOL,
                                buf, SSL_FINISHED_HASH_SIZE+4);
}

/**
 * Send an alert message.
 * Return 1 if the alert was an "error".
 */
int send_alert(SSL *ssl, int error_code)
{
    int alert_num = 0;
    int is_warning = 0;
    uint8_t buf[2];

    /* Don't bother we're already dead */
    if (ssl->hs_status == SSL_ERROR_DEAD)
    {
        return SSL_ERROR_CONN_LOST;
    }

#ifdef CONFIG_SSL_FULL_MODE
    if (IS_SET_SSL_FLAG(SSL_DISPLAY_STATES))
        ssl_display_error(error_code);
#endif

    switch (error_code)
    {
        case SSL_ALERT_CLOSE_NOTIFY:
            is_warning = 1;
            alert_num = SSL_ALERT_CLOSE_NOTIFY;
            break;

        case SSL_ERROR_CONN_LOST:       /* don't send alert just yet */
            is_warning = 1;
            break;

        case SSL_ERROR_INVALID_HANDSHAKE:
        case SSL_ERROR_INVALID_PROT_MSG:
            alert_num = SSL_ALERT_HANDSHAKE_FAILURE;
            break;

        case SSL_ERROR_INVALID_HMAC:
        case SSL_ERROR_FINISHED_INVALID:
            alert_num = SSL_ALERT_BAD_RECORD_MAC;
            break;

        case SSL_ERROR_INVALID_VERSION:
            alert_num = SSL_ALERT_INVALID_VERSION;
            break;

        case SSL_ERROR_INVALID_SESSION:
        case SSL_ERROR_NO_CIPHER:
        case SSL_ERROR_INVALID_KEY:
            alert_num = SSL_ALERT_ILLEGAL_PARAMETER;
            break;

        case SSL_ERROR_BAD_CERTIFICATE:
            alert_num = SSL_ALERT_BAD_CERTIFICATE;
            break;

        case SSL_ERROR_NO_CLIENT_RENOG:
            alert_num = SSL_ALERT_NO_RENEGOTIATION;
            break;

        default:
            /* a catch-all for any badly verified certificates */
            alert_num = (error_code <= SSL_X509_OFFSET) ?  
                SSL_ALERT_BAD_CERTIFICATE : SSL_ALERT_UNEXPECTED_MESSAGE;
            break;
    }

    buf[0] = is_warning ? 1 : 2;
    buf[1] = alert_num;
    send_packet(ssl, PT_ALERT_PROTOCOL, buf, sizeof(buf));
    DISPLAY_ALERT(ssl, alert_num);
    return is_warning ? 0 : 1;
}

/**
 * Process a client finished message.
 */
int process_finished(SSL *ssl, uint8_t *buf, int hs_len)
{
    int ret = SSL_OK;
    int is_client = IS_SET_SSL_FLAG(SSL_IS_CLIENT);
    int resume = IS_SET_SSL_FLAG(SSL_SESSION_RESUME);
    (void)hs_len; /* GBG: unused */
    
    PARANOIA_CHECK(ssl->bm_index, SSL_FINISHED_HASH_SIZE+4);

    /* check that we all work before we continue */
    if (memcmp(ssl->dc->final_finish_mac, &buf[4], SSL_FINISHED_HASH_SIZE))
        return SSL_ERROR_FINISHED_INVALID;

    if ((!is_client && !resume) || (is_client && resume))
    {
        if ((ret = send_change_cipher_spec(ssl)) == SSL_OK)
            ret = send_finished(ssl);
    }

    /* if we ever renegotiate */
    ssl->next_state = is_client ? HS_HELLO_REQUEST : HS_CLIENT_HELLO;  
    ssl->hs_status = ret;  /* set the final handshake status */

error:
    return ret;
}

/**
 * Send a certificate.
 */
int send_certificate(SSL *ssl)
{
    uint8_t *buf = ssl->bm_data;
    int offset = 7;
    int chain_length;
    SSL_CERT* cert = ssl->ssl_ctx->certs; /* GBG: added */

    buf[0] = HS_CERTIFICATE;
    buf[1] = 0;
    buf[4] = 0;

    while (cert)
    {
        buf[offset++] = 0;        
        buf[offset++] = cert->size >> 8;        /* cert 1 length */
        buf[offset++] = cert->size & 0xff;
        memcpy(&buf[offset], cert->buf, cert->size);
        offset += cert->size;
        cert = cert->next;
    }

    chain_length = offset - 7;
    buf[5] = chain_length >> 8;        /* cert chain length */
    buf[6] = chain_length & 0xff;
    chain_length += 3;
    buf[2] = chain_length >> 8;        /* handshake length */
    buf[3] = chain_length & 0xff;
    ssl->bm_index = offset;
    return send_packet(ssl, PT_HANDSHAKE_PROTOCOL, NULL, offset);
}

/**
 * Create a blob of memory that we'll get rid of once the handshake is
 * complete.
 */
void disposable_new(SSL *ssl)
{
    if (ssl->dc == NULL)
    {
        ssl->dc = (DISPOSABLE_CTX *)calloc(1, sizeof(DISPOSABLE_CTX));
        MD5_Init(&ssl->dc->md5_ctx);
        SHA1_Init(&ssl->dc->sha1_ctx);
    }
}

/**
 * Remove the temporary blob of memory.
 */
void disposable_free(SSL *ssl)
{
    if (ssl->dc)
    {
        free(ssl->dc->key_block);
        memset(ssl->dc, 0, sizeof(DISPOSABLE_CTX));
        free(ssl->dc);
        ssl->dc = NULL;
    }

}

#ifndef CONFIG_SSL_SKELETON_MODE     /* no session resumption in this mode */
/**
 * Find if an existing session has the same session id. If so, use the
 * master secret from this session for session resumption.
 */
SSL_SESSION *ssl_session_update(int max_sessions, SSL_SESSION *ssl_sessions[], 
        SSL *ssl, const uint8_t *session_id)
{
    time_t tm = time(NULL);
    time_t oldest_sess_time = tm;
    SSL_SESSION *oldest_sess = NULL;
    int i;

    /* no sessions? Then bail */
    if (max_sessions == 0)
        return NULL;

    SSL_CTX_LOCK(ssl->ssl_ctx->mutex);
    if (session_id)
    {
        for (i = 0; i < max_sessions; i++)
        {
            if (ssl_sessions[i])
            {
                /* kill off any expired sessions */
                if (tm > ssl_sessions[i]->conn_time + SSL_EXPIRY_TIME)
                {
                    session_free(ssl_sessions, i);
                    continue;
                }

                /* if the session id matches, it must still be less than 
                   the expiry time */
                if (memcmp(ssl_sessions[i]->session_id, session_id,
                                                SSL_SESSION_ID_SIZE) == 0)
                {
                    ssl->session_index = i;
                    memcpy(ssl->dc->master_secret, 
                            ssl_sessions[i]->master_secret, SSL_SECRET_SIZE);
                    SET_SSL_FLAG(SSL_SESSION_RESUME);
                    SSL_CTX_UNLOCK(ssl->ssl_ctx->mutex);
                    return ssl_sessions[i];  /* a session was found */
                }
            }
        }
    }

    /* If we've got here, no matching session was found - so create one */
    for (i = 0; i < max_sessions; i++)
    {
        if (ssl_sessions[i] == NULL)
        {
            /* perfect, this will do */
            ssl_sessions[i] = (SSL_SESSION *)calloc(1, sizeof(SSL_SESSION));
            ssl_sessions[i]->conn_time = tm;
            ssl->session_index = i;
            SSL_CTX_UNLOCK(ssl->ssl_ctx->mutex);
            return ssl_sessions[i]; /* return the session object */
        }
        else if (ssl_sessions[i]->conn_time <= oldest_sess_time)
        {
            /* find the oldest session */
            oldest_sess_time = ssl_sessions[i]->conn_time;
            oldest_sess = ssl_sessions[i];
            ssl->session_index = i;
        }
    }

    /* ok, we've used up all of our sessions. So blow the oldest session away */
    if (oldest_sess != NULL)
    {
        oldest_sess->conn_time = tm;
        memset(oldest_sess->session_id, 0, sizeof(SSL_SESSION_ID_SIZE));
        memset(oldest_sess->master_secret, 0, sizeof(SSL_SECRET_SIZE));
    }

    SSL_CTX_UNLOCK(ssl->ssl_ctx->mutex);
    return oldest_sess;
}

/**
 * Free an existing session.
 */
static void session_free(SSL_SESSION *ssl_sessions[], int sess_index)
{
    if (ssl_sessions[sess_index])
    {
        free(ssl_sessions[sess_index]);
        ssl_sessions[sess_index] = NULL;
    }
}

/**
 * This ssl object doesn't want this session anymore.
 */
void kill_ssl_session(SSL_SESSION **ssl_sessions, SSL *ssl)
{
    SSL_CTX_LOCK(ssl->ssl_ctx->mutex);

    if (ssl->ssl_ctx->num_sessions)
    {
        session_free(ssl_sessions, ssl->session_index);
        ssl->session = NULL;
    }

    SSL_CTX_UNLOCK(ssl->ssl_ctx->mutex);
}
#endif /* CONFIG_SSL_SKELETON_MODE */

/*
 * Get the session id for a handshake. This will be a 32 byte sequence.
 */
EXP_FUNC const uint8_t * STDCALL ssl_get_session_id(const SSL *ssl)
{
    return ssl->session_id;
}

/*
 * Get the session id size for a handshake. 
 */
EXP_FUNC uint8_t STDCALL ssl_get_session_id_size(const SSL *ssl)
{
    return ssl->sess_id_size;
}

/*
 * Return the cipher id (in the SSL form).
 */
EXP_FUNC uint8_t STDCALL ssl_get_cipher_id(const SSL *ssl)
{
    return ssl->cipher;
}

/*
 * Return the status of the handshake.
 */
EXP_FUNC int STDCALL ssl_handshake_status(const SSL *ssl)
{
    return ssl->hs_status;
}

#if 0 /* GBG: removed */
/*
 * Retrieve various parameters about the SSL engine.
 */
EXP_FUNC int STDCALL ssl_get_config(int offset)
{
    switch (offset)
    {
        /* return the appropriate build mode */
        case SSL_BUILD_MODE:
#if defined(CONFIG_SSL_FULL_MODE)
            return SSL_BUILD_FULL_MODE;
#elif defined(CONFIG_SSL_ENABLE_CLIENT)
            return SSL_BUILD_ENABLE_CLIENT;
#elif defined(CONFIG_ENABLE_VERIFICATION)
            return SSL_BUILD_ENABLE_VERIFICATION;
#elif defined(CONFIG_SSL_SERVER_ONLY )
            return SSL_BUILD_SERVER_ONLY;
#else 
            return SSL_BUILD_SKELETON_MODE;
#endif

#if 0 /* GBG: removed */
        case SSL_MAX_CERT_CFG_OFFSET:
            return CONFIG_SSL_MAX_CERTS;
#endif

#ifdef CONFIG_SSL_CERT_VERIFICATION
        case SSL_MAX_CA_CERT_CFG_OFFSET:
            return CONFIG_X509_MAX_CA_CERTS;
#endif
#ifdef CONFIG_SSL_HAS_PEM
        case SSL_HAS_PEM:
            return 1;
#endif
        default:
            return 0;
    }
}
#endif /* GBG */

#ifdef CONFIG_SSL_CERT_VERIFICATION
/**
 * Authenticate a received certificate.
 */
EXP_FUNC int STDCALL ssl_verify_cert(const SSL *ssl)
{
    int ret;
    SSL_CTX_LOCK(ssl->ssl_ctx->mutex);
    ret = x509_verify(ssl->ssl_ctx->ca_certs /* GBG: modified */, ssl->x509_ctx, NULL);
    SSL_CTX_UNLOCK(ssl->ssl_ctx->mutex);

    if (ret)        /* modify into an SSL error type */
    {
        ret = SSL_X509_ERROR(ret);
    }

    return ret;
}

/**
 * Process a certificate message.
 */
int process_certificate(SSL *ssl, X509_CTX **x509_ctx)
{
    int ret = SSL_OK;
    uint8_t *buf = &ssl->bm_data[ssl->dc->bm_proc_index];
    int pkt_size = ssl->bm_index;
    int cert_size, offset = 5;
    int total_cert_size = (buf[offset]<<8) + buf[offset+1];
    int is_client = IS_SET_SSL_FLAG(SSL_IS_CLIENT);
    X509_CTX **chain = x509_ctx;
    offset += 2;

    PARANOIA_CHECK(total_cert_size, offset);

    while (offset < total_cert_size)
    {
        offset++;       /* skip empty char */
        cert_size = (buf[offset]<<8) + buf[offset+1];
        offset += 2;
        
        if (x509_new(&buf[offset], NULL, chain))
        {
            ret = SSL_ERROR_BAD_CERTIFICATE;
            goto error;
        }

        chain = &((*chain)->next);
        offset += cert_size;
    }

    PARANOIA_CHECK(pkt_size, offset);

    /* GBG: modif: verify for server and client (was: if we are client we can do the verify now or later) */
    if (!IS_SET_SSL_FLAG(SSL_SERVER_VERIFY_LATER))
    {
        ret = ssl_verify_cert(ssl);
    }

    ssl->next_state = is_client ? HS_SERVER_HELLO_DONE : HS_CLIENT_KEY_XCHG;
    ssl->dc->bm_proc_index += offset;
error:
    return ret;
}

#endif /* CONFIG_SSL_CERT_VERIFICATION */

/**
 * Debugging routine to display SSL handshaking stuff.
 */
#ifdef CONFIG_SSL_FULL_MODE
/**
 * Debugging routine to display SSL states.
 */
void DISPLAY_STATE(SSL *ssl, int is_send, uint8_t state, int not_ok)
{
    const char *str;

    if (!IS_SET_SSL_FLAG(SSL_DISPLAY_STATES))
        return;

    printf(not_ok ? "Error - invalid State:\t" : "State:\t");
    printf(is_send ? "sending " : "receiving ");

    switch (state)
    {
        case HS_HELLO_REQUEST:
            str = "Hello Request (0)";
            break;

        case HS_CLIENT_HELLO:
            str = "Client Hello (1)";
            break;

        case HS_SERVER_HELLO:
            str = "Server Hello (2)";
            break;

        case HS_CERTIFICATE:
            str = "Certificate (11)";
            break;

        case HS_SERVER_KEY_XCHG:
            str = "Certificate Request (12)";
            break;

        case HS_CERT_REQ:
            str = "Certificate Request (13)";
            break;

        case HS_SERVER_HELLO_DONE:
            str = "Server Hello Done (14)";
            break;

        case HS_CERT_VERIFY:
            str = "Certificate Verify (15)";
            break;

        case HS_CLIENT_KEY_XCHG:
            str = "Client Key Exchange (16)";
            break;

        case HS_FINISHED:
            str = "Finished (16)";
            break;

        default:
            str = "Error (Unknown)";
            
            break;
    }

    printf("%s\n", str);
    TTY_FLUSH();
}

/**
 * Debugging routine to display RSA objects
 */
void DISPLAY_RSA(SSL *ssl, const RSA_CTX *rsa_ctx)
{
    if (!IS_SET_SSL_FLAG(SSL_DISPLAY_RSA))
        return;

    RSA_print(rsa_ctx);
    TTY_FLUSH();
}

/**
 * Debugging routine to display SSL handshaking bytes.
 */
void DISPLAY_BYTES(SSL *ssl, const char *format, 
        const uint8_t *data, int size, ...)
{
    va_list(ap);

    if (!IS_SET_SSL_FLAG(SSL_DISPLAY_BYTES))
        return;

    va_start(ap, size);
    print_blob(format, data, size, va_arg(ap, char *));
    va_end(ap);
    TTY_FLUSH();
}

/**
 * Debugging routine to display SSL handshaking errors.
 */
EXP_FUNC void STDCALL ssl_display_error(int error_code)
{
    if (error_code == SSL_OK)
        return;

    printf("Error: ");

    /* X509 error? */
    if (error_code < SSL_X509_OFFSET)
    {
        printf("%s\n", x509_display_error(error_code - SSL_X509_OFFSET));
        return;
    }

    /* SSL alert error code */
    if (error_code > SSL_ERROR_CONN_LOST)
    {
        printf("SSL error %d\n", -error_code);
        return;
    }

    switch (error_code)
    {
        case SSL_ERROR_DEAD:
            printf("connection dead");
            break;

        case SSL_ERROR_INVALID_HANDSHAKE:
            printf("invalid handshake");
            break;

        case SSL_ERROR_INVALID_PROT_MSG:
            printf("invalid protocol message");
            break;

        case SSL_ERROR_INVALID_HMAC:
            printf("invalid mac");
            break;

        case SSL_ERROR_INVALID_VERSION:
            printf("invalid version");
            break;

        case SSL_ERROR_INVALID_SESSION:
            printf("invalid session");
            break;

        case SSL_ERROR_NO_CIPHER:
            printf("no cipher");
            break;

        case SSL_ERROR_CONN_LOST:
            printf("connection lost");
            break;

        case SSL_ERROR_BAD_CERTIFICATE:
            printf("bad certificate");
            break;

        case SSL_ERROR_INVALID_KEY:
            printf("invalid key");
            break;

        case SSL_ERROR_FINISHED_INVALID:
            printf("finished invalid");
            break;

        case SSL_ERROR_NO_CERT_DEFINED:
            printf("no certificate defined");
            break;

        case SSL_ERROR_NO_CLIENT_RENOG:
            printf("client renegotiation not supported");
            break;
            
        case SSL_ERROR_NOT_SUPPORTED:
            printf("Option not supported");
            break;

        default:
            printf("undefined as yet - %d", error_code);
            break;
    }

    printf("\n");
    TTY_FLUSH();
}

/**
 * Debugging routine to display alerts.
 */
void DISPLAY_ALERT(SSL *ssl, int alert)
{
    if (!IS_SET_SSL_FLAG(SSL_DISPLAY_STATES))
        return;

    printf("Alert: ");

    switch (alert)
    {
        case SSL_ALERT_CLOSE_NOTIFY:
            printf("close notify");
            break;

        case SSL_ALERT_INVALID_VERSION:
            printf("invalid version");
            break;

        case SSL_ALERT_BAD_CERTIFICATE:
            printf("bad certificate");
            break;

        case SSL_ALERT_UNEXPECTED_MESSAGE:
            printf("unexpected message");
            break;

        case SSL_ALERT_BAD_RECORD_MAC:
            printf("bad record mac");
            break;

        case SSL_ALERT_HANDSHAKE_FAILURE:
            printf("handshake failure");
            break;

        case SSL_ALERT_ILLEGAL_PARAMETER:
            printf("illegal parameter");
            break;

        case SSL_ALERT_DECODE_ERROR:
            printf("decode error");
            break;

        case SSL_ALERT_DECRYPT_ERROR:
            printf("decrypt error");
            break;

        case SSL_ALERT_NO_RENEGOTIATION:
            printf("no renegotiation");
            break;

        default:
            printf("alert - (unknown %d)", alert);
            break;
    }

    printf("\n");
    TTY_FLUSH();
}

#endif /* CONFIG_SSL_FULL_MODE */

/**
 * Return the version of this library.
 */
EXP_FUNC const char  * STDCALL ssl_version()
{
    static const char * axtls_version = AXTLS_VERSION;
    return axtls_version;
}

EXP_FUNC void ssl_mem_free(void* mem) /* GBG */
{
    if (mem) free(mem);
}

/**
 * Enable the various language bindings to work regardless of the
 * configuration - they just return an error statement and a bad return code.
 */
#if !defined(CONFIG_SSL_FULL_MODE)
EXP_FUNC void STDCALL ssl_display_error(int error_code) { (void)error_code; }
#endif

#ifdef CONFIG_BINDINGS
#if !defined(CONFIG_SSL_ENABLE_CLIENT)
EXP_FUNC SSL * STDCALL ssl_client_new(SSL_CTX *ssl_ctx, int client_fd, const
        uint8_t *session_id, uint8_t sess_id_size)
{
    printf(unsupported_str);
    return NULL;
}
#endif

#if !defined(CONFIG_SSL_CERT_VERIFICATION)
EXP_FUNC int STDCALL ssl_verify_cert(const SSL *ssl)
{
    printf(unsupported_str);
    return -1;
}


EXP_FUNC const char * STDCALL ssl_get_cert_dn(const SSL *ssl, int component)
{
    printf(unsupported_str);
    return NULL;
}

EXP_FUNC const char * STDCALL ssl_get_cert_subject_alt_dnsname(const SSL *ssl, int index)
{
    printf(unsupported_str);
    return NULL;
}

#endif  /* CONFIG_SSL_CERT_VERIFICATION */

#endif /* CONFIG_BINDINGS */

