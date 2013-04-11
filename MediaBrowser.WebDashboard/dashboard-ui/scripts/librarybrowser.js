var LibraryBrowser = (function (window, $) {

    return {

        getDetaultPageSize: function () {

            if (window.location.toString().toLowerCase().indexOf('localhost') != -1) {
                return 100;
            }
            return 25;
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

                    html += "<img class='posterDetailViewImage' src='" + ApiClient.getImageUrl(item.Id, {
                        type: "Primary",
                        height: height,
                        width: width,
                        tag: item.ImageTags.Primary

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

                html += '<p class="itemMiscInfo">' + LibraryBrowser.getMiscInfoHtml(item, false) + '</p>';

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

            return item.IsFolder ? (item.Id ? "itemList.html?parentId=" + item.Id : "#") : "itemdetails.html?id=" + item.Id;

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

        getBoxsetPosterViewHtml: function (options) {

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
                    html += "<img style='background:" + LibraryBrowser.getMetroColor(item.Id) + ";' src='css/images/items/list/video.png' />";
                }

                if (showText) {
                    html += "<div class='posterViewItemText posterViewItemPrimaryText'>";
                    html += item.Name;
                    html += "</div>";
                    html += "<div class='posterViewItemText'>";
                    html += item.ChildCount + " Movie";
                    if (item.ChildCount > 1) html += "s";
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

            if (!item.IsFolder) {

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

        getLinksHtml: function (item) {

            var html = 'Links:&nbsp;&nbsp;';
            var links = [];

            if (item.ProviderIds.Imdb) {
                if (item.Type == "Movie" || item.Type == "Episode")
                    links.push('<a class="ui-link" href="http://www.imdb.com/title/' + item.ProviderIds.Imdb + '" target="_blank">IMDb</a>');
                else if (item.Type == "Person")
                    links.push('<a class="ui-link" href="http://www.imdb.com/name/' + item.ProviderIds.Imdb + '" target="_blank">IMDb</a>');
            }
            if (item.ProviderIds.Tmdb) {
                if (item.Type == "Movie")
                    links.push('<a class="ui-link" href="http://www.themoviedb.org/movie/' + item.ProviderIds.Tmdb + '" target="_blank">TMDB</a>');
                else if (item.Type == "Person")
                    links.push('<a class="ui-link" href="http://www.themoviedb.org/person/' + item.ProviderIds.Tmdb + '" target="_blank">TMDB</a>');
            }
            if (item.ProviderIds.Tvdb)
                links.push('<a class="ui-link" href="http://thetvdb.com/index.php?tab=series&id=' + item.ProviderIds.Tvdb + '" target="_blank">TVDB</a>');
            if (item.ProviderIds.Tvcom) {
                if (item.Type == "Episode")
                    links.push('<a class="ui-link" href="http://www.tv.com/shows/' + item.ProviderIds.Tvcom + '" target="_blank">TV.com</a>');
                else if (item.Type == "Person")
                    links.push('<a class="ui-link" href="http://www.tv.com/people/' + item.ProviderIds.Tvcom + '" target="_blank">TV.com</a>');
            }
            if (item.ProviderIds.Musicbrainz)
                links.push('<a class="ui-link" href="http://musicbrainz.org/release/' + item.ProviderIds.Musicbrainz + '" target="_blank">MusicBrainz</a>');
            if (item.ProviderIds.Gamesdb)
                links.push('<a class="ui-link" href="http://www.games-db.com/Game/' + item.ProviderIds.Gamesdb + '" target="_blank">GamesDB</a>');

            html += links.join('&nbsp;&nbsp;/&nbsp;&nbsp;');

            return html;
        },

        renderLinks: function (item, page) {

            if (item.ProviderIds) {

                $('#itemLinks', page).html(LibraryBrowser.getLinksHtml(item));

            } else {
                $('#itemLinks', page).hide();
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
            html += 'Results ' + (query.StartIndex + 1) + '-' + recordsEnd + ' of ' + totalRecordCount + ', page ' + dropdownHtml + ' of ' + pageCount;
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

            var $link = $(link);

            var markAsFavorite = $link.hasClass('imgFavoriteOff');

            ApiClient.updateFavoriteStatus(Dashboard.getCurrentUserId(), id, markAsFavorite);

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

            var $link = $(link);

            if ($link.hasClass('imgLikeOff')) {

                ApiClient.updateUserItemRating(Dashboard.getCurrentUserId(), id, true);

                link.src = "css/images/userdata/thumbs_up_on.png";
                $link.addClass('imgLike').removeClass('imgLikeOff');

            } else {

                ApiClient.clearUserItemRating(Dashboard.getCurrentUserId(), id);

                link.src = "css/images/userdata/thumbs_up_off.png";
                $link.addClass('imgLikeOff').removeClass('imgLike');
            }

            $link.prev().removeClass('imgDislike').addClass('imgDislikeOff').each(function () {
                this.src = "css/images/userdata/thumbs_down_off.png";
            });
        },

        markDislike: function (link) {

            var id = link.getAttribute('data-itemid');

            var $link = $(link);

            if ($link.hasClass('imgDislikeOff')) {

                ApiClient.updateUserItemRating(Dashboard.getCurrentUserId(), id, false);

                link.src = "css/images/userdata/thumbs_down_on.png";
                $link.addClass('imgDislike').removeClass('imgDislikeOff');

            } else {

                ApiClient.clearUserItemRating(Dashboard.getCurrentUserId(), id);

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
                        width: 800,
                        tag: imageTags.Primary,
                        type: "primary"
                    });
                }
                else if (item.Type == "Genre") {
                    url = ApiClient.getGenreImageUrl(item.Name, {
                        width: 800,
                        tag: imageTags.Primary,
                        type: "primary"
                    });
                }
                else if (item.Type == "Studio") {
                    url = ApiClient.getStudioImageUrl(item.Name, {
                        width: 800,
                        tag: imageTags.Primary,
                        type: "primary"
                    });
                }
                else {
                    url = ApiClient.getImageUrl(item.Id, {
                        type: "Primary",
                        width: 800,
                        tag: item.ImageTags.Primary
                    });
                }
            }
            else if (item.BackdropImageTags && item.BackdropImageTags.length) {

                url = ApiClient.getImageUrl(item.Id, {
                    type: "Backdrop",
                    width: 800,
                    tag: item.BackdropImageTags[0]
                });
            }
            else if (imageTags.Thumb) {

                url = ApiClient.getImageUrl(item.Id, {
                    type: "Thumb",
                    width: 800,
                    tag: item.ImageTags.Thumb
                });
            }
            else if (imageTags.Disc) {

                url = ApiClient.getImageUrl(item.Id, {
                    type: "Disc",
                    width: 800,
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
        }

    };

})(window, jQuery);