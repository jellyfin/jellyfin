(function ($, document) {

    function loadNextUp(page) {

        var screenWidth = $(window).width();

        var query = {

            Limit: 24,
            Fields: "PrimaryImageAspectRatio,SeriesInfo,DateCreated",
            UserId: Dashboard.getCurrentUserId(),
            ExcludeLocationTypes: "Virtual"
        };

        query.ParentId = LibraryMenu.getTopParentId();
        
        if (query.ParentId) {

            $('.scopedLibraryViewNav', page).show();
            $('.globalNav', page).hide();

        } else {
            $('.scopedLibraryViewNav', page).hide();
            $('.globalNav', page).show();
        }

        ApiClient.getNextUpEpisodes(query).done(function (result) {

            if (result.Items.length) {
                $('.noNextUpItems', page).hide();
            } else {
                $('.noNextUpItems', page).show();
            }

            $('#nextUpItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "backdrop",
                showTitle: true,
                showParentTitle: true,
                overlayText: screenWidth >= 600,
                context: 'home-nextup',
                lazy: true

            })).trigger('create').createPosterItemMenus();

        });
    }

    $(document).on('pagebeforeshow', "#tvRecommendedPage", function () {

        var page = this;

        loadNextUp(page);
    });


})(jQuery, document);