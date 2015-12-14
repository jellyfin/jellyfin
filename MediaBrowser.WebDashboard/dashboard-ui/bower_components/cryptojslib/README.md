CryptoJS
--------

This repo is straight unmodified-in-any-way copy of Google Code hosted CryptoJS project at https://code.google.com/p/crypto-js/ . This is hosted at github to add bower package so future updates can be managed better.

### Directory Structure
You have two folders:
* components
* rollups

The files in rollups folder is concatenation of one or more files in components folder followed by minification. This makes files in rollups folder standalone includable in your projects without worrying about its dependencies. You can view relation between files in rollup and components here: https://code.google.com/p/crypto-js/source/browse/tags/3.1.2/builder/build.yml

### Install

If you are not using bower then just include the .js file from rollups folder for whatever algorithm you want to use. UTF8 encoder is included in each rollup js. If you need UTF16 or Base64 encoder then also add corresponding files from components folder (see following example).

Using Bower:

```
bower install cryptojslib
```

### APIs

You can play with below code live at http://jsbin.com/IziHAdIf/1/edit?html,console.

Please see the Quick Start guide at https://code.google.com/p/crypto-js/#Quick-start_Guide

Below are very quick examples of core usage:

##### MD5
MD5 is a widely used hash function. It's been used in a variety of security applications and is also commonly used to check the integrity of files. Though, MD5 is not collision resistant, and it isn't suitable for applications like SSL certificates or digital signatures that rely on this property.

```
<script src="http://<mysite>/<libs location>/cryptojs/rollups/md5.js"></script>
<script src="http://<mysite>/<libs location>/cryptojs/components/enc-base64-min.js"></script>
<script>
	//The hash algorithms accept either strings or instances of CryptoJS.lib.WordArray. 
	//A WordArray object represents an array of 32-bit words. 
	//When you pass a string, it's automatically converted to a WordArray encoded as UTF-8.
    var hash = CryptoJS.MD5("Message");
	alert(hash.toString(CryptoJS.enc.Base64));
</script>
```

##### SHA-3
SHA-3 is the winner of a five-year competition to select a new cryptographic hash algorithm where 64 competing designs were evaluated.

```
<script src="http://<mysite>/<libs location>/cryptojs/rollups/sha3.js"></script>
<script>
    var hash = CryptoJS.SHA3("Message");
	
	//The hash you get back isn't a string yet. It's a WordArray object. 
	//When you use a WordArray object in a string context, 
	//it's automatically converted to a hex string.	
	alert(hash.toString()); //Same as hash.toString(CryptoJS.enc.Hex);
</script>
```

##### Encoding and decoding
You can convert string to word arrays using various encoders. And word array in to string using decoders.

```
<script>
    var wordArray = CryptoJS.enc.Utf8.parse('𤭢');
    var utf8  = CryptoJS.enc.Utf8.stringify(wordArray);
    console.log(utf8);
</script>	
```

The UTF8 encoder/decoder is included in core.js and hence is available in rollup files for algorithms. However if you need UTF16 and Base64 encoder then you need to include corresponding file from components folder (see below for MD5 hash with Base64 example).


### Using with AMD/RequireJS 
CryptoJS does not have built-in support for AMD/RequireJS yet. However adding shims is almost trivial. For use with RequireJS, using files in components is probably more desirable instead of files in rollups folder because you probably already have setup RequireJS optimizer or other build process. To build the shim for RequireJS follow this steps:

- Identify algorithms and encoder/decoders you need.
- Look up relationship between components and rollups file here: https://code.google.com/p/crypto-js/source/browse/tags/3.1.2/builder/build.yml
- Write a shim for this relationship.

Here's the example: Let's say we want to use MD5 with Base64 encoder. The shim would look like this

```
require.config({
    paths: {
        'cryptojs.core': "path/to/cryptojs/components/core.js",
        'cryptojs.md5': "path/to/cryptojs/components/md5.js",
        'cryptojs.base64': "path/to/cryptojs/components/enc-base64.js"
    },
    shim: {
		'cryptojs.core': {
			exports: "CryptoJS"
		},
		'cryptojs.md5': {
			deps: ['cryptojs.core']
			exports: "CryptoJS"	//You can also use "CryptoJS.MD5"
		},
		'cryptojs.base64': {
			deps: ['cryptojs.core']
			exports: "CryptoJS"	//You can also use "CryptoJS.enc.Base64"
		}
    }
});
```

### Copyrights
Please see copyrights.txt which is copy of corresponding file from Google Code project.
