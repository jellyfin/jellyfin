/*=========================
  Scrollbar
  ===========================*/
s.scrollbar = {
    isTouched: false,
    setDragPosition: function (e) {
        var sb = s.scrollbar;
        var x = 0, y = 0;
        var translate;
        var pointerPosition = s.isHorizontal() ?
            ((e.type === 'touchstart' || e.type === 'touchmove') ? e.targetTouches[0].pageX : e.pageX || e.clientX) :
            ((e.type === 'touchstart' || e.type === 'touchmove') ? e.targetTouches[0].pageY : e.pageY || e.clientY) ;
        var position = (pointerPosition) - sb.track.offset()[s.isHorizontal() ? 'left' : 'top'] - sb.dragSize / 2;
        var positionMin = -s.minTranslate() * sb.moveDivider;
        var positionMax = -s.maxTranslate() * sb.moveDivider;
        if (position < positionMin) {
            position = positionMin;
        }
        else if (position > positionMax) {
            position = positionMax;
        }
        position = -position / sb.moveDivider;
        s.updateProgress(position);
        s.setWrapperTranslate(position, true);
    },
    dragStart: function (e) {
        var sb = s.scrollbar;
        sb.isTouched = true;
        e.preventDefault();
        e.stopPropagation();

        sb.setDragPosition(e);
        clearTimeout(sb.dragTimeout);

        sb.track.transition(0);
        if (s.params.scrollbarHide) {
            sb.track.css('opacity', 1);
        }
        s.wrapper.transition(100);
        sb.drag.transition(100);
        s.emit('onScrollbarDragStart', s);
    },
    dragMove: function (e) {
        var sb = s.scrollbar;
        if (!sb.isTouched) return;
        if (e.preventDefault) e.preventDefault();
        else e.returnValue = false;
        sb.setDragPosition(e);
        s.wrapper.transition(0);
        sb.track.transition(0);
        sb.drag.transition(0);
        s.emit('onScrollbarDragMove', s);
    },
    dragEnd: function (e) {
        var sb = s.scrollbar;
        if (!sb.isTouched) return;
        sb.isTouched = false;
        if (s.params.scrollbarHide) {
            clearTimeout(sb.dragTimeout);
            sb.dragTimeout = setTimeout(function () {
                sb.track.css('opacity', 0);
                sb.track.transition(400);
            }, 1000);

        }
        s.emit('onScrollbarDragEnd', s);
        if (s.params.scrollbarSnapOnRelease) {
            s.slideReset();
        }
    },
    enableDraggable: function () {
        var sb = s.scrollbar;
        var target = s.support.touch ? sb.track : document;
        $(sb.track).on(s.touchEvents.start, sb.dragStart);
        $(target).on(s.touchEvents.move, sb.dragMove);
        $(target).on(s.touchEvents.end, sb.dragEnd);
    },
    disableDraggable: function () {
        var sb = s.scrollbar;
        var target = s.support.touch ? sb.track : document;
        $(sb.track).off(s.touchEvents.start, sb.dragStart);
        $(target).off(s.touchEvents.move, sb.dragMove);
        $(target).off(s.touchEvents.end, sb.dragEnd);
    },
    set: function () {
        if (!s.params.scrollbar) return;
        var sb = s.scrollbar;
        sb.track = $(s.params.scrollbar);
        if (s.params.uniqueNavElements && typeof s.params.scrollbar === 'string' && sb.track.length > 1 && s.container.find(s.params.scrollbar).length === 1) {
            sb.track = s.container.find(s.params.scrollbar);
        }
        sb.drag = sb.track.find('.swiper-scrollbar-drag');
        if (sb.drag.length === 0) {
            sb.drag = $('<div class="swiper-scrollbar-drag"></div>');
            sb.track.append(sb.drag);
        }
        sb.drag[0].style.width = '';
        sb.drag[0].style.height = '';
        sb.trackSize = s.isHorizontal() ? sb.track[0].offsetWidth : sb.track[0].offsetHeight;

        sb.divider = s.size / s.virtualSize;
        sb.moveDivider = sb.divider * (sb.trackSize / s.size);
        sb.dragSize = sb.trackSize * sb.divider;

        if (s.isHorizontal()) {
            sb.drag[0].style.width = sb.dragSize + 'px';
        }
        else {
            sb.drag[0].style.height = sb.dragSize + 'px';
        }

        if (sb.divider >= 1) {
            sb.track[0].style.display = 'none';
        }
        else {
            sb.track[0].style.display = '';
        }
        if (s.params.scrollbarHide) {
            sb.track[0].style.opacity = 0;
        }
    },
    setTranslate: function () {
        if (!s.params.scrollbar) return;
        var diff;
        var sb = s.scrollbar;
        var translate = s.translate || 0;
        var newPos;

        var newSize = sb.dragSize;
        newPos = (sb.trackSize - sb.dragSize) * s.progress;
        if (s.rtl && s.isHorizontal()) {
            newPos = -newPos;
            if (newPos > 0) {
                newSize = sb.dragSize - newPos;
                newPos = 0;
            }
            else if (-newPos + sb.dragSize > sb.trackSize) {
                newSize = sb.trackSize + newPos;
            }
        }
        else {
            if (newPos < 0) {
                newSize = sb.dragSize + newPos;
                newPos = 0;
            }
            else if (newPos + sb.dragSize > sb.trackSize) {
                newSize = sb.trackSize - newPos;
            }
        }
        if (s.isHorizontal()) {
            if (s.support.transforms3d) {
                sb.drag.transform('translate3d(' + (newPos) + 'px, 0, 0)');
            }
            else {
                sb.drag.transform('translateX(' + (newPos) + 'px)');
            }
            sb.drag[0].style.width = newSize + 'px';
        }
        else {
            if (s.support.transforms3d) {
                sb.drag.transform('translate3d(0px, ' + (newPos) + 'px, 0)');
            }
            else {
                sb.drag.transform('translateY(' + (newPos) + 'px)');
            }
            sb.drag[0].style.height = newSize + 'px';
        }
        if (s.params.scrollbarHide) {
            clearTimeout(sb.timeout);
            sb.track[0].style.opacity = 1;
            sb.timeout = setTimeout(function () {
                sb.track[0].style.opacity = 0;
                sb.track.transition(400);
            }, 1000);
        }
    },
    setTransition: function (duration) {
        if (!s.params.scrollbar) return;
        s.scrollbar.drag.transition(duration);
    }
};