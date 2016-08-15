define(['css!./appfooter'], function () {

    function render(options) {

        var elem = document.createElement('div');

        elem.classList.add('appfooter');

        document.body.appendChild(elem);

        return elem;
    }

    function initHeadRoom(instance, elem) {

        require(["headroom"], function () {

            // construct an instance of Headroom, passing the element
            var headroom = new Headroom(elem, {
                // or scroll tolerance per direction
                tolerance: {
                    down: 20,
                    up: 0
                },
                classes: {
                    pinned: 'appfooter--pinned',
                    unpinned: 'appfooter--unpinned',
                    top: 'appfooter--top',
                    notTop: 'appfooter--not-top',
                    initial: 'appfooter-headroom'
                }
            });
            // initialise
            headroom.init();

            instance.headroom = headroom;
        });
    }

    function dockedTabs(options) {

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

    dockedTabs.prototype.destroy = function () {
        var self = this;

        if (self.headroom) {
            self.headroom.destroy();
        }

        self.Element = null;
    };

    return dockedTabs;
});