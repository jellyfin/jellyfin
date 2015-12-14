(function ($, document) {

    var data = {};

    function getPageData() {
        var key = getSavedQueryKey();
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

    function getQuery() {

        return getPageData().query;
    }

    function getSavedQueryKey() {

        return LibraryBrowser.getSavedQueryKey('movies');
    }

    function reloadFeatures(page) {

        var channelId = getParameterByName('id');

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

            var query = getQuery();
            if (maxPageSize) {
                query.Limit = Math.min(maxPageSize, query.Limit || maxPageSize);
            }

            getPageData().sortFields = features.DefaultSortFields;

            reloadItems(page);
        });
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        var channelId = getParameterByName('id');
        var folderId = getParameterByName('folderId');

        var query = getQuery();
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
                viewButton: true,
                showLimit: false,
                updatePageSizeSetting: false,
                viewIcon: 'filter-list',
                sortButton: true
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

            // On callback make sure to set StartIndex = 0
            $('.btnSort', page).on('click', function () {
                showSortMenu(page);
            });

            Dashboard.hideLoadingMsg();

        }, function () {

            Dashboard.hideLoadingMsg();
        });
    }

    function showSortMenu(page) {

        var sortFields = getPageData().sortFields;

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
            query: getQuery()
        });
    }

    function updateFilterControls(page) {

        var query = getQuery();
        $('.chkStandardFilter', page).each(function () {

            var filters = "," + (query.Filters || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        });

        $('.alphabetPicker', page).alphaValue(query.NameStartsWith);
    }

    pageIdOn('pageinit', "channelItemsPage", function () {

        var page = this;

        $('.chkStandardFilter', this).on('change', function () {

            var query = getQuery();
            var filterName = this.getAttribute('data-filter');
            var filters = query.Filters || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.Filters = filters;

            reloadItems(page);
        });

        $('.alphabetPicker', this).on('alphaselect', function (e, character) {

            var query = getQuery();
            query.NameStartsWithOrGreater = character;
            query.StartIndex = 0;

            reloadItems(page);

        }).on('alphaclear', function (e) {

            var query = getQuery();
            query.NameStartsWithOrGreater = '';

            reloadItems(page);
        });

    });

    pageIdOn('pagebeforeshow', "channelItemsPage", function () {

        var page = this;
        reloadFeatures(page);
        updateFilterControls(page);
    });

})(jQuery, document);