define(['events'], function (events) {

    function thresholdMonitor(elem, horizontal, lowerTolerance, upperTolerance) {

        var defaultTolerance = horizontal ? (screen.availWidth / 2) : (screen.availHeight / 2);
        lowerTolerance = lowerTolerance || defaultTolerance;
        upperTolerance = upperTolerance || defaultTolerance;

        var self = this;
        var upperTriggered = false;
        var lowerTriggered = false;

        var scrollSize;

        function getScrollSize() {

            if (!scrollSize) {
                scrollSize = horizontal ? (elem.scrollWidth - elem.clientWidth) : (elem.scrollHeight - elem.offsetHeight);
            }
            return scrollSize;
        }

        function onScroll(e) {

            if (lowerTriggered && upperTriggered) {
                return;
            }

            var position = horizontal ? elem.scrollLeft : elem.scrollTop;

            //console.log('onscroll: ' + position + '-' + getScrollSize());

            // Detect upper threshold
            if (!upperTriggered && position < upperTolerance) {
                upperTriggered = true;
                events.trigger(self, 'upper-threshold');
            }
            // Detect lower threshold
            if (!lowerTriggered && position >= (getScrollSize() - lowerTolerance)) {
                lowerTriggered = false;
                events.trigger(self, 'lower-threshold');
            }
        }

        self.reset = function () {
            self.resetSize();
            upperTriggered = false;
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