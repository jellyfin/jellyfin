define(['jQuery'], function ($) {

    // The base query options
    var data = {};

    function getQuery(params) {

        var key = getSavedQueryKey();
        var pageData = data[key];

        if (!pageData) {
            pageData = data[key] = {
                query: {
                    SortBy: "SortName",
                    SortOrder: "Ascending",
                    IncludeItemTypes: "Series",
                    Recursive: true,
                    Fields: "DateCreated,ItemCounts",
                    StartIndex: 0
                }
            };

            pageData.query.ParentId = params.topParentId;
            LibraryBrowser.loadSavedQueryValues(key, pageData.query);
        }
        return pageData.query;
    }

    function getSavedQueryKey() {

        return LibraryBrowser.getSavedQueryKey('studios');
    }

    function reloadItems(context, params) {

        var query = getQuery(params);

        Dashboard.showLoadingMsg();

        ApiClient.getStudios(Dashboard.getCurrentUserId(), query).then(function (result) {

            var html = '';

            html += LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "backdrop",
                showTitle: false,
                context: 'tv',
                preferThumb: true,
                showItemCounts: true,
                centerText: true,
                lazy: true

            });

            var elem = context.querySelector('#items');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);

            LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);
            Dashboard.hideLoadingMsg();
        });
    }
    return function (view, params, tabContent) {

        var self = this;

        self.renderTab = function () {

            reloadItems(tabContent, params);
        };
    };
});