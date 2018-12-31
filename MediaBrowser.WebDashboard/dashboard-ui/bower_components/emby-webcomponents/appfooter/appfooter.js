define(['browser', 'css!./appfooter'], function (browser) {
    'use strict';

    function render(options) {

        var elem = document.createElement('div');

        elem.classList.add('appfooter');

        if (!browser.chrome) {
            // chrome does not display this properly
            elem.classList.add('appfooter-blurred');
        }

        document.body.appendChild(elem);

        return elem;
    }

    function appFooter(options) {

        var self = this;

        self.element = render(options);

        self.add = function (elem) {
            self.element.appendChild(elem);
        };

        self.insert = function (elem) {
            if (typeof elem === 'string') {
                self.element.insertAdjacentHTML('afterbegin', elem);
            } else {
                self.element.insertBefore(elem, self.element.firstChild);
            }
        };
    }

    appFooter.prototype.destroy = function () {
        var self = this;

        self.element = null;
    };

    return appFooter;
});