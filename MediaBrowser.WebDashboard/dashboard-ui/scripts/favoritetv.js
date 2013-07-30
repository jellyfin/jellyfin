(function ($, document) {

    var shape = "poster";
    
    // The base query options
    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending",
        IncludeItemTypes: "Series",
        Recursive: true,
        Fields: "DisplayMediaType,SeriesInfo,ItemCounts,DateCreated,UserData",
        StartIndex: 0,
        Filters: "IsFavorite"
    };

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            var html = '';

            $('.listTopPaging', page).html(LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, true)).trigger('create');

            html += LibraryBrowser.getPosterDetailViewHtml({
                items: result.Items,
                context: "tv",
                shape: shape
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

            Dashboard.hideLoadingMsg();
        });
    }

    $(document).on('pageinit', "#favoriteTvPage", function () {

        var page = this;

        $('.btnFavoriteType', page).on('click', function () {

            $('.favoriteTypes .ui-btn-active', page).removeClass('ui-btn-active');
            $(this).addClass('ui-btn-active');

        });
        
        $('.btnFavoriteSeries', page).on('click', function () {

            shape = "poster";
            query.IncludeItemTypes = "Series";
            reloadItems(page);
        });

        $('.btnFavoriteSeasons', page).on('click', function () {

            shape = "poster";
            query.IncludeItemTypes = "Season";
            reloadItems(page);
        });

        $('.btnFavoriteEpisodes', page).on('click', function () {

            shape = "backdrop";
            query.IncludeItemTypes = "Episode";
            reloadItems(page);
        });

        $('.btnFavoriteSeries', page).on('click', function () {

            query.IncludeItemTypes = "Series";
            reloadItems(page);
        });

        $('#chkIncludeLikes', page).on('change', function () {

            query.Filters = this.checked ? "IsFavoriteOrLikes" : "IsFavorite";
            reloadItems(page);
        });

        $('.alphabetPicker', page).on('alphaselect', function (e, character) {

            query.NameStartsWithOrGreater = character;
            query.StartIndex = 0;

            reloadItems(page);

        }).on('alphaclear', function (e) {

            query.NameStartsWithOrGreater = '';

            reloadItems(page);
        });

    }).on('pagebeforeshow', "#favoriteTvPage", function () {

        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
            query.StartIndex = 0;
        }

        reloadItems(this);

    }).on('pageshow', "#favoriteTvPage", function () {

        $('.alphabetPicker', this).alphaValue(query.NameStartsWith);
    });

})(jQuery, document);