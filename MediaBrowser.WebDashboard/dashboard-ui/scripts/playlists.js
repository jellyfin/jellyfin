define(["loading", "listView", "cardBuilder", "libraryMenu", "libraryBrowser", "apphost", "imageLoader", "emby-itemscontainer"], function(loading, listView, cardBuilder, libraryMenu, libraryBrowser, appHost, imageLoader) {
    "use strict";
    return function(view, params) {
        function getPageData(context) {
            var key = getSavedQueryKey(context),
                pageData = data[key];
            return pageData || (pageData = data[key] = {
                query: {
                    SortBy: "SortName",
                    SortOrder: "Ascending",
                    IncludeItemTypes: "Playlist",
                    Recursive: !0,
                    Fields: "PrimaryImageAspectRatio,SortName,CumulativeRunTimeTicks,CanDelete",
                    StartIndex: 0,
                    Limit: 100
                },
                view: libraryBrowser.getSavedView(key) || "Poster"
            }, pageData.query.ParentId = libraryMenu.getTopParentId(), libraryBrowser.loadSavedQueryValues(key, pageData.query)), pageData
        }

        function getQuery(context) {
            return getPageData(context).query
        }

        function getSavedQueryKey(context) {
            return context.savedQueryKey || (context.savedQueryKey = libraryBrowser.getSavedQueryKey()), context.savedQueryKey
        }

        function showLoadingMessage() {
            loading.show()
        }

        function hideLoadingMessage() {
            loading.hide()
        }

        function onViewStyleChange() {
            var viewStyle = getPageData(view).view,
                itemsContainer = view.querySelector(".itemsContainer");
            "List" == viewStyle ? (itemsContainer.classList.add("vertical-list"), itemsContainer.classList.remove("vertical-wrap")) : (itemsContainer.classList.remove("vertical-list"), itemsContainer.classList.add("vertical-wrap")), itemsContainer.innerHTML = ""
        }

        function reloadItems() {
            showLoadingMessage();
            var query = getQuery(view),
                promise1 = ApiClient.getItems(Dashboard.getCurrentUserId(), query),
                promise2 = Dashboard.getCurrentUser();
            Promise.all([promise1, promise2]).then(function(responses) {
                var result = responses[0];
                responses[1];
                window.scrollTo(0, 0);
                var html = "",
                    viewStyle = getPageData(view).view;
                view.querySelector(".listTopPaging").innerHTML = libraryBrowser.getQueryPagingHtml({
                    startIndex: query.StartIndex,
                    limit: query.Limit,
                    totalRecordCount: result.TotalRecordCount,
                    viewButton: !1,
                    showLimit: !1,
                    updatePageSizeSetting: !1,
                    addLayoutButton: !0,
                    layouts: "List,Poster,PosterCard,Thumb,ThumbCard",
                    currentLayout: viewStyle
                }), result.TotalRecordCount ? (html = "List" == viewStyle ? listView.getListViewHtml({
                    items: result.Items,
                    sortBy: query.SortBy
                }) : "PosterCard" == viewStyle ? cardBuilder.getCardsHtml({
                    items: result.Items,
                    shape: "square",
                    coverImage: !0,
                    showTitle: !0,
                    cardLayout: !0,
                    vibrant: !0
                }) : "Thumb" == viewStyle ? cardBuilder.getCardsHtml({
                    items: result.Items,
                    shape: "backdrop",
                    showTitle: !0,
                    centerText: !0,
                    preferThumb: !0,
                    overlayPlayButton: !0
                }) : "ThumbCard" == viewStyle ? cardBuilder.getCardsHtml({
                    items: result.Items,
                    shape: "backdrop",
                    showTitle: !0,
                    preferThumb: !0,
                    cardLayout: !0,
                    vibrant: !0
                }) : cardBuilder.getCardsHtml({
                    items: result.Items,
                    shape: "square",
                    showTitle: !0,
                    coverImage: !0,
                    centerText: !0,
                    overlayPlayButton: !0
                }), view.querySelector(".noItemsMessage").classList.add("hide")) : view.querySelector(".noItemsMessage").classList.remove("hide");
                var elem = view.querySelector(".itemsContainer");
                elem.innerHTML = html, imageLoader.lazyChildren(elem);
                var btnNextPage = view.querySelector(".btnNextPage");
                btnNextPage && btnNextPage.addEventListener("click", function() {
                    query.StartIndex += query.Limit, reloadItems()
                });
                var btnPreviousPage = view.querySelector(".btnPreviousPage");
                btnPreviousPage && btnPreviousPage.addEventListener("click", function() {
                    query.StartIndex -= query.Limit, reloadItems()
                });
                var btnChangeLayout = view.querySelector(".btnChangeLayout");
                btnChangeLayout && btnChangeLayout.addEventListener("layoutchange", function(e) {
                    var layout = e.detail.viewStyle;
                    getPageData(view).view = layout, libraryBrowser.saveViewSetting(getSavedQueryKey(view), layout), onViewStyleChange(), reloadItems()
                }), libraryBrowser.saveQueryValues(getSavedQueryKey(view), query), hideLoadingMessage()
            })
        }
        var data = {};
        view.addEventListener("viewbeforeshow", function() {
            reloadItems()
        }), view.querySelector(".btnNewPlaylist").addEventListener("click", function() {
            require(["playlistEditor"], function(playlistEditor) {
                var serverId = ApiClient.serverInfo().Id;
                (new playlistEditor).show({
                    items: [],
                    serverId: serverId
                })
            })
        }), onViewStyleChange()
    }
});