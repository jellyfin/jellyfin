(function ($, document) {

    $(document).on('pageshow', "#moviesPage", function () {

        var page = this;

        var options = {

            SortBy: "SortName",
            SortOrder: "Ascending",
            IncludeItemTypes: "Movie",
            Recursive: true,
            Fields: "PrimaryImageAspectRatio"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            $('#items', page).html(Dashboard.getPosterViewHtml({
                items: result.Items,
                useAverageAspectRatio: true
            }));

        });

    });


})(jQuery, document);