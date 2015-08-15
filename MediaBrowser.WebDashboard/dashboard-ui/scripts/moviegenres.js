(function ($, document) {

    var view = LibraryBrowser.getDefaultItemsView('Thumb', 'Thumb');

    var data = {};
    function getQuery() {

        var key = getSavedQueryKey();
        var pageData = data[key];

        if (!pageData) {
            pageData = data[key] = {
                query: {
                    SortBy: "SortName",
                    SortOrder: "Ascending",
                    IncludeItemTypes: "Movie",
                    Recursive: true,
                    Fields: "DateCreated,SyncInfo,ItemCounts",
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

        return getWindowUrl();
    }

    function reloadItems(page, viewPanel) {

        Dashboard.showLoadingMsg();

        var query = getQuery();
        ApiClient.getGenres(Dashboard.getCurrentUserId(), query).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            window.scrollTo(0, 0);

            var html = '';

            $('.listTopPaging', page).html(LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                viewButton: true,
                showLimit: false,
                viewPanelClass: 'genreViewPanel'

            })).trigger('create');

            updateFilterControls(page);

            if (view == "Thumb") {
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "backdrop",
                    preferThumb: true,
                    context: 'movies',
                    showItemCounts: true,
                    centerText: true,
                    lazy: true
                });
            }
            else if (view == "ThumbCard") {

                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "backdrop",
                    preferThumb: true,
                    context: 'movies',
                    showItemCounts: true,
                    cardLayout: true,
                    showTitle: true,
                    lazy: true
                });
            }
            else if (view == "PosterCard") {
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "portrait",
                    context: 'movies',
                    showItemCounts: true,
                    lazy: true,
                    cardLayout: true,
                    showTitle: true
                });
            }
            else if (view == "Poster") {
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "portrait",
                    context: 'movies',
                    centerText: true,
                    showItemCounts: true,
                    lazy: true
                });
            }

            var elem = page.querySelector('.itemsContainer');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);

            $('.btnNextPage', page).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page, viewPanel);
            });

            $('.btnPreviousPage', page).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page, viewPanel);
            });

            LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);

            Dashboard.hideLoadingMsg();
        });
    }

    function updateFilterControls(page) {

        var query = getQuery();
        $('select.selectPageSize', page).val(query.Limit).selectmenu('refresh');
        $('select.selectView', page).val(view).selectmenu('refresh');
    }

    $(document).on('pageinitdepends', "#moviesRecommendedPage", function () {

        var page = this;
        var index = 4;
        var tabContent = page.querySelector('.pageTabContent[data-index=\'' + index + '\']');
        var viewPanel = $('.genreViewPanel', page);

        $(page.querySelector('neon-animated-pages')).on('tabchange', function () {

            if (parseInt(this.selected) == index) {
                if (LibraryBrowser.needsRefresh(tabContent)) {
                    reloadItems(tabContent, viewPanel);
                    updateFilterControls(viewPanel);
                }
            }
        });

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

        $('select.selectPageSize', viewPanel).on('change', function () {
            var query = getQuery();
            query.Limit = parseInt(this.value);
            query.StartIndex = 0;
            reloadItems(tabContent, viewPanel);
        });

        $('select.selectView', viewPanel).on('change', function () {

            view = this.value;

            reloadItems(tabContent, viewPanel);
            LibraryBrowser.saveViewSetting(getSavedQueryKey(), view);
        });

    });

})(jQuery, document);