# eventie - event binding helper

Makes dealing with events in IE8 bearable. Supported by IE8+ and good browsers.

``` js
var elem = document.querySelector('#my-elem');
function onElemClick( event ) {
  console.log( event.type + ' just happened on #' + event.target.id );
  // -> click just happened on #my-elem
}

eventie.bind( elem, 'click', onElemClick );

eventie.unbind( elem, 'click', onElemClick );
```

## Install

Download [eventie.js](eventie.js)

Install with [Bower :bird:](http://bower.io) `bower install eventie`

Install with npm :truck: `npm install eventie`

Install with [Component :nut_and_bolt:](https://github.com/component/component) `component install desandro/eventie`

## IE 8

eventie add support for `event.target` and [`.handleEvent` method](https://developer.mozilla.org/en-US/docs/DOM/EventListener#handleEvent\(\)) for Internet Explorer 8.

## MIT license

eventie is released under the [MIT license](http://desandro.mit-license.org).
