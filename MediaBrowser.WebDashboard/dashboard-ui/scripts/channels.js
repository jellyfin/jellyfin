define(['jQuery'], function ($) {

    // The base query options
    var query = {

        StartIndex: 0
    };

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        query.UserId = Dashboard.getCurrentUserId();

        ApiClient.getJSON(ApiClient.getUrl("Channels", query)).then(function (result) {

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

    pageIdOn('pageinit', "channelsPage", function () {

        var page = this;

        var tabs = page.querySelector('paper-tabs');
        var pageTabsContainer = page.querySelector('.pageTabsContainer');

        LibraryBrowser.configurePaperLibraryTabs(page, tabs, pageTabsContainer, 'channels.html');

        pageTabsContainer.addEventListener('tabchange', function (e) {
            loadTab(page, parseInt(e.detail.selectedTabIndex));
        });

    });

});