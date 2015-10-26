# matchesSelector helper

[`matches`/`matchesSelector`](https://developer.mozilla.org/en-US/docs/Web/API/Element/matches) is pretty hot :fire:, but has [vendor-prefix baggage](http://caniuse.com/#feat=matchesselector) :handbag: :pouch:. This helper function takes care of that, without augmenting `Element.prototype`.

``` js
matchesSelector( elem, selector );

matchesSelector( myElem, 'div.my-hawt-selector' );

// this DOES NOT polyfill myElem.matchesSelector
```

## Install

Download [matches-selector.js](https://github.com/desandro/matches-selector/raw/master/matches-selector.js)

Install with [Bower](http://bower.io): `bower install matches-selector`

[Install with npm](https://www.npmjs.org/package/desandro-matches-selector): `npm install desandro-matches-selector`

Install with [Component](https://github.com/component/component): `component install desandro/matches-selector`

## MIT license

matchesSelector is released under the [MIT license](http://desandro.mit-license.org)
