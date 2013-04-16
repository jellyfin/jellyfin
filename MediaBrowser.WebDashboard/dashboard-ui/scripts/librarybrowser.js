var LibraryBrowser = (function (window, $) {

    return {

        getDetaultPageSize: function () {

            if (window.location.toString().toLowerCase().indexOf('localhost') != -1) {
                return 100;
            }
            return 20;
        },

        getPosterDetailViewHtml: function (options) {

            var items = options.items;

            var primaryImageAspectRatio = options.useAverageAspectRatio ? LibraryBrowser.getAveragePrimaryImageAspectRatio(items) : null;

            var html = '';

            for (var i = 0, length = items.length; i < length; i++) {

                var item = items[i];

                html += '<a class="posterDetailViewItem" href="' + LibraryBrowser.getHref(item) + '">';

                if (options.preferBackdrop && item.BackdropImageTags && item.BackdropImageTags.length) {

                    html += "<img class='posterDetailViewImage' src='" + ApiClient.getImageUrl(item.Id, {
                        type: "Backdrop",
                        height: 198,
                        width: 352,
                        tag: item.BackdropImageTags[0]

                    }) + "' />";

                }
                else if (options.preferBackdrop && item.ImageTags && item.ImageTags.Thumb) {

                    html += "<img class='posterDetailViewImage' src='" + ApiClient.getImageUrl(item.Id, {
                        type: "Thumb",
                        height: 198,
                        width: 352,
                        tag: item.ImageTags.Thumb

                    }) + "' />";

                }
                else if (item.ImageTags && item.ImageTags.Primary) {

                    var height = 300;
                    var width = primaryImageAspectRatio ? parseInt(height * primaryImageAspectRatio) : null;

                    html += "<img class='posterDetailViewImage' src='" + LibraryBrowser.getPrimaryImageUrl(item, {
                        height: height,
                        width: width

                    }) + "' />";

                }
                else if (item.BackdropImageTags && item.BackdropImageTags.length) {
                    html += "<img class='posterDetailViewImage' src='" + ApiClient.getImageUrl(item.Id, {
                        type: "Backdrop",
                        height: 198,
                        width: 352,
                        tag: item.BackdropImageTags[0]

                    }) + "' />";
                }
                else if (item.MediaType == "Audio" || item.Type == "MusicAlbum" || item.Type == "MusicArtist") {

                    html += "<img class='posterDetailViewImage' style='background:" + LibraryBrowser.getMetroColor(item.Id) + ";' src='css/images/items/list/audio.png' />";
                }
                else if (item.MediaType == "Video" || item.Type == "Season" || item.Type == "Series") {

                    html += "<img class='posterDetailViewImage' style='background:" + LibraryBrowser.getMetroColor(item.Id) + ";' src='css/images/items/list/video.png' />";
                }
                else if (item.Type == "Person") {

                    html += "<img class='posterDetailViewImage' style='background:" + LibraryBrowser.getMetroColor(item.Id) + ";' src='css/images/items/list/person.png' />";
                }
                else if (item.MediaType == "Game") {

                    html += "<img class='posterDetailViewImage' style='background:" + LibraryBrowser.getMetroColor(item.Id) + ";' src='css/images/items/list/game.png' />";
                }
                else {

                    html += "<img class='posterDetailViewImage' style='background:" + LibraryBrowser.getMetroColor(item.Id) + ";' src='css/images/items/list/collection.png' />";
                }

                html += '<div class="posterDetailViewContentContainer">';

                if (item.SeriesName || item.Album) {
                    var seriesName = item.SeriesName || item.Album;
                    html += '<div class="posterDetailViewName">' + seriesName + '</div>';
                }

                var name = item.Name;

                if (item.IndexNumber != null) {
                    name = item.IndexNumber + " - " + name;
                }
                if (item.ParentIndexNumber != null) {
                    name = item.ParentIndexNumber + "." + name;
                }

                html += '<div class="posterDetailViewName">' + name + '</div>';

                if (item.CommunityRating) {
                    html += '<p>' + LibraryBrowser.getFiveStarRatingHtml(item) + '</p>';
                }

                var childText;

                if (item.Type == "BoxSet") {

                    childText = item.ChildCount == 1 ? "1 Movie" : item.ChildCount + " Movies";

                    html += '<p class="itemMiscInfo">' + childText + '</p>';
                }
                else if (item.Type == "Genre" || item.Type == "Studio" || item.Type == "Person") {

                    childText = item.ChildCount == 1 ? "1 " + options.countNameSingular : item.ChildCount + " " + options.countNamePlural;

                    html += '<p class="itemMiscInfo">' + childText + '</p>';
                }
                else {
                    html += '<p class="itemMiscInfo">' + LibraryBrowser.getMiscInfoHtml(item, false) + '</p>';
                }

                html += '<p class="userDataIcons">' + LibraryBrowser.getUserDataIconsHtml(item) + '</p>';

                if (item.PlayedPercentage || (item.UserData && item.UserData.PlaybackPositionTicks)) {
                    html += '<p>' + LibraryBrowser.getProgressBarHtml(item) + '</p>';
                }

                html += '</div>';

                html += LibraryBrowser.getNewIndicatorHtml(item);

                html += "</a>";
            }

            return html;
        },

        getHref: function (item) {

            if (item.url) {
                return item.url;
            }

            if (item.Type == "Series") {
                return "tvseries.html?id=" + item.Id;
            }
            if (item.Type == "BoxSet") {
                return "boxset.html?id=" + item.Id;
            }
            if (item.Type == "Genre") {
                return "itembynamedetails.html?genre=" + item.Name;
            }
            if (item.Type == "Studio") {
                return "itembynamedetails.html?studio=" + item.Name;
            }
            if (item.Type == "Person") {
                return "itembynamedetails.html?person=" + item.Name;
            }

            return item.IsFolder ? (item.Id ? "itemList.html?parentId=" + item.Id : "#") : "itemdetails.html?id=" + item.Id;

        },

        getPrimaryImageUrl: function (item, options) {

            options = options || {};
            options.type = "Primary";
            options.tag = item.ImageTags.Primary;

            if (item.Type == "Studio") {

                return ApiClient.getStudioImageUrl(item.Name, options);
            }
            if (item.Type == "Person") {

                return ApiClient.getPersonImageUrl(item.Name, options);
            }
            if (item.Type == "Genre") {

                return ApiClient.getGenreImageUrl(item.Name, options);
            }

            return ApiClient.getImageUrl(item.Id, options);

        },

        getPosterViewHtml: function (options) {

            var items = options.items;

            var primaryImageAspectRatio = options.useAverageAspectRatio ? LibraryBrowser.getAveragePrimaryImageAspectRatio(items) : null;

            var html = "";

            for (var i = 0, length = items.length; i < length; i++) {
                var item = items[i];

                var hasPrimaryImage = item.ImageTags && item.ImageTags.Primary;

                var showText = options.showTitle || !hasPrimaryImage;

                var cssClass = showText ? "posterViewItem" : "posterViewItem posterViewItemWithNoText";

                html += "<div class='" + cssClass + "'><a href='" + LibraryBrowser.getHref(item) + "'>";

                if (options.preferBackdrop && item.BackdropImageTags && item.BackdropImageTags.length) {
                    html += "<img src='" + ApiClient.getImageUrl(item.Id, {
                        type: "Backdrop",
                        height: 198,
                        width: 352,
                        tag: item.BackdropImageTags[0]

                    }) + "' />";
                } else if (hasPrimaryImage) {

                    var height = 300;
                    var width = primaryImageAspectRatio ? parseInt(height * primaryImageAspectRatio) : null;

                    html += "<img src='" + ApiClient.getImageUrl(item.Id, {
                        type: "Primary",
                        height: height,
                        width: width,
                        tag: item.ImageTags.Primary

                    }) + "' />";

                } else if (item.BackdropImageTags && item.BackdropImageTags.length) {
                    html += "<img src='" + ApiClient.getImageUrl(item.Id, {
                        type: "Backdrop",
                        height: 198,
                        width: 352,
                        tag: item.BackdropImageTags[0]

                    }) + "' />";
                }
                else if (item.MediaType == "Audio" || item.Type == "MusicAlbum" || item.Type == "MusicArtist") {

                    html += "<img style='background:" + LibraryBrowser.getMetroColor(item.Id) + ";' src='css/images/items/list/audio.png' />";
                }
                else if (item.MediaType == "Video" || item.Type == "Season" || item.Type == "Series") {

                    html += "<img style='background:" + LibraryBrowser.getMetroColor(item.Id) + ";' src='css/images/items/list/video.png' />";
                }
                else {

                    html += "<img style='background:" + LibraryBrowser.getMetroColor(item.Id) + ";' src='css/images/items/list/collection.png' />";
                }

                if (showText) {
                    html += "<div class='posterViewItemText'>";
                    html += item.Name;
                    html += "</div>";
                }

                if (options.showNewIndicator !== false) {
                    html += LibraryBrowser.getNewIndicatorHtml(item);
                }

                html += "</a></div>";
            }

            return html;
        },

        getEpisodePosterViewHtml: function (options) {

            var items = options.items;

            var primaryImageAspectRatio = options.useAverageAspectRatio ? LibraryBrowser.getAveragePrimaryImageAspectRatio(items) : null;

            var html = "";

            for (var i = 0, length = items.length; i < length; i++) {
                var item = items[i];

                var hasPrimaryImage = item.ImageTags && item.ImageTags.Primary;

                var showText = options.showTitle || !hasPrimaryImage || (item.Type !== 'Movie' && item.Type !== 'Series' && item.Type !== 'Season' && item.Type !== 'Trailer');

                var cssClass = showText ? "posterViewItem posterViewItemWithDualText" : "posterViewItem posterViewItemWithNoText";

                html += "<div class='" + cssClass + "'><a href='" + LibraryBrowser.getHref(item) + "'>";

                if (options.preferBackdrop && item.BackdropImageTags && item.BackdropImageTags.length) {
                    html += "<img src='" + ApiClient.getImageUrl(item.Id, {
                        type: "Backdrop",
                        height: 198,
                        width: 352,
                        tag: item.BackdropImageTags[0]
                    }) + "' />";
                } else if (hasPrimaryImage) {

                    var height = 300;
                    var width = primaryImageAspectRatio ? parseInt(height * primaryImageAspectRatio) : null;

                    html += "<img src='" + ApiClient.getImageUrl(item.Id, {
                        type: "Primary",
                        height: height,
                        width: width,
                        tag: item.ImageTags.Primary
                    }) + "' />";

                } else if (item.BackdropImageTags && item.BackdropImageTags.length) {
                    html += "<img src='" + ApiClient.getImageUrl(item.Id, {
                        type: "Backdrop",
                        height: 198,
                        width: 352,
                        tag: item.BackdropImageTags[0]
                    }) + "' />";
                } else {
                    html += "<img style='background:" + LibraryBrowser.getMetroColor(item.Id) + ";' src='css/images/items/list/collection.png' />";
                }

                if (showText) {
                    html += "<div class='posterViewItemText posterViewItemPrimaryText'>";
                    if (item.SeriesName != null) {
                        html += item.SeriesName;
                        html += "</div>";
                        html += "<div class='posterViewItemText'>";
                    }
                    if (item.ParentIndexNumber != null) {
                        html += item.ParentIndexNumber + ".";
                    }
                    if (item.IndexNumber != null) {
                        html += item.IndexNumber + " -";
                    }

                    html += " " + item.Name;
                    html += "</div>";
                }

                if (options.showNewIndicator !== false) {
                    html += LibraryBrowser.getNewIndicatorHtml(item);
                }

                html += "</a></div>";
            }

            return html;
        },

        getNewIndicatorHtml: function (item) {

            if (item.RecentlyAddedItemCount) {
                return '<div class="posterRibbon">' + item.RecentlyAddedItemCount + ' New</div>';
            }

            if (!item.IsFolder && item.Type !== "Genre" && item.Type !== "Studio" && item.Type !== "Person") {

                var date = item.DateCreated;

                if (date && (new Date().getTime() - parseISO8601Date(date).getTime()) < 1209600000) {
                    return "<div class='posterRibbon'>New</div>";
                }
            }

            return '';
        },

        getProgressBarHtml: function (item) {

            var html = '';

            var tooltip;

            if (item.PlayedPercentage) {

                tooltip = parseInt(item.PlayedPercentage) + '% watched';
                html += '<progress class="itemProgress" min="0" max="100" value="' + item.PlayedPercentage + '" title="' + tooltip + '"></progress>';
            }
            else if (item.UserData && item.UserData.PlaybackPositionTicks && item.RunTimeTicks) {

                tooltip = DashboardPage.getDisplayText(item.UserData.PlaybackPositionTicks) + " / " + DashboardPage.getDisplayText(item.RunTimeTicks);

                html += '<progress class="itemProgress" min="0" max="100" value="' + (item.UserData.PlaybackPositionTicks / item.RunTimeTicks) + '" title="' + tooltip + '"></progress>';
            }

            return html;
        },

        getAveragePrimaryImageAspectRatio: function (items) {

            var values = [];

            for (var i = 0, length = items.length; i < length; i++) {

                var ratio = items[i].PrimaryImageAspectRatio || 0;

                if (!ratio) {
                    continue;
                }

                values[values.length] = ratio;
            }

            if (!values.length) {
                return null;
            }

            // Use the median
            values.sort(function (a, b) { return a - b; });

            var half = Math.floor(values.length / 2);

            if (values.length % 2)
                return values[half];
            else
                return (values[half - 1] + values[half]) / 2.0;
        },

        metroColors: ["#6FBD45", "#4BB3DD", "#4164A5", "#E12026", "#800080", "#E1B222", "#008040", "#0094FF", "#FF00C7", "#FF870F", "#7F0037"],

        getRandomMetroColor: function () {

            var index = Math.floor(Math.random() * (LibraryBrowser.metroColors.length - 1));

            return LibraryBrowser.metroColors[index];
        },

        getMetroColor: function (str) {

            if (str) {
                var char = String(str.substr(0, 1).charCodeAt());
                var sum = 0;
                for (var i = 0; i < char.length; i++) {
                    sum += parseInt(char.charAt(i));
                }
                var index = String(sum).substr(-1);

                return LibraryBrowser.metroColors[index];
            } else {
                return LibraryBrowser.getRandomMetroColor();
            }

        },

        renderLinks: function (linksElem, item) {

            var links = [];

            if (item.HomePageUrl) {
                links.push('<a class="ui-link" href="' + item.HomePageUrl + '" target="_blank">Website</a>');
            }

            var providerIds = item.ProviderIds || {};

            if (providerIds.Imdb) {
                if (item.Type == "Movie" || item.Type == "Episode")
                    links.push('<a class="ui-link" href="http://www.imdb.com/title/' + providerIds.Imdb + '" target="_blank">IMDb</a>');
                else if (item.Type == "Person")
                    links.push('<a class="ui-link" href="http://www.imdb.com/name/' + providerIds.Imdb + '" target="_blank">IMDb</a>');
            }
            if (providerIds.Tmdb) {
                if (item.Type == "Movie")
                    links.push('<a class="ui-link" href="http://www.themoviedb.org/movie/' + providerIds.Tmdb + '" target="_blank">TMDB</a>');
                else if (item.Type == "Person")
                    links.push('<a class="ui-link" href="http://www.themoviedb.org/person/' + providerIds.Tmdb + '" target="_blank">TMDB</a>');
            }
            if (providerIds.Tvdb)
                links.push('<a class="ui-link" href="http://thetvdb.com/index.php?tab=series&id=' + providerIds.Tvdb + '" target="_blank">TVDB</a>');
            if (providerIds.Tvcom) {
                if (item.Type == "Episode")
                    links.push('<a class="ui-link" href="http://www.tv.com/shows/' + providerIds.Tvcom + '" target="_blank">TV.com</a>');
                else if (item.Type == "Person")
                    links.push('<a class="ui-link" href="http://www.tv.com/people/' + providerIds.Tvcom + '" target="_blank">TV.com</a>');
            }
            if (providerIds.Musicbrainz)
                links.push('<a class="ui-link" href="http://musicbrainz.org/release/' + providerIds.Musicbrainz + '" target="_blank">MusicBrainz</a>');
            if (providerIds.Gamesdb)
                links.push('<a class="ui-link" href="http://www.games-db.com/Game/' + providerIds.Gamesdb + '" target="_blank">GamesDB</a>');


            if (links.length) {

                var html = 'Links:&nbsp;&nbsp;' + links.join('&nbsp;&nbsp;/&nbsp;&nbsp;');

                $(linksElem).html(html);

            } else {
                $(linksElem).hide();
            }
        },

        getPagingHtml: function (query, totalRecordCount, isTop) {

            var html = '';

            var pageCount = Math.ceil(totalRecordCount / query.Limit);
            var pageNumber = (query.StartIndex / query.Limit) + 1;

            var dropdownHtml = '<select data-enhance="false" data-role="none">';
            for (var i = 1; i <= pageCount; i++) {

                if (i == pageNumber) {
                    dropdownHtml += '<option value="' + i + '" selected="selected">' + i + '</option>';
                } else {
                    dropdownHtml += '<option value="' + i + '">' + i + '</option>';
                }
            }
            dropdownHtml += '</select>';

            var recordsEnd = Math.min(query.StartIndex + query.Limit, totalRecordCount);

            html += isTop ? '<div class="listPaging topListPaging">' : '<div class="listPaging">';

            html += '<span style="margin-right: 10px;">';
            html += (query.StartIndex + 1) + '-' + recordsEnd + ' of ' + totalRecordCount + ', page ' + dropdownHtml + ' of ' + pageCount;
            html += '</span>';

            html += '<button data-icon="arrow-left" data-iconpos="notext" data-inline="true" data-mini="true" class="btnPreviousPage" ' + (query.StartIndex ? '' : 'disabled') + '>Previous Page</button>';

            html += '<button data-icon="arrow-right" data-iconpos="notext" data-inline="true" data-mini="true" class="btnNextPage" ' + (query.StartIndex + query.Limit > totalRecordCount ? 'disabled' : '') + '>Next Page</button>';

            html += '</div>';

            return html;
        },

        getStarRatingHtml: function (item) {
            var rating = item.CommunityRating;

            var html = "";
            for (var i = 1; i <= 10; i++) {
                if (rating < i - 1) {
                    html += "<div class='starRating emptyStarRating'></div>";
                }
                else if (rating < i) {
                    html += "<div class='starRating halfStarRating'></div>";
                }
                else {
                    html += "<div class='starRating'></div>";
                }
            }

            return html;
        },

        getFiveStarRatingHtml: function (item) {

            var rating = item.CommunityRating / 2;

            var html = "";
            for (var i = 1; i <= 5; i++) {
                if (rating < i - 1) {
                    html += "<div class='starRating emptyStarRating'></div>";
                }
                else if (rating < i) {
                    html += "<div class='starRating halfStarRating'></div>";
                }
                else {
                    html += "<div class='starRating'></div>";
                }
            }

            return html;
        },

        getUserDataIconsHtml: function (item) {

            var html = '';

            var userData = item.UserData || {};

            var itemId = item.Id;
            var type = item.Type;

            if (type == "Person") {
                itemId = item.Name;
            }
            else if (type == "Studio") {
                itemId = item.Name;
            }
            else if (type == "Genre") {
                itemId = item.Name;
            }

            if (item.MediaType) {
                if (userData.Played) {
                    html += '<img data-type="' + type + '" data-itemid="' + itemId + '" class="imgUserItemRating imgPlayed" src="css/images/userdata/played.png" alt="Played" title="Played" onclick="LibraryBrowser.markPlayed(this);return false;" />';
                } else {
                    html += '<img data-type="' + type + '" data-itemid="' + itemId + '" class="imgUserItemRating imgPlayedOff" src="css/images/userdata/unplayed.png" alt="Played" title="Played" onclick="LibraryBrowser.markPlayed(this);return false;" />';
                }
            }

            if (typeof userData.Likes == "undefined") {
                html += '<img onclick="LibraryBrowser.markDislike(this);return false;" data-type="' + type + '" data-itemid="' + itemId + '" class="imgUserItemRating imgDislikeOff" src="css/images/userdata/thumbs_down_off.png" alt="Dislike" title="Dislike" />';
                html += '<img onclick="LibraryBrowser.markLike(this);return false;" data-type="' + type + '" data-itemid="' + itemId + '" class="imgUserItemRating imgLikeOff" src="css/images/userdata/thumbs_up_off.png" alt="Like" title="Like" />';
            }
            else if (userData.Likes) {
                html += '<img onclick="LibraryBrowser.markDislike(this);return false;" data-type="' + type + '" data-itemid="' + itemId + '" class="imgUserItemRating imgDislikeOff" src="css/images/userdata/thumbs_down_off.png" alt="Dislike" title="Dislike" />';
                html += '<img onclick="LibraryBrowser.markLike(this);return false;" data-type="' + type + '" data-itemid="' + itemId + '" class="imgUserItemRating imgLike" src="css/images/userdata/thumbs_up_on.png" alt="Like" title="Like" />';
            }
            else {
                html += '<img onclick="LibraryBrowser.markDislike(this);return false;" data-type="' + type + '" data-itemid="' + itemId + '" class="imgUserItemRating imgDislike" src="css/images/userdata/thumbs_down_on.png" alt="Dislike" title="Dislike" />';
                html += '<img onclick="LibraryBrowser.markLike(this);return false;" data-type="' + type + '" data-itemid="' + itemId + '" class="imgUserItemRating imgLikeOff" src="css/images/userdata/thumbs_up_off.png" alt="Like" title="Like" />';
            }

            if (userData.IsFavorite) {
                html += '<img onclick="LibraryBrowser.markFavorite(this);return false;" data-type="' + type + '" data-itemid="' + itemId + '" class="imgUserItemRating imgFavorite" src="css/images/userdata/heart_on.png" alt="Favorite" title="Favorite" />';
            } else {
                html += '<img onclick="LibraryBrowser.markFavorite(this);return false;" data-type="' + type + '" data-itemid="' + itemId + '" class="imgUserItemRating imgFavoriteOff" src="css/images/userdata/heart_off.png" alt="Favorite" title="Favorite" />';
            }

            return html;
        },

        markPlayed: function (link) {

            var id = link.getAttribute('data-itemid');

            var $link = $(link);

            var markAsPlayed = $link.hasClass('imgPlayedOff');

            ApiClient.updatePlayedStatus(Dashboard.getCurrentUserId(), id, markAsPlayed);

            if (markAsPlayed) {
                link.src = "css/images/userdata/played.png";
                $link.addClass('imgPlayed').removeClass('imgPlayedOff');
            } else {
                link.src = "css/images/userdata/unplayed.png";
                $link.addClass('imgPlayedOff').removeClass('imgPlayed');
            }
        },

        markFavorite: function (link) {

            var id = link.getAttribute('data-itemid');
            var type = link.getAttribute('data-type');

            var $link = $(link);

            var markAsFavorite = $link.hasClass('imgFavoriteOff');

            if (type == "Person" || type == "Studio" || type == "Genre") {
                ApiClient.updateItemByNameFavoriteStatus(Dashboard.getCurrentUserId(), id, markAsFavorite);
            }
            else {
                ApiClient.updateFavoriteStatus(Dashboard.getCurrentUserId(), id, markAsFavorite);
            }

            if (markAsFavorite) {
                link.src = "css/images/userdata/heart_on.png";
                $link.addClass('imgFavorite').removeClass('imgFavoriteOff');
            } else {
                link.src = "css/images/userdata/heart_off.png";
                $link.addClass('imgFavoriteOff').removeClass('imgFavorite');
            }
        },

        markLike: function (link) {

            var id = link.getAttribute('data-itemid');
            var type = link.getAttribute('data-type');

            var $link = $(link);

            if ($link.hasClass('imgLikeOff')) {

                if (type == "Person" || type == "Studio" || type == "Genre") {
                    ApiClient.updateItemByNameRating(Dashboard.getCurrentUserId(), id, true);
                }
                else {
                    ApiClient.updateUserItemRating(Dashboard.getCurrentUserId(), id, true);
                }

                link.src = "css/images/userdata/thumbs_up_on.png";
                $link.addClass('imgLike').removeClass('imgLikeOff');

            } else {

                if (type == "Person" || type == "Studio" || type == "Genre") {
                    ApiClient.clearItemByNameRating(Dashboard.getCurrentUserId(), id);
                }
                else {
                    ApiClient.clearUserItemRating(Dashboard.getCurrentUserId(), id);
                }

                link.src = "css/images/userdata/thumbs_up_off.png";
                $link.addClass('imgLikeOff').removeClass('imgLike');
            }

            $link.prev().removeClass('imgDislike').addClass('imgDislikeOff').each(function () {
                this.src = "css/images/userdata/thumbs_down_off.png";
            });
        },

        markDislike: function (link) {

            var id = link.getAttribute('data-itemid');
            var type = link.getAttribute('data-type');

            var $link = $(link);

            if ($link.hasClass('imgDislikeOff')) {

                if (type == "Person" || type == "Studio" || type == "Genre") {
                    ApiClient.updateItemByNameRating(Dashboard.getCurrentUserId(), id, false);
                }
                else {
                    ApiClient.updateUserItemRating(Dashboard.getCurrentUserId(), id, false);
                }

                link.src = "css/images/userdata/thumbs_down_on.png";
                $link.addClass('imgDislike').removeClass('imgDislikeOff');

            } else {

                if (type == "Person" || type == "Studio" || type == "Genre") {
                    ApiClient.clearItemByNameRating(Dashboard.getCurrentUserId(), id);
                }
                else {
                    ApiClient.clearUserItemRating(Dashboard.getCurrentUserId(), id);
                }

                link.src = "css/images/userdata/thumbs_down_off.png";
                $link.addClass('imgDislikeOff').removeClass('imgDislike');
            }

            $link.next().removeClass('imgLike').addClass('imgLikeOff').each(function () {
                this.src = "css/images/userdata/thumbs_up_off.png";
            });
        },

        getDetailImageHtml: function (item) {

            var imageTags = item.ImageTags || {};

            var html = '';

            var url;
            var useBackgroundColor;

            if (imageTags.Primary) {

                if (item.Type == "Person") {
                    url = ApiClient.getPersonImageUrl(item.Name, {
                        maxwidth: 800,
                        tag: imageTags.Primary,
                        type: "primary"
                    });
                }
                else if (item.Type == "Genre") {
                    url = ApiClient.getGenreImageUrl(item.Name, {
                        maxwidth: 800,
                        tag: imageTags.Primary,
                        type: "primary"
                    });
                }
                else if (item.Type == "Studio") {
                    url = ApiClient.getStudioImageUrl(item.Name, {
                        maxwidth: 800,
                        tag: imageTags.Primary,
                        type: "primary"
                    });
                }
                else {
                    url = ApiClient.getImageUrl(item.Id, {
                        type: "Primary",
                        maxwidth: 800,
                        tag: item.ImageTags.Primary
                    });
                }
            }
            else if (item.BackdropImageTags && item.BackdropImageTags.length) {

                url = ApiClient.getImageUrl(item.Id, {
                    type: "Backdrop",
                    maxwidth: 800,
                    tag: item.BackdropImageTags[0]
                });
            }
            else if (imageTags.Thumb) {

                url = ApiClient.getImageUrl(item.Id, {
                    type: "Thumb",
                    maxwidth: 800,
                    tag: item.ImageTags.Thumb
                });
            }
            else if (imageTags.Disc) {

                url = ApiClient.getImageUrl(item.Id, {
                    type: "Disc",
                    maxwidth: 800,
                    tag: item.ImageTags.Disc
                });
            }
            else if (item.MediaType == "Audio") {
                url = "css/images/items/detail/audio.png";
                useBackgroundColor = true;
            }
            else if (item.MediaType == "Game") {
                url = "css/images/items/detail/game.png";
                useBackgroundColor = true;
            }
            else if (item.Type == "Person") {
                url = "css/images/items/detail/person.png";
                useBackgroundColor = true;
            }
            else {
                url = "css/images/items/detail/video.png";
                useBackgroundColor = true;
            }

            if (url) {

                var style = useBackgroundColor ? "background-color:" + LibraryBrowser.getMetroColor(item.Id) + ";" : "";

                html += "<img class='itemDetailImage' src='" + url + "' style='" + style + "' />";
            }

            return html;
        },

        getMiscInfoHtml: function (item, includeMediaInfo) {

            var miscInfo = [];

            if (item.ProductionYear) {
                miscInfo.push(item.ProductionYear);
            }

            if (item.OfficialRating) {
                miscInfo.push(item.OfficialRating);
            }

            if (item.RunTimeTicks) {

                var minutes = item.RunTimeTicks / 600000000;

                minutes = minutes || 1;

                miscInfo.push(parseInt(minutes) + "min");
            }

            if (includeMediaInfo !== false) {
                if (item.DisplayMediaType) {
                    miscInfo.push(item.DisplayMediaType);
                }

                if (item.VideoFormat && item.VideoFormat !== 'Standard') {
                    miscInfo.push(item.VideoFormat);
                }
            }


            return miscInfo.join('&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;');
        },

        renderStudios: function (elem, item) {

            if (item.Studios && item.Studios.length) {

                var html = 'Studios:&nbsp;&nbsp;';

                for (var i = 0, length = item.Studios.length; i < length; i++) {

                    if (i > 0) {
                        html += '&nbsp;&nbsp;/&nbsp;&nbsp;';
                    }

                    html += '<a href="itembynamedetails.html?studio=' + item.Studios[i] + '">' + item.Studios[i] + '</a>';
                }

                elem.show().html(html).trigger('create');


            } else {
                elem.hide();
            }
        },

        renderGenres: function (elem, item) {

            if (item.Genres && item.Genres.length) {
                var html = 'Genres:&nbsp;&nbsp;';

                for (var i = 0, length = item.Genres.length; i < length; i++) {

                    if (i > 0) {
                        html += '&nbsp;&nbsp;/&nbsp;&nbsp;';
                    }

                    html += '<a href="itembynamedetails.html?genre=' + item.Genres[i] + '">' + item.Genres[i] + '</a>';
                }

                elem.show().html(html).trigger('create');


            } else {
                elem.hide();
            }
        },

        renderBudget: function (elem, item) {
            if (item.Budget) {
                elem.show().html('Budget:&nbsp;&nbsp;$' + item.Budget);
            } else {
                elem.hide();
            }
        },

        getGamePosterViewHtml: function (options) {

            var items = options.items;

            var primaryImageAspectRatio = options.useAverageAspectRatio ? LibraryBrowser.getAveragePrimaryImageAspectRatio(items) : null;

            var html = "";

            for (var i = 0, length = items.length; i < length; i++) {
                var item = items[i];

                var hasPrimaryImage = item.ImageTags && item.ImageTags.Primary;

                var showText = options.showTitle || !hasPrimaryImage;

                var cssClass = showText ? "posterViewItem" : "posterViewItem posterViewItemWithNoText";

                html += "<div class='" + cssClass + "'><a href='" + LibraryBrowser.getHref(item) + "'>";

                if (options.preferBackdrop && item.BackdropImageTags && item.BackdropImageTags.length) {
                    html += "<img src='" + ApiClient.getImageUrl(item.Id, {
                        type: "Backdrop",
                        height: 198,
                        width: 352,
                        tag: item.BackdropImageTags[0]

                    }) + "' />";
                } else if (hasPrimaryImage) {

                    var height = 300;
                    var width = primaryImageAspectRatio ? parseInt(height * primaryImageAspectRatio) : null;

                    html += "<img src='" + ApiClient.getImageUrl(item.Id, {
                        type: "Primary",
                        height: height,
                        width: width,
                        tag: item.ImageTags.Primary

                    }) + "' />";

                } else if (item.BackdropImageTags && item.BackdropImageTags.length) {
                    html += "<img src='" + ApiClient.getImageUrl(item.Id, {
                        type: "Backdrop",
                        height: 198,
                        width: 352,
                        tag: item.BackdropImageTags[0]

                    }) + "' />";
                }
                else {
                    html += "<img style='background:" + LibraryBrowser.getMetroColor(item.Id) + ";' src='css/images/items/list/game.png' />";
                }

                if (showText) {
                    html += "<div class='posterViewItemText'>";
                    html += item.Name;
                    html += "</div>";
                }

                if (options.showNewIndicator !== false) {
                    html += LibraryBrowser.getNewIndicatorHtml(item);
                }

                html += "</a></div>";
            }

            return html;
        },

        getGalleryHtml: function (item) {
            
		    var html = '';
		    var i, length;

            var imageTags = item.ImageTags || {};

            if (imageTags.Logo) {

                html += LibraryBrowser.createGalleryImage(item.Id, "Logo", imageTags.Logo);
		    }
		    if (imageTags.Thumb) {

		        html += LibraryBrowser.createGalleryImage(item.Id, "Thumb", imageTags.Thumb);
		    }
		    if (imageTags.Art) {

		        html += LibraryBrowser.createGalleryImage(item.Id, "Art", imageTags.Art);

		    }
		    if (imageTags.Menu) {

		        html += LibraryBrowser.createGalleryImage(item.Id, "Menu", imageTags.Menu);

		    }
		    if (imageTags.Disc) {

		        html += LibraryBrowser.createGalleryImage(item.Id, "Disc", imageTags.Disc);
		    }
		    if (imageTags.Box) {

		        html += LibraryBrowser.createGalleryImage(item.Id, "Box", imageTags.Box);
		    }

		    if (item.BackdropImageTags) {

			    for (i = 0, length = item.BackdropImageTags.length; i < length; i++) {
				    html += LibraryBrowser.createGalleryImage(item.Id, "Backdrop", item.BackdropImageTags[0], i);
			    }

		    }

		    if (item.ScreenshotImageTags) {

			    for (i = 0, length = item.ScreenshotImageTags.length; i < length; i++) {
				    html += LibraryBrowser.createGalleryImage(item.Id, "Screenshot", item.ScreenshotImageTags[0], i);
			    }
		    }

		    return html;
	    },

	    createGalleryImage: function (itemId, type, tag, index) {

			var downloadWidth = 400;
			var lightboxWidth = 800;
			var html = '';

			if (typeof (index) == "undefined") index = 0;

			html += '<div class="posterViewItem" style="padding-bottom:0px;">';
			html += '<a href="#pop_' + index + '_' + tag + '" data-transition="fade" data-rel="popup" data-position-to="window">';
			html += '<img class="galleryImage" src="' + ApiClient.getImageUrl(itemId, {
				type: type,
				maxwidth: downloadWidth,
				tag: tag,
				index: index
			}) + '" />';
			html += '</div>';

			html += '<div class="galleryPopup" id="pop_' + index + '_' + tag + '" data-role="popup" data-theme="d" data-corners="false" data-overlay-theme="a">';
			html += '<a href="#" data-rel="back" data-role="button" data-theme="a" data-icon="delete" data-iconpos="notext" class="ui-btn-right">Close</a>';
			html += '<img class="" src="' + ApiClient.getImageUrl(itemId, {
				type: type,
				maxwidth: lightboxWidth,
				tag: tag,
				index: index
			}) + '" />';
			html += '</div>';

			return html;
		},

	    createCastImage: function (cast) {

			var html = '';

			var role = cast.Role || cast.Type;

			html += '<a href="itembynamedetails.html?person=' + cast.Name + '">';
			html += '<div class="posterViewItem posterViewItemWithDualText">';

			if (cast.PrimaryImageTag) {

				var imgUrl = ApiClient.getPersonImageUrl(cast.Name, {
					width: 185,
					tag: cast.PrimaryImageTag,
					type: "primary"
				});

				html += '<img src="' + imgUrl + '" />';
			} else {
				var style = "background-color:" + LibraryBrowser.getMetroColor(cast.Name) + ";";

				html += '<img src="css/images/items/list/person.png" style="max-width:185px; ' + style + '"/>';
			}

			html += '<div class="posterViewItemText posterViewItemPrimaryText">' + cast.Name + '</div>';
			html += '<div class="posterViewItemText">' + role + '</div>';

			html += '</div></a>';

		    return html;
		}

    };

})(window, jQuery);