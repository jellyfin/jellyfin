webcomponents.js
================

[![Join the chat at https://gitter.im/webcomponents/webcomponentsjs](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/webcomponents/webcomponentsjs?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

A suite of polyfills supporting the [Web Components](http://webcomponents.org) specs:

**Custom Elements**: allows authors to define their own custom tags ([spec](https://w3c.github.io/webcomponents/spec/custom/)).

**HTML Imports**: a way to include and reuse HTML documents via other HTML documents ([spec](https://w3c.github.io/webcomponents/spec/imports/)).

**Shadow DOM**: provides encapsulation by hiding DOM subtrees under shadow roots ([spec](https://w3c.github.io/webcomponents/spec/shadow/)).

This also folds in polyfills for `MutationObserver` and `WeakMap`.


## Releases

Pre-built (concatenated & minified) versions of the polyfills are maintained in the [tagged versions](https://github.com/webcomponents/webcomponentsjs/releases) of this repo. There are two variants:

`webcomponents.js` includes all of the polyfills.

`webcomponents-lite.js` includes all polyfills except for shadow DOM.


## Browser Support

Our polyfills are intended to work in the latest versions of evergreen browsers. See below
for our complete browser support matrix:

| Polyfill   | IE10 | IE11+ | Chrome* | Firefox* | Safari 7+* | Chrome Android* | Mobile Safari* |
| ---------- |:----:|:-----:|:-------:|:--------:|:----------:|:---------------:|:--------------:|
| Custom Elements | ~ | ✓ | ✓ | ✓ | ✓ | ✓| ✓ |
| HTML Imports | ~ | ✓ | ✓ | ✓ | ✓| ✓| ✓ |
| Shadow DOM | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Templates | ✓ | ✓ | ✓ | ✓| ✓ | ✓ | ✓ |


*Indicates the current version of the browser

~Indicates support may be flaky. If using Custom Elements or HTML Imports with Shadow DOM,
you will get the non-flaky Mutation Observer polyfill that Shadow DOM includes.

The polyfills may work in older browsers, however require additional polyfills (such as classList)
to be used. We cannot guarantee support for browsers outside of our compatibility matrix.


### Manually Building

If you wish to build the polyfills yourself, you'll need `node` and `gulp` on your system:

 * install [node.js](http://nodejs.org/) using the instructions on their website
 * use `npm` to install [gulp.js](http://gulpjs.com/): `npm install -g gulp`

Now you are ready to build the polyfills with:

    # install dependencies
    npm install
    # build
    gulp build

The builds will be placed into the `dist/` directory.

## Contribute

See the [contributing guide](CONTRIBUTING.md)

## License

Everything in this repository is BSD style license unless otherwise specified.

Copyright (c) 2015 The Polymer Authors. All rights reserved.

## Helper utilities

### `WebComponentsReady`

Under native HTML Imports, `<script>` tags in the main document block the loading of such imports. This is to ensure the imports have loaded and any registered elements in them have been upgraded. 

The webcomponents.js and webcomponents-lite.js polyfills parse element definitions and handle their upgrade asynchronously. If prematurely fetching the element from the DOM before it has an opportunity to upgrade, you'll be working with an `HTMLUnknownElement`. 

For these situations (or when you need an approximate replacement for the Polymer 0.5 `polymer-ready` behavior), you can use the `WebComponentsReady` event as a signal before interacting with the element. The criteria for this event to fire is all Custom Elements with definitions registered by the time HTML Imports available at load time have loaded have upgraded.

```js
window.addEventListener('WebComponentsReady', function(e) {
  // imports are loaded and elements have been registered
  console.log('Components are ready');
});
```

## Known Issues

  * [Custom element's constructor property is unreliable](#constructor)
  * [Contenteditable elements do not trigger MutationObserver](#contentedit)
  * [ShadowCSS: :host-context(...):host(...) doesn't work](#hostcontext)
  * [execCommand isn't supported under Shadow DOM](#execcommand)

### Custom element's constructor property is unreliable <a id="constructor"></a>
See [#215](https://github.com/webcomponents/webcomponentsjs/issues/215) for background.

In Safari and IE, instances of Custom Elements have a `constructor` property of `HTMLUnknownElementConstructor` and `HTMLUnknownElement`, respectively. It's unsafe to rely on this property for checking element types.

It's worth noting that `customElement.__proto__.__proto__.constructor` is `HTMLElementPrototype` and that the prototype chain isn't modified by the polyfills(onto `ElementPrototype`, etc.)

### Contenteditable elements do not trigger MutationObserver <a id="contentedit"></a>
Using the MutationObserver polyfill, it isn't possible to monitor mutations of an element marked `contenteditable`.
See [the mailing list](https://groups.google.com/forum/#!msg/polymer-dev/LHdtRVXXVsA/v1sGoiTYWUkJ)

### ShadowCSS: :host-context(...):host(...) doesn't work <a id="hostcontext"></a>
See [#16](https://github.com/webcomponents/webcomponentsjs/issues/16) for background.

Under the shadow DOM polyfill, rules like:
```
:host-context(.foo):host(.bar) {...}
```
don't work, despite working under native Shadow DOM. The solution is to use `polyfill-next-selector` like:

```
polyfill-next-selector { content: '.foo :host.bar, :host.foo.bar'; }
```

### execCommand and contenteditable isn't supported under Shadow DOM <a id="execcommand"></a>
See [#212](https://github.com/webcomponents/webcomponentsjs/issues/212)

`execCommand`, and `contenteditable` aren't supported under the ShadowDOM polyfill, with commands that insert or remove nodes being especially prone to failure.
