define(['layoutManager', 'browser', 'css!./emby-textarea', 'registerElement'], function (layoutManager, browser) {

    function autoGrow(textarea, maxLines) {
        var self = this;

        if (maxLines === undefined) {
            maxLines = 999;
        }

        /**
         * Calculates the vertical padding of the element
         * @param textarea
         * @returns {number}
         */
        self.getOffset = function (textarea) {
            var style = window.getComputedStyle(textarea, null),
                props = ['paddingTop', 'paddingBottom'],
                offset = 0;

            for (var i = 0; i < props.length; i++) {
                offset += parseInt(style[props[i]]);
            }
            return offset;
        };

        var offset;
        function reset() {
            textarea.rows = 1;
            offset = self.getOffset(textarea);
            self.rows = textarea.rows || 1;
            self.lineHeight = (textarea.scrollHeight / self.rows) - (offset / self.rows);
            self.maxAllowedHeight = (self.lineHeight * maxLines) - offset;
        }

        function autogrowFn() {
            if (!self.lineHeight || self.lineHeight <= 0) {
                reset();
            }
            if (self.lineHeight <= 0) {
                textarea.style.overflowY = 'scroll';
                textarea.style.height = 'auto';
                textarea.rows = 3;
                return;
            }
            var newHeight = 0, hasGrown = false;

            if ((textarea.scrollHeight - offset) > self.maxAllowedHeight) {
                textarea.style.overflowY = 'scroll';
                newHeight = self.maxAllowedHeight;
            }
            else {
                textarea.style.overflowY = 'hidden';
                textarea.style.height = 'auto';
                newHeight = textarea.scrollHeight/* - offset*/;
                hasGrown = true;
            }
            textarea.style.height = newHeight + 'px';
        }

        // Call autogrowFn() when textarea's value is changed
        textarea.addEventListener('input', autogrowFn);
        textarea.addEventListener('focus', autogrowFn);
        textarea.addEventListener('valueset', autogrowFn);

        autogrowFn();
    }

    var EmbyTextAreaPrototype = Object.create(HTMLTextAreaElement.prototype);

    var elementId = 0;
    var supportsFloatingLabel = false;

    if (Object.getOwnPropertyDescriptor && Object.defineProperty) {

        var descriptor = Object.getOwnPropertyDescriptor(HTMLTextAreaElement.prototype, 'value');

        // descriptor returning null in webos
        if (descriptor && descriptor.configurable) {
            var baseSetMethod = descriptor.set;
            descriptor.set = function (value) {
                baseSetMethod.call(this, value);

                this.dispatchEvent(new CustomEvent('valueset', {
                    bubbles: false,
                    cancelable: false
                }));
            }

            Object.defineProperty(HTMLTextAreaElement.prototype, 'value', descriptor);
            supportsFloatingLabel = true;
        }
    }

    EmbyTextAreaPrototype.createdCallback = function () {

        if (!this.id) {
            this.id = 'embytextarea' + elementId;
            elementId++;
        }
    };

    EmbyTextAreaPrototype.attachedCallback = function () {

        if (this.classList.contains('emby-textarea')) {
            return;
        }

        this.rows = 1;
        this.classList.add('emby-textarea');

        var parentNode = this.parentNode;
        var label = this.ownerDocument.createElement('label');
        label.innerHTML = this.getAttribute('label') || '';
        label.classList.add('textareaLabel');

        if (!supportsFloatingLabel || this.type == 'date') {
            label.classList.add('nofloat');
        }

        label.htmlFor = this.id;
        parentNode.insertBefore(label, this);

        var div = document.createElement('div');
        div.classList.add('emby-textarea-selectionbar');
        parentNode.insertBefore(div, this.nextSibling);

        this.addEventListener('focus', function () {
            label.classList.add('textareaLabelFocused');
            label.classList.remove('textareaLabelUnfocused');
        });
        this.addEventListener('blur', function () {
            label.classList.remove('textareaLabelFocused');
            label.classList.add('textareaLabelUnfocused');
        });

        this.label = function (text) {
            label.innerHTML = text;
        };

        new autoGrow(this);
    };

    document.registerElement('emby-textarea', {
        prototype: EmbyTextAreaPrototype,
        extends: 'textarea'
    });
});