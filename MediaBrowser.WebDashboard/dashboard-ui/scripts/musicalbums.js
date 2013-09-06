(function ($, document) {

    var view = "Poster";
    
    // The base query options
    var query = {

        SortBy: "AlbumArtist,SortName",
        SortOrder: "Ascending",
        IncludeItemTypes: "MusicAlbum",
        Recursive: true,
        Fields: "ItemCounts,DateCreated,UserData",
        StartIndex: 0
    };

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            var html = '';

            $('.listTopPaging', page).html(LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, true)).trigger('create');

            if (view == "Poster") {
                html += LibraryBrowser.getPosterDetailViewHtml({
                    items: result.Items,
                    context: "music",
                    shape: "square"
                });
                $('.itemsContainer', page).removeClass('timelineItemsContainer');
            }
            else if (view == "Timeline") {
                html += LibraryBrowser.getPosterDetailViewHtml({
                    items: result.Items,
                    context: "music",
                    shape: "square",
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

    $(document).on('pageinit', "#musicAlbumsPage", function () {

        var page = this;

        $('.radioSortBy', page).on('click', function () {
            query.SortBy = this.getAttribute('data-sortby');
            query.StartIndex = 0;
            
            // Clear this
            $('.alphabetPicker', page).alphaClear();
            query.NameStartsWithOrGreater = '';
            query.AlbumArtistStartsWithOrGreater = '';

            reloadItems(page);
        });

        $('.radioSortOrder', page).on('click', function () {
            query.SortOrder = this.getAttribute('data-sortorder');
            query.StartIndex = 0;

            // Clear this
            $('.alphabetPicker', page).alphaClear();
            query.NameStartsWithOrGreater = '';
            query.AlbumArtistStartsWithOrGreater = '';

            reloadItems(page);
        });

        $('.chkStandardFilter', page).on('change', function () {

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

        $('#selectView', page).on('change', function () {

            view = this.value;

            if (view == "Timeline") {

                query.SortBy = "PremiereDate";
                query.StartIndex = 0;
                $('#radioPremiereDate', page)[0].click();

            } else {
                reloadItems(page);
            }
        });

        $('.alphabetPicker', page).on('alphaselect', function (e, character) {

            if (query.SortBy.indexOf('AlbumArtist') == -1) {
                query.NameStartsWithOrGreater = character;
                query.AlbumArtistStartsWithOrGreater = '';
            } else {
                query.AlbumArtistStartsWithOrGreater = character;
                query.NameStartsWithOrGreater = '';
            }
            
            query.StartIndex = 0;

            reloadItems(page);

        }).on('alphaclear', function (e) {

            query.NameStartsWithOrGreater = '';
            query.AlbumArtistStartsWithOrGreater = '';

            reloadItems(page);
        });

    }).on('pagebeforeshow', "#musicAlbumsPage", function () {

        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
            query.StartIndex = 0;
        }

        reloadItems(this);

    }).on('pageshow', "#musicAlbumsPage", function () {

        $('#selectView', this).val(view).selectmenu('refresh');

        // Reset form values using the last used query
        $('.radioSortBy', this).each(function () {

            this.checked = query.SortBy == this.getAttribute('data-sortby');

        }).checkboxradio('refresh');

        $('.radioSortOrder', this).each(function () {

            this.checked = query.SortOrder == this.getAttribute('data-sortorder');

        }).checkboxradio('refresh');

        $('.alphabetPicker', this).alphaValue(query.NameStartsWith);
    });

})(jQuery, document);