define(["filerepository", "itemrepository", "useractionrepository", "transfermanager"], function(filerepository, itemrepository, useractionrepository, transfermanager) {
    "use strict";

    function getLocalItem(serverId, itemId) {
        return console.log("[lcoalassetmanager] Begin getLocalItem"), itemrepository.get(serverId, itemId)
    }

    function recordUserAction(action) {
        return action.Id = createGuid(), useractionrepository.set(action.Id, action)
    }

    function getUserActions(serverId) {
        return useractionrepository.getByServerId(serverId)
    }

    function deleteUserAction(action) {
        return useractionrepository.remove(action.Id)
    }

    function deleteUserActions(actions) {
        var results = [];
        return actions.forEach(function(action) {
            results.push(deleteUserAction(action))
        }), Promise.all(results)
    }

    function getServerItems(serverId) {
        return console.log("[localassetmanager] Begin getServerItems"), itemrepository.getAll(serverId)
    }

    function getItemsFromIds(serverId, ids) {
        var actions = ids.map(function(id) {
            var strippedId = stripStart(id, "local:");
            return getLocalItem(serverId, strippedId)
        });
        return Promise.all(actions).then(function(items) {
            var libItems = items.map(function(locItem) {
                return locItem.Item
            });
            return Promise.resolve(libItems)
        })
    }

    function getViews(serverId, userId) {
        return itemrepository.getServerItemTypes(serverId, userId).then(function(types) {
            var item, list = [];
            return types.indexOf("Audio") > -1 && (item = {
                Name: "Music",
                ServerId: serverId,
                Id: "localview:MusicView",
                Type: "MusicView",
                CollectionType: "music",
                IsFolder: !0
            }, list.push(item)), types.indexOf("Photo") > -1 && (item = {
                Name: "Photos",
                ServerId: serverId,
                Id: "localview:PhotosView",
                Type: "PhotosView",
                CollectionType: "photos",
                IsFolder: !0
            }, list.push(item)), types.indexOf("Episode") > -1 && (item = {
                Name: "TV",
                ServerId: serverId,
                Id: "localview:TVView",
                Type: "TVView",
                CollectionType: "tvshows",
                IsFolder: !0
            }, list.push(item)), types.indexOf("Movie") > -1 && (item = {
                Name: "Movies",
                ServerId: serverId,
                Id: "localview:MoviesView",
                Type: "MoviesView",
                CollectionType: "movies",
                IsFolder: !0
            }, list.push(item)), types.indexOf("Video") > -1 && (item = {
                Name: "Videos",
                ServerId: serverId,
                Id: "localview:VideosView",
                Type: "VideosView",
                CollectionType: "videos",
                IsFolder: !0
            }, list.push(item)), types.indexOf("MusicVideo") > -1 && (item = {
                Name: "Music Videos",
                ServerId: serverId,
                Id: "localview:MusicVideosView",
                Type: "MusicVideosView",
                CollectionType: "videos",
                IsFolder: !0
            }, list.push(item)), Promise.resolve(list)
        })
    }

    function updateFiltersForTopLevelView(parentId, mediaTypes, includeItemTypes, query) {
        switch (parentId) {
            case "MusicView":
                return query.Recursive ? includeItemTypes.push("Audio") : includeItemTypes.push("MusicAlbum"), !0;
            case "PhotosView":
                return query.Recursive ? includeItemTypes.push("Photo") : includeItemTypes.push("PhotoAlbum"), !0;
            case "TVView":
                return query.Recursive ? includeItemTypes.push("Episode") : includeItemTypes.push("Series"), !0;
            case "VideosView":
                return query.Recursive, includeItemTypes.push("Video"), !0;
            case "MoviesView":
                return query.Recursive, includeItemTypes.push("Movie"), !0;
            case "MusicVideosView":
                return query.Recursive, includeItemTypes.push("MusicVideo"), !0
        }
        return !1
    }

    function normalizeId(id) {
        return id ? (id = stripStart(id, "localview:"), id = stripStart(id, "local:")) : null
    }

    function normalizeIdList(val) {
        return val ? val.split(",").map(normalizeId) : []
    }

    function shuffle(array) {
        for (var temporaryValue, randomIndex, currentIndex = array.length; 0 !== currentIndex;) randomIndex = Math.floor(Math.random() * currentIndex), currentIndex -= 1, temporaryValue = array[currentIndex], array[currentIndex] = array[randomIndex], array[randomIndex] = temporaryValue;
        return array
    }

    function sortItems(items, query) {
        if (!query.SortBy || 0 === query.SortBy.length) return items;
        if ("Random" === query.SortBy) return shuffle(items);
        var sortSpec = getSortSpec(query);
        return items.sort(function(a, b) {
            for (var i = 0; i < sortSpec.length; i++) {
                var result = compareValues(a, b, sortSpec[i].Field, sortSpec[i].OrderDescending);
                if (0 !== result) return result
            }
            return 0
        }), items
    }

    function compareValues(a, b, field, orderDesc) {
        if (!a.hasOwnProperty(field) || !b.hasOwnProperty(field)) return 0;
        var valA = a[field],
            valB = b[field],
            result = 0;
        return "string" == typeof valA || "string" == typeof valB ? (valA = valA || "", valB = valB || "", result = valA.toLowerCase().localeCompare(valB.toLowerCase())) : valA > valB ? result = 1 : valA < valB && (result = -1), orderDesc && (result *= -1), result
    }

    function getSortSpec(query) {
        for (var sortFields = (query.SortBy || "").split(","), sortOrders = (query.SortOrder || "").split(","), sortSpec = [], i = 0; i < sortFields.length; i++) {
            var orderDesc = !1;
            i < sortOrders.length && -1 !== sortOrders[i].toLowerCase().indexOf("desc") && (orderDesc = !0), sortSpec.push({
                Field: sortFields[i],
                OrderDescending: orderDesc
            })
        }
        return sortSpec
    }

    function getViewItems(serverId, userId, options) {
        var searchParentId = options.ParentId;
        searchParentId = normalizeId(searchParentId);
        var seasonId = normalizeId(options.SeasonId || options.seasonId),
            seriesId = normalizeId(options.SeriesId || options.seriesId),
            albumIds = normalizeIdList(options.AlbumIds || options.albumIds),
            includeItemTypes = options.IncludeItemTypes ? options.IncludeItemTypes.split(",") : [],
            filters = options.Filters ? options.Filters.split(",") : [],
            mediaTypes = options.MediaTypes ? options.MediaTypes.split(",") : [];
        return updateFiltersForTopLevelView(searchParentId, mediaTypes, includeItemTypes, options) && (searchParentId = null), getServerItems(serverId).then(function(items) {
            var itemsMap = new Map,
                subtreeIdSet = new Set;
            if (items.forEach(function(item) {
                    item.Item.LocalChildren = [], itemsMap.set(item.Item.Id, item.Item)
                }), itemsMap.forEach(function(item, ignored, ignored2) {
                    if (item.ParentId && itemsMap.has(item.ParentId)) {
                        itemsMap.get(item.ParentId).LocalChildren.push(item)
                    }
                }), options.Recursive && searchParentId && itemsMap.has(searchParentId)) {
                var addSubtreeIds = function(recurseItem) {
                        subtreeIdSet.has(recurseItem.Id) || subtreeIdSet.add(recurseItem.Id), recurseItem.LocalChildren.forEach(function(childItem) {
                            addSubtreeIds(childItem)
                        })
                    },
                    searchParentItem = itemsMap.get(searchParentId);
                addSubtreeIds(searchParentItem)
            }
            var resultItems = items.filter(function(item) {
                return (!item.SyncStatus || "synced" === item.SyncStatus) && ((!mediaTypes.length || -1 !== mediaTypes.indexOf(item.Item.MediaType || "")) && ((!seriesId || item.Item.SeriesId === seriesId) && ((!seasonId || item.Item.SeasonId === seasonId) && ((!albumIds.length || -1 !== albumIds.indexOf(item.Item.AlbumId || "")) && ((!item.Item.IsFolder || -1 === filters.indexOf("IsNotFolder")) && (!(!item.Item.IsFolder && -1 !== filters.indexOf("IsFolder")) && ((!includeItemTypes.length || -1 !== includeItemTypes.indexOf(item.Item.Type || "")) && (!searchParentId || (options.Recursive ? subtreeIdSet.has(item.Item.Id) : item.Item.ParentId === searchParentId)))))))))
            }).map(function(item2) {
                return item2.Item
            });
            return resultItems = sortItems(resultItems, options), options.Limit && (resultItems = resultItems.slice(0, options.Limit)), Promise.resolve(resultItems)
        })
    }

    function removeObsoleteContainerItems(serverId) {
        return getServerItems(serverId).then(function(items) {
            var seriesItems = items.filter(function(item) {
                    return "series" === (item.Item.Type || "").toLowerCase()
                }),
                seasonItems = items.filter(function(item) {
                    return "season" === (item.Item.Type || "").toLowerCase()
                }),
                albumItems = items.filter(function(item) {
                    var type = (item.Item.Type || "").toLowerCase();
                    return "musicalbum" === type || "photoalbum" === type
                }),
                requiredSeriesIds = items.filter(function(item) {
                    return "episode" === (item.Item.Type || "").toLowerCase()
                }).map(function(item2) {
                    return item2.Item.SeriesId
                }).filter(filterDistinct),
                requiredSeasonIds = items.filter(function(item) {
                    return "episode" === (item.Item.Type || "").toLowerCase()
                }).map(function(item2) {
                    return item2.Item.SeasonId
                }).filter(filterDistinct),
                requiredAlbumIds = items.filter(function(item) {
                    var type = (item.Item.Type || "").toLowerCase();
                    return "audio" === type || "photo" === type
                }).map(function(item2) {
                    return item2.Item.AlbumId
                }).filter(filterDistinct),
                obsoleteItems = [];
            seriesItems.forEach(function(item) {
                requiredSeriesIds.indexOf(item.Item.Id) < 0 && obsoleteItems.push(item)
            }), seasonItems.forEach(function(item) {
                requiredSeasonIds.indexOf(item.Item.Id) < 0 && obsoleteItems.push(item)
            }), albumItems.forEach(function(item) {
                requiredAlbumIds.indexOf(item.Item.Id) < 0 && obsoleteItems.push(item)
            });
            var p = Promise.resolve();
            return obsoleteItems.forEach(function(item) {
                p = p.then(function() {
                    return itemrepository.remove(item.ServerId, item.Id)
                })
            }), p
        })
    }

    function removeLocalItem(localItem) {
        return itemrepository.get(localItem.ServerId, localItem.Id).then(function(item) {
            var onFileDeletedSuccessOrFail = function() {
                    return itemrepository.remove(localItem.ServerId, localItem.Id)
                },
                p = Promise.resolve();
            return item.LocalPath && (p = p.then(function() {
                return filerepository.deleteFile(item.LocalPath)
            })), item && item.Item && item.Item.MediaSources && item.Item.MediaSources.forEach(function(mediaSource) {
                mediaSource.MediaStreams && mediaSource.MediaStreams.length > 0 && mediaSource.MediaStreams.forEach(function(mediaStream) {
                    mediaStream.Path && (p = p.then(function() {
                        return filerepository.deleteFile(mediaStream.Path)
                    }))
                })
            }), p.then(onFileDeletedSuccessOrFail, onFileDeletedSuccessOrFail)
        }, function(item) {
            return Promise.resolve()
        })
    }

    function addOrUpdateLocalItem(localItem) {
        return itemrepository.set(localItem.ServerId, localItem.Id, localItem)
    }

    function getSubtitleSaveFileName(localItem, mediaPath, language, isForced, format) {
        var name = getNameWithoutExtension(mediaPath);
        name = filerepository.getValidFileName(name), language && (name += "." + language.toLowerCase()), isForced && (name += ".foreign"), name = name + "." + format.toLowerCase();
        var mediaFolder = filerepository.getParentPath(localItem.LocalPath);
        return filerepository.combinePath(mediaFolder, name)
    }

    function getItemFileSize(path) {
        return filerepository.getItemFileSize(path)
    }

    function getNameWithoutExtension(path) {
        var fileName = path,
            pos = fileName.lastIndexOf(".");
        return pos > 0 && (fileName = fileName.substring(0, pos)), fileName
    }

    function downloadFile(url, localItem) {
        var imageUrl = getImageUrl(localItem.Item.ServerId, localItem.Item.Id, {
            type: "Primary",
            index: 0
        });
        return transfermanager.downloadFile(url, localItem, imageUrl)
    }

    function downloadSubtitles(url, fileName) {
        return transfermanager.downloadSubtitles(url, fileName)
    }

    function getImageUrl(serverId, itemId, imageOptions) {
        var imageType = imageOptions.type,
            index = imageOptions.index,
            pathArray = getImagePath(serverId, itemId, imageType, index);
        return filerepository.getImageUrl(pathArray)
    }

    function hasImage(serverId, itemId, imageType, index) {
        var pathArray = getImagePath(serverId, itemId, imageType, index),
            localFilePath = filerepository.getFullMetadataPath(pathArray);
        return filerepository.fileExists(localFilePath).then(function(exists) {
            return Promise.resolve(exists)
        }, function(err) {
            return Promise.resolve(!1)
        })
    }

    function fileExists(localFilePath) {
        return filerepository.fileExists(localFilePath)
    }

    function downloadImage(localItem, url, serverId, itemId, imageType, index) {
        var localPathParts = getImagePath(serverId, itemId, imageType, index);
        return transfermanager.downloadImage(url, localPathParts)
    }

    function isDownloadFileInQueue(path) {
        return transfermanager.isDownloadFileInQueue(path)
    }

    function getDownloadItemCount() {
        return transfermanager.getDownloadItemCount()
    }

    function getDirectoryPath(item) {
        var parts = [],
            itemtype = item.Type.toLowerCase(),
            mediaType = (item.MediaType || "").toLowerCase();
        "episode" === itemtype || "series" === itemtype || "season" === itemtype ? parts.push("TV") : "video" === mediaType ? parts.push("Videos") : "audio" === itemtype || "musicalbum" === itemtype || "musicartist" === itemtype ? parts.push("Music") : "photo" === itemtype || "photoalbum" === itemtype ? parts.push("Photos") : "game" !== itemtype && "gamesystem" !== itemtype || parts.push("Games");
        var albumArtist = item.AlbumArtist;
        albumArtist && parts.push(albumArtist);
        var seriesName = item.SeriesName;
        seriesName && parts.push(seriesName);
        var seasonName = item.SeasonName;
        seasonName && parts.push(seasonName), item.Album && parts.push(item.Album), ("video" === mediaType && "episode" !== itemtype || "game" === itemtype || item.IsFolder) && parts.push(item.Name);
        for (var finalParts = [], i = 0; i < parts.length; i++) finalParts.push(filerepository.getValidFileName(parts[i]));
        return finalParts
    }

    function getImagePath(serverId, itemId, imageType, index) {
        var parts = [];
        parts.push("images"), index = index || 0, parts.push(itemId + "_" + imageType + "_" + index.toString());
        for (var finalParts = [], i = 0; i < parts.length; i++) finalParts.push(parts[i]);
        return finalParts
    }

    function getLocalFileName(item, originalFileName) {
        var filename = originalFileName || item.Name;
        return filerepository.getValidFileName(filename)
    }

    function resyncTransfers() {
        return transfermanager.resyncTransfers()
    }

    function createGuid() {
        var d = (new Date).getTime();
        return window.performance && "function" == typeof window.performance.now && (d += performance.now()), "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function(c) {
            var r = (d + 16 * Math.random()) % 16 | 0;
            return d = Math.floor(d / 16), ("x" === c ? r : 3 & r | 8).toString(16)
        })
    }

    function startsWith(str, find) {
        return !!(str && find && str.length > find.length && 0 === str.indexOf(find))
    }

    function stripStart(str, find) {
        return startsWith(str, find) ? str.substr(find.length) : str
    }

    function filterDistinct(value, index, self) {
        return self.indexOf(value) === index
    }

    function enableBackgroundCompletion() {
        return transfermanager.enableBackgroundCompletion
    }
    return {
        getLocalItem: getLocalItem,
        getDirectoryPath: getDirectoryPath,
        getLocalFileName: getLocalFileName,
        recordUserAction: recordUserAction,
        getUserActions: getUserActions,
        deleteUserAction: deleteUserAction,
        deleteUserActions: deleteUserActions,
        removeLocalItem: removeLocalItem,
        addOrUpdateLocalItem: addOrUpdateLocalItem,
        downloadFile: downloadFile,
        downloadSubtitles: downloadSubtitles,
        hasImage: hasImage,
        downloadImage: downloadImage,
        getImageUrl: getImageUrl,
        getSubtitleSaveFileName: getSubtitleSaveFileName,
        getServerItems: getServerItems,
        getItemFileSize: getItemFileSize,
        isDownloadFileInQueue: isDownloadFileInQueue,
        getDownloadItemCount: getDownloadItemCount,
        getViews: getViews,
        getViewItems: getViewItems,
        resyncTransfers: resyncTransfers,
        getItemsFromIds: getItemsFromIds,
        removeObsoleteContainerItems: removeObsoleteContainerItems,
        fileExists: fileExists,
        enableBackgroundCompletion: enableBackgroundCompletion
    }
});