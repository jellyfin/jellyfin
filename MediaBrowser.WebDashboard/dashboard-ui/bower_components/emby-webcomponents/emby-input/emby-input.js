define(['layoutManager', 'browser', 'css!./emby-input', 'registerElement'], function (layoutManager, browser) {

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
            }

            Object.defineProperty(HTMLInputElement.prototype, 'value', descriptor);
            supportsFloatingLabel = true;
        }
    }

    EmbyInputPrototype.createdCallback = function () {

        if (!this.id) {
            this.id = 'embyinput' + inputId;
            inputId++;
        }
    };

    EmbyInputPrototype.attachedCallback = function () {

        if (this.classList.contains('emby-input')) {
            return;
        }

        this.classList.add('emby-input');

        var parentNode = this.parentNode;
        var label = this.ownerDocument.createElement('label');
        label.innerHTML = this.getAttribute('label') || '';
        label.classList.add('inputLabel');

        var instanceSupportsFloat = supportsFloatingLabel && this.type != 'date';

        label.htmlFor = this.id;
        parentNode.insertBefore(label, this);

        var div = document.createElement('div');
        div.classList.add('emby-input-selectionbar');
        parentNode.insertBefore(div, this.nextSibling);

        function onChange() {
            if (this.value) {
                label.classList.remove('inputLabel-float');
            } else {

                if (instanceSupportsFloat) {
                    label.classList.add('inputLabel-float');
                }
            }
        }

        this.addEventListener('focus', function () {
            onChange.call(this);
            label.classList.add('inputLabelFocused');
            label.classList.remove('inputLabelUnfocused');
        });
        this.addEventListener('blur', function () {
            onChange.call(this);
            label.classList.remove('inputLabelFocused');
            label.classList.add('inputLabelUnfocused');
        });

        this.addEventListener('change', onChange);
        this.addEventListener('input', onChange);
        this.addEventListener('valueset', onChange);

        onChange.call(this);

        this.label = function (text) {
            label.innerHTML = text;
        };
    };

    document.registerElement('emby-input', {
        prototype: EmbyInputPrototype,
        extends: 'input'
    });
});