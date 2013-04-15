(function ($, document, LibraryBrowser, window) {

    var currentItem;

    function reload(page) {

        var id = getParameterByName('id');

        Dashboard.showLoadingMsg();

        ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(function (item) {

            currentItem = item;

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

            if (item.SeriesName || item.Album) {
                var seriesName = item.SeriesName || item.Album;
                $('#seriesName', page).html(seriesName).show();
            }

            setInitialCollapsibleState(page, item);
            renderDetails(page, item);

            if (MediaPlayer.canPlay(item)) {
                $('#btnPlayMenu', page).show();
                $('#playButtonShadow', page).show();
                $('#btnQueueMenu', page).hide();
            } else {
                $('#btnPlayMenu', page).hide();
                $('#playButtonShadow', page).hide();
                $('#btnQueueMenu', page).hide();
            }

            Dashboard.hideLoadingMsg();
        });
    }

    function setInitialCollapsibleState(page, item) {

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
    }

    function renderDetails(page, item) {

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

        LibraryBrowser.renderBudget($('#itemBudget', page), item);

        $('#itemMiscInfo', page).html(LibraryBrowser.getMiscInfoHtml(item));

        LibraryBrowser.renderGenres($('#itemGenres', page), item);
        LibraryBrowser.renderStudios($('#itemStudios', page), item);
        renderUserDataIcons(page, item);
        LibraryBrowser.renderLinks($('#itemLinks', page), item);
    }

    function renderUserDataIcons(page, item) {
        $('#itemRatings', page).html(LibraryBrowser.getUserDataIconsHtml(item));
    }

    function renderScenes(page, item) {
        var html = '';

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
    }

    function renderGallery(page, item) {

        var imageTags = item.ImageTags || {};
        var html = '';
        var i, length;

        if (imageTags.Logo) {

            html += createGalleryImage(item.Id, "Logo", item.ImageTags.Logo);
        }
        if (imageTags.Thumb) {

            html += createGalleryImage(item.Id, "Thumb", item.ImageTags.Thumb);
        }
        if (imageTags.Art) {

            html += createGalleryImage(item.Id, "Art", item.ImageTags.Art);

        }
        if (imageTags.Menu) {

            html += createGalleryImage(item.Id, "Menu", item.ImageTags.Menu);

        }
        if (imageTags.Disc) {

            html += createGalleryImage(item.Id, "Disc", item.ImageTags.Disc);
        }
        if (imageTags.Box) {

            html += createGalleryImage(item.Id, "Box", item.ImageTags.Box);
        }

        if (item.BackdropImageTags) {

            for (i = 0, length = item.BackdropImageTags.length; i < length; i++) {
                html += createGalleryImage(item.Id, "Backdrop", item.BackdropImageTags[0], i);
            }

        }

        if (item.ScreenshotImageTags) {

            for (i = 0, length = item.ScreenshotImageTags.length; i < length; i++) {
                html += createGalleryImage(item.Id, "Screenshot", item.ScreenshotImageTags[0], i);
            }
        }

        $('#galleryContent', page).html(html).trigger('create');
    }

    function createGalleryImage(itemId, type, tag, index) {

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

    function renderCast(page, item) {
        var html = '';

        var casts = item.People || [];

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

        $('#mediaInfoCollapsible', page).on('expand.lazyload', function () {
            renderMediaInfo(page, currentItem);

            $(this).off('expand.lazyload');
        });

        $('#scenesCollapsible', page).on('expand.lazyload', function () {

            if (currentItem) {

                renderScenes(page, currentItem);

                $(this).off('expand.lazyload');
            }
        });

        $('#specialsCollapsible', page).on('expand.lazyload', function () {
            renderSpecials(page, currentItem);

            $(this).off('expand.lazyload');
        });

        $('#trailersCollapsible', page).on('expand.lazyload', function () {
            renderTrailers(page, currentItem);

            $(this).off('expand.lazyload');
        });

        $('#castCollapsible', page).on('expand.lazyload', function () {
            renderCast(page, currentItem);

            $(this).off('expand.lazyload');
        });

        $('#galleryCollapsible', page).on('expand.lazyload', function () {

            renderGallery(page, currentItem);

            $(this).off('expand.lazyload');
        });

    }).on('pagehide', "#itemDetailPage", function () {

        currentItem = null;
        var page = this;

        $('#mediaInfoCollapsible', page).off('expand.lazyload');
        $('#scenesCollapsible', page).off('expand.lazyload');
        $('#specialsCollapsible', page).off('expand.lazyload');
        $('#trailersCollapsible', page).off('expand.lazyload');
        $('#castCollapsible', page).off('expand.lazyload');
        $('#galleryCollapsible', page).off('expand.lazyload');
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