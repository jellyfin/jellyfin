define(["apphost", "globalize", "connectionManager", "itemHelper", "appRouter", "playbackManager", "loading", "appSettings", "browser", "actionsheet"], function(appHost, globalize, connectionManager, itemHelper, appRouter, playbackManager, loading, appSettings, browser, actionsheet) {
    "use strict";

    function getCommands(options) {
        var item = options.item,
            canPlay = playbackManager.canPlay(item),
            commands = [],
            user = options.user,
            restrictOptions = (browser.operaTv || browser.web0s) && !user.Policy.IsAdministrator;
        canPlay && "Photo" !== item.MediaType && (!1 !== options.play && commands.push({
            name: globalize.translate("sharedcomponents#Play"),
            id: "resume"
        }), options.playAllFromHere && "Program" !== item.Type && "TvChannel" !== item.Type && commands.push({
            name: globalize.translate("sharedcomponents#PlayAllFromHere"),
            id: "playallfromhere"
        })), playbackManager.canQueue(item) && (!1 !== options.queue && commands.push({
            name: globalize.translate("sharedcomponents#AddToPlayQueue"),
            id: "queue"
        }), !1 !== options.queue && commands.push({
            name: globalize.translate("sharedcomponents#PlayNext"),
            id: "queuenext"
        })), (item.IsFolder || "MusicArtist" === item.Type || "MusicGenre" === item.Type) && "livetv" !== item.CollectionType && !1 !== options.shuffle && commands.push({
            name: globalize.translate("sharedcomponents#Shuffle"),
            id: "shuffle"
        }), "Audio" !== item.MediaType && "MusicAlbum" !== item.Type && "MusicArtist" !== item.Type && "MusicGenre" !== item.Type || !1 === options.instantMix || itemHelper.isLocalItem(item) || commands.push({
            name: globalize.translate("sharedcomponents#InstantMix"),
            id: "instantmix"
        }), commands.length && commands.push({
            divider: !0
        }), restrictOptions || (itemHelper.supportsAddingToCollection(item) && commands.push({
            name: globalize.translate("sharedcomponents#AddToCollection"),
            id: "addtocollection"
        }), itemHelper.supportsAddingToPlaylist(item) && commands.push({
            name: globalize.translate("sharedcomponents#AddToPlaylist"),
            id: "addtoplaylist"
        })), "Timer" === item.Type && user.Policy.EnableLiveTvManagement && !1 !== options.cancelTimer && commands.push({
            name: globalize.translate("sharedcomponents#CancelRecording"),
            id: "canceltimer"
        }), "Recording" === item.Type && "InProgress" === item.Status && user.Policy.EnableLiveTvManagement && !1 !== options.cancelTimer && commands.push({
            name: globalize.translate("sharedcomponents#CancelRecording"),
            id: "canceltimer"
        }), "SeriesTimer" === item.Type && user.Policy.EnableLiveTvManagement && !1 !== options.cancelTimer && commands.push({
            name: globalize.translate("sharedcomponents#CancelSeries"),
            id: "cancelseriestimer"
        }), itemHelper.canConvert(item, user, connectionManager.getApiClient(item)) && commands.push({
            name: globalize.translate("sharedcomponents#Convert"),
            id: "convert"
        }), item.CanDelete && !1 !== options.deleteItem && ("Playlist" === item.Type || "BoxSet" === item.Type ? commands.push({
            name: globalize.translate("sharedcomponents#Delete"),
            id: "delete"
        }) : commands.push({
            name: globalize.translate("sharedcomponents#DeleteMedia"),
            id: "delete"
        })), item.CanDownload && appHost.supports("filedownload") && commands.push({
            name: globalize.translate("sharedcomponents#Download"),
            id: "download"
        }), appHost.supports("sync") && !1 !== options.syncLocal && itemHelper.canSync(user, item) && commands.push({
            name: globalize.translate("sharedcomponents#Download"),
            id: "synclocal"
        });
        var canEdit = itemHelper.canEdit(user, item);
        if (canEdit && !1 !== options.edit && "SeriesTimer" !== item.Type) {
            var text = "Timer" === item.Type || "SeriesTimer" === item.Type ? globalize.translate("sharedcomponents#Edit") : globalize.translate("sharedcomponents#EditMetadata");
            commands.push({
                name: text,
                id: "edit"
            })
        }
        return itemHelper.canEditImages(user, item) && !1 !== options.editImages && commands.push({
            name: globalize.translate("sharedcomponents#EditImages"),
            id: "editimages"
        }), canEdit && ("Video" !== item.MediaType || "TvChannel" === item.Type || "Program" === item.Type || "Virtual" === item.LocationType || "Recording" === item.Type && "Completed" !== item.Status || !1 !== options.editSubtitles && commands.push({
            name: globalize.translate("sharedcomponents#EditSubtitles"),
            id: "editsubtitles"
        })), !1 !== options.identify && itemHelper.canIdentify(user, item) && commands.push({
            name: globalize.translate("sharedcomponents#Identify"),
            id: "identify"
        }), "Program" === item.Type && !1 !== options.record && item.TimerId && commands.push({
            name: Globalize.translate("sharedcomponents#ManageRecording"),
            id: "record"
        }), "Program" === item.Type && !1 !== options.record && (item.TimerId || commands.push({
            name: Globalize.translate("sharedcomponents#Record"),
            id: "record"
        })), itemHelper.canRefreshMetadata(item, user) && commands.push({
            name: globalize.translate("sharedcomponents#RefreshMetadata"),
            id: "refresh"
        }), item.PlaylistItemId && options.playlistId && commands.push({
            name: globalize.translate("sharedcomponents#RemoveFromPlaylist"),
            id: "removefromplaylist"
        }), options.collectionId && commands.push({
            name: globalize.translate("sharedcomponents#RemoveFromCollection"),
            id: "removefromcollection"
        }), restrictOptions || !0 === options.share && itemHelper.canShare(item, user) && commands.push({
            name: globalize.translate("sharedcomponents#Share"),
            id: "share"
        }), !1 !== options.sync && itemHelper.canSync(user, item) && commands.push({
            name: globalize.translate("sharedcomponents#Sync"),
            id: "sync"
        }), !1 !== options.openAlbum && item.AlbumId && "Photo" !== item.MediaType && commands.push({
            name: Globalize.translate("sharedcomponents#ViewAlbum"),
            id: "album"
        }), !1 !== options.openArtist && item.ArtistItems && item.ArtistItems.length && commands.push({
            name: Globalize.translate("sharedcomponents#ViewArtist"),
            id: "artist"
        }), commands
    }

    function getResolveFunction(resolve, id, changed, deleted) {
        return function() {
            resolve({
                command: id,
                updated: changed,
                deleted: deleted
            })
        }
    }

    function executeCommand(item, id, options) {
        var itemId = item.Id,
            serverId = item.ServerId,
            apiClient = connectionManager.getApiClient(serverId);
        return new Promise(function(resolve, reject) {
            switch (id) {
                case "addtocollection":
                    require(["collectionEditor"], function(collectionEditor) {
                        (new collectionEditor).show({
                            items: [itemId],
                            serverId: serverId
                        }).then(getResolveFunction(resolve, id, !0), getResolveFunction(resolve, id))
                    });
                    break;
                case "addtoplaylist":
                    require(["playlistEditor"], function(playlistEditor) {
                        (new playlistEditor).show({
                            items: [itemId],
                            serverId: serverId
                        }).then(getResolveFunction(resolve, id, !0), getResolveFunction(resolve, id))
                    });
                    break;
                case "download":
                    require(["fileDownloader"], function(fileDownloader) {
                        var downloadHref = apiClient.getItemDownloadUrl(itemId);
                        fileDownloader.download([{
                            url: downloadHref,
                            itemId: itemId,
                            serverId: serverId
                        }]), getResolveFunction(getResolveFunction(resolve, id), id)()
                    });
                    break;
                case "editsubtitles":
                    require(["subtitleEditor"], function(subtitleEditor) {
                        subtitleEditor.show(itemId, serverId).then(getResolveFunction(resolve, id, !0), getResolveFunction(resolve, id))
                    });
                    break;
                case "edit":
                    editItem(apiClient, item).then(getResolveFunction(resolve, id, !0), getResolveFunction(resolve, id));
                    break;
                case "editimages":
                    require(["imageEditor"], function(imageEditor) {
                        imageEditor.show({
                            itemId: itemId,
                            serverId: serverId
                        }).then(getResolveFunction(resolve, id, !0), getResolveFunction(resolve, id))
                    });
                    break;
                case "identify":
                    require(["itemIdentifier"], function(itemIdentifier) {
                        itemIdentifier.show(itemId, serverId).then(getResolveFunction(resolve, id, !0), getResolveFunction(resolve, id))
                    });
                    break;
                case "refresh":
                    refresh(apiClient, item), getResolveFunction(resolve, id)();
                    break;
                case "open":
                    appRouter.showItem(item), getResolveFunction(resolve, id)();
                    break;
                case "play":
                    play(item, !1), getResolveFunction(resolve, id)();
                    break;
                case "resume":
                    play(item, !0), getResolveFunction(resolve, id)();
                    break;
                case "queue":
                    play(item, !1, !0), getResolveFunction(resolve, id)();
                    break;
                case "queuenext":
                    play(item, !1, !0, !0), getResolveFunction(resolve, id)();
                    break;
                case "record":
                    require(["recordingCreator"], function(recordingCreator) {
                        recordingCreator.show(itemId, serverId).then(getResolveFunction(resolve, id, !0), getResolveFunction(resolve, id))
                    });
                    break;
                case "shuffle":
                    playbackManager.shuffle(item), getResolveFunction(resolve, id)();
                    break;
                case "instantmix":
                    playbackManager.instantMix(item), getResolveFunction(resolve, id)();
                    break;
                case "delete":
                    deleteItem(apiClient, item).then(getResolveFunction(resolve, id, !0, !0), getResolveFunction(resolve, id));
                    break;
                case "share":
                    navigator.share({
                        title: item.Name,
                        text: item.Overview,
                        url: "https://github.com/jellyfin/jellyfin"
                    });
                    break;
                case "album":
                    appRouter.showItem(item.AlbumId, item.ServerId), getResolveFunction(resolve, id)();
                    break;
                case "artist":
                    appRouter.showItem(item.ArtistItems[0].Id, item.ServerId), getResolveFunction(resolve, id)();
                    break;
                case "playallfromhere":
                case "queueallfromhere":
                    getResolveFunction(resolve, id)();
                    break;
                case "convert":
                    require(["syncDialog"], function(syncDialog) {
                        syncDialog.showMenu({
                            items: [item],
                            serverId: serverId,
                            mode: "convert"
                        })
                    }), getResolveFunction(resolve, id)();
                    break;
                case "sync":
                    require(["syncDialog"], function(syncDialog) {
                        syncDialog.showMenu({
                            items: [item],
                            serverId: serverId,
                            mode: "sync"
                        })
                    }), getResolveFunction(resolve, id)();
                    break;
                case "synclocal":
                    require(["syncDialog"], function(syncDialog) {
                        syncDialog.showMenu({
                            items: [item],
                            serverId: serverId,
                            mode: "download"
                        })
                    }), getResolveFunction(resolve, id)();
                    break;
                case "removefromplaylist":
                    apiClient.ajax({
                        url: apiClient.getUrl("Playlists/" + options.playlistId + "/Items", {
                            EntryIds: [item.PlaylistItemId].join(",")
                        }),
                        type: "DELETE"
                    }).then(function() {
                        getResolveFunction(resolve, id, !0)()
                    });
                    break;
                case "removefromcollection":
                    apiClient.ajax({
                        type: "DELETE",
                        url: apiClient.getUrl("Collections/" + options.collectionId + "/Items", {
                            Ids: [item.Id].join(",")
                        })
                    }).then(function() {
                        getResolveFunction(resolve, id, !0)()
                    });
                    break;
                case "canceltimer":
                    deleteTimer(apiClient, item, resolve, id);
                    break;
                case "cancelseriestimer":
                    deleteSeriesTimer(apiClient, item, resolve, id);
                    break;
                default:
                    reject()
            }
        })
    }

    function deleteTimer(apiClient, item, resolve, command) {
        require(["recordingHelper"], function(recordingHelper) {
            var timerId = item.TimerId || item.Id;
            recordingHelper.cancelTimerWithConfirmation(timerId, item.ServerId).then(function() {
                getResolveFunction(resolve, command, !0)()
            })
        })
    }

    function deleteSeriesTimer(apiClient, item, resolve, command) {
        require(["recordingHelper"], function(recordingHelper) {
            recordingHelper.cancelSeriesTimerWithConfirmation(item.Id, item.ServerId).then(function() {
                getResolveFunction(resolve, command, !0)()
            })
        })
    }

    function play(item, resume, queue, queueNext) {
        var method = queue ? queueNext ? "queueNext" : "queue" : "play",
            startPosition = 0;
        resume && item.UserData && item.UserData.PlaybackPositionTicks && (startPosition = item.UserData.PlaybackPositionTicks), "Program" === item.Type ? playbackManager[method]({
            ids: [item.ChannelId],
            startPositionTicks: startPosition,
            serverId: item.ServerId
        }) : playbackManager[method]({
            items: [item],
            startPositionTicks: startPosition
        })
    }

    function editItem(apiClient, item) {
        return new Promise(function(resolve, reject) {
            var serverId = apiClient.serverInfo().Id;
            "Timer" === item.Type ? require(["recordingEditor"], function(recordingEditor) {
                recordingEditor.show(item.Id, serverId).then(resolve, reject)
            }) : "SeriesTimer" === item.Type ? require(["seriesRecordingEditor"], function(recordingEditor) {
                recordingEditor.show(item.Id, serverId).then(resolve, reject)
            }) : require(["metadataEditor"], function(metadataEditor) {
                metadataEditor.show(item.Id, serverId).then(resolve, reject)
            })
        })
    }

    function deleteItem(apiClient, item) {
        return new Promise(function(resolve, reject) {
            require(["deleteHelper"], function(deleteHelper) {
                deleteHelper.deleteItem({
                    item: item,
                    navigate: !1
                }).then(function() {
                    resolve(!0)
                }, reject)
            })
        })
    }

    function refresh(apiClient, item) {
        require(["refreshDialog"], function(refreshDialog) {
            new refreshDialog({
                itemIds: [item.Id],
                serverId: apiClient.serverInfo().Id,
                mode: "CollectionFolder" === item.Type ? "scan" : null
            }).show()
        })
    }

    function show(options) {
        var commands = getCommands(options);
        return commands.length ? actionsheet.show({
            items: commands,
            positionTo: options.positionTo,
            resolveOnClick: ["share"]
        }).then(function(id) {
            return executeCommand(options.item, id, options)
        }) : Promise.reject()
    }
    return {
        getCommands: getCommands,
        show: show
    }
});