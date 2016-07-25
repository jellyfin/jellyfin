define(['focusManager', 'css!./style.css', 'clearButtonStyle', 'paper-icon-button-light', 'material-icons'], function (focusManager) {

    function focus() {
        var selected = this.querySelector('.selected');

        if (selected) {
            focusManager.focus(selected);
        } else {
            focusManager.autoFocus(this, true);
        }
    }

    function getLetterButton(l) {
        return '<button data-value="' + l + '" class="clearButton alphaPickerButton">' + l + '</button>';
    }

    function render(element, options) {

        element.classList.add('alphaPicker');
        element.classList.add('focuscontainer-x');

        var html = '';
        var letters;

        html += '<div class="alphaPickerRow">';
        if (options.mode == 'keyboard') {
            // space_bar icon
            html += '<button data-value=" " is="paper-icon-button-light" class="alphaPickerButton autoSize">\
                <i class="md-icon">&#xE256;</i>\
            </button>';
        } else {
            letters = ['#'];
            html += letters.map(getLetterButton).join('');
        }

        letters = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'];
        html += letters.map(getLetterButton).join('');

        if (options.mode == 'keyboard') {
            // backspace icon
            html += '<button data-value="backspace" is="paper-icon-button-light" class="alphaPickerButton autoSize">\
                <i class="md-icon">&#xE14A;</i>\
            </button>';
            html += '</div>';

            letters = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'];
            html += '<div class="alphaPickerRow">';
            html += '<br/>';
            html += letters.map(getLetterButton).join('');
            html += '</div>';
        } else {
            html += '</div>';
        }

        element.innerHTML = html;

        element.classList.add('focusable');
        element.focus = focus;
    }

    function alphaPicker(options) {

        var self = this;

        var element = options.element;
        var itemsContainer = options.itemsContainer;
        var itemClass = options.itemClass;

        var itemFocusValue;
        var itemFocusTimeout;

        function onItemFocusTimeout() {
            itemFocusTimeout = null;
            self.value(itemFocusValue);
        }

        var alphaFocusedElement;
        var alphaFocusTimeout;

        function onAlphaFocusTimeout() {

            alphaFocusTimeout = null;

            if (document.activeElement == alphaFocusedElement) {
                var value = alphaFocusedElement.getAttribute('data-value');
                self.value(value, true);
            }
        }

        function parentWithClass(elem, className) {

            while (!elem.classList || !elem.classList.contains(className)) {
                elem = elem.parentNode;

                if (!elem) {
                    return null;
                }
            }

            return elem;
        }

        function onAlphaPickerInKeyboardModeClick(e) {

            var alphaPickerButton = parentWithClass(e.target, 'alphaPickerButton');

            if (alphaPickerButton) {
                var value = alphaPickerButton.getAttribute('data-value');

                element.dispatchEvent(new CustomEvent("alphavalueclicked", {
                    detail: {
                        value: value
                    }
                }));
            }
        }

        function onAlphaPickerClick(e) {

            var alphaPickerButton = parentWithClass(e.target, 'alphaPickerButton');

            if (alphaPickerButton) {
                var value = alphaPickerButton.getAttribute('data-value');

                if (currentValue == value.toUpperCase()) {
                    self.value(null, true);
                } else {
                    self.value(value, true);
                }
            }
        }

        function onAlphaPickerFocusIn(e) {

            if (alphaFocusTimeout) {
                clearTimeout(alphaFocusTimeout);
                alphaFocusTimeout = null;
            }

            var alphaPickerButton = parentWithClass(e.target, 'alphaPickerButton');

            if (alphaPickerButton) {
                alphaFocusedElement = alphaPickerButton;
                alphaFocusTimeout = setTimeout(onAlphaFocusTimeout, 100);
            }
        }

        function onItemsFocusIn(e) {

            var item = parentWithClass(e.target, itemClass);

            if (item) {
                var prefix = item.getAttribute('data-prefix');
                if (prefix && prefix.length) {

                    itemFocusValue = prefix[0];
                    if (itemFocusTimeout) {
                        clearTimeout(itemFocusTimeout);
                    }
                    itemFocusTimeout = setTimeout(onItemFocusTimeout, 100);
                }
            }
        }

        self.enabled = function (enabled) {

            if (enabled) {

                if (itemsContainer) {
                    itemsContainer.addEventListener('focus', onItemsFocusIn, true);
                }

                if (options.mode == 'keyboard') {
                    element.addEventListener('click', onAlphaPickerInKeyboardModeClick);
                }

                if (options.valueChangeEvent !== 'click') {
                    element.addEventListener('focus', onAlphaPickerFocusIn, true);
                } else {
                    element.addEventListener('click', onAlphaPickerClick);
                }

            } else {

                if (itemsContainer) {
                    itemsContainer.removeEventListener('focus', onItemsFocusIn, true);
                }

                element.removeEventListener('click', onAlphaPickerInKeyboardModeClick);
                element.removeEventListener('focus', onAlphaPickerFocusIn, true);
                element.removeEventListener('click', onAlphaPickerClick);
            }
        };

        self.on = function (name, fn) {
            element.addEventListener(name, fn);
        };

        self.off = function (name, fn) {
            element.removeEventListener(name, fn);
        };

        self.destroy = function () {

            self.enabled(false);
            element.classList.remove('focuscontainer-x');
        };

        self.visible = function (visible) {

            element.style.visibility = visible ? 'visible' : 'hidden';
        };

        var currentValue;
        self.value = function (value, applyValue) {

            var btn, selected;

            if (value !== undefined) {
                if (value != null) {

                    value = value.toUpperCase();
                    currentValue = value;

                    if (options.mode != 'keyboard') {
                        selected = element.querySelector('.selected');
                        btn = element.querySelector('.alphaPickerButton[data-value=\'' + value + '\']');

                        if (btn && btn != selected) {
                            btn.classList.add('selected');
                        }
                        if (selected && selected != btn) {
                            selected.classList.remove('selected');
                        }
                    }
                } else {
                    currentValue = value;

                    selected = element.querySelector('.selected');
                    if (selected) {
                        selected.classList.remove('selected');
                    }
                }
            }

            if (applyValue) {
                element.dispatchEvent(new CustomEvent("alphavaluechanged", {
                    detail: {
                        value: value
                    }
                }));
            }

            return currentValue;
        };

        self.values = function () {

            var elems = element.querySelectorAll('.alphaPickerButton');
            var values = [];
            for (var i = 0, length = elems.length; i < length; i++) {

                values.push(elems[i].getAttribute('data-value'));

            }

            return values;
        };

        self.focus = function () {
            focusManager.autoFocus(element, true);
        };

        render(element, options);

        self.enabled(true);
        self.visible(true);
    }

    return alphaPicker;
});