define(['layoutManager', 'browser', 'css!./emby-input'], function (layoutManager, browser) {

    var EmbyInputPrototype = Object.create(HTMLInputElement.prototype);

    var inputId = 0;

    if (Object.getOwnPropertyDescriptor && Object.defineProperty) {

        var descriptor = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value');

        if (descriptor.configurable) {
            var baseSetMethod = descriptor.set;
            descriptor.set = function (value) {
                baseSetMethod.call(this, value);
                this.dispatchEvent(new CustomEvent('valueset', {
                    bubbles: false,
                    cancelable: false
                }));
            }

            Object.defineProperty(HTMLInputElement.prototype, 'value', descriptor);
        }
    }

    EmbyInputPrototype.createdCallback = function () {

        if (!this.id) {
            this.id = 'embyinput' + inputId;
            inputId++;
        }
    };

    EmbyInputPrototype.attachedCallback = function () {

        if (this.getAttribute('data-embyinput') == 'true') {
            return;
        }

        this.setAttribute('data-embyinput', 'true');

        var parentNode = this.parentNode;
        var label = this.ownerDocument.createElement('label');
        label.innerHTML = this.getAttribute('label') || '';
        label.classList.add('inputLabel');

        label.htmlFor = this.id;
        parentNode.insertBefore(label, this);

        var div = document.createElement('div');
        div.classList.add('emby-input-selectionbar');
        parentNode.insertBefore(div, this.nextSibling);

        function onChange() {
            if (this.value) {
                label.classList.remove('blank');
            } else {
                label.classList.add('blank');
            }
        }

        this.addEventListener('focus', function () {
            onChange.call(this);
            label.classList.add('focused');
        });
        this.addEventListener('blur', function () {
            onChange.call(this);
            label.classList.remove('focused');
        });

        this.addEventListener('change', onChange);
        this.addEventListener('input', onChange);
        this.addEventListener('valueset', onChange);

        onChange.call(this);
    };

    EmbyInputPrototype.detachedCallback = function () {

        var observer = this.observer;
        if (observer) {
            observer.disconnect();
            this.observer = null;
        }
    };

    document.registerElement('emby-input', {
        prototype: EmbyInputPrototype,
        extends: 'input'
    });
});