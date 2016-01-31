var defaults = {
    direction: 'horizontal',
    touchEventsTarget: 'container',
    initialSlide: 0,
    speed: 300,
    // autoplay
    autoplay: false,
    autoplayDisableOnInteraction: true,
    autoplayStopOnLast: false,
    // To support iOS's swipe-to-go-back gesture (when being used in-app, with UIWebView).
    iOSEdgeSwipeDetection: false,
    iOSEdgeSwipeThreshold: 20,
    // Free mode
    freeMode: false,
    freeModeMomentum: true,
    freeModeMomentumRatio: 1,
    freeModeMomentumBounce: true,
    freeModeMomentumBounceRatio: 1,
    freeModeSticky: false,
    freeModeMinimumVelocity: 0.02,
    // Autoheight
    autoHeight: false,
    // Set wrapper width
    setWrapperSize: false,
    // Virtual Translate
    virtualTranslate: false,
    // Effects
    effect: 'slide', // 'slide' or 'fade' or 'cube' or 'coverflow' or 'flip'
    coverflow: {
        rotate: 50,
        stretch: 0,
        depth: 100,
        modifier: 1,
        slideShadows : true
    },
    flip: {
        slideShadows : true,
        limitRotation: true
    },
    cube: {
        slideShadows: true,
        shadow: true,
        shadowOffset: 20,
        shadowScale: 0.94
    },
    fade: {
        crossFade: false
    },
    // Parallax
    parallax: false,
    // Scrollbar
    scrollbar: null,
    scrollbarHide: true,
    scrollbarDraggable: false,
    scrollbarSnapOnRelease: false,
    // Keyboard Mousewheel
    keyboardControl: false,
    mousewheelControl: false,
    mousewheelReleaseOnEdges: false,
    mousewheelInvert: false,
    mousewheelForceToAxis: false,
    mousewheelSensitivity: 1,
    // Hash Navigation
    hashnav: false,
    // Breakpoints
    breakpoints: undefined,
    // Slides grid
    spaceBetween: 0,
    slidesPerView: 1,
    slidesPerColumn: 1,
    slidesPerColumnFill: 'column',
    slidesPerGroup: 1,
    centeredSlides: false,
    slidesOffsetBefore: 0, // in px
    slidesOffsetAfter: 0, // in px
    // Round length
    roundLengths: false,
    // Touches
    touchRatio: 1,
    touchAngle: 45,
    simulateTouch: true,
    shortSwipes: true,
    longSwipes: true,
    longSwipesRatio: 0.5,
    longSwipesMs: 300,
    followFinger: true,
    onlyExternal: false,
    threshold: 0,
    touchMoveStopPropagation: true,
    // Pagination
    pagination: null,
    paginationElement: 'span',
    paginationClickable: false,
    paginationHide: false,
    paginationBulletRender: null,
    paginationProgressRender: null,
    paginationFractionRender: null,
    paginationCustomRender: null,
    paginationType: 'bullets', // 'bullets' or 'progress' or 'fraction' or 'custom'
    // Resistance
    resistance: true,
    resistanceRatio: 0.85,
    // Next/prev buttons
    nextButton: null,
    prevButton: null,
    // Progress
    watchSlidesProgress: false,
    watchSlidesVisibility: false,
    // Cursor
    grabCursor: false,
    // Clicks
    preventClicks: true,
    preventClicksPropagation: true,
    slideToClickedSlide: false,
    // Lazy Loading
    lazyLoading: false,
    lazyLoadingInPrevNext: false,
    lazyLoadingInPrevNextAmount: 1,
    lazyLoadingOnTransitionStart: false,
    // Images
    preloadImages: true,
    updateOnImagesReady: true,
    // loop
    loop: false,
    loopAdditionalSlides: 0,
    loopedSlides: null,
    // Control
    control: undefined,
    controlInverse: false,
    controlBy: 'slide', //or 'container'
    // Swiping/no swiping
    allowSwipeToPrev: true,
    allowSwipeToNext: true,
    swipeHandler: null, //'.swipe-handler',
    noSwiping: true,
    noSwipingClass: 'swiper-no-swiping',
    // NS
    slideClass: 'swiper-slide',
    slideActiveClass: 'swiper-slide-active',
    slideVisibleClass: 'swiper-slide-visible',
    slideDuplicateClass: 'swiper-slide-duplicate',
    slideNextClass: 'swiper-slide-next',
    slidePrevClass: 'swiper-slide-prev',
    wrapperClass: 'swiper-wrapper',
    bulletClass: 'swiper-pagination-bullet',
    bulletActiveClass: 'swiper-pagination-bullet-active',
    buttonDisabledClass: 'swiper-button-disabled',
    paginationCurrentClass: 'swiper-pagination-current',
    paginationTotalClass: 'swiper-pagination-total',
    paginationHiddenClass: 'swiper-pagination-hidden',
    paginationProgressbarClass: 'swiper-pagination-progressbar',
    // Observer
    observer: false,
    observeParents: false,
    // Accessibility
    a11y: false,
    prevSlideMessage: 'Previous slide',
    nextSlideMessage: 'Next slide',
    firstSlideMessage: 'This is the first slide',
    lastSlideMessage: 'This is the last slide',
    paginationBulletMessage: 'Go to slide {{index}}',
    // Callbacks
    runCallbacksOnInit: true
    /*
    Callbacks:
    onInit: function (swiper)
    onDestroy: function (swiper)
    onClick: function (swiper, e)
    onTap: function (swiper, e)
    onDoubleTap: function (swiper, e)
    onSliderMove: function (swiper, e)
    onSlideChangeStart: function (swiper)
    onSlideChangeEnd: function (swiper)
    onTransitionStart: function (swiper)
    onTransitionEnd: function (swiper)
    onImagesReady: function (swiper)
    onProgress: function (swiper, progress)
    onTouchStart: function (swiper, e)
    onTouchMove: function (swiper, e)
    onTouchMoveOpposite: function (swiper, e)
    onTouchEnd: function (swiper, e)
    onReachBeginning: function (swiper)
    onReachEnd: function (swiper)
    onSetTransition: function (swiper, duration)
    onSetTranslate: function (swiper, translate)
    onAutoplayStart: function (swiper)
    onAutoplayStop: function (swiper),
    onLazyImageLoad: function (swiper, slide, image)
    onLazyImageReady: function (swiper, slide, image)
    */

};
var initialVirtualTranslate = params && params.virtualTranslate;

params = params || {};
var originalParams = {};
for (var param in params) {
    if (typeof params[param] === 'object' && params[param] !== null && !(params[param].nodeType || params[param] === window || params[param] === document || (typeof Dom7 !== 'undefined' && params[param] instanceof Dom7) || (typeof jQuery !== 'undefined' && params[param] instanceof jQuery))) {
        originalParams[param] = {};
        for (var deepParam in params[param]) {
            originalParams[param][deepParam] = params[param][deepParam];
        }
    }
    else {
        originalParams[param] = params[param];
    }
}
for (var def in defaults) {
    if (typeof params[def] === 'undefined') {
        params[def] = defaults[def];
    }
    else if (typeof params[def] === 'object') {
        for (var deepDef in defaults[def]) {
            if (typeof params[def][deepDef] === 'undefined') {
                params[def][deepDef] = defaults[def][deepDef];
            }
        }
    }
}

// Swiper
var s = this;

// Params
s.params = params;
s.originalParams = originalParams;

// Classname
s.classNames = [];
/*=========================
  Dom Library and plugins
  ===========================*/
if (typeof $ !== 'undefined' && typeof Dom7 !== 'undefined'){
    $ = Dom7;
}
if (typeof $ === 'undefined') {
    if (typeof Dom7 === 'undefined') {
        $ = window.Dom7 || window.Zepto || window.jQuery;
    }
    else {
        $ = Dom7;
    }
    if (!$) return;
}
// Export it to Swiper instance
s.$ = $;

/*=========================
  Breakpoints
  ===========================*/
s.currentBreakpoint = undefined;
s.getActiveBreakpoint = function () {
    //Get breakpoint for window width
    if (!s.params.breakpoints) return false;
    var breakpoint = false;
    var points = [], point;
    for ( point in s.params.breakpoints ) {
        if (s.params.breakpoints.hasOwnProperty(point)) {
            points.push(point);
        }
    }
    points.sort(function (a, b) {
        return parseInt(a, 10) > parseInt(b, 10);
    });
    for (var i = 0; i < points.length; i++) {
        point = points[i];
        if (point >= window.innerWidth && !breakpoint) {
            breakpoint = point;
        }
    }
    return breakpoint || 'max';
};
s.setBreakpoint = function () {
    //Set breakpoint for window width and update parameters
    var breakpoint = s.getActiveBreakpoint();
    if (breakpoint && s.currentBreakpoint !== breakpoint) {
        var breakPointsParams = breakpoint in s.params.breakpoints ? s.params.breakpoints[breakpoint] : s.originalParams;
        for ( var param in breakPointsParams ) {
            s.params[param] = breakPointsParams[param];
        }
        s.currentBreakpoint = breakpoint;
    }
};
// Set breakpoint on load
if (s.params.breakpoints) {
    s.setBreakpoint();
}

/*=========================
  Preparation - Define Container, Wrapper and Pagination
  ===========================*/
s.container = $(container);
if (s.container.length === 0) return;
if (s.container.length > 1) {
    s.container.each(function () {
        new Swiper(this, params);
    });
    return;
}

// Save instance in container HTML Element and in data
s.container[0].swiper = s;
s.container.data('swiper', s);

s.classNames.push('swiper-container-' + s.params.direction);

if (s.params.freeMode) {
    s.classNames.push('swiper-container-free-mode');
}
if (!s.support.flexbox) {
    s.classNames.push('swiper-container-no-flexbox');
    s.params.slidesPerColumn = 1;
}
if (s.params.autoHeight) {
    s.classNames.push('swiper-container-autoheight');
}
// Enable slides progress when required
if (s.params.parallax || s.params.watchSlidesVisibility) {
    s.params.watchSlidesProgress = true;
}
// Coverflow / 3D
if (['cube', 'coverflow', 'flip'].indexOf(s.params.effect) >= 0) {
    if (s.support.transforms3d) {
        s.params.watchSlidesProgress = true;
        s.classNames.push('swiper-container-3d');
    }
    else {
        s.params.effect = 'slide';
    }
}
if (s.params.effect !== 'slide') {
    s.classNames.push('swiper-container-' + s.params.effect);
}
if (s.params.effect === 'cube') {
    s.params.resistanceRatio = 0;
    s.params.slidesPerView = 1;
    s.params.slidesPerColumn = 1;
    s.params.slidesPerGroup = 1;
    s.params.centeredSlides = false;
    s.params.spaceBetween = 0;
    s.params.virtualTranslate = true;
    s.params.setWrapperSize = false;
}
if (s.params.effect === 'fade' || s.params.effect === 'flip') {
    s.params.slidesPerView = 1;
    s.params.slidesPerColumn = 1;
    s.params.slidesPerGroup = 1;
    s.params.watchSlidesProgress = true;
    s.params.spaceBetween = 0;
    s.params.setWrapperSize = false;
    if (typeof initialVirtualTranslate === 'undefined') {
        s.params.virtualTranslate = true;
    }
}

// Grab Cursor
if (s.params.grabCursor && s.support.touch) {
    s.params.grabCursor = false;
}

// Wrapper
s.wrapper = s.container.children('.' + s.params.wrapperClass);

// Pagination
if (s.params.pagination) {
    s.paginationContainer = $(s.params.pagination);
    if (s.params.paginationType === 'bullets' && s.params.paginationClickable) {
        s.paginationContainer.addClass('swiper-pagination-clickable');
    }
    else {
        s.params.paginationClickable = false;
    }
    s.paginationContainer.addClass('swiper-pagination-' + s.params.paginationType);
}

// Is Horizontal
s.isHorizontal = function () {
    return s.params.direction === 'horizontal';
};
// s.isH = isH;

// RTL
s.rtl = s.isHorizontal() && (s.container[0].dir.toLowerCase() === 'rtl' || s.container.css('direction') === 'rtl');
if (s.rtl) {
    s.classNames.push('swiper-container-rtl');
}

// Wrong RTL support
if (s.rtl) {
    s.wrongRTL = s.wrapper.css('display') === '-webkit-box';
}

// Columns
if (s.params.slidesPerColumn > 1) {
    s.classNames.push('swiper-container-multirow');
}

// Check for Android
if (s.device.android) {
    s.classNames.push('swiper-container-android');
}

// Add classes
s.container.addClass(s.classNames.join(' '));

// Translate
s.translate = 0;

// Progress
s.progress = 0;

// Velocity
s.velocity = 0;

/*=========================
  Locks, unlocks
  ===========================*/
s.lockSwipeToNext = function () {
    s.params.allowSwipeToNext = false;
};
s.lockSwipeToPrev = function () {
    s.params.allowSwipeToPrev = false;
};
s.lockSwipes = function () {
    s.params.allowSwipeToNext = s.params.allowSwipeToPrev = false;
};
s.unlockSwipeToNext = function () {
    s.params.allowSwipeToNext = true;
};
s.unlockSwipeToPrev = function () {
    s.params.allowSwipeToPrev = true;
};
s.unlockSwipes = function () {
    s.params.allowSwipeToNext = s.params.allowSwipeToPrev = true;
};

/*=========================
  Round helper
  ===========================*/
function round(a) {
    return Math.floor(a);
}
/*=========================
  Set grab cursor
  ===========================*/
if (s.params.grabCursor) {
    s.container[0].style.cursor = 'move';
    s.container[0].style.cursor = '-webkit-grab';
    s.container[0].style.cursor = '-moz-grab';
    s.container[0].style.cursor = 'grab';
}
/*=========================
  Update on Images Ready
  ===========================*/
s.imagesToLoad = [];
s.imagesLoaded = 0;

s.loadImage = function (imgElement, src, srcset, checkForComplete, callback) {
    var image;
    function onReady () {
        if (callback) callback();
    }
    if (!imgElement.complete || !checkForComplete) {
        if (src) {
            image = new window.Image();
            image.onload = onReady;
            image.onerror = onReady;
            if (srcset) {
                image.srcset = srcset;
            }
            if (src) {
                image.src = src;
            }
        } else {
            onReady();
        }

    } else {//image already loaded...
        onReady();
    }
};
s.preloadImages = function () {
    s.imagesToLoad = s.container.find('img');
    function _onReady() {
        if (typeof s === 'undefined' || s === null) return;
        if (s.imagesLoaded !== undefined) s.imagesLoaded++;
        if (s.imagesLoaded === s.imagesToLoad.length) {
            if (s.params.updateOnImagesReady) s.update();
            s.emit('onImagesReady', s);
        }
    }
    for (var i = 0; i < s.imagesToLoad.length; i++) {
        s.loadImage(s.imagesToLoad[i], (s.imagesToLoad[i].currentSrc || s.imagesToLoad[i].getAttribute('src')), (s.imagesToLoad[i].srcset || s.imagesToLoad[i].getAttribute('srcset')), true, _onReady);
    }
};

/*=========================
  Autoplay
  ===========================*/
s.autoplayTimeoutId = undefined;
s.autoplaying = false;
s.autoplayPaused = false;
function autoplay() {
    s.autoplayTimeoutId = setTimeout(function () {
        if (s.params.loop) {
            s.fixLoop();
            s._slideNext();
            s.emit('onAutoplay', s);
        }
        else {
            if (!s.isEnd) {
                s._slideNext();
                s.emit('onAutoplay', s);
            }
            else {
                if (!params.autoplayStopOnLast) {
                    s._slideTo(0);
                    s.emit('onAutoplay', s);
                }
                else {
                    s.stopAutoplay();
                }
            }
        }
    }, s.params.autoplay);
}
s.startAutoplay = function () {
    if (typeof s.autoplayTimeoutId !== 'undefined') return false;
    if (!s.params.autoplay) return false;
    if (s.autoplaying) return false;
    s.autoplaying = true;
    s.emit('onAutoplayStart', s);
    autoplay();
};
s.stopAutoplay = function (internal) {
    if (!s.autoplayTimeoutId) return;
    if (s.autoplayTimeoutId) clearTimeout(s.autoplayTimeoutId);
    s.autoplaying = false;
    s.autoplayTimeoutId = undefined;
    s.emit('onAutoplayStop', s);
};
s.pauseAutoplay = function (speed) {
    if (s.autoplayPaused) return;
    if (s.autoplayTimeoutId) clearTimeout(s.autoplayTimeoutId);
    s.autoplayPaused = true;
    if (speed === 0) {
        s.autoplayPaused = false;
        autoplay();
    }
    else {
        s.wrapper.transitionEnd(function () {
            if (!s) return;
            s.autoplayPaused = false;
            if (!s.autoplaying) {
                s.stopAutoplay();
            }
            else {
                autoplay();
            }
        });
    }
};
/*=========================
  Min/Max Translate
  ===========================*/
s.minTranslate = function () {
    return (-s.snapGrid[0]);
};
s.maxTranslate = function () {
    return (-s.snapGrid[s.snapGrid.length - 1]);
};
/*=========================
  Slider/slides sizes
  ===========================*/
s.updateAutoHeight = function () {
    // Update Height
    var slide = s.slides.eq(s.activeIndex)[0];
    if (typeof slide !== 'undefined') {
        var newHeight = slide.offsetHeight;
        if (newHeight) s.wrapper.css('height', newHeight + 'px');
    }
};
s.updateContainerSize = function () {
    var width, height;
    if (typeof s.params.width !== 'undefined') {
        width = s.params.width;
    }
    else {
        width = s.container[0].clientWidth;
    }
    if (typeof s.params.height !== 'undefined') {
        height = s.params.height;
    }
    else {
        height = s.container[0].clientHeight;
    }
    if (width === 0 && s.isHorizontal() || height === 0 && !s.isHorizontal()) {
        return;
    }

    //Subtract paddings
    width = width - parseInt(s.container.css('padding-left'), 10) - parseInt(s.container.css('padding-right'), 10);
    height = height - parseInt(s.container.css('padding-top'), 10) - parseInt(s.container.css('padding-bottom'), 10);

    // Store values
    s.width = width;
    s.height = height;
    s.size = s.isHorizontal() ? s.width : s.height;
};

s.updateSlidesSize = function () {
    s.slides = s.wrapper.children('.' + s.params.slideClass);
    s.snapGrid = [];
    s.slidesGrid = [];
    s.slidesSizesGrid = [];

    var spaceBetween = s.params.spaceBetween,
        slidePosition = -s.params.slidesOffsetBefore,
        i,
        prevSlideSize = 0,
        index = 0;
    if (typeof spaceBetween === 'string' && spaceBetween.indexOf('%') >= 0) {
        spaceBetween = parseFloat(spaceBetween.replace('%', '')) / 100 * s.size;
    }

    s.virtualSize = -spaceBetween;
    // reset margins
    if (s.rtl) s.slides.css({marginLeft: '', marginTop: ''});
    else s.slides.css({marginRight: '', marginBottom: ''});

    var slidesNumberEvenToRows;
    if (s.params.slidesPerColumn > 1) {
        if (Math.floor(s.slides.length / s.params.slidesPerColumn) === s.slides.length / s.params.slidesPerColumn) {
            slidesNumberEvenToRows = s.slides.length;
        }
        else {
            slidesNumberEvenToRows = Math.ceil(s.slides.length / s.params.slidesPerColumn) * s.params.slidesPerColumn;
        }
        if (s.params.slidesPerView !== 'auto' && s.params.slidesPerColumnFill === 'row') {
            slidesNumberEvenToRows = Math.max(slidesNumberEvenToRows, s.params.slidesPerView * s.params.slidesPerColumn);
        }
    }

    // Calc slides
    var slideSize;
    var slidesPerColumn = s.params.slidesPerColumn;
    var slidesPerRow = slidesNumberEvenToRows / slidesPerColumn;
    var numFullColumns = slidesPerRow - (s.params.slidesPerColumn * slidesPerRow - s.slides.length);
    for (i = 0; i < s.slides.length; i++) {
        slideSize = 0;
        var slide = s.slides.eq(i);
        if (s.params.slidesPerColumn > 1) {
            // Set slides order
            var newSlideOrderIndex;
            var column, row;
            if (s.params.slidesPerColumnFill === 'column') {
                column = Math.floor(i / slidesPerColumn);
                row = i - column * slidesPerColumn;
                if (column > numFullColumns || (column === numFullColumns && row === slidesPerColumn-1)) {
                    if (++row >= slidesPerColumn) {
                        row = 0;
                        column++;
                    }
                }
                newSlideOrderIndex = column + row * slidesNumberEvenToRows / slidesPerColumn;
                slide
                    .css({
                        '-webkit-box-ordinal-group': newSlideOrderIndex,
                        '-moz-box-ordinal-group': newSlideOrderIndex,
                        '-ms-flex-order': newSlideOrderIndex,
                        '-webkit-order': newSlideOrderIndex,
                        'order': newSlideOrderIndex
                    });
            }
            else {
                row = Math.floor(i / slidesPerRow);
                column = i - row * slidesPerRow;
            }
            slide
                .css({
                    'margin-top': (row !== 0 && s.params.spaceBetween) && (s.params.spaceBetween + 'px')
                })
                .attr('data-swiper-column', column)
                .attr('data-swiper-row', row);

        }
        if (slide.css('display') === 'none') continue;
        if (s.params.slidesPerView === 'auto') {
            slideSize = s.isHorizontal() ? slide.outerWidth(true) : slide.outerHeight(true);
            if (s.params.roundLengths) slideSize = round(slideSize);
        }
        else {
            slideSize = (s.size - (s.params.slidesPerView - 1) * spaceBetween) / s.params.slidesPerView;
            if (s.params.roundLengths) slideSize = round(slideSize);

            if (s.isHorizontal()) {
                s.slides[i].style.width = slideSize + 'px';
            }
            else {
                s.slides[i].style.height = slideSize + 'px';
            }
        }
        s.slides[i].swiperSlideSize = slideSize;
        s.slidesSizesGrid.push(slideSize);


        if (s.params.centeredSlides) {
            slidePosition = slidePosition + slideSize / 2 + prevSlideSize / 2 + spaceBetween;
            if (i === 0) slidePosition = slidePosition - s.size / 2 - spaceBetween;
            if (Math.abs(slidePosition) < 1 / 1000) slidePosition = 0;
            if ((index) % s.params.slidesPerGroup === 0) s.snapGrid.push(slidePosition);
            s.slidesGrid.push(slidePosition);
        }
        else {
            if ((index) % s.params.slidesPerGroup === 0) s.snapGrid.push(slidePosition);
            s.slidesGrid.push(slidePosition);
            slidePosition = slidePosition + slideSize + spaceBetween;
        }

        s.virtualSize += slideSize + spaceBetween;

        prevSlideSize = slideSize;

        index ++;
    }
    s.virtualSize = Math.max(s.virtualSize, s.size) + s.params.slidesOffsetAfter;
    var newSlidesGrid;

    if (
        s.rtl && s.wrongRTL && (s.params.effect === 'slide' || s.params.effect === 'coverflow')) {
        s.wrapper.css({width: s.virtualSize + s.params.spaceBetween + 'px'});
    }
    if (!s.support.flexbox || s.params.setWrapperSize) {
        if (s.isHorizontal()) s.wrapper.css({width: s.virtualSize + s.params.spaceBetween + 'px'});
        else s.wrapper.css({height: s.virtualSize + s.params.spaceBetween + 'px'});
    }

    if (s.params.slidesPerColumn > 1) {
        s.virtualSize = (slideSize + s.params.spaceBetween) * slidesNumberEvenToRows;
        s.virtualSize = Math.ceil(s.virtualSize / s.params.slidesPerColumn) - s.params.spaceBetween;
        s.wrapper.css({width: s.virtualSize + s.params.spaceBetween + 'px'});
        if (s.params.centeredSlides) {
            newSlidesGrid = [];
            for (i = 0; i < s.snapGrid.length; i++) {
                if (s.snapGrid[i] < s.virtualSize + s.snapGrid[0]) newSlidesGrid.push(s.snapGrid[i]);
            }
            s.snapGrid = newSlidesGrid;
        }
    }

    // Remove last grid elements depending on width
    if (!s.params.centeredSlides) {
        newSlidesGrid = [];
        for (i = 0; i < s.snapGrid.length; i++) {
            if (s.snapGrid[i] <= s.virtualSize - s.size) {
                newSlidesGrid.push(s.snapGrid[i]);
            }
        }
        s.snapGrid = newSlidesGrid;
        if (Math.floor(s.virtualSize - s.size) > Math.floor(s.snapGrid[s.snapGrid.length - 1])) {
            s.snapGrid.push(s.virtualSize - s.size);
        }
    }
    if (s.snapGrid.length === 0) s.snapGrid = [0];

    if (s.params.spaceBetween !== 0) {
        if (s.isHorizontal()) {
            if (s.rtl) s.slides.css({marginLeft: spaceBetween + 'px'});
            else s.slides.css({marginRight: spaceBetween + 'px'});
        }
        else s.slides.css({marginBottom: spaceBetween + 'px'});
    }
    if (s.params.watchSlidesProgress) {
        s.updateSlidesOffset();
    }
};
s.updateSlidesOffset = function () {
    for (var i = 0; i < s.slides.length; i++) {
        s.slides[i].swiperSlideOffset = s.isHorizontal() ? s.slides[i].offsetLeft : s.slides[i].offsetTop;
    }
};

/*=========================
  Slider/slides progress
  ===========================*/
s.updateSlidesProgress = function (translate) {
    if (typeof translate === 'undefined') {
        translate = s.translate || 0;
    }
    if (s.slides.length === 0) return;
    if (typeof s.slides[0].swiperSlideOffset === 'undefined') s.updateSlidesOffset();

    var offsetCenter = -translate;
    if (s.rtl) offsetCenter = translate;

    // Visible Slides
    s.slides.removeClass(s.params.slideVisibleClass);
    for (var i = 0; i < s.slides.length; i++) {
        var slide = s.slides[i];
        var slideProgress = (offsetCenter - slide.swiperSlideOffset) / (slide.swiperSlideSize + s.params.spaceBetween);
        if (s.params.watchSlidesVisibility) {
            var slideBefore = -(offsetCenter - slide.swiperSlideOffset);
            var slideAfter = slideBefore + s.slidesSizesGrid[i];
            var isVisible =
                (slideBefore >= 0 && slideBefore < s.size) ||
                (slideAfter > 0 && slideAfter <= s.size) ||
                (slideBefore <= 0 && slideAfter >= s.size);
            if (isVisible) {
                s.slides.eq(i).addClass(s.params.slideVisibleClass);
            }
        }
        slide.progress = s.rtl ? -slideProgress : slideProgress;
    }
};
s.updateProgress = function (translate) {
    if (typeof translate === 'undefined') {
        translate = s.translate || 0;
    }
    var translatesDiff = s.maxTranslate() - s.minTranslate();
    var wasBeginning = s.isBeginning;
    var wasEnd = s.isEnd;
    if (translatesDiff === 0) {
        s.progress = 0;
        s.isBeginning = s.isEnd = true;
    }
    else {
        s.progress = (translate - s.minTranslate()) / (translatesDiff);
        s.isBeginning = s.progress <= 0;
        s.isEnd = s.progress >= 1;
    }
    if (s.isBeginning && !wasBeginning) s.emit('onReachBeginning', s);
    if (s.isEnd && !wasEnd) s.emit('onReachEnd', s);

    if (s.params.watchSlidesProgress) s.updateSlidesProgress(translate);
    s.emit('onProgress', s, s.progress);
};
s.updateActiveIndex = function () {
    var translate = s.rtl ? s.translate : -s.translate;
    var newActiveIndex, i, snapIndex;
    for (i = 0; i < s.slidesGrid.length; i ++) {
        if (typeof s.slidesGrid[i + 1] !== 'undefined') {
            if (translate >= s.slidesGrid[i] && translate < s.slidesGrid[i + 1] - (s.slidesGrid[i + 1] - s.slidesGrid[i]) / 2) {
                newActiveIndex = i;
            }
            else if (translate >= s.slidesGrid[i] && translate < s.slidesGrid[i + 1]) {
                newActiveIndex = i + 1;
            }
        }
        else {
            if (translate >= s.slidesGrid[i]) {
                newActiveIndex = i;
            }
        }
    }
    // Normalize slideIndex
    if (newActiveIndex < 0 || typeof newActiveIndex === 'undefined') newActiveIndex = 0;
    // for (i = 0; i < s.slidesGrid.length; i++) {
        // if (- translate >= s.slidesGrid[i]) {
            // newActiveIndex = i;
        // }
    // }
    snapIndex = Math.floor(newActiveIndex / s.params.slidesPerGroup);
    if (snapIndex >= s.snapGrid.length) snapIndex = s.snapGrid.length - 1;

    if (newActiveIndex === s.activeIndex) {
        return;
    }
    s.snapIndex = snapIndex;
    s.previousIndex = s.activeIndex;
    s.activeIndex = newActiveIndex;
    s.updateClasses();
};

/*=========================
  Classes
  ===========================*/
s.updateClasses = function () {
    s.slides.removeClass(s.params.slideActiveClass + ' ' + s.params.slideNextClass + ' ' + s.params.slidePrevClass);
    var activeSlide = s.slides.eq(s.activeIndex);
    // Active classes
    activeSlide.addClass(s.params.slideActiveClass);
    activeSlide.next('.' + s.params.slideClass).addClass(s.params.slideNextClass);
    activeSlide.prev('.' + s.params.slideClass).addClass(s.params.slidePrevClass);

    // Pagination
    if (s.paginationContainer && s.paginationContainer.length > 0) {
        // Current/Total
        var current,
            total = s.params.loop ? Math.ceil((s.slides.length - s.loopedSlides * 2) / s.params.slidesPerGroup) : s.snapGrid.length;
        if (s.params.loop) {
            current = Math.ceil(s.activeIndex - s.loopedSlides)/s.params.slidesPerGroup;
            if (current > s.slides.length - 1 - s.loopedSlides * 2) {
                current = current - (s.slides.length - s.loopedSlides * 2);
            }
            if (current > total - 1) current = current - total;
            if (current < 0 && s.params.paginationType !== 'bullets') current = total + current;
        }
        else {
            if (typeof s.snapIndex !== 'undefined') {
                current = s.snapIndex;
            }
            else {
                current = s.activeIndex || 0;
            }
        }
        // Types
        if (s.params.paginationType === 'bullets' && s.bullets && s.bullets.length > 0) {
            s.bullets.removeClass(s.params.bulletActiveClass);
            if (s.paginationContainer.length > 1) {
                s.bullets.each(function () {
                    if ($(this).index() === current) $(this).addClass(s.params.bulletActiveClass);
                });
            }
            else {
                s.bullets.eq(current).addClass(s.params.bulletActiveClass);
            }
        }
        if (s.params.paginationType === 'fraction') {
            s.paginationContainer.find('.' + s.params.paginationCurrentClass).text(current + 1);
            s.paginationContainer.find('.' + s.params.paginationTotalClass).text(total);
        }
        if (s.params.paginationType === 'progress') {
            var scale = (current + 1) / total,
                scaleX = scale,
                scaleY = 1;
            if (!s.isHorizontal()) {
                scaleY = scale;
                scaleX = 1;
            }
            s.paginationContainer.find('.' + s.params.paginationProgressbarClass).transform('translate3d(0,0,0) scaleX(' + scaleX + ') scaleY(' + scaleY + ')').transition(s.params.speed);
        }
        if (s.params.paginationType === 'custom' && s.params.paginationCustomRender) {
            s.paginationContainer.html(s.params.paginationCustomRender(s, current + 1, total));
        }
    }

    // Next/active buttons
    if (!s.params.loop) {
        if (s.params.prevButton) {
            if (s.isBeginning) {
                $(s.params.prevButton).addClass(s.params.buttonDisabledClass);
                if (s.params.a11y && s.a11y) s.a11y.disable($(s.params.prevButton));
            }
            else {
                $(s.params.prevButton).removeClass(s.params.buttonDisabledClass);
                if (s.params.a11y && s.a11y) s.a11y.enable($(s.params.prevButton));
            }
        }
        if (s.params.nextButton) {
            if (s.isEnd) {
                $(s.params.nextButton).addClass(s.params.buttonDisabledClass);
                if (s.params.a11y && s.a11y) s.a11y.disable($(s.params.nextButton));
            }
            else {
                $(s.params.nextButton).removeClass(s.params.buttonDisabledClass);
                if (s.params.a11y && s.a11y) s.a11y.enable($(s.params.nextButton));
            }
        }
    }
};

/*=========================
  Pagination
  ===========================*/
s.updatePagination = function () {
    if (!s.params.pagination) return;
    if (s.paginationContainer && s.paginationContainer.length > 0) {
        var paginationHTML = '';
        if (s.params.paginationType === 'bullets') {
            var numberOfBullets = s.params.loop ? Math.ceil((s.slides.length - s.loopedSlides * 2) / s.params.slidesPerGroup) : s.snapGrid.length;
            for (var i = 0; i < numberOfBullets; i++) {
                if (s.params.paginationBulletRender) {
                    paginationHTML += s.params.paginationBulletRender(i, s.params.bulletClass);
                }
                else {
                    paginationHTML += '<' + s.params.paginationElement+' class="' + s.params.bulletClass + '"></' + s.params.paginationElement + '>';
                }
            }
            s.paginationContainer.html(paginationHTML);
            s.bullets = s.paginationContainer.find('.' + s.params.bulletClass);
            if (s.params.paginationClickable && s.params.a11y && s.a11y) {
                s.a11y.initPagination();
            }
        }
        if (s.params.paginationType === 'fraction') {
            if (s.params.paginationFractionRender) {
                paginationHTML = s.params.paginationFractionRender(s, s.params.paginationCurrentClass, s.params.paginationTotalClass);
            }
            else {
                paginationHTML =
                    '<span class="' + s.params.paginationCurrentClass + '"></span>' +
                    ' / ' +
                    '<span class="' + s.params.paginationTotalClass+'"></span>';
            }
            s.paginationContainer.html(paginationHTML);
        }
        if (s.params.paginationType === 'progress') {
            if (s.params.paginationProgressRender) {
                paginationHTML = s.params.paginationProgressRender(s, s.params.paginationProgressbarClass);
            }
            else {
                paginationHTML = '<span class="' + s.params.paginationProgressbarClass + '"></span>';
            }
            s.paginationContainer.html(paginationHTML);
        }
    }
};
/*=========================
  Common update method
  ===========================*/
s.update = function (updateTranslate) {
    s.updateContainerSize();
    s.updateSlidesSize();
    s.updateProgress();
    s.updatePagination();
    s.updateClasses();
    if (s.params.scrollbar && s.scrollbar) {
        s.scrollbar.set();
    }
    function forceSetTranslate() {
        newTranslate = Math.min(Math.max(s.translate, s.maxTranslate()), s.minTranslate());
        s.setWrapperTranslate(newTranslate);
        s.updateActiveIndex();
        s.updateClasses();
    }
    if (updateTranslate) {
        var translated, newTranslate;
        if (s.controller && s.controller.spline) {
            s.controller.spline = undefined;
        }
        if (s.params.freeMode) {
            forceSetTranslate();
            if (s.params.autoHeight) {
                s.updateAutoHeight();
            }
        }
        else {
            if ((s.params.slidesPerView === 'auto' || s.params.slidesPerView > 1) && s.isEnd && !s.params.centeredSlides) {
                translated = s.slideTo(s.slides.length - 1, 0, false, true);
            }
            else {
                translated = s.slideTo(s.activeIndex, 0, false, true);
            }
            if (!translated) {
                forceSetTranslate();
            }
        }
    }
    else if (s.params.autoHeight) {
        s.updateAutoHeight();
    }
};

/*=========================
  Resize Handler
  ===========================*/
s.onResize = function (forceUpdatePagination) {
    //Breakpoints
    if (s.params.breakpoints) {
        s.setBreakpoint();
    }

    // Disable locks on resize
    var allowSwipeToPrev = s.params.allowSwipeToPrev;
    var allowSwipeToNext = s.params.allowSwipeToNext;
    s.params.allowSwipeToPrev = s.params.allowSwipeToNext = true;

    s.updateContainerSize();
    s.updateSlidesSize();
    if (s.params.slidesPerView === 'auto' || s.params.freeMode || forceUpdatePagination) s.updatePagination();
    if (s.params.scrollbar && s.scrollbar) {
        s.scrollbar.set();
    }
    if (s.controller && s.controller.spline) {
        s.controller.spline = undefined;
    }
    if (s.params.freeMode) {
        var newTranslate = Math.min(Math.max(s.translate, s.maxTranslate()), s.minTranslate());
        s.setWrapperTranslate(newTranslate);
        s.updateActiveIndex();
        s.updateClasses();

        if (s.params.autoHeight) {
            s.updateAutoHeight();
        }
    }
    else {
        s.updateClasses();
        if ((s.params.slidesPerView === 'auto' || s.params.slidesPerView > 1) && s.isEnd && !s.params.centeredSlides) {
            s.slideTo(s.slides.length - 1, 0, false, true);
        }
        else {
            s.slideTo(s.activeIndex, 0, false, true);
        }
    }
    // Return locks after resize
    s.params.allowSwipeToPrev = allowSwipeToPrev;
    s.params.allowSwipeToNext = allowSwipeToNext;
};

/*=========================
  Events
  ===========================*/

//Define Touch Events
var desktopEvents = ['mousedown', 'mousemove', 'mouseup'];
if (window.navigator.pointerEnabled) desktopEvents = ['pointerdown', 'pointermove', 'pointerup'];
else if (window.navigator.msPointerEnabled) desktopEvents = ['MSPointerDown', 'MSPointerMove', 'MSPointerUp'];
s.touchEvents = {
    start : s.support.touch || !s.params.simulateTouch  ? 'touchstart' : desktopEvents[0],
    move : s.support.touch || !s.params.simulateTouch ? 'touchmove' : desktopEvents[1],
    end : s.support.touch || !s.params.simulateTouch ? 'touchend' : desktopEvents[2]
};


// WP8 Touch Events Fix
if (window.navigator.pointerEnabled || window.navigator.msPointerEnabled) {
    (s.params.touchEventsTarget === 'container' ? s.container : s.wrapper).addClass('swiper-wp8-' + s.params.direction);
}

// Attach/detach events
s.initEvents = function (detach) {
    var actionDom = detach ? 'off' : 'on';
    var action = detach ? 'removeEventListener' : 'addEventListener';
    var touchEventsTarget = s.params.touchEventsTarget === 'container' ? s.container[0] : s.wrapper[0];
    var target = s.support.touch ? touchEventsTarget : document;

    var moveCapture = s.params.nested ? true : false;

    //Touch Events
    if (s.browser.ie) {
        touchEventsTarget[action](s.touchEvents.start, s.onTouchStart, false);
        target[action](s.touchEvents.move, s.onTouchMove, moveCapture);
        target[action](s.touchEvents.end, s.onTouchEnd, false);
    }
    else {
        if (s.support.touch) {
            touchEventsTarget[action](s.touchEvents.start, s.onTouchStart, false);
            touchEventsTarget[action](s.touchEvents.move, s.onTouchMove, moveCapture);
            touchEventsTarget[action](s.touchEvents.end, s.onTouchEnd, false);
        }
        if (params.simulateTouch && !s.device.ios && !s.device.android) {
            touchEventsTarget[action]('mousedown', s.onTouchStart, false);
            document[action]('mousemove', s.onTouchMove, moveCapture);
            document[action]('mouseup', s.onTouchEnd, false);
        }
    }
    window[action]('resize', s.onResize);

    // Next, Prev, Index
    if (s.params.nextButton) {
        $(s.params.nextButton)[actionDom]('click', s.onClickNext);
        if (s.params.a11y && s.a11y) $(s.params.nextButton)[actionDom]('keydown', s.a11y.onEnterKey);
    }
    if (s.params.prevButton) {
        $(s.params.prevButton)[actionDom]('click', s.onClickPrev);
        if (s.params.a11y && s.a11y) $(s.params.prevButton)[actionDom]('keydown', s.a11y.onEnterKey);
    }
    if (s.params.pagination && s.params.paginationClickable) {
        $(s.paginationContainer)[actionDom]('click', '.' + s.params.bulletClass, s.onClickIndex);
        if (s.params.a11y && s.a11y) $(s.paginationContainer)[actionDom]('keydown', '.' + s.params.bulletClass, s.a11y.onEnterKey);
    }

    // Prevent Links Clicks
    if (s.params.preventClicks || s.params.preventClicksPropagation) touchEventsTarget[action]('click', s.preventClicks, true);
};
s.attachEvents = function (detach) {
    s.initEvents();
};
s.detachEvents = function () {
    s.initEvents(true);
};

/*=========================
  Handle Clicks
  ===========================*/
// Prevent Clicks
s.allowClick = true;
s.preventClicks = function (e) {
    if (!s.allowClick) {
        if (s.params.preventClicks) e.preventDefault();
        if (s.params.preventClicksPropagation && s.animating) {
            e.stopPropagation();
            e.stopImmediatePropagation();
        }
    }
};
// Clicks
s.onClickNext = function (e) {
    e.preventDefault();
    if (s.isEnd && !s.params.loop) return;
    s.slideNext();
};
s.onClickPrev = function (e) {
    e.preventDefault();
    if (s.isBeginning && !s.params.loop) return;
    s.slidePrev();
};
s.onClickIndex = function (e) {
    e.preventDefault();
    var index = $(this).index() * s.params.slidesPerGroup;
    if (s.params.loop) index = index + s.loopedSlides;
    s.slideTo(index);
};

/*=========================
  Handle Touches
  ===========================*/
function findElementInEvent(e, selector) {
    var el = $(e.target);
    if (!el.is(selector)) {
        if (typeof selector === 'string') {
            el = el.parents(selector);
        }
        else if (selector.nodeType) {
            var found;
            el.parents().each(function (index, _el) {
                if (_el === selector) found = selector;
            });
            if (!found) return undefined;
            else return selector;
        }
    }
    if (el.length === 0) {
        return undefined;
    }
    return el[0];
}
s.updateClickedSlide = function (e) {
    var slide = findElementInEvent(e, '.' + s.params.slideClass);
    var slideFound = false;
    if (slide) {
        for (var i = 0; i < s.slides.length; i++) {
            if (s.slides[i] === slide) slideFound = true;
        }
    }

    if (slide && slideFound) {
        s.clickedSlide = slide;
        s.clickedIndex = $(slide).index();
    }
    else {
        s.clickedSlide = undefined;
        s.clickedIndex = undefined;
        return;
    }
    if (s.params.slideToClickedSlide && s.clickedIndex !== undefined && s.clickedIndex !== s.activeIndex) {
        var slideToIndex = s.clickedIndex,
            realIndex,
            duplicatedSlides;
        if (s.params.loop) {
            if (s.animating) return;
            realIndex = $(s.clickedSlide).attr('data-swiper-slide-index');
            if (s.params.centeredSlides) {
                if ((slideToIndex < s.loopedSlides - s.params.slidesPerView/2) || (slideToIndex > s.slides.length - s.loopedSlides + s.params.slidesPerView/2)) {
                    s.fixLoop();
                    slideToIndex = s.wrapper.children('.' + s.params.slideClass + '[data-swiper-slide-index="' + realIndex + '"]:not(.swiper-slide-duplicate)').eq(0).index();
                    setTimeout(function () {
                        s.slideTo(slideToIndex);
                    }, 0);
                }
                else {
                    s.slideTo(slideToIndex);
                }
            }
            else {
                if (slideToIndex > s.slides.length - s.params.slidesPerView) {
                    s.fixLoop();
                    slideToIndex = s.wrapper.children('.' + s.params.slideClass + '[data-swiper-slide-index="' + realIndex + '"]:not(.swiper-slide-duplicate)').eq(0).index();
                    setTimeout(function () {
                        s.slideTo(slideToIndex);
                    }, 0);
                }
                else {
                    s.slideTo(slideToIndex);
                }
            }
        }
        else {
            s.slideTo(slideToIndex);
        }
    }
};

var isTouched,
    isMoved,
    allowTouchCallbacks,
    touchStartTime,
    isScrolling,
    currentTranslate,
    startTranslate,
    allowThresholdMove,
    // Form elements to match
    formElements = 'input, select, textarea, button',
    // Last click time
    lastClickTime = Date.now(), clickTimeout,
    //Velocities
    velocities = [],
    allowMomentumBounce;

// Animating Flag
s.animating = false;

// Touches information
s.touches = {
    startX: 0,
    startY: 0,
    currentX: 0,
    currentY: 0,
    diff: 0
};

// Touch handlers
var isTouchEvent, startMoving;
s.onTouchStart = function (e) {
    if (e.originalEvent) e = e.originalEvent;
    isTouchEvent = e.type === 'touchstart';
    if (!isTouchEvent && 'which' in e && e.which === 3) return;
    if (s.params.noSwiping && findElementInEvent(e, '.' + s.params.noSwipingClass)) {
        s.allowClick = true;
        return;
    }
    if (s.params.swipeHandler) {
        if (!findElementInEvent(e, s.params.swipeHandler)) return;
    }

    var startX = s.touches.currentX = e.type === 'touchstart' ? e.targetTouches[0].pageX : e.pageX;
    var startY = s.touches.currentY = e.type === 'touchstart' ? e.targetTouches[0].pageY : e.pageY;

    // Do NOT start if iOS edge swipe is detected. Otherwise iOS app (UIWebView) cannot swipe-to-go-back anymore
    if(s.device.ios && s.params.iOSEdgeSwipeDetection && startX <= s.params.iOSEdgeSwipeThreshold) {
        return;
    }

    isTouched = true;
    isMoved = false;
    allowTouchCallbacks = true;
    isScrolling = undefined;
    startMoving = undefined;
    s.touches.startX = startX;
    s.touches.startY = startY;
    touchStartTime = Date.now();
    s.allowClick = true;
    s.updateContainerSize();
    s.swipeDirection = undefined;
    if (s.params.threshold > 0) allowThresholdMove = false;
    if (e.type !== 'touchstart') {
        var preventDefault = true;
        if ($(e.target).is(formElements)) preventDefault = false;
        if (document.activeElement && $(document.activeElement).is(formElements)) {
            document.activeElement.blur();
        }
        if (preventDefault) {
            e.preventDefault();
        }
    }
    s.emit('onTouchStart', s, e);
};

s.onTouchMove = function (e) {
    if (e.originalEvent) e = e.originalEvent;
    if (isTouchEvent && e.type === 'mousemove') return;
    if (e.preventedByNestedSwiper) return;
    if (s.params.onlyExternal) {
        // isMoved = true;
        s.allowClick = false;
        if (isTouched) {
            s.touches.startX = s.touches.currentX = e.type === 'touchmove' ? e.targetTouches[0].pageX : e.pageX;
            s.touches.startY = s.touches.currentY = e.type === 'touchmove' ? e.targetTouches[0].pageY : e.pageY;
            touchStartTime = Date.now();
        }
        return;
    }
    if (isTouchEvent && document.activeElement) {
        if (e.target === document.activeElement && $(e.target).is(formElements)) {
            isMoved = true;
            s.allowClick = false;
            return;
        }
    }
    if (allowTouchCallbacks) {
        s.emit('onTouchMove', s, e);
    }
    if (e.targetTouches && e.targetTouches.length > 1) return;

    s.touches.currentX = e.type === 'touchmove' ? e.targetTouches[0].pageX : e.pageX;
    s.touches.currentY = e.type === 'touchmove' ? e.targetTouches[0].pageY : e.pageY;

    if (typeof isScrolling === 'undefined') {
        var touchAngle = Math.atan2(Math.abs(s.touches.currentY - s.touches.startY), Math.abs(s.touches.currentX - s.touches.startX)) * 180 / Math.PI;
        isScrolling = s.isHorizontal() ? touchAngle > s.params.touchAngle : (90 - touchAngle > s.params.touchAngle);
    }
    if (isScrolling) {
        s.emit('onTouchMoveOpposite', s, e);
    }
    if (typeof startMoving === 'undefined' && s.browser.ieTouch) {
        if (s.touches.currentX !== s.touches.startX || s.touches.currentY !== s.touches.startY) {
            startMoving = true;
        }
    }
    if (!isTouched) return;
    if (isScrolling)  {
        isTouched = false;
        return;
    }
    if (!startMoving && s.browser.ieTouch) {
        return;
    }
    s.allowClick = false;
    s.emit('onSliderMove', s, e);
    e.preventDefault();
    if (s.params.touchMoveStopPropagation && !s.params.nested) {
        e.stopPropagation();
    }

    if (!isMoved) {
        if (params.loop) {
            s.fixLoop();
        }
        startTranslate = s.getWrapperTranslate();
        s.setWrapperTransition(0);
        if (s.animating) {
            s.wrapper.trigger('webkitTransitionEnd transitionend oTransitionEnd MSTransitionEnd msTransitionEnd');
        }
        if (s.params.autoplay && s.autoplaying) {
            if (s.params.autoplayDisableOnInteraction) {
                s.stopAutoplay();
            }
            else {
                s.pauseAutoplay();
            }
        }
        allowMomentumBounce = false;
        //Grab Cursor
        if (s.params.grabCursor) {
            s.container[0].style.cursor = 'move';
            s.container[0].style.cursor = '-webkit-grabbing';
            s.container[0].style.cursor = '-moz-grabbin';
            s.container[0].style.cursor = 'grabbing';
        }
    }
    isMoved = true;

    var diff = s.touches.diff = s.isHorizontal() ? s.touches.currentX - s.touches.startX : s.touches.currentY - s.touches.startY;

    diff = diff * s.params.touchRatio;
    if (s.rtl) diff = -diff;

    s.swipeDirection = diff > 0 ? 'prev' : 'next';
    currentTranslate = diff + startTranslate;

    var disableParentSwiper = true;
    if ((diff > 0 && currentTranslate > s.minTranslate())) {
        disableParentSwiper = false;
        if (s.params.resistance) currentTranslate = s.minTranslate() - 1 + Math.pow(-s.minTranslate() + startTranslate + diff, s.params.resistanceRatio);
    }
    else if (diff < 0 && currentTranslate < s.maxTranslate()) {
        disableParentSwiper = false;
        if (s.params.resistance) currentTranslate = s.maxTranslate() + 1 - Math.pow(s.maxTranslate() - startTranslate - diff, s.params.resistanceRatio);
    }

    if (disableParentSwiper) {
        e.preventedByNestedSwiper = true;
    }

    // Directions locks
    if (!s.params.allowSwipeToNext && s.swipeDirection === 'next' && currentTranslate < startTranslate) {
        currentTranslate = startTranslate;
    }
    if (!s.params.allowSwipeToPrev && s.swipeDirection === 'prev' && currentTranslate > startTranslate) {
        currentTranslate = startTranslate;
    }

    if (!s.params.followFinger) return;

    // Threshold
    if (s.params.threshold > 0) {
        if (Math.abs(diff) > s.params.threshold || allowThresholdMove) {
            if (!allowThresholdMove) {
                allowThresholdMove = true;
                s.touches.startX = s.touches.currentX;
                s.touches.startY = s.touches.currentY;
                currentTranslate = startTranslate;
                s.touches.diff = s.isHorizontal() ? s.touches.currentX - s.touches.startX : s.touches.currentY - s.touches.startY;
                return;
            }
        }
        else {
            currentTranslate = startTranslate;
            return;
        }
    }
    // Update active index in free mode
    if (s.params.freeMode || s.params.watchSlidesProgress) {
        s.updateActiveIndex();
    }
    if (s.params.freeMode) {
        //Velocity
        if (velocities.length === 0) {
            velocities.push({
                position: s.touches[s.isHorizontal() ? 'startX' : 'startY'],
                time: touchStartTime
            });
        }
        velocities.push({
            position: s.touches[s.isHorizontal() ? 'currentX' : 'currentY'],
            time: (new window.Date()).getTime()
        });
    }
    // Update progress
    s.updateProgress(currentTranslate);
    // Update translate
    s.setWrapperTranslate(currentTranslate);
};
s.onTouchEnd = function (e) {
    if (e.originalEvent) e = e.originalEvent;
    if (allowTouchCallbacks) {
        s.emit('onTouchEnd', s, e);
    }
    allowTouchCallbacks = false;
    if (!isTouched) return;
    //Return Grab Cursor
    if (s.params.grabCursor && isMoved && isTouched) {
        s.container[0].style.cursor = 'move';
        s.container[0].style.cursor = '-webkit-grab';
        s.container[0].style.cursor = '-moz-grab';
        s.container[0].style.cursor = 'grab';
    }

    // Time diff
    var touchEndTime = Date.now();
    var timeDiff = touchEndTime - touchStartTime;

    // Tap, doubleTap, Click
    if (s.allowClick) {
        s.updateClickedSlide(e);
        s.emit('onTap', s, e);
        if (timeDiff < 300 && (touchEndTime - lastClickTime) > 300) {
            if (clickTimeout) clearTimeout(clickTimeout);
            clickTimeout = setTimeout(function () {
                if (!s) return;
                if (s.params.paginationHide && s.paginationContainer.length > 0 && !$(e.target).hasClass(s.params.bulletClass)) {
                    s.paginationContainer.toggleClass(s.params.paginationHiddenClass);
                }
                s.emit('onClick', s, e);
            }, 300);

        }
        if (timeDiff < 300 && (touchEndTime - lastClickTime) < 300) {
            if (clickTimeout) clearTimeout(clickTimeout);
            s.emit('onDoubleTap', s, e);
        }
    }

    lastClickTime = Date.now();
    setTimeout(function () {
        if (s) s.allowClick = true;
    }, 0);

    if (!isTouched || !isMoved || !s.swipeDirection || s.touches.diff === 0 || currentTranslate === startTranslate) {
        isTouched = isMoved = false;
        return;
    }
    isTouched = isMoved = false;

    var currentPos;
    if (s.params.followFinger) {
        currentPos = s.rtl ? s.translate : -s.translate;
    }
    else {
        currentPos = -currentTranslate;
    }
    if (s.params.freeMode) {
        if (currentPos < -s.minTranslate()) {
            s.slideTo(s.activeIndex);
            return;
        }
        else if (currentPos > -s.maxTranslate()) {
            if (s.slides.length < s.snapGrid.length) {
                s.slideTo(s.snapGrid.length - 1);
            }
            else {
                s.slideTo(s.slides.length - 1);
            }
            return;
        }

        if (s.params.freeModeMomentum) {
            if (velocities.length > 1) {
                var lastMoveEvent = velocities.pop(), velocityEvent = velocities.pop();

                var distance = lastMoveEvent.position - velocityEvent.position;
                var time = lastMoveEvent.time - velocityEvent.time;
                s.velocity = distance / time;
                s.velocity = s.velocity / 2;
                if (Math.abs(s.velocity) < s.params.freeModeMinimumVelocity) {
                    s.velocity = 0;
                }
                // this implies that the user stopped moving a finger then released.
                // There would be no events with distance zero, so the last event is stale.
                if (time > 150 || (new window.Date().getTime() - lastMoveEvent.time) > 300) {
                    s.velocity = 0;
                }
            } else {
                s.velocity = 0;
            }

            velocities.length = 0;
            var momentumDuration = 1000 * s.params.freeModeMomentumRatio;
            var momentumDistance = s.velocity * momentumDuration;

            var newPosition = s.translate + momentumDistance;
            if (s.rtl) newPosition = - newPosition;
            var doBounce = false;
            var afterBouncePosition;
            var bounceAmount = Math.abs(s.velocity) * 20 * s.params.freeModeMomentumBounceRatio;
            if (newPosition < s.maxTranslate()) {
                if (s.params.freeModeMomentumBounce) {
                    if (newPosition + s.maxTranslate() < -bounceAmount) {
                        newPosition = s.maxTranslate() - bounceAmount;
                    }
                    afterBouncePosition = s.maxTranslate();
                    doBounce = true;
                    allowMomentumBounce = true;
                }
                else {
                    newPosition = s.maxTranslate();
                }
            }
            else if (newPosition > s.minTranslate()) {
                if (s.params.freeModeMomentumBounce) {
                    if (newPosition - s.minTranslate() > bounceAmount) {
                        newPosition = s.minTranslate() + bounceAmount;
                    }
                    afterBouncePosition = s.minTranslate();
                    doBounce = true;
                    allowMomentumBounce = true;
                }
                else {
                    newPosition = s.minTranslate();
                }
            }
            else if (s.params.freeModeSticky) {
                var j = 0,
                    nextSlide;
                for (j = 0; j < s.snapGrid.length; j += 1) {
                    if (s.snapGrid[j] > -newPosition) {
                        nextSlide = j;
                        break;
                    }

                }
                if (Math.abs(s.snapGrid[nextSlide] - newPosition) < Math.abs(s.snapGrid[nextSlide - 1] - newPosition) || s.swipeDirection === 'next') {
                    newPosition = s.snapGrid[nextSlide];
                } else {
                    newPosition = s.snapGrid[nextSlide - 1];
                }
                if (!s.rtl) newPosition = - newPosition;
            }
            //Fix duration
            if (s.velocity !== 0) {
                if (s.rtl) {
                    momentumDuration = Math.abs((-newPosition - s.translate) / s.velocity);
                }
                else {
                    momentumDuration = Math.abs((newPosition - s.translate) / s.velocity);
                }
            }
            else if (s.params.freeModeSticky) {
                s.slideReset();
                return;
            }

            if (s.params.freeModeMomentumBounce && doBounce) {
                s.updateProgress(afterBouncePosition);
                s.setWrapperTransition(momentumDuration);
                s.setWrapperTranslate(newPosition);
                s.onTransitionStart();
                s.animating = true;
                s.wrapper.transitionEnd(function () {
                    if (!s || !allowMomentumBounce) return;
                    s.emit('onMomentumBounce', s);

                    s.setWrapperTransition(s.params.speed);
                    s.setWrapperTranslate(afterBouncePosition);
                    s.wrapper.transitionEnd(function () {
                        if (!s) return;
                        s.onTransitionEnd();
                    });
                });
            } else if (s.velocity) {
                s.updateProgress(newPosition);
                s.setWrapperTransition(momentumDuration);
                s.setWrapperTranslate(newPosition);
                s.onTransitionStart();
                if (!s.animating) {
                    s.animating = true;
                    s.wrapper.transitionEnd(function () {
                        if (!s) return;
                        s.onTransitionEnd();
                    });
                }

            } else {
                s.updateProgress(newPosition);
            }

            s.updateActiveIndex();
        }
        if (!s.params.freeModeMomentum || timeDiff >= s.params.longSwipesMs) {
            s.updateProgress();
            s.updateActiveIndex();
        }
        return;
    }

    // Find current slide
    var i, stopIndex = 0, groupSize = s.slidesSizesGrid[0];
    for (i = 0; i < s.slidesGrid.length; i += s.params.slidesPerGroup) {
        if (typeof s.slidesGrid[i + s.params.slidesPerGroup] !== 'undefined') {
            if (currentPos >= s.slidesGrid[i] && currentPos < s.slidesGrid[i + s.params.slidesPerGroup]) {
                stopIndex = i;
                groupSize = s.slidesGrid[i + s.params.slidesPerGroup] - s.slidesGrid[i];
            }
        }
        else {
            if (currentPos >= s.slidesGrid[i]) {
                stopIndex = i;
                groupSize = s.slidesGrid[s.slidesGrid.length - 1] - s.slidesGrid[s.slidesGrid.length - 2];
            }
        }
    }

    // Find current slide size
    var ratio = (currentPos - s.slidesGrid[stopIndex]) / groupSize;

    if (timeDiff > s.params.longSwipesMs) {
        // Long touches
        if (!s.params.longSwipes) {
            s.slideTo(s.activeIndex);
            return;
        }
        if (s.swipeDirection === 'next') {
            if (ratio >= s.params.longSwipesRatio) s.slideTo(stopIndex + s.params.slidesPerGroup);
            else s.slideTo(stopIndex);

        }
        if (s.swipeDirection === 'prev') {
            if (ratio > (1 - s.params.longSwipesRatio)) s.slideTo(stopIndex + s.params.slidesPerGroup);
            else s.slideTo(stopIndex);
        }
    }
    else {
        // Short swipes
        if (!s.params.shortSwipes) {
            s.slideTo(s.activeIndex);
            return;
        }
        if (s.swipeDirection === 'next') {
            s.slideTo(stopIndex + s.params.slidesPerGroup);

        }
        if (s.swipeDirection === 'prev') {
            s.slideTo(stopIndex);
        }
    }
};
/*=========================
  Transitions
  ===========================*/
s._slideTo = function (slideIndex, speed) {
    return s.slideTo(slideIndex, speed, true, true);
};
s.slideTo = function (slideIndex, speed, runCallbacks, internal) {
    if (typeof runCallbacks === 'undefined') runCallbacks = true;
    if (typeof slideIndex === 'undefined') slideIndex = 0;
    if (slideIndex < 0) slideIndex = 0;
    s.snapIndex = Math.floor(slideIndex / s.params.slidesPerGroup);
    if (s.snapIndex >= s.snapGrid.length) s.snapIndex = s.snapGrid.length - 1;

    var translate = - s.snapGrid[s.snapIndex];
    // Stop autoplay
    if (s.params.autoplay && s.autoplaying) {
        if (internal || !s.params.autoplayDisableOnInteraction) {
            s.pauseAutoplay(speed);
        }
        else {
            s.stopAutoplay();
        }
    }
    // Update progress
    s.updateProgress(translate);

    // Normalize slideIndex
    for (var i = 0; i < s.slidesGrid.length; i++) {
        if (- Math.floor(translate * 100) >= Math.floor(s.slidesGrid[i] * 100)) {
            slideIndex = i;
        }
    }

    // Directions locks
    if (!s.params.allowSwipeToNext && translate < s.translate && translate < s.minTranslate()) {
        return false;
    }
    if (!s.params.allowSwipeToPrev && translate > s.translate && translate > s.maxTranslate()) {
        if ((s.activeIndex || 0) !== slideIndex ) return false;
    }

    // Update Index
    if (typeof speed === 'undefined') speed = s.params.speed;
    s.previousIndex = s.activeIndex || 0;
    s.activeIndex = slideIndex;

    if ((s.rtl && -translate === s.translate) || (!s.rtl && translate === s.translate)) {
        // Update Height
        if (s.params.autoHeight) {
            s.updateAutoHeight();
        }
        s.updateClasses();
        if (s.params.effect !== 'slide') {
            s.setWrapperTranslate(translate);
        }
        return false;
    }
    s.updateClasses();
    s.onTransitionStart(runCallbacks);

    if (speed === 0) {
        s.setWrapperTranslate(translate);
        s.setWrapperTransition(0);
        s.onTransitionEnd(runCallbacks);
    }
    else {
        s.setWrapperTranslate(translate);
        s.setWrapperTransition(speed);
        if (!s.animating) {
            s.animating = true;
            s.wrapper.transitionEnd(function () {
                if (!s) return;
                s.onTransitionEnd(runCallbacks);
            });
        }

    }

    return true;
};

s.onTransitionStart = function (runCallbacks) {
    if (typeof runCallbacks === 'undefined') runCallbacks = true;
    if (s.params.autoHeight) {
        s.updateAutoHeight();
    }
    if (s.lazy) s.lazy.onTransitionStart();
    if (runCallbacks) {
        s.emit('onTransitionStart', s);
        if (s.activeIndex !== s.previousIndex) {
            s.emit('onSlideChangeStart', s);
            if (s.activeIndex > s.previousIndex) {
                s.emit('onSlideNextStart', s);
            }
            else {
                s.emit('onSlidePrevStart', s);
            }
        }

    }
};
s.onTransitionEnd = function (runCallbacks) {
    s.animating = false;
    s.setWrapperTransition(0);
    if (typeof runCallbacks === 'undefined') runCallbacks = true;
    if (s.lazy) s.lazy.onTransitionEnd();
    if (runCallbacks) {
        s.emit('onTransitionEnd', s);
        if (s.activeIndex !== s.previousIndex) {
            s.emit('onSlideChangeEnd', s);
            if (s.activeIndex > s.previousIndex) {
                s.emit('onSlideNextEnd', s);
            }
            else {
                s.emit('onSlidePrevEnd', s);
            }
        }
    }
    if (s.params.hashnav && s.hashnav) {
        s.hashnav.setHash();
    }

};
s.slideNext = function (runCallbacks, speed, internal) {
    if (s.params.loop) {
        if (s.animating) return false;
        s.fixLoop();
        var clientLeft = s.container[0].clientLeft;
        return s.slideTo(s.activeIndex + s.params.slidesPerGroup, speed, runCallbacks, internal);
    }
    else return s.slideTo(s.activeIndex + s.params.slidesPerGroup, speed, runCallbacks, internal);
};
s._slideNext = function (speed) {
    return s.slideNext(true, speed, true);
};
s.slidePrev = function (runCallbacks, speed, internal) {
    if (s.params.loop) {
        if (s.animating) return false;
        s.fixLoop();
        var clientLeft = s.container[0].clientLeft;
        return s.slideTo(s.activeIndex - 1, speed, runCallbacks, internal);
    }
    else return s.slideTo(s.activeIndex - 1, speed, runCallbacks, internal);
};
s._slidePrev = function (speed) {
    return s.slidePrev(true, speed, true);
};
s.slideReset = function (runCallbacks, speed, internal) {
    return s.slideTo(s.activeIndex, speed, runCallbacks);
};

/*=========================
  Translate/transition helpers
  ===========================*/
s.setWrapperTransition = function (duration, byController) {
    s.wrapper.transition(duration);
    if (s.params.effect !== 'slide' && s.effects[s.params.effect]) {
        s.effects[s.params.effect].setTransition(duration);
    }
    if (s.params.parallax && s.parallax) {
        s.parallax.setTransition(duration);
    }
    if (s.params.scrollbar && s.scrollbar) {
        s.scrollbar.setTransition(duration);
    }
    if (s.params.control && s.controller) {
        s.controller.setTransition(duration, byController);
    }
    s.emit('onSetTransition', s, duration);
};
s.setWrapperTranslate = function (translate, updateActiveIndex, byController) {
    var x = 0, y = 0, z = 0;
    if (s.isHorizontal()) {
        x = s.rtl ? -translate : translate;
    }
    else {
        y = translate;
    }

    if (s.params.roundLengths) {
        x = round(x);
        y = round(y);
    }

    if (!s.params.virtualTranslate) {
        if (s.support.transforms3d) s.wrapper.transform('translate3d(' + x + 'px, ' + y + 'px, ' + z + 'px)');
        else s.wrapper.transform('translate(' + x + 'px, ' + y + 'px)');
    }

    s.translate = s.isHorizontal() ? x : y;

    // Check if we need to update progress
    var progress;
    var translatesDiff = s.maxTranslate() - s.minTranslate();
    if (translatesDiff === 0) {
        progress = 0;
    }
    else {
        progress = (translate - s.minTranslate()) / (translatesDiff);
    }
    if (progress !== s.progress) {
        s.updateProgress(translate);
    }

    if (updateActiveIndex) s.updateActiveIndex();
    if (s.params.effect !== 'slide' && s.effects[s.params.effect]) {
        s.effects[s.params.effect].setTranslate(s.translate);
    }
    if (s.params.parallax && s.parallax) {
        s.parallax.setTranslate(s.translate);
    }
    if (s.params.scrollbar && s.scrollbar) {
        s.scrollbar.setTranslate(s.translate);
    }
    if (s.params.control && s.controller) {
        s.controller.setTranslate(s.translate, byController);
    }
    s.emit('onSetTranslate', s, s.translate);
};

s.getTranslate = function (el, axis) {
    var matrix, curTransform, curStyle, transformMatrix;

    // automatic axis detection
    if (typeof axis === 'undefined') {
        axis = 'x';
    }

    if (s.params.virtualTranslate) {
        return s.rtl ? -s.translate : s.translate;
    }

    curStyle = window.getComputedStyle(el, null);
    if (window.WebKitCSSMatrix) {
        curTransform = curStyle.transform || curStyle.webkitTransform;
        if (curTransform.split(',').length > 6) {
            curTransform = curTransform.split(', ').map(function(a){
                return a.replace(',','.');
            }).join(', ');
        }
        // Some old versions of Webkit choke when 'none' is passed; pass
        // empty string instead in this case
        transformMatrix = new window.WebKitCSSMatrix(curTransform === 'none' ? '' : curTransform);
    }
    else {
        transformMatrix = curStyle.MozTransform || curStyle.OTransform || curStyle.MsTransform || curStyle.msTransform  || curStyle.transform || curStyle.getPropertyValue('transform').replace('translate(', 'matrix(1, 0, 0, 1,');
        matrix = transformMatrix.toString().split(',');
    }

    if (axis === 'x') {
        //Latest Chrome and webkits Fix
        if (window.WebKitCSSMatrix)
            curTransform = transformMatrix.m41;
        //Crazy IE10 Matrix
        else if (matrix.length === 16)
            curTransform = parseFloat(matrix[12]);
        //Normal Browsers
        else
            curTransform = parseFloat(matrix[4]);
    }
    if (axis === 'y') {
        //Latest Chrome and webkits Fix
        if (window.WebKitCSSMatrix)
            curTransform = transformMatrix.m42;
        //Crazy IE10 Matrix
        else if (matrix.length === 16)
            curTransform = parseFloat(matrix[13]);
        //Normal Browsers
        else
            curTransform = parseFloat(matrix[5]);
    }
    if (s.rtl && curTransform) curTransform = -curTransform;
    return curTransform || 0;
};
s.getWrapperTranslate = function (axis) {
    if (typeof axis === 'undefined') {
        axis = s.isHorizontal() ? 'x' : 'y';
    }
    return s.getTranslate(s.wrapper[0], axis);
};

/*=========================
  Observer
  ===========================*/
s.observers = [];
function initObserver(target, options) {
    options = options || {};
    // create an observer instance
    var ObserverFunc = window.MutationObserver || window.WebkitMutationObserver;
    var observer = new ObserverFunc(function (mutations) {
        mutations.forEach(function (mutation) {
            s.onResize(true);
            s.emit('onObserverUpdate', s, mutation);
        });
    });

    observer.observe(target, {
        attributes: typeof options.attributes === 'undefined' ? true : options.attributes,
        childList: typeof options.childList === 'undefined' ? true : options.childList,
        characterData: typeof options.characterData === 'undefined' ? true : options.characterData
    });

    s.observers.push(observer);
}
s.initObservers = function () {
    if (s.params.observeParents) {
        var containerParents = s.container.parents();
        for (var i = 0; i < containerParents.length; i++) {
            initObserver(containerParents[i]);
        }
    }

    // Observe container
    initObserver(s.container[0], {childList: false});

    // Observe wrapper
    initObserver(s.wrapper[0], {attributes: false});
};
s.disconnectObservers = function () {
    for (var i = 0; i < s.observers.length; i++) {
        s.observers[i].disconnect();
    }
    s.observers = [];
};
/*=========================
  Loop
  ===========================*/
// Create looped slides
s.createLoop = function () {
    // Remove duplicated slides
    s.wrapper.children('.' + s.params.slideClass + '.' + s.params.slideDuplicateClass).remove();

    var slides = s.wrapper.children('.' + s.params.slideClass);

    if(s.params.slidesPerView === 'auto' && !s.params.loopedSlides) s.params.loopedSlides = slides.length;

    s.loopedSlides = parseInt(s.params.loopedSlides || s.params.slidesPerView, 10);
    s.loopedSlides = s.loopedSlides + s.params.loopAdditionalSlides;
    if (s.loopedSlides > slides.length) {
        s.loopedSlides = slides.length;
    }

    var prependSlides = [], appendSlides = [], i;
    slides.each(function (index, el) {
        var slide = $(this);
        if (index < s.loopedSlides) appendSlides.push(el);
        if (index < slides.length && index >= slides.length - s.loopedSlides) prependSlides.push(el);
        slide.attr('data-swiper-slide-index', index);
    });
    for (i = 0; i < appendSlides.length; i++) {
        s.wrapper.append($(appendSlides[i].cloneNode(true)).addClass(s.params.slideDuplicateClass));
    }
    for (i = prependSlides.length - 1; i >= 0; i--) {
        s.wrapper.prepend($(prependSlides[i].cloneNode(true)).addClass(s.params.slideDuplicateClass));
    }
};
s.destroyLoop = function () {
    s.wrapper.children('.' + s.params.slideClass + '.' + s.params.slideDuplicateClass).remove();
    s.slides.removeAttr('data-swiper-slide-index');
};
s.fixLoop = function () {
    var newIndex;
    //Fix For Negative Oversliding
    if (s.activeIndex < s.loopedSlides) {
        newIndex = s.slides.length - s.loopedSlides * 3 + s.activeIndex;
        newIndex = newIndex + s.loopedSlides;
        s.slideTo(newIndex, 0, false, true);
    }
    //Fix For Positive Oversliding
    else if ((s.params.slidesPerView === 'auto' && s.activeIndex >= s.loopedSlides * 2) || (s.activeIndex > s.slides.length - s.params.slidesPerView * 2)) {
        newIndex = -s.slides.length + s.activeIndex + s.loopedSlides;
        newIndex = newIndex + s.loopedSlides;
        s.slideTo(newIndex, 0, false, true);
    }
};
/*=========================
  Append/Prepend/Remove Slides
  ===========================*/
s.appendSlide = function (slides) {
    if (s.params.loop) {
        s.destroyLoop();
    }
    if (typeof slides === 'object' && slides.length) {
        for (var i = 0; i < slides.length; i++) {
            if (slides[i]) s.wrapper.append(slides[i]);
        }
    }
    else {
        s.wrapper.append(slides);
    }
    if (s.params.loop) {
        s.createLoop();
    }
    if (!(s.params.observer && s.support.observer)) {
        s.update(true);
    }
};
s.prependSlide = function (slides) {
    if (s.params.loop) {
        s.destroyLoop();
    }
    var newActiveIndex = s.activeIndex + 1;
    if (typeof slides === 'object' && slides.length) {
        for (var i = 0; i < slides.length; i++) {
            if (slides[i]) s.wrapper.prepend(slides[i]);
        }
        newActiveIndex = s.activeIndex + slides.length;
    }
    else {
        s.wrapper.prepend(slides);
    }
    if (s.params.loop) {
        s.createLoop();
    }
    if (!(s.params.observer && s.support.observer)) {
        s.update(true);
    }
    s.slideTo(newActiveIndex, 0, false);
};
s.removeSlide = function (slidesIndexes) {
    if (s.params.loop) {
        s.destroyLoop();
        s.slides = s.wrapper.children('.' + s.params.slideClass);
    }
    var newActiveIndex = s.activeIndex,
        indexToRemove;
    if (typeof slidesIndexes === 'object' && slidesIndexes.length) {
        for (var i = 0; i < slidesIndexes.length; i++) {
            indexToRemove = slidesIndexes[i];
            if (s.slides[indexToRemove]) s.slides.eq(indexToRemove).remove();
            if (indexToRemove < newActiveIndex) newActiveIndex--;
        }
        newActiveIndex = Math.max(newActiveIndex, 0);
    }
    else {
        indexToRemove = slidesIndexes;
        if (s.slides[indexToRemove]) s.slides.eq(indexToRemove).remove();
        if (indexToRemove < newActiveIndex) newActiveIndex--;
        newActiveIndex = Math.max(newActiveIndex, 0);
    }

    if (s.params.loop) {
        s.createLoop();
    }

    if (!(s.params.observer && s.support.observer)) {
        s.update(true);
    }
    if (s.params.loop) {
        s.slideTo(newActiveIndex + s.loopedSlides, 0, false);
    }
    else {
        s.slideTo(newActiveIndex, 0, false);
    }

};
s.removeAllSlides = function () {
    var slidesIndexes = [];
    for (var i = 0; i < s.slides.length; i++) {
        slidesIndexes.push(i);
    }
    s.removeSlide(slidesIndexes);
};
