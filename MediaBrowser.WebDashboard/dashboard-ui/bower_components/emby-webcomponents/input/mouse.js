define(["inputManager", "focusManager", "browser", "layoutManager", "events", "dom"], function(inputmanager, focusManager, browser, layoutManager, events, dom) {
    "use strict";

    function mouseIdleTime() {
        return (new Date).getTime() - lastMouseInputTime
    }

    function notifyApp() {
        inputmanager.notifyMouseMove()
    }

    function removeIdleClasses() {
        var classList = document.body.classList;
        classList.remove("mouseIdle"), classList.remove("mouseIdle-tv")
    }

    function addIdleClasses() {
        var classList = document.body.classList;
        classList.add("mouseIdle"), layoutManager.tv && classList.add("mouseIdle-tv")
    }

    function onPointerMove(e) {
        var eventX = e.screenX,
            eventY = e.screenY;
        if (void 0 !== eventX || void 0 !== eventY) {
            var obj = lastPointerMoveData;
            if (!obj) return void(lastPointerMoveData = {
                x: eventX,
                y: eventY
            });
            Math.abs(eventX - obj.x) < 10 && Math.abs(eventY - obj.y) < 10 || (obj.x = eventX, obj.y = eventY, lastMouseInputTime = (new Date).getTime(), notifyApp(), isMouseIdle && (isMouseIdle = !1, removeIdleClasses(), events.trigger(self, "mouseactive")))
        }
    }

    function onPointerEnter(e) {
        if ("mouse" === (e.pointerType || (layoutManager.mobile ? "touch" : "mouse")) && !isMouseIdle) {
            var parent = focusManager.focusableParent(e.target);
            parent && focusManager.focus(parent)
        }
    }

    function enableFocusWithMouse() {
        return !!layoutManager.tv && (!browser.web0s && !!browser.tv)
    }

    function onMouseInterval() {
        !isMouseIdle && mouseIdleTime() >= 5e3 && (isMouseIdle = !0, addIdleClasses(), events.trigger(self, "mouseidle"))
    }

    function startMouseInterval() {
        mouseInterval || (mouseInterval = setInterval(onMouseInterval, 5e3))
    }

    function stopMouseInterval() {
        var interval = mouseInterval;
        interval && (clearInterval(interval), mouseInterval = null), removeIdleClasses()
    }

    function initMouse() {
        stopMouseInterval(), dom.removeEventListener(document, window.PointerEvent ? "pointermove" : "mousemove", onPointerMove, {
            passive: !0
        }), layoutManager.mobile || (startMouseInterval(), dom.addEventListener(document, window.PointerEvent ? "pointermove" : "mousemove", onPointerMove, {
            passive: !0
        })), dom.removeEventListener(document, window.PointerEvent ? "pointerenter" : "mouseenter", onPointerEnter, {
            capture: !0,
            passive: !0
        }), enableFocusWithMouse() && dom.addEventListener(document, window.PointerEvent ? "pointerenter" : "mouseenter", onPointerEnter, {
            capture: !0,
            passive: !0
        })
    }
    var isMouseIdle, lastPointerMoveData, mouseInterval, self = {},
        lastMouseInputTime = (new Date).getTime();
    return initMouse(), events.on(layoutManager, "modechange", initMouse), self
});