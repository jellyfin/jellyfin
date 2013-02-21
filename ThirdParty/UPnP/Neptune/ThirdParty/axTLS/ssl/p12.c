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
 * Process PKCS#8/PKCS#12 keys.
 *
 * The decoding of a PKCS#12 key is fairly specific - this code was tested on a
 * key generated with:
 *
 * openssl pkcs12 -export -in axTLS.x509_1024.pem -inkey axTLS.key_1024.pem
 * -keypbe PBE-SHA1-RC4-128 -certpbe PBE-SHA1-RC4-128 
 * -name "p12_withoutCA" -out axTLS.withoutCA.p12 -password pass:abcd
 *
 * or with a certificate chain:
 *
 * openssl pkcs12 -export -in axTLS.x509_1024.pem -inkey axTLS.key_1024.pem
 * -certfile axTLS.ca_x509.pem -keypbe PBE-SHA1-RC4-128 -certpbe
 * PBE-SHA1-RC4-128 -name "p12_withCA" -out axTLS.withCA.p12 -password pass:abcd
 *
 * Note that the PBE has to be specified with PBE-SHA1-RC4-128. The
 * private/public keys/certs have to use RSA encryption. Both the integrity
 * and privacy passwords are the same.
 *
 * The PKCS#8 files were generated with something like:
 *
 * PEM format:
 * openssl pkcs8 -in axTLS.key_512.pem -passout pass:abcd -topk8 -v1
 * PBE-SHA1-RC4-128 -out axTLS.encrypted_pem.p8
 *
 * DER format:
 * openssl pkcs8 -in axTLS.key_512.pem -passout pass:abcd -topk8 -outform DER
 * -v1 PBE-SHA1-RC4-128 -out axTLS.encrypted.p8
 */

#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include "os_port.h"
#include "ssl.h"

/* all commented out if not used */
#ifdef CONFIG_SSL_USE_PKCS12

#define BLOCK_SIZE          64
#define PKCS12_KEY_ID       1
#define PKCS12_IV_ID        2
#define PKCS12_MAC_ID       3

static char *make_uni_pass(const char *password, int *uni_pass_len);
static int p8_decrypt(const char *uni_pass, int uni_pass_len, 
                        const uint8_t *salt, int iter, 
                        uint8_t *priv_key, int priv_key_len, int id);
static int p8_add_key(SSL_CTX *ssl_ctx, uint8_t *priv_key);
static int get_pbe_params(uint8_t *buf, int *offset, 
        const uint8_t **salt, int *iterations);

/*
 * Take a raw pkcs8 block and then decrypt it and turn it into a normal key.
 */
int pkcs8_decode(SSL_CTX *ssl_ctx, SSLObjLoader *ssl_obj, const char *password)
{
    uint8_t *buf = ssl_obj->buf;
    int len, offset = 0;
    int iterations;
    int ret = SSL_NOT_OK;
    uint8_t *version = NULL;
    const uint8_t *salt;
    uint8_t *priv_key;
    int uni_pass_len;
    char *uni_pass = make_uni_pass(password, &uni_pass_len);

    if (asn1_next_obj(buf, &offset, ASN1_SEQUENCE) < 0)
    {
#ifdef CONFIG_SSL_FULL_MODE
        printf("Error: Invalid p8 ASN.1 file\n");
#endif
        goto error;
    }

    /* unencrypted key? */
    if (asn1_get_int(buf, &offset, &version) > 0 && *version == 0)
    {
        ret = p8_add_key(ssl_ctx, buf);
        goto error;
    }

    if (get_pbe_params(buf, &offset, &salt, &iterations) < 0)
        goto error;

    if ((len = asn1_next_obj(buf, &offset, ASN1_OCTET_STRING)) < 0)
        goto error;

    priv_key = &buf[offset];

    p8_decrypt(uni_pass, uni_pass_len, salt, 
                        iterations, priv_key, len, PKCS12_KEY_ID);
    ret = p8_add_key(ssl_ctx, priv_key);

error:
    free(version);
    free(uni_pass);
    return ret;
}

/*
 * Take the unencrypted pkcs8 and turn it into a private key 
 */
static int p8_add_key(SSL_CTX *ssl_ctx, uint8_t *priv_key)
{
    uint8_t *buf = priv_key;
    int len, offset = 0;
    int ret = SSL_NOT_OK;

    /* Skip the preamble and go straight to the private key.
       We only support rsaEncryption (1.2.840.113549.1.1.1)  */
    if (asn1_next_obj(buf, &offset, ASN1_SEQUENCE) < 0 ||
            asn1_skip_obj(buf, &offset, ASN1_INTEGER) < 0 ||
            asn1_skip_obj(buf, &offset, ASN1_SEQUENCE) < 0 ||
            (len = asn1_next_obj(buf, &offset, ASN1_OCTET_STRING)) < 0)
        goto error;

    ret = asn1_get_private_key(&buf[offset], len, &ssl_ctx->rsa_ctx);

error:
    return ret;
}

/*
 * Create the unicode password 
 */
static char *make_uni_pass(const char *password, int *uni_pass_len)
{
    int pass_len = 0, i;
    char *uni_pass;

    if (password == NULL)
    {
        password = "";
    }

    uni_pass = (char *)malloc((strlen(password)+1)*2);

    /* modify the password into a unicode version */
    for (i = 0; i < (int)strlen(password); i++)
    {
        uni_pass[pass_len++] = 0;
        uni_pass[pass_len++] = password[i];
    }

    uni_pass[pass_len++] = 0;       /* null terminate */
    uni_pass[pass_len++] = 0;
    *uni_pass_len = pass_len;
    return uni_pass;
}

/*
 * Decrypt a pkcs8 block.
 */
static int p8_decrypt(const char *uni_pass, int uni_pass_len,
                        const uint8_t *salt, int iter, 
                        uint8_t *priv_key, int priv_key_len, int id)
{
    uint8_t p[BLOCK_SIZE*2];
    uint8_t d[BLOCK_SIZE];
    uint8_t Ai[SHA1_SIZE];
    SHA1_CTX sha_ctx;
    RC4_CTX rc4_ctx;
    int i;

    for (i = 0; i < BLOCK_SIZE; i++)
    {
        p[i] = salt[i % SALT_SIZE];
        p[BLOCK_SIZE+i] = uni_pass[i % uni_pass_len];
        d[i] = id;
    }

    /* get the key - no IV since we are using RC4 */
    SHA1_Init(&sha_ctx);
    SHA1_Update(&sha_ctx, d, sizeof(d));
    SHA1_Update(&sha_ctx, p, sizeof(p));
    SHA1_Final(Ai, &sha_ctx);

    for (i = 1; i < iter; i++)
    {
        SHA1_Init(&sha_ctx);
        SHA1_Update(&sha_ctx, Ai, SHA1_SIZE);
        SHA1_Final(Ai, &sha_ctx);
    }

    /* do the decryption */
    if (id == PKCS12_KEY_ID)
    {
        RC4_setup(&rc4_ctx, Ai, 16);
        RC4_crypt(&rc4_ctx, priv_key, priv_key, priv_key_len);
    }
    else  /* MAC */
        memcpy(priv_key, Ai, SHA1_SIZE);

    return 0;
}

/*
 * Take a raw pkcs12 block and the decrypt it and turn it into a certificate(s)
 * and keys.
 */
int pkcs12_decode(SSL_CTX *ssl_ctx, SSLObjLoader *ssl_obj, const char *password)
{
    uint8_t *buf = ssl_obj->buf;
    int len, iterations, auth_safes_start, 
              auth_safes_end, auth_safes_len, key_offset, offset = 0;
    int all_certs = 0;
    uint8_t *version = NULL, *auth_safes = NULL, *cert, *orig_mac;
    uint8_t key[SHA1_SIZE];
    uint8_t mac[SHA1_SIZE];
    const uint8_t *salt;
    int uni_pass_len, ret = SSL_OK;
    char *uni_pass = make_uni_pass(password, &uni_pass_len);
    static const uint8_t pkcs_data[] = /* pkc7 data */
        { 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x07, 0x01 };
    static const uint8_t pkcs_encrypted[] = /* pkc7 encrypted */
        { 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x07, 0x06 };
    static const uint8_t pkcs8_key_bag[] = /* 1.2.840.113549.1.12.10.1.2 */
        { 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x0c, 0x0a, 0x01, 0x02 };

    if (asn1_next_obj(buf, &offset, ASN1_SEQUENCE) < 0)
    {
#ifdef CONFIG_SSL_FULL_MODE
        printf("Error: Invalid p12 ASN.1 file\n");
#endif
        goto error;
    }

    if (asn1_get_int(buf, &offset, &version) < 0 || *version != 3)
    {
        ret = SSL_ERROR_INVALID_VERSION;
        goto error;
    }

    /* remove all the boring pcks7 bits */
    if (asn1_next_obj(buf, &offset, ASN1_SEQUENCE) < 0 || 
                (len = asn1_next_obj(buf, &offset, ASN1_OID)) < 0 ||
                len != sizeof(pkcs_data) || 
                memcmp(&buf[offset], pkcs_data, sizeof(pkcs_data)))
        goto error;

    offset += len;

    if (asn1_next_obj(buf, &offset, ASN1_EXPLICIT_TAG) < 0 ||
            asn1_next_obj(buf, &offset, ASN1_OCTET_STRING) < 0)
        goto error;

    /* work out the MAC start/end points (done on AuthSafes) */
    auth_safes_start = offset;
    auth_safes_end = offset;
    if (asn1_skip_obj(buf, &auth_safes_end, ASN1_SEQUENCE) < 0)
        goto error;

    auth_safes_len = auth_safes_end - auth_safes_start;
    auth_safes = malloc(auth_safes_len);

    memcpy(auth_safes, &buf[auth_safes_start], auth_safes_len);

    if (asn1_next_obj(buf, &offset, ASN1_SEQUENCE) < 0 ||
            asn1_next_obj(buf, &offset, ASN1_SEQUENCE) < 0 ||
            (len = asn1_next_obj(buf, &offset, ASN1_OID)) < 0 ||
            (len != sizeof(pkcs_encrypted) || 
            memcmp(&buf[offset], pkcs_encrypted, sizeof(pkcs_encrypted))))
        goto error;

    offset += len;

    if (asn1_next_obj(buf, &offset, ASN1_EXPLICIT_TAG) < 0 ||
            asn1_next_obj(buf, &offset, ASN1_SEQUENCE) < 0 ||
            asn1_skip_obj(buf, &offset, ASN1_INTEGER) < 0 ||
            asn1_next_obj(buf, &offset, ASN1_SEQUENCE) < 0 ||
            (len = asn1_next_obj(buf, &offset, ASN1_OID)) < 0 ||
            len != sizeof(pkcs_data) || 
            memcmp(&buf[offset], pkcs_data, sizeof(pkcs_data)))
        goto error;

    offset += len;

    /* work out the salt for the certificate */
    if (get_pbe_params(buf, &offset, &salt, &iterations) < 0 ||
            (len = asn1_next_obj(buf, &offset, ASN1_IMPLICIT_TAG)) < 0)
        goto error;

    /* decrypt the certificate */
    cert = &buf[offset];
    if ((ret = p8_decrypt(uni_pass, uni_pass_len, salt, iterations, cert, 
                            len, PKCS12_KEY_ID)) < 0)
        goto error;

    offset += len;

    /* load the certificate */
    key_offset = 0;
    all_certs = asn1_next_obj(cert, &key_offset, ASN1_SEQUENCE);

    /* keep going until all certs are loaded */
    while (key_offset < all_certs)
    {
        int cert_offset = key_offset;

        if (asn1_skip_obj(cert, &cert_offset, ASN1_SEQUENCE) < 0 ||
                asn1_next_obj(cert, &key_offset, ASN1_SEQUENCE) < 0 ||
                asn1_skip_obj(cert, &key_offset, ASN1_OID) < 0 ||
                asn1_next_obj(cert, &key_offset, ASN1_EXPLICIT_TAG) < 0 ||
                asn1_next_obj(cert, &key_offset, ASN1_SEQUENCE) < 0 ||
                asn1_skip_obj(cert, &key_offset, ASN1_OID) < 0 ||
                asn1_next_obj(cert, &key_offset, ASN1_EXPLICIT_TAG) < 0 ||
                (len = asn1_next_obj(cert, &key_offset, ASN1_OCTET_STRING)) < 0)
            goto error;

        if ((ret = add_cert(ssl_ctx, &cert[key_offset], len)) < 0)
            goto error;

        key_offset = cert_offset;
    }

    if (asn1_next_obj(buf, &offset, ASN1_SEQUENCE) < 0 ||
            (len = asn1_next_obj(buf, &offset, ASN1_OID)) < 0 ||
            len != sizeof(pkcs_data) || 
            memcmp(&buf[offset], pkcs_data, sizeof(pkcs_data)))
        goto error;

    offset += len;

    if (asn1_next_obj(buf, &offset, ASN1_EXPLICIT_TAG) < 0 ||
            asn1_next_obj(buf, &offset, ASN1_OCTET_STRING) < 0 ||
            asn1_next_obj(buf, &offset, ASN1_SEQUENCE) < 0 ||
            asn1_next_obj(buf, &offset, ASN1_SEQUENCE) < 0 ||
            (len = asn1_next_obj(buf, &offset, ASN1_OID)) < 0 ||
            (len != sizeof(pkcs8_key_bag)) || 
            memcmp(&buf[offset], pkcs8_key_bag, sizeof(pkcs8_key_bag)))
        goto error;

    offset += len;

    /* work out the salt for the private key */
    if (asn1_next_obj(buf, &offset, ASN1_EXPLICIT_TAG) < 0 ||
            asn1_next_obj(buf, &offset, ASN1_SEQUENCE) < 0 ||
            get_pbe_params(buf, &offset, &salt, &iterations) < 0 ||
            (len = asn1_next_obj(buf, &offset, ASN1_OCTET_STRING)) < 0)
        goto error;

    /* decrypt the private key */
    cert = &buf[offset];
    if ((ret = p8_decrypt(uni_pass, uni_pass_len, salt, iterations, cert, 
                            len, PKCS12_KEY_ID)) < 0)
        goto error;

    offset += len;

    /* load the private key */
    if ((ret = p8_add_key(ssl_ctx, cert)) < 0)
        goto error;

    /* miss out on friendly name, local key id etc */
    if (asn1_skip_obj(buf, &offset, ASN1_SET) < 0)
        goto error;

    /* work out the MAC */
    if (asn1_next_obj(buf, &offset, ASN1_SEQUENCE) < 0 ||
            asn1_next_obj(buf, &offset, ASN1_SEQUENCE) < 0 ||
            asn1_skip_obj(buf, &offset, ASN1_SEQUENCE) < 0 ||
            (len = asn1_next_obj(buf, &offset, ASN1_OCTET_STRING)) < 0 ||
            len != SHA1_SIZE)
        goto error;

    orig_mac = &buf[offset];
    offset += len;

    /* get the salt */
    if ((len = asn1_next_obj(buf, &offset, ASN1_OCTET_STRING)) < 0 || len != 8)
        goto error;

    salt = &buf[offset];

    /* work out what the mac should be */
    if ((ret = p8_decrypt(uni_pass, uni_pass_len, salt, iterations, 
                            key, SHA1_SIZE, PKCS12_MAC_ID)) < 0)
        goto error;

    hmac_sha1(auth_safes, auth_safes_len, key, SHA1_SIZE, mac);

    if (memcmp(mac, orig_mac, SHA1_SIZE))
    {
        ret = SSL_ERROR_INVALID_HMAC;                  
        goto error;
    }

error:
    free(version);
    free(uni_pass);
    free(auth_safes);
    return ret;
}

/*
 * Retrieve the salt/iteration details from a PBE block.
 */
static int get_pbe_params(uint8_t *buf, int *offset, 
        const uint8_t **salt, int *iterations)
{
    static const uint8_t pbeSH1RC4[] = /* pbeWithSHAAnd128BitRC4  */
            { 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x0c, 0x01, 0x01 };

    int i, len;
    uint8_t *iter = NULL;
    int error_code = SSL_ERROR_NOT_SUPPORTED;

    /* Get the PBE type */
    if (asn1_next_obj(buf, offset, ASN1_SEQUENCE) < 0 ||
            (len = asn1_next_obj(buf, offset, ASN1_OID)) < 0)
        goto error;

    /* we expect pbeWithSHAAnd128BitRC4 (1.2.840.113549.1.12.1.1) 
       which is the only algorithm we support */
    if (len != sizeof(pbeSH1RC4) || 
                    memcmp(&buf[*offset], pbeSH1RC4, sizeof(pbeSH1RC4)))
    {
#ifdef CONFIG_SSL_FULL_MODE
        printf("Error: pkcs8/pkcs12 must use \"PBE-SHA1-RC4-128\"\n");
#endif
        goto error;
    }

    *offset += len;

    if (asn1_next_obj(buf, offset, ASN1_SEQUENCE) < 0 ||
            (len = asn1_next_obj(buf, offset, ASN1_OCTET_STRING)) < 0 || 
            len != 8)
        goto error;

    *salt = &buf[*offset];
    *offset += len;

    if ((len = asn1_get_int(buf, offset, &iter)) < 0)
        goto error;

    *iterations = 0;
    for (i = 0; i < len; i++)
    {
        (*iterations) <<= 8;
        (*iterations) += iter[i];
    }

    free(iter);
    error_code = SSL_OK;       /* got here - we are ok */

error:
    return error_code;
}

#endif
