define(['libraryBrowser', 'cardBuilder'], function (libraryBrowser, cardBuilder) {

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
                    Fields: "DateCreated,ItemCounts,PrimaryImageAspectRatio",
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

            var elem = context.querySelector('#items');
            cardBuilder.buildCards(result.Items, {
                itemsContainer: elem,
                shape: "backdrop",
                preferThumb: true,
                showTitle: false,
                scalable: true,
                showItemCounts: true,
                centerText: true,
                overlayMoreButton: true
            });

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