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

        events.on(apiclientcore, 'websocketmessage', onWebSocketMessage);

        var self = this;


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

        // Map "base" methods
        self.serverAddress = apiclientcore.serverAddress;
        self.serverInfo = apiclientcore.serverInfo;
        self.serverId = apiclientcore.serverId;
        self.getCurrentUser = apiclientcore.getCurrentUser;
        self.isLoggedIn = apiclientcore.isLoggedIn;
        self.getCurrentUserId = apiclientcore.getCurrentUserId;
        self.accessToken = apiclientcore.accessToken;
        self.deviceName = apiclientcore.deviceName;
        self.deviceId = apiclientcore.deviceId;
        self.appName = apiclientcore.appName;
        self.appVersion = apiclientcore.appVersion;
        self.clearAuthenticationInfo = apiclientcore.clearAuthenticationInfo;
        self.setAuthenticationInfo = apiclientcore.setAuthenticationInfo;
        self.encodeName = apiclientcore.encodeName;
        self.setRequestHeaders = apiclientcore.setRequestHeaders;
        self.ajax = apiclientcore.ajax;
        self.fetch = apiclientcore.fetch;
        self.getJSON = apiclientcore.getJSON;
        self.fetchWithFailover = apiclientcore.fetchWithFailover;
        self.get = apiclientcore.get;
        self.getUrl = apiclientcore.getUrl;
        self.updateServerInfo = apiclientcore.updateServerInfo;
        self.isWebSocketSupported = apiclientcore.isWebSocketSupported;
        self.ensureWebSocket = apiclientcore.ensureWebSocket;
        self.openWebSocket = apiclientcore.openWebSocket;
        self.closeWebSocket = apiclientcore.closeWebSocket;
        self.sendWebSocketMessage = apiclientcore.sendWebSocketMessage;
        self.isWebSocketOpen = apiclientcore.isWebSocketOpen;
        self.isWebSocketOpenOrConnecting = apiclientcore.isWebSocketOpenOrConnecting;
        self.getProductNews = apiclientcore.getProductNews;
        self.getDownloadSpeed = apiclientcore.getDownloadSpeed;
        self.detectBitrate = apiclientcore.detectBitrate;
        //self.getItem = apiclientcore.getItem;
        self.getRootFolder = apiclientcore.getRootFolder;
        self.getNotificationSummary = apiclientcore.getNotificationSummary;
        self.getNotifications = apiclientcore.getNotifications;
        self.markNotificationsRead = apiclientcore.markNotificationsRead;
        self.logout = apiclientcore.logout;
        self.getRemoteImageProviders = apiclientcore.getRemoteImageProviders;
        self.getAvailableRemoteImages = apiclientcore.getAvailableRemoteImages;
        self.downloadRemoteImage = apiclientcore.downloadRemoteImage;
        self.getLiveTvInfo = apiclientcore.getLiveTvInfo;
        self.getLiveTvGuideInfo = apiclientcore.getLiveTvGuideInfo;
        self.getLiveTvChannel = apiclientcore.getLiveTvChannel;
        self.getLiveTvChannels = apiclientcore.getLiveTvChannels;
        self.getLiveTvPrograms = apiclientcore.getLiveTvPrograms;
        self.getLiveTvRecommendedPrograms = apiclientcore.getLiveTvRecommendedPrograms;
        self.getLiveTvRecordings = apiclientcore.getLiveTvRecordings;
        self.getLiveTvRecordingSeries = apiclientcore.getLiveTvRecordingSeries;
        self.getLiveTvRecordingGroups = apiclientcore.getLiveTvRecordingGroups;
        self.getLiveTvRecordingGroup = apiclientcore.getLiveTvRecordingGroup;
        self.getLiveTvRecording = apiclientcore.getLiveTvRecording;
        self.getLiveTvProgram = apiclientcore.getLiveTvProgram;
        self.deleteLiveTvRecording = apiclientcore.deleteLiveTvRecording;
        self.cancelLiveTvTimer = apiclientcore.cancelLiveTvTimer;
        self.getLiveTvTimers = apiclientcore.getLiveTvTimers;
        self.getLiveTvTimer = apiclientcore.getLiveTvTimer;
        self.getNewLiveTvTimerDefaults = apiclientcore.getNewLiveTvTimerDefaults;
        self.createLiveTvTimer = apiclientcore.createLiveTvTimer;
        self.updateLiveTvTimer = apiclientcore.updateLiveTvTimer;
        self.resetLiveTvTuner = apiclientcore.resetLiveTvTuner;
        self.getLiveTvSeriesTimers = apiclientcore.getLiveTvSeriesTimers;
        self.getFileOrganizationResults = apiclientcore.getFileOrganizationResults;
        self.deleteOriginalFileFromOrganizationResult = apiclientcore.deleteOriginalFileFromOrganizationResult;
        self.clearOrganizationLog = apiclientcore.clearOrganizationLog;
        self.performOrganization = apiclientcore.performOrganization;
        self.performEpisodeOrganization = apiclientcore.performEpisodeOrganization;
        self.getLiveTvSeriesTimer = apiclientcore.getLiveTvSeriesTimer;
        self.cancelLiveTvSeriesTimer = apiclientcore.cancelLiveTvSeriesTimer;
        self.createLiveTvSeriesTimer = apiclientcore.createLiveTvSeriesTimer;
        self.updateLiveTvSeriesTimer = apiclientcore.updateLiveTvSeriesTimer;
        self.getRegistrationInfo = apiclientcore.getRegistrationInfo;
        self.getSystemInfo = apiclientcore.getSystemInfo;
        self.getPublicSystemInfo = apiclientcore.getPublicSystemInfo;
        self.getInstantMixFromItem = apiclientcore.getInstantMixFromItem;
        self.getEpisodes = apiclientcore.getEpisodes;
        self.getDisplayPreferences = apiclientcore.getDisplayPreferences;
        self.updateDisplayPreferences = apiclientcore.updateDisplayPreferences;
        self.getSeasons = apiclientcore.getSeasons;
        //self.getSimilarItems = apiclientcore.getSimilarItems;
        self.getCultures = apiclientcore.getCultures;
        self.getCountries = apiclientcore.getCountries;
        self.getPluginSecurityInfo = apiclientcore.getPluginSecurityInfo;
        self.getDirectoryContents = apiclientcore.getDirectoryContents;
        self.getNetworkShares = apiclientcore.getNetworkShares;
        self.getParentPath = apiclientcore.getParentPath;
        self.getDrives = apiclientcore.getDrives;
        self.getNetworkDevices = apiclientcore.getNetworkDevices;
        self.cancelPackageInstallation = apiclientcore.cancelPackageInstallation;
        self.refreshItem = apiclientcore.refreshItem;
        self.installPlugin = apiclientcore.installPlugin;
        self.restartServer = apiclientcore.restartServer;
        self.shutdownServer = apiclientcore.shutdownServer;
        self.getPackageInfo = apiclientcore.getPackageInfo;
        self.getAvailableApplicationUpdate = apiclientcore.getAvailableApplicationUpdate;
        self.getAvailablePluginUpdates = apiclientcore.getAvailablePluginUpdates;
        self.getVirtualFolders = apiclientcore.getVirtualFolders;
        self.getPhysicalPaths = apiclientcore.getPhysicalPaths;
        self.getServerConfiguration = apiclientcore.getServerConfiguration;
        self.getDevicesOptions = apiclientcore.getDevicesOptions;
        self.getContentUploadHistory = apiclientcore.getContentUploadHistory;
        self.getNamedConfiguration = apiclientcore.getNamedConfiguration;
        self.getScheduledTasks = apiclientcore.getScheduledTasks;
        self.startScheduledTask = apiclientcore.startScheduledTask;
        self.getScheduledTask = apiclientcore.getScheduledTask;
        self.getNextUpEpisodes = apiclientcore.getNextUpEpisodes;
        self.stopScheduledTask = apiclientcore.stopScheduledTask;
        self.getPluginConfiguration = apiclientcore.getPluginConfiguration;
        self.getAvailablePlugins = apiclientcore.getAvailablePlugins;
        self.uninstallPlugin = apiclientcore.uninstallPlugin;
        self.removeVirtualFolder = apiclientcore.removeVirtualFolder;
        self.addVirtualFolder = apiclientcore.addVirtualFolder;
        self.updateVirtualFolderOptions = apiclientcore.updateVirtualFolderOptions;
        self.renameVirtualFolder = apiclientcore.renameVirtualFolder;
        self.addMediaPath = apiclientcore.addMediaPath;
        self.updateMediaPath = apiclientcore.updateMediaPath;
        self.removeMediaPath = apiclientcore.removeMediaPath;
        self.deleteUser = apiclientcore.deleteUser;
        self.deleteUserImage = apiclientcore.deleteUserImage;
        self.deleteItemImage = apiclientcore.deleteItemImage;
        self.deleteItem = apiclientcore.deleteItem;
        self.stopActiveEncodings = apiclientcore.stopActiveEncodings;
        self.reportCapabilities = apiclientcore.reportCapabilities;
        self.updateItemImageIndex = apiclientcore.updateItemImageIndex;
        self.getItemImageInfos = apiclientcore.getItemImageInfos;
        self.getCriticReviews = apiclientcore.getCriticReviews;
        self.getSessions = apiclientcore.getSessions;
        self.uploadUserImage = apiclientcore.uploadUserImage;
        self.uploadItemImage = apiclientcore.uploadItemImage;
        self.getInstalledPlugins = apiclientcore.getInstalledPlugins;
        self.getUser = apiclientcore.getUser;
        self.getOfflineUser = apiclientcore.getOfflineUser;
        self.getStudio = apiclientcore.getStudio;
        self.getGenre = apiclientcore.getGenre;
        self.getMusicGenre = apiclientcore.getMusicGenre;
        self.getGameGenre = apiclientcore.getGameGenre;
        self.getArtist = apiclientcore.getArtist;
        self.getPerson = apiclientcore.getPerson;
        self.getPublicUsers = apiclientcore.getPublicUsers;
        self.getUsers = apiclientcore.getUsers;
        self.getParentalRatings = apiclientcore.getParentalRatings;
        self.getDefaultImageQuality = apiclientcore.getDefaultImageQuality;
        self.getUserImageUrl = apiclientcore.getUserImageUrl;
        self.getImageUrl = apiclientcore.getImageUrl;
        //self.getScaledImageUrl = apiclientcore.getScaledImageUrl;
        self.getThumbImageUrl = apiclientcore.getThumbImageUrl;
        self.authenticateUserByName = apiclientcore.authenticateUserByName;
        self.updateUserPassword = apiclientcore.updateUserPassword;
        self.updateEasyPassword = apiclientcore.updateEasyPassword;
        self.resetUserPassword = apiclientcore.resetUserPassword;
        self.resetEasyPassword = apiclientcore.resetEasyPassword;
        self.updateServerConfiguration = apiclientcore.updateServerConfiguration;
        self.updateNamedConfiguration = apiclientcore.updateNamedConfiguration;
        self.updateItem = apiclientcore.updateItem;
        self.updatePluginSecurityInfo = apiclientcore.updatePluginSecurityInfo;
        self.createUser = apiclientcore.createUser;
        self.updateUser = apiclientcore.updateUser;
        self.updateUserPolicy = apiclientcore.updateUserPolicy;
        self.updateUserConfiguration = apiclientcore.updateUserConfiguration;
        self.updateScheduledTaskTriggers = apiclientcore.updateScheduledTaskTriggers;
        self.updatePluginConfiguration = apiclientcore.updatePluginConfiguration;
        self.getAncestorItems = apiclientcore.getAncestorItems;
        //self.getItems = apiclientcore.getItems;
        self.getMovieRecommendations = apiclientcore.getMovieRecommendations;
        self.getUpcomingEpisodes = apiclientcore.getUpcomingEpisodes;
        self.getChannels = apiclientcore.getChannels;
        self.getLatestChannelItems = apiclientcore.getLatestChannelItems;
        //self.getUserViews = apiclientcore.getUserViews;
        self.getArtists = apiclientcore.getArtists;
        self.getAlbumArtists = apiclientcore.getAlbumArtists;
        self.getGenres = apiclientcore.getGenres;
        self.getMusicGenres = apiclientcore.getMusicGenres;
        self.getGameGenres = apiclientcore.getGameGenres;
        self.getPeople = apiclientcore.getPeople;
        self.getStudios = apiclientcore.getStudios;
        self.getLocalTrailers = apiclientcore.getLocalTrailers;
        self.getGameSystems = apiclientcore.getGameSystems;
        self.getAdditionalVideoParts = apiclientcore.getAdditionalVideoParts;
        //self.getThemeMedia = apiclientcore.getThemeMedia;
        self.getSearchHints = apiclientcore.getSearchHints;
        self.getSpecialFeatures = apiclientcore.getSpecialFeatures;
        self.getDateParamValue = apiclientcore.getDateParamValue;
        self.markPlayed = apiclientcore.markPlayed;
        self.markUnplayed = apiclientcore.markUnplayed;
        //self.updateFavoriteStatus = apiclientcore.updateFavoriteStatus;
        self.updateUserItemRating = apiclientcore.updateUserItemRating;
        self.getItemCounts = apiclientcore.getItemCounts;
        self.clearUserItemRating = apiclientcore.clearUserItemRating;
        self.reportPlaybackStart = apiclientcore.reportPlaybackStart;
        self.reportPlaybackProgress = apiclientcore.reportPlaybackProgress;
        self.reportOfflineActions = apiclientcore.reportOfflineActions;
        self.syncData = apiclientcore.syncData;
        self.getReadySyncItems = apiclientcore.getReadySyncItems;
        self.reportSyncJobItemTransferred = apiclientcore.reportSyncJobItemTransferred;
        self.cancelSyncItems = apiclientcore.cancelSyncItems;
        self.reportPlaybackStopped = apiclientcore.reportPlaybackStopped;
        self.sendPlayCommand = apiclientcore.sendPlayCommand;
        self.sendCommand = apiclientcore.sendCommand;
        self.sendMessageCommand = apiclientcore.sendMessageCommand;
        self.sendPlayStateCommand = apiclientcore.sendPlayStateCommand;
        self.createPackageReview = apiclientcore.createPackageReview;
        self.getPackageReviews = apiclientcore.getPackageReviews;
        self.getSmartMatchInfos = apiclientcore.getSmartMatchInfos;
        self.deleteSmartMatchEntries = apiclientcore.deleteSmartMatchEntries;
        self.createPin = apiclientcore.createPin;
        self.getPinStatus = apiclientcore.getPinStatus;
        self.exchangePin = apiclientcore.exchangePin;

    };

});