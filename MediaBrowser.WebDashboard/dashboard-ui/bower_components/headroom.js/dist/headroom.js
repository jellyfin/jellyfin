/*!
 * headroom.js v0.7.0 - Give your page some headroom. Hide your header until you need it
 * Copyright (c) 2014 Nick Williams - http://wicky.nillia.ms/headroom.js
 * License: MIT
 */

(function (window, document) {

    'use strict';

    /* exported features */

    var requestAnimationFrame = window.requestAnimationFrame || window.webkitRequestAnimationFrame || window.mozRequestAnimationFrame;

    /**
     * Handles debouncing of events via requestAnimationFrame
     * @see http://www.html5rocks.com/en/tutorials/speed/animations/
     * @param {Function} callback The callback to handle whichever event
     */
    function Debouncer(callback) {
        this.callback = callback;
        this.ticking = false;
    }
    Debouncer.prototype = {
        constructor: Debouncer,

        /**
         * dispatches the event to the supplied callback
         * @private
         */
        update: function () {
            this.callback && this.callback();
            this.ticking = false;
        },

        /**
         * ensures events don't get stacked
         * @private
         */
        requestTick: function () {
            if (!this.ticking) {
                requestAnimationFrame(this.rafCallback || (this.rafCallback = this.update.bind(this)));
                this.ticking = true;
            }
        },

        /**
         * Attach this as the event listeners
         */
        handleEvent: function () {
            this.requestTick();
        }
    };
    /**
     * Check if object is part of the DOM
     * @constructor
     * @param {Object} obj element to check
     */
    function isDOMElement(obj) {
        return obj && typeof window !== 'undefined' && (obj === window || obj.nodeType);
    }

    /**
     * Helper function for extending objects
     */
    function extend(object /*, objectN ... */) {
        if (arguments.length <= 0) {
            throw new Error('Missing arguments in extend function');
        }

        var result = object || {},
            key,
            i;

        for (i = 1; i < arguments.length; i++) {
            var replacement = arguments[i] || {};

            for (key in replacement) {
                // Recurse into object except if the object is a DOM element
                if (typeof result[key] === 'object' && !isDOMElement(result[key])) {
                    result[key] = extend(result[key], replacement[key]);
                }
                else {
                    result[key] = result[key] || replacement[key];
                }
            }
        }

        return result;
    }

    /**
     * Helper function for normalizing tolerance option to object format
     */
    function normalizeTolerance(t) {
        return t === Object(t) ? t : { down: t, up: t };
    }

    var supportsCaptureOption = false;
    try {
        var opts = Object.defineProperty({}, 'capture', {
            get: function () {
                supportsCaptureOption = true;
            }
        });
        window.addEventListener("test", null, opts);
    } catch (e) { }

    function addEventListenerWithOptions(target, type, handler, options) {
        var optionsOrCapture = options;
        if (!supportsCaptureOption) {
            optionsOrCapture = options.capture;
        }
        target.addEventListener(type, handler, optionsOrCapture);
    }

    function removeEventListenerWithOptions(target, type, handler, options) {
        var optionsOrCapture = options;
        if (!supportsCaptureOption) {
            optionsOrCapture = options.capture;
        }
        target.removeEventListener(type, handler, optionsOrCapture);
    }

    /**
   * UI enhancement for fixed headers.
   * Hides header when scrolling down
   * Shows header when scrolling up
   * @constructor
   * @param {DOMElement} elem the header element
   * @param {Object} options options for the widget
   */
    function Headroom(elems, options) {
        options = extend(options, Headroom.options);

        this.lastKnownScrollY = 0;
        this.elems = elems;
        this.debouncer = new Debouncer(this.update.bind(this));
        this.tolerance = normalizeTolerance(options.tolerance);
        this.classes = options.classes;
        this.offset = options.offset;
        this.scroller = options.scroller;
        this.initialised = false;
        this.onPin = options.onPin;
        this.onUnpin = options.onUnpin;
    }
    Headroom.prototype = {
        constructor: Headroom,

        /**
         * Initialises the widget
         */
        init: function () {

            for (var i = 0, length = this.elems.length; i < length; i++) {
                this.elems[i].classList.add(this.classes.initial);
            }

            this.attachEvent();

            return this;
        },

        add: function (elem) {
            elem.classList.add(this.classes.initial);
            this.elems.push(elem);
        },

        remove: function (elem) {

            var classes = this.classes;
            elem.classList.remove(classes.unpinned, classes.pinned, classes.initial);
            var i = this.elems.indexOf(elem);
            if (i != -1) {
                this.elems.splice(i, 1);
            }
        },

        /**
         * Unattaches events and removes any classes that were added
         */
        destroy: function () {
            var classes = this.classes;

            this.initialised = false;

            for (var i = 0, length = this.elems.length; i < length; i++) {
                this.elems[i].classList.remove(classes.unpinned, classes.pinned, classes.initial);
            }

            removeEventListenerWithOptions(this.scroller, 'scroll', this.debouncer, {
                capture: false,
                passive: true
            });
        },

        /**
         * Attaches the scroll event
         * @private
         */
        attachEvent: function () {
            if (!this.initialised) {
                this.lastKnownScrollY = this.getScrollY();
                this.initialised = true;
                addEventListenerWithOptions(this.scroller, 'scroll', this.debouncer, {
                    capture: false,
                    passive: true
                });

                this.debouncer.handleEvent();
            }
        },

        /**
         * Unpins the header if it's currently pinned
         */
        unpin: function () {

            var classes = this.classes;

            for (var i = 0, length = this.elems.length; i < length; i++) {
                var classList = this.elems[i].classList;

                if (classList.contains(classes.pinned) || !classList.contains(classes.unpinned)) {
                    classList.add(classes.unpinned);
                    classList.remove(classes.pinned);
                    this.onUnpin && this.onUnpin.call(this);
                }
            }
        },

        /**
         * Pins the header if it's currently unpinned
         */
        pin: function () {

            var classes = this.classes;

            for (var i = 0, length = this.elems.length; i < length; i++) {
                var classList = this.elems[i].classList;

                if (classList.contains(classes.unpinned)) {
                    classList.remove(classes.unpinned);
                    classList.add(classes.pinned);
                    this.onPin && this.onPin.call(this);
                }
            }

        },

        /**
         * Gets the Y scroll position
         * @see https://developer.mozilla.org/en-US/docs/Web/API/Window.scrollY
         * @return {Number} pixels the page has scrolled along the Y-axis
         */
        getScrollY: function () {

            var pageYOffset = this.scroller.pageYOffset;
            if (pageYOffset !== undefined) {
                return pageYOffset;
            }

            var scrollTop = this.scroller.scrollTop;
            if (scrollTop !== undefined) {
                return scrollTop;
            }

            return (document.documentElement || document.body).scrollTop;
        },

        /**
         * determines if the tolerance has been exceeded
         * @param  {int} currentScrollY the current scroll y position
         * @return {bool} true if tolerance exceeded, false otherwise
         */
        toleranceExceeded: function (currentScrollY, direction) {
            return Math.abs(currentScrollY - this.lastKnownScrollY) >= this.tolerance[direction];
        },

        /**
         * determine if it is appropriate to unpin
         * @param  {int} currentScrollY the current y scroll position
         * @param  {bool} toleranceExceeded has the tolerance been exceeded?
         * @return {bool} true if should unpin, false otherwise
         */
        shouldUnpin: function (currentScrollY, toleranceExceeded) {
            var scrollingDown = currentScrollY > this.lastKnownScrollY,
              pastOffset = currentScrollY >= this.offset;

            return scrollingDown && pastOffset && toleranceExceeded;
        },

        /**
         * determine if it is appropriate to pin
         * @param  {int} currentScrollY the current y scroll position
         * @param  {bool} toleranceExceeded has the tolerance been exceeded?
         * @return {bool} true if should pin, false otherwise
         */
        shouldPin: function (currentScrollY, toleranceExceeded) {
            var scrollingUp = currentScrollY < this.lastKnownScrollY,
              pastOffset = currentScrollY <= this.offset;

            return (scrollingUp && toleranceExceeded) || pastOffset;
        },

        /**
         * Handles updating the state of the widget
         */
        update: function () {
            var currentScrollY = this.getScrollY(),
              scrollDirection = currentScrollY > this.lastKnownScrollY ? 'down' : 'up',
              toleranceExceeded = this.toleranceExceeded(currentScrollY, scrollDirection);

            if (currentScrollY < 0) { // Ignore bouncy scrolling in OSX
                return;
            }

            if (this.shouldUnpin(currentScrollY, toleranceExceeded)) {
                this.unpin();
            }
            else if (this.shouldPin(currentScrollY, toleranceExceeded)) {
                this.pin();
            }

            this.lastKnownScrollY = currentScrollY;
        }
    };
    /**
     * Default options
     * @type {Object}
     */
    Headroom.options = {
        tolerance: {
            up: 0,
            down: 0
        },
        offset: 0,
        scroller: window,
        classes: {
            pinned: 'headroom--pinned',
            unpinned: 'headroom--unpinned',
            initial: 'headroom'
        }
    };

    window.Headroom = Headroom;

}(window, document));