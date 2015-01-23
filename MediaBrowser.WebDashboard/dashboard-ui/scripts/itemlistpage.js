(function ($, document) {

    var view = "Poster";

    // The base query options
    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending",
        Fields: "DateCreated,PrimaryImageAspectRatio,MediaSourceCount,SyncInfo",
        StartIndex: 0,
        ImageTypeLimit: 1,
        EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
    };

    var currentItem;

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        var userId = Dashboard.getCurrentUserId();

        var parentItemPromise = query.ParentId ?
           ApiClient.getItem(userId, query.ParentId) :
           ApiClient.getRootFolder(userId);

        var itemsPromise = ApiClient.getItems(userId, query);

        $.when(parentItemPromise, itemsPromise).done(function(r1, r2) {

            var item = r1[0];
            currentItem = item;
            var result = r2[0];

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            var html = '';

            $('.listTopPaging', page).html(LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                viewButton: true,
                showLimit: false
            })).trigger('create');

            updateFilterControls(page);

            var context = getParameterByName('context');

            if (context == 'home') {
                context = 'folders';
            }

            var defaultAction = currentItem.Type == 'PhotoAlbum' ? 'photoslideshow' : null;

            if (view == "Backdrop") {

                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "smallBackdrop",
                    showTitle: true,
                    centerText: true,
                    preferBackdrop: true,
                    context: context,
                    defaultAction: defaultAction
                });
            }
            else if (view == "Poster") {
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "auto",
                    showTitle: true,
                    centerText: true,
                    context: context,
                    defaultAction: defaultAction
                });
            }

            $('#items', page).html(html).lazyChildren();

            $('.btnNextPage', page).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page);
            });

            $('.btnPreviousPage', page).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page);
            });

            LibraryBrowser.saveQueryValues(getParameterByName('parentId'), query);

            var name = item.Name;

            if (item.IndexNumber != null) {
                name = item.IndexNumber + " - " + name;
            }
            if (item.ParentIndexNumber != null) {
                name = item.ParentIndexNumber + "." + name;
            }

            $('#itemName', page).html(name);

            Dashboard.setPageTitle(name);

            $(page).trigger('displayingitem', [{

                item: item
            }]);

            Dashboard.hideLoadingMsg();
        });

        Dashboard.getCurrentUser().done(function (user) {

            if (user.Policy.IsAdministrator && query.ParentId) {
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
        $('#selectPageSize', page).val(query.Limit).selectmenu('refresh');
    }

    function startSlideshow(page, index) {

        index += (query.StartIndex || 0);

        var userId = Dashboard.getCurrentUserId();

        var localQuery = $.extend({}, query);
        localQuery.StartIndex = 0;
        localQuery.Limit = null;
        localQuery.MediaTypes = "Photo";
        localQuery.Recursive = true;
        localQuery.Filters = "IsNotFolder";

        ApiClient.getItems(userId, localQuery).done(function(result) {

            showSlideshow(page, result.Items, index);
        });
    }

    function showSlideshow(page, items, index) {

        var slideshowItems = items.map(function (item) {

            var imgUrl = ApiClient.getScaledImageUrl(item.Id, {
                
                tag: item.ImageTags.Primary,
                type: 'Primary'

            });

            return {
                title: item.Name,
                href: imgUrl
            };
        });

        index = Math.max(index || 0, 0);

        $.swipebox(slideshowItems, {
            initialIndexOnArray: index,
            hideBarsDelay: 30000
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

        $('#selectPageSize', page).on('change', function () {
            query.Limit = parseInt(this.value);
            query.StartIndex = 0;
            reloadItems(page);
        });

        $('.itemsContainer', page).on('photoslideshow', function (e, index) {
            startSlideshow(page, index);
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