define(['browser', 'dom', 'css!./emby-slider', 'registerElement', 'emby-input'], function (browser, dom) {
    'use strict';

    var EmbySliderPrototype = Object.create(HTMLInputElement.prototype);

    var supportsNativeProgressStyle = browser.firefox || browser.edge || browser.msie;
    var supportsValueSetOverride = false;

    if (Object.getOwnPropertyDescriptor && Object.defineProperty) {

        var descriptor = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value');
        // descriptor returning null in webos
        if (descriptor && descriptor.configurable) {
            supportsValueSetOverride = true;
        }
    }

    function updateValues(range, backgroundLower, backgroundUpper) {

        var value = range.value;
        requestAnimationFrame(function () {

            if (backgroundLower) {
                var fraction = (value - range.min) / (range.max - range.min);

                if (browser.noFlex) {
                    backgroundLower.style['-webkit-flex'] = fraction;
                    backgroundUpper.style['-webkit-flex'] = 1 - fraction;
                    backgroundLower.style['-webkit-box-flex'] = fraction;
                    backgroundUpper.style['-webkit-box-flex'] = 1 - fraction;
                }

                backgroundLower.style.flex = fraction;
                backgroundUpper.style.flex = 1 - fraction;
            }
        });
    }

    function updateBubble(range, value, bubble, bubbleText) {

        bubble.style.left = (value - 1) + '%';

        if (range.getBubbleText) {
            value = range.getBubbleText(value);
        } else {
            value = Math.round(value);
        }

        bubbleText.innerHTML = value;
    }

    EmbySliderPrototype.attachedCallback = function () {

        if (this.getAttribute('data-embycheckbox') === 'true') {
            return;
        }

        this.setAttribute('data-embycheckbox', 'true');

        this.classList.add('mdl-slider');
        this.classList.add('mdl-js-slider');

        if (browser.noFlex) {
            this.classList.add('slider-no-webkit-thumb');
        }

        var containerElement = this.parentNode;
        containerElement.classList.add('mdl-slider__container');

        var htmlToInsert = '';

        if (!supportsNativeProgressStyle) {
            htmlToInsert += '<div class="mdl-slider__background-flex"><div class="mdl-slider__background-lower"></div><div class="mdl-slider__background-upper"></div></div>';
        }

        htmlToInsert += '<div class="sliderBubble hide"><h1 class="sliderBubbleText"></h1></div>';

        containerElement.insertAdjacentHTML('beforeend', htmlToInsert);

        var backgroundLower = containerElement.querySelector('.mdl-slider__background-lower');
        var backgroundUpper = containerElement.querySelector('.mdl-slider__background-upper');
        var sliderBubble = containerElement.querySelector('.sliderBubble');
        var sliderBubbleText = containerElement.querySelector('.sliderBubbleText');

        var hasHideClass = sliderBubble.classList.contains('hide');

        dom.addEventListener(this, 'input', function (e) {
            this.dragging = true;

            updateBubble(this, this.value, sliderBubble, sliderBubbleText);

            if (hasHideClass) {
                sliderBubble.classList.remove('hide');
                hasHideClass = false;
            }
        }, {
            passive: true
        });

        dom.addEventListener(this, 'change', function () {
            this.dragging = false;
            updateValues(this, backgroundLower, backgroundUpper);

            sliderBubble.classList.add('hide');
            hasHideClass = true;

        }, {
            passive: true
        });

        // In firefox this feature disrupts the ability to move the slider
        if (!browser.firefox) {
            dom.addEventListener(this, 'mousemove', function (e) {

                if (!this.dragging) {
                    var rect = this.getBoundingClientRect();
                    var clientX = e.clientX;
                    var bubbleValue = (clientX - rect.left) / rect.width;
                    bubbleValue *= 100;
                    updateBubble(this, bubbleValue, sliderBubble, sliderBubbleText);

                    if (hasHideClass) {
                        sliderBubble.classList.remove('hide');
                        hasHideClass = false;
                    }
                }

            }, {
                passive: true
            });

            dom.addEventListener(this, 'mouseleave', function () {
                sliderBubble.classList.add('hide');
                hasHideClass = true;
            }, {
                passive: true
            });
        }

        if (!supportsNativeProgressStyle) {

            if (supportsValueSetOverride) {
                this.addEventListener('valueset', function () {
                    updateValues(this, backgroundLower, backgroundUpper);
                });
            } else {
                startInterval(this, backgroundLower, backgroundUpper);
            }
        }
    };

    function startInterval(range, backgroundLower, backgroundUpper) {
        var interval = range.interval;
        if (interval) {
            clearInterval(interval);
        }
        range.interval = setInterval(function () {
            updateValues(range, backgroundLower, backgroundUpper);
        }, 100);
    }

    EmbySliderPrototype.detachedCallback = function () {

        var interval = this.interval;
        if (interval) {
            clearInterval(interval);
        }
        this.interval = null;
    };

    document.registerElement('emby-slider', {
        prototype: EmbySliderPrototype,
        extends: 'input'
    });
});