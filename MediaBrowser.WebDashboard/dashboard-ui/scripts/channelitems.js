(function ($, document) {

    var maxPageSize;

    // The base query options
    var query = {

        SortBy: "",
        SortOrder: "Ascending",
        Fields: "PrimaryImageAspectRatio,SyncInfo",
        StartIndex: 0
    };

    function getPageSizes() {

        var sizes = [];

        if (!maxPageSize || maxPageSize >= 10) sizes.push(10);
        if (!maxPageSize || maxPageSize >= 20) sizes.push(20);
        if (!maxPageSize || maxPageSize >= 30) sizes.push(30);
        if (!maxPageSize || maxPageSize >= 40) sizes.push(40);
        if (!maxPageSize || maxPageSize >= 50) sizes.push(50);
        if (!maxPageSize || maxPageSize >= 100) sizes.push(100);

        return sizes;
    }

    function getSavedQueryId() {
        return 'channels-1-' + getParameterByName('id') + (getParameterByName('folderId') || '');
    }

    function showLoadingMessage(page) {

        $('#popupDialog', page).popup('open');
    }

    function hideLoadingMessage(page) {
        $('#popupDialog', page).popup('close');
    }

    function reloadFeatures(page) {

        var channelId = getParameterByName('id');

        ApiClient.getJSON(ApiClient.getUrl("Channels/" + channelId + "/Features", query)).done(function (features) {

            if (features.CanFilter) {

                $('.filterControls', page).show();

            } else {
                $('.filterControls', page).hide();
            }

            if (features.SupportsSortOrderToggle) {

                $('.sortOrderToggle', page).show();
            } else {
                $('.sortOrderToggle', page).hide();
            }

            maxPageSize = features.MaxPageSize;

            if (maxPageSize) {
                query.Limit = Math.min(maxPageSize, query.Limit || maxPageSize);
            }

            updateSortOrders(page, features.DefaultSortFields);

            reloadItems(page);
        });
    }

    function updateSortOrders(page, fields) {

        updateSortOrder(page, fields, 'Name');
        updateSortOrder(page, fields, 'CommunityRating');
        updateSortOrder(page, fields, 'PremiereDate');
        updateSortOrder(page, fields, 'PlayCount');
        updateSortOrder(page, fields, 'Runtime');
        updateSortOrder(page, fields, 'DateCreated');
    }

    function updateSortOrder(page, fields, name) {

        var cssClass = "sortby" + name;

        if (fields.indexOf(name) == -1) {

            $('.' + cssClass, page).hide();
        } else {
            $('.' + cssClass, page).show();
        }
    }

    function reloadItems(page) {

        showLoadingMessage(page);

        var channelId = getParameterByName('id');
        var folderId = getParameterByName('folderId');

        query.UserId = Dashboard.getCurrentUserId();

        if (folderId) {

            ApiClient.getItem(query.UserId, folderId).done(function (item) {

                $('.categoryTitle', page).show().html(item.Name);
                $('.channelHeader', page).show().html('<a href="channelitems.html?id=' + item.ChannelId + '">' + item.ChannelName + '</a>').trigger('create');
            });

        } else {

            ApiClient.getItem(query.UserId, channelId).done(function (item) {

                $('.categoryTitle', page).hide().html(item.Name);
                $('.channelHeader', page).show().html('<a href="channelitems.html?id=' + item.Id + '">' + item.Name + '</a>');
            });
        }

        query.folderId = folderId;

        ApiClient.getJSON(ApiClient.getUrl("Channels/" + channelId + "/Items", query)).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            var html = '';

            $('.listTopPaging', page).html(LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                viewButton: true,
                showLimit: false,
                updatePageSizeSetting: false
            })).trigger('create');

            updateFilterControls(page);

            html = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "auto",
                defaultShape: 'square',
                context: 'channels',
                showTitle: true,
                centerText: true,
                coverImage: true
            });

            $('#items', page).html(html).lazyChildren();

            $('.btnNextPage', page).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page);
            });

            $('.btnPreviousPage', page).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page);
            });

            LibraryBrowser.saveQueryValues(getSavedQueryId(), query);


        }).always(function () {

            hideLoadingMessage(page);
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

        $('.alphabetPicker', page).alphaValue(query.NameStartsWith);
        $('#selectPageSize', page).val(query.Limit).selectmenu('refresh');
    }

    $(document).on('pageinit', "#channelItemsPage", function () {

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

        $('#selectPageSize', page).on('change', function () {
            query.Limit = parseInt(this.value);
            query.StartIndex = 0;
            reloadItems(page);
        });

    }).on('pagebeforeshow', "#channelItemsPage", function () {


    }).on('pageshow', "#channelItemsPage", function () {

        var page = this;
        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
        }
        query.StartIndex = 0;

        LibraryBrowser.loadSavedQueryValues(getSavedQueryId(), query);

        reloadFeatures(page);

        updateFilterControls(page);
    });

})(jQuery, document);