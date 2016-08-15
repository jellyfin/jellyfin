define(['css!./dockedtabs', 'emby-tabs'], function () {

    function render(options) {

        var elem = document.createElement('div');

        elem.classList.add('dockedtabs');
        elem.classList.add('dockedtabs-bottom');

        // tabs: 
        // home
        // favorites
        // live tv
        // now playing

        var html = '';

        html += '    <div is="emby-tabs" class="dockedtabs-tabs" data-selectionbar="false">\
            <button is="emby-button" class="dockedtabs-tab-button emby-tab-button emby-tab-button-active" data-index="0">\
                <div class="dockedtabs-tab-button-foreground emby-button-foreground"><i class="dockedtabs-tab-button-icon md-icon">home</i><div>Home</div></div>\
            </button>\
            <button is="emby-button" class="dockedtabs-tab-button emby-tab-button" data-index="1">\
                <div class="dockedtabs-tab-button-foreground emby-button-foreground"><i class="dockedtabs-tab-button-icon md-icon">dvr</i><div>Live TV</div></div>\
            </button>\
            <button is="emby-button" class="dockedtabs-tab-button emby-tab-button homeFavoritesTab" data-index="2">\
                <div class="dockedtabs-tab-button-foreground emby-button-foreground"><i class="dockedtabs-tab-button-icon md-icon">favorite</i><div>Favorites</div></div>\
            </button>\
            <button is="emby-button" class="dockedtabs-tab-button emby-tab-button" data-index="3">\
                <div class="dockedtabs-tab-button-foreground emby-button-foreground"><i class="dockedtabs-tab-button-icon md-icon">playlist_play</i><div>Now Playing</div></div>\
            </button>\
            <button is="emby-button" class="dockedtabs-tab-button emby-tab-button" data-index="3">\
                <div class="dockedtabs-tab-button-foreground emby-button-foreground"><i class="dockedtabs-tab-button-icon md-icon">menu</i><div>More</div></div>\
            </button>\
    </div>\
';

        elem.innerHTML = html;

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