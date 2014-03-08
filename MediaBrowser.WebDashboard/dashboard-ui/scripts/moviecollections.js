(function ($, document) {

    // The base query options
    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending",
        IncludeItemTypes: "BoxSet",
        Recursive: true,
        Fields: "DateCreated,PrimaryImageAspectRatio",
        StartIndex: 0
    };

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            var html = '';

            $('.listTopPaging', page).html(LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, true)).trigger('create');

            updateFilterControls(page);

            if (result.TotalRecordCount) {
                
                var checkSortOption = $('.radioSortBy:checked', page);
                $('.viewSummary', page).html(LibraryBrowser.getViewSummaryHtml(query, checkSortOption)).trigger('create');

                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "portrait",
                    context: 'movies',
                    useAverageAspectRatio: true,
                    showTitle: true,
                    centerText: true
                });
                
                html += LibraryBrowser.getPagingHtml(query, result.TotalRecordCount);

            } else {
                
                html += '<p>Collections allow you to enjoy personalized groupings of Movies, Series, Albums, Books and Games. Click the New button to start creating Collections.</p>';
            }

            $('#items', page).html(html).trigger('create').createPosterItemHoverMenu();

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

            LibraryBrowser.saveQueryValues('boxsets', query);

            Dashboard.getCurrentUser().done(function(user) {
                
                if (user.Configuration.IsAdministrator) {
                    $('#btnNewCollection', page).removeClass('hide');
                } else {
                    $('#btnNewCollection', page).addClass('hide');
                }

            });

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

        $('#chkTrailer', page).checked(query.HasTrailer == true).checkboxradio('refresh');
        $('#chkThemeSong', page).checked(query.HasThemeSong == true).checkboxradio('refresh');
        $('#chkThemeVideo', page).checked(query.HasThemeVideo == true).checkboxradio('refresh');

        $('.alphabetPicker', page).alphaValue(query.NameStartsWithOrGreater);
    }

    function showNewCollectionPanel(page) {

        $('#newCollectionPanel', page).panel('toggle');

        $('#txtNewCollectionName', page).val('').focus();
    }

    $(document).on('pageinit', "#boxsetsPage", function () {

        var page = this;

        $('.radioSortBy', this).on('click', function () {
            query.SortBy = this.getAttribute('data-sortby');
            reloadItems(page);
        });

        $('.radioSortOrder', this).on('click', function () {
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

        $('#btnNewCollection', page).on('click', function () {

            showNewCollectionPanel(page);
        });

    }).on('pagebeforeshow', "#boxsetsPage", function () {

        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
            query.StartIndex = 0;
        }

        LibraryBrowser.loadSavedQueryValues('boxsets', query);

        reloadItems(this);

    }).on('pageshow', "#boxsetsPage", function () {

        updateFilterControls(this);
    });

    window.BoxSetsPage = {

        onNewCollectionSubmit: function () {

            Dashboard.showLoadingMsg();

            var page = $(this).parents('.page');

            var url = ApiClient.getUrl("Collections", {
                
                Name: $('#txtNewCollectionName', page).val(),
                IsLocked: !$('#chkEnableInternetMetadata', page).checked()

            });

            $.ajax({
                type: "POST",
                url: url

            }).done(function () {

                Dashboard.hideLoadingMsg();

                $('#newCollectionPanel', page).panel('toggle');

                reloadItems(page);

            });

            return false;
        }
    };

})(jQuery, document);