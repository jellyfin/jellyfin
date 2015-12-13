/*
CryptoJS v3.1.2
code.google.com/p/crypto-js
(c) 2009-2013 by Jeff Mott. All rights reserved.
code.google.com/p/crypto-js/wiki/License
*/
CryptoJS.pad.Iso97971={pad:function(a,b){a.concat(CryptoJS.lib.WordArray.create([2147483648],1));CryptoJS.pad.ZeroPadding.pad(a,b)},unpad:function(a){CryptoJS.pad.ZeroPadding.unpad(a);a.sigBytes--}};
