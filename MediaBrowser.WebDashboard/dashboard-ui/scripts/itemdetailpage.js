define(["loading", "appRouter", "layoutManager", "connectionManager", "cardBuilder", "datetime", "mediaInfo", "backdrop", "listView", "itemContextMenu", "itemHelper", "dom", "indicators", "apphost", "imageLoader", "libraryMenu", "globalize", "browser", "events", "scrollHelper", "playbackManager", "libraryBrowser", "scrollStyles", "emby-itemscontainer", "emby-checkbox", "emby-linkbutton", "emby-playstatebutton", "emby-ratingbutton", "emby-downloadbutton", "emby-scroller", "emby-select"], function(loading, appRouter, layoutManager, connectionManager, cardBuilder, datetime, mediaInfo, backdrop, listView, itemContextMenu, itemHelper, dom, indicators, appHost, imageLoader, libraryMenu, globalize, browser, events, scrollHelper, playbackManager, libraryBrowser) {
    "use strict";

    function getPromise(apiClient, params) {
        var id = params.id;
        if (id) return apiClient.getItem(apiClient.getCurrentUserId(), id);
        if (params.seriesTimerId) return apiClient.getLiveTvSeriesTimer(params.seriesTimerId);
        var name = params.genre;
        if (name) return apiClient.getGenre(name, apiClient.getCurrentUserId());
        if (name = params.musicgenre) return apiClient.getMusicGenre(name, apiClient.getCurrentUserId());
        if (name = params.gamegenre) return apiClient.getGameGenre(name, apiClient.getCurrentUserId());
        if (name = params.musicartist) return apiClient.getArtist(name, apiClient.getCurrentUserId());
        throw new Error("Invalid request")
    }

    function hideAll(page, className, show) {
        var i, length, elems = page.querySelectorAll("." + className);
        for (i = 0, length = elems.length; i < length; i++) show ? elems[i].classList.remove("hide") : elems[i].classList.add("hide")
    }

    function getContextMenuOptions(item, user, button) {
        var options = {
            item: item,
            open: !1,
            play: !1,
            playAllFromHere: !1,
            queueAllFromHere: !1,
            positionTo: button,
            cancelTimer: !1,
            record: !1,
            deleteItem: !0 === item.IsFolder,
            shuffle: !1,
            instantMix: !1,
            user: user,
            share: !0
        };
        return appHost.supports("sync") && (options.syncLocal = !1), options
    }

    function renderSyncLocalContainer(page, params, user, item) {
        if (appHost.supports("sync"))
            for (var canSync = itemHelper.canSync(user, item), buttons = page.querySelectorAll(".btnSyncDownload"), i = 0, length = buttons.length; i < length; i++) buttons[i].setItem(item), canSync ? buttons[i].classList.remove("hide") : buttons[i].classList.add("hide")
    }

    function getProgramScheduleHtml(items, options) {
        options = options || {};
        var html = "";
        return html += '<div is="emby-itemscontainer" class="itemsContainer vertical-list" data-contextmenu="false">', html += listView.getListViewHtml({
            items: items,
            enableUserDataButtons: !1,
            image: !0,
            imageSource: "channel",
            showProgramDateTime: !0,
            showChannel: !1,
            mediaInfo: !1,
            action: "none",
            moreButton: !1,
            recordButton: !1
        }), html += "</div>"
    }

    function renderSeriesTimerSchedule(page, apiClient, seriesTimerId) {
        apiClient.getLiveTvTimers({
            UserId: apiClient.getCurrentUserId(),
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Thumb",
            SortBy: "StartDate",
            EnableTotalRecordCount: !1,
            EnableUserData: !1,
            SeriesTimerId: seriesTimerId,
            Fields: "ChannelInfo,ChannelImage"
        }).then(function(result) {
            result.Items.length && result.Items[0].SeriesTimerId != seriesTimerId && (result.Items = []);
            var html = getProgramScheduleHtml(result.Items),
                scheduleTab = page.querySelector(".seriesTimerSchedule");
            scheduleTab.innerHTML = html, imageLoader.lazyChildren(scheduleTab)
        })
    }

    function renderTimerEditor(page, item, apiClient, user) {
        if ("Recording" !== item.Type || !user.Policy.EnableLiveTvManagement || !item.TimerId || "InProgress" !== item.Status) return void hideAll(page, "btnCancelTimer");
        hideAll(page, "btnCancelTimer", !0)
    }

    function renderSeriesTimerEditor(page, item, apiClient, user) {
        return "SeriesTimer" !== item.Type ? void hideAll(page, "btnCancelSeriesTimer") : user.Policy.EnableLiveTvManagement ? (require(["seriesRecordingEditor"], function(seriesRecordingEditor) {
            seriesRecordingEditor.embed(item, apiClient.serverId(), {
                context: page.querySelector(".seriesRecordingEditor")
            })
        }), page.querySelector(".seriesTimerScheduleSection").classList.remove("hide"), hideAll(page, "btnCancelSeriesTimer", !0), void renderSeriesTimerSchedule(page, apiClient, item.Id)) : (page.querySelector(".seriesTimerScheduleSection").classList.add("hide"), void hideAll(page, "btnCancelSeriesTimer"))
    }

    function renderTrackSelections(page, instance, item, forceReload) {
        var select = page.querySelector(".selectSource");
        if (!item.MediaSources || !itemHelper.supportsMediaSourceSelection(item) || -1 === playbackManager.getSupportedCommands().indexOf("PlayMediaSource") || !playbackManager.canPlay(item)) return page.querySelector(".trackSelections").classList.add("hide"), select.innerHTML = "", page.querySelector(".selectVideo").innerHTML = "", page.querySelector(".selectAudio").innerHTML = "", void(page.querySelector(".selectSubtitles").innerHTML = "");
        playbackManager.getPlaybackMediaSources(item).then(function(mediaSources) {
            instance._currentPlaybackMediaSources = mediaSources, page.querySelector(".trackSelections").classList.remove("hide"), select.setLabel(globalize.translate("sharedcomponents#LabelVersion"));
            var currentValue = select.value,
                selectedId = mediaSources[0].Id;
            select.innerHTML = mediaSources.map(function(v) {
                var selected = v.Id === selectedId ? " selected" : "";
                return '<option value="' + v.Id + '"' + selected + ">" + v.Name + "</option>"
            }).join(""), mediaSources.length > 1 ? page.querySelector(".selectSourceContainer").classList.remove("hide") : page.querySelector(".selectSourceContainer").classList.add("hide"), (select.value !== currentValue || forceReload) && (renderVideoSelections(page, mediaSources), renderAudioSelections(page, mediaSources), renderSubtitleSelections(page, mediaSources))
        })
    }

    function renderVideoSelections(page, mediaSources) {
        var mediaSourceId = page.querySelector(".selectSource").value,
            mediaSource = mediaSources.filter(function(m) {
                return m.Id === mediaSourceId
            })[0],
            tracks = mediaSource.MediaStreams.filter(function(m) {
                return "Video" === m.Type
            }),
            select = page.querySelector(".selectVideo");
        select.setLabel(globalize.translate("sharedcomponents#LabelVideo"));
        var selectedId = tracks.length ? tracks[0].Index : -1;
        select.innerHTML = tracks.map(function(v) {
            var selected = v.Index === selectedId ? " selected" : "",
                titleParts = [],
                resolutionText = mediaInfo.getResolutionText(v);
            return resolutionText && titleParts.push(resolutionText), v.Codec && titleParts.push(v.Codec.toUpperCase()), '<option value="' + v.Index + '" ' + selected + ">" + (v.DisplayTitle || titleParts.join(" ")) + "</option>"
        }).join(""), select.setAttribute("disabled", "disabled"), tracks.length ? page.querySelector(".selectVideoContainer").classList.remove("hide") : page.querySelector(".selectVideoContainer").classList.add("hide")
    }

    function renderAudioSelections(page, mediaSources) {
        var mediaSourceId = page.querySelector(".selectSource").value,
            mediaSource = mediaSources.filter(function(m) {
                return m.Id === mediaSourceId
            })[0],
            tracks = mediaSource.MediaStreams.filter(function(m) {
                return "Audio" === m.Type
            }),
            select = page.querySelector(".selectAudio");
        select.setLabel(globalize.translate("sharedcomponents#LabelAudio"));
        var selectedId = mediaSource.DefaultAudioStreamIndex;
        select.innerHTML = tracks.map(function(v) {
            var selected = v.Index === selectedId ? " selected" : "";
            return '<option value="' + v.Index + '" ' + selected + ">" + v.DisplayTitle + "</option>"
        }).join(""), tracks.length > 1 ? select.removeAttribute("disabled") : select.setAttribute("disabled", "disabled"), tracks.length ? page.querySelector(".selectAudioContainer").classList.remove("hide") : page.querySelector(".selectAudioContainer").classList.add("hide")
    }

    function renderSubtitleSelections(page, mediaSources) {
        var mediaSourceId = page.querySelector(".selectSource").value,
            mediaSource = mediaSources.filter(function(m) {
                return m.Id === mediaSourceId
            })[0],
            tracks = mediaSource.MediaStreams.filter(function(m) {
                return "Subtitle" === m.Type
            }),
            select = page.querySelector(".selectSubtitles");
        select.setLabel(globalize.translate("sharedcomponents#LabelSubtitles"));
        var selectedId = null == mediaSource.DefaultSubtitleStreamIndex ? -1 : mediaSource.DefaultSubtitleStreamIndex;
        if (tracks.length) {
            var selected = -1 === selectedId ? " selected" : "";
            select.innerHTML = '<option value="-1">' + globalize.translate("sharedcomponents#Off") + "</option>" + tracks.map(function(v) {
                return selected = v.Index === selectedId ? " selected" : "", '<option value="' + v.Index + '" ' + selected + ">" + v.DisplayTitle + "</option>"
            }).join(""), page.querySelector(".selectSubtitlesContainer").classList.remove("hide")
        } else select.innerHTML = "", page.querySelector(".selectSubtitlesContainer").classList.add("hide")
    }

    function reloadPlayButtons(page, item) {
        var canPlay = !1;
        if ("Program" == item.Type) {
            var now = new Date;
            now >= datetime.parseISO8601Date(item.StartDate, !0) && now < datetime.parseISO8601Date(item.EndDate, !0) ? (hideAll(page, "btnPlay", !0), canPlay = !0) : hideAll(page, "btnPlay"), hideAll(page, "btnResume"), hideAll(page, "btnInstantMix"), hideAll(page, "btnShuffle")
        } else if (playbackManager.canPlay(item)) {
            hideAll(page, "btnPlay", !0);
            var enableInstantMix = -1 !== ["Audio", "MusicAlbum", "MusicGenre", "MusicArtist"].indexOf(item.Type);
            hideAll(page, "btnInstantMix", enableInstantMix);
            var enableShuffle = item.IsFolder || -1 !== ["MusicAlbum", "MusicGenre", "MusicArtist"].indexOf(item.Type);
            hideAll(page, "btnShuffle", enableShuffle), canPlay = !0, hideAll(page, "btnResume", item.UserData && item.UserData.PlaybackPositionTicks > 0)
        } else hideAll(page, "btnPlay"), hideAll(page, "btnResume"), hideAll(page, "btnInstantMix"), hideAll(page, "btnShuffle");
        return canPlay
    }

    function reloadUserDataButtons(page, item) {
        var i, length, btnPlaystates = page.querySelectorAll(".btnPlaystate");
        for (i = 0, length = btnPlaystates.length; i < length; i++) {
            var btnPlaystate = btnPlaystates[i];
            itemHelper.canMarkPlayed(item) ? (btnPlaystate.classList.remove("hide"), btnPlaystate.setItem(item)) : (btnPlaystate.classList.add("hide"), btnPlaystate.setItem(null))
        }
        var btnUserRatings = page.querySelectorAll(".btnUserRating");
        for (i = 0, length = btnUserRatings.length; i < length; i++) {
            var btnUserRating = btnUserRatings[i];
            itemHelper.canRate(item) ? (btnUserRating.classList.remove("hide"), btnUserRating.setItem(item)) : (btnUserRating.classList.add("hide"), btnUserRating.setItem(null))
        }
    }

    function getArtistLinksHtml(artists, serverId, context) {
        for (var html = [], i = 0, length = artists.length; i < length; i++) {
            var artist = artists[i],
                href = appRouter.getRouteUrl(artist, {
                    context: context,
                    itemType: "MusicArtist",
                    serverId: serverId
                });
            html.push('<a style="color:inherit;" class="button-link" is="emby-linkbutton" href="' + href + '">' + artist.Name + "</a>")
        }
        return html = html.join(" / ")
    }

    function renderName(item, container, isStatic, context) {
        var parentRoute, parentNameHtml = [],
            parentNameLast = !1;
        item.AlbumArtists ? (parentNameHtml.push(getArtistLinksHtml(item.AlbumArtists, item.ServerId, context)), parentNameLast = !0) : item.ArtistItems && item.ArtistItems.length && "MusicVideo" === item.Type ? (parentNameHtml.push(getArtistLinksHtml(item.ArtistItems, item.ServerId, context)), parentNameLast = !0) : item.SeriesName && "Episode" === item.Type ? (parentRoute = appRouter.getRouteUrl({
            Id: item.SeriesId,
            Name: item.SeriesName,
            Type: "Series",
            IsFolder: !0,
            ServerId: item.ServerId
        }, {
            context: context
        }), parentNameHtml.push('<a style="color:inherit;" class="button-link" is="emby-linkbutton" href="' + parentRoute + '">' + item.SeriesName + "</a>")) : (item.IsSeries || item.EpisodeTitle) && parentNameHtml.push(item.Name), item.SeriesName && "Season" === item.Type ? (parentRoute = appRouter.getRouteUrl({
            Id: item.SeriesId,
            Name: item.SeriesName,
            Type: "Series",
            IsFolder: !0,
            ServerId: item.ServerId
        }, {
            context: context
        }), parentNameHtml.push('<a style="color:inherit;" class="button-link" is="emby-linkbutton" href="' + parentRoute + '">' + item.SeriesName + "</a>")) : null != item.ParentIndexNumber && "Episode" === item.Type ? (parentRoute = appRouter.getRouteUrl({
            Id: item.SeasonId,
            Name: item.SeasonName,
            Type: "Season",
            IsFolder: !0,
            ServerId: item.ServerId
        }, {
            context: context
        }), parentNameHtml.push('<a style="color:inherit;" class="button-link" is="emby-linkbutton" href="' + parentRoute + '">' + item.SeasonName + "</a>")) : null != item.ParentIndexNumber && item.IsSeries ? parentNameHtml.push(item.SeasonName || "S" + item.ParentIndexNumber) : item.Album && item.AlbumId && ("MusicVideo" === item.Type || "Audio" === item.Type) ? (parentRoute = appRouter.getRouteUrl({
            Id: item.AlbumId,
            Name: item.Album,
            Type: "MusicAlbum",
            IsFolder: !0,
            ServerId: item.ServerId
        }, {
            context: context
        }), parentNameHtml.push('<a style="color:inherit;" class="button-link" is="emby-linkbutton" href="' + parentRoute + '">' + item.Album + "</a>")) : item.Album && parentNameHtml.push(item.Album);
        var html = "";
        parentNameHtml.length && (html = parentNameLast ? '<h3 class="parentName" style="margin: .25em 0;">' + parentNameHtml.join(" - ") + "</h3>" : '<h1 class="parentName" style="margin: .1em 0 .25em;">' + parentNameHtml.join(" - ") + "</h1>");
        var name = itemHelper.getDisplayName(item, {
            includeParentInfo: !1
        });
        html && !parentNameLast ? html += '<h3 class="itemName" style="margin: .25em 0 .5em;">' + name + "</h3>" : html = parentNameLast ? '<h1 class="itemName" style="margin: .1em 0 .25em;">' + name + "</h1>" + html : '<h1 class="itemName" style="margin: .1em 0 .5em;">' + name + "</h1>" + html, container.innerHTML = html, html.length ? container.classList.remove("hide") : container.classList.add("hide")
    }

    function setTrailerButtonVisibility(page, item) {
        (item.LocalTrailerCount || item.RemoteTrailers && item.RemoteTrailers.length) && -1 !== playbackManager.getSupportedCommands().indexOf("PlayTrailers") ? hideAll(page, "btnPlayTrailer", !0) : hideAll(page, "btnPlayTrailer")
    }

    function renderDetailPageBackdrop(page, item, apiClient) {
        var imgUrl, screenWidth = screen.availWidth,
            hasbackdrop = !1,
            itemBackdropElement = page.querySelector("#itemBackdrop"),
            usePrimaryImage = "Video" === item.MediaType && "Movie" !== item.Type && "Trailer" !== item.Type || item.MediaType && "Video" !== item.MediaType;
        return "Program" === item.Type && item.ImageTags && item.ImageTags.Thumb ? (imgUrl = apiClient.getScaledImageUrl(item.Id, {
            type: "Thumb",
            index: 0,
            maxWidth: screenWidth,
            tag: item.ImageTags.Thumb
        }), itemBackdropElement.classList.remove("noBackdrop"), imageLoader.lazyImage(itemBackdropElement, imgUrl, !1), hasbackdrop = !0) : usePrimaryImage && item.ImageTags && item.ImageTags.Primary ? (imgUrl = apiClient.getScaledImageUrl(item.Id, {
            type: "Primary",
            index: 0,
            maxWidth: screenWidth,
            tag: item.ImageTags.Primary
        }), itemBackdropElement.classList.remove("noBackdrop"), imageLoader.lazyImage(itemBackdropElement, imgUrl, !1), hasbackdrop = !0) : item.BackdropImageTags && item.BackdropImageTags.length ? (imgUrl = apiClient.getScaledImageUrl(item.Id, {
            type: "Backdrop",
            index: 0,
            maxWidth: screenWidth,
            tag: item.BackdropImageTags[0]
        }), itemBackdropElement.classList.remove("noBackdrop"), imageLoader.lazyImage(itemBackdropElement, imgUrl, !1), hasbackdrop = !0) : item.ParentBackdropItemId && item.ParentBackdropImageTags && item.ParentBackdropImageTags.length ? (imgUrl = apiClient.getScaledImageUrl(item.ParentBackdropItemId, {
            type: "Backdrop",
            index: 0,
            tag: item.ParentBackdropImageTags[0],
            maxWidth: screenWidth
        }), itemBackdropElement.classList.remove("noBackdrop"), imageLoader.lazyImage(itemBackdropElement, imgUrl, !1), hasbackdrop = !0) : item.ImageTags && item.ImageTags.Thumb ? (imgUrl = apiClient.getScaledImageUrl(item.Id, {
            type: "Thumb",
            index: 0,
            maxWidth: screenWidth,
            tag: item.ImageTags.Thumb
        }), itemBackdropElement.classList.remove("noBackdrop"), imageLoader.lazyImage(itemBackdropElement, imgUrl, !1), hasbackdrop = !0) : (itemBackdropElement.classList.add("noBackdrop"), itemBackdropElement.style.backgroundImage = ""), hasbackdrop
    }

    function reloadFromItem(instance, page, params, item, user) {
        var context = params.context;
        renderName(item, page.querySelector(".nameContainer"), !1, context);
        var apiClient = connectionManager.getApiClient(item.ServerId);
        renderSeriesTimerEditor(page, item, apiClient, user), renderTimerEditor(page, item, apiClient, user), renderImage(page, item, apiClient, user), renderLogo(page, item, apiClient), setTitle(item, apiClient), setInitialCollapsibleState(page, item, apiClient, context, user), renderDetails(page, item, apiClient, context), renderTrackSelections(page, instance, item), dom.getWindowSize().innerWidth >= 1e3 ? backdrop.setBackdrops([item]) : backdrop.clear(), renderDetailPageBackdrop(page, item, apiClient);
        var canPlay = reloadPlayButtons(page, item);
        (item.LocalTrailerCount || item.RemoteTrailers && item.RemoteTrailers.length) && -1 !== playbackManager.getSupportedCommands().indexOf("PlayTrailers") ? hideAll(page, "btnPlayTrailer", !0) : hideAll(page, "btnPlayTrailer"), setTrailerButtonVisibility(page, item), item.CanDelete && !item.IsFolder ? hideAll(page, "btnDeleteItem", !0) : hideAll(page, "btnDeleteItem"), renderSyncLocalContainer(page, params, user, item), "Program" !== item.Type || canPlay ? hideAll(page, "mainDetailButtons", !0) : hideAll(page, "mainDetailButtons"), showRecordingFields(instance, page, item, user);
        var groupedVersions = (item.MediaSources || []).filter(function(g) {
            return "Grouping" == g.Type
        });
        user.Policy.IsAdministrator && groupedVersions.length ? page.querySelector(".splitVersionContainer").classList.remove("hide") : page.querySelector(".splitVersionContainer").classList.add("hide"), itemContextMenu.getCommands(getContextMenuOptions(item, user)).length ? hideAll(page, "btnMoreCommands", !0) : hideAll(page, "btnMoreCommands");
        var itemBirthday = page.querySelector("#itemBirthday");
        if ("Person" == item.Type && item.PremiereDate) try {
            var birthday = datetime.parseISO8601Date(item.PremiereDate, !0).toDateString();
            itemBirthday.classList.remove("hide"), itemBirthday.innerHTML = globalize.translate("BirthDateValue").replace("{0}", birthday)
        } catch (err) {
            itemBirthday.classList.add("hide")
        } else itemBirthday.classList.add("hide");
        var itemDeathDate = page.querySelector("#itemDeathDate");
        if ("Person" == item.Type && item.EndDate) try {
            var deathday = datetime.parseISO8601Date(item.EndDate, !0).toDateString();
            itemDeathDate.classList.remove("hide"), itemDeathDate.innerHTML = globalize.translate("DeathDateValue").replace("{0}", deathday)
        } catch (err) {
            itemDeathDate.classList.add("hide")
        }
        var itemBirthLocation = page.querySelector("#itemBirthLocation");
        if ("Person" == item.Type && item.ProductionLocations && item.ProductionLocations.length) {
            var gmap = '<a is="emby-linkbutton" class="button-link textlink" target="_blank" href="https://maps.google.com/maps?q=' + item.ProductionLocations[0] + '">' + item.ProductionLocations[0] + "</a>";
            itemBirthLocation.classList.remove("hide"), itemBirthLocation.innerHTML = globalize.translate("BirthPlaceValue").replace("{0}", gmap)
        } else itemBirthLocation.classList.add("hide");
        setPeopleHeader(page, item), loading.hide()
    }

    function logoImageUrl(item, apiClient, options) {
        return options = options || {}, options.type = "Logo", item.ImageTags && item.ImageTags.Logo ? (options.tag = item.ImageTags.Logo, apiClient.getScaledImageUrl(item.Id, options)) : item.ParentLogoImageTag ? (options.tag = item.ParentLogoImageTag, apiClient.getScaledImageUrl(item.ParentLogoItemId, options)) : null
    }

    function setTitle(item, apiClient) {
        var url = logoImageUrl(item, apiClient, {});
        if (url = null) {
            var pageTitle = document.querySelector(".pageTitle");
            pageTitle.style.backgroundImage = "url('" + url + "')", pageTitle.classList.add("pageTitleWithLogo"), pageTitle.innerHTML = ""
        } else Emby.Page.setTitle("")
    }

    function renderLogo(page, item, apiClient) {
        var url = logoImageUrl(item, apiClient, {
                maxWidth: 300
            }),
            detailLogo = page.querySelector(".detailLogo");
        url ? (detailLogo.classList.remove("hide"), detailLogo.classList.add("lazy"), detailLogo.setAttribute("data-src", url), imageLoader.lazyImage(detailLogo)) : detailLogo.classList.add("hide")
    }

    function showRecordingFields(instance, page, item, user) {
        if (!instance.currentRecordingFields) {
            var recordingFieldsElement = page.querySelector(".recordingFields");
            "Program" == item.Type && user.Policy.EnableLiveTvManagement ? require(["recordingFields"], function(recordingFields) {
                instance.currentRecordingFields = new recordingFields({
                    parent: recordingFieldsElement,
                    programId: item.Id,
                    serverId: item.ServerId
                }), recordingFieldsElement.classList.remove("hide")
            }) : (recordingFieldsElement.classList.add("hide"), recordingFieldsElement.innerHTML = "")
        }
    }

    function renderLinks(linksElem, item) {
        var html = [];
        if (item.DateCreated && itemHelper.enableDateAddedDisplay(item)) {
            var dateCreated = datetime.parseISO8601Date(item.DateCreated);
            html.push(globalize.translate("sharedcomponents#AddedOnValue", datetime.toLocaleDateString(dateCreated) + " " + datetime.getDisplayTime(dateCreated)))
        }
        var links = [];
        if (!layoutManager.tv && (item.HomePageUrl && links.push('<a style="color:inherit;" is="emby-linkbutton" class="button-link" href="' + item.HomePageUrl + '" target="_blank">' + globalize.translate("ButtonWebsite") + "</a>"), item.ExternalUrls))
            for (var i = 0, length = item.ExternalUrls.length; i < length; i++) {
                var url = item.ExternalUrls[i];
                links.push('<a style="color:inherit;" is="emby-linkbutton" class="button-link" href="' + url.Url + '" target="_blank">' + url.Name + "</a>")
            }
        links.length && html.push(globalize.translate("sharedcomponents#LinksValue", links.join(", "))), linksElem.innerHTML = html.join(", "), html.length ? linksElem.classList.remove("hide") : linksElem.classList.add("hide")
    }

    function renderDetailImage(page, elem, item, apiClient, editable, imageLoader, indicators) {
        "SeriesTimer" !== item.Type && "Program" !== item.Type || (editable = !1), "Person" !== item.Type ? (elem.classList.add("detailimg-hidemobile"), page.querySelector(".detailPageContent").classList.add("detailPageContent-nodetailimg")) : page.querySelector(".detailPageContent").classList.remove("detailPageContent-nodetailimg");
        var imageTags = item.ImageTags || {};
        item.PrimaryImageTag && (imageTags.Primary = item.PrimaryImageTag);
        var url, html = "",
            shape = "portrait",
            detectRatio = !1;
        imageTags.Primary ? (url = apiClient.getScaledImageUrl(item.Id, {
            type: "Primary",
            maxHeight: 360,
            tag: item.ImageTags.Primary
        }), detectRatio = !0) : item.BackdropImageTags && item.BackdropImageTags.length ? (url = apiClient.getScaledImageUrl(item.Id, {
            type: "Backdrop",
            maxHeight: 360,
            tag: item.BackdropImageTags[0]
        }), shape = "thumb") : imageTags.Thumb ? (url = apiClient.getScaledImageUrl(item.Id, {
            type: "Thumb",
            maxHeight: 360,
            tag: item.ImageTags.Thumb
        }), shape = "thumb") : imageTags.Disc ? (url = apiClient.getScaledImageUrl(item.Id, {
            type: "Disc",
            maxHeight: 360,
            tag: item.ImageTags.Disc
        }), shape = "square") : item.AlbumId && item.AlbumPrimaryImageTag ? (url = apiClient.getScaledImageUrl(item.AlbumId, {
            type: "Primary",
            maxHeight: 360,
            tag: item.AlbumPrimaryImageTag
        }), shape = "square") : item.SeriesId && item.SeriesPrimaryImageTag ? url = apiClient.getScaledImageUrl(item.SeriesId, {
            type: "Primary",
            maxHeight: 360,
            tag: item.SeriesPrimaryImageTag
        }) : item.ParentPrimaryImageItemId && item.ParentPrimaryImageTag && (url = apiClient.getScaledImageUrl(item.ParentPrimaryImageItemId, {
            type: "Primary",
            maxHeight: 360,
            tag: item.ParentPrimaryImageTag
        })), html += '<div style="position:relative;">', editable && (html += "<a class='itemDetailGalleryLink' is='emby-linkbutton' style='display:block;padding:2px;margin:0;' href='#'>"), detectRatio && item.PrimaryImageAspectRatio && (item.PrimaryImageAspectRatio >= 1.48 ? shape = "thumb" : item.PrimaryImageAspectRatio >= .85 && item.PrimaryImageAspectRatio <= 1.34 && (shape = "square")), html += "<img class='itemDetailImage lazy' src='data:image/gif;base64,R0lGODlhAQABAAD/ACwAAAAAAQABAAACADs=' />", editable && (html += "</a>");
        var progressHtml = item.IsFolder || !item.UserData ? "" : indicators.getProgressBarHtml(item);
        html += '<div class="detailImageProgressContainer">', progressHtml && (html += progressHtml), html += "</div>", html += "</div>", elem.innerHTML = html, "thumb" == shape ? (elem.classList.add("thumbDetailImageContainer"), elem.classList.remove("portraitDetailImageContainer"), elem.classList.remove("squareDetailImageContainer")) : "square" == shape ? (elem.classList.remove("thumbDetailImageContainer"), elem.classList.remove("portraitDetailImageContainer"), elem.classList.add("squareDetailImageContainer")) : (elem.classList.remove("thumbDetailImageContainer"), elem.classList.add("portraitDetailImageContainer"), elem.classList.remove("squareDetailImageContainer")), url && imageLoader.lazyImage(elem.querySelector("img"), url)
    }

    function renderImage(page, item, apiClient, user) {
        renderDetailImage(page, page.querySelector(".detailImageContainer"), item, apiClient, user.Policy.IsAdministrator && "Photo" != item.MediaType, imageLoader, indicators)
    }

    function refreshDetailImageUserData(elem, item) {
        elem.querySelector(".detailImageProgressContainer").innerHTML = indicators.getProgressBarHtml(item)
    }

    function refreshImage(page, item, user) {
        refreshDetailImageUserData(page.querySelector(".detailImageContainer"), item)
    }

    function setPeopleHeader(page, item) {
        "Audio" == item.MediaType || "MusicAlbum" == item.Type || "Book" == item.MediaType || "Photo" == item.MediaType ? page.querySelector("#peopleHeader").innerHTML = globalize.translate("HeaderPeople") : page.querySelector("#peopleHeader").innerHTML = globalize.translate("HeaderCastAndCrew")
    }

    function renderNextUp(page, item, user) {
        var section = page.querySelector(".nextUpSection");
        if ("Series" != item.Type) return void section.classList.add("hide");
        connectionManager.getApiClient(item.ServerId).getNextUpEpisodes({
            SeriesId: item.Id,
            UserId: user.Id
        }).then(function(result) {
            result.Items.length ? section.classList.remove("hide") : section.classList.add("hide");
            var html = cardBuilder.getCardsHtml({
                    items: result.Items,
                    shape: getThumbShape(!1),
                    showTitle: !0,
                    displayAsSpecial: "Season" == item.Type && item.IndexNumber,
                    overlayText: !1,
                    centerText: !0,
                    overlayPlayButton: !0
                }),
                itemsContainer = section.querySelector(".nextUpItems");
            itemsContainer.innerHTML = html, imageLoader.lazyChildren(itemsContainer)
        })
    }

    function setInitialCollapsibleState(page, item, apiClient, context, user) {
        page.querySelector(".collectionItems").innerHTML = "", "Playlist" == item.Type ? (page.querySelector("#childrenCollapsible").classList.remove("hide"), renderPlaylistItems(page, item, user)) : "Studio" == item.Type || "Person" == item.Type || "Genre" == item.Type || "MusicGenre" == item.Type || "GameGenre" == item.Type || "MusicArtist" == item.Type ? (page.querySelector("#childrenCollapsible").classList.remove("hide"), renderItemsByName(page, item, user)) : item.IsFolder ? ("BoxSet" == item.Type && page.querySelector("#childrenCollapsible").classList.add("hide"), renderChildren(page, item)) : page.querySelector("#childrenCollapsible").classList.add("hide"), "Series" == item.Type && renderSeriesSchedule(page, item, user), "Series" == item.Type ? renderNextUp(page, item, user) : page.querySelector(".nextUpSection").classList.add("hide"), item.MediaSources && item.MediaSources.length && (null == item.EnableMediaSourceDisplay ? "Channel" !== item.SourceType : item.EnableMediaSourceDisplay) ? renderMediaSources(page, user, item) : page.querySelector(".audioVideoMediaInfo").classList.add("hide"), renderScenes(page, item), item.SpecialFeatureCount && 0 != item.SpecialFeatureCount && "Series" != item.Type ? (page.querySelector("#specialsCollapsible").classList.remove("hide"), renderSpecials(page, item, user, 6)) : page.querySelector("#specialsCollapsible").classList.add("hide"), renderCast(page, item, context, enableScrollX() ? null : 12), item.PartCount && item.PartCount > 1 ? (page.querySelector("#additionalPartsCollapsible").classList.remove("hide"), renderAdditionalParts(page, item, user)) : page.querySelector("#additionalPartsCollapsible").classList.add("hide"), "MusicAlbum" == item.Type ? renderMusicVideos(page, item, user) : page.querySelector("#musicVideosCollapsible").classList.add("hide")
    }

    function renderOverview(elems, item) {
        for (var i = 0, length = elems.length; i < length; i++) {
            var elem = elems[i],
                overview = item.Overview || "";
            if (overview) {
                elem.innerHTML = overview, elem.classList.remove("hide");
                for (var anchors = elem.querySelectorAll("a"), j = 0, length2 = anchors.length; j < length2; j++) anchors[j].setAttribute("target", "_blank")
            } else elem.innerHTML = "", elem.classList.add("hide")
        }
    }

    function renderGenres(page, item, apiClient, context, isStatic) {
        context = context || inferContext(item);
        var type, genres = item.GenreItems || [];
        switch (context) {
            case "games":
                type = "GameGenre";
                break;
            case "music":
                type = "MusicGenre";
                break;
            default:
                type = "Genre"
        }
        var html = genres.map(function(p) {
                return '<a style="color:inherit;" class="button-link" is="emby-linkbutton" href="' + appRouter.getRouteUrl({
                    Name: p.Name,
                    Type: type,
                    ServerId: item.ServerId,
                    Id: p.Id
                }, {
                    context: context
                }) + '">' + p.Name + "</a>"
            }).join(", "),
            elem = page.querySelector(".genres");
        elem.innerHTML = genres.length > 1 ? globalize.translate("sharedcomponents#GenresValue", html) : globalize.translate("sharedcomponents#GenreValue", html), genres.length ? elem.classList.remove("hide") : elem.classList.add("hide")
    }

    function renderDirector(page, item, apiClient, context, isStatic) {
        var directors = (item.People || []).filter(function(p) {
                return "Director" === p.Type
            }),
            html = directors.map(function(p) {
                return '<a style="color:inherit;" class="button-link" is="emby-linkbutton" href="' + appRouter.getRouteUrl({
                    Name: p.Name,
                    Type: "Person",
                    ServerId: item.ServerId,
                    Id: p.Id
                }, {
                    context: context
                }) + '">' + p.Name + "</a>"
            }).join(", "),
            elem = page.querySelector(".directors");
        elem.innerHTML = directors.length > 1 ? globalize.translate("sharedcomponents#DirectorsValue", html) : globalize.translate("sharedcomponents#DirectorValue", html), directors.length ? elem.classList.remove("hide") : elem.classList.add("hide")
    }

    function renderDetails(page, item, apiClient, context, isStatic) {
        renderSimilarItems(page, item, context), renderMoreFromSeason(page, item, apiClient), renderMoreFromArtist(page, item, apiClient), renderDirector(page, item, apiClient, context, isStatic), renderGenres(page, item, apiClient, context, isStatic), renderChannelGuide(page, apiClient, item);
        var taglineElement = page.querySelector(".tagline");
        item.Taglines && item.Taglines.length ? (taglineElement.classList.remove("hide"), taglineElement.innerHTML = item.Taglines[0]) : taglineElement.classList.add("hide");
        var overview = page.querySelector(".overview"),
            externalLinksElem = page.querySelector(".itemExternalLinks");
        "Season" !== item.Type && "MusicAlbum" !== item.Type && "MusicArtist" !== item.Type || (overview.classList.add("detailsHiddenOnMobile"), externalLinksElem.classList.add("detailsHiddenOnMobile")), renderOverview([overview], item);
        var i, length, itemMiscInfo = page.querySelectorAll(".itemMiscInfo-primary");
        for (i = 0, length = itemMiscInfo.length; i < length; i++) mediaInfo.fillPrimaryMediaInfo(itemMiscInfo[i], item, {
            interactive: !0,
            episodeTitle: !1,
            subtitles: !1
        }), itemMiscInfo[i].innerHTML && "SeriesTimer" !== item.Type ? itemMiscInfo[i].classList.remove("hide") : itemMiscInfo[i].classList.add("hide");
        for (itemMiscInfo = page.querySelectorAll(".itemMiscInfo-secondary"), i = 0, length = itemMiscInfo.length; i < length; i++) mediaInfo.fillSecondaryMediaInfo(itemMiscInfo[i], item, {
            interactive: !0
        }), itemMiscInfo[i].innerHTML ? itemMiscInfo[i].classList.remove("hide") : itemMiscInfo[i].classList.add("hide");
        reloadUserDataButtons(page, item), renderLinks(externalLinksElem, item), renderTags(page, item), renderSeriesAirTime(page, item, isStatic)
    }

    function enableScrollX() {
        return browser.mobile && screen.availWidth <= 1e3
    }

    function getPortraitShape(scrollX) {
        return null == scrollX && (scrollX = enableScrollX()), scrollX ? "overflowPortrait" : "portrait"
    }

    function getSquareShape(scrollX) {
        return null == scrollX && (scrollX = enableScrollX()), scrollX ? "overflowSquare" : "square"
    }

    function getThumbShape(scrollX) {
        return null == scrollX && (scrollX = enableScrollX()), scrollX ? "overflowBackdrop" : "backdrop"
    }

    function renderMoreFromSeason(view, item, apiClient) {
        var section = view.querySelector(".moreFromSeasonSection");
        if (section) {
            if ("Episode" !== item.Type || !item.SeasonId || !item.SeriesId) return void section.classList.add("hide");
            var userId = apiClient.getCurrentUserId();
            apiClient.getEpisodes(item.SeriesId, {
                SeasonId: item.SeasonId,
                UserId: userId,
                Fields: "ItemCounts,PrimaryImageAspectRatio,BasicSyncInfo,CanDelete,MediaSourceCount"
            }).then(function(result) {
                if (result.Items.length < 2) return void section.classList.add("hide");
                section.classList.remove("hide"), section.querySelector("h2").innerHTML = globalize.translate("MoreFromValue", item.SeasonName);
                var itemsContainer = section.querySelector(".itemsContainer");
                cardBuilder.buildCards(result.Items, {
                    parentContainer: section,
                    itemsContainer: itemsContainer,
                    shape: "autooverflow",
                    sectionTitleTagName: "h2",
                    scalable: !0,
                    showTitle: !0,
                    overlayText: !1,
                    centerText: !0,
                    includeParentInfoInTitle: !1,
                    allowBottomPadding: !1
                });
                var card = itemsContainer.querySelector('.card[data-id="' + item.Id + '"]');
                card && setTimeout(function() {
                    section.querySelector(".emby-scroller").toStart(card.previousSibling || card, !0)
                }, 100)
            })
        }
    }

    function renderMoreFromArtist(view, item, apiClient) {
        var section = view.querySelector(".moreFromArtistSection");
        if (section) {
            if ("MusicArtist" === item.Type) {
                if (!apiClient.isMinServerVersion("3.4.1.19")) return void section.classList.add("hide")
            } else if ("MusicAlbum" !== item.Type || !item.AlbumArtists || !item.AlbumArtists.length) return void section.classList.add("hide");
            var query = {
                IncludeItemTypes: "MusicAlbum",
                Recursive: !0,
                ExcludeItemIds: item.Id,
                SortBy: "ProductionYear,SortName",
                SortOrder: "Descending"
            };
            "MusicArtist" === item.Type ? query.ContributingArtistIds = item.Id : apiClient.isMinServerVersion("3.4.1.18") ? query.AlbumArtistIds = item.AlbumArtists[0].Id : query.ArtistIds = item.AlbumArtists[0].Id, apiClient.getItems(apiClient.getCurrentUserId(), query).then(function(result) {
                if (!result.Items.length) return void section.classList.add("hide");
                section.classList.remove("hide"), "MusicArtist" === item.Type ? section.querySelector("h2").innerHTML = globalize.translate("sharedcomponents#HeaderAppearsOn") : section.querySelector("h2").innerHTML = globalize.translate("MoreFromValue", item.AlbumArtists[0].Name), cardBuilder.buildCards(result.Items, {
                    parentContainer: section,
                    itemsContainer: section.querySelector(".itemsContainer"),
                    shape: "autooverflow",
                    sectionTitleTagName: "h2",
                    scalable: !0,
                    coverImage: "MusicArtist" === item.Type || "MusicAlbum" === item.Type,
                    showTitle: !0,
                    showParentTitle: !1,
                    centerText: !0,
                    overlayText: !1,
                    overlayPlayButton: !0,
                    showYear: !0
                })
            })
        }
    }

    function renderSimilarItems(page, item, context) {
        var similarCollapsible = page.querySelector("#similarCollapsible");
        if (similarCollapsible) {
            if ("Movie" != item.Type && "Trailer" != item.Type && "Series" != item.Type && "Program" != item.Type && "Recording" != item.Type && "Game" != item.Type && "MusicAlbum" != item.Type && "MusicArtist" != item.Type && "Playlist" != item.Type) return void similarCollapsible.classList.add("hide");
            similarCollapsible.classList.remove("hide");
            var apiClient = connectionManager.getApiClient(item.ServerId),
                options = {
                    userId: apiClient.getCurrentUserId(),
                    limit: 12,
                    fields: "PrimaryImageAspectRatio,UserData,CanDelete"
                };
            "MusicAlbum" == item.Type && item.AlbumArtists && item.AlbumArtists.length && (options.ExcludeArtistIds = item.AlbumArtists[0].Id), apiClient.getSimilarItems(item.Id, options).then(function(result) {
                if (!result.Items.length) return void similarCollapsible.classList.add("hide");
                similarCollapsible.classList.remove("hide");
                var html = "";
                html += cardBuilder.getCardsHtml({
                    items: result.Items,
                    shape: "autooverflow",
                    showParentTitle: "MusicAlbum" == item.Type,
                    centerText: !0,
                    showTitle: !0,
                    context: context,
                    lazy: !0,
                    showDetailsMenu: !0,
                    coverImage: "MusicAlbum" == item.Type || "MusicArtist" == item.Type,
                    overlayPlayButton: !0,
                    overlayText: !1,
                    showYear: "Movie" === item.Type || "Trailer" === item.Type
                });
                var similarContent = similarCollapsible.querySelector(".similarContent");
                similarContent.innerHTML = html, imageLoader.lazyChildren(similarContent)
            })
        }
    }

    function renderSeriesAirTime(page, item, isStatic) {
        var seriesAirTime = page.querySelector("#seriesAirTime");
        if ("Series" != item.Type) return void seriesAirTime.classList.add("hide");
        var html = "";
        if (item.AirDays && item.AirDays.length && (html += 7 == item.AirDays.length ? "daily" : item.AirDays.map(function(a) {
                return a + "s"
            }).join(",")), item.AirTime && (html += " at " + item.AirTime), item.Studios.length)
            if (isStatic) html += " on " + item.Studios[0].Name;
            else {
                var context = inferContext(item),
                    href = appRouter.getRouteUrl(item.Studios[0], {
                        context: context,
                        itemType: "Studio",
                        serverId: item.ServerId
                    });
                html += ' on <a class="textlink button-link" is="emby-linkbutton" href="' + href + '">' + item.Studios[0].Name + "</a>"
            } html ? (html = ("Ended" == item.Status ? "Aired " : "Airs ") + html, seriesAirTime.innerHTML = html, seriesAirTime.classList.remove("hide")) : seriesAirTime.classList.add("hide")
    }

    function renderTags(page, item) {
        var itemTags = page.querySelector(".itemTags"),
            tagElements = [],
            tags = item.Tags || [];
        "Program" === item.Type && (tags = []);
        for (var i = 0, length = tags.length; i < length; i++) tagElements.push(tags[i]);
        tagElements.length ? (itemTags.innerHTML = globalize.translate("sharedcomponents#TagsValue", tagElements.join(", ")), itemTags.classList.remove("hide")) : (itemTags.innerHTML = "", itemTags.classList.add("hide"))
    }

    function renderChildren(page, item) {
        var fields = "ItemCounts,PrimaryImageAspectRatio,BasicSyncInfo,CanDelete,MediaSourceCount",
            query = {
                ParentId: item.Id,
                Fields: fields
            };
        "BoxSet" !== item.Type && (query.SortBy = "SortName");
        var promise, apiClient = connectionManager.getApiClient(item.ServerId),
            userId = apiClient.getCurrentUserId();
        "Series" == item.Type ? promise = apiClient.getSeasons(item.Id, {
            userId: userId,
            Fields: fields
        }) : "Season" == item.Type ? (fields += ",Overview", promise = apiClient.getEpisodes(item.SeriesId, {
            seasonId: item.Id,
            userId: userId,
            Fields: fields
        })) : "MusicAlbum" == item.Type || "MusicArtist" == item.Type && (query.SortBy = "ProductionYear,SortName"), promise = promise || apiClient.getItems(apiClient.getCurrentUserId(), query), promise.then(function(result) {
            var html = "",
                scrollX = !1,
                isList = !1,
                childrenItemsContainer = page.querySelector(".childrenItemsContainer");
            if ("MusicAlbum" == item.Type) html = listView.getListViewHtml({
                items: result.Items,
                smallIcon: !0,
                showIndex: !0,
                index: "disc",
                showIndexNumberLeft: !0,
                playFromHere: !0,
                action: "playallfromhere",
                image: !1,
                artist: "auto",
                containerAlbumArtists: item.AlbumArtists,
                addToListButton: !0
            }), isList = !0;
            else if ("Series" == item.Type) scrollX = enableScrollX(), html = cardBuilder.getCardsHtml({
                items: result.Items,
                shape: getPortraitShape(),
                showTitle: !0,
                centerText: !0,
                lazy: !0,
                overlayPlayButton: !0,
                allowBottomPadding: !scrollX
            });
            else if ("Season" == item.Type || "Episode" == item.Type) {
                if ("Episode" === item.Type || (isList = !0), scrollX = "Episode" == item.Type, result.Items.length < 2 && "Episode" === item.Type) return;
                "Episode" === item.Type ? html = cardBuilder.getCardsHtml({
                    items: result.Items,
                    shape: getThumbShape(scrollX),
                    showTitle: !0,
                    displayAsSpecial: "Season" == item.Type && item.IndexNumber,
                    playFromHere: !0,
                    overlayText: !0,
                    lazy: !0,
                    showDetailsMenu: !0,
                    overlayPlayButton: !0,
                    allowBottomPadding: !scrollX,
                    includeParentInfoInTitle: !1
                }) : "Season" === item.Type && (html = listView.getListViewHtml({
                    items: result.Items,
                    showIndexNumber: !1,
                    enableOverview: !0,
                    imageSize: "large",
                    enableSideMediaInfo: !1,
                    highlight: !1,
                    action: "none",
                    infoButton: !0,
                    imagePlayButton: !0,
                    includeParentInfoInTitle: !1
                }))
            } else "GameSystem" == item.Type && (html = cardBuilder.getCardsHtml({
                items: result.Items,
                shape: "auto",
                showTitle: !0,
                centerText: !0,
                lazy: !0,
                showDetailsMenu: !0
            }));
            if ("BoxSet" !== item.Type && page.querySelector("#childrenCollapsible").classList.remove("hide"), scrollX ? (childrenItemsContainer.classList.add("scrollX"), childrenItemsContainer.classList.add("hiddenScrollX"), childrenItemsContainer.classList.remove("vertical-wrap"), childrenItemsContainer.classList.remove("vertical-list")) : (childrenItemsContainer.classList.remove("scrollX"), childrenItemsContainer.classList.remove("hiddenScrollX"), childrenItemsContainer.classList.remove("smoothScrollX"), isList ? (childrenItemsContainer.classList.add("vertical-list"), childrenItemsContainer.classList.remove("vertical-wrap")) : (childrenItemsContainer.classList.add("vertical-wrap"), childrenItemsContainer.classList.remove("vertical-list"))), childrenItemsContainer.innerHTML = html, imageLoader.lazyChildren(childrenItemsContainer), "BoxSet" == item.Type) {
                var collectionItemTypes = [{
                    name: globalize.translate("HeaderVideos"),
                    mediaType: "Video"
                }, {
                    name: globalize.translate("HeaderSeries"),
                    type: "Series"
                }, {
                    name: globalize.translate("HeaderAlbums"),
                    type: "MusicAlbum"
                }, {
                    name: globalize.translate("HeaderGames"),
                    type: "Game"
                }, {
                    name: globalize.translate("HeaderBooks"),
                    type: "Book"
                }];
                renderCollectionItems(page, item, collectionItemTypes, result.Items)
            }
        }), "Season" == item.Type ? page.querySelector("#childrenTitle").innerHTML = globalize.translate("HeaderEpisodes") : "Series" == item.Type ? page.querySelector("#childrenTitle").innerHTML = globalize.translate("HeaderSeasons") : "MusicAlbum" == item.Type ? page.querySelector("#childrenTitle").innerHTML = globalize.translate("HeaderTracks") : "GameSystem" == item.Type ? page.querySelector("#childrenTitle").innerHTML = globalize.translate("HeaderGames") : page.querySelector("#childrenTitle").innerHTML = globalize.translate("HeaderItems"), "MusicAlbum" == item.Type || "Season" == item.Type ? (page.querySelector(".childrenSectionHeader").classList.add("hide"), page.querySelector("#childrenCollapsible").classList.add("verticalSection-extrabottompadding")) : page.querySelector(".childrenSectionHeader").classList.remove("hide")
    }

    function renderItemsByName(page, item, user) {
        require("scripts/itembynamedetailpage".split(","), function() {
            window.ItemsByName.renderItems(page, item)
        })
    }

    function renderPlaylistItems(page, item, user) {
        require("scripts/playlistedit".split(","), function() {
            PlaylistViewer.render(page, item)
        })
    }

    function renderProgramsForChannel(page, result) {
        for (var html = "", currentItems = [], currentStartDate = null, i = 0, length = result.Items.length; i < length; i++) {
            var item = result.Items[i],
                itemStartDate = datetime.parseISO8601Date(item.StartDate);
            currentStartDate && currentStartDate.toDateString() === itemStartDate.toDateString() || (currentItems.length && (html += '<div class="verticalSection verticalDetailSection">', html += '<h2 class="sectionTitle padded-left">' + datetime.toLocaleDateString(currentStartDate, {
                weekday: "long",
                month: "long",
                day: "numeric"
            }) + "</h2>", html += '<div is="emby-itemscontainer" class="vertical-list padded-left padded-right">' + listView.getListViewHtml({
                items: currentItems,
                enableUserDataButtons: !1,
                showParentTitle: !0,
                image: !1,
                showProgramTime: !0,
                mediaInfo: !1,
                parentTitleWithTitle: !0
            }) + "</div></div>"), currentStartDate = itemStartDate, currentItems = []), currentItems.push(item)
        }
        currentItems.length && (html += '<div class="verticalSection verticalDetailSection">', html += '<h2 class="sectionTitle padded-left">' + datetime.toLocaleDateString(currentStartDate, {
            weekday: "long",
            month: "long",
            day: "numeric"
        }) + "</h2>", html += '<div is="emby-itemscontainer" class="vertical-list padded-left padded-right">' + listView.getListViewHtml({
            items: currentItems,
            enableUserDataButtons: !1,
            showParentTitle: !0,
            image: !1,
            showProgramTime: !0,
            mediaInfo: !1,
            parentTitleWithTitle: !0
        }) + "</div></div>"), page.querySelector(".programGuide").innerHTML = html
    }

    function renderChannelGuide(page, apiClient, item) {
        "TvChannel" === item.Type && (page.querySelector(".programGuideSection").classList.remove("hide"), apiClient.getLiveTvPrograms({
            ChannelIds: item.Id,
            UserId: apiClient.getCurrentUserId(),
            HasAired: !1,
            SortBy: "StartDate",
            EnableTotalRecordCount: !1,
            EnableImages: !1,
            ImageTypeLimit: 0,
            EnableUserData: !1
        }).then(function(result) {
            renderProgramsForChannel(page, result)
        }))
    }

    function renderSeriesSchedule(page, item, user) {
        var apiClient = connectionManager.getApiClient(item.ServerId);
        apiClient.getLiveTvPrograms({
            UserId: apiClient.getCurrentUserId(),
            HasAired: !1,
            SortBy: "StartDate",
            EnableTotalRecordCount: !1,
            EnableImages: !1,
            ImageTypeLimit: 0,
            Limit: 50,
            EnableUserData: !1,
            LibrarySeriesId: item.Id
        }).then(function(result) {
            result.Items.length ? page.querySelector("#seriesScheduleSection").classList.remove("hide") : page.querySelector("#seriesScheduleSection").classList.add("hide"), page.querySelector("#seriesScheduleList").innerHTML = listView.getListViewHtml({
                items: result.Items,
                enableUserDataButtons: !1,
                showParentTitle: !1,
                image: !1,
                showProgramDateTime: !0,
                mediaInfo: !1,
                showTitle: !0,
                moreButton: !1,
                action: "programdialog"
            }), loading.hide()
        })
    }

    function inferContext(item) {
        return "Movie" === item.Type || "BoxSet" === item.Type ? "movies" : "Series" === item.Type || "Season" === item.Type || "Episode" === item.Type ? "tvshows" : "Game" === item.Type || "GameSystem" === item.Type ? "games" : "Game" === item.Type || "GameSystem" === item.Type ? "games" : "MusicArtist" === item.Type || "MusicAlbum" === item.Type || "Audio" === item.Type || "AudioBook" === item.Type ? "music" : "Program" === item.Type ? "livetv" : null
    }

    function filterItemsByCollectionItemType(items, typeInfo) {
        return items.filter(function(item) {
            return typeInfo.mediaType ? item.MediaType == typeInfo.mediaType : item.Type == typeInfo.type
        })
    }

    function renderCollectionItems(page, parentItem, types, items) {
        page.querySelector(".collectionItems").innerHTML = "";
        var i, length;
        for (i = 0, length = types.length; i < length; i++) {
            var type = types[i],
                typeItems = filterItemsByCollectionItemType(items, type);
            typeItems.length && renderCollectionItemType(page, parentItem, type, typeItems)
        }
        var otherType = {
                name: globalize.translate("HeaderOtherItems")
            },
            otherTypeItems = items.filter(function(curr) {
                return !types.filter(function(t) {
                    return filterItemsByCollectionItemType([curr], t).length > 0
                }).length
            });
        otherTypeItems.length && renderCollectionItemType(page, parentItem, otherType, otherTypeItems), items.length || renderCollectionItemType(page, parentItem, {
            name: globalize.translate("HeaderItems")
        }, items);
        var containers = page.querySelectorAll(".collectionItemsContainer"),
            notifyRefreshNeeded = function() {
                renderChildren(page, parentItem)
            };
        for (i = 0, length = containers.length; i < length; i++) containers[i].notifyRefreshNeeded = notifyRefreshNeeded
    }

    function renderCollectionItemType(page, parentItem, type, items) {
        var html = "";
        html += '<div class="verticalSection">', html += '<div class="sectionTitleContainer sectionTitleContainer-cards padded-left">', html += '<h2 class="sectionTitle sectionTitle-cards">', html += "<span>" + type.name + "</span>", html += "</h2>", html += '<button class="btnAddToCollection sectionTitleButton" type="button" is="paper-icon-button-light" style="margin-left:1em;"><i class="md-icon" icon="add">&#xE145;</i></button>', html += "</div>", html += '<div is="emby-itemscontainer" class="itemsContainer collectionItemsContainer vertical-wrap padded-left padded-right">';
        var shape = "MusicAlbum" == type.type ? getSquareShape(!1) : getPortraitShape(!1);
        html += cardBuilder.getCardsHtml({
            items: items,
            shape: shape,
            showTitle: !0,
            centerText: !0,
            lazy: !0,
            showDetailsMenu: !0,
            overlayMoreButton: !0,
            showAddToCollection: !1,
            showRemoveFromCollection: !0,
            collectionId: parentItem.Id
        }), html += "</div>", html += "</div>";
        var collectionItems = page.querySelector(".collectionItems");
        collectionItems.insertAdjacentHTML("beforeend", html), imageLoader.lazyChildren(collectionItems), collectionItems.querySelector(".btnAddToCollection").addEventListener("click", function() {
            require(["alert"], function(alert) {
                alert({
                    text: globalize.translate("AddItemToCollectionHelp"),
                    html: globalize.translate("AddItemToCollectionHelp") + '<br/><br/><a is="emby-linkbutton" class="button-link" target="_blank" href="https://web.archive.org/web/20181216120305/https://github.com/MediaBrowser/Wiki/wiki/Collections">' + globalize.translate("ButtonLearnMore") + "</a>"
                })
            })
        })
    }

    function renderMusicVideos(page, item, user) {
        connectionManager.getApiClient(item.ServerId).getItems(user.Id, {
            SortBy: "SortName",
            SortOrder: "Ascending",
            IncludeItemTypes: "MusicVideo",
            Recursive: !0,
            Fields: "PrimaryImageAspectRatio,BasicSyncInfo,CanDelete,MediaSourceCount",
            AlbumIds: item.Id
        }).then(function(result) {
            if (result.Items.length) {
                page.querySelector("#musicVideosCollapsible").classList.remove("hide");
                var musicVideosContent = page.querySelector(".musicVideosContent");
                musicVideosContent.innerHTML = getVideosHtml(result.Items, user), imageLoader.lazyChildren(musicVideosContent)
            } else page.querySelector("#musicVideosCollapsible").classList.add("hide")
        })
    }

    function renderAdditionalParts(page, item, user) {
        connectionManager.getApiClient(item.ServerId).getAdditionalVideoParts(user.Id, item.Id).then(function(result) {
            if (result.Items.length) {
                page.querySelector("#additionalPartsCollapsible").classList.remove("hide");
                var additionalPartsContent = page.querySelector("#additionalPartsContent");
                additionalPartsContent.innerHTML = getVideosHtml(result.Items, user), imageLoader.lazyChildren(additionalPartsContent)
            } else page.querySelector("#additionalPartsCollapsible").classList.add("hide")
        })
    }

    function renderScenes(page, item) {
        var chapters = item.Chapters || [];
        if (chapters.length && !chapters[0].ImageTag && (chapters = []), chapters.length) {
            page.querySelector("#scenesCollapsible").classList.remove("hide");
            var scenesContent = page.querySelector("#scenesContent");
            require(["chaptercardbuilder"], function(chaptercardbuilder) {
                chaptercardbuilder.buildChapterCards(item, chapters, {
                    itemsContainer: scenesContent,
                    width: 400,
                    backdropShape: "overflowBackdrop",
                    squareShape: "overflowSquare"
                })
            })
        } else page.querySelector("#scenesCollapsible").classList.add("hide")
    }

    function renderMediaSources(page, user, item) {
        var html = item.MediaSources.map(function(v) {
            return getMediaSourceHtml(user, item, v)
        }).join('<div style="border-top:1px solid #444;margin: 1em 0;"></div>');
        item.MediaSources.length > 1 && (html = "<br/>" + html), page.querySelector("#mediaInfoContent").innerHTML = html, html ? page.querySelector(".audioVideoMediaInfo").classList.remove("hide") : page.querySelector(".audioVideoMediaInfo").classList.add("hide")
    }

    function getMediaSourceHtml(user, item, version) {
        var html = "";
        version.Name && item.MediaSources.length > 1 && (html += '<div><span class="mediaInfoAttribute">' + version.Name + "</span></div><br/>");
        for (var i = 0, length = version.MediaStreams.length; i < length; i++) {
            var stream = version.MediaStreams[i];
            if ("Data" != stream.Type) {
                html += '<div class="mediaInfoStream">';
                html += '<h3 class="mediaInfoStreamType">' + globalize.translate("MediaInfoStreamType" + stream.Type) + "</h3>";
                var attributes = [];
                stream.DisplayTitle && attributes.push(createAttribute("Title", stream.DisplayTitle)), stream.Language && "Video" != stream.Type && attributes.push(createAttribute(globalize.translate("MediaInfoLanguage"), stream.Language)), stream.Codec && attributes.push(createAttribute(globalize.translate("MediaInfoCodec"), stream.Codec.toUpperCase())), stream.CodecTag && attributes.push(createAttribute(globalize.translate("MediaInfoCodecTag"), stream.CodecTag)), null != stream.IsAVC && attributes.push(createAttribute("AVC", stream.IsAVC ? "Yes" : "No")), stream.Profile && attributes.push(createAttribute(globalize.translate("MediaInfoProfile"), stream.Profile)), stream.Level && attributes.push(createAttribute(globalize.translate("MediaInfoLevel"), stream.Level)), (stream.Width || stream.Height) && attributes.push(createAttribute(globalize.translate("MediaInfoResolution"), stream.Width + "x" + stream.Height)), stream.AspectRatio && "mjpeg" != stream.Codec && attributes.push(createAttribute(globalize.translate("MediaInfoAspectRatio"), stream.AspectRatio)), "Video" == stream.Type && (null != stream.IsAnamorphic && attributes.push(createAttribute(globalize.translate("MediaInfoAnamorphic"), stream.IsAnamorphic ? "Yes" : "No")), attributes.push(createAttribute(globalize.translate("MediaInfoInterlaced"), stream.IsInterlaced ? "Yes" : "No"))), (stream.AverageFrameRate || stream.RealFrameRate) && attributes.push(createAttribute(globalize.translate("MediaInfoFramerate"), stream.AverageFrameRate || stream.RealFrameRate)), stream.ChannelLayout && attributes.push(createAttribute(globalize.translate("MediaInfoLayout"), stream.ChannelLayout)), stream.Channels && attributes.push(createAttribute(globalize.translate("MediaInfoChannels"), stream.Channels + " ch")), stream.BitRate && "mjpeg" != stream.Codec && attributes.push(createAttribute(globalize.translate("MediaInfoBitrate"), parseInt(stream.BitRate / 1e3) + " kbps")), stream.SampleRate && attributes.push(createAttribute(globalize.translate("MediaInfoSampleRate"), stream.SampleRate + " Hz")), stream.VideoRange && "SDR" !== stream.VideoRange && attributes.push(createAttribute(globalize.translate("sharedcomponents#VideoRange"), stream.VideoRange)), stream.ColorPrimaries && attributes.push(createAttribute(globalize.translate("sharedcomponents#ColorPrimaries"), stream.ColorPrimaries)), stream.ColorSpace && attributes.push(createAttribute(globalize.translate("sharedcomponents#ColorSpace"), stream.ColorSpace)), stream.ColorTransfer && attributes.push(createAttribute(globalize.translate("sharedcomponents#ColorTransfer"), stream.ColorTransfer)), stream.BitDepth && attributes.push(createAttribute(globalize.translate("MediaInfoBitDepth"), stream.BitDepth + " bit")), stream.PixelFormat && attributes.push(createAttribute(globalize.translate("MediaInfoPixelFormat"), stream.PixelFormat)), stream.RefFrames && attributes.push(createAttribute(globalize.translate("MediaInfoRefFrames"), stream.RefFrames)), stream.NalLengthSize && attributes.push(createAttribute("NAL", stream.NalLengthSize)), "Video" != stream.Type && attributes.push(createAttribute(globalize.translate("MediaInfoDefault"), stream.IsDefault ? "Yes" : "No")), "Subtitle" == stream.Type && (attributes.push(createAttribute(globalize.translate("MediaInfoForced"), stream.IsForced ? "Yes" : "No")), attributes.push(createAttribute(globalize.translate("MediaInfoExternal"), stream.IsExternal ? "Yes" : "No"))), "Video" == stream.Type && version.Timestamp && attributes.push(createAttribute(globalize.translate("MediaInfoTimestamp"), version.Timestamp)), html += attributes.join("<br/>"), html += "</div>"
            }
        }
        if (version.Container && (html += '<div><span class="mediaInfoLabel">' + globalize.translate("MediaInfoContainer") + '</span><span class="mediaInfoAttribute">' + version.Container + "</span></div>"), version.Formats && version.Formats.length, version.Path && "Http" != version.Protocol && user && user.Policy.IsAdministrator && (html += '<div><span class="mediaInfoLabel">' + globalize.translate("MediaInfoPath") + '</span><span class="mediaInfoAttribute">' + version.Path + "</span></div>"), version.Size) {
            var size = (version.Size / 1048576).toFixed(0);
            html += '<div><span class="mediaInfoLabel">' + globalize.translate("MediaInfoSize") + '</span><span class="mediaInfoAttribute">' + size + " MB</span></div>"
        }
        return html
    }

    function createAttribute(label, value) {
        return '<span class="mediaInfoLabel">' + label + '</span><span class="mediaInfoAttribute">' + value + "</span>"
    }

    function getVideosHtml(items, user, limit, moreButtonClass) {
        var html = cardBuilder.getCardsHtml({
            items: items,
            shape: "auto",
            showTitle: !0,
            action: "play",
            overlayText: !1,
            centerText: !0,
            showRuntime: !0
        });
        return limit && items.length > limit && (html += '<p style="margin: 0;padding-left:5px;"><button is="emby-button" type="button" class="raised more ' + moreButtonClass + '">' + globalize.translate("ButtonMore") + "</button></p>"), html
    }

    function renderSpecials(page, item, user, limit) {
        connectionManager.getApiClient(item.ServerId).getSpecialFeatures(user.Id, item.Id).then(function(specials) {
            var specialsContent = page.querySelector("#specialsContent");
            specialsContent.innerHTML = getVideosHtml(specials, user, limit, "moreSpecials"), imageLoader.lazyChildren(specialsContent)
        })
    }

    function renderCast(page, item, context, limit, isStatic) {
        var people = (item.People || []).filter(function(p) {
            return "Director" !== p.Type
        });
        if (!people.length) return void page.querySelector("#castCollapsible").classList.add("hide");
        page.querySelector("#castCollapsible").classList.remove("hide");
        var castContent = page.querySelector("#castContent");
        enableScrollX() ? (castContent.classList.add("scrollX"), limit = 32) : castContent.classList.add("vertical-wrap");
        var limitExceeded = limit && people.length > limit;
        limitExceeded && (people = people.slice(0), people.length = Math.min(limit, people.length)), require(["peoplecardbuilder"], function(peoplecardbuilder) {
            peoplecardbuilder.buildPeopleCards(people, {
                itemsContainer: castContent,
                coverImage: !0,
                serverId: item.ServerId,
                width: 160,
                shape: getPortraitShape()
            })
        });
        var morePeopleButton = page.querySelector(".morePeople");
        morePeopleButton && (limitExceeded && !enableScrollX() ? morePeopleButton.classList.remove("hide") : morePeopleButton.classList.add("hide"))
    }

    function itemDetailPage() {
        var self = this;
        self.setInitialCollapsibleState = setInitialCollapsibleState, self.renderDetails = renderDetails, self.renderCast = renderCast, self.renderMediaSources = renderMediaSources
    }

    function bindAll(view, selector, eventName, fn) {
        var i, length, elems = view.querySelectorAll(selector);
        for (i = 0, length = elems.length; i < length; i++) elems[i].addEventListener(eventName, fn)
    }

    function onTrackSelectionsSubmit(e) {
        return e.preventDefault(), !1
    }
    return window.ItemDetailPage = new itemDetailPage,
        function(view, params) {
            function reload(instance, page, params) {
                loading.show();
                var apiClient = params.serverId ? connectionManager.getApiClient(params.serverId) : ApiClient,
                    promises = [getPromise(apiClient, params), apiClient.getCurrentUser()];
                Promise.all(promises).then(function(responses) {
                    var item = responses[0],
                        user = responses[1];
                    currentItem = item, reloadFromItem(instance, page, params, item, user)
                })
            }

            function splitVersions(instance, page, apiClient, params) {
                require(["confirm"], function(confirm) {
                    confirm("Are you sure you wish to split the media sources into separate items?", "Split Media Apart").then(function() {
                        loading.show(), apiClient.ajax({
                            type: "DELETE",
                            url: apiClient.getUrl("Videos/" + params.id + "/AlternateSources")
                        }).then(function() {
                            loading.hide(), reload(instance, page, params)
                        })
                    })
                })
            }

            function getPlayOptions(startPosition) {
                var audioStreamIndex = view.querySelector(".selectAudio").value || null;
                return {
                    startPositionTicks: startPosition,
                    mediaSourceId: view.querySelector(".selectSource").value,
                    audioStreamIndex: audioStreamIndex,
                    subtitleStreamIndex: view.querySelector(".selectSubtitles").value
                }
            }

            function playItem(item, startPosition) {
                var playOptions = getPlayOptions(startPosition);
                playOptions.items = [item], playbackManager.play(playOptions)
            }

            function playTrailer(page) {
                playbackManager.playTrailers(currentItem)
            }

            function playCurrentItem(button, mode) {
                var item = currentItem;
                if ("Program" === item.Type) {
                    var apiClient = connectionManager.getApiClient(item.ServerId);
                    return void apiClient.getLiveTvChannel(item.ChannelId, apiClient.getCurrentUserId()).then(function(channel) {
                        playbackManager.play({
                            items: [channel]
                        })
                    })
                }
                playItem(item, item.UserData && "resume" === mode ? item.UserData.PlaybackPositionTicks : 0)
            }

            function onPlayClick() {
                playCurrentItem(this, this.getAttribute("data-mode"))
            }

            function onInstantMixClick() {
                playbackManager.instantMix(currentItem)
            }

            function onShuffleClick() {
                playbackManager.shuffle(currentItem)
            }

            function onDeleteClick() {
                require(["deleteHelper"], function(deleteHelper) {
                    deleteHelper.deleteItem({
                        item: currentItem,
                        navigate: !0
                    })
                })
            }

            function onCancelSeriesTimerClick() {
                require(["recordingHelper"], function(recordingHelper) {
                    recordingHelper.cancelSeriesTimerWithConfirmation(currentItem.Id, currentItem.ServerId).then(function() {
                        Dashboard.navigate("livetv.html")
                    })
                })
            }

            function onCancelTimerClick() {
                require(["recordingHelper"], function(recordingHelper) {
                    recordingHelper.cancelTimer(connectionManager.getApiClient(currentItem.ServerId), currentItem.TimerId).then(function() {
                        reload(self, view, params)
                    })
                })
            }

            function onPlayTrailerClick() {
                playTrailer(view)
            }

            function onDownloadChange() {
                reload(self, view, params)
            }

            function onMoreCommandsClick() {
                var button = this;
                apiClient.getCurrentUser().then(function(user) {
                    itemContextMenu.show(getContextMenuOptions(currentItem, user, button)).then(function(result) {
                        result.deleted ? appRouter.goHome() : result.updated && reload(self, view, params)
                    })
                })
            }

            function onPlayerChange() {
                renderTrackSelections(view, self, currentItem), setTrailerButtonVisibility(view, currentItem)
            }

            function editImages() {
                return new Promise(function(resolve, reject) {
                    require(["imageEditor"], function(imageEditor) {
                        imageEditor.show({
                            itemId: currentItem.Id,
                            serverId: currentItem.ServerId
                        }).then(resolve, reject)
                    })
                })
            }

            function onWebSocketMessage(e, data) {
                var msg = data;
                if ("UserDataChanged" === msg.MessageType && currentItem && msg.Data.UserId == apiClient.getCurrentUserId()) {
                    var key = currentItem.UserData.Key,
                        userData = msg.Data.UserDataList.filter(function(u) {
                            return u.Key == key
                        })[0];
                    userData && (currentItem.UserData = userData, reloadPlayButtons(view, currentItem), apiClient.getCurrentUser().then(function(user) {
                        refreshImage(view, currentItem, user)
                    }))
                }
            }
            var currentItem, self = this,
                apiClient = params.serverId ? connectionManager.getApiClient(params.serverId) : ApiClient;
            view.querySelectorAll(".btnPlay");
            bindAll(view, ".btnPlay", "click", onPlayClick), bindAll(view, ".btnResume", "click", onPlayClick), bindAll(view, ".btnInstantMix", "click", onInstantMixClick), bindAll(view, ".btnShuffle", "click", onShuffleClick), bindAll(view, ".btnPlayTrailer", "click", onPlayTrailerClick), bindAll(view, ".btnCancelSeriesTimer", "click", onCancelSeriesTimerClick), bindAll(view, ".btnCancelTimer", "click", onCancelTimerClick), bindAll(view, ".btnDeleteItem", "click", onDeleteClick), bindAll(view, ".btnSyncDownload", "download", onDownloadChange), bindAll(view, ".btnSyncDownload", "download-cancel", onDownloadChange), view.querySelector(".btnMoreCommands i").innerHTML = "&#xE5D3;", view.querySelector(".trackSelections").addEventListener("submit", onTrackSelectionsSubmit), view.querySelector(".btnSplitVersions").addEventListener("click", function() {
                splitVersions(self, view, apiClient, params)
            }), bindAll(view, ".btnMoreCommands", "click", onMoreCommandsClick), view.querySelector(".selectSource").addEventListener("change", function() {
                renderVideoSelections(view, self._currentPlaybackMediaSources), renderAudioSelections(view, self._currentPlaybackMediaSources), renderSubtitleSelections(view, self._currentPlaybackMediaSources)
            }), view.addEventListener("click", function(e) {
                dom.parentWithClass(e.target, "moreScenes") ? apiClient.getCurrentUser().then(function(user) {
                    renderScenes(view, currentItem)
                }) : dom.parentWithClass(e.target, "morePeople") ? renderCast(view, currentItem, params.context) : dom.parentWithClass(e.target, "moreSpecials") && apiClient.getCurrentUser().then(function(user) {
                    renderSpecials(view, currentItem, user)
                })
            }), view.querySelector(".detailImageContainer").addEventListener("click", function(e) {
                dom.parentWithClass(e.target, "itemDetailGalleryLink") && editImages().then(function() {
                    reload(self, view, params)
                })
            }), view.addEventListener("viewshow", function(e) {
                var page = this;
                libraryMenu.setTransparentMenu(!0), e.detail.isRestored ? currentItem && (setTitle(currentItem, connectionManager.getApiClient(currentItem.ServerId)), renderTrackSelections(page, self, currentItem, !0)) : reload(self, page, params), events.on(apiClient, "message", onWebSocketMessage), events.on(playbackManager, "playerchange", onPlayerChange)
            }), view.addEventListener("viewbeforehide", function() {
                events.off(apiClient, "message", onWebSocketMessage), events.off(playbackManager, "playerchange", onPlayerChange), libraryMenu.setTransparentMenu(!1)
            }), view.addEventListener("viewdestroy", function() {
                currentItem = null, self._currentPlaybackMediaSources = null, self.currentRecordingFields = null
            })
        }
});