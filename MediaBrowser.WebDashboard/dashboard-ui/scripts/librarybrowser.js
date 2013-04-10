var LibraryBrowser = {

    getDetaultPageSize: function () {

        if (window.location.toString().toLowerCase().indexOf('localhost') != -1) {
            return 100;
        }
        return 25;
    },

    getPosterViewHtml: function (options) {

        var items = options.items;

        var primaryImageAspectRatio = options.useAverageAspectRatio ? LibraryBrowser.getAveragePrimaryImageAspectRatio(items) : null;

        var html = "";

        for (var i = 0, length = items.length; i < length; i++) {
            var item = items[i];

            var hasPrimaryImage = item.ImageTags && item.ImageTags.Primary;

            var href = item.url || (item.IsFolder ? (item.Id ? "itemList.html?parentId=" + item.Id : "#") : "itemdetails.html?id=" + item.Id);

            var showText = options.showTitle || !hasPrimaryImage;

            var cssClass = showText ? "posterViewItem" : "posterViewItem posterViewItemWithNoText";

            html += "<div class='" + cssClass + "'><a href='" + href + "'>";

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

            html += LibraryBrowser.getProgressBarHtml(item);

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

            var href = item.url || (item.IsFolder ? (item.Id ? "itemList.html?parentId=" + item.Id : "#") : "itemdetails.html?id=" + item.Id);

            var showText = options.showTitle || !hasPrimaryImage || (item.Type !== 'Movie' && item.Type !== 'Series' && item.Type !== 'Season' && item.Type !== 'Trailer');

            var cssClass = showText ? "posterViewItem posterViewItemWithDualText" : "posterViewItem posterViewItemWithNoText";

            html += "<div class='" + cssClass + "'><a href='" + href + "'>";

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

            html += LibraryBrowser.getProgressBarHtml(item);

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

            var href = item.url || ("boxset.html?id=" + item.Id);

            var showText = options.showTitle || !hasPrimaryImage || (item.Type !== 'Movie' && item.Type !== 'Series' && item.Type !== 'Season' && item.Type !== 'Trailer');

            var cssClass = showText ? "posterViewItem posterViewItemWithDualText" : "posterViewItem posterViewItemWithNoText";

            html += "<div class='" + cssClass + "'><a href='" + href + "'>";

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

    getSeriesPosterViewHtml: function (options) {

        var items = options.items;

        var primaryImageAspectRatio = options.useAverageAspectRatio ? LibraryBrowser.getAveragePrimaryImageAspectRatio(items) : null;

        var html = "";

        for (var i = 0, length = items.length; i < length; i++) {
            var item = items[i];

            var hasPrimaryImage = item.ImageTags && item.ImageTags.Primary;

            var href = item.url || "tvseries.html?id=" + item.Id;

            var showText = options.showTitle || !hasPrimaryImage;

            var cssClass = showText ? "posterViewItem" : "posterViewItem posterViewItemWithNoText";

            html += "<div class='" + cssClass + "'><a href='" + href + "'>";

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

            html += LibraryBrowser.getProgressBarHtml(item);

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

        return '';
        var html = '';

        if (item.PlayedPercentage && item.PlayedPercentage < 100) {
            html += '<progress class="itemProgress" min="0" max="100" value="' + item.PlayedPercentage + '"></progress>';
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

    getPagingHtml: function (query, totalRecordCount) {

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

        html += '<div class="listPaging">';
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

    getUserDataIconsHtml: function (item) {

        var html = '';

        var userData = item.UserData || {};

        var itemId = item.Id;
        var type = item.Type;

        if (item.MediaType) {
            if (userData.Played) {
                html += '<img data-type="' + type + '" data-itemid="' + itemId + '" class="imgUserItemRating imgPlayed" src="css/images/userdata/played.png" alt="Played" title="Played" onclick="LibraryBrowser.markPlayed(this);" />';
            } else {
                html += '<img data-type="' + type + '" data-itemid="' + itemId + '" class="imgUserItemRating imgPlayedOff" src="css/images/userdata/unplayed.png" alt="Played" title="Played" onclick="LibraryBrowser.markPlayed(this);" />';
            }
        }

        if (typeof userData.Likes == "undefined") {
            html += '<img onclick="LibraryBrowser.markDislike(this);" data-type="' + type + '" data-itemid="' + itemId + '" class="imgUserItemRating imgDislikeOff" src="css/images/userdata/thumbs_down_off.png" alt="Dislike" title="Dislike" />';
            html += '<img onclick="LibraryBrowser.markLike(this);" data-type="' + type + '" data-itemid="' + itemId + '" class="imgUserItemRating imgLikeOff" src="css/images/userdata/thumbs_up_off.png" alt="Like" title="Like" />';
        }
        else if (userData.Likes) {
            html += '<img onclick="LibraryBrowser.markDislike(this);" data-type="' + type + '" data-itemid="' + itemId + '" class="imgUserItemRating imgDislikeOff" src="css/images/userdata/thumbs_down_off.png" alt="Dislike" title="Dislike" />';
            html += '<img onclick="LibraryBrowser.markLike(this);" data-type="' + type + '" data-itemid="' + itemId + '" class="imgUserItemRating imgLike" src="css/images/userdata/thumbs_up_on.png" alt="Like" title="Like" />';
        }
        else {
            html += '<img onclick="LibraryBrowser.markDislike(this);" data-type="' + type + '" data-itemid="' + itemId + '" class="imgUserItemRating imgDislike" src="css/images/userdata/thumbs_down_on.png" alt="Dislike" title="Dislike" />';
            html += '<img onclick="LibraryBrowser.markLike(this);" data-type="' + type + '" data-itemid="' + itemId + '" class="imgUserItemRating imgLikeOff" src="css/images/userdata/thumbs_up_off.png" alt="Like" title="Like" />';
        }

        if (userData.IsFavorite) {
            html += '<img onclick="LibraryBrowser.markFavorite(this);" data-type="' + type + '" data-itemid="' + itemId + '" class="imgUserItemRating imgFavorite" src="css/images/userdata/heart_on.png" alt="Favorite" title="Favorite" />';
        } else {
            html += '<img onclick="LibraryBrowser.markFavorite(this);" data-type="' + type + '" data-itemid="' + itemId + '" class="imgUserItemRating imgFavoriteOff" src="css/images/userdata/heart_off.png" alt="Favorite" title="Favorite" />';
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

            url = ApiClient.getImageUrl(item.Id, {
                type: "Primary",
                width: 800,
                tag: item.ImageTags.Primary
            });
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

    getMiscInfoHtml: function (item) {

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

        if (item.DisplayMediaType) {
            miscInfo.push(item.DisplayMediaType);
        }

        if (item.VideoFormat && item.VideoFormat !== 'Standard') {
            miscInfo.push(item.VideoFormat);
        }

        return miscInfo.join('&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;');
    }

};