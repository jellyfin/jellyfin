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
#include <time.h>
#include <stdio.h>
#include "os_port.h"
#include "ssl.h"

#ifdef CONFIG_SSL_ENABLE_CLIENT        /* all commented out if no client */

static int send_client_hello(SSL *ssl);
static int process_server_hello(SSL *ssl);
static int process_server_hello_done(SSL *ssl);
static int send_client_key_xchg(SSL *ssl);
static int process_cert_req(SSL *ssl);
static int send_cert_verify(SSL *ssl);

/*
 * Establish a new SSL connection to an SSL server.
 */
EXP_FUNC SSL * STDCALL ssl_client_new(SSL_CTX *ssl_ctx, SSL_SOCKET* client_fd, const
        uint8_t *session_id, uint8_t sess_id_size)
{
    SSL *ssl = ssl_new(ssl_ctx, client_fd);
    ssl->version = SSL_PROTOCOL_VERSION_MAX; /* try top version first */

    if (session_id && ssl_ctx->num_sessions)
    {
        if (sess_id_size > SSL_SESSION_ID_SIZE) /* validity check */
        {
            ssl_free(ssl);
            return NULL;
        }

        memcpy(ssl->session_id, session_id, sess_id_size);
        ssl->sess_id_size = sess_id_size;
        SET_SSL_FLAG(SSL_SESSION_RESUME);   /* just flag for later */
    }

    SET_SSL_FLAG(SSL_IS_CLIENT);
    do_client_connect(ssl);
    return ssl;
}

/*
 * Process the handshake record.
 */
int do_clnt_handshake(SSL *ssl, int handshake_type, uint8_t *buf, int hs_len)
{
    int ret = SSL_OK;
    (void)buf; /* GBG: unused */
    
    /* To get here the state must be valid */
    switch (handshake_type)
    {
        case HS_SERVER_HELLO:
            ret = process_server_hello(ssl);
            break;

        case HS_CERTIFICATE:
            ret = process_certificate(ssl, &ssl->x509_ctx);
            break;

        case HS_SERVER_HELLO_DONE:
            if ((ret = process_server_hello_done(ssl)) == SSL_OK)
            {
                if (IS_SET_SSL_FLAG(SSL_HAS_CERT_REQ))
                {
                    if ((ret = send_certificate(ssl)) == SSL_OK &&
                        (ret = send_client_key_xchg(ssl)) == SSL_OK)
                    {
                        send_cert_verify(ssl);
                    }
                }
                else
                {
                    ret = send_client_key_xchg(ssl);
                }

                if (ret == SSL_OK && 
                     (ret = send_change_cipher_spec(ssl)) == SSL_OK)
                {
                    ret = send_finished(ssl);
                }
            }
            break;

        case HS_CERT_REQ:
            ret = process_cert_req(ssl);
            break;

        case HS_FINISHED:
            ret = process_finished(ssl, buf, hs_len);
            disposable_free(ssl);   /* free up some memory */
            /* note: client renegotiation is not allowed after this */
            break;

        case HS_HELLO_REQUEST:
            disposable_new(ssl);
            ret = do_client_connect(ssl);
            break;

        default:
            ret = SSL_ERROR_INVALID_HANDSHAKE;
            break;
    }

    return ret;
}

/*
 * Do the handshaking from the beginning.
 */
int do_client_connect(SSL *ssl)
{
    int ret = SSL_OK;

    send_client_hello(ssl);                 /* send the client hello */
    ssl->bm_read_index = 0;
    ssl->next_state = HS_SERVER_HELLO;
    ssl->hs_status = SSL_NOT_OK;            /* not connected */

    /* sit in a loop until it all looks good */
    if (!IS_SET_SSL_FLAG(SSL_CONNECT_IN_PARTS))
    {
        while (ssl->hs_status != SSL_OK)
        { 
            ret = ssl_read(ssl, NULL);

            if (ret < SSL_OK)
                break;
        }

        ssl->hs_status = ret;            /* connected? */    
    }

    return ret;
}

/*
 * Send the initial client hello.
 */
static int send_client_hello(SSL *ssl)
{
    uint8_t *buf = ssl->bm_data;
    time_t tm = time(NULL);
    uint8_t *tm_ptr = &buf[6]; /* time will go here */
    int i, offset;

    buf[0] = HS_CLIENT_HELLO;
    buf[1] = 0;
    buf[2] = 0;
    /* byte 3 is calculated later */
    buf[4] = 0x03;
    buf[5] = ssl->version & 0x0f;

    /* client random value - spec says that 1st 4 bytes are big endian time */
    *tm_ptr++ = (uint8_t)(((long)tm & 0xff000000) >> 24);
    *tm_ptr++ = (uint8_t)(((long)tm & 0x00ff0000) >> 16);
    *tm_ptr++ = (uint8_t)(((long)tm & 0x0000ff00) >> 8);
    *tm_ptr++ = (uint8_t)(((long)tm & 0x000000ff));
    get_random(SSL_RANDOM_SIZE-4, &buf[10]);
    memcpy(ssl->dc->client_random, &buf[6], SSL_RANDOM_SIZE);
    offset = 6 + SSL_RANDOM_SIZE;

    /* give session resumption a go */
    if (IS_SET_SSL_FLAG(SSL_SESSION_RESUME))    /* set initially by user */
    {
        buf[offset++] = ssl->sess_id_size;
        memcpy(&buf[offset], ssl->session_id, ssl->sess_id_size);
        offset += ssl->sess_id_size;
        CLR_SSL_FLAG(SSL_SESSION_RESUME);       /* clear so we can set later */
    }
    else
    {
        /* no session id - because no session resumption just yet */
        buf[offset++] = 0;
    }

    buf[offset++] = 0;              /* number of ciphers */
    buf[offset++] = NUM_PROTOCOLS*2;/* number of ciphers */

    /* put all our supported protocols in our request */
    for (i = 0; i < NUM_PROTOCOLS; i++)
    {
        buf[offset++] = 0;          /* cipher we are using */
        buf[offset++] = ssl_prot_prefs[i];
    }

    buf[offset++] = 1;              /* no compression */
    buf[offset++] = 0;
    buf[3] = offset - 4;            /* handshake size */

    return send_packet(ssl, PT_HANDSHAKE_PROTOCOL, NULL, offset);
}

/*
 * Process the server hello.
 */
static int process_server_hello(SSL *ssl)
{
    uint8_t *buf = ssl->bm_data;
    int pkt_size = ssl->bm_index;
    int num_sessions = ssl->ssl_ctx->num_sessions;
    uint8_t sess_id_size;
    int offset, ret = SSL_OK;

    /* check that we are talking to a TLSv1 server */
    uint8_t version = (buf[4] << 4) + buf[5];
    if (version > SSL_PROTOCOL_VERSION_MAX)
    {
        version = SSL_PROTOCOL_VERSION_MAX;
    }
    else if (ssl->version < SSL_PROTOCOL_MIN_VERSION)
    {
        ret = SSL_ERROR_INVALID_VERSION;
        ssl_display_error(ret);
        goto error;
    }

    ssl->version = version;

    /* get the server random value */
    memcpy(ssl->dc->server_random, &buf[6], SSL_RANDOM_SIZE);
    offset = 6 + SSL_RANDOM_SIZE; /* skip of session id size */
    sess_id_size = buf[offset++];

    if (sess_id_size > SSL_SESSION_ID_SIZE)
    {
        ret = SSL_ERROR_INVALID_SESSION;
        goto error;
    }

    if (num_sessions)
    {
        ssl->session = ssl_session_update(num_sessions,
                ssl->ssl_ctx->ssl_sessions, ssl, &buf[offset]);
        memcpy(ssl->session->session_id, &buf[offset], sess_id_size);

        /* pad the rest with 0's */
        if (sess_id_size < SSL_SESSION_ID_SIZE)
        {
            memset(&ssl->session->session_id[sess_id_size], 0,
                SSL_SESSION_ID_SIZE-sess_id_size);
        }
    }

    memcpy(ssl->session_id, &buf[offset], sess_id_size);
    ssl->sess_id_size = sess_id_size;
    offset += sess_id_size;

    /* get the real cipher we are using */
    ssl->cipher = buf[++offset];
    ssl->next_state = IS_SET_SSL_FLAG(SSL_SESSION_RESUME) ? 
                                        HS_FINISHED : HS_CERTIFICATE;

    offset++;   /* skip the compr */
    PARANOIA_CHECK(pkt_size, offset);
    ssl->dc->bm_proc_index = offset+1; 

error:
    return ret;
}

/**
 * Process the server hello done message.
 */
static int process_server_hello_done(SSL *ssl)
{
    ssl->next_state = HS_FINISHED;
    return SSL_OK;
}

/*
 * Send a client key exchange message.
 */
static int send_client_key_xchg(SSL *ssl)
{
    uint8_t *buf = ssl->bm_data;
    uint8_t premaster_secret[SSL_SECRET_SIZE];
    int enc_secret_size = -1;

    buf[0] = HS_CLIENT_KEY_XCHG;
    buf[1] = 0;

    premaster_secret[0] = 0x03; /* encode the version number */
    premaster_secret[1] = SSL_PROTOCOL_MINOR_VERSION; /* must be TLS 1.1 */
    get_random(SSL_SECRET_SIZE-2, &premaster_secret[2]);
    DISPLAY_RSA(ssl, ssl->x509_ctx->rsa_ctx);

    /* rsa_ctx->bi_ctx is not thread-safe */
    SSL_CTX_LOCK(ssl->ssl_ctx->mutex);
    enc_secret_size = RSA_encrypt(ssl->x509_ctx->rsa_ctx, premaster_secret,
            SSL_SECRET_SIZE, &buf[6], 0);
    SSL_CTX_UNLOCK(ssl->ssl_ctx->mutex);

    buf[2] = (enc_secret_size + 2) >> 8;
    buf[3] = (enc_secret_size + 2) & 0xff;
    buf[4] = enc_secret_size >> 8;
    buf[5] = enc_secret_size & 0xff;

    generate_master_secret(ssl, premaster_secret);
    return send_packet(ssl, PT_HANDSHAKE_PROTOCOL, NULL, enc_secret_size+6);
}

/*
 * Process the certificate request.
 */
static int process_cert_req(SSL *ssl)
{
    uint8_t *buf = &ssl->bm_data[ssl->dc->bm_proc_index];
    int ret = SSL_OK;
    int offset = (buf[2] << 4) + buf[3];
    int pkt_size = ssl->bm_index;

    /* don't do any processing - we will send back an RSA certificate anyway */
    ssl->next_state = HS_SERVER_HELLO_DONE;
    SET_SSL_FLAG(SSL_HAS_CERT_REQ);
    ssl->dc->bm_proc_index += offset;
    PARANOIA_CHECK(pkt_size, offset);
error:
    return ret;
}

/*
 * Send a certificate verify message.
 */
static int send_cert_verify(SSL *ssl)
{
    uint8_t *buf = ssl->bm_data;
    uint8_t dgst[MD5_SIZE+SHA1_SIZE];
    RSA_CTX *rsa_ctx = ssl->ssl_ctx->rsa_ctx;
    int n = 0, ret;

    DISPLAY_RSA(ssl, rsa_ctx);

    buf[0] = HS_CERT_VERIFY;
    buf[1] = 0;

    finished_digest(ssl, NULL, dgst);   /* calculate the digest */

    /* rsa_ctx->bi_ctx is not thread-safe */
    if (rsa_ctx)
    {
        SSL_CTX_LOCK(ssl->ssl_ctx->mutex);
        n = RSA_encrypt(rsa_ctx, dgst, sizeof(dgst), &buf[6], 1);
        SSL_CTX_UNLOCK(ssl->ssl_ctx->mutex);

        if (n == 0)
        {
            ret = SSL_ERROR_INVALID_KEY;
            goto error;
        }
    }
    
    buf[4] = n >> 8;        /* add the RSA size (not officially documented) */
    buf[5] = n & 0xff;
    n += 2;
    buf[2] = n >> 8;
    buf[3] = n & 0xff;
    ret = send_packet(ssl, PT_HANDSHAKE_PROTOCOL, NULL, n+4);

error:
    return ret;
}

#endif      /* CONFIG_SSL_ENABLE_CLIENT */
