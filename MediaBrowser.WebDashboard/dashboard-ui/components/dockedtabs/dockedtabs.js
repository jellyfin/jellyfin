define(['css!./dockedtabs'], function () {

    function render(options) {

        var elem = document.createElement('div');

        elem.classList.add('dockedtabs');
        elem.classList.add('dockedtabs-bottom');

        // tabs: 
        // home
        // favorites
        // live tv
        // now playing

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
                    pinned: 'dockedtabs--pinned',
                    unpinned: 'dockedtabs--unpinned',
                    top: 'dockedtabs--top',
                    notTop: 'dockedtabs--not-top',
                    initial: 'dockedtabs-headroom'
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

        initHeadRoom(self, self.element);
    }

    dockedTabs.prototype.destroy = function() {
        var self = this;

        if (self.headroom) {
            self.headroom.destroy();
        }

        self.Element = null;
    };

    return dockedTabs;
});