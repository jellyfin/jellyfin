(function ($, document) {

    var view = "Poster";

    // The base query options
    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending",
        Fields: "UserData,DisplayMediaType,ItemCounts,DateCreated",
        StartIndex: 0
    };

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        var userId = Dashboard.getCurrentUserId();

        ApiClient.getItems(userId, query).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            var html = '';

            $('.listTopPaging', page).html(LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, true)).trigger('create');

            if (view == "Backdrop") {
                html += LibraryBrowser.getPosterDetailViewHtml({
                    items: result.Items,
                    preferBackdrop: true
                });
            }
            else if (view == "Poster") {
                html += LibraryBrowser.getPosterDetailViewHtml({
                    items: result.Items
                });
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

        ApiClient.getItem(userId, query.ParentId).done(function (item) {

            var name = item.Name;

            if (item.IndexNumber != null) {
                name = item.IndexNumber + " - " + name;
            }
            if (item.ParentIndexNumber != null) {
                name = item.ParentIndexNumber + "." + name;
            }

            $('#itemName', page).html(name);

            Dashboard.setPageTitle(name);

            if (ApiClient.isWebSocketOpen()) {
                ApiClient.sendWebSocketMessage("Context", [item.Type, item.Id, item.Name].join('|'));
            }

        });

        Dashboard.getCurrentUser().done(function (user) {

            if (user.Configuration.IsAdministrator) {
                $('#editButtonContainer', page).show();
            } else {
                $('#editButtonContainer', page).hide();
            }

        });
    }

    $(document).on('pageinit', "#itemListPage", function () {

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

        $('#selectView', this).on('change', function () {

            view = this.value;

            reloadItems(page);
        });

        $('#btnEdit', page).on('click', function () {

            Dashboard.navigate("edititemmetadata.html?id=" + query.ParentId);
        });

    }).on('pageshow', "#itemListPage", function () {

        query.Limit = LibraryBrowser.getDefaultPageSize();
        query.ParentId = getParameterByName('parentId');
        query.Filters = "";
        query.SortBy = "SortName";
        query.SortOrder = "Ascending";
        query.StartIndex = 0;

        reloadItems(this);

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

        $('#selectView', this).val(view).selectmenu('refresh');
    });

})(jQuery, document);