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
 * Some primitive asn methods for extraction ASN.1 data.
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>
#include "os_port.h"
#include "crypto.h"
#include "crypto_misc.h"
#include "ssl.h"
#include "tls1.h"

#define SIG_OID_PREFIX_SIZE 8
#define SIG_IIS6_OID_SIZE   5
#define SIG_SUBJECT_ALT_NAME_SIZE 3

/* Must be an RSA algorithm with either SHA1 or MD5 for verifying to work */
static const uint8_t sig_oid_prefix[SIG_OID_PREFIX_SIZE] = 
{
    0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x01
};

static const uint8_t sig_sha1WithRSAEncrypt[SIG_IIS6_OID_SIZE] =
{
    0x2b, 0x0e, 0x03, 0x02, 0x1d
};

static const uint8_t sig_subject_alt_name[SIG_SUBJECT_ALT_NAME_SIZE] =
{
    0x55, 0x1d, 0x11
};

/* CN, O, OU */
static const uint8_t g_dn_types[] = { 3, 10, 11, 0 };

int get_asn1_length(const uint8_t *buf, int *offset)
{
    int len, i;

    if (!(buf[*offset] & 0x80)) /* short form */
    {
        len = buf[(*offset)++];
    }
    else  /* long form */
    {
        int length_bytes = buf[(*offset)++]&0x7f;
        len = 0;
        for (i = 0; i < length_bytes; i++)
        {
            len <<= 8;
            len += buf[(*offset)++];
        }
    }

    return len;
}

/**
 * Skip the ASN1.1 object type and its length. Get ready to read the object's
 * data.
 */
int asn1_next_obj(const uint8_t *buf, int *offset, int obj_type)
{
    if (buf[*offset] != obj_type)
        return X509_NOT_OK;
    (*offset)++;
    return get_asn1_length(buf, offset);
}

/**
 * Skip over an ASN.1 object type completely. Get ready to read the next
 * object.
 */
int asn1_skip_obj(const uint8_t *buf, int *offset, int obj_type)
{
    int len;

    if (buf[*offset] != obj_type)
        return X509_NOT_OK;
    (*offset)++;
    len = get_asn1_length(buf, offset);
    *offset += len;
    return 0;
}

/**
 * Read an integer value for ASN.1 data
 * Note: This function allocates memory which must be freed by the user.
 */
int asn1_get_int(const uint8_t *buf, int *offset, uint8_t **object)
{
    int len;

    if ((len = asn1_next_obj(buf, offset, ASN1_INTEGER)) < 0)
        goto end_int_array;

    if (len > 1 && buf[*offset] == 0x00)    /* ignore the negative byte */
    {
        len--;
        (*offset)++;
    }

    *object = (uint8_t *)malloc(len);
    memcpy(*object, &buf[*offset], len);
    *offset += len;

end_int_array:
    return len;
}

/**
 * Get all the RSA private key specifics from an ASN.1 encoded file 
 */
int asn1_get_private_key(const uint8_t *buf, int len, RSA_CTX **rsa_ctx)
{
    int offset = 7;
    uint8_t *modulus = NULL, *priv_exp = NULL, *pub_exp = NULL;
    int mod_len, priv_len, pub_len;
#ifdef CONFIG_BIGINT_CRT
    uint8_t *p = NULL, *q = NULL, *dP = NULL, *dQ = NULL, *qInv = NULL;
    int p_len, q_len, dP_len, dQ_len, qInv_len;
#endif

    /* not in der format */
    if (buf[0] != ASN1_SEQUENCE) /* basic sanity check */
    {
#ifdef CONFIG_SSL_FULL_MODE
        printf("Error: This is not a valid ASN.1 file\n");
#endif
        return X509_INVALID_PRIV_KEY;
    }

    /* initialise the RNG */
    RNG_initialize(buf, len);

    mod_len = asn1_get_int(buf, &offset, &modulus);
    pub_len = asn1_get_int(buf, &offset, &pub_exp);
    priv_len = asn1_get_int(buf, &offset, &priv_exp);

    if (mod_len <= 0 || pub_len <= 0 || priv_len <= 0)
        return X509_INVALID_PRIV_KEY;

#ifdef CONFIG_BIGINT_CRT
    p_len = asn1_get_int(buf, &offset, &p);
    q_len = asn1_get_int(buf, &offset, &q);
    dP_len = asn1_get_int(buf, &offset, &dP);
    dQ_len = asn1_get_int(buf, &offset, &dQ);
    qInv_len = asn1_get_int(buf, &offset, &qInv);

    if (p_len <= 0 || q_len <= 0 || dP_len <= 0 || dQ_len <= 0 || qInv_len <= 0)
        return X509_INVALID_PRIV_KEY;

    RSA_priv_key_new(rsa_ctx, 
            modulus, mod_len, pub_exp, pub_len, priv_exp, priv_len,
            p, p_len, q, p_len, dP, dP_len, dQ, dQ_len, qInv, qInv_len);

    free(p);
    free(q);
    free(dP);
    free(dQ);
    free(qInv);
#else
    RSA_priv_key_new(rsa_ctx, 
            modulus, mod_len, pub_exp, pub_len, priv_exp, priv_len);
#endif

    free(modulus);
    free(priv_exp);
    free(pub_exp);
    return X509_OK;
}

/**
 * Get the time of a certificate. Ignore hours/minutes/seconds.
 */
static int asn1_get_utc_time(const uint8_t *buf, int *offset, SSL_DateTime *t)
{
    int ret = X509_NOT_OK, len, t_offset;
    uint8_t time_encoding;
    
    memset(t, 0, sizeof(*t));
    time_encoding = buf[(*offset)++];
    if (time_encoding != ASN1_UTC_TIME && time_encoding != ASN1_GENERALIZED_TIME) /* GBG */
        goto end_utc_time;
    len = get_asn1_length(buf, offset);
    t_offset = *offset;

    if (time_encoding == ASN1_UTC_TIME) {
        t->year = (buf[t_offset] - '0')*10 + (buf[t_offset+1] - '0');

        if (t->year <= 50)    /* 1951-2050 thing */
        {
            t->year += 100;
        }
        t->year += 1900;
        t_offset += 2;
    } else {
        t->year = (buf[t_offset  ] - '0')*1000 +
                  (buf[t_offset+1] - '0')*100  + 
                  (buf[t_offset+2] - '0')*10   + 
                  (buf[t_offset+3] - '0');
        t_offset += 4;
    }

    t->month = (buf[t_offset  ] - '0')*10 + (buf[t_offset+1] - '0');
    t->day   = (buf[t_offset+2] - '0')*10 + (buf[t_offset+3] - '0');
    *offset += len;
    ret = X509_OK;

end_utc_time:
    return ret;
}

/**
 * Get the version type of a certificate (which we don't actually care about)
 */
int asn1_version(const uint8_t *cert, int *offset, X509_CTX *x509_ctx)
{
    int ret = X509_NOT_OK;
    (void)x509_ctx; /* GBG: unused */
    
    (*offset) += 2;        /* get past explicit tag */
    if (asn1_skip_obj(cert, offset, ASN1_INTEGER))
        goto end_version;

    ret = X509_OK;
end_version:
    return ret;
}

/**
 * Retrieve the notbefore and notafter certificate times.
 */
int asn1_validity(const uint8_t *cert, int *offset, X509_CTX *x509_ctx)
{
    return (asn1_next_obj(cert, offset, ASN1_SEQUENCE) < 0 ||
              asn1_get_utc_time(cert, offset, &x509_ctx->not_before) ||
              asn1_get_utc_time(cert, offset, &x509_ctx->not_after));
}

#if 0 /* GBG */
/**
 * Get the components of a distinguished name 
 */
static int asn1_get_oid_x520(const uint8_t *buf, int *offset)
{
    int dn_type = 0;
    int len;

    if ((len = asn1_next_obj(buf, offset, ASN1_OID)) < 0)
        goto end_oid;

    /* expect a sequence of 2.5.4.[x] where x is a one of distinguished name 
       components we are interested in. */
    if (len == 3 && buf[*offset] == 0x55 && buf[*offset+1] == 0x04) { /* GBG */
        dn_type = buf[*offset+2];
    } 
    *offset += len;     /* skip over it */

end_oid:
    return dn_type;
}
#endif

/**
 * Obtain an ASN.1 printable string type.
 */
static int asn1_get_printable_str(const uint8_t *buf, int *offset, char **str)
{
    int len = X509_NOT_OK;
    int string_type = buf[*offset]; /* GBG */
    
    /* some certs have this awful crud in them for some reason */
    if (string_type != ASN1_PRINTABLE_STR &&  
        string_type != ASN1_TELETEX_STR   &&  
        string_type != ASN1_IA5_STR       &&  
        string_type != ASN1_UNICODE_STR   &&
        string_type != ASN1_UTF8_STR      &&
        string_type != ASN1_UNIVERSAL_STR)
        goto end_pnt_str;

        (*offset)++;
        len = get_asn1_length(buf, offset);

        if (string_type == ASN1_UNICODE_STR)
        {
            int i;
            *str = (char *)malloc(len/2+1);     /* allow for null */

            for (i = 0; i < len; i += 2)
                (*str)[i/2] = buf[*offset + i + 1];

            (*str)[len/2] = 0;                  /* null terminate */
        }
        else
        {
            *str = (char *)malloc(len+1);       /* allow for null */
            memcpy(*str, &buf[*offset], len);
            (*str)[len] = 0;                    /* null terminate */
        }

        *offset += len;

end_pnt_str:
    return len;
}

/** 
 * Add an integer value at the end of a string
 */
static char*
asn1_append_int_value(char* str, unsigned int value)
{
    char workspace[32];
    char* c = &workspace[31];
    int digit_count = 0;

    /* process the digits */
    do {
        unsigned int digit = value%10;
        *c-- = '0'+digit;
        value /= 10;
        ++digit_count;
    } while(value);

    /* copy the string */
    memcpy(str, &workspace[32-digit_count], digit_count);
        
    return str+digit_count;
}

/**
 * Convert an ASN.1 OID to a string. (GBG)
 */
static char*
asn1_oid_to_string(const uint8_t *oid, int len)
{
    char* str = NULL;
    char* result = NULL;
    int i;
    
    /* sanity check */
    if (len < 2) return NULL;
    if (len >= 3 && (oid[len-1] & 0x80)) return NULL; /* last byte should not have the high bit set */
    
    /* allocate space with an upper bound size estimate */
    str = malloc(len*4+len+3);
    result = str;
    
    /* first two values */
    {
        int value1 = oid[0]/40;
        int value2 = oid[0]-(40*value1);
        str = asn1_append_int_value(str, value1);
        *str++ = '.';
        str = asn1_append_int_value(str, value2);
    }
    
    /* other values */
    {
        unsigned long value = 0;
        for (i=1; i<len; i++) {
            value = (value<<7) + (oid[i]&0x7F);
            if ((oid[i]&0x80) == 0) {
                /* termination of value */
                *str++ = '.';
                str = asn1_append_int_value(str, value);
                value = 0;
            }
        }
    }
    *str++ = '\0';
    return result;
}


/**
 * Get the subject name (or the issuer) of a certificate.
 */
int asn1_name(const uint8_t *cert, int *offset, char *dn[])
{
    int ret = X509_NOT_OK;
    int dn_type = 0;
    char *name = NULL;
    char* name_prefix = NULL; /* GBG */
    
    if (asn1_next_obj(cert, offset, ASN1_SEQUENCE) < 0)
        goto end_name;

    while (asn1_next_obj(cert, offset, ASN1_SET) >= 0)
    {
        int i, found = 0;

        if (asn1_next_obj(cert, offset, ASN1_SEQUENCE) < 0) {  /* GBG */
            goto end_name;
        }
        /* get the oid */
        {
            int len = asn1_next_obj(cert, offset, ASN1_OID);
            int oid_offset = *offset;
            
            if (len < 0) goto end_name;
            if (len == 3 && cert[oid_offset] == 0x55 && cert[oid_offset+1] == 0x04) {
                dn_type = cert[oid_offset+2];
            } else {
                /* convert the OID to a string */
                name_prefix = asn1_oid_to_string(cert+oid_offset, len);
                if (name_prefix == NULL) goto end_name;
            }
            *offset += len;
        }
        name = NULL;
        if (asn1_get_printable_str(cert, offset, &name) < 0) {
            free(name);
            if (name_prefix) free(name_prefix);
            goto end_name;
        }
        /* add the prefix if there is one */
        if (name_prefix) {
            int name_prefix_len = (int)strlen(name_prefix);
            int name_len        = (int)strlen(name);
            char* compound = malloc(name_prefix_len+name_len+2);
            memcpy(compound, name_prefix, name_prefix_len);
            compound[name_prefix_len] = '=';
            memcpy(compound+name_prefix_len+1, name, name_len+1);
            free(name);
            free(name_prefix);
            name = compound;
            name_prefix = NULL;
        }

        /* find the distinguished named type */
        for (i = 0; i < X509_NUM_DN_TYPES; i++)
        {
            if (dn_type == g_dn_types[i])
            {
                if (dn[i] == NULL)
                {
                    dn[i] = name;
                    found = 1;
                    break;
                }
            }
        }

        if (found == 0) /* not found so get rid of it */
        {
            free(name);
        }
    }

    ret = X509_OK;
end_name:
    return ret;
}

/**
 * Read the modulus and public exponent of a certificate.
 */
int asn1_public_key(const uint8_t *cert, int *offset, X509_CTX *x509_ctx)
{
    int ret = X509_NOT_OK, mod_len, pub_len;
    uint8_t *modulus = NULL, *pub_exp = NULL;

    if (asn1_next_obj(cert, offset, ASN1_SEQUENCE) < 0 ||
            asn1_skip_obj(cert, offset, ASN1_SEQUENCE) ||
            asn1_next_obj(cert, offset, ASN1_BIT_STRING) < 0)
        goto end_pub_key;

    (*offset)++;        /* ignore the padding bit field */

    if (asn1_next_obj(cert, offset, ASN1_SEQUENCE) < 0)
        goto end_pub_key;

    mod_len = asn1_get_int(cert, offset, &modulus);
    pub_len = asn1_get_int(cert, offset, &pub_exp);

    RSA_pub_key_new(&x509_ctx->rsa_ctx, modulus, mod_len, pub_exp, pub_len);

    free(modulus);
    free(pub_exp);
    ret = X509_OK;

end_pub_key:
    return ret;
}

#ifdef CONFIG_SSL_CERT_VERIFICATION
/**
 * Read the signature of the certificate.
 */
int asn1_signature(const uint8_t *cert, int *offset, X509_CTX *x509_ctx)
{
    int ret = X509_NOT_OK;

    if (cert[(*offset)++] != ASN1_BIT_STRING)
        goto end_sig;

    x509_ctx->sig_len = get_asn1_length(cert, offset)-1;
    (*offset)++;            /* ignore bit string padding bits */
    x509_ctx->signature = (uint8_t *)malloc(x509_ctx->sig_len);
    memcpy(x509_ctx->signature, &cert[*offset], x509_ctx->sig_len);
    *offset += x509_ctx->sig_len;
    ret = X509_OK;

end_sig:
    return ret;
}

/*
 * Compare 2 distinguished name components for equality 
 * @return 0 if a match
 */
static int asn1_compare_dn_comp(const char *dn1, const char *dn2)
{
    int ret;

    if (dn1 == NULL && dn2 == NULL)
        ret = 0;
    else
        ret = (dn1 && dn2) ? strcmp(dn1, dn2) : -1;

    return ret;
}

#if 0 /* GBG */
/**
 * Clean up all of the CA certificates.
 */
void remove_ca_certs(CA_CERT_CTX *ca_cert_ctx)
{
    int i = 0;

    if (ca_cert_ctx == NULL)
        return;

    while (i < CONFIG_X509_MAX_CA_CERTS && ca_cert_ctx->cert[i])
    {
        x509_free(ca_cert_ctx->cert[i]);
        ca_cert_ctx->cert[i++] = NULL;
    }

    free(ca_cert_ctx);
}
#endif /* GBG */

/*
 * Compare 2 distinguished names for equality 
 * @return 0 if a match
 */
int asn1_compare_dn(char * const dn1[], char * const dn2[])
{
    int i;

    for (i = 0; i < X509_NUM_DN_TYPES; i++)
    {
        if (asn1_compare_dn_comp(dn1[i], dn2[i]))
            return 1;
    }

    return 0;       /* all good */
}

static int asn1_find_oid(const uint8_t* cert, int* offset, 
                    const uint8_t* oid, int oid_length)
{
    int seqlen;
    if ((seqlen = asn1_next_obj(cert, offset, ASN1_SEQUENCE))> 0)
    {
        int end = *offset + seqlen;

        while (*offset < end)
        {
            int type = cert[(*offset)++];
            int length = get_asn1_length(cert, offset);
            int noffset = *offset + length;

            if (type == ASN1_SEQUENCE)
            {
                type = cert[(*offset)++];
                length = get_asn1_length(cert, offset);

                if (type == ASN1_OID && length == oid_length && 
                              memcmp(cert + *offset, oid, oid_length) == 0)
                {
                    *offset += oid_length;
                    return 1;
                }
            }

            *offset = noffset;
        }
    }

    return 0;
}

int asn1_find_subjectaltname(const uint8_t* cert, int offset)
{
    if (asn1_find_oid(cert, &offset, sig_subject_alt_name, 
                                SIG_SUBJECT_ALT_NAME_SIZE))
    {
        return offset;
    }

    return 0;
}

#endif /* CONFIG_SSL_CERT_VERIFICATION */

/**
 * Read the signature type of the certificate. We only support RSA-MD5 and
 * RSA-SHA1 signature types.
 */
int asn1_signature_type(const uint8_t *cert, 
                                int *offset, X509_CTX *x509_ctx)
{
    int ret = X509_NOT_OK, len;

    if (cert[(*offset)++] != ASN1_OID)
        goto end_check_sig;

    len = get_asn1_length(cert, offset);

    if (len == 5 && memcmp(sig_sha1WithRSAEncrypt, &cert[*offset], 
                                    SIG_IIS6_OID_SIZE) == 0)
    {
        x509_ctx->sig_type = SIG_TYPE_SHA1;
    }
    else
    {
        if (memcmp(sig_oid_prefix, &cert[*offset], SIG_OID_PREFIX_SIZE))
            goto end_check_sig;     /* unrecognised cert type */

        x509_ctx->sig_type = cert[*offset + SIG_OID_PREFIX_SIZE];
    }

    *offset += len;
    asn1_skip_obj(cert, offset, ASN1_NULL); /* if it's there */
    ret = X509_OK;

end_check_sig:
    return ret;
}

