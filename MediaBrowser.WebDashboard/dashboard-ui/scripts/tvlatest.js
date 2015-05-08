(function ($, document) {

    $(document).on('pagebeforeshow', "#tvNextUpPage", function () {

        var userId = Dashboard.getCurrentUserId();

        var parentId = LibraryMenu.getTopParentId();

        var page = this;

        var limit = AppInfo.hasLowImageBandwidth ?
         20 :
         30;

        var options = {

            IncludeItemTypes: "Episode",
            Limit: limit,
            Fields: "PrimaryImageAspectRatio,SyncInfo",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options)).done(function (items) {

            $('#latestEpisodes', page).html(LibraryBrowser.getPosterViewHtml({
                items: items,
                shape: "backdrop",
                preferThumb: true,
                inheritThumb: false,
                showParentTitle: false,
                showUnplayedIndicator: false,
                showChildCountIndicator: true,
                overlayText: true,
                lazy: true

            })).lazyChildren();

        });
    });


})(jQuery, document);