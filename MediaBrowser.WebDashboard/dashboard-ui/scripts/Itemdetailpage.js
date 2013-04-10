var ItemDetailPage = {

    onPageShow: function () {

        ItemDetailPage.reload();

        $('#mediaInfoCollapsible', this).on('expand', ItemDetailPage.onMediaInfoExpand);
        $('#scenesCollapsible', this).on('expand', ItemDetailPage.onScenesExpand);
        $('#specialsCollapsible', this).on('expand', ItemDetailPage.onSpecialsExpand);
        $('#trailersCollapsible', this).on('expand', ItemDetailPage.onTrailersExpand);
        $('#castCollapsible', this).on('expand', ItemDetailPage.onCastExpand);
        $('#galleryCollapsible', this).on('expand', ItemDetailPage.onGalleryExpand);
    },

    onPageHide: function () {

        $('#mediaInfoCollapsible', this).off('expand', ItemDetailPage.onMediaInfoExpand);
        $('#scenesCollapsible', this).off('expand', ItemDetailPage.onScenesExpand);
        $('#specialsCollapsible', this).off('expand', ItemDetailPage.onSpecialsExpand);
        $('#trailersCollapsible', this).off('expand', ItemDetailPage.onTrailersExpand);
        $('#castCollapsible', this).off('expand', ItemDetailPage.onCastExpand);
        $('#galleryCollapsible', this).off('expand', ItemDetailPage.onGalleryExpand);

        ItemDetailPage.item = null;
    },

    reload: function () {
        var id = getParameterByName('id');

        Dashboard.showLoadingMsg();

        ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(ItemDetailPage.renderItem);
    },

    renderItem: function (item) {

        ItemDetailPage.item = item;

        var page = $.mobile.activePage;

        ItemDetailPage.item = item;

        var name = item.Name;

        if (item.IndexNumber != null) {
            name = item.IndexNumber + " - " + name;
        }
        if (item.ParentIndexNumber != null) {
            name = item.ParentIndexNumber + "." + name;
        }

        Dashboard.setPageTitle(name);

        ItemDetailPage.renderImage(item);
        ItemDetailPage.renderOverviewBlock(item);
        ItemDetailPage.renderGallery(item);

        if (!item.MediaStreams || !item.MediaStreams.length) {
            $('#mediaInfoCollapsible', page).hide();
        } else {
            $('#mediaInfoCollapsible', page).show();
        }
        if (!item.Chapters || !item.Chapters.length) {
            $('#scenesCollapsible', page).hide();
        } else {
            $('#scenesCollapsible', page).show();
        }
        if (!item.LocalTrailerCount || item.LocalTrailerCount == 0) {
            $('#trailersCollapsible', page).hide();
        } else {
            $('#trailersCollapsible', page).show();
        }
        if (!item.SpecialFeatureCount || item.SpecialFeatureCount == 0) {
            $('#specialsCollapsible', page).hide();
        } else {
            $('#specialsCollapsible', page).show();
        }
        if (!item.People || !item.People.length) {
            $('#castCollapsible', page).hide();
        } else {
            $('#castCollapsible', page).show();
        }

        $('#itemName', page).html(name);

        if (item.SeriesName || item.Album) {
            var seriesName = item.SeriesName || item.Album;
            $('#seriesName', page).html(seriesName).show();
        }

        ItemDetailPage.renderFav(item);
        LibraryBrowser.renderLinks(item);

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
            $('#itemCommunityRating', page).html(LibraryBrowser.getStarRatingHtml(item)).show().attr('title', item.CommunityRating);
        } else {
            $('#itemCommunityRating', page).hide();
        }

        if (MediaPlayer.canPlay(item)) {
            $('#btnPlay', page).show();
            $('#playButtonShadow', page).show();
        } else {
            $('#btnPlay', page).hide();
            $('#playButtonShadow', page).hide();
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

        ItemDetailPage.renderGenres(item);
        ItemDetailPage.renderStudios(item);
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

    onScenesExpand: function () {

        if (ItemDetailPage.item) {

            ItemDetailPage.renderScenes(ItemDetailPage.item);

            $(this).off('expand', ItemDetailPage.onScenesExpand);
        }
    },

    renderScenes: function (item) {

        var html = '';
        var page = $.mobile.activePage;
        var chapters = item.Chapters || {};

        for (var i = 0, length = chapters.length; i < length; i++) {

            var chapter = chapters[i];
            var chapterName = chapter.Name || "Chapter " + i;

            html += '<div class="posterViewItem posterViewItemWithDualText">';
            html += '<a href="#play-Chapter-' + i + '" onclick="ItemDetailPage.play(' + chapter.StartPositionTicks + ');">';

            if (chapter.ImageTag) {

                var imgUrl = ApiClient.getImageUrl(item.Id, {
                    width: 500,
                    tag: chapter.ImageTag,
                    type: "Chapter",
                    index: i
                });

                html += '<img src="' + imgUrl + '" />';
            } else {
                html += '<img src="css/images/items/list/chapter.png"/>';
            }

            html += '<div class="posterViewItemText posterViewItemPrimaryText">' + chapterName + '</div>';
            html += '<div class="posterViewItemText">';

            html += ticks_to_human(chapter.StartPositionTicks);

            html += '</div>';

            html += '</a>';

            html += '</div>';

        }

        $('#scenesContent', page).html(html);
    },

    onPlayButtonClick: function () {

        var item = ItemDetailPage.item;

        var userdata = item.UserData || {};

        if (userdata.PlaybackPositionTicks) {

            var page = $.mobile.activePage;

            var pos = $('#playMenuAnchor', page).offset();

            $('#playMenu', page).popup("open", {
                
                x: pos.left + 125,
                y: pos.top + 20

            });

        } else {
            ItemDetailPage.play();
        }

    },

    play: function (startPosition) {

        var page = $.mobile.activePage;
        $('#playMenu', page).popup("close");
        MediaPlayer.play([ItemDetailPage.item], startPosition);
    },
    
    resume: function() {

        var item = ItemDetailPage.item;

        var userdata = item.UserData || {};

        ItemDetailPage.play(userdata.PlaybackPositionTicks);
    },

    onGalleryExpand: function () {

        if (ItemDetailPage.item) {

            ItemDetailPage.renderGallery(ItemDetailPage.item);

            $(this).off('expand', ItemDetailPage.onGalleryExpand);
        }
    },

    renderGallery: function (item) {

        var page = $.mobile.activePage;
        var imageTags = item.ImageTags || {};
        var html = '';

        if (imageTags.Logo) {

            html += ItemDetailPage.createGalleryImage(item.Id, "Logo", item.ImageTags.Logo);
        }
        if (imageTags.Thumb) {

            html += ItemDetailPage.createGalleryImage(item.Id, "Thumb", item.ImageTags.Thumb);
        }
        if (imageTags.Art) {

            html += ItemDetailPage.createGalleryImage(item.Id, "Art", item.ImageTags.Art);

        }
        if (imageTags.Menu) {

            html += ItemDetailPage.createGalleryImage(item.Id, "Menu", item.ImageTags.Menu);

        }
        if (imageTags.Disc) {

            html += ItemDetailPage.createGalleryImage(item.Id, "Disc", item.ImageTags.Disc);
        }
        if (imageTags.Box) {

            html += ItemDetailPage.createGalleryImage(item.Id, "Box", item.ImageTags.Box);
        }

        if (item.BackdropImageTags) {

            for (var i = 0, length = item.BackdropImageTags.length; i < length; i++) {
                html += ItemDetailPage.createGalleryImage(item.Id, "Backdrop", item.BackdropImageTags[0], i);
            }

        }

        $('#galleryContent', page).html(html).trigger('create');
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

    onMediaInfoExpand: function () {

        if (ItemDetailPage.item) {

            ItemDetailPage.renderMediaInfo(ItemDetailPage.item);

            $(this).off('expand', ItemDetailPage.onMediaInfoExpand);
        }
    },

    renderMediaInfo: function (item) {

        var page = $.mobile.activePage;
        var html = '';

        for (var i = 0, length = item.MediaStreams.length; i < length; i++) {

            var stream = item.MediaStreams[i];

            if (stream.Type == "Data") {
                continue;
            }

            var type;
            if (item.MediaType == "Audio" && stream.Type == "Video") {
                type = "Embedded Image";
            } else {
                type = stream.Type;
            }

            html += '<div class="mediaInfoStream">';

            html += '<p class="mediaInfoStreamType">' + type + '</p>';

            html += '<ul class="mediaInfoDetails">';

            if (stream.Codec) {
                html += '<li><span class="mediaInfoLabel">Codec</span> ' + stream.Codec + '</li>';
            }
            if (stream.Profile) {
                html += '<li><span class="mediaInfoLabel">Profile</span> ' + stream.Profile + '</li>';
            }
            if (stream.Level) {
                html += '<li><span class="mediaInfoLabel">Level</span> ' + stream.Level + '</li>';
            }
            if (stream.Language) {
                html += '<li><span class="mediaInfoLabel">Language</span> ' + stream.Language + '</li>';
            }
            if (stream.Width) {
                html += '<li><span class="mediaInfoLabel">Width</span> ' + stream.Width + '</li>';
            }
            if (stream.Height) {
                html += '<li><span class="mediaInfoLabel">Height</span> ' + stream.Height + '</li>';
            }
            if (stream.AspectRatio) {
                html += '<li><span class="mediaInfoLabel">Aspect Ratio</span> ' + stream.AspectRatio + '</li>';
            }
            if (stream.Bitrate) {
                html += '<li><span class="mediaInfoLabel">Bitrate</span> ' + stream.Bitrate + '</li>';
            }
            if (stream.Channels) {
                html += '<li><span class="mediaInfoLabel">Channels</span> ' + stream.Channels + '</li>';
            }
            if (stream.SampleRate) {
                html += '<li><span class="mediaInfoLabel">Sample Rate</span> ' + stream.SampleRate + '</li>';
            }

            var framerate = stream.AverageFrameRate || stream.RealFrameRate;

            if (framerate) {
                html += '<li><span class="mediaInfoLabel">Framerate</span> ' + framerate + '</li>';
            }

            if (stream.PixelFormat) {
                html += '<li><span class="mediaInfoLabel">Pixel Format</span> ' + stream.PixelFormat + '</li>';
            }

            if (stream.IsDefault) {
                html += '<li>Default</li>';
            }
            if (stream.IsForced) {
                html += '<li>Forced</li>';
            }
            if (stream.IsExternal) {
                html += '<li>External</li>';
            }
            if (stream.Path) {
                html += '<li><span class="mediaInfoLabel">Path</span> ' + stream.Path + '</li>';
            }

            html += '</ul>';

            html += '</div>';
        }

        $('#mediaInfoContent', page).html(html).trigger('create');

        $('#mediaInfoCollapsible', page).show();
    },

    playTrailer: function (index) {
        ApiClient.getLocalTrailers(Dashboard.getCurrentUserId(), ItemDetailPage.item.Id).done(function (trailers) {
            MediaPlayer.play([trailers[index]]);
        });
    },

    onTrailersExpand: function () {

        if (ItemDetailPage.item) {

            ItemDetailPage.renderTrailers(ItemDetailPage.item);

            $(this).off('expand', ItemDetailPage.onTrailersExpand);
        }
    },

    renderTrailers: function (item) {

        var html = '';
        var page = $.mobile.activePage;

        ApiClient.getLocalTrailers(Dashboard.getCurrentUserId(), item.Id).done(function (trailers) {

            for (var i = 0, length = trailers.length; i < length; i++) {

                var trailer = trailers[i];

                html += '<div class="posterViewItem posterViewItemWithDualText">';
                html += '<a href="#play-Trailer-' + i + '" onclick="ItemDetailPage.playTrailer(' + i + ');">';

                var imageTags = trailer.ImageTags || {};

                if (imageTags.Primary) {

                    var imgUrl = ApiClient.getImageUrl(trailer.Id, {
                        maxwidth: 500,
                        tag: imageTags.Primary,
                        type: "primary"
                    });

                    html += '<img src="' + imgUrl + '" />';
                } else {
                    html += '<img src="css/images/items/detail/video.png"/>';
                }

                html += '<div class="posterViewItemText posterViewItemPrimaryText">' + trailer.Name + '</div>';
                html += '<div class="posterViewItemText">';

                if (trailer.RunTimeTicks != "") {
                    html += ticks_to_human(trailer.RunTimeTicks);
                }
                else {
                    html += "&nbsp;";
                }
                html += '</div>';

                html += '</a>';

                html += '</div>';
            }

            $('#trailersContent', page).html(html);

        });

    },

    playSpecial: function (index) {
        ApiClient.getSpecialFeatures(Dashboard.getCurrentUserId(), ItemDetailPage.item.Id).done(function (specials) {
            MediaPlayer.play([specials[index]]);
        });
    },

    onSpecialsExpand: function () {

        if (ItemDetailPage.item) {

            ItemDetailPage.renderSpecials(ItemDetailPage.item);

            $(this).off('expand', ItemDetailPage.onSpecialsExpand);
        }
    },

    renderSpecials: function (item) {

        var html = '';
        var page = $.mobile.activePage;

        ApiClient.getSpecialFeatures(Dashboard.getCurrentUserId(), item.Id).done(function (specials) {

            for (var i = 0, length = specials.length; i < length; i++) {

                var special = specials[i];

                html += '<div class="posterViewItem posterViewItemWithDualText">';
                html += '<a href="#play-Special-' + i + '" onclick="ItemDetailPage.playSpecial(' + i + ');">';

                var imageTags = special.ImageTags || {};

                if (imageTags.Primary) {

                    var imgUrl = ApiClient.getImageUrl(special.Id, {
                        maxwidth: 500,
                        tag: imageTags.Primary,
                        type: "primary"
                    });

                    html += '<img src="' + imgUrl + '" />';
                } else {
                    html += '<img src="css/images/items/detail/video.png"/>';
                }

                html += '<div class="posterViewItemText posterViewItemPrimaryText">' + special.Name + '</div>';
                html += '<div class="posterViewItemText">';

                if (special.RunTimeTicks != "") {
                    html += ticks_to_human(special.RunTimeTicks);
                }
                else {
                    html += "&nbsp;";
                }
                html += '</div>';

                html += '</a>';

                html += '</div>';
            }

            $('#specialsContent', page).html(html);

        });
    },

    onCastExpand: function () {

        if (ItemDetailPage.item) {

            ItemDetailPage.renderCast(ItemDetailPage.item);

            $(this).off('expand', ItemDetailPage.onCastExpand);
        }
    },

    renderCast: function (item) {

        var html = '';
        var page = $.mobile.activePage;
        var casts = item.People || {};

        for (var i = 0, length = casts.length; i < length; i++) {

            var cast = casts[i];
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

        }

        $('#castContent', page).html(html);
    },

    renderFav: function (item) {
        var html = '';
        var page = $.mobile.activePage;

        var userData = item.UserData || {};

        //played/unplayed
        if (userData.Played) {
            html += '<img class="imgUserItemRating" src="css/images/userdata/played.png" alt="Played" title="Played" onclick="ItemDetailPage.setPlayed();" />';
        } else {
            html += '<img class="imgUserItemRating" src="css/images/userdata/unplayed.png" alt="Played" title="Played" onclick="ItemDetailPage.setPlayed();" />';
        }

        if (typeof userData.Likes == "undefined") {
            html += '<img class="imgUserItemRating" src="css/images/userdata/thumbs_down_off.png" alt="Dislike" title="Dislike" onclick="ItemDetailPage.setDislike();" />';
            html += '<img class="imgUserItemRating" src="css/images/userdata/thumbs_up_off.png" alt="Like" title="Like" onclick="ItemDetailPage.setLike();" />';
        } else if (userData.Likes) {
            html += '<img class="imgUserItemRating" src="css/images/userdata/thumbs_down_off.png" alt="Dislike" title="Dislike" onclick="ItemDetailPage.setDislike();" />';
            html += '<img class="imgUserItemRating" src="css/images/userdata/thumbs_up_on.png" alt="Liked" title="Like" onclick="ItemDetailPage.clearLike();" />';
        } else {
            html += '<img class="imgUserItemRating" src="css/images/userdata/thumbs_down_on.png" alt="Dislike" title="Dislike" onclick="ItemDetailPage.clearLike();" />';
            html += '<img class="imgUserItemRating" src="css/images/userdata/thumbs_up_off.png" alt="Like" title="Like" onclick="ItemDetailPage.setLike();" />';
        }

        if (userData.IsFavorite) {
            html += '<img class="imgUserItemRating" src="css/images/userdata/heart_on.png" alt="Favorite" title="Favorite" onclick="ItemDetailPage.setFavorite();" />';
        } else {
            html += '<img class="imgUserItemRating" src="css/images/userdata/heart_off.png" alt="Favorite" title="Favorite" onclick="ItemDetailPage.setFavorite();" />';
        }

        $('#itemRatings', page).html(html);
    },

    setFavorite: function () {
        var item = ItemDetailPage.item;

        item.UserData = item.UserData || {};

        var setting = !item.UserData.IsFavorite;
        item.UserData.IsFavorite = setting;

        ApiClient.updateFavoriteStatus(Dashboard.getCurrentUserId(), item.Id, setting);

        ItemDetailPage.renderFav(item);
    },

    setLike: function () {

        var item = ItemDetailPage.item;

        item.UserData = item.UserData || {};

        item.UserData.Likes = true;

        ApiClient.updateUserItemRating(Dashboard.getCurrentUserId(), item.Id, true);

        ItemDetailPage.renderFav(item);
    },

    clearLike: function () {

        var item = ItemDetailPage.item;

        item.UserData = item.UserData || {};

        item.UserData.Likes = undefined;

        ApiClient.clearUserItemRating(Dashboard.getCurrentUserId(), item.Id);

        ItemDetailPage.renderFav(item);
    },

    setDislike: function () {
        var item = ItemDetailPage.item;

        item.UserData = item.UserData || {};

        item.UserData.Likes = false;

        ApiClient.updateUserItemRating(Dashboard.getCurrentUserId(), item.Id, false);

        ItemDetailPage.renderFav(item);
    },

    setPlayed: function () {
        var item = ItemDetailPage.item;

        item.UserData = item.UserData || {};

        var setting = !item.UserData.Played;
        item.UserData.Played = setting;

        ApiClient.updatePlayedStatus(Dashboard.getCurrentUserId(), item.Id, setting);

        ItemDetailPage.renderFav(item);
    }

};

$(document).on('pageshow', "#itemDetailPage", ItemDetailPage.onPageShow).on('pagehide', "#itemDetailPage", ItemDetailPage.onPageHide);
