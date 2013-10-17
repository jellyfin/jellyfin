(function ($, document) {

    var defaultSortBy = "Album,SortName";

    // The base query options
    var query = {

        SortBy: defaultSortBy,
        SortOrder: "Ascending",
        IncludeItemTypes: "Audio",
        Recursive: true,
        Fields: "DateCreated,AudioInfo,ParentId",
        Limit: 200,
        StartIndex: 0
    };
	
	LibraryBrowser.loadSavedQueryValues('songs', query);

    function updateFilterControls(page) {

        // Reset form values using the last used query
        $('.radioSortBy', page).each(function () {

            this.checked = query.SortBy == this.getAttribute('data-sortby');

        }).checkboxradio('refresh');

        $('.radioSortOrder', page).each(function () {

            this.checked = query.SortOrder == this.getAttribute('data-sortorder');

        }).checkboxradio('refresh');
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            var html = '';

            $('.listTopPaging', page).html(LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, true)).trigger('create');

            var checkSortOption = $('.radioSortBy:checked', page);
            $('.viewSummary', page).html(LibraryBrowser.getViewSummaryHtml(query, checkSortOption)).trigger('create');

            html += LibraryBrowser.getSongTableHtml(result.Items, {
                showAlbum: true,
                showArtist: true,
                enableColumnSorting: true,
                sortBy: query.SortBy,
                sortOrder: query.SortOrder
            });

            html += LibraryBrowser.getPagingHtml(query, result.TotalRecordCount);

            $('#items', page).html(html).trigger('create');

            $('.btnChangeToDefaultSort', page).on('click', function () {
                $('.defaultSort', page)[0].click();
            });

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

            $('.lnkColumnSort', page).on('click', function () {

                var order = this.getAttribute('data-sortfield');

                if (query.SortBy == order) {

                    if (query.SortOrder == "Descending") {

                        query.SortOrder = "Ascending";
                        query.SortBy = defaultSortBy;

                    } else {

                        query.SortOrder = "Descending";
                        query.SortBy = order;
                    }

                } else {

                    query.SortOrder = "Ascending";
                    query.SortBy = order;
                }

                query.StartIndex = 0;

                reloadItems(page);
            });
			
			LibraryBrowser.saveQueryValues('songs', query);

            Dashboard.hideLoadingMsg();

            $(page).trigger('itemsreloaded');
        });
    }

    $(document).on('pageinit', "#songsPage", function () {

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

    }).on('pagebeforeshow', "#songsPage", function () {

        reloadItems(this);

    }).on('pageshow', "#songsPage", function () {

        updateFilterControls(this);

    }).on('itemsreloaded', "#songsPage", function () {

        updateFilterControls(this);
    });

})(jQuery, document);