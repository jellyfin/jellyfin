define(['libraryBrowser', 'cardBuilder', 'emby-itemscontainer'], function (libraryBrowser, cardBuilder) {

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

                html = cardBuilder.getCardsHtml({
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

                html = cardBuilder.getCardsHtml({
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

            libraryBrowser.saveQueryValues('channels', query);

            Dashboard.hideLoadingMsg();
        });
    }

    function loadTab(page, index) {

        switch (index) {

            case 1:
                libraryBrowser.loadSavedQueryValues('channels', query);
                reloadItems(page);
                break;
            default:
                break;
        }
    }

    pageIdOn('pageinit', "channelsPage", function () {

        var page = this;

        var mdlTabs = page.querySelector('.libraryViewNav');

        libraryBrowser.configurePaperLibraryTabs(page, mdlTabs, page.querySelectorAll('.pageTabContent'), [0, 1]);

        mdlTabs.addEventListener('tabchange', function (e) {
            loadTab(page, parseInt(e.detail.selectedTabIndex));
        });

    });

});