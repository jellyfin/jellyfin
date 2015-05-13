(function ($, document) {

    function getView() {
        
        if (AppInfo.hasLowImageBandwidth) {
            return 'PosterCard';
        }

        return 'PosterCard';
    }

    function getResumeView() {

        if (AppInfo.hasLowImageBandwidth) {
            return 'ThumbCard';
        }

        return 'ThumbCard';
    }

    function loadLatest(page, userId, parentId) {

        var limit = 18;
        if (AppInfo.hasLowImageBandwidth) {
            limit = 10;
        }

        var options = {

            IncludeItemTypes: "Movie",
            Limit: limit,
            Fields: "PrimaryImageAspectRatio,MediaSourceCount,SyncInfo",
            ParentId: parentId,
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options)).done(function (items) {

            var view = getView();
            var html = '';

            if (view == 'PosterCard') {

                html += LibraryBrowser.getPosterViewHtml({
                    items: items,
                    lazy: true,
                    shape: 'portrait',
                    overlayText: false,
                    showTitle: true,
                    showYear: true,
                    cardLayout: true

                });

            } else if (view == 'Poster') {
                
                html += LibraryBrowser.getPosterViewHtml({
                    items: items,
                    shape: "portrait",
                    centerText: true,
                    lazy: true,
                    overlayText: true
                });
            }

            $('#recentlyAddedItems', page).html(html).lazyChildren().trigger('create');
        });
    }

    function loadResume(page, userId, parentId) {

        var screenWidth = $(window).width();

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

            var view = getResumeView();
            var html = '';

            if (view == 'ThumbCard') {

                html += LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    preferThumb: true,
                    shape: 'backdrop',
                    showTitle: true,
                    showYear: true,
                    lazy: true,
                    cardLayout: true

                });

            } else if (view == 'Thumb') {

                html += LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    preferThumb: true,
                    shape: 'backdrop',
                    overlayText: true,
                    showTitle: true,
                    lazy: true
                });
            }

            $('#resumableItems', page).html(html).lazyChildren().trigger('create');

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

        var view = getView();

        if (view == 'PosterCard') {

            html += LibraryBrowser.getPosterViewHtml({
                items: recommendation.Items,
                lazy: true,
                shape: 'portrait',
                overlayText: false,
                showTitle: true,
                showYear: true,
                cardLayout: true

            });

        } else if (view == 'Poster') {

            html += LibraryBrowser.getPosterViewHtml({
                items: recommendation.Items,
                shape: "portrait",
                centerText: true,
                lazy: true,
                overlayText: true
            });
        }
        html += '</div>';

        return html;
    }

    function loadSuggestions(page, userId, parentId) {

        var screenWidth = $(window).width();

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
    }

    $(document).on('pageinit', "#moviesRecommendedPage", function () {

        var page = this;

        $('.recommendations', page).createCardMenus();

    }).on('pagebeforeshow', "#moviesRecommendedPage", function () {

        var parentId = LibraryMenu.getTopParentId();

        var page = this;
        var userId = Dashboard.getCurrentUserId();

        loadResume(page, userId, parentId);
        loadLatest(page, userId, parentId);

        if (!AppInfo.hasLowImageBandwidth) {
            loadSuggestions(page, userId, parentId);
        }

    });


})(jQuery, document);