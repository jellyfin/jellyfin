# getStyleProperty - quick & dirty CSS property testing

[Original by @kangax](https://github.com/kangax/cft/blob/gh-pages/getStyleProperty.js) :heart_eyes: :zap: :star2:. See [perfectionkills.com/feature-testing-css-properties/](http://perfectionkills.com/feature-testing-css-properties/)

``` js
var transformProp = getStyleProperty('transform');
// returns WebkitTransform on Chrome / Safari
// or transform on Firefox, or MozTransform on old firefox

// then you can use it when setting CSS
element.style[ transformProp ] = 'translate( 12px, 34px )';

// or simply check if its supported
var supportsTranforms = !!transformProp;
```

## Install

[Bower](http://bower.io) :bird:: `bower install get-style-property`

npm: `npm install desandro-get-style-property`

[Component](http://github.com/component/component): `component install desandro/get-style-property`

## MIT License

getStyleProperty is released under the [MIT License](http://desandro.mit-license.org/).
