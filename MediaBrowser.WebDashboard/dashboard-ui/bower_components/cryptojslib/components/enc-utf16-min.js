/*
CryptoJS v3.1.2
code.google.com/p/crypto-js
(c) 2009-2013 by Jeff Mott. All rights reserved.
code.google.com/p/crypto-js/wiki/License
*/
(function(){var e=CryptoJS,f=e.lib.WordArray,e=e.enc;e.Utf16=e.Utf16BE={stringify:function(b){var d=b.words;b=b.sigBytes;for(var c=[],a=0;a<b;a+=2)c.push(String.fromCharCode(d[a>>>2]>>>16-8*(a%4)&65535));return c.join("")},parse:function(b){for(var d=b.length,c=[],a=0;a<d;a++)c[a>>>1]|=b.charCodeAt(a)<<16-16*(a%2);return f.create(c,2*d)}};e.Utf16LE={stringify:function(b){var d=b.words;b=b.sigBytes;for(var c=[],a=0;a<b;a+=2)c.push(String.fromCharCode((d[a>>>2]>>>16-8*(a%4)&65535)<<8&4278255360|(d[a>>>
2]>>>16-8*(a%4)&65535)>>>8&16711935));return c.join("")},parse:function(b){for(var d=b.length,c=[],a=0;a<d;a++){var e=c,g=a>>>1,j=e[g],h=b.charCodeAt(a)<<16-16*(a%2);e[g]=j|h<<8&4278255360|h>>>8&16711935}return f.create(c,2*d)}}})();
