define(["lazyLoader", "imageFetcher", "layoutManager", "browser", "appSettings", "require", "css!./style"], function(lazyLoader, imageFetcher, layoutManager, browser, appSettings, require) {
    "use strict";

    function fillImage(elem, source, enableEffects) {
        if (!elem) throw new Error("elem cannot be null");
        source || (source = elem.getAttribute("data-src")), source && fillImageElement(elem, source, enableEffects)
    }

    function fillImageElement(elem, source, enableEffects) {
        imageFetcher.loadImage(elem, source).then(function() {
            enableFade && !1 !== enableEffects && fadeIn(elem), elem.removeAttribute("data-src")
        })
    }

    function getVibrantInfoFromElement(elem, url) {
        return new Promise(function(resolve, reject) {
            require(["vibrant"], function() {
                if ("IMG" === elem.tagName) return void resolve(getVibrantInfo(elem, url));
                var img = new Image;
                img.onload = function() {
                    resolve(getVibrantInfo(img, url))
                }, img.src = url
            })
        })
    }

    function getSettingsKey(url) {
        var parts = url.split("://");
        url = parts[parts.length - 1], url = url.substring(url.indexOf("/") + 1), url = url.split("?")[0];
        return "vibrant31" + url
    }

    function getCachedVibrantInfo(url) {
        return appSettings.get(getSettingsKey(url))
    }

    function getVibrantInfo(img, url) {
        var value = getCachedVibrantInfo(url);
        if (value) return value;
        var vibrant = new Vibrant(img),
            swatches = vibrant.swatches();
        return value = "", value += getSwatchString(swatches.DarkVibrant), appSettings.set(getSettingsKey(url), value), value
    }

    function getSwatchString(swatch) {
        return swatch ? swatch.getHex() + "|" + swatch.getBodyTextColor() + "|" + swatch.getTitleTextColor() : "||"
    }

    function fadeIn(elem) {
        elem.classList.add("lazy-image-fadein")
    }

    function lazyChildren(elem) {
        lazyLoader.lazyChildren(elem, fillImage)
    }

    function getPrimaryImageAspectRatio(items) {
        for (var values = [], i = 0, length = items.length; i < length; i++) {
            var ratio = items[i].PrimaryImageAspectRatio || 0;
            ratio && (values[values.length] = ratio)
        }
        if (!values.length) return null;
        values.sort(function(a, b) {
            return a - b
        });
        var result, half = Math.floor(values.length / 2);
        result = values.length % 2 ? values[half] : (values[half - 1] + values[half]) / 2;
        if (Math.abs(2 / 3 - result) <= .15) return 2 / 3;
        if (Math.abs(16 / 9 - result) <= .2) return 16 / 9;
        if (Math.abs(1 - result) <= .15) return 1;
        return Math.abs(4 / 3 - result) <= .15 ? 4 / 3 : result
    }

    function fillImages(elems) {
        for (var i = 0, length = elems.length; i < length; i++) {
            fillImage(elems[0])
        }
    }
    var self = (window.requestIdleCallback, {}),
        enableFade = !1;
    return self.fillImages = fillImages, self.lazyImage = fillImage, self.lazyChildren = lazyChildren, self.getPrimaryImageAspectRatio = getPrimaryImageAspectRatio, self.getCachedVibrantInfo = getCachedVibrantInfo, self.getVibrantInfoFromElement = getVibrantInfoFromElement, self
});