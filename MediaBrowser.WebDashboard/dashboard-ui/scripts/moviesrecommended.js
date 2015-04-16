(function ($, document) {

    function loadLatest(page, userId, parentId) {

        var options = {

            IncludeItemTypes: "Movie",
            Limit: 18,
            Fields: "PrimaryImageAspectRatio,MediaSourceCount,SyncInfo",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options)).done(function (items) {

            $('#recentlyAddedItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: items,
                lazy: true,
                shape: 'portrait',
                overlayText: false

            })).lazyChildren().trigger('create');
        });
    }

    function getRecommendationHtml(recommendation) {

        var html = '';

        var title = '';

        switch (recommendation.RecommendationType) {

            case 'SimilarToRecentlyPlayed':
                title = Globalize.translate('RecommendationBecauseYouWatched').replace("{0}", recommendation.BaselineItemName);
                break;
            case 'SimilarToLikedItem':
                title = Globalize.translate('RecommendationBecauseYouLike').replace("{0}", recommendation.BaselineItemName);
                break;
            case 'HasDirectorFromRecentlyPlayed':
            case 'HasLikedDirector':
                title = Globalize.translate('RecommendationDirectedBy').replace("{0}", recommendation.BaselineItemName);
                break;
            case 'HasActorFromRecentlyPlayed':
            case 'HasLikedActor':
                title = Globalize.translate('RecommendationStarring').replace("{0}", recommendation.BaselineItemName);
                break;
        }

        html += '<h1 class="listHeader">' + title + '</h1>';

        html += '<div>';
        html += LibraryBrowser.getPosterViewHtml({
            items: recommendation.Items,
            lazy: true,
            shape: 'portrait',
            overlayText: true
        });
        html += '</div>';

        return html;
    }

    $(document).on('pageinit', "#moviesRecommendedPage", function () {

        var page = this;

        $('.recommendations', page).createCardMenus();

    }).on('pagebeforeshow', "#moviesRecommendedPage", function () {

        var parentId = LibraryMenu.getTopParentId();

        var screenWidth = $(window).width();

        var page = this;
        var userId = Dashboard.getCurrentUserId();

        var options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            IncludeItemTypes: "Movie",
            Filters: "IsResumable",
            Limit: screenWidth >= 1920 ? 6 : (screenWidth >= 1600 ? 4 : 3),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,MediaSourceCount,SyncInfo",
            CollapseBoxSetItems: false,
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        ApiClient.getItems(userId, options).done(function (result) {

            if (result.Items.length) {
                $('#resumableSection', page).show();
            } else {
                $('#resumableSection', page).hide();
            }

            $('#resumableItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                preferThumb: true,
                shape: 'backdrop',
                overlayText: true,
                showTitle: true,
                lazy: true

            })).lazyChildren().trigger('create');

        });

        loadLatest(page, userId, parentId);

        var url = ApiClient.getUrl("Movies/Recommendations", {

            userId: userId,
            categoryLimit: screenWidth >= 1200 ? 4 : 3,
            ItemLimit: screenWidth >= 1920 ? 9 : (screenWidth >= 1600 ? 8 : (screenWidth >= 1200 ? 7 : 6)),
            Fields: "PrimaryImageAspectRatio,MediaSourceCount,SyncInfo",
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        });

        ApiClient.getJSON(url).done(function (recommendations) {

            if (!recommendations.length) {

                $('.noItemsMessage', page).show();
                $('.recommendations', page).html('');
                return;
            }

            var html = recommendations.map(getRecommendationHtml).join('');

            $('.noItemsMessage', page).hide();
            $('.recommendations', page).html(html).lazyChildren();
        });

    });


})(jQuery, document);