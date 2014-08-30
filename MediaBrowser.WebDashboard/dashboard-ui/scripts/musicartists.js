(function ($, document) {

    var view = LibraryBrowser.getDefaultItemsView('Poster', 'List');

    // The base query options
    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending",
        Recursive: true,
        Fields: "PrimaryImageAspectRatio,SortName,DateCreated",
        StartIndex: 0
    };

    function getSavedQueryKey() {

        return 'musicartists' + (query.ParentId || '');
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getArtists(Dashboard.getCurrentUserId(), query).done(function (result) {

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

            if (view == "List") {

                html = LibraryBrowser.getListViewHtml({
                    items: result.Items,
                    context: 'music',
                    sortBy: query.SortBy
                });
            }
            else if (view == "Poster") {
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "square",
                    context: 'music',
                    showTitle: true,
                    coverImage: true,
                    centerText: true,
                    lazy: true,
                    selectionPanel: true
                });
            }

            html += pagingHtml;

            $('#items', page).html(html).trigger('create').createCardMenus();

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

    function updateFilterControls(page) {

        $('.chkStandardFilter', page).each(function () {

            var filters = "," + (query.Filters || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('#selectView', page).val(view).selectmenu('refresh');

        $('.alphabetPicker', page).alphaValue(query.NameStartsWithOrGreater);
        $('#selectPageSize', page).val(query.Limit).selectmenu('refresh');
    }

    $(document).on('pageinit', "#musicArtistsPage", function () {

        var page = this;

        $('.chkStandardFilter', this).on('change', function () {

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

            query.NameStartsWithOrGreater = character;
            query.StartIndex = 0;

            reloadItems(page);

        }).on('alphaclear', function (e) {

            query.NameStartsWithOrGreater = '';

            reloadItems(page);
        });

        $('#selectView', this).on('change', function () {

            view = this.value;

            reloadItems(page);

            LibraryBrowser.saveViewSetting(getSavedQueryKey(), view);
        });

        $('#selectPageSize', page).on('change', function () {
            query.Limit = parseInt(this.value);
            query.StartIndex = 0;
            reloadItems(page);
        });

    }).on('pagebeforeshow', "#musicArtistsPage", function () {

        var page = this;
        
        query.ParentId = LibraryMenu.getTopParentId();

        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
            query.StartIndex = 0;
        }

        var viewkey = getSavedQueryKey();

        LibraryBrowser.loadSavedQueryValues(viewkey, query);

        LibraryBrowser.getSavedViewSetting(viewkey).done(function (val) {

            if (val) {
                $('#selectView', page).val(val).selectmenu('refresh').trigger('change');
            } else {
                reloadItems(page);
            }
        });

    }).on('pageshow', "#musicArtistsPage", function () {

        updateFilterControls(this);
    });

})(jQuery, document);