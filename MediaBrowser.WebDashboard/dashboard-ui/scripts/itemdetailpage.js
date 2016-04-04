define(['layoutManager', 'jQuery'], function (layoutManager, $) {

    var currentItem;

    function getPromise() {

        var id = getParameterByName('id');

        if (id) {
            return ApiClient.getItem(Dashboard.getCurrentUserId(), id);
        }

        var name = getParameterByName('genre');

        if (name) {
            return ApiClient.getGenre(name, Dashboard.getCurrentUserId());
        }

        name = getParameterByName('musicgenre');

        if (name) {
            return ApiClient.getMusicGenre(name, Dashboard.getCurrentUserId());
        }

        name = getParameterByName('gamegenre');

        if (name) {
            return ApiClient.getGameGenre(name, Dashboard.getCurrentUserId());
        }

        name = getParameterByName('musicartist');

        if (name) {
            return ApiClient.getArtist(name, Dashboard.getCurrentUserId());
        }
        else {
            throw new Error('Invalid request');
        }
    }

    function reload(page) {

        Dashboard.showLoadingMsg();

        getPromise().then(function (item) {

            reloadFromItem(page, item);
            window.scrollTo(0, 0);
        });
    }

    function reloadFromItem(page, item) {

        currentItem = item;

        var context = getContext(item);

        renderHeader(page, item, context);

        LibraryBrowser.renderName(item, $('.itemName', page), false, context);
        LibraryBrowser.renderParentName(item, $('.parentName', page), context);
        LibraryMenu.setTitle(item.SeriesName || item.Name);

        Dashboard.getCurrentUser().then(function (user) {

            renderImage(page, item, user);

            setInitialCollapsibleState(page, item, context, user);
            renderDetails(page, item, context);

            var hasBackdrop = false;

            // For these types, make the backdrop a little smaller so that the items are more quickly accessible
            if (item.Type == 'MusicArtist' || item.Type == "MusicAlbum" || item.Type == "Playlist" || item.Type == "BoxSet" || item.MediaType == "Audio" || !layoutManager.mobile) {
                $('#itemBackdrop', page).addClass('noBackdrop').css('background-image', 'none');
                require(['backdrop'], function (backdrop) {
                    backdrop.setBackdrops([item]);
                });
            }
            else {
                //$('#itemBackdrop', page).addClass('noBackdrop').css('background-image', 'none');
                //Backdrops.setBackdrops(page, [item]);
                hasBackdrop = LibraryBrowser.renderDetailPageBackdrop(page, item);
            }

            var transparentHeader = hasBackdrop && page.classList.contains('noSecondaryNavPage');

            LibraryMenu.setTransparentMenu(transparentHeader);

            var canPlay = false;

            if (item.Type == 'Program') {

                var now = new Date();

                if (now >= parseISO8601Date(item.StartDate, { toLocal: true }) && now < parseISO8601Date(item.EndDate, { toLocal: true })) {
                    $('.btnPlay', page).removeClass('hide');
                    canPlay = true;
                } else {
                    $('.btnPlay', page).addClass('hide');
                }
            }
            else if (MediaController.canPlay(item)) {
                $('.btnPlay', page).removeClass('hide');
                canPlay = true;
            }
            else {
                $('.btnPlay', page).addClass('hide');
            }

            if (item.LocalTrailerCount && item.PlayAccess == 'Full') {
                $('.btnPlayTrailer', page).removeClass('hide');
            } else {
                $('.btnPlayTrailer', page).addClass('hide');
            }

            if (LibraryBrowser.enableSync(item, user)) {
                $('.btnSync', page).removeClass('hide');
            } else {
                $('.btnSync', page).addClass('hide');
            }

            if (item.Type == 'Program' && item.TimerId) {
                $('.btnCancelRecording', page).removeClass('hide');
            } else {
                $('.btnCancelRecording', page).addClass('hide');
            }

            if (item.Type == 'Program' && (!item.TimerId && !item.SeriesTimerId)) {

                if (canPlay) {
                    $('.btnRecord', page).removeClass('hide');
                    $('.btnFloatingRecord', page).addClass('hide');
                } else {
                    $('.btnRecord', page).addClass('hide');
                    $('.btnFloatingRecord', page).removeClass('hide');
                }
            } else {
                $('.btnRecord', page).addClass('hide');
                $('.btnFloatingRecord', page).addClass('hide');
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

            if (LibraryBrowser.getMoreCommands(item, user).length > 0) {
                $('.btnMoreCommands', page).removeClass('hide');
            } else {
                $('.btnMoreCommands', page).addClass('hide');
            }

            if (user.Policy.IsAdministrator) {
                $('.chapterSettingsButton', page).show();
            } else {
                $('.chapterSettingsButton', page).hide();
            }

            LiveTvHelpers.renderOriginalAirDate($('.airDate', page), item);

            if (item.Type == "Person" && item.PremiereDate) {

                try {
                    var birthday = parseISO8601Date(item.PremiereDate, { toLocal: true }).toDateString();

                    $('#itemBirthday', page).show().html(Globalize.translate('BirthDateValue').replace('{0}', birthday));
                }
                catch (err) {
                    $('#itemBirthday', page).hide();
                }
            } else {
                $('#itemBirthday', page).hide();
            }

            if (item.Type == "Person" && item.EndDate) {

                try {
                    var deathday = parseISO8601Date(item.EndDate, { toLocal: true }).toDateString();

                    $('#itemDeathDate', page).show().html(Globalize.translate('DeathDateValue').replace('{0}', deathday));
                }
                catch (err) {
                    $('#itemBirthday', page).hide();
                }
            } else {
            }

            if (item.Type == "Person" && item.ProductionLocations && item.ProductionLocations.length) {

                var gmap = '<a class="textlink" target="_blank" href="https://maps.google.com/maps?q=' + item.ProductionLocations[0] + '">' + item.ProductionLocations[0] + '</a>';

                $('#itemBirthLocation', page).show().html(Globalize.translate('BirthPlaceValue').replace('{0}', gmap));
            } else {
                $('#itemBirthLocation', page).hide();
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

        page.dispatchEvent(new CustomEvent("displayingitem", {
            detail: {
                item: item,
                context: context
            },
            bubbles: true
        }));

        Dashboard.hideLoadingMsg();
    }

    function renderImage(page, item, user) {

        LibraryBrowser.renderDetailImage(page.querySelector('.detailImageContainer'), item, user.Policy.IsAdministrator && item.MediaType != 'Photo');
    }

    function refreshImage(page, item, user) {

        LibraryBrowser.refreshDetailImageUserData(page.querySelector('.detailImageContainer'), item);
    }

    function onWebSocketMessage(e, data) {

        var msg = data;
        var page = $($.mobile.activePage)[0];

        if (msg.MessageType === "UserDataChanged") {

            if (currentItem && msg.Data.UserId == Dashboard.getCurrentUserId()) {

                var key = currentItem.UserData.Key;

                var userData = msg.Data.UserDataList.filter(function (u) {

                    return u.Key == key;
                })[0];

                if (userData) {

                    currentItem.UserData = userData;

                    Dashboard.getCurrentUser().then(function (user) {

                        refreshImage(page, currentItem, user);
                    });
                }
            }
        }

    }

    function setPeopleHeader(page, item) {

        if (item.MediaType == "Audio" || item.Type == "MusicAlbum" || item.MediaType == "Book" || item.MediaType == "Photo") {
            $('#peopleHeader', page).html(Globalize.translate('HeaderPeople'));
        } else {
            $('#peopleHeader', page).html(Globalize.translate('HeaderCastAndCrew'));
        }

    }

    function getContext(item) {

        return getParameterByName('context');
    }

    function renderHeader(page, item, context) {

        $('.itemTabs', page).hide();

        if (context == 'tv') {

            $(page).removeClass('noSecondaryNavPage');

            $('#tvShowsTabs', page).show();
            LibraryMenu.setMenuButtonVisible(true);
        }
        else {
            $(page).addClass('noSecondaryNavPage');
            LibraryMenu.setBackButtonVisible(true);
            LibraryMenu.setMenuButtonVisible(false);
        }
    }

    function renderNextUp(page, item, user) {

        var section = page.querySelector('.nextUpSection');

        var userData = item.UserData || {};

        if (item.Type != 'Series' || !userData.PlayedPercentage) {
            section.classList.add('hide');
            return;
        }

        ApiClient.getNextUpEpisodes({

            SeriesId: item.Id,
            UserId: user.Id

        }).then(function (result) {

            if (result.Items.length) {
                section.classList.remove('hide');
            } else {
                section.classList.add('hide');
            }

            var html = LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "detailPage169",
                showTitle: true,
                displayAsSpecial: item.Type == "Season" && item.IndexNumber,
                overlayText: true,
                lazy: true,
                overlayPlayButton: true
            });

            var itemsContainer = section.querySelector('.nextUpItems');
            
            itemsContainer.innerHTML = html;
            ImageLoader.lazyChildren(itemsContainer);
            $(itemsContainer).createCardMenus();
        });
    }

    function setInitialCollapsibleState(page, item, context, user) {

        $('.collectionItems', page).empty();

        if (item.Type == 'TvChannel') {

            $('#childrenCollapsible', page).removeClass('hide');
            renderChannelGuide(page, item, user);
        }
        else if (item.Type == 'Playlist') {

            $('#childrenCollapsible', page).removeClass('hide');
            renderPlaylistItems(page, item, user);
        }
        else if (item.Type == 'Studio' || item.Type == 'Person' || item.Type == 'Genre' || item.Type == 'MusicGenre' || item.Type == 'GameGenre' || item.Type == 'MusicArtist') {

            $('#childrenCollapsible', page).removeClass('hide');
            renderItemsByName(page, item, user);
        }
        else if (item.IsFolder) {

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

        if (item.Type == 'Series') {

            renderNextUp(page, item, user);
        } else {
            page.querySelector('.nextUpSection').classList.add('hide');
        }

        if (item.MediaSources && item.MediaSources.length) {
            renderMediaSources(page, item);
        }

        var chapters = item.Chapters || [];

        if (!chapters.length || !AppInfo.enableDetailPageChapters) {
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
            renderCast(page, item, context, enableScrollX() ? null : 6);
        }

        if (item.PartCount && item.PartCount > 1) {
            $('#additionalPartsCollapsible', page).removeClass('hide');
            renderAdditionalParts(page, item, user);
        } else {
            $('#additionalPartsCollapsible', page).addClass('hide');
        }

        $('#themeSongsCollapsible', page).hide();
        $('#themeVideosCollapsible', page).hide();

        if (item.Type == "MusicAlbum") {
            renderMusicVideos(page, item, user);
        } else {
            $('#musicVideosCollapsible', page).hide();
        }

        renderThemeMedia(page, item, user);

        if (enableScrollX()) {
            renderCriticReviews(page, item);
        } else {
            renderCriticReviews(page, item, 1);
        }
    }

    function renderDetails(page, item, context, isStatic) {

        renderSimilarItems(page, item, context);

        if (!isStatic) {
            renderSiblingLinks(page, item, context);
        }

        if (item.Taglines && item.Taglines.length) {
            $('.tagline', page).html(item.Taglines[0]).show();
        } else {
            $('.tagline', page).hide();
        }

        var topOverview = page.querySelector('.topOverview');
        var bottomOverview = page.querySelector('.bottomOverview');

        var seasonOnBottom = screen.availHeight < 800 || screen.availWidth < 600;

        if (item.Type == 'MusicAlbum' || item.Type == 'MusicArtist' || (item.Type == 'Season' && seasonOnBottom)) {
            LibraryBrowser.renderOverview([bottomOverview], item);
            topOverview.classList.add('hide');
            bottomOverview.classList.remove('hide');
        } else {
            LibraryBrowser.renderOverview([topOverview], item);
            topOverview.classList.remove('hide');
            bottomOverview.classList.add('hide');
        }

        $('.itemCommunityRating', page).html(LibraryBrowser.getRatingHtml(item));

        LibraryBrowser.renderAwardSummary($('#awardSummary', page), item);

        $('.itemMiscInfo', page).html(LibraryBrowser.getMiscInfoHtml(item));

        LibraryBrowser.renderGenres($('.itemGenres', page), item, null, isStatic);
        LibraryBrowser.renderStudios($('.itemStudios', page), item, isStatic);
        renderUserDataIcons(page, item);
        LibraryBrowser.renderLinks(page.querySelector('.itemExternalLinks'), item);

        $('.criticRatingScore', page).html((item.CriticRating || '0') + '%');

        if (item.CriticRatingSummary) {
            $('#criticRatingSummary', page).show();
            $('.criticRatingSummaryText', page).html(item.CriticRatingSummary);

        } else {
            $('#criticRatingSummary', page).hide();
        }

        renderTags(page, item);

        renderSeriesAirTime(page, item, isStatic);

        if (item.Players) {
            $('#players', page).show().html(item.Players + ' Player');
        } else {
            $('#players', page).hide();
        }

        if (item.ArtistItems && item.ArtistItems.length && item.Type != "MusicAlbum") {
            $('.artist', page).show().html(getArtistLinksHtml(item.ArtistItems, context));
        } else {
            $('.artist', page).hide();
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
            //attributes.push(createAttribute(Globalize.translate('MediaInfoOrientation'), item.ImageOrientation));
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

        $('.photoInfoContent', page).html(html);
    }

    function renderTabButtons(page, item) {

        var elem = $('.tabDetails', page)[0];
        var text = elem.textContent || elem.innerText || '';

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

            html.push('<a class="textlink" href="itemdetails.html?id=' + artist.Id + '">' + artist.Name + '</a>');

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

        promise.then(function (result) {

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

    function enableScrollX() {
        return browserInfo.mobile && AppInfo.enableAppLayouts && screen.availWidth <= 1000;
    }

    function getPortraitShape() {
        return enableScrollX() ? 'overflowPortrait' : 'detailPagePortrait';
    }

    function getSquareShape() {
        return enableScrollX() ? 'overflowSquare' : 'detailPageSquare';
    }

    function getThumbShape() {
        return enableScrollX() ? 'overflowBackdrop' : 'detailPage169';
    }

    function renderSimilarItems(page, item, context) {

        if (item.Type == "Movie" || item.Type == "Trailer" || item.Type == "Series" || item.Type == "Program" || item.Type == "Recording" || item.Type == "Game" || item.Type == "MusicAlbum" || item.Type == "MusicArtist" || item.Type == "ChannelVideoItem") {
            $('#similarCollapsible', page).show();
        }
        else {
            $('#similarCollapsible', page).hide();
            return;
        }

        var shape = item.Type == "MusicAlbum" || item.Type == "MusicArtist" ? getSquareShape() : getPortraitShape();
        var screenWidth = $(window).width();
        var screenHeight = $(window).height();

        var options = {
            userId: Dashboard.getCurrentUserId(),
            limit: screenWidth > 800 && shape == "detailPagePortrait" ? 4 : 4,
            fields: "PrimaryImageAspectRatio,UserData,SyncInfo"
        };

        if (screenWidth >= 800 && screenHeight >= 1000) {
            options.limit *= 2;
        }

        if (enableScrollX()) {
            options.limit = 12;
        }

        ApiClient.getSimilarItems(item.Id, options).then(function (result) {

            if (!result.Items.length) {

                $('#similarCollapsible', page).hide();
                return;
            }

            var elem = $('#similarCollapsible', page).show();

            $('.similiarHeader', elem).html(Globalize.translate('HeaderIfYouLikeCheckTheseOut', item.Name));

            var html = '';

            if (enableScrollX()) {
                html += '<div class="hiddenScrollX itemsContainer">';
            } else {
                html += '<div class="itemsContainer">';
            }
            html += LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: shape,
                showParentTitle: item.Type == "MusicAlbum",
                centerText: true,
                showTitle: item.Type == "MusicAlbum" || item.Type == "Game" || item.Type == "MusicArtist",
                borderless: item.Type == "Game",
                context: context,
                lazy: true,
                showDetailsMenu: true,
                coverImage: item.Type == "MusicAlbum" || item.Type == "MusicArtist",
                overlayPlayButton: true
            });
            html += '</div>';

            $('#similarContent', page).html(html).lazyChildren().createCardMenus();
        });
    }

    $.fn.lazyChildren = function () {

        for (var i = 0, length = this.length; i < length; i++) {
            ImageLoader.lazyChildren(this[i]);
        }
        return this;
    };

    function renderSeriesAirTime(page, item, isStatic) {

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

            if (isStatic) {
                html += ' on ' + item.Studios[0].Name;
            } else {
                html += ' on <a class="textlink" href="itemdetails.html?id=' + item.Studios[0].Id + '">' + item.Studios[0].Name + '</a>';
            }
        }

        if (html) {
            html = (item.Status == 'Ended' ? 'Aired ' : 'Airs ') + html;

            $('#seriesAirTime', page).show().html(html);
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

    function getEpisodesFunction(seriesId, query) {

        query = $.extend({}, query);

        return function (index, limit, fields) {

            query.StartIndex = index;
            query.Limit = limit;
            query.Fields = fields;

            return ApiClient.getEpisodes(seriesId, query);

        };

    }

    function getAlbumSongsFunction(query) {

        query = $.extend({}, query);

        return function (index, limit, fields) {

            query.StartIndex = index;
            query.Limit = limit;
            query.Fields = fields;

            return ApiClient.getItems(Dashboard.getCurrentUserId(), query);

        };

    }

    var _childrenItemsFunction = null;
    function renderChildren(page, item, user, context) {

        _childrenItemsFunction = null;

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

            _childrenItemsFunction = getEpisodesFunction(item.SeriesId, {

                seasonId: item.Id,
                userId: user.Id
            });
        }
        else if (item.Type == "MusicAlbum") {

            _childrenItemsFunction = getAlbumSongsFunction(query);
        }

        promise = promise || ApiClient.getItems(Dashboard.getCurrentUserId(), query);

        promise.then(function (result) {

            var html = '';

            var scrollX = false;

            if (item.Type == "MusicAlbum") {

                html = LibraryBrowser.getListViewHtml({
                    items: result.Items,
                    smallIcon: true,
                    showIndex: true,
                    index: 'disc',
                    showIndexNumber: true,
                    playFromHere: true,
                    defaultAction: 'playallfromhere',
                    lazy: true
                });

            }
            else if (item.Type == "Series") {

                scrollX = enableScrollX();

                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: getPortraitShape(),
                    showTitle: true,
                    centerText: true,
                    lazy: true,
                    overlayPlayButton: true
                });
            }
            else if (item.Type == "Season") {
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "detailPage169",
                    showTitle: true,
                    displayAsSpecial: item.Type == "Season" && item.IndexNumber,
                    playFromHere: true,
                    overlayText: true,
                    lazy: true,
                    showDetailsMenu: true,
                    overlayPlayButton: AppInfo.enableAppLayouts
                });
            }
            else if (item.Type == "GameSystem") {
                html = LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    shape: "auto",
                    showTitle: true,
                    centerText: true,
                    lazy: true,
                    showDetailsMenu: true
                });
            }

            var elem = page.querySelector('.childrenItemsContainer');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);

            if (scrollX) {
                elem.classList.add('hiddenScrollX');
            } else {
                elem.classList.remove('hiddenScrollX');
            }

            $(elem).createCardMenus();

            if (item.Type == "BoxSet") {

                var collectionItemTypes = [
                    { name: Globalize.translate('HeaderMovies'), type: 'Movie' },
                    { name: Globalize.translate('HeaderSeries'), type: 'Series' },
                    { name: Globalize.translate('HeaderAlbums'), type: 'MusicAlbum' },
                    { name: Globalize.translate('HeaderGames'), type: 'Game' },
                    { name: Globalize.translate('HeaderBooks'), type: 'Book' }
                ];

                renderCollectionItems(page, item, collectionItemTypes, result.Items, user, context);
            }
        });

        if (item.Type == "Season") {
            page.querySelector('#childrenTitle').innerHTML = Globalize.translate('HeaderEpisodes');
        }
        else if (item.Type == "Series") {
            page.querySelector('#childrenTitle').innerHTML = Globalize.translate('HeaderSeasons');
        }
        else if (item.Type == "MusicAlbum") {
            page.querySelector('#childrenTitle').innerHTML = Globalize.translate('HeaderTracks');
        }
        else if (item.Type == "GameSystem") {
            page.querySelector('#childrenTitle').innerHTML = Globalize.translate('HeaderGames');
        }
        else {
            page.querySelector('#childrenTitle').innerHTML = Globalize.translate('HeaderItems');
        }

        if (item.Type == "MusicAlbum") {
            $('.childrenSectionHeader', page).hide();
        } else {
            $('.childrenSectionHeader', page).show();
        }
    }

    function renderItemsByName(page, item, user) {

        require('scripts/itembynamedetailpage'.split(','), function () {


            window.ItemsByName.renderItems(page, item);
        });
    }

    function renderPlaylistItems(page, item, user) {

        require('scripts/playlistedit'.split(','), function () {


            PlaylistViewer.render(page, item);
        });
    }

    function renderChannelGuide(page, item, user) {

        require('scripts/livetvcomponents,scripts/livetvchannel,livetvcss'.split(','), function () {


            LiveTvChannelPage.renderPrograms(page, item.Id);
        });
    }

    function renderCollectionItems(page, parentItem, types, items, user) {

        // First empty out existing content
        page.querySelector('.collectionItems').innerHTML = '';

        for (var i = 0, length = types.length; i < length; i++) {

            var type = types[i];

            var typeItems = items.filter(function (curr) {

                return curr.Type == type.type;

            });

            if (typeItems.length) {
                renderCollectionItemType(page, parentItem, type, typeItems, user);
            }
        }

        var otherType = { name: Globalize.translate('HeaderOtherItems') };

        var otherTypeItems = items.filter(function (curr) {

            return !types.filter(function (t) {

                return t.type == curr.Type;

            }).length;

        });

        if (otherTypeItems.length) {
            renderCollectionItemType(page, parentItem, otherType, otherTypeItems, user);
        }

        if (!items.length) {
            renderCollectionItemType(page, parentItem, { name: Globalize.translate('HeaderItems') }, items, user);
        }

        $('.collectionItems .itemsContainer', page).createCardMenus();
    }

    function renderCollectionItemType(page, parentItem, type, items, user, context) {

        var html = '';

        html += '<div class="detailSection">';

        html += '<h1>';
        html += '<span>' + type.name + '</span>';

        html += '</h1>';

        html += '<div class="detailSectionContent itemsContainer">';

        var shape = type.type == 'MusicAlbum' ? 'detailPageSquare' : 'detailPagePortrait';

        html += LibraryBrowser.getPosterViewHtml({
            items: items,
            shape: shape,
            showTitle: true,
            centerText: true,
            context: context,
            lazy: true,
            showDetailsMenu: true,
            overlayMoreButton: true,
            showAddToCollection: false,
            showRemoveFromCollection: true
        });
        html += '</div>';

        html += '</div>';

        var collectionItems = page.querySelector('.collectionItems');
        $(collectionItems).append(html);
        ImageLoader.lazyChildren(collectionItems);

        $(collectionItems).off('removefromcollection').on('removefromcollection', function (e, itemId) {

            removeFromCollection(page, parentItem, [itemId], user, context);
        });
    }

    function removeFromCollection(page, parentItem, itemIds, user, context) {

        Dashboard.showLoadingMsg();

        var url = ApiClient.getUrl("Collections/" + parentItem.Id + "/Items", {

            Ids: itemIds.join(',')
        });

        ApiClient.ajax({
            type: "DELETE",
            url: url

        }).then(function () {

            renderChildren(page, parentItem, user, context);
            Dashboard.hideLoadingMsg();
        });
    }

    function renderUserDataIcons(page, item) {

        $('.userDataIcons', page).html(LibraryBrowser.getUserDataIconsHtml(item, true, 'icon-button'));
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

        ApiClient.getCriticReviews(item.Id, options).then(function (result) {

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

            html += '<div class="paperList criticReviewPaperList">';
            html += '<paper-icon-item style="padding-top:.5em;padding-bottom:.5em;">';

            if (review.Score != null) {
                //html += review.Score;
            }
            else if (review.Likes != null) {

                if (review.Likes) {
                    html += '<paper-fab mini style="background-color:transparent;background-image:url(\'css/images/fresh.png\');background-repeat:no-repeat;background-position:center center;background-size: cover;" item-icon></paper-fab>';
                } else {
                    html += '<paper-fab mini style="background-color:transparent;background-image:url(\'css/images/rotten.png\');background-repeat:no-repeat;background-position:center center;background-size: cover;" item-icon></paper-fab>';
                }
            }

            html += '<paper-item-body three-line>';

            html += '<div style="white-space:normal;">' + review.Caption + '</div>';

            var vals = [];

            if (review.ReviewerName) {
                vals.push(review.ReviewerName);
            }
            if (review.Publisher) {
                vals.push(review.Publisher);
            }

            html += '<div secondary>' + vals.join(', ') + '.';
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
                html += '<div secondary><a class="textlink" href="' + review.Url + '" target="_blank">' + Globalize.translate('ButtonFullReview') + '</a></div>';
            }

            html += '</paper-item-body>';

            html += '</paper-icon-item>';
            html += '</div>';
        }

        if (limit && result.TotalRecordCount > limit) {
            html += '<p style="margin: 0;"><paper-button raised class="more moreCriticReviews">' + Globalize.translate('ButtonMore') + '</paper-button></p>';
        }

        var criticReviewsContent = page.querySelector('#criticReviewsContent');
        criticReviewsContent.innerHTML = html;

        if (enableScrollX()) {
            criticReviewsContent.classList.add('hiddenScrollX');
        } else {
            criticReviewsContent.classList.remove('hiddenScrollX');
        }
    }

    function renderThemeMedia(page, item) {

        ApiClient.getThemeMedia(Dashboard.getCurrentUserId(), item.Id, true).then(function (result) {

            var themeSongs = result.ThemeSongsResult.OwnerId == item.Id ?
                result.ThemeSongsResult.Items :
                [];

            var themeVideos = result.ThemeVideosResult.OwnerId == item.Id ?
                result.ThemeVideosResult.Items :
                [];

            renderThemeSongs(page, themeSongs);
            renderThemeVideos(page, themeVideos);

            page.dispatchEvent(new CustomEvent("thememediadownload", {
                detail: {
                    themeMediaResult: result
                },
                bubbles: true
            }));

        });

    }

    function renderThemeSongs(page, items) {

        if (items.length) {

            $('#themeSongsCollapsible', page).show();

            var html = LibraryBrowser.getListViewHtml({
                items: items,
                smallIcon: true
            });

            page.querySelector('#themeSongsContent').innerHTML = html;
        } else {
            $('#themeSongsCollapsible', page).hide();
        }
    }

    function renderThemeVideos(page, items, user) {

        if (items.length) {

            $('#themeVideosCollapsible', page).show();

            $('#themeVideosContent', page).html(getVideosHtml(items, user)).lazyChildren();
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

        }).then(function (result) {
            if (result.Items.length) {

                $('#musicVideosCollapsible', page).show();

                $('#musicVideosContent', page).html(getVideosHtml(result.Items, user)).lazyChildren();
            } else {
                $('#musicVideosCollapsible', page).hide();
            }
        });

    }

    function renderAdditionalParts(page, item, user) {

        ApiClient.getAdditionalVideoParts(user.Id, item.Id).then(function (result) {

            if (result.Items.length) {

                $('#additionalPartsCollapsible', page).show();

                $('#additionalPartsContent', page).html(getVideosHtml(result.Items, user)).lazyChildren();
            } else {
                $('#additionalPartsCollapsible', page).hide();
            }
        });
    }

    function renderScenes(page, item, user, limit, isStatic) {
        var html = '';

        var chapters = item.Chapters || [];

        var maxWidth = LibraryBrowser.getPosterViewInfo().backdropWidth;

        if (enableScrollX()) {
            html += '<div class="hiddenScrollX itemsContainer">';
            limit = null;
        } else {
            html += '<div class="itemsContainer">';
        }

        for (var i = 0, length = chapters.length; i < length; i++) {

            if (limit && i >= limit) {
                break;
            }

            var chapter = chapters[i];
            var chapterName = chapter.Name || "Chapter " + i;

            var onclick = item.PlayAccess == 'Full' && !isStatic ? ' onclick="ItemDetailPage.play(' + chapter.StartPositionTicks + ');"' : '';

            html += '<a class="card ' + getThumbShape() + 'Card" href="#"' + onclick + '>';

            html += '<div class="cardBox">';
            html += '<div class="cardScalable">';

            var imgUrl;

            if (chapter.ImageTag) {

                imgUrl = ApiClient.getScaledImageUrl(item.Id, {
                    maxWidth: maxWidth,
                    tag: chapter.ImageTag,
                    type: "Chapter",
                    index: i
                });
            } else {
                imgUrl = "css/images/items/list/chapter.png";
            }

            html += '<div class="cardPadder"></div>';

            html += '<div class="cardContent">';
            html += '<div class="cardImage lazy" data-src="' + imgUrl + '"></div>';

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

        html += '</div>';

        if (limit && chapters.length > limit) {
            html += '<p style="margin: 0;"><paper-button raised class="more moreScenes">' + Globalize.translate('ButtonMore') + '</paper-button></p>';
        }

        var scenesContent = page.querySelector('#scenesContent');
        scenesContent.innerHTML = html;
        ImageLoader.lazyChildren(scenesContent);
    }

    function renderMediaSources(page, item) {

        var html = item.MediaSources.map(function (v) {

            return getMediaSourceHtml(item, v);

        }).join('<div style="border-top:1px solid #444;margin: 1em 0;"></div>');

        if (item.MediaSources.length > 1) {
            html = '<br/>' + html;
        }

        var mediaInfoContent = page.querySelector('#mediaInfoContent');
        mediaInfoContent.innerHTML = html;
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

            if (stream.CodecTag) {
                attributes.push(createAttribute(Globalize.translate('MediaInfoCodecTag'), stream.CodecTag));
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
            if (stream.Channels) {
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

            if (stream.KeyFrames) {
                //attributes.push(createAttribute(Globalize.translate('MediaInfoKeyFrames'), stream.KeyFrames.join(', ')));
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

        var maxWidth = LibraryBrowser.getPosterViewInfo().backdropWidth;

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
                    maxWidth: maxWidth,
                    tag: imageTags.Primary,
                    type: "primary"
                });

            } else {
                imgUrl = "css/images/items/detail/video.png";
            }

            html += '<div class="cardPadder"></div>';

            html += '<div class="cardContent">';
            html += '<div class="cardImage lazy" data-src="' + imgUrl + '"></div>';

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
            html += '<p style="margin: 0;padding-left:5px;"><paper-button raised class="more ' + moreButtonClass + '">' + Globalize.translate('ButtonMore') + '</paper-button></p>';
        }

        return html;
    }

    function renderSpecials(page, item, user, limit) {

        ApiClient.getSpecialFeatures(user.Id, item.Id).then(function (specials) {

            var specialsContent = page.querySelector('#specialsContent');
            specialsContent.innerHTML = getVideosHtml(specials, user, limit, "moreSpecials");
            ImageLoader.lazyChildren(specialsContent);

        });
    }

    function renderCast(page, item, context, limit, isStatic) {

        if (enableScrollX()) {
            renderHorizontalCast(page, item, context, isStatic);
            return;
        }

        var html = '';

        var casts = item.People || [];

        for (var i = 0, length = casts.length; i < length; i++) {

            if (limit && i >= limit) {
                break;
            }

            var cast = casts[i];
            var href = isStatic ? '#' : 'itemdetails.html?id=' + cast.Id + '';
            html += '<a class="tileItem smallPosterTileItem" href="' + href + '">';

            var imgUrl;
            var lazy = true;

            if (cast.PrimaryImageTag) {

                imgUrl = ApiClient.getScaledImageUrl(cast.Id, {
                    maxWidth: 100,
                    tag: cast.PrimaryImageTag,
                    type: "primary",
                    minScale: 2
                });

            } else {

                imgUrl = "css/images/items/list/person.png";
                lazy = false;
            }

            if (lazy) {
                html += '<div class="tileImage lazy" data-src="' + imgUrl + '"></div>';
            } else {
                html += '<div class="tileImage" style="background-image:url(\'' + imgUrl + '\');"></div>';
            }

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
            html += '<p style="margin: 0;padding-left:5px;"><paper-button raised class="more morePeople">' + Globalize.translate('ButtonMore') + '</paper-button></p>';
        }

        var castContent = page.querySelector('#castContent');
        castContent.innerHTML = html;
        ImageLoader.lazyChildren(castContent);
    }

    function renderHorizontalCast(page, item, context, isStatic) {

        var html = '';

        if (enableScrollX()) {
            html += '<div class="hiddenScrollX itemsContainer">';
        } else {
            html += '<div class="itemsContainer">';
        }

        var casts = item.People || [];

        casts = casts.filter(function (c) {

            return c.PrimaryImageTag;
        });

        if (!casts.length) {
            casts = item.People || [];
        }

        for (var i = 0, length = casts.length; i < length; i++) {

            var cast = casts[i];
            var href = isStatic ? '#' : 'itemdetails.html?id=' + cast.Id + '';

            html += '<div class="card ' + getPortraitShape() + 'Card">';

            html += '<div class="cardBox">';
            html += '<div class="cardScalable">';

            var imgUrl;
            var lazy = true;

            if (cast.PrimaryImageTag) {

                imgUrl = ApiClient.getScaledImageUrl(cast.Id, {
                    maxWidth: 100,
                    tag: cast.PrimaryImageTag,
                    type: "primary",
                    minScale: 2
                });

            } else {

                imgUrl = "css/images/items/list/person.png";
                lazy = false;
            }

            html += '<div class="cardPadder"></div>';

            html += '<a class="cardContent" href="' + href + '">';
            if (lazy) {
                html += '<div class="cardImage coveredCardImage lazy" data-src="' + imgUrl + '"></div>';
            } else {
                html += '<div class="cardImage coveredCardImage" style="background-image:url(\'' + imgUrl + '\');"></div>';
            }

            //cardFooter
            html += "</div>";

            // cardContent
            html += '</a>';

            // cardScalable
            html += '</div>';

            html += '<div class="cardFooter outerCardFooter">';
            html += '<div class="cardText">' + cast.Name + '</div>';
            html += '<div class="cardText">';

            var role = cast.Role ? Globalize.translate('ValueAsRole', cast.Role) : cast.Type;

            if (role == "GuestStar") {
                role = Globalize.translate('ValueGuestStar');
            }

            role = role || "";

            var maxlength = 40;

            if (role.length > maxlength) {
                role = role.substring(0, maxlength - 3) + '...';
            }

            html += role;
            html += '</div>';

            // cardBox
            html += '</div>';

            html += '</div>';

            //html += '<a class="tileItem smallPosterTileItem" href="' + href + '">';

            //var imgUrl;
            //var lazy = true;

            //if (cast.PrimaryImageTag) {

            //    imgUrl = ApiClient.getScaledImageUrl(cast.Id, {
            //        width: 100,
            //        tag: cast.PrimaryImageTag,
            //        type: "primary",
            //        minScale: 2
            //    });

            //} else {

            //    imgUrl = "css/images/items/list/person.png";
            //    lazy = false;
            //}

            //if (lazy) {
            //    html += '<div class="tileImage lazy" data-src="' + imgUrl + '"></div>';
            //} else {
            //    html += '<div class="tileImage" style="background-image:url(\'' + imgUrl + '\');"></div>';
            //}

            //html += '<div class="tileContent">';

            //html += '<p>' + cast.Name + '</p>';

            //var role = cast.Role ? Globalize.translate('ValueAsRole', cast.Role) : cast.Type;

            //if (role == "GuestStar") {
            //    role = Globalize.translate('ValueGuestStar');
            //}

            //role = role || "";

            //var maxlength = 40;

            //if (role.length > maxlength) {
            //    role = role.substring(0, maxlength - 3) + '...';
            //}

            //html += '<p>' + role + '</p>';

            //html += '</div>';

            //html += '</a>';
        }

        html += '</div>';

        var castContent = page.querySelector('#castContent');
        castContent.innerHTML = html;
        ImageLoader.lazyChildren(castContent);
    }

    function play(startPosition) {

        MediaController.play({
            items: [currentItem],
            startPositionTicks: startPosition
        });
    }

    function splitVersions(page) {

        var id = getParameterByName('id');

        require(['confirm'], function (confirm) {

            confirm("Are you sure you wish to split the media sources into separate items?", "Split Media Apart").then(function () {

                Dashboard.showLoadingMsg();

                ApiClient.ajax({
                    type: "DELETE",
                    url: ApiClient.getUrl("Videos/" + id + "/AlternateSources")

                }).then(function () {

                    Dashboard.hideLoadingMsg();

                    reload(page);
                });
            });
        });
    }

    function playTrailer(page) {

        ApiClient.getLocalTrailers(Dashboard.getCurrentUserId(), currentItem.Id).then(function (trailers) {

            MediaController.play({ items: trailers });

        });
    }

    function onItemDeleted(e, itemId) {

        if (currentItem && currentItem.Id == itemId) {
            if (currentItem.Type == 'Recording') {
                LibraryBrowser.showTab('livetv.html', 3);
            } else {
                Dashboard.navigate('home.html');
            }
        }
    }

    function playCurrentItem(button) {

        if (currentItem.Type == 'Program') {

            ApiClient.getLiveTvChannel(currentItem.ChannelId, Dashboard.getCurrentUserId()).then(function (channel) {

                LibraryBrowser.showPlayMenu(null, channel.Id, channel.Type, false, channel.MediaType, (channel.UserData || {}).PlaybackPositionTicks);
            });

            return;
        }

        var userdata = currentItem.UserData || {};

        var mediaType = currentItem.MediaType;

        if (currentItem.Type == "MusicArtist" || currentItem.Type == "MusicAlbum") {
            mediaType = "Audio";
        }

        LibraryBrowser.showPlayMenu(button, currentItem.Id, currentItem.Type, currentItem.IsFolder, mediaType, userdata.PlaybackPositionTicks);
    }

    function deleteTimer(page, id) {

        require(['confirm'], function (confirm) {

            confirm(Globalize.translate('MessageConfirmRecordingCancellation'), Globalize.translate('HeaderConfirmRecordingCancellation')).then(function () {

                Dashboard.showLoadingMsg();

                ApiClient.cancelLiveTvTimer(id).then(function () {

                    require(['toast'], function (toast) {
                        toast(Globalize.translate('MessageRecordingCancelled'));
                    });

                    reload(page);
                });
            });
        });
    }

    pageIdOn('pageinit', "itemDetailPage", function () {

        var page = this;

        $('.btnPlay', page).on('click', function () {
            playCurrentItem(this);
        });

        $('.btnPlayTrailer', page).on('click', function () {
            playTrailer(page);
        });

        $('.btnSplitVersions', page).on('click', function () {

            splitVersions(page);
        });

        $('.btnSync', page).on('click', function () {

            require(['syncDialog'], function (syncDialog) {
                syncDialog.showMenu({
                    items: [currentItem]
                });
            });
        });

        $('.btnRecord,.btnFloatingRecord', page).on('click', function () {

            var id = getParameterByName('id');
            require(['components/recordingcreator/recordingcreator'], function (recordingcreator) {
                recordingcreator.show(id).then(function () {
                    reload(page);
                });
            });

        });

        $('.btnCancelRecording', page).on('click', function () {

            deleteTimer(page, currentItem.TimerId);
        });

        $('.btnMoreCommands', page).on('click', function () {

            var button = this;

            Dashboard.getCurrentUser().then(function (user) {

                LibraryBrowser.showMoreCommands(button, currentItem.Id, LibraryBrowser.getMoreCommands(currentItem, user));
            });
        });

        $('.childrenItemsContainer', page).on('playallfromhere', function (e, index) {

            LibraryBrowser.playAllFromHere(_childrenItemsFunction, index);

        }).on('queueallfromhere', function (e, index) {

            LibraryBrowser.queueAllFromHere(_childrenItemsFunction, index);

        });

        $(page).on("click", ".moreScenes", function () {

            Dashboard.getCurrentUser().then(function (user) {
                renderScenes(page, currentItem, user);
            });

        }).on("click", ".morePeople", function () {

            renderCast(page, currentItem, getContext(currentItem));

        }).on("click", ".moreSpecials", function () {

            Dashboard.getCurrentUser().then(function (user) {
                renderSpecials(page, currentItem, user);
            });

        }).on("click", ".moreCriticReviews", function () {

            renderCriticReviews(page, currentItem);
        });

        //var btnMore = page.querySelectorAll('.btnMoreCommands iron-icon');
        //for (var i = 0, length = btnMore.length; i < length; i++) {
        //    btnMore[i].icon = AppInfo.moreIcon;
        //}

    });

    pageIdOn('pagebeforeshow', "itemDetailPage", function () {

        var page = this;

        reload(page);

        Events.on(ApiClient, 'websocketmessage', onWebSocketMessage);

        Events.on(LibraryBrowser, 'itemdeleting', onItemDeleted);

    });


    pageIdOn('pagebeforehide', "itemDetailPage", function () {

        Events.off(LibraryBrowser, 'itemdeleting', onItemDeleted);

        currentItem = null;

        var page = this;

        Events.off(ApiClient, 'websocketmessage', onWebSocketMessage);
        LibraryMenu.setTransparentMenu(false);
    });

    function itemDetailPage() {

        var self = this;

        self.play = play;
        self.setInitialCollapsibleState = setInitialCollapsibleState;
        self.renderDetails = renderDetails;
        self.renderCriticReviews = renderCriticReviews;
        self.renderCast = renderCast;
        self.renderScenes = renderScenes;
        self.renderMediaSources = renderMediaSources;
    }

    window.ItemDetailPage = new itemDetailPage();

});