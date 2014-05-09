(function ($, document, apiClient) {

    function fillSeriesSpotlight(elem, item, nextUp) {

        var html = '<h1 class="spotlightTitle">' + item.Name + '</h1>';

        var imgUrl = ApiClient.getImageUrl(item.Id, {
            type: "Backdrop",
            tag: item.BackdropImageTags[0]
        });

        html += '<div class="spotlight" style="background-image:url(\'' + imgUrl + '\');">';

        imgUrl = ApiClient.getImageUrl(item.Id, {
            type: "Primary",
            tag: item.ImageTags.Primary,
            EnableImageEnhancers: false
        });

        html += '<div class="spotlightContent">';
        html += '<div class="spotlightPoster" style="background-image:url(\'' + imgUrl + '\');">';

        html += '<div class="spotlightContentInner">';
        html += '<p>' + LibraryBrowser.getMiscInfoHtml(item) + '</p>';
        html += '<p>' + (item.Overview || '') + '</p>';
        html += '</div>';

        html += '</div>';
        html += '</div>';
        
        if (nextUp && nextUp.ImageTags && nextUp.ImageTags.Primary) {
            
            html += '<div class="spotlightContent rightSpotlightContent">';

            imgUrl = ApiClient.getImageUrl(nextUp.Id, {
                type: "Primary",
                tag: nextUp.ImageTags.Primary,
                EnableImageEnhancers: false
            });

            html += '<div class="spotlightPoster" style="background-image:url(\'' + imgUrl + '\');">';

            html += '<div class="spotlightContentInner">';
            html += LibraryBrowser.getPosterViewDisplayName(nextUp);
            html += '</div>';

            html += '</div>';
            html += '</div>';
        }

        html += '</div>';

        html += '<div class="spotlightPlaceHolder"></div>';

        $(elem).html(html);
    }

    function reloadSpotlight(page, allPromise) {

        var options = {

            SortBy: "Random",
            SortOrder: "Descending",
            Limit: 1,
            Recursive: true,
            IncludeItemTypes: "Series",
            ImageTypes: "Backdrop,Primary",
            Fields: "Overview"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            allPromise.done(function () {

                var index = 0;
                $('.spotlightContainer', page).each(function () {

                    var elem = this;
                    var item = result.Items[index];
                    index++;

                    if (item && item.Type == 'Series') {

                        options = {

                            Limit: 1,
                            UserId: Dashboard.getCurrentUserId(),
                            SeriesId: item.Id
                        };

                        ApiClient.getNextUpEpisodes(options).done(function (nextUpResult) {

                            fillSeriesSpotlight(elem, item, nextUpResult.Items[0]);
                        });

                    } else {
                        $(this).hide();
                    }

                });

            });
        });
    }

    $(document).on('pagebeforeshow', "#indexPage", function () {

        var screenWidth = $(window).width();

        var page = this;

        $('.spotlightContainer', page).empty();

        var options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            MediaTypes: "Video",
            Filters: "IsResumable",
            Limit: screenWidth >= 1920 ? 5 : (screenWidth >= 1440 ? 4 : 3),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio",
            CollapseBoxSetItems: false,
            ExcludeLocationTypes: "Virtual"
        };

        var promise1 = ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

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
                showParentTitle: true

            })).createPosterItemMenus();

        });

        options = {

            SortBy: "DateCreated",
            SortOrder: "Descending",
            Limit: screenWidth >= 2400 ? 21 : (screenWidth >= 1920 ? 15 : (screenWidth >= 1440 ? 12 : (screenWidth >= 800 ? 12 : 8))),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio",
            Filters: "IsUnplayed,IsNotFolder",
            CollapseBoxSetItems: false,
            ExcludeLocationTypes: "Virtual,Remote"
        };

        var promise2 = ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            $('#recentlyAddedItems', page).html(LibraryBrowser.getPosterViewHtml({

                items: result.Items,
                preferThumb: true,
                shape: 'backdrop',
                showTitle: true,
                centerText: true

            })).createPosterItemMenus();
        });

        //var allPromise = $.when(promise1, promise2);
        //reloadSpotlight(page, allPromise);
    });

})(jQuery, document, ApiClient);