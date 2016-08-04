define(['jQuery', 'listView'], function ($, listView) {

    var data = {};

    function getPageData(context) {
        var key = getSavedQueryKey(context);
        var pageData = data[key];

        if (!pageData) {
            pageData = data[key] = {
                query: {
                    SortBy: "SortName",
                    SortOrder: "Ascending",
                    MediaTypes: "Game",
                    Recursive: true,
                    Fields: "Genres,Studios,PrimaryImageAspectRatio,SortName",
                    ImageTypeLimit: 1,
                    EnableImageTypes: "Primary,Backdrop,Banner,Thumb",
                    StartIndex: 0,
                    Limit: LibraryBrowser.getDefaultPageSize()
                },
                view: LibraryBrowser.getSavedView(key) || 'Poster'
            };

            pageData.query.ParentId = LibraryMenu.getTopParentId();
            LibraryBrowser.loadSavedQueryValues(key, pageData.query);
        }
        return pageData;
    }

    function getQuery(context) {

        return getPageData(context).query;
    }

    function getSavedQueryKey(context) {

        if (!context.savedQueryKey) {
            context.savedQueryKey = LibraryBrowser.getSavedQueryKey('games');
        }
        return context.savedQueryKey;
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        var query = getQuery(page);

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).then(function (result) {

            // Scroll back up so they can see the results from the beginning
            window.scrollTo(0, 0);

            var html = '';

            $('.listTopPaging', page).html(LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                showLimit: false,
                filterButton: true
            }));

            var view = getPageData(page).view;

            if (view == "List") {

                html = listView.getListViewHtml({
                    items: result.Items,
                    context: 'games',
                    sortBy: query.SortBy
                });
            }
            else if (view == "Poster") {
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "auto",
                    context: 'games',
                    showTitle: true,
                    showParentTitle: true,
                    centerText: true
                });
            }
            else if (view == "PosterCard") {
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "auto",
                    context: 'games',
                    showTitle: true,
                    showParentTitle: true,
                    cardLayout: true
                });
            }

            var elem = page.querySelector('#items');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);

            $('.btnNextPage', page).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page);
            });

            $('.btnPreviousPage', page).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page);
            });

            $('.btnFilter', page).on('click', function () {
                showFilterMenu(page);
            });

            LibraryBrowser.saveQueryValues(getSavedQueryKey(page), query);

            Dashboard.hideLoadingMsg();
        });
    }

    function showFilterMenu(page) {

        require(['components/filterdialog/filterdialog'], function (filterDialogFactory) {

            var filterDialog = new filterDialogFactory({
                query: getQuery(page),
                mode: 'games'
            });

            Events.on(filterDialog, 'filterchange', function () {
                reloadItems(page);
            });

            filterDialog.show();
        });
    }

    $(document).on('pageinit', "#gamesPage", function () {

        var page = this;

        $('.alphabetPicker', this).on('alphaselect', function (e, character) {

            var query = getQuery(page);
            query.NameStartsWithOrGreater = character;
            query.StartIndex = 0;

            reloadItems(page);

        }).on('alphaclear', function (e) {

            var query = getQuery(page);
            query.NameStartsWithOrGreater = '';

            reloadItems(page);
        });

    }).on('pagebeforeshow', "#gamesPage", function () {

        var page = this;
        var query = getQuery(page);
        query.ParentId = LibraryMenu.getTopParentId();

        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
            query.StartIndex = 0;
        }

        var viewkey = getSavedQueryKey(page);

        LibraryBrowser.loadSavedQueryValues(viewkey, query);

        LibraryBrowser.getSavedViewSetting(viewkey).then(function (val) {

            if (val) {
                $('#selectView', page).val(val).trigger('change');
            } else {
                reloadItems(page);
            }
        });
    });

});