define(['cardBuilder', 'emby-itemscontainer'], function (cardBuilder) {

    return function (view, params, tabContent) {

        var self = this;
        var data = {};

        function getPageData(context) {
            var key = getSavedQueryKey(context);
            var pageData = data[key];

            if (!pageData) {
                pageData = data[key] = {
                    query: {
                        StartIndex: 0,
                        EnableFavoriteSorting: true,
                        Limit: LibraryBrowser.getDefaultPageSize()
                    }
                };

                LibraryBrowser.loadSavedQueryValues(key, pageData.query);
            }
            return pageData;
        }

        function getQuery(context) {

            return getPageData(context).query;
        }

        function getSavedQueryKey(context) {

            if (!context.savedQueryKey) {
                context.savedQueryKey = LibraryBrowser.getSavedQueryKey('channels');
            }
            return context.savedQueryKey;
        }

        function getChannelsHtml(channels) {

            return cardBuilder.getCardsHtml({
                items: channels,
                shape: "square",
                showTitle: true,
                lazy: true,
                cardLayout: true,
                showDetailsMenu: true
            });
        }

        function renderChannels(context, result) {

            var query = getQuery(context);

            context.querySelector('.paging').innerHTML = LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                showLimit: false,
                updatePageSizeSetting: false,
                filterButton: false
            });

            var html = getChannelsHtml(result.Items);

            var elem = context.querySelector('#items');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);

            var i, length;
            var elems;

            function onNextPageClick() {
                query.StartIndex += query.Limit;
                reloadItems(context);
            }

            function onPreviousPageClick() {
                query.StartIndex -= query.Limit;
                reloadItems(context);
            }

            elems = context.querySelectorAll('.btnNextPage');
            for (i = 0, length = elems.length; i < length; i++) {
                elems[i].addEventListener('click', onNextPageClick);
            }

            elems = context.querySelectorAll('.btnPreviousPage');
            for (i = 0, length = elems.length; i < length; i++) {
                elems[i].addEventListener('click', onPreviousPageClick);
            }

            LibraryBrowser.saveQueryValues(getSavedQueryKey(context), query);
        }

        function showFilterMenu(context) {

            require(['components/filterdialog/filterdialog'], function (filterDialogFactory) {

                var filterDialog = new filterDialogFactory({
                    query: getQuery(context),
                    mode: 'livetvchannels'
                });

                Events.on(filterDialog, 'filterchange', function () {
                    reloadItems(context);
                });

                filterDialog.show();
            });
        }

        function reloadItems(context) {

            Dashboard.showLoadingMsg();

            var query = getQuery(context);

            query.UserId = Dashboard.getCurrentUserId();

            ApiClient.getLiveTvChannels(query).then(function (result) {

                renderChannels(context, result);

                Dashboard.hideLoadingMsg();
            });
        }

        tabContent.querySelector('.btnFilter').addEventListener('click', function () {
            showFilterMenu(tabContent);
        });

        self.renderTab = function () {

            reloadItems(tabContent);
        };
    };

});