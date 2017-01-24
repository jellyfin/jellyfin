define(['apiclientcore', 'localassetmanager', 'events'], function (apiclientcorefactory, localassetmanager, events) {
    'use strict';

    var localPrefix = 'local:';
    var localViewPrefix = 'localview:';

    /**
     * Creates a new api client instance
     * @param {String} serverAddress
     * @param {String} clientName s
     * @param {String} applicationVersion 
     */
    return function (serverAddress, clientName, applicationVersion, deviceName, deviceId, devicePixelRatio) {

        var apiclientcore = new apiclientcorefactory(serverAddress, clientName, applicationVersion, deviceName, deviceId, devicePixelRatio);

        var self = Object.assign(this, apiclientcore);

        events.on(apiclientcore, 'websocketmessage', onWebSocketMessage);

        Object.defineProperty(self, 'onAuthenticated', {
            get: function () { return apiclientcore.onAuthenticated; },
            set: function (newValue) { apiclientcore.onAuthenticated = newValue; }
        });

        Object.defineProperty(self, 'enableAutomaticBitrateDetection', {
            get: function () { return apiclientcore.enableAutomaticBitrateDetection; },
            set: function (newValue) { apiclientcore.enableAutomaticBitrateDetection = newValue; }
        });

        function getUserViews(userId) {

            return apiclientcore.getUserViews(userId).then(function (result) {

                var serverInfo = apiclientcore.serverInfo();

                if (serverInfo) {

                    return getLocalView(serverInfo.Id, userId).then(function (localView) {

                        if (localView) {

                            result.Items.push(localView);
                            result.TotalRecordCount++;
                        }

                        return Promise.resolve(result);
                    });
                }

                return Promis.resolve(result);
            });
        }

        function getLocalView(serverId, userId) {

            return localassetmanager.getViews(serverId, userId).then(function (views) {

                var localView = null;

                if (views.length > 0) {

                    localView = {
                        Name: 'Offline Items',
                        ServerId: serverId,
                        Id: 'localview',
                        Type: 'localview'
                    };
                }

                return Promise.resolve(localView);
            });
        }

        function getItems(userId, options) {

            var serverInfo = apiclientcore.serverInfo();
            var i;

            if (serverInfo && options.ParentId === 'localview') {

                return localassetmanager.getViews(serverInfo.Id, userId).then(function (items) {
                    var result = {
                        Items: items,
                        TotalRecordCount: items.length
                    };

                    return Promise.resolve(result);
                });

            } else if (serverInfo && options && (startsWith(options.ParentId, localViewPrefix) || startsWith(options.ParentId, localPrefix))) {

                return localassetmanager.getViewItems(serverInfo.Id, userId, options.ParentId).then(function (items) {

                    items.forEach(function (item) {
                        item.Id = localPrefix + item.Id;
                    });

                    items.sort(function (a, b) { return a.SortName.toLowerCase().localeCompare(b.SortName.toLowerCase()); });

                    var result = {
                        Items: items,
                        TotalRecordCount: items.length
                    };

                    return Promise.resolve(result);
                });
            } else if (options && options.ExcludeItemIds && options.ExcludeItemIds.length) {

                var exItems = options.ExcludeItemIds.split(',');

                for (i = 0; i < exItems.length; i++) {
                    if (startsWith(exItems[i], localPrefix)) {
                        return Promise.resolve(createEmptyList());
                    }
                }
            } else if (options && options.Ids && options.Ids.length) {

                var ids = options.Ids.split(',');
                var hasLocal = false;

                for (i = 0; i < ids.length; i++) {
                    if (startsWith(ids[i], localPrefix)) {
                        hasLocal = true;
                    }
                }

                if (hasLocal) {
                    return localassetmanager.getItemsFromIds(serverInfo.Id, ids).then(function (items) {

                        items.forEach(function (item) {
                            item.Id = localPrefix + item.Id;
                        });

                        var result = {
                            Items: items,
                            TotalRecordCount: items.length
                        };

                        return Promise.resolve(result);
                    });
                }
            }

            return apiclientcore.getItems(userId, options);
        }

        function getItem(userId, itemId) {

            if (itemId) {
                itemId = itemId.toString();
            }

            var serverInfo;

            if (startsWith(itemId, localViewPrefix)) {

                serverInfo = apiclientcore.serverInfo();

                if (serverInfo) {
                    return localassetmanager.getViews(serverInfo.Id, userId).then(function (items) {

                        var views = items.filter(function (item) {
                            return item.Id === itemId;
                        });

                        if (views.length > 0) {
                            return Promise.resolve(views[0]);
                        }

                        // TODO: Test consequence of this
                        return Promise.reject();
                    });
                }
            }

            if (startsWith(itemId, localPrefix)) {

                serverInfo = apiclientcore.serverInfo();

                if (serverInfo) {
                    return localassetmanager.getLocalItem(serverInfo.Id, stripStart(itemId, localPrefix)).then(function (item) {

                        item.Item.Id = localPrefix + item.Item.Id;

                        return Promise.resolve(item.Item);
                    });
                }
            }

            return apiclientcore.getItem(userId, itemId);
        }

        function getNextUpEpisodes(options) {

            if (options.SeriesId) {
                if (startsWith(options.SeriesId, localPrefix)) {
                    return Promise.resolve(createEmptyList());
                }
            }

            return apiclientcore.getNextUpEpisodes(options);
        }

        function getSeasons(itemId, options) {

            if (startsWith(itemId, localPrefix)) {
                options.ParentId = itemId;
                return getItems(apiclientcore.getCurrentUserId(), options);
            }

            return apiclientcore.getSeasons(itemId, options);
        }

        function getEpisodes(itemId, options) {

            if (startsWith(options.SeasonId, localPrefix)) {
                options.ParentId = options.SeasonId;
                return getItems(apiclientcore.getCurrentUserId(), options);
            }

            return apiclientcore.getEpisodes(itemId, options);
        }

        function getThemeMedia(userId, itemId, inherit) {

            if (startsWith(itemId, localViewPrefix) || startsWith(itemId, localPrefix)) {
                return Promise.reject();
            }

            return apiclientcore.getThemeMedia(userId, itemId, inherit);
        }

        function getSimilarItems(itemId, options) {

            if (startsWith(itemId, localPrefix)) {
                return Promise.resolve(createEmptyList());
            }

            return apiclientcore.getSimilarItems(itemId, options);
        }

        function updateFavoriteStatus(userId, itemId, isFavorite) {

            if (startsWith(itemId, localPrefix)) {
                return Promise.resolve();
            }

            return apiclientcore.updateFavoriteStatus(userId, itemId, isFavorite);
        }

        function getScaledImageUrl(itemId, options) {

            if (startsWith(itemId, localPrefix)) {

                var serverInfo = apiclientcore.serverInfo();
                var id = stripStart(itemId, localPrefix);

                return localassetmanager.getImageUrl(serverInfo.Id, id, options.type, 0);
            }


            return apiclientcore.getScaledImageUrl(itemId, options);
        }

        function onWebSocketMessage(e, msg) {

            events.trigger(self, 'websocketmessage', [msg]);
        }

        // **************** Helper functions

        function startsWith(str, find) {

            if (str && find && str.length > find.length) {
                if (str.indexOf(find) === 0) {
                    return true;
                }
            }

            return false;
        }

        function stripStart(str, find) {
            if (startsWith(str, find)) {
                return str.substr(find.length);
            }

            return str;
        }

        function createEmptyList() {
            var result = {
                Items: [],
                TotalRecordCount: 0
            };

            return result;
        }

        function getPlaybackInfo(itemId, options, deviceProfile) {

            return localassetmanager.getLocalItem(apiclientcore.serverId(), stripStart(itemId, localPrefix)).then(function (item) {

                // TODO: This was already done during the sync process, right? If so, remove it
                var mediaSources = item.Item.MediaSources.map(function (m) {
                    m.SupportsDirectPlay = true;
                    m.SupportsDirectStream = false;
                    m.SupportsTranscoding = false;
                    return m;
                });

                return {
                    MediaSources: mediaSources
                };
            });
        }

        // "Override" methods

        self.detectBitrate = function () {
            return Promise.reject();
        };

        self.reportPlaybackStart = function (options) {

            if (!options) {
                throw new Error("null options");
            }

            return Promise.resolve();
        };

        self.reportPlaybackProgress = function (options) {

            if (!options) {
                throw new Error("null options");
            }

            return Promise.resolve();
        };

        self.reportPlaybackStopped = function (options) {

            if (!options) {
                throw new Error("null options");
            }

            return Promise.resolve();
        };

        self.getIntros = function (itemId) {

            return Promise.resolve({
                Items: [],
                TotalRecordCount: 0
            });
        };

        self.getUserViews = getUserViews;
        self.getItems = getItems;
        self.getItem = getItem;
        self.getSeasons = getSeasons;
        self.getEpisodes = getEpisodes;
        self.getThemeMedia = getThemeMedia;
        self.getNextUpEpisodes = getNextUpEpisodes;
        self.getSimilarItems = getSimilarItems;
        self.updateFavoriteStatus = updateFavoriteStatus;
        self.getScaledImageUrl = getScaledImageUrl;
        self.getPlaybackInfo = getPlaybackInfo;
    };

});