
(function ($, document) {

    var view = "Poster";

    // The base query options
    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending",
        MediaTypes: "Game",
        Recursive: true,
        Fields: "Genres,Studios,PrimaryImageAspectRatio",
        StartIndex: 0
    };

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            var html = '';

            $('.listTopPaging', page).html(LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, true)).trigger('create');

            if (view == "Backdrop") {
                html += LibraryBrowser.getPosterDetailViewHtml({
                    items: result.Items,
                    preferBackdrop: true,
                    context: "games",
                    shape: "backdrop"
                });
                $('.itemsContainer', page).removeClass('timelineItemsContainer');
            }
            else if (view == "Poster") {
                html += LibraryBrowser.getPosterDetailViewHtml({
                    items: result.Items,
                    context: "games",
                    shape: "poster"
                });
                $('.itemsContainer', page).removeClass('timelineItemsContainer');
            }
            else if (view == "Timeline") {
                html += LibraryBrowser.getPosterDetailViewHtml({
                    items: result.Items,
                    context: "games",
                    shape: "poster",
                    timeline: true
                });
                $('.itemsContainer', page).addClass('timelineItemsContainer');
            }

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

            Dashboard.hideLoadingMsg();
        });
    }

    $(document).on('pageinit', "#gamesPage", function () {

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

        $('.radioPlayers', this).on('click', function () {
            
            query.StartIndex = 0;

            var val = this.getAttribute('data-value');

            query.MinPlayers = val == "all" ? null : val;
            
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

        $('#selectView', this).on('change', function () {

            view = this.value;

            if (view == "Timeline") {

                query.SortBy = "PremiereDate";
                query.StartIndex = 0;
                $('#radioPremiereDate', page)[0].click();

            } else {
                reloadItems(page);
            }
        });

        $('#chkTrailer', this).on('change', function () {

            query.StartIndex = 0;
            query.HasTrailer = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkThemeSong', this).on('change', function () {

            query.StartIndex = 0;
            query.HasThemeSong = this.checked ? true : null;

            reloadItems(page);
        });

        $('#chkThemeVideo', this).on('change', function () {

            query.StartIndex = 0;
            query.HasThemeVideo = this.checked ? true : null;

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

    }).on('pagebeforeshow', "#gamesPage", function () {

        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
            query.StartIndex = 0;
        }

        reloadItems(this);

    }).on('pageshow', "#gamesPage", function () {


        // Reset form values using the last used query
        $('.radioSortBy', this).each(function () {

            this.checked = query.SortBy == this.getAttribute('data-sortby');

        }).checkboxradio('refresh');

        $('.radioSortOrder', this).each(function () {

            this.checked = query.SortOrder == this.getAttribute('data-sortorder');

        }).checkboxradio('refresh');

        $('.radioPlayers', this).each(function () {

            var val = this.getAttribute('data-value');

            if (val == "all") {

                this.checked = query.MinPlayers == null;
            } else {
                this.checked = query.MinPlayers == val;
            }

        }).checkboxradio('refresh');

        $('.chkStandardFilter', this).each(function () {

            var filters = "," + (query.Filters || "");
            var filterName = this.getAttribute('data-filter');

            this.checked = filters.indexOf(',' + filterName) != -1;

        }).checkboxradio('refresh');

        $('#selectView', this).val(view).selectmenu('refresh');

        $('#chkTrailer', this).checked(query.HasTrailer == true).checkboxradio('refresh');
        $('#chkThemeSong', this).checked(query.HasThemeSong == true).checkboxradio('refresh');
        $('#chkThemeVideo', this).checked(query.HasThemeVideo == true).checkboxradio('refresh');

        $('.alphabetPicker', this).alphaValue(query.NameStartsWith);
    });

})(jQuery, document);