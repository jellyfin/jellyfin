define(['layoutManager', 'browser', 'dom', 'css!./emby-input', 'registerElement'], function (layoutManager, browser, dom) {
    'use strict';

    var EmbyInputPrototype = Object.create(HTMLInputElement.prototype);

    var inputId = 0;
    var supportsFloatingLabel = false;

    if (Object.getOwnPropertyDescriptor && Object.defineProperty) {

        var descriptor = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value');

        // descriptor returning null in webos
        if (descriptor && descriptor.configurable) {
            var baseSetMethod = descriptor.set;
            descriptor.set = function (value) {
                baseSetMethod.call(this, value);

                this.dispatchEvent(new CustomEvent('valueset', {
                    bubbles: false,
                    cancelable: false
                }));
            };

            Object.defineProperty(HTMLInputElement.prototype, 'value', descriptor);
            supportsFloatingLabel = true;
        }
    }

    EmbyInputPrototype.createdCallback = function () {

        if (!this.id) {
            this.id = 'embyinput' + inputId;
            inputId++;
        } if (this.classList.contains('emby-input')) {
            return;
        }

        this.classList.add('emby-input');

        var parentNode = this.parentNode;
        var document = this.ownerDocument;
        var label = document.createElement('label');
        label.innerHTML = this.getAttribute('label') || '';
        label.classList.add('inputLabel');
        label.classList.add('inputLabelUnfocused');

        label.htmlFor = this.id;
        parentNode.insertBefore(label, this);
        this.labelElement = label;

        var div = document.createElement('div');
        div.classList.add('emby-input-selectionbar');
        parentNode.insertBefore(div, this.nextSibling);

        dom.addEventListener(this, 'focus', function () {
            onChange.call(this);
            label.classList.add('inputLabelFocused');
            label.classList.remove('inputLabelUnfocused');
        }, {
            passive: true
        });

        dom.addEventListener(this, 'blur', function () {
            onChange.call(this);
            label.classList.remove('inputLabelFocused');
            label.classList.add('inputLabelUnfocused');
        }, {
            passive: true
        });

        dom.addEventListener(this, 'change', onChange, {
            passive: true
        });
        dom.addEventListener(this, 'input', onChange, {
            passive: true
        });
        dom.addEventListener(this, 'valueset', onChange, {
            passive: true
        });
    };

    function onChange() {

        var label = this.labelElement;
        if (this.value) {
            label.classList.remove('inputLabel-float');
        } else {

            var instanceSupportsFloat = supportsFloatingLabel && this.type !== 'date' && this.type !== 'time';

            if (instanceSupportsFloat) {
                label.classList.add('inputLabel-float');
            }
        }
    }

    EmbyInputPrototype.attachedCallback = function () {

        this.labelElement.htmlFor = this.id;

        onChange.call(this);
    };

    EmbyInputPrototype.label = function (text) {
        this.labelElement.innerHTML = text;
    };

    document.registerElement('emby-input', {
        prototype: EmbyInputPrototype,
        extends: 'input'
    });
});