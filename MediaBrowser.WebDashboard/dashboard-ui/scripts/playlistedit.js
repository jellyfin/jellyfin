(function ($, document) {

    var view = LibraryBrowser.getDefaultItemsView('List', 'List');
    var currentItem;

    // The base query options
    var query = {

        Recursive: true,
        Fields: "PrimaryImageAspectRatio",
        StartIndex: 0
    };

    function getSavedQueryKey() {

        return 'playlists' + (query.ParentId || '');
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        query.ParentId = getParameterByName('id');

        var promise1 = ApiClient.getJSON(ApiClient.getUrl('Playlists/' + query.ParentId + '/Items', { userId: Dashboard.getCurrentUserId() }));
        var promise2 = Dashboard.getCurrentUser();
        var promise3 = ApiClient.getItem(Dashboard.getCurrentUserId(), query.ParentId);

        $.when(promise1, promise2, promise3).done(function (response1, response2, response3) {

            var result = response1[0];
            var user = response2[0];
            var item = response3[0];

            currentItem = item;

            if (MediaController.canPlay(item)) {
                $('.btnPlay', page).removeClass('hide');
            }
            else {
                $('.btnPlay', page).addClass('hide');
            }

            if (item.LocalTrailerCount && item.PlayAccess == 'Full') {
                $('.btnPlayTrailer', page).removeClass('hide');
            } else {
                $('.btnPlayTrailer', page).addClass('hide');
            }

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

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

            if (result.TotalRecordCount) {

                if (view == "List") {

                    html = LibraryBrowser.getListViewHtml({
                        items: result.Items,
                        context: 'playlists',
                        sortBy: query.SortBy,
                        showIndex: false,
                        title: item.Name
                    });
                }

                html += pagingHtml;
                $('.noItemsMessage', page).hide();

            } else {

                $('.noItemsMessage', page).show();
            }

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

        $('#selectView', page).val(view).selectmenu('refresh');

        $('#selectPageSize', page).val(query.Limit).selectmenu('refresh');
    }

    $(document).on('pageinit', "#playlistEditorPage", function () {

        var page = this;

        $('#selectPageSize', page).on('change', function () {
            query.Limit = parseInt(this.value);
            query.StartIndex = 0;
            reloadItems(page);
        });

        $('.btnPlay', page).on('click', function () {
            var userdata = currentItem.UserData || {};

            var mediaType = currentItem.MediaType;

            if (currentItem.Type == "MusicArtist" || currentItem.Type == "MusicAlbum") {
                mediaType = "Audio";
            }

            LibraryBrowser.showPlayMenu(this, currentItem.Id, currentItem.Type, currentItem.IsFolder, mediaType, userdata.PlaybackPositionTicks);
        });

    }).on('pagebeforeshow', "#playlistEditorPage", function () {

        var page = this;

        query.ParentId = LibraryMenu.getTopParentId();

        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
            query.StartIndex = 0;
        }

        var viewkey = getSavedQueryKey();

        LibraryBrowser.loadSavedQueryValues(viewkey, query);
        reloadItems(page);

    }).on('pageshow', "#playlistEditorPage", function () {

        updateFilterControls(this);

    });

})(jQuery, document);