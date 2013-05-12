(function ($, document, LibraryBrowser, window) {

    var currentItem;

    function reload(page) {

        var id = getParameterByName('id');

        Dashboard.showLoadingMsg();

        ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(function (item) {

            currentItem = item;

            renderHeader(page, item);

            $('#itemImage', page).html(LibraryBrowser.getDetailImageHtml(item));

            LibraryBrowser.renderTitle(item, $('#itemName', page), $('#parentName', page), $('#grandParentName', page));

            var context = getContext(item);
            setInitialCollapsibleState(page, item, context);
            renderDetails(page, item, context);

            if (MediaPlayer.canPlay(item)) {
                $('#playButtonContainer', page).show();
            } else {
                $('#playButtonContainer', page).hide();
            }

            Dashboard.getCurrentUser().done(function (user) {

                if (user.Configuration.IsAdministrator) {
                    $('#editButtonContainer', page).show();
                } else {
                    $('#editButtonContainer', page).hide();
                }

            });

            $(".autoNumeric").autoNumeric('init');

            if (ApiClient.isWebSocketOpen()) {
                ApiClient.sendWebSocketMessage("Context", [item.Type, item.Id, context].join('|'));
            }

            Dashboard.hideLoadingMsg();
        });
    }

    function getContext(item) {

        // should return either movies, tv, music or games

        if (item.Type == "Episode" || item.Type == "Series" || item.Type == "Season") {
            return "tv";
        }
        if (item.Type == "Movie" || item.Type == "Trailer" || item.Type == "BoxSet") {
            return "movies";
        }
        if (item.Type == "Audio" || item.Type == "MusicAlbum" || item.Type == "MusicArtist" || item.Type == "Artist") {
            return "music";
        }
        if (item.MediaType == "Game") {
            return "games";
        }
        return "";
    }

    function enableCustomHeader(page, text) {
        var elem = $('.libraryPageHeader', page).show();

        $('span', elem).html(text);
    }

    function renderHeader(page, item) {

        if (item.Type == "Movie" || item.Type == "Trailer" || item.Type == "BoxSet") {
            enableCustomHeader(page, "Movies");
            $('#standardLogo', page).hide();
        }
        else if (item.Type == "Episode" || item.Type == "Season" || item.Type == "Series") {
            enableCustomHeader(page, "TV Shows");
            $('#standardLogo', page).hide();
        }
        else if (item.Type == "Audio" || item.Type == "MusicAlbum") {
            enableCustomHeader(page, "Music");
            $('#standardLogo', page).hide();
        }
        else if (item.MediaType == "Game" || item.Type == "GamePlatform") {
            enableCustomHeader(page, "Games");
            $('#standardLogo', page).hide();
        }
        else {
            $('.libraryPageHeader', page).hide();
            $('#standardLogo', page).show();
        }

        $('.itemTabs', page).hide();

        if (item.Type == "MusicAlbum") {
            $('#albumTabs', page).show();
        }

        if (item.Type == "Audio") {
            $('#songTabs', page).show();
        }

        if (item.Type == "Movie") {
            $('#movieTabs', page).show();
        }

        if (item.MediaType == "Game") {
            $('#gameTabs', page).show();
        }

        if (item.Type == "GamePlatform") {
            $('#gameSystemTabs', page).show();
        }

        if (item.Type == "BoxSet") {
            $('#boxsetTabs', page).show();
        }

        if (item.Type == "Trailer") {
            $('#trailerTabs', page).show();
        }

        if (item.Type == "Episode" || item.Type == "Season" || item.Type == "Series") {
            $('#tvShowsTabs', page).show();
        }
    }

    function setInitialCollapsibleState(page, item, context) {

        if (item.ChildCount && item.Type == "MusicAlbum") {
            $('#itemSongs', page).show();
            $('#childrenCollapsible', page).hide();
            renderChildren(page, item);
        }
        else if (item.ChildCount) {
            $('#itemSongs', page).hide();
            $('#childrenCollapsible', page).show();
            renderChildren(page, item);
        }
        else {
            $('#itemSongs', page).hide();
            $('#childrenCollapsible', page).hide();
        }
        if (LibraryBrowser.shouldDisplayGallery(item)) {
            $('#galleryCollapsible', page).show();
            renderGallery(page, item);
        } else {
            $('#galleryCollapsible', page).hide();
        }

        if (!item.MediaStreams || !item.MediaStreams.length) {
            $('#mediaInfoCollapsible', page).hide();
        } else {
            $('#mediaInfoCollapsible', page).show();
            renderMediaInfo(page, item);
        }
        if (!item.Chapters || !item.Chapters.length) {
            $('#scenesCollapsible', page).hide();
        } else {
            $('#scenesCollapsible', page).show();
            renderScenes(page, item, 6);
        }
        if (!item.LocalTrailerCount || item.LocalTrailerCount == 0) {
            $('#trailersCollapsible', page).hide();
        } else {
            $('#trailersCollapsible', page).show();
            renderTrailers(page, item);
        }
        if (!item.SpecialFeatureCount || item.SpecialFeatureCount == 0) {
            $('#specialsCollapsible', page).hide();
        } else {
            $('#specialsCollapsible', page).show();
            renderSpecials(page, item, 6);
        }
        if (!item.People || !item.People.length) {
            $('#castCollapsible', page).hide();
        } else {
            $('#castCollapsible', page).show();
            renderCast(page, item, context, 10);
        }

        $('#themeSongsCollapsible', page).hide();
        $('#themeVideosCollapsible', page).hide();

        ApiClient.getThemeSongs(Dashboard.getCurrentUserId(), item.Id).done(function (result) {
            renderThemeSongs(page, item, result);
        });

        ApiClient.getThemeVideos(Dashboard.getCurrentUserId(), item.Id).done(function (result) {
            renderThemeVideos(page, item, result);
        });

        renderCriticReviews(page, item, 1);
    }

    function renderDetails(page, item, context) {

        if (item.Taglines && item.Taglines.length) {
            $('#itemTagline', page).html(item.Taglines[0]).show();
        } else {
            $('#itemTagline', page).hide();
        }

        LibraryBrowser.renderOverview($('#itemOverview', page), item);

        if (item.CommunityRating || item.CriticRating) {
            $('#itemCommunityRating', page).html(LibraryBrowser.getRatingHtml(item)).show();
        } else {
            $('#itemCommunityRating', page).hide();
        }

        if (item.Type != "Episode" && item.Type != "Movie") {
            var premiereDateElem = $('#itemPremiereDate', page).show();
            LibraryBrowser.renderPremiereDate(premiereDateElem, item);
        } else {
            $('#itemPremiereDate', page).hide();
        }

        LibraryBrowser.renderBudget($('#itemBudget', page), item);
        LibraryBrowser.renderRevenue($('#itemRevenue', page), item);

        $('#itemMiscInfo', page).html(LibraryBrowser.getMiscInfoHtml(item));

        LibraryBrowser.renderGenres($('#itemGenres', page), item, context);
        LibraryBrowser.renderStudios($('#itemStudios', page), item, context);
        renderUserDataIcons(page, item);
        LibraryBrowser.renderLinks($('#itemLinks', page), item);

        if (item.CriticRatingSummary) {
            $('#criticRatingSummary', page).show();
            $('#criticRatingSummaryText', page).html(item.CriticRatingSummary);

        } else {
            $('#criticRatingSummary', page).hide();
        }

        renderTags(page, item);
    }

    function renderTags(page, item) {

        if (item.Tags && item.Tags.length) {

            var html = '';

            for (var i = 0, length = item.Tags.length; i < length; i++) {

                html += '<div class="itemTag">' + item.Tags[i] + '</div>';

            }

            $('#itemTags', page).show().html(html);

        } else {
            $('#itemTags', page).hide();
        }
    }

    function renderChildren(page, item) {

        ApiClient.getItems(Dashboard.getCurrentUserId(), {

            ParentId: getParameterByName('id'),
            SortBy: "SortName",
            Fields: "PrimaryImageAspectRatio,ItemCounts,DisplayMediaType,DateCreated,UserData,AudioInfo"

        }).done(function (result) {

            if (item.Type == "MusicAlbum") {

                $('#itemSongs', page).html(LibraryBrowser.getSongTableHtml(result.Items, { showArtist: true })).trigger('create');

            } else {

                var shape = "smallPoster";

                if (item.Type == "Season") {
                    shape = "smallBackdrop";
                }

                var html = LibraryBrowser.getPosterDetailViewHtml({
                    items: result.Items,
                    useAverageAspectRatio: true,
                    shape: shape
                });

                $('#childrenContent', page).html(html);

            }
        });

        if (item.Type == "Season") {
            $('#childrenTitle', page).html('Episodes (' + item.ChildCount + ')');
        }
        else if (item.Type == "Series") {
            $('#childrenTitle', page).html('Seasons (' + item.ChildCount + ')');
        }
        else if (item.Type == "BoxSet") {
            $('#childrenTitle', page).html('Movies (' + item.ChildCount + ')');
        }
        else if (item.Type == "MusicAlbum") {
            $('#childrenTitle', page).html('Tracks (' + item.ChildCount + ')');
        }
        else if (item.Type == "GamePlatform") {
            $('#childrenTitle', page).html('Games (' + item.ChildCount + ')');
        }
        else {
            $('#childrenTitle', page).html('Items (' + item.ChildCount + ')');
        }
    }
    function renderUserDataIcons(page, item) {
        $('#itemRatings', page).html(LibraryBrowser.getUserDataIconsHtml(item));
    }

    function renderCriticReviews(page, item, limit) {

        var options = {};

        if (limit) {
            options.limit = limit;
        }

        ApiClient.getCriticReviews(item.Id, options).done(function (result) {

            if (result.TotalRecordCount) {
                $('#criticReviewsCollapsible', page).show();
                renderCriticReviewsContent(page, result, limit);
            } else {
                $('#criticReviewsCollapsible', page).hide();
            }
        });
    }

    function renderCriticReviewsContent(page, result, limit) {

        var html = '';

        var reviews = result.ItemReviews;

        for (var i = 0, length = reviews.length; i < length; i++) {

            var review = reviews[i];

            html += '<div class="criticReview">';

            html += '<div class="reviewScore">';


            if (review.Score != null) {
                html += review.Score;
            }
            else if (review.Likes != null) {

                if (review.Likes) {
                    html += '<img src="css/images/fresh.png" />';
                } else {
                    html += '<img src="css/images/rotten.png" />';
                }
            }
            
            html += '</div>';

            html += '<div class="reviewCaption">' + review.Caption + '</div>';

            var vals = [];

            if (review.ReviewerName) {
                vals.push(review.ReviewerName);
            }
            if (review.Publisher) {
                vals.push(review.Publisher);
            }

            html += '<div class="reviewerName">' + vals.join(', ') + '.';

            if (review.Date) {

                try {

                    var date = parseISO8601Date(review.Date, true).toLocaleDateString();

                    html += '<span class="reviewDate">' + date + '</span>';
                }
                catch (error) {

                }

            }

            html += '</div>';

            if (review.Url) {
                html += '<div class="reviewLink"><a class="textlink" href="' + review.Url + '" target="_blank">Full review</a></div>';
            }

            html += '</div>';
        }

        if (limit && result.TotalRecordCount > limit) {
            html += '<p style="margin: .5em 0 0;padding-left: .5em;"><button class="moreCriticReviews" data-inline="true" data-mini="true">More ...</button></p>';
        }

        $('#criticReviewsContent', page).html(html).trigger('create');
    }

    function renderThemeSongs(page, item, result) {

        if (result.Items.length) {

            $('#themeSongsCollapsible', page).show();

            $('#themeSongsContent', page).html(LibraryBrowser.getSongTableHtml(result.Items, { showArtist: true, showAlbum: true })).trigger('create');
        }
    }

    function renderThemeVideos(page, item, result) {

        if (result.Items.length) {

            $('#themeVideosCollapsible', page).show();

            $('#themeVideosContent', page).html(getVideosHtml(result.Items)).trigger('create');
        }
    }

    function renderScenes(page, item, limit) {
        var html = '';

        var chapters = item.Chapters || [];

        for (var i = 0, length = chapters.length; i < length; i++) {

            if (limit && i >= limit) {
                break;
            }

            var chapter = chapters[i];
            var chapterName = chapter.Name || "Chapter " + i;

            html += '<a class="posterItem smallBackdropPosterItem" href="#play-Chapter-' + i + '" onclick="ItemDetailPage.play(' + chapter.StartPositionTicks + ');">';

            var imgUrl;

            if (chapter.ImageTag) {

                imgUrl = ApiClient.getImageUrl(item.Id, {
                    width: 400,
                    tag: chapter.ImageTag,
                    type: "Chapter",
                    index: i
                });
            } else {
                imgUrl = "css/images/items/list/chapter.png";
            }

            html += '<div class="posterItemImage" style="background-image:url(\'' + imgUrl + '\');"></div>';

            html += '<div class="posterItemText">' + chapterName + '</div>';
            html += '<div class="posterItemText">';

            html += ticks_to_human(chapter.StartPositionTicks);

            html += '</div>';

            html += '</a>';
        }

        if (limit && chapters.length > limit) {
            html += '<p style="margin: .5em 0 0;padding-left: .5em;"><button class="moreScenes" data-inline="true" data-mini="true">More ...</button></p>';
        }

        $('#scenesContent', page).html(html).trigger('create');
    }

    function renderGallery(page, item) {

        var html = LibraryBrowser.getGalleryHtml(item);

        $('#galleryContent', page).html(html).trigger('create');
    }

    function renderMediaInfo(page, item) {

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
                html += '<li><span class="mediaInfoLabel">Codec: </span> ' + stream.Codec + '</li>';
            }
            if (stream.Profile) {
                html += '<li><span class="mediaInfoLabel">Profile: </span> ' + stream.Profile + '</li>';
            }
            if (stream.Level) {
                html += '<li><span class="mediaInfoLabel">Level: </span> ' + stream.Level + '</li>';
            }
            if (stream.Language) {
                html += '<li><span class="mediaInfoLabel">Language: </span> ' + stream.Language + '</li>';
            }
            if (stream.Width) {
                html += '<li><span class="mediaInfoLabel">Width: </span> ' + stream.Width + '</li>';
            }
            if (stream.Height) {
                html += '<li><span class="mediaInfoLabel">Height: </span> ' + stream.Height + '</li>';
            }
            if (stream.AspectRatio) {
                html += '<li><span class="mediaInfoLabel">Aspect Ratio: </span> ' + stream.AspectRatio + '</li>';
            }
            if (stream.BitRate) {
                html += '<li><span class="mediaInfoLabel">Bitrate: </span> <span class="autoNumeric" data-a-pad="false">' + (parseInt(stream.BitRate / 1000)) + ' kbps</span></li>';
            }
            if (stream.Channels) {
                html += '<li><span class="mediaInfoLabel">Channels: </span> ' + stream.Channels + '</li>';
            }
            if (stream.SampleRate) {
                html += '<li><span class="mediaInfoLabel">Sample Rate: </span> <span class="autoNumeric" data-a-pad="false">' + stream.SampleRate + ' khz</span></li>';
            }

            var framerate = stream.AverageFrameRate || stream.RealFrameRate;

            if (framerate) {
                html += '<li><span class="mediaInfoLabel">Framerate: </span> ' + framerate + '</li>';
            }

            if (stream.PixelFormat) {
                html += '<li><span class="mediaInfoLabel">Pixel Format: </span> ' + stream.PixelFormat + '</li>';
            }

            if (stream.IsDefault || stream.IsForced || stream.IsExternal) {

                var vals = [];

                if (stream.IsDefault) {
                    vals.push("Default");
                }
                if (stream.IsForced) {
                    vals.push("Forced");
                }
                if (stream.IsExternal) {
                    vals.push("External");
                }

                html += '<li><span class="mediaInfoLabel">Flags: </span> ' + vals.join(", ") + '</li>';
            }

            if (stream.Path) {
                html += '<li><span class="mediaInfoLabel">Path: </span> ' + stream.Path + '</li>';
            }

            html += '</ul>';

            html += '</div>';
        }

        $('#mediaInfoContent', page).html(html).trigger('create');
    }

    function getVideosHtml(items, limit, moreButtonClass) {

        var html = '';

        for (var i = 0, length = items.length; i < length; i++) {

            if (limit && i >= limit) {
                break;
            }

            var item = items[i];

            var cssClass = "posterItem smallBackdropPosterItem";

            html += '<a class="' + cssClass + '" href="#" onclick="MediaPlayer.playById(\'' + item.Id + '\');">';

            var imageTags = item.ImageTags || {};

            var imgUrl;

            if (imageTags.Primary) {

                imgUrl = ApiClient.getImageUrl(item.Id, {
                    maxwidth: 500,
                    tag: imageTags.Primary,
                    type: "primary"
                });

            } else {
                imgUrl = "css/images/items/detail/video.png";
            }

            html += '<div class="posterItemImage" style="background-image:url(\'' + imgUrl + '\');"></div>';

            html += '<div class="posterItemText">' + item.Name + '</div>';
            html += '<div class="posterItemText">';

            if (item.RunTimeTicks != "") {
                html += ticks_to_human(item.RunTimeTicks);
            }
            else {
                html += "&nbsp;";
            }
            html += '</div>';

            html += '</a>';

        }

        if (limit && items.length > limit) {
            html += '<p style="margin: .5em 0 0;padding-left: .5em;"><button class="' + moreButtonClass + '" data-inline="true" data-mini="true">More ...</button></p>';
        }

        return html;
    }

    function renderSpecials(page, item, limit) {

        ApiClient.getSpecialFeatures(Dashboard.getCurrentUserId(), item.Id).done(function (specials) {

            $('#specialsContent', page).html(getVideosHtml(specials, limit, "moreSpecials")).trigger('create');

        });
    }

    function renderTrailers(page, item) {

        ApiClient.getLocalTrailers(Dashboard.getCurrentUserId(), item.Id).done(function (trailers) {

            $('#trailersContent', page).html(getVideosHtml(trailers));

        });
    }

    function renderCast(page, item, context, limit) {

        var html = '';

        var casts = item.People || [];

        for (var i = 0, length = casts.length; i < length; i++) {

            if (limit && i >= limit) {
                break;
            }

            var cast = casts[i];

            html += '<a class="tileItem smallPosterTileItem" href="itembynamedetails.html?context=' + context + '&person=' + ApiClient.encodeName(cast.Name) + '">';

            var imgUrl;

            if (cast.PrimaryImageTag) {

                imgUrl = ApiClient.getPersonImageUrl(cast.Name, {
                    width: 130,
                    tag: cast.PrimaryImageTag,
                    type: "primary"
                });

            } else {

                imgUrl = "css/images/items/list/person.png";
            }

            html += '<div class="tileImage" style="background-image:url(\'' + imgUrl + '\');"></div>';



            html += '<div class="tileContent">';

            html += '<p>' + cast.Name + '</p>';

            var role = cast.Role ? "as " + cast.Role : cast.Type;

            html += '<p>' + (role || "") + '</p>';

            html += '</div>';

            html += '</a>';
        }

        if (limit && casts.length > limit) {
            html += '<p style="margin: .5em 0 0;padding-left: .5em;"><button class="morePeople" data-inline="true" data-mini="true">More ...</button></p>';
        }

        $('#castContent', page).html(html).trigger('create');
    }

    function play(startPosition) {

        MediaPlayer.play([currentItem], startPosition);
    }

    $(document).on('pageinit', "#itemDetailPage", function () {

        var page = this;

        $('#btnPlay', page).on('click', function () {
            var userdata = currentItem.UserData || {};
            LibraryBrowser.showPlayMenu(this, currentItem.Id, currentItem.MediaType, userdata.PlaybackPositionTicks);
        });

        $('#btnEdit', page).on('click', function () {

            Dashboard.navigate("edititemmetadata.html?id=" + currentItem.Id);
        });

    }).on('pageshow', "#itemDetailPage", function () {

        var page = this;

        $(page).on("click.moreScenes", ".moreScenes", function () {

            renderScenes(page, currentItem);

        }).on("click.morePeople", ".morePeople", function () {

            renderCast(page, currentItem, getContext(currentItem));

        }).on("click.moreSpecials", ".moreSpecials", function () {

            renderSpecials(page, currentItem);

        }).on("click.moreCriticReviews", ".moreCriticReviews", function () {

            renderCriticReviews(page, currentItem);

        });

        reload(page);

    }).on('pagehide', "#itemDetailPage", function () {

        currentItem = null;

        var page = this;

        $(page).off("click.moreScenes").off("click.morePeople").off("click.moreSpecials").off("click.moreCriticReviews");
    });

    function itemDetailPage() {

        var self = this;

        self.play = play;
    }

    window.ItemDetailPage = new itemDetailPage();


})(jQuery, document, LibraryBrowser, window);