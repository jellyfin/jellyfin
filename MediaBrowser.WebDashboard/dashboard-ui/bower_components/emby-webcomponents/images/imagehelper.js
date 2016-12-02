define(['lazyLoader', 'imageFetcher', 'layoutManager', 'browser', 'appSettings', 'require'], function (lazyLoader, imageFetcher, layoutManager, browser, appSettings, require) {
    'use strict';

    var requestIdleCallback = window.requestIdleCallback || function (fn) {
        fn();
    };

    //var imagesWorker = new Worker(require.toUrl('.').split('?')[0] + '/imagesworker.js');

    var self = {};

    var enableFade = browser.animate && !browser.slow;

    function fillImage(elem, source, enableEffects) {

        if (!source) {
            source = elem.getAttribute('data-src');
        }

        if (!source) {
            return;
        }

        fillImageElement(elem, source, enableEffects);
    }

    function fillImageElement(elem, source, enableEffects) {
        imageFetcher.loadImage(elem, source).then(function () {

            var fillingVibrant = fillVibrant(elem, source);

            if (enableFade && !layoutManager.tv && enableEffects !== false && !fillingVibrant) {
                fadeIn(elem);
            }

            elem.removeAttribute("data-src");
        });
    }

    //var placeholder = document.createElement('div');
    //imagesWorker.onmessage = function (evt) {
    //    placeholder.dispatchEvent(new CustomEvent('decoded', {
    //        detail: evt.data,
    //        bubbles: false,
    //        cancellable: false
    //    }));
    //};

    //var uniqueId = 0;

    //function fillCanvas(elem, source) {

    //    var newUniqueId = (++uniqueId);

    //    imagesWorker.postMessage({
    //        url: source,
    //        id: newUniqueId
    //    });

    //    placeholder.addEventListener('decoded', function (e) {

    //        if (e.detail.id == newUniqueId) {

    //            var imageBitmap = e.detail.imageBitmap;
    //            var canvas = document.createElement('canvas');
    //            var canvasContext = canvas.getContext('2d');

    //            //drawWidth *= ratio;
    //            //drawHeight *= ratio;

    //            // https://stackoverflow.com/questions/21961839/simulation-background-size-cover-in-canvas/21961894#21961894
    //            canvasContext.imageSmoothingEnabled = false;
    //            var width = canvas.width = elem.offsetWidth;
    //            var height = canvas.height = elem.offsetHeight;
    //            canvasContext.drawImage(imageBitmap, 0, 0, imageBitmap.width, imageBitmap.height, 0, 0, width, height);

    //            fillVibrant(elem, source, canvas, canvasContext);

    //            elem.insertBefore(canvas, elem.firstChild);
    //            elem.removeAttribute("data-src");
    //        }
    //    });
    //}

    function fillVibrant(img, url, canvas, canvasContext) {

        var vibrantElement = img.getAttribute('data-vibrant');
        if (!vibrantElement) {
            return false;
        }

        if (window.Vibrant) {
            fillVibrantOnLoaded(img, url, vibrantElement, canvas, canvasContext);
            return true;
        }

        require(['vibrant'], function () {
            fillVibrantOnLoaded(img, url, vibrantElement, canvas, canvasContext);
        });
        return true;
    }

    function fillVibrantOnLoaded(img, url, vibrantElement) {

        vibrantElement = document.getElementById(vibrantElement);
        if (!vibrantElement) {
            return;
        }

        requestIdleCallback(function () {

            //var now = new Date().getTime();
            getVibrantInfoFromElement(img, url).then(function (vibrantInfo) {

                var swatch = vibrantInfo.split('|');
                //console.log('vibrant took ' + (new Date().getTime() - now) + 'ms');
                if (swatch.length) {

                    var index = 0;
                    vibrantElement.style.backgroundColor = swatch[index];
                    vibrantElement.style.color = swatch[index + 1];
                }
            });
        });
        /*
         * Results into:
         * Vibrant #7a4426
         * Muted #7b9eae
         * DarkVibrant #348945
         * DarkMuted #141414
         * LightVibrant #f3ccb4
         */
    }

    function getVibrantInfoFromElement(elem, url) {

        return new Promise(function (resolve, reject) {

            require(['vibrant'], function () {

                if (elem.tagName === 'IMG') {
                    resolve(getVibrantInfo(elem, url));
                    return;
                }

                var img = new Image();
                img.onload = function () {
                    resolve(getVibrantInfo(img, url));
                };
                img.src = url;
            });
        });
    }

    function getSettingsKey(url) {

        var parts = url.split('://');
        url = parts[parts.length - 1];

        url = url.substring(url.indexOf('/') + 1);

        url = url.split('?')[0];

        var cacheKey = 'vibrant31';
        //cacheKey = 'vibrant' + new Date().getTime();
        return cacheKey + url;
    }

    function getCachedVibrantInfo(url) {

        return appSettings.get(getSettingsKey(url));
    }

    function getVibrantInfo(img, url) {

        var value = getCachedVibrantInfo(url);
        if (value) {
            return value;
        }

        var vibrant = new Vibrant(img);
        var swatches = vibrant.swatches();

        value = '';
        var swatch = swatches.DarkVibrant;
        value += getSwatchString(swatch);

        appSettings.set(getSettingsKey(url), value);

        return value;
    }

    function getSwatchString(swatch) {

        if (swatch) {
            return swatch.getHex() + '|' + swatch.getBodyTextColor() + '|' + swatch.getTitleTextColor();
        }
        return '||';
    }

    function fadeIn(elem) {

        var duration = layoutManager.tv ? 160 : 300;

        var keyframes = [
          { opacity: '0', offset: 0 },
          { opacity: '1', offset: 1 }];
        var timing = { duration: duration, iterations: 1 };
        elem.animate(keyframes, timing);
    }

    function lazyChildren(elem) {

        lazyLoader.lazyChildren(elem, fillImage);
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

        if (values.length % 2) {
            result = values[half];
        }
        else {
            result = (values[half - 1] + values[half]) / 2.0;
        }

        // If really close to 2:3 (poster image), just return 2:3
        var aspect2x3 = 2 / 3;
        if (Math.abs(aspect2x3 - result) <= 0.15) {
            return aspect2x3;
        }

        // If really close to 16:9 (episode image), just return 16:9
        var aspect16x9 = 16 / 9;
        if (Math.abs(aspect16x9 - result) <= 0.2) {
            return aspect16x9;
        }

        // If really close to 1 (square image), just return 1
        if (Math.abs(1 - result) <= 0.15) {
            return 1;
        }

        // If really close to 4:3 (poster image), just return 2:3
        var aspect4x3 = 4 / 3;
        if (Math.abs(aspect4x3 - result) <= 0.15) {
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
    self.getCachedVibrantInfo = getCachedVibrantInfo;
    self.getVibrantInfoFromElement = getVibrantInfoFromElement;

    return self;
});