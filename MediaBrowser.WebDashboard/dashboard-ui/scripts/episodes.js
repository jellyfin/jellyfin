(function ($, document) {

    var data = {};

    function getPageData() {
        var key = getSavedQueryKey();
        var pageData = data[key];

        if (!pageData) {
            pageData = data[key] = {
                query: {
                    SortBy: "SeriesSortName,SortName",
                    SortOrder: "Ascending",
                    IncludeItemTypes: "Episode",
                    Recursive: true,
                    Fields: "PrimaryImageAspectRatio,MediaSourceCount,UserData,SyncInfo",
                    IsMissing: false,
                    IsVirtualUnaired: false,
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

        return LibraryBrowser.getSavedQueryKey('episodes');
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
                viewPanelClass: 'episodesViewPanel',
                updatePageSizeSetting: false,
                addLayoutButton: true,
                viewIcon: 'filter-list',
                sortButton: true,
                currentLayout: view,
                layouts: 'Poster,PosterCard'
            });

            page.querySelector('.listTopPaging').innerHTML = pagingHtml;

            updateFilterControls(page, viewPanel);

            if (view == "List") {

                html = LibraryBrowser.getListViewHtml({
                    items: result.Items,
                    sortBy: query.SortBy
                });
            }
            else if (view == "Poster") {
                html += LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "backdrop",
                    showTitle: true,
                    showParentTitle: true,
                    overlayText: true,
                    lazy: true,
                    showDetailsMenu: true,
                    overlayPlayButton: true
                });
            }
            else if (view == "PosterCard") {
                html += LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "backdrop",
                    showTitle: true,
                    showParentTitle: true,
                    lazy: true,
                    cardLayout: true,
                    showDetailsMenu: true
                });
            }

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
                        id: 'SeriesSortName,SortName'
                    },
                    {
                        name: Globalize.translate('OptionTvdbRating'),
                        id: 'CommunityRating,SeriesSortName,SortName'
                    },
                    {
                        name: Globalize.translate('OptionDateAdded'),
                        id: 'DateCreated,SeriesSortName,SortName'
                    },
                    {
                        name: Globalize.translate('OptionPremiereDate'),
                        id: 'PremiereDate,SeriesSortName,SortName'
                    },
                    {
                        name: Globalize.translate('OptionDatePlayed'),
                        id: 'DatePlayed,SeriesSortName,SortName'
                    },
                    {
                        name: Globalize.translate('OptionParentalRating'),
                        id: 'OfficialRating,SeriesSortName,SortName'
                    },
                    {
                        name: Globalize.translate('OptionPlayCount'),
                        id: 'PlayCount,SeriesSortName,SortName'
                    },
                    {
                        name: Globalize.translate('OptionRuntime'),
                        id: 'Runtime,SeriesSortName,SortName'
                    },
                    {
                        name: Globalize.translate('OptionVideoBitrate'),
                        id: 'VideoBitRate,SeriesSortName,SortName'
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

            this.checked = filters.toLowerCase().indexOf(',' + filterName.toLowerCase()) != -1;

        });

        $('.chkVideoTypeFilter', viewPanel).each(function () {

            var filters = "," + (query.VideoTypes || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        });

        $('#chkHD', viewPanel).checked(query.IsHD == true);
        $('#chkSD', viewPanel).checked(query.IsHD == false);

        $('#chkSubtitle', viewPanel).checked(query.HasSubtitles == true);
        $('#chkTrailer', viewPanel).checked(query.HasTrailer == true);
        $('#chkThemeSong', viewPanel).checked(query.HasThemeSong == true);
        $('#chkThemeVideo', viewPanel).checked(query.HasThemeVideo == true);
        $('#chkSpecialFeature', viewPanel).checked(query.ParentIndexNumber == 0);

        $('#chkMissingEpisode', viewPanel).checked(query.IsMissing == true);
        $('#chkFutureEpisode', viewPanel).checked(query.IsUnaired == true);

        $('.alphabetPicker', tabContent).alphaValue(query.NameStartsWithOrGreater);
    }

    function initPage(tabContent, viewPanel) {

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

        $('#chkSubtitle', viewPanel).on('change', function () {

            var query = getQuery();
            query.StartIndex = 0;
            query.HasSubtitles = this.checked ? true : null;

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

        $('#chkThemeVideo', viewPanel).on('change', function () {

            var query = getQuery();
            query.StartIndex = 0;
            query.HasThemeVideo = this.checked ? true : null;

            reloadItems(tabContent, viewPanel);
        });

        $('#chkSpecialFeature', viewPanel).on('change', function () {

            var query = getQuery();
            query.ParentIndexNumber = this.checked ? 0 : null;

            reloadItems(tabContent, viewPanel);
        });

        $('#chkMissingEpisode', viewPanel).on('change', function () {

            var query = getQuery();
            query.StartIndex = 0;
            query.IsMissing = this.checked ? true : false;

            reloadItems(tabContent, viewPanel);
        });

        $('#chkFutureEpisode', viewPanel).on('change', function () {

            var query = getQuery();
            query.StartIndex = 0;

            if (this.checked) {
                query.IsUnaired = true;
                query.IsVirtualUnaired = null;
            } else {
                query.IsUnaired = null;
                query.IsVirtualUnaired = false;
            }


            reloadItems(tabContent, viewPanel);
        });

        $('#chkHD', viewPanel).on('change', function () {

            var query = getQuery();
            query.StartIndex = 0;
            query.IsHD = this.checked ? true : null;

            reloadItems(tabContent, viewPanel);
        });

        $('#chkSD', viewPanel).on('change', function () {

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

        $('.itemsContainer', tabContent).on('needsrefresh', function () {

            reloadItems(tabContent, viewPanel);

        });
    }

    window.TvPage.initEpisodesTab = function (page, tabContent) {

        var viewPanel = page.querySelector('.episodesViewPanel');
        initPage(tabContent, viewPanel);
    };

    window.TvPage.renderEpisodesTab = function (page, tabContent) {

        if (LibraryBrowser.needsRefresh(tabContent)) {
            var viewPanel = page.querySelector('.episodesViewPanel');
            reloadItems(tabContent, viewPanel);
            updateFilterControls(tabContent, viewPanel);
        }
    };

})(jQuery, document);