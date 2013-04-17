(function ($, document) {

    var view = "Backdrop";

    // The base query options
    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending",
        IncludeItemTypes: "Movie",
        Recursive: true,
        Fields: "PrimaryImageAspectRatio,UserData,DisplayMediaType,ItemCounts,DateCreated",
        Limit: LibraryBrowser.getDetaultPageSize(),
        StartIndex: 0
    };

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).done(function (result) {

            var html = '';

            var showPaging = result.TotalRecordCount > query.Limit;

            if (showPaging) {
                html += LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, true);
            }

            if (view == "Backdrop") {
                html += LibraryBrowser.getPosterDetailViewHtml({
                    items: result.Items,
                    useAverageAspectRatio: true,
                    preferBackdrop: true
                });
            }
            else if (view == "Poster") {
                html += LibraryBrowser.getPosterDetailViewHtml({
                    items: result.Items,
                    useAverageAspectRatio: true
                });
            }

            if (showPaging) {
                html += LibraryBrowser.getPagingHtml(query, result.TotalRecordCount);
            }

            var elem = $('#items', page).html(html).trigger('create');

            $('select', elem).on('change', function () {
                query.StartIndex = (parseInt(this.value) - 1) * query.Limit;
                reloadItems(page);
            });
            
            $('.btnNextPage', elem).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page);
            });

            $('.btnPreviousPage', elem).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page);
            });

            Dashboard.hideLoadingMsg();
        });
    }

    $(document).on('pageinit', "#moviesPage", function () {

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


        $('.chkVideoTypeFilter', this).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.VideoTypes || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.StartIndex = 0;
            query.VideoTypes = filters;

            reloadItems(page);
        });

        $('#selectView', this).on('change', function () {

            view = this.value;

            reloadItems(page);
        });

        $('#chk3D', this).on('change', function () {

            query.StartIndex = 0;
            query.VideoFormats = this.checked ? this.getAttribute('data-filter') : null;

            reloadItems(page);
        });

    }).on('pagebeforeshow', "#moviesPage", function () {

        reloadItems(this);

    }).on('pageshow', "#moviesPage", function () {


        // Reset form values using the last used query
        $('.radioSortBy', this).each(function () {

            this.checked = query.SortBy == this.getAttribute('data-sortby');

        }).checkboxradio('refresh');

        $('.radioSortOrder', this).each(function () {

            this.checked = query.SortOrder == this.getAttribute('data-sortorder');

        }).checkboxradio('refresh');

        $('.chkStandardFilter', this).each(function () {

            var filters = "," + (query.Filters || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('.chkVideoTypeFilter', this).each(function () {

            var filters = "," + (query.VideoTypes || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('#selectView', this).val(view).selectmenu('refresh');

        $('#chk3D', this).checked(query.VideoFormats == "Digital3D,Sbs3D").checkboxradio('refresh');
    });

})(jQuery, document);