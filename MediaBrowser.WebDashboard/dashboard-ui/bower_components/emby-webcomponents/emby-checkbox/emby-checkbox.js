define(['css!./emby-checkbox', 'registerElement'], function () {

    var EmbyCheckboxPrototype = Object.create(HTMLInputElement.prototype);

    function onKeyDown(e) {

        // Don't submit form on enter
        if (e.keyCode == 13) {
            e.preventDefault();

            this.checked = !this.checked;

            return false;
        }
    }

    EmbyCheckboxPrototype.attachedCallback = function () {

        if (this.getAttribute('data-embycheckbox') == 'true') {
            return;
        }

        this.setAttribute('data-embycheckbox', 'true');

        this.classList.add('mdl-checkbox__input');

        var labelElement = this.parentNode;
        //labelElement.classList.add('mdl-checkbox mdl-js-checkbox mdl-js-ripple-effect mdl-js-ripple-effect--ignore-events');
        labelElement.classList.add('mdl-checkbox');
        labelElement.classList.add('mdl-js-checkbox');
        labelElement.classList.add('mdl-js-ripple-effect');

        var labelTextElement = labelElement.querySelector('span');

        var outlineClass = 'checkboxOutline';

        var customClass = this.getAttribute('data-outlineclass');
        if (customClass) {
            outlineClass += ' ' + customClass;
        }

        labelElement.insertAdjacentHTML('beforeend', '<span class="mdl-checkbox__focus-helper"></span><span class="' + outlineClass + '"><span class="mdl-checkbox__tick-outline"></span></span>');

        labelTextElement.classList.add('checkboxLabel');

        this.addEventListener('keydown', onKeyDown);
    };

    document.registerElement('emby-checkbox', {
        prototype: EmbyCheckboxPrototype,
        extends: 'input'
    });
});