define(['focusManager', 'dom', 'scrollStyles'], function (focusManager, dom) {

    function getBoundingClientRect(elem) {

        // Support: BlackBerry 5, iOS 3 (original iPhone)
        // If we don't have gBCR, just use 0,0 rather than error
        if (elem.getBoundingClientRect) {
            return elem.getBoundingClientRect();
        } else {
            return { top: 0, left: 0 };
        }
    }

    function getPosition(scrollContainer, item, horizontal) {

        var slideeOffset = getBoundingClientRect(scrollContainer);
        var itemOffset = getBoundingClientRect(item);

        var offset = horizontal ? itemOffset.left - slideeOffset.left : itemOffset.top - slideeOffset.top;
        var size = horizontal ? itemOffset.width : itemOffset.height;
        if (!size && size !== 0) {
            size = item[horizontal ? 'offsetWidth' : 'offsetHeight'];
        }

        if (horizontal) {
            offset += scrollContainer.scrollLeft;
        } else {
            offset += scrollContainer.scrollTop;
        }

        var frameSize = horizontal ? scrollContainer.offsetWidth : scrollContainer.offsetHeight;

        return {
            start: offset,
            center: (offset - (frameSize / 2) + (size / 2)),
            end: offset - frameSize + size,
            size: size
        };
    }

    function toCenter(container, elem, horizontal) {
        var pos = getPosition(container, elem, horizontal);

        if (container.scrollTo) {
            if (horizontal) {
                container.scrollTo(pos.center, 0);
            } else {
                container.scrollTo(0, pos.center);
            }
        } else {
            if (horizontal) {
                container.scrollLeft = Math.round(pos.center);
            } else {
                container.scrollTop = Math.round(pos.center);
            }
        }
    }

    function centerOnFocus(e, scrollSlider, horizontal) {
        var focused = focusManager.focusableParent(e.target);

        if (focused) {
            toCenter(scrollSlider, focused, horizontal);
        }
    }

    function centerOnFocusHorizontal(e) {
        centerOnFocus(e, this, true);
    }
    function centerOnFocusVertical(e) {
        centerOnFocus(e, this, false);
    }

    return {
        getPosition: getPosition,
        centerFocus: {
            on: function (element, horizontal) {
                if (horizontal) {
                    dom.addEventListener(element, 'focus', centerOnFocusHorizontal, {
                        capture: true,
                        passive: true
                    });
                } else {
                    dom.addEventListener(element, 'focus', centerOnFocusVertical, {
                        capture: true,
                        passive: true
                    });
                }
            },
            off: function (element, horizontal) {
                if (horizontal) {
                    dom.removeEventListener(element, 'focus', centerOnFocusHorizontal, {
                        capture: true,
                        passive: true
                    });
                } else {
                    dom.removeEventListener(element, 'focus', centerOnFocusVertical, {
                        capture: true,
                        passive: true
                    });
                }
            }
        },
        toCenter: toCenter
    };
});