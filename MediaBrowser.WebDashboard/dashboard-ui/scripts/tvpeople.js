(function ($, document) {

    // The base query options
    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending",
        IncludeItemTypes: "Series,Episode",
        Recursive: true,
        Fields: "DateCreated",
        PersonTypes: "",
        StartIndex: 0
    };

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getPeople(Dashboard.getCurrentUserId(), query).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            var html = '';

            $('.listTopPaging', page).html(LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, true)).trigger('create');

            updateFilterControls(page);

            var checkSortOption = $('.radioSortBy:checked', page);
            $('.viewSummary', page).html(LibraryBrowser.getViewSummaryHtml(query, checkSortOption)).trigger('create');

            html = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "portrait",
                context: 'tv',
                useAverageAspectRatio: true,
                showTitle: true,
                showItemCounts: true,
                coverImage: true
            });

            html += LibraryBrowser.getPagingHtml(query, result.TotalRecordCount);

            $('#items', page).html(html).trigger('create');

            $('.btnNextPage', page).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page);
            });

            $('.btnPreviousPage', page).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page);
            });

            $('.selectPageSize', page).on('change', function () {
                query.Limit = parseInt(this.value);
                query.StartIndex = 0;
                reloadItems(page);
            });

            LibraryBrowser.saveQueryValues('tvpeople', query);

            Dashboard.hideLoadingMsg();
        });
    }

    function updateFilterControls(page) {

        // Reset form values using the last used query
        $('.radioSortBy', page).each(function () {

            this.checked = (query.SortBy || '').toLowerCase() == this.getAttribute('data-sortby').toLowerCase();

        }).checkboxradio('refresh');

        $('.radioSortOrder', page).each(function () {

            this.checked = (query.SortOrder || '').toLowerCase() == this.getAttribute('data-sortorder').toLowerCase();

        }).checkboxradio('refresh');

        $('.chkStandardFilter', page).each(function () {

            var filters = "," + (query.Filters || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('.chkPersonTypeFilter', page).each(function () {

            var filters = "," + (query.PersonTypes || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('.alphabetPicker', page).alphaValue(query.NameStartsWithOrGreater);
    }

    $(document).on('pageinit', "#tvPeoplePage", function () {

        var page = this;

        $('.radioSortBy', this).on('click', function () {
            query.SortBy = this.getAttribute('data-sortby');
            query.StartIndex = 0;
            reloadItems(page);
        });

        $('.radioSortOrder', this).on('click', function () {
            query.SortOrder = this.getAttribute('data-sortorder');
            query.StartIndex = 0;
            reloadItems(page);
        });

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

        $('.chkPersonTypeFilter', this).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.PersonTypes || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.PersonTypes = filters;

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

    }).on('pagebeforeshow', "#tvPeoplePage", function () {

        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
            query.StartIndex = 0;
        }

        LibraryBrowser.loadSavedQueryValues('tvpeople', query);

        reloadItems(this);

    }).on('pageshow', "#tvPeoplePage", function () {

        updateFilterControls(this);
    });

})(jQuery, document);