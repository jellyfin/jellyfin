(function ($, document) {

    // The base query options
    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending",
        IncludeItemTypes: "Movie,Trailer",
        Recursive: true,
        Fields: "DateCreated",
        PersonTypes: "",
        StartIndex: 0
    };
	
	LibraryBrowser.loadSavedQueryValues('moviepeople', query);

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getPeople(Dashboard.getCurrentUserId(), query).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            var html = '';

            $('.listTopPaging', page).html(LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, true)).trigger('create');

            var checkSortOption = $('.radioSortBy:checked', page);
            $('.viewSummary', page).html(LibraryBrowser.getViewSummaryHtml(query, checkSortOption)).trigger('create');

            html += LibraryBrowser.getPosterDetailViewHtml({
                items: result.Items,
                context: "movies"
            });

            html += LibraryBrowser.getPagingHtml(query, result.TotalRecordCount);

            $('#items', page).html(html).trigger('create');

            $('.selectPage', page).on('change', function () {
                query.StartIndex = (parseInt(this.value) - 1) * query.Limit;
                reloadItems(page);
            });

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

			LibraryBrowser.saveQueryValues('moviepeople', query);
			
            Dashboard.hideLoadingMsg();
        });
    }

    $(document).on('pageinit', "#moviePeoplePage", function () {

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

    }).on('pagebeforeshow', "#moviePeoplePage", function () {

        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
            query.StartIndex = 0;
        }

        reloadItems(this);

    }).on('pageshow', "#moviePeoplePage", function () {

        // Reset form values using the last used query
        $('.radioSortBy', this).each(function () {

            this.checked = query.SortBy == this.getAttribute('data-sortby');

        }).checkboxradio('refresh');

        $('.radioSortOrder', this).each(function () {

            this.checked = query.SortOrder == this.getAttribute('data-sortorder');

        }).checkboxradio('refresh');

        $('.chkPersonTypeFilter', this).each(function () {

            var filters = "," + (query.PersonTypes || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('.alphabetPicker', this).alphaValue(query.NameStartsWithOrGreater);
    });

})(jQuery, document);