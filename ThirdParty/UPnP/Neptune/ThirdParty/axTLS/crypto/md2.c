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

/*
 *  RFC 1115/1319 compliant MD2 implementation
 *  The MD2 algorithm was designed by Ron Rivest in 1989.
 *
 *  http://www.ietf.org/rfc/rfc1115.txt
 *  http://www.ietf.org/rfc/rfc1319.txt
 */

#include <string.h>
#include <stdio.h>
#include "os_port.h"
#include "crypto.h"

/**
 * This code is only here to enable the verification of Verisign root
 * certificates. So only enable it for verification mode.
 */
#ifdef CONFIG_SSL_CERT_VERIFICATION

static const uint8_t PI_SUBST[256] =
{
    0x29, 0x2E, 0x43, 0xC9, 0xA2, 0xD8, 0x7C, 0x01, 0x3D, 0x36,
    0x54, 0xA1, 0xEC, 0xF0, 0x06, 0x13, 0x62, 0xA7, 0x05, 0xF3,
    0xC0, 0xC7, 0x73, 0x8C, 0x98, 0x93, 0x2B, 0xD9, 0xBC, 0x4C,
    0x82, 0xCA, 0x1E, 0x9B, 0x57, 0x3C, 0xFD, 0xD4, 0xE0, 0x16,
    0x67, 0x42, 0x6F, 0x18, 0x8A, 0x17, 0xE5, 0x12, 0xBE, 0x4E,
    0xC4, 0xD6, 0xDA, 0x9E, 0xDE, 0x49, 0xA0, 0xFB, 0xF5, 0x8E,
    0xBB, 0x2F, 0xEE, 0x7A, 0xA9, 0x68, 0x79, 0x91, 0x15, 0xB2,
    0x07, 0x3F, 0x94, 0xC2, 0x10, 0x89, 0x0B, 0x22, 0x5F, 0x21,
    0x80, 0x7F, 0x5D, 0x9A, 0x5A, 0x90, 0x32, 0x27, 0x35, 0x3E,
    0xCC, 0xE7, 0xBF, 0xF7, 0x97, 0x03, 0xFF, 0x19, 0x30, 0xB3,
    0x48, 0xA5, 0xB5, 0xD1, 0xD7, 0x5E, 0x92, 0x2A, 0xAC, 0x56,
    0xAA, 0xC6, 0x4F, 0xB8, 0x38, 0xD2, 0x96, 0xA4, 0x7D, 0xB6,
    0x76, 0xFC, 0x6B, 0xE2, 0x9C, 0x74, 0x04, 0xF1, 0x45, 0x9D,
    0x70, 0x59, 0x64, 0x71, 0x87, 0x20, 0x86, 0x5B, 0xCF, 0x65,
    0xE6, 0x2D, 0xA8, 0x02, 0x1B, 0x60, 0x25, 0xAD, 0xAE, 0xB0,
    0xB9, 0xF6, 0x1C, 0x46, 0x61, 0x69, 0x34, 0x40, 0x7E, 0x0F,
    0x55, 0x47, 0xA3, 0x23, 0xDD, 0x51, 0xAF, 0x3A, 0xC3, 0x5C,
    0xF9, 0xCE, 0xBA, 0xC5, 0xEA, 0x26, 0x2C, 0x53, 0x0D, 0x6E,
    0x85, 0x28, 0x84, 0x09, 0xD3, 0xDF, 0xCD, 0xF4, 0x41, 0x81,
    0x4D, 0x52, 0x6A, 0xDC, 0x37, 0xC8, 0x6C, 0xC1, 0xAB, 0xFA,
    0x24, 0xE1, 0x7B, 0x08, 0x0C, 0xBD, 0xB1, 0x4A, 0x78, 0x88,
    0x95, 0x8B, 0xE3, 0x63, 0xE8, 0x6D, 0xE9, 0xCB, 0xD5, 0xFE,
    0x3B, 0x00, 0x1D, 0x39, 0xF2, 0xEF, 0xB7, 0x0E, 0x66, 0x58,
    0xD0, 0xE4, 0xA6, 0x77, 0x72, 0xF8, 0xEB, 0x75, 0x4B, 0x0A,
    0x31, 0x44, 0x50, 0xB4, 0x8F, 0xED, 0x1F, 0x1A, 0xDB, 0x99,
    0x8D, 0x33, 0x9F, 0x11, 0x83, 0x14
};

/*
 * MD2 context setup
 */
EXP_FUNC void STDCALL MD2_Init(MD2_CTX *ctx)
{
    memset(ctx, 0, sizeof *ctx);
}

static void md2_process(MD2_CTX *ctx)
{
    int i, j;
    uint8_t t = 0;

    for (i = 0; i < 16; i++)
    {
        ctx->state[i + 16] = ctx->buffer[i];
        ctx->state[i + 32] = ctx->buffer[i] ^ ctx->state[i];
    }

    for (i = 0; i < 18; i++)
    {
        for (j = 0; j < 48; j++)
            t = (ctx->state[j] ^= PI_SUBST[t]);

        t = (t + i) & 0xFF;
    }

    t = ctx->cksum[15];

    for (i = 0; i < 16; i++)
        t = (ctx->cksum[i] ^= PI_SUBST[ctx->buffer[i] ^ t]);
}

/*
 * MD2 process buffer
 */
EXP_FUNC void STDCALL MD2_Update(MD2_CTX *ctx, const uint8_t *input, int ilen)
{
    int fill;

    while (ilen > 0)
    {
        if (ctx->left + ilen > 16)
            fill = 16 - ctx->left;
        else
            fill = ilen;

        memcpy(ctx->buffer + ctx->left, input, fill);

        ctx->left += fill;
        input += fill;
        ilen  -= fill;

        if (ctx->left == 16)
        {
            ctx->left = 0;
            md2_process(ctx);
        }
    }
}

/*
 * MD2 final digest
 */
EXP_FUNC void STDCALL MD2_Final(uint8_t *output, MD2_CTX *ctx)
{
    int i;
    uint8_t x;

    x = (uint8_t)(16 - ctx->left);

    for (i = ctx->left; i < 16; i++)
        ctx->buffer[i] = x;

    md2_process(ctx);

    memcpy(ctx->buffer, ctx->cksum, 16);
    md2_process(ctx);

    memcpy(output, ctx->state, 16);
}

#endif
