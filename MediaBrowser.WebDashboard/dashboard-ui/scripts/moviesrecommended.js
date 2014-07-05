(function ($, document) {

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
            lazy: true
        });
        html += '</div>';

        return html;
    }

    $(document).on('pagebeforeshow', "#moviesRecommendedPage", function () {

        var parentId = LibraryMenu.getTopParentId();

        var screenWidth = $(window).width();

        var page = this;

        var options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            IncludeItemTypes: "Movie",
            Filters: "IsResumable",
            Limit: screenWidth >= 1920 ? 4 : (screenWidth >= 1440 ? 4 : 3),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio",
            CollapseBoxSetItems: false,
            ParentId: parentId
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            if (result.Items.length) {
                $('#resumableSection', page).show();
            } else {
                $('#resumableSection', page).hide();
            }

            $('#resumableItems', page).html(LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                preferBackdrop: true,
                shape: 'backdrop',
                overlayText: screenWidth >= 600,
                showTitle: true,
                lazy: true

            })).trigger('create').createPosterItemMenus();

        });

        var url = ApiClient.getUrl("Movies/Recommendations", {

            userId: Dashboard.getCurrentUserId(),
            categoryLimit: screenWidth >= 1200 ? 4 : 3,
            itemLimit: screenWidth >= 1920 ? 8 : (screenWidth >= 1440 ? 8 : 7),
            Fields: "PrimaryImageAspectRatio"
        });

        ApiClient.getJSON(url).done(function (recommendations) {

            if (!recommendations.length) {

                $('.noItemsMessage', page).show();
                $('.recommendations', page).html('');
                return;
            }

            var html = recommendations.map(getRecommendationHtml).join('');

            $('.noItemsMessage', page).hide();
            $('.recommendations', page).html(html).trigger('create').createPosterItemMenus();
        });

    });


})(jQuery, document);