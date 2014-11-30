(function ($, document) {

    $(document).on('pagebeforeshow', "#moviesLatestPage", function () {

        var parentId = LibraryMenu.getTopParentId();
        var userId = Dashboard.getCurrentUserId();
        
        var screenWidth = $(window).width();

        var page = this;

        var options = {

            IncludeItemTypes: "Movie",
            Limit: screenWidth >= 1600 ? 28 : (screenWidth >= 1440 ? 30 : (screenWidth >= 800 ? 28 : 18)),
            Fields: "PrimaryImageAspectRatio,MediaSourceCount",
            ParentId: parentId,
            IsPlayed: false,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Logo,Thumb"
        };

        ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options)).done(function (items) {

            $('#recentlyAddedItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: items,
                lazy: true,
                shape: 'homePagePortrait',
                overlayText: true

            })).trigger('create').createCardMenus();
        });

    });


})(jQuery, document);