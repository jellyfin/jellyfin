define(['dom'], function (dom) {
    'use strict';

    var scopes = [];
    function pushScope(elem) {
        scopes.push(elem);
    }

    function popScope(elem) {

        if (scopes.length) {
            scopes.length -= 1;
        }
    }

    function autoFocus(view, defaultToFirst, findAutoFocusElement) {

        var element;
        if (findAutoFocusElement !== false) {
            element = view.querySelector('*[autofocus]');
            if (element) {
                focus(element);
                return element;
            }
        }

        if (defaultToFirst !== false) {
            element = getFocusableElements(view, 1, 'noautofocus')[0];

            if (element) {
                focus(element);
                return element;
            }
        }

        return null;
    }

    function focus(element) {

        try {
            element.focus({
                preventScroll: true
            });
        } catch (err) {
            console.log('Error in focusManager.autoFocus: ' + err);
        }
    }

    var focusableTagNames = ['INPUT', 'TEXTAREA', 'SELECT', 'BUTTON', 'A'];
    var focusableContainerTagNames = ['BODY', 'DIALOG'];
    var focusableQuery = focusableTagNames.map(function (t) {

        if (t === 'INPUT') {
            t += ':not([type="range"]):not([type="file"])';
        }
        return t + ':not([tabindex="-1"]):not(:disabled)';

    }).join(',') + ',.focusable';

    function isFocusable(elem) {

        if (focusableTagNames.indexOf(elem.tagName) !== -1) {
            return true;
        }

        if (elem.classList && elem.classList.contains('focusable')) {
            return true;
        }

        return false;
    }

    function normalizeFocusable(elem, originalElement) {
        if (elem) {
            var tagName = elem.tagName;
            if (!tagName || tagName === 'HTML' || tagName === 'BODY') {
                elem = originalElement;
            }
        }

        return elem;
    }

    function focusableParent(elem) {

        var originalElement = elem;

        while (!isFocusable(elem)) {
            var parent = elem.parentNode;

            if (!parent) {
                return normalizeFocusable(elem, originalElement);
            }

            elem = parent;
        }

        return normalizeFocusable(elem, originalElement);
    }

    // Determines if a focusable element can be focused at a given point in time 
    function isCurrentlyFocusableInternal(elem) {

        // http://stackoverflow.com/questions/19669786/check-if-element-is-visible-in-dom
        if (elem.offsetParent === null) {
            return false;
        }

        return true;
    }

    // Determines if a focusable element can be focused at a given point in time 
    function isCurrentlyFocusable(elem) {

        if (elem.disabled) {
            return false;
        }

        if (elem.getAttribute('tabindex') === "-1") {
            return false;
        }

        if (elem.tagName === 'INPUT') {
            var type = elem.type;
            if (type === 'range') {
                return false;
            }
            if (type === 'file') {
                return false;
            }
        }

        return isCurrentlyFocusableInternal(elem);
    }

    function getDefaultScope() {
        return scopes[0] || document.body;
    }

    function getFocusableElements(parent, limit, excludeClass) {
        var elems = (parent || getDefaultScope()).querySelectorAll(focusableQuery);
        var focusableElements = [];

        for (var i = 0, length = elems.length; i < length; i++) {

            var elem = elems[i];

            if (excludeClass && elem.classList.contains(excludeClass)) {
                continue;
            }

            if (isCurrentlyFocusableInternal(elem)) {
                focusableElements.push(elem);

                if (limit && focusableElements.length >= limit) {
                    break;
                }
            }
        }

        return focusableElements;
    }

    function isFocusContainer(elem, direction) {

        if (focusableContainerTagNames.indexOf(elem.tagName) !== -1) {
            return true;
        }

        var classList = elem.classList;

        if (classList.contains('focuscontainer')) {
            return true;
        }

        if (direction === 0) {
            if (classList.contains('focuscontainer-x')) {
                return true;
            }
            if (classList.contains('focuscontainer-left')) {
                return true;
            }
        }
        else if (direction === 1) {
            if (classList.contains('focuscontainer-x')) {
                return true;
            }
            if (classList.contains('focuscontainer-right')) {
                return true;
            }
        }
        else if (direction === 2) {
            if (classList.contains('focuscontainer-y')) {
                return true;
            }
        }
        else if (direction === 3) {
            if (classList.contains('focuscontainer-y')) {
                return true;
            }
            if (classList.contains('focuscontainer-down')) {
                return true;
            }
        }

        return false;
    }

    function getFocusContainer(elem, direction) {
        while (!isFocusContainer(elem, direction)) {
            elem = elem.parentNode;

            if (!elem) {
                return getDefaultScope();
            }
        }

        return elem;
    }

    function getOffset(elem) {

        var box;

        // Support: BlackBerry 5, iOS 3 (original iPhone)
        // If we don't have gBCR, just use 0,0 rather than error
        if (elem.getBoundingClientRect) {
            box = elem.getBoundingClientRect();
        } else {
            box = {
                top: 0,
                left: 0,
                width: 0,
                height: 0
            };
        }

        if (box.right === null) {

            // Create a new object because some browsers will throw an error when trying to set data onto the Rect object
            var newBox = {
                top: box.top,
                left: box.left,
                width: box.width,
                height: box.height
            };

            box = newBox;

            box.right = box.left + box.width;
            box.bottom = box.top + box.height;
        }

        return box;
    }

    function nav(activeElement, direction, container, focusableElements) {

        activeElement = activeElement || document.activeElement;

        if (activeElement) {
            activeElement = focusableParent(activeElement);
        }

        container = container || (activeElement ? getFocusContainer(activeElement, direction) : getDefaultScope());

        if (!activeElement) {
            autoFocus(container, true, false);
            return;
        }

        var focusableContainer = dom.parentWithClass(activeElement, 'focusable');

        var rect = getOffset(activeElement);

        // Get elements and work out x/y points
        var cache = [],
            point1x = parseFloat(rect.left) || 0,
            point1y = parseFloat(rect.top) || 0,
            point2x = parseFloat(point1x + rect.width - 1) || point1x,
            point2y = parseFloat(point1y + rect.height - 1) || point1y,
            // Shortcuts to help with compression
            min = Math.min,
            max = Math.max;

        var sourceMidX = rect.left + (rect.width / 2);
        var sourceMidY = rect.top + (rect.height / 2);

        var focusable = focusableElements || container.querySelectorAll(focusableQuery);

        var maxDistance = Infinity;
        var minDistance = maxDistance;
        var nearestElement;

        for (var i = 0, length = focusable.length; i < length; i++) {
            var curr = focusable[i];

            if (curr === activeElement) {
                continue;
            }
            // Don't refocus into the same container
            if (curr === focusableContainer) {
                continue;
            }

            //if (!isCurrentlyFocusableInternal(curr)) {
            //    continue;
            //}

            var elementRect = getOffset(curr);

            // not currently visible
            if (!elementRect.width && !elementRect.height) {
                continue;
            }

            switch (direction) {

                case 0:
                    // left
                    if (elementRect.left >= rect.left) {
                        continue;
                    }
                    if (elementRect.right === rect.right) {
                        continue;
                    }
                    break;
                case 1:
                    // right
                    if (elementRect.right <= rect.right) {
                        continue;
                    }
                    if (elementRect.left === rect.left) {
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

            var x = elementRect.left,
                y = elementRect.top,
                x2 = x + elementRect.width - 1,
                y2 = y + elementRect.height - 1;

            var intersectX = intersects(point1x, point2x, x, x2);
            var intersectY = intersects(point1y, point2y, y, y2);

            var midX = elementRect.left + (elementRect.width / 2);
            var midY = elementRect.top + (elementRect.height / 2);

            var distX;
            var distY;

            switch (direction) {

                case 0:
                    // left
                    distX = Math.abs(point1x - Math.min(point1x, x2));
                    distY = intersectY ? 0 : Math.abs(sourceMidY - midY);
                    break;
                case 1:
                    // right
                    distX = Math.abs(point2x - Math.max(point2x, x));
                    distY = intersectY ? 0 : Math.abs(sourceMidY - midY);
                    break;
                case 2:
                    // up
                    distY = Math.abs(point1y - Math.min(point1y, y2));
                    distX = intersectX ? 0 : Math.abs(sourceMidX - midX);
                    break;
                case 3:
                    // down
                    distY = Math.abs(point2y - Math.max(point2y, y));
                    distX = intersectX ? 0 : Math.abs(sourceMidX - midX);
                    break;
                default:
                    break;
            }

            var dist = Math.sqrt(distX * distX + distY * distY);

            if (dist < minDistance) {
                nearestElement = curr;
                minDistance = dist;
            }
        }

        if (nearestElement) {

            // See if there's a focusable container, and if so, send the focus command to that
            if (activeElement) {
                var nearestElementFocusableParent = dom.parentWithClass(nearestElement, 'focusable');
                if (nearestElementFocusableParent && nearestElementFocusableParent !== nearestElement) {
                    if (focusableContainer !== nearestElementFocusableParent) {
                        nearestElement = nearestElementFocusableParent;
                    }
                }
            }
            focus(nearestElement);
        }
    }

    function intersectsInternal(a1, a2, b1, b2) {

        return (b1 >= a1 && b1 <= a2) || (b2 >= a1 && b2 <= a2);
    }

    function intersects(a1, a2, b1, b2) {

        return intersectsInternal(a1, a2, b1, b2) || intersectsInternal(b1, b2, a1, a2);
    }

    function sendText(text) {
        var elem = document.activeElement;

        elem.value = text;
    }

    function focusFirst(container, focusableSelector) {

        var elems = container.querySelectorAll(focusableSelector);

        for (var i = 0, length = elems.length; i < length; i++) {

            var elem = elems[i];

            if (isCurrentlyFocusableInternal(elem)) {
                focus(elem);
                break;
            }
        }
    }

    function focusLast(container, focusableSelector) {

        var elems = [].slice.call(container.querySelectorAll(focusableSelector), 0).reverse();

        for (var i = 0, length = elems.length; i < length; i++) {

            var elem = elems[i];

            if (isCurrentlyFocusableInternal(elem)) {
                focus(elem);
                break;
            }
        }
    }

    function moveFocus(sourceElement, container, focusableSelector, offset) {

        var elems = container.querySelectorAll(focusableSelector);
        var list = [];
        var i, length, elem;

        for (i = 0, length = elems.length; i < length; i++) {

            elem = elems[i];

            if (isCurrentlyFocusableInternal(elem)) {
                list.push(elem);
            }
        }

        var currentIndex = -1;

        for (i = 0, length = list.length; i < length; i++) {

            elem = list[i];

            if (sourceElement === elem || elem.contains(sourceElement)) {
                currentIndex = i;
                break;
            }
        }

        if (currentIndex === -1) {
            return;
        }

        var newIndex = currentIndex + offset;
        newIndex = Math.max(0, newIndex);
        newIndex = Math.min(newIndex, list.length - 1);

        var newElem = list[newIndex];
        if (newElem) {
            focus(newElem);
        }
    }

    return {
        autoFocus: autoFocus,
        focus: focus,
        focusableParent: focusableParent,
        getFocusableElements: getFocusableElements,
        moveLeft: function (sourceElement, options) {

            var container = options ? options.container : null;
            var focusableElements = options ? options.focusableElements : null;
            nav(sourceElement, 0, container, focusableElements);

        },
        moveRight: function (sourceElement, options) {

            var container = options ? options.container : null;
            var focusableElements = options ? options.focusableElements : null;
            nav(sourceElement, 1, container, focusableElements);

        },
        moveUp: function (sourceElement, options) {

            var container = options ? options.container : null;
            var focusableElements = options ? options.focusableElements : null;
            nav(sourceElement, 2, container, focusableElements);

        },
        moveDown: function (sourceElement, options) {

            var container = options ? options.container : null;
            var focusableElements = options ? options.focusableElements : null;
            nav(sourceElement, 3, container, focusableElements);

        },
        sendText: sendText,
        isCurrentlyFocusable: isCurrentlyFocusable,
        pushScope: pushScope,
        popScope: popScope,
        focusFirst: focusFirst,
        focusLast: focusLast,
        moveFocus: moveFocus
    };
});