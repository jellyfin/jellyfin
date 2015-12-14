/*
CryptoJS v3.1.2
code.google.com/p/crypto-js
(c) 2009-2013 by Jeff Mott. All rights reserved.
code.google.com/p/crypto-js/wiki/License
*/
/**
 * ISO 10126 padding strategy.
 */
CryptoJS.pad.Iso10126 = {
    pad: function (data, blockSize) {
        // Shortcut
        var blockSizeBytes = blockSize * 4;

        // Count padding bytes
        var nPaddingBytes = blockSizeBytes - data.sigBytes % blockSizeBytes;

        // Pad
        data.concat(CryptoJS.lib.WordArray.random(nPaddingBytes - 1)).
             concat(CryptoJS.lib.WordArray.create([nPaddingBytes << 24], 1));
    },

    unpad: function (data) {
        // Get number of padding bytes from last byte
        var nPaddingBytes = data.words[(data.sigBytes - 1) >>> 2] & 0xff;

        // Remove padding
        data.sigBytes -= nPaddingBytes;
    }
};
