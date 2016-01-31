/*===========================
Dom7 Library
===========================*/
var Dom7 = (function () {
    var Dom7 = function (arr) {
        var _this = this, i = 0;
        // Create array-like object
        for (i = 0; i < arr.length; i++) {
            _this[i] = arr[i];
        }
        _this.length = arr.length;
        // Return collection with methods
        return this;
    };
    var $ = function (selector, context) {
        var arr = [], i = 0;
        if (selector && !context) {
            if (selector instanceof Dom7) {
                return selector;
            }
        }
        if (selector) {
            // String
            if (typeof selector === 'string') {
                var els, tempParent, html = selector.trim();
                if (html.indexOf('<') >= 0 && html.indexOf('>') >= 0) {
                    var toCreate = 'div';
                    if (html.indexOf('<li') === 0) toCreate = 'ul';
                    if (html.indexOf('<tr') === 0) toCreate = 'tbody';
                    if (html.indexOf('<td') === 0 || html.indexOf('<th') === 0) toCreate = 'tr';
                    if (html.indexOf('<tbody') === 0) toCreate = 'table';
                    if (html.indexOf('<option') === 0) toCreate = 'select';
                    tempParent = document.createElement(toCreate);
                    tempParent.innerHTML = selector;
                    for (i = 0; i < tempParent.childNodes.length; i++) {
                        arr.push(tempParent.childNodes[i]);
                    }
                }
                else {
                    if (!context && selector[0] === '#' && !selector.match(/[ .<>:~]/)) {
                        // Pure ID selector
                        els = [document.getElementById(selector.split('#')[1])];
                    }
                    else {
                        // Other selectors
                        els = (context || document).querySelectorAll(selector);
                    }
                    for (i = 0; i < els.length; i++) {
                        if (els[i]) arr.push(els[i]);
                    }
                }
            }
            // Node/element
            else if (selector.nodeType || selector === window || selector === document) {
                arr.push(selector);
            }
            //Array of elements or instance of Dom
            else if (selector.length > 0 && selector[0].nodeType) {
                for (i = 0; i < selector.length; i++) {
                    arr.push(selector[i]);
                }
            }
        }
        return new Dom7(arr);
    };
    Dom7.prototype = {
        // Classes and attriutes
        addClass: function (className) {
            if (typeof className === 'undefined') {
                return this;
            }
            var classes = className.split(' ');
            for (var i = 0; i < classes.length; i++) {
                for (var j = 0; j < this.length; j++) {
                    this[j].classList.add(classes[i]);
                }
            }
            return this;
        },
        removeClass: function (className) {
            var classes = className.split(' ');
            for (var i = 0; i < classes.length; i++) {
                for (var j = 0; j < this.length; j++) {
                    this[j].classList.remove(classes[i]);
                }
            }
            return this;
        },
        hasClass: function (className) {
            if (!this[0]) return false;
            else return this[0].classList.contains(className);
        },
        toggleClass: function (className) {
            var classes = className.split(' ');
            for (var i = 0; i < classes.length; i++) {
                for (var j = 0; j < this.length; j++) {
                    this[j].classList.toggle(classes[i]);
                }
            }
            return this;
        },
        attr: function (attrs, value) {
            if (arguments.length === 1 && typeof attrs === 'string') {
                // Get attr
                if (this[0]) return this[0].getAttribute(attrs);
                else return undefined;
            }
            else {
                // Set attrs
                for (var i = 0; i < this.length; i++) {
                    if (arguments.length === 2) {
                        // String
                        this[i].setAttribute(attrs, value);
                    }
                    else {
                        // Object
                        for (var attrName in attrs) {
                            this[i][attrName] = attrs[attrName];
                            this[i].setAttribute(attrName, attrs[attrName]);
                        }
                    }
                }
                return this;
            }
        },
        removeAttr: function (attr) {
            for (var i = 0; i < this.length; i++) {
                this[i].removeAttribute(attr);
            }
            return this;
        },
        data: function (key, value) {
            if (typeof value === 'undefined') {
                // Get value
                if (this[0]) {
                    var dataKey = this[0].getAttribute('data-' + key);
                    if (dataKey) return dataKey;
                    else if (this[0].dom7ElementDataStorage && (key in this[0].dom7ElementDataStorage)) return this[0].dom7ElementDataStorage[key];
                    else return undefined;
                }
                else return undefined;
            }
            else {
                // Set value
                for (var i = 0; i < this.length; i++) {
                    var el = this[i];
                    if (!el.dom7ElementDataStorage) el.dom7ElementDataStorage = {};
                    el.dom7ElementDataStorage[key] = value;
                }
                return this;
            }
        },
        // Transforms
        transform : function (transform) {
            for (var i = 0; i < this.length; i++) {
                var elStyle = this[i].style;
                elStyle.webkitTransform = elStyle.MsTransform = elStyle.msTransform = elStyle.MozTransform = elStyle.OTransform = elStyle.transform = transform;
            }
            return this;
        },
        transition: function (duration) {
            if (typeof duration !== 'string') {
                duration = duration + 'ms';
            }
            for (var i = 0; i < this.length; i++) {
                var elStyle = this[i].style;
                elStyle.webkitTransitionDuration = elStyle.MsTransitionDuration = elStyle.msTransitionDuration = elStyle.MozTransitionDuration = elStyle.OTransitionDuration = elStyle.transitionDuration = duration;
            }
            return this;
        },
        //Events
        on: function (eventName, targetSelector, listener, capture) {
            function handleLiveEvent(e) {
                var target = e.target;
                if ($(target).is(targetSelector)) listener.call(target, e);
                else {
                    var parents = $(target).parents();
                    for (var k = 0; k < parents.length; k++) {
                        if ($(parents[k]).is(targetSelector)) listener.call(parents[k], e);
                    }
                }
            }
            var events = eventName.split(' ');
            var i, j;
            for (i = 0; i < this.length; i++) {
                if (typeof targetSelector === 'function' || targetSelector === false) {
                    // Usual events
                    if (typeof targetSelector === 'function') {
                        listener = arguments[1];
                        capture = arguments[2] || false;
                    }
                    for (j = 0; j < events.length; j++) {
                        this[i].addEventListener(events[j], listener, capture);
                    }
                }
                else {
                    //Live events
                    for (j = 0; j < events.length; j++) {
                        if (!this[i].dom7LiveListeners) this[i].dom7LiveListeners = [];
                        this[i].dom7LiveListeners.push({listener: listener, liveListener: handleLiveEvent});
                        this[i].addEventListener(events[j], handleLiveEvent, capture);
                    }
                }
            }

            return this;
        },
        off: function (eventName, targetSelector, listener, capture) {
            var events = eventName.split(' ');
            for (var i = 0; i < events.length; i++) {
                for (var j = 0; j < this.length; j++) {
                    if (typeof targetSelector === 'function' || targetSelector === false) {
                        // Usual events
                        if (typeof targetSelector === 'function') {
                            listener = arguments[1];
                            capture = arguments[2] || false;
                        }
                        this[j].removeEventListener(events[i], listener, capture);
                    }
                    else {
                        // Live event
                        if (this[j].dom7LiveListeners) {
                            for (var k = 0; k < this[j].dom7LiveListeners.length; k++) {
                                if (this[j].dom7LiveListeners[k].listener === listener) {
                                    this[j].removeEventListener(events[i], this[j].dom7LiveListeners[k].liveListener, capture);
                                }
                            }
                        }
                    }
                }
            }
            return this;
        },
        once: function (eventName, targetSelector, listener, capture) {
            var dom = this;
            if (typeof targetSelector === 'function') {
                targetSelector = false;
                listener = arguments[1];
                capture = arguments[2];
            }
            function proxy(e) {
                listener(e);
                dom.off(eventName, targetSelector, proxy, capture);
            }
            dom.on(eventName, targetSelector, proxy, capture);
        },
        trigger: function (eventName, eventData) {
            for (var i = 0; i < this.length; i++) {
                var evt;
                try {
                    evt = new window.CustomEvent(eventName, {detail: eventData, bubbles: true, cancelable: true});
                }
                catch (e) {
                    evt = document.createEvent('Event');
                    evt.initEvent(eventName, true, true);
                    evt.detail = eventData;
                }
                this[i].dispatchEvent(evt);
            }
            return this;
        },
        transitionEnd: function (callback) {
            var events = ['webkitTransitionEnd', 'transitionend', 'oTransitionEnd', 'MSTransitionEnd', 'msTransitionEnd'],
                i, j, dom = this;
            function fireCallBack(e) {
                /*jshint validthis:true */
                if (e.target !== this) return;
                callback.call(this, e);
                for (i = 0; i < events.length; i++) {
                    dom.off(events[i], fireCallBack);
                }
            }
            if (callback) {
                for (i = 0; i < events.length; i++) {
                    dom.on(events[i], fireCallBack);
                }
            }
            return this;
        },
        // Sizing/Styles
        width: function () {
            if (this[0] === window) {
                return window.innerWidth;
            }
            else {
                if (this.length > 0) {
                    return parseFloat(this.css('width'));
                }
                else {
                    return null;
                }
            }
        },
        outerWidth: function (includeMargins) {
            if (this.length > 0) {
                if (includeMargins)
                    return this[0].offsetWidth + parseFloat(this.css('margin-right')) + parseFloat(this.css('margin-left'));
                else
                    return this[0].offsetWidth;
            }
            else return null;
        },
        height: function () {
            if (this[0] === window) {
                return window.innerHeight;
            }
            else {
                if (this.length > 0) {
                    return parseFloat(this.css('height'));
                }
                else {
                    return null;
                }
            }
        },
        outerHeight: function (includeMargins) {
            if (this.length > 0) {
                if (includeMargins)
                    return this[0].offsetHeight + parseFloat(this.css('margin-top')) + parseFloat(this.css('margin-bottom'));
                else
                    return this[0].offsetHeight;
            }
            else return null;
        },
        offset: function () {
            if (this.length > 0) {
                var el = this[0];
                var box = el.getBoundingClientRect();
                var body = document.body;
                var clientTop  = el.clientTop  || body.clientTop  || 0;
                var clientLeft = el.clientLeft || body.clientLeft || 0;
                var scrollTop  = window.pageYOffset || el.scrollTop;
                var scrollLeft = window.pageXOffset || el.scrollLeft;
                return {
                    top: box.top  + scrollTop  - clientTop,
                    left: box.left + scrollLeft - clientLeft
                };
            }
            else {
                return null;
            }
        },
        css: function (props, value) {
            var i;
            if (arguments.length === 1) {
                if (typeof props === 'string') {
                    if (this[0]) return window.getComputedStyle(this[0], null).getPropertyValue(props);
                }
                else {
                    for (i = 0; i < this.length; i++) {
                        for (var prop in props) {
                            this[i].style[prop] = props[prop];
                        }
                    }
                    return this;
                }
            }
            if (arguments.length === 2 && typeof props === 'string') {
                for (i = 0; i < this.length; i++) {
                    this[i].style[props] = value;
                }
                return this;
            }
            return this;
        },

        //Dom manipulation
        each: function (callback) {
            for (var i = 0; i < this.length; i++) {
                callback.call(this[i], i, this[i]);
            }
            return this;
        },
        html: function (html) {
            if (typeof html === 'undefined') {
                return this[0] ? this[0].innerHTML : undefined;
            }
            else {
                for (var i = 0; i < this.length; i++) {
                    this[i].innerHTML = html;
                }
                return this;
            }
        },
        text: function (text) {
            if (typeof text === 'undefined') {
                if (this[0]) {
                    return this[0].textContent.trim();
                }
                else return null;
            }
            else {
                for (var i = 0; i < this.length; i++) {
                    this[i].textContent = text;
                }
                return this;
            }
        },
        is: function (selector) {
            if (!this[0]) return false;
            var compareWith, i;
            if (typeof selector === 'string') {
                var el = this[0];
                if (el === document) return selector === document;
                if (el === window) return selector === window;

                if (el.matches) return el.matches(selector);
                else if (el.webkitMatchesSelector) return el.webkitMatchesSelector(selector);
                else if (el.mozMatchesSelector) return el.mozMatchesSelector(selector);
                else if (el.msMatchesSelector) return el.msMatchesSelector(selector);
                else {
                    compareWith = $(selector);
                    for (i = 0; i < compareWith.length; i++) {
                        if (compareWith[i] === this[0]) return true;
                    }
                    return false;
                }
            }
            else if (selector === document) return this[0] === document;
            else if (selector === window) return this[0] === window;
            else {
                if (selector.nodeType || selector instanceof Dom7) {
                    compareWith = selector.nodeType ? [selector] : selector;
                    for (i = 0; i < compareWith.length; i++) {
                        if (compareWith[i] === this[0]) return true;
                    }
                    return false;
                }
                return false;
            }

        },
        index: function () {
            if (this[0]) {
                var child = this[0];
                var i = 0;
                while ((child = child.previousSibling) !== null) {
                    if (child.nodeType === 1) i++;
                }
                return i;
            }
            else return undefined;
        },
        eq: function (index) {
            if (typeof index === 'undefined') return this;
            var length = this.length;
            var returnIndex;
            if (index > length - 1) {
                return new Dom7([]);
            }
            if (index < 0) {
                returnIndex = length + index;
                if (returnIndex < 0) return new Dom7([]);
                else return new Dom7([this[returnIndex]]);
            }
            return new Dom7([this[index]]);
        },
        append: function (newChild) {
            var i, j;
            for (i = 0; i < this.length; i++) {
                if (typeof newChild === 'string') {
                    var tempDiv = document.createElement('div');
                    tempDiv.innerHTML = newChild;
                    while (tempDiv.firstChild) {
                        this[i].appendChild(tempDiv.firstChild);
                    }
                }
                else if (newChild instanceof Dom7) {
                    for (j = 0; j < newChild.length; j++) {
                        this[i].appendChild(newChild[j]);
                    }
                }
                else {
                    this[i].appendChild(newChild);
                }
            }
            return this;
        },
        prepend: function (newChild) {
            var i, j;
            for (i = 0; i < this.length; i++) {
                if (typeof newChild === 'string') {
                    var tempDiv = document.createElement('div');
                    tempDiv.innerHTML = newChild;
                    for (j = tempDiv.childNodes.length - 1; j >= 0; j--) {
                        this[i].insertBefore(tempDiv.childNodes[j], this[i].childNodes[0]);
                    }
                    // this[i].insertAdjacentHTML('afterbegin', newChild);
                }
                else if (newChild instanceof Dom7) {
                    for (j = 0; j < newChild.length; j++) {
                        this[i].insertBefore(newChild[j], this[i].childNodes[0]);
                    }
                }
                else {
                    this[i].insertBefore(newChild, this[i].childNodes[0]);
                }
            }
            return this;
        },
        insertBefore: function (selector) {
            var before = $(selector);
            for (var i = 0; i < this.length; i++) {
                if (before.length === 1) {
                    before[0].parentNode.insertBefore(this[i], before[0]);
                }
                else if (before.length > 1) {
                    for (var j = 0; j < before.length; j++) {
                        before[j].parentNode.insertBefore(this[i].cloneNode(true), before[j]);
                    }
                }
            }
        },
        insertAfter: function (selector) {
            var after = $(selector);
            for (var i = 0; i < this.length; i++) {
                if (after.length === 1) {
                    after[0].parentNode.insertBefore(this[i], after[0].nextSibling);
                }
                else if (after.length > 1) {
                    for (var j = 0; j < after.length; j++) {
                        after[j].parentNode.insertBefore(this[i].cloneNode(true), after[j].nextSibling);
                    }
                }
            }
        },
        next: function (selector) {
            if (this.length > 0) {
                if (selector) {
                    if (this[0].nextElementSibling && $(this[0].nextElementSibling).is(selector)) return new Dom7([this[0].nextElementSibling]);
                    else return new Dom7([]);
                }
                else {
                    if (this[0].nextElementSibling) return new Dom7([this[0].nextElementSibling]);
                    else return new Dom7([]);
                }
            }
            else return new Dom7([]);
        },
        nextAll: function (selector) {
            var nextEls = [];
            var el = this[0];
            if (!el) return new Dom7([]);
            while (el.nextElementSibling) {
                var next = el.nextElementSibling;
                if (selector) {
                    if($(next).is(selector)) nextEls.push(next);
                }
                else nextEls.push(next);
                el = next;
            }
            return new Dom7(nextEls);
        },
        prev: function (selector) {
            if (this.length > 0) {
                if (selector) {
                    if (this[0].previousElementSibling && $(this[0].previousElementSibling).is(selector)) return new Dom7([this[0].previousElementSibling]);
                    else return new Dom7([]);
                }
                else {
                    if (this[0].previousElementSibling) return new Dom7([this[0].previousElementSibling]);
                    else return new Dom7([]);
                }
            }
            else return new Dom7([]);
        },
        prevAll: function (selector) {
            var prevEls = [];
            var el = this[0];
            if (!el) return new Dom7([]);
            while (el.previousElementSibling) {
                var prev = el.previousElementSibling;
                if (selector) {
                    if($(prev).is(selector)) prevEls.push(prev);
                }
                else prevEls.push(prev);
                el = prev;
            }
            return new Dom7(prevEls);
        },
        parent: function (selector) {
            var parents = [];
            for (var i = 0; i < this.length; i++) {
                if (selector) {
                    if ($(this[i].parentNode).is(selector)) parents.push(this[i].parentNode);
                }
                else {
                    parents.push(this[i].parentNode);
                }
            }
            return $($.unique(parents));
        },
        parents: function (selector) {
            var parents = [];
            for (var i = 0; i < this.length; i++) {
                var parent = this[i].parentNode;
                while (parent) {
                    if (selector) {
                        if ($(parent).is(selector)) parents.push(parent);
                    }
                    else {
                        parents.push(parent);
                    }
                    parent = parent.parentNode;
                }
            }
            return $($.unique(parents));
        },
        find : function (selector) {
            var foundElements = [];
            for (var i = 0; i < this.length; i++) {
                var found = this[i].querySelectorAll(selector);
                for (var j = 0; j < found.length; j++) {
                    foundElements.push(found[j]);
                }
            }
            return new Dom7(foundElements);
        },
        children: function (selector) {
            var children = [];
            for (var i = 0; i < this.length; i++) {
                var childNodes = this[i].childNodes;

                for (var j = 0; j < childNodes.length; j++) {
                    if (!selector) {
                        if (childNodes[j].nodeType === 1) children.push(childNodes[j]);
                    }
                    else {
                        if (childNodes[j].nodeType === 1 && $(childNodes[j]).is(selector)) children.push(childNodes[j]);
                    }
                }
            }
            return new Dom7($.unique(children));
        },
        remove: function () {
            for (var i = 0; i < this.length; i++) {
                if (this[i].parentNode) this[i].parentNode.removeChild(this[i]);
            }
            return this;
        },
        add: function () {
            var dom = this;
            var i, j;
            for (i = 0; i < arguments.length; i++) {
                var toAdd = $(arguments[i]);
                for (j = 0; j < toAdd.length; j++) {
                    dom[dom.length] = toAdd[j];
                    dom.length++;
                }
            }
            return dom;
        }
    };
    $.fn = Dom7.prototype;
    $.unique = function (arr) {
        var unique = [];
        for (var i = 0; i < arr.length; i++) {
            if (unique.indexOf(arr[i]) === -1) unique.push(arr[i]);
        }
        return unique;
    };

    return $;
})();
