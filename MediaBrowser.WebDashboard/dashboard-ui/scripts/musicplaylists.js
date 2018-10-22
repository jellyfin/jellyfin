define(["libraryBrowser", "cardBuilder", "apphost", "imageLoader", "loading"], function(libraryBrowser, cardBuilder, appHost, imageLoader, loading) {
    "use strict";
    return function(view, params, tabContent) {
        function getPageData() {
            var key = getSavedQueryKey(),
                pageData = data[key];
            return pageData || (pageData = data[key] = {
                query: {
                    SortBy: "SortName",
                    SortOrder: "Ascending",
                    IncludeItemTypes: "Playlist",
                    Recursive: !0,
                    Fields: "PrimaryImageAspectRatio,SortName,CanDelete",
                    StartIndex: 0
                },
                view: libraryBrowser.getSavedView(key) || "Poster"
            }, pageData.query.ParentId = params.topParentId, libraryBrowser.loadSavedQueryValues(key, pageData.query)), pageData
        }

        function getQuery() {
            return getPageData().query
        }

        function getSavedQueryKey() {
            return libraryBrowser.getSavedQueryKey("genres")
        }

        function getPromise() {
            loading.show();
            var query = getQuery();
            return ApiClient.getItems(ApiClient.getCurrentUserId(), query)
        }

        function reloadItems(context, promise) {
            var query = getQuery();
            promise.then(function(result) {
                var html = "";
                html = cardBuilder.getCardsHtml({
                    items: result.Items,
                    shape: "square",
                    showTitle: !0,
                    coverImage: !0,
                    centerText: !0,
                    overlayPlayButton: !0,
                    allowBottomPadding: !0,
                    cardLayout: !1,
                    vibrant: !1
                });
                var elem = context.querySelector("#items");
                elem.innerHTML = html, imageLoader.lazyChildren(elem), libraryBrowser.saveQueryValues(getSavedQueryKey(), query), loading.hide()
            })
        }
        var self = this,
            data = {};
        self.getCurrentViewStyle = function() {
            return getPageData(tabContent).view
        };
        var promise;
        self.preRender = function() {
            promise = getPromise()
        }, self.renderTab = function() {
            reloadItems(tabContent, promise)
        }
    }
});