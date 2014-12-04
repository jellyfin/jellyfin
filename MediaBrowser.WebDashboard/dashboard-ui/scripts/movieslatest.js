(function ($, document) {

    $(document).on('pagebeforeshow', "#moviesLatestPage", function () {

        var parentId = LibraryMenu.getTopParentId();
        var userId = Dashboard.getCurrentUserId();
        
        var page = this;

        var options = {

            IncludeItemTypes: "Movie",
            Limit: 30,
            Fields: "PrimaryImageAspectRatio,MediaSourceCount",
            ParentId: parentId,
            IsPlayed: false,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options)).done(function (items) {

            $('#recentlyAddedItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: items,
                lazy: true,
                shape: 'homePagePortrait',
                overlayText: false

            })).trigger('create').createCardMenus();
        });

    });


})(jQuery, document);