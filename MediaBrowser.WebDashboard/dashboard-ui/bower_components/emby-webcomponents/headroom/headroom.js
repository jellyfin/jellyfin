/*!
 * headroom.js v0.7.0 - Give your page some headroom. Hide your header until you need it
 * Copyright (c) 2014 Nick Williams - http://wicky.nillia.ms/headroom.js
 * License: MIT
 */

define(['dom', 'layoutManager', 'browser', 'css!./headroom'], function (dom, layoutManager, browser) {

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
            if (this.callback) {
                this.callback();
            }
            this.ticking = false;
        },

        /**
         * Attach this as the event listeners
         */
        handleEvent: function () {
            if (!this.ticking) {
                requestAnimationFrame(this.rafCallback || (this.rafCallback = this.update.bind(this)));
                this.ticking = true;
            }
        }
    };

    function onHeadroomClearedExternally() {
        this.state = null;
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
        options = Object.assign(Headroom.options, options || {});

        this.lastKnownScrollY = 0;
        this.elems = elems;

        this.scroller = options.scroller;

        this.debouncer = onScroll.bind(this);
        this.offset = options.offset;
        this.initialised = false;

        this.initialClass = options.initialClass;
        this.unPinnedClass = options.unPinnedClass;
        this.pinnedClass = options.pinnedClass;

        this.state = 'clear';
    }

    function onScroll() {

        if (this.paused) {
            return;
        }

        requestAnimationFrame(this.rafCallback || (this.rafCallback = this.update.bind(this)));
    }

    Headroom.prototype = {
        constructor: Headroom,

        /**
         * Initialises the widget
         */
        init: function () {

            if (browser.supportsCssAnimation()) {
                for (var i = 0, length = this.elems.length; i < length; i++) {
                    this.elems[i].classList.add(this.initialClass);
                    this.elems[i].addEventListener('clearheadroom', onHeadroomClearedExternally.bind(this));
                }

                this.attachEvent();
            }

            return this;
        },

        add: function (elem) {

            if (browser.supportsCssAnimation()) {
                elem.classList.add(this.initialClass);
                elem.addEventListener('clearheadroom', onHeadroomClearedExternally.bind(this));
                this.elems.push(elem);
            }
        },

        remove: function (elem) {

            elem.classList.remove(this.unPinnedClass);
            elem.classList.remove(this.initialClass);
            elem.classList.remove(this.pinnedClass);

            var i = this.elems.indexOf(elem);
            if (i !== -1) {
                this.elems.splice(i, 1);
            }
        },

        pause: function () {
            this.paused = true;
        },

        resume: function () {
            this.paused = false;
        },

        /**
         * Unattaches events and removes any classes that were added
         */
        destroy: function () {

            this.initialised = false;

            for (var i = 0, length = this.elems.length; i < length; i++) {

                var classList = this.elems[i].classList;

                classList.remove(this.unPinnedClass);
                classList.remove(this.initialClass);
                classList.remove(this.pinnedClass);
            }

            var scrollEventName = this.scroller.getScrollEventName ? this.scroller.getScrollEventName() : 'scroll';

            dom.removeEventListener(this.scroller, scrollEventName, this.debouncer, {
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

                var scrollEventName = this.scroller.getScrollEventName ? this.scroller.getScrollEventName() : 'scroll';

                dom.addEventListener(this.scroller, scrollEventName, this.debouncer, {
                    capture: false,
                    passive: true
                });

                this.update();
            }
        },

        /**
         * Unpins the header if it's currently pinned
         */
        clear: function () {

            if (this.state === 'clear') {
                return;
            }

            this.state = 'clear';

            var unpinnedClass = this.unPinnedClass;
            var pinnedClass = this.pinnedClass;

            for (var i = 0, length = this.elems.length; i < length; i++) {
                var classList = this.elems[i].classList;

                classList.remove(unpinnedClass);
                //classList.remove(pinnedClass);
            }
        },

        /**
         * Unpins the header if it's currently pinned
         */
        pin: function () {

            if (this.state === 'pin') {
                return;
            }

            this.state = 'pin';

            var unpinnedClass = this.unPinnedClass;
            var pinnedClass = this.pinnedClass;

            for (var i = 0, length = this.elems.length; i < length; i++) {
                var classList = this.elems[i].classList;

                classList.remove(unpinnedClass);
                classList.add(pinnedClass);
            }
        },

        /**
         * Unpins the header if it's currently pinned
         */
        unpin: function () {

            if (this.state === 'unpin') {
                return;
            }

            this.state = 'unpin';

            var unpinnedClass = this.unPinnedClass;
            var pinnedClass = this.pinnedClass;

            for (var i = 0, length = this.elems.length; i < length; i++) {
                var classList = this.elems[i].classList;

                classList.add(unpinnedClass);
                //classList.remove(pinnedClass);
            }
        },

        /**
         * Gets the Y scroll position
         * @see https://developer.mozilla.org/en-US/docs/Web/API/Window.scrollY
         * @return {Number} pixels the page has scrolled along the Y-axis
         */
        getScrollY: function () {

            var scroller = this.scroller;

            if (scroller.getScrollPosition) {
                return scroller.getScrollPosition();
            }

            var pageYOffset = scroller.pageYOffset;
            if (pageYOffset !== undefined) {
                return pageYOffset;
            }

            var scrollTop = scroller.scrollTop;
            if (scrollTop !== undefined) {
                return scrollTop;
            }

            return (document.documentElement || document.body).scrollTop;
        },

        /**
         * determine if it is appropriate to unpin
         * @param  {int} currentScrollY the current y scroll position
         * @return {bool} true if should unpin, false otherwise
         */
        shouldUnpin: function (currentScrollY) {
            var scrollingDown = currentScrollY > this.lastKnownScrollY,
                pastOffset = currentScrollY >= this.offset;

            return scrollingDown && pastOffset;
        },

        /**
         * determine if it is appropriate to pin
         * @param  {int} currentScrollY the current y scroll position
         * @return {bool} true if should pin, false otherwise
         */
        shouldPin: function (currentScrollY) {
            var scrollingUp = currentScrollY < this.lastKnownScrollY,
                pastOffset = currentScrollY <= this.offset;

            return scrollingUp || pastOffset;
        },

        /**
         * Handles updating the state of the widget
         */
        update: function () {

            if (this.paused) {
                return;
            }

            var currentScrollY = this.getScrollY();

            var lastKnownScrollY = this.lastKnownScrollY;

            var isTv = layoutManager.tv;

            if (currentScrollY <= (isTv ? 120 : 10)) {
                this.clear();
            }
            else if (this.shouldUnpin(currentScrollY)) {
                this.unpin();
            }
            else if (this.shouldPin(currentScrollY)) {

                var toleranceExceeded = Math.abs(currentScrollY - lastKnownScrollY) >= 14;

                if (currentScrollY && isTv) {
                    this.unpin();
                } else if (toleranceExceeded) {
                    this.clear();
                }
            } else if (isTv) {
                //this.clear();
            }

            this.lastKnownScrollY = currentScrollY;
        }
    };
    /**
     * Default options
     * @type {Object}
     */
    Headroom.options = {
        offset: 0,
        scroller: window,
        initialClass: 'headroom',
        unPinnedClass: 'headroom--unpinned',
        pinnedClass: 'headroom--pinned'
    };

    return Headroom;

});