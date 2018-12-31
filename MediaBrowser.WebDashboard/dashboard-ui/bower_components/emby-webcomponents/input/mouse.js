define(['inputManager', 'focusManager', 'browser', 'layoutManager', 'events', 'dom'], function (inputmanager, focusManager, browser, layoutManager, events, dom) {
    'use strict';

    var self = {};

    var lastMouseInputTime = new Date().getTime();
    var isMouseIdle;

    function mouseIdleTime() {
        return new Date().getTime() - lastMouseInputTime;
    }

    function notifyApp() {

        inputmanager.notifyMouseMove();
    }

    function removeIdleClasses() {

        var classList = document.body.classList;

        classList.remove('mouseIdle');
        classList.remove('mouseIdle-tv');
    }

    function addIdleClasses() {

        var classList = document.body.classList;

        classList.add('mouseIdle');

        if (layoutManager.tv) {
            classList.add('mouseIdle-tv');
        }
    }

    var lastPointerMoveData;
    function onPointerMove(e) {

        var eventX = e.screenX;
        var eventY = e.screenY;

        // if coord don't exist how could it move
        if (typeof eventX === "undefined" && typeof eventY === "undefined") {
            return;
        }

        var obj = lastPointerMoveData;
        if (!obj) {
            lastPointerMoveData = {
                x: eventX,
                y: eventY
            };
            return;
        }

        // if coord are same, it didn't move
        if (Math.abs(eventX - obj.x) < 10 && Math.abs(eventY - obj.y) < 10) {
            return;
        }

        obj.x = eventX;
        obj.y = eventY;

        lastMouseInputTime = new Date().getTime();
        notifyApp();

        if (isMouseIdle) {
            isMouseIdle = false;
            removeIdleClasses();
            events.trigger(self, 'mouseactive');
        }
    }

    function onPointerEnter(e) {

        var pointerType = e.pointerType || (layoutManager.mobile ? 'touch' : 'mouse');

        if (pointerType === 'mouse') {
            if (!isMouseIdle) {
                var parent = focusManager.focusableParent(e.target);
                if (parent) {
                    focusManager.focus(parent);
                }
            }
        }
    }

    function enableFocusWithMouse() {

        if (!layoutManager.tv) {
            return false;
        }

        if (browser.web0s) {
            return false;
        }

        if (browser.tv) {
            return true;
        }

        return false;
    }

    function onMouseInterval() {

        if (!isMouseIdle && mouseIdleTime() >= 5000) {
            isMouseIdle = true;
            addIdleClasses();
            events.trigger(self, 'mouseidle');
        }
    }

    var mouseInterval;
    function startMouseInterval() {

        if (!mouseInterval) {
            mouseInterval = setInterval(onMouseInterval, 5000);
        }
    }

    function stopMouseInterval() {

        var interval = mouseInterval;

        if (interval) {
            clearInterval(interval);
            mouseInterval = null;
        }

        removeIdleClasses();
    }

    function initMouse() {

        stopMouseInterval();

        dom.removeEventListener(document, (window.PointerEvent ? 'pointermove' : 'mousemove'), onPointerMove, {
            passive: true
        });

        if (!layoutManager.mobile) {
            startMouseInterval();

            dom.addEventListener(document, (window.PointerEvent ? 'pointermove' : 'mousemove'), onPointerMove, {
                passive: true
            });
        }

        dom.removeEventListener(document, (window.PointerEvent ? 'pointerenter' : 'mouseenter'), onPointerEnter, {
            capture: true,
            passive: true
        });

        if (enableFocusWithMouse()) {
            dom.addEventListener(document, (window.PointerEvent ? 'pointerenter' : 'mouseenter'), onPointerEnter, {
                capture: true,
                passive: true
            });
        }
    }

    initMouse();

    events.on(layoutManager, 'modechange', initMouse);

    return self;
});