define(['libraryBrowser', 'cardBuilder', 'lazyLoader', 'apphost', 'globalize', 'dom'], function (libraryBrowser, cardBuilder, lazyLoader, appHost, globalize, dom) {
    'use strict';

    return function (view, params, tabContent) {

        var self = this;

        var data = {};
        function getPageData() {
            var key = getSavedQueryKey();
            var pageData = data[key];

            if (!pageData) {
                pageData = data[key] = {
                    query: {
                        SortBy: "SortName",
                        SortOrder: "Ascending",
                        IncludeItemTypes: "Series",
                        Recursive: true,
                        EnableTotalRecordCount: false
                    },
                    view: libraryBrowser.getSavedView(key) || (appHost.preferVisualCards ? 'PosterCard' : 'Poster')
                };

                pageData.query.ParentId = params.topParentId;
                libraryBrowser.loadSavedQueryValues(key, pageData.query);
            }
            return pageData;
        }

        function getQuery() {

            return getPageData().query;
        }

        function getSavedQueryKey() {

            return libraryBrowser.getSavedQueryKey('seriesgenres');
        }

        function getPromise() {

            Dashboard.showLoadingMsg();
            var query = getQuery();

            return ApiClient.getGenres(Dashboard.getCurrentUserId(), query);
        }

        function enableScrollX() {
            return browserInfo.mobile && AppInfo.enableAppLayouts;
        }

        function getThumbShape() {
            return enableScrollX() ? 'overflowBackdrop' : 'backdrop';
        }

        function getPortraitShape() {
            return enableScrollX() ? 'overflowPortrait' : 'portrait';
        }

        function getMoreItemsHref(itemId, type) {

            return 'secondaryitems.html?type=' + type + '&parentId=' + itemId;
        }

        dom.addEventListener(tabContent, 'click', function (e) {

            var btnMoreFromGenre = dom.parentWithClass(e.target, 'btnMoreFromGenre');
            if (btnMoreFromGenre) {
                var id = btnMoreFromGenre.getAttribute('data-id');
                Dashboard.navigate(getMoreItemsHref(id, 'Series'));
            }

        }, {
            passive: true
        });

        function fillItemsContainer(elem) {

            var id = elem.getAttribute('data-id');

            var viewStyle = self.getCurrentViewStyle();

            var limit = viewStyle == 'Thumb' || viewStyle == 'ThumbCard' ?
                5 :
                8;

            if (enableScrollX()) {
                limit = 10;
            }

            var enableImageTypes = viewStyle == 'Thumb' || viewStyle == 'ThumbCard' ?
              "Primary,Backdrop,Thumb" :
              "Primary";

            var query = {
                SortBy: "SortName",
                SortOrder: "Ascending",
                IncludeItemTypes: "Series",
                Recursive: true,
                Fields: "PrimaryImageAspectRatio,MediaSourceCount,BasicSyncInfo",
                ImageTypeLimit: 1,
                EnableImageTypes: enableImageTypes,
                Limit: limit,
                GenreIds: id,
                EnableTotalRecordCount: false,
                ParentId: params.topParentId
            };

            ApiClient.getItems(Dashboard.getCurrentUserId(), query).then(function (result) {

                var supportsImageAnalysis = appHost.supports('imageanalysis');

                if (viewStyle == "Thumb") {
                    cardBuilder.buildCards(result.Items, {
                        itemsContainer: elem,
                        shape: getThumbShape(),
                        preferThumb: true,
                        showTitle: true,
                        scalable: true,
                        centerText: true,
                        overlayMoreButton: true,
                        allowBottomPadding: false
                    });
                }
                else if (viewStyle == "ThumbCard") {

                    cardBuilder.buildCards(result.Items, {
                        itemsContainer: elem,
                        shape: getThumbShape(),
                        preferThumb: true,
                        showTitle: true,
                        scalable: true,
                        centerText: false,
                        cardLayout: true,
                        vibrant: supportsImageAnalysis,
                        showSeriesYear: true
                    });
                }
                else if (viewStyle == "PosterCard") {
                    cardBuilder.buildCards(result.Items, {
                        itemsContainer: elem,
                        shape: getPortraitShape(),
                        showTitle: true,
                        scalable: true,
                        centerText: false,
                        cardLayout: true,
                        vibrant: supportsImageAnalysis,
                        showSeriesYear: true
                    });
                }
                else if (viewStyle == "Poster") {
                    cardBuilder.buildCards(result.Items, {
                        itemsContainer: elem,
                        shape: getPortraitShape(),
                        showTitle: true,
                        scalable: true,
                        centerText: true,
                        overlayMoreButton: true,
                        allowBottomPadding: false
                    });
                }

                if (result.Items.length >= query.Limit) {
                    tabContent.querySelector('.btnMoreFromGenre' + id).classList.remove('hide');
                }
            });
        }

        function reloadItems(context, promise) {

            var query = getQuery();

            promise.then(function (result) {

                var elem = context.querySelector('#items');
                var html = '';

                var items = result.Items;

                for (var i = 0, length = items.length; i < length; i++) {

                    var item = items[i];

                    html += '<div class="homePageSection">';

                    html += '<div style="display:flex;align-items:center;">';
                    html += '<h1 class="listHeader">';
                    html += item.Name;
                    html += '</h1>';
                    html += '<button is="emby-button" type="button" class="raised more mini noIcon hide btnMoreFromGenre btnMoreFromGenre' + item.Id + '" data-id="' + item.Id + '">';
                    html += '<span>' + globalize.translate('ButtonMore') + '</span>';
                    html += '</button>';
                    html += '</div>';

                    if (enableScrollX()) {
                        html += '<div is="emby-itemscontainer" class="itemsContainer hiddenScrollX lazy" data-id="' + item.Id + '">';
                    } else {
                        html += '<div is="emby-itemscontainer" class="itemsContainer vertical-wrap lazy" data-id="' + item.Id + '">';
                    }
                    html += '</div>';

                    html += '</div>';
                }

                elem.innerHTML = html;

                lazyLoader.lazyChildren(elem, fillItemsContainer);

                libraryBrowser.saveQueryValues(getSavedQueryKey(), query);

                Dashboard.hideLoadingMsg();
            });
        }
        self.getViewStyles = function () {
            return 'Poster,PosterCard,Thumb,ThumbCard'.split(',');
        };

        self.getCurrentViewStyle = function () {
            return getPageData(tabContent).view;
        };

        self.setCurrentViewStyle = function (viewStyle) {
            getPageData(tabContent).view = viewStyle;
            libraryBrowser.saveViewSetting(getSavedQueryKey(tabContent), viewStyle);
            fullyReload();
        };

        self.enableViewSelection = true;
        var promise;

        self.preRender = function () {
            promise = getPromise();
        };

        self.renderTab = function () {

            reloadItems(tabContent, promise);
        };

        function fullyReload() {
            self.preRender();
            self.renderTab();
        }
    };
});