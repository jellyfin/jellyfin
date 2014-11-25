(function ($, document) {

    $(document).on('pagebeforeshow', "#tvNextUpPage", function () {

        var screenWidth = $(window).width();
        var userId = Dashboard.getCurrentUserId();

        var parentId = LibraryMenu.getTopParentId();

        var page = this;

        var options = {

            IncludeItemTypes: "Episode",
            Limit: 24,
            Fields: "PrimaryImageAspectRatio",
            ParentId: parentId,
            IsPlayed: false
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
                overlayText: screenWidth >= 600,
                lazy: true

            })).trigger('create').createCardMenus();

        });
    });


})(jQuery, document);