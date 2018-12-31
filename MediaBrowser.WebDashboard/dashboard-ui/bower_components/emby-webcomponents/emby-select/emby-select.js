define(['layoutManager', 'browser', 'actionsheet', 'css!./emby-select', 'registerElement'], function (layoutManager, browser, actionsheet) {
    'use strict';

    var EmbySelectPrototype = Object.create(HTMLSelectElement.prototype);

    function enableNativeMenu() {

        if (browser.edgeUwp || browser.xboxOne) {
            return true;
        }

        // Doesn't seem to work at all
        if (browser.tizen || browser.orsay || browser.web0s) {
            return false;
        }

        // Take advantage of the native input methods
        if (browser.tv) {
            return true;
        }

        if (layoutManager.tv) {
            return false;
        }

        return true;
    }

    function triggerChange(select) {
        var evt = document.createEvent("HTMLEvents");
        evt.initEvent("change", false, true);
        select.dispatchEvent(evt);
    }

    function setValue(select, value) {

        select.value = value;
    }

    function showActionSheet(select) {

        var labelElem = getLabel(select);
        var title = labelElem ? (labelElem.textContent || labelElem.innerText) : null;

        actionsheet.show({
            items: select.options,
            positionTo: select,
            title: title

        }).then(function (value) {
            setValue(select, value);
            triggerChange(select);
        });
    }

    function getLabel(select) {
        var elem = select.previousSibling;
        while (elem && elem.tagName !== 'LABEL') {
            elem = elem.previousSibling;
        }
        return elem;
    }

    function onFocus(e) {
        var label = getLabel(this);
        if (label) {
            label.classList.add('selectLabelFocused');
        }
    }

    function onBlur(e) {
        var label = getLabel(this);
        if (label) {
            label.classList.remove('selectLabelFocused');
        }
    }

    function onMouseDown(e) {

        // e.button=0 for primary (left) mouse button click
        if (!e.button && !enableNativeMenu()) {
            e.preventDefault();
            showActionSheet(this);
        }
    }

    function onKeyDown(e) {

        switch (e.keyCode) {

            case 13:
                if (!enableNativeMenu()) {
                    e.preventDefault();
                    showActionSheet(this);
                }
                return;
            case 37:
            case 38:
            case 39:
            case 40:
                if (layoutManager.tv) {
                    e.preventDefault();
                }
                return;
            default:
                break;
        }
    }

    var inputId = 0;

    EmbySelectPrototype.createdCallback = function () {

        if (!this.id) {
            this.id = 'embyselect' + inputId;
            inputId++;
        }

        if (!browser.firefox) {
            this.classList.add('emby-select-withcolor');

			if (layoutManager.tv) {
                this.classList.add('emby-select-tv-withcolor');
			}
        }

        if (layoutManager.tv) {
            this.classList.add('emby-select-focusscale');
        }

        this.addEventListener('mousedown', onMouseDown);
        this.addEventListener('keydown', onKeyDown);

        this.addEventListener('focus', onFocus);
        this.addEventListener('blur', onBlur);
    };

    EmbySelectPrototype.attachedCallback = function () {

        if (this.classList.contains('emby-select')) {
            return;
        }

        this.classList.add('emby-select');

        var label = this.ownerDocument.createElement('label');
        label.innerHTML = this.getAttribute('label') || '';
        label.classList.add('selectLabel');
        label.htmlFor = this.id;
        this.parentNode.insertBefore(label, this);

        if (this.classList.contains('emby-select-withcolor')) {
            this.parentNode.insertAdjacentHTML('beforeend', '<div class="selectArrowContainer"><div style="visibility:hidden;">0</div><i class="selectArrow md-icon">&#xE313;</i></div>');
        }
    };

    EmbySelectPrototype.setLabel = function (text) {

        var label = this.parentNode.querySelector('label');

        label.innerHTML = text;
    };

    document.registerElement('emby-select', {
        prototype: EmbySelectPrototype,
        extends: 'select'
    });
});