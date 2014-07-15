(function ($, document) {

    $(document).on('pagebeforeshow', "#tvNextUpPage", function () {

        var screenWidth = $(window).width();
        var userId = Dashboard.getCurrentUserId();

        var parentId = LibraryMenu.getTopParentId();

        var page = this;

        var options = {

            IncludeItemTypes: "Episode",
            Limit: screenWidth >= 1920 ? 24 : (screenWidth >= 1440 ? 16 : 15),
            Fields: "PrimaryImageAspectRatio",
            ParentId: parentId,
            IsPlayed: false
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
                overlayText: screenWidth >= 600,
                lazy: true

            })).trigger('create').createPosterItemMenus();

        });
    });


})(jQuery, document);