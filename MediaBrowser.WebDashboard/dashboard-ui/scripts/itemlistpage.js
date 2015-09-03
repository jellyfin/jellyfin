(function ($, document) {

    var view = LibraryBrowser.getDefaultItemsView('Poster', 'Poster');

    var currentItem;

    var data = {};

    function getQuery() {

        var key = getSavedQueryKey();
        var pageData = data[key];

        if (!pageData) {
            pageData = data[key] = {
                query: {
                    SortBy: "SortName",
                    SortOrder: "Ascending",
                    Fields: "DateCreated,PrimaryImageAspectRatio,MediaSourceCount,SyncInfo",
                    ImageTypeLimit: 1,
                    EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                    StartIndex: 0,
                    Limit: LibraryBrowser.getDefaultPageSize()
                }
            };

            pageData.query.Filters = "";
            pageData.query.NameStartsWithOrGreater = '';

            pageData.query.ParentId = getParameterByName('parentId');
            LibraryBrowser.loadSavedQueryValues(key, pageData.query);
        }
        return pageData.query;
    }

    function getSavedQueryKey() {

        return getWindowUrl();
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        var query = getQuery();
        var userId = Dashboard.getCurrentUserId();

        var parentItemPromise = query.ParentId ?
           ApiClient.getItem(userId, query.ParentId) :
           ApiClient.getRootFolder(userId);

        var itemsPromise = ApiClient.getItems(userId, query);

        $.when(parentItemPromise, itemsPromise).done(function (r1, r2) {

            var item = r1[0];
            currentItem = item;
            var result = r2[0];

            // Scroll back up so they can see the results from the beginning
            window.scrollTo(0, 0);

            var html = '';
            var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                viewButton: true,
                showLimit: false
            });

            page.querySelector('.listTopPaging').innerHTML = pagingHtml;

            updateFilterControls(page);

            var context = getParameterByName('context');

            var posterOptions = {
                items: result.Items,
                shape: "auto",
                centerText: true,
                lazy: true
            };

            if (view == "Backdrop") {

                posterOptions.shape = 'backdrop';
                posterOptions.showTitle = true;
                posterOptions.preferBackdrop = true;

                html = LibraryBrowser.getPosterViewHtml(posterOptions);
            }
            else if (view == "Poster") {

                posterOptions.showTitle = context == 'photos' ? 'auto' : true;
                posterOptions.overlayText = context == 'photos';

                html = LibraryBrowser.getPosterViewHtml(posterOptions);
            }
            else if (view == "PosterCard") {

                posterOptions.showTitle = true;
                posterOptions.showYear = true;
                posterOptions.cardLayout = true;

                html = LibraryBrowser.getPosterViewHtml(posterOptions);
            }

            var elem = page.querySelector('#items');
            elem.innerHTML = html + pagingHtml;
            ImageLoader.lazyChildren(elem);

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

            LibraryMenu.setTitle(name);

            $(page).trigger('displayingitem', [{

                item: item
            }]);

            LibraryBrowser.setLastRefreshed(page);
            Dashboard.hideLoadingMsg();
        });
    }

    function updateFilterControls(page) {

        var query = getQuery();
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

        $('#selectView', page).val(view);

        $('.alphabetPicker', page).alphaValue(query.NameStartsWithOrGreater);
        $('#selectPageSize', page).val(query.Limit);
    }

    function onListItemClick(e) {

        var query = getQuery();
        var page = $(this).parents('.page');
        var info = LibraryBrowser.getListItemInfo(this);

        if (info.mediaType == 'Photo') {
            require(['scripts/photos'], function () {
                Photos.startSlideshow(page, query, info.id);
            });
            return false;
        }
    }

    $(document).on('pageinit', "#itemListPage", function () {

        var page = this;

        $('.radioSortBy', this).on('click', function () {
            var query = getQuery();
            query.StartIndex = 0;
            query.SortBy = this.getAttribute('data-sortby');
            reloadItems(page);
        });

        $('.radioSortOrder', this).on('click', function () {
            var query = getQuery();
            query.StartIndex = 0;
            query.SortOrder = this.getAttribute('data-sortorder');
            reloadItems(page);
        });

        $('.chkStandardFilter', this).on('change', function () {

            var query = getQuery();
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

        $('.alphabetPicker', this).on('alphaselect', function (e, character) {

            var query = getQuery();
            query.NameStartsWithOrGreater = character;
            query.StartIndex = 0;

            reloadItems(page);

        }).on('alphaclear', function (e) {

            var query = getQuery();
            query.NameStartsWithOrGreater = '';

            reloadItems(page);
        });

        $('#selectPageSize', page).on('change', function () {
            var query = getQuery();
            query.Limit = parseInt(this.value);
            query.StartIndex = 0;
            reloadItems(page);
        });

        $(page).on('click', '.mediaItem', onListItemClick);

    }).on('pagebeforeshow', "#itemListPage", function () {

        var page = this;

        if (LibraryBrowser.needsRefresh(page)) {
            LibraryBrowser.getSavedViewSetting(getSavedQueryKey()).done(function (val) {

                if (val) {
                    $('#selectView', page).val(val).trigger('change');
                } else {
                    reloadItems(page);
                }
            });
        }

        updateFilterControls(page);
        LibraryMenu.setBackButtonVisible(getParameterByName('context'));

    }).on('pagebeforehide', "#itemListPage", function () {

        currentItem = null;

    });

})(jQuery, document);