/*
CryptoJS v3.1.2
code.google.com/p/crypto-js
(c) 2009-2013 by Jeff Mott. All rights reserved.
code.google.com/p/crypto-js/wiki/License
*/
CryptoJS.mode.CFB=function(){function g(c,b,e,a){var d=this._iv;d?(d=d.slice(0),this._iv=void 0):d=this._prevBlock;a.encryptBlock(d,0);for(a=0;a<e;a++)c[b+a]^=d[a]}var f=CryptoJS.lib.BlockCipherMode.extend();f.Encryptor=f.extend({processBlock:function(c,b){var e=this._cipher,a=e.blockSize;g.call(this,c,b,a,e);this._prevBlock=c.slice(b,b+a)}});f.Decryptor=f.extend({processBlock:function(c,b){var e=this._cipher,a=e.blockSize,d=c.slice(b,b+a);g.call(this,c,b,a,e);this._prevBlock=d}});return f}();
