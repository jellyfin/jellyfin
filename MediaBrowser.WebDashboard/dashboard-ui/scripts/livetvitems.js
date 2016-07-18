define(['emby-itemscontainer'], function () {

    var view = LibraryBrowser.getDefaultItemsView('Poster', 'Poster');

    var currentDate = new Date();
    currentDate.setHours(0, 0, 0, 0);

    // The base query options
    var query = {
        UserId: Dashboard.getCurrentUserId(),
        SortBy: "StartDate,SortName",
        SortOrder: "Ascending",
        StartIndex: 0,
        HasAired: false
    };

    function getSavedQueryKey() {
        return LibraryBrowser.getSavedQueryKey();
    }

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getLiveTvPrograms(query).then(function (result) {

            // Scroll back up so they can see the results from the beginning
            window.scrollTo(0, 0);

            var html = '';
            var pagingHtml = LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                showLimit: false
            });

            page.querySelector('.listTopPaging').innerHTML = pagingHtml;

            if (view == "Poster") {
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: query.IsMovie ? 'portrait' : "auto",
                    context: 'livetv',
                    showTitle: false,
                    centerText: true,
                    lazy: true,
                    showStartDateIndex: true,
                    overlayText: false,
                    showProgramAirInfo: true,
                    overlayMoreButton: true
                });
            }
            else if (view == "PosterCard") {
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "portrait",
                    context: 'livetv',
                    showTitle: true,
                    showStartDateIndex: true,
                    lazy: true,
                    cardLayout: true,
                    showProgramAirInfo: true,
                    overlayMoreButton: true
                });
            }

            var elem = page.querySelector('.itemsContainer');
            elem.innerHTML = html + pagingHtml;
            ImageLoader.lazyChildren(elem);

            var i, length;
            var elems;

            function onNextPageClick() {
                query.StartIndex += query.Limit;
                reloadItems(page);
            }

            function onPreviousPageClick() {
                query.StartIndex -= query.Limit;
                reloadItems(page);
            }

            elems = page.querySelectorAll('.btnNextPage');
            for (i = 0, length = elems.length; i < length; i++) {
                elems[i].addEventListener('click', onNextPageClick);
            }

            elems = page.querySelectorAll('.btnPreviousPage');
            for (i = 0, length = elems.length; i < length; i++) {
                elems[i].addEventListener('click', onPreviousPageClick);
            }

            LibraryBrowser.saveQueryValues(getSavedQueryKey(), query);

            Dashboard.hideLoadingMsg();
        });
    }

    pageIdOn('pagebeforeshow', "liveTvItemsPage", function () {

        query.ParentId = LibraryMenu.getTopParentId();

        var page = this;
        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
            query.StartIndex = 0;
        }

        query.IsMovie = getParameterByName('type') == 'movies' ? true : null;
        query.IsSports = getParameterByName('type') == 'sports' ? true : null;
        query.IsKids = getParameterByName('type') == 'kids' ? true : null;

        var viewkey = getSavedQueryKey();

        LibraryBrowser.loadSavedQueryValues(viewkey, query);

        reloadItems(page);
    });

});