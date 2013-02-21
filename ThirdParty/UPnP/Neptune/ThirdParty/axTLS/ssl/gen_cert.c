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

#include "config.h"

#ifdef CONFIG_SSL_GENERATE_X509_CERT
#include <string.h>
#include <stdlib.h>
#include "os_port.h"
#include "ssl.h"

/**
 * Generate a basic X.509 certificate
 */

static uint8_t set_gen_length(int len, uint8_t *buf, int *offset)
{
    if (len < 0x80) /* short form */
    {
        buf[(*offset)++] = len;
        return 1;
    }
    else /* long form */
    {
        int i, length_bytes = 0;

        if (len & 0x00FF0000)
            length_bytes = 3;
        else if (len & 0x0000FF00)
            length_bytes = 2;
        else if (len & 0x000000FF)
            length_bytes = 1;
            
        buf[(*offset)++] = 0x80 + length_bytes;

        for (i = length_bytes-1; i >= 0; i--)
        {
            buf[*offset+i] = len & 0xFF;
            len >>= 8;
        }

        *offset += length_bytes;
        return length_bytes+1;
    }
}

static int pre_adjust_with_size(uint8_t type,
        int *seq_offset, uint8_t *buf, int *offset)
{
    buf[(*offset)++] = type;
    *seq_offset = *offset;
    *offset += 4;   /* fill in later */
    return *offset;
}

static void adjust_with_size(int seq_size, int seq_start, 
                uint8_t *buf, int *offset)
{
    uint8_t seq_byte_size; 
    int orig_seq_size = seq_size;
    int orig_seq_start = seq_start;

    seq_size = *offset-seq_size;
    seq_byte_size = set_gen_length(seq_size, buf, &seq_start);

    if (seq_byte_size != 4)
    {
        memmove(&buf[orig_seq_start+seq_byte_size], 
                &buf[orig_seq_size], seq_size);
        *offset -= 4-seq_byte_size;
    }
}

static void gen_serial_number(uint8_t *buf, int *offset)
{
    static const uint8_t ser_oid[] = { ASN1_INTEGER, 1, 0x7F };
    memcpy(&buf[*offset], ser_oid , sizeof(ser_oid));
    *offset += sizeof(ser_oid);
}

static void gen_signature_alg(uint8_t *buf, int *offset)
{
    /* OBJECT IDENTIFIER sha1withRSAEncryption (1 2 840 113549 1 1 5) */
    static const uint8_t sig_oid[] = 
    {
        ASN1_SEQUENCE, 0x0d, ASN1_OID, 0x09, 
        0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x01, 0x05,
        ASN1_NULL, 0x00
    };

    memcpy(&buf[*offset], sig_oid, sizeof(sig_oid));
    *offset += sizeof(sig_oid);
}

static int gen_dn(const char *name, uint8_t dn_type, 
                        uint8_t *buf, int *offset)
{
    int ret = X509_OK;
    int name_size = (int)strlen(name);

    if (name_size > 0x70)    /* just too big */
    {
        ret = X509_NOT_OK;
        goto error;
    }

    buf[(*offset)++] = ASN1_SET;
    set_gen_length(9+name_size, buf, offset);
    buf[(*offset)++] = ASN1_SEQUENCE;
    set_gen_length(7+name_size, buf, offset);
    buf[(*offset)++] = ASN1_OID;
    buf[(*offset)++] = 3;
    buf[(*offset)++] = 0x55;
    buf[(*offset)++] = 0x04;
    buf[(*offset)++] = dn_type;
    buf[(*offset)++] = ASN1_PRINTABLE_STR;
    buf[(*offset)++] = name_size;
    memcpy((char*)&buf[*offset], name, name_size);
    *offset += name_size;

error:
    return ret;
}

static int gen_issuer(const char * dn[], uint8_t *buf, int *offset)
{
    int ret = X509_OK;
    int seq_offset;
    int seq_size = pre_adjust_with_size(
                            ASN1_SEQUENCE, &seq_offset, buf, offset);
#if 0 /* GBG */
    char fqdn[128]; 

    /* we need the common name, so if not configured, work out the fully
     * qualified domain name */
    if (dn[X509_COMMON_NAME] == NULL || strlen(dn[X509_COMMON_NAME]) == 0)
    {
        int fqdn_len;
        gethostname(fqdn, sizeof(fqdn));
        fqdn_len = strlen(fqdn);
        fqdn[fqdn_len++] = '.';
        getdomainname(&fqdn[fqdn_len], sizeof(fqdn)-fqdn_len);
        fqdn_len = strlen(fqdn);

        if (fqdn[fqdn_len-1] == '.')    /* ensure '.' is not last char */
            fqdn[fqdn_len-1] = 0;

        dn[X509_COMMON_NAME] = fqdn;
    }
#endif /* GBG */

    if ((ret = gen_dn(dn[X509_COMMON_NAME], 3, buf, offset)))
        goto error;

    if (dn[X509_ORGANIZATION] != NULL && strlen(dn[X509_ORGANIZATION]) > 0)
    {
        if ((ret = gen_dn(dn[X509_ORGANIZATION], 10, buf, offset)))
            goto error;
    }

    if (dn[X509_ORGANIZATIONAL_UNIT] != NULL &&
                                strlen(dn[X509_ORGANIZATIONAL_UNIT]) > 0)
    {
        if ((ret = gen_dn(dn[X509_ORGANIZATIONAL_UNIT], 11, buf, offset)))
            goto error;
    }

    adjust_with_size(seq_size, seq_offset, buf, offset);

error:
    return ret;
}

static void gen_utc_time(uint8_t *buf, int *offset)
{
    static const uint8_t time_seq[] = 
    {
        ASN1_SEQUENCE, 30, 
        ASN1_UTC_TIME, 13, 
        '0', '7', '0', '1', '0', '1', '0', '0', '0', '0', '0', '0', 'Z', 
        ASN1_UTC_TIME, 13,  /* make it good for 30 or so years */
        '3', '8', '0', '1', '0', '1', '0', '0', '0', '0', '0', '0', 'Z'
    };

    /* fixed time */
    memcpy(&buf[*offset], time_seq, sizeof(time_seq));
    *offset += sizeof(time_seq);
}

static void gen_pub_key2(const RSA_CTX *rsa_ctx, uint8_t *buf, int *offset)
{
    static const uint8_t pub_key_seq[] = 
    {
        ASN1_INTEGER, 0x03, 0x01, 0x00, 0x01 /* INTEGER 65537 */
    };

    int seq_offset;
    int pub_key_size = rsa_ctx->num_octets;
    uint8_t *block = (uint8_t *)alloca(pub_key_size);
    int seq_size = pre_adjust_with_size(
                            ASN1_SEQUENCE, &seq_offset, buf, offset);
    buf[(*offset)++] = ASN1_INTEGER;
    bi_export(rsa_ctx->bi_ctx, rsa_ctx->m, block, pub_key_size);

    if (*block & 0x80)  /* make integer positive */
    {
        set_gen_length(pub_key_size+1, buf, offset);
        buf[(*offset)++] = 0;
    }
    else
        set_gen_length(pub_key_size, buf, offset);

    memcpy(&buf[*offset], block, pub_key_size);
    *offset += pub_key_size;
    memcpy(&buf[*offset], pub_key_seq, sizeof(pub_key_seq));
    *offset += sizeof(pub_key_seq);
    adjust_with_size(seq_size, seq_offset, buf, offset);
}

static void gen_pub_key1(const RSA_CTX *rsa_ctx, uint8_t *buf, int *offset)
{
    int seq_offset;
    int seq_size = pre_adjust_with_size(
                            ASN1_BIT_STRING, &seq_offset, buf, offset);
    buf[(*offset)++] = 0;   /* bit string is multiple of 8 */
    gen_pub_key2(rsa_ctx, buf, offset);
    adjust_with_size(seq_size, seq_offset, buf, offset);
}

static void gen_pub_key(const RSA_CTX *rsa_ctx, uint8_t *buf, int *offset)
{
    /*  OBJECT IDENTIFIER rsaEncryption (1 2 840 113549 1 1 1) */
    static const uint8_t rsa_enc_oid[] =
    {
        ASN1_SEQUENCE, 0x0d, ASN1_OID, 0x09,
        0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x01, 0x01,
        ASN1_NULL, 0x00
    };

    int seq_offset;
    int seq_size = pre_adjust_with_size(
                            ASN1_SEQUENCE, &seq_offset, buf, offset);

    memcpy(&buf[*offset], rsa_enc_oid, sizeof(rsa_enc_oid));
    *offset += sizeof(rsa_enc_oid);
    gen_pub_key1(rsa_ctx, buf, offset);
    adjust_with_size(seq_size, seq_offset, buf, offset);
}

static void gen_signature(const RSA_CTX *rsa_ctx, const uint8_t *sha_dgst, 
                        uint8_t *buf, int *offset)
{
    static const uint8_t asn1_sig[] = 
    {
        ASN1_SEQUENCE,  0x21, ASN1_SEQUENCE, 0x09, ASN1_OID, 0x05, 
        0x2b, 0x0e, 0x03, 0x02, 0x1a, /* sha1 (1 3 14 3 2 26) */
        ASN1_NULL, 0x00, ASN1_OCTET_STRING, 0x14 
    };

    uint8_t *enc_block = (uint8_t *)alloca(rsa_ctx->num_octets);
    uint8_t *block = (uint8_t *)alloca(sizeof(asn1_sig) + SHA1_SIZE);
    int sig_size;

    /* add the digest as an embedded asn.1 sequence */
    memcpy(block, asn1_sig, sizeof(asn1_sig));
    memcpy(&block[sizeof(asn1_sig)], sha_dgst, SHA1_SIZE);

    sig_size = RSA_encrypt(rsa_ctx, block, 
                            sizeof(asn1_sig) + SHA1_SIZE, enc_block, 1);

    buf[(*offset)++] = ASN1_BIT_STRING;
    set_gen_length(sig_size+1, buf, offset);
    buf[(*offset)++] = 0;   /* bit string is multiple of 8 */
    memcpy(&buf[*offset], enc_block, sig_size);
    *offset += sig_size;
}

static int gen_tbs_cert(const char * dn[],
                    const RSA_CTX *rsa_ctx, uint8_t *buf, int *offset,
                    uint8_t *sha_dgst)
{
    int ret = X509_OK;
    SHA1_CTX sha_ctx;
    int seq_offset;
    int begin_tbs = *offset;
    int seq_size = pre_adjust_with_size(
                        ASN1_SEQUENCE, &seq_offset, buf, offset);

    gen_serial_number(buf, offset);
    gen_signature_alg(buf, offset);

    /* CA certicate issuer */
    if ((ret = gen_issuer(dn, buf, offset)))
        goto error;

    gen_utc_time(buf, offset);

    /* certificate issuer */
    if ((ret = gen_issuer(dn, buf, offset)))
        goto error;

    gen_pub_key(rsa_ctx, buf, offset);
    adjust_with_size(seq_size, seq_offset, buf, offset);

    SHA1_Init(&sha_ctx);
    SHA1_Update(&sha_ctx, &buf[begin_tbs], *offset-begin_tbs);
    SHA1_Final(sha_dgst, &sha_ctx);

error:
    return ret;
}

/**
 * Create a new certificate.
 */
EXP_FUNC int STDCALL ssl_x509_create(SSL_CTX *ssl_ctx, uint32_t options, const char * dn[], uint8_t **cert_data)
{
    int ret = X509_OK, offset = 0, seq_offset;
    /* allocate enough space to load a new certificate */
    uint8_t *buf = (uint8_t *)alloca(ssl_ctx->rsa_ctx->num_octets*2 + 512);
    uint8_t sha_dgst[SHA1_SIZE];
    int seq_size = pre_adjust_with_size(ASN1_SEQUENCE, 
                                    &seq_offset, buf, &offset);

    if ((ret = gen_tbs_cert(dn, ssl_ctx->rsa_ctx, buf, &offset, sha_dgst)) < 0)
        goto error;

    (void)options; /* GBG */
    gen_signature_alg(buf, &offset);
    gen_signature(ssl_ctx->rsa_ctx, sha_dgst, buf, &offset);
    adjust_with_size(seq_size, seq_offset, buf, &offset);
    *cert_data = (uint8_t *)malloc(offset); /* create the exact memory for it */
    memcpy(*cert_data, buf, offset);
    
error:
    return ret < 0 ? ret : offset;
}

#endif

