(function ($, document) {

    var view = LibraryBrowser.getDefaultItemsView('Poster', 'Poster');

    var currentDate = new Date();
    currentDate.setHours(0, 0, 0, 0);

    // The base query options
    var query = {
        UserId: Dashboard.getCurrentUserId(),
        SortBy: "PremiereDate,SortName",
        SortOrder: "Ascending",
        StartIndex: 0
    };

    function getSavedQueryKey() {
        return 'livetvitems' + (query.ParentId || '');
    }

    function updateFilterControls(page) {

        // Reset form values using the last used query
        $('.radioSortBy', page).each(function () {

            this.checked = (query.SortBy || '').toLowerCase() == this.getAttribute('data-sortby').toLowerCase();

        }).checkboxradio('refresh');

        $('.radioSortOrder', page).each(function () {

            this.checked = (query.SortOrder || '').toLowerCase() == this.getAttribute('data-sortorder').toLowerCase();

        }).checkboxradio('refresh');

        $('#selectView', page).val(view).selectmenu('refresh');
        $('.alphabetPicker', page).alphaValue(query.NameStartsWithOrGreater);
        $('#selectPageSize', page).val(query.Limit).selectmenu('refresh');
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getLiveTvPrograms(query).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            var html = '';
            var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                viewButton: true,
                showLimit: false,
                addSelectionButton: true
            });

            $('.listTopPaging', page).html(pagingHtml).trigger('create');

            updateFilterControls(page);
            var trigger = false;

            if (view == "Poster") {
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "portrait",
                    context: 'livetv',
                    showTitle: false,
                    centerText: true,
                    lazy: true,
                    overlayText: false
                });
            }
            else if (view == "PosterCard") {
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "portrait",
                    context: 'livetv',
                    showTitle: true,
                    showYear: true,
                    lazy: true,
                    cardLayout: true
                });
            }

            $('.itemsContainer', page).html(html).lazyChildren();

            var elem = $('.itemsContainer', page).html(html).lazyChildren();

            $(pagingHtml).appendTo(elem).trigger('create');

            $('.btnNextPage', page).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page);
            });

            $('.btnPreviousPage', page).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page);
            });

            LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);

            Dashboard.hideLoadingMsg();
        });
    }

    $(document).on('pageinit', "#liveTvItemsPage", function () {

        var page = this;

        $('.radioSortBy', this).on('click', function () {
            query.StartIndex = 0;
            query.SortBy = this.getAttribute('data-sortby');
            reloadItems(page);
        });

        $('.radioSortOrder', this).on('click', function () {
            query.StartIndex = 0;
            query.SortOrder = this.getAttribute('data-sortorder');
            reloadItems(page);
        });

        $('#selectView', this).on('change', function () {

            view = this.value;

            reloadItems(page);

            LibraryBrowser.saveViewSetting(getSavedQueryKey(), view);
        });

        $('#radioBasicFilters', this).on('change', function () {

            if (this.checked) {
                $('.basicFilters', page).show();
                $('.advancedFilters', page).hide();
            } else {
                $('.basicFilters', page).hide();
            }
        });

        $('#radioAdvancedFilters', this).on('change', function () {

            if (this.checked) {
                $('.advancedFilters', page).show();
                $('.basicFilters', page).hide();
            } else {
                $('.advancedFilters', page).hide();
            }
        });

        $('.itemsContainer', page).on('needsrefresh', function () {

            reloadItems(page);

        });

        $('#selectPageSize', page).on('change', function () {
            query.Limit = parseInt(this.value);
            query.StartIndex = 0;
            reloadItems(page);
        });

    }).on('pagebeforeshow', "#liveTvItemsPage", function () {

        query.ParentId = LibraryMenu.getTopParentId();

        var page = this;
        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
            query.StartIndex = 0;
        }

        query.IsMovie = getParameterByName('type') == 'movies' ? true : null;

        var viewkey = getSavedQueryKey();

        LibraryBrowser.loadSavedQueryValues(viewkey, query);

        QueryFilters.onPageShow(page, query);

        LibraryBrowser.getSavedViewSetting(viewkey).done(function (val) {

            if (val) {
                $('#selectView', page).val(val).selectmenu('refresh').trigger('change');
            } else {
                reloadItems(page);
            }
        });

    }).on('pageshow', "#liveTvItemsPage", function () {

        var page = this;

        updateFilterControls(page);
    });

})(jQuery, document);