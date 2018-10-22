define(["loading", "libraryBrowser", "cardBuilder", "apphost"], function(loading, libraryBrowser, cardBuilder, appHost) {
    "use strict";

    function getQuery(params) {
        var key = getSavedQueryKey(),
            pageData = data[key];
        return pageData || (pageData = data[key] = {
            query: {
                SortBy: "SortName",
                SortOrder: "Ascending",
                IncludeItemTypes: "Series",
                Recursive: !0,
                Fields: "DateCreated,PrimaryImageAspectRatio",
                StartIndex: 0
            }
        }, pageData.query.ParentId = params.topParentId), pageData.query
    }

    function getSavedQueryKey() {
        return libraryBrowser.getSavedQueryKey("studios")
    }

    function getPromise(context, params) {
        var query = getQuery(params);
        return loading.show(), ApiClient.getStudios(ApiClient.getCurrentUserId(), query)
    }

    function reloadItems(context, params, promise) {
        promise.then(function(result) {
            var elem = context.querySelector("#items");
            cardBuilder.buildCards(result.Items, {
                itemsContainer: elem,
                shape: "backdrop",
                preferThumb: !0,
                showTitle: !0,
                scalable: !0,
                centerText: !0,
                overlayMoreButton: !0,
                context: "tvshows"
            }), loading.hide()
        })
    }
    var data = {};
    return function(view, params, tabContent) {
        var promise, self = this;
        self.preRender = function() {
            promise = getPromise(view, params)
        }, self.renderTab = function() {
            reloadItems(tabContent, params, promise)
        }
    }
});