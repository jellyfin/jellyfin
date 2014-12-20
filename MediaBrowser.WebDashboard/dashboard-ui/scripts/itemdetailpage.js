(function ($, document, LibraryBrowser, window) {

    var currentItem;

    function getExternalPlayUrl(item) {

        var providerIds = item.ProviderIds || {};
        if (item.GameSystem == "Nintendo" && item.MediaType == "Game" && providerIds.NesBox && providerIds.NesBoxRom) {

            return "http://nesbox.com/game/" + providerIds.NesBox + '/rom/' + providerIds.NesBoxRom;
        }

        if (item.GameSystem == "Super Nintendo" && item.MediaType == "Game" && providerIds.NesBox && providerIds.NesBoxRom) {

            return "http://snesbox.com/game/" + providerIds.NesBox + '/rom/' + providerIds.NesBoxRom;
        }

        return null;
    }

    function reload(page) {

        var id = getParameterByName('id');

        Dashboard.showLoadingMsg();

        ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(function (item) {

            reloadFromItem(page, item);
        });
    }

    function reloadFromItem(page, item) {

        currentItem = item;

        var context = getContext(item);

        renderHeader(page, item, context);

        LibraryBrowser.renderName(item, $('.itemName', page), false, context);
        LibraryBrowser.renderParentName(item, $('.parentName', page), context);

        Dashboard.getCurrentUser().done(function (user) {

            renderImage(page, item, user);

            setInitialCollapsibleState(page, item, context, user);
            renderDetails(page, item, context);
            LibraryBrowser.renderDetailPageBackdrop(page, item);

            var externalPlayUrl = getExternalPlayUrl(item);
            $('.btnPlayExternal', page).attr('href', externalPlayUrl || '#');

            if (externalPlayUrl) {
                $('.btnPlayExternal', page).removeClass('hide');
                $('.btnPlay', page).addClass('hide');
            }
            else if (MediaController.canPlay(item)) {
                $('.btnPlay', page).removeClass('hide');
                $('.btnPlayExternal', page).addClass('hide');
            }
            else {
                $('.btnPlay', page).addClass('hide');
                $('.btnPlayExternal', page).addClass('hide');
            }

            if (item.LocalTrailerCount && item.PlayAccess == 'Full') {
                $('.btnPlayTrailer', page).removeClass('hide');
            } else {
                $('.btnPlayTrailer', page).addClass('hide');
            }

            if (SyncManager.isAvailable(item, user)) {
                $('.btnSync', page).removeClass('hide');
            } else {
                $('.btnSync', page).addClass('hide');
            }

            if (!item.LocalTrailerCount && item.RemoteTrailers.length && item.PlayAccess == 'Full') {

                $('.btnPlayExternalTrailer', page).removeClass('hide').attr('href', item.RemoteTrailers[0].Url);

            } else {

                $('.btnPlayExternalTrailer', page).addClass('hide').attr('href', '#');
            }

            var groupedVersions = (item.MediaSources || []).filter(function (g) {
                return g.Type == "Grouping";
            });

            if (user.Policy.IsAdministrator && groupedVersions.length) {
                $('.splitVersionContainer', page).show();
            } else {
                $('.splitVersionContainer', page).hide();
            }

            if (LibraryBrowser.getMoreCommands(item, user).length) {
                $('.btnMoreCommands', page).show();
            } else {
                $('.btnMoreCommands', page).show();
            }
        });

        if (item.LocationType == "Offline") {

            $('.offlineIndicator', page).show();
        }
        else {
            $('.offlineIndicator', page).hide();
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

            $('.missingIndicator', page).show();
        }
        else {
            $('.missingIndicator', page).hide();
        }

        setPeopleHeader(page, item);

        $(page).trigger('displayingitem', [{

            item: item,
            context: context
        }]);

        Dashboard.hideLoadingMsg();
    }

    function renderImage(page, item, user) {

        var imageHref = user.Policy.IsAdministrator && item.MediaType != 'Photo' ? "edititemimages.html?id=" + item.Id : "";

        $('#itemImage', page).html(LibraryBrowser.getDetailImageHtml(item, imageHref));
    }

    function onWebSocketMessage(e, data) {

        var msg = data;
        var page = $.mobile.activePage;

        if (msg.MessageType === "UserDataChanged") {

            if (currentItem && msg.Data.UserId == Dashboard.getCurrentUserId()) {

                var key = currentItem.UserData.Key;

                var userData = msg.Data.UserDataList.filter(function (u) {

                    return u.Key == key;
                })[0];

                if (userData) {

                    currentItem.UserData = userData;
                    renderUserDataIcons(page, currentItem);

                    Dashboard.getCurrentUser().done(function (user) {

                        renderImage(page, currentItem, user);
                    });
                }
            }
        }

    }

    function setPeopleHeader(page, item) {

        if (item.Type == "Audio" || item.Type == "MusicAlbum" || item.MediaType == "Book" || item.MediaType == "Photo") {
            $('#peopleHeader', page).html(Globalize.translate('HeaderPeople'));
        } else {
            $('#peopleHeader', page).html(Globalize.translate('HeaderCastAndCrew'));
        }

    }

    function getContext(item) {

        // should return either movies, tv, music or games
        var context = getParameterByName('context');

        if (context) {
            return context;
        }

        if (item.Type == "Episode" || item.Type == "Series" || item.Type == "Season") {
            return "tv";
        }
        if (item.Type == "Movie" || item.Type == "Trailer") {
            return "movies";
        }
        if (item.Type == "Audio" || item.Type == "MusicAlbum" || item.Type == "MusicArtist" || item.Type == "MusicVideo") {
            return "music";
        }
        if (item.MediaType == "Game") {
            return "games";
        }
        if (item.Type == "BoxSet") {
            return "boxsets";
        }
        return "";
    }

    function renderHeader(page, item, context) {

        $('.itemTabs', page).hide();
        $('.channelHeader', page).hide();
        var elem;

        if (context == 'home') {
            elem = $('.homeTabs', page).show();
            $('a', elem).removeClass('ui-btn-active');
            $('.lnkHomeHome', page).addClass('ui-btn-active');
        }
        else if (context == 'home-nextup') {
            elem = $('.homeTabs', page).show();
            $('a', elem).removeClass('ui-btn-active');
            $('.lnkHomeNextUp', page).addClass('ui-btn-active');
        }
        else if (context == 'home-favorites') {
            elem = $('.homeTabs', page).show();
            $('a', elem).removeClass('ui-btn-active');
            $('.lnkHomeFavorites', page).addClass('ui-btn-active');
        }
        else if (context == 'home-upcoming') {
            elem = $('.homeTabs', page).show();
            $('a', elem).removeClass('ui-btn-active');
            $('.lnkHomeUpcoming', page).addClass('ui-btn-active');
        }
        else if (context == 'home-latest') {
            elem = $('.homeTabs', page).show();
            $('a', elem).removeClass('ui-btn-active');
            $('.lnkHomeLatest', page).addClass('ui-btn-active');
        }
        else if (context == 'movies' || item.Type == 'Movie' || context == 'movies-trailers') {
            elem = $('#movieTabs', page).show();
            $('a', elem).removeClass('ui-btn-active');

            if (item.Type == 'BoxSet') {
                $('.lnkCollections', page).addClass('ui-btn-active');
            }
            else if (context == 'movies-trailers') {
                $('.lnkMovieTrailers', page).addClass('ui-btn-active');
            }
            else {
                $('.lnkMovies', page).addClass('ui-btn-active');
            }
        }
        else if (item.Type == "MusicAlbum") {
            $('#albumTabs', page).show();
        }

        else if (item.Type == "MusicVideo") {
            $('#musicVideoTabs', page).show();
        }

        else if (item.Type == "Audio") {
            $('#songTabs', page).show();
        }

        else if (item.Type == "ChannelVideoItem" || item.Type == "ChannelAudioItem" || item.Type == "ChannelFolderItem") {
            $('#channelTabs', page).show();
            $('.channelHeader', page).show().html('<a href="channelitems.html?id=' + item.ChannelId + '">' + item.ChannelName + '</a>').trigger('create');
        }

        else if (item.Type == "BoxSet") {
            $('#boxsetTabs', page).show();
        }

        else if (item.MediaType == "Game") {
            $('#gameTabs', page).show();
        }

        else if (item.Type == "GameSystem") {
            $('#gameSystemTabs', page).show();
        }

        else if (item.Type == "Episode" || item.Type == "Season" || item.Type == "Series") {
            $('#tvShowsTabs', page).show();
        }
    }

    function setInitialCollapsibleState(page, item, context, user) {

        $('.collectionItems', page).empty();

        if (item.IsFolder) {

            if (item.Type == "BoxSet") {
                $('#childrenCollapsible', page).addClass('hide');
            } else {
                $('#childrenCollapsible', page).removeClass('hide');
            }
            renderChildren(page, item, user, context);
        }
        else {
            $('#childrenCollapsible', page).addClass('hide');
        }

        if (item.MediaSources && item.MediaSources.length) {
            renderMediaSources(page, item);
        }

        var chapters = item.Chapters || [];

        if (!chapters.length) {
            $('#scenesCollapsible', page).hide();
        } else {
            $('#scenesCollapsible', page).show();
            renderScenes(page, item, user, 3);
        }

        if (!item.SpecialFeatureCount || item.SpecialFeatureCount == 0 || item.Type == "Series") {
            $('#specialsCollapsible', page).addClass('hide');
        } else {
            $('#specialsCollapsible', page).removeClass('hide');
            renderSpecials(page, item, user, 6);
        }
        if (!item.People || !item.People.length) {
            $('#castCollapsible', page).hide();
        } else {
            $('#castCollapsible', page).show();
            renderCast(page, item, context, 6);
        }

        if (!item.PartCount || item.PartCount < 2) {
            $('#additionalPartsCollapsible', page).addClass('hide');
        } else {
            $('#additionalPartsCollapsible', page).removeClass('hide');
            renderAdditionalParts(page, item, user);
        }

        $('#themeSongsCollapsible', page).hide();
        $('#themeVideosCollapsible', page).hide();

        if (item.Type == "MusicAlbum") {
            renderMusicVideos(page, item, user);
        } else {
            $('#musicVideosCollapsible', page).hide();
        }

        renderThemeMedia(page, item, user);
        renderCriticReviews(page, item, 1);
    }

    function renderDetails(page, item, context) {

        renderSimilarItems(page, item, context);
        renderSiblingLinks(page, item, context);

        if (item.Taglines && item.Taglines.length) {
            $('#itemTagline', page).html(item.Taglines[0]).show();
        } else {
            $('#itemTagline', page).hide();
        }

        LibraryBrowser.renderOverview($('.itemOverview', page), item);

        $('.itemCommunityRating', page).html(LibraryBrowser.getRatingHtml(item));

        LibraryBrowser.renderBudget($('#itemBudget', page), item);
        LibraryBrowser.renderRevenue($('#itemRevenue', page), item);
        LibraryBrowser.renderAwardSummary($('#awardSummary', page), item);

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
        renderKeywords(page, item);

        renderSeriesAirTime(page, item, context);

        if (item.Players) {
            $('#players', page).show().html(item.Players + ' Player');
        } else {
            $('#players', page).hide();
        }

        if (item.Artists && item.Artists.length && item.Type != "MusicAlbum") {
            $('#artist', page).show().html(getArtistLinksHtml(item.Artists, context)).trigger('create');
        } else {
            $('#artist', page).hide();
        }

        if (item.MediaSources && item.MediaSources.length && item.Path) {
            $('.audioVideoMediaInfo', page).removeClass('hide');
        } else {
            $('.audioVideoMediaInfo', page).addClass('hide');
        }

        if (item.MediaType == 'Photo') {
            $('.photoInfo', page).removeClass('hide');
            renderPhotoInfo(page, item);
        } else {
            $('.photoInfo', page).addClass('hide');
        }

        renderTabButtons(page, item);
    }

    function renderPhotoInfo(page, item) {

        var html = '';

        var attributes = [];

        if (item.CameraMake) {
            attributes.push(createAttribute(Globalize.translate('MediaInfoCameraMake'), item.CameraMake));
        }

        if (item.CameraModel) {
            attributes.push(createAttribute(Globalize.translate('MediaInfoCameraModel'), item.CameraModel));
        }

        if (item.Altitude) {
            attributes.push(createAttribute(Globalize.translate('MediaInfoAltitude'), item.Altitude.toFixed(1)));
        }

        if (item.Aperture) {
            attributes.push(createAttribute(Globalize.translate('MediaInfoAperture'), 'F' + item.Aperture.toFixed(1)));
        }

        if (item.ExposureTime) {

            var val = 1 / item.ExposureTime;

            attributes.push(createAttribute(Globalize.translate('MediaInfoExposureTime'), '1/' + val + ' s'));
        }

        if (item.FocalLength) {
            attributes.push(createAttribute(Globalize.translate('MediaInfoFocalLength'), item.FocalLength.toFixed(1) + ' mm'));
        }

        if (item.ImageOrientation) {
            attributes.push(createAttribute(Globalize.translate('MediaInfoOrientation'), item.ImageOrientation));
        }

        if (item.IsoSpeedRating) {
            attributes.push(createAttribute(Globalize.translate('MediaInfoIsoSpeedRating'), item.IsoSpeedRating));
        }

        if (item.Latitude) {
            attributes.push(createAttribute(Globalize.translate('MediaInfoLatitude'), item.Latitude.toFixed(1)));
        }

        if (item.Longitude) {
            attributes.push(createAttribute(Globalize.translate('MediaInfoLongitude'), item.Longitude.toFixed(1)));
        }

        if (item.ShutterSpeed) {
            attributes.push(createAttribute(Globalize.translate('MediaInfoShutterSpeed'), item.ShutterSpeed));
        }

        if (item.Software) {
            attributes.push(createAttribute(Globalize.translate('MediaInfoSoftware'), item.Software));
        }

        html += attributes.join('<br/>');

        $('.photoInfoContent', page).html(html).trigger('create');
    }

    function renderTabButtons(page, item) {

        var elem = $('.tabDetails', page)[0];
        var text = elem.textContent || elem.innerText;

        if (text.trim()) {

            $('.detailsSection', page).removeClass('hide');

        } else {
            $('.detailsSection', page).addClass('hide');
        }
    }

    function getArtistLinksHtml(artists, context) {

        var html = [];

        for (var i = 0, length = artists.length; i < length; i++) {

            var artist = artists[i];

            html.push('<a class="textlink" href="itembynamedetails.html?context=' + context + '&musicartist=' + ApiClient.encodeName(artist) + '">' + artist + '</a>');

        }

        html = html.join(' / ');

        if (artists.length == 1) {
            return Globalize.translate('ValueArtist', html);
        }
        if (artists.length > 1) {
            return Globalize.translate('ValueArtists', html);
        }

        return html;
    }

    function renderSiblingLinks(page, item, context) {

        $('.lnkSibling', page).addClass('hide');

        if ((item.Type != "Episode" && item.Type != "Season" && item.Type != "Audio" && item.Type != "Photo")) {
            return;
        }

        var promise;

        if (item.Type == "Season") {

            promise = ApiClient.getSeasons(item.SeriesId, {

                userId: Dashboard.getCurrentUserId(),
                AdjacentTo: item.Id
            });
        }
        else if (item.Type == "Episode" && item.SeasonId) {

            // Use dedicated episodes endpoint
            promise = ApiClient.getEpisodes(item.SeriesId, {

                seasonId: item.SeasonId,
                userId: Dashboard.getCurrentUserId(),
                AdjacentTo: item.Id
            });

        } else {
            promise = ApiClient.getItems(Dashboard.getCurrentUserId(), {
                AdjacentTo: item.Id,
                ParentId: item.ParentId,
                SortBy: 'SortName'
            });
        }

        context = context || '';

        promise.done(function (result) {

            var foundExisting = false;

            for (var i = 0, length = result.Items.length; i < length; i++) {

                var curr = result.Items[i];

                if (curr.Id == item.Id) {
                    foundExisting = true;
                }
                else if (!foundExisting) {

                    $('.lnkPreviousItem', page).removeClass('hide').attr('href', 'itemdetails.html?id=' + curr.Id + '&context=' + context);
                }
                else {

                    $('.lnkNextItem', page).removeClass('hide').attr('href', 'itemdetails.html?id=' + curr.Id + '&context=' + context);
                }
            }
        });
    }

    function renderSimilarItems(page, item, context) {

        var promise;

        var options = {
            userId: Dashboard.getCurrentUserId(),
            limit: 5,
            fields: "PrimaryImageAspectRatio,UserData,SyncInfo"
        };

        if (item.Type == "Movie") {
            promise = ApiClient.getSimilarMovies(item.Id, options);
        }
        else if (item.Type == "Trailer" ||
            (item.Type == "ChannelVideoItem" && item.ExtraType == "Trailer")) {
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

            $('.detailSectionHeader', elem).html(Globalize.translate('HeaderIfYouLikeCheckTheseOut', item.Name));

            var html = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: item.Type == "MusicAlbum" ? "detailPageSquare" : "detailPagePortrait",
                showParentTitle: item.Type == "MusicAlbum",
                centerText: item.Type != "MusicAlbum",
                showTitle: item.Type == "MusicAlbum" || item.Type == "Game",
                borderless: item.Type == "Game",
                context: context,
                overlayText: item.Type != "MusicAlbum"
            });

            $('#similarContent', page).html(html).createCardMenus();
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
            html += ' on <a class="textlink" href="itembynamedetails.html?context=' + context + '&id=' + item.Studios[0].Id + '">' + item.Studios[0].Name + '</a>';
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
            html += '<p>' + Globalize.translate('HeaderTags') + '</p>';
            for (var i = 0, length = item.Tags.length; i < length; i++) {

                html += '<div class="itemTag">' + item.Tags[i] + '</div>';

            }

            $('.itemTags', page).show().html(html);

        } else {
            $('.itemTags', page).hide();
        }
    }

    function renderKeywords(page, item) {

        if (item.Keywords && item.Keywords.length) {

            var html = '';
            html += '<p>' + Globalize.translate('HeaderPlotKeywords') + '</p>';
            for (var i = 0, length = item.Keywords.length; i < length; i++) {

                html += '<div class="itemTag">' + item.Keywords[i] + '</div>';

            }

            $('.itemKeywords', page).show().html(html);

        } else {
            $('.itemKeywords', page).hide();
        }
    }

    var _childrenItemsQuery = null;
    function renderChildren(page, item, user, context) {

        _childrenItemsQuery = null;

        var fields = "ItemCounts,AudioInfo,PrimaryImageAspectRatio,SyncInfo";

        var query = {
            ParentId: item.Id,
            Fields: fields
        };

        // Let the server pre-sort boxsets
        if (item.Type !== "BoxSet") {
            query.SortBy = "SortName";
        }

        var promise;

        if (item.Type == "Series") {

            promise = ApiClient.getSeasons(item.Id, {

                userId: user.Id,
                Fields: fields
            });
        }
        else if (item.Type == "Season") {

            // Use dedicated episodes endpoint
            promise = ApiClient.getEpisodes(item.SeriesId, {

                seasonId: item.Id,
                userId: user.Id,
                Fields: fields
            });
        }

        _childrenItemsQuery = query;
        promise = promise || ApiClient.getItems(Dashboard.getCurrentUserId(), query);

        promise.done(function (result) {

            var html = '';

            if (item.Type == "MusicAlbum") {

                html = LibraryBrowser.getListViewHtml({
                    items: result.Items,
                    smallIcon: true,
                    showIndex: true,
                    index: 'disc',
                    showIndexNumber: true,
                    playFromHere: true,
                    defaultAction: 'playallfromhere'
                });

            }
            else if (item.Type == "Series") {
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "detailPagePortrait",
                    showTitle: false,
                    centerText: true,
                    context: context,
                    overlayText: true
                });
            }
            else if (item.Type == "Season") {
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "detailPage169",
                    showTitle: true,
                    displayAsSpecial: item.Type == "Season" && item.IndexNumber,
                    context: context,
                    overlayText: true
                });
            }
            else if (item.Type == "GameSystem") {
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "auto",
                    showTitle: true,
                    centerText: true,
                    context: context
                });
            }

            $('.childrenItemsContainer', page).html(html).trigger('create').createCardMenus();

            if (item.Type == "BoxSet") {

                var collectionItemTypes = [
                    { name: Globalize.translate('HeaderMovies'), type: 'Movie' },
                    { name: Globalize.translate('HeaderSeries'), type: 'Series' },
                    { name: Globalize.translate('HeaderAlbums'), type: 'MusicAlbum' },
                    { name: Globalize.translate('HeaderGames'), type: 'Game' },
                    { name: Globalize.translate('HeaderBooks'), type: 'Book' }
                ];

                renderCollectionItems(page, collectionItemTypes, result.Items, user, context);
            }
        });

        if (item.Type == "Season") {
            $('#childrenTitle', page).html(Globalize.translate('HeaderEpisodes'));
        }
        else if (item.Type == "Series") {
            $('#childrenTitle', page).html(Globalize.translate('HeaderSeasons'));
        }
        else if (item.Type == "MusicAlbum") {
            $('#childrenTitle', page).html(Globalize.translate('HeaderTracks'));
        }
        else if (item.Type == "GameSystem") {
            $('#childrenTitle', page).html(Globalize.translate('HeaderGames'));
        }
        else {
            $('#childrenTitle', page).html(Globalize.translate('HeaderItems'));
        }
    }

    function renderCollectionItems(page, types, items, user) {

        for (var i = 0, length = types.length; i < length; i++) {

            var type = types[i];

            var typeItems = items.filter(function (curr) {

                return curr.Type == type.type;

            });

            if (typeItems.length) {
                renderCollectionItemType(page, type, typeItems, user);
            }
        }

        var otherType = { name: Globalize.translate('HeaderOtherItems') };

        var otherTypeItems = items.filter(function (curr) {

            return !types.filter(function (t) {

                return t.type == curr.Type;

            }).length;

        });

        if (otherTypeItems.length) {
            renderCollectionItemType(page, otherType, otherTypeItems, user);
        }

        if (!items.length) {
            renderCollectionItemType(page, { name: Globalize.translate('HeaderItems') }, items, user);
        }

        $('.collectionItems', page).trigger('create').createCardMenus();
    }

    function renderCollectionItemType(page, type, items, user, context) {

        var html = '';

        html += '<div class="detailSection">';

        html += '<div class="detailSectionHeader" style="position: relative;">';
        html += '<span>' + type.name + '</span>';

        if (user.Policy.IsAdministrator) {
            html += '<a class="detailSectionHeaderButton" href="editcollectionitems.html?id=' + currentItem.Id + '" data-role="button" data-icon="edit" data-iconpos="notext" data-inline="true">' + Globalize.translate('ButtonEdit') + '</a>';
        }

        html += '</div>';

        html += '<div class="detailSectionContent">';

        var shape = type.type == 'MusicAlbum' ? 'detailPageSquare' : 'detailPagePortrait';

        html += LibraryBrowser.getPosterViewHtml({
            items: items,
            shape: shape,
            showTitle: true,
            centerText: true,
            context: context
        });
        html += '</div>';

        html += '</div>';

        $('.collectionItems', page).append(html);
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

            if (result.TotalRecordCount || item.CriticRatingSummary || item.AwardSummary) {
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
                html += '<div class="reviewLink"><a class="textlink" href="' + review.Url + '" target="_blank">' + Globalize.translate('ButtonFullReview') + '</a></div>';
            }

            html += '</div>';
        }

        if (limit && result.TotalRecordCount > limit) {
            html += '<p style="margin: 0;padding-left: .5em;"><button class="moreCriticReviews" data-inline="true" data-mini="true">' + Globalize.translate('ButtonMoreItems') + '</button></p>';
        }

        $('#criticReviewsContent', page).html(html).trigger('create');
    }

    function renderThemeMedia(page, item) {

        ApiClient.getThemeMedia(Dashboard.getCurrentUserId(), item.Id, true).done(function (result) {

            var themeSongs = result.ThemeSongsResult.OwnerId == item.Id ?
                result.ThemeSongsResult.Items :
                [];

            var themeVideos = result.ThemeVideosResult.OwnerId == item.Id ?
                result.ThemeVideosResult.Items :
                [];

            renderThemeSongs(page, themeSongs);
            renderThemeVideos(page, themeVideos);

            $(page).trigger('thememediadownload', [result]);
        });

    }

    function renderThemeSongs(page, items) {

        if (items.length) {

            $('#themeSongsCollapsible', page).show();

            var html = LibraryBrowser.getListViewHtml({
                items: items,
                smallIcon: true
            });

            $('#themeSongsContent', page).html(html).trigger('create');
        } else {
            $('#themeSongsCollapsible', page).hide();
        }
    }

    function renderThemeVideos(page, items, user) {

        if (items.length) {

            $('#themeVideosCollapsible', page).show();

            $('#themeVideosContent', page).html(getVideosHtml(items, user)).trigger('create');
        } else {
            $('#themeVideosCollapsible', page).hide();
        }
    }

    function renderMusicVideos(page, item, user) {

        ApiClient.getItems(user.Id, {

            SortBy: "SortName",
            SortOrder: "Ascending",
            IncludeItemTypes: "MusicVideo",
            Recursive: true,
            Fields: "DateCreated,SyncInfo",
            Albums: item.Name

        }).done(function (result) {
            if (result.Items.length) {

                $('#musicVideosCollapsible', page).show();

                $('#musicVideosContent', page).html(getVideosHtml(result.Items, user)).trigger('create');
            } else {
                $('#musicVideosCollapsible', page).hide();
            }
        });

    }

    function renderAdditionalParts(page, item, user) {

        ApiClient.getAdditionalVideoParts(user.Id, item.Id).done(function (result) {

            if (result.Items.length) {

                $('#additionalPartsCollapsible', page).show();

                $('#additionalPartsContent', page).html(getVideosHtml(result.Items, user)).trigger('create');
            } else {
                $('#additionalPartsCollapsible', page).hide();
            }
        });
    }

    function renderScenes(page, item, user, limit) {
        var html = '';

        var chapters = item.Chapters || [];

        for (var i = 0, length = chapters.length; i < length; i++) {

            if (limit && i >= limit) {
                break;
            }

            var chapter = chapters[i];
            var chapterName = chapter.Name || "Chapter " + i;

            var onclick = item.PlayAccess == 'Full' ? ' onclick="ItemDetailPage.play(' + chapter.StartPositionTicks + ');"' : '';

            html += '<a class="card detailPage169Card" href="#play-Chapter-' + i + '"' + onclick + '>';

            html += '<div class="cardBox">';
            html += '<div class="cardScalable">';

            var imgUrl;

            if (chapter.ImageTag) {

                imgUrl = ApiClient.getScaledImageUrl(item.Id, {
                    maxWidth: 210,
                    tag: chapter.ImageTag,
                    type: "Chapter",
                    index: i
                });
            } else {
                imgUrl = "css/images/items/list/chapter.png";
            }

            html += '<div class="cardPadder"></div>';

            html += '<div class="cardContent">';
            html += '<div class="cardImage" style="background-image:url(\'' + imgUrl + '\');"></div>';

            html += '<div class="cardFooter">';
            html += '<div class="cardText">' + chapterName + '</div>';
            html += '<div class="cardText">';
            html += Dashboard.getDisplayTime(chapter.StartPositionTicks);
            html += '</div>';

            //cardFooter
            html += "</div>";

            // cardContent
            html += '</div>';

            // cardScalable
            html += '</div>';

            // cardBox
            html += '</div>';

            html += '</a>';
        }

        if (limit && chapters.length > limit) {
            html += '<p style="margin: 0;padding-left: .5em;"><button class="moreScenes" data-inline="true" data-mini="true">' + Globalize.translate('ButtonMoreItems') + '</button></p>';
        }

        $('#scenesContent', page).html(html).trigger('create');
    }

    function renderMediaSources(page, item) {

        var html = item.MediaSources.map(function (v) {

            return getMediaSourceHtml(item, v);

        }).join('<div style="border-top:1px solid #444;margin: 1em 0;"></div>');

        if (item.MediaSources.length > 1) {
            html = '<br/>' + html;
        }

        $('#mediaInfoContent', page).html(html).trigger('create');
    }

    function getMediaSourceHtml(item, version) {

        var html = '';

        if (version.Name && item.MediaSources.length > 1) {
            html += '<div><span class="mediaInfoAttribute">' + version.Name + '</span></div><br/>';
        }

        for (var i = 0, length = version.MediaStreams.length; i < length; i++) {

            var stream = version.MediaStreams[i];

            if (stream.Type == "Data") {
                continue;
            }

            html += '<div class="mediaInfoStream">';

            var displayType = Globalize.translate('MediaInfoStreamType' + stream.Type);

            html += '<div class="mediaInfoStreamType">' + displayType + '</div>';

            var attributes = [];

            if (stream.Language && stream.Type != "Video") {
                attributes.push(createAttribute(Globalize.translate('MediaInfoLanguage'), stream.Language));
            }

            if (stream.Codec) {
                attributes.push(createAttribute(Globalize.translate('MediaInfoCodec'), stream.Codec.toUpperCase()));
            }

            if (stream.Profile) {
                attributes.push(createAttribute(Globalize.translate('MediaInfoProfile'), stream.Profile));
            }

            if (stream.Level) {
                attributes.push(createAttribute(Globalize.translate('MediaInfoLevel'), stream.Level));
            }

            if (stream.Width || stream.Height) {
                attributes.push(createAttribute(Globalize.translate('MediaInfoResolution'), stream.Width + 'x' + stream.Height));
            }

            if (stream.AspectRatio && stream.Codec != "mjpeg") {
                attributes.push(createAttribute(Globalize.translate('MediaInfoAspectRatio'), stream.AspectRatio));
            }

            if (stream.Type == "Video") {
                if (stream.IsAnamorphic != null) {
                    attributes.push(createAttribute(Globalize.translate('MediaInfoAnamorphic'), (stream.IsAnamorphic ? 'Yes' : 'No')));
                }

                attributes.push(createAttribute(Globalize.translate('MediaInfoInterlaced'), (stream.IsInterlaced ? 'Yes' : 'No')));
            }

            if (stream.AverageFrameRate || stream.RealFrameRate) {
                attributes.push(createAttribute(Globalize.translate('MediaInfoFramerate'), (stream.AverageFrameRate || stream.RealFrameRate)));
            }

            if (stream.ChannelLayout) {
                attributes.push(createAttribute(Globalize.translate('MediaInfoLayout'), stream.ChannelLayout));
            }
            else if (stream.Channels) {
                attributes.push(createAttribute(Globalize.translate('MediaInfoChannels'), stream.Channels + ' ch'));
            }

            if (stream.BitRate && stream.Codec != "mjpeg") {
                attributes.push(createAttribute(Globalize.translate('MediaInfoBitrate'), (parseInt(stream.BitRate / 1024)) + ' kbps'));
            }

            if (stream.SampleRate) {
                attributes.push(createAttribute(Globalize.translate('MediaInfoSampleRate'), stream.SampleRate + ' khz'));
            }

            if (stream.BitDepth) {
                attributes.push(createAttribute(Globalize.translate('MediaInfoBitDepth'), stream.BitDepth + ' bit'));
            }

            if (stream.PixelFormat) {
                attributes.push(createAttribute(Globalize.translate('MediaInfoPixelFormat'), stream.PixelFormat));
            }

            if (stream.RefFrames) {
                attributes.push(createAttribute(Globalize.translate('MediaInfoRefFrames'), stream.RefFrames));
            }

            if (stream.Type != "Video") {
                attributes.push(createAttribute(Globalize.translate('MediaInfoDefault'), (stream.IsDefault ? 'Yes' : 'No')));
            }
            if (stream.Type == "Subtitle") {
                attributes.push(createAttribute(Globalize.translate('MediaInfoForced'), (stream.IsForced ? 'Yes' : 'No')));
                attributes.push(createAttribute(Globalize.translate('MediaInfoExternal'), (stream.IsExternal ? 'Yes' : 'No')));
            }

            if (stream.Type == "Video" && version.Timestamp) {
                attributes.push(createAttribute(Globalize.translate('MediaInfoTimestamp'), version.Timestamp));
            }

            if (stream.IsCabac != null) {
                attributes.push(createAttribute(Globalize.translate('CABAC'), (stream.IsCabac ? 'Yes' : 'No')));
            }

            html += attributes.join('<br/>');

            html += '</div>';
        }

        if (version.Container) {
            html += '<div><span class="mediaInfoLabel">' + Globalize.translate('MediaInfoContainer') + '</span><span class="mediaInfoAttribute">' + version.Container + '</span></div>';
        }

        if (version.Formats && version.Formats.length) {
            //html += '<div><span class="mediaInfoLabel">'+Globalize.translate('MediaInfoFormat')+'</span><span class="mediaInfoAttribute">' + version.Formats.join(',') + '</span></div>';
        }

        if (version.Path && version.Protocol != 'Http') {
            html += '<div style="max-width:600px;overflow:hidden;"><span class="mediaInfoLabel">' + Globalize.translate('MediaInfoPath') + '</span><span class="mediaInfoAttribute">' + version.Path + '</span></div>';
        }

        if (version.Size) {

            var size = (version.Size / (1024 * 1024)).toFixed(0);

            html += '<div><span class="mediaInfoLabel">' + Globalize.translate('MediaInfoSize') + '</span><span class="mediaInfoAttribute">' + size + ' MB</span></div>';
        }

        return html;
    }

    function createAttribute(label, value) {
        return '<span class="mediaInfoLabel">' + label + '</span><span class="mediaInfoAttribute">' + value + '</span>'
    }

    function getVideosHtml(items, user, limit, moreButtonClass) {

        var html = '';

        for (var i = 0, length = items.length; i < length; i++) {

            if (limit && i >= limit) {
                break;
            }

            var item = items[i];

            var cssClass = "card detailPage169Card";

            var href = "itemdetails.html?id=" + item.Id;

            var onclick = item.PlayAccess == 'Full' ? ' onclick="MediaController.play(\'' + item.Id + '\'); return false;"' : "";

            html += '<a class="' + cssClass + '" href="' + href + '"' + onclick + '>';

            html += '<div class="cardBox">';
            html += '<div class="cardScalable">';

            var imageTags = item.ImageTags || {};

            var imgUrl;

            if (imageTags.Primary) {

                imgUrl = ApiClient.getScaledImageUrl(item.Id, {
                    maxWidth: 210,
                    tag: imageTags.Primary,
                    type: "primary"
                });

            } else {
                imgUrl = "css/images/items/detail/video.png";
            }

            html += '<div class="cardPadder"></div>';

            html += '<div class="cardContent">';
            html += '<div class="cardImage" style="background-image:url(\'' + imgUrl + '\');"></div>';

            html += '<div class="cardFooter">';
            html += '<div class="cardText">' + item.Name + '</div>';
            html += '<div class="cardText">';
            if (item.RunTimeTicks != "") {
                html += Dashboard.getDisplayTime(item.RunTimeTicks);
            }
            else {
                html += "&nbsp;";
            }
            html += '</div>';

            //cardFooter
            html += "</div>";

            // cardContent
            html += '</div>';

            // cardScalable
            html += '</div>';

            // cardBox
            html += '</div>';

            html += '</a>';
        }

        if (limit && items.length > limit) {
            html += '<p style="margin: 0;padding-left: .5em;"><button class="' + moreButtonClass + '" data-inline="true" data-mini="true">' + Globalize.translate('ButtonMoreItems') + '</button></p>';
        }

        return html;
    }

    function renderSpecials(page, item, user, limit) {

        ApiClient.getSpecialFeatures(user.Id, item.Id).done(function (specials) {

            $('#specialsContent', page).html(getVideosHtml(specials, user, limit, "moreSpecials")).trigger('create');

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

            html += '<a class="tileItem smallPosterTileItem" href="itembynamedetails.html?context=' + context + '&id=' + cast.Id + '">';

            var imgUrl;

            if (cast.PrimaryImageTag) {

                imgUrl = ApiClient.getScaledImageUrl(cast.Id, {
                    width: 100,
                    tag: cast.PrimaryImageTag,
                    type: "primary"
                });

            } else {

                imgUrl = "css/images/items/list/person.png";
            }

            html += '<div class="tileImage" style="background-image:url(\'' + imgUrl + '\');"></div>';



            html += '<div class="tileContent">';

            html += '<p>' + cast.Name + '</p>';

            var role = cast.Role ? Globalize.translate('ValueAsRole', cast.Role) : cast.Type;

            if (role == "GuestStar") {
                role = Globalize.translate('ValueGuestStar');
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
            html += '<p style="margin: 0;padding-left: .5em;"><button class="morePeople" data-inline="true" data-mini="true">' + Globalize.translate('ButtonMoreItems') + '</button></p>';
        }

        $('#castContent', page).html(html).trigger('create');
    }

    function play(startPosition) {

        MediaController.play({
            items: [currentItem],
            startPositionTicks: startPosition
        });
    }

    function splitVersions(page) {

        var id = getParameterByName('id');

        Dashboard.confirm("Are you sure you wish to split the media sources into separate items?", "Split Media Apart", function (confirmResult) {

            if (confirmResult) {

                Dashboard.showLoadingMsg();

                ApiClient.ajax({
                    type: "DELETE",
                    url: ApiClient.getUrl("Videos/" + id + "/AlternateSources")

                }).done(function () {

                    Dashboard.hideLoadingMsg();

                    reload(page);
                });
            }
        });
    }

    function playTrailer(page) {

        ApiClient.getLocalTrailers(Dashboard.getCurrentUserId(), currentItem.Id).done(function (trailers) {

            MediaController.play({ items: trailers });

        });
    }

    $(document).on('pageinit', "#itemDetailPage", function () {

        var page = this;

        $('.btnPlay', page).on('click', function () {
            var userdata = currentItem.UserData || {};

            var mediaType = currentItem.MediaType;

            if (currentItem.Type == "MusicArtist" || currentItem.Type == "MusicAlbum") {
                mediaType = "Audio";
            }

            LibraryBrowser.showPlayMenu(this, currentItem.Id, currentItem.Type, currentItem.IsFolder, mediaType, userdata.PlaybackPositionTicks);
        });

        $('.btnPlayTrailer', page).on('click', function () {
            playTrailer(page);
        });

        $('.btnPlayExternal', page).on('click', function () {

            ApiClient.markPlayed(Dashboard.getCurrentUserId(), currentItem.Id, new Date());
        });

        $('.btnSplitVersions', page).on('click', function () {

            splitVersions(page);
        });

        $('.btnSync', page).on('click', function () {

            SyncManager.showMenu({
                items: [currentItem]
            });
        });

        $('.btnMoreCommands', page).on('click', function () {

            var button = this;

            Dashboard.getCurrentUser().done(function (user) {

                LibraryBrowser.showMoreCommands(button, currentItem.Id, LibraryBrowser.getMoreCommands(currentItem, user));
            });
        });

        $('.childrenItemsContainer', page).on('playallfromhere', function (e, index) {

            LibraryBrowser.playAllFromHere(_childrenItemsQuery, index);

        }).on('queueallfromhere', function (e, index) {

            LibraryBrowser.queueAllFromHere(_childrenItemsQuery, index);

        });

    }).on('pageshow', "#itemDetailPage", function () {

        var page = this;

        $(page).on("click.moreScenes", ".moreScenes", function () {

            Dashboard.getCurrentUser().done(function (user) {
                renderScenes(page, currentItem, user);
            });

        }).on("click.morePeople", ".morePeople", function () {

            renderCast(page, currentItem, getContext(currentItem));

        }).on("click.moreSpecials", ".moreSpecials", function () {

            Dashboard.getCurrentUser().done(function (user) {
                renderSpecials(page, currentItem, user);
            });

        }).on("click.moreCriticReviews", ".moreCriticReviews", function () {

            renderCriticReviews(page, currentItem);

        });

        reload(page);

        $(ApiClient).on('websocketmessage', onWebSocketMessage);

        $(LibraryBrowser).on('itemdeleting.detailpage', function (e, itemId) {

            if (currentItem && currentItem.Id == itemId) {
                Dashboard.navigate('index.html');
            }
        });

    }).on('pagehide', "#itemDetailPage", function () {

        $(LibraryBrowser).off('itemdeleting.detailpage');

        currentItem = null;

        var page = this;

        $(page).off("click.moreScenes").off("click.morePeople").off("click.moreSpecials").off("click.moreCriticReviews");

        $(ApiClient).off('websocketmessage', onWebSocketMessage);
    });

    function itemDetailPage() {

        var self = this;

        self.play = play;
    }

    window.ItemDetailPage = new itemDetailPage();


})(jQuery, document, LibraryBrowser, window);