# Changelog

### v3.3.1

+ Updated Outlayer v1.4.1
  + Added jQuery events
  + Fixed Safari layout and transition bugs. Fixed [#698](https://github.com/desandro/masonry/issues/698)

## v3.3.0

+ Added `percentPosition` option. Fixed [#574](https://github.com/desandro/masonry/issues/574)
+ Removed first `instance` argument from `layoutComplete` and `removeComplete` events
+ Added use of [fizzy-ui-utils](https://github.com/metafizzy/fizzy-ui-utils)

### v3.2.3

+ Fixed pixel rounding errors related to Firefox, gutters. Fixed [#580](https://github.com/desandro/masonry/pull/580)
+ Moved poorly named `examples/` to better named `sandbox/`. Fixed [#539](https://github.com/desandro/masonry/issues/539)
+ Moved [`masonry-v2-shim.js` shim to its own repo](https://github.com/desandro/masonry-v2-3-shim)

### v3.2.2

+ Update [getSize](https://github.com/desandro/get-size) to v1.2.1 to fix IE8 bug

### v3.2.1

+ Fix missing dependencies in `package.json`

## v3.2.0

+ Add CommonJS support [#480](https://github.com/desandro/masonry/issues/480)
+ jQuery Bridget no longer in explicit dependency tree

### v3.1.5

+ Add dist/pkgd files
+ Upgrade to Outlayer v1.2

### v3.1.4

Fix stamp bug if multiple of columnWidth

### v3.1.3

Round if off by 1px

### v3.1.2

Fix IE8 bugs w/ hidden items

### v3.1.1

update Outlayer v1.1.2

## v3.1.0

Add better RequireJS support

### v3.0.3

Fix bug with `isFitWidth` and resizing

### v3.0.2

Add back `isFitWidth`

### v3.0.1

fixed empty container

## v3.0.0

+ Complete rewrite
+ Componentize with Bower
+ Remove jQuery as strict dependency
+ Remove smartresize jQuery plugin
+ imagesLoaded no longer included
+ jQuery animation has been removed. animationOptions has been removed. This means no animation for in IE8 and IE9.
+ Corner stamp is now integrated as `stamp` option and `stamp` method
+ `isRTL` option removed, use `isOriginLeft: false` instead
+ `isResizable` option renamed to `isResizeBound`
+ `layout` method renamed to `layoutItems`
+ `gutterWidth` option renamed to `gutter`
