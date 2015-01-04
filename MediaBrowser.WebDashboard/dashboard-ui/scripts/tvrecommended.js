(function ($, document) {

    function reload(page) {

        var query = {

            Limit: 24,
            Fields: "PrimaryImageAspectRatio,SeriesInfo,DateCreated,SyncInfo",
            UserId: Dashboard.getCurrentUserId(),
            ExcludeLocationTypes: "Virtual",
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        query.ParentId = LibraryMenu.getTopParentId();
        var context = '';

        if (query.ParentId) {

            $('.scopedLibraryViewNav', page).show();
            $('.globalNav', page).hide();
            $('.scopedContent', page).show();
            context = 'tv';
            $('.nextUpHeader', page).removeClass('firstListHeader');

            loadResume(page);

        } else {
            $('.scopedLibraryViewNav', page).hide();
            $('.globalNav', page).show();
            $('.scopedContent', page).hide();
            $('.nextUpHeader', page).addClass('firstListHeader');
        }

        loadNextUp(page, context || 'home-nextup');
    }

    function loadNextUp(page, context) {

        var query = {

            Limit: 24,
            Fields: "PrimaryImageAspectRatio,SeriesInfo,DateCreated,SyncInfo",
            UserId: Dashboard.getCurrentUserId(),
            ExcludeLocationTypes: "Virtual",
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        query.ParentId = LibraryMenu.getTopParentId();

        ApiClient.getNextUpEpisodes(query).done(function (result) {

            if (result.Items.length) {
                $('.noNextUpItems', page).hide();
            } else {
                $('.noNextUpItems', page).show();
            }

            var html = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "homePageBackdrop",
                showTitle: true,
                showParentTitle: true,
                overlayText: true,
                context: context,
                lazy: true

            });

            $('#nextUpItems', page).html(html).trigger('create').createCardMenus();

        });
    }

    function loadResume(page) {

        var parentId = LibraryMenu.getTopParentId();

        var options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            IncludeItemTypes: "Episode",
            Filters: "IsResumable",
            Limit: 6,
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,SeriesInfo,UserData,SyncInfo",
            ExcludeLocationTypes: "Virtual",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            if (result.Items.length) {
                $('#resumableSection', page).show();
            } else {
                $('#resumableSection', page).hide();
                $('.nextUpHeader', page).addClass('firstListHeader');
            }

            $('#resumableItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "homePageBackdrop",
                showTitle: true,
                showParentTitle: true,
                overlayText: true,
                lazy: true,
                context: 'tv'

            })).trigger('create').createCardMenus();

        });
    }

    $(document).on('pagebeforeshow', "#tvRecommendedPage", function () {

        var page = this;

        reload(page);
    });


})(jQuery, document);