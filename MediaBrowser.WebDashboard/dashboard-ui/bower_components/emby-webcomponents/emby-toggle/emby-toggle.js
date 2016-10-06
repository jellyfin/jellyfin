define(['css!./emby-toggle', 'registerElement'], function () {
    'use strict';

    var EmbyTogglePrototype = Object.create(HTMLInputElement.prototype);

    function onKeyDown(e) {

        // Don't submit form on enter
        if (e.keyCode === 13) {
            e.preventDefault();

            this.checked = !this.checked;

            this.dispatchEvent(new CustomEvent('change', {
                bubbles: true
            }));

            return false;
        }
    }

    EmbyTogglePrototype.attachedCallback = function () {

        if (this.getAttribute('data-embytoggle') === 'true') {
            return;
        }

        this.setAttribute('data-embytoggle', 'true');

        this.classList.add('mdl-switch__input');

        var labelElement = this.parentNode;
        labelElement.classList.add('mdl-switch');
        labelElement.classList.add('mdl-js-switch');

        var labelTextElement = labelElement.querySelector('span');

        labelElement.insertAdjacentHTML('beforeend', '<div class="mdl-switch__trackContainer"><div class="mdl-switch__track"></div><div class="mdl-switch__thumb"><span class="mdl-switch__focus-helper"></span></div></div>');

        labelTextElement.classList.add('toggleButtonLabel');
        labelTextElement.classList.add('mdl-switch__label');

        this.addEventListener('keydown', onKeyDown);
    };

    document.registerElement('emby-toggle', {
        prototype: EmbyTogglePrototype,
        extends: 'input'
    });
});