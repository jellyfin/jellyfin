define(['jQuery', 'emby-itemscontainer'], function ($) {

    var data = {};

    function getPageData(context) {
        var key = getSavedQueryKey(context);
        var pageData = data[key];

        if (!pageData) {
            pageData = data[key] = {
                query: {
                    SortBy: "",
                    SortOrder: "Ascending",
                    Fields: "PrimaryImageAspectRatio,SyncInfo",
                    StartIndex: 0,
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
            context.savedQueryKey = LibraryBrowser.getSavedQueryKey('channelitems');
        }
        return context.savedQueryKey;
    }

    function getParam(context, name) {
        
        if (!context.pageParams) {
            context.pageParams = {};
        }

        if (!context.pageParams[name]) {
            context.pageParams[name] = getParameterByName(name);
        }

        return context.pageParams[name];
    }

    function reloadFeatures(page) {

        var channelId = getParam(page, 'id');

        ApiClient.getJSON(ApiClient.getUrl("Channels/" + channelId + "/Features")).then(function (features) {

            if (features.CanFilter) {

                $('.filterControls', page).show();

            } else {
                $('.filterControls', page).hide();
            }

            if (features.SupportsSortOrderToggle) {

                $('.sortOrderToggle', page).show();
            } else {
                $('.sortOrderToggle', page).hide();
            }

            var maxPageSize = features.MaxPageSize;

            var query = getQuery(page);
            if (maxPageSize) {
                query.Limit = Math.min(maxPageSize, query.Limit || maxPageSize);
            }

            getPageData(page).sortFields = features.DefaultSortFields;

            reloadItems(page);
        });
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        var channelId = getParam(page, 'id');
        var folderId = getParam(page, 'folderId');

        var query = getQuery(page);
        query.UserId = Dashboard.getCurrentUserId();

        if (folderId) {

            ApiClient.getItem(query.UserId, folderId).then(function (item) {

                LibraryMenu.setTitle(item.Name);
            });

        } else {

            ApiClient.getItem(query.UserId, channelId).then(function (item) {

                LibraryMenu.setTitle(item.Name);
            });
        }

        query.folderId = folderId;

        ApiClient.getJSON(ApiClient.getUrl("Channels/" + channelId + "/Items", query)).then(function (result) {

            // Scroll back up so they can see the results from the beginning
            window.scrollTo(0, 0);

            var html = '';
            var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                showLimit: false,
                updatePageSizeSetting: false,
                sortButton: true,
                filterButton: true
            });

            page.querySelector('.listTopPaging').innerHTML = pagingHtml;

            updateFilterControls(page);

            html = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "auto",
                defaultShape: 'square',
                context: 'channels',
                showTitle: true,
                coverImage: true,
                showYear: true,
                lazy: true,
                centerText: true
            });

            var elem = page.querySelector('#items');
            elem.innerHTML = html + pagingHtml;
            ImageLoader.lazyChildren(elem);

            $('.btnNextPage', page).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page);
            });

            $('.btnPreviousPage', page).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page);
            });

            $('.btnFilter', page).on('click', function () {
                showFilterMenu(page);
            });

            // On callback make sure to set StartIndex = 0
            $('.btnSort', page).on('click', function () {
                showSortMenu(page);
            });

            Dashboard.hideLoadingMsg();

        }, function () {

            Dashboard.hideLoadingMsg();
        });
    }

    function showFilterMenu(page) {

        require(['components/filterdialog/filterdialog'], function (filterDialogFactory) {

            var filterDialog = new filterDialogFactory({
                query: getQuery(page)
            });

            Events.on(filterDialog, 'filterchange', function () {
                reloadItems(page);
            });

            filterDialog.show();
        });
    }

    function showSortMenu(page) {

        var sortFields = getPageData(page).sortFields;

        var items = [];

        items.push({
            name: Globalize.translate('OptionDefaultSort'),
            id: ''
        });

        if (sortFields.indexOf('Name') != -1) {
            items.push({
                name: Globalize.translate('OptionNameSort'),
                id: 'SortName'
            });
        }
        if (sortFields.indexOf('CommunityRating') != -1) {
            items.push({
                name: Globalize.translate('OptionCommunityRating'),
                id: 'CommunityRating'
            });
        }
        if (sortFields.indexOf('DateCreated') != -1) {
            items.push({
                name: Globalize.translate('OptionDateAdded'),
                id: 'DateCreated'
            });
        }
        if (sortFields.indexOf('PlayCount') != -1) {
            items.push({
                name: Globalize.translate('OptionPlayCount'),
                id: 'PlayCount'
            });
        }
        if (sortFields.indexOf('PremiereDate') != -1) {
            items.push({
                name: Globalize.translate('OptionReleaseDate'),
                id: 'PremiereDate'
            });
        }
        if (sortFields.indexOf('Runtime') != -1) {
            items.push({
                name: Globalize.translate('OptionRuntime'),
                id: 'Runtime'
            });
        }

        LibraryBrowser.showSortMenu({
            items: items,
            callback: function () {
                reloadItems(page);
            },
            query: getQuery(page)
        });
    }

    function updateFilterControls(page) {

    }

    pageIdOn('pagebeforeshow', "channelItemsPage", function () {

        var page = this;
        reloadFeatures(page);
        updateFilterControls(page);
    });

});