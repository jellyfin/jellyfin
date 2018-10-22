define(["scroller", "dom", "layoutManager", "inputManager", "focusManager", "browser", "registerElement"], function(scroller, dom, layoutManager, inputManager, focusManager, browser) {
    "use strict";

    function initCenterFocus(elem, scrollerInstance) {
        dom.addEventListener(elem, "focus", function(e) {
            var focused = focusManager.focusableParent(e.target);
            focused && scrollerInstance.toCenter(focused)
        }, {
            capture: !0,
            passive: !0
        })
    }

    function onInputCommand(e) {
        var cmd = e.detail.command;
        "end" === cmd ? (focusManager.focusLast(this, "." + this.getAttribute("data-navcommands")), e.preventDefault(), e.stopPropagation()) : "pageup" === cmd ? (focusManager.moveFocus(e.target, this, "." + this.getAttribute("data-navcommands"), -12), e.preventDefault(), e.stopPropagation()) : "pagedown" === cmd && (focusManager.moveFocus(e.target, this, "." + this.getAttribute("data-navcommands"), 12), e.preventDefault(), e.stopPropagation())
    }

    function initHeadroom(elem) {
        require(["headroom"], function(Headroom) {
            var headroom = new Headroom([], {
                scroller: elem
            });
            headroom.init(), headroom.add(document.querySelector(".skinHeader")), elem.headroom = headroom
        })
    }

    function loadScrollButtons(scroller) {
        require(["emby-scrollbuttons"], function() {
            scroller.insertAdjacentHTML("beforeend", '<div is="emby-scrollbuttons"></div>')
        })
    }
    var ScrollerProtoType = Object.create(HTMLDivElement.prototype);
    ScrollerProtoType.createdCallback = function() {
        this.classList.add("emby-scroller")
    }, ScrollerProtoType.scrollToBeginning = function() {
        this.scroller && this.scroller.slideTo(0, !0)
    }, ScrollerProtoType.toStart = function(elem, immediate) {
        this.scroller && this.scroller.toStart(elem, immediate)
    }, ScrollerProtoType.toCenter = function(elem, immediate) {
        this.scroller && this.scroller.toCenter(elem, immediate)
    }, ScrollerProtoType.scrollToPosition = function(pos, immediate) {
        this.scroller && this.scroller.slideTo(pos, immediate)
    }, ScrollerProtoType.getScrollPosition = function() {
        if (this.scroller) return this.scroller.getScrollPosition()
    }, ScrollerProtoType.getScrollSize = function() {
        if (this.scroller) return this.scroller.getScrollSize()
    }, ScrollerProtoType.getScrollEventName = function() {
        if (this.scroller) return this.scroller.getScrollEventName()
    }, ScrollerProtoType.getScrollSlider = function() {
        if (this.scroller) return this.scroller.getScrollSlider()
    }, ScrollerProtoType.addScrollEventListener = function(fn, options) {
        this.scroller && dom.addEventListener(this.scroller.getScrollFrame(), this.scroller.getScrollEventName(), fn, options)
    }, ScrollerProtoType.removeScrollEventListener = function(fn, options) {
        this.scroller && dom.removeEventListener(this.scroller.getScrollFrame(), this.scroller.getScrollEventName(), fn, options)
    }, ScrollerProtoType.attachedCallback = function() {
        this.getAttribute("data-navcommands") && inputManager.on(this, onInputCommand);
        var horizontal = "false" !== this.getAttribute("data-horizontal"),
            slider = this.querySelector(".scrollSlider");
        horizontal && (slider.style["white-space"] = "nowrap");
        var bindHeader = "true" === this.getAttribute("data-bindheader"),
            scrollFrame = this,
            enableScrollButtons = layoutManager.desktop && horizontal && "false" !== this.getAttribute("data-scrollbuttons"),
            options = {
                horizontal: horizontal,
                mouseDragging: 1,
                mouseWheel: "false" !== this.getAttribute("data-mousewheel"),
                touchDragging: 1,
                slidee: slider,
                scrollBy: 200,
                speed: horizontal ? 270 : 240,
                elasticBounds: 1,
                dragHandle: 1,
                scrollWidth: "auto" === this.getAttribute("data-scrollsize") ? null : 5e6,
                autoImmediate: !0,
                skipSlideToWhenVisible: "true" === this.getAttribute("data-skipfocuswhenvisible"),
                dispatchScrollEvent: enableScrollButtons || bindHeader || "true" === this.getAttribute("data-scrollevent"),
                hideScrollbar: enableScrollButtons || "true" === this.getAttribute("data-hidescrollbar"),
                allowNativeSmoothScroll: "true" === this.getAttribute("data-allownativesmoothscroll") && !enableScrollButtons,
                allowNativeScroll: !enableScrollButtons,
                forceHideScrollbars: enableScrollButtons,
                requireAnimation: enableScrollButtons && browser.edge
            };
        this.scroller = new scroller(scrollFrame, options), this.scroller.init(), layoutManager.tv && this.getAttribute("data-centerfocus") && initCenterFocus(this, this.scroller), bindHeader && initHeadroom(this), enableScrollButtons && loadScrollButtons(this)
    }, ScrollerProtoType.pause = function() {
        var headroom = this.headroom;
        headroom && headroom.pause()
    }, ScrollerProtoType.resume = function() {
        var headroom = this.headroom;
        headroom && headroom.resume()
    }, ScrollerProtoType.detachedCallback = function() {
        this.getAttribute("data-navcommands") && inputManager.off(this, onInputCommand);
        var headroom = this.headroom;
        headroom && (headroom.destroy(), this.headroom = null);
        var scrollerInstance = this.scroller;
        scrollerInstance && (scrollerInstance.destroy(), this.scroller = null)
    }, document.registerElement("emby-scroller", {
        prototype: ScrollerProtoType,
        extends: "div"
    })
});