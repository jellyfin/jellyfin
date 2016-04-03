define(['jQuery'], function ($) {

    var data = {};

    function getQuery(context) {

        var key = getSavedQueryKey(context);
        var pageData = data[key];

        if (!pageData) {
            pageData = data[key] = {
                query: {
                    SortBy: "SortName",
                    SortOrder: "Ascending",
                    IncludeItemTypes: "Trailer",
                    Recursive: true,
                    Fields: "PrimaryImageAspectRatio,SortName,MediaSourceCount,SyncInfo",
                    ImageTypeLimit: 1,
                    EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                    StartIndex: 0,
                    Limit: LibraryBrowser.getDefaultPageSize()
                }
            };

            LibraryBrowser.loadSavedQueryValues(key, pageData.query);
        }
        return pageData.query;
    }

    function getSavedQueryKey(context) {

        if (!context.savedQueryKey) {
            context.savedQueryKey = LibraryBrowser.getSavedQueryKey('trailers');
        }
        return context.savedQueryKey;
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        var query = getQuery(page);
        var userId = Dashboard.getCurrentUserId();

        ApiClient.getItems(userId, query).then(function (result) {

            // Scroll back up so they can see the results from the beginning
            window.scrollTo(0, 0);

            if (result.Items.length) {
                $('.noItemsMessage', page).hide();
            }
            else {
                $('.noItemsMessage', page).show();
            }

            var html = '';
            var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                sortButton: true,
                showLimit: false,
                updatePageSizeSetting: false,
                filterButton: true
            });

            page.querySelector('.listTopPaging').innerHTML = pagingHtml;

            updateFilterControls(page);

            html = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "portrait",
                lazy: true,
                showDetailsMenu: true,
                overlayPlayButton: true
            });

            var elem = page.querySelector('.itemsContainer');
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
                LibraryBrowser.showSortMenu({
                    items: [{
                        name: Globalize.translate('OptionNameSort'),
                        id: 'SortName'
                    },
                    {
                        name: Globalize.translate('OptionImdbRating'),
                        id: 'CommunityRating,SortName'
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
                    }],
                    callback: function () {
                        reloadItems(page);
                    },
                    query: query
                });
            });

            LibraryBrowser.saveQueryValues(getSavedQueryKey(page), query);

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

    function updateFilterControls(tabContent) {

        var query = getQuery(tabContent);

        $('.alphabetPicker', tabContent).alphaValue(query.NameStartsWithOrGreater);
    }

    function initPage(page, tabContent) {

        $('.alphabetPicker', tabContent).on('alphaselect', function (e, character) {

            var query = getQuery(page);
            query.NameStartsWithOrGreater = character;
            query.StartIndex = 0;

            reloadItems(tabContent);

        }).on('alphaclear', function (e) {

            var query = getQuery(page);
            query.NameStartsWithOrGreater = '';

            reloadItems(tabContent);
        });

        $('.itemsContainer', tabContent).on('needsrefresh', function () {

            reloadItems(tabContent);

        });
    }

    return function (view, params, tabContent) {

        var self = this;

        self.initTab = function () {

            initPage(view, tabContent);
        };

        self.renderTab = function () {

            reloadItems(tabContent);
            updateFilterControls(tabContent);
        };
    };

});