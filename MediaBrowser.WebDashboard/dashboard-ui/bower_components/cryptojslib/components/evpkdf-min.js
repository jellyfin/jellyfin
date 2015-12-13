/*
CryptoJS v3.1.2
code.google.com/p/crypto-js
(c) 2009-2013 by Jeff Mott. All rights reserved.
code.google.com/p/crypto-js/wiki/License
*/
(function(){var b=CryptoJS,a=b.lib,f=a.Base,k=a.WordArray,a=b.algo,l=a.EvpKDF=f.extend({cfg:f.extend({keySize:4,hasher:a.MD5,iterations:1}),init:function(a){this.cfg=this.cfg.extend(a)},compute:function(a,b){for(var c=this.cfg,d=c.hasher.create(),g=k.create(),f=g.words,h=c.keySize,c=c.iterations;f.length<h;){e&&d.update(e);var e=d.update(a).finalize(b);d.reset();for(var j=1;j<c;j++)e=d.finalize(e),d.reset();g.concat(e)}g.sigBytes=4*h;return g}});b.EvpKDF=function(a,b,c){return l.create(c).compute(a,
b)}})();
