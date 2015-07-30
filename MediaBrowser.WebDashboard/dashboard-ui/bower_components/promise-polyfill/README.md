<a href="http://promises-aplus.github.com/promises-spec">
    <img src="http://promises-aplus.github.com/promises-spec/assets/logo-small.png"
         align="right" alt="Promises/A+ logo" />
</a>
Promise [![Build Status](https://travis-ci.org/taylorhakes/promise-polyfill.png?branch=master)](https://travis-ci.org/taylorhakes/promise-polyfill)
=============

Lightweight promise polyfill for the browser and node. A+ Compliant. It is a perfect polyfill IE, Firefox or any other browser that does not support native promises.

This implementation is based on [then/promise](https://github.com/then/promise). It has been changed to use the prototype for performance and memory reasons.

For API information about Promises, please check out this article [HTML5Rocks article](http://www.html5rocks.com/en/tutorials/es6/promises/).

It is extremely lightweight. ***< 1kb Gzipped***

## Browser Support
IE8+, Chrome, Firefox, IOS 4+, Safari 5+, Opera

## Downloads

- [Promise](https://raw.github.com/taylorhakes/promise-polyfill/master/Promise.js)
- [Promise-min](https://raw.github.com/taylorhakes/promise-polyfill/master/Promise.min.js)

### Node
```
npm install promise-polyfill
```
### Bower
```
bower install promise-polyfill
```

## Simple use
```
var prom = new Promise(function(resolve, reject) {
  // do a thing, possibly async, then…

  if (/* everything turned out fine */) {
    resolve("Stuff worked!");
  }  else {
    reject(new Error("It broke"));
  }
});

// Do something when async done
prom.then(function() {
  ...
});
```
## Performance
By default promise-polyfill uses `setImmediate`, but falls back to `setTimeout` for executing asynchronously. If a browser does not support `setImmediate`, you may see performance issues.
Use a `setImmediate` polyfill to fix this issue. [setAsap](https://github.com/taylorhakes/setAsap) or [setImmediate](https://github.com/YuzuJS/setImmediate) work well.

If you polyfill `window.setImmediate` or use `Promise._setImmediateFn(immedateFn)` it will be used instead of `window.setTimeout`

## Testing
```
npm install
npm test
```

## License
MIT
