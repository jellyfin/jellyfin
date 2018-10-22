define(["dom", "layoutManager", "browser", "css!./headroom"], function(dom, layoutManager, browser) {
    "use strict";

    function Debouncer(callback) {
        this.callback = callback, this.ticking = !1
    }

    function onHeadroomClearedExternally() {
        this.state = null
    }

    function Headroom(elems, options) {
        options = Object.assign(Headroom.options, options || {}), this.lastKnownScrollY = 0, this.elems = elems, this.scroller = options.scroller, this.debouncer = onScroll.bind(this), this.offset = options.offset, this.initialised = !1, this.initialClass = options.initialClass, this.unPinnedClass = options.unPinnedClass, this.pinnedClass = options.pinnedClass, this.state = "clear"
    }

    function onScroll() {
        this.paused || requestAnimationFrame(this.rafCallback || (this.rafCallback = this.update.bind(this)))
    }
    var requestAnimationFrame = window.requestAnimationFrame || window.webkitRequestAnimationFrame || window.mozRequestAnimationFrame;
    return Debouncer.prototype = {
        constructor: Debouncer,
        update: function() {
            this.callback && this.callback(), this.ticking = !1
        },
        handleEvent: function() {
            this.ticking || (requestAnimationFrame(this.rafCallback || (this.rafCallback = this.update.bind(this))), this.ticking = !0)
        }
    }, Headroom.prototype = {
        constructor: Headroom,
        init: function() {
            if (browser.supportsCssAnimation()) {
                for (var i = 0, length = this.elems.length; i < length; i++) this.elems[i].classList.add(this.initialClass), this.elems[i].addEventListener("clearheadroom", onHeadroomClearedExternally.bind(this));
                this.attachEvent()
            }
            return this
        },
        add: function(elem) {
            browser.supportsCssAnimation() && (elem.classList.add(this.initialClass), elem.addEventListener("clearheadroom", onHeadroomClearedExternally.bind(this)), this.elems.push(elem))
        },
        remove: function(elem) {
            elem.classList.remove(this.unPinnedClass), elem.classList.remove(this.initialClass), elem.classList.remove(this.pinnedClass);
            var i = this.elems.indexOf(elem); - 1 !== i && this.elems.splice(i, 1)
        },
        pause: function() {
            this.paused = !0
        },
        resume: function() {
            this.paused = !1
        },
        destroy: function() {
            this.initialised = !1;
            for (var i = 0, length = this.elems.length; i < length; i++) {
                var classList = this.elems[i].classList;
                classList.remove(this.unPinnedClass), classList.remove(this.initialClass), classList.remove(this.pinnedClass)
            }
            var scrollEventName = this.scroller.getScrollEventName ? this.scroller.getScrollEventName() : "scroll";
            dom.removeEventListener(this.scroller, scrollEventName, this.debouncer, {
                capture: !1,
                passive: !0
            })
        },
        attachEvent: function() {
            if (!this.initialised) {
                this.lastKnownScrollY = this.getScrollY(), this.initialised = !0;
                var scrollEventName = this.scroller.getScrollEventName ? this.scroller.getScrollEventName() : "scroll";
                dom.addEventListener(this.scroller, scrollEventName, this.debouncer, {
                    capture: !1,
                    passive: !0
                }), this.update()
            }
        },
        clear: function() {
            if ("clear" !== this.state) {
                this.state = "clear";
                for (var unpinnedClass = this.unPinnedClass, i = (this.pinnedClass, 0), length = this.elems.length; i < length; i++) {
                    this.elems[i].classList.remove(unpinnedClass)
                }
            }
        },
        pin: function() {
            if ("pin" !== this.state) {
                this.state = "pin";
                for (var unpinnedClass = this.unPinnedClass, pinnedClass = this.pinnedClass, i = 0, length = this.elems.length; i < length; i++) {
                    var classList = this.elems[i].classList;
                    classList.remove(unpinnedClass), classList.add(pinnedClass)
                }
            }
        },
        unpin: function() {
            if ("unpin" !== this.state) {
                this.state = "unpin";
                for (var unpinnedClass = this.unPinnedClass, i = (this.pinnedClass, 0), length = this.elems.length; i < length; i++) {
                    this.elems[i].classList.add(unpinnedClass)
                }
            }
        },
        getScrollY: function() {
            var scroller = this.scroller;
            if (scroller.getScrollPosition) return scroller.getScrollPosition();
            var pageYOffset = scroller.pageYOffset;
            if (void 0 !== pageYOffset) return pageYOffset;
            var scrollTop = scroller.scrollTop;
            return void 0 !== scrollTop ? scrollTop : (document.documentElement || document.body).scrollTop
        },
        shouldUnpin: function(currentScrollY) {
            var scrollingDown = currentScrollY > this.lastKnownScrollY,
                pastOffset = currentScrollY >= this.offset;
            return scrollingDown && pastOffset
        },
        shouldPin: function(currentScrollY) {
            var scrollingUp = currentScrollY < this.lastKnownScrollY,
                pastOffset = currentScrollY <= this.offset;
            return scrollingUp || pastOffset
        },
        update: function() {
            if (!this.paused) {
                var currentScrollY = this.getScrollY(),
                    lastKnownScrollY = this.lastKnownScrollY,
                    isTv = layoutManager.tv;
                if (currentScrollY <= (isTv ? 120 : 10)) this.clear();
                else if (this.shouldUnpin(currentScrollY)) this.unpin();
                else if (this.shouldPin(currentScrollY)) {
                    var toleranceExceeded = Math.abs(currentScrollY - lastKnownScrollY) >= 14;
                    currentScrollY && isTv ? this.unpin() : toleranceExceeded && this.clear()
                }
                this.lastKnownScrollY = currentScrollY
            }
        }
    }, Headroom.options = {
        offset: 0,
        scroller: window,
        initialClass: "headroom",
        unPinnedClass: "headroom--unpinned",
        pinnedClass: "headroom--pinned"
    }, Headroom
});