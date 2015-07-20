(function ($, document) {

    // The base query options
    var query = {

        StartIndex: 0
    };

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        query.UserId = Dashboard.getCurrentUserId();

        ApiClient.getJSON(ApiClient.getUrl("Channels", query)).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            window.scrollTo(0, 0);

            var html = '';

            var view = 'Thumb';

            if (view == "Thumb") {

                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "backdrop",
                    context: 'channels',
                    showTitle: true,
                    lazy: true,
                    centerText: true,
                    preferThumb: true
                });

            }
            else if (view == "ThumbCard") {

                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "backdrop",
                    preferThumb: true,
                    context: 'channels',
                    lazy: true,
                    cardLayout: true,
                    showTitle: true
                });
            }

            var elem = page.querySelector('#items');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);

            LibraryBrowser.saveQueryValues('channels', query);

            Dashboard.hideLoadingMsg();
        });
    }

    function loadTab(page, index) {

        switch (index) {

            case 1:
                LibraryBrowser.loadSavedQueryValues('channels', query);
                reloadItems(page);
                break;
            default:
                break;
        }
    }

    $(document).on('pageinitdepends', "#channelsPage", function () {

        var page = this;

        var tabs = page.querySelector('paper-tabs');
        var pages = page.querySelector('neon-animated-pages');

        LibraryBrowser.configurePaperLibraryTabs(page, tabs, pages, 1);

        $(tabs).on('iron-select', function () {
            var selected = this.selected;

            if (LibraryBrowser.navigateOnLibraryTabSelect()) {

                if (selected) {
                    Dashboard.navigate('channels.html?tab=' + selected);
                } else {
                    Dashboard.navigate('channels.html');
                }

            } else {
                page.querySelector('neon-animated-pages').selected = selected;
            }
        });

        $(pages).on('tabchange', function () {
            loadTab(page, parseInt(this.selected));
        });

    });

})(jQuery, document);