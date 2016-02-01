/*=========================
  Keyboard Control
  ===========================*/
function handleKeyboard(e) {
    if (e.originalEvent) e = e.originalEvent; //jquery fix
    var kc = e.keyCode || e.charCode;
    // Directions locks
    if (!s.params.allowSwipeToNext && (s.isHorizontal() && kc === 39 || !s.isHorizontal() && kc === 40)) {
        return false;
    }
    if (!s.params.allowSwipeToPrev && (s.isHorizontal() && kc === 37 || !s.isHorizontal() && kc === 38)) {
        return false;
    }
    if (e.shiftKey || e.altKey || e.ctrlKey || e.metaKey) {
        return;
    }
    if (document.activeElement && document.activeElement.nodeName && (document.activeElement.nodeName.toLowerCase() === 'input' || document.activeElement.nodeName.toLowerCase() === 'textarea')) {
        return;
    }
    if (kc === 37 || kc === 39 || kc === 38 || kc === 40) {
        var inView = false;
        //Check that swiper should be inside of visible area of window
        if (s.container.parents('.swiper-slide').length > 0 && s.container.parents('.swiper-slide-active').length === 0) {
            return;
        }
        var windowScroll = {
            left: window.pageXOffset,
            top: window.pageYOffset
        };
        var windowWidth = window.innerWidth;
        var windowHeight = window.innerHeight;
        var swiperOffset = s.container.offset();
        if (s.rtl) swiperOffset.left = swiperOffset.left - s.container[0].scrollLeft;
        var swiperCoord = [
            [swiperOffset.left, swiperOffset.top],
            [swiperOffset.left + s.width, swiperOffset.top],
            [swiperOffset.left, swiperOffset.top + s.height],
            [swiperOffset.left + s.width, swiperOffset.top + s.height]
        ];
        for (var i = 0; i < swiperCoord.length; i++) {
            var point = swiperCoord[i];
            if (
                point[0] >= windowScroll.left && point[0] <= windowScroll.left + windowWidth &&
                point[1] >= windowScroll.top && point[1] <= windowScroll.top + windowHeight
            ) {
                inView = true;
            }

        }
        if (!inView) return;
    }
    if (s.isHorizontal()) {
        if (kc === 37 || kc === 39) {
            if (e.preventDefault) e.preventDefault();
            else e.returnValue = false;
        }
        if ((kc === 39 && !s.rtl) || (kc === 37 && s.rtl)) s.slideNext();
        if ((kc === 37 && !s.rtl) || (kc === 39 && s.rtl)) s.slidePrev();
    }
    else {
        if (kc === 38 || kc === 40) {
            if (e.preventDefault) e.preventDefault();
            else e.returnValue = false;
        }
        if (kc === 40) s.slideNext();
        if (kc === 38) s.slidePrev();
    }
}
s.disableKeyboardControl = function () {
    s.params.keyboardControl = false;
    $(document).off('keydown', handleKeyboard);
};
s.enableKeyboardControl = function () {
    s.params.keyboardControl = true;
    $(document).on('keydown', handleKeyboard);
};
