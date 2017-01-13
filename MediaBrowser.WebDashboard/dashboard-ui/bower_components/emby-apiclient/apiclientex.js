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

            if (serverInfo && options.ParentId === 'localview') {

                return localassetmanager.getViews(serverInfo.Id, userId).then(function (items) {
                    var result = {
                        Items: items,
                        TotalRecordCount: items.length
                    };

                    return Promise.resolve(result);
                });

            } else if (serverInfo && options && startsWith(options.ParentId, localViewPrefix)) {

                return localassetmanager.getViewItems(serverInfo.Id, userId, options.ParentId).then(function (items) {

                    items.forEach(function (item) {
                        item.Id = localPrefix + item.Id;
                    });

                    var result = {
                        Items: items,
                        TotalRecordCount: items.length
                    };

                    return Promise.resolve(result);
                });
            } else if (options && options.ExcludeItemIds && options.ExcludeItemIds.length) {

                var exItems = options.ExcludeItemIds;

                for (var i = 0; i < exItems.length; i++) {
                    if (startsWith(exItems[i], localPrefix)) {
                        return Promise.resolve(this.createEmptyList());
                    }
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

        // "Override" methods
        self.getUserViews = getUserViews;
        self.getItems = getItems;
        self.getItem = getItem;
        self.getThemeMedia = getThemeMedia;
        self.getSimilarItems = getSimilarItems;
        self.updateFavoriteStatus = updateFavoriteStatus;
        self.getScaledImageUrl = getScaledImageUrl;
    };

});