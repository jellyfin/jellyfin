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
 * @file x509.c
 * 
 * Certificate processing.
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>
#include "os_port.h"
#include "crypto_misc.h"

#ifdef CONFIG_SSL_CERT_VERIFICATION
/**
 * Retrieve the signature from a certificate.
 */
static const uint8_t *get_signature(const uint8_t *asn1_sig, int *len)
{
    int offset = 0;
    const uint8_t *ptr = NULL;

    if (asn1_next_obj(asn1_sig, &offset, ASN1_SEQUENCE) < 0 || 
            asn1_skip_obj(asn1_sig, &offset, ASN1_SEQUENCE))
        goto end_get_sig;

    if (asn1_sig[offset++] != ASN1_OCTET_STRING)
        goto end_get_sig;
    *len = get_asn1_length(asn1_sig, &offset);
    ptr = &asn1_sig[offset];          /* all ok */

end_get_sig:
    return ptr;
}

#endif

/**
 * Construct a new x509 object.
 * @return 0 if ok. < 0 if there was a problem.
 */
int x509_new(const uint8_t *cert, int *len, X509_CTX **ctx)
{
    int begin_tbs, end_tbs;
    int ret = X509_NOT_OK, offset = 0, cert_size = 0;
    X509_CTX *x509_ctx;
    BI_CTX *bi_ctx;

    *ctx = (X509_CTX *)calloc(1, sizeof(X509_CTX));
    x509_ctx = *ctx;

    /* get the certificate size */
    asn1_skip_obj(cert, &cert_size, ASN1_SEQUENCE); 

    if (asn1_next_obj(cert, &offset, ASN1_SEQUENCE) < 0)
        goto end_cert;

    begin_tbs = offset;         /* start of the tbs */
    end_tbs = begin_tbs;        /* work out the end of the tbs */
    asn1_skip_obj(cert, &end_tbs, ASN1_SEQUENCE);

    if (asn1_next_obj(cert, &offset, ASN1_SEQUENCE) < 0)
        goto end_cert;

    if (cert[offset] == ASN1_EXPLICIT_TAG)   /* optional version */
    {
        if (asn1_version(cert, &offset, x509_ctx))
            goto end_cert;
    }

    if (asn1_skip_obj(cert, &offset, ASN1_INTEGER) || /* serial number */ 
            asn1_next_obj(cert, &offset, ASN1_SEQUENCE) < 0)
        goto end_cert;

    /* make sure the signature is ok */
    if (asn1_signature_type(cert, &offset, x509_ctx))
    {
        ret = X509_VFY_ERROR_UNSUPPORTED_DIGEST;
        goto end_cert;
    }

    if (asn1_name(cert, &offset, x509_ctx->ca_cert_dn) || 
            asn1_validity(cert, &offset, x509_ctx) ||
            asn1_name(cert, &offset, x509_ctx->cert_dn) ||
            asn1_public_key(cert, &offset, x509_ctx))
    {
        goto end_cert;
    }

    bi_ctx = x509_ctx->rsa_ctx->bi_ctx;

#ifdef CONFIG_SSL_CERT_VERIFICATION /* only care if doing verification */
    /* use the appropriate signature algorithm (SHA1/MD5/MD2) */
    if (x509_ctx->sig_type == SIG_TYPE_MD5)
    {
        MD5_CTX md5_ctx;
        uint8_t md5_dgst[MD5_SIZE];
        MD5_Init(&md5_ctx);
        MD5_Update(&md5_ctx, &cert[begin_tbs], end_tbs-begin_tbs);
        MD5_Final(md5_dgst, &md5_ctx);
        x509_ctx->digest = bi_import(bi_ctx, md5_dgst, MD5_SIZE);
    }
    else if (x509_ctx->sig_type == SIG_TYPE_SHA1)
    {
        SHA1_CTX sha_ctx;
        uint8_t sha_dgst[SHA1_SIZE];
        SHA1_Init(&sha_ctx);
        SHA1_Update(&sha_ctx, &cert[begin_tbs], end_tbs-begin_tbs);
        SHA1_Final(sha_dgst, &sha_ctx);
        x509_ctx->digest = bi_import(bi_ctx, sha_dgst, SHA1_SIZE);
    }
    else if (x509_ctx->sig_type == SIG_TYPE_SHA256)
    {
        uint8_t sha_dgst[SHA256_SIZE];
        SSL_Sha256_ComputeDigest(&cert[begin_tbs], end_tbs-begin_tbs, sha_dgst);
        x509_ctx->digest = bi_import(bi_ctx, sha_dgst, SHA256_SIZE);
    }
    else if (x509_ctx->sig_type == SIG_TYPE_MD2)
    {
        MD2_CTX md2_ctx;
        uint8_t md2_dgst[MD2_SIZE];
        MD2_Init(&md2_ctx);
        MD2_Update(&md2_ctx, &cert[begin_tbs], end_tbs-begin_tbs);
        MD2_Final(md2_dgst, &md2_ctx);
        x509_ctx->digest = bi_import(bi_ctx, md2_dgst, MD2_SIZE);
    }

    if (cert[offset] == ASN1_V3_DATA)
    {
        int suboffset;

        ++offset;
        get_asn1_length(cert, &offset);

        if ((suboffset = asn1_find_subjectaltname(cert, offset)) > 0)
        {
            if (asn1_next_obj(cert, &suboffset, ASN1_OCTET_STRING) > 0)
            {
                int altlen;

                if ((altlen = asn1_next_obj(cert, 
                                            &suboffset, ASN1_SEQUENCE)) > 0)
                {
                    int endalt = suboffset + altlen;
                    int totalnames = 0;

                    while (suboffset < endalt)
                    {
                        int type = cert[suboffset++];
                        int dnslen = get_asn1_length(cert, &suboffset);

                        if (type == ASN1_CONTEXT_DNSNAME)
                        {
                            x509_ctx->subject_alt_dnsnames = (char**)
                                    realloc(x509_ctx->subject_alt_dnsnames, 
                                       (totalnames + 2) * sizeof(char*));
                            x509_ctx->subject_alt_dnsnames[totalnames] = 
                                    (char*)malloc(dnslen + 1);
                            x509_ctx->subject_alt_dnsnames[totalnames+1] = NULL;
                            memcpy(x509_ctx->subject_alt_dnsnames[totalnames], 
                                    cert + suboffset, dnslen);
                            x509_ctx->subject_alt_dnsnames[
                                    totalnames][dnslen] = 0;
                            ++totalnames;
                        }

                        suboffset += dnslen;
                    }
                }
            }
        }
    }

    offset = end_tbs;   /* skip the rest of v3 data */
    if (asn1_skip_obj(cert, &offset, ASN1_SEQUENCE) || 
            asn1_signature(cert, &offset, x509_ctx))
        goto end_cert;
        
    /* GBG: compute the fingerprints */
    {
        MD5_CTX md5_ctx;
        MD5_Init(&md5_ctx);
        MD5_Update(&md5_ctx, cert, cert_size);
        MD5_Final(x509_ctx->fingerprint.md5, &md5_ctx);
    }
    {
        SHA1_CTX sha1_ctx;
        SHA1_Init(&sha1_ctx);
        SHA1_Update(&sha1_ctx, cert, cert_size);
        SHA1_Final(x509_ctx->fingerprint.sha1, &sha1_ctx);
    }
    /* /GBG: compute the fingerprints */
        
#endif
    ret = X509_OK;
end_cert:
    if (len)
    {
        *len = cert_size;
    }

    if (ret)
    {
#ifdef CONFIG_SSL_FULL_MODE
        printf("Error: Invalid X509 ASN.1 file (%s)\n",
                        x509_display_error(ret));
#endif
        x509_free(x509_ctx);
        *ctx = NULL;
    }

    return ret;
}

/**
 * Free an X.509 object's resources.
 */
void x509_free(X509_CTX *x509_ctx)
{
    X509_CTX *next;
    int i;

    if (x509_ctx == NULL)       /* if already null, then don't bother */
        return;

    for (i = 0; i < X509_NUM_DN_TYPES; i++)
    {
        free(x509_ctx->ca_cert_dn[i]);
        free(x509_ctx->cert_dn[i]);
    }

    free(x509_ctx->signature);

#ifdef CONFIG_SSL_CERT_VERIFICATION 
    if (x509_ctx->digest)
    {
        bi_free(x509_ctx->rsa_ctx->bi_ctx, x509_ctx->digest);
    }

    if (x509_ctx->subject_alt_dnsnames)
    {
        for (i = 0; x509_ctx->subject_alt_dnsnames[i]; ++i)
            free(x509_ctx->subject_alt_dnsnames[i]);

        free(x509_ctx->subject_alt_dnsnames);
    }
#endif

    RSA_free(x509_ctx->rsa_ctx);
    next = x509_ctx->next;
    free(x509_ctx);
    x509_free(next);        /* clear the chain */
}

#ifdef CONFIG_SSL_CERT_VERIFICATION
/**
 * Take a signature and decrypt it.
 */
static bigint *sig_verify(BI_CTX *ctx, const uint8_t *sig, int sig_len,
        bigint *modulus, bigint *pub_exp)
{
    int i, size;
    bigint *decrypted_bi, *dat_bi;
    bigint *bir = NULL;
    uint8_t *block = (uint8_t *)alloca(sig_len);

    /* decrypt */
    dat_bi = bi_import(ctx, sig, sig_len);
    ctx->mod_offset = BIGINT_M_OFFSET;

    /* convert to a normal block */
    decrypted_bi = bi_mod_power2(ctx, dat_bi, modulus, pub_exp);

    bi_export(ctx, decrypted_bi, block, sig_len);
    ctx->mod_offset = BIGINT_M_OFFSET;

    i = 10; /* start at the first possible non-padded byte */
    while (block[i++] && i < sig_len);
    size = sig_len - i;

    /* get only the bit we want */
    if (size > 0)
    {
        int len;
        const uint8_t *sig_ptr = get_signature(&block[i], &len);

        if (sig_ptr)
        {
            bir = bi_import(ctx, sig_ptr, len);
        }
    }

    /* save a few bytes of memory */
    bi_clear_cache(ctx);
    return bir;
}

/**
 * Do some basic checks on the certificate chain.
 *
 * Certificate verification consists of a number of checks:
 * - The date of the certificate is after the start date.
 * - The date of the certificate is before the finish date.
 * - A root certificate exists in the certificate store.
 * - That the certificate(s) are not self-signed.
 * - The certificate chain is valid.
 * - The signature of the certificate is valid.
 */
int x509_verify(X509_CTX* ca_certs /* GBG: changed */, const X509_CTX *cert, const SSL_DateTime* now) 
{
    int ret = X509_OK;
    bigint *cert_sig;
    X509_CTX *next_cert = NULL;
    BI_CTX *ctx = NULL;
    bigint *mod = NULL, *expn = NULL;
    int match_ca_cert = 0;
    uint8_t is_self_signed = 0;
    SSL_DateTime now_dt;

    if (cert == NULL)
    {
        ret = X509_VFY_ERROR_NO_TRUSTED_CERT;       
        goto end_verify;
    }

    /* a self-signed certificate that is not in the CA store - use this 
       to check the signature */
    if (asn1_compare_dn(cert->ca_cert_dn, cert->cert_dn) == 0)
    {
        is_self_signed = 1;
        ctx = cert->rsa_ctx->bi_ctx;
        mod = cert->rsa_ctx->m;
        expn = cert->rsa_ctx->e;
    }

    /* if the time was not passed in, get it now */
    if (now == NULL) {
        SSL_DateTime_Now(&now_dt);
        now = &now_dt;
    }

    /* check the not before date */
    if (SSL_DateTime_Before(now, &cert->not_before))
    {
        ret = X509_VFY_ERROR_NOT_YET_VALID;
        goto end_verify;
    }

    /* check the not after date */
    if (SSL_DateTime_Before(&cert->not_after, now))
    {
        ret = X509_VFY_ERROR_EXPIRED;
        goto end_verify;
    }

    next_cert = cert->next;

    /* last cert in the chain - look for a trusted cert */
    if (next_cert == NULL)
    {
        /* GBG: modified */
        X509_CTX* ca_cert = ca_certs;
        while (ca_cert) {
            if (asn1_compare_dn(cert->ca_cert_dn, ca_cert->cert_dn) == 0) {
                /* use this CA certificate for signature verification */
                match_ca_cert = 1;
                ctx  = ca_cert->rsa_ctx->bi_ctx;
                mod  = ca_cert->rsa_ctx->m;
                expn = ca_cert->rsa_ctx->e;
                break;
            }
            ca_cert = ca_cert->next;
        }

       /* couldn't find a trusted cert (& let self-signed errors be returned) */
        if (!match_ca_cert && !is_self_signed)
        {
            ret = X509_VFY_ERROR_NO_TRUSTED_CERT;       
            goto end_verify;
        }
    }
    else if (asn1_compare_dn(cert->ca_cert_dn, next_cert->cert_dn) != 0)
    {
        /* check the chain */
        ret = X509_VFY_ERROR_INVALID_CHAIN;
        goto end_verify;
    }
    else /* use the next certificate in the chain for signature verify */
    {
        ctx = next_cert->rsa_ctx->bi_ctx;
        mod = next_cert->rsa_ctx->m;
        expn = next_cert->rsa_ctx->e;
    }

    /* cert is self signed */
    if (!match_ca_cert && is_self_signed)
    {
        ret = X509_VFY_ERROR_SELF_SIGNED;
        goto end_verify;
    }

    /* check the signature */
    cert_sig = sig_verify(ctx, cert->signature, cert->sig_len, 
                        bi_clone(ctx, mod), bi_clone(ctx, expn));

    if (cert_sig && cert->digest)
    {
        if (bi_compare(cert_sig, cert->digest) != 0)
            ret = X509_VFY_ERROR_BAD_SIGNATURE;


        bi_free(ctx, cert_sig);
    }
    else
    {
        ret = X509_VFY_ERROR_BAD_SIGNATURE;
    }

    if (ret)
        goto end_verify;

    /* go down the certificate chain using recursion. */
    if (next_cert != NULL)
    {
        ret = x509_verify(ca_certs, next_cert, now);
    }

end_verify:
    return ret;
}
#endif

#if defined (CONFIG_SSL_FULL_MODE)
/**
 * Used for diagnostics.
 */
static const char *not_part_of_cert = "<Not Part Of Certificate>";
void x509_print(const X509_CTX *cert, CA_CERT_CTX *ca_cert_ctx) 
{
    if (cert == NULL)
        return;

    printf("=== CERTIFICATE ISSUED TO ===\n");
    printf("Common Name (CN):\t\t");
    printf("%s\n", cert->cert_dn[X509_COMMON_NAME] ?
                    cert->cert_dn[X509_COMMON_NAME] : not_part_of_cert);

    printf("Organization (O):\t\t");
    printf("%s\n", cert->cert_dn[X509_ORGANIZATION] ?
        cert->cert_dn[X509_ORGANIZATION] : not_part_of_cert);

    printf("Organizational Unit (OU):\t");
    printf("%s\n", cert->cert_dn[X509_ORGANIZATIONAL_UNIT] ?
        cert->cert_dn[X509_ORGANIZATIONAL_UNIT] : not_part_of_cert);

    printf("=== CERTIFICATE ISSUED BY ===\n");
    printf("Common Name (CN):\t\t");
    printf("%s\n", cert->ca_cert_dn[X509_COMMON_NAME] ?
                    cert->ca_cert_dn[X509_COMMON_NAME] : not_part_of_cert);

    printf("Organization (O):\t\t");
    printf("%s\n", cert->ca_cert_dn[X509_ORGANIZATION] ?
        cert->ca_cert_dn[X509_ORGANIZATION] : not_part_of_cert);

    printf("Organizational Unit (OU):\t");
    printf("%s\n", cert->ca_cert_dn[X509_ORGANIZATIONAL_UNIT] ?
        cert->ca_cert_dn[X509_ORGANIZATIONAL_UNIT] : not_part_of_cert);

    printf("Not Before:\t\t\t%s", ctime(&cert->not_before));
    printf("Not After:\t\t\t%s", ctime(&cert->not_after));
    printf("RSA bitsize:\t\t\t%d\n", cert->rsa_ctx->num_octets*8);
    printf("Sig Type:\t\t\t");
    switch (cert->sig_type)
    {
        case SIG_TYPE_MD5:
            printf("MD5\n");
            break;
        case SIG_TYPE_SHA1:
            printf("SHA1\n");
            break;
        case SIG_TYPE_MD2:
            printf("MD2\n");
            break;
        default:
            printf("Unrecognized: %d\n", cert->sig_type);
            break;
    }

    if (ca_cert_ctx)
    {
        printf("Verify:\t\t\t\t%s\n",
                x509_display_error(x509_verify(ca_cert_ctx, cert)));
    }

#if 0
    print_blob("Signature", cert->signature, cert->sig_len);
    bi_print("Modulus", cert->rsa_ctx->m);
    bi_print("Pub Exp", cert->rsa_ctx->e);
#endif

    if (ca_cert_ctx)
    {
        x509_print(cert->next, ca_cert_ctx);
    }

    TTY_FLUSH();
}

const char * x509_display_error(int error)
{
    switch (error)
    {
        case X509_OK:
            return "Certificate verify successful";

        case X509_NOT_OK:
            return "X509 not ok";

        case X509_VFY_ERROR_NO_TRUSTED_CERT:
            return "No trusted cert is available";

        case X509_VFY_ERROR_BAD_SIGNATURE:
            return "Bad signature";

        case X509_VFY_ERROR_NOT_YET_VALID:
            return "Cert is not yet valid";

        case X509_VFY_ERROR_EXPIRED:
            return "Cert has expired";

        case X509_VFY_ERROR_SELF_SIGNED:
            return "Cert is self-signed";

        case X509_VFY_ERROR_INVALID_CHAIN:
            return "Chain is invalid (check order of certs)";

        case X509_VFY_ERROR_UNSUPPORTED_DIGEST:
            return "Unsupported digest";

        case X509_INVALID_PRIV_KEY:
            return "Invalid private key";

        default:
            return "Unknown";
    }
}
#endif      /* CONFIG_SSL_FULL_MODE */

