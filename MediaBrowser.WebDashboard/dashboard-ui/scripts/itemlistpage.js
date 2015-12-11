(function ($, document) {

    var currentItem;

    var data = {};

    function getPageData() {
        var key = getSavedQueryKey();
        var pageData = data[key];

        if (!pageData) {
            pageData = data[key] = {
                query: {
                    SortBy: "SortName",
                    SortOrder: "Ascending",
                    Fields: "DateCreated,PrimaryImageAspectRatio,MediaSourceCount,SyncInfo",
                    ImageTypeLimit: 1,
                    EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                    StartIndex: 0,
                    Limit: LibraryBrowser.getDefaultPageSize()
                }
            };

            pageData.query.Filters = "";
            pageData.query.NameStartsWithOrGreater = '';
            pageData.view = LibraryBrowser.getSavedView(key) || LibraryBrowser.getDefaultItemsView('Poster', 'Poster');

            pageData.query.ParentId = getParameterByName('parentId');
            LibraryBrowser.loadSavedQueryValues(key, pageData.query);
        }
        return pageData;
    }

    function getQuery() {

        return getPageData().query;
    }
    function getSavedQueryKey() {

        return LibraryBrowser.getSavedQueryKey();
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        var query = getQuery();
        var userId = Dashboard.getCurrentUserId();

        var parentItemPromise = query.ParentId ?
           ApiClient.getItem(userId, query.ParentId) :
           ApiClient.getRootFolder(userId);

        var itemsPromise = ApiClient.getItems(userId, query);

        Promise.all([parentItemPromise, itemsPromise]).then(function (responses) {

            var item = responses[0];
            currentItem = item;
            var result = responses[1];

            // Scroll back up so they can see the results from the beginning
            window.scrollTo(0, 0);

            var view = getPageData().view;

            var html = '';
            var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                viewButton: true,
                showLimit: false,
                addLayoutButton: true,
                currentLayout: view,
                viewIcon: 'filter-list',
                sortButton: true,
                layouts: 'Poster,PosterCard,Thumb'
            });

            page.querySelector('.listTopPaging').innerHTML = pagingHtml;

            updateFilterControls(page);

            var context = getParameterByName('context');

            var posterOptions = {
                items: result.Items,
                shape: "auto",
                centerText: true,
                lazy: true,
                coverImage: item.Type == 'PhotoAlbum'
            };

            if (view == "Backdrop") {

                posterOptions.shape = 'backdrop';
                posterOptions.showTitle = true;
                posterOptions.preferBackdrop = true;

                html = LibraryBrowser.getPosterViewHtml(posterOptions);
            }
            else if (view == "Poster") {

                posterOptions.showTitle = context == 'photos' ? 'auto' : true;
                posterOptions.overlayText = context == 'photos';

                html = LibraryBrowser.getPosterViewHtml(posterOptions);
            }
            else if (view == "PosterCard") {

                posterOptions.showTitle = true;
                posterOptions.showYear = true;
                posterOptions.cardLayout = true;
                posterOptions.centerText = false;

                html = LibraryBrowser.getPosterViewHtml(posterOptions);
            }

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

            $('.btnChangeLayout', page).on('layoutchange', function (e, layout) {
                getPageData().view = layout;
                LibraryBrowser.saveViewSetting(getSavedQueryKey(), layout);
                reloadItems(page);
            });

            // On callback make sure to set StartIndex = 0
            $('.btnSort', page).on('click', function () {
                LibraryBrowser.showSortMenu({
                    items: [{
                        name: Globalize.translate('OptionNameSort'),
                        id: 'SortName'
                    },
                    {
                        name: Globalize.translate('OptionCommunityRating'),
                        id: 'CommunityRating,SortName'
                    },
                    {
                        name: Globalize.translate('OptionCriticRating'),
                        id: 'CriticRating,SortName'
                    },
                    {
                        name: Globalize.translate('OptionDateAdded'),
                        id: 'DateCreated,SortName'
                    },
                    {
                        name: Globalize.translate('OptionDatePlayed'),
                        id: 'DatePlayed,SortName'
                    },
                    {
                        name: Globalize.translate('OptionParentalRating'),
                        id: 'OfficialRating,SortName'
                    },
                    {
                        name: Globalize.translate('OptionPlayCount'),
                        id: 'PlayCount,SortName'
                    },
                    {
                        name: Globalize.translate('OptionReleaseDate'),
                        id: 'PremiereDate,SortName'
                    },
                    {
                        name: Globalize.translate('OptionRuntime'),
                        id: 'Runtime,SortName'
                    }],
                    callback: function () {
                        reloadItems(page);
                    },
                    query: query
                });
            });

            LibraryBrowser.saveQueryValues(getParameterByName('parentId'), query);

            var name = item.Name;

            if (item.IndexNumber != null) {
                name = item.IndexNumber + " - " + name;
            }
            if (item.ParentIndexNumber != null) {
                name = item.ParentIndexNumber + "." + name;
            }

            LibraryMenu.setTitle(name);

            page.dispatchEvent(new CustomEvent("displayingitem", {
                detail: {
                    item: item
                },
                bubbles: true
            }));

            LibraryBrowser.setLastRefreshed(page);
            Dashboard.hideLoadingMsg();
        });
    }

    function updateFilterControls(page) {

        var query = getQuery();

        $('.chkStandardFilter', page).each(function () {

            var filters = "," + (query.Filters || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        });

        $('.alphabetPicker', page).alphaValue(query.NameStartsWithOrGreater);
    }

    function onListItemClick(e) {

        var query = getQuery();
        var page = $(this).parents('.page');
        var info = LibraryBrowser.getListItemInfo(this);

        if (info.mediaType == 'Photo') {
            require(['scripts/photos'], function () {
                Photos.startSlideshow(page, query, info.id);
            });
            return false;
        }
    }

    pageIdOn('pageinit', "itemListPage", function () {

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

        $(page).on('click', '.mediaItem', onListItemClick);

    });

    pageIdOn('pagebeforeshow', "itemListPage", function () {

        var page = this;

        reloadItems(page);
        updateFilterControls(page);
        LibraryMenu.setBackButtonVisible(getParameterByName('context'));

    });

    pageIdOn('pagebeforehide', "itemListPage", function () {

        currentItem = null;

    });

})(jQuery, document);