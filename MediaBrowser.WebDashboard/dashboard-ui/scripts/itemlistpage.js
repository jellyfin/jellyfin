(function ($, document) {

    var view = "Poster";

    // The base query options
    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending",
        Fields: "DateCreated",
        StartIndex: 0
    };

    var currentItem;

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        var userId = Dashboard.getCurrentUserId();

        ApiClient.getItems(userId, query).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            var html = '';

            $('.listTopPaging', page).html(LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, true)).trigger('create');

            updateFilterControls(page);

            var checkSortOption = $('.radioSortBy:checked', page);
            $('.viewSummary', page).html(LibraryBrowser.getViewSummaryHtml(query, checkSortOption)).trigger('create');

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

            LibraryBrowser.saveQueryValues(getParameterByName('parentId'), query);

            Dashboard.hideLoadingMsg();
        });

        var promise = query.ParentId ? ApiClient.getItem(userId, query.ParentId) : ApiClient.getRootFolder(userId);

        promise.done(function (item) {

            currentItem = item;

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

        $('#selectView', page).val(view).selectmenu('refresh');

        $('.alphabetPicker', page).alphaValue(query.NameStartsWithOrGreater);
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

            LibraryBrowser.saveViewSetting(getParameterByName('parentId'), view);
        });

        $('#btnEdit', page).on('click', function () {

            Dashboard.navigate("edititemmetadata.html?id=" + currentItem.Id);
        });

        $('.alphabetPicker', this).on('alphaselect', function (e, character) {

            query.NameStartsWithOrGreater = character;
            query.StartIndex = 0;

            reloadItems(page);

        }).on('alphaclear', function (e) {

            query.NameStartsWithOrGreater = '';

            reloadItems(page);
        });

    }).on('pageshow', "#itemListPage", function () {

        var page = this;
        
        query.Limit = LibraryBrowser.getDefaultPageSize();
        query.ParentId = getParameterByName('parentId');
        query.Filters = "";
        query.SortBy = "SortName";
        query.SortOrder = "Ascending";
        query.StartIndex = 0;
        query.NameStartsWithOrGreater = '';

        var key = getParameterByName('parentId');
        LibraryBrowser.loadSavedQueryValues(key, query);

        LibraryBrowser.getSavedViewSetting(key).done(function (val) {

            if (val) {
                $('#selectView', page).val(val).selectmenu('refresh').trigger('change');
            } else {
                reloadItems(page);
            }
        });

        updateFilterControls(page);

    }).on('pagehide', "#itemListPage", function () {

        currentItem = null;

    });

})(jQuery, document);