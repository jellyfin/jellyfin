/*
 *
 * This file contains an adaptation of the AES decryption algorithm
 * from the Standford Javascript Cryptography Library. That work is
 * covered by the following copyright and permissions notice:
 *
 * Copyright 2009-2010 Emily Stark, Mike Hamburg, Dan Boneh.
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are
 * met:
 *
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 *
 * 2. Redistributions in binary form must reproduce the above
 *    copyright notice, this list of conditions and the following
 *    disclaimer in the documentation and/or other materials provided
 *    with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHORS ``AS IS'' AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
 * BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE
 * OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN
 * IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * The views and conclusions contained in the software and documentation
 * are those of the authors and should not be interpreted as representing
 * official policies, either expressed or implied, of the authors.
 */

import AES from './aes';

class AES128Decrypter {

  constructor(key, initVector) {
    this.key = key;
    this.iv = initVector;
  }

  /**
   * Convert network-order (big-endian) bytes into their little-endian
   * representation.
   */
  ntoh(word) {
    return (word << 24) |
      ((word & 0xff00) << 8) |
      ((word & 0xff0000) >> 8) |
      (word >>> 24);
  }


  /**
   * Decrypt bytes using AES-128 with CBC and PKCS#7 padding.
   * @param encrypted {Uint8Array} the encrypted bytes
   * @param key {Uint32Array} the bytes of the decryption key
   * @param initVector {Uint32Array} the initialization vector (IV) to
   * use for the first round of CBC.
   * @return {Uint8Array} the decrypted bytes
   *
   * @see http://en.wikipedia.org/wiki/Advanced_Encryption_Standard
   * @see http://en.wikipedia.org/wiki/Block_cipher_mode_of_operation#Cipher_Block_Chaining_.28CBC.29
   * @see https://tools.ietf.org/html/rfc2315
   */
  doDecrypt(encrypted, key, initVector) {
    var
      // word-level access to the encrypted bytes
      encrypted32 = new Int32Array(encrypted.buffer, encrypted.byteOffset, encrypted.byteLength >> 2),

    decipher = new AES(Array.prototype.slice.call(key)),

    // byte and word-level access for the decrypted output
    decrypted = new Uint8Array(encrypted.byteLength),
    decrypted32 = new Int32Array(decrypted.buffer),

    // temporary variables for working with the IV, encrypted, and
    // decrypted data
    init0, init1, init2, init3,
    encrypted0, encrypted1, encrypted2, encrypted3,

    // iteration variable
    wordIx;

    // pull out the words of the IV to ensure we don't modify the
    // passed-in reference and easier access
    init0 = ~~initVector[0];
    init1 = ~~initVector[1];
    init2 = ~~initVector[2];
    init3 = ~~initVector[3];

    // decrypt four word sequences, applying cipher-block chaining (CBC)
    // to each decrypted block
    for (wordIx = 0; wordIx < encrypted32.length; wordIx += 4) {
      // convert big-endian (network order) words into little-endian
      // (javascript order)
      encrypted0 = ~~this.ntoh(encrypted32[wordIx]);
      encrypted1 = ~~this.ntoh(encrypted32[wordIx + 1]);
      encrypted2 = ~~this.ntoh(encrypted32[wordIx + 2]);
      encrypted3 = ~~this.ntoh(encrypted32[wordIx + 3]);

      // decrypt the block
      decipher.decrypt(encrypted0,
          encrypted1,
          encrypted2,
          encrypted3,
          decrypted32,
          wordIx);

      // XOR with the IV, and restore network byte-order to obtain the
      // plaintext
      decrypted32[wordIx]     = this.ntoh(decrypted32[wordIx] ^ init0);
      decrypted32[wordIx + 1] = this.ntoh(decrypted32[wordIx + 1] ^ init1);
      decrypted32[wordIx + 2] = this.ntoh(decrypted32[wordIx + 2] ^ init2);
      decrypted32[wordIx + 3] = this.ntoh(decrypted32[wordIx + 3] ^ init3);

      // setup the IV for the next round
      init0 = encrypted0;
      init1 = encrypted1;
      init2 = encrypted2;
      init3 = encrypted3;
    }

    return decrypted;
  }

  localDecrypt(encrypted, key, initVector, decrypted) {
    var bytes = this.doDecrypt(encrypted,
        key,
        initVector);
    decrypted.set(bytes, encrypted.byteOffset);
  }

  decrypt(encrypted) {
    var
      step = 4 * 8000,
    //encrypted32 = new Int32Array(encrypted.buffer),
    encrypted32 = new Int32Array(encrypted),
    decrypted = new Uint8Array(encrypted.byteLength),
    i = 0;

    // split up the encryption job and do the individual chunks asynchronously
    var key = this.key;
    var initVector = this.iv;
    this.localDecrypt(encrypted32.subarray(i, i + step), key, initVector, decrypted);

    for (i = step; i < encrypted32.length; i += step) {
      initVector = new Uint32Array([
          this.ntoh(encrypted32[i - 4]),
          this.ntoh(encrypted32[i - 3]),
          this.ntoh(encrypted32[i - 2]),
          this.ntoh(encrypted32[i - 1])
      ]);
      this.localDecrypt(encrypted32.subarray(i, i + step), key, initVector, decrypted);
    }

    return decrypted;
  }
}

export default AES128Decrypter;
