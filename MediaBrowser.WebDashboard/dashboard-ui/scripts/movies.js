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
                    IncludeItemTypes: "Movie",
                    Recursive: true,
                    Fields: "PrimaryImageAspectRatio,SortName,MediaSourceCount,SyncInfo",
                    ImageTypeLimit: 1,
                    EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                    StartIndex: 0,
                    Limit: LibraryBrowser.getDefaultPageSize()
                },
                view: LibraryBrowser.getSavedView(key) || LibraryBrowser.getDefaultItemsView('Poster', 'Poster')
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

        return LibraryBrowser.getSavedQueryKey('movies');
    }

    function reloadItems(page, viewPanel) {

        Dashboard.showLoadingMsg();

        var userId = Dashboard.getCurrentUserId();

        var query = getQuery();
        var view = getPageData().view;

        ApiClient.getItems(userId, query).then(function (result) {

            // Scroll back up so they can see the results from the beginning
            window.scrollTo(0, 0);

            var html = '';

            var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                viewButton: true,
                showLimit: false,
                viewPanelClass: 'movieViewPanel',
                updatePageSizeSetting: false,
                addLayoutButton: true,
                viewIcon: 'filter-list',
                sortButton: true,
                currentLayout: view,
                layouts: 'Banner,List,Poster,PosterCard,Thumb,ThumbCard,Timeline'
            });

            page.querySelector('.listTopPaging').innerHTML = pagingHtml;

            updateFilterControls(page);
            var trigger = false;

            if (view == "Thumb") {
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "backdrop",
                    preferThumb: true,
                    lazy: true,
                    showDetailsMenu: true
                });
            }
            else if (view == "ThumbCard") {

                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "backdrop",
                    preferThumb: true,
                    lazy: true,
                    showTitle: true,
                    cardLayout: true,
                    showYear: true,
                    showDetailsMenu: true
                });
            }
            else if (view == "Banner") {

                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "banner",
                    preferBanner: true,
                    lazy: true,
                    showDetailsMenu: true
                });
            }
            else if (view == "List") {

                html = LibraryBrowser.getListViewHtml({
                    items: result.Items,
                    sortBy: query.SortBy
                });
                trigger = true;
            }
            else if (view == "Poster") {
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "portrait",
                    centerText: true,
                    lazy: true,
                    showDetailsMenu: true
                });
            }
            else if (view == "PosterCard") {
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "portrait",
                    showTitle: true,
                    showYear: true,
                    lazy: true,
                    cardLayout: true,
                    showDetailsMenu: true
                });
            }
            else if (view == "Timeline") {
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "portrait",
                    showTitle: true,
                    timeline: true,
                    centerText: true,
                    lazy: true,
                    showDetailsMenu: true
                });
            }

            var elem = page.querySelector('.itemsContainer');
            elem.innerHTML = html + pagingHtml;
            ImageLoader.lazyChildren(elem);

            if (trigger) {
                Events.trigger(elem, 'create');
            }

            $('.btnNextPage', page).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page, viewPanel);
            });

            $('.btnPreviousPage', page).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page, viewPanel);
            });

            $('.btnChangeLayout', page).on('layoutchange', function (e, layout) {

                if (layout == 'Timeline') {
                    getQuery().SortBy = 'ProductionYear,PremiereDate,SortName';
                    getQuery().SortOrder = 'Descending';
                }

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
                        name: Globalize.translate('OptionBudget'),
                        id: 'Budget,SortName'
                    },
                    {
                        name: Globalize.translate('OptionImdbRating'),
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
                    },
                    {
                        name: Globalize.translate('OptionRevenue'),
                        id: 'Revenue,SortName'
                    },
                    {
                        name: Globalize.translate('OptionRuntime'),
                        id: 'Runtime,SortName'
                    },
                    {
                        name: Globalize.translate('OptionVideoBitrate'),
                        id: 'VideoBitRate,SortName'
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

        var query = getQuery();

        $('.chkStandardFilter', viewPanel).each(function () {

            var filters = "," + (query.Filters || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        });

        $('.chkVideoTypeFilter', viewPanel).each(function () {

            var filters = "," + (query.VideoTypes || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        });

        $('.chk3DFilter', viewPanel).checked(query.Is3D == true);
        $('.chkHDFilter', viewPanel).checked(query.IsHD == true);
        $('.chkSDFilter', viewPanel).checked(query.IsHD == false);

        $('.alphabetPicker', tabContent).alphaValue(query.NameStartsWithOrGreater);
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

            query.StartIndex = 0;
            query.Filters = filters;

            reloadItems(tabContent, viewPanel);
        });

        $('.chkVideoTypeFilter', viewPanel).on('change', function () {

            var query = getQuery();
            var filterName = this.getAttribute('data-filter');
            var filters = query.VideoTypes || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.VideoTypes = filters;

            reloadItems(tabContent, viewPanel);
        });

        $('.chk3DFilter', viewPanel).on('change', function () {

            var query = getQuery();
            query.StartIndex = 0;
            query.Is3D = this.checked ? true : null;

            reloadItems(tabContent, viewPanel);
        });

        $('.chkHDFilter', viewPanel).on('change', function () {

            var query = getQuery();
            query.StartIndex = 0;
            query.IsHD = this.checked ? true : null;
            reloadItems(tabContent, viewPanel);
        });

        $('.chkSDFilter', viewPanel).on('change', function () {

            var query = getQuery();
            query.StartIndex = 0;
            query.IsHD = this.checked ? false : null;

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

        $('.radioBasicFilters', viewPanel).on('change', function () {

            if (this.checked) {
                $('.basicFilters', viewPanel).show();
                $('.advancedFilters', viewPanel).hide();
            } else {
                $('.basicFilters', viewPanel).hide();
            }
        });

        $('.radioAdvancedFilters', viewPanel).on('change', function () {

            if (this.checked) {
                $('.advancedFilters', viewPanel).show();
                $('.basicFilters', viewPanel).hide();
            } else {
                $('.advancedFilters', viewPanel).hide();
            }
        });

        $('.itemsContainer', tabContent).on('needsrefresh', function () {

            reloadItems(tabContent, viewPanel);

        });
    }

    window.MoviesPage.initMoviesTab = function (page, tabContent) {

        var viewPanel = page.querySelector('.movieViewPanel');
        initPage(tabContent, viewPanel);
    };

    window.MoviesPage.renderMoviesTab = function (page, tabContent) {

        if (LibraryBrowser.needsRefresh(tabContent)) {
            var viewPanel = page.querySelector('.movieViewPanel');
            reloadItems(tabContent, viewPanel);
            updateFilterControls(tabContent, viewPanel);
        }
    };

})(jQuery, document);