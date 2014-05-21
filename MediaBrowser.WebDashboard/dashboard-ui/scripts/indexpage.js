(function ($, document, apiClient) {

    function createMediaLinks(options) {

        var html = "";

        var items = options.items;

        // "My Library" backgrounds
        for (var i = 0, length = items.length; i < length; i++) {

            var item = items[i];

            var imgUrl;

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
                case "livetv":
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
                case "channels":
                    imgUrl = "css/images/items/folders/channels.png";
                    break;
                case "boxsets":
                default:
                    imgUrl = "css/images/items/folders/folder.png";
                    break;
            }

            var cssClass = "posterItem";
            cssClass += ' ' + options.shape + 'PosterItem';

            if (item.CollectionType) {
                cssClass += ' ' + item.CollectionType + 'PosterItem';
            }

            var href = item.url || LibraryBrowser.getHref(item, options.context);

            html += '<a data-itemid="' + item.Id + '" class="' + cssClass + '" href="' + href + '">';

            var style = "";

            if (imgUrl) {
                style += 'background-image:url(\'' + imgUrl + '\');';
            }

            var imageCssClass = 'posterItemImage';

            html += '<div class="' + imageCssClass + '" style="' + style + '">';
            html += '</div>';

            if (options.showTitle) {
                html += "<div class='posterItemDefaultText'>";
                html += item.Name;
                html += "</div>";
            }

            html += "</a>";
        }

        return html;
    }

    function refreshMediaLibrary(page) {

        var options = {

            SortBy: "SortName",
            Fields: "PrimaryImageAspectRatio"
        };

        var userId = Dashboard.getCurrentUserId();

        var promise1 = ApiClient.getItems(userId, options);
        
        var promise2 = ApiClient.getLiveTvInfo();

        var promise3 = $.getJSON(ApiClient.getUrl("Channels", {
            userId: userId,

            // We just want the total record count
            limit: 0
        }));

        $.when(promise1, promise2, promise3).done(function(r1, r2, r3) {

            var result = r1[0];
            var liveTvInfo = r2[0];
            var channelResponse = r3[0];

            if (channelResponse.TotalRecordCount) {

                result.Items.push({
                    Name: 'Channels',
                    CollectionType: 'channels',
                    Id: 'channels',
                    url: 'channels.html'
                });
            }

            var showLiveTv = liveTvInfo.EnabledUsers.indexOf(userId) != -1;

            if (showLiveTv) {
                
                result.Items.push({
                    Name: 'Live TV',
                    CollectionType: 'livetv',
                    Id: 'livetv',
                    url: 'livetvsuggested.html'
                });
            }

            $('.myLibrary', page).html(createMediaLinks({
                items: result.Items,
                shape: 'myLibrary',
                showTitle: true,
                centerText: true

            }));
        });
    }

    $(document).on('pagebeforeshow', "#indexPage", function () {

        var screenWidth = $(window).width();

        var page = this;

        refreshMediaLibrary(page);

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