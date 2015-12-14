/*
CryptoJS v3.1.2
code.google.com/p/crypto-js
(c) 2009-2013 by Jeff Mott. All rights reserved.
code.google.com/p/crypto-js/wiki/License
*/
/*

 Counter block mode compatible with  Dr Brian Gladman fileenc.c
 derived from CryptoJS.mode.CTR 
 Jan Hruby jhruby.web@gmail.com
*/
CryptoJS.mode.CTRGladman=function(){function h(a){if(255===(a>>24&255)){var c=a>>16&255,b=a>>8&255,e=a&255;255===c?(c=0,255===b?(b=0,255===e?e=0:++e):++b):++c;a=0+(c<<16)+(b<<8);a+=e}else a+=16777216;return a}var g=CryptoJS.lib.BlockCipherMode.extend(),j=g.Encryptor=g.extend({processBlock:function(a,c){var b=this._cipher,e=b.blockSize,d=this._iv,f=this._counter;d&&(f=this._counter=d.slice(0),this._iv=void 0);d=f;if(0===(d[0]=h(d[0])))d[1]=h(d[1]);f=f.slice(0);b.encryptBlock(f,0);for(b=0;b<e;b++)a[c+
b]^=f[b]}});g.Decryptor=j;return g}();
