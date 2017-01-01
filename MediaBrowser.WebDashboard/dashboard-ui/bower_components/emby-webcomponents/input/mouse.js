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

    var lastMouseMoveData;
    dom.addEventListener(document, 'mousemove', function (e) {

        var eventX = e.screenX;
        var eventY = e.screenY;

        // if coord don't exist how could it move
        if (typeof eventX === "undefined" && typeof eventY === "undefined") {
            return;
        }

        var obj = lastMouseMoveData;
        if (!obj) {
            lastMouseMoveData = {
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
            document.body.classList.remove('mouseIdle');
            events.trigger(self, 'mouseactive');
        }
    }, {
        passive: true
    });

    function onMouseEnter(e) {

        var parent = focusManager.focusableParent(e.target);
        if (parent) {
            focusManager.focus(e.target);
        }
    }

    function enableFocusWithMouse() {

        if (!layoutManager.tv) {
            return false;
        }

        if (browser.xboxOne) {
            return true;
        }

        if (browser.ps4) {
            return true;
        }

        if (browser.tv) {
            return true;
        }

        return false;
    }

    function initMouseFocus() {

        dom.removeEventListener(document, 'mouseenter', onMouseEnter, {
            capture: true,
            passive: true
        });

        if (enableFocusWithMouse()) {
            dom.addEventListener(document, 'mouseenter', onMouseEnter, {
                capture: true,
                passive: true
            });
        }
    }

    initMouseFocus();

    events.on(layoutManager, 'modechange', initMouseFocus);

    setInterval(function () {

        if (mouseIdleTime() >= 5000) {
            isMouseIdle = true;
            document.body.classList.add('mouseIdle');
            events.trigger(self, 'mouseidle');
        }

    }, 5000);

    return self;
});