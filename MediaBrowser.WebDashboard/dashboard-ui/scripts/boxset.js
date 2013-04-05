var BoxsetPage = {

    onPageShow: function () {

        BoxsetPage.reload();
    },

    onPageHide: function () {

        BoxsetPage.item = null;
    },

    reload: function () {
        var id = getParameterByName('id');

        Dashboard.showLoadingMsg();

        ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(BoxsetPage.renderItem);
    },

    renderItem: function (item) {

        BoxsetPage.item = item;

        var page = $.mobile.activePage;

        BoxsetPage.item = item;

        var name = item.Name;

        if (item.IndexNumber != null) {
            name = item.IndexNumber + " - " + name;
        }
        if (item.ParentIndexNumber != null) {
            name = item.ParentIndexNumber + "." + name;
        }

        Dashboard.setPageTitle(name);

        BoxsetPage.renderImage(item);
        BoxsetPage.renderOverviewBlock(item);

        $('#itemName', page).html(name);

        if (item.SeriesName || item.Album) {
            var series_name = item.SeriesName || item.Album;
            $('#seriesName', page).html(series_name).show();
        }

        BoxsetPage.renderFav(item);

        Dashboard.hideLoadingMsg();
    },

    renderImage: function (item) {

        var page = $.mobile.activePage;

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

        $('#itemImage', page).html(html);
    },

    renderOverviewBlock: function (item) {

        var page = $.mobile.activePage;

        if (item.Taglines && item.Taglines.length) {
            $('#itemTagline', page).html(item.Taglines[0]).show();
        } else {
            $('#itemTagline', page).hide();
        }

        if (item.Overview || item.OverviewHtml) {
            var overview = item.OverviewHtml || item.Overview;

            $('#itemOverview', page).html(overview).show();
            $('#itemOverview a').each(function () {
                $(this).attr("target", "_blank");
            });
        } else {
            $('#itemOverview', page).hide();
        }

        if (item.CommunityRating) {
            $('#itemCommunityRating', page).html(BoxsetPage.getStarRating(item)).show().attr('title', item.CommunityRating);
        } else {
            $('#itemCommunityRating', page).hide();
        }

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

        $('#itemMiscInfo', page).html(miscInfo.join('&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;'));

        BoxsetPage.renderGenres(item);
        BoxsetPage.renderStudios(item);
    },

    renderGenres: function (item) {

        var page = $.mobile.activePage;

        if (item.Genres && item.Genres.length) {
            var elem = $('#itemGenres', page).show();

            var html = 'Genres:&nbsp;&nbsp;';

            for (var i = 0, length = item.Genres.length; i < length; i++) {

                if (i > 0) {
                    html += '&nbsp;&nbsp;/&nbsp;&nbsp;';
                }

                html += '<a href="itembynamedetails.html?genre=' + item.Genres[i] + '">' + item.Genres[i] + '</a>';
            }

            elem.html(html).trigger('create');


        } else {
            $('#itemGenres', page).hide();
        }
    },

    renderStudios: function (item) {

        var page = $.mobile.activePage;

        if (item.Studios && item.Studios.length) {
            var elem = $('#itemStudios', page).show();

            var html = 'Studios:&nbsp;&nbsp;';

            for (var i = 0, length = item.Studios.length; i < length; i++) {

                if (i > 0) {
                    html += '&nbsp;&nbsp;/&nbsp;&nbsp;';
                }

                html += '<a href="itembynamedetails.html?studio=' + item.Studios[i] + '">' + item.Studios[i] + '</a>';
            }

            elem.html(html).trigger('create');


        } else {
            $('#itemStudios', page).hide();
        }
    },

    getStarRating: function (item) {
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

    renderFav: function (item) {
        var html = '';
        var page = $.mobile.activePage;

        var userData = item.UserData || {};

        if (typeof userData.Likes == "undefined") {
            html += '<div class="userItemRating"><img class="imgUserItemRating" src="css/images/userdata/thumbs_down_off.png" alt="Dislike" title="Dislike" onclick="BoxsetPage.setDislike();" /></div>';
            html += '<div class="userItemRating"><img class="imgUserItemRating" src="css/images/userdata/thumbs_up_off.png" alt="Like" title="Like" onclick="BoxsetPage.setLike();" /></div>';
        } else if (userData.Likes) {
            html += '<div class="userItemRating"><img class="imgUserItemRating" src="css/images/userdata/thumbs_down_off.png" alt="Dislike" title="Dislike" onclick="BoxsetPage.setDislike();" /></div>';
            html += '<div class="userItemRating"><img class="imgUserItemRating" src="css/images/userdata/thumbs_up_on.png" alt="Liked" title="Like" onclick="BoxsetPage.clearLike();" /></div>';
        } else {
            html += '<div class="userItemRating"><img class="imgUserItemRating" src="css/images/userdata/thumbs_down_on.png" alt="Dislike" title="Dislike" onclick="BoxsetPage.clearLike();" /></div>';
            html += '<div class="userItemRating"><img class="imgUserItemRating" src="css/images/userdata/thumbs_up_off.png" alt="Like" title="Like" onclick="BoxsetPage.setLike();" /></div>';
        }

        if (userData.IsFavorite) {
            html += '<div class="userItemRating"><img class="imgUserItemRating" src="css/images/userdata/heart_on.png" alt="Favorite" title="Favorite" onclick="BoxsetPage.setFavorite();" /></div>';
        } else {
            html += '<div class="userItemRating"><img class="imgUserItemRating" src="css/images/userdata/heart_off.png" alt="Favorite" title="Favorite" onclick="BoxsetPage.setFavorite();" /></div>';
        }

        //played/unplayed
        if (userData.Played) {
            html += '<div class="userItemRating"><img class="imgUserItemRating" src="css/images/userdata/played.png" alt="Played" title="Played" onclick="BoxsetPage.setPlayed();" /></div>';
        } else {
            html += '<div class="userItemRating"><img class="imgUserItemRating" src="css/images/userdata/unplayed.png" alt="Unplayed" title="Unplayed" onclick="BoxsetPage.setPlayed();" /></div>';
        }

        $('#itemRatings', page).html(html);
    },

    setFavorite: function () {
        var item = BoxsetPage.item;

        item.UserData = item.UserData || {};

        var setting = !item.UserData.IsFavorite;
        item.UserData.IsFavorite = setting;

        ApiClient.updateFavoriteStatus(Dashboard.getCurrentUserId(), item.Id, setting);

        BoxsetPage.renderFav(item);
    },

    setLike: function () {

        var item = BoxsetPage.item;

        item.UserData = item.UserData || {};

        item.UserData.Likes = true;

        ApiClient.updateUserItemRating(Dashboard.getCurrentUserId(), item.Id, true);

        BoxsetPage.renderFav(item);
    },

    clearLike: function () {

        var item = BoxsetPage.item;

        item.UserData = item.UserData || {};

        item.UserData.Likes = undefined;

        ApiClient.clearUserItemRating(Dashboard.getCurrentUserId(), item.Id);

        BoxsetPage.renderFav(item);
    },

    setDislike: function () {
        var item = BoxsetPage.item;

        item.UserData = item.UserData || {};

        item.UserData.Likes = false;

        ApiClient.updateUserItemRating(Dashboard.getCurrentUserId(), item.Id, false);

        BoxsetPage.renderFav(item);
    },

    setPlayed: function () {
        var item = BoxsetPage.item;

        item.UserData = item.UserData || {};

        var setting = !item.UserData.Played;
        item.UserData.Played = setting;

        ApiClient.updatePlayedStatus(Dashboard.getCurrentUserId(), item.Id, setting);

        BoxsetPage.renderFav(item);
    }

};

$(document).on('pageshow', "#boxsetPage", BoxsetPage.onPageShow).on('pagehide', "#boxsetPage", BoxsetPage.onPageHide);
