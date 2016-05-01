define(['events'], function (events) {

    function thresholdMonitor(elem, horizontal, lowerTolerance, upperTolerance) {

        var defaultTolerance = horizontal ? (screen.availWidth / 3) : (screen.availHeight / 3);
        lowerTolerance = lowerTolerance || defaultTolerance;
        upperTolerance = upperTolerance || defaultTolerance;

        var self = this;
        var upperTriggered = true;
        var lowerTriggered = false;
        var isWindow = elem == window || elem.tagName == 'HTML' || elem.tagName == 'BODY';

        var scrollSize;

        function getScrollSize() {

            if (!scrollSize) {

                if (isWindow) {
                    scrollSize = horizontal ? (document.documentElement.scrollWidth - document.documentElement.offsetWidth) : (document.documentElement.scrollHeight - document.documentElement.offsetHeight);
                } else {
                    scrollSize = horizontal ? (elem.scrollWidth - elem.offsetWidth) : (elem.scrollHeight - elem.offsetHeight);
                }
            }
            return scrollSize;
        }

        function onScroll(e) {

            if (lowerTriggered && upperTriggered) {
                return;
            }

            var position;

            if (isWindow) {
                position = horizontal ? window.pageXOffset : window.pageYOffset;
            } else {
                position = horizontal ? elem.scrollLeft : elem.scrollTop;
            }

            //console.log('onscroll: ' + position + '-' + getScrollSize());

            // Detect upper threshold
            if (position < upperTolerance) {
                if (!upperTriggered) {
                    upperTriggered = true;
                    events.trigger(self, 'upper-threshold');
                }
            } else {
                upperTriggered = false;
            }

            // Detect lower threshold
            if (position >= (getScrollSize() - lowerTolerance)) {
                if (!lowerTriggered) {
                    lowerTriggered = true;
                    events.trigger(self, 'lower-threshold');
                }
            } else {
                lowerTriggered = false;
            }
        }

        self.reset = function () {
            self.resetSize();
            upperTriggered = true;
            lowerTriggered = false;
        };

        self.resetSize = function () {
            scrollSize = null;
        };

        self.enabled = function (enabled) {

            self.reset();

            if (enabled) {
                elem.addEventListener('scroll', onScroll, true);
            } else {
                elem.removeEventListener('scroll', onScroll, true);
            }
        };

        self.enabled(true);

        self.destroy = function () {
            self.enabled(false);
        };
    }

    return thresholdMonitor;
});