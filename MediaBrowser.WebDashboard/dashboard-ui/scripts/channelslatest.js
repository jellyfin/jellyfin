(function ($, document) {

    function reloadItems(page) {

        Dashboard.showLoadingMsg();

        var options = {

            Limit: 30,
            Fields: "PrimaryImageAspectRatio",
            Filters: "IsUnplayed",
            UserId: Dashboard.getCurrentUserId()
        };

        $.getJSON(ApiClient.getUrl("Channels/Items/Latest", options)).done(function (result) {

            var html = '';

            html += LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: 'auto',
                showTitle: true,
                centerText: true,
                context: 'channels',
                lazy: true
            });

            $("#items", page).html(html).trigger('create').createPosterItemMenus();
            
            Dashboard.hideLoadingMsg();
        });
    }

    $(document).on('pagebeforeshow', "#channelsLatestPage", function () {

        reloadItems(this);

    });

})(jQuery, document);