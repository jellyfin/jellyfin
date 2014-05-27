(function ($, document) {

    // The base query options
    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending",
        StartIndex: 0
    };

    function getSavedQueryId() {
        return 'channels-' + getParameterByName('id') + (getParameterByName('folderId') || '');
    }

    function showLoadingMessage(page) {

        $('#popupDialog', page).popup('open');
    }

    function hideLoadingMessage(page) {
        $('#popupDialog', page).popup('close');
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
        query.Limit = 50;
        $.getJSON(ApiClient.getUrl("Channels/" + channelId + "/Items", query)).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            var html = '';

            $('.listTopPaging', page).html(LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, true)).trigger('create');

            updateFilterControls(page);

            html = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "auto",
                context: 'channels',
                showTitle: true,
                centerText: true,
                coverImage: true
            });

            html += LibraryBrowser.getPagingHtml(query, result.TotalRecordCount);

            $('#items', page).html(html).trigger('create').createPosterItemMenus();

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

        reloadItems(page);

        updateFilterControls(page);
    });

})(jQuery, document);