define(['layoutManager', 'cardBuilder', 'datetime', 'mediaInfo', 'backdrop', 'listView', 'itemContextMenu', 'itemHelper', 'userdataButtons', 'dom', 'indicators', 'apphost', 'scrollStyles', 'emby-itemscontainer', 'emby-checkbox'], function (layoutManager, cardBuilder, datetime, mediaInfo, backdrop, listView, itemContextMenu, itemHelper, userdataButtons, dom, indicators, appHost) {

    var currentItem;

    function getPromise(params) {

        var id = params.id;

        if (id) {
            return ApiClient.getItem(Dashboard.getCurrentUserId(), id);
        }

        var name = params.genre;

        if (name) {
            return ApiClient.getGenre(name, Dashboard.getCurrentUserId());
        }

        name = params.musicgenre;

        if (name) {
            return ApiClient.getMusicGenre(name, Dashboard.getCurrentUserId());
        }

        name = params.gamegenre;

        if (name) {
            return ApiClient.getGameGenre(name, Dashboard.getCurrentUserId());
        }

        name = params.musicartist;

        if (name) {
            return ApiClient.getArtist(name, Dashboard.getCurrentUserId());
        }
        else {
            throw new Error('Invalid request');
        }
    }

    function reload(page, params) {

        Dashboard.showLoadingMsg();

        getPromise(params).then(function (item) {

            reloadFromItem(page, params, item);
        });
    }

    function hideAll(page, className, show) {

        var i, length;
        var elems = page.querySelectorAll('.' + className);
        for (i = 0, length = elems.length; i < length; i++) {
            if (show) {
                elems[i].classList.remove('hide');
            } else {
                elems[i].classList.add('hide');
            }
        }
    }

    function getContextMenuOptions(item, button) {

        var options = {
            item: item,
            open: false,
            play: false,
            queue: false,
            playAllFromHere: false,
            queueAllFromHere: false,
            positionTo: button
        };

        if (appHost.supports('sync')) {
            // Will be displayed via button
            options.syncLocal = false;
        } else {
            // Will be displayed via button
            options.sync = false;
        }

        return options;
    }

    function updateSyncStatus(page, item) {

        var i, length;
        var elems = page.querySelectorAll('.chkOffline');
        for (i = 0, length = elems.length; i < length; i++) {

            elems[i].checked = item.SyncPercent != null;
        }
    }

    function reloadFromItem(page, params, item) {

        currentItem = item;

        var context = params.context;

        LibraryMenu.setBackButtonVisible(true);
        LibraryMenu.setMenuButtonVisible(false);

        LibraryBrowser.renderName(item, page.querySelector('.itemName'), false, context);
        LibraryBrowser.renderParentName(item, page.querySelector('.parentName'), context);
        LibraryMenu.setTitle(item.SeriesName || item.Name);

        Dashboard.getCurrentUser().then(function (user) {

            window.scrollTo(0, 0);

            renderImage(page, item, user);

            setInitialCollapsibleState(page, item, context, user);
            renderDetails(page, item, context);

            var hasBackdrop = false;

            // For these types, make the backdrop a little smaller so that the items are more quickly accessible
            if (item.Type == 'MusicArtist' || item.Type == "MusicAlbum" || item.Type == "Playlist" || item.Type == "BoxSet" || item.MediaType == "Audio" || !layoutManager.mobile) {
                var itemBackdropElement = page.querySelector('#itemBackdrop');
                itemBackdropElement.classList.add('noBackdrop');
                itemBackdropElement.style.backgroundImage = 'none';
                backdrop.setBackdrops([item]);
            }
            else {
                hasBackdrop = LibraryBrowser.renderDetailPageBackdrop(page, item);
                backdrop.clear();
            }

            var transparentHeader = hasBackdrop && page.classList.contains('noSecondaryNavPage');

            LibraryMenu.setTransparentMenu(transparentHeader);

            var canPlay = false;

            if (item.Type == 'Program') {

                var now = new Date();

                if (now >= datetime.parseISO8601Date(item.StartDate, true) && now < datetime.parseISO8601Date(item.EndDate, true)) {
                    hideAll(page, 'btnPlay', true);
                    canPlay = true;
                } else {
                    hideAll(page, 'btnPlay');
                }
            }
            else if (MediaController.canPlay(item)) {
                hideAll(page, 'btnPlay', true);
                canPlay = true;
            }
            else {
                hideAll(page, 'btnPlay');
            }

            if (item.LocalTrailerCount && item.PlayAccess == 'Full') {
                hideAll(page, 'btnPlayTrailer', true);
            } else {
                hideAll(page, 'btnPlayTrailer');
            }

            if (itemHelper.canSync(user, item)) {
                if (appHost.supports('sync')) {
                    hideAll(page, 'syncLocalContainer', true);
                    hideAll(page, 'btnSync');
                } else {
                    hideAll(page, 'syncLocalContainer');
                    hideAll(page, 'btnSync', true);
                }
                updateSyncStatus(page, item);
            } else {
                hideAll(page, 'btnSync');
                hideAll(page, 'syncLocalContainer');
            }

            if (item.Type == 'Program' && item.TimerId) {
                hideAll(page, 'btnCancelRecording', true);
            } else {
                hideAll(page, 'btnCancelRecording');
            }

            if (item.Type == 'Program' && (!item.TimerId && !item.SeriesTimerId)) {

                if (canPlay) {
                    hideAll(page, 'btnRecord', true);
                    hideAll(page, 'btnFloatingRecord');
                } else {
                    hideAll(page, 'btnRecord');
                    hideAll(page, 'btnFloatingRecord', true);
                }
            } else {
                hideAll(page, 'btnRecord');
                hideAll(page, 'btnFloatingRecord');
            }

            var btnPlayExternalTrailer = page.querySelectorAll('.btnPlayExternalTrailer');
            for (var i = 0, length = btnPlayExternalTrailer.length; i < length; i++) {
                if (!item.LocalTrailerCount && item.RemoteTrailers.length && item.PlayAccess == 'Full') {

                    btnPlayExternalTrailer[i].classList.remove('hide');
                    btnPlayExternalTrailer[i].href = item.RemoteTrailers[0].Url;

                } else {

                    btnPlayExternalTrailer[i].classList.add('hide');
                    btnPlayExternalTrailer[i].href = '#';
                }
            }

            var groupedVersions = (item.MediaSources || []).filter(function (g) {
                return g.Type == "Grouping";
            });

            if (user.Policy.IsAdministrator && groupedVersions.length) {
                page.querySelector('.splitVersionContainer').classList.remove('hide');
            } else {
                page.querySelector('.splitVersionContainer').classList.add('hide');
            }

            itemContextMenu.getCommands(getContextMenuOptions(item)).then(function (commands) {
                if (commands.length) {
                    hideAll(page, 'btnMoreCommands', true);
                } else {
                    hideAll(page, 'btnMoreCommands');
                }
            });

            if (user.Policy.IsAdministrator) {
                page.querySelector('.chapterSettingsButton').classList.remove('hide');
            } else {
                page.querySelector('.chapterSettingsButton').classList.add('hide');
            }

            var itemBirthday = page.querySelector('#itemBirthday');
            if (item.Type == "Person" && item.PremiereDate) {

                try {
                    var birthday = datetime.parseISO8601Date(item.PremiereDate, true).toDateString();

                    itemBirthday.classList.remove('hide');
                    itemBirthday.innerHTML = Globalize.translate('BirthDateValue').replace('{0}', birthday);
                }
                catch (err) {
                    itemBirthday.classList.add('hide');
                }
            } else {
                itemBirthday.classList.add('hide');
            }

            var itemDeathDate = page.querySelector('#itemBirthday');
            if (item.Type == "Person" && item.EndDate) {

                try {
                    var deathday = datetime.parseISO8601Date(item.EndDate, true).toDateString();

                    itemDeathDate.classList.remove('hide');
                    itemDeathDate.innerHTML = Globalize.translate('DeathDateValue').replace('{0}', deathday);
                }
                catch (err) {
                    itemDeathDate.classList.add('hide');
                }
            } else {
            }

            var itemBirthLocation = page.querySelector('#itemBirthLocation');
            if (item.Type == "Person" && item.ProductionLocations && item.ProductionLocations.length) {

                var gmap = '<a class="textlink" target="_blank" href="https://maps.google.com/maps?q=' + item.ProductionLocations[0] + '">' + item.ProductionLocations[0] + '</a>';

                itemBirthLocation.classList.remove('hide');
                itemBirthLocation.innerHTML = Globalize.translate('BirthPlaceValue').replace('{0}', gmap);
            } else {
                itemBirthLocation.classList.add('hide');
            }
        });

        //if (item.LocationType == "Offline") {

        //    page.querySelector('.offlineIndicator').classList.remove('hide');
        //}
        //else {
        //    page.querySelector('.offlineIndicator').classList.add('hide');
        //}

        var isMissingEpisode = false;

        if (item.LocationType == "Virtual" && item.Type == "Episode") {
            try {
                if (item.PremiereDate && (new Date().getTime() >= datetime.parseISO8601Date(item.PremiereDate, true).getTime())) {
                    isMissingEpisode = true;
                }
            } catch (err) {

            }
        }

        //if (isMissingEpisode) {

        //    page.querySelector('.missingIndicator').classList.remove('hide');
        //}
        //else {
        //    page.querySelector('.missingIndicator').classList.add('hide');
        //}

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

    function renderLinks(linksElem, item) {

        var links = [];

        if (item.HomePageUrl) {
            links.push('<a class="textlink" href="' + item.HomePageUrl + '" target="_blank">' + Globalize.translate('ButtonWebsite') + '</a>');
        }

        if (item.ExternalUrls) {

            for (var i = 0, length = item.ExternalUrls.length; i < length; i++) {

                var url = item.ExternalUrls[i];

                links.push('<a class="textlink" href="' + url.Url + '" target="_blank">' + url.Name + '</a>');
            }
        }

        if (links.length) {

            var html = links.join('&nbsp;&nbsp;/&nbsp;&nbsp;');

            html = Globalize.translate('ValueLinks', html);

            linksElem.innerHTML = html;
            linksElem.classList.remove('hide');

        } else {
            linksElem.classList.add('hide');
        }
    }

    function renderImage(page, item, user) {

        LibraryBrowser.renderDetailImage(page.querySelector('.detailImageContainer'), item, user.Policy.IsAdministrator && item.MediaType != 'Photo');
    }

    function refreshDetailImageUserData(elem, item) {

        var detailImageProgressContainer = elem.querySelector('.detailImageProgressContainer');

        detailImageProgressContainer.innerHTML = indicators.getProgressBarHtml(item);
    }

    function refreshImage(page, item, user) {

        refreshDetailImageUserData(page.querySelector('.detailImageContainer'), item);
    }

    function setPeopleHeader(page, item) {

        if (item.MediaType == "Audio" || item.Type == "MusicAlbum" || item.MediaType == "Book" || item.MediaType == "Photo") {
            page.querySelector('#peopleHeader').innerHTML = Globalize.translate('HeaderPeople');
        } else {
            page.querySelector('#peopleHeader').innerHTML = Globalize.translate('HeaderCastAndCrew');
        }

    }

    function renderNextUp(page, item, user) {

        var section = page.querySelector('.nextUpSection');

        if (item.Type != 'Series') {
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

            var html = cardBuilder.getCardsHtml({
                items: result.Items,
                shape: getThumbShape(false),
                showTitle: true,
                displayAsSpecial: item.Type == "Season" && item.IndexNumber,
                overlayText: true,
                lazy: true,
                overlayPlayButton: true
            });

            var itemsContainer = section.querySelector('.nextUpItems');

            itemsContainer.innerHTML = html;
            ImageLoader.lazyChildren(itemsContainer);
        });
    }

    function setInitialCollapsibleState(page, item, context, user) {

        page.querySelector('.collectionItems').innerHTML = '';

        if (item.Type == 'TvChannel') {

            page.querySelector('#childrenCollapsible').classList.remove('hide');
            renderChannelGuide(page, item, user);
        }
        else if (item.Type == 'Playlist') {

            page.querySelector('#childrenCollapsible').classList.remove('hide');
            renderPlaylistItems(page, item, user);
        }
        else if (item.Type == 'Studio' || item.Type == 'Person' || item.Type == 'Genre' || item.Type == 'MusicGenre' || item.Type == 'GameGenre' || item.Type == 'MusicArtist') {

            page.querySelector('#childrenCollapsible').classList.remove('hide');
            renderItemsByName(page, item, user);
        }
        else if (item.IsFolder) {

            if (item.Type == "BoxSet") {
                page.querySelector('#childrenCollapsible').classList.add('hide');
            } else {
                page.querySelector('#childrenCollapsible').classList.remove('hide');
            }
            renderChildren(page, item);
        }
        else {
            page.querySelector('#childrenCollapsible').classList.add('hide');
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

        if (!chapters.length) {
            page.querySelector('#scenesCollapsible').classList.add('hide');
        } else {
            page.querySelector('#scenesCollapsible').classList.remove('hide');
            renderScenes(page, item, user, 3);
        }

        if (!item.SpecialFeatureCount || item.SpecialFeatureCount == 0 || item.Type == "Series") {
            page.querySelector('#specialsCollapsible').classList.add('hide');
        } else {
            page.querySelector('#specialsCollapsible').classList.remove('hide');
            renderSpecials(page, item, user, 6);
        }
        if (!item.People || !item.People.length) {
            page.querySelector('#castCollapsible').classList.add('hide');
        } else {
            page.querySelector('#castCollapsible').classList.remove('hide');
            renderCast(page, item, context, enableScrollX() ? null : 12);
        }

        if (item.PartCount && item.PartCount > 1) {
            page.querySelector('#additionalPartsCollapsible').classList.remove('hide');
            renderAdditionalParts(page, item, user);
        } else {
            page.querySelector('#additionalPartsCollapsible').classList.add('hide');
        }

        page.querySelector('#themeSongsCollapsible').classList.add('hide');
        page.querySelector('#themeVideosCollapsible').classList.add('hide');

        if (item.Type == "MusicAlbum") {
            renderMusicVideos(page, item, user);
        } else {
            page.querySelector('#musicVideosCollapsible').classList.add('hide');
        }

        renderThemeMedia(page, item, user);

        if (enableScrollX()) {
            renderCriticReviews(page, item);
        } else {
            renderCriticReviews(page, item, 1);
        }
    }

    function renderOverview(elems, item) {

        for (var i = 0, length = elems.length; i < length; i++) {
            var elem = elems[i];
            var overview = item.Overview || '';

            if (overview) {
                elem.innerHTML = overview;

                elem.classList.remove('empty');

                var anchors = elem.querySelectorAll('a');
                for (var j = 0, length2 = anchors.length; j < length2; j++) {
                    anchors[j].setAttribute("target", "_blank");
                }

            } else {
                elem.innerHTML = '';

                elem.classList.add('empty');
            }
        }
    }

    function renderDetails(page, item, context, isStatic) {

        renderSimilarItems(page, item, context);
        renderMoreFromItems(page, item);

        if (!isStatic) {
            renderSiblingLinks(page, item, context);
        }

        var taglineElement = page.querySelector('.tagline');

        if (item.Taglines && item.Taglines.length) {
            taglineElement.classList.remove('hide');
            taglineElement.innerHTML = item.Taglines[0];
        } else {
            taglineElement.classList.add('hide');
        }

        var topOverview = page.querySelector('.topOverview');
        var bottomOverview = page.querySelector('.bottomOverview');

        var seasonOnBottom = screen.availHeight < 800 || screen.availWidth < 600;

        if (item.Type == 'MusicAlbum' || item.Type == 'MusicArtist' || (item.Type == 'Season' && seasonOnBottom)) {
            renderOverview([bottomOverview], item);
            topOverview.classList.add('hide');
            bottomOverview.classList.remove('hide');
        } else {
            renderOverview([topOverview], item);
            topOverview.classList.remove('hide');
            bottomOverview.classList.add('hide');
        }

        renderAwardSummary(page.querySelector('#awardSummary'), item);

        var i, length;
        var itemMiscInfo = page.querySelectorAll('.itemMiscInfo');
        for (i = 0, length = itemMiscInfo.length; i < length; i++) {
            mediaInfo.fillPrimaryMediaInfo(itemMiscInfo[i], item, {
                interactive: true
            });
        }
        var itemGenres = page.querySelectorAll('.itemGenres');
        for (i = 0, length = itemGenres.length; i < length; i++) {
            renderGenres(itemGenres[i], item, null, isStatic);
        }

        renderStudios(page.querySelector('.itemStudios'), item, isStatic);
        renderUserDataIcons(page, item);
        renderLinks(page.querySelector('.itemExternalLinks'), item);

        page.querySelector('.criticRatingScore').innerHTML = (item.CriticRating || '0') + '%';

        if (item.CriticRatingSummary) {
            page.querySelector('#criticRatingSummary').classList.remove('hide');
            page.querySelector('.criticRatingSummaryText').innerHTML = item.CriticRatingSummary;

        } else {
            page.querySelector('#criticRatingSummary').classList.add('hide');
        }

        renderTags(page, item);

        renderSeriesAirTime(page, item, isStatic);

        var playersElement = page.querySelector('#players');

        if (item.Players) {
            playersElement.classList.remove('hide');
            playersElement.innerHTML = item.Players + ' Player';
        } else {
            playersElement.classList.add('hide');
        }

        var artist = page.querySelectorAll('.artist');
        for (i = 0, length = artist.length; i < length; i++) {
            if (item.ArtistItems && item.ArtistItems.length && item.Type != "MusicAlbum") {
                artist[i].classList.remove('hide');
                artist[i].innerHTML = getArtistLinksHtml(item.ArtistItems, context);
            } else {
                artist[i].classList.add('hide');
            }
        }

        if (item.MediaSources && item.MediaSources.length && item.Path) {
            page.querySelector('.audioVideoMediaInfo').classList.remove('hide');
        } else {
            page.querySelector('.audioVideoMediaInfo').classList.add('hide');
        }

        if (item.MediaType == 'Photo') {
            page.querySelector('.photoInfo').classList.remove('hide');
            renderPhotoInfo(page, item);
        } else {
            page.querySelector('.photoInfo').classList.add('hide');
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

        page.querySelector('.photoInfoContent').innerHTML = html;
    }

    function renderTabButtons(page, item) {

        var elem = page.querySelector('.tabDetails');
        var text = elem.textContent || elem.innerText || '';

        if (text.trim()) {

            page.querySelector('.detailsSection').classList.remove('hide');

        } else {
            page.querySelector('.detailsSection').classList.add('hide');
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

        var lnkPreviousItem = page.querySelector('.lnkPreviousItem');
        var lnkNextItem = page.querySelector('.lnkNextItem');

        if ((item.Type != "Episode" && item.Type != "Season" && item.Type != "Audio" && item.Type != "Photo")) {
            lnkNextItem.classList.add('hide');
            lnkPreviousItem.classList.add('hide');

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

                    lnkPreviousItem.classList.remove('hide');
                    lnkPreviousItem.href = 'itemdetails.html?id=' + curr.Id + '&context=' + context;
                }
                else {

                    lnkNextItem.classList.remove('hide');
                    lnkNextItem.href = 'itemdetails.html?id=' + curr.Id + '&context=' + context;
                }
            }
        });
    }

    function enableScrollX() {
        return browserInfo.mobile && AppInfo.enableAppLayouts && screen.availWidth <= 1000;
    }

    function getPortraitShape(scrollX) {
        if (scrollX == null) {
            scrollX = enableScrollX();
        }
        return scrollX ? 'overflowPortrait' : 'portrait';
    }

    function getSquareShape(scrollX) {
        if (scrollX == null) {
            scrollX = enableScrollX();
        }
        return scrollX ? 'overflowSquare' : 'square';
    }

    function getThumbShape(scrollX) {

        if (scrollX == null) {
            scrollX = enableScrollX();
        }
        return scrollX ? 'overflowBackdrop' : 'backdrop';
    }

    function renderMoreFromItems(page, item) {

        var moreFromSection = page.querySelector('#moreFromSection');

        if (!moreFromSection) {
            return;
        }

        if (item.Type != 'MusicAlbum' || !item.AlbumArtists || !item.AlbumArtists.length) {
            moreFromSection.classList.add('hide');
            return;
        }

        ApiClient.getItems(Dashboard.getCurrentUserId(), {

            IncludeItemTypes: "MusicAlbum",
            ArtistIds: item.AlbumArtists[0].Id,
            Recursive: true,
            ExcludeItemIds: item.Id

        }).then(function (result) {

            if (!result.Items.length) {
                moreFromSection.classList.add('hide');
                return;
            }
            moreFromSection.classList.remove('hide');

            moreFromSection.querySelector('.moreFromHeader').innerHTML = Globalize.translate('MoreFromValue', item.AlbumArtists[0].Name);

            var html = '';

            if (enableScrollX()) {
                html += '<div is="emby-itemscontainer" class="hiddenScrollX itemsContainer">';
            } else {
                html += '<div is="emby-itemscontainer" class="itemsContainer vertical-wrap">';
            }

            var shape = item.Type == "MusicAlbum" || item.Type == "MusicArtist" ? getSquareShape() : getPortraitShape();

            html += cardBuilder.getCardsHtml({
                items: result.Items,
                shape: shape,
                showParentTitle: item.Type == "MusicAlbum",
                centerText: true,
                showTitle: item.Type == "MusicAlbum" || item.Type == "Game" || item.Type == "MusicArtist",
                coverImage: item.Type == "MusicAlbum" || item.Type == "MusicArtist",
                overlayPlayButton: true
            });
            html += '</div>';

            var similarContent = page.querySelector('#moreFromItems');
            similarContent.innerHTML = html;
            ImageLoader.lazyChildren(similarContent);
        });
    }

    function renderSimilarItems(page, item, context) {

        var similarCollapsible = page.querySelector('#similarCollapsible');

        if (!similarCollapsible) {
            return;
        }

        if (item.Type == "Movie" || item.Type == "Trailer" || item.Type == "Series" || item.Type == "Program" || item.Type == "Recording" || item.Type == "Game" || item.Type == "MusicAlbum" || item.Type == "MusicArtist" || item.Type == "ChannelVideoItem") {
            similarCollapsible.classList.remove('hide');
        }
        else {
            similarCollapsible.classList.add('hide');
            return;
        }

        var shape = item.Type == "MusicAlbum" || item.Type == "MusicArtist" ? getSquareShape() : getPortraitShape();

        var options = {
            userId: Dashboard.getCurrentUserId(),
            limit: 8,
            fields: "PrimaryImageAspectRatio,UserData,CanDelete"
        };

        if (item.Type == 'MusicAlbum' && item.AlbumArtists && item.AlbumArtists.length) {
            options.ExcludeArtistIds = item.AlbumArtists[0].Id;
        }

        if (enableScrollX()) {
            options.limit = 12;
        }

        ApiClient.getSimilarItems(item.Id, options).then(function (result) {

            if (!result.Items.length) {

                similarCollapsible.classList.add('hide');
                return;
            }

            similarCollapsible.classList.remove('hide');
            similarCollapsible.querySelector('.similiarHeader').innerHTML = Globalize.translate('HeaderIfYouLikeCheckTheseOut', item.Name);

            var html = '';

            if (enableScrollX()) {
                html += '<div is="emby-itemscontainer" class="hiddenScrollX itemsContainer">';
            } else {
                html += '<div is="emby-itemscontainer" class="itemsContainer vertical-wrap">';
            }
            html += cardBuilder.getCardsHtml({
                items: result.Items,
                shape: shape,
                showParentTitle: item.Type == "MusicAlbum",
                centerText: true,
                showTitle: item.Type == "MusicAlbum" || item.Type == "Game" || item.Type == "MusicArtist",
                context: context,
                lazy: true,
                showDetailsMenu: true,
                coverImage: item.Type == "MusicAlbum" || item.Type == "MusicArtist",
                overlayPlayButton: true
            });
            html += '</div>';

            var similarContent = page.querySelector('#similarContent');
            similarContent.innerHTML = html;
            ImageLoader.lazyChildren(similarContent);
        });
    }

    function renderSeriesAirTime(page, item, isStatic) {

        var seriesAirTime = page.querySelector('#seriesAirTime');

        if (item.Type != "Series") {
            seriesAirTime.classList.add('hide');
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

            seriesAirTime.innerHTML = html;
            seriesAirTime.classList.remove('hide');
        } else {
            seriesAirTime.classList.add('hide');
        }
    }

    function renderTags(page, item) {

        var itemTags = page.querySelector('.itemTags');

        if (item.Tags && item.Tags.length) {

            var html = '';
            html += '<p>' + Globalize.translate('HeaderTags') + '</p>';
            for (var i = 0, length = item.Tags.length; i < length; i++) {

                html += '<div class="itemTag">' + item.Tags[i] + '</div>';

            }

            itemTags.innerHTML = html;
            itemTags.classList.remove('hide');

        } else {
            itemTags.classList.add('hide');
        }
    }

    function getEpisodesFunction(seriesId, query) {

        query = Object.assign({}, query);

        return function (index, limit, fields) {

            query.StartIndex = index;
            query.Limit = limit;
            query.Fields = fields;

            return ApiClient.getEpisodes(seriesId, query);

        };

    }

    function getAlbumSongsFunction(query) {

        query = Object.assign({}, query);

        return function (index, limit, fields) {

            query.StartIndex = index;
            query.Limit = limit;
            query.Fields = fields;

            return ApiClient.getItems(Dashboard.getCurrentUserId(), query);

        };

    }

    var _childrenItemsFunction = null;
    function renderChildren(page, item) {

        _childrenItemsFunction = null;

        var fields = "ItemCounts,AudioInfo,PrimaryImageAspectRatio,BasicSyncInfo,CanDelete";

        var query = {
            ParentId: item.Id,
            Fields: fields
        };

        // Let the server pre-sort boxsets
        if (item.Type !== "BoxSet") {
            query.SortBy = "SortName";
        }

        var userId = Dashboard.getCurrentUserId();
        var promise;

        if (item.Type == "Series") {

            promise = ApiClient.getSeasons(item.Id, {

                userId: userId,
                Fields: fields
            });
        }
        else if (item.Type == "Season") {

            // Use dedicated episodes endpoint
            promise = ApiClient.getEpisodes(item.SeriesId, {

                seasonId: item.Id,
                userId: userId,
                Fields: fields
            });

            _childrenItemsFunction = getEpisodesFunction(item.SeriesId, {

                seasonId: item.Id,
                userId: userId,
                Fields: fields
            });
        }
        else if (item.Type == "MusicAlbum") {

            _childrenItemsFunction = getAlbumSongsFunction(query);
        }

        promise = promise || ApiClient.getItems(Dashboard.getCurrentUserId(), query);

        promise.then(function (result) {

            var html = '';

            var scrollX = false;
            var isList = false;

            if (item.Type == "MusicAlbum") {

                html = listView.getListViewHtml({
                    items: result.Items,
                    smallIcon: true,
                    showIndex: true,
                    index: 'disc',
                    showIndexNumber: true,
                    playFromHere: true,
                    action: 'playallfromhere',
                    lazy: true
                });
                isList = true;
            }
            else if (item.Type == "Series") {

                scrollX = enableScrollX();

                html = cardBuilder.getCardsHtml({
                    items: result.Items,
                    shape: getPortraitShape(),
                    showTitle: true,
                    centerText: true,
                    lazy: true,
                    overlayPlayButton: true,
                    allowBottomPadding: !scrollX
                });
            }
            else if (item.Type == "Season") {

                html = cardBuilder.getCardsHtml({
                    items: result.Items,
                    shape: getThumbShape(false),
                    showTitle: true,
                    displayAsSpecial: item.Type == "Season" && item.IndexNumber,
                    playFromHere: true,
                    overlayText: true,
                    lazy: true,
                    showDetailsMenu: true,
                    overlayPlayButton: AppInfo.enableAppLayouts,
                });
            }
            else if (item.Type == "GameSystem") {
                html = cardBuilder.getCardsHtml({
                    items: result.Items,
                    shape: "auto",
                    showTitle: true,
                    centerText: true,
                    lazy: true,
                    showDetailsMenu: true
                });
            }

            var elem = page.querySelector('.childrenItemsContainer');
            if (scrollX) {
                elem.classList.add('hiddenScrollX');
                elem.classList.remove('vertical-wrap');
                elem.classList.remove('vertical-list');
            } else {
                elem.classList.remove('hiddenScrollX');

                if (isList) {
                    elem.classList.add('vertical-list');
                    elem.classList.remove('vertical-wrap');
                } else {
                    elem.classList.add('vertical-wrap');
                    elem.classList.remove('vertical-list');
                }
            }

            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);

            if (item.Type == "BoxSet") {

                var collectionItemTypes = [
                    { name: Globalize.translate('HeaderMovies'), type: 'Movie' },
                    { name: Globalize.translate('HeaderSeries'), type: 'Series' },
                    { name: Globalize.translate('HeaderAlbums'), type: 'MusicAlbum' },
                    { name: Globalize.translate('HeaderGames'), type: 'Game' },
                    { name: Globalize.translate('HeaderBooks'), type: 'Book' }
                ];

                renderCollectionItems(page, item, collectionItemTypes, result.Items);
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
            page.querySelector('.childrenSectionHeader', page).classList.add('hide');
        } else {
            page.querySelector('.childrenSectionHeader', page).classList.remove('hide');
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

    function renderStudios(elem, item, isStatic) {

        if (item.Studios && item.Studios.length && item.Type != "Series") {

            var html = '';

            for (var i = 0, length = item.Studios.length; i < length; i++) {

                if (i > 0) {
                    html += '&nbsp;&nbsp;/&nbsp;&nbsp;';
                }

                if (isStatic) {
                    html += item.Studios[i].Name;
                } else {
                    html += '<a class="textlink" href="itemdetails.html?id=' + item.Studios[i].Id + '">' + item.Studios[i].Name + '</a>';
                }
            }

            var translationKey = item.Studios.length > 1 ? "ValueStudios" : "ValueStudio";

            html = Globalize.translate(translationKey, html);

            elem.innerHTML = html;
            elem.classList.remove('hide');

        } else {
            elem.classList.add('hide');
        }
    }

    function renderGenres(elem, item, limit, isStatic) {

        var html = '';

        var genres = item.Genres || [];

        for (var i = 0, length = genres.length; i < length; i++) {

            if (limit && i >= limit) {
                break;
            }

            if (i > 0) {
                html += '<span>&nbsp;&nbsp;/&nbsp;&nbsp;</span>';
            }

            var param = item.Type == "Audio" || item.Type == "MusicArtist" || item.Type == "MusicAlbum" ? "musicgenre" : "genre";

            if (item.MediaType == "Game") {
                param = "gamegenre";
            }

            if (isStatic) {
                html += genres[i];
            } else {
                html += '<a class="textlink" href="itemdetails.html?' + param + '=' + ApiClient.encodeName(genres[i]) + '">' + genres[i] + '</a>';
            }
        }

        elem.innerHTML = html;
    }

    function renderAwardSummary(elem, item) {
        if (item.AwardSummary) {
            elem.classList.remove('hide');
            elem.innerHTML = Globalize.translate('ValueAwards', item.AwardSummary);
        } else {
            elem.classList.add('hide');
        }
    }

    function renderCollectionItems(page, parentItem, types, items) {

        // First empty out existing content
        page.querySelector('.collectionItems').innerHTML = '';
        var i, length;

        for (i = 0, length = types.length; i < length; i++) {

            var type = types[i];

            var typeItems = items.filter(function (curr) {

                return curr.Type == type.type;

            });

            if (typeItems.length) {
                renderCollectionItemType(page, parentItem, type, typeItems);
            }
        }

        var otherType = { name: Globalize.translate('HeaderOtherItems') };

        var otherTypeItems = items.filter(function (curr) {

            return !types.filter(function (t) {

                return t.type == curr.Type;

            }).length;

        });

        if (otherTypeItems.length) {
            renderCollectionItemType(page, parentItem, otherType, otherTypeItems);
        }

        if (!items.length) {
            renderCollectionItemType(page, parentItem, { name: Globalize.translate('HeaderItems') }, items);
        }
    }

    function renderCollectionItemType(page, parentItem, type, items) {

        var html = '';

        html += '<div class="detailSection">';

        html += '<div style="display:flex;align-items:center;">';
        html += '<h1>';
        html += '<span>' + type.name + '</span>';

        html += '</h1>';
        html += '<button class="btnAddToCollection autoSize" type="button" is="paper-icon-button-light" style="margin-left:1em;"><i class="md-icon" icon="add">add</i></button>';
        html += '</div>';

        html += '<div is="emby-itemscontainer" class="detailSectionContent itemsContainer vertical-wrap">';

        var shape = type.type == 'MusicAlbum' ? getSquareShape(false) : getPortraitShape(false);

        html += cardBuilder.getCardsHtml({
            items: items,
            shape: shape,
            showTitle: true,
            centerText: true,
            lazy: true,
            showDetailsMenu: true,
            overlayMoreButton: true,
            showAddToCollection: false,
            showRemoveFromCollection: true,
            collectionId: parentItem.Id
        });
        html += '</div>';

        html += '</div>';

        var collectionItems = page.querySelector('.collectionItems');
        collectionItems.insertAdjacentHTML('beforeend', html);
        ImageLoader.lazyChildren(collectionItems);

        collectionItems.querySelector('.btnAddToCollection').addEventListener('click', function () {
            require(['alert'], function (alert) {
                alert({
                    text: Globalize.translate('AddItemToCollectionHelp'),
                    html: Globalize.translate('AddItemToCollectionHelp') + '<br/><br/><a target="_blank" href="https://github.com/MediaBrowser/Wiki/wiki/Collections">' + Globalize.translate('ButtonLearnMore') + '</a>'
                });
            });
        });
    }

    function renderUserDataIcons(page, item) {

        var userDataIcons = page.querySelectorAll('.userDataIcons');

        var html = userdataButtons.getIconsHtml({
            item: item,
            style: 'fab-mini'
        });

        for (var i = 0, length = userDataIcons.length; i < length; i++) {
            userDataIcons[i].innerHTML = html;
        }
    }

    function renderCriticReviews(page, item, limit) {

        if (item.Type != "Movie" && item.Type != "Trailer" && item.Type != "MusicVideo") {
            page.querySelector('#criticReviewsCollapsible').classList.add('hide');
            return;
        }

        var options = {};

        if (limit) {
            options.limit = limit;
        }

        ApiClient.getCriticReviews(item.Id, options).then(function (result) {

            if (result.TotalRecordCount || item.CriticRatingSummary || item.AwardSummary) {
                page.querySelector('#criticReviewsCollapsible').classList.remove('hide');
                renderCriticReviewsContent(page, result, limit);
            } else {
                page.querySelector('#criticReviewsCollapsible').classList.add('hide');
            }
        });
    }

    function renderCriticReviewsContent(page, result, limit) {

        var html = '';

        var reviews = result.Items;
        for (var i = 0, length = reviews.length; i < length; i++) {

            var review = reviews[i];

            html += '<div class="paperList criticReviewPaperList">';
            html += '<div class="listItem">';

            if (review.Score != null) {
                //html += review.Score;
            }
            else if (review.Likes != null) {

                if (review.Likes) {
                    html += '<div style="background-color:transparent;background-image:url(\'css/images/fresh.png\');background-repeat:no-repeat;background-position:center center;background-size: cover;width:40px;height:40px;"></div>';
                } else {
                    html += '<div style="background-color:transparent;background-image:url(\'css/images/rotten.png\');background-repeat:no-repeat;background-position:center center;background-size: cover;width:40px;height:40px;"></div>';
                }
            }

            html += '<div class="listItemBody two-line">';

            html += '<div style="white-space:normal;">' + review.Caption + '</div>';

            var vals = [];

            if (review.ReviewerName) {
                vals.push(review.ReviewerName);
            }
            if (review.Publisher) {
                vals.push(review.Publisher);
            }

            html += '<div class="secondary">' + vals.join(', ') + '.';
            if (review.Date) {

                try {

                    var date = datetime.parseISO8601Date(review.Date, true).toLocaleDateString();

                    html += '<span class="reviewDate">' + date + '</span>';
                }
                catch (error) {

                }

            }
            html += '</div>';

            if (review.Url) {
                html += '<div class="secondary"><a class="textlink" href="' + review.Url + '" target="_blank">' + Globalize.translate('ButtonFullReview') + '</a></div>';
            }

            html += '</div>';

            html += '</div>';
            html += '</div>';
        }

        if (limit && result.TotalRecordCount > limit) {
            html += '<p style="margin: 0;"><button is="emby-button" type="button" class="raised more moreCriticReviews">' + Globalize.translate('ButtonMore') + '</button></p>';
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
        });
    }

    function renderThemeSongs(page, items) {

        if (items.length) {

            page.querySelector('#themeSongsCollapsible').classList.remove('hide');

            var html = listView.getListViewHtml({
                items: items
            });

            page.querySelector('#themeSongsContent').innerHTML = html;
        } else {
            page.querySelector('#themeSongsCollapsible').classList.add('hide');
        }
    }

    function renderThemeVideos(page, items, user) {

        if (items.length) {

            page.querySelector('#themeVideosCollapsible').classList.remove('hide');

            var themeVideosContent = page.querySelector('#themeVideosContent');
            themeVideosContent.innerHTML = getVideosHtml(items, user);
            ImageLoader.lazyChildren(themeVideosContent);
        } else {
            page.querySelector('#themeVideosCollapsible').classList.add('hide');
        }
    }

    function renderMusicVideos(page, item, user) {

        ApiClient.getItems(user.Id, {

            SortBy: "SortName",
            SortOrder: "Ascending",
            IncludeItemTypes: "MusicVideo",
            Recursive: true,
            Fields: "DateCreated,CanDelete",
            Albums: item.Name

        }).then(function (result) {
            if (result.Items.length) {

                page.querySelector('#musicVideosCollapsible').classList.remove('hide');

                var musicVideosContent = page.querySelector('.musicVideosContent');
                musicVideosContent.innerHTML = getVideosHtml(result.Items, user);
                ImageLoader.lazyChildren(musicVideosContent);

            } else {
                page.querySelector('#musicVideosCollapsible').classList.add('hide');
            }
        });

    }

    function renderAdditionalParts(page, item, user) {

        ApiClient.getAdditionalVideoParts(user.Id, item.Id).then(function (result) {

            if (result.Items.length) {

                page.querySelector('#additionalPartsCollapsible').classList.remove('hide');

                var additionalPartsContent = page.querySelector('#additionalPartsContent');
                additionalPartsContent.innerHTML = getVideosHtml(result.Items, user);
                ImageLoader.lazyChildren(additionalPartsContent);

            } else {
                page.querySelector('#additionalPartsCollapsible').classList.add('hide');
            }
        });
    }

    function renderScenes(page, item, user, limit, isStatic) {

        var chapters = item.Chapters || [];
        var scenesContent = page.querySelector('#scenesContent');

        if (enableScrollX()) {
            scenesContent.classList.add('smoothScrollX');
            limit = null;
        } else {
            scenesContent.classList.add('vertical-wrap');
        }

        var limitExceeded = limit && chapters.length > limit;

        if (limitExceeded) {
            chapters = chapters.slice(0);
            chapters.length = Math.min(limit, chapters.length);
        }

        require(['chaptercardbuilder'], function (chaptercardbuilder) {

            chaptercardbuilder.buildChapterCards(item, chapters, {
                itemsContainer: scenesContent,
                coverImage: true,
                width: 400,
                backdropShape: getThumbShape(),
                squareShape: getSquareShape()
            });
        });

        if (limitExceeded) {
            page.querySelector('.moreScenes').classList.remove('hide');
        } else {
            page.querySelector('.moreScenes').classList.add('hide');
        }
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

            html += '<h3 class="mediaInfoStreamType">' + displayType + '</h3>';

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

            if (stream.IsAVC != null) {
                attributes.push(createAttribute('AVC', (stream.IsAVC ? 'Yes' : 'No')));
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
                attributes.push(createAttribute(Globalize.translate('MediaInfoBitrate'), (parseInt(stream.BitRate / 1000)) + ' kbps'));
            }

            if (stream.SampleRate) {
                attributes.push(createAttribute(Globalize.translate('MediaInfoSampleRate'), stream.SampleRate + ' Hz'));
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

            if (stream.NalLengthSize) {
                attributes.push(createAttribute('NAL', stream.NalLengthSize));
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

            if (stream.DisplayTitle) {
                attributes.push(createAttribute('Title', stream.DisplayTitle));
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

            var cssClass = "card backdropCard scalableCard backdropCard-scalable";

            var href = "itemdetails.html?id=" + item.Id;

            var onclick = item.PlayAccess == 'Full' ? ' onclick="MediaController.play(\'' + item.Id + '\'); return false;"' : "";

            html += '<a class="' + cssClass + '" href="' + href + '"' + onclick + '>';

            html += '<div class="cardBox">';
            html += '<div class="cardScalable">';

            var imageTags = item.ImageTags || {};

            var imgUrl;

            if (imageTags.Primary) {

                imgUrl = ApiClient.getScaledImageUrl(item.Id, {
                    maxWidth: 400,
                    tag: imageTags.Primary,
                    type: "primary"
                });

            } else {
                imgUrl = "css/images/items/detail/video.png";
            }

            html += '<div class="cardPadder cardPadder-backdrop"></div>';

            html += '<div class="cardContent">';
            html += '<div class="cardImage lazy" data-src="' + imgUrl + '"></div>';

            html += '<div class="innerCardFooter">';
            html += '<div class="cardText">' + item.Name + '</div>';
            html += '<div class="cardText">';
            if (item.RunTimeTicks != "") {
                html += datetime.getDisplayRunningTime(item.RunTimeTicks);
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
            html += '<p style="margin: 0;padding-left:5px;"><button is="emby-button" type="button" class="raised more ' + moreButtonClass + '">' + Globalize.translate('ButtonMore') + '</button></p>';
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

        var people = item.People || [];
        var castContent = page.querySelector('#castContent');

        if (enableScrollX()) {
            castContent.classList.add('smoothScrollX');
            limit = 32;
        } else {
            castContent.classList.add('vertical-wrap');
        }

        var limitExceeded = limit && people.length > limit;

        if (limitExceeded) {
            people = people.slice(0);
            people.length = Math.min(limit, people.length);
        }

        require(['peoplecardbuilder'], function (peoplecardbuilder) {

            peoplecardbuilder.buildPeopleCards(people, {
                itemsContainer: castContent,
                coverImage: true,
                serverId: item.ServerId,
                width: 160,
                shape: getPortraitShape()
            });
        });

        if (limitExceeded && !enableScrollX()) {
            page.querySelector('.morePeople').classList.remove('hide');
        } else {
            page.querySelector('.morePeople').classList.add('hide');
        }
    }

    function play(startPosition) {

        MediaController.play({
            items: [currentItem],
            startPositionTicks: startPosition
        });
    }

    function splitVersions(page, params) {

        require(['confirm'], function (confirm) {

            confirm("Are you sure you wish to split the media sources into separate items?", "Split Media Apart").then(function () {

                Dashboard.showLoadingMsg();

                ApiClient.ajax({
                    type: "DELETE",
                    url: ApiClient.getUrl("Videos/" + params.id + "/AlternateSources")

                }).then(function () {

                    Dashboard.hideLoadingMsg();

                    reload(page, params);
                });
            });
        });
    }

    function playTrailer(page) {

        ApiClient.getLocalTrailers(Dashboard.getCurrentUserId(), currentItem.Id).then(function (trailers) {

            MediaController.play({ items: trailers });

        });
    }

    function showPlayMenu(item, target) {

        require(['playMenu'], function (playMenu) {

            playMenu.show({

                item: item,
                positionTo: target
            });
        });
    }

    function playCurrentItem(button) {

        if (currentItem.Type == 'Program') {

            ApiClient.getLiveTvChannel(currentItem.ChannelId, Dashboard.getCurrentUserId()).then(function (channel) {

                showPlayMenu(channel, button);
            });

            return;
        }

        showPlayMenu(currentItem, button);
    }

    function deleteTimer(page, params, id) {

        require(['confirm'], function (confirm) {

            confirm(Globalize.translate('MessageConfirmRecordingCancellation'), Globalize.translate('HeaderConfirmRecordingCancellation')).then(function () {

                Dashboard.showLoadingMsg();

                ApiClient.cancelLiveTvTimer(id).then(function () {

                    require(['toast'], function (toast) {
                        toast(Globalize.translate('MessageRecordingCancelled'));
                    });

                    reload(page, params);
                });
            });
        });
    }

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

    function onPlayClick() {
        playCurrentItem(this);
    }

    function onSyncClick() {
        require(['syncDialog'], function (syncDialog) {
            syncDialog.showMenu({
                items: [currentItem],
                serverId: ApiClient.serverId()
            });
        });
    }

    return function (view, params) {

        function resetSyncStatus() {
            updateSyncStatus(view, currentItem);
        }

        function onSyncLocalClick() {

            if (this.checked) {
                require(['syncDialog'], function (syncDialog) {
                    syncDialog.showMenu({
                        items: [currentItem],
                        isLocalSync: true,
                        serverId: ApiClient.serverId()

                    }).then(function () {
                        reload(view, params);
                    }, resetSyncStatus);
                });
            } else {

                require(['confirm'], function (confirm) {

                    confirm(Globalize.translate('ConfirmRemoveDownload')).then(function () {
                        ApiClient.cancelSyncItems([currentItem.Id]);
                    }, resetSyncStatus);
                });
            }
        }

        function onPlayTrailerClick() {
            playTrailer(view);
        }

        function onRecordClick() {
            var id = params.id;
            Dashboard.showLoadingMsg();

            require(['recordingCreator'], function (recordingCreator) {
                recordingCreator.show(id, currentItem.ServerId).then(function () {
                    reload(view, params);
                });
            });
        }

        function onCancelRecordingClick() {
            deleteTimer(view, params, currentItem.TimerId);
        }

        function onMoreCommandsClick() {
            var button = this;

            itemContextMenu.show(getContextMenuOptions(currentItem, button)).then(function (result) {

                if (result.deleted) {
                    Emby.Page.goHome();

                } else if (result.updated) {
                    reload(view, params);
                }
            });
        }

        var elems = view.querySelectorAll('.btnPlay');
        var i, length;
        for (i = 0, length = elems.length; i < length; i++) {
            elems[i].addEventListener('click', onPlayClick);
        }

        elems = view.querySelectorAll('.btnPlayTrailer');
        for (i = 0, length = elems.length; i < length; i++) {
            elems[i].addEventListener('click', onPlayTrailerClick);
        }

        view.querySelector('.btnSplitVersions').addEventListener('click', function () {

            splitVersions(view, params);
        });

        elems = view.querySelectorAll('.btnSync');
        for (i = 0, length = elems.length; i < length; i++) {
            elems[i].addEventListener('click', onSyncClick);
        }

        elems = view.querySelectorAll('.chkOffline');
        for (i = 0, length = elems.length; i < length; i++) {
            elems[i].addEventListener('change', onSyncLocalClick);
        }

        elems = view.querySelectorAll('.btnRecord,.btnFloatingRecord');
        for (i = 0, length = elems.length; i < length; i++) {
            elems[i].addEventListener('click', onRecordClick);
        }

        elems = view.querySelectorAll('.btnCancelRecording');
        for (i = 0, length = elems.length; i < length; i++) {
            elems[i].addEventListener('click', onCancelRecordingClick);
        }

        elems = view.querySelectorAll('.btnMoreCommands');
        for (i = 0, length = elems.length; i < length; i++) {
            elems[i].addEventListener('click', onMoreCommandsClick);
        }

        view.addEventListener('click', function (e) {

            if (dom.parentWithClass(e.target, 'moreScenes')) {
                Dashboard.getCurrentUser().then(function (user) {
                    renderScenes(view, currentItem, user);
                });
            }
            else if (dom.parentWithClass(e.target, 'morePeople')) {
                renderCast(view, currentItem, params.context);
            }
            else if (dom.parentWithClass(e.target, 'moreSpecials')) {
                Dashboard.getCurrentUser().then(function (user) {
                    renderSpecials(view, currentItem, user);
                });
            }
            else if (dom.parentWithClass(e.target, 'moreCriticReviews')) {
                renderCriticReviews(view, currentItem);
            }
        });

        view.querySelector('.collectionItems').addEventListener('needsrefresh', function (e) {

            renderChildren(view, currentItem);
        });

        function editImages() {
            return new Promise(function (resolve, reject) {

                require(['components/imageeditor/imageeditor'], function (ImageEditor) {

                    ImageEditor.show(currentItem.Id).then(resolve, reject);
                });
            });
        }

        view.querySelector('.detailImageContainer').addEventListener('click', function (e) {
            var itemDetailGalleryLink = dom.parentWithClass(e.target, 'itemDetailGalleryLink');
            if (itemDetailGalleryLink) {
                editImages().then(function () {
                    reload(view, params);
                });
            }
        });

        function onWebSocketMessage(e, data) {

            var msg = data;

            if (msg.MessageType === "UserDataChanged") {

                if (currentItem && msg.Data.UserId == Dashboard.getCurrentUserId()) {

                    var key = currentItem.UserData.Key;

                    var userData = msg.Data.UserDataList.filter(function (u) {

                        return u.Key == key;
                    })[0];

                    if (userData) {

                        currentItem.UserData = userData;

                        Dashboard.getCurrentUser().then(function (user) {

                            refreshImage(view, currentItem, user);
                        });
                    }
                }
            }

        }

        view.querySelector('.chapterSettingsButton').addEventListener('click', function () {
            Dashboard.navigate('librarysettings.html');
        });

        view.addEventListener('viewbeforeshow', function () {
            var page = this;
            reload(page, params);

            Events.on(ApiClient, 'websocketmessage', onWebSocketMessage);
        });

        view.addEventListener('viewbeforehide', function () {

            currentItem = null;

            Events.off(ApiClient, 'websocketmessage', onWebSocketMessage);
            LibraryMenu.setTransparentMenu(false);
        });
    };
});