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
                <div class="dockedtabs-tab-button-foreground emby-button-foreground"><i class="dockedtabs-tab-button-icon md-icon">dvr</i><div>Libraries</div></div>\
            </button>\
            <button is="emby-button" class="dockedtabs-tab-button emby-tab-button homeFavoritesTab" data-index="2">\
                <div class="dockedtabs-tab-button-foreground emby-button-foreground"><i class="dockedtabs-tab-button-icon md-icon">favorite</i><div>Favorites</div></div>\
            </button>\
            <button is="emby-button" class="dockedtabs-tab-button emby-tab-button" data-index="3">\
                <div class="dockedtabs-tab-button-foreground emby-button-foreground"><i class="dockedtabs-tab-button-icon md-icon">file_download</i><div>Downloads</div></div>\
            </button>\
            <button is="emby-button" class="dockedtabs-tab-button emby-tab-button" data-index="3">\
                <div class="dockedtabs-tab-button-foreground emby-button-foreground"><i class="dockedtabs-tab-button-icon md-icon">menu</i><div>More</div></div>\
            </button>\
    </div>\
';

        elem.innerHTML = html;

        options.appFooter.add(elem);

        return elem;
    }

    function dockedTabs(options) {

        var self = this;

        self.element = render(options);
    }

    dockedTabs.prototype.destroy = function () {
        var self = this;

        self.Element = null;
    };

    return dockedTabs;
});