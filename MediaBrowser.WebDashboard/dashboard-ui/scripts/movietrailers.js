(function ($, document) {

    var view = LibraryBrowser.getDefaultItemsView('Poster', 'Poster');

    // The base query options
    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending",
        Recursive: true,
        Fields: "PrimaryImageAspectRatio,SortName,SyncInfo",
        StartIndex: 0,
        ImageTypeLimit: 1,
        EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
    };

    function getSavedQueryKey() {

        return 'trailers' + (query.ParentId || '');
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        query.UserId = Dashboard.getCurrentUserId();

        ApiClient.getJSON(ApiClient.getUrl('Trailers', query)).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            if (result.Items.length) {
                $('.noItemsMessage', page).hide();
            }
            else {
                $('.noItemsMessage', page).show();
            }

            var html = '';

            var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                viewButton: true,
                showLimit: false
            });

            $('.listTopPaging', page).html(pagingHtml).trigger('create');

            updateFilterControls(page);

            html = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "portrait",
                context: 'movies-trailers',
                showTitle: false,
                centerText: true,
                lazy: true,
                overlayText: false
            });

            $('.itemsContainer', page).removeClass('timelineItemsContainer');

            html += pagingHtml;

            $('.itemsContainer', page).html(html).trigger('create').createCardMenus();

            $('.btnNextPage', page).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page);
            });

            $('.btnPreviousPage', page).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page);
            });

            LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);

            Dashboard.getCurrentUser().done(function (user) {

                if (user.Configuration.EnableMediaPlayback && result.Items.length) {
                    $('.btnTrailerReel', page).show();
                } else {
                    $('.btnTrailerReel', page).hide();
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

        $('.alphabetPicker', page).alphaValue(query.NameStartsWithOrGreater);
        $('#selectPageSize', page).val(query.Limit).selectmenu('refresh');
    }

    function playReel(page) {

        $('.popupTrailerReel', page).popup('close');

        var reelQuery = {
            UserId: Dashboard.getCurrentUserId(),
            SortBy: 'Random',
            Limit: 50,
            Fields: "MediaSources,Chapters"
        };

        if ($('#chkUnwatchedOnly', page).checked()) {
            reelQuery.Filters = "IsPlayed";
        }

        ApiClient.getJSON(ApiClient.getUrl('Trailers', reelQuery)).done(function (result) {

            MediaController.play({
                items: result.Items
            });
        });
    }

    $(document).on('pageinit', "#movieTrailersPage", function () {

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

        $('.alphabetPicker', this).on('alphaselect', function (e, character) {

            query.NameStartsWithOrGreater = character;
            query.StartIndex = 0;

            reloadItems(page);

        }).on('alphaclear', function (e) {

            query.NameStartsWithOrGreater = '';

            reloadItems(page);
        });

        $('.itemsContainer', page).on('needsrefresh', function () {

            reloadItems(page);

        });

        $('#selectPageSize', page).on('change', function () {
            query.Limit = parseInt(this.value);
            query.StartIndex = 0;
            reloadItems(page);
        });

        $('.btnTrailerReel', page).on('click', function () {

            $('.popupTrailerReel', page).popup('open');

        });

    }).on('pagebeforeshow', "#movieTrailersPage", function () {

        query.ParentId = LibraryMenu.getTopParentId();

        var page = this;
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

    }).on('pageshow', "#movieTrailersPage", function () {

        updateFilterControls(this);
    });

    window.MovieTrailerPage = {

        onSubmit: function () {

            var page = $(this).parents('.page');

            playReel(page);
            return false;
        }
    };

})(jQuery, document);