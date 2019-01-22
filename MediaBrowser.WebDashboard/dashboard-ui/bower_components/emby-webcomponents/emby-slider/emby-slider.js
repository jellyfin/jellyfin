define(["browser", "dom", "layoutManager", "css!./emby-slider", "registerElement", "emby-input"], function(browser, dom, layoutManager) {
    "use strict";

    function updateValues() {
        var range = this,
            value = range.value;
        requestAnimationFrame(function() {
            var backgroundLower = range.backgroundLower;
            if (backgroundLower) {
                var fraction = (value - range.min) / (range.max - range.min);
                enableWidthWithTransform ? backgroundLower.style.transform = "scaleX(" + fraction + ")" : (fraction *= 100, backgroundLower.style.width = fraction + "%")
            }
        })
    }

    function updateBubble(range, value, bubble, bubbleText) {
        requestAnimationFrame(function() {
            bubble.style.left = value + "%", range.getBubbleHtml ? value = range.getBubbleHtml(value) : (value = range.getBubbleText ? range.getBubbleText(value) : Math.round(value), value = '<h1 class="sliderBubbleText">' + value + "</h1>"), bubble.innerHTML = value
        })
    }

    function setRange(elem, startPercent, endPercent) {
        var style = elem.style;
        style.left = Math.max(startPercent, 0) + "%";
        var widthPercent = endPercent - startPercent;
        style.width = Math.max(Math.min(widthPercent, 100), 0) + "%"
    }

    function mapRangesFromRuntimeToPercent(ranges, runtime) {
        return runtime ? ranges.map(function(r) {
            return {
                start: r.start / runtime * 100,
                end: r.end / runtime * 100
            }
        }) : []
    }

    function startInterval(range) {
        var interval = range.interval;
        interval && clearInterval(interval), range.interval = setInterval(updateValues.bind(range), 100)
    }
    var enableWidthWithTransform, EmbySliderPrototype = Object.create(HTMLInputElement.prototype),
        supportsNativeProgressStyle = browser.firefox,
        supportsValueSetOverride = !1;
    if (Object.getOwnPropertyDescriptor && Object.defineProperty) {
        var descriptor = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, "value");
        descriptor && descriptor.configurable && (supportsValueSetOverride = !0)
    }
    EmbySliderPrototype.attachedCallback = function() {
        if ("true" !== this.getAttribute("data-embyslider")) {
            this.setAttribute("data-embyslider", "true"), this.classList.add("mdl-slider"), this.classList.add("mdl-js-slider"), browser.noFlex && this.classList.add("slider-no-webkit-thumb"), layoutManager.mobile || this.classList.add("mdl-slider-hoverthumb");
            var containerElement = this.parentNode;
            containerElement.classList.add("mdl-slider-container");
            var htmlToInsert = "";
            supportsNativeProgressStyle || (htmlToInsert += '<div class="mdl-slider-background-flex">', htmlToInsert += '<div class="mdl-slider-background-flex-inner">', htmlToInsert += '<div class="mdl-slider-background-upper"></div>', htmlToInsert += enableWidthWithTransform ? '<div class="mdl-slider-background-lower mdl-slider-background-lower-withtransform"></div>' : '<div class="mdl-slider-background-lower"></div>', htmlToInsert += "</div>", htmlToInsert += "</div>"), htmlToInsert += '<div class="sliderBubble hide"></div>', containerElement.insertAdjacentHTML("beforeend", htmlToInsert), this.backgroundLower = containerElement.querySelector(".mdl-slider-background-lower"), this.backgroundUpper = containerElement.querySelector(".mdl-slider-background-upper");
            var sliderBubble = containerElement.querySelector(".sliderBubble"),
                hasHideClass = sliderBubble.classList.contains("hide");
            dom.addEventListener(this, "input", function(e) {
                this.dragging = !0, updateBubble(this, this.value, sliderBubble), hasHideClass && (sliderBubble.classList.remove("hide"), hasHideClass = !1)
            }, {
                passive: !0
            }), dom.addEventListener(this, "change", function() {
                this.dragging = !1, updateValues.call(this), sliderBubble.classList.add("hide"), hasHideClass = !0
            }, {
                passive: !0
            }), browser.firefox || (dom.addEventListener(this, window.PointerEvent ? "pointermove" : "mousemove", function(e) {
                if (!this.dragging) {
                    var rect = this.getBoundingClientRect(),
                        clientX = e.clientX,
                        bubbleValue = (clientX - rect.left) / rect.width;
                    bubbleValue *= 100, updateBubble(this, bubbleValue, sliderBubble), hasHideClass && (sliderBubble.classList.remove("hide"), hasHideClass = !1)
                }
            }, {
                passive: !0
            }), dom.addEventListener(this, window.PointerEvent ? "pointerleave" : "mouseleave", function() {
                sliderBubble.classList.add("hide"), hasHideClass = !0
            }, {
                passive: !0
            })), supportsNativeProgressStyle || (supportsValueSetOverride ? this.addEventListener("valueset", updateValues) : startInterval(this))
        }
    }, EmbySliderPrototype.setBufferedRanges = function(ranges, runtime, position) {
        var elem = this.backgroundUpper;
        if (elem) {
            null != runtime && (ranges = mapRangesFromRuntimeToPercent(ranges, runtime), position = position / runtime * 100);
            for (var i = 0, length = ranges.length; i < length; i++) {
                var range = ranges[i];
                if (!(null != position && position >= range.end)) return void setRange(elem, range.start, range.end)
            }
            setRange(elem, 0, 0)
        }
    }, EmbySliderPrototype.setIsClear = function(isClear) {
        var backgroundLower = this.backgroundLower;
        backgroundLower && (isClear ? backgroundLower.classList.add("mdl-slider-background-lower-clear") : backgroundLower.classList.remove("mdl-slider-background-lower-clear"))
    }, EmbySliderPrototype.detachedCallback = function() {
        var interval = this.interval;
        interval && clearInterval(interval), this.interval = null, this.backgroundUpper = null, this.backgroundLower = null
    }, document.registerElement("emby-slider", {
        prototype: EmbySliderPrototype,
        extends: "input"
    })
});