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
                    IncludeItemTypes: "BoxSet",
                    Recursive: true,
                    Fields: "PrimaryImageAspectRatio,SortName,SyncInfo,CanDelete",
                    ImageTypeLimit: 1,
                    EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                    StartIndex: 0,
                    Limit: LibraryBrowser.getDefaultPageSize()
                }
            };

            //pageData.query.ParentId = LibraryMenu.getTopParentId();
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
        var promise1 = ApiClient.getItems(Dashboard.getCurrentUserId(), query);
        var promise2 = Dashboard.getCurrentUser();

        $.when(promise1, promise2).done(function (response1, response2) {

            var result = response1[0];
            var user = response2[0];

            // Scroll back up so they can see the results from the beginning
            window.scrollTo(0, 0);

            var html = '';

            $('.listTopPaging', page).html(LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                viewButton: true,
                showLimit: false,
                viewPanelClass: 'collectionViewPanel'

            })).trigger('create');

            updateFilterControls(page, viewPanel);
            var trigger = false;

            if (result.TotalRecordCount) {

                var context = getParameterByName('context');

                if (view == "List") {

                    html = LibraryBrowser.getListViewHtml({
                        items: result.Items,
                        context: context,
                        sortBy: query.SortBy
                    });
                    trigger = true;
                }
                else if (view == "Poster") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "auto",
                        context: context,
                        showTitle: true,
                        centerText: true,
                        lazy: true
                    });
                }
                else if (view == "PosterCard") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "auto",
                        context: context,
                        showTitle: true,
                        cardLayout: true,
                        lazy: true,
                        showItemCounts: true
                    });
                }
                else if (view == "Thumb") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "backdrop",
                        context: context,
                        showTitle: true,
                        centerText: true,
                        lazy: true,
                        preferThumb: true
                    });
                }
                else if (view == "ThumbCard") {
                    html = LibraryBrowser.getPosterViewHtml({
                        items: result.Items,
                        shape: "backdrop",
                        context: context,
                        showTitle: true,
                        lazy: true,
                        preferThumb: true,
                        cardLayout: true,
                        showItemCounts: true
                    });
                }

                $('.noItemsMessage', page).hide();

            } else {

                $('.noItemsMessage', page).show();
            }

            var elem = page.querySelector('.itemsContainer');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);

            if (trigger) {
                $(elem).trigger('create');
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

            Dashboard.hideLoadingMsg();
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

        $('select.selectView', viewPanel).val(view).selectmenu('refresh');

        $('.chkTrailer', viewPanel).checked(query.HasTrailer == true).checkboxradio('refresh');
        $('.chkThemeSong', viewPanel).checked(query.HasThemeSong == true).checkboxradio('refresh');
        $('.chkThemeVideo', viewPanel).checked(query.HasThemeVideo == true).checkboxradio('refresh');

        $('select.selectPageSize', viewPanel).val(query.Limit).selectmenu('refresh');
    }

    function initEvents(tabContent, viewPanel) {

        $('.radioSortBy', viewPanel).on('click', function () {
            var query = getQuery();
            query.SortBy = this.getAttribute('data-sortby');
            reloadItems(tabContent, viewPanel);
        });

        $('.radioSortOrder', viewPanel).on('click', function () {
            var query = getQuery();
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

        $('.chkTrailer', viewPanel).on('change', function () {

            var query = getQuery();
            query.StartIndex = 0;
            query.HasTrailer = this.checked ? true : null;

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

        $('select.selectView', viewPanel).on('change', function () {

            view = this.value;

            reloadItems(tabContent, viewPanel);

            LibraryBrowser.saveViewSetting(getSavedQueryKey(), view);
        });

        $('select.selectPageSize', viewPanel).on('change', function () {
            var query = getQuery();
            query.Limit = parseInt(this.value);
            query.StartIndex = 0;
            reloadItems(tabContent, viewPanel);
        });
    }

    $(document).on('pageinitdepends', "#moviesRecommendedPage", function () {

        var page = this;
        var index = 3;
        var tabContent = page.querySelector('.pageTabContent[data-index=\'' + index + '\']');
        var viewPanel = $('.collectionViewPanel', page);

        $(page.querySelector('neon-animated-pages')).on('tabchange', function () {

            if (parseInt(this.selected) == index) {
                if (LibraryBrowser.needsRefresh(tabContent)) {
                    reloadItems(tabContent, viewPanel);
                    updateFilterControls(viewPanel);
                }
            }
        });

        initEvents(tabContent, viewPanel);
    });

    $(document).on('pageinitdepends', "#boxsetsPage", function () {

        var page = this;

        var content = page;
        var viewPanel = page.querySelector('.viewPanel');

        initEvents(content, viewPanel);

    }).on('pagebeforeshowready', "#boxsetsPage", function () {

        var page = this;

        var content = page;
        var viewPanel = page.querySelector('.viewPanel');

        reloadItems(content, viewPanel);
        updateFilterControls(content, viewPanel);
    });

})(jQuery, document);