document-register-element
=========================

A stand-alone working lightweight version of the [W3C Custom Elements](http://w3c.github.io/webcomponents/spec/custom/) specification.


[![build status](https://travis-ci.org/WebReflection/document-register-element.svg)](https://travis-ci.org/WebReflection/document-register-element)


### How

`npm install document-register-element` will put [build/document-register-element.js](build/document-register-element.js) inside `node_modules/document-register-element/` of your project.

If you're working with a tool like Browserify, Webpack, RequireJS, etc, you can
import the script at some point before you need to use the API.

```js
import 'document-register-element' // ES2015
// or
require('document-register-element') // CommonJS
// or
define(['document-register-element'], function() {}) // AMD
```

If you're not using a module system, just place
`node_modules/document-register-element/build/document-register-element.js`
somewhere where it will be served by your server, then put

```html
<script src="/path/to/document-register-element.js"></script>
```

in your head element and you should be good to go.

#### via CDN
Many thanks to [cdnjs](http://www.cdnjs.com) for hosting this script. Following an example on how to include it.
```html
<script
  src="//cdnjs.cloudflare.com/ajax/libs/document-register-element/0.5.3/document-register-element.js"
>/* W3C Custom Elements */</script>
```

### TL;DR does it work ?
If you [see the first clock ticking](http://webreflection.github.io/document-register-element/test/examples/x-clock.html), the TL;DR answer is yes.


### Usage Example

A basic HTML example page
```html
<!DOCTYPE html>
<html>
<head>
  <title>testing my-element</title>
  <script src="js/document-register-element.js"></script>
  <script src="js/my-element.js"></script>
</head>
<body>
  <my-element>
    some content
  </my-element>
</body>
```

with the following `my-element.js` content
```javascript
var MyElement = document.registerElement(
  'my-element',
  {
    prototype: Object.create(
      HTMLElement.prototype, {
      createdCallback: {value: function() {
        console.log('here I am ^_^ ');
        console.log('with content: ', this.textContent);
      }},
      attachedCallback: {value: function() {
        console.log('live on DOM ;-) ');
      }},
      detachedCallback: {value: function() {
        console.log('leaving the DOM :-( )');
      }},
      attributeChangedCallback: {value: function(
        name, previousValue, value
      ) {
        if (previousValue == null) {
          console.log(
            'got a new attribute ', name,
            ' with value ', value
          );
        } else if (value == null) {
          console.log(
            'somebody removed ', name,
            ' its value was ', previousValue
          );
        } else {
          console.log(
            name,
            ' changed from ', previousValue,
            ' to ', value
          );
        }
      }}
    })
  }
);
```


### Why
I wrote a [couple](http://webreflection.blogspot.co.uk/2014/07/a-w3c-custom-elements-alternative.html) of blog [posts](http://webreflection.blogspot.co.uk/2015/03/bringing-custom-elements-to-ie8.html) about this polyfill, and here's the quick summary:

* [document-register-element.js](build/document-register-element.js) is a stand alone polyfill which aims to support as many browsers as possible, without requiring extra dependencies at all, all in about **3KB** minified and gzipped.

Add if you want the [dom4](https://github.com/WebReflection/dom4#dom4) normalizer, and you'll find yourself in a modern DOM environment that works reliably with today's browsers, with an eye always open on performance.



### Tested On

The [live test page](http://webreflection.github.io/document-register-element/test/) is here, containing all tests as listed in [the test file](test/document-register-element.js).

The following list of **desktop** browsers has been successfully tested:

  * Chrome
  * Firefox
  * IE 8 or greater (please read about IE8 caveats)
  * Safari
  * Opera

The following list of **mobile** OS has been successfully tested:

  * iOS 5.1 or greater
  * Android 2.2 or greater
  * FirefoxOS 1.1 or greater
  * KindleFire 3 or greater
  * Windows Phone 7 or greater
  * Opera Mobile 12 or greater
  * Blackberry OS 7* and OS 10
  * webOS 2 or LG TV
  * Samsung Bada OS 2 or greater
  * NOKIA Asha with Express Browser

The good old [BB OS 7](http://us.blackberry.com/software/smartphones/blackberry-7-os.html) is the only one failing the test with `className` which is not notified as `attributeChanged` when it's changed. This means BB OS 7 will also fail with `id`, however changing `id` at runtime has never been a common or useful pattern.


### Common Issues + Caveat
Here a list of gotchas you might encounter when developing *CustomElement* components.

#### HTML{TABLE|ROW|INPUT|SELECT|others...}Element
As described in [issue 6](https://github.com/WebReflection/document-register-element/issues/6) it's not possible to fully inherit a table, input, select, or other special element behaviors.
```js
// This will NOT work as expected
document.registerElement(
  'my-input',
  {
    prototype: Object.create(
      HTMLInputElement.prototype
    )
  }
);

var mi = document.createElement('my-input');
```

The correct way to properly implement a custom input that will be also backward compatible is the following one:
```js
// This will NOT work as expected
document.registerElement(
  'my-input',
  {
    extends: 'input', // <== IMPORTANT
    prototype: Object.create(
      HTMLInputElement.prototype
    )
  }
);

// how to create the input
var mi = document.createElement(
  'input',    // the extend
  'my-input'  // the enriched custom definition
);
```

Another approach is to use just a basic `HTMLElement` component and initialize its content at runtime.
```js
document.registerElement(
  'my-input',
  {
    prototype: Object.create(
      HTMLElement.prototype,
      {
        createdCallback: {value: function () {
          // here the input
          this.el = this.appendChild(
            document.createElement('input')
          );
        }}
      }
    )
  }
);

var mi = document.createElement('my-input');
```
In this case every method that wants to interact with the input will refer `this.el` instead of just `this`.


#### Using `innerHTML`
In order to avoid huge performance impact, native behavior overwrite problems and incompatibilities, there is now a [helper script](https://github.com/WebReflection/document-register-element/blob/master/build/innerHTML.max.js),
which aim is to make **off-line custom elements creation** possible using template strings instead of needing manual `document.createElement` replacements.

The helper is a simple `innerHTML` function that returns the given node, after setting `innerHTML` and, in case the polyfill is used, initialize nodes.

This helper is needed in order to be aligned with native implementations, but please remember that `createdCallback` could be asynchronous, even if triggered ASAP after injecting HTML through this function.



#### Changing the `style` property

If you change the style property via `node.style.cssText` or `node.style.backgroundColor = "red"` this change will most likely reflect through `node.getAttribute("style")`.

In order to prevent footguns inside `attributeChangedCallback` invocations causing potential stack overflows, the `style` property has been filtered starting from version `0.1.1`, also reflecting current native implementation where changing this special property won't invoke the callback.

(yes, even using `node.setAttribute("style", "value")` that you shouldn't ... just use `node.style.cssText = "value"` instead)


#### About IE8

Starting from version `0.2.0` there is an experimental support for IE8.
There is a specific file that needs to be loaded in IE8 only upfront, plus a sequence of polyfills
that will be simply ignored by every browser but downloaded in IE8.

Please check [base.html file](examples/base.html) in order to have a basic model to reuse in case you want to support IE8.

All tests pass and there is a [map component example](http://webreflection.github.io/document-register-element/test/examples/x-map.html) that already works in IE8 too.

Remember there are few things to consider when IE8 is a target but since it didn't cost many bytes
to have it in, I've decided to merge the logic and maintain only one file that will work in IE8 too.


#### IE8 caveats

  * it's IE8
  * all operations are batched and eventually executed ASAP but asynchronously. This behavior is closer to native Mutation Observers but might have some extra glitch in rendering time
  * `className` is right now the only special attribute that reacts. Others might be implemented in the [dre-ie8-upfront-fix.js](src/dre-ie8-upfront-fix.js) file.
  * in order to have node reacting to attributes changes, these must be live on the DOM
  * if you are using `extends` when create a custom element, remember to minify the production code or wrap such reserved word in quotes

