define(['require', 'browser'], function (require, browser) {
    'use strict';

    function LazyLoader(options) {

        this.options = options;
    }

    if (browser.edge) {
        require(['css!./lazyedgehack']);
    }

    LazyLoader.prototype.createObserver = function () {

        var observerOptions = {};
        var options = this.options;
        var loadedCount = 0;
        var callback = options.callback;

        observerOptions.rootMargin = "50%";

        var observerId = 'obs' + new Date().getTime();

        var self = this;
        var observer = new IntersectionObserver(function (entries) {
            for (var j = 0, length2 = entries.length; j < length2; j++) {
                var entry = entries[j];

                if (entry.intersectionRatio > 0) {

                    // Stop watching and load the image
                    var target = entry.target;

                    observer.unobserve(target);

                    if (!target[observerId]) {
                        target[observerId] = 1;
                        callback(target);
                        loadedCount++;

                        if (loadedCount >= self.elementCount) {
                            self.destroyObserver();
                        }
                    }
                }
            }
        },
            observerOptions
        );

        this.observer = observer;
    };

    LazyLoader.prototype.addElements = function (elements) {

        var observer = this.observer;

        if (!observer) {
            this.createObserver();
            observer = this.observer;
        }

        this.elementCount = (this.elementCount || 0) + elements.length;

        for (var i = 0, length = elements.length; i < length; i++) {
            observer.observe(elements[i]);
        }
    };

    LazyLoader.prototype.destroyObserver = function (elements) {

        var observer = this.observer;

        if (observer) {
            observer.disconnect();
            this.observer = null;
        }
    };

    LazyLoader.prototype.destroy = function (elements) {

        this.destroyObserver();
        this.options = null;
    };

    function unveilElements(elements, root, callback) {

        if (!elements.length) {
            return;
        }
        var lazyLoader = new LazyLoader({
            callback: callback
        });
        lazyLoader.addElements(elements);
    }

    LazyLoader.lazyChildren = function (elem, callback) {

        unveilElements(elem.getElementsByClassName('lazy'), elem, callback);
    };

    return LazyLoader;
});