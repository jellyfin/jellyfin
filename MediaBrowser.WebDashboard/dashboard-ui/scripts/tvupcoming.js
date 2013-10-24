(function ($, document) {

    $(document).on('pagebeforeshow', "#tvUpcomingPage", function () {

        var page = this;

        var now = new Date();

        var options = {

            SortBy: "PremiereDate,AirTime",
            SortOrder: "Ascending",
            IncludeItemTypes: "Episode",
            Limit: 40,
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,SeriesInfo,UserData",
            HasPremiereDate: true,
            MinPremiereDate: LibraryBrowser.getDateParamValue(new Date(now.getFullYear(), now.getMonth(), now.getDate()))
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            if (!result.Items.length) {
                $('#upcomingItems', page).html("Nothing here. To utilize this feature, please enable future episodes in the dashboard metadata configuration.");
                return;
            }
            $('#upcomingItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                useAverageAspectRatio: true,
                showLocationTypeIndicator: false,
                showNewIndicator: false,
                shape: "backdrop",
                showTitle: true,
                showParentTitle: true,
                showPremiereDate: true,
                showPremiereDateIndex: true,
                preferThumb: true
            }));

        });
    });


})(jQuery, document);