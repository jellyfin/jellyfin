/*
CryptoJS v3.1.2
code.google.com/p/crypto-js
(c) 2009-2013 by Jeff Mott. All rights reserved.
code.google.com/p/crypto-js/wiki/License
*/
CryptoJS.pad.Iso10126={pad:function(a,c){var b=4*c,b=b-a.sigBytes%b;a.concat(CryptoJS.lib.WordArray.random(b-1)).concat(CryptoJS.lib.WordArray.create([b<<24],1))},unpad:function(a){a.sigBytes-=a.words[a.sigBytes-1>>>2]&255}};
