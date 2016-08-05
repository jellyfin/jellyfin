define(['browser', 'css!./emby-slider', 'registerElement', 'emby-input'], function (browser) {

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

                backgroundLower.style.flex = fraction;
                backgroundUpper.style.flex = 1 - fraction;
            }
        });
    }

    function updateBubble(range, bubble) {

        var value = range.value;
        bubble.style.left = (value - 1) + '%';

        if (range.getBubbleText) {
            value = range.getBubbleText(value);
        }
        bubble.innerHTML = value;
    }

    EmbySliderPrototype.attachedCallback = function () {

        if (this.getAttribute('data-embycheckbox') == 'true') {
            return;
        }

        this.setAttribute('data-embycheckbox', 'true');

        this.classList.add('mdl-slider');
        this.classList.add('mdl-js-slider');

        var containerElement = this.parentNode;
        containerElement.classList.add('mdl-slider__container');

        var htmlToInsert = '';

        if (!supportsNativeProgressStyle) {
            htmlToInsert += '<div class="mdl-slider__background-flex"><div class="mdl-slider__background-lower"></div><div class="mdl-slider__background-upper"></div></div>';
        }

        htmlToInsert += '<div class="sliderBubble hide"></div>';

        containerElement.insertAdjacentHTML('beforeend', htmlToInsert);

        var backgroundLower = containerElement.querySelector('.mdl-slider__background-lower');
        var backgroundUpper = containerElement.querySelector('.mdl-slider__background-upper');
        var sliderBubble = containerElement.querySelector('.sliderBubble');

        var hasHideClass = sliderBubble.classList.contains('hide');

        this.addEventListener('input', function (e) {
            this.dragging = true;
            updateBubble(this, sliderBubble);

            if (hasHideClass) {
                sliderBubble.classList.remove('hide');
                hasHideClass = false;
            }
        });
        this.addEventListener('change', function () {
            this.dragging = false;
            updateValues(this, backgroundLower, backgroundUpper);
            sliderBubble.classList.add('hide');
            hasHideClass = true;
        });

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