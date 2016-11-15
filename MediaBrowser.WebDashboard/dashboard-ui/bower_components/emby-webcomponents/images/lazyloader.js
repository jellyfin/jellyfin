define(['visibleinviewport', 'browser', 'dom'], function (visibleinviewport, browser, dom) {
    'use strict';

    var thresholdX;
    var thresholdY;

    var requestIdleCallback = window.requestIdleCallback || function (fn) {
        fn();
    };

    var supportsIntersectionObserver = function () {

        if (window.IntersectionObserver) {

            return true;
        }

        return false;
    }();

    function resetThresholds() {

        var x = screen.availWidth;
        var y = screen.availHeight;

        if (browser.touch) {
            x *= 1.5;
            y *= 1.5;
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

    function cancelAll(tokens) {
        for (var i = 0, length = tokens.length; i < length; i++) {

            tokens[i] = true;
        }
    }

    function unveilWithIntersection(elements, root, callback) {

        var filledCount = 0;

        var options = {};

        //options.rootMargin = "300%";

        var observer = new IntersectionObserver(function (entries) {
            for (var j = 0, length2 = entries.length; j < length2; j++) {
                var entry = entries[j];
                var target = entry.target;
                observer.unobserve(target);
                callback(target);
                filledCount++;
            }
        },
        options
        );
        // Start observing an element
        for (var i = 0, length = elements.length; i < length; i++) {
            observer.observe(elements[i]);
        }
    }

    function unveilElements(elements, root, callback) {

        if (!elements.length) {
            return;
        }

        if (supportsIntersectionObserver) {
            unveilWithIntersection(elements, root, callback);
            return;
        }

        var unveiledElements = [];
        var cancellationTokens = [];

        function unveilInternal(tokenIndex) {

            var anyFound = false;
            var out = false;

            // TODO: This out construct assumes left to right, top to bottom

            for (var i = 0, length = elements.length; i < length; i++) {

                if (cancellationTokens[tokenIndex]) {
                    return;
                }
                if (unveiledElements[i]) {
                    continue;
                }
                var elem = elements[i];
                if (!out && isVisible(elem)) {
                    anyFound = true;
                    unveiledElements[i] = true;
                    callback(elem);
                } else {

                    if (anyFound) {
                        out = true;
                    }
                }
            }

            if (!elements.length) {
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

    function lazyChildren(elem, callback) {

        unveilElements(elem.getElementsByClassName('lazy'), elem, callback);
    }

    self.lazyChildren = lazyChildren;

    return self;
});