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

    function createMediaLinks(options) {

        var html = "";

        var items = options.items;

        console.log("options", options);

        // "My Library" backgrounds
        for (var i = 0, length = items.length; i < length; i++) {

            var item = items[i];
            var background = "#333";
            var backgroundSize = "45px 45px";
            var backgroundPosition = "20px center";

            switch (item.CollectionType) {
                case "movies":
                    imgUrl = "css/images/items/folders/movies.png";
                    break;
                case "music":
                    imgUrl = "css/images/items/folders/music.png";
                    break;
                case "photos":
                    imgUrl = "css/images/items/folders/photos.png";
                    break;
                case "tvshows":
                    imgUrl = "css/images/items/folders/tv.png";
                    break;
                case "games":
                    imgUrl = "css/images/items/folders/games.png";
                    break;
                case "trailers":
                    imgUrl = "css/images/items/folders/games.png";
                    break;
                case "homevideos":
                    imgUrl = "css/images/items/folders/homevideos.png";
                    break;
                case "musicvideos":
                    imgUrl = "css/images/items/folders/musicvideos.png";
                    break;
                case "boxsets":
                default:
                    imgUrl = "css/images/items/folders/folder.png";
                    break;
            }

            var cssClass = "posterItem";
            cssClass += ' ' + options.shape + 'PosterItem';

            var mediaSourceCount = item.MediaSourceCount || 1;

            var href = options.linkItem === false ? '#' : LibraryBrowser.getHref(item, options.context);

            html += '<a data-itemid="' + item.Id + '" class="' + cssClass + '" data-mediasourcecount="' + mediaSourceCount + '" href="' + href + '">';

            var style = "";

            if (imgUrl) {
                style += 'background-image:url(\'' + imgUrl + '\');';
            }

            var imageCssClass = 'posterItemImage';

            html += '<div class="' + imageCssClass + '" style="' + style + '">';
            html += '</div>';

            var name = LibraryBrowser.getPosterViewDisplayName(item, options.displayAsSpecial);

            if (options.showTitle) {
                html += "<div class='posterItemDefaultText'>";
                html += name;
                html += "</div>";
            }

            cssClass = options.centerText ? "posterItemText posterItemTextCentered" : "posterItemText";

            html += "</a>";
        }

        console.log("html", html);

        return html;
    }

    $(document).on('pagebeforeshow', "#indexPage", function () {

        var screenWidth = $(window).width();

        var page = this;

        $('.spotlightContainer', page).empty();

        var options = {

            SortBy: "SortName",
            Fields: "PrimaryImageAspectRatio"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            $('.myLibrary', page).html(createMediaLinks({
                items: result.Items,
                shape: 'myLibrary',
                showTitle: true,
                centerText: true

            }));

        });

        options = {

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
                showParentTitle: true

            })).createPosterItemMenus();

        });

        options = {

            SortBy: "DateCreated",
            SortOrder: "Descending",
            Limit: screenWidth >= 2400 ? 30 : (screenWidth >= 1920 ? 20 : (screenWidth >= 1440 ? 12 : (screenWidth >= 800 ? 12 : 8))),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio",
            Filters: "IsUnplayed,IsNotFolder",
            CollapseBoxSetItems: false,
            ExcludeLocationTypes: "Virtual,Remote"
        };

        ApiClient.getItems(Dashboard.getCurrentUserId(), options).done(function (result) {

            $('#recentlyAddedItems', page).html(LibraryBrowser.getPosterViewHtml({

                items: result.Items,
                preferThumb: true,
                shape: 'backdrop',
                showTitle: true,
                centerText: true

            })).createPosterItemMenus();
        });
    });

})(jQuery, document, ApiClient);