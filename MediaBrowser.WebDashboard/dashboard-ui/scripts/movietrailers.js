(function ($, document) {

    var data = {};

    function getQuery() {

        var key = getSavedQueryKey();
        var pageData = data[key];

        if (!pageData) {
            pageData = data[key] = {
                query: {
                    SortBy: "SortName",
                    SortOrder: "Ascending",
                    Recursive: true,
                    Fields: "PrimaryImageAspectRatio,SortName,SyncInfo",
                    ImageTypeLimit: 1,
                    EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                    StartIndex: 0,
                    Limit: LibraryBrowser.getDefaultPageSize()
                }
            };

            pageData.query.ParentId = LibraryMenu.getTopParentId();
            LibraryBrowser.loadSavedQueryValues(key, pageData.query);
        }
        return pageData.query;
    }

    function getSavedQueryKey() {

        return LibraryBrowser.getSavedQueryKey('trailers');
    }

    function reloadItems(page, viewPanel) {

        Dashboard.showLoadingMsg();

        var query = getQuery();
        query.UserId = Dashboard.getCurrentUserId();

        ApiClient.getJSON(ApiClient.getUrl('Trailers', query)).then(function (result) {

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
                viewButton: true,
                viewIcon: 'filter-list',
                sortButton: true,
                showLimit: false,
                viewPanelClass: 'trailerViewPanel',
                updatePageSizeSetting: false
            });

            page.querySelector('.listTopPaging').innerHTML = pagingHtml;

            updateFilterControls(page, viewPanel);

            html = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "portrait",
                lazy: true,
                showDetailsMenu: true
            });

            var elem = page.querySelector('.itemsContainer');
            elem.innerHTML = html + pagingHtml;
            ImageLoader.lazyChildren(elem);

            $('.btnNextPage', page).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page, viewPanel);
            });

            $('.btnPreviousPage', page).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page, viewPanel);
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
                        reloadItems(page, viewPanel);
                    },
                    query: query
                });
            });

            LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);

            Dashboard.hideLoadingMsg();
        });
    }

    function updateFilterControls(tabContent, viewPanel) {

        var query = getQuery();

        $('.chkStandardFilter', viewPanel).each(function () {

            var filters = "," + (query.Filters || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        });

        $('.alphabetPicker', tabContent).alphaValue(query.NameStartsWithOrGreater);
    }

    function initPage(page, tabContent, viewPanel) {

        $('.chkStandardFilter', viewPanel).on('change', function () {

            var query = getQuery();
            var filterName = this.getAttribute('data-filter');
            var filters = query.Filters || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.Filters = filters;

            reloadItems(tabContent, viewPanel);
        });

        $('.alphabetPicker', tabContent).on('alphaselect', function (e, character) {

            var query = getQuery();
            query.NameStartsWithOrGreater = character;
            query.StartIndex = 0;

            reloadItems(tabContent, viewPanel);

        }).on('alphaclear', function (e) {

            var query = getQuery();
            query.NameStartsWithOrGreater = '';

            reloadItems(tabContent, viewPanel);
        });

        $('.itemsContainer', tabContent).on('needsrefresh', function () {

            reloadItems(tabContent, viewPanel);

        });
    }

    window.MoviesPage.initTrailerTab = function (page, tabContent) {

        var viewPanel = page.querySelector('.trailerViewPanel');
        initPage(page, tabContent, viewPanel);
    };

    window.MoviesPage.renderTrailerTab = function (page, tabContent) {

        if (LibraryBrowser.needsRefresh(tabContent)) {
            var viewPanel = page.querySelector('.trailerViewPanel');
            reloadItems(tabContent, viewPanel);
            updateFilterControls(tabContent, viewPanel);
        }
    };

})(jQuery, document);