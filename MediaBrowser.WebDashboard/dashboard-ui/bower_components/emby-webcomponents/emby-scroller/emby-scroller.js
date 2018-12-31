define(['scroller', 'dom', 'layoutManager', 'inputManager', 'focusManager', 'browser', 'registerElement'], function (scroller, dom, layoutManager, inputManager, focusManager, browser) {
    'use strict';

    var ScrollerProtoType = Object.create(HTMLDivElement.prototype);

    ScrollerProtoType.createdCallback = function () {
        this.classList.add('emby-scroller');
    };

    function initCenterFocus(elem, scrollerInstance) {

        dom.addEventListener(elem, 'focus', function (e) {

            var focused = focusManager.focusableParent(e.target);

            if (focused) {
                scrollerInstance.toCenter(focused);
            }

        }, {
            capture: true,
            passive: true
        });
    }

    ScrollerProtoType.scrollToBeginning = function () {
        if (this.scroller) {
            this.scroller.slideTo(0, true);
        }
    };
    ScrollerProtoType.toStart = function (elem, immediate) {
        if (this.scroller) {
            this.scroller.toStart(elem, immediate);
        }
    };
    ScrollerProtoType.toCenter = function (elem, immediate) {
        if (this.scroller) {
            this.scroller.toCenter(elem, immediate);
        }
    };

    ScrollerProtoType.scrollToPosition = function (pos, immediate) {
        if (this.scroller) {
            this.scroller.slideTo(pos, immediate);
        }
    };

    ScrollerProtoType.getScrollPosition = function () {
        if (this.scroller) {
            return this.scroller.getScrollPosition();
        }
    };

    ScrollerProtoType.getScrollSize = function () {
        if (this.scroller) {
            return this.scroller.getScrollSize();
        }
    };

    ScrollerProtoType.getScrollEventName = function () {
        if (this.scroller) {
            return this.scroller.getScrollEventName();
        }
    };

    ScrollerProtoType.getScrollSlider = function () {
        if (this.scroller) {
            return this.scroller.getScrollSlider();
        }
    };

    ScrollerProtoType.addScrollEventListener = function (fn, options) {
        if (this.scroller) {
            dom.addEventListener(this.scroller.getScrollFrame(), this.scroller.getScrollEventName(), fn, options);
        }
    };

    ScrollerProtoType.removeScrollEventListener = function (fn, options) {
        if (this.scroller) {
            dom.removeEventListener(this.scroller.getScrollFrame(), this.scroller.getScrollEventName(), fn, options);
        }
    };

    function onInputCommand(e) {

        var cmd = e.detail.command;

        if (cmd === 'end') {
            focusManager.focusLast(this, '.' + this.getAttribute('data-navcommands'));
            e.preventDefault();
            e.stopPropagation();
        }
        else if (cmd === 'pageup') {
            focusManager.moveFocus(e.target, this, '.' + this.getAttribute('data-navcommands'), -12);
            e.preventDefault();
            e.stopPropagation();
        }
        else if (cmd === 'pagedown') {
            focusManager.moveFocus(e.target, this, '.' + this.getAttribute('data-navcommands'), 12);
            e.preventDefault();
            e.stopPropagation();
        }
    }

    function initHeadroom(elem) {
        require(['headroom'], function (Headroom) {

            var headroom = new Headroom([], {
                scroller: elem
            });
            // initialise
            headroom.init();
            headroom.add(document.querySelector('.skinHeader'));
            elem.headroom = headroom;
        });
    }

    ScrollerProtoType.attachedCallback = function () {

        if (this.getAttribute('data-navcommands')) {
            inputManager.on(this, onInputCommand);
        }

        var horizontal = this.getAttribute('data-horizontal') !== 'false';

        var slider = this.querySelector('.scrollSlider');

        if (horizontal) {
            slider.style['white-space'] = 'nowrap';
        }

        var bindHeader = this.getAttribute('data-bindheader') === 'true';

        var scrollFrame = this;
        var enableScrollButtons = layoutManager.desktop && horizontal && this.getAttribute('data-scrollbuttons') !== 'false';

        var options = {
            horizontal: horizontal,
            mouseDragging: 1,
            mouseWheel: this.getAttribute('data-mousewheel') !== 'false',
            touchDragging: 1,
            slidee: slider,
            scrollBy: 200,
            speed: horizontal ? 270 : 240,
            //immediateSpeed: pageOptions.immediateSpeed,
            elasticBounds: 1,
            dragHandle: 1,
            scrollWidth: this.getAttribute('data-scrollsize') === 'auto' ? null : 5000000,
            autoImmediate: true,
            skipSlideToWhenVisible: this.getAttribute('data-skipfocuswhenvisible') === 'true',
            dispatchScrollEvent: enableScrollButtons || bindHeader || this.getAttribute('data-scrollevent') === 'true',
            hideScrollbar: enableScrollButtons || this.getAttribute('data-hidescrollbar') === 'true',
            allowNativeSmoothScroll: this.getAttribute('data-allownativesmoothscroll') === 'true' && !enableScrollButtons,
            allowNativeScroll: !enableScrollButtons,
            forceHideScrollbars: enableScrollButtons,

            // In edge, with the native scroll, the content jumps around when hovering over the buttons
            requireAnimation: enableScrollButtons && browser.edge
        };

        // If just inserted it might not have any height yet - yes this is a hack
        this.scroller = new scroller(scrollFrame, options);
        this.scroller.init();

        if (layoutManager.tv && this.getAttribute('data-centerfocus')) {
            initCenterFocus(this, this.scroller);
        }

        if (bindHeader) {
            initHeadroom(this);
        }

        if (enableScrollButtons) {
            loadScrollButtons(this);
        }
    };

    function loadScrollButtons(scroller) {

        require(['emby-scrollbuttons'], function () {
            scroller.insertAdjacentHTML('beforeend', '<div is="emby-scrollbuttons"></div>');
        });
    }

    ScrollerProtoType.pause = function () {

        var headroom = this.headroom;
        if (headroom) {
            headroom.pause();
        }
    };

    ScrollerProtoType.resume = function () {

        var headroom = this.headroom;
        if (headroom) {
            headroom.resume();
        }
    };

    ScrollerProtoType.detachedCallback = function () {

        if (this.getAttribute('data-navcommands')) {
            inputManager.off(this, onInputCommand);
        }

        var headroom = this.headroom;
        if (headroom) {
            headroom.destroy();
            this.headroom = null;
        }

        var scrollerInstance = this.scroller;
        if (scrollerInstance) {
            scrollerInstance.destroy();
            this.scroller = null;
        }
    };

    document.registerElement('emby-scroller', {
        prototype: ScrollerProtoType,
        extends: 'div'
    });
});