(function ($, document) {

    var view = LibraryBrowser.getDefaultItemsView('Poster', 'Poster');

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
                    Fields: "PrimaryImageAspectRatio,SortName,MediaSourceCount,IsUnidentified,SyncInfo",
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

        return getWindowUrl();
    }

    function reloadItems(page, viewPanel) {

        Dashboard.showLoadingMsg();

        var userId = Dashboard.getCurrentUserId();

        var query = getQuery();

        ApiClient.getItems(userId, query).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            window.scrollTo(0, 0);

            var html = '';

            var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                viewButton: true,
                showLimit: false,
                addSelectionButton: true,
                viewPanelClass: 'movieViewPanel'

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
                    overlayText: true,
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
                    overlayText: true,
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

            LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);

            LibraryBrowser.setLastRefreshed(page);

            Dashboard.hideLoadingMsg();
        });

        Dashboard.getCurrentUser().done(function (user) {
            if (user.Policy.IsAdministrator) {
                $('.btnMergeVersions', page).show();
            } else {
                $('.btnMergeVersions', page).hide();
            }
        });
    }

    function updateFilterControls(tabContent, viewPanel) {

        var query = getQuery();
        // Reset form values using the last used query
        $('.radioSortBy', viewPanel).each(function () {

            this.checked = (query.SortBy || '').toLowerCase() == this.getAttribute('data-sortby').toLowerCase();

        }).checkboxradio('refresh');

        $('.radioSortOrder', viewPanel).each(function () {

            this.checked = (query.SortOrder || '').toLowerCase() == this.getAttribute('data-sortorder').toLowerCase();

        }).checkboxradio('refresh');

        $('.chkStandardFilter', viewPanel).each(function () {

            var filters = "," + (query.Filters || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('.chkVideoTypeFilter', viewPanel).each(function () {

            var filters = "," + (query.VideoTypes || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('select.selectView', viewPanel).val(view).selectmenu('refresh');

        $('.chk3D', viewPanel).checked(query.Is3D == true).checkboxradio('refresh');
        $('.chkHD', viewPanel).checked(query.IsHD == true).checkboxradio('refresh');
        $('.chkSD', viewPanel).checked(query.IsHD == false).checkboxradio('refresh');

        $('.chkSubtitle', viewPanel).checked(query.HasSubtitles == true).checkboxradio('refresh');
        $('.chkTrailer', viewPanel).checked(query.HasTrailer == true).checkboxradio('refresh');
        $('.chkSpecialFeature', viewPanel).checked(query.HasSpecialFeature == true).checkboxradio('refresh');
        $('.chkThemeSong', viewPanel).checked(query.HasThemeSong == true).checkboxradio('refresh');
        $('.chkThemeVideo', viewPanel).checked(query.HasThemeVideo == true).checkboxradio('refresh');

        $('.alphabetPicker', tabContent).alphaValue(query.NameStartsWithOrGreater);
        $('select.selectPageSize', viewPanel).val(query.Limit).selectmenu('refresh');
    }

    var filtersLoaded;
    function reloadFiltersIfNeeded(tabContent, viewPanel) {

        if (!filtersLoaded) {

            filtersLoaded = true;

            var query = getQuery();
            QueryFilters.loadFilters(viewPanel, Dashboard.getCurrentUserId(), query, function () {

                reloadItems(tabContent, viewPanel);
            });
        }
    }

    $(document).on('pageinitdepends', "#moviesRecommendedPage", function () {

        var page = this;
        var index = 1;
        var tabContent = page.querySelector('.pageTabContent[data-index=\'' + index + '\']');
        var viewPanel = $('.movieViewPanel', page);

        $(page.querySelector('neon-animated-pages')).on('tabchange', function () {

            if (parseInt(this.selected) == index) {
                if (LibraryBrowser.needsRefresh(tabContent)) {
                    reloadItems(tabContent, viewPanel);
                    updateFilterControls(tabContent, viewPanel);
                }
            }
        });

        $(viewPanel).on('panelopen', function () {

            reloadFiltersIfNeeded(tabContent, viewPanel);
        });

        $('.radioSortBy', viewPanel).on('click', function () {
            var query = getQuery();
            query.StartIndex = 0;
            query.SortBy = this.getAttribute('data-sortby');
            reloadItems(tabContent, viewPanel);
        });

        $('.radioSortOrder', viewPanel).on('click', function () {
            var query = getQuery();
            query.StartIndex = 0;
            query.SortOrder = this.getAttribute('data-sortorder');
            reloadItems(tabContent, viewPanel);
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

        $('select.selectView', viewPanel).on('change', function () {

            view = this.value;

            var query = getQuery();
            if (view == "Timeline") {

                query.SortBy = "PremiereDate";
                query.SortOrder = "Descending";
                query.StartIndex = 0;
                $('.radioPremiereDate', page)[0].click();

            } else {
                reloadItems(tabContent, viewPanel);
            }

            LibraryBrowser.saveViewSetting(getSavedQueryKey(), view);
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

        $('.chk3D', viewPanel).on('change', function () {

            var query = getQuery();
            query.StartIndex = 0;
            query.Is3D = this.checked ? true : null;

            reloadItems(tabContent, viewPanel);
        });

        $('.chkHD', viewPanel).on('change', function () {

            var query = getQuery();
            query.StartIndex = 0;
            query.IsHD = this.checked ? true : null;

            reloadItems(tabContent, viewPanel);
        });

        $('.chkSD', viewPanel).on('change', function () {

            var query = getQuery();
            query.StartIndex = 0;
            query.IsHD = this.checked ? false : null;

            reloadItems(tabContent, viewPanel);
        });

        $('.chkSubtitle', viewPanel).on('change', function () {

            var query = getQuery();
            query.StartIndex = 0;
            query.HasSubtitles = this.checked ? true : null;

            reloadItems(tabContent, viewPanel);
        });

        $('.chkTrailer', viewPanel).on('change', function () {

            var query = getQuery();
            query.StartIndex = 0;
            query.HasTrailer = this.checked ? true : null;

            reloadItems(tabContent, viewPanel);
        });

        $('.chkSpecialFeature', viewPanel).on('change', function () {

            var query = getQuery();
            query.StartIndex = 0;
            query.HasSpecialFeature = this.checked ? true : null;

            reloadItems(tabContent, viewPanel);
        });

        $('.chkThemeSong', viewPanel).on('change', function () {

            var query = getQuery();
            query.StartIndex = 0;
            query.HasThemeSong = this.checked ? true : null;

            reloadItems(tabContent, viewPanel);
        });

        $('.chkThemeVideo', viewPanel).on('change', function () {

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

        $('select.selectPageSize', viewPanel).on('change', function () {
            var query = getQuery();
            query.Limit = parseInt(this.value);
            query.StartIndex = 0;
            reloadItems(tabContent, viewPanel);
        });

    });

})(jQuery, document);