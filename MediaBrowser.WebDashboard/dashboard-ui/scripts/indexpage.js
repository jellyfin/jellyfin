(function ($, document) {

    $(document).on('pageshow', "#indexPage", function () {

        var page = this;

        var userId = Dashboard.getCurrentUserId();
        
        if (!userId) {
            return;
        }

        page = $(page);

        var options = {

            sortBy: "SortName"
        };

        ApiClient.getItems(userId, options).done(function (result) {

            $('#divCollections', page).html(Dashboard.getPosterViewHtml({
                items: result.Items,
                showTitle: true
            }));

        });
    });

})(jQuery, document);