(function ($, document) {

    // The base query options
    var query = {

        SortBy: "SortName",
        SortOrder: "Ascending",
        Fields: "PrimaryImageAspectRatio,UserData,DisplayMediaType,ItemCounts,DateCreated",
        Limit: LibraryBrowser.getDetaultPageSize(),
        StartIndex: 0
    };

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        var userId = Dashboard.getCurrentUserId();

        ApiClient.getItems(userId, query).done(function (result) {

            var html = '';

            var showPaging = result.TotalRecordCount > query.Limit;

            if (showPaging) {
                html += LibraryBrowser.getPagingHtml(query, result.TotalRecordCount, true);
            }

            html += LibraryBrowser.getPosterDetailViewHtml({
                items: result.Items,
                useAverageAspectRatio: true
            });

            if (showPaging) {
                html += LibraryBrowser.getPagingHtml(query, result.TotalRecordCount);
            }

            var elem = $('#items', page).html(html).trigger('create');

            $('select', elem).on('change', function () {
                query.StartIndex = (parseInt(this.value) - 1) * query.Limit;
                reloadItems(page);
            });

            $('.btnNextPage', elem).on('click', function () {
                query.StartIndex += query.Limit;
                reloadItems(page);
            });

            $('.btnPreviousPage', elem).on('click', function () {
                query.StartIndex -= query.Limit;
                reloadItems(page);
            });

            Dashboard.hideLoadingMsg();
        });

        ApiClient.getItem(userId, query.ParentId).done(function (item) {

            $('#itemName', page).html(item.Name);

        });
    }

    $(document).on('pageinit', "#itemListPage", function () {


    }).on('pagebeforeshow', "#itemListPage", function () {

        query.ParentId = getParameterByName('parentId');
        reloadItems(this);

    }).on('pageshow', "#itemListPage", function () {

    });

})(jQuery, document);