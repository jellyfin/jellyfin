define(['dom', 'events'], function (dom, events) {
    'use strict';

    function getTouches(e) {

        return e.changedTouches || e.targetTouches || e.touches;
    }

    function TouchHelper(elem, options) {

        options = options || {};
        var touchTarget;
        var touchStartX;
        var touchStartY;
        var lastDeltaX;
        var lastDeltaY;
        var thresholdYMet;
        var self = this;

        var swipeXThreshold = options.swipeXThreshold || 50;
        var swipeYThreshold = options.swipeYThreshold || 50;
        var swipeXMaxY = 30;

        var excludeTagNames = options.ignoreTagNames || [];

        var touchStart = function (e) {

            var touch = getTouches(e)[0];
            touchTarget = null;
            touchStartX = 0;
            touchStartY = 0;
            lastDeltaX = null;
            lastDeltaY = null;
            thresholdYMet = false;

            if (touch) {

                var currentTouchTarget = touch.target;

                if (dom.parentWithTag(currentTouchTarget, excludeTagNames)) {
                    return;
                }

                touchTarget = currentTouchTarget;
                touchStartX = touch.clientX;
                touchStartY = touch.clientY;
            }
        };

        var touchEnd = function (e) {

            var isTouchMove = e.type === 'touchmove';

            if (touchTarget) {
                var touch = getTouches(e)[0];

                var deltaX;
                var deltaY;

                var clientX;
                var clientY;

                if (touch) {
                    clientX = touch.clientX || 0;
                    clientY = touch.clientY || 0;
                    deltaX = clientX - (touchStartX || 0);
                    deltaY = clientY - (touchStartY || 0);
                } else {
                    deltaX = 0;
                    deltaY = 0;
                }

                var currentDeltaX = lastDeltaX == null ? deltaX : (deltaX - lastDeltaX);
                var currentDeltaY = lastDeltaY == null ? deltaY : (deltaY - lastDeltaY);

                lastDeltaX = deltaX;
                lastDeltaY = deltaY;

                if (deltaX > swipeXThreshold && Math.abs(deltaY) < swipeXMaxY) {
                    events.trigger(self, 'swiperight', [touchTarget]);
                }
                else if (deltaX < (0 - swipeXThreshold) && Math.abs(deltaY) < swipeXMaxY) {
                    events.trigger(self, 'swipeleft', [touchTarget]);
                }
                else if ((deltaY < (0 - swipeYThreshold) || thresholdYMet) && Math.abs(deltaX) < swipeXMaxY) {

                    thresholdYMet = true;

                    events.trigger(self, 'swipeup', [touchTarget, {
                        deltaY: deltaY,
                        deltaX: deltaX,
                        clientX: clientX,
                        clientY: clientY,
                        currentDeltaX: currentDeltaX,
                        currentDeltaY: currentDeltaY
                    }]);
                }
                else if ((deltaY > swipeYThreshold || thresholdYMet) && Math.abs(deltaX) < swipeXMaxY) {
                    thresholdYMet = true;

                    events.trigger(self, 'swipedown', [touchTarget, {
                        deltaY: deltaY,
                        deltaX: deltaX,
                        clientX: clientX,
                        clientY: clientY,
                        currentDeltaX: currentDeltaX,
                        currentDeltaY: currentDeltaY
                    }]);
                }

                if (isTouchMove && options.preventDefaultOnMove) {
                    e.preventDefault();
                }
            }

            if (!isTouchMove) {
                touchTarget = null;
                touchStartX = 0;
                touchStartY = 0;
                lastDeltaX = null;
                lastDeltaY = null;
                thresholdYMet = false;
            }
        };

        this.touchStart = touchStart;
        this.touchEnd = touchEnd;

        dom.addEventListener(elem, 'touchstart', touchStart, {
            passive: true
        });
        if (options.triggerOnMove) {
            dom.addEventListener(elem, 'touchmove', touchEnd, {
                passive: !options.preventDefaultOnMove
            });
        }
        dom.addEventListener(elem, 'touchend', touchEnd, {
            passive: true
        });
        dom.addEventListener(elem, 'touchcancel', touchEnd, {
            passive: true
        });
    }

    TouchHelper.prototype.destroy = function () {

        var elem = this.elem;

        if (elem) {
            var touchStart = this.touchStart;
            var touchEnd = this.touchEnd;

            dom.removeEventListener(elem, 'touchstart', touchStart, {
                passive: true
            });
            dom.removeEventListener(elem, 'touchmove', touchEnd, {
                passive: true
            });
            dom.removeEventListener(elem, 'touchend', touchEnd, {
                passive: true
            });
            dom.removeEventListener(elem, 'touchcancel', touchEnd, {
                passive: true
            });
        }

        this.touchStart = null;
        this.touchEnd = null;

        this.elem = null;
    };

    return TouchHelper;
});