(function ($, document) {

    function reload(page) {

        var query = {

            Limit: 24,
            Fields: "PrimaryImageAspectRatio,SeriesInfo,DateCreated",
            UserId: Dashboard.getCurrentUserId(),
            ExcludeLocationTypes: "Virtual"
        };

        query.ParentId = LibraryMenu.getTopParentId();
        var context = '';

        if (query.ParentId) {

            $('.scopedLibraryViewNav', page).show();
            $('.globalNav', page).hide();
            $('.ehsContent', page).css('text-align', 'left');
            $('.scopedContent', page).show();
            context = 'tv';

            loadResume(page);

        } else {
            $('.scopedLibraryViewNav', page).hide();
            $('.globalNav', page).show();
            $('.ehsContent', page).css('text-align', 'center');
            $('.scopedContent', page).hide();
        }

        loadNextUp(page, context || 'home-nextup');
    }

    function loadNextUp(page, context) {

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
            $('.ehsContent', page).css('text-align', 'left');
            $('.scopedContent', page).show();

        } else {
            $('.scopedLibraryViewNav', page).hide();
            $('.globalNav', page).show();
            $('.ehsContent', page).css('text-align', 'center');
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
                context: context,
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
            Limit: screenWidth >= 1200 ? 6 : 4,
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
                overlayText: screenWidth >= 600,
                lazy: true,
                context: 'tv'

            })).trigger('create').createPosterItemMenus();

        });
    }

    $(document).on('pagebeforeshow', "#tvRecommendedPage", function () {

        var page = this;

        reload(page);
    });


})(jQuery, document);