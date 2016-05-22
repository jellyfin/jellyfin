define(['layoutManager', 'browser', 'actionsheet', 'css!./emby-select'], function (layoutManager, browser, actionsheet) {

    var EmbySelectPrototype = Object.create(HTMLSelectElement.prototype);

    function enableNativeMenu() {

        // Take advantage of the native input methods
        if (browser.tv) {
            return true;
        }

        if (layoutManager.tv) {
            return false;
        }

        return true;
    }

    function showActionSheeet(select) {

        actionsheet.show({
            items: select.options,
            positionTo: select

        }).then(function (value) {
            select.value = value;
        });
    }

    function getLabel(select) {
        var elem = select.previousSibling;
        while (elem && elem.tagName != 'LABEL') {
            elem = elem.previousSibling;
        }
        return elem;
    }

    function onFocus(e) {
        var label = getLabel(this);
        if (label) {
            label.classList.add('selectLabelFocus');
        }
    }

    function onBlur(e) {
        var label = getLabel(this);
        if (label) {
            label.classList.remove('selectLabelFocus');
        }
    }

    function onMouseDown(e) {

        if (!enableNativeMenu()) {
            e.preventDefault();
            showActionSheeet(this);
        }
    }

    function onKeyDown(e) {

        switch (e.keyCode) {

            case 13:
                if (!enableNativeMenu()) {
                    e.preventDefault();
                    showActionSheeet(this);
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

    EmbySelectPrototype.createdCallback = function () {

        if (!this.id) {
            this.id = 'select' + new Date().getTime();
        }
        this.addEventListener('mousedown', onMouseDown);
        this.addEventListener('keydown', onKeyDown);
        this.addEventListener('focus', onFocus);
        this.addEventListener('keydown', onBlur);
    };

    EmbySelectPrototype.attachedCallback = function () {

        var label = this.ownerDocument.createElement('label');
        label.innerHTML = this.getAttribute('label') || '';
        label.classList.add('selectLabel');
        label.htmlFor = this.id;
        this.parentNode.insertBefore(label, this);

        var div = document.createElement('div');
        div.classList.add('emby-select-selectionbar');
        div.innerHTML = '<div class="emby-select-selectionbarInner"></div>';
        this.parentNode.insertBefore(div, this.nextSibling);
    };

    document.registerElement('emby-select', {
        prototype: EmbySelectPrototype,
        extends: 'select'
    });
});