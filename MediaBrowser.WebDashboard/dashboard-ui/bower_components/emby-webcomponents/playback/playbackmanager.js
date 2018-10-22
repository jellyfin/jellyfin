define(["events", "datetime", "appSettings", "itemHelper", "pluginManager", "playQueueManager", "userSettings", "globalize", "connectionManager", "loading", "apphost", "fullscreenManager"], function(events, datetime, appSettings, itemHelper, pluginManager, PlayQueueManager, userSettings, globalize, connectionManager, loading, apphost, fullscreenManager) {
    "use strict";

    function enableLocalPlaylistManagement(player) {
        return !player.getPlaylist && !!player.isLocalPlayer
    }

    function bindToFullscreenChange(player) {
        events.on(fullscreenManager, "fullscreenchange", function() {
            events.trigger(player, "fullscreenchange")
        })
    }

    function triggerPlayerChange(playbackManagerInstance, newPlayer, newTarget, previousPlayer, previousTargetInfo) {
        (newPlayer || previousPlayer) && (newTarget && previousTargetInfo && newTarget.id === previousTargetInfo.id || events.trigger(playbackManagerInstance, "playerchange", [newPlayer, newTarget, previousPlayer]))
    }

    function reportPlayback(playbackManagerInstance, state, player, reportPlaylist, serverId, method, progressEventName) {
        if (serverId) {
            var info = Object.assign({}, state.PlayState);
            info.ItemId = state.NowPlayingItem.Id, progressEventName && (info.EventName = progressEventName), reportPlaylist && addPlaylistToPlaybackReport(playbackManagerInstance, info, player, serverId);
            connectionManager.getApiClient(serverId)[method](info)
        }
    }

    function getPlaylistSync(playbackManagerInstance, player) {
        return player = player || playbackManagerInstance._currentPlayer, player && !enableLocalPlaylistManagement(player) ? player.getPlaylistSync() : playbackManagerInstance._playQueueManager.getPlaylist()
    }

    function addPlaylistToPlaybackReport(playbackManagerInstance, info, player, serverId) {
        info.NowPlayingQueue = getPlaylistSync(playbackManagerInstance, player).map(function(i) {
            var itemInfo = {
                Id: i.Id,
                PlaylistItemId: i.PlaylistItemId
            };
            return i.ServerId !== serverId && (itemInfo.ServerId = i.ServerId), itemInfo
        })
    }

    function normalizeName(t) {
        return t.toLowerCase().replace(" ", "")
    }

    function getItemsForPlayback(serverId, query) {
        var apiClient = connectionManager.getApiClient(serverId);
        if (query.Ids && 1 === query.Ids.split(",").length) {
            var itemId = query.Ids.split(",");
            return apiClient.getItem(apiClient.getCurrentUserId(), itemId).then(function(item) {
                return {
                    Items: [item],
                    TotalRecordCount: 1
                }
            })
        }
        return query.Limit = query.Limit || 300, query.Fields = "Chapters", query.ExcludeLocationTypes = "Virtual", query.EnableTotalRecordCount = !1, query.CollapseBoxSetItems = !1, apiClient.getItems(apiClient.getCurrentUserId(), query)
    }

    function createStreamInfoFromUrlItem(item) {
        return {
            url: item.Url || item.Path,
            playMethod: "DirectPlay",
            item: item,
            textTracks: [],
            mediaType: item.MediaType
        }
    }

    function mergePlaybackQueries(obj1, obj2) {
        var query = Object.assign(obj1, obj2),
            filters = query.Filters ? query.Filters.split(",") : [];
        return -1 === filters.indexOf("IsNotFolder") && filters.push("IsNotFolder"), query.Filters = filters.join(","), query
    }

    function backdropImageUrl(apiClient, item, options) {
        return options = options || {}, options.type = options.type || "Backdrop", options.maxWidth || options.width || options.maxHeight || options.height || (options.quality = 100), item.BackdropImageTags && item.BackdropImageTags.length ? (options.tag = item.BackdropImageTags[0], apiClient.getScaledImageUrl(item.Id, options)) : item.ParentBackdropImageTags && item.ParentBackdropImageTags.length ? (options.tag = item.ParentBackdropImageTags[0], apiClient.getScaledImageUrl(item.ParentBackdropItemId, options)) : null
    }

    function getMimeType(type, container) {
        if (container = (container || "").toLowerCase(), "audio" === type) {
            if ("opus" === container) return "audio/ogg";
            if ("webma" === container) return "audio/webm";
            if ("m4a" === container) return "audio/mp4"
        } else if ("video" === type) {
            if ("mkv" === container) return "video/x-matroska";
            if ("m4v" === container) return "video/mp4";
            if ("mov" === container) return "video/quicktime";
            if ("mpg" === container) return "video/mpeg";
            if ("flv" === container) return "video/x-flv"
        }
        return type + "/" + container
    }

    function getParam(name, url) {
        name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
        var regexS = "[\\?&]" + name + "=([^&#]*)",
            regex = new RegExp(regexS, "i"),
            results = regex.exec(url);
        return null == results ? "" : decodeURIComponent(results[1].replace(/\+/g, " "))
    }

    function isAutomaticPlayer(player) {
        return !!player.isLocalPlayer
    }

    function getAutomaticPlayers(instance, forceLocalPlayer) {
        if (!forceLocalPlayer) {
            var player = instance._currentPlayer;
            if (player && !isAutomaticPlayer(player)) return [player]
        }
        return instance.getPlayers().filter(isAutomaticPlayer)
    }

    function isServerItem(item) {
        return !!item.Id
    }

    function enableIntros(item) {
        return "Video" === item.MediaType && ("TvChannel" !== item.Type && ("InProgress" !== item.Status && isServerItem(item)))
    }

    function getIntros(firstItem, apiClient, options) {
        return options.startPositionTicks || options.startIndex || !1 === options.fullscreen || !enableIntros(firstItem) || !userSettings.enableCinemaMode() ? Promise.resolve({
            Items: []
        }) : apiClient.getIntros(firstItem.Id).then(function(result) {
            return result
        }, function(err) {
            return Promise.resolve({
                Items: []
            })
        })
    }

    function getAudioMaxValues(deviceProfile) {
        var maxAudioSampleRate = null,
            maxAudioBitDepth = null,
            maxAudioBitrate = null;
        return deviceProfile.CodecProfiles.map(function(codecProfile) {
            "Audio" === codecProfile.Type && (codecProfile.Conditions || []).map(function(condition) {
                "LessThanEqual" === condition.Condition && "AudioBitDepth" === condition.Property && (maxAudioBitDepth = condition.Value), "LessThanEqual" === condition.Condition && "AudioSampleRate" === condition.Property && (maxAudioSampleRate = condition.Value), "LessThanEqual" === condition.Condition && "AudioBitrate" === condition.Property && (maxAudioBitrate = condition.Value)
            })
        }), {
            maxAudioSampleRate: maxAudioSampleRate,
            maxAudioBitDepth: maxAudioBitDepth,
            maxAudioBitrate: maxAudioBitrate
        }
    }

    function getAudioStreamUrl(item, transcodingProfile, directPlayContainers, maxBitrate, apiClient, maxAudioSampleRate, maxAudioBitDepth, maxAudioBitrate, startPosition) {
        var url = "Audio/" + item.Id + "/universal";
        return startingPlaySession++, apiClient.getUrl(url, {
            UserId: apiClient.getCurrentUserId(),
            DeviceId: apiClient.deviceId(),
            MaxStreamingBitrate: maxAudioBitrate || maxBitrate,
            Container: directPlayContainers,
            TranscodingContainer: transcodingProfile.Container || null,
            TranscodingProtocol: transcodingProfile.Protocol || null,
            AudioCodec: transcodingProfile.AudioCodec,
            MaxAudioSampleRate: maxAudioSampleRate,
            MaxAudioBitDepth: maxAudioBitDepth,
            api_key: apiClient.accessToken(),
            PlaySessionId: startingPlaySession,
            StartTimeTicks: startPosition || 0,
            EnableRedirection: !0,
            EnableRemoteMedia: apphost.supports("remoteaudio")
        })
    }

    function getAudioStreamUrlFromDeviceProfile(item, deviceProfile, maxBitrate, apiClient, startPosition) {
        var transcodingProfile = deviceProfile.TranscodingProfiles.filter(function(p) {
                return "Audio" === p.Type && "Streaming" === p.Context
            })[0],
            directPlayContainers = "";
        deviceProfile.DirectPlayProfiles.map(function(p) {
            "Audio" === p.Type && (directPlayContainers ? directPlayContainers += "," + p.Container : directPlayContainers = p.Container, p.AudioCodec && (directPlayContainers += "|" + p.AudioCodec))
        });
        var maxValues = getAudioMaxValues(deviceProfile);
        return getAudioStreamUrl(item, transcodingProfile, directPlayContainers, maxBitrate, apiClient, maxValues.maxAudioSampleRate, maxValues.maxAudioBitDepth, maxValues.maxAudioBitrate, startPosition)
    }

    function getStreamUrls(items, deviceProfile, maxBitrate, apiClient, startPosition) {
        var audioTranscodingProfile = deviceProfile.TranscodingProfiles.filter(function(p) {
                return "Audio" === p.Type && "Streaming" === p.Context
            })[0],
            audioDirectPlayContainers = "";
        deviceProfile.DirectPlayProfiles.map(function(p) {
            "Audio" === p.Type && (audioDirectPlayContainers ? audioDirectPlayContainers += "," + p.Container : audioDirectPlayContainers = p.Container, p.AudioCodec && (audioDirectPlayContainers += "|" + p.AudioCodec))
        });
        for (var maxValues = getAudioMaxValues(deviceProfile), streamUrls = [], i = 0, length = items.length; i < length; i++) {
            var streamUrl, item = items[i];
            "Audio" !== item.MediaType || itemHelper.isLocalItem(item) || (streamUrl = getAudioStreamUrl(item, audioTranscodingProfile, audioDirectPlayContainers, maxBitrate, apiClient, maxValues.maxAudioSampleRate, maxValues.maxAudioBitDepth, maxValues.maxAudioBitrate, startPosition)), streamUrls.push(streamUrl || ""), 0 === i && (startPosition = 0)
        }
        return Promise.resolve(streamUrls)
    }

    function setStreamUrls(items, deviceProfile, maxBitrate, apiClient, startPosition) {
        return getStreamUrls(items, deviceProfile, maxBitrate, apiClient, startPosition).then(function(streamUrls) {
            for (var i = 0, length = items.length; i < length; i++) {
                var item = items[i],
                    streamUrl = streamUrls[i];
                streamUrl && (item.PresetMediaSource = {
                    StreamUrl: streamUrl,
                    Id: item.Id,
                    MediaStreams: [],
                    RunTimeTicks: item.RunTimeTicks
                })
            }
        })
    }

    function getPlaybackInfo(player, apiClient, item, deviceProfile, maxBitrate, startPosition, isPlayback, mediaSourceId, audioStreamIndex, subtitleStreamIndex, liveStreamId, enableDirectPlay, enableDirectStream, allowVideoStreamCopy, allowAudioStreamCopy) {
        if (!itemHelper.isLocalItem(item) && "Audio" === item.MediaType) return Promise.resolve({
            MediaSources: [{
                StreamUrl: getAudioStreamUrlFromDeviceProfile(item, deviceProfile, maxBitrate, apiClient, startPosition),
                Id: item.Id,
                MediaStreams: [],
                RunTimeTicks: item.RunTimeTicks
            }]
        });
        if (item.PresetMediaSource) return Promise.resolve({
            MediaSources: [item.PresetMediaSource]
        });
        var itemId = item.Id,
            query = {
                UserId: apiClient.getCurrentUserId(),
                StartTimeTicks: startPosition || 0
            };
        return isPlayback ? (query.IsPlayback = !0, query.AutoOpenLiveStream = !0) : (query.IsPlayback = !1, query.AutoOpenLiveStream = !1), null != audioStreamIndex && (query.AudioStreamIndex = audioStreamIndex), null != subtitleStreamIndex && (query.SubtitleStreamIndex = subtitleStreamIndex), null != enableDirectPlay && (query.EnableDirectPlay = enableDirectPlay), null != enableDirectStream && (query.EnableDirectStream = enableDirectStream), null != allowVideoStreamCopy && (query.AllowVideoStreamCopy = allowVideoStreamCopy), null != allowAudioStreamCopy && (query.AllowAudioStreamCopy = allowAudioStreamCopy), mediaSourceId && (query.MediaSourceId = mediaSourceId), liveStreamId && (query.LiveStreamId = liveStreamId), maxBitrate && (query.MaxStreamingBitrate = maxBitrate), player.enableMediaProbe && !player.enableMediaProbe(item) && (query.EnableMediaProbe = !1), !1 !== query.EnableDirectStream && player.supportsPlayMethod && !player.supportsPlayMethod("DirectStream", item) && (query.EnableDirectStream = !1), player.getDirectPlayProtocols && (query.DirectPlayProtocols = player.getDirectPlayProtocols()), apiClient.getPlaybackInfo(itemId, query, deviceProfile)
    }

    function getOptimalMediaSource(apiClient, item, versions) {
        var promises = versions.map(function(v) {
            return supportsDirectPlay(apiClient, item, v)
        });
        return promises.length ? Promise.all(promises).then(function(results) {
            for (var i = 0, length = versions.length; i < length; i++) versions[i].enableDirectPlay = results[i] || !1;
            var optimalVersion = versions.filter(function(v) {
                return v.enableDirectPlay
            })[0];
            return optimalVersion || (optimalVersion = versions.filter(function(v) {
                return v.SupportsDirectStream
            })[0]), (optimalVersion = optimalVersion || versions.filter(function(s) {
                return s.SupportsTranscoding
            })[0]) || versions[0]
        }) : Promise.reject()
    }

    function getLiveStream(player, apiClient, item, playSessionId, deviceProfile, maxBitrate, startPosition, mediaSource, audioStreamIndex, subtitleStreamIndex) {
        var postData = {
                DeviceProfile: deviceProfile,
                OpenToken: mediaSource.OpenToken
            },
            query = {
                UserId: apiClient.getCurrentUserId(),
                StartTimeTicks: startPosition || 0,
                ItemId: item.Id,
                PlaySessionId: playSessionId
            };
        return maxBitrate && (query.MaxStreamingBitrate = maxBitrate), null != audioStreamIndex && (query.AudioStreamIndex = audioStreamIndex), null != subtitleStreamIndex && (query.SubtitleStreamIndex = subtitleStreamIndex), !1 !== query.EnableDirectStream && player.supportsPlayMethod && !player.supportsPlayMethod("DirectStream", item) && (query.EnableDirectStream = !1), apiClient.ajax({
            url: apiClient.getUrl("LiveStreams/Open", query),
            type: "POST",
            data: JSON.stringify(postData),
            contentType: "application/json",
            dataType: "json"
        })
    }

    function isHostReachable(mediaSource, apiClient) {
        return mediaSource.IsRemote ? Promise.resolve(!0) : apiClient.getEndpointInfo().then(function(endpointInfo) {
            if (endpointInfo.IsInNetwork) {
                if (!endpointInfo.IsLocal) {
                    var path = (mediaSource.Path || "").toLowerCase();
                    if (-1 !== path.indexOf("localhost") || -1 !== path.indexOf("127.0.0.1")) return Promise.resolve(!1)
                }
                return Promise.resolve(!0)
            }
            return Promise.resolve(!1)
        })
    }

    function supportsDirectPlay(apiClient, item, mediaSource) {
        var isFolderRip = "BluRay" === mediaSource.VideoType || "Dvd" === mediaSource.VideoType || "HdDvd" === mediaSource.VideoType;
        if (mediaSource.SupportsDirectPlay || isFolderRip) {
            if (mediaSource.IsRemote && !apphost.supports("remotevideo")) return Promise.resolve(!1);
            if ("Http" === mediaSource.Protocol && !mediaSource.RequiredHttpHeaders.length) return mediaSource.SupportsDirectStream || mediaSource.SupportsTranscoding ? isHostReachable(mediaSource, apiClient) : Promise.resolve(!0);
            if ("File" === mediaSource.Protocol) return new Promise(function(resolve, reject) {
                require(["filesystem"], function(filesystem) {
                    filesystem[isFolderRip ? "directoryExists" : "fileExists"](mediaSource.Path).then(function() {
                        resolve(!0)
                    }, function() {
                        resolve(!1)
                    })
                })
            })
        }
        return Promise.resolve(!1)
    }

    function validatePlaybackInfoResult(instance, result) {
        return !result.ErrorCode || (showPlaybackInfoErrorMessage(instance, result.ErrorCode), !1)
    }

    function showPlaybackInfoErrorMessage(instance, errorCode, playNextTrack) {
        require(["alert"], function(alert) {
            alert({
                text: globalize.translate("sharedcomponents#PlaybackError" + errorCode),
                title: globalize.translate("sharedcomponents#HeaderPlaybackError")
            }).then(function() {
                playNextTrack && instance.nextTrack()
            })
        })
    }

    function normalizePlayOptions(playOptions) {
        playOptions.fullscreen = !1 !== playOptions.fullscreen
    }

    function truncatePlayOptions(playOptions) {
        return {
            fullscreen: playOptions.fullscreen,
            mediaSourceId: playOptions.mediaSourceId,
            audioStreamIndex: playOptions.audioStreamIndex,
            subtitleStreamIndex: playOptions.subtitleStreamIndex,
            startPositionTicks: playOptions.startPositionTicks
        }
    }

    function getNowPlayingItemForReporting(player, item, mediaSource) {
        var nowPlayingItem = Object.assign({}, item);
        return mediaSource && (nowPlayingItem.RunTimeTicks = mediaSource.RunTimeTicks, nowPlayingItem.MediaStreams = mediaSource.MediaStreams, nowPlayingItem.MediaSources = null), nowPlayingItem.RunTimeTicks = nowPlayingItem.RunTimeTicks || 1e4 * player.duration(), nowPlayingItem
    }

    function displayPlayerIndividually(player) {
        return !player.isLocalPlayer
    }

    function createTarget(instance, player) {
        return {
            name: player.name,
            id: player.id,
            playerName: player.name,
            playableMediaTypes: ["Audio", "Video", "Game", "Photo", "Book"].map(player.canPlayMediaType),
            isLocalPlayer: player.isLocalPlayer,
            supportedCommands: instance.getSupportedCommands(player)
        }
    }

    function getPlayerTargets(player) {
        return player.getTargets ? player.getTargets() : Promise.resolve([createTarget(player)])
    }

    function sortPlayerTargets(a, b) {
        var aVal = a.isLocalPlayer ? 0 : 1,
            bVal = b.isLocalPlayer ? 0 : 1;
        return aVal = aVal.toString() + a.name, bVal = bVal.toString() + b.name, aVal.localeCompare(bVal)
    }

    function PlaybackManager() {
        function getCurrentSubtitleStream(player) {
            if (!player) throw new Error("player cannot be null");
            var index = getPlayerData(player).subtitleStreamIndex;
            return null == index || -1 === index ? null : getSubtitleStream(player, index)
        }

        function getSubtitleStream(player, index) {
            return self.subtitleTracks(player).filter(function(s) {
                return "Subtitle" === s.Type && s.Index === index
            })[0]
        }

        function removeCurrentPlayer(player) {
            var previousPlayer = self._currentPlayer;
            previousPlayer && player.id !== previousPlayer.id || setCurrentPlayerInternal(null)
        }

        function setCurrentPlayerInternal(player, targetInfo) {
            var previousPlayer = self._currentPlayer,
                previousTargetInfo = currentTargetInfo;
            if (player && !targetInfo && player.isLocalPlayer && (targetInfo = createTarget(self, player)), player && !targetInfo) throw new Error("targetInfo cannot be null");
            currentPairingId = null, self._currentPlayer = player, currentTargetInfo = targetInfo, targetInfo && console.log("Active player: " + JSON.stringify(targetInfo)), player && player.isLocalPlayer && (lastLocalPlayer = player), previousPlayer && self.endPlayerUpdates(previousPlayer), player && self.beginPlayerUpdates(player), triggerPlayerChange(self, player, targetInfo, previousPlayer, previousTargetInfo)
        }

        function getDefaultPlayOptions() {
            return {
                fullscreen: !0
            }
        }

        function isAudioStreamSupported(mediaSource, index, deviceProfile) {
            var mediaStream, i, length, mediaStreams = mediaSource.MediaStreams;
            for (i = 0, length = mediaStreams.length; i < length; i++)
                if ("Audio" === mediaStreams[i].Type && mediaStreams[i].Index === index) {
                    mediaStream = mediaStreams[i];
                    break
                } if (!mediaStream) return !1;
            var codec = (mediaStream.Codec || "").toLowerCase();
            return !!codec && (deviceProfile.DirectPlayProfiles || []).filter(function(p) {
                return "Video" === p.Type && (!p.AudioCodec || (0 === p.AudioCodec.indexOf("-") ? -1 === p.AudioCodec.toLowerCase().indexOf(codec) : -1 !== p.AudioCodec.toLowerCase().indexOf(codec)))
            }).length > 0
        }

        function getSavedMaxStreamingBitrate(apiClient, mediaType) {
            apiClient || (apiClient = connectionManager.currentApiClient());
            var endpointInfo = apiClient.getSavedEndpointInfo() || {};
            return appSettings.maxStreamingBitrate(endpointInfo.IsInNetwork, mediaType)
        }

        function getDeliveryMethod(subtitleStream) {
            return subtitleStream.DeliveryMethod ? subtitleStream.DeliveryMethod : subtitleStream.IsExternal ? "External" : "Embed"
        }

        function canPlayerSeek(player) {
            if (!player) throw new Error("player cannot be null");
            return -1 !== (getPlayerData(player).streamInfo.url || "").toLowerCase().indexOf(".m3u8") || (player.seekable ? player.seekable() : !("Transcode" === self.playMethod(player)) && player.duration())
        }

        function changeStream(player, ticks, params) {
            if (canPlayerSeek(player) && null == params) return void player.currentTime(parseInt(ticks / 1e4));
            params = params || {};
            var liveStreamId = getPlayerData(player).streamInfo.liveStreamId,
                lastMediaInfoQuery = getPlayerData(player).streamInfo.lastMediaInfoQuery,
                playSessionId = self.playSessionId(player),
                currentItem = self.currentItem(player);
            player.getDeviceProfile(currentItem, {
                isRetry: !1 === params.EnableDirectPlay
            }).then(function(deviceProfile) {
                var audioStreamIndex = null == params.AudioStreamIndex ? getPlayerData(player).audioStreamIndex : params.AudioStreamIndex,
                    subtitleStreamIndex = null == params.SubtitleStreamIndex ? getPlayerData(player).subtitleStreamIndex : params.SubtitleStreamIndex,
                    currentMediaSource = self.currentMediaSource(player),
                    apiClient = connectionManager.getApiClient(currentItem.ServerId);
                ticks && (ticks = parseInt(ticks));
                var maxBitrate = params.MaxStreamingBitrate || self.getMaxStreamingBitrate(player),
                    currentPlayOptions = currentItem.playOptions || {};
                getPlaybackInfo(player, apiClient, currentItem, deviceProfile, maxBitrate, ticks, !0, currentMediaSource.Id, audioStreamIndex, subtitleStreamIndex, liveStreamId, params.EnableDirectPlay, params.EnableDirectStream, params.AllowVideoStreamCopy, params.AllowAudioStreamCopy).then(function(result) {
                    if (validatePlaybackInfoResult(self, result)) {
                        currentMediaSource = result.MediaSources[0];
                        var streamInfo = createStreamInfo(apiClient, currentItem.MediaType, currentItem, currentMediaSource, ticks);
                        if (streamInfo.fullscreen = currentPlayOptions.fullscreen, streamInfo.lastMediaInfoQuery = lastMediaInfoQuery, !streamInfo.url) return void showPlaybackInfoErrorMessage(self, "NoCompatibleStream", !0);
                        getPlayerData(player).subtitleStreamIndex = subtitleStreamIndex, getPlayerData(player).audioStreamIndex = audioStreamIndex, getPlayerData(player).maxStreamingBitrate = maxBitrate, changeStreamToUrl(apiClient, player, playSessionId, streamInfo)
                    }
                })
            })
        }

        function changeStreamToUrl(apiClient, player, playSessionId, streamInfo, newPositionTicks) {
            var playerData = getPlayerData(player);
            playerData.isChangingStream = !0, playerData.streamInfo && playSessionId ? apiClient.stopActiveEncodings(playSessionId).then(function() {
                var afterSetSrc = function() {
                    apiClient.stopActiveEncodings(playSessionId)
                };
                setSrcIntoPlayer(apiClient, player, streamInfo).then(afterSetSrc, afterSetSrc)
            }) : setSrcIntoPlayer(apiClient, player, streamInfo)
        }

        function setSrcIntoPlayer(apiClient, player, streamInfo) {
            return player.play(streamInfo).then(function() {
                var playerData = getPlayerData(player);
                playerData.isChangingStream = !1, playerData.streamInfo = streamInfo, streamInfo.started = !0, streamInfo.ended = !1, sendProgressUpdate(player, "timeupdate")
            }, function(e) {
                getPlayerData(player).isChangingStream = !1, onPlaybackError.call(player, e, {
                    type: "mediadecodeerror",
                    streamInfo: streamInfo
                })
            })
        }

        function translateItemsForPlayback(items, options) {
            var promise, firstItem = items[0],
                serverId = firstItem.ServerId,
                queryOptions = options.queryOptions || {};
            return "Program" === firstItem.Type ? promise = getItemsForPlayback(serverId, {
                Ids: firstItem.ChannelId
            }) : "Playlist" === firstItem.Type ? promise = getItemsForPlayback(serverId, {
                ParentId: firstItem.Id,
                SortBy: options.shuffle ? "Random" : null
            }) : "MusicArtist" === firstItem.Type ? promise = getItemsForPlayback(serverId, {
                ArtistIds: firstItem.Id,
                Filters: "IsNotFolder",
                Recursive: !0,
                SortBy: options.shuffle ? "Random" : "SortName",
                MediaTypes: "Audio"
            }) : "Photo" === firstItem.MediaType ? promise = getItemsForPlayback(serverId, {
                ParentId: firstItem.ParentId,
                Filters: "IsNotFolder",
                Recursive: !1,
                SortBy: options.shuffle ? "Random" : "SortName",
                MediaTypes: "Photo,Video",
                Limit: 5e3
            }).then(function(result) {
                var items = result.Items,
                    index = items.map(function(i) {
                        return i.Id
                    }).indexOf(firstItem.Id);
                return -1 === index && (index = 0), options.startIndex = index, Promise.resolve(result)
            }) : "PhotoAlbum" === firstItem.Type ? promise = getItemsForPlayback(serverId, {
                ParentId: firstItem.Id,
                Filters: "IsNotFolder",
                Recursive: !1,
                SortBy: options.shuffle ? "Random" : "SortName",
                MediaTypes: "Photo,Video",
                Limit: 1e3
            }) : "MusicGenre" === firstItem.Type ? promise = getItemsForPlayback(serverId, {
                GenreIds: firstItem.Id,
                Filters: "IsNotFolder",
                Recursive: !0,
                SortBy: options.shuffle ? "Random" : "SortName",
                MediaTypes: "Audio"
            }) : firstItem.IsFolder ? promise = getItemsForPlayback(serverId, mergePlaybackQueries({
                ParentId: firstItem.Id,
                Filters: "IsNotFolder",
                Recursive: !0,
                SortBy: options.shuffle ? "Random" : -1 === ["BoxSet"].indexOf(firstItem.Type) ? "SortName" : null,
                MediaTypes: "Audio,Video"
            }, queryOptions)) : "Episode" === firstItem.Type && 1 === items.length && !1 !== getPlayer(firstItem, options).supportsProgress && (promise = new Promise(function(resolve, reject) {
                var apiClient = connectionManager.getApiClient(firstItem.ServerId);
                apiClient.getCurrentUser().then(function(user) {
                    if (!user.Configuration.EnableNextEpisodeAutoPlay || !firstItem.SeriesId) return void resolve(null);
                    apiClient.getEpisodes(firstItem.SeriesId, {
                        IsVirtualUnaired: !1,
                        IsMissing: !1,
                        UserId: apiClient.getCurrentUserId(),
                        Fields: "Chapters"
                    }).then(function(episodesResult) {
                        var foundItem = !1;
                        episodesResult.Items = episodesResult.Items.filter(function(e) {
                            return !!foundItem || e.Id === firstItem.Id && (foundItem = !0, !0)
                        }), episodesResult.TotalRecordCount = episodesResult.Items.length, resolve(episodesResult)
                    }, reject)
                })
            })), promise ? promise.then(function(result) {
                return result ? result.Items : items
            }) : Promise.resolve(items)
        }

        function getPlayerData(player) {
            if (!player) throw new Error("player cannot be null");
            if (!player.name) throw new Error("player name cannot be null");
            var state = playerStates[player.name];
            return state || (playerStates[player.name] = {}, state = playerStates[player.name]), player
        }

        function getCurrentTicks(player) {
            if (!player) throw new Error("player cannot be null");
            var playerTime = Math.floor(1e4 * (player || self._currentPlayer).currentTime());
            return getPlayerData(player).streamInfo && (playerTime += getPlayerData(player).streamInfo.transcodingOffsetTicks || 0), playerTime
        }

        function playPhotos(items, options, user) {
            var playStartIndex = options.startIndex || 0,
                player = getPlayer(items[playStartIndex], options);
            return loading.hide(), options.items = items, player.play(options)
        }

        function playWithIntros(items, options, user) {
            var playStartIndex = options.startIndex || 0,
                firstItem = items[playStartIndex];
            if (firstItem || (playStartIndex = 0, firstItem = items[playStartIndex]), !firstItem) return showPlaybackInfoErrorMessage(self, "NoCompatibleStream", !1), Promise.reject();
            if ("Photo" === firstItem.MediaType) return playPhotos(items, options, user);
            var apiClient = connectionManager.getApiClient(firstItem.ServerId);
            return getIntros(firstItem, apiClient, options).then(function(introsResult) {
                var introPlayOptions, introItems = introsResult.Items;
                return firstItem.playOptions = truncatePlayOptions(options), introPlayOptions = introItems.length ? {
                    fullscreen: firstItem.playOptions.fullscreen
                } : firstItem.playOptions, items = introItems.concat(items), introPlayOptions.items = items, introPlayOptions.startIndex = playStartIndex, playInternal(items[playStartIndex], introPlayOptions, function() {
                    self._playQueueManager.setPlaylist(items), setPlaylistState(items[playStartIndex].PlaylistItemId, playStartIndex), loading.hide()
                })
            })
        }

        function setPlaylistState(playlistItemId, index) {
            isNaN(index) || self._playQueueManager.setPlaylistState(playlistItemId, index)
        }

        function playInternal(item, playOptions, onPlaybackStartedFn) {
            return item.IsPlaceHolder ? (loading.hide(), showPlaybackInfoErrorMessage(self, "PlaceHolder", !0), Promise.reject()) : (normalizePlayOptions(playOptions), playOptions.isFirstItem ? playOptions.isFirstItem = !1 : playOptions.isFirstItem = !0, runInterceptors(item, playOptions).then(function() {
                playOptions.fullscreen && loading.show();
                var mediaType = item.MediaType,
                    onBitrateDetectionFailure = function() {
                        return playAfterBitrateDetect(getSavedMaxStreamingBitrate(connectionManager.getApiClient(item.ServerId), mediaType), item, playOptions, onPlaybackStartedFn)
                    };
                if (!isServerItem(item) || itemHelper.isLocalItem(item)) return onBitrateDetectionFailure();
                var apiClient = connectionManager.getApiClient(item.ServerId);
                apiClient.getEndpointInfo().then(function(endpointInfo) {
                    if (("Video" === mediaType || "Audio" === mediaType) && appSettings.enableAutomaticBitrateDetection(endpointInfo.IsInNetwork, mediaType)) return apiClient.detectBitrate().then(function(bitrate) {
                        return appSettings.maxStreamingBitrate(endpointInfo.IsInNetwork, mediaType, bitrate), playAfterBitrateDetect(bitrate, item, playOptions, onPlaybackStartedFn)
                    }, onBitrateDetectionFailure);
                    onBitrateDetectionFailure()
                }, onBitrateDetectionFailure)
            }, onInterceptorRejection))
        }

        function onInterceptorRejection() {
            var player = self._currentPlayer;
            return player && (destroyPlayer(player), removeCurrentPlayer(player)), events.trigger(self, "playbackcancelled"), Promise.reject()
        }

        function destroyPlayer(player) {
            player.destroy()
        }

        function runInterceptors(item, playOptions) {
            return new Promise(function(resolve, reject) {
                var interceptors = pluginManager.ofType("preplayintercept");
                if (interceptors.sort(function(a, b) {
                        return (a.order || 0) - (b.order || 0)
                    }), !interceptors.length) return void resolve();
                loading.hide();
                var options = Object.assign({}, playOptions);
                options.mediaType = item.MediaType, options.item = item, runNextPrePlay(interceptors, 0, options, resolve, reject)
            })
        }

        function runNextPrePlay(interceptors, index, options, resolve, reject) {
            if (index >= interceptors.length) return void resolve();
            interceptors[index].intercept(options).then(function() {
                runNextPrePlay(interceptors, index + 1, options, resolve, reject)
            }, reject)
        }

        function sendPlaybackListToPlayer(player, items, deviceProfile, maxBitrate, apiClient, startPositionTicks, mediaSourceId, audioStreamIndex, subtitleStreamIndex, startIndex) {
            return setStreamUrls(items, deviceProfile, maxBitrate, apiClient, startPositionTicks).then(function() {
                return loading.hide(), player.play({
                    items: items,
                    startPositionTicks: startPositionTicks || 0,
                    mediaSourceId: mediaSourceId,
                    audioStreamIndex: audioStreamIndex,
                    subtitleStreamIndex: subtitleStreamIndex,
                    startIndex: startIndex
                })
            })
        }

        function playAfterBitrateDetect(maxBitrate, item, playOptions, onPlaybackStartedFn) {
            var promise, startPosition = playOptions.startPositionTicks,
                player = getPlayer(item, playOptions),
                activePlayer = self._currentPlayer;
            return activePlayer ? (self._playNextAfterEnded = !1, promise = onPlaybackChanging(activePlayer, player, item)) : promise = Promise.resolve(), isServerItem(item) && "Game" !== item.MediaType && "Book" !== item.MediaType ? Promise.all([promise, player.getDeviceProfile(item)]).then(function(responses) {
                var deviceProfile = responses[1],
                    apiClient = connectionManager.getApiClient(item.ServerId),
                    mediaSourceId = playOptions.mediaSourceId,
                    audioStreamIndex = playOptions.audioStreamIndex,
                    subtitleStreamIndex = playOptions.subtitleStreamIndex;
                return player && !enableLocalPlaylistManagement(player) ? sendPlaybackListToPlayer(player, playOptions.items, deviceProfile, maxBitrate, apiClient, startPosition, mediaSourceId, audioStreamIndex, subtitleStreamIndex, playOptions.startIndex) : (playOptions.items = null, getPlaybackMediaSource(player, apiClient, deviceProfile, maxBitrate, item, startPosition, mediaSourceId, audioStreamIndex, subtitleStreamIndex).then(function(mediaSource) {
                    var streamInfo = createStreamInfo(apiClient, item.MediaType, item, mediaSource, startPosition);
                    return streamInfo.fullscreen = playOptions.fullscreen, getPlayerData(player).isChangingStream = !1, getPlayerData(player).maxStreamingBitrate = maxBitrate, player.play(streamInfo).then(function() {
                        loading.hide(), onPlaybackStartedFn(), onPlaybackStarted(player, playOptions, streamInfo, mediaSource)
                    }, function(err) {
                        onPlaybackStartedFn(), onPlaybackStarted(player, playOptions, streamInfo, mediaSource), setTimeout(function() {
                            onPlaybackError.call(player, err, {
                                type: "mediadecodeerror",
                                streamInfo: streamInfo
                            })
                        }, 100)
                    })
                }))
            }) : promise.then(function() {
                var streamInfo = createStreamInfoFromUrlItem(item);
                return streamInfo.fullscreen = playOptions.fullscreen, getPlayerData(player).isChangingStream = !1, player.play(streamInfo).then(function() {
                    loading.hide(), onPlaybackStartedFn(), onPlaybackStarted(player, playOptions, streamInfo)
                }, function() {
                    self.stop(player)
                })
            })
        }

        function createStreamInfo(apiClient, type, item, mediaSource, startPosition) {
            var mediaUrl, contentType, directOptions, transcodingOffsetTicks = 0,
                playerStartPositionTicks = startPosition,
                liveStreamId = mediaSource.LiveStreamId,
                playMethod = "Transcode",
                mediaSourceContainer = (mediaSource.Container || "").toLowerCase();
            if ("Video" === type || "Audio" === type)
                if (contentType = getMimeType(type.toLowerCase(), mediaSourceContainer), mediaSource.enableDirectPlay) mediaUrl = mediaSource.Path, playMethod = "DirectPlay";
                else if (mediaSource.StreamUrl) playMethod = "Transcode", mediaUrl = mediaSource.StreamUrl;
            else if (mediaSource.SupportsDirectStream) {
                directOptions = {
                    Static: !0,
                    mediaSourceId: mediaSource.Id,
                    deviceId: apiClient.deviceId(),
                    api_key: apiClient.accessToken()
                }, mediaSource.ETag && (directOptions.Tag = mediaSource.ETag), mediaSource.LiveStreamId && (directOptions.LiveStreamId = mediaSource.LiveStreamId);
                var prefix = "Video" === type ? "Videos" : "Audio";
                mediaUrl = apiClient.getUrl(prefix + "/" + item.Id + "/stream." + mediaSourceContainer, directOptions), playMethod = "DirectStream"
            } else mediaSource.SupportsTranscoding && (mediaUrl = apiClient.getUrl(mediaSource.TranscodingUrl), "hls" === mediaSource.TranscodingSubProtocol ? contentType = "application/x-mpegURL" : (playerStartPositionTicks = null, contentType = getMimeType(type.toLowerCase(), mediaSource.TranscodingContainer), -1 === mediaUrl.toLowerCase().indexOf("copytimestamps=true") && (transcodingOffsetTicks = startPosition || 0)));
            else mediaUrl = mediaSource.Path, playMethod = "DirectPlay";
            !mediaUrl && mediaSource.SupportsDirectPlay && (mediaUrl = mediaSource.Path, playMethod = "DirectPlay");
            var resultInfo = {
                    url: mediaUrl,
                    mimeType: contentType,
                    transcodingOffsetTicks: transcodingOffsetTicks,
                    playMethod: playMethod,
                    playerStartPositionTicks: playerStartPositionTicks,
                    item: item,
                    mediaSource: mediaSource,
                    textTracks: getTextTracks(apiClient, item, mediaSource),
                    tracks: getTextTracks(apiClient, item, mediaSource),
                    mediaType: type,
                    liveStreamId: liveStreamId,
                    playSessionId: getParam("playSessionId", mediaUrl),
                    title: item.Name
                },
                backdropUrl = backdropImageUrl(apiClient, item, {});
            return backdropUrl && (resultInfo.backdropUrl = backdropUrl), resultInfo
        }

        function getTextTracks(apiClient, item, mediaSource) {
            for (var subtitleStreams = mediaSource.MediaStreams.filter(function(s) {
                    return "Subtitle" === s.Type
                }), textStreams = subtitleStreams.filter(function(s) {
                    return "External" === s.DeliveryMethod
                }), tracks = [], i = 0, length = textStreams.length; i < length; i++) {
                var textStreamUrl, textStream = textStreams[i];
                textStreamUrl = itemHelper.isLocalItem(item) ? textStream.Path : textStream.IsExternalUrl ? textStream.DeliveryUrl : apiClient.getUrl(textStream.DeliveryUrl), tracks.push({
                    url: textStreamUrl,
                    language: textStream.Language || "und",
                    isDefault: textStream.Index === mediaSource.DefaultSubtitleStreamIndex,
                    index: textStream.Index,
                    format: textStream.Codec
                })
            }
            return tracks
        }

        function getPlaybackMediaSource(player, apiClient, deviceProfile, maxBitrate, item, startPosition, mediaSourceId, audioStreamIndex, subtitleStreamIndex) {
            return getPlaybackInfo(player, apiClient, item, deviceProfile, maxBitrate, startPosition, !0, mediaSourceId, audioStreamIndex, subtitleStreamIndex, null).then(function(playbackInfoResult) {
                return validatePlaybackInfoResult(self, playbackInfoResult) ? getOptimalMediaSource(apiClient, item, playbackInfoResult.MediaSources).then(function(mediaSource) {
                    return mediaSource ? mediaSource.RequiresOpening && !mediaSource.LiveStreamId ? getLiveStream(player, apiClient, item, playbackInfoResult.PlaySessionId, deviceProfile, maxBitrate, startPosition, mediaSource, null, null).then(function(openLiveStreamResult) {
                        return supportsDirectPlay(apiClient, item, openLiveStreamResult.MediaSource).then(function(result) {
                            return openLiveStreamResult.MediaSource.enableDirectPlay = result, openLiveStreamResult.MediaSource
                        })
                    }) : mediaSource : (showPlaybackInfoErrorMessage(self, "NoCompatibleStream"), Promise.reject())
                }) : Promise.reject()
            })
        }

        function getPlayer(item, playOptions, forceLocalPlayers) {
            var serverItem = isServerItem(item);
            return getAutomaticPlayers(self, forceLocalPlayers).filter(function(p) {
                if (p.canPlayMediaType(item.MediaType)) {
                    if (serverItem) return !p.canPlayItem || p.canPlayItem(item, playOptions);
                    if (item.Url && p.canPlayUrl) return p.canPlayUrl(item.Url)
                }
                return !1
            })[0]
        }

        function queue(options, mode, player) {
            if (!(player = player || self._currentPlayer)) return self.play(options);
            if (options.items) return translateItemsForPlayback(options.items, options).then(function(items) {
                queueAll(items, mode, player)
            });
            if (!options.serverId) throw new Error("serverId required!");
            return getItemsForPlayback(options.serverId, {
                Ids: options.ids.join(",")
            }).then(function(result) {
                return translateItemsForPlayback(result.Items, options).then(function(items) {
                    queueAll(items, mode, player)
                })
            })
        }

        function queueAll(items, mode, player) {
            if (items.length) {
                if (!player.isLocalPlayer) return void("next" === mode ? player.queueNext({
                    items: items
                }) : player.queue({
                    items: items
                }));
                if (player && !enableLocalPlaylistManagement(player)) {
                    var apiClient = connectionManager.getApiClient(items[0].ServerId);
                    return void player.getDeviceProfile(items[0]).then(function(profile) {
                        setStreamUrls(items, profile, self.getMaxStreamingBitrate(player), apiClient, 0).then(function() {
                            "next" === mode ? player.queueNext(items) : player.queue(items)
                        })
                    })
                }
                "next" === mode ? self._playQueueManager.queueNext(items) : self._playQueueManager.queue(items)
            }
        }

        function onPlayerProgressInterval() {
            sendProgressUpdate(this, "timeupdate")
        }

        function startPlaybackProgressTimer(player) {
            stopPlaybackProgressTimer(player), player._progressInterval = setInterval(onPlayerProgressInterval.bind(player), 1e4)
        }

        function stopPlaybackProgressTimer(player) {
            player._progressInterval && (clearInterval(player._progressInterval), player._progressInterval = null)
        }

        function onPlaybackStarted(player, playOptions, streamInfo, mediaSource) {
            if (!player) throw new Error("player cannot be null");
            setCurrentPlayerInternal(player);
            var playerData = getPlayerData(player);
            playerData.streamInfo = streamInfo, streamInfo.playbackStartTimeTicks = 1e4 * (new Date).getTime(), mediaSource ? (playerData.audioStreamIndex = mediaSource.DefaultAudioStreamIndex, playerData.subtitleStreamIndex = mediaSource.DefaultSubtitleStreamIndex) : (playerData.audioStreamIndex = null, playerData.subtitleStreamIndex = null), self._playNextAfterEnded = !0;
            var isFirstItem = playOptions.isFirstItem,
                fullscreen = playOptions.fullscreen,
                state = self.getPlayerState(player, streamInfo.item, streamInfo.mediaSource);
            reportPlayback(self, state, player, !0, state.NowPlayingItem.ServerId, "reportPlaybackStart"), state.IsFirstItem = isFirstItem, state.IsFullscreen = fullscreen, events.trigger(player, "playbackstart", [state]), events.trigger(self, "playbackstart", [player, state]), streamInfo.started = !0, startPlaybackProgressTimer(player)
        }

        function onPlaybackStartedFromSelfManagingPlayer(e, item, mediaSource) {
            var player = this;
            setCurrentPlayerInternal(player);
            var playOptions = item.playOptions || {},
                isFirstItem = playOptions.isFirstItem,
                fullscreen = playOptions.fullscreen;
            playOptions.isFirstItem = !1;
            var playerData = getPlayerData(player);
            playerData.streamInfo = {};
            var streamInfo = playerData.streamInfo;
            streamInfo.playbackStartTimeTicks = 1e4 * (new Date).getTime();
            var state = self.getPlayerState(player, item, mediaSource);
            reportPlayback(self, state, player, !0, state.NowPlayingItem.ServerId, "reportPlaybackStart"), state.IsFirstItem = isFirstItem, state.IsFullscreen = fullscreen, events.trigger(player, "playbackstart", [state]), events.trigger(self, "playbackstart", [player, state]), streamInfo.started = !0, startPlaybackProgressTimer(player)
        }

        function onPlaybackStoppedFromSelfManagingPlayer(e, playerStopInfo) {
            var player = this;
            stopPlaybackProgressTimer(player);
            var state = self.getPlayerState(player, playerStopInfo.item, playerStopInfo.mediaSource),
                nextItem = playerStopInfo.nextItem,
                nextMediaType = playerStopInfo.nextMediaType,
                playbackStopInfo = {
                    player: player,
                    state: state,
                    nextItem: nextItem ? nextItem.item : null,
                    nextMediaType: nextMediaType
                };
            state.NextMediaType = nextMediaType, getPlayerData(player).streamInfo.ended = !0, isServerItem(playerStopInfo.item) && (state.PlayState.PositionTicks = 1e4 * (playerStopInfo.positionMs || 0), reportPlayback(self, state, player, !0, playerStopInfo.item.ServerId, "reportPlaybackStopped")), state.NextItem = playbackStopInfo.nextItem, events.trigger(player, "playbackstop", [state]), events.trigger(self, "playbackstop", [playbackStopInfo]);
            var nextItemPlayOptions = nextItem ? nextItem.item.playOptions || getDefaultPlayOptions() : getDefaultPlayOptions();
            (nextItem ? getPlayer(nextItem.item, nextItemPlayOptions) : null) !== player && (destroyPlayer(player), removeCurrentPlayer(player))
        }

        function enablePlaybackRetryWithTranscoding(streamInfo, errorType, currentlyPreventsVideoStreamCopy, currentlyPreventsAudioStreamCopy) {
            return !(!streamInfo.mediaSource.SupportsTranscoding || currentlyPreventsVideoStreamCopy && currentlyPreventsAudioStreamCopy)
        }

        function onPlaybackError(e, error) {
            var player = this;
            error = error || {};
            var errorType = error.type;
            console.log("playbackmanager playback error type: " + (errorType || ""));
            var streamInfo = error.streamInfo || getPlayerData(player).streamInfo;
            if (streamInfo) {
                var currentlyPreventsVideoStreamCopy = -1 !== streamInfo.url.toLowerCase().indexOf("allowvideostreamcopy=false"),
                    currentlyPreventsAudioStreamCopy = -1 !== streamInfo.url.toLowerCase().indexOf("allowaudiostreamcopy=false");
                if (enablePlaybackRetryWithTranscoding(streamInfo, errorType, currentlyPreventsVideoStreamCopy, currentlyPreventsAudioStreamCopy)) {
                    return void changeStream(player, getCurrentTicks(player) || streamInfo.playerStartPositionTicks, {
                        EnableDirectPlay: !1,
                        EnableDirectStream: !1,
                        AllowVideoStreamCopy: !1,
                        AllowAudioStreamCopy: !currentlyPreventsAudioStreamCopy && !currentlyPreventsVideoStreamCopy && null
                    }, !0)
                }
            }
            onPlaybackStopped.call(player, e, "NoCompatibleStream")
        }

        function onPlaybackStopped(e, displayErrorCode) {
            var player = this;
            if (!getPlayerData(player).isChangingStream) {
                stopPlaybackProgressTimer(player);
                var state = self.getPlayerState(player),
                    streamInfo = getPlayerData(player).streamInfo,
                    nextItem = self._playNextAfterEnded ? self._playQueueManager.getNextItemInfo() : null,
                    nextMediaType = nextItem ? nextItem.item.MediaType : null,
                    playbackStopInfo = {
                        player: player,
                        state: state,
                        nextItem: nextItem ? nextItem.item : null,
                        nextMediaType: nextMediaType
                    };
                state.NextMediaType = nextMediaType, isServerItem(streamInfo.item) && (!1 === player.supportsProgress && state.PlayState && !state.PlayState.PositionTicks && (state.PlayState.PositionTicks = streamInfo.item.RunTimeTicks), streamInfo.ended = !0, reportPlayback(self, state, player, !0, streamInfo.item.ServerId, "reportPlaybackStopped")), state.NextItem = playbackStopInfo.nextItem, nextItem || self._playQueueManager.reset(), events.trigger(player, "playbackstop", [state]), events.trigger(self, "playbackstop", [playbackStopInfo]);
                var nextItemPlayOptions = nextItem ? nextItem.item.playOptions || getDefaultPlayOptions() : getDefaultPlayOptions();
                (nextItem ? getPlayer(nextItem.item, nextItemPlayOptions) : null) !== player && (destroyPlayer(player), removeCurrentPlayer(player)), displayErrorCode && "string" == typeof displayErrorCode ? showPlaybackInfoErrorMessage(self, displayErrorCode, nextItem) : nextItem && self.nextTrack()
            }
        }

        function onPlaybackChanging(activePlayer, newPlayer, newItem) {
            var promise, state = self.getPlayerState(activePlayer),
                serverId = self.currentItem(activePlayer).ServerId;
            return stopPlaybackProgressTimer(activePlayer), unbindStopped(activePlayer), promise = activePlayer === newPlayer ? activePlayer.stop(!1) : activePlayer.stop(!0), promise.then(function() {
                bindStopped(activePlayer), enableLocalPlaylistManagement(activePlayer) && reportPlayback(self, state, activePlayer, !0, serverId, "reportPlaybackStopped"), events.trigger(self, "playbackstop", [{
                    player: activePlayer,
                    state: state,
                    nextItem: newItem,
                    nextMediaType: newItem.MediaType
                }])
            })
        }

        function bindStopped(player) {
            enableLocalPlaylistManagement(player) && (events.off(player, "stopped", onPlaybackStopped), events.on(player, "stopped", onPlaybackStopped))
        }

        function onPlaybackTimeUpdate(e) {
            sendProgressUpdate(this, "timeupdate")
        }

        function onPlaybackPause(e) {
            sendProgressUpdate(this, "pause")
        }

        function onPlaybackUnpause(e) {
            sendProgressUpdate(this, "unpause")
        }

        function onPlaybackVolumeChange(e) {
            sendProgressUpdate(this, "volumechange")
        }

        function onRepeatModeChange(e) {
            sendProgressUpdate(this, "repeatmodechange")
        }

        function onPlaylistItemMove(e) {
            sendProgressUpdate(this, "playlistitemmove", !0)
        }

        function onPlaylistItemRemove(e) {
            sendProgressUpdate(this, "playlistitemremove", !0)
        }

        function onPlaylistItemAdd(e) {
            sendProgressUpdate(this, "playlistitemadd", !0)
        }

        function unbindStopped(player) {
            events.off(player, "stopped", onPlaybackStopped)
        }

        function initLegacyVolumeMethods(player) {
            player.getVolume = function() {
                return player.volume()
            }, player.setVolume = function(val) {
                return player.volume(val)
            }
        }

        function initMediaPlayer(player) {
            players.push(player), players.sort(function(a, b) {
                return (a.priority || 0) - (b.priority || 0)
            }), !1 !== player.isLocalPlayer && (player.isLocalPlayer = !0), player.currentState = {}, player.getVolume && player.setVolume || initLegacyVolumeMethods(player), enableLocalPlaylistManagement(player) ? (events.on(player, "error", onPlaybackError), events.on(player, "timeupdate", onPlaybackTimeUpdate), events.on(player, "pause", onPlaybackPause), events.on(player, "unpause", onPlaybackUnpause), events.on(player, "volumechange", onPlaybackVolumeChange), events.on(player, "repeatmodechange", onRepeatModeChange), events.on(player, "playlistitemmove", onPlaylistItemMove), events.on(player, "playlistitemremove", onPlaylistItemRemove), events.on(player, "playlistitemadd", onPlaylistItemAdd)) : player.isLocalPlayer && (events.on(player, "itemstarted", onPlaybackStartedFromSelfManagingPlayer), events.on(player, "itemstopped", onPlaybackStoppedFromSelfManagingPlayer), events.on(player, "timeupdate", onPlaybackTimeUpdate), events.on(player, "pause", onPlaybackPause), events.on(player, "unpause", onPlaybackUnpause), events.on(player, "volumechange", onPlaybackVolumeChange), events.on(player, "repeatmodechange", onRepeatModeChange), events.on(player, "playlistitemmove", onPlaylistItemMove), events.on(player, "playlistitemremove", onPlaylistItemRemove), events.on(player, "playlistitemadd", onPlaylistItemAdd)), player.isLocalPlayer && bindToFullscreenChange(player), bindStopped(player)
        }

        function sendProgressUpdate(player, progressEventName, reportPlaylist) {
            if (!player) throw new Error("player cannot be null");
            var state = self.getPlayerState(player);
            if (state.NowPlayingItem) {
                var serverId = state.NowPlayingItem.ServerId,
                    streamInfo = getPlayerData(player).streamInfo;
                streamInfo && streamInfo.started && !streamInfo.ended && reportPlayback(self, state, player, reportPlaylist, serverId, "reportPlaybackProgress", progressEventName), streamInfo && streamInfo.liveStreamId && (new Date).getTime() - (streamInfo.lastMediaInfoQuery || 0) >= 6e5 && getLiveStreamMediaInfo(player, streamInfo, self.currentMediaSource(player), streamInfo.liveStreamId, serverId)
            }
        }

        function getLiveStreamMediaInfo(player, streamInfo, mediaSource, liveStreamId, serverId) {
            console.log("getLiveStreamMediaInfo"), streamInfo.lastMediaInfoQuery = (new Date).getTime(), connectionManager.getApiClient(serverId).isMinServerVersion("3.2.70.7") && connectionManager.getApiClient(serverId).getLiveStreamMediaInfo(liveStreamId).then(function(info) {
                mediaSource.MediaStreams = info.MediaStreams, events.trigger(player, "mediastreamschange")
            }, function() {})
        }
        var currentTargetInfo, lastLocalPlayer, self = this,
            players = [],
            currentPairingId = null;
        this._playNextAfterEnded = !0;
        var playerStates = {};
        this._playQueueManager = new PlayQueueManager, self.currentItem = function(player) {
            if (!player) throw new Error("player cannot be null");
            if (player.currentItem) return player.currentItem();
            var data = getPlayerData(player);
            return data.streamInfo ? data.streamInfo.item : null
        }, self.currentMediaSource = function(player) {
            if (!player) throw new Error("player cannot be null");
            if (player.currentMediaSource) return player.currentMediaSource();
            var data = getPlayerData(player);
            return data.streamInfo ? data.streamInfo.mediaSource : null
        }, self.playMethod = function(player) {
            if (!player) throw new Error("player cannot be null");
            if (player.playMethod) return player.playMethod();
            var data = getPlayerData(player);
            return data.streamInfo ? data.streamInfo.playMethod : null
        }, self.playSessionId = function(player) {
            if (!player) throw new Error("player cannot be null");
            if (player.playSessionId) return player.playSessionId();
            var data = getPlayerData(player);
            return data.streamInfo ? data.streamInfo.playSessionId : null
        }, self.getPlayerInfo = function() {
            var player = self._currentPlayer;
            if (!player) return null;
            var target = currentTargetInfo || {};
            return {
                name: player.name,
                isLocalPlayer: player.isLocalPlayer,
                id: target.id,
                deviceName: target.deviceName,
                playableMediaTypes: target.playableMediaTypes,
                supportedCommands: target.supportedCommands
            }
        }, self.setActivePlayer = function(player, targetInfo) {
            if ("localplayer" === player || "localplayer" === player.name) {
                if (self._currentPlayer && self._currentPlayer.isLocalPlayer) return;
                return void setCurrentPlayerInternal(null, null)
            }
            if ("string" == typeof player && (player = players.filter(function(p) {
                    return p.name === player
                })[0]), !player) throw new Error("null player");
            setCurrentPlayerInternal(player, targetInfo)
        }, self.trySetActivePlayer = function(player, targetInfo) {
            if ("localplayer" === player || "localplayer" === player.name) return void(self._currentPlayer && self._currentPlayer.isLocalPlayer);
            if ("string" == typeof player && (player = players.filter(function(p) {
                    return p.name === player
                })[0]), !player) throw new Error("null player");
            if (currentPairingId !== targetInfo.id) {
                currentPairingId = targetInfo.id;
                var promise = player.tryPair ? player.tryPair(targetInfo) : Promise.resolve();
                events.trigger(self, "pairing"), promise.then(function() {
                    events.trigger(self, "paired"), setCurrentPlayerInternal(player, targetInfo)
                }, function() {
                    events.trigger(self, "pairerror"), currentPairingId === targetInfo.id && (currentPairingId = null)
                })
            }
        }, self.getTargets = function() {
            var promises = players.filter(displayPlayerIndividually).map(getPlayerTargets);
            return Promise.all(promises).then(function(responses) {
                return connectionManager.currentApiClient().getCurrentUser().then(function(user) {
                    var targets = [];
                    targets.push({
                        name: globalize.translate("sharedcomponents#HeaderMyDevice"),
                        id: "localplayer",
                        playerName: "localplayer",
                        playableMediaTypes: ["Audio", "Video", "Game", "Photo", "Book"],
                        isLocalPlayer: !0,
                        supportedCommands: self.getSupportedCommands({
                            isLocalPlayer: !0
                        }),
                        user: user
                    });
                    for (var i = 0; i < responses.length; i++)
                        for (var subTargets = responses[i], j = 0; j < subTargets.length; j++) targets.push(subTargets[j]);
                    return targets = targets.sort(sortPlayerTargets)
                })
            })
        }, self.getPlaylist = function(player) {
            return player = player || self._currentPlayer, player && !enableLocalPlaylistManagement(player) ? player.getPlaylistSync ? Promise.resolve(player.getPlaylistSync()) : player.getPlaylist() : Promise.resolve(self._playQueueManager.getPlaylist())
        }, self.isPlaying = function(player) {
            return player = player || self._currentPlayer, player && player.isPlaying ? player.isPlaying() : null != player && null != player.currentSrc()
        }, self.isPlayingMediaType = function(mediaType, player) {
            if ((player = player || self._currentPlayer) && player.isPlaying) return player.isPlaying(mediaType);
            if (self.isPlaying(player)) {
                return getPlayerData(player).streamInfo.mediaType === mediaType
            }
            return !1
        }, self.isPlayingLocally = function(mediaTypes, player) {
            return !(!(player = player || self._currentPlayer) || !player.isLocalPlayer) && mediaTypes.filter(function(mediaType) {
                return self.isPlayingMediaType(mediaType, player)
            }).length > 0
        }, self.isPlayingVideo = function(player) {
            return self.isPlayingMediaType("Video", player)
        }, self.isPlayingAudio = function(player) {
            return self.isPlayingMediaType("Audio", player)
        }, self.getPlayers = function() {
            return players
        }, self.canPlay = function(item) {
            var itemType = item.Type;
            if ("PhotoAlbum" === itemType || "MusicGenre" === itemType || "Season" === itemType || "Series" === itemType || "BoxSet" === itemType || "MusicAlbum" === itemType || "MusicArtist" === itemType || "Playlist" === itemType) return !0;
            if ("Virtual" === item.LocationType && "Program" !== itemType) return !1;
            if ("Program" === itemType) {
                if (!item.EndDate || !item.StartDate) return !1;
                if ((new Date).getTime() > datetime.parseISO8601Date(item.EndDate).getTime() || (new Date).getTime() < datetime.parseISO8601Date(item.StartDate).getTime()) return !1
            }
            return null != getPlayer(item, getDefaultPlayOptions())
        }, self.toggleAspectRatio = function(player) {
            if (player = player || self._currentPlayer) {
                for (var current = self.getAspectRatio(player), supported = self.getSupportedAspectRatios(player), index = -1, i = 0, length = supported.length; i < length; i++)
                    if (supported[i].id === current) {
                        index = i;
                        break
                    } index++, index >= supported.length && (index = 0), self.setAspectRatio(supported[index].id, player)
            }
        }, self.setAspectRatio = function(val, player) {
            (player = player || self._currentPlayer) && player.setAspectRatio && player.setAspectRatio(val)
        }, self.getSupportedAspectRatios = function(player) {
            return player = player || self._currentPlayer, player && player.getSupportedAspectRatios ? player.getSupportedAspectRatios() : []
        }, self.getAspectRatio = function(player) {
            if ((player = player || self._currentPlayer) && player.getAspectRatio) return player.getAspectRatio()
        };
        var brightnessOsdLoaded;
        self.setBrightness = function(val, player) {
            (player = player || self._currentPlayer) && (brightnessOsdLoaded || (brightnessOsdLoaded = !0, require(["brightnessOsd"])), player.setBrightness(val))
        }, self.getBrightness = function(player) {
            if (player = player || self._currentPlayer) return player.getBrightness()
        }, self.setVolume = function(val, player) {
            (player = player || self._currentPlayer) && player.setVolume(val)
        }, self.getVolume = function(player) {
            if (player = player || self._currentPlayer) return player.getVolume()
        }, self.volumeUp = function(player) {
            (player = player || self._currentPlayer) && player.volumeUp()
        }, self.volumeDown = function(player) {
            (player = player || self._currentPlayer) && player.volumeDown()
        }, self.changeAudioStream = function(player) {
            if ((player = player || self._currentPlayer) && !enableLocalPlaylistManagement(player)) return player.changeAudioStream();
            if (player) {
                var i, length, currentMediaSource = self.currentMediaSource(player),
                    mediaStreams = [];
                for (i = 0, length = currentMediaSource.MediaStreams.length; i < length; i++) "Audio" === currentMediaSource.MediaStreams[i].Type && mediaStreams.push(currentMediaSource.MediaStreams[i]);
                if (!(mediaStreams.length <= 1)) {
                    var currentStreamIndex = self.getAudioStreamIndex(player),
                        indexInList = -1;
                    for (i = 0, length = mediaStreams.length; i < length; i++)
                        if (mediaStreams[i].Index === currentStreamIndex) {
                            indexInList = i;
                            break
                        } var nextIndex = indexInList + 1;
                    nextIndex >= mediaStreams.length && (nextIndex = 0), nextIndex = -1 === nextIndex ? -1 : mediaStreams[nextIndex].Index, self.setAudioStreamIndex(nextIndex, player)
                }
            }
        }, self.changeSubtitleStream = function(player) {
            if ((player = player || self._currentPlayer) && !enableLocalPlaylistManagement(player)) return player.changeSubtitleStream();
            if (player) {
                var i, length, currentMediaSource = self.currentMediaSource(player),
                    mediaStreams = [];
                for (i = 0, length = currentMediaSource.MediaStreams.length; i < length; i++) "Subtitle" === currentMediaSource.MediaStreams[i].Type && mediaStreams.push(currentMediaSource.MediaStreams[i]);
                if (mediaStreams.length) {
                    var currentStreamIndex = self.getSubtitleStreamIndex(player),
                        indexInList = -1;
                    for (i = 0, length = mediaStreams.length; i < length; i++)
                        if (mediaStreams[i].Index === currentStreamIndex) {
                            indexInList = i;
                            break
                        } var nextIndex = indexInList + 1;
                    nextIndex >= mediaStreams.length && (nextIndex = -1), nextIndex = -1 === nextIndex ? -1 : mediaStreams[nextIndex].Index, self.setSubtitleStreamIndex(nextIndex, player)
                }
            }
        }, self.getAudioStreamIndex = function(player) {
            return player = player || self._currentPlayer, player && !enableLocalPlaylistManagement(player) ? player.getAudioStreamIndex() : getPlayerData(player).audioStreamIndex
        }, self.setAudioStreamIndex = function(index, player) {
            if ((player = player || self._currentPlayer) && !enableLocalPlaylistManagement(player)) return player.setAudioStreamIndex(index);
            "Transcode" !== self.playMethod(player) && player.canSetAudioStreamIndex() ? player.getDeviceProfile(self.currentItem(player)).then(function(profile) {
                isAudioStreamSupported(self.currentMediaSource(player), index, profile) ? (player.setAudioStreamIndex(index), getPlayerData(player).audioStreamIndex = index) : (changeStream(player, getCurrentTicks(player), {
                    AudioStreamIndex: index
                }), getPlayerData(player).audioStreamIndex = index)
            }) : (changeStream(player, getCurrentTicks(player), {
                AudioStreamIndex: index
            }), getPlayerData(player).audioStreamIndex = index)
        }, self.getMaxStreamingBitrate = function(player) {
            if ((player = player || self._currentPlayer) && player.getMaxStreamingBitrate) return player.getMaxStreamingBitrate();
            var playerData = getPlayerData(player);
            if (playerData.maxStreamingBitrate) return playerData.maxStreamingBitrate;
            var mediaType = playerData.streamInfo ? playerData.streamInfo.mediaType : null,
                currentItem = self.currentItem(player);
            return getSavedMaxStreamingBitrate(currentItem ? connectionManager.getApiClient(currentItem.ServerId) : connectionManager.currentApiClient(), mediaType)
        }, self.enableAutomaticBitrateDetection = function(player) {
            if ((player = player || self._currentPlayer) && player.enableAutomaticBitrateDetection) return player.enableAutomaticBitrateDetection();
            var playerData = getPlayerData(player),
                mediaType = playerData.streamInfo ? playerData.streamInfo.mediaType : null,
                currentItem = self.currentItem(player),
                apiClient = currentItem ? connectionManager.getApiClient(currentItem.ServerId) : connectionManager.currentApiClient(),
                endpointInfo = apiClient.getSavedEndpointInfo() || {};
            return appSettings.enableAutomaticBitrateDetection(endpointInfo.IsInNetwork, mediaType)
        }, self.setMaxStreamingBitrate = function(options, player) {
            if ((player = player || self._currentPlayer) && player.setMaxStreamingBitrate) return player.setMaxStreamingBitrate(options);
            var apiClient = connectionManager.getApiClient(self.currentItem(player).ServerId);
            apiClient.getEndpointInfo().then(function(endpointInfo) {
                var promise, playerData = getPlayerData(player),
                    mediaType = playerData.streamInfo ? playerData.streamInfo.mediaType : null;
                options.enableAutomaticBitrateDetection ? (appSettings.enableAutomaticBitrateDetection(endpointInfo.IsInNetwork, mediaType, !0), promise = apiClient.detectBitrate(!0)) : (appSettings.enableAutomaticBitrateDetection(endpointInfo.IsInNetwork, mediaType, !1), promise = Promise.resolve(options.maxBitrate)), promise.then(function(bitrate) {
                    appSettings.maxStreamingBitrate(endpointInfo.IsInNetwork, mediaType, bitrate), changeStream(player, getCurrentTicks(player), {
                        MaxStreamingBitrate: bitrate
                    })
                })
            })
        }, self.isFullscreen = function(player) {
            return player = player || self._currentPlayer, !player.isLocalPlayer || player.isFullscreen ? player.isFullscreen() : fullscreenManager.isFullScreen()
        }, self.toggleFullscreen = function(player) {
            if (player = player || self._currentPlayer, !player.isLocalPlayer || player.toggleFulscreen) return player.toggleFulscreen();
            fullscreenManager.isFullScreen() ? fullscreenManager.exitFullscreen() : fullscreenManager.requestFullscreen()
        }, self.togglePictureInPicture = function(player) {
            return player = player || self._currentPlayer, player.togglePictureInPicture()
        }, self.getSubtitleStreamIndex = function(player) {
            if ((player = player || self._currentPlayer) && !enableLocalPlaylistManagement(player)) return player.getSubtitleStreamIndex();
            if (!player) throw new Error("player cannot be null");
            return getPlayerData(player).subtitleStreamIndex
        }, self.setSubtitleStreamIndex = function(index, player) {
            if ((player = player || self._currentPlayer) && !enableLocalPlaylistManagement(player)) return player.setSubtitleStreamIndex(index);
            var currentStream = getCurrentSubtitleStream(player),
                newStream = getSubtitleStream(player, index);
            if (currentStream || newStream) {
                var selectedTrackElementIndex = -1,
                    currentPlayMethod = self.playMethod(player);
                currentStream && !newStream ? ("Encode" === getDeliveryMethod(currentStream) || "Embed" === getDeliveryMethod(currentStream) && "Transcode" === currentPlayMethod) && changeStream(player, getCurrentTicks(player), {
                    SubtitleStreamIndex: -1
                }) : !currentStream && newStream ? "External" === getDeliveryMethod(newStream) ? selectedTrackElementIndex = index : "Embed" === getDeliveryMethod(newStream) && "Transcode" !== currentPlayMethod ? selectedTrackElementIndex = index : changeStream(player, getCurrentTicks(player), {
                    SubtitleStreamIndex: index
                }) : currentStream && newStream && ("External" === getDeliveryMethod(newStream) || "Embed" === getDeliveryMethod(newStream) && "Transcode" !== currentPlayMethod ? (selectedTrackElementIndex = index, "External" !== getDeliveryMethod(currentStream) && "Embed" !== getDeliveryMethod(currentStream) && changeStream(player, getCurrentTicks(player), {
                    SubtitleStreamIndex: -1
                })) : changeStream(player, getCurrentTicks(player), {
                    SubtitleStreamIndex: index
                })), player.setSubtitleStreamIndex(selectedTrackElementIndex), getPlayerData(player).subtitleStreamIndex = index
            }
        }, self.seek = function(ticks, player) {
            if (ticks = Math.max(0, ticks), (player = player || self._currentPlayer) && !enableLocalPlaylistManagement(player)) return player.isLocalPlayer ? player.seek((ticks || 0) / 1e4) : player.seek(ticks);
            changeStream(player, ticks)
        }, self.seekRelative = function(offsetTicks, player) {
            if ((player = player || self._currentPlayer) && !enableLocalPlaylistManagement(player) && player.seekRelative) return player.isLocalPlayer ? player.seekRelative((ticks || 0) / 1e4) : player.seekRelative(ticks);
            var ticks = getCurrentTicks(player) + offsetTicks;
            return this.seek(ticks, player)
        }, self.play = function(options) {
            if (normalizePlayOptions(options), self._currentPlayer) {
                if (!1 === options.enableRemotePlayers && !self._currentPlayer.isLocalPlayer) return Promise.reject();
                if (!self._currentPlayer.isLocalPlayer) return self._currentPlayer.play(options)
            }
            if (options.fullscreen && loading.show(), options.items) return translateItemsForPlayback(options.items, options).then(function(items) {
                return playWithIntros(items, options)
            });
            if (!options.serverId) throw new Error("serverId required!");
            return getItemsForPlayback(options.serverId, {
                Ids: options.ids.join(",")
            }).then(function(result) {
                return translateItemsForPlayback(result.Items, options).then(function(items) {
                    return playWithIntros(items, options)
                })
            })
        }, self.getPlayerState = function(player, item, mediaSource) {
            if (!(player = player || self._currentPlayer)) throw new Error("player cannot be null");
            if (!enableLocalPlaylistManagement(player) && player.getPlayerState) return player.getPlayerState();
            item = item || self.currentItem(player), mediaSource = mediaSource || self.currentMediaSource(player);
            var state = {
                PlayState: {}
            };
            return player && (state.PlayState.VolumeLevel = player.getVolume(), state.PlayState.IsMuted = player.isMuted(), state.PlayState.IsPaused = player.paused(), state.PlayState.RepeatMode = self.getRepeatMode(player), state.PlayState.MaxStreamingBitrate = self.getMaxStreamingBitrate(player), state.PlayState.PositionTicks = getCurrentTicks(player), state.PlayState.PlaybackStartTimeTicks = self.playbackStartTime(player), state.PlayState.SubtitleStreamIndex = self.getSubtitleStreamIndex(player), state.PlayState.AudioStreamIndex = self.getAudioStreamIndex(player), state.PlayState.BufferedRanges = self.getBufferedRanges(player), state.PlayState.PlayMethod = self.playMethod(player), mediaSource && (state.PlayState.LiveStreamId = mediaSource.LiveStreamId), state.PlayState.PlaySessionId = self.playSessionId(player), state.PlayState.PlaylistItemId = self.getCurrentPlaylistItemId(player)), mediaSource && (state.PlayState.MediaSourceId = mediaSource.Id, state.NowPlayingItem = {
                RunTimeTicks: mediaSource.RunTimeTicks
            }, state.PlayState.CanSeek = (mediaSource.RunTimeTicks || 0) > 0 || canPlayerSeek(player)), item && (state.NowPlayingItem = getNowPlayingItemForReporting(player, item, mediaSource)), state.MediaSource = mediaSource, state
        }, self.duration = function(player) {
            if ((player = player || self._currentPlayer) && !enableLocalPlaylistManagement(player) && !player.isLocalPlayer) return player.duration();
            if (!player) throw new Error("player cannot be null");
            var mediaSource = self.currentMediaSource(player);
            if (mediaSource && mediaSource.RunTimeTicks) return mediaSource.RunTimeTicks;
            var playerDuration = player.duration();
            return playerDuration && (playerDuration *= 1e4), playerDuration
        }, self.getCurrentTicks = getCurrentTicks, self.getPlaybackInfo = function(item, options) {
            options = options || {};
            var startPosition = options.startPositionTicks || 0,
                mediaType = options.mediaType || item.MediaType,
                player = getPlayer(item, options),
                apiClient = connectionManager.getApiClient(item.ServerId);
            return apiClient.getEndpointInfo().then(function() {
                var maxBitrate = getSavedMaxStreamingBitrate(connectionManager.getApiClient(item.ServerId), mediaType);
                return player.getDeviceProfile(item).then(function(deviceProfile) {
                    return getPlaybackMediaSource(player, apiClient, deviceProfile, maxBitrate, item, startPosition, options.mediaSourceId, options.audioStreamIndex, options.subtitleStreamIndex).then(function(mediaSource) {
                        return createStreamInfo(apiClient, item.MediaType, item, mediaSource, startPosition)
                    })
                })
            })
        }, self.getPlaybackMediaSources = function(item, options) {
            options = options || {};
            var startPosition = options.startPositionTicks || 0,
                mediaType = options.mediaType || item.MediaType,
                player = getPlayer(item, options, !0),
                apiClient = connectionManager.getApiClient(item.ServerId);
            return apiClient.getEndpointInfo().then(function() {
                var maxBitrate = getSavedMaxStreamingBitrate(connectionManager.getApiClient(item.ServerId), mediaType);
                return player.getDeviceProfile(item).then(function(deviceProfile) {
                    return getPlaybackInfo(player, apiClient, item, deviceProfile, maxBitrate, startPosition, !1, null, null, null, null).then(function(playbackInfoResult) {
                        return playbackInfoResult.MediaSources
                    })
                })
            })
        }, self.setCurrentPlaylistItem = function(playlistItemId, player) {
            if ((player = player || self._currentPlayer) && !enableLocalPlaylistManagement(player)) return player.setCurrentPlaylistItem(playlistItemId);
            for (var newItem, newItemIndex, playlist = self._playQueueManager.getPlaylist(), i = 0, length = playlist.length; i < length; i++)
                if (playlist[i].PlaylistItemId === playlistItemId) {
                    newItem = playlist[i], newItemIndex = i;
                    break
                } if (newItem) {
                var newItemPlayOptions = newItem.playOptions || {};
                playInternal(newItem, newItemPlayOptions, function() {
                    setPlaylistState(newItem.PlaylistItemId, newItemIndex)
                })
            }
        }, self.removeFromPlaylist = function(playlistItemIds, player) {
            if (!playlistItemIds) throw new Error("Invalid playlistItemIds");
            if ((player = player || self._currentPlayer) && !enableLocalPlaylistManagement(player)) return player.removeFromPlaylist(playlistItemIds);
            var removeResult = self._playQueueManager.removeFromPlaylist(playlistItemIds);
            if ("empty" === removeResult.result) return self.stop(player);
            var isCurrentIndex = removeResult.isCurrentIndex;
            return events.trigger(player, "playlistitemremove", [{
                playlistItemIds: playlistItemIds
            }]), isCurrentIndex ? self.setCurrentPlaylistItem(self._playQueueManager.getPlaylist()[0].PlaylistItemId, player) : Promise.resolve()
        }, self.movePlaylistItem = function(playlistItemId, newIndex, player) {
            if ((player = player || self._currentPlayer) && !enableLocalPlaylistManagement(player)) return player.movePlaylistItem(playlistItemId, newIndex);
            var moveResult = self._playQueueManager.movePlaylistItem(playlistItemId, newIndex);
            "noop" !== moveResult.result && events.trigger(player, "playlistitemmove", [{
                playlistItemId: moveResult.playlistItemId,
                newIndex: moveResult.newIndex
            }])
        }, self.getCurrentPlaylistIndex = function(player) {
            return player = player || self._currentPlayer, player && !enableLocalPlaylistManagement(player) ? player.getCurrentPlaylistIndex() : self._playQueueManager.getCurrentPlaylistIndex()
        }, self.getCurrentPlaylistItemId = function(player) {
            return player = player || self._currentPlayer, player && !enableLocalPlaylistManagement(player) ? player.getCurrentPlaylistItemId() : self._playQueueManager.getCurrentPlaylistItemId()
        }, self.channelUp = function(player) {
            return player = player || self._currentPlayer, self.nextTrack(player)
        }, self.channelDown = function(player) {
            return player = player || self._currentPlayer, self.previousTrack(player)
        }, self.nextTrack = function(player) {
            if ((player = player || self._currentPlayer) && !enableLocalPlaylistManagement(player)) return player.nextTrack();
            var newItemInfo = self._playQueueManager.getNextItemInfo();
            if (newItemInfo) {
                console.log("playing next track");
                var newItemPlayOptions = newItemInfo.item.playOptions || {};
                playInternal(newItemInfo.item, newItemPlayOptions, function() {
                    setPlaylistState(newItemInfo.item.PlaylistItemId, newItemInfo.index)
                })
            }
        }, self.previousTrack = function(player) {
            if ((player = player || self._currentPlayer) && !enableLocalPlaylistManagement(player)) return player.previousTrack();
            var newIndex = self.getCurrentPlaylistIndex(player) - 1;
            if (newIndex >= 0) {
                var playlist = self._playQueueManager.getPlaylist(),
                    newItem = playlist[newIndex];
                if (newItem) {
                    var newItemPlayOptions = newItem.playOptions || {};
                    newItemPlayOptions.startPositionTicks = 0, playInternal(newItem, newItemPlayOptions, function() {
                        setPlaylistState(newItem.PlaylistItemId, newIndex)
                    })
                }
            }
        }, self.queue = function(options, player) {
            queue(options, "", player)
        }, self.queueNext = function(options, player) {
            queue(options, "next", player)
        }, events.on(pluginManager, "registered", function(e, plugin) {
            "mediaplayer" === plugin.type && initMediaPlayer(plugin)
        }), pluginManager.ofType("mediaplayer").map(initMediaPlayer), self.onAppClose = function() {
            var player = this._currentPlayer;
            player && this.isPlaying(player) && (this._playNextAfterEnded = !1, onPlaybackStopped.call(player))
        }, self.playbackStartTime = function(player) {
            if ((player = player || this._currentPlayer) && !enableLocalPlaylistManagement(player) && !player.isLocalPlayer) return player.playbackStartTime();
            var streamInfo = getPlayerData(player).streamInfo;
            return streamInfo ? streamInfo.playbackStartTimeTicks : null
        }, apphost.supports("remotecontrol") && require(["serverNotifications"], function(serverNotifications) {
            events.on(serverNotifications, "ServerShuttingDown", self.setDefaultPlayerActive.bind(self)), events.on(serverNotifications, "ServerRestarting", self.setDefaultPlayerActive.bind(self))
        })
    }
    var startingPlaySession = (new Date).getTime();
    return PlaybackManager.prototype.getCurrentPlayer = function() {
        return this._currentPlayer
    }, PlaybackManager.prototype.currentTime = function(player) {
        return player = player || this._currentPlayer, !player || enableLocalPlaylistManagement(player) || player.isLocalPlayer ? this.getCurrentTicks(player) : player.currentTime()
    }, PlaybackManager.prototype.nextItem = function(player) {
        if ((player = player || this._currentPlayer) && !enableLocalPlaylistManagement(player)) return player.nextItem();
        var nextItem = this._playQueueManager.getNextItemInfo();
        if (!nextItem || !nextItem.item) return Promise.reject();
        var apiClient = connectionManager.getApiClient(nextItem.item.ServerId);
        return apiClient.getItem(apiClient.getCurrentUserId(), nextItem.item.Id)
    }, PlaybackManager.prototype.canQueue = function(item) {
        return "MusicAlbum" === item.Type || "MusicArtist" === item.Type || "MusicGenre" === item.Type ? this.canQueueMediaType("Audio") : this.canQueueMediaType(item.MediaType)
    }, PlaybackManager.prototype.canQueueMediaType = function(mediaType) {
        return !!this._currentPlayer && this._currentPlayer.canPlayMediaType(mediaType)
    }, PlaybackManager.prototype.isMuted = function(player) {
        return !!(player = player || this._currentPlayer) && player.isMuted()
    }, PlaybackManager.prototype.setMute = function(mute, player) {
        (player = player || this._currentPlayer) && player.setMute(mute)
    }, PlaybackManager.prototype.toggleMute = function(mute, player) {
        (player = player || this._currentPlayer) && (player.toggleMute ? player.toggleMute() : player.setMute(!player.isMuted()))
    }, PlaybackManager.prototype.toggleDisplayMirroring = function() {
        this.enableDisplayMirroring(!this.enableDisplayMirroring())
    }, PlaybackManager.prototype.enableDisplayMirroring = function(enabled) {
        if (null != enabled) {
            var val = enabled ? "1" : "0";
            return void appSettings.set("displaymirror", val)
        }
        return "0" !== (appSettings.get("displaymirror") || "")
    }, PlaybackManager.prototype.nextChapter = function(player) {
        player = player || this._currentPlayer;
        var item = this.currentItem(player),
            ticks = this.getCurrentTicks(player),
            nextChapter = (item.Chapters || []).filter(function(i) {
                return i.StartPositionTicks > ticks
            })[0];
        nextChapter ? this.seek(nextChapter.StartPositionTicks, player) : this.nextTrack(player)
    }, PlaybackManager.prototype.previousChapter = function(player) {
        player = player || this._currentPlayer;
        var item = this.currentItem(player),
            ticks = this.getCurrentTicks(player);
        ticks -= 1e8, 0 === this.getCurrentPlaylistIndex(player) && (ticks = Math.max(ticks, 0));
        var previousChapters = (item.Chapters || []).filter(function(i) {
            return i.StartPositionTicks <= ticks
        });
        previousChapters.length ? this.seek(previousChapters[previousChapters.length - 1].StartPositionTicks, player) : this.previousTrack(player)
    }, PlaybackManager.prototype.fastForward = function(player) {
        if (player = player || this._currentPlayer, null != player.fastForward) return void player.fastForward(userSettings.skipForwardLength());
        var offsetTicks = 1e4 * userSettings.skipForwardLength();
        this.seekRelative(offsetTicks, player)
    }, PlaybackManager.prototype.rewind = function(player) {
        if (player = player || this._currentPlayer, null != player.rewind) return void player.rewind(userSettings.skipBackLength());
        var offsetTicks = 0 - 1e4 * userSettings.skipBackLength();
        this.seekRelative(offsetTicks, player)
    }, PlaybackManager.prototype.seekPercent = function(percent, player) {
        player = player || this._currentPlayer;
        var ticks = this.duration(player) || 0;
        percent /= 100, ticks *= percent, this.seek(parseInt(ticks), player)
    }, PlaybackManager.prototype.playTrailers = function(item) {
        var player = this._currentPlayer;
        if (player && player.playTrailers) return player.playTrailers(item);
        var apiClient = connectionManager.getApiClient(item.ServerId),
            instance = this;
        if (item.LocalTrailerCount) return apiClient.getLocalTrailers(apiClient.getCurrentUserId(), item.Id).then(function(result) {
            return instance.play({
                items: result
            })
        });
        var remoteTrailers = item.RemoteTrailers || [];
        return remoteTrailers.length ? this.play({
            items: remoteTrailers.map(function(t) {
                return {
                    Name: t.Name || item.Name + " Trailer",
                    Url: t.Url,
                    MediaType: "Video",
                    Type: "Trailer",
                    ServerId: apiClient.serverId()
                }
            })
        }) : Promise.reject()
    }, PlaybackManager.prototype.getSubtitleUrl = function(textStream, serverId) {
        var apiClient = connectionManager.getApiClient(serverId);
        return textStream.IsExternalUrl ? textStream.DeliveryUrl : apiClient.getUrl(textStream.DeliveryUrl)
    }, PlaybackManager.prototype.stop = function(player) {
        return player = player || this._currentPlayer, player ? (enableLocalPlaylistManagement(player) && (this._playNextAfterEnded = !1), player.stop(!0, !0)) : Promise.resolve()
    }, PlaybackManager.prototype.getBufferedRanges = function(player) {
        return player = player || this._currentPlayer, player && player.getBufferedRanges ? player.getBufferedRanges() : []
    }, PlaybackManager.prototype.playPause = function(player) {
        if (player = player || this._currentPlayer) return player.playPause ? player.playPause() : player.paused() ? this.unpause(player) : this.pause(player)
    }, PlaybackManager.prototype.paused = function(player) {
        if (player = player || this._currentPlayer) return player.paused()
    }, PlaybackManager.prototype.pause = function(player) {
        (player = player || this._currentPlayer) && player.pause()
    }, PlaybackManager.prototype.unpause = function(player) {
        (player = player || this._currentPlayer) && player.unpause()
    }, PlaybackManager.prototype.instantMix = function(item, player) {
        if ((player = player || this._currentPlayer) && player.instantMix) return player.instantMix(item);
        var apiClient = connectionManager.getApiClient(item.ServerId),
            options = {};
        options.UserId = apiClient.getCurrentUserId(), options.Limit = 200;
        var instance = this;
        apiClient.getInstantMixFromItem(item.Id, options).then(function(result) {
            instance.play({
                items: result.Items
            })
        })
    }, PlaybackManager.prototype.shuffle = function(shuffleItem, player, queryOptions) {
        return player = player || this._currentPlayer, player && player.shuffle ? player.shuffle(shuffleItem) : this.play({
            items: [shuffleItem],
            shuffle: !0
        })
    }, PlaybackManager.prototype.audioTracks = function(player) {
        if (player = player || this._currentPlayer, player.audioTracks) {
            var result = player.audioTracks();
            if (result) return result
        }
        return ((this.currentMediaSource(player) || {}).MediaStreams || []).filter(function(s) {
            return "Audio" === s.Type
        })
    }, PlaybackManager.prototype.subtitleTracks = function(player) {
        if (player = player || this._currentPlayer, player.subtitleTracks) {
            var result = player.subtitleTracks();
            if (result) return result
        }
        return ((this.currentMediaSource(player) || {}).MediaStreams || []).filter(function(s) {
            return "Subtitle" === s.Type
        })
    }, PlaybackManager.prototype.getSupportedCommands = function(player) {
        if (player = player || this._currentPlayer || {
                isLocalPlayer: !0
            }, player.isLocalPlayer) {
            var list = ["GoHome", "GoToSettings", "VolumeUp", "VolumeDown", "Mute", "Unmute", "ToggleMute", "SetVolume", "SetAudioStreamIndex", "SetSubtitleStreamIndex", "SetMaxStreamingBitrate", "DisplayContent", "GoToSearch", "DisplayMessage", "SetRepeatMode", "PlayMediaSource", "PlayTrailers"];
            return apphost.supports("fullscreenchange") && list.push("ToggleFullscreen"), player.supports && (player.supports("PictureInPicture") && list.push("PictureInPicture"), player.supports("SetBrightness") && list.push("SetBrightness"), player.supports("SetAspectRatio") && list.push("SetAspectRatio")), list
        }
        var info = this.getPlayerInfo();
        return info ? info.supportedCommands : []
    }, PlaybackManager.prototype.setRepeatMode = function(value, player) {
        if ((player = player || this._currentPlayer) && !enableLocalPlaylistManagement(player)) return player.setRepeatMode(value);
        this._playQueueManager.setRepeatMode(value), events.trigger(player, "repeatmodechange")
    }, PlaybackManager.prototype.getRepeatMode = function(player) {
        return player = player || this._currentPlayer, player && !enableLocalPlaylistManagement(player) ? player.getRepeatMode() : this._playQueueManager.getRepeatMode()
    }, PlaybackManager.prototype.trySetActiveDeviceName = function(name) {
        name = normalizeName(name);
        var instance = this;
        instance.getTargets().then(function(result) {
            var target = result.filter(function(p) {
                return normalizeName(p.name) === name
            })[0];
            target && instance.trySetActivePlayer(target.playerName, target)
        })
    }, PlaybackManager.prototype.displayContent = function(options, player) {
        (player = player || this._currentPlayer) && player.displayContent && player.displayContent(options)
    }, PlaybackManager.prototype.beginPlayerUpdates = function(player) {
        player.beginPlayerUpdates && player.beginPlayerUpdates()
    }, PlaybackManager.prototype.endPlayerUpdates = function(player) {
        player.endPlayerUpdates && player.endPlayerUpdates()
    }, PlaybackManager.prototype.setDefaultPlayerActive = function() {
        this.setActivePlayer("localplayer")
    }, PlaybackManager.prototype.removeActivePlayer = function(name) {
        var playerInfo = this.getPlayerInfo();
        playerInfo && playerInfo.name === name && this.setDefaultPlayerActive()
    }, PlaybackManager.prototype.removeActiveTarget = function(id) {
        var playerInfo = this.getPlayerInfo();
        playerInfo && playerInfo.id === id && this.setDefaultPlayerActive()
    }, PlaybackManager.prototype.sendCommand = function(cmd, player) {
        switch (console.log("MediaController received command: " + cmd.Name), cmd.Name) {
            case "SetRepeatMode":
                this.setRepeatMode(cmd.Arguments.RepeatMode, player);
                break;
            case "VolumeUp":
                this.volumeUp(player);
                break;
            case "VolumeDown":
                this.volumeDown(player);
                break;
            case "Mute":
                this.setMute(!0, player);
                break;
            case "Unmute":
                this.setMute(!1, player);
                break;
            case "ToggleMute":
                this.toggleMute(player);
                break;
            case "SetVolume":
                this.setVolume(cmd.Arguments.Volume, player);
                break;
            case "SetAspectRatio":
                this.setAspectRatio(cmd.Arguments.AspectRatio, player);
                break;
            case "SetBrightness":
                this.setBrightness(cmd.Arguments.Brightness, player);
                break;
            case "SetAudioStreamIndex":
                this.setAudioStreamIndex(parseInt(cmd.Arguments.Index), player);
                break;
            case "SetSubtitleStreamIndex":
                this.setSubtitleStreamIndex(parseInt(cmd.Arguments.Index), player);
                break;
            case "SetMaxStreamingBitrate":
                break;
            case "ToggleFullscreen":
                this.toggleFullscreen(player);
                break;
            default:
                player.sendCommand && player.sendCommand(cmd)
        }
    }, new PlaybackManager
});