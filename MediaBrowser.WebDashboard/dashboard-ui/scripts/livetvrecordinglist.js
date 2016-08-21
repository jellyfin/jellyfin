define(['jQuery', 'cardBuilder', 'dom'], function ($, cardBuilder, dom) {

    // The base query options
    var query = {

        UserId: Dashboard.getCurrentUserId(),
        StartIndex: 0,
        Fields: "CanDelete,PrimaryImageAspectRatio"
    };

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        ApiClient.getLiveTvRecordings(query).then(function (result) {

            // Scroll back up so they can see the results from the beginning
            window.scrollTo(0, 0);

            var html = '';

            $('.listTopPaging', page).html(LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                showLimit: false,
                updatePageSizeSetting: false
            }));

            updateFilterControls();

            var screenWidth = dom.getWindowSize().innerWidth;

            html += cardBuilder.getCardsHtml({

                items: result.Items,
                shape: "auto",
                showTitle: true,
                showParentTitle: true,
                overlayText: screenWidth >= 600,
                coverImage: true

            });

            html += LibraryBrowser.getQueryPagingHtml({
                startIndex: query.StartIndex,
                limit: query.Limit,
                totalRecordCount: result.TotalRecordCount,
                showLimit: false,
                updatePageSizeSetting: false
            });

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

            $('.selectPageSize', page).on('change', function () {
                query.Limit = parseInt(this.value);
                query.StartIndex = 0;
                reloadItems(page);
            });

            if (getParameterByName('savequery') != 'false') {
                LibraryBrowser.saveQueryValues('episodes', query);
            }

            Dashboard.hideLoadingMsg();
        });
    }

    function updateFilterControls(page) {

    }

    $(document).on('pageinit', "#liveTvRecordingListPage", function () {

        var page = this;


    }).on('pagebeforeshow', "#liveTvRecordingListPage", function () {

        var page = this;

        var limit = LibraryBrowser.getDefaultPageSize();

        // If the default page size has changed, the start index will have to be reset
        if (limit != query.Limit) {
            query.Limit = limit;
            query.StartIndex = 0;
        }

        LibraryBrowser.loadSavedQueryValues('episodes', query);

        var groupId = getParameterByName('groupid');
        query.GroupId = groupId;

        reloadItems(page);

        if (query.GroupId) {

            ApiClient.getLiveTvRecordingGroup(query.GroupId).then(function (group) {
                $('.listName', page).html(group.Name);
            });

        } else {
            $('.listName', page).html(Globalize.translate('HeaderAllRecordings'));
        }

        updateFilterControls(this);
    });

});