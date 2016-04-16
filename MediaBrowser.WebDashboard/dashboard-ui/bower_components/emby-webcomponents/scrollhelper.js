define(['focusManager', 'scrollStyles'], function (focusManager) {

    function getOffsets(elems) {

        var doc = document;
        var results = [];

        if (!doc) {
            return results;
        }

        var docElem = doc.documentElement;
        var docElemValues = {
            clientTop: docElem.clientTop,
            clientLeft: docElem.clientLeft
        };

        var win = doc.defaultView;
        var winValues = {
            pageXOffset: win.pageXOffset,
            pageYOffset: win.pageYOffset
        };

        var box;
        var elem;

        for (var i = 0, length = elems.length; i < length; i++) {

            elem = elems[i];
            // Support: BlackBerry 5, iOS 3 (original iPhone)
            // If we don't have gBCR, just use 0,0 rather than error
            if (elem.getBoundingClientRect) {
                box = elem.getBoundingClientRect();
            } else {
                box = { top: 0, left: 0 };
            }

            results[i] = {
                top: box.top + winValues.pageYOffset - docElemValues.clientTop,
                left: box.left + winValues.pageXOffset - docElemValues.clientLeft
            };
        }

        return results;
    }

    function getPosition(scrollContainer, item, horizontal) {

        var offsets = getOffsets([scrollContainer, item]);
        var slideeOffset = offsets[0];
        var itemOffset = offsets[1];

        var offset = horizontal ? itemOffset.left - slideeOffset.left : itemOffset.top - slideeOffset.top;
        var size = item[horizontal ? 'offsetWidth' : 'offsetHeight'];

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
                    element.addEventListener('focus', centerOnFocusHorizontal, true);
                } else {
                    element.addEventListener('focus', centerOnFocusVertical, true);
                }
            },
            off: function (element, horizontal) {
                if (horizontal) {
                    element.removeEventListener('focus', centerOnFocusHorizontal, true);
                } else {
                    element.removeEventListener('focus', centerOnFocusVertical, true);
                }
            }
        },
        toCenter: toCenter
    };
});