/*
CryptoJS v3.1.2
code.google.com/p/crypto-js
(c) 2009-2013 by Jeff Mott. All rights reserved.
code.google.com/p/crypto-js/wiki/License
*/
CryptoJS.mode.OFB=function(){var b=CryptoJS.lib.BlockCipherMode.extend(),d=b.Encryptor=b.extend({processBlock:function(b,e){var a=this._cipher,d=a.blockSize,f=this._iv,c=this._keystream;f&&(c=this._keystream=f.slice(0),this._iv=void 0);a.encryptBlock(c,0);for(a=0;a<d;a++)b[e+a]^=c[a]}});b.Decryptor=d;return b}();
