(function ($, document, LibraryBrowser, window) {

    var currentItem;

    function reload(page) {

        var id = getParameterByName('id');

        Dashboard.showLoadingMsg();

        ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(function (item) {

            currentItem = item;

            renderHeader(page, item);

            var name = item.Name;

            if (item.IndexNumber != null) {
                name = item.IndexNumber + " - " + name;
            }
            if (item.ParentIndexNumber != null) {
                name = item.ParentIndexNumber + "." + name;
            }

            $('#itemImage', page).html(LibraryBrowser.getDetailImageHtml(item));

            Dashboard.setPageTitle(name);

            $('#itemName', page).html(name);

            if (item.SeriesName) {

                $('#seriesName', page).html('<a class="detailPageParentLink" href="itemdetails.html?id=' + item.SeriesId + '">' + item.SeriesName + '</a>').show().trigger('create');
            }
            else if (item.Album && item.Type == "Audio" && item.ParentId) {
                $('#seriesName', page).html('<a class="detailPageParentLink" href="itemdetails.html?id=' + item.ParentId + '">' + item.Album + '</a>').show().trigger('create');

            }
            else if (item.Album) {
                $('#seriesName', page).html(item.Album).show();

            }
            else {
                $('#seriesName', page).hide();
            }

            var context = getContext(item);
            setInitialCollapsibleState(page, item, context);
            renderDetails(page, item, context);

            if (MediaPlayer.canPlay(item)) {
                $('#btnPlayMenu', page).show();
                $('#playButtonShadow', page).show();
                if (MediaPlayer.isPlaying())
                    $('#btnQueueMenu', page).show();
                else
                    $('#btnQueueMenu', page).hide();
            } else {
                $('#btnPlayMenu', page).hide();
                $('#playButtonShadow', page).hide();
                $('#btnQueueMenu', page).hide();
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
        else if (item.MediaType == "Game") {
            enableCustomHeader(page, "Games");
            $('#standardLogo', page).hide();
        }
        else {
            $('.libraryPageHeader', page).hide();
            $('#standardLogo', page).show();
        }

        if (item.Type == "MusicAlbum") {
            $('#albumTabs', page).show();
        } else {
            $('#albumTabs', page).hide();
        }

        if (item.Type == "Audio") {
            $('#songTabs', page).show();
        } else {
            $('#songTabs', page).hide();
        }

        if (item.Type == "Movie") {
            $('#movieTabs', page).show();
        } else {
            $('#movieTabs', page).hide();
        }

        if (item.MediaType == "Game") {
            $('#gameTabs', page).show();
        } else {
            $('#gameTabs', page).hide();
        }

        if (item.Type == "BoxSet") {
            $('#boxsetTabs', page).show();
        } else {
            $('#boxsetTabs', page).hide();
        }
        if (item.Type == "Trailer") {
            $('#trailerTabs', page).show();
        } else {
            $('#trailerTabs', page).hide();
        }
        if (item.Type == "Episode" || item.Type == "Season" || item.Type == "Series") {
            $('#tvShowsTabs', page).show();
        } else {
            $('#tvShowsTabs', page).hide();
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
            renderScenes(page, item);
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
            renderSpecials(page, item);
        }
        if (!item.People || !item.People.length) {
            $('#castCollapsible', page).hide();
        } else {
            $('#castCollapsible', page).show();
            renderCast(page, item, context);
        }

        $('#themeSongsCollapsible', page).hide();

        ApiClient.getThemeSongs(Dashboard.getCurrentUserId(), item.Id).done(function (result) {
            renderThemeSongs(page, item, result);
        });
    }

    function renderDetails(page, item, context) {

        if (item.Taglines && item.Taglines.length) {
            $('#itemTagline', page).html(item.Taglines[0]).show();
        } else {
            $('#itemTagline', page).hide();
        }

        LibraryBrowser.renderOverview($('#itemOverview', page), item);

        if (item.CommunityRating) {
            $('#itemCommunityRating', page).html(LibraryBrowser.getStarRatingHtml(item)).show().attr('title', item.CommunityRating);
        } else {
            $('#itemCommunityRating', page).hide();
        }

        LibraryBrowser.renderPremiereDate($('#itemPremiereDate', page), item);
        LibraryBrowser.renderBudget($('#itemBudget', page), item);
        LibraryBrowser.renderRevenue($('#itemRevenue', page), item);

        $('#itemMiscInfo', page).html(LibraryBrowser.getMiscInfoHtml(item));

        LibraryBrowser.renderGenres($('#itemGenres', page), item, context);
        LibraryBrowser.renderStudios($('#itemStudios', page), item, context);
        renderUserDataIcons(page, item);
        LibraryBrowser.renderLinks($('#itemLinks', page), item);
    }

    function renderChildren(page, item) {

        ApiClient.getItems(Dashboard.getCurrentUserId(), {

            ParentId: getParameterByName('id'),
            SortBy: "SortName",
            Fields: "PrimaryImageAspectRatio,ItemCounts,DisplayMediaType,DateCreated,UserData"

        }).done(function (result) {

            if (item.Type == "MusicAlbum") {

                $('#itemSongs', page).html(LibraryBrowser.getSongTableHtml(result.Items)).trigger('create');

            } else {
                var html = LibraryBrowser.getPosterDetailViewHtml({
                    items: result.Items,
                    useAverageAspectRatio: true
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
        else {
            $('#childrenTitle', page).html('Items (' + item.ChildCount + ')');
        }
    }
    function renderUserDataIcons(page, item) {
        $('#itemRatings', page).html(LibraryBrowser.getUserDataIconsHtml(item));
    }

    function renderThemeSongs(page, item, result) {

        if (result.Items.length) {

            $('#themeSongsCollapsible', page).show();

            $('#themeSongsContent', page).html(LibraryBrowser.getSongTableHtml(result.Items, { showArtist: true, showAlbum: true })).trigger('create');
        }
    }

    function renderScenes(page, item) {
        var html = '';

        var chapters = item.Chapters || {};

        for (var i = 0, length = chapters.length; i < length; i++) {

            var chapter = chapters[i];
            var chapterName = chapter.Name || "Chapter " + i;

            html += '<div class="scenePosterViewItem posterViewItem posterViewItemWithDualText">';
            html += '<a href="#play-Chapter-' + i + '" onclick="ItemDetailPage.play(' + chapter.StartPositionTicks + ');">';

            if (chapter.ImageTag) {

                var imgUrl = ApiClient.getImageUrl(item.Id, {
                    width: 400,
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
            if (stream.BitRate) {
                html += '<li><span class="mediaInfoLabel">Bitrate</span> ' + stream.BitRate + '</li>';
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
    }

    function renderSpecials(page, item) {
        var html = '';

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
    }

    function renderTrailers(page, item) {
        var html = '';

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
    }

    function renderCast(page, item, context) {
        var html = '';

        var casts = item.People || [];

        for (var i = 0, length = casts.length; i < length; i++) {

            var cast = casts[i];

            html += LibraryBrowser.createCastImage(cast, context);
        }

        $('#castContent', page).html(html);
    }

    function play(startPosition) {

        MediaPlayer.play([currentItem], startPosition);
    }

    $(document).on('pageinit', "#itemDetailPage", function () {

        var page = this;

        $('#btnPlayMenu', page).on('click', function () {

            var userdata = currentItem.UserData || {};

            if (userdata.PlaybackPositionTicks) {

                var pos = $('#playMenuAnchor', page).offset();

                $('#playMenu', page).popup("open", {
                    x: pos.left + 125,
                    y: pos.top + 20
                });

            }
            else {
                play();
            }
        });

        $('#btnQueueMenu', page).on('click', function () {
            var pos = $('#queueMenuAnchor', page).offset();

            $('#queueMenu', page).popup("open", {
                x: pos.left + 165,
                y: pos.top + 20
            });
        });


        $('#btnPlay', page).on('click', function () {

            $('#playMenu', page).popup("close");
            play();
        });

        $('#btnResume', page).on('click', function () {

            $('#playMenu', page).popup("close");

            var userdata = currentItem.UserData || {};

            play(userdata.PlaybackPositionTicks);
        });

        $('#btnQueue', page).on('click', function () {

            $('#queueMenu', page).popup("close");
            Playlist.add(currentItem);
        });

    }).on('pageshow', "#itemDetailPage", function () {

        var page = this;

        reload(page);

    }).on('pagehide', "#itemDetailPage", function () {

        currentItem = null;
    });

    function itemDetailPage() {

        var self = this;

        self.play = play;

        self.playTrailer = function (index) {
            ApiClient.getLocalTrailers(Dashboard.getCurrentUserId(), currentItem.Id).done(function (trailers) {
                MediaPlayer.play([trailers[index]]);
            });
        };

        self.playSpecial = function (index) {
            ApiClient.getSpecialFeatures(Dashboard.getCurrentUserId(), currentItem.Id).done(function (specials) {
                MediaPlayer.play([specials[index]]);
            });
        };
    }

    window.ItemDetailPage = new itemDetailPage();


})(jQuery, document, LibraryBrowser, window);