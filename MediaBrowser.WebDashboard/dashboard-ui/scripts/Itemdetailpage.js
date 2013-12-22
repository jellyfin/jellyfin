(function ($, document, LibraryBrowser, window) {

    var currentItem;

    function reload(page) {

        var id = getParameterByName('id');

        Dashboard.showLoadingMsg();

        ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(function (item) {

            currentItem = item;

            renderHeader(page, item);

            LibraryBrowser.renderName(item, $('.itemName', page));
            LibraryBrowser.renderParentName(item, $('.parentName', page));

            var context = getContext(item);

            Dashboard.getCurrentUser().done(function (user) {

                var imageHref = user.Configuration.IsAdministrator ? "edititemimages.html?id=" + item.Id : "";

                $('#itemImage', page).html(LibraryBrowser.getDetailImageHtml(item, imageHref));

                setInitialCollapsibleState(page, item, context, user);
                renderDetails(page, item, context);
                LibraryBrowser.renderDetailPageBackdrop(page, item);

                if (user.Configuration.IsAdministrator) {
                    $('#editButtonContainer', page).show();
                } else {
                    $('#editButtonContainer', page).hide();
                }

            });

            if (item.LocationType == "Offline") {

                $('#offlineIndicator', page).show();
            }
            else {
                $('#offlineIndicator', page).hide();
            }

            var isMissingEpisode = false;

            if (item.LocationType == "Virtual" && item.Type == "Episode") {
                try {
                    if (item.PremiereDate && (new Date().getTime() >= parseISO8601Date(item.PremiereDate, { toLocal: true }).getTime())) {
                        isMissingEpisode = true;
                    }
                } catch (err) {

                }
            }

            if (isMissingEpisode) {

                $('#missingIndicator', page).show();
            }
            else {
                $('#missingIndicator', page).hide();
            }

            if (MediaPlayer.canPlay(item) && item.LocationType !== "Offline" && item.LocationType !== "Virtual") {

                var url = MediaPlayer.getPlayUrl(item);

                if (url) {
                    $('#playExternalButtonContainer', page).show();
                    $('#playButtonContainer', page).hide();
                } else {
                    $('#playButtonContainer', page).show();
                    $('#playExternalButtonContainer', page).hide();
                }

                $('#btnPlayExternal', page).attr('href', url || '#');

            } else {
                $('#playButtonContainer', page).hide();
                $('#playExternalButtonContainer', page).hide();
            }

            $(".autoNumeric").autoNumeric('init');

            setPeopleHeader(page, item);

            if (ApiClient.isWebSocketOpen()) {
                ApiClient.sendWebSocketMessage("Context", [item.Type, item.Id, item.Name, context].join('|'));
            }

            Dashboard.hideLoadingMsg();
        });
    }

    function setPeopleHeader(page, item) {

        if (item.Type == "Audio" || item.Type == "MusicAlbum" || item.MediaType == "Book" || item.MediaType == "Photo") {
            $('#peopleHeader', page).html('People');
        } else {
            $('#peopleHeader', page).html('Cast & Crew');
        }

    }

    function getContext(item) {

        // should return either movies, tv, music or games

        if (item.Type == "Episode" || item.Type == "Series" || item.Type == "Season") {
            return "tv";
        }
        if (item.Type == "Movie" || item.Type == "Trailer" || item.Type == "BoxSet") {
            return "movies";
        }
        if (item.Type == "Audio" || item.Type == "MusicAlbum" || item.Type == "MusicArtist" || item.Type == "MusicVideo") {
            return "music";
        }
        if (item.MediaType == "Game") {
            return "games";
        }
        return "";
    }

    function renderHeader(page, item) {

        $('.itemTabs', page).hide();

        if (item.Type == "MusicAlbum") {
            $('#albumTabs', page).show();
        }

        if (item.Type == "MusicVideo") {
            $('#musicVideoTabs', page).show();
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

        if (item.Type == "GameSystem") {
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

    function setInitialCollapsibleState(page, item, context, user) {

        if (item.IsFolder) {
            $('#childrenCollapsible', page).removeClass('hide');
            renderChildren(page, item, user);
        }
        else {
            $('#childrenCollapsible', page).addClass('hide');
        }

        if (item.MediaStreams && item.MediaStreams.length) {
            renderMediaInfo(page, item);
        }
        if (!item.Chapters || !item.Chapters.length) {
            $('#scenesCollapsible', page).hide();
        } else {
            $('#scenesCollapsible', page).show();
            renderScenes(page, item, 4);
        }
        if (!item.LocalTrailerCount && !item.RemoteTrailers.length) {
            $('#trailersCollapsible', page).addClass('hide');
        } else {
            $('#trailersCollapsible', page).removeClass('hide');
            renderTrailers(page, item);
        }
        if (!item.SpecialFeatureCount || item.SpecialFeatureCount == 0 || item.Type == "Series") {
            $('#specialsCollapsible', page).addClass('hide');
        } else {
            $('#specialsCollapsible', page).removeClass('hide');
            renderSpecials(page, item, 6);
        }
        if (!item.People || !item.People.length) {
            $('#castCollapsible', page).hide();
        } else {
            $('#castCollapsible', page).show();
            renderCast(page, item, context, 8);
        }

        if (!item.PartCount || item.PartCount < 2) {
            $('#additionalPartsCollapsible', page).addClass('hide');
        } else {
            $('#additionalPartsCollapsible', page).removeClass('hide');
            renderAdditionalParts(page, item);
        }

        $('#themeSongsCollapsible', page).hide();
        $('#themeVideosCollapsible', page).hide();

        if (!item.SoundtrackIds || !item.SoundtrackIds.length) {
            $('#soundtracksCollapsible', page).hide();
        } else {
            $('#soundtracksCollapsible', page).show();
            renderSoundtracks(page, item);
        }

        if (item.Type == "MusicAlbum") {
            renderMusicVideos(page, item);
        } else {
            $('#musicVideosCollapsible', page).hide();
        }

        renderThemeSongs(page, item);
        renderThemeVideos(page, item);
        renderCriticReviews(page, item, 1);
    }

    function renderDetails(page, item, context) {

        renderSimilarItems(page, item);
        renderSiblingLinks(page, item);

        if (item.Taglines && item.Taglines.length) {
            $('#itemTagline', page).html(item.Taglines[0]).show();
        } else {
            $('#itemTagline', page).hide();
        }

        LibraryBrowser.renderOverview($('.itemOverview', page), item);

        if (item.CommunityRating || item.CriticRating) {
            $('.itemCommunityRating', page).html(LibraryBrowser.getRatingHtml(item)).show();
        } else {
            $('.itemCommunityRating', page).hide();
        }

        if (item.Type != "Episode" && item.Type != "Movie" && item.Type != "Series") {
            var premiereDateElem = $('#itemPremiereDate', page).show();
            LibraryBrowser.renderPremiereDate(premiereDateElem, item);
        } else {
            $('#itemPremiereDate', page).hide();
        }

        LibraryBrowser.renderBudget($('#itemBudget', page), item);
        LibraryBrowser.renderRevenue($('#itemRevenue', page), item);

        $('.itemMiscInfo', page).html(LibraryBrowser.getMiscInfoHtml(item));

        LibraryBrowser.renderGenres($('.itemGenres', page), item, context);
        LibraryBrowser.renderStudios($('.itemStudios', page), item, context);
        renderUserDataIcons(page, item);
        LibraryBrowser.renderLinks($('.itemExternalLinks', page), item);

        $('.criticRatingScore', page).html((item.CriticRating || '0') + '%');

        if (item.CriticRatingSummary) {
            $('#criticRatingSummary', page).show();
            $('.criticRatingSummaryText', page).html(item.CriticRatingSummary);

        } else {
            $('#criticRatingSummary', page).hide();
        }

        renderTags(page, item);

        renderSeriesAirTime(page, item, context);

        if (item.Players) {
            $('#players', page).show().html(item.Players + ' Player');
        } else {
            $('#players', page).hide();
        }

        if (item.Artists && item.Artists.length && item.Type != "MusicAlbum") {
            $('#artist', page).show().html(getArtistLinksHtml(item.Artists)).trigger('create');
        } else {
            $('#artist', page).hide();
        }

        var detailsSection = $('#detailsSection', page);
        var elem = $('.detailSectionContent', detailsSection)[0];
        var text = elem.textContent || elem.innerText;

        if (!text.trim()) {
            detailsSection.addClass('hide');
        } else {
            detailsSection.removeClass('hide');
        }
    }

    function getArtistLinksHtml(artists) {

        var html = [];

        for (var i = 0, length = artists.length; i < length; i++) {

            var artist = artists[i];

            html.push('<a class="textlink" href="itembynamedetails.html?context=music&musicartist=' + ApiClient.encodeName(artist) + '">' + artist + '</a>');

        }

        html = html.join(' / ');

        if (artists.length == 1) {
            return 'Artist:&nbsp;&nbsp;' + html;
        }
        if (artists.length > 1) {
            return 'Artists:&nbsp;&nbsp;' + html;
        }

        return html;
    }

    function renderSoundtracks(page, item) {

        if (item.Type == "MusicAlbum") {
            $('#soundtracksHeader', page).html("This album is the soundtrack for ...");
        } else {
            $('#soundtracksHeader', page).html("Soundtrack(s)");
        }

        ApiClient.getItems(Dashboard.getCurrentUserId(), {

            Ids: item.SoundtrackIds.join(","),
            ItemFields: "PrimaryImageAspectRatio,ItemCounts,DateCreated,AudioInfo",
            SortBy: "SortName"

        }).done(function (result) {

            var html = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                useAverageAspectRatio: true,
                showNewIndicator: true,
                shape: item.Type == "MusicAlbum" ? "portrait" : "square"
            });

            $('#soundtracksContent', page).html(html);
        });
    }

    function renderSiblingLinks(page, item) {

        $('.lnkSibling', page).addClass('hide');

        if ((item.Type != "Episode" && item.Type != "Season" && item.Type != "Audio") || item.IndexNumber == null) {
            return;
        }

        var promise;

        if (item.Type == "Season") {

            promise = ApiClient.getSeasons(item.SeriesId, {

                userId: Dashboard.getCurrentUserId(),
                AdjacentTo: item.Id
            });
        }
        else if (item.Type == "Episode") {

            // Use dedicated episodes endpoint
            promise = ApiClient.getEpisodes(item.SeriesId, {

                seasonId: item.SeasonId,
                userId: Dashboard.getCurrentUserId(),
                AdjacentTo: item.Id
            });

        } else {
            promise = ApiClient.getItems(Dashboard.getCurrentUserId(), {
                AdjacentTo: item.Id,
                ParentId: item.ParentId
            });
        }

        promise.done(function (result) {

            for (var i = 0, length = result.Items.length; i < length; i++) {

                var curr = result.Items[i];

                if (curr.IndexNumber == null) {
                    continue;
                }

                var friendlyTypeName = item.Type == "Audio" ? "song" : item.Type.toLowerCase();

                if (curr.IndexNumber < item.IndexNumber) {

                    $('.lnkPreviousItem', page).removeClass('hide').attr('href', 'itemdetails.html?id=' + curr.Id).html('← Previous ' + friendlyTypeName);
                }
                else if (curr.IndexNumber > item.IndexNumber) {

                    $('.lnkNextItem', page).removeClass('hide').attr('href', 'itemdetails.html?id=' + curr.Id).html('Next ' + friendlyTypeName + ' →');
                }
            }
        });
    }

    function renderSimilarItems(page, item) {

        var promise;

        var options = {
            userId: Dashboard.getCurrentUserId(),
            limit: item.Type == "MusicAlbum" ? 5 : 5,
            fields: "PrimaryImageAspectRatio,DateCreated,UserData"
        };

        if (item.Type == "Movie") {
            promise = ApiClient.getSimilarMovies(item.Id, options);
        }
        else if (item.Type == "Trailer") {
            promise = ApiClient.getSimilarTrailers(item.Id, options);
        }
        else if (item.Type == "MusicAlbum") {
            promise = ApiClient.getSimilarAlbums(item.Id, options);
        }
        else if (item.Type == "Series") {
            promise = ApiClient.getSimilarShows(item.Id, options);
        }
        else if (item.MediaType == "Game") {
            promise = ApiClient.getSimilarGames(item.Id, options);
        } else {
            $('#similarCollapsible', page).hide();
            return;
        }

        promise.done(function (result) {

            if (!result.Items.length) {

                $('#similarCollapsible', page).hide();
                return;
            }

            var elem = $('#similarCollapsible', page).show();

            $('.detailSectionHeader', elem).html('If you like ' + item.Name + ', check these out...');

            var html = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                useAverageAspectRatio: item.MediaType != "Game",
                showNewIndicator: true,
                shape: item.Type == "MusicAlbum" ? "square" : "portrait"
            });

            $('#similarContent', page).html(html);
        });
    }

    function renderSeriesAirTime(page, item, context) {

        if (item.Type != "Series") {
            $('#seriesAirTime', page).hide();
            return;
        }

        var html = '';

        if (item.AirDays && item.AirDays.length) {
            html += item.AirDays.length == 7 ? 'daily' : item.AirDays.map(function (a) {
                return a + "s";

            }).join(',');
        }

        if (item.AirTime) {
            html += ' at ' + item.AirTime;
        }

        if (item.Studios.length) {
            html += ' on <a class="textlink" href="itembynamedetails.html?context=' + context + '&studio=' + ApiClient.encodeName(item.Studios[0].Name) + '">' + item.Studios[0].Name + '</a>';
        }

        if (html) {
            html = (item.Status == 'Ended' ? 'Aired ' : 'Airs ') + html;

            $('#seriesAirTime', page).show().html(html).trigger('create');
        } else {
            $('#seriesAirTime', page).hide();
        }
    }

    function renderTags(page, item) {

        if (item.Tags && item.Tags.length) {

            var html = '';

            for (var i = 0, length = item.Tags.length; i < length; i++) {

                html += '<div class="itemTag">' + item.Tags[i] + '</div>';

            }

            $('.itemTags', page).show().html(html);

        } else {
            $('.itemTags', page).hide();
        }
    }

    function renderChildren(page, item, user) {

        var sortBy = item.Type == "BoxSet" ? "ProductionYear,SortName" : "SortName";

        var query = {
            ParentId: item.Id,
            SortBy: sortBy,
            Fields: "ItemCounts,DateCreated,AudioInfo"
        };

        var promise;

        if (item.Type == "Series") {

            promise = ApiClient.getSeasons(item.Id, {

                userId: user.Id
            });
        }
        else if (item.Type == "Season") {

            // Use dedicated episodes endpoint
            promise = ApiClient.getEpisodes(item.SeriesId, {

                seasonId: item.Id,
                userId: user.Id
            });
        }

        promise = promise || ApiClient.getItems(Dashboard.getCurrentUserId(), query);

        promise.done(function (result) {

            if (item.Type == "MusicAlbum") {

                $('#childrenContent', page).html(LibraryBrowser.getSongTableHtml(result.Items, { showArtist: true })).trigger('create');

            } else {

                var shape = "smallPoster";

                if (item.Type == "Season") {
                    shape = "smallBackdrop";
                }

                var html = LibraryBrowser.getPosterDetailViewHtml({
                    items: result.Items,
                    useAverageAspectRatio: true,
                    shape: shape,
                    showParentName: false,
                    displayAsSpecial: item.Type == "Season" && item.IndexNumber
                });

                $('#childrenContent', page).html(html);

            }
        });

        if (item.Type == "Season") {
            $('#childrenTitle', page).html('Episodes');
        }
        else if (item.Type == "Series") {
            $('#childrenTitle', page).html('Seasons');
        }
        else if (item.Type == "BoxSet") {
            $('#childrenTitle', page).html('Movies');
        }
        else if (item.Type == "MusicAlbum") {
            $('#childrenTitle', page).html('Tracks');
        }
        else if (item.Type == "GameSystem") {
            $('#childrenTitle', page).html('Games');
        }
        else {
            $('#childrenTitle', page).html('Items');
        }
    }
    function renderUserDataIcons(page, item) {
        $('.userDataIcons', page).html(LibraryBrowser.getUserDataIconsHtml(item));
    }

    function renderCriticReviews(page, item, limit) {

        if (item.Type != "Movie" && item.Type != "Trailer" && item.Type != "MusicVideo") {
            $('#criticReviewsCollapsible', page).hide();
            return;
        }

        var options = {};

        if (limit) {
            options.limit = limit;
        }

        ApiClient.getCriticReviews(item.Id, options).done(function (result) {

            if (result.TotalRecordCount || item.CriticRatingSummary) {
                $('#criticReviewsCollapsible', page).show();
                renderCriticReviewsContent(page, result, limit);
            } else {
                $('#criticReviewsCollapsible', page).hide();
            }
        });
    }

    function renderCriticReviewsContent(page, result, limit) {

        var html = '';

        var reviews = result.Items;

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

                    var date = parseISO8601Date(review.Date, { toLocal: true }).toLocaleDateString();

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
            html += '<p style="margin: 0;padding-left: .5em;"><button class="moreCriticReviews" data-inline="true" data-mini="true">More ...</button></p>';
        }

        $('#criticReviewsContent', page).html(html).trigger('create');
    }

    function renderThemeSongs(page, item) {

        ApiClient.getThemeSongs(Dashboard.getCurrentUserId(), item.Id).done(function (result) {
            if (result.Items.length) {

                $('#themeSongsCollapsible', page).show();

                $('#themeSongsContent', page).html(LibraryBrowser.getSongTableHtml(result.Items, { showArtist: true, showAlbum: true })).trigger('create');
            } else {
                $('#themeSongsCollapsible', page).hide();
            }
        });

    }

    function renderMusicVideos(page, item) {

        ApiClient.getItems(Dashboard.getCurrentUserId(), {

            SortBy: "SortName",
            SortOrder: "Ascending",
            IncludeItemTypes: "MusicVideo",
            Recursive: true,
            Fields: "DateCreated",
            Albums: item.Name

        }).done(function (result) {
            if (result.Items.length) {

                $('#musicVideosCollapsible', page).show();

                $('#musicVideosContent', page).html(getVideosHtml(result.Items)).trigger('create');
            } else {
                $('#musicVideosCollapsible', page).hide();
            }
        });

    }

    function renderThemeVideos(page, item) {

        ApiClient.getThemeVideos(Dashboard.getCurrentUserId(), item.Id).done(function (result) {
            if (result.Items.length) {

                $('#themeVideosCollapsible', page).show();

                $('#themeVideosContent', page).html(getVideosHtml(result.Items)).trigger('create');
            } else {
                $('#themeVideosCollapsible', page).hide();
            }
        });

    }

    function renderAdditionalParts(page, item) {

        ApiClient.getAdditionalVideoParts(Dashboard.getCurrentUserId(), item.Id).done(function (result) {

            if (result.Items.length) {

                $('#additionalPartsCollapsible', page).show();

                $('#additionalPartsContent', page).html(getVideosHtml(result.Items)).trigger('create');
            } else {
                $('#additionalPartsCollapsible', page).hide();
            }
        });
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
                    maxwidth: 400,
                    tag: chapter.ImageTag,
                    type: "Chapter",
                    index: i
                });
            } else {
                imgUrl = "css/images/items/list/chapter.png";
            }

            html += '<div class="posterItemImage" style="background-image:url(\'' + imgUrl + '\');"></div>';

            html += '<div class="posterItemTextOverlay">';
            html += '<div class="posterItemText">' + chapterName + '</div>';
            html += '<div class="posterItemText">';

            html += Dashboard.getDisplayTime(chapter.StartPositionTicks);

            html += '</div>';
            html += '</div>';

            html += '</a>';
        }

        if (limit && chapters.length > limit) {
            html += '<p style="margin: 0;padding-left: .5em;"><button class="moreScenes" data-inline="true" data-mini="true">More ...</button></p>';
        }

        $('#scenesContent', page).html(html).trigger('create');
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

            html += '<span class="mediaInfoStreamType">' + type + ':</span>';

            var attributes = [];

            if (stream.Language) {
                attributes.push('<span class="mediaInfoAttribute">' + stream.Language + '</span>');
            }

            if (stream.Codec && stream.Codec != "dca") {
                attributes.push('<span class="mediaInfoAttribute">' + stream.Codec + '</span>');
            }

            if (stream.Profile && stream.Codec == "dca") {
                attributes.push('<span class="mediaInfoAttribute">' + stream.Profile + '</span>');
            }

            if (stream.Width || stream.Height) {
                attributes.push('<span class="mediaInfoAttribute">' + stream.Width + 'x' + stream.Height + '</span>');
            }

            if (stream.AspectRatio && stream.Codec != "mjpeg") {
                attributes.push('<span class="mediaInfoAttribute">' + stream.AspectRatio + '</span>');
            }

            if (stream.ChannelLayout) {
                attributes.push('<span class="mediaInfoAttribute">' + stream.ChannelLayout + '</span>');
            }
            else if (stream.Channels) {
                attributes.push('<span class="mediaInfoAttribute">' + stream.Channels + ' ch</span>');
            }

            if (stream.BitRate && stream.Codec != "mjpeg") {
                attributes.push('<span class="mediaInfoAttribute">' + (parseInt(stream.BitRate / 1000)) + ' kbps</span>');
            }

            if (stream.IsDefault) {
                attributes.push('<span class="mediaInfoAttribute">Default</span>');
            }
            if (stream.IsForced) {
                attributes.push('<span class="mediaInfoAttribute">Forced</span>');
            }
            if (stream.IsExternal) {
                attributes.push('<span class="mediaInfoAttribute">External</span>');
            }

            html += attributes.join('&nbsp;&#149;&nbsp;');

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

            var href = "itemdetails.html?id=" + item.Id;

            html += '<a class="' + cssClass + '" href="' + href + '" onclick="MediaPlayer.playById(\'' + item.Id + '\'); return false;">';

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

            html += '<div class="posterItemTextOverlay">';
            html += '<div class="posterItemText">' + item.Name + '</div>';
            html += '<div class="posterItemText">';

            if (item.RunTimeTicks != "") {
                html += Dashboard.getDisplayTime(item.RunTimeTicks);
            }
            else {
                html += "&nbsp;";
            }
            html += '</div>';
            html += '</div>';

            html += '</a>';

        }

        if (limit && items.length > limit) {
            html += '<p style="margin: 0;padding-left: .5em;"><button class="' + moreButtonClass + '" data-inline="true" data-mini="true">More ...</button></p>';
        }

        return html;
    }

    function renderSpecials(page, item, limit) {

        ApiClient.getSpecialFeatures(Dashboard.getCurrentUserId(), item.Id).done(function (specials) {

            $('#specialsContent', page).html(getVideosHtml(specials, limit, "moreSpecials")).trigger('create');

        });
    }

    function renderTrailers(page, item) {

        if (item.Type == "Trailer") {
            $('#trailerSectionHeader', page).html('More trailers');
        } else {
            $('#trailerSectionHeader', page).html('Trailers');
        }

        var remoteTrailersHtml = '';

        for (var i = 0, length = item.RemoteTrailers.length; i < length; i++) {

            var trailer = item.RemoteTrailers[i];

            var id = getParameterByName('v', trailer.Url);

            if (id) {
                remoteTrailersHtml += '<iframe class="posterItem smallBackdropPosterItem" style="margin:0 3px;width:auto;" src="//www.youtube.com/embed/' + id + '" frameborder="0" allowfullscreen></iframe>';
            }
        }

        var elem = $('#trailersContent', page).html(remoteTrailersHtml);

        if (item.LocalTrailerCount) {
            ApiClient.getLocalTrailers(Dashboard.getCurrentUserId(), item.Id).done(function (trailers) {

                elem.prepend(getVideosHtml(trailers));

            });
        }
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

            if (role == "GuestStar") {
                role = "Guest star";
            }

            role = role || "";

            var maxlength = 40;

            if (role.length > maxlength) {
                role = role.substring(0, maxlength - 3) + '...';
            }

            html += '<p>' + role + '</p>';

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

            var mediaType = currentItem.MediaType;

            if (currentItem.Type == "MusicArtist" || currentItem.Type == "MusicAlbum") {
                mediaType = "Audio";
            }

            LibraryBrowser.showPlayMenu(this, currentItem.Id, currentItem.Type, mediaType, userdata.PlaybackPositionTicks);
        });

        $('#btnPlayExternal', page).on('click', function () {

            ApiClient.markPlayed(Dashboard.getCurrentUserId(), currentItem.Id, new Date());
        });

        $('#btnEdit', page).on('click', function () {

            Dashboard.navigate("edititemmetadata.html?id=" + currentItem.Id);
        });

        $('#btnRemote', page).on('click', function () {

            RemoteControl.showMenuForItem({

                item: currentItem,
                context: getContext(currentItem),

                themeSongs: $('#themeSongsCollapsible:visible', page).length > 0,

                themeVideos: $('#themeVideosCollapsible:visible', page).length > 0
            });
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