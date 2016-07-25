define(['libraryBrowser'], function (libraryBrowser) {

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
                    IncludeItemTypes: "Movie",
                    Recursive: true,
                    Fields: "DateCreated,ItemCounts",
                    StartIndex: 0
                }
            };

            pageData.query.ParentId = params.topParentId;
        }
        return pageData.query;
    }

    function getSavedQueryKey() {

        return libraryBrowser.getSavedQueryKey('studios');
    }

    function getPromise(context, params) {

        var query = getQuery(params);

        Dashboard.showLoadingMsg();

        return ApiClient.getStudios(Dashboard.getCurrentUserId(), query);
    }
    function reloadItems(context, params, promise) {

        promise.then(function (result) {

            var html = '';

            html += libraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "backdrop",
                showTitle: false,
                context: 'movies',
                preferThumb: true,
                showItemCounts: true,
                centerText: true,
                lazy: true

            });

            var elem = context.querySelector('#items');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);

            Dashboard.hideLoadingMsg();
        });
    }
    return function (view, params, tabContent) {

        var self = this;
        var promise;

        self.preRender = function () {
            promise = getPromise(view, params);
        };

        self.renderTab = function () {

            reloadItems(tabContent, params, promise);
        };
    };
});