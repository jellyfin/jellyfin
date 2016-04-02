define(['libraryBrowser', 'jQuery'], function (libraryBrowser, $) {

    return function (view, params) {

        var currentItem;

        var data;

        function getPageData() {
            var pageData = data;

            if (!pageData) {
                pageData = data = {
                    query: {
                        SortBy: "SortName",
                        SortOrder: "Ascending",
                        Fields: "DateCreated,PrimaryImageAspectRatio,MediaSourceCount,SyncInfo",
                        ImageTypeLimit: 1,
                        EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                        StartIndex: 0,
                        Limit: libraryBrowser.getDefaultPageSize()
                    }
                };

                pageData.query.Filters = "";
                pageData.query.NameStartsWithOrGreater = '';
                var key = getSavedQueryKey();
                pageData.view = libraryBrowser.getSavedView(key) || libraryBrowser.getDefaultItemsView('Poster', 'Poster');

                pageData.query.ParentId = params.parentId || null;
                libraryBrowser.loadSavedQueryValues(key, pageData.query);
            }
            return pageData;
        }

        function getQuery() {

            return getPageData().query;
        }
        function getSavedQueryKey() {

            if (!view.savedQueryKey) {
                view.savedQueryKey = libraryBrowser.getSavedQueryKey('items');
            }
            return view.savedQueryKey;
        }

        function reloadItems() {

            Dashboard.showLoadingMsg();

            var query = getQuery();
            var userId = Dashboard.getCurrentUserId();

            var parentItemPromise = query.ParentId ?
               ApiClient.getItem(userId, query.ParentId) :
               ApiClient.getRootFolder(userId);

            var itemsPromise = ApiClient.getItems(userId, query);

            Promise.all([parentItemPromise, itemsPromise]).then(function (responses) {

                var item = responses[0];
                currentItem = item;
                var result = responses[1];

                // Scroll back up so they can see the results from the beginning
                window.scrollTo(0, 0);

                var viewStyle = getPageData(view).view;

                var html = '';
                var pagingHtml = libraryBrowser.getQueryPagingHtml({
                    startIndex: query.StartIndex,
                    limit: query.Limit,
                    totalRecordCount: result.TotalRecordCount,
                    showLimit: false,
                    addLayoutButton: true,
                    currentLayout: viewStyle,
                    sortButton: true,
                    layouts: 'Poster,PosterCard,Thumb',
                    filterButton: true
                });

                view.querySelector('.listTopPaging').innerHTML = pagingHtml;

                updateFilterControls();

                var context = params.context;

                var posterOptions = {
                    items: result.Items,
                    shape: "auto",
                    centerText: true,
                    lazy: true,
                    coverImage: item.Type == 'PhotoAlbum'
                };

                if (viewStyle == "Backdrop") {

                    posterOptions.shape = 'backdrop';
                    posterOptions.showTitle = true;
                    posterOptions.preferBackdrop = true;

                    html = libraryBrowser.getPosterViewHtml(posterOptions);
                }
                else if (viewStyle == "Poster") {

                    posterOptions.showTitle = context == 'photos' ? 'auto' : true;
                    posterOptions.overlayText = context == 'photos';

                    html = libraryBrowser.getPosterViewHtml(posterOptions);
                }
                else if (viewStyle == "PosterCard") {

                    posterOptions.showTitle = true;
                    posterOptions.showYear = true;
                    posterOptions.cardLayout = true;
                    posterOptions.centerText = false;

                    html = libraryBrowser.getPosterViewHtml(posterOptions);
                }
                else if (viewStyle == "Thumb") {

                    posterOptions.preferThumb = true;
                    posterOptions.shape = "backdrop";
                    html = libraryBrowser.getPosterViewHtml(posterOptions);
                }

                var elem = view.querySelector('#items');
                elem.innerHTML = html + pagingHtml;
                ImageLoader.lazyChildren(elem);

                $('.btnFilter', view).on('click', function () {
                    showFilterMenu();
                });

                $('.btnNextPage', view).on('click', function () {
                    query.StartIndex += query.Limit;
                    reloadItems(view);
                });

                $('.btnPreviousPage', view).on('click', function () {
                    query.StartIndex -= query.Limit;
                    reloadItems(view);
                });

                $('.btnChangeLayout', view).on('layoutchange', function (e, layout) {
                    getPageData(view).view = layout;
                    libraryBrowser.saveViewSetting(getSavedQueryKey(), layout);
                    reloadItems(view);
                });

                // On callback make sure to set StartIndex = 0
                $('.btnSort', view).on('click', function () {
                    libraryBrowser.showSortMenu({
                        items: [{
                            name: Globalize.translate('OptionNameSort'),
                            id: 'SortName'
                        },
                        {
                            name: Globalize.translate('OptionCommunityRating'),
                            id: 'CommunityRating,SortName'
                        },
                        {
                            name: Globalize.translate('OptionCriticRating'),
                            id: 'CriticRating,SortName'
                        },
                        {
                            name: Globalize.translate('OptionDateAdded'),
                            id: 'DateCreated,SortName'
                        },
                        {
                            name: Globalize.translate('OptionDatePlayed'),
                            id: 'DatePlayed,SortName'
                        },
                        {
                            name: Globalize.translate('OptionParentalRating'),
                            id: 'OfficialRating,SortName'
                        },
                        {
                            name: Globalize.translate('OptionPlayCount'),
                            id: 'PlayCount,SortName'
                        },
                        {
                            name: Globalize.translate('OptionReleaseDate'),
                            id: 'PremiereDate,SortName'
                        },
                        {
                            name: Globalize.translate('OptionRuntime'),
                            id: 'Runtime,SortName'
                        }],
                        callback: function () {
                            reloadItems(view);
                        },
                        query: query
                    });
                });

                libraryBrowser.saveQueryValues(params.parentId, query);

                var name = item.Name;

                if (item.IndexNumber != null) {
                    name = item.IndexNumber + " - " + name;
                }
                if (item.ParentIndexNumber != null) {
                    name = item.ParentIndexNumber + "." + name;
                }

                LibraryMenu.setTitle(name);

                view.dispatchEvent(new CustomEvent("displayingitem", {
                    detail: {
                        item: item
                    },
                    bubbles: true
                }));

                libraryBrowser.setLastRefreshed(view);
                Dashboard.hideLoadingMsg();
            });
        }

        function showFilterMenu() {

            require(['components/filterdialog/filterdialog'], function (filterDialogFactory) {

                var filterDialog = new filterDialogFactory({
                    query: getQuery()
                });

                Events.on(filterDialog, 'filterchange', function () {
                    reloadItems();
                });

                filterDialog.show();
            });
        }

        function updateFilterControls() {

            var query = getQuery();

            $('.alphabetPicker', view).alphaValue(query.NameStartsWithOrGreater);
        }

        function onListItemClick(e) {

            var query = getQuery();
            var info = libraryBrowser.getListItemInfo(this);

            if (info.mediaType == 'Photo') {
                require(['scripts/photos'], function () {
                    Photos.startSlideshow(view, query, info.id);
                });
                return false;
            }
        }

        $('.alphabetPicker', view).on('alphaselect', function (e, character) {

            var query = getQuery();
            query.NameStartsWithOrGreater = character;
            query.StartIndex = 0;

            reloadItems(view);

        }).on('alphaclear', function (e) {

            var query = getQuery();
            query.NameStartsWithOrGreater = '';

            reloadItems(view);
        });

        $(view).on('click', '.mediaItem', onListItemClick);

        view.addEventListener('viewbeforeshow', function (e) {
            reloadItems(view);
            updateFilterControls();
            LibraryMenu.setBackButtonVisible(params.context);
        });
    };
});