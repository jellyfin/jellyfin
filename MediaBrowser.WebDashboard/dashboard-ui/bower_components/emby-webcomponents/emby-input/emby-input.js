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
            label.classList.remove('focused');
        });

        this.addEventListener('change', onChange);
        this.addEventListener('keypress', onChange);
        this.addEventListener('keyup', onChange);

        onChange.call(this);

        if (window.IntersectionObserver) {
            var observer = new IntersectionObserver(function (entries) {
                for (var j = 0, length2 = entries.length; j < length2; j++) {
                    var entry = entries[j];
                    var intersectionRatio = entry.intersectionRatio;
                    if (intersectionRatio) {

                        var target = entry.target;
                        onChange.call(target);
                    }
                }
            }, {});

            observer.observe(this);
            this.observer = observer;
        }
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