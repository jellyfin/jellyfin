/*
CryptoJS v3.1.2
code.google.com/p/crypto-js
(c) 2009-2013 by Jeff Mott. All rights reserved.
code.google.com/p/crypto-js/wiki/License
*/
CryptoJS.pad.AnsiX923={pad:function(a,d){var b=a.sigBytes,c=4*d,c=c-b%c,b=b+c-1;a.clamp();a.words[b>>>2]|=c<<24-8*(b%4);a.sigBytes+=c},unpad:function(a){a.sigBytes-=a.words[a.sigBytes-1>>>2]&255}};
