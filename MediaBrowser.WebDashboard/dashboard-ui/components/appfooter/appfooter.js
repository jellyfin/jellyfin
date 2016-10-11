define(['browser', 'css!./appfooter'], function (browser) {

    function render(options) {

        var elem = document.createElement('div');

        elem.classList.add('appfooter');

        if (browser.safari) {
            elem.classList.add('appfooter-blurred');
        }

        document.body.appendChild(elem);

        return elem;
    }

    function initHeadRoom(instance, elem) {

        require(["headroom-window"], function (headroom) {

            self.headroom = headroom;
            headroom.add(elem);
        });
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

        initHeadRoom(self, self.element);
    }

    appFooter.prototype.destroy = function () {
        var self = this;

        if (self.headroom) {
            self.headroom.remove(self.element);
            self.headroom = null;
        }

        self.element = null;
    };

    return appFooter;
});