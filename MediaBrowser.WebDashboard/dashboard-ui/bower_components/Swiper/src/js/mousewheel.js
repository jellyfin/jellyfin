/*=========================
  Mousewheel Control
  ===========================*/
s.mousewheel = {
    event: false,
    lastScrollTime: (new window.Date()).getTime()
};
if (s.params.mousewheelControl) {
    try {
        new window.WheelEvent('wheel');
        s.mousewheel.event = 'wheel';
    } catch (e) {
        if (window.WheelEvent || (s.container[0] && 'wheel' in s.container[0])) {
            s.mousewheel.event = 'wheel';
        }
    }
    if (!s.mousewheel.event && window.WheelEvent) {

    }
    if (!s.mousewheel.event && document.onmousewheel !== undefined) {
        s.mousewheel.event = 'mousewheel';
    }
    if (!s.mousewheel.event) {
        s.mousewheel.event = 'DOMMouseScroll';
    }
}
function handleMousewheel(e) {
    if (e.originalEvent) e = e.originalEvent; //jquery fix
    var we = s.mousewheel.event;
    var delta = 0;
    var rtlFactor = s.rtl ? -1 : 1;

    //WebKits
    if (we === 'mousewheel') {
        if (s.params.mousewheelForceToAxis) {
            if (s.isHorizontal()) {
                if (Math.abs(e.wheelDeltaX) > Math.abs(e.wheelDeltaY)) delta = e.wheelDeltaX * rtlFactor;
                else return;
            }
            else {
                if (Math.abs(e.wheelDeltaY) > Math.abs(e.wheelDeltaX)) delta = e.wheelDeltaY;
                else return;
            }
        }
        else {
            delta = Math.abs(e.wheelDeltaX) > Math.abs(e.wheelDeltaY) ? - e.wheelDeltaX * rtlFactor : - e.wheelDeltaY;
        }
    }
    //Old FireFox
    else if (we === 'DOMMouseScroll') delta = -e.detail;
    //New FireFox
    else if (we === 'wheel') {
        if (s.params.mousewheelForceToAxis) {
            if (s.isHorizontal()) {
                if (Math.abs(e.deltaX) > Math.abs(e.deltaY)) delta = -e.deltaX * rtlFactor;
                else return;
            }
            else {
                if (Math.abs(e.deltaY) > Math.abs(e.deltaX)) delta = -e.deltaY;
                else return;
            }
        }
        else {
            delta = Math.abs(e.deltaX) > Math.abs(e.deltaY) ? - e.deltaX * rtlFactor : - e.deltaY;
        }
    }
    if (delta === 0) return;

    if (s.params.mousewheelInvert) delta = -delta;

    if (!s.params.freeMode) {
        if ((new window.Date()).getTime() - s.mousewheel.lastScrollTime > 60) {
            if (delta < 0) {
                if ((!s.isEnd || s.params.loop) && !s.animating) s.slideNext();
                else if (s.params.mousewheelReleaseOnEdges) return true;
            }
            else {
                if ((!s.isBeginning || s.params.loop) && !s.animating) s.slidePrev();
                else if (s.params.mousewheelReleaseOnEdges) return true;
            }
        }
        s.mousewheel.lastScrollTime = (new window.Date()).getTime();

    }
    else {
        //Freemode or scrollContainer:
        var position = s.getWrapperTranslate() + delta * s.params.mousewheelSensitivity;
        var wasBeginning = s.isBeginning,
            wasEnd = s.isEnd;

        if (position >= s.minTranslate()) position = s.minTranslate();
        if (position <= s.maxTranslate()) position = s.maxTranslate();

        s.setWrapperTransition(0);
        s.setWrapperTranslate(position);
        s.updateProgress();
        s.updateActiveIndex();

        if (!wasBeginning && s.isBeginning || !wasEnd && s.isEnd) {
            s.updateClasses();
        }

        if (s.params.freeModeSticky) {
            clearTimeout(s.mousewheel.timeout);
            s.mousewheel.timeout = setTimeout(function () {
                s.slideReset();
            }, 300);
        }
        else {
            if (s.params.lazyLoading && s.lazy) {
                s.lazy.load();
            }
        }

        // Return page scroll on edge positions
        if (position === 0 || position === s.maxTranslate()) return;
    }
    if (s.params.autoplay) s.stopAutoplay();

    if (e.preventDefault) e.preventDefault();
    else e.returnValue = false;
    return false;
}
s.disableMousewheelControl = function () {
    if (!s.mousewheel.event) return false;
    s.container.off(s.mousewheel.event, handleMousewheel);
    return true;
};

s.enableMousewheelControl = function () {
    if (!s.mousewheel.event) return false;
    s.container.on(s.mousewheel.event, handleMousewheel);
    return true;
};
