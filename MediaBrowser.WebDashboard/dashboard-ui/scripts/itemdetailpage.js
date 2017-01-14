define(['layoutManager', 'cardBuilder', 'datetime', 'mediaInfo', 'backdrop', 'listView', 'itemContextMenu', 'itemHelper', 'userdataButtons', 'dom', 'indicators', 'apphost', 'imageLoader', 'libraryMenu', 'globalize', 'browser', 'events', 'scrollHelper', 'playbackManager', 'scrollStyles', 'emby-itemscontainer', 'emby-checkbox'], function (layoutManager, cardBuilder, datetime, mediaInfo, backdrop, listView, itemContextMenu, itemHelper, userdataButtons, dom, indicators, appHost, imageLoader, libraryMenu, globalize, browser, events, scrollHelper, playbackManager) {
    'use strict';

    function getPromise(params) {

        var id = params.id;

        if (id) {
            return ApiClient.getItem(Dashboard.getCurrentUserId(), id);
        }

        if (params.seriesTimerId) {
            return ApiClient.getLiveTvSeriesTimer(params.seriesTimerId);
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

    var currentItem;
    var currentRecordingFields;

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
            playAllFromHere: false,
            queueAllFromHere: false,
            positionTo: button,
            cancelTimer: false,
            record: false,
            deleteItem: item.IsFolder === true
        };

        if (appHost.supports('sync')) {
            // Will be displayed via button
            options.syncLocal = false;
        }

        return options;
    }

    function renderSyncLocalContainer(page, params, user, item) {

        if (page.syncToggleInstance) {
            page.syncToggleInstance.refresh(item);
            return;
        }

        require(['syncToggle'], function (syncToggle) {

            page.syncToggleInstance = new syncToggle({
                user: user,
                item: item,
                container: page.querySelector('.syncLocalContainer')
            });

            events.on(page.syncToggleInstance, 'sync', function () {
                reload(page, params);
            });
        });
    }

    function getProgramScheduleHtml(items, options) {

        options = options || {};

        var html = '';
        html += '<div is="emby-itemscontainer" class="itemsContainer vertical-list" data-contextmenu="false">';
        html += listView.getListViewHtml({
            items: items,
            enableUserDataButtons: false,
            image: false,
            showProgramDateTime: true,
            mediaInfo: false,
            action: 'none',
            moreButton: false,
            recordButton: false
        });

        html += '</div>';

        return html;
    }

    function renderSeriesTimerSchedule(page, seriesTimerId) {

        ApiClient.getLiveTvTimers({
            UserId: ApiClient.getCurrentUserId(),
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Thumb",
            SortBy: "StartDate",
            EnableTotalRecordCount: false,
            EnableUserData: false,
            SeriesTimerId: seriesTimerId,
            Fields: "ChannelInfo"

        }).then(function (result) {

            if (result.Items.length && result.Items[0].SeriesTimerId != seriesTimerId) {
                result.Items = [];
            }

            var html = getProgramScheduleHtml(result.Items);

            var scheduleTab = page.querySelector('.seriesTimerSchedule');
            scheduleTab.innerHTML = html;

            imageLoader.lazyChildren(scheduleTab);
        });
    }

    function renderSeriesTimerEditor(page, item, user) {

        if (item.Type !== 'SeriesTimer') {
            return;
        }

        if (!user.Policy.EnableLiveTvManagement) {
            page.querySelector('.seriesTimerScheduleSection').classList.add('hide');
            page.querySelector('.btnCancelSeriesTimer').classList.add('hide');
            return;
        }

        require(['seriesRecordingEditor'], function (seriesRecordingEditor) {
            seriesRecordingEditor.embed(item, ApiClient.serverId(), {
                context: page.querySelector('.seriesRecordingEditor')
            });
        });

        page.querySelector('.seriesTimerScheduleSection').classList.remove('hide');
        page.querySelector('.btnCancelSeriesTimer').classList.remove('hide');

        renderSeriesTimerSchedule(page, item.Id);
    }

    function reloadFromItem(page, params, item) {

        currentItem = item;

        var context = params.context;

        LibraryBrowser.renderName(item, page.querySelector('.itemName'), false, context);
        LibraryBrowser.renderParentName(item, page.querySelector('.parentName'), context);
        libraryMenu.setTitle('');

        Dashboard.getCurrentUser().then(function (user) {

            window.scrollTo(0, 0);

            renderSeriesTimerEditor(page, item, user);

            renderImage(page, item, user);
            renderLogo(page, item, ApiClient);

            setInitialCollapsibleState(page, item, context, user);
            renderDetails(page, item, context);

            if (dom.getWindowSize().innerWidth >= 800) {
                backdrop.setBackdrops([item]);
            } else {
                backdrop.clear();
            }

            LibraryBrowser.renderDetailPageBackdrop(page, item, imageLoader);

            libraryMenu.setTransparentMenu(true);

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
            else if (playbackManager.canPlay(item)) {
                hideAll(page, 'btnPlay', true);
                canPlay = true;
            }
            else {
                hideAll(page, 'btnPlay');
            }

            var hasAnyButton = canPlay;

            if ((item.LocalTrailerCount || (item.RemoteTrailers && item.RemoteTrailers.length)) && item.PlayAccess == 'Full') {
                hideAll(page, 'btnPlayTrailer', true);
                hasAnyButton = true;
            } else {
                hideAll(page, 'btnPlayTrailer');
            }

            if (item.CanDelete && !item.IsFolder) {
                hideAll(page, 'btnDeleteItem', true);
                hasAnyButton = true;
            } else {
                hideAll(page, 'btnDeleteItem');
            }

            renderSyncLocalContainer(page, params, user, item);

            if (hasAnyButton || item.Type !== 'Program') {
                hideAll(page, 'mainDetailButtons', true);
            } else {
                hideAll(page, 'mainDetailButtons');
            }

            showRecordingFields(page, item, user);

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

            var itemBirthday = page.querySelector('#itemBirthday');
            if (item.Type == "Person" && item.PremiereDate) {

                try {
                    var birthday = datetime.parseISO8601Date(item.PremiereDate, true).toDateString();

                    itemBirthday.classList.remove('hide');
                    itemBirthday.innerHTML = globalize.translate('BirthDateValue').replace('{0}', birthday);
                }
                catch (err) {
                    itemBirthday.classList.add('hide');
                }
            } else {
                itemBirthday.classList.add('hide');
            }

            var itemDeathDate = page.querySelector('#itemDeathDate');
            if (item.Type == "Person" && item.EndDate) {

                try {
                    var deathday = datetime.parseISO8601Date(item.EndDate, true).toDateString();

                    itemDeathDate.classList.remove('hide');
                    itemDeathDate.innerHTML = globalize.translate('DeathDateValue').replace('{0}', deathday);
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
                itemBirthLocation.innerHTML = globalize.translate('BirthPlaceValue').replace('{0}', gmap);
            } else {
                itemBirthLocation.classList.add('hide');
            }
        });

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

    function logoImageUrl(item, apiClient, options) {

        options = options || {};
        options.type = "Logo";

        if (item.ImageTags && item.ImageTags.Logo) {

            options.tag = item.ImageTags.Logo;
            return apiClient.getScaledImageUrl(item.Id, options);
        }

        if (item.ParentLogoImageTag) {
            options.tag = item.ParentLogoImageTag;
            return apiClient.getScaledImageUrl(item.ParentLogoItemId, options);
        }

        return null;
    }

    function bounceIn(elem) {
        var keyframes = [
          { transform: 'scale3d(.3, .3, .3)', opacity: '0', offset: 0 },
          { transform: 'scale3d(1.1, 1.1, 1.1)', offset: 0.2 },
          { transform: 'scale3d(.9, .9, .9)', offset: 0.4 },
          { transform: 'scale3d(1.03, 1.03, 1.03)', opacity: '1', offset: 0.6 },
          { transform: 'scale3d(.97, .97, .97)', offset: 0.8 },
          { transform: 'scale3d(1, 1, 1)', opacity: '1', offset: 1 }];
        var timing = { duration: 900, iterations: 1, easing: 'cubic-bezier(0.215, 0.610, 0.355, 1.000)' };
        return elem.animate(keyframes, timing);
    }

    function renderLogo(page, item, apiClient) {
        var url = logoImageUrl(item, apiClient, {
            maxWidth: 300
        });

        var detailLogo = page.querySelector('.detailLogo');

        if (url) {
            detailLogo.classList.remove('hide');
            detailLogo.classList.add('lazy');
            detailLogo.setAttribute('data-src', url);
            imageLoader.lazyImage(detailLogo);

            //if (detailLogo.animate) {
            //    setTimeout(function() {
            //        bounceIn(detailLogo);
            //    }, 100);
            //}

        } else {
            detailLogo.classList.add('hide');
        }
    }

    function showRecordingFields(page, item, user) {

        if (currentRecordingFields) {
            return;
        }

        var recordingFieldsElement = page.querySelector('.recordingFields');

        if (item.Type == 'Program' && user.Policy.EnableLiveTvManagement) {

            require(['recordingFields'], function (recordingFields) {

                currentRecordingFields = new recordingFields({
                    parent: recordingFieldsElement,
                    programId: item.Id,
                    serverId: item.ServerId
                });
                recordingFieldsElement.classList.remove('hide');
            });
        } else {
            recordingFieldsElement.classList.add('hide');
            recordingFieldsElement.innerHTML = '';
        }
    }

    function renderLinks(linksElem, item) {

        var links = [];

        if (item.HomePageUrl) {
            links.push('<a class="textlink" href="' + item.HomePageUrl + '" target="_blank">' + globalize.translate('ButtonWebsite') + '</a>');
        }

        if (item.ExternalUrls) {

            for (var i = 0, length = item.ExternalUrls.length; i < length; i++) {

                var url = item.ExternalUrls[i];

                links.push('<a class="textlink" href="' + url.Url + '" target="_blank">' + url.Name + '</a>');
            }
        }

        if (links.length) {

            var html = links.join('<span class="bulletSeparator">&bull;</span>');

            linksElem.innerHTML = html;
            linksElem.classList.remove('hide');

        } else {
            linksElem.classList.add('hide');
        }
    }

    function shadeBlendConvert(p, from, to) {
        if (typeof (p) != "number" || p < -1 || p > 1 || typeof (from) != "string" || (from[0] != 'r' && from[0] != '#') || (typeof (to) != "string" && typeof (to) != "undefined")) return null; //ErrorCheck

        var sbcRip = function (d) {
            var l = d.length, RGB = new Object();
            if (l > 9) {
                d = d.split(",");
                if (d.length < 3 || d.length > 4) return null; //ErrorCheck
                RGB[0] = i(d[0].slice(4)), RGB[1] = i(d[1]), RGB[2] = i(d[2]), RGB[3] = d[3] ? parseFloat(d[3]) : -1;
            } else {
                if (l == 8 || l == 6 || l < 4) return null; //ErrorCheck
                if (l < 6) d = "#" + d[1] + d[1] + d[2] + d[2] + d[3] + d[3] + (l > 4 ? d[4] + "" + d[4] : ""); //3 digit
                d = i(d.slice(1), 16), RGB[0] = d >> 16 & 255, RGB[1] = d >> 8 & 255, RGB[2] = d & 255, RGB[3] = l == 9 || l == 5 ? r(((d >> 24 & 255) / 255) * 10000) / 10000 : -1;
            }
            return RGB;
        };

        var i = parseInt, r = Math.round, h = from.length > 9, h = typeof (to) == "string" ? to.length > 9 ? true : to == "c" ? !h : false : h, b = p < 0, p = b ? p * -1 : p, to = to && to != "c" ? to : b ? "#000000" : "#FFFFFF", f = sbcRip(from), t = sbcRip(to);
        if (!f || !t) return null; //ErrorCheck
        if (h) return "rgb(" + r((t[0] - f[0]) * p + f[0]) + "," + r((t[1] - f[1]) * p + f[1]) + "," + r((t[2] - f[2]) * p + f[2]) + (f[3] < 0 && t[3] < 0 ? ")" : "," + (f[3] > -1 && t[3] > -1 ? r(((t[3] - f[3]) * p + f[3]) * 10000) / 10000 : t[3] < 0 ? f[3] : t[3]) + ")");
        else return "#" + (0x100000000 + (f[3] > -1 && t[3] > -1 ? r(((t[3] - f[3]) * p + f[3]) * 255) : t[3] > -1 ? r(t[3] * 255) : f[3] > -1 ? r(f[3] * 255) : 255) * 0x1000000 + r((t[0] - f[0]) * p + f[0]) * 0x10000 + r((t[1] - f[1]) * p + f[1]) * 0x100 + r((t[2] - f[2]) * p + f[2])).toString(16).slice(f[3] > -1 || t[3] > -1 ? 1 : 3);
    }

    function loadSwatch(page, item) {

        var imageTags = item.ImageTags || {};

        if (item.PrimaryImageTag) {
            imageTags.Primary = item.PrimaryImageTag;
        }

        var url;
        var imageHeight = 300;

        if (item.SeriesId && item.SeriesPrimaryImageTag) {

            url = ApiClient.getScaledImageUrl(item.SeriesId, {
                type: "Primary",
                maxHeight: imageHeight,
                tag: item.SeriesPrimaryImageTag
            });
        }
        else if (imageTags.Primary) {

            url = ApiClient.getScaledImageUrl(item.Id, {
                type: "Primary",
                maxHeight: imageHeight,
                tag: item.ImageTags.Primary
            });
        }

        if (!url) {
            return;
        }

        var img = new Image();
        img.onload = function () {

            imageLoader.getVibrantInfoFromElement(img, url).then(function (vibrantInfo) {

                vibrantInfo = vibrantInfo.split('|');
                var detailPageContent = page.querySelector('.detailPageContent');
                var detailPagePrimaryContainer = page.querySelector('.detailPagePrimaryContainer');

                detailPageContent.style.color = vibrantInfo[1];

                detailPagePrimaryContainer.style.backgroundColor = vibrantInfo[0];

            });
        };

        img.src = url;
    }

    function renderImage(page, item, user) {

        var container = page.querySelector('.detailImageContainer');

        LibraryBrowser.renderDetailImage(container, item, user.Policy.IsAdministrator && item.MediaType != 'Photo', null, imageLoader, indicators);

        //loadSwatch(page, item);
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
            page.querySelector('#peopleHeader').innerHTML = globalize.translate('HeaderPeople');
        } else {
            page.querySelector('#peopleHeader').innerHTML = globalize.translate('HeaderCastAndCrew');
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
            imageLoader.lazyChildren(itemsContainer);
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
        else if (item.IsFolder || item.Type == 'Episode') {

            if (item.Type == "BoxSet") {
                page.querySelector('#childrenCollapsible').classList.add('hide');
            }
            renderChildren(page, item);
        }
        else {
            page.querySelector('#childrenCollapsible').classList.add('hide');
        }

        if (item.Type == 'Series') {

            renderSeriesSchedule(page, item, user);
        }

        if (item.Type == 'Series') {

            renderNextUp(page, item, user);
        } else {
            page.querySelector('.nextUpSection').classList.add('hide');
        }

        if (item.MediaSources && item.MediaSources.length) {
            renderMediaSources(page, user, item);
        }

        renderScenes(page, item);

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

                elem.classList.remove('hide');

                var anchors = elem.querySelectorAll('a');
                for (var j = 0, length2 = anchors.length; j < length2; j++) {
                    anchors[j].setAttribute("target", "_blank");
                }

            } else {
                elem.innerHTML = '';

                elem.classList.add('hide');
            }
        }
    }

    function renderDetails(page, item, context, isStatic) {

        renderSimilarItems(page, item, context);
        renderMoreFromItems(page, item);

        var taglineElement = page.querySelector('.tagline');

        if (item.Taglines && item.Taglines.length) {
            taglineElement.classList.remove('hide');
            taglineElement.innerHTML = item.Taglines[0];
        } else {
            taglineElement.classList.add('hide');
        }

        var overview = page.querySelector('.overview');
        var externalLinksElem = page.querySelector('.itemExternalLinks');

        if (item.Type === 'Season' || item.Type === 'MusicAlbum' || item.Type === 'MusicArtist') {
            overview.classList.add('detailsHiddenOnMobile');
            externalLinksElem.classList.add('detailsHiddenOnMobile');
        }

        renderOverview([overview], item);

        renderAwardSummary(page.querySelector('#awardSummary'), item);

        var i, length;
        var itemMiscInfo = page.querySelectorAll('.itemMiscInfo-primary');
        for (i = 0, length = itemMiscInfo.length; i < length; i++) {
            mediaInfo.fillPrimaryMediaInfo(itemMiscInfo[i], item, {
                interactive: true,
                episodeTitle: false
            });
            if (itemMiscInfo[i].innerHTML) {
                itemMiscInfo[i].classList.remove('hide');
            } else {
                itemMiscInfo[i].classList.add('hide');
            }
        }
        itemMiscInfo = page.querySelectorAll('.itemMiscInfo-secondary');
        for (i = 0, length = itemMiscInfo.length; i < length; i++) {
            mediaInfo.fillSecondaryMediaInfo(itemMiscInfo[i], item, {
                interactive: true
            });
            if (itemMiscInfo[i].innerHTML) {
                itemMiscInfo[i].classList.remove('hide');
            } else {
                itemMiscInfo[i].classList.add('hide');
            }
        }
        var itemGenres = page.querySelectorAll('.itemGenres');
        for (i = 0, length = itemGenres.length; i < length; i++) {
            renderGenres(itemGenres[i], item, null, isStatic);
        }

        renderStudios(page.querySelector('.itemStudios'), item, isStatic);
        renderUserDataIcons(page, item);
        renderLinks(externalLinksElem, item);

        page.querySelector('.criticRatingScore').innerHTML = (item.CriticRating || '0') + '%';

        if (item.CriticRatingSummary) {
            page.querySelector('#criticRatingSummary').classList.remove('hide');
            page.querySelector('.criticRatingSummaryText').innerHTML = item.CriticRatingSummary;

        } else {
            page.querySelector('#criticRatingSummary').classList.add('hide');
        }

        renderTags(page, item);

        renderSeriesAirTime(page, item, isStatic);

        if (renderDynamicMediaIcons(page, item)) {
            page.querySelector('.mediaInfoIcons').classList.remove('hide');
        } else {
            page.querySelector('.mediaInfoIcons').classList.add('hide');
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
    }

    function renderDynamicMediaIcons(view, item) {

        var html = mediaInfo.getMediaInfoStats(item).map(function (mediaInfoItem) {

            var text = mediaInfoItem.text;

            if (mediaInfoItem.type === 'added') {
                return '<div class="mediaInfoText">' + text + '</div>';
            }

            return '<div class="mediaInfoText mediaInfoText-upper">' + text + '</div>';

        }).join('');

        view.querySelector('.mediaInfoIcons').innerHTML = html;

        return html;
    }

    function renderPhotoInfo(page, item) {

        var html = '';

        var attributes = [];

        if (item.CameraMake) {
            attributes.push(createAttribute(globalize.translate('MediaInfoCameraMake'), item.CameraMake));
        }

        if (item.CameraModel) {
            attributes.push(createAttribute(globalize.translate('MediaInfoCameraModel'), item.CameraModel));
        }

        if (item.Altitude) {
            attributes.push(createAttribute(globalize.translate('MediaInfoAltitude'), item.Altitude.toFixed(1)));
        }

        if (item.Aperture) {
            attributes.push(createAttribute(globalize.translate('MediaInfoAperture'), 'F' + item.Aperture.toFixed(1)));
        }

        if (item.ExposureTime) {

            var val = 1 / item.ExposureTime;

            attributes.push(createAttribute(globalize.translate('MediaInfoExposureTime'), '1/' + val + ' s'));
        }

        if (item.FocalLength) {
            attributes.push(createAttribute(globalize.translate('MediaInfoFocalLength'), item.FocalLength.toFixed(1) + ' mm'));
        }

        if (item.ImageOrientation) {
            //attributes.push(createAttribute(Globalize.translate('MediaInfoOrientation'), item.ImageOrientation));
        }

        if (item.IsoSpeedRating) {
            attributes.push(createAttribute(globalize.translate('MediaInfoIsoSpeedRating'), item.IsoSpeedRating));
        }

        if (item.Latitude) {
            attributes.push(createAttribute(globalize.translate('MediaInfoLatitude'), item.Latitude.toFixed(1)));
        }

        if (item.Longitude) {
            attributes.push(createAttribute(globalize.translate('MediaInfoLongitude'), item.Longitude.toFixed(1)));
        }

        if (item.ShutterSpeed) {
            attributes.push(createAttribute(globalize.translate('MediaInfoShutterSpeed'), item.ShutterSpeed));
        }

        if (item.Software) {
            attributes.push(createAttribute(globalize.translate('MediaInfoSoftware'), item.Software));
        }

        html += attributes.join('<br/>');

        page.querySelector('.photoInfoContent').innerHTML = html;
    }

    function getArtistLinksHtml(artists, context) {

        var html = [];

        for (var i = 0, length = artists.length; i < length; i++) {

            var artist = artists[i];

            html.push('<a class="textlink" href="itemdetails.html?id=' + artist.Id + '">' + artist.Name + '</a>');

        }

        html = html.join(' / ');

        if (artists.length == 1) {
            return globalize.translate('ValueArtist', html);
        }
        if (artists.length > 1) {
            return globalize.translate('ValueArtists', html);
        }

        return html;
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

            moreFromSection.querySelector('.moreFromHeader').innerHTML = globalize.translate('MoreFromValue', item.AlbumArtists[0].Name);

            var html = '';

            if (enableScrollX()) {
                html += '<div is="emby-itemscontainer" class="hiddenScrollX itemsContainer">';
            } else {
                html += '<div is="emby-itemscontainer" class="itemsContainer vertical-wrap">';
            }

            var shape = item.Type == "MusicAlbum" || item.Type == "MusicArtist" ? getSquareShape() : getPortraitShape();

            var supportsImageAnalysis = appHost.supports('imageanalysis');

            html += cardBuilder.getCardsHtml({
                items: result.Items,
                shape: shape,
                showParentTitle: item.Type == "MusicAlbum",
                centerText: !supportsImageAnalysis,
                showTitle: item.Type == "MusicAlbum" || item.Type == "Game" || item.Type == "MusicArtist",
                coverImage: item.Type == "MusicAlbum" || item.Type == "MusicArtist",
                overlayPlayButton: true,
                cardLayout: supportsImageAnalysis,
                vibrant: supportsImageAnalysis
            });
            html += '</div>';

            var similarContent = page.querySelector('#moreFromItems');
            similarContent.innerHTML = html;
            imageLoader.lazyChildren(similarContent);
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
            limit: item.Type == "MusicAlbum" || item.Type == "MusicArtist" ? 8 : 10,
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

            var html = '';

            if (enableScrollX()) {
                html += '<div is="emby-itemscontainer" class="hiddenScrollX itemsContainer">';
            } else {
                html += '<div is="emby-itemscontainer" class="itemsContainer vertical-wrap">';
            }

            var supportsImageAnalysis = appHost.supports('imageanalysis');
            var cardLayout = supportsImageAnalysis && (item.Type == "MusicAlbum" || item.Type == "Game" || item.Type == "MusicArtist");

            html += cardBuilder.getCardsHtml({
                items: result.Items,
                shape: shape,
                showParentTitle: item.Type == "MusicAlbum",
                centerText: !cardLayout,
                showTitle: item.Type == "MusicAlbum" || item.Type == "Game" || item.Type == "MusicArtist",
                context: context,
                lazy: true,
                showDetailsMenu: true,
                coverImage: item.Type == "MusicAlbum" || item.Type == "MusicArtist",
                overlayPlayButton: true,
                cardLayout: cardLayout,
                vibrant: cardLayout && supportsImageAnalysis
            });
            html += '</div>';

            var similarContent = similarCollapsible.querySelector('.similarContent');
            similarContent.innerHTML = html;
            imageLoader.lazyChildren(similarContent);
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

                var context = inferContext(item);

                var href = LibraryBrowser.getHref(item.Studios[0], context);
                html += ' on <a class="textlink" href="' + href + '">' + item.Studios[0].Name + '</a>';
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
        else if (item.Type == "Episode" && item.SeriesId && item.SeasonId) {

            // Use dedicated episodes endpoint
            promise = ApiClient.getEpisodes(item.SeriesId, {

                seasonId: item.SeasonId,
                userId: userId,
                Fields: fields
            });

            _childrenItemsFunction = getEpisodesFunction(item.SeriesId, {

                seasonId: item.SeasonId,
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

            var scrollClass = 'hiddenScrollX';
            var childrenItemsContainer = page.querySelector('.childrenItemsContainer');

            if (item.Type == "MusicAlbum") {

                html = listView.getListViewHtml({
                    items: result.Items,
                    smallIcon: true,
                    showIndex: true,
                    index: 'disc',
                    showIndexNumber: true,
                    playFromHere: true,
                    action: 'playallfromhere',
                    image: false,
                    artist: 'auto',
                    containerAlbumArtist: item.AlbumArtist,
                    addToListButton: true
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
            else if (item.Type == "Season" || item.Type == "Episode") {

                if (item.Type === 'Episode') {
                    childrenItemsContainer.classList.add('darkScroller');
                }

                scrollX = item.Type == "Episode";
                if (!browser.touch) {
                    scrollClass = 'smoothScrollX';
                }

                if (result.Items.length == 1 && item.Type === 'Episode') {

                    return;

                } else {
                    html = cardBuilder.getCardsHtml({
                        items: result.Items,
                        shape: getThumbShape(scrollX),
                        showTitle: true,
                        displayAsSpecial: item.Type == "Season" && item.IndexNumber,
                        playFromHere: true,
                        overlayText: true,
                        lazy: true,
                        showDetailsMenu: true,
                        overlayPlayButton: true,
                        allowBottomPadding: !scrollX,
                        includeParentInfoInTitle: false
                    });
                }
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

            page.querySelector('#childrenCollapsible').classList.remove('hide');

            if (scrollX) {
                childrenItemsContainer.classList.add(scrollClass);
                childrenItemsContainer.classList.remove('vertical-wrap');
                childrenItemsContainer.classList.remove('vertical-list');
            } else {
                childrenItemsContainer.classList.remove('hiddenScrollX');
                childrenItemsContainer.classList.remove('smoothScrollX');

                if (isList) {
                    childrenItemsContainer.classList.add('vertical-list');
                    childrenItemsContainer.classList.remove('vertical-wrap');
                } else {
                    childrenItemsContainer.classList.add('vertical-wrap');
                    childrenItemsContainer.classList.remove('vertical-list');
                }
            }

            childrenItemsContainer.innerHTML = html;
            imageLoader.lazyChildren(childrenItemsContainer);

            if (item.Type == "BoxSet") {

                var collectionItemTypes = [
                    { name: globalize.translate('HeaderMovies'), type: 'Movie' },
                    { name: globalize.translate('HeaderSeries'), type: 'Series' },
                    { name: globalize.translate('HeaderAlbums'), type: 'MusicAlbum' },
                    { name: globalize.translate('HeaderGames'), type: 'Game' },
                    { name: globalize.translate('HeaderBooks'), type: 'Book' }
                ];

                renderCollectionItems(page, item, collectionItemTypes, result.Items);
            }
            else if (item.Type === 'Episode') {

                var card = childrenItemsContainer.querySelector('.card[data-id="' + item.Id + '"]');
                if (card) {
                    scrollHelper.toStart(childrenItemsContainer, card.previousSibling || card, true);
                }
            }
        });

        if (item.Type == "Season") {
            page.querySelector('#childrenTitle').innerHTML = globalize.translate('HeaderEpisodes');
        }
        else if (item.Type == "Episode") {
            page.querySelector('#childrenTitle').innerHTML = globalize.translate('MoreFromValue', item.SeasonName);
        }
        else if (item.Type == "Series") {
            page.querySelector('#childrenTitle').innerHTML = globalize.translate('HeaderSeasons');
        }
        else if (item.Type == "MusicAlbum") {
            page.querySelector('#childrenTitle').innerHTML = globalize.translate('HeaderTracks');
        }
        else if (item.Type == "GameSystem") {
            page.querySelector('#childrenTitle').innerHTML = globalize.translate('HeaderGames');
        }
        else {
            page.querySelector('#childrenTitle').innerHTML = globalize.translate('HeaderItems');
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

        require('scripts/livetvchannel,scripts/livetvcomponents,livetvcss'.split(','), function (liveTvChannelPage) {

            liveTvChannelPage.renderPrograms(page, item.Id);
        });
    }

    function renderSeriesSchedule(page, item, user) {

        return;
        ApiClient.getLiveTvPrograms({

            UserId: Dashboard.getCurrentUserId(),
            HasAired: false,
            SortBy: "StartDate",
            EnableTotalRecordCount: false,
            EnableImages: false,
            ImageTypeLimit: 0,
            Limit: 50,
            EnableUserData: false,
            LibrarySeriesId: item.Id

        }).then(function (result) {

            if (result.Items.length) {
                page.querySelector('#seriesScheduleSection').classList.remove('hide');

            } else {
                page.querySelector('#seriesScheduleSection').classList.add('hide');
            }

            page.querySelector('#seriesScheduleList').innerHTML = listView.getListViewHtml({
                items: result.Items,
                enableUserDataButtons: false,
                showParentTitle: false,
                image: false,
                showProgramDateTime: true,
                mediaInfo: false,
                showTitle: true,
                moreButton: false,
                action: 'programdialog'
            });

            Dashboard.hideLoadingMsg();
        });
    }

    function inferContext(item) {

        if (item.Type == 'Movie' || item.Type == 'BoxSet') {
            return 'movies';
        }
        if (item.Type == 'Series' || item.Type == 'Season' || item.Type == 'Episode') {
            return 'tvshows';
        }
        if (item.Type == 'Game' || item.Type == 'GameSystem') {
            return 'games';
        }
        if (item.Type == 'Game' || item.Type == 'GameSystem') {
            return 'games';
        }
        if (item.Type == 'MusicArtist' || item.Type == 'MusicAlbum') {
            return 'music';
        }

        return null;
    }

    function renderStudios(elem, item, isStatic) {

        var context = inferContext(item);

        if (item.Studios && item.Studios.length && item.Type != "Series" && false) {

            var html = '';

            for (var i = 0, length = item.Studios.length; i < length; i++) {

                if (i > 0) {
                    html += '&nbsp;&nbsp;/&nbsp;&nbsp;';
                }

                if (isStatic) {
                    html += item.Studios[i].Name;
                } else {

                    item.Studios[i].Type = 'Studio';
                    var href = LibraryBrowser.getHref(item.Studios[i], context);
                    html += '<a class="textlink" href="' + href + '">' + item.Studios[i].Name + '</a>';
                }
            }

            var translationKey = item.Studios.length > 1 ? "ValueStudios" : "ValueStudio";

            html = globalize.translate(translationKey, html);

            elem.innerHTML = html;
            elem.classList.remove('hide');

        } else {
            elem.classList.add('hide');
        }
    }

    function renderGenres(elem, item, limit, isStatic) {

        var context = inferContext(item);

        var html = '';

        var genres = item.Genres || [];

        for (var i = 0, length = genres.length; i < length; i++) {

            if (limit && i >= limit) {
                break;
            }

            if (i > 0) {
                html += '<span class="bulletSeparator">&bull;</span>';
            }

            var param = item.Type == "Audio" || item.Type == "MusicArtist" || item.Type == "MusicAlbum" ? "musicgenre" : "genre";

            if (item.MediaType == "Game") {
                param = "gamegenre";
            }

            if (isStatic) {
                html += genres[i];
            } else {

                var type;
                switch (context) {
                    case 'tvshows':
                        type = 'Series';
                        break;
                    case 'games':
                        type = 'Game';
                        break;
                    case 'music':
                        type = 'MusicAlbum';
                        break;
                    default:
                        type = 'Movie';
                        break;
                }

                var url = "secondaryitems.html?type=" + type + "&" + param + "=" + ApiClient.encodeName(genres[i]);

                html += '<a class="textlink" href="' + url + '">' + genres[i] + '</a>';
            }
        }

        elem.innerHTML = html;
    }

    function renderAwardSummary(elem, item) {
        if (item.AwardSummary) {
            elem.classList.remove('hide');
            elem.innerHTML = globalize.translate('ValueAwards', item.AwardSummary);
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

        var otherType = { name: globalize.translate('HeaderOtherItems') };

        var otherTypeItems = items.filter(function (curr) {

            return !types.filter(function (t) {

                return t.type == curr.Type;

            }).length;

        });

        if (otherTypeItems.length) {
            renderCollectionItemType(page, parentItem, otherType, otherTypeItems);
        }

        if (!items.length) {
            renderCollectionItemType(page, parentItem, { name: globalize.translate('HeaderItems') }, items);
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
        imageLoader.lazyChildren(collectionItems);

        collectionItems.querySelector('.btnAddToCollection').addEventListener('click', function () {
            require(['alert'], function (alert) {
                alert({
                    text: globalize.translate('AddItemToCollectionHelp'),
                    html: globalize.translate('AddItemToCollectionHelp') + '<br/><br/><a target="_blank" href="https://github.com/MediaBrowser/Wiki/wiki/Collections">' + globalize.translate('ButtonLearnMore') + '</a>'
                });
            });
        });
    }

    function renderUserDataIcons(page, item) {

        var userDataIcons = page.querySelectorAll('.userDataIcons');

        for (var i = 0, length = userDataIcons.length; i < length; i++) {

            if (item.Type == 'Program' || item.Type == 'SeriesTimer') {
                userDataIcons[i].classList.add('hide');
            } else {
                userDataIcons[i].classList.remove('hide');
            }

            userdataButtons.fill({
                item: item,
                style: 'fab-mini',
                element: userDataIcons[i]
            });
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
                    html += '<div style="flex-shrink:0;background-color:transparent;background-image:url(\'css/images/fresh.png\');background-repeat:no-repeat;background-position:center center;background-size: cover;width:40px;height:40px;"></div>';
                } else {
                    html += '<div style="flex-shrink:0;background-color:transparent;background-image:url(\'css/images/rotten.png\');background-repeat:no-repeat;background-position:center center;background-size: cover;width:40px;height:40px;"></div>';
                }
            }

            html += '<div class="listItemBody two-line">';

            html += '<h3 class="listItemBodyText" style="white-space:normal;">' + review.Caption + '</h3>';

            var vals = [];

            if (review.ReviewerName) {
                vals.push(review.ReviewerName);
            }
            if (review.Publisher) {
                vals.push(review.Publisher);
            }

            html += '<div class="secondary listItemBodyText">' + vals.join(', ') + '.';
            if (review.Date) {

                try {

                    var date = datetime.toLocaleDateString(datetime.parseISO8601Date(review.Date, true));

                    html += '<span class="reviewDate">' + date + '</span>';
                }
                catch (error) {

                }

            }
            html += '</div>';

            if (review.Url) {
                html += '<div class="secondary listItemBodyText"><a class="textlink" href="' + review.Url + '" target="_blank">' + globalize.translate('ButtonFullReview') + '</a></div>';
            }

            html += '</div>';

            html += '</div>';
            html += '</div>';
        }

        if (limit && result.TotalRecordCount > limit) {
            html += '<p style="margin: 0;"><button is="emby-button" type="button" class="raised more moreCriticReviews">' + globalize.translate('ButtonMore') + '</button></p>';
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

        if (item.Type === 'SeriesTimer' || item.Type === 'Timer' || item.Type === 'Genre' || item.Type === 'MusicGenre' || item.Type === 'GameGenre' || item.Type === 'Studio' || item.Type === 'Person') {
            return;
        }

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
            imageLoader.lazyChildren(themeVideosContent);
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
                imageLoader.lazyChildren(musicVideosContent);

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
                imageLoader.lazyChildren(additionalPartsContent);

            } else {
                page.querySelector('#additionalPartsCollapsible').classList.add('hide');
            }
        });
    }

    function renderScenes(page, item) {

        var chapters = item.Chapters || [];

        // If there are no chapter images, don't show a bunch of empty tiles
        if (chapters.length && !chapters[0].ImageTag) {
            chapters = [];
        }

        if (!chapters.length) {
            page.querySelector('#scenesCollapsible').classList.add('hide');
        } else {
            page.querySelector('#scenesCollapsible').classList.remove('hide');

            var scenesContent = page.querySelector('#scenesContent');

            if (enableScrollX()) {
                scenesContent.classList.add('smoothScrollX');
            } else {
                scenesContent.classList.add('vertical-wrap');
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
        }
    }

    function renderMediaSources(page, user, item) {

        var html = item.MediaSources.map(function (v) {

            return getMediaSourceHtml(user, item, v);

        }).join('<div style="border-top:1px solid #444;margin: 1em 0;"></div>');

        if (item.MediaSources.length > 1) {
            html = '<br/>' + html;
        }

        var mediaInfoContent = page.querySelector('#mediaInfoContent');
        mediaInfoContent.innerHTML = html;
    }

    function getMediaSourceHtml(user, item, version) {

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

            var displayType = globalize.translate('MediaInfoStreamType' + stream.Type);

            html += '<h3 class="mediaInfoStreamType">' + displayType + '</h3>';

            var attributes = [];

            if (stream.Language && stream.Type != "Video") {
                attributes.push(createAttribute(globalize.translate('MediaInfoLanguage'), stream.Language));
            }

            if (stream.Codec) {
                attributes.push(createAttribute(globalize.translate('MediaInfoCodec'), stream.Codec.toUpperCase()));
            }

            if (stream.CodecTag) {
                attributes.push(createAttribute(globalize.translate('MediaInfoCodecTag'), stream.CodecTag));
            }

            if (stream.IsAVC != null) {
                attributes.push(createAttribute('AVC', (stream.IsAVC ? 'Yes' : 'No')));
            }

            if (stream.Profile) {
                attributes.push(createAttribute(globalize.translate('MediaInfoProfile'), stream.Profile));
            }

            if (stream.Level) {
                attributes.push(createAttribute(globalize.translate('MediaInfoLevel'), stream.Level));
            }

            if (stream.Width || stream.Height) {
                attributes.push(createAttribute(globalize.translate('MediaInfoResolution'), stream.Width + 'x' + stream.Height));
            }

            if (stream.AspectRatio && stream.Codec != "mjpeg") {
                attributes.push(createAttribute(globalize.translate('MediaInfoAspectRatio'), stream.AspectRatio));
            }

            if (stream.Type == "Video") {
                if (stream.IsAnamorphic != null) {
                    attributes.push(createAttribute(globalize.translate('MediaInfoAnamorphic'), (stream.IsAnamorphic ? 'Yes' : 'No')));
                }

                attributes.push(createAttribute(globalize.translate('MediaInfoInterlaced'), (stream.IsInterlaced ? 'Yes' : 'No')));
            }

            if (stream.AverageFrameRate || stream.RealFrameRate) {
                attributes.push(createAttribute(globalize.translate('MediaInfoFramerate'), (stream.AverageFrameRate || stream.RealFrameRate)));
            }

            if (stream.ChannelLayout) {
                attributes.push(createAttribute(globalize.translate('MediaInfoLayout'), stream.ChannelLayout));
            }
            if (stream.Channels) {
                attributes.push(createAttribute(globalize.translate('MediaInfoChannels'), stream.Channels + ' ch'));
            }

            if (stream.BitRate && stream.Codec != "mjpeg") {
                attributes.push(createAttribute(globalize.translate('MediaInfoBitrate'), (parseInt(stream.BitRate / 1000)) + ' kbps'));
            }

            if (stream.SampleRate) {
                attributes.push(createAttribute(globalize.translate('MediaInfoSampleRate'), stream.SampleRate + ' Hz'));
            }

            if (stream.BitDepth) {
                attributes.push(createAttribute(globalize.translate('MediaInfoBitDepth'), stream.BitDepth + ' bit'));
            }

            if (stream.PixelFormat) {
                attributes.push(createAttribute(globalize.translate('MediaInfoPixelFormat'), stream.PixelFormat));
            }

            if (stream.RefFrames) {
                attributes.push(createAttribute(globalize.translate('MediaInfoRefFrames'), stream.RefFrames));
            }

            if (stream.NalLengthSize) {
                attributes.push(createAttribute('NAL', stream.NalLengthSize));
            }

            if (stream.Type != "Video") {
                attributes.push(createAttribute(globalize.translate('MediaInfoDefault'), (stream.IsDefault ? 'Yes' : 'No')));
            }
            if (stream.Type == "Subtitle") {
                attributes.push(createAttribute(globalize.translate('MediaInfoForced'), (stream.IsForced ? 'Yes' : 'No')));
                attributes.push(createAttribute(globalize.translate('MediaInfoExternal'), (stream.IsExternal ? 'Yes' : 'No')));
            }

            if (stream.Type == "Video" && version.Timestamp) {
                attributes.push(createAttribute(globalize.translate('MediaInfoTimestamp'), version.Timestamp));
            }

            if (stream.DisplayTitle) {
                attributes.push(createAttribute('Title', stream.DisplayTitle));
            }

            html += attributes.join('<br/>');

            html += '</div>';
        }

        if (version.Container) {
            html += '<div><span class="mediaInfoLabel">' + globalize.translate('MediaInfoContainer') + '</span><span class="mediaInfoAttribute">' + version.Container + '</span></div>';
        }

        if (version.Formats && version.Formats.length) {
            //html += '<div><span class="mediaInfoLabel">'+Globalize.translate('MediaInfoFormat')+'</span><span class="mediaInfoAttribute">' + version.Formats.join(',') + '</span></div>';
        }

        if (version.Path && version.Protocol != 'Http' && user && user.Policy.IsAdministrator) {
            html += '<div style="max-width:600px;overflow:hidden;"><span class="mediaInfoLabel">' + globalize.translate('MediaInfoPath') + '</span><span class="mediaInfoAttribute">' + version.Path + '</span></div>';
        }

        if (version.Size) {

            var size = (version.Size / (1024 * 1024)).toFixed(0);

            html += '<div><span class="mediaInfoLabel">' + globalize.translate('MediaInfoSize') + '</span><span class="mediaInfoAttribute">' + size + ' MB</span></div>';
        }

        return html;
    }

    function createAttribute(label, value) {
        return '<span class="mediaInfoLabel">' + label + '</span><span class="mediaInfoAttribute">' + value + '</span>'
    }

    function getVideosHtml(items, user, limit, moreButtonClass) {

        var html = cardBuilder.getCardsHtml({
            items: items,
            shape: "auto",
            showTitle: true,
            action: 'play',
            overlayText: true,
            showRuntime: true
        });

        if (limit && items.length > limit) {
            html += '<p style="margin: 0;padding-left:5px;"><button is="emby-button" type="button" class="raised more ' + moreButtonClass + '">' + globalize.translate('ButtonMore') + '</button></p>';
        }

        return html;
    }

    function renderSpecials(page, item, user, limit) {

        ApiClient.getSpecialFeatures(user.Id, item.Id).then(function (specials) {

            var specialsContent = page.querySelector('#specialsContent');
            specialsContent.innerHTML = getVideosHtml(specials, user, limit, "moreSpecials");
            imageLoader.lazyChildren(specialsContent);

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

        var morePeopleButton = page.querySelector('.morePeople');
        if (morePeopleButton) {
            if (limitExceeded && !enableScrollX()) {
                morePeopleButton.classList.remove('hide');
            } else {
                morePeopleButton.classList.add('hide');
            }
        }
    }

    function play(startPosition) {

        playbackManager.play({
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

        playbackManager.playTrailers(currentItem);
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

            confirm(globalize.translate('MessageConfirmRecordingCancellation'), globalize.translate('HeaderConfirmRecordingCancellation')).then(function () {

                Dashboard.showLoadingMsg();

                ApiClient.cancelLiveTvTimer(id).then(function () {

                    require(['toast'], function (toast) {
                        toast(globalize.translate('MessageRecordingCancelled'));
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

    function onDeleteClick() {

        require(['deleteHelper'], function (deleteHelper) {

            deleteHelper.deleteItem({
                item: currentItem,
                navigate: true
            });
        });
    }

    function onCancelSeriesTimerClick() {

        require(['recordingHelper'], function (recordingHelper) {

            recordingHelper.cancelSeriesTimerWithConfirmation(currentItem.Id, currentItem.ServerId).then(function () {
                Dashboard.navigate('livetv.html');
            });
        });
    }

    return function (view, params) {

        function onPlayTrailerClick() {
            playTrailer(view);
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

        elems = view.querySelectorAll('.btnCancelSeriesTimer');
        for (i = 0, length = elems.length; i < length; i++) {
            elems[i].addEventListener('click', onCancelSeriesTimerClick);
        }

        elems = view.querySelectorAll('.btnDeleteItem');
        for (i = 0, length = elems.length; i < length; i++) {
            elems[i].addEventListener('click', onDeleteClick);
        }

        view.querySelector('.btnSplitVersions').addEventListener('click', function () {

            splitVersions(view, params);
        });

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

                require(['imageEditor'], function (imageEditor) {

                    imageEditor.show({

                        itemId: currentItem.Id,
                        serverId: currentItem.ServerId

                    }).then(resolve, reject);
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

        view.addEventListener('viewbeforeshow', function () {
            var page = this;
            reload(page, params);

            events.on(ApiClient, 'websocketmessage', onWebSocketMessage);
        });

        view.addEventListener('viewbeforehide', function () {

            currentItem = null;
            currentRecordingFields = null;

            events.off(ApiClient, 'websocketmessage', onWebSocketMessage);
            libraryMenu.setTransparentMenu(false);
        });

        view.addEventListener('viewdestroy', function () {

            if (view.syncToggleInstance) {
                view.syncToggleInstance.destroy();
                view.syncToggleInstance = null;
            }
        });
    };
});