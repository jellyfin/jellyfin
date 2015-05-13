(function ($, document) {

    var view = LibraryBrowser.getDefaultItemsView('Poster', 'PosterCard');

    // The base query options
    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending",
        Fields: "PrimaryImageAspectRatio,SortName,SyncInfo",
        StartIndex: 0,
        ImageTypeLimit: 1,
        EnableImageTypes: "Primary"
    };

    function getSavedQueryKey() {

        return 'photos' + (query.ParentId || '');
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).done(function (result) {

            // Scroll back up so they can see the results from the beginning
            $(document).scrollTop(0);

            var html = '';
            var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                viewButton: false,
                showLimit: false
            });

            $('.listTopPaging', page).html(pagingHtml).trigger('create');

            updateFilterControls(page);

            if (AppInfo.hasLowImageBandwidth) {
                if (view == 'Poster') {
                    view = 'PosterCard';
                }
            }

            if (view == "Poster") {
                // Poster
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "auto",
                    context: getParameterByName('context') || 'photos',
                    showTitle: query.MediaTypes != 'Photo',
                    overlayText: true,
                    lazy: true
                });
            }
            else if (view == "PosterCard") {

                // Poster
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "auto",
                    context: getParameterByName('context') || 'photos',
                    showTitle: true,
                    lazy: true,
                    cardLayout: true,
                    showPremiereDate: true
                });
            }

            var elem = $('#items', page).html(html).lazyChildren();

            $(pagingHtml).appendTo(elem).trigger('create');

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
    }

    var filtersLoaded;
    function reloadFiltersIfNeeded(page) {

        if (!filtersLoaded) {

            filtersLoaded = true;

            QueryFilters.loadFilters(page, Dashboard.getCurrentUserId(), query, function () {

                reloadItems(page);
            });
        }
    }

    function setQueryPerContext(page) {

        var context = getParameterByName('context');

        $('.libraryViewNav a', page).removeClass('ui-btn-active');

        if (context == 'photos-photos') {
            query.Recursive = true;
            query.MediaTypes = 'Photo';
            $('.lnkPhotos', page).addClass('ui-btn-active');
        }
        else if (context == 'photos-videos') {
            query.Recursive = true;
            query.MediaTypes = 'Video';
            $('.lnkVideos', page).addClass('ui-btn-active');
        }
        else {
            query.Recursive = false;
            query.MediaTypes = null;
            $('.lnkPhotoAlbums', page).addClass('ui-btn-active');
        }

        query.ParentId = getParameterByName('parentId') || LibraryMenu.getTopParentId();
    }

    function startSlideshow(page, itemQuery, startItemId) {

        var userId = Dashboard.getCurrentUserId();

        var localQuery = $.extend({}, itemQuery);
        localQuery.StartIndex = 0;
        localQuery.Limit = null;
        localQuery.MediaTypes = "Photo";
        localQuery.Recursive = true;
        localQuery.Filters = "IsNotFolder";

        ApiClient.getItems(userId, localQuery).done(function (result) {

            showSlideshow(page, result.Items, startItemId);
        });
    }

    function showSlideshow(page, items, startItemId) {

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

        var index = items.map(function (i) {
            return i.Id;

        }).indexOf(startItemId);

        if (index == -1) {
            index = 0;
        }

        Dashboard.loadSwipebox().done(function () {

            $.swipebox(slideshowItems, {
                initialIndexOnArray: index,
                hideBarsDelay: 30000
            });
        });
    }

    function onListItemClick(e) {

        var page = $(this).parents('.page');
        var info = LibraryBrowser.getListItemInfo(this);

        if (info.mediaType == 'Photo') {
            Photos.startSlideshow(page, query, info.id);
            return false;
        }
    }

    $(document).on('pageinit', "#photosPage", function () {

        var page = this;

        $('.viewPanel', page).on('panelopen', function () {

            reloadFiltersIfNeeded(page);
        });

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

        $(page).on('click', '.mediaItem', onListItemClick);

        $('.chkStandardFilter', this).on('change', function () {

            var filterName = this.getAttribute('data-filter');
            var filters = query.Filters || "";

            filters = (',' + filters).replace(',' + filterName, '').substring(1);

            if (this.checked) {
                filters = filters ? (filters + ',' + filterName) : filterName;
            }

            query.Filters = filters;
            query.StartIndex = 0;
            reloadItems(page);
        });

        $('#radioBasicFilters', this).on('change', function () {

            if (this.checked) {
                $('.basicFilters', page).show();
                $('.advancedFilters', page).hide();
            } else {
                $('.basicFilters', page).hide();
            }
        });

        $('#radioAdvancedFilters', this).on('change', function () {

            if (this.checked) {
                $('.advancedFilters', page).show();
                $('.basicFilters', page).hide();
            } else {
                $('.advancedFilters', page).hide();
            }
        });

        $('#selectPageSize', page).on('change', function () {
            query.Limit = parseInt(this.value);
            query.StartIndex = 0;
            reloadItems(page);
        });

    }).on('pagebeforeshow', "#photosPage", function () {

        var page = this;

        setQueryPerContext(page);

        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
            query.StartIndex = 0;
        }

        var viewKey = getSavedQueryKey();

        LibraryBrowser.loadSavedQueryValues(viewKey, query);
        QueryFilters.onPageShow(page, query);

        LibraryBrowser.getSavedViewSetting(viewKey).done(function (val) {

            if (val) {
                $('#selectView', page).val(val).selectmenu('refresh').trigger('change');
            } else {
                reloadItems(page);
            }
        });

    }).on('pageshow', "#photosPage", function () {

        updateFilterControls(this);
    });

    window.Photos = {
        startSlideshow: startSlideshow
    };

})(jQuery, document);