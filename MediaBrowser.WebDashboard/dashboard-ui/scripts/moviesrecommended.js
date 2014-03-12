(function ($, document) {

    function getRecommendationHtml(recommendation) {

        var html = '';

        var title = '';
        
        switch (recommendation.RecommendationType) {
        
            case 'SimilarToRecentlyPlayed':
                title = 'Because you watched ' + recommendation.BaselineItemName;
                break;
            case 'SimilarToLikedItem':
                title = 'Because you like ' + recommendation.BaselineItemName;
                break;
            case 'HasDirectorFromRecentlyPlayed':
            case 'HasLikedDirector':
                title = 'Directed by ' + recommendation.BaselineItemName;
                break;
            case 'HasActorFromRecentlyPlayed':
            case 'HasLikedActor':
                title = 'Starring ' + recommendation.BaselineItemName;
                break;
        }
        
        html += '<h1 class="listHeader">' + title + '</h1>';

        html += '<div>';
        html += LibraryBrowser.getPosterViewHtml({
            items: recommendation.Items,
            useAverageAspectRatio: true
        });
        html += '</div>';

        return html;
    }
    
    $(document).on('pagebeforeshow', "#moviesRecommendedPage", function () {

        var screenWidth = $(window).width();

        var page = this;
        
        var options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            IncludeItemTypes: "Movie",
            Filters: "IsResumable",
            Limit: screenWidth >= 1920 ? 4 : (screenWidth >= 1440 ? 4 : 3),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio"
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
                overlayText: true,
                showTitle: true
                
            })).createPosterItemHoverMenu();

        });

        var url = ApiClient.getUrl("Movies/Recommendations", {
            
            userId: Dashboard.getCurrentUserId(),
            categoryLimit: screenWidth >= 1200 ? 6 : 3,
            itemLimit: screenWidth >= 1920 ? 8 : (screenWidth >= 1440 ? 8 : 6),
            Fields: "PrimaryImageAspectRatio"
        });

        $.getJSON(url).done(function(recommendations) {

            if (!recommendations.length) {

                $('.recommendations', page).html('<br/><p>No movie suggestions are currently available. Start watching and rating your movies, and then come back to view your recommendations.</p>');
                return;
            }

            var html = recommendations.map(getRecommendationHtml).join('');

            $('.recommendations', page).html(html).createPosterItemHoverMenu();
        });
    });


})(jQuery, document);