define(['visibleinviewport', 'imageFetcher', 'layoutManager', 'events', 'browser', 'dom'], function (visibleinviewport, imageFetcher, layoutManager, events, browser, dom) {

    var thresholdX;
    var thresholdY;

    var supportsIntersectionObserver = function () {

        if (window.IntersectionObserver) {

            // The api exists in chrome 50 but doesn't work
            if (browser.chrome) {

                var version = parseInt(browser.version.split('.')[0]);
                return version >= 51;
            }
            return true;
        }

        return false;
    }();

    function resetThresholds() {

        var x = screen.availWidth;
        var y = screen.availHeight;

        if (browser.touch) {
            x *= 2;
            y *= 2;
        }

        thresholdX = x;
        thresholdY = y;
    }

    if (!supportsIntersectionObserver) {
        dom.addEventListener(window, "orientationchange", resetThresholds, { passive: true });
        dom.addEventListener(window, 'resize', resetThresholds, { passive: true });
        resetThresholds();
    }

    function isVisible(elem) {
        return visibleinviewport(elem, true, thresholdX, thresholdY);
    }

    var wheelEvent = (document.implementation.hasFeature('Event.wheel', '3.0') ? 'wheel' : 'mousewheel');
    var self = {};

    var enableFade = browser.animate && !browser.slow;

    function fillImage(elem, source, enableEffects) {

        if (!source) {
            source = elem.getAttribute('data-src');
        }
        if (source) {
            if (enableFade && !layoutManager.tv && enableEffects !== false) {
                imageFetcher.loadImage(elem, source).then(fadeIn);
            } else {
                imageFetcher.loadImage(elem, source);
            }
            elem.removeAttribute("data-src");
        }
    }

    function fadeIn(elem) {

        var duration = layoutManager.tv ? 160 : 300;

        var keyframes = [
          { opacity: '0', offset: 0 },
          { opacity: '1', offset: 1 }];
        var timing = { duration: duration, iterations: 1 };
        elem.animate(keyframes, timing);
    }

    function cancelAll(tokens) {
        for (var i = 0, length = tokens.length; i < length; i++) {

            tokens[i] = true;
        }
    }

    function unveilWithIntersection(images, root) {

        var filledCount = 0;

        var options = {};

        //options.rootMargin = "300%";

        var observer = new IntersectionObserver(function (entries) {
            for (var j = 0, length2 = entries.length; j < length2; j++) {
                var entry = entries[j];
                var target = entry.target;
                observer.unobserve(target);
                fillImage(target);
                filledCount++;
            }
        },
        options
        );
        // Start observing an element
        for (var i = 0, length = images.length; i < length; i++) {
            observer.observe(images[i]);
        }
    }

    function unveilElements(images, root) {

        if (!images.length) {
            return;
        }

        if (supportsIntersectionObserver) {
            unveilWithIntersection(images, root);
            return;
        }

        var filledImages = [];
        var cancellationTokens = [];

        function unveilInternal(tokenIndex) {

            var anyFound = false;
            var out = false;

            // TODO: This out construct assumes left to right, top to bottom

            for (var i = 0, length = images.length; i < length; i++) {

                if (cancellationTokens[tokenIndex]) {
                    return;
                }
                if (filledImages[i]) {
                    continue;
                }
                var img = images[i];
                if (!out && isVisible(img)) {
                    anyFound = true;
                    filledImages[i] = true;
                    fillImage(img);
                } else {

                    if (anyFound) {
                        out = true;
                    }
                }
            }

            if (!images.length) {
                dom.removeEventListener(document, 'focus', unveil, {
                    capture: true,
                    passive: true
                });
                dom.removeEventListener(document, 'scroll', unveil, {
                    capture: true,
                    passive: true
                });
                dom.removeEventListener(document, wheelEvent, unveil, {
                    capture: true,
                    passive: true
                });
                dom.removeEventListener(window, 'resize', unveil, {
                    capture: true,
                    passive: true
                });
            }
        }

        function unveil() {

            cancelAll(cancellationTokens);

            var index = cancellationTokens.length;
            cancellationTokens.length++;

            setTimeout(function () {
                unveilInternal(index);
            }, 1);
        }

        dom.addEventListener(document, 'focus', unveil, {
            capture: true,
            passive: true
        });
        dom.addEventListener(document, 'scroll', unveil, {
            capture: true,
            passive: true
        });
        dom.addEventListener(document, wheelEvent, unveil, {
            capture: true,
            passive: true
        });
        dom.addEventListener(window, 'resize', unveil, {
            capture: true,
            passive: true
        });

        unveil();
    }

    function lazyChildren(elem) {

        unveilElements(elem.getElementsByClassName('lazy'), elem);
    }

    function getPrimaryImageAspectRatio(items) {

        var values = [];

        for (var i = 0, length = items.length; i < length; i++) {

            var ratio = items[i].PrimaryImageAspectRatio || 0;

            if (!ratio) {
                continue;
            }

            values[values.length] = ratio;
        }

        if (!values.length) {
            return null;
        }

        // Use the median
        values.sort(function (a, b) { return a - b; });

        var half = Math.floor(values.length / 2);

        var result;

        if (values.length % 2)
            result = values[half];
        else
            result = (values[half - 1] + values[half]) / 2.0;

        // If really close to 2:3 (poster image), just return 2:3
        var aspect2x3 = 2 / 3;
        if (Math.abs(aspect2x3 - result) <= .15) {
            return aspect2x3;
        }

        // If really close to 16:9 (episode image), just return 16:9
        var aspect16x9 = 16 / 9;
        if (Math.abs(aspect16x9 - result) <= .2) {
            return aspect16x9;
        }

        // If really close to 1 (square image), just return 1
        if (Math.abs(1 - result) <= .15) {
            return 1;
        }

        // If really close to 4:3 (poster image), just return 2:3
        var aspect4x3 = 4 / 3;
        if (Math.abs(aspect4x3 - result) <= .15) {
            return aspect4x3;
        }

        return result;
    }

    function fillImages(elems) {

        for (var i = 0, length = elems.length; i < length; i++) {
            var elem = elems[0];
            fillImage(elem);
        }
    }

    self.fillImages = fillImages;
    self.lazyImage = fillImage;
    self.lazyChildren = lazyChildren;
    self.getPrimaryImageAspectRatio = getPrimaryImageAspectRatio;

    return self;
});