# Change Log

## Swiper 3.3.0 - Released on January 10, 2016
  * New 3D Flip effect. Can be enabled with `effect: 'flip' parameter
  * New types of pagination with new parameters:
    * `paginationType` - type of pagination. Can be `'bullets'` (default) or `'fraction'` or `'progress'` or `'custom'`
    * `paginationFractionRender(swiper, currentClass, totalClass)` - custom function to render "fraction" type pagination
    * `paginationProgressRender(swiper, progressbarClass)` - custom function to render "progress" type pagination
    * `paginationCustomRender(swiper, current, total)` - custom function to render "custom" type pagination
  * New `lazyLoadingInPrevNextAmount` parameter allows to lazy load images in specified amount of next/prev slides
  * New `autoplayStopOnLast` parameter (`true` by default) tells to autoplay should it stop on last slide or start from first slide
  * New `onAutoplay(swiper)` callback
  * Minor fixes

## Swiper 3.2.7 - Released on December 7, 2015
  * Fixed issue with using HTMLElements for next/prevButton parameters with breakpoints
  * Fixed issue with not working Auto Height when using Controller

## Swiper 3.2.6 - Released on November 28, 2015
  * Fixed issue in RTL layout using `mousewheelControl`
  * Fixed issue in RTL layout using Parallax

## Swiper 3.2.5 - Released on November 21, 2015
  * New "Auto Height" mode when container/wrapper adopts to the height of currently active slide. Can be enabled with `autoHeight: true` parameter
  * Fixed issue with break points in FireFox
  * Fixed issue with wrong slides position when using effects
  * Fixed issue with none-updated scroll bar after using `setWrapperTranslate`
  * Minor fixes

## Swiper 3.2.0 - Released on November 7, 2015
  * Added responsive breakpoints support using new `breakpoints` parameter. Now you can specify different `slidesPerView` and other similar parameters for different sizes:
    ```js
    slidesPerView: 5,
    spaceBetween: 50,
    breakpoints: {
      1024: {
        slidesPerView: 4,
        spaceBetween: 40
      },
      768: {
        slidesPerView: 3,
        spaceBetween: 30
      },
      320: {
        slidesPerView: 1,
        spaceBetween: 10
      }
    }
    ```

  * New callbacks: `onSlideNextStart`, `onSlideNextEnd`, `onSlidePrevStart`, `onSlidePrevEnd`
  * Added Meteor package `meteor add nolimits4web:swiper`
  * Fixed issue with mouse touchMove/End callbacks firing all the time
  * Fixed issue with mousewheel in Chrome
  * Minor fixes

## Swiper 3.1.7 - Released on October 10, 2015
  * Fixed issue with lazy loading trying to download `undefined`-src images
  * Fixed lazy loading on slides using jQuery version
  * Fixed issue with `slideToClickedSlide` with `loop` and `centeredSlides`
  * Fixed issue with wrong slides fill when number of slides less than `slidesPerView * slidesPerColumn` with `slidesPerColumnFill: 'row'`
  * Minor fixes

## Swiper 3.1.5 - Released on September 28, 2015
  * Added support for images `srcset` with lazy loading using `data-srcset` attribute
  * Fixed new Chrome errors with `WebkitCSSMatrix`
  * Fixed issue with `slideToClickedSlide` with `loop` and `centeredSlides`
  * New `freeModeMinimumVelocity` parameter to set minimum required touch velocity to trigger free mode momentum
  * Ability to make the Scrollbar draggable using new paramaters:
    * `scrollbarDraggable` - (boolean) by default is `false`. Allows to enable draggable scrollbar
    * `scrollbarSnapOnRelease` - (boolean) by default is `false`. Control slider snap on scrollbar release
  * Minor fixes

## Swiper 3.1.2 - Released on August 22, 2015
  * Fixed issues with loop and mousewheel when swiper stopped on last slide
  * Imporved mouse wheel behavior in latest Chrome
  * Fixed issue with `slidesPerView: 'auto'` and enabled `loop:true` mode to set `loopedSlides` to the amount of slides by default (if not specified)
  * New `mousewheelSensitivity: 1` parameter allows to tweak mouse wheel sensitivity
  * Fixed issue with updating swiper when swiping is locked (with `allowSwipeToNext`/`allowSwipeToPrev`)
  * Fixed issue with wrong calculating of "visible" slides with enabled `centeredSlides`
  * CSS fixes for 3D effects
  * New options to release Swiper events for swipe-to-go-back work in iOS UIWebView with two options:
    * `iOSEdgeSwipeDetection` (by default is `false`) - enable ios edge detection and release Swiper events
    * `iOSEdgeSwipeThreshold` (default value is `20`) - area in `px` from left edge of screen to release events
  * Improved source maps
  * Minor fixes

## Swiper 3.1.0 - Released on July 14, 2015
  * Accessibility (a11y)
    * Fixed issue with wrong buttons labels
    * Added support for pagination bullets
    * New accessibility parameter for pagination label `paginationBulletMessage: 'Go to slide {{index}}'`
  * Controler
    * New parameter `controlBy` which can be 'slide' (by default) or 'container'. Defines a way how to control another slider: slide by slide or depending on all slides/container (like before)
    * Now controllers in `controlBy: 'slide'` (default) mode will respect grid of each other
  * Pagination
    * New `paginationElement` parameter defines which HTML tag will be use to represent single pagination bullet. By default it is `span`
  * New `roundLengths` parameter (by default is `false`) to round values of slides width and height to prevent blurry texts on usual resolution screens
  * New `slidesOffsetBefore: 0` and `slidesOffsetAfter: 0` (in px) parameters to add additional slide offset within a container
  * Correct calculation for slides size when use CSS padding on `.swiper-container`
  * Fixed issue with not working onResize handler when swipes are locked
  * Fixed issue with "jumping" effect when you disable `onlyExternal` during touchmove
  * Fixed issue when slider goes to previos slide from last slide after window resize
  * Added new `swiper.jquery.umd.js` version for the environment where both Swiper and jQuery included as modules
  * Minor fixes

## Swiper 3.0.8 - Released on June 14, 2015
  * Fixed issue with wrong active index and callbacks in Fade effect
  * New mousewheel parameters:
    * `mousewheelReleaseOnEdges` - will release mousewheel event and allow page scrolling when swiper is on edge positions (in the beginning or in the end)
    * `mousewheelInvert` - option to invert mousewheel slides
  * Fixed issue with lazy loading in next slides when `slidesPerView` > 1
  * Fixed issue with resistance bounds when swiping is locked
  * Fixed issue with wrong slides order in multi-row mode (when `slidesPerColumn` > 1)
  * Fixed issue with not working keyboard control in RTL mode
  * Fixed issue with nested fade-effect swipers
  * Minor fixes

## Swiper 3.0.7 - Released on April 25, 2015
  * New `width` and `height` parameters to force Swiper size, useful when it is hidden on intialization
  * Better support for "Scroll Container". So now Swiper can be used as a scroll container with one single "scrollable"/"swipeable" slide
  * Added lazy loading for background images with `data-background` attribute on required elements
  * New "Sticky Free Mode" (with `freeModeSticky` parameter) which will snap to slides positions in free mode
  * Fixed issues with lazy loading  
  * Fixed slide removing when loop mode is enabled
  * Fixed issues with Autoplay and Fade effect
  * Minor fixes

## Swiper 3.0.6 - Released on March 27, 2015
  * Fixed sometimes wrong slides position when using "Fade" effect
  * `.destroy(deleteInstance, cleanupStyles)` method now has second `cleanupStyles` argument, when passed - all custom styles will be removed from slides, wrapper and container. Useful if you need to destroy Swiper and to init again with new options or in different direction
  * Minor fixes

## Swiper 3.0.5 - Released on March 21, 2015
  * New Keyboard accessibility module to provide foucsable navigation buttons and basic ARIA for screen readers with new parameters:
    * `a11y: false` - enable accessibility
    * `prevSlideMessage: 'Previous slide'` - message for screen readers for previous button
    * `nextSlideMessage: 'Next slide'` - message for screen readers for next button
    * `firstSlideMessage: 'This is the first slide'` - message for screen readers for previous button when swiper is on first slide
    * `lastSlideMessage: 'This is the last slide'` - message for screen readers for next button when swiper is on last slide
  * New Emitter module. It allows to work with callbacks like with events, even adding them after initialization with new methods:
    * `.on(event, handler)` - add event/callback
    * `.off(event, handler)` - remove this event/callback
    * `.once(event, handler)` - add event/callback that will be executed only once
  * Plugins API is back. It allows to write custom Swiper plugins
  * Better support for browser that don't support flexbox layout
  * New parameter `setWrapperSize` (be default it is `false`) to provide better compatibility with browser without flexbox support. Enabled this option and plugin will set width/height on swiper wrapper equal to total size of all slides
  * New `virtualTranslate` parameter. When it is enabled swiper will be operated as usual except it will not move. Useful when you may need to create custom slide transition
  * Added support for multiple Pagination containers
  * Fixed `onLazyImage...` callbacks
  * Fixed issue with not accessible links inside of Slides on Android < 4.4
  * Fixed pagination bullets behavior in loop mode with specified `slidesPerGroup`
  * Fixed issues with clicks on IE 10+ touch devices
  * Fixed issues with Coverflow support on IE 10+
  * Hashnav now will update document hash after transition to prevent browsers UI lags, not in the beginning like before
  * Super basic support for IE 9 with swiper.jquery version. No animation and transitions, but basic stuff like switching slides/pagination/scrollbars works
  

## Swiper 3.0.4 - Released on March 6, 2015
  * New Images Lazy Load component
    * With new parameters `lazyLoading`, `lazyLoadingInPrevNext`, `lazyLoadingOnTransitionStart` (all disabled by default)
    * With new callbacks `onLazyImageLoad` and `onLazyImageReady`
  * `updateOnImages` ready split into 2 parameters:
    * `preloadImages` (by default is true) - to preload all images on swiper init
    * `updateOnImages` (by default is true) - update swiper when all images loaded
  * Fixed issues with touchmove on fouces form elements
  * New `onObserverUpdate` callback function to be called after updates by ovserver
  * Fixed issue with not working inputs with keyboard control for jQuery version
  * New `paginationBulletRender` parameter that accepts function which allow custom pagination elements layout
  * Hash Navigation will run callback dpending on `runCallbacksOnInit` parameter
  * `watchVisibility` parameter renamed to `watchSlidesVisibility`

## Swiper 3.0.3 - Released on March 1, 2015
  * Fixed issue with not firing onSlideChangeEnd callback after calling .slideTo with
runCallbacks=false
  * Fixed values of isBeginning/isEnd when there is only one slide
  * New `crossFade` option for fade effect
  * Improved support for devices with both touch and mouse inputs, not yet on IE
  * Fixed not correctly working mousewheel and keyobard control in swiper.jquery version
  * New parallax module for transitions with parallax effects on internal elements
  * Improved .update and .onResize methods
  * Minor fixes

## Swiper 3.0.2 - Released on February 22, 2015
  * Fixed issue with keyboard events not cleaned up with Swiper.destroy
  * Encoded inline SVG images for IE support
  * New callbacks
    * onInit (swiper)
    * onTouchMoveOpposite (swiper, e)
  * Fixed free mode momentum in RTL layout
  * `.update` method improved to fully cover what `onResize` do for full and correct update
  * Exposed `swiper.touches` object with the following properties: `startX`, `startY`, `currentX`, `currentY`, `diff`
  * New methods to remove slides
    * `.removeSlide(index)` or `.removeSlide([indexes])` - to remove selected slides
    * `.removeAllSlides()` - to remove all slides

## Swiper 3.0.1 - Released on February 13, 2015
  * Fixed issue with navigation buttons in Firefox in loop mode
  * Fixed issue with image dragging in IE 10+

## Swiper 3.0.0 - Released on February 11, 2015
  * Initial release of all new Swiper 3
  * Removed features
    * Dropped support for old browsers. Now it is compatible with:
      * iOS 7+
      * Android 4+ (multirow mode only for Android 4.4+)
      * Latest Chrome, Safari, Firefox and Opera desktop browsers
      * WP 8+, IE 10+ (3D effects may not work correctly on IE because of wrong nested 3D transform support)
    * Scroll Container. Removed in favor of pure CSS `overflow: auto` with `-webkit-overflow-scrolling: touch`
  * New features
    * Swiper now uses modern flexbox layout, which by itself give more features and advantages
    * Such Swiper 2.x plugins as Hash Navigation, Smooth Progress, 3D Flow and Scrollbar are now incoroporated into Swiper 3.x core
    * Full RTL support
    * Built-in navigation buttons/arrows
    * Controller. Now one Swiper could be controlled (or control itself) by another Swiper
    * Multi row slides layout with `slidesPerColumn` option
    * Better support for nested Swipers, now it is possible to use same-direction nested Swipers, like horizontal in horizontal
    * Space between slides
    * New transition effects: 3D Coverflow, 3D Cube and Fade transitions
    * Slides are `border-box` now, so it is possible to use borders and paddings directly on slides
    * Auto layout mode (`slidesPerView: 'auto'`) now gives more freedom, you can even specify slides sizes in % and use margins on them
    * Mutation Observers. If enabled, Swiper will watch for changes in Dom and update its layout automatically
    * Better clicks prevention during swiping
  * Many of API methods, parameters and callbacks are changed
  * Added a bit lightweight jQuery/Zepto version of Swiper that can be used if you use jQuery/Zepto in your project


