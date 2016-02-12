(function ($, document) {

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
                    Fields: "PrimaryImageAspectRatio,SortName,SyncInfo",
                    ImageTypeLimit: 1,
                    EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                    StartIndex: 0,
                    Limit: LibraryBrowser.getDefaultPageSize()
                },
                view: LibraryBrowser.getSavedView(key) || LibraryBrowser.getDefaultItemsView('Poster', 'Thumb')
            };

            pageData.query.ParentId = LibraryMenu.getTopParentId();
            LibraryBrowser.loadSavedQueryValues(key, pageData.query);
        }
        return pageData;
    }

    function getQuery() {

        return getPageData().query;
    }

    function getSavedQueryKey() {

        return LibraryBrowser.getSavedQueryKey('series');
    }

    function reloadItems(page, viewPanel) {

        Dashboard.showLoadingMsg();

        var query = getQuery();

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).then(function (result) {

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
                viewPanelClass: 'seriesViewPanel',
                updatePageSizeSetting: false,
                addLayoutButton: true,
                viewIcon: 'filter-list',
                sortButton: true,
                currentLayout: view,
                layouts: 'Banner,List,Poster,PosterCard,Thumb,ThumbCard'
            });

            page.querySelector('.listTopPaging').innerHTML = pagingHtml;

            updateFilterControls(page);

            if (view == "Thumb") {

                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "backdrop",
                    preferThumb: true,
                    context: 'tv',
                    lazy: true,
                    overlayPlayButton: true
                });

            }
            else if (view == "ThumbCard") {

                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "backdrop",
                    preferThumb: true,
                    context: 'tv',
                    lazy: true,
                    cardLayout: true,
                    showTitle: true,
                    showSeriesYear: true
                });
            }
            else if (view == "Banner") {

                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "banner",
                    preferBanner: true,
                    context: 'tv',
                    lazy: true
                });
            }
            else if (view == "List") {

                html = LibraryBrowser.getListViewHtml({
                    items: result.Items,
                    context: 'tv',
                    sortBy: query.SortBy
                });
            }
            else if (view == "PosterCard") {
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "portrait",
                    context: 'tv',
                    showTitle: true,
                    showYear: true,
                    lazy: true,
                    cardLayout: true
                });
            }
            else {

                // Poster
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "portrait",
                    context: 'tv',
                    centerText: true,
                    lazy: true,
                    overlayPlayButton: true
                });
            }

            var elem = page.querySelector('#items');
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

            $('.btnChangeLayout', page).on('layoutchange', function (e, layout) {
                getPageData().view = layout;
                LibraryBrowser.saveViewSetting(getSavedQueryKey(), layout);
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
                        name: Globalize.translate('OptionMetascore'),
                        id: 'Metascore,SortName'
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

            LibraryBrowser.setLastRefreshed(page);
            Dashboard.hideLoadingMsg();
        });
    }

    function updateFilterControls(tabContent, viewPanel) {

        $('.chkStatus', viewPanel).each(function () {

            var filters = "," + (getQuery().SeriesStatus || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        });

        $('.chkStandardFilter', viewPanel).each(function () {

            var filters = "," + (getQuery().Filters || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        });

        $('.chkAirDays', viewPanel).each(function () {

            var filters = "," + (getQuery().AirDays || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        });

        var query = getQuery();

        $('#chkTrailer', viewPanel).checked(query.HasTrailer == true);
        $('#chkThemeSong', viewPanel).checked(query.HasThemeSong == true);
        $('#chkThemeVideo', viewPanel).checked(query.HasThemeVideo == true);
        $('#chkSpecialFeature', viewPanel).checked(query.HasSpecialFeature == true);

        $('.alphabetPicker', tabContent).alphaValue(query.NameStartsWith);
    }

    function reloadFiltersIfNeeded(tabContent, viewPanel) {

        if (!getPageData().filtersLoaded) {

            getPageData().filtersLoaded = true;

            var query = getQuery();
            QueryFilters.loadFilters(viewPanel, Dashboard.getCurrentUserId(), query, function () {

                reloadItems(tabContent, viewPanel);
            });
        }
    }

    function initPage(tabContent, viewPanel) {

        $(viewPanel).on('panelopen', function () {

            reloadFiltersIfNeeded(tabContent, viewPanel);
        });

        $('.chkStandardFilter', viewPanel).on('change', function () {

            var query = getQuery();
            var filterName = this.getAttribute('data-filter');
            var filters = query.Filters || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.Filters = filters;
            query.StartIndex = 0;
            reloadItems(tabContent, viewPanel);
        });

        $('.chkStatus', viewPanel).on('change', function () {

            var query = getQuery();
            var filterName = this.getAttribute('data-filter');
            var filters = query.SeriesStatus || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.SeriesStatus = filters;
            query.StartIndex = 0;
            reloadItems(tabContent, viewPanel);
        });

        $('.chkAirDays', viewPanel).on('change', function () {

            var query = getQuery();
            var filterName = this.getAttribute('data-filter');
            var filters = query.AirDays || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.AirDays = filters;
            query.StartIndex = 0;
            reloadItems(tabContent, viewPanel);
        });

        $('#chkTrailer', viewPanel).on('change', function () {

            var query = getQuery();
            query.StartIndex = 0;
            query.HasTrailer = this.checked ? true : null;

            reloadItems(tabContent, viewPanel);
        });

        $('#chkThemeSong', viewPanel).on('change', function () {

            var query = getQuery();
            query.StartIndex = 0;
            query.HasThemeSong = this.checked ? true : null;

            reloadItems(tabContent, viewPanel);
        });

        $('#chkSpecialFeature', viewPanel).on('change', function () {

            var query = getQuery();
            query.StartIndex = 0;
            query.HasSpecialFeature = this.checked ? true : null;

            reloadItems(tabContent, viewPanel);
        });

        $('#chkThemeVideo', viewPanel).on('change', function () {

            var query = getQuery();
            query.StartIndex = 0;
            query.HasThemeVideo = this.checked ? true : null;

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

        $('#radioBasicFilters', viewPanel).on('change', function () {

            if (this.checked) {
                $('.basicFilters', viewPanel).show();
                $('.advancedFilters', viewPanel).hide();
            } else {
                $('.basicFilters', viewPanel).hide();
            }
        });

        $('#radioAdvancedFilters', viewPanel).on('change', function () {

            if (this.checked) {
                $('.advancedFilters', viewPanel).show();
                $('.basicFilters', viewPanel).hide();
            } else {
                $('.advancedFilters', viewPanel).hide();
            }
        });
    }

    window.TvPage.initSeriesTab = function (page, tabContent) {

        var viewPanel = page.querySelector('.seriesViewPanel');
        initPage(tabContent, viewPanel);
    };

    window.TvPage.renderSeriesTab = function (page, tabContent) {

        if (LibraryBrowser.needsRefresh(tabContent)) {
            var viewPanel = page.querySelector('.seriesViewPanel');
            reloadItems(tabContent, viewPanel);
            updateFilterControls(tabContent, viewPanel);
        }
    };

})(jQuery, document);