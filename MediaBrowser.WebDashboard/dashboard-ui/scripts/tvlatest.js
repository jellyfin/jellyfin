(function ($, document) {

    $(document).on('pagebeforeshow', "#tvNextUpPage", function () {

        var userId = Dashboard.getCurrentUserId();

        var parentId = LibraryMenu.getTopParentId();

        var page = this;

        var options = {

            IncludeItemTypes: "Episode",
            Limit: 30,
            Fields: "PrimaryImageAspectRatio,SyncInfo",
            ParentId: parentId,
            IsPlayed: false,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options)).done(function (items) {

            $('#latestEpisodes', page).html(LibraryBrowser.getPosterViewHtml({
                items: items,
                shape: "homePageBackdrop",
                preferThumb: true,
                inheritThumb: false,
                showParentTitle: false,
                showUnplayedIndicator: false,
                showChildCountIndicator: true,
                overlayText: true,
                lazy: true

            })).trigger('create').createCardMenus();

        });
    });


})(jQuery, document);