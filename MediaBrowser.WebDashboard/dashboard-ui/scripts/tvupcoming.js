(function ($, document) {

    $(document).on('pagebeforeshow', "#tvUpcomingPage", function () {

        var page = this;

        var query = {

            Limit: 40,
            Fields: "SeriesInfo,UserData,SeriesStudio,SyncInfo",
            UserId: Dashboard.getCurrentUserId(),
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        query.ParentId = LibraryMenu.getTopParentId();

        var context = '';

        if (query.ParentId) {

            $('.scopedLibraryViewNav', page).show();
            $('.globalNav', page).hide();
            context = 'tv';

        } else {
            $('.scopedLibraryViewNav', page).hide();
            $('.globalNav', page).show();
        }

        ApiClient.getJSON(ApiClient.getUrl("Shows/Upcoming", query)).done(function (result) {

            var items = result.Items;

            if (items.length) {
                $('.noItemsMessage', page).hide();
            } else {
                $('.noItemsMessage', page).show();
            }

            $('#upcomingItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: items,
                showLocationTypeIndicator: false,
                shape: "homePageBackdrop",
                showTitle: true,
                showPremiereDate: true,
                showPremiereDateIndex: true,
                preferThumb: true,
                context: context || 'home-upcoming',
                lazy: true,

            })).trigger('create').createCardMenus();
        });
    });


})(jQuery, document);