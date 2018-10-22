define(["playbackManager", "dom", "inputmanager", "datetime", "itemHelper", "mediaInfo", "focusManager", "imageLoader", "scrollHelper", "events", "connectionManager", "browser", "globalize", "apphost", "layoutManager", "userSettings", "scrollStyles", "emby-slider", "paper-icon-button-light", "css!css/videoosd"], function(playbackManager, dom, inputManager, datetime, itemHelper, mediaInfo, focusManager, imageLoader, scrollHelper, events, connectionManager, browser, globalize, appHost, layoutManager, userSettings) {
    "use strict";

    function seriesImageUrl(item, options) {
        if ("Episode" !== item.Type) return null;
        if (options = options || {}, options.type = options.type || "Primary", "Primary" === options.type && item.SeriesPrimaryImageTag) return options.tag = item.SeriesPrimaryImageTag, connectionManager.getApiClient(item.ServerId).getScaledImageUrl(item.SeriesId, options);
        if ("Thumb" === options.type) {
            if (item.SeriesThumbImageTag) return options.tag = item.SeriesThumbImageTag, connectionManager.getApiClient(item.ServerId).getScaledImageUrl(item.SeriesId, options);
            if (item.ParentThumbImageTag) return options.tag = item.ParentThumbImageTag, connectionManager.getApiClient(item.ServerId).getScaledImageUrl(item.ParentThumbItemId, options)
        }
        return null
    }

    function imageUrl(item, options) {
        return options = options || {}, options.type = options.type || "Primary", item.ImageTags && item.ImageTags[options.type] ? (options.tag = item.ImageTags[options.type], connectionManager.getApiClient(item.ServerId).getScaledImageUrl(item.PrimaryImageItemId || item.Id, options)) : "Primary" === options.type && item.AlbumId && item.AlbumPrimaryImageTag ? (options.tag = item.AlbumPrimaryImageTag, connectionManager.getApiClient(item.ServerId).getScaledImageUrl(item.AlbumId, options)) : null
    }

    function logoImageUrl(item, apiClient, options) {
        return options = options || {}, options.type = "Logo", item.ImageTags && item.ImageTags.Logo ? (options.tag = item.ImageTags.Logo, apiClient.getScaledImageUrl(item.Id, options)) : item.ParentLogoImageTag ? (options.tag = item.ParentLogoImageTag, apiClient.getScaledImageUrl(item.ParentLogoItemId, options)) : null
    }
    return function(view, params) {
        function onVerticalSwipe(e, elem, data) {
            var player = currentPlayer;
            if (player) {
                var deltaY = data.currentDeltaY,
                    windowSize = dom.getWindowSize();
                if (supportsBrightnessChange && data.clientX < windowSize.innerWidth / 2) return void doBrightnessTouch(deltaY, player, windowSize.innerHeight);
                doVolumeTouch(deltaY, player, windowSize.innerHeight)
            }
        }

        function doBrightnessTouch(deltaY, player, viewHeight) {
            var delta = -deltaY / viewHeight * 100,
                newValue = playbackManager.getBrightness(player) + delta;
            newValue = Math.min(newValue, 100), newValue = Math.max(newValue, 0), playbackManager.setBrightness(newValue, player)
        }

        function doVolumeTouch(deltaY, player, viewHeight) {
            var delta = -deltaY / viewHeight * 100,
                newValue = playbackManager.getVolume(player) + delta;
            newValue = Math.min(newValue, 100), newValue = Math.max(newValue, 0), playbackManager.setVolume(newValue, player)
        }

        function onDoubleClick(e) {
            var clientX = e.clientX;
            if (null != clientX) {
                clientX < dom.getWindowSize().innerWidth / 2 ? playbackManager.rewind(currentPlayer) : playbackManager.fastForward(currentPlayer), e.preventDefault(), e.stopPropagation()
            }
        }

        function getDisplayItem(item) {
            if ("TvChannel" === item.Type) {
                var apiClient = connectionManager.getApiClient(item.ServerId);
                return apiClient.getItem(apiClient.getCurrentUserId(), item.Id).then(function(refreshedItem) {
                    return {
                        originalItem: refreshedItem,
                        displayItem: refreshedItem.CurrentProgram
                    }
                })
            }
            return Promise.resolve({
                originalItem: item
            })
        }

        function updateRecordingButton(item) {
            if (!item || "Program" !== item.Type) return recordingButtonManager && (recordingButtonManager.destroy(), recordingButtonManager = null), void view.querySelector(".btnRecord").classList.add("hide");
            connectionManager.getApiClient(item.ServerId).getCurrentUser().then(function(user) {
                user.Policy.EnableLiveTvManagement && require(["recordingButton"], function(RecordingButton) {
                    if (recordingButtonManager) return void recordingButtonManager.refreshItem(item);
                    recordingButtonManager = new RecordingButton({
                        item: item,
                        button: view.querySelector(".btnRecord")
                    }), view.querySelector(".btnRecord").classList.remove("hide")
                })
            })
        }

        function updateDisplayItem(itemInfo) {
            var item = itemInfo.originalItem;
            currentItem = item;
            var displayItem = itemInfo.displayItem || item;
            updateRecordingButton(displayItem), setPoster(displayItem, item);
            var parentName = displayItem.SeriesName || displayItem.Album;
            (displayItem.EpisodeTitle || displayItem.IsSeries) && (parentName = displayItem.Name), setTitle(displayItem, parentName);
            var titleElement, osdTitle = view.querySelector(".osdTitle");
            titleElement = osdTitle;
            var displayName = itemHelper.getDisplayName(displayItem, {
                includeParentInfo: "Program" !== displayItem.Type,
                includeIndexNumber: "Program" !== displayItem.Type
            });
            !displayName && displayItem.Type, titleElement.innerHTML = displayName, displayName ? titleElement.classList.remove("hide") : titleElement.classList.add("hide");
            var mediaInfoHtml = mediaInfo.getPrimaryMediaInfoHtml(displayItem, {
                    runtime: !1,
                    subtitles: !1,
                    tomatoes: !1,
                    endsAt: !1,
                    episodeTitle: !1,
                    originalAirDate: "Program" !== displayItem.Type,
                    episodeTitleIndexNumber: "Program" !== displayItem.Type,
                    programIndicator: !1
                }),
                osdMediaInfo = view.querySelector(".osdMediaInfo");
            osdMediaInfo.innerHTML = mediaInfoHtml, mediaInfoHtml ? osdMediaInfo.classList.remove("hide") : osdMediaInfo.classList.add("hide");
            var secondaryMediaInfo = view.querySelector(".osdSecondaryMediaInfo"),
                secondaryMediaInfoHtml = mediaInfo.getSecondaryMediaInfoHtml(displayItem, {
                    startDate: !1,
                    programTime: !1
                });
            secondaryMediaInfo.innerHTML = secondaryMediaInfoHtml, secondaryMediaInfoHtml ? secondaryMediaInfo.classList.remove("hide") : secondaryMediaInfo.classList.add("hide"), displayName ? view.querySelector(".osdMainTextContainer").classList.remove("hide") : view.querySelector(".osdMainTextContainer").classList.add("hide"), enableProgressByTimeOfDay ? (setDisplayTime(startTimeText, displayItem.StartDate), setDisplayTime(endTimeText, displayItem.EndDate), startTimeText.classList.remove("hide"), endTimeText.classList.remove("hide"), programStartDateMs = displayItem.StartDate ? datetime.parseISO8601Date(displayItem.StartDate).getTime() : 0, programEndDateMs = displayItem.EndDate ? datetime.parseISO8601Date(displayItem.EndDate).getTime() : 0) : (startTimeText.classList.add("hide"), endTimeText.classList.add("hide"), startTimeText.innerHTML = "", endTimeText.innerHTML = "", programStartDateMs = 0, programEndDateMs = 0)
        }

        function getDisplayTimeWithoutAmPm(date, showSeconds) {
            return showSeconds ? datetime.toLocaleTimeString(date, {
                hour: "numeric",
                minute: "2-digit",
                second: "2-digit"
            }).toLowerCase().replace("am", "").replace("pm", "").trim() : datetime.getDisplayTime(date).toLowerCase().replace("am", "").replace("pm", "").trim()
        }

        function setDisplayTime(elem, date) {
            var html;
            date && (date = datetime.parseISO8601Date(date), html = getDisplayTimeWithoutAmPm(date)), elem.innerHTML = html || ""
        }

        function shouldEnableProgressByTimeOfDay(item) {
            return !("TvChannel" !== item.Type || !item.CurrentProgram)
        }

        function updateNowPlayingInfo(player, state) {
            var item = state.NowPlayingItem;
            if (currentItem = item, !item) return setPoster(null), updateRecordingButton(null), Emby.Page.setTitle(""), nowPlayingVolumeSlider.disabled = !0, nowPlayingPositionSlider.disabled = !0, btnFastForward.disabled = !0, btnRewind.disabled = !0, view.querySelector(".btnSubtitles").classList.add("hide"), view.querySelector(".btnAudio").classList.add("hide"), view.querySelector(".osdTitle").innerHTML = "", void(view.querySelector(".osdMediaInfo").innerHTML = "");
            enableProgressByTimeOfDay = shouldEnableProgressByTimeOfDay(item), getDisplayItem(item).then(updateDisplayItem), nowPlayingVolumeSlider.disabled = !1, nowPlayingPositionSlider.disabled = !1, btnFastForward.disabled = !1, btnRewind.disabled = !1, playbackManager.subtitleTracks(player).length ? view.querySelector(".btnSubtitles").classList.remove("hide") : view.querySelector(".btnSubtitles").classList.add("hide"), playbackManager.audioTracks(player).length > 1 ? view.querySelector(".btnAudio").classList.remove("hide") : view.querySelector(".btnAudio").classList.add("hide")
        }

        function setTitle(item, parentName) {
            var url = logoImageUrl(item, connectionManager.getApiClient(item.ServerId), {});
            if (url) {
                Emby.Page.setTitle("");
                var pageTitle = document.querySelector(".pageTitle");
                pageTitle.style.backgroundImage = "url('" + url + "')", pageTitle.classList.add("pageTitleWithLogo"), pageTitle.classList.remove("pageTitleWithDefaultLogo"), pageTitle.innerHTML = ""
            } else Emby.Page.setTitle(parentName || "");
            var documentTitle = parentName || (item ? item.Name : null);
            documentTitle && (document.title = documentTitle)
        }

        function setPoster(item, secondaryItem) {
            var osdPoster = view.querySelector(".osdPoster");
            if (item) {
                var imgUrl = seriesImageUrl(item, {
                    type: "Primary"
                }) || seriesImageUrl(item, {
                    type: "Thumb"
                }) || imageUrl(item, {
                    type: "Primary"
                });
                if (!imgUrl && secondaryItem && (imgUrl = seriesImageUrl(secondaryItem, {
                        type: "Primary"
                    }) || seriesImageUrl(secondaryItem, {
                        type: "Thumb"
                    }) || imageUrl(secondaryItem, {
                        type: "Primary"
                    })), imgUrl) return void(osdPoster.innerHTML = '<img src="' + imgUrl + '" />')
            }
            osdPoster.innerHTML = ""
        }

        function showOsd() {
            slideDownToShow(headerElement), showMainOsdControls(), startOsdHideTimer()
        }

        function hideOsd() {
            slideUpToHide(headerElement), hideMainOsdControls()
        }

        function toggleOsd() {
            "osd" === currentVisibleMenu ? hideOsd() : currentVisibleMenu || showOsd()
        }

        function startOsdHideTimer() {
            stopOsdHideTimer(), osdHideTimeout = setTimeout(hideOsd, 5e3)
        }

        function stopOsdHideTimer() {
            osdHideTimeout && (clearTimeout(osdHideTimeout), osdHideTimeout = null)
        }

        function slideDownToShow(elem) {
            elem.classList.remove("osdHeader-hidden")
        }

        function slideUpToHide(elem) {
            elem.classList.add("osdHeader-hidden")
        }

        function clearHideAnimationEventListeners(elem) {
            dom.removeEventListener(elem, transitionEndEventName, onHideAnimationComplete, {
                once: !0
            })
        }

        function onHideAnimationComplete(e) {
            var elem = e.target;
            elem.classList.add("hide"), dom.removeEventListener(elem, transitionEndEventName, onHideAnimationComplete, {
                once: !0
            })
        }

        function showMainOsdControls() {
            if (!currentVisibleMenu) {
                var elem = osdBottomElement;
                currentVisibleMenu = "osd", clearHideAnimationEventListeners(elem), elem.classList.remove("hide"), elem.offsetWidth, elem.classList.remove("videoOsdBottom-hidden"), layoutManager.mobile || setTimeout(function() {
                    focusManager.focus(elem.querySelector(".btnPause"))
                }, 50)
            }
        }

        function hideMainOsdControls() {
            if ("osd" === currentVisibleMenu) {
                var elem = osdBottomElement;
                clearHideAnimationEventListeners(elem), elem.offsetWidth, elem.classList.add("videoOsdBottom-hidden"), dom.addEventListener(elem, transitionEndEventName, onHideAnimationComplete, {
                    once: !0
                }), currentVisibleMenu = null
            }
        }

        function onPointerMove(e) {
            if ("mouse" === (e.pointerType || (layoutManager.mobile ? "touch" : "mouse"))) {
                var eventX = e.screenX || 0,
                    eventY = e.screenY || 0,
                    obj = lastPointerMoveData;
                if (!obj) return void(lastPointerMoveData = {
                    x: eventX,
                    y: eventY
                });
                if (Math.abs(eventX - obj.x) < 10 && Math.abs(eventY - obj.y) < 10) return;
                obj.x = eventX, obj.y = eventY, showOsd()
            }
        }

        function onInputCommand(e) {
            var player = currentPlayer;
            switch (e.detail.command) {
                case "left":
                    "osd" === currentVisibleMenu ? showOsd() : currentVisibleMenu || (e.preventDefault(), playbackManager.rewind(player));
                    break;
                case "right":
                    "osd" === currentVisibleMenu ? showOsd() : currentVisibleMenu || (e.preventDefault(), playbackManager.fastForward(player));
                    break;
                case "pageup":
                    playbackManager.nextChapter(player);
                    break;
                case "pagedown":
                    playbackManager.previousChapter(player);
                    break;
                case "up":
                case "down":
                case "select":
                case "menu":
                case "info":
                case "play":
                case "playpause":
                case "pause":
                case "fastforward":
                case "rewind":
                case "next":
                case "previous":
                    showOsd();
                    break;
                case "record":
                    onRecordingCommand(), showOsd();
                    break;
                case "togglestats":
                    toggleStats()
            }
        }

        function onRecordingCommand() {
            var btnRecord = view.querySelector(".btnRecord");
            btnRecord.classList.contains("hide") || btnRecord.click()
        }

        function updateFullscreenIcon() {
            playbackManager.isFullscreen(currentPlayer) ? (view.querySelector(".btnFullscreen").setAttribute("title", globalize.translate("ExitFullscreen")), view.querySelector(".btnFullscreen i").innerHTML = "&#xE5D1;") : (view.querySelector(".btnFullscreen").setAttribute("title", globalize.translate("Fullscreen")), view.querySelector(".btnFullscreen i").innerHTML = "&#xE5D0;")
        }

        function onPlayerChange() {
            bindToPlayer(playbackManager.getCurrentPlayer())
        }

        function onStateChanged(event, state) {
            var player = this;
            state.NowPlayingItem && (isEnabled = !0, updatePlayerStateInternal(event, player, state), updatePlaylist(player), enableStopOnBack(!0))
        }

        function onPlayPauseStateChanged(e) {
            if (isEnabled) {
                updatePlayPauseState(this.paused())
            }
        }

        function onVolumeChanged(e) {
            if (isEnabled) {
                var player = this;
                updatePlayerVolumeState(player, player.isMuted(), player.getVolume())
            }
        }

        function onPlaybackStart(e, state) {
            console.log("nowplaying event: " + e.type);
            var player = this;
            onStateChanged.call(player, e, state), resetUpNextDialog()
        }

        function resetUpNextDialog() {
            comingUpNextDisplayed = !1;
            var dlg = currentUpNextDialog;
            dlg && (dlg.destroy(), currentUpNextDialog = null)
        }

        function onPlaybackStopped(e, state) {
            currentRuntimeTicks = null, resetUpNextDialog(), console.log("nowplaying event: " + e.type), "Video" !== state.NextMediaType && (view.removeEventListener("viewbeforehide", onViewHideStopPlayback), Emby.Page.back())
        }

        function onMediaStreamsChanged(e) {
            var player = this,
                state = playbackManager.getPlayerState(player);
            onStateChanged.call(player, {
                type: "init"
            }, state)
        }

        function bindToPlayer(player) {
            if (player !== currentPlayer && (releaseCurrentPlayer(), currentPlayer = player, player)) {
                var state = playbackManager.getPlayerState(player);
                onStateChanged.call(player, {
                    type: "init"
                }, state), events.on(player, "playbackstart", onPlaybackStart), events.on(player, "playbackstop", onPlaybackStopped), events.on(player, "volumechange", onVolumeChanged), events.on(player, "pause", onPlayPauseStateChanged), events.on(player, "unpause", onPlayPauseStateChanged), events.on(player, "timeupdate", onTimeUpdate), events.on(player, "fullscreenchange", updateFullscreenIcon), events.on(player, "mediastreamschange", onMediaStreamsChanged), resetUpNextDialog()
            }
        }

        function releaseCurrentPlayer() {
            destroyStats(), resetUpNextDialog();
            var player = currentPlayer;
            player && (events.off(player, "playbackstart", onPlaybackStart), events.off(player, "playbackstop", onPlaybackStopped), events.off(player, "volumechange", onVolumeChanged), events.off(player, "pause", onPlayPauseStateChanged), events.off(player, "unpause", onPlayPauseStateChanged), events.off(player, "timeupdate", onTimeUpdate), events.off(player, "fullscreenchange", updateFullscreenIcon), events.off(player, "mediastreamschange", onMediaStreamsChanged), currentPlayer = null)
        }

        function onTimeUpdate(e) {
            if (isEnabled) {
                var now = (new Date).getTime();
                if (!(now - lastUpdateTime < 700)) {
                    lastUpdateTime = now;
                    var player = this;
                    currentRuntimeTicks = playbackManager.duration(player);
                    var currentTime = playbackManager.currentTime(player);
                    updateTimeDisplay(currentTime, currentRuntimeTicks, playbackManager.playbackStartTime(player), playbackManager.getBufferedRanges(player));
                    var item = currentItem;
                    refreshProgramInfoIfNeeded(player, item), showComingUpNextIfNeeded(player, item, currentTime, currentRuntimeTicks)
                }
            }
        }

        function showComingUpNextIfNeeded(player, currentItem, currentTimeTicks, runtimeTicks) {
            if (runtimeTicks && currentTimeTicks && !comingUpNextDisplayed && !currentVisibleMenu && "Episode" === currentItem.Type && userSettings.enableNextVideoInfoOverlay()) {
                var showAtSecondsLeft = runtimeTicks >= 3e10 ? 40 : runtimeTicks >= 24e9 ? 35 : 30,
                    showAtTicks = runtimeTicks - 1e3 * showAtSecondsLeft * 1e4,
                    timeRemainingTicks = runtimeTicks - currentTimeTicks;
                currentTimeTicks >= showAtTicks && runtimeTicks >= 6e9 && timeRemainingTicks >= 2e8 && showComingUpNext(player)
            }
        }

        function onUpNextHidden() {
            "upnext" === currentVisibleMenu && (currentVisibleMenu = null)
        }

        function showComingUpNext(player) {
            require(["upNextDialog"], function(UpNextDialog) {
                currentVisibleMenu || currentUpNextDialog || (currentVisibleMenu = "upnext", comingUpNextDisplayed = !0, playbackManager.nextItem(player).then(function(nextItem) {
                    currentUpNextDialog = new UpNextDialog({
                        parent: view.querySelector(".upNextContainer"),
                        player: player,
                        nextItem: nextItem
                    }), events.on(currentUpNextDialog, "hide", onUpNextHidden)
                }, onUpNextHidden))
            })
        }

        function refreshProgramInfoIfNeeded(player, item) {
            if ("TvChannel" === item.Type) {
                var program = item.CurrentProgram;
                if (program && program.EndDate) try {
                    var endDate = datetime.parseISO8601Date(program.EndDate);
                    if ((new Date).getTime() >= endDate.getTime()) {
                        console.log("program info needs to be refreshed");
                        var state = playbackManager.getPlayerState(player);
                        onStateChanged.call(player, {
                            type: "init"
                        }, state)
                    }
                } catch (e) {
                    console.log("Error parsing date: " + program.EndDate)
                }
            }
        }

        function updatePlayPauseState(isPaused) {
            view.querySelector(".btnPause i").innerHTML = isPaused ? "&#xE037;" : "&#xE034;"
        }

        function updatePlayerStateInternal(event, player, state) {
            var playState = state.PlayState || {};
            updatePlayPauseState(playState.IsPaused);
            var supportedCommands = playbackManager.getSupportedCommands(player);
            currentPlayerSupportedCommands = supportedCommands, supportsBrightnessChange = -1 !== supportedCommands.indexOf("SetBrightness"), updatePlayerVolumeState(player, playState.IsMuted, playState.VolumeLevel), nowPlayingPositionSlider && !nowPlayingPositionSlider.dragging && (nowPlayingPositionSlider.disabled = !playState.CanSeek), btnFastForward.disabled = !playState.CanSeek, btnRewind.disabled = !playState.CanSeek;
            var nowPlayingItem = state.NowPlayingItem || {};
            playbackStartTimeTicks = playState.PlaybackStartTimeTicks, updateTimeDisplay(playState.PositionTicks, nowPlayingItem.RunTimeTicks, playState.PlaybackStartTimeTicks, playState.BufferedRanges || []), updateNowPlayingInfo(player, state), state.MediaSource && state.MediaSource.SupportsTranscoding && -1 !== supportedCommands.indexOf("SetMaxStreamingBitrate") ? view.querySelector(".btnVideoOsdSettings").classList.remove("hide") : view.querySelector(".btnVideoOsdSettings").classList.add("hide");
            var isProgressClear = state.MediaSource && null == state.MediaSource.RunTimeTicks;
            nowPlayingPositionSlider.setIsClear(isProgressClear), -1 === supportedCommands.indexOf("ToggleFullscreen") || player.isLocalPlayer && layoutManager.tv && playbackManager.isFullscreen(player) ? view.querySelector(".btnFullscreen").classList.add("hide") : view.querySelector(".btnFullscreen").classList.remove("hide"), -1 === supportedCommands.indexOf("PictureInPicture") ? view.querySelector(".btnPip").classList.add("hide") : view.querySelector(".btnPip").classList.remove("hide"), updateFullscreenIcon()
        }

        function getDisplayPercentByTimeOfDay(programStartDateMs, programRuntimeMs, currentTimeMs) {
            return (currentTimeMs - programStartDateMs) / programRuntimeMs * 100
        }

        function updateTimeDisplay(positionTicks, runtimeTicks, playbackStartTimeTicks, bufferedRanges) {
            if (enableProgressByTimeOfDay) {
                if (nowPlayingPositionSlider && !nowPlayingPositionSlider.dragging)
                    if (programStartDateMs && programEndDateMs) {
                        var currentTimeMs = (playbackStartTimeTicks + (positionTicks || 0)) / 1e4,
                            programRuntimeMs = programEndDateMs - programStartDateMs;
                        if (nowPlayingPositionSlider.value = getDisplayPercentByTimeOfDay(programStartDateMs, programRuntimeMs, currentTimeMs), bufferedRanges.length) {
                            var rangeStart = getDisplayPercentByTimeOfDay(programStartDateMs, programRuntimeMs, (playbackStartTimeTicks + (bufferedRanges[0].start || 0)) / 1e4),
                                rangeEnd = getDisplayPercentByTimeOfDay(programStartDateMs, programRuntimeMs, (playbackStartTimeTicks + (bufferedRanges[0].end || 0)) / 1e4);
                            nowPlayingPositionSlider.setBufferedRanges([{
                                start: rangeStart,
                                end: rangeEnd
                            }])
                        } else nowPlayingPositionSlider.setBufferedRanges([])
                    } else nowPlayingPositionSlider.value = 0, nowPlayingPositionSlider.setBufferedRanges([]);
                nowPlayingPositionText.innerHTML = "", nowPlayingDurationText.innerHTML = ""
            } else {
                if (nowPlayingPositionSlider && !nowPlayingPositionSlider.dragging) {
                    if (runtimeTicks) {
                        var pct = positionTicks / runtimeTicks;
                        pct *= 100, nowPlayingPositionSlider.value = pct
                    } else nowPlayingPositionSlider.value = 0;
                    runtimeTicks && null != positionTicks && currentRuntimeTicks && !enableProgressByTimeOfDay && currentItem.RunTimeTicks && "Recording" !== currentItem.Type ? endsAtText.innerHTML = "&nbsp;&nbsp;-&nbsp;&nbsp;" + mediaInfo.getEndsAtFromPosition(runtimeTicks, positionTicks, !0) : endsAtText.innerHTML = ""
                }
                nowPlayingPositionSlider && nowPlayingPositionSlider.setBufferedRanges(bufferedRanges, runtimeTicks, positionTicks), updateTimeText(nowPlayingPositionText, positionTicks), updateTimeText(nowPlayingDurationText, runtimeTicks, !0)
            }
        }

        function updatePlayerVolumeState(player, isMuted, volumeLevel) {
            var supportedCommands = currentPlayerSupportedCommands,
                showMuteButton = !0,
                showVolumeSlider = !0; - 1 === supportedCommands.indexOf("Mute") && (showMuteButton = !1), -1 === supportedCommands.indexOf("SetVolume") && (showVolumeSlider = !1), player.isLocalPlayer && appHost.supports("physicalvolumecontrol") && (showMuteButton = !1, showVolumeSlider = !1), isMuted ? (view.querySelector(".buttonMute").setAttribute("title", globalize.translate("Unmute")), view.querySelector(".buttonMute i").innerHTML = "&#xE04F;") : (view.querySelector(".buttonMute").setAttribute("title", globalize.translate("Mute")), view.querySelector(".buttonMute i").innerHTML = "&#xE050;"), showMuteButton ? view.querySelector(".buttonMute").classList.remove("hide") : view.querySelector(".buttonMute").classList.add("hide"), nowPlayingVolumeSlider && (showVolumeSlider ? nowPlayingVolumeSliderContainer.classList.remove("hide") : nowPlayingVolumeSliderContainer.classList.add("hide"), nowPlayingVolumeSlider.dragging || (nowPlayingVolumeSlider.value = volumeLevel || 0))
        }

        function updatePlaylist(player) {
            var btnPreviousTrack = view.querySelector(".btnPreviousTrack"),
                btnNextTrack = view.querySelector(".btnNextTrack");
            btnPreviousTrack.classList.remove("hide"), btnNextTrack.classList.remove("hide"), btnNextTrack.disabled = !1, btnPreviousTrack.disabled = !1
        }

        function updateTimeText(elem, ticks, divider) {
            if (null == ticks) return void(elem.innerHTML = "");
            var html = datetime.getDisplayRunningTime(ticks);
            divider && (html = "&nbsp;/&nbsp;" + html), elem.innerHTML = html
        }

        function onSettingsButtonClick(e) {
            var btn = this;
            require(["playerSettingsMenu"], function(playerSettingsMenu) {
                var player = currentPlayer;
                player && playerSettingsMenu.show({
                    mediaType: "Video",
                    player: player,
                    positionTo: btn,
                    stats: !0,
                    onOption: onSettingsOption
                })
            })
        }

        function onSettingsOption(selectedOption) {
            "stats" === selectedOption && toggleStats()
        }

        function toggleStats() {
            require(["playerStats"], function(PlayerStats) {
                var player = currentPlayer;
                player && (statsOverlay ? statsOverlay.toggle() : statsOverlay = new PlayerStats({
                    player: player
                }))
            })
        }

        function destroyStats() {
            statsOverlay && (statsOverlay.destroy(), statsOverlay = null)
        }

        function showAudioTrackSelection() {
            var player = currentPlayer,
                audioTracks = playbackManager.audioTracks(player),
                currentIndex = playbackManager.getAudioStreamIndex(player),
                menuItems = audioTracks.map(function(stream) {
                    var opt = {
                        name: stream.DisplayTitle,
                        id: stream.Index
                    };
                    return stream.Index === currentIndex && (opt.selected = !0), opt
                }),
                positionTo = this;
            require(["actionsheet"], function(actionsheet) {
                actionsheet.show({
                    items: menuItems,
                    title: globalize.translate("Audio"),
                    positionTo: positionTo
                }).then(function(id) {
                    var index = parseInt(id);
                    index !== currentIndex && playbackManager.setAudioStreamIndex(index, player)
                })
            })
        }

        function showSubtitleTrackSelection() {
            var player = currentPlayer,
                streams = playbackManager.subtitleTracks(player),
                currentIndex = playbackManager.getSubtitleStreamIndex(player);
            null == currentIndex && (currentIndex = -1), streams.unshift({
                Index: -1,
                DisplayTitle: globalize.translate("Off")
            });
            var menuItems = streams.map(function(stream) {
                    var opt = {
                        name: stream.DisplayTitle,
                        id: stream.Index
                    };
                    return stream.Index === currentIndex && (opt.selected = !0), opt
                }),
                positionTo = this;
            require(["actionsheet"], function(actionsheet) {
                actionsheet.show({
                    title: globalize.translate("Subtitles"),
                    items: menuItems,
                    positionTo: positionTo
                }).then(function(id) {
                    var index = parseInt(id);
                    index !== currentIndex && playbackManager.setSubtitleStreamIndex(index, player)
                })
            })
        }

        function onWindowKeyDown(e) {
            if (!currentVisibleMenu && (32 === e.keyCode || 13 === e.keyCode)) return playbackManager.playPause(currentPlayer), void showOsd();
            switch (e.key) {
                case "f":
                    e.ctrlKey || playbackManager.toggleFullscreen(currentPlayer);
                    break;
                case "m":
                    playbackManager.toggleMute(currentPlayer);
                    break;
                case "ArrowLeft":
                case "Left":
                case "NavigationLeft":
                case "GamepadDPadLeft":
                case "GamepadLeftThumbstickLeft":
                    e.shiftKey && playbackManager.rewind(currentPlayer);
                    break;
                case "ArrowRight":
                case "Right":
                case "NavigationRight":
                case "GamepadDPadRight":
                case "GamepadLeftThumbstickRight":
                    e.shiftKey && playbackManager.fastForward(currentPlayer)
            }
        }

        function getImgUrl(item, chapter, index, maxWidth, apiClient) {
            return chapter.ImageTag ? apiClient.getScaledImageUrl(item.Id, {
                maxWidth: maxWidth,
                tag: chapter.ImageTag,
                type: "Chapter",
                index: index
            }) : null
        }

        function getChapterBubbleHtml(apiClient, item, chapters, positionTicks) {
            for (var chapter, index = -1, i = 0, length = chapters.length; i < length; i++) {
                var currentChapter = chapters[i];
                positionTicks >= currentChapter.StartPositionTicks && (chapter = currentChapter, index = i)
            }
            if (!chapter) return null;
            var src = getImgUrl(item, chapter, index, 400, apiClient);
            if (src) {
                var html = '<div class="chapterThumbContainer">';
                return html += '<img class="chapterThumb" src="' + src + '" />', html += '<div class="chapterThumbTextContainer">', html += '<div class="chapterThumbText chapterThumbText-dim">', html += chapter.Name, html += "</div>", html += '<h2 class="chapterThumbText">', html += datetime.getDisplayRunningTime(positionTicks), html += "</h2>", html += "</div>", html += "</div>"
            }
            return null
        }

        function onViewHideStopPlayback() {
            if (playbackManager.isPlayingVideo()) {
                var player = currentPlayer;
                view.removeEventListener("viewbeforehide", onViewHideStopPlayback), releaseCurrentPlayer(), playbackManager.stop(player)
            }
        }

        function enableStopOnBack(enabled) {
            view.removeEventListener("viewbeforehide", onViewHideStopPlayback), enabled && playbackManager.isPlayingVideo(currentPlayer) && view.addEventListener("viewbeforehide", onViewHideStopPlayback)
        }
        var currentPlayer, comingUpNextDisplayed, currentUpNextDialog, isEnabled, currentItem, recordingButtonManager, enableProgressByTimeOfDay, supportsBrightnessChange, currentVisibleMenu, statsOverlay, osdHideTimeout, lastPointerMoveData, self = this,
            currentPlayerSupportedCommands = [],
            currentRuntimeTicks = 0,
            lastUpdateTime = 0,
            programStartDateMs = 0,
            programEndDateMs = 0,
            playbackStartTimeTicks = 0,
            nowPlayingVolumeSlider = view.querySelector(".osdVolumeSlider"),
            nowPlayingVolumeSliderContainer = view.querySelector(".osdVolumeSliderContainer"),
            nowPlayingPositionSlider = view.querySelector(".osdPositionSlider"),
            nowPlayingPositionText = view.querySelector(".osdPositionText"),
            nowPlayingDurationText = view.querySelector(".osdDurationText"),
            startTimeText = view.querySelector(".startTimeText"),
            endTimeText = view.querySelector(".endTimeText"),
            endsAtText = view.querySelector(".endsAtText"),
            btnRewind = view.querySelector(".btnRewind"),
            btnFastForward = view.querySelector(".btnFastForward"),
            transitionEndEventName = dom.whichTransitionEvent(),
            headerElement = document.querySelector(".skinHeader"),
            osdBottomElement = document.querySelector(".videoOsdBottom-maincontrols");
        view.addEventListener("viewbeforeshow", function(e) {
            headerElement.classList.add("osdHeader"), Emby.Page.setTransparency("full")
        }), view.addEventListener("viewshow", function(e) {
            events.on(playbackManager, "playerchange", onPlayerChange), bindToPlayer(playbackManager.getCurrentPlayer()), dom.addEventListener(document, window.PointerEvent ? "pointermove" : "mousemove", onPointerMove, {
                passive: !0
            }), document.body.classList.add("autoScrollY"), showOsd(), inputManager.on(window, onInputCommand), dom.addEventListener(window, "keydown", onWindowKeyDown, {
                passive: !0
            })
        }), view.addEventListener("viewbeforehide", function() {
            statsOverlay && statsOverlay.enabled(!1), dom.removeEventListener(window, "keydown", onWindowKeyDown, {
                passive: !0
            }), stopOsdHideTimer(), headerElement.classList.remove("osdHeader"), headerElement.classList.remove("osdHeader-hidden"), dom.removeEventListener(document, window.PointerEvent ? "pointermove" : "mousemove", onPointerMove, {
                passive: !0
            }), document.body.classList.remove("autoScrollY"), inputManager.off(window, onInputCommand), events.off(playbackManager, "playerchange", onPlayerChange), releaseCurrentPlayer()
        }), view.querySelector(".btnFullscreen").addEventListener("click", function() {
            playbackManager.toggleFullscreen(currentPlayer)
        }), view.querySelector(".btnPip").addEventListener("click", function() {
            playbackManager.togglePictureInPicture(currentPlayer)
        }), view.querySelector(".btnVideoOsdSettings").addEventListener("click", onSettingsButtonClick), view.addEventListener("viewhide", function() {
            headerElement.classList.remove("hide")
        }), view.addEventListener("viewdestroy", function() {
            self.touchHelper && (self.touchHelper.destroy(), self.touchHelper = null), recordingButtonManager && (recordingButtonManager.destroy(), recordingButtonManager = null), destroyStats()
        });
        var lastPointerDown = 0;
        dom.addEventListener(view, window.PointerEvent ? "pointerdown" : "click", function(e) {
            if (dom.parentWithClass(e.target, ["videoOsdBottom", "upNextContainer"])) return void showOsd();
            var pointerType = e.pointerType || (layoutManager.mobile ? "touch" : "mouse"),
                now = (new Date).getTime();
            switch (pointerType) {
                case "touch":
                    now - lastPointerDown > 300 && (lastPointerDown = now, toggleOsd());
                    break;
                case "mouse":
                    e.button || (playbackManager.playPause(currentPlayer), showOsd());
                    break;
                default:
                    playbackManager.playPause(currentPlayer), showOsd()
            }
        }, {
            passive: !0
        }), browser.touch && dom.addEventListener(view, "dblclick", onDoubleClick, {}), view.querySelector(".buttonMute").addEventListener("click", function() {
            playbackManager.toggleMute(currentPlayer)
        }), nowPlayingVolumeSlider.addEventListener("change", function() {
            playbackManager.setVolume(this.value, currentPlayer)
        }), nowPlayingPositionSlider.addEventListener("change", function() {
            var player = currentPlayer;
            if (player) {
                var newPercent = parseFloat(this.value);
                if (enableProgressByTimeOfDay) {
                    var seekAirTimeTicks = newPercent / 100 * (programEndDateMs - programStartDateMs) * 1e4;
                    seekAirTimeTicks += 1e4 * programStartDateMs, seekAirTimeTicks -= playbackStartTimeTicks, playbackManager.seek(seekAirTimeTicks, player)
                } else playbackManager.seekPercent(newPercent, player)
            }
        }), nowPlayingPositionSlider.getBubbleHtml = function(value) {
            if (showOsd(), enableProgressByTimeOfDay) {
                if (programStartDateMs && programEndDateMs) {
                    var ms = programEndDateMs - programStartDateMs;
                    ms /= 100, ms *= value, ms += programStartDateMs;
                    return '<h1 class="sliderBubbleText">' + getDisplayTimeWithoutAmPm(new Date(parseInt(ms)), !0) + "</h1>"
                }
                return "--:--"
            }
            if (!currentRuntimeTicks) return "--:--";
            var ticks = currentRuntimeTicks;
            ticks /= 100, ticks *= value;
            var item = currentItem;
            if (item && item.Chapters && item.Chapters.length && item.Chapters[0].ImageTag) {
                var html = getChapterBubbleHtml(connectionManager.getApiClient(item.ServerId), item, item.Chapters, ticks);
                if (html) return html
            }
            return '<h1 class="sliderBubbleText">' + datetime.getDisplayRunningTime(ticks) + "</h1>"
        }, view.querySelector(".btnPreviousTrack").addEventListener("click", function() {
            playbackManager.previousTrack(currentPlayer)
        }), view.querySelector(".btnPause").addEventListener("click", function() {
            playbackManager.playPause(currentPlayer)
        }), view.querySelector(".btnNextTrack").addEventListener("click", function() {
            playbackManager.nextTrack(currentPlayer)
        }), btnRewind.addEventListener("click", function() {
            playbackManager.rewind(currentPlayer)
        }), btnFastForward.addEventListener("click", function() {
            playbackManager.fastForward(currentPlayer)
        }), view.querySelector(".btnAudio").addEventListener("click", showAudioTrackSelection), view.querySelector(".btnSubtitles").addEventListener("click", showSubtitleTrackSelection), browser.touch && function() {
            require(["touchHelper"], function(TouchHelper) {
                self.touchHelper = new TouchHelper(view, {
                    swipeYThreshold: 30,
                    triggerOnMove: !0,
                    preventDefaultOnMove: !0,
                    ignoreTagNames: ["BUTTON", "INPUT", "TEXTAREA"]
                }), events.on(self.touchHelper, "swipeup", onVerticalSwipe), events.on(self.touchHelper, "swipedown", onVerticalSwipe)
            })
        }()
    }
});