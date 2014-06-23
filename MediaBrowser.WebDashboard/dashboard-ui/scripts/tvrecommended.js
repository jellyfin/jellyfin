(function ($, document) {

    function reload(page) {

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
            $('.homeEhsContent', page).css('text-align', 'left');
            $('.scopedContent', page).show();

            loadResume(page);

        } else {
            $('.scopedLibraryViewNav', page).hide();
            $('.globalNav', page).show();
            $('.homeEhsContent', page).css('text-align', 'center');
            $('.scopedContent', page).hide();
        }

        loadNextUp(page);
    }

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
            $('.ehsContent', page).css('text-align', 'left').removeClass('homeEhsContent');
            $('.scopedContent', page).show();

        } else {
            $('.scopedLibraryViewNav', page).hide();
            $('.globalNav', page).show();
            $('.ehsContent', page).css('text-align', 'center').addClass('homeEhsContent');
            $('.scopedContent', page).hide();
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

    function loadResume(page) {

        var screenWidth = $(window).width();

        var parentId = LibraryMenu.getTopParentId();

        var options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            IncludeItemTypes: "Episode",
            Filters: "IsResumable",
            Limit: screenWidth >= 1920 ? 5 : 4,
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,SeriesInfo,UserData",
            ExcludeLocationTypes: "Virtual",
            ParentId: parentId
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            if (result.Items.length) {
                $('#resumableSection', page).show();
                $('.nextUpHeader', page).removeClass('firstListHeader');
            } else {
                $('#resumableSection', page).hide();
                $('.nextUpHeader', page).addClass('firstListHeader');
            }

            $('#resumableItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "backdrop",
                showTitle: true,
                showParentTitle: true,
                overlayText: screenWidth >= 600

            })).createPosterItemMenus();

        });
    }

    $(document).on('pagebeforeshow', "#tvRecommendedPage", function () {

        var page = this;

        reload(page);
    });


})(jQuery, document);