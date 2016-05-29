define(['layoutManager', 'browser', 'css!./emby-input'], function (layoutManager, browser) {

    var EmbyInputPrototype = Object.create(HTMLInputElement.prototype);

    EmbyInputPrototype.createdCallback = function () {

        if (!this.id) {
            this.id = 'input' + new Date().getTime();
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

        label.classList.add('inputLabelUnfocused');
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
            label.classList.add('inputLabelFocused');
            label.classList.remove('inputLabelUnfocused');
        });
        this.addEventListener('blur', function () {
            label.classList.add('inputLabelUnfocused');
            label.classList.remove('inputLabelFocused');
        });

        this.addEventListener('change', onChange);
        this.addEventListener('keypress', onChange);
        this.addEventListener('keyup', onChange);

        onChange.call(this);
    };

    document.registerElement('emby-input', {
        prototype: EmbyInputPrototype,
        extends: 'input'
    });
});