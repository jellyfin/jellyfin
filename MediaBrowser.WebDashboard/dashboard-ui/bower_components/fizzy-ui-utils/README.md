# Fizzy UI utils

UI utility & helper functions

Used in [Flickity](http://flickity.metafizzy.co), [Isotope](http://isotope.metafizzy.co), [Masonry](http://masonry.desandro.com), [Draggabilly](http://draggabilly.desandro.com)

## Install

Bower: `bower install fizzy-ui-utils --save`

npm: `npm install fizzy-ui-utils --save`

## API

``` js
// fizzyUIUtils is the browser global
var utils = fizzyUIUtils;

// ---- ---- //

utils.extend( a, b )
// extend object

utils.modulo( num, div )
// num [modulo] div

utils.isArray( obj )
// check if object is Array

utils.makeArray( obj )
// make array from object

utils.indexOf( ary, obj )
// get index of object in array

utils.removeFrom( ary, obj )
// remove object from array

utils.isElement( obj )
// check if object is an element

utils.setText( elem, text )
// set text of an element

utils.getParent( elem, selector )
// get parent element of an element, given a selector string

utils.getQueryElement( elem )
// if elem is a string, use it as a selector and return element

Class.prototype.handleEvent = utils.handleEvent;
// enable Class.onclick when element.addEventListener( 'click', this, false )

utils.filterFindElements( elems, selector )
// iterate through elems, filter and find all elements that match selector

utils.debounceMethod( Class, methodName, threhold )
// debounce a class method

utils.toDashed( str )
// 'camelCaseString' -> 'camel-case-string'

utils.htmlInit( Class, namespace )
// on document ready, initialize Class on every element
// that matches js-namespace
// pass in JSON options from element's data-options-namespace attribute
```

---

MIT license. Have at it.

By [Metafizzy](http://metafizzy.co)
