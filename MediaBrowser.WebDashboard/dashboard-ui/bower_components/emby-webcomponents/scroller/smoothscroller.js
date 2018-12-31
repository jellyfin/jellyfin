define(['browser', 'layoutManager', 'dom', 'focusManager', 'ResizeObserver', 'scrollStyles'], function (browser, layoutManager, dom, focusManager, ResizeObserver) {
    'use strict';

    /**
* Return type of the value.
*
* @param  {Mixed} value
*
* @return {String}
*/
    function type(value) {
        if (value == null) {
            return String(value);
        }

        if (typeof value === 'object' || typeof value === 'function') {
            return Object.prototype.toString.call(value).match(/\s([a-z]+)/i)[1].toLowerCase() || 'object';
        }

        return typeof value;
    }

    /**
	 * Disables an event it was triggered on and unbinds itself.
	 *
	 * @param  {Event} event
	 *
	 * @return {Void}
	 */
    function disableOneEvent(event) {
        /*jshint validthis:true */
        event.preventDefault();
        event.stopPropagation();
        this.removeEventListener(event.type, disableOneEvent);
    }

    /**
	 * Make sure that number is within the limits.
	 *
	 * @param {Number} number
	 * @param {Number} min
	 * @param {Number} max
	 *
	 * @return {Number}
	 */
    function within(number, min, max) {
        return number < min ? min : number > max ? max : number;
    }

    // Other global values
    var dragMouseEvents = ['mousemove', 'mouseup'];
    var dragTouchEvents = ['touchmove', 'touchend'];
    var wheelEvent = (document.implementation.hasFeature('Event.wheel', '3.0') ? 'wheel' : 'mousewheel');
    var interactiveElements = ['INPUT', 'SELECT', 'TEXTAREA'];
    var tmpArray = [];
    var time;

    // Math shorthands
    var abs = Math.abs;
    var sqrt = Math.sqrt;
    var pow = Math.pow;
    var round = Math.round;
    var max = Math.max;
    var min = Math.min;

    var scrollerFactory = function (frame, options) {

        // Extend options
        var o = Object.assign({}, {
            slidee: null, // Selector, DOM element, or jQuery object with DOM element representing SLIDEE.
            horizontal: false, // Switch to horizontal mode.

            // Scrolling
            mouseWheel: true,
            scrollBy: 0, // Pixels or items to move per one mouse scroll. 0 to disable scrolling

            // Dragging
            dragSource: null, // Selector or DOM element for catching dragging events. Default is FRAME.
            mouseDragging: 1, // Enable navigation by dragging the SLIDEE with mouse cursor.
            touchDragging: 1, // Enable navigation by dragging the SLIDEE with touch events.
            dragThreshold: 3, // Distance in pixels before Sly recognizes dragging.
            intervactive: null, // Selector for special interactive elements.

            // Mixed options
            speed: 0, // Animations speed in milliseconds. 0 to disable animations.

        }, options);

        var isSmoothScrollSupported = 'scrollBehavior' in document.documentElement.style;

        // native scroll is a must with touch input
        // also use native scroll when scrolling vertically in desktop mode - excluding horizontal because the mouse wheel support is choppy at the moment
        // in cases with firefox, if the smooth scroll api is supported then use that because their implementation is very good
        if (options.allowNativeScroll === false) {
            options.enableNativeScroll = false;
        }
        else if (isSmoothScrollSupported && ((browser.firefox && !layoutManager.tv) || options.allowNativeSmoothScroll)) {
            // native smooth scroll
            options.enableNativeScroll = true;
        }
        else if (options.requireAnimation && (browser.animate || browser.supportsCssAnimation())) {

            // transform is the only way to guarantee animation
            options.enableNativeScroll = false;
        }
        else if (!layoutManager.tv || !browser.animate) {

            options.enableNativeScroll = true;
        }

        // Need this for the magic wheel. With the animated scroll the magic wheel will run off of the screen
        if (browser.web0s) {
            options.enableNativeScroll = true;
        }

        // Private variables
        var self = this;
        self.options = o;

        // Frame
        var slideeElement = o.slidee ? o.slidee : sibling(frame.firstChild)[0];
        self._pos = {
            start: 0,
            center: 0,
            end: 0,
            cur: 0,
            dest: 0
        };

        var transform = !options.enableNativeScroll;

        // Miscellaneous
        var scrollSource = frame;
        var dragSourceElement = o.dragSource ? o.dragSource : frame;
        var dragging = {
            released: 1
        };
        var scrolling = {
            last: 0,
            delta: 0,
            resetTime: 200
        };

        // Expose properties
        self.initialized = 0;
        self.slidee = slideeElement;
        self.options = o;
        self.dragging = dragging;

        var nativeScrollElement = frame;

        function sibling(n, elem) {
            var matched = [];

            for (; n; n = n.nextSibling) {
                if (n.nodeType === 1 && n !== elem) {
                    matched.push(n);
                }
            }
            return matched;
        }

        var requiresReflow = true;

        var frameSize = 0;
        var slideeSize = 0;
        function ensureSizeInfo() {

            if (requiresReflow) {

                requiresReflow = false;

                // Reset global variables
                frameSize = o.horizontal ? (frame).offsetWidth : (frame).offsetHeight;

                slideeSize = o.scrollWidth || Math.max(slideeElement[o.horizontal ? 'offsetWidth' : 'offsetHeight'], slideeElement[o.horizontal ? 'scrollWidth' : 'scrollHeight']);

                // Set position limits & relativess
                self._pos.end = max(slideeSize - frameSize, 0);
            }
        }

        /**
		 * Loading function.
		 *
		 * Populate arrays, set sizes, bind events, ...
		 *
		 * @param {Boolean} [isInit] Whether load is called from within self.init().
		 * @return {Void}
		 */
        function load(isInit) {

            requiresReflow = true;

            if (!isInit) {

                ensureSizeInfo();

                // Fix possible overflowing
                var pos = self._pos;
                self.slideTo(within(pos.dest, pos.start, pos.end));
            }
        }

        function initFrameResizeObserver() {

            var observerOptions = {};

            self.frameResizeObserver = new ResizeObserver(onResize, observerOptions);

            self.frameResizeObserver.observe(frame);
        }

        self.reload = function () { load(); };

        self.getScrollEventName = function () {
            return transform ? 'scrollanimate' : 'scroll';
        };

        self.getScrollSlider = function () {
            return slideeElement;
        };

        self.getScrollFrame = function () {
            return frame;
        };

        function nativeScrollTo(container, pos, immediate) {


            if (container.scroll) {
                if (o.horizontal) {

                    container.scroll({
                        left: pos,
                        behavior: immediate ? 'instant' : 'smooth'
                    });
                } else {

                    container.scroll({
                        top: pos,
                        behavior: immediate ? 'instant' : 'smooth'
                    });
                }
            }
            else if (!immediate && container.scrollTo) {
                if (o.horizontal) {
                    container.scrollTo(Math.round(pos), 0);
                } else {
                    container.scrollTo(0, Math.round(pos));
                }
            } else {
                if (o.horizontal) {
                    container.scrollLeft = Math.round(pos);
                } else {
                    container.scrollTop = Math.round(pos);
                }
            }
        }

        var lastAnimate;

        /**
          * Animate to a position.
          *
          * @param {Int}  newPos    New position.
          * @param {Bool} immediate Reposition immediately without an animation.
          *
          * @return {Void}
          */
        self.slideTo = function (newPos, immediate, fullItemPos) {

            ensureSizeInfo();
            var pos = self._pos;

            newPos = within(newPos, pos.start, pos.end);

            if (!transform) {

                nativeScrollTo(nativeScrollElement, newPos, immediate);
                return;
            }

            // Update the animation object
            var from = pos.cur;
            immediate = immediate || dragging.init || !o.speed;

            var now = new Date().getTime();

            if (o.autoImmediate) {
                if (!immediate && (now - (lastAnimate || 0)) <= 50) {
                    immediate = true;
                }
            }

            if (!immediate && o.skipSlideToWhenVisible && fullItemPos && fullItemPos.isVisible) {

                return;
            }

            // Start animation rendering
            if (newPos !== pos.dest) {
                pos.dest = newPos;

                renderAnimateWithTransform(from, newPos, immediate);

                lastAnimate = now;
            }
        };

        function setStyleProperty(elem, name, value, speed, resetTransition) {

            var style = elem.style;

            if (resetTransition || browser.edge) {
                style.transition = 'none';
                void elem.offsetWidth;
            }

            style.transition = 'transform ' + speed + 'ms ease-out';
            style[name] = value;
        }

        function dispatchScrollEventIfNeeded() {
            if (o.dispatchScrollEvent) {
                frame.dispatchEvent(new CustomEvent(self.getScrollEventName(), {
                    bubbles: true,
                    cancelable: false
                }));
            }
        }

        function renderAnimateWithTransform(fromPosition, toPosition, immediate) {

            var speed = o.speed;

            if (immediate) {
                speed = o.immediateSpeed || 50;
            }

            if (o.horizontal) {
                setStyleProperty(slideeElement, 'transform', 'translateX(' + (-round(toPosition)) + 'px)', speed);
            } else {
                setStyleProperty(slideeElement, 'transform', 'translateY(' + (-round(toPosition)) + 'px)', speed);
            }
            self._pos.cur = toPosition;

            dispatchScrollEventIfNeeded();
        }

        function getBoundingClientRect(elem) {

            // Support: BlackBerry 5, iOS 3 (original iPhone)
            // If we don't have gBCR, just use 0,0 rather than error
            if (elem.getBoundingClientRect) {
                return elem.getBoundingClientRect();
            } else {
                return { top: 0, left: 0 };
            }
        }

        /**
         * Returns the position object.
         *
         * @param {Mixed} item
         *
         * @return {Object}
         */
        self.getPos = function (item) {

            var scrollElement = transform ? slideeElement : nativeScrollElement;
            var slideeOffset = getBoundingClientRect(scrollElement);
            var itemOffset = getBoundingClientRect(item);

            var slideeStartPos = o.horizontal ? slideeOffset.left : slideeOffset.top;
            var slideeEndPos = o.horizontal ? slideeOffset.right : slideeOffset.bottom;

            var offset = o.horizontal ? itemOffset.left - slideeOffset.left : itemOffset.top - slideeOffset.top;

            var size = o.horizontal ? itemOffset.width : itemOffset.height;
            if (!size && size !== 0) {
                size = item[o.horizontal ? 'offsetWidth' : 'offsetHeight'];
            }

            var centerOffset = o.centerOffset || 0;

            if (!transform) {
                centerOffset = 0;
                if (o.horizontal) {
                    offset += nativeScrollElement.scrollLeft;
                } else {
                    offset += nativeScrollElement.scrollTop;
                }
            }

            ensureSizeInfo();

            var currentStart = self._pos.cur;
            var currentEnd = currentStart + frameSize;

            //console.log('offset:' + offset + ' currentStart:' + currentStart + ' currentEnd:' + currentEnd);
            var isVisible = offset >= currentStart && (offset + size) <= currentEnd;

            return {
                start: offset,
                center: offset + centerOffset - (frameSize / 2) + (size / 2),
                end: offset - frameSize + size,
                size: size,
                isVisible: isVisible
            };
        };

        self.getCenterPosition = function (item) {

            ensureSizeInfo();

            var pos = self.getPos(item);
            return within(pos.center, pos.start, pos.end);
        };

        function dragInitSlidee(event) {
            var isTouch = event.type === 'touchstart';

            // Ignore when already in progress, or interactive element in non-touch navivagion
            if (dragging.init || !isTouch && isInteractive(event.target)) {
                return;
            }

            // SLIDEE dragging conditions
            if (!(isTouch ? o.touchDragging : o.mouseDragging && event.which < 2)) {
                return;
            }

            if (!isTouch) {
                // prevents native image dragging in Firefox
                event.preventDefault();
            }

            // Reset dragging object
            dragging.released = 0;

            // Properties used in dragHandler
            dragging.init = 0;
            dragging.source = event.target;
            dragging.touch = isTouch;
            var pointer = isTouch ? event.touches[0] : event;
            dragging.initX = pointer.pageX;
            dragging.initY = pointer.pageY;
            dragging.initPos = self._pos.cur;
            dragging.start = +new Date();
            dragging.time = 0;
            dragging.path = 0;
            dragging.delta = 0;
            dragging.locked = 0;
            dragging.pathToLock = isTouch ? 30 : 10;

            // Bind dragging events
            if (transform) {

                if (isTouch) {
                    dragTouchEvents.forEach(function (eventName) {
                        dom.addEventListener(document, eventName, dragHandler, {
                            passive: true
                        });
                    });
                } else {
                    dragMouseEvents.forEach(function (eventName) {
                        dom.addEventListener(document, eventName, dragHandler, {
                            passive: true
                        });
                    });
                }
            }
        }

        /**
		 * Handler for dragging scrollbar handle or SLIDEE.
		 *
		 * @param  {Event} event
		 *
		 * @return {Void}
		 */
        function dragHandler(event) {
            dragging.released = event.type === 'mouseup' || event.type === 'touchend';
            var pointer = dragging.touch ? event[dragging.released ? 'changedTouches' : 'touches'][0] : event;
            dragging.pathX = pointer.pageX - dragging.initX;
            dragging.pathY = pointer.pageY - dragging.initY;
            dragging.path = sqrt(pow(dragging.pathX, 2) + pow(dragging.pathY, 2));
            dragging.delta = o.horizontal ? dragging.pathX : dragging.pathY;

            if (!dragging.released && dragging.path < 1) {
                return;
            }

            // We haven't decided whether this is a drag or not...
            if (!dragging.init) {
                // If the drag path was very short, maybe it's not a drag?
                if (dragging.path < o.dragThreshold) {
                    // If the pointer was released, the path will not become longer and it's
                    // definitely not a drag. If not released yet, decide on next iteration
                    return dragging.released ? dragEnd() : undefined;
                } else {
                    // If dragging path is sufficiently long we can confidently start a drag
                    // if drag is in different direction than scroll, ignore it
                    if (o.horizontal ? abs(dragging.pathX) > abs(dragging.pathY) : abs(dragging.pathX) < abs(dragging.pathY)) {
                        dragging.init = 1;
                    } else {
                        return dragEnd();
                    }
                }
            }

            //event.preventDefault();

            // Disable click on a source element, as it is unwelcome when dragging
            if (!dragging.locked && dragging.path > dragging.pathToLock) {
                dragging.locked = 1;
                dragging.source.addEventListener('click', disableOneEvent);
            }

            // Cancel dragging on release
            if (dragging.released) {
                dragEnd();
            }

            self.slideTo(round(dragging.initPos - dragging.delta));
        }

        /**
		 * Stops dragging and cleans up after it.
		 *
		 * @return {Void}
		 */
        function dragEnd() {
            dragging.released = true;

            dragTouchEvents.forEach(function (eventName) {
                dom.removeEventListener(document, eventName, dragHandler, {
                    passive: true
                });
            });

            dragMouseEvents.forEach(function (eventName) {
                dom.removeEventListener(document, eventName, dragHandler, {
                    passive: true
                });
            });

            // Make sure that disableOneEvent is not active in next tick.
            setTimeout(function () {
                dragging.source.removeEventListener('click', disableOneEvent);
            });

            dragging.init = 0;
        }

        /**
		 * Check whether element is interactive.
		 *
		 * @return {Boolean}
		 */
        function isInteractive(element) {

            while (element) {

                if (interactiveElements.indexOf(element.tagName) !== -1) {
                    return true;
                }

                element = element.parentNode;
            }
            return false;
        }

        /**
		 * Mouse wheel delta normalization.
		 *
		 * @param  {Event} event
		 *
		 * @return {Int}
		 */
        function normalizeWheelDelta(event) {
            // wheelDelta needed only for IE8-
            scrolling.curDelta = ((o.horizontal ? event.deltaY || event.deltaX : event.deltaY) || -event.wheelDelta);

            if (transform) {
                scrolling.curDelta /= event.deltaMode === 1 ? 3 : 100;
            }
            return scrolling.curDelta;
        }

        /**
		 * Mouse scrolling handler.
		 *
		 * @param  {Event} event
		 *
		 * @return {Void}
		 */
        function scrollHandler(event) {

            ensureSizeInfo();
            var pos = self._pos;
            // Ignore if there is no scrolling to be done
            if (!o.scrollBy || pos.start === pos.end) {
                return;
            }
            var delta = normalizeWheelDelta(event);

            if (transform) {
                // Trap scrolling only when necessary and/or requested
                if (delta > 0 && pos.dest < pos.end || delta < 0 && pos.dest > pos.start) {
                    //stopDefault(event, 1);
                }

                self.slideBy(o.scrollBy * delta);
            } else {

                if (isSmoothScrollSupported) {
                    delta *= 12;
                }

                if (o.horizontal) {
                    nativeScrollElement.scrollLeft += delta;
                } else {
                    nativeScrollElement.scrollTop += delta;
                }
            }
        }

        /**
		 * Destroys instance and everything it created.
		 *
		 * @return {Void}
		 */
        self.destroy = function () {

            if (self.frameResizeObserver) {
                self.frameResizeObserver.disconnect();
                self.frameResizeObserver = null;
            }

            // Reset native FRAME element scroll
            dom.removeEventListener(frame, 'scroll', resetScroll, {
                passive: true
            });

            dom.removeEventListener(scrollSource, wheelEvent, scrollHandler, {
                passive: true
            });

            dom.removeEventListener(dragSourceElement, 'touchstart', dragInitSlidee, {
                passive: true
            });

            dom.removeEventListener(frame, 'click', onFrameClick, {
                passive: true,
                capture: true
            });

            dom.removeEventListener(dragSourceElement, 'mousedown', dragInitSlidee, {
                //passive: true
            });

            // Reset initialized status and return the instance
            self.initialized = 0;
            return self;
        };

        var contentRect = {};

        function onResize(entries) {

            var entry = entries[0];

            if (entry) {

                var newRect = entry.contentRect;

                // handle element being hidden
                if (newRect.width === 0 || newRect.height === 0) {
                    return;
                }

                if (newRect.width !== contentRect.width || newRect.height !== contentRect.height) {

                    contentRect = newRect;

                    load(false);
                }
            }
        }

        function resetScroll() {
            if (o.horizontal) {
                this.scrollLeft = 0;
            } else {
                this.scrollTop = 0;
            }
        }

        function onFrameClick(e) {
            if (e.which === 1) {
                var focusableParent = focusManager.focusableParent(e.target);
                if (focusableParent && focusableParent !== document.activeElement) {
                    focusableParent.focus();
                }
            }
        }

        self.getScrollPosition = function () {

            if (transform) {
                return self._pos.cur;
            }

            if (o.horizontal) {
                return nativeScrollElement.scrollLeft;
            } else {
                return nativeScrollElement.scrollTop;
            }
        };

        self.getScrollSize = function () {

            if (transform) {
                return slideeSize;
            }

            if (o.horizontal) {
                return nativeScrollElement.scrollWidth;
            } else {
                return nativeScrollElement.scrollHeight;
            }
        };

        /**
		 * Initialize.
		 *
		 * @return {Object}
		 */
        self.init = function () {
            if (self.initialized) {
                return;
            }

            if (!transform) {
                if (o.horizontal) {
                    if (layoutManager.desktop && !o.hideScrollbar) {
                        nativeScrollElement.classList.add('scrollX');
                    } else {
                        nativeScrollElement.classList.add('scrollX');
                        nativeScrollElement.classList.add('hiddenScrollX');

                        if (layoutManager.tv && o.allowNativeSmoothScroll !== false) {
                            nativeScrollElement.classList.add('smoothScrollX');
                        }
                    }

                    if (o.forceHideScrollbars) {
                        nativeScrollElement.classList.add('hiddenScrollX-forced');
                    }
                } else {
                    if (layoutManager.desktop && !o.hideScrollbar) {
                        nativeScrollElement.classList.add('scrollY');
                    } else {
                        nativeScrollElement.classList.add('scrollY');
                        nativeScrollElement.classList.add('hiddenScrollY');

                        if (layoutManager.tv && o.allowNativeSmoothScroll !== false) {
                            nativeScrollElement.classList.add('smoothScrollY');
                        }
                    }

                    if (o.forceHideScrollbars) {
                        nativeScrollElement.classList.add('hiddenScrollY-forced');
                    }
                }
            } else {
                frame.style.overflow = 'hidden';
                slideeElement.style['will-change'] = 'transform';
                slideeElement.style.transition = 'transform ' + o.speed + 'ms ease-out';

                if (o.horizontal) {
                    slideeElement.classList.add('animatedScrollX');
                } else {
                    slideeElement.classList.add('animatedScrollY');
                }
            }

            if (transform || layoutManager.tv) {
                // This can prevent others from being able to listen to mouse events
                dom.addEventListener(dragSourceElement, 'mousedown', dragInitSlidee, {
                    //passive: true
                });
            }

            initFrameResizeObserver();

            if (transform) {

                dom.addEventListener(dragSourceElement, 'touchstart', dragInitSlidee, {
                    passive: true
                });

                if (!o.horizontal) {
                    dom.addEventListener(frame, 'scroll', resetScroll, {
                        passive: true
                    });
                }

                if (o.mouseWheel) {
                    // Scrolling navigation
                    dom.addEventListener(scrollSource, wheelEvent, scrollHandler, {
                        passive: true
                    });
                }

            } else if (o.horizontal) {

                // Don't bind to mouse events with vertical scroll since the mouse wheel can handle this natively

                if (o.mouseWheel) {
                    // Scrolling navigation
                    dom.addEventListener(scrollSource, wheelEvent, scrollHandler, {
                        passive: true
                    });
                }
            }

            dom.addEventListener(frame, 'click', onFrameClick, {
                passive: true,
                capture: true
            });

            // Mark instance as initialized
            self.initialized = 1;

            // Load
            load(true);

            // Return instance
            return self;
        };
    };

    /**
     * Slide SLIDEE by amount of pixels.
     *
     * @param {Int}  delta     Pixels/Items. Positive means forward, negative means backward.
     * @param {Bool} immediate Reposition immediately without an animation.
     *
     * @return {Void}
     */
    scrollerFactory.prototype.slideBy = function (delta, immediate) {
        if (!delta) {
            return;
        }
        this.slideTo(this._pos.dest + delta, immediate);
    };

    /**
     * Core method for handling `toLocation` methods.
     *
     * @param  {String} location
     * @param  {Mixed}  item
     * @param  {Bool}   immediate
     *
     * @return {Void}
     */
    scrollerFactory.prototype.to = function (location, item, immediate) {
        // Optional arguments logic
        if (type(item) === 'boolean') {
            immediate = item;
            item = undefined;
        }

        if (item === undefined) {
            this.slideTo(this._pos[location], immediate);
        } else {

            //if (!transform) {

            //    item.scrollIntoView();
            //    return;
            //}

            var itemPos = this.getPos(item);

            if (itemPos) {
                this.slideTo(itemPos[location], immediate, itemPos);
            }
        }
    };

    /**
     * Animate element or the whole SLIDEE to the start of the frame.
     *
     * @param {Mixed} item      Item DOM element, or index starting at 0. Omitting will animate SLIDEE.
     * @param {Bool}  immediate Reposition immediately without an animation.
     *
     * @return {Void}
     */
    scrollerFactory.prototype.toStart = function (item, immediate) {
        this.to('start', item, immediate);
    };

    /**
     * Animate element or the whole SLIDEE to the end of the frame.
     *
     * @param {Mixed} item      Item DOM element, or index starting at 0. Omitting will animate SLIDEE.
     * @param {Bool}  immediate Reposition immediately without an animation.
     *
     * @return {Void}
     */
    scrollerFactory.prototype.toEnd = function (item, immediate) {
        this.to('end', item, immediate);
    };

    /**
     * Animate element or the whole SLIDEE to the center of the frame.
     *
     * @param {Mixed} item      Item DOM element, or index starting at 0. Omitting will animate SLIDEE.
     * @param {Bool}  immediate Reposition immediately without an animation.
     *
     * @return {Void}
     */
    scrollerFactory.prototype.toCenter = function (item, immediate) {
        this.to('center', item, immediate);
    };

    scrollerFactory.create = function (frame, options) {
        var instance = new scrollerFactory(frame, options);
        return Promise.resolve(instance);
    };

    return scrollerFactory;
});