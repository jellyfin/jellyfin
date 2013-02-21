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

#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include "os_port.h"
#include "ssl.h"

static const uint8_t g_hello_done[] = { HS_SERVER_HELLO_DONE, 0, 0, 0 };

static int process_client_hello(SSL *ssl);
static int send_server_hello_sequence(SSL *ssl);
static int send_server_hello(SSL *ssl);
static int send_server_hello_done(SSL *ssl);
static int process_client_key_xchg(SSL *ssl);
#ifdef CONFIG_SSL_CERT_VERIFICATION
static int send_certificate_request(SSL *ssl);
static int process_cert_verify(SSL *ssl);
#endif

/*
 * Establish a new SSL connection to an SSL client.
 */
EXP_FUNC SSL * STDCALL ssl_server_new(SSL_CTX *ssl_ctx, SSL_SOCKET* client_fd)
{
    SSL *ssl;

    ssl = ssl_new(ssl_ctx, client_fd);
    ssl->next_state = HS_CLIENT_HELLO;

#ifdef CONFIG_SSL_FULL_MODE
    if (ssl_ctx->chain_length == 0)
        printf("Warning - no server certificate defined\n"); TTY_FLUSH();
#endif

    return ssl;
}

/*
 * Process the handshake record.
 */
int do_svr_handshake(SSL *ssl, int handshake_type, uint8_t *buf, int hs_len)
{
    int ret = SSL_OK;
    ssl->hs_status = SSL_NOT_OK;            /* not connected */

    /* To get here the state must be valid */
    switch (handshake_type)
    {
        case HS_CLIENT_HELLO:
            if ((ret = process_client_hello(ssl)) == SSL_OK)
                ret = send_server_hello_sequence(ssl);
            break;

#ifdef CONFIG_SSL_CERT_VERIFICATION
        case HS_CERTIFICATE:/* the client sends its cert */
            ret = process_certificate(ssl, &ssl->x509_ctx);

            /* GBG: removed (modified process_certificate to do the cert verification for both client and server
            if (ret == SSL_OK)    
            { 
                int cert_res;
                cert_res = x509_verify(
                        ssl->ssl_ctx->ca_cert_ctx, ssl->x509_ctx, NULL);
                ret = (cert_res == 0) ? SSL_OK : SSL_X509_ERROR(cert_res);
            }*/
            break;

        case HS_CERT_VERIFY:    
            ret = process_cert_verify(ssl);
            add_packet(ssl, buf, hs_len);   /* needs to be done after */
            break;
#endif
        case HS_CLIENT_KEY_XCHG:
            ret = process_client_key_xchg(ssl);
            break;

        case HS_FINISHED:
            ret = process_finished(ssl, buf, hs_len);
            disposable_free(ssl);   /* free up some memory */
            break;
    }

    return ret;
}

/* 
 * Process a client hello message.
 */
static int process_client_hello(SSL *ssl)
{
    uint8_t *buf = ssl->bm_data;
    uint8_t *record_buf = ssl->hmac_header;
    int pkt_size = ssl->bm_index;
    int i, j, cs_len, id_len, offset = 6 + SSL_RANDOM_SIZE;
    int ret = SSL_OK;
    
    uint8_t version = (record_buf[1] << 4) + record_buf[2];
    ssl->version = ssl->client_version = version;

    if (version > SSL_PROTOCOL_VERSION_MAX)
    {
        /* use client's version instead */
        ssl->version = SSL_PROTOCOL_VERSION_MAX; 
    }
    else if (version < SSL_PROTOCOL_MIN_VERSION)  /* old version supported? */
    {
        ret = SSL_ERROR_INVALID_VERSION;
        ssl_display_error(ret);
        goto error;
    }

    memcpy(ssl->dc->client_random, &buf[6], SSL_RANDOM_SIZE);

    /* process the session id */
    id_len = buf[offset++];
    if (id_len > SSL_SESSION_ID_SIZE)
    {
        return SSL_ERROR_INVALID_SESSION;
    }

#ifndef CONFIG_SSL_SKELETON_MODE
    ssl->session = ssl_session_update(ssl->ssl_ctx->num_sessions,
            ssl->ssl_ctx->ssl_sessions, ssl, id_len ? &buf[offset] : NULL);
#endif

    offset += id_len;
    cs_len = (buf[offset]<<8) + buf[offset+1];
    offset += 3;        /* add 1 due to all cipher suites being 8 bit */

    PARANOIA_CHECK(pkt_size, offset);

    /* work out what cipher suite we are going to use - client defines 
       the preference */
    for (i = 0; i < cs_len; i += 2)
    {
        for (j = 0; j < NUM_PROTOCOLS; j++)
        {
            if (ssl_prot_prefs[j] == buf[offset+i])   /* got a match? */
            {
                ssl->cipher = ssl_prot_prefs[j];
                goto do_state;
            }
        }
    }

    /* ouch! protocol is not supported */
    ret = SSL_ERROR_NO_CIPHER;

do_state:
error:
    return ret;
}

#ifdef CONFIG_SSL_ENABLE_V23_HANDSHAKE
/*
 * Some browsers use a hybrid SSLv2 "client hello" 
 */
int process_sslv23_client_hello(SSL *ssl)
{
    uint8_t *buf = ssl->bm_data;
    int bytes_needed = ((buf[0] & 0x7f) << 8) + buf[1];
    int ret = SSL_OK;

    /* we have already read 3 extra bytes so far */
    int read_len = SOCKET_READ(ssl->client_fd, buf, bytes_needed-3);
    int cs_len = buf[1];
    int id_len = buf[3];
    int ch_len = buf[5];
    int i, j, offset = 8;   /* start at first cipher */
    int random_offset = 0;

    DISPLAY_BYTES(ssl, "received %d bytes", buf, read_len, read_len);
    
    add_packet(ssl, buf, read_len);

    /* connection has gone, so die */
    if (bytes_needed < 0)
    {
        return SSL_ERROR_CONN_LOST;
    }

    /* now work out what cipher suite we are going to use */
    for (j = 0; j < NUM_PROTOCOLS; j++)
    {
        for (i = 0; i < cs_len; i += 3)
        {
            if (ssl_prot_prefs[j] == buf[offset+i])
            {
                ssl->cipher = ssl_prot_prefs[j];
                goto server_hello;
            }
        }
    }

    /* ouch! protocol is not supported */
    ret = SSL_ERROR_NO_CIPHER;
    goto error;

server_hello:
    /* get the session id */
    offset += cs_len - 2;   /* we've gone 2 bytes past the end */
#ifndef CONFIG_SSL_SKELETON_MODE
    ssl->session = ssl_session_update(ssl->ssl_ctx->num_sessions,
            ssl->ssl_ctx->ssl_sessions, ssl, id_len ? &buf[offset] : NULL);
#endif

    /* get the client random data */
    offset += id_len;

    /* random can be anywhere between 16 and 32 bytes long - so it is padded
     * with 0's to the left */
    if (ch_len == 0x10)
    {
        random_offset += 0x10;
    }

    memcpy(&ssl->dc->client_random[random_offset], &buf[offset], ch_len);
    ret = send_server_hello_sequence(ssl);

error:
    return ret;
}
#endif

/*
 * Send the entire server hello sequence
 */
static int send_server_hello_sequence(SSL *ssl)
{
    int ret;

    if ((ret = send_server_hello(ssl)) == SSL_OK)
    {
#ifndef CONFIG_SSL_SKELETON_MODE
        /* resume handshake? */
        if (IS_SET_SSL_FLAG(SSL_SESSION_RESUME))
        {
            if ((ret = send_change_cipher_spec(ssl)) == SSL_OK)
            {
                ret = send_finished(ssl);
                ssl->next_state = HS_FINISHED;
            }
        }
        else 
#endif
        if ((ret = send_certificate(ssl)) == SSL_OK)
        {
#ifdef CONFIG_SSL_CERT_VERIFICATION
            /* ask the client for its certificate */
            if (IS_SET_SSL_FLAG(SSL_CLIENT_AUTHENTICATION))
            {
                if ((ret = send_certificate_request(ssl)) == SSL_OK)
                {
                    ret = send_server_hello_done(ssl);
                    ssl->next_state = HS_CERTIFICATE;
                }
            }
            else
#endif
            {
                ret = send_server_hello_done(ssl);
                ssl->next_state = HS_CLIENT_KEY_XCHG;
            }
        }
    }

    return ret;
}

/*
 * Send a server hello message.
 */
static int send_server_hello(SSL *ssl)
{
    uint8_t *buf = ssl->bm_data;
    int offset = 0;

    buf[0] = HS_SERVER_HELLO;
    buf[1] = 0;
    buf[2] = 0;
    /* byte 3 is calculated later */
    buf[4] = 0x03;
    buf[5] = ssl->version & 0x0f;

    /* server random value */
    get_random(SSL_RANDOM_SIZE, &buf[6]);
    memcpy(ssl->dc->server_random, &buf[6], SSL_RANDOM_SIZE);
    offset = 6 + SSL_RANDOM_SIZE;

#ifndef CONFIG_SSL_SKELETON_MODE
    if (IS_SET_SSL_FLAG(SSL_SESSION_RESUME))
    {
        /* retrieve id from session cache */
        buf[offset++] = SSL_SESSION_ID_SIZE;
        memcpy(&buf[offset], ssl->session->session_id, SSL_SESSION_ID_SIZE);
        memcpy(ssl->session_id, ssl->session->session_id, SSL_SESSION_ID_SIZE);
        ssl->sess_id_size = SSL_SESSION_ID_SIZE;
        offset += SSL_SESSION_ID_SIZE;
    }
    else    /* generate our own session id */
#endif
    {
#ifndef CONFIG_SSL_SKELETON_MODE
        buf[offset++] = SSL_SESSION_ID_SIZE;
        get_random(SSL_SESSION_ID_SIZE, &buf[offset]);
        memcpy(ssl->session_id, &buf[offset], SSL_SESSION_ID_SIZE);
        ssl->sess_id_size = SSL_SESSION_ID_SIZE;

        /* store id in session cache */
        if (ssl->ssl_ctx->num_sessions)
        {
            memcpy(ssl->session->session_id, 
                    ssl->session_id, SSL_SESSION_ID_SIZE);
        }

        offset += SSL_SESSION_ID_SIZE;
#else
        buf[offset++] = 0;  /* don't bother with session id in skelton mode */
#endif
    }

    buf[offset++] = 0;      /* cipher we are using */
    buf[offset++] = ssl->cipher;
    buf[offset++] = 0;      /* no compression */
    buf[3] = offset - 4;    /* handshake size */
    return send_packet(ssl, PT_HANDSHAKE_PROTOCOL, NULL, offset);
}

/*
 * Send the server hello done message.
 */
static int send_server_hello_done(SSL *ssl)
{
    return send_packet(ssl, PT_HANDSHAKE_PROTOCOL, 
                            g_hello_done, sizeof(g_hello_done));
}

/*
 * Pull apart a client key exchange message. Decrypt the pre-master key (using
 * our RSA private key) and then work out the master key. Initialise the
 * ciphers.
 */
static int process_client_key_xchg(SSL *ssl)
{
    uint8_t *buf = &ssl->bm_data[ssl->dc->bm_proc_index];
    int pkt_size = ssl->bm_index;
    int premaster_size, secret_length = (buf[2] << 8) + buf[3];
    uint8_t premaster_secret[MAX_KEY_BYTE_SIZE];
    RSA_CTX *rsa_ctx = ssl->ssl_ctx->rsa_ctx;
    int offset = 4;
    int ret = SSL_OK;
    
    if (rsa_ctx == NULL)
    {
        ret = SSL_ERROR_NO_CERT_DEFINED;
        goto error;
    }

    /* is there an extra size field? */
    if ((secret_length - 2) == rsa_ctx->num_octets)
        offset += 2;

    PARANOIA_CHECK(pkt_size, rsa_ctx->num_octets+offset);

    /* rsa_ctx->bi_ctx is not thread-safe */
    SSL_CTX_LOCK(ssl->ssl_ctx->mutex);
    premaster_size = RSA_decrypt(rsa_ctx, &buf[offset], premaster_secret, 1);
    SSL_CTX_UNLOCK(ssl->ssl_ctx->mutex);

    if (premaster_size != SSL_SECRET_SIZE || 
            premaster_secret[0] != 0x03 ||  /* must be the same as client
                                               offered version */
                premaster_secret[1] != (ssl->client_version & 0x0f))
    {
        /* guard against a Bleichenbacher attack */
        get_random(SSL_SECRET_SIZE, premaster_secret);
        /* and continue - will die eventually when checking the mac */
    }

#if 0
    print_blob("pre-master", premaster_secret, SSL_SECRET_SIZE);
#endif

    generate_master_secret(ssl, premaster_secret);

#ifdef CONFIG_SSL_CERT_VERIFICATION
    ssl->next_state = IS_SET_SSL_FLAG(SSL_CLIENT_AUTHENTICATION) ?  
                                            HS_CERT_VERIFY : HS_FINISHED;
#else
    ssl->next_state = HS_FINISHED; 
#endif

    ssl->dc->bm_proc_index += rsa_ctx->num_octets+offset;
error:
    return ret;
}

#ifdef CONFIG_SSL_CERT_VERIFICATION
static const uint8_t g_cert_request[] = { HS_CERT_REQ, 0, 0, 4, 1, /* GBG: changed from 0 to 1 */ 1, 0, 0 };

/*
 * Send the certificate request message.
 */
static int send_certificate_request(SSL *ssl)
{
    return send_packet(ssl, PT_HANDSHAKE_PROTOCOL, 
            g_cert_request, sizeof(g_cert_request));
}

/*
 * Ensure the client has the private key by first decrypting the packet and
 * then checking the packet digests.
 */
static int process_cert_verify(SSL *ssl)
{
    uint8_t *buf = &ssl->bm_data[ssl->dc->bm_proc_index];
    int pkt_size = ssl->bm_index;
    uint8_t dgst_buf[MAX_KEY_BYTE_SIZE];
    uint8_t dgst[MD5_SIZE+SHA1_SIZE];
    X509_CTX *x509_ctx = ssl->x509_ctx;
    int ret = SSL_OK;
    int n;

    PARANOIA_CHECK(pkt_size, x509_ctx->rsa_ctx->num_octets+6);
    DISPLAY_RSA(ssl, x509_ctx->rsa_ctx);

    /* rsa_ctx->bi_ctx is not thread-safe */
    SSL_CTX_LOCK(ssl->ssl_ctx->mutex);
    n = RSA_decrypt(x509_ctx->rsa_ctx, &buf[6], dgst_buf, 0);
    SSL_CTX_UNLOCK(ssl->ssl_ctx->mutex);

    if (n != SHA1_SIZE + MD5_SIZE)
    {
        ret = SSL_ERROR_INVALID_KEY;
        goto end_cert_vfy;
    }

    finished_digest(ssl, NULL, dgst);       /* calculate the digest */
    if (memcmp(dgst_buf, dgst, MD5_SIZE + SHA1_SIZE))
    {
        ret = SSL_ERROR_INVALID_KEY;
    }

end_cert_vfy:
    ssl->next_state = HS_FINISHED;
error:
    return ret;
}

#endif
