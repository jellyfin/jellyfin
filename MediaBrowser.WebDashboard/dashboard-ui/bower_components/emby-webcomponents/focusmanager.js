define([], function () {

    function autoFocus(view, defaultToFirst) {

        var element = view.querySelector('*[autofocus]');
        if (element) {
            focus(element);
            return element;
        } else if (defaultToFirst !== false) {
            element = getFocusableElements(view)[0];

            if (element) {
                focus(element);
                return element;
            }
        }

        return null;
    }

    function focus(element) {

        var tagName = element.tagName;
        if (tagName == 'PAPER-INPUT' || tagName == 'PAPER-DROPDOWN-MENU' || tagName == 'EMBY-DROPDOWN-MENU') {
            element = element.querySelector('input') || element;
        }

        try {
            element.focus();
        } catch (err) {
            console.log('Error in focusManager.autoFocus: ' + err);
        }
    }

    var focusableTagNames = ['INPUT', 'TEXTAREA', 'SELECT', 'BUTTON', 'A', 'PAPER-BUTTON', 'PAPER-INPUT', 'PAPER-TEXTAREA', 'PAPER-ICON-BUTTON', 'PAPER-FAB', 'PAPER-CHECKBOX', 'PAPER-ICON-ITEM', 'PAPER-MENU-ITEM', 'PAPER-DROPDOWN-MENU', 'EMBY-DROPDOWN-MENU'];
    var focusableContainerTagNames = ['BODY', 'PAPER-DIALOG', 'DIALOG'];
    var focusableQuery = focusableTagNames.join(',') + ',.focusable';

    function isFocusable(elem) {

        if (focusableTagNames.indexOf(elem.tagName) != -1) {
            return true;
        }

        if (elem.classList && elem.classList.contains('focusable')) {
            return true;
        }

        return false;
    }

    function focusableParent(elem) {

        while (!isFocusable(elem)) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    // Determines if a focusable element can be focused at a given point in time 
    function isCurrentlyFocusable(elem) {

        if (elem.disabled) {
            return false;
        }

        if (elem.getAttribute('tabindex') == "-1") {
            return false;
        }

        // http://stackoverflow.com/questions/19669786/check-if-element-is-visible-in-dom
        if (elem.offsetParent === null) {
            return false;
        }

        return true;
    }

    function getFocusableElements(parent) {
        var elems = (parent || document).querySelectorAll(focusableQuery);
        var focusableElements = [];

        for (var i = 0, length = elems.length; i < length; i++) {

            var elem = elems[i];

            if (isCurrentlyFocusable(elem)) {
                focusableElements.push(elem);
            }
        }

        return focusableElements;
    }

    function isFocusContainer(elem, direction) {

        if (focusableContainerTagNames.indexOf(elem.tagName) != -1) {
            return true;
        }

        if (direction < 2) {
            if (elem.classList.contains('focuscontainer-x')) {
                return true;
            }
        }
        else if (direction == 3) {
            if (elem.classList.contains('focuscontainer-down')) {
                return true;
            }
        }

        return false;
    }

    function getFocusContainer(elem, direction) {
        while (!isFocusContainer(elem, direction)) {
            elem = elem.parentNode;

            if (!elem) {
                return document.body;
            }
        }

        return elem;
    }

    function getWindowData(win, documentElement) {

        return {
            pageYOffset: win.pageYOffset,
            pageXOffset: win.pageXOffset,
            clientTop: documentElement.clientTop,
            clientLeft: documentElement.clientLeft
        };
    }

    function getOffset(elem, windowData) {

        var box = { top: 0, left: 0 };

        // Support: BlackBerry 5, iOS 3 (original iPhone)
        // If we don't have gBCR, just use 0,0 rather than error
        if (elem.getBoundingClientRect) {
            box = elem.getBoundingClientRect();
        }
        return {
            top: box.top + windowData.pageYOffset - windowData.clientTop,
            left: box.left + windowData.pageXOffset - windowData.clientLeft
        };
    }

    function getViewportBoundingClientRect(elem, windowData) {

        var offset = getOffset(elem, windowData);

        var posY = offset.top - windowData.pageXOffset;
        var posX = offset.left - windowData.pageYOffset;

        var width = elem.offsetWidth;
        var height = elem.offsetHeight;

        return {
            left: posX,
            top: posY,
            width: width,
            height: height,
            right: posX + width,
            bottom: posY + height
        };
    }

    function nav(activeElement, direction) {

        activeElement = activeElement || document.activeElement;

        if (activeElement) {
            activeElement = focusableParent(activeElement);
        }

        var container = activeElement ? getFocusContainer(activeElement, direction) : document.body;

        if (!activeElement) {
            autoFocus(container, true);
            return;
        }

        var focusableContainer = parentWithClass(activeElement, 'focusable');

        var doc = activeElement.ownerDocument;
        var windowData = getWindowData(doc.defaultView, doc.documentElement);
        var rect = getViewportBoundingClientRect(activeElement, windowData);
        var focusableElements = [];

        var focusable = container.querySelectorAll(focusableQuery);
        for (var i = 0, length = focusable.length; i < length; i++) {
            var curr = focusable[i];

            if (curr == activeElement) {
                continue;
            }
            // Don't refocus into the same container
            if (curr == focusableContainer) {
                continue;
            }

            if (!isCurrentlyFocusable(curr)) {
                continue;
            }

            var elementRect = getViewportBoundingClientRect(curr, windowData);

            switch (direction) {

                case 0:
                    // left
                    if (elementRect.left >= rect.left) {
                        continue;
                    }
                    if (elementRect.right == rect.right) {
                        continue;
                    }
                    break;
                case 1:
                    // right
                    if (elementRect.right <= rect.right) {
                        continue;
                    }
                    if (elementRect.left == rect.left) {
                        continue;
                    }
                    break;
                case 2:
                    // up
                    if (elementRect.top >= rect.top) {
                        continue;
                    }
                    if (elementRect.bottom >= rect.bottom) {
                        continue;
                    }
                    break;
                case 3:
                    // down
                    if (elementRect.bottom <= rect.bottom) {
                        continue;
                    }
                    if (elementRect.top <= rect.top) {
                        continue;
                    }
                    break;
                default:
                    break;
            }
            focusableElements.push({
                element: curr,
                clientRect: elementRect
            });
        }

        var nearest = getNearestElements(focusableElements, rect, direction);

        if (nearest.length) {

            var nearestElement = nearest[0].node;

            // See if there's a focusable container, and if so, send the focus command to that
            var nearestElementFocusableParent = parentWithClass(nearestElement, 'focusable');
            if (nearestElementFocusableParent && nearestElementFocusableParent != nearestElement && activeElement) {
                if (parentWithClass(activeElement, 'focusable') != nearestElementFocusableParent) {
                    nearestElement = nearestElementFocusableParent;
                }
            }

            focus(nearestElement);
        }
    }

    function parentWithClass(elem, className) {

        while (!elem.classList || !elem.classList.contains(className)) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    function intersectsInternal(a1, a2, b1, b2) {

        return (b1 >= a1 && b1 <= a2) || (b2 >= a1 && b2 <= a2);
    }

    function intersects(a1, a2, b1, b2) {

        return intersectsInternal(a1, a2, b1, b2) || intersectsInternal(b1, b2, a1, a2);
    }

    var enableDebugInfo = false;

    function getNearestElements(elementInfos, options, direction) {

        if (enableDebugInfo) {
            removeAll();
        }

        // Get elements and work out x/y points
        var cache = [],
			point1x = parseFloat(options.left) || 0,
			point1y = parseFloat(options.top) || 0,
			point2x = parseFloat(point1x + options.width - 1) || point1x,
			point2y = parseFloat(point1y + options.height - 1) || point1y,
			// Shortcuts to help with compression
			min = Math.min,
			max = Math.max;

        var sourceMidX = options.left + (options.width / 2);
        var sourceMidY = options.top + (options.height / 2);

        // Loop through all elements and check their positions
        for (var i = 0, length = elementInfos.length; i < length; i++) {

            var elementInfo = elementInfos[i];
            var elem = elementInfo.element;

            var off = elementInfo.clientRect,
                x = off.left,
                y = off.top,
                x2 = x + off.width - 1,
                y2 = y + off.height - 1;

            var intersectX = intersects(point1x, point2x, x, x2);
            var intersectY = intersects(point1y, point2y, y, y2);

            var midX = off.left + (off.width / 2);
            var midY = off.top + (off.height / 2);

            var distX;
            var distY;
            var distX2;
            var distY2;

            switch (direction) {

                case 0:
                    // left
                    distX = distX2 = Math.abs(point1x - Math.min(point1x, x2));
                    distY = intersectY ? 0 : Math.abs(sourceMidY - midY);
                    distY2 = Math.abs(sourceMidY - midY);
                    break;
                case 1:
                    // right
                    distX = distX2 = Math.abs(point2x - Math.max(point2x, x));
                    distY = intersectY ? 0 : Math.abs(sourceMidY - midY);
                    distY2 = Math.abs(sourceMidY - midY);
                    break;
                case 2:
                    // up
                    distY = distY2 = Math.abs(point1y - Math.min(point1y, y2));
                    distX = intersectX ? 0 : Math.abs(sourceMidX - midX);
                    distX2 = Math.abs(sourceMidX - midX);
                    break;
                case 3:
                    // down
                    distY = distY2 = Math.abs(point2y - Math.max(point2y, y));
                    distX = intersectX ? 0 : Math.abs(sourceMidX - midX);
                    distX2 = Math.abs(sourceMidX - midX);
                    break;
                default:
                    break;
            }

            if (enableDebugInfo) {
                addDebugInfo(elem, distX, distY);
            }

            var distT = Math.sqrt(distX * distX + distY * distY);
            var distT2 = Math.sqrt(distX2 * distX2 + distY2 * distY2);

            cache.push({
                node: elem,
                distX: distX,
                distY: distY,
                distT: distT,
                distT2: distT2
            });
        }

        cache.sort(sortNodesT);
        //if (direction >= 2) {
        //    cache.sort(sortNodesX);
        //} else {
        //    cache.sort(sortNodesY);
        //}

        return cache;
    }

    function addDebugInfo(elem, distX, distY) {

        var div = elem.querySelector('focusInfo');

        if (!div) {
            div = document.createElement('div');
            div.classList.add('focusInfo');
            elem.appendChild(div);

            if (getComputedStyle(elem, null).getPropertyValue('position') == 'static') {
                elem.style.position = 'relative';
            }
            div.style.position = 'absolute';
            div.style.left = '0';
            div.style.top = '0';
            div.style.color = 'white';
            div.style.backgroundColor = 'red';
            div.style.padding = '2px';
        }

        div.innerHTML = Math.round(distX) + ',' + Math.round(distY);
    }

    function removeAll() {
        var elems = document.querySelectorAll('.focusInfo');
        for (var i = 0, length = elems.length; i < length; i++) {
            elems[i].parentNode.removeChild(elems[i]);
        }
    }

    function sortNodesX(a, b) {
        var result = a.distX - b.distX;

        if (result == 0) {
            return a.distT - b.distT;
        }

        return result;
    }

    function sortNodesT(a, b) {
        var result = a.distT - b.distT;

        if (result == 0) {
            return a.distT2 - b.distT2;
        }

        return result;
    }

    function sortNodesY(a, b) {
        var result = a.distY - b.distY;

        if (result == 0) {
            return a.distT - b.distT;
        }

        return result;
    }

    function sendText(text) {
        var elem = document.activeElement;

        elem.value = text;
    }

    return {
        autoFocus: autoFocus,
        focus: focus,
        focusableParent: focusableParent,
        getFocusableElements: getFocusableElements,
        moveLeft: function (sourceElement) {
            nav(sourceElement, 0);
        },
        moveRight: function (sourceElement) {
            nav(sourceElement, 1);
        },
        moveUp: function (sourceElement) {
            nav(sourceElement, 2);
        },
        moveDown: function (sourceElement) {
            nav(sourceElement, 3);
        },
        sendText: sendText,
        isCurrentlyFocusable: isCurrentlyFocusable
    };
});