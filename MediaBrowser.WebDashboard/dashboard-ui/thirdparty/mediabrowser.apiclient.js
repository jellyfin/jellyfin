(function (window) {

    function myStore(defaultObject) {

        var self = this;
        self.localData = {};

        var isDefaultAvailable;

        if (defaultObject) {
            try {
                defaultObject.setItem('_test', '0');
                isDefaultAvailable = true;
            } catch (e) {

            }
        }

        self.setItem = function (name, value) {

            if (isDefaultAvailable) {
                defaultObject.setItem(name, value);
            } else {
                self.localData[name] = value;
            }
        };

        self.getItem = function (name) {

            if (isDefaultAvailable) {
                return defaultObject.getItem(name);
            }

            return self.localData[name];
        };

        self.removeItem = function (name) {

            if (isDefaultAvailable) {
                defaultObject.removeItem(name);
            } else {
                self.localData[name] = null;
            }
        };
    }

    window.store = new myStore(window.localStorage);
    window.sessionStore = new myStore(window.sessionStorage);

})(window);

if (!window.MediaBrowser) {
    window.MediaBrowser = {};
}

MediaBrowser.ApiClient = function ($, navigator, JSON, WebSocket, setTimeout, window, FileReader) {

    /**
     * Creates a new api client instance
     * @param {String} serverAddress
     * @param {String} clientName 
     * @param {String} applicationVersion 
     */
    return function (serverAddress, clientName, applicationVersion, deviceName, deviceId) {

        if (!serverAddress) {
            throw new Error("Must supply a serverAddress");
        }

        var self = this;
        var currentUserId;
        var accessToken;
        var webSocket;

        /**
         * Gets the server address.
         */
        self.serverAddress = function () {

            return serverAddress;
        };

        self.apiPrefix = function () {

            return "/mediabrowser";
        };

        /**
         * Gets or sets the current user id.
         */
        self.getCurrentUserId = function () {

            return currentUserId;
        };

        self.accessToken = function () {
            return accessToken;
        };

        self.setCurrentUserId = function (userId, token) {

            currentUserId = userId;
            accessToken = token;
        };

        self.deviceName = function () {
            return deviceName;
        };

        self.deviceId = function () {
            return deviceId;
        };

        self.encodeName = function (name) {

            name = name.split('/').join('-');

            name = name.split('?').join('-');

            var val = $.param({ name: name });
            return val.substring(val.indexOf('=') + 1).replace("'", '%27');
        };

        /**
         * Wraps around jQuery ajax methods to add additional info to the request.
         */
        self.ajax = function (request) {

            if (!request) {
                throw new Error("Request cannot be null");
            }

            if (clientName) {

                var auth = 'MediaBrowser Client="' + clientName + '", Device="' + deviceName + '", DeviceId="' + deviceId + '", Version="' + applicationVersion + '"';

                if (currentUserId) {
                    auth += ', UserId="' + currentUserId + '"';
                }

                request.headers = {
                    Authorization: auth
                };
            }

            if (accessToken) {
                request.headers['X-MediaBrowser-Token'] = accessToken;
            }

            return $.ajax(request);
        };

        self.getJSON = function (url) {

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Creates an api url based on a handler name and query string parameters
         * @param {String} name
         * @param {Object} params
         */
        self.getUrl = function (name, params) {

            if (!name) {
                throw new Error("Url name cannot be empty");
            }

            var url = serverAddress;

            url += self.apiPrefix() + "/" + name;

            if (params) {
                url += "?" + $.param(params);
            }

            return url;
        };

        self.openWebSocket = function (webSocketAddress) {

            var url = webSocketAddress + self.apiPrefix();

            webSocket = new WebSocket(url);

            webSocket.onmessage = function (msg) {

                msg = JSON.parse(msg.data);
                $(self).trigger("websocketmessage", [msg]);
            };

            webSocket.onopen = function () {

                console.log('web socket connection opened');
                setTimeout(function () {

                    self.sendWebSocketMessage("Identity", clientName + "|" + deviceId + "|" + applicationVersion + "|" + deviceName);

                    $(self).trigger("websocketopen");

                }, 500);
            };
            webSocket.onerror = function () {
                setTimeout(function () {
                    $(self).trigger("websocketerror");
                }, 0);
            };
            webSocket.onclose = function () {
                setTimeout(function () {
                    $(self).trigger("websocketclose");
                }, 0);
            };
        };

        self.closeWebSocket = function () {
            if (webSocket && webSocket.readyState === WebSocket.OPEN) {
                webSocket.close();
            }
        };

        self.sendWebSocketMessage = function (name, data) {

            console.log('Sending web socket message: ' + name);

            var msg = { MessageType: name };

            if (data) {
                msg.Data = data;
            }

            msg = JSON.stringify(msg);

            webSocket.send(msg);
        };

        self.isWebSocketOpen = function () {
            return webSocket && webSocket.readyState === WebSocket.OPEN;
        };

        self.isWebSocketOpenOrConnecting = function () {
            return webSocket && (webSocket.readyState === WebSocket.OPEN || webSocket.readyState === WebSocket.CONNECTING);
        };

        self.getProductNews = function (options) {

            options = options || {};

            var url = self.getUrl("News/Product", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets an item from the server
         * Omit itemId to get the root folder.
         */
        self.getItem = function (userId, itemId) {

            if (!userId) {
                throw new Error("null userId");
            }
            if (!itemId) {
                throw new Error("null itemId");
            }

            var url = self.getUrl("Users/" + userId + "/Items/" + itemId);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets the root folder from the server
         */
        self.getRootFolder = function (userId) {

            if (!userId) {
                throw new Error("null userId");
            }

            var url = self.getUrl("Users/" + userId + "/Items/Root");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getNotificationSummary = function (userId) {

            if (!userId) {
                throw new Error("null userId");
            }

            var url = self.getUrl("Notifications/" + userId + "/Summary");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getNotifications = function (userId, options) {

            if (!userId) {
                throw new Error("null userId");
            }

            var url = self.getUrl("Notifications/" + userId, options || {});

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.markNotificationsRead = function (userId, idList, isRead) {

            if (!userId) {
                throw new Error("null userId");
            }

            if (!idList || !idList.length) {
                throw new Error("null idList");
            }

            var suffix = isRead ? "Read" : "Unread";

            var params = {
                UserId: userId,
                Ids: idList.join(',')
            };

            var url = self.getUrl("Notifications/" + userId + "/" + suffix, params);

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        self.logout = function () {

            var done = function () {
                self.setCurrentUserId(null, null);
            };

            if (accessToken) {
                var url = self.getUrl("Sessions/Logout");

                return self.ajax({
                    type: "POST",
                    url: url
                }).done(done);
            }

            var deferred = $.Deferred();
            deferred.resolveWith(null, []);
            return deferred.promise().done(done);
        };

        function getRemoteImagePrefix(options) {

            var urlPrefix;

            if (options.artist) {
                urlPrefix = "Artists/" + self.encodeName(options.artist);
                delete options.artist;
            } else if (options.person) {
                urlPrefix = "Persons/" + self.encodeName(options.person);
                delete options.person;
            } else if (options.genre) {
                urlPrefix = "Genres/" + self.encodeName(options.genre);
                delete options.genre;
            } else if (options.musicGenre) {
                urlPrefix = "MusicGenres/" + self.encodeName(options.musicGenre);
                delete options.musicGenre;
            } else if (options.gameGenre) {
                urlPrefix = "GameGenres/" + self.encodeName(options.gameGenre);
                delete options.gameGenre;
            } else if (options.studio) {
                urlPrefix = "Studios/" + self.encodeName(options.studio);
                delete options.studio;
            } else {
                urlPrefix = "Items/" + options.itemId;
                delete options.itemId;
            }

            return urlPrefix;
        }

        self.getRemoteImageProviders = function (options) {

            if (!options) {
                throw new Error("null options");
            }

            var urlPrefix = getRemoteImagePrefix(options);

            var url = self.getUrl(urlPrefix + "/RemoteImages/Providers", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getAvailableRemoteImages = function (options) {

            if (!options) {
                throw new Error("null options");
            }

            var urlPrefix = getRemoteImagePrefix(options);

            var url = self.getUrl(urlPrefix + "/RemoteImages", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.downloadRemoteImage = function (options) {

            if (!options) {
                throw new Error("null options");
            }

            var urlPrefix = getRemoteImagePrefix(options);

            var url = self.getUrl(urlPrefix + "/RemoteImages/Download", options);

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        self.getLiveTvInfo = function (options) {

            var url = self.getUrl("LiveTv/Info", options || {});

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getLiveTvGuideInfo = function (options) {

            var url = self.getUrl("LiveTv/GuideInfo", options || {});

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getLiveTvChannel = function (id, userId) {

            if (!id) {
                throw new Error("null id");
            }

            var options = {

            };

            if (userId) {
                options.userId = userId;
            }

            var url = self.getUrl("LiveTv/Channels/" + id, options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getLiveTvChannels = function (options) {

            var url = self.getUrl("LiveTv/Channels", options || {});

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getLiveTvPrograms = function (options) {

            options = options || {};

            if (options.channelIds && options.channelIds.length > 1800) {

                return self.ajax({
                    type: "POST",
                    url: self.getUrl("LiveTv/Programs"),
                    data: JSON.stringify(options),
                    contentType: "application/json",
                    dataType: "json"
                });

            } else {

                return self.ajax({
                    type: "GET",
                    url: self.getUrl("LiveTv/Programs", options),
                    dataType: "json"
                });
            }
        };

        self.getLiveTvRecommendedPrograms = function (options) {

            options = options || {};

            return self.ajax({
                type: "GET",
                url: self.getUrl("LiveTv/Programs/Recommended", options),
                dataType: "json"
            });
        };

        self.getLiveTvRecordings = function (options) {

            var url = self.getUrl("LiveTv/Recordings", options || {});

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getLiveTvRecordingGroups = function (options) {

            var url = self.getUrl("LiveTv/Recordings/Groups", options || {});

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getLiveTvRecordingGroup = function (id) {

            if (!id) {
                throw new Error("null id");
            }

            var url = self.getUrl("LiveTv/Recordings/Groups/" + id);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getLiveTvRecording = function (id, userId) {

            if (!id) {
                throw new Error("null id");
            }

            var options = {

            };

            if (userId) {
                options.userId = userId;
            }

            var url = self.getUrl("LiveTv/Recordings/" + id, options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getLiveTvProgram = function (id, userId) {

            if (!id) {
                throw new Error("null id");
            }

            var options = {

            };

            if (userId) {
                options.userId = userId;
            }

            var url = self.getUrl("LiveTv/Programs/" + id, options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.deleteLiveTvRecording = function (id) {

            if (!id) {
                throw new Error("null id");
            }

            var url = self.getUrl("LiveTv/Recordings/" + id);

            return self.ajax({
                type: "DELETE",
                url: url
            });
        };

        self.cancelLiveTvTimer = function (id) {

            if (!id) {
                throw new Error("null id");
            }

            var url = self.getUrl("LiveTv/Timers/" + id);

            return self.ajax({
                type: "DELETE",
                url: url
            });
        };

        self.getLiveTvTimers = function (options) {

            var url = self.getUrl("LiveTv/Timers", options || {});

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getLiveTvTimer = function (id) {

            if (!id) {
                throw new Error("null id");
            }

            var url = self.getUrl("LiveTv/Timers/" + id);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getNewLiveTvTimerDefaults = function (options) {

            options = options || {};

            var url = self.getUrl("LiveTv/Timers/Defaults", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.createLiveTvTimer = function (item) {

            if (!item) {
                throw new Error("null item");
            }

            var url = self.getUrl("LiveTv/Timers");

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(item),
                contentType: "application/json"
            });
        };

        self.updateLiveTvTimer = function (item) {

            if (!item) {
                throw new Error("null item");
            }

            var url = self.getUrl("LiveTv/Timers/" + item.Id);

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(item),
                contentType: "application/json"
            });
        };

        self.resetLiveTvTuner = function (id) {

            if (!id) {
                throw new Error("null id");
            }

            var url = self.getUrl("LiveTv/Tuners/" + id + "/Reset");

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        self.getLiveTvSeriesTimers = function (options) {

            var url = self.getUrl("LiveTv/SeriesTimers", options || {});

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getFileOrganizationResults = function (options) {

            var url = self.getUrl("Library/FileOrganization", options || {});

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.deleteOriginalFileFromOrganizationResult = function (id) {

            var url = self.getUrl("Library/FileOrganizations/" + id + "/File");

            return self.ajax({
                type: "DELETE",
                url: url
            });
        };

        self.clearOrganizationLog = function () {

            var url = self.getUrl("Library/FileOrganizations");

            return self.ajax({
                type: "DELETE",
                url: url
            });
        };

        self.performOrganization = function (id) {

            var url = self.getUrl("Library/FileOrganizations/" + id + "/Organize");

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        self.performEpisodeOrganization = function (id, options) {

            var url = self.getUrl("Library/FileOrganizations/" + id + "/Episode/Organize", options || {});

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        self.getLiveTvSeriesTimer = function (id) {

            if (!id) {
                throw new Error("null id");
            }

            var url = self.getUrl("LiveTv/SeriesTimers/" + id);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.cancelLiveTvSeriesTimer = function (id) {

            if (!id) {
                throw new Error("null id");
            }

            var url = self.getUrl("LiveTv/SeriesTimers/" + id);

            return self.ajax({
                type: "DELETE",
                url: url
            });
        };

        self.createLiveTvSeriesTimer = function (item) {

            if (!item) {
                throw new Error("null item");
            }

            var url = self.getUrl("LiveTv/SeriesTimers");

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(item),
                contentType: "application/json"
            });
        };

        self.updateLiveTvSeriesTimer = function (item) {

            if (!item) {
                throw new Error("null item");
            }

            var url = self.getUrl("LiveTv/SeriesTimers/" + item.Id);

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(item),
                contentType: "application/json"
            });
        };

        /**
         * Gets the current server status
         */
        self.getSystemInfo = function () {

            var url = self.getUrl("System/Info");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getInstantMixFromSong = function (itemId, options) {

            var url = self.getUrl("Songs/" + itemId + "/InstantMix", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getInstantMixFromAlbum = function (itemId, options) {

            var url = self.getUrl("Albums/" + itemId + "/InstantMix", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getInstantMixFromArtist = function (options) {

            var url = self.getUrl("Artists/InstantMix", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getInstantMixFromMusicGenre = function (options) {

            var url = self.getUrl("MusicGenres/InstantMix", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getEpisodes = function (itemId, options) {

            var url = self.getUrl("Shows/" + itemId + "/Episodes", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getDisplayPreferences = function (id, userId, app) {

            var url = self.getUrl("DisplayPreferences/" + id, {
                userId: userId,
                client: app
            });

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.updateDisplayPreferences = function (id, obj, userId, app) {

            var url = self.getUrl("DisplayPreferences/" + id, {
                userId: userId,
                client: app
            });

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(obj),
                contentType: "application/json"
            });
        };

        self.getSeasons = function (itemId, options) {

            var url = self.getUrl("Shows/" + itemId + "/Seasons", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getSimilarMovies = function (itemId, options) {

            var url = self.getUrl("Movies/" + itemId + "/Similar", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getSimilarTrailers = function (itemId, options) {

            var url = self.getUrl("Trailers/" + itemId + "/Similar", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getSimilarShows = function (itemId, options) {

            var url = self.getUrl("Shows/" + itemId + "/Similar", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getSimilarAlbums = function (itemId, options) {

            var url = self.getUrl("Albums/" + itemId + "/Similar", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getSimilarGames = function (itemId, options) {

            var url = self.getUrl("Games/" + itemId + "/Similar", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets all cultures known to the server
         */
        self.getCultures = function () {

            var url = self.getUrl("Localization/cultures");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets all countries known to the server
         */
        self.getCountries = function () {

            var url = self.getUrl("Localization/countries");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets plugin security info
         */
        self.getPluginSecurityInfo = function () {

            var url = self.getUrl("Plugins/SecurityInfo");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets the directory contents of a path on the server
         */
        self.getDirectoryContents = function (path, options) {

            if (!path) {
                throw new Error("null path");
            }

            options = options || {};

            options.path = path;

            var url = self.getUrl("Environment/DirectoryContents", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets shares from a network device
         */
        self.getNetworkShares = function (path) {

            if (!path) {
                throw new Error("null path");
            }

            var options = {};
            options.path = path;

            var url = self.getUrl("Environment/NetworkShares", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets the parent of a given path
         */
        self.getParentPath = function (path) {

            if (!path) {
                throw new Error("null path");
            }

            var options = {};
            options.path = path;

            var url = self.getUrl("Environment/ParentPath", options);

            return self.ajax({
                type: "GET",
                url: url
            });
        };

        /**
         * Gets a list of physical drives from the server
         */
        self.getDrives = function () {

            var url = self.getUrl("Environment/Drives");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets a list of network devices from the server
         */
        self.getNetworkDevices = function () {

            var url = self.getUrl("Environment/NetworkDevices");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Cancels a package installation
         */
        self.cancelPackageInstallation = function (installationId) {

            if (!installationId) {
                throw new Error("null installationId");
            }

            var url = self.getUrl("Packages/Installing/" + installationId);

            return self.ajax({
                type: "DELETE",
                url: url
            });
        };

        /**
         * Refreshes metadata for an item
         */
        self.refreshItem = function (itemId, options) {

            if (!itemId) {
                throw new Error("null itemId");
            }

            var url = self.getUrl("Items/" + itemId + "/Refresh", options || {});

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        /**
         * Installs or updates a new plugin
         */
        self.installPlugin = function (name, guid, updateClass, version) {

            if (!name) {
                throw new Error("null name");
            }

            if (!updateClass) {
                throw new Error("null updateClass");
            }

            var options = {
                updateClass: updateClass,
                AssemblyGuid: guid
            };

            if (version) {
                options.version = version;
            }

            var url = self.getUrl("Packages/Installed/" + name, options);

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        /**
         * Instructs the server to perform a restart.
         */
        self.restartServer = function () {

            var url = self.getUrl("System/Restart");

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        /**
         * Instructs the server to perform a shutdown.
         */
        self.shutdownServer = function () {

            var url = self.getUrl("System/Shutdown");

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        /**
         * Gets information about an installable package
         */
        self.getPackageInfo = function (name, guid) {

            if (!name) {
                throw new Error("null name");
            }

            var options = {
                AssemblyGuid: guid
            };

            var url = self.getUrl("Packages/" + name, options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets the latest available application update (if any)
         */
        self.getAvailableApplicationUpdate = function () {

            var url = self.getUrl("Packages/Updates", { PackageType: "System" });

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets the latest available plugin updates (if any)
         */
        self.getAvailablePluginUpdates = function () {

            var url = self.getUrl("Packages/Updates", { PackageType: "UserInstalled" });

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets the virtual folder list
         */
        self.getVirtualFolders = function (userId) {

            var url = userId ? "Users/" + userId + "/VirtualFolders" : "Library/VirtualFolders";

            url = self.getUrl(url);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets all the paths of the locations in the physical root.
         */
        self.getPhysicalPaths = function () {

            var url = self.getUrl("Library/PhysicalPaths");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets the current server configuration
         */
        self.getServerConfiguration = function () {

            var url = self.getUrl("System/Configuration");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getNamedConfiguration = function (name) {

            var url = self.getUrl("System/Configuration/" + name);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets the server's scheduled tasks
         */
        self.getScheduledTasks = function (options) {

            options = options || {};

            var url = self.getUrl("ScheduledTasks", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
        * Starts a scheduled task
        */
        self.startScheduledTask = function (id) {

            if (!id) {
                throw new Error("null id");
            }

            var url = self.getUrl("ScheduledTasks/Running/" + id);

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        /**
        * Gets a scheduled task
        */
        self.getScheduledTask = function (id) {

            if (!id) {
                throw new Error("null id");
            }

            var url = self.getUrl("ScheduledTasks/" + id);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getNextUpEpisodes = function (options) {

            var url = self.getUrl("Shows/NextUp", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
        * Stops a scheduled task
        */
        self.stopScheduledTask = function (id) {

            if (!id) {
                throw new Error("null id");
            }

            var url = self.getUrl("ScheduledTasks/Running/" + id);

            return self.ajax({
                type: "DELETE",
                url: url
            });
        };

        /**
         * Gets the configuration of a plugin
         * @param {String} Id
         */
        self.getPluginConfiguration = function (id) {

            if (!id) {
                throw new Error("null Id");
            }

            var url = self.getUrl("Plugins/" + id + "/Configuration");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets a list of plugins that are available to be installed
         */
        self.getAvailablePlugins = function (options) {

            options = $.extend({}, options || {});
            options.PackageType = "UserInstalled";

            var url = self.getUrl("Packages", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Uninstalls a plugin
         * @param {String} Id
         */
        self.uninstallPlugin = function (id) {

            if (!id) {
                throw new Error("null Id");
            }

            var url = self.getUrl("Plugins/" + id);

            return self.ajax({
                type: "DELETE",
                url: url
            });
        };

        /**
        * Removes a virtual folder
        * @param {String} name
        */
        self.removeVirtualFolder = function (name, refreshLibrary) {

            if (!name) {
                throw new Error("null name");
            }

            var url = "Library/VirtualFolders";

            url = self.getUrl(url, {
                refreshLibrary: refreshLibrary ? true : false,
                name: name
            });

            return self.ajax({
                type: "DELETE",
                url: url
            });
        };

        /**
       * Adds a virtual folder
       * @param {String} name
       */
        self.addVirtualFolder = function (name, type, refreshLibrary) {

            if (!name) {
                throw new Error("null name");
            }

            var options = {};

            if (type) {
                options.collectionType = type;
            }

            options.refreshLibrary = refreshLibrary ? true : false;
            options.name = name;

            var url = "Library/VirtualFolders";

            url = self.getUrl(url, options);

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        /**
       * Renames a virtual folder
       * @param {String} name
       */
        self.renameVirtualFolder = function (name, newName, refreshLibrary) {

            if (!name) {
                throw new Error("null name");
            }

            var url = "Library/VirtualFolders/Name";

            url = self.getUrl(url, {
                refreshLibrary: refreshLibrary ? true : false,
                newName: newName,
                name: name
            });

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        /**
        * Adds an additional mediaPath to an existing virtual folder
        * @param {String} name
        */
        self.addMediaPath = function (virtualFolderName, mediaPath, refreshLibrary) {

            if (!virtualFolderName) {
                throw new Error("null virtualFolderName");
            }

            if (!mediaPath) {
                throw new Error("null mediaPath");
            }

            var url = "Library/VirtualFolders/Paths";

            url = self.getUrl(url, {
                refreshLibrary: refreshLibrary ? true : false,
                path: mediaPath,
                name: virtualFolderName
            });

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        /**
        * Removes a media path from a virtual folder
        * @param {String} name
        */
        self.removeMediaPath = function (virtualFolderName, mediaPath, refreshLibrary) {

            if (!virtualFolderName) {
                throw new Error("null virtualFolderName");
            }

            if (!mediaPath) {
                throw new Error("null mediaPath");
            }

            var url = "Library/VirtualFolders/Paths";

            url = self.getUrl(url, {
                refreshLibrary: refreshLibrary ? true : false,
                path: mediaPath,
                name: virtualFolderName
            });

            return self.ajax({
                type: "DELETE",
                url: url
            });
        };

        /**
         * Deletes a user
         * @param {String} id
         */
        self.deleteUser = function (id) {

            if (!id) {
                throw new Error("null id");
            }

            var url = self.getUrl("Users/" + id);

            return self.ajax({
                type: "DELETE",
                url: url
            });
        };

        /**
         * Deletes a user image
         * @param {String} userId
         * @param {String} imageType The type of image to delete, based on the server-side ImageType enum.
         */
        self.deleteUserImage = function (userId, imageType, imageIndex) {

            if (!userId) {
                throw new Error("null userId");
            }

            if (!imageType) {
                throw new Error("null imageType");
            }

            var url = self.getUrl("Users/" + userId + "/Images/" + imageType);

            if (imageIndex != null) {
                url += "/" + imageIndex;
            }

            return self.ajax({
                type: "DELETE",
                url: url
            });
        };

        self.deleteItemImage = function (itemId, imageType, imageIndex) {

            if (!imageType) {
                throw new Error("null imageType");
            }

            var url = self.getUrl("Items/" + itemId + "/Images");

            url += "/" + imageType;

            if (imageIndex != null) {
                url += "/" + imageIndex;
            }

            return self.ajax({
                type: "DELETE",
                url: url
            });
        };

        self.deleteItem = function (itemId) {

            if (!itemId) {
                throw new Error("null itemId");
            }

            var url = self.getUrl("Items/" + itemId);

            return self.ajax({
                type: "DELETE",
                url: url
            });
        };

        self.stopActiveEncodings = function () {

            var url = self.getUrl("Videos/ActiveEncodings", {

                deviceId: deviceId
            });

            return self.ajax({
                type: "DELETE",
                url: url
            });
        };

        self.reportCapabilities = function (options) {

            var url = self.getUrl("Sessions/Capabilities", options);

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        self.updateItemImageIndex = function (itemId, imageType, imageIndex, newIndex) {

            if (!imageType) {
                throw new Error("null imageType");
            }

            var options = { newIndex: newIndex };

            var url = self.getUrl("Items/" + itemId + "/Images/" + imageType + "/" + imageIndex + "/Index", options);

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        self.getItemImageInfos = function (itemId) {

            var url = self.getUrl("Items/" + itemId + "/Images");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getCriticReviews = function (itemId, options) {

            if (!itemId) {
                throw new Error("null itemId");
            }

            var url = self.getUrl("Items/" + itemId + "/CriticReviews", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getSessions = function (options) {

            var url = self.getUrl("Sessions", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Uploads a user image
         * @param {String} userId
         * @param {String} imageType The type of image to delete, based on the server-side ImageType enum.
         * @param {Object} file The file from the input element
         */
        self.uploadUserImage = function (userId, imageType, file) {

            if (!userId) {
                throw new Error("null userId");
            }

            if (!imageType) {
                throw new Error("null imageType");
            }

            if (!file) {
                throw new Error("File must be an image.");
            }

            if (file.type != "image/png" && file.type != "image/jpeg" && file.type != "image/jpeg") {
                throw new Error("File must be an image.");
            }

            var deferred = $.Deferred();

            var reader = new FileReader();

            reader.onerror = function () {
                deferred.reject();
            };

            reader.onabort = function () {
                deferred.reject();
            };

            // Closure to capture the file information.
            reader.onload = function (e) {

                // Split by a comma to remove the url: prefix
                var data = e.target.result.split(',')[1];

                var url = self.getUrl("Users/" + userId + "/Images/" + imageType);

                self.ajax({
                    type: "POST",
                    url: url,
                    data: data,
                    contentType: "image/" + file.name.substring(file.name.lastIndexOf('.') + 1)
                }).done(function (result) {

                    deferred.resolveWith(null, [result]);

                }).fail(function () {
                    deferred.reject();
                });
            };

            // Read in the image file as a data URL.
            reader.readAsDataURL(file);

            return deferred.promise();
        };

        self.uploadItemImage = function (itemId, imageType, file) {

            if (!itemId) {
                throw new Error("null itemId");
            }

            if (!imageType) {
                throw new Error("null imageType");
            }

            if (!file) {
                throw new Error("File must be an image.");
            }

            if (file.type != "image/png" && file.type != "image/jpeg" && file.type != "image/jpeg") {
                throw new Error("File must be an image.");
            }

            var url = self.getUrl("Items/" + itemId + "/Images");

            url += "/" + imageType;

            var deferred = $.Deferred();

            var reader = new FileReader();

            reader.onerror = function () {
                deferred.reject();
            };

            reader.onabort = function () {
                deferred.reject();
            };

            // Closure to capture the file information.
            reader.onload = function (e) {

                // Split by a comma to remove the url: prefix
                var data = e.target.result.split(',')[1];

                self.ajax({
                    type: "POST",
                    url: url,
                    data: data,
                    contentType: "image/" + file.name.substring(file.name.lastIndexOf('.') + 1)
                }).done(function (result) {

                    deferred.resolveWith(null, [result]);

                }).fail(function () {
                    deferred.reject();
                });
            };

            // Read in the image file as a data URL.
            reader.readAsDataURL(file);

            return deferred.promise();
        };

        /**
         * Gets the list of installed plugins on the server
         */
        self.getInstalledPlugins = function () {

            var url = self.getUrl("Plugins");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets a user by id
         * @param {String} id
         */
        self.getUser = function (id) {

            if (!id) {
                throw new Error("Must supply a userId");
            }

            var url = self.getUrl("Users/" + id);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets a studio
         */
        self.getStudio = function (name, userId) {

            if (!name) {
                throw new Error("null name");
            }

            var options = {};

            if (userId) {
                options.userId = userId;
            }

            var url = self.getUrl("Studios/" + self.encodeName(name), options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets a genre
         */
        self.getGenre = function (name, userId) {

            if (!name) {
                throw new Error("null name");
            }

            var options = {};

            if (userId) {
                options.userId = userId;
            }

            var url = self.getUrl("Genres/" + self.encodeName(name), options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getMusicGenre = function (name, userId) {

            if (!name) {
                throw new Error("null name");
            }

            var options = {};

            if (userId) {
                options.userId = userId;
            }

            var url = self.getUrl("MusicGenres/" + self.encodeName(name), options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getGameGenre = function (name, userId) {

            if (!name) {
                throw new Error("null name");
            }

            var options = {};

            if (userId) {
                options.userId = userId;
            }

            var url = self.getUrl("GameGenres/" + self.encodeName(name), options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets an artist
         */
        self.getArtist = function (name, userId) {

            if (!name) {
                throw new Error("null name");
            }

            var options = {};

            if (userId) {
                options.userId = userId;
            }

            var url = self.getUrl("Artists/" + self.encodeName(name), options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets a Person
         */
        self.getPerson = function (name, userId) {

            if (!name) {
                throw new Error("null name");
            }

            var options = {};

            if (userId) {
                options.userId = userId;
            }

            var url = self.getUrl("Persons/" + self.encodeName(name), options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getPublicUsers = function () {

            var url = self.getUrl("users/public");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets all users from the server
         */
        self.getUsers = function (options) {

            var url = self.getUrl("users", options || {});

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets all available parental ratings from the server
         */
        self.getParentalRatings = function () {

            var url = self.getUrl("Localization/ParentalRatings");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        function supportsWebP() {

            // TODO: Improve with http://webpjs.appspot.com/
            return $.browser.chrome;
        }

        function normalizeImageOptions(options) {

            var ratio = window.devicePixelRatio || 1;

            if (ratio) {

                if (options.minScale) {
                    ratio = Math.max(options.minScale, ratio);
                }

                if (options.width) {
                    options.width = Math.round(options.width * ratio);
                }
                if (options.height) {
                    options.height = Math.round(options.height * ratio);
                }
                if (options.maxWidth) {
                    options.maxWidth = Math.round(options.maxWidth * ratio);
                }
                if (options.maxHeight) {
                    options.maxHeight = Math.round(options.maxHeight * ratio);
                }
            }

            options.quality = options.quality || (options.type.toLowerCase() == 'backdrop' ? 80 : 90);

            if (supportsWebP()) {
                options.format = 'webp';
            }
        }

        /**
         * Constructs a url for a user image
         * @param {String} userId
         * @param {Object} options
         * Options supports the following properties:
         * width - download the image at a fixed width
         * height - download the image at a fixed height
         * maxWidth - download the image at a maxWidth
         * maxHeight - download the image at a maxHeight
         * quality - A scale of 0-100. This should almost always be omitted as the default will suffice.
         * For best results do not specify both width and height together, as aspect ratio might be altered.
         */
        self.getUserImageUrl = function (userId, options) {

            if (!userId) {
                throw new Error("null userId");
            }

            options = options || {

            };

            var url = "Users/" + userId + "/Images/" + options.type;

            if (options.index != null) {
                url += "/" + options.index;
            }

            normalizeImageOptions(options);

            // Don't put these on the query string
            delete options.type;
            delete options.index;

            return self.getUrl(url, options);
        };

        /**
         * Constructs a url for an item image
         * @param {String} itemId
         * @param {Object} options
         * Options supports the following properties:
         * type - Primary, logo, backdrop, etc. See the server-side enum ImageType
         * index - When downloading a backdrop, use this to specify which one (omitting is equivalent to zero)
         * width - download the image at a fixed width
         * height - download the image at a fixed height
         * maxWidth - download the image at a maxWidth
         * maxHeight - download the image at a maxHeight
         * quality - A scale of 0-100. This should almost always be omitted as the default will suffice.
         * For best results do not specify both width and height together, as aspect ratio might be altered.
         */
        self.getImageUrl = function (itemId, options) {

            if (!itemId) {
                throw new Error("itemId cannot be empty");
            }

            options = options || {};

            var url = "Items/" + itemId + "/Images/" + options.type;

            if (options.index != null) {
                url += "/" + options.index;
            }

            options.quality = options.quality || (options.type.toLowerCase() == 'backdrop' ? 80 : 90);

            // Don't put these on the query string
            delete options.type;
            delete options.index;

            if (supportsWebP()) {
                options.format = 'webp';
            }

            return self.getUrl(url, options);
        };

        self.getScaledImageUrl = function (itemId, options) {

            if (!itemId) {
                throw new Error("itemId cannot be empty");
            }

            options = options || {};

            var url = "Items/" + itemId + "/Images/" + options.type;

            if (options.index != null) {
                url += "/" + options.index;
            }

            normalizeImageOptions(options);

            // Don't put these on the query string
            delete options.type;
            delete options.index;
            delete options.minScale;

            return self.getUrl(url, options);
        };

        self.getThumbImageUrl = function (item, options) {

            if (!item) {
                throw new Error("null item");
            }

            options = options || {

            };

            options.imageType = "thumb";

            var itemId = item.ImageTags && item.ImageTags.Thumb ? item.Id : item.ParentThumbItemId;

            return itemId ? self.getImageUrl(itemId, options) : null;
        };

        /**
         * Authenticates a user
         * @param {String} name
         * @param {String} password
         */
        self.authenticateUserByName = function (name, password) {

            if (!name) {
                throw new Error("null name");
            }

            var url = self.getUrl("Users/authenticatebyname");

            var postData = {
                password: MediaBrowser.SHA1(password || ""),
                Username: name
            };

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(postData),
                dataType: "json",
                contentType: "application/json"
            });
        };

        /**
         * Authenticates a user
         * @param {String} userId
         * @param {String} password
         */
        self.authenticateUser = function (userId, password) {

            if (!userId) {
                throw new Error("null userId");
            }

            var url = self.getUrl("Users/" + userId + "/authenticate");

            var postData = {
                password: MediaBrowser.SHA1(password || "")
            };

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(postData),
                dataType: "json",
                contentType: "application/json"
            });
        };

        /**
         * Updates a user's password
         * @param {String} userId
         * @param {String} currentPassword
         * @param {String} newPassword
         */
        self.updateUserPassword = function (userId, currentPassword, newPassword) {

            if (!userId) {
                throw new Error("null userId");
            }

            var url = self.getUrl("Users/" + userId + "/Password");

            var postData = {

            };

            postData.currentPassword = MediaBrowser.SHA1(currentPassword);

            if (newPassword) {
                postData.newPassword = newPassword;
            }

            return self.ajax({
                type: "POST",
                url: url,
                data: postData
            });
        };

        /**
        * Resets a user's password
        * @param {String} userId
        */
        self.resetUserPassword = function (userId) {

            if (!userId) {
                throw new Error("null userId");
            }

            var url = self.getUrl("Users/" + userId + "/Password");

            var postData = {

            };

            postData.resetPassword = true;

            return self.ajax({
                type: "POST",
                url: url,
                data: postData
            });
        };

        /**
         * Updates the server's configuration
         * @param {Object} configuration
         */
        self.updateServerConfiguration = function (configuration) {

            if (!configuration) {
                throw new Error("null configuration");
            }

            var url = self.getUrl("System/Configuration");

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(configuration),
                contentType: "application/json"
            });
        };

        self.updateNamedConfiguration = function (name, configuration) {

            if (!configuration) {
                throw new Error("null configuration");
            }

            var url = self.getUrl("System/Configuration/" + name);

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(configuration),
                contentType: "application/json"
            });
        };

        self.updateItem = function (item) {

            if (!item) {
                throw new Error("null item");
            }

            var url = self.getUrl("Items/" + item.Id);

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(item),
                contentType: "application/json"
            });
        };

        /**
         * Updates plugin security info
         */
        self.updatePluginSecurityInfo = function (info) {

            var url = self.getUrl("Plugins/SecurityInfo");

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(info),
                contentType: "application/json"
            });
        };

        /**
         * Creates a user
         * @param {Object} user
         */
        self.createUser = function (user) {

            if (!user) {
                throw new Error("null user");
            }

            var url = self.getUrl("Users");

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(user),
                dataType: "json",
                contentType: "application/json"
            });
        };

        /**
         * Updates a user
         * @param {Object} user
         */
        self.updateUser = function (user) {

            if (!user) {
                throw new Error("null user");
            }

            var url = self.getUrl("Users/" + user.Id);

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(user),
                contentType: "application/json"
            });
        };

        /**
         * Updates the Triggers for a ScheduledTask
         * @param {String} id
         * @param {Object} triggers
         */
        self.updateScheduledTaskTriggers = function (id, triggers) {

            if (!id) {
                throw new Error("null id");
            }

            if (!triggers) {
                throw new Error("null triggers");
            }

            var url = self.getUrl("ScheduledTasks/" + id + "/Triggers");

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(triggers),
                contentType: "application/json"
            });
        };

        /**
         * Updates a plugin's configuration
         * @param {String} Id
         * @param {Object} configuration
         */
        self.updatePluginConfiguration = function (id, configuration) {

            if (!id) {
                throw new Error("null Id");
            }

            if (!configuration) {
                throw new Error("null configuration");
            }

            var url = self.getUrl("Plugins/" + id + "/Configuration");

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(configuration),
                contentType: "application/json"
            });
        };

        self.getAncestorItems = function (itemId, userId) {

            if (!itemId) {
                throw new Error("null itemId");
            }

            var options = {};

            if (userId) {
                options.userId = userId;
            }

            var url = self.getUrl("Items/" + itemId + "/Ancestors", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets items based on a query, typically for children of a folder
         * @param {String} userId
         * @param {Object} options
         * Options accepts the following properties:
         * itemId - Localize the search to a specific folder (root if omitted)
         * startIndex - Use for paging
         * limit - Use to limit results to a certain number of items
         * filter - Specify one or more ItemFilters, comma delimeted (see server-side enum)
         * sortBy - Specify an ItemSortBy (comma-delimeted list see server-side enum)
         * sortOrder - ascending/descending
         * fields - additional fields to include aside from basic info. This is a comma delimited list. See server-side enum ItemFields.
         * index - the name of the dynamic, localized index function
         * dynamicSortBy - the name of the dynamic localized sort function
         * recursive - Whether or not the query should be recursive
         * searchTerm - search term to use as a filter
         */
        self.getItems = function (userId, options) {

            if (!userId) {
                throw new Error("null userId");
            }

            var url;

            if ((typeof userId).toString().toLowerCase() == 'string') {
                url = self.getUrl("Users/" + userId + "/Items", options);
            } else {
                options = userId;
                url = self.getUrl("Items", options || {});
            }

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getUserViews = function (userId, options) {

            if (!userId) {
                throw new Error("null userId");
            }

            options = options || {};

            var url = self.getUrl("Users/" + userId + "/Views", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
            Gets artists from an item
        */
        self.getArtists = function (userId, options) {

            if (!userId) {
                throw new Error("null userId");
            }

            options = options || {};
            options.userId = userId;

            var url = self.getUrl("Artists", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
            Gets artists from an item
        */
        self.getAlbumArtists = function (userId, options) {

            if (!userId) {
                throw new Error("null userId");
            }

            options = options || {};
            options.userId = userId;

            var url = self.getUrl("Artists/AlbumArtists", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
            Gets genres from an item
        */
        self.getGenres = function (userId, options) {

            if (!userId) {
                throw new Error("null userId");
            }

            options = options || {};
            options.userId = userId;

            var url = self.getUrl("Genres", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getMusicGenres = function (userId, options) {

            if (!userId) {
                throw new Error("null userId");
            }

            options = options || {};
            options.userId = userId;

            var url = self.getUrl("MusicGenres", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getGameGenres = function (userId, options) {

            if (!userId) {
                throw new Error("null userId");
            }

            options = options || {};
            options.userId = userId;

            var url = self.getUrl("GameGenres", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
            Gets people from an item
        */
        self.getPeople = function (userId, options) {

            if (!userId) {
                throw new Error("null userId");
            }

            options = options || {};
            options.userId = userId;

            var url = self.getUrl("Persons", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
            Gets studios from an item
        */
        self.getStudios = function (userId, options) {

            if (!userId) {
                throw new Error("null userId");
            }

            options = options || {};
            options.userId = userId;

            var url = self.getUrl("Studios", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets local trailers for an item
         */
        self.getLocalTrailers = function (userId, itemId) {

            if (!userId) {
                throw new Error("null userId");
            }
            if (!itemId) {
                throw new Error("null itemId");
            }

            var url = self.getUrl("Users/" + userId + "/Items/" + itemId + "/LocalTrailers");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getAdditionalVideoParts = function (userId, itemId) {

            if (!itemId) {
                throw new Error("null itemId");
            }

            var options = {};

            if (userId) {
                options.userId = userId;
            }

            var url = self.getUrl("Videos/" + itemId + "/AdditionalParts", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getThemeMedia = function (userId, itemId, inherit) {

            if (!itemId) {
                throw new Error("null itemId");
            }

            var options = {};

            if (userId) {
                options.userId = userId;
            }

            options.InheritFromParent = inherit || false;

            var url = self.getUrl("Items/" + itemId + "/ThemeMedia", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getSearchHints = function (options) {

            var url = self.getUrl("Search/Hints", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Gets special features for an item
         */
        self.getSpecialFeatures = function (userId, itemId) {

            if (!userId) {
                throw new Error("null userId");
            }
            if (!itemId) {
                throw new Error("null itemId");
            }

            var url = self.getUrl("Users/" + userId + "/Items/" + itemId + "/SpecialFeatures");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.getDateParamValue = function (date) {

            function formatDigit(i) {
                return i < 10 ? "0" + i : i;
            }

            var d = date;

            return "" + d.getFullYear() + formatDigit(d.getMonth() + 1) + formatDigit(d.getDate()) + formatDigit(d.getHours()) + formatDigit(d.getMinutes()) + formatDigit(d.getSeconds());
        };

        self.markPlayed = function (userId, itemId, date) {

            if (!userId) {
                throw new Error("null userId");
            }

            if (!itemId) {
                throw new Error("null itemId");
            }

            var options = {};

            if (date) {
                options.DatePlayed = self.getDateParamValue(date);
            }

            var url = self.getUrl("Users/" + userId + "/PlayedItems/" + itemId, options);

            return self.ajax({
                type: "POST",
                url: url,
                dataType: "json"
            });
        };

        self.markUnplayed = function (userId, itemId) {

            if (!userId) {
                throw new Error("null userId");
            }

            if (!itemId) {
                throw new Error("null itemId");
            }

            var url = self.getUrl("Users/" + userId + "/PlayedItems/" + itemId);

            return self.ajax({
                type: "DELETE",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Updates a user's favorite status for an item.
         * @param {String} userId
         * @param {String} itemId
         * @param {Boolean} isFavorite
         */
        self.updateFavoriteStatus = function (userId, itemId, isFavorite) {

            if (!userId) {
                throw new Error("null userId");
            }

            if (!itemId) {
                throw new Error("null itemId");
            }

            var url = self.getUrl("Users/" + userId + "/FavoriteItems/" + itemId);

            var method = isFavorite ? "POST" : "DELETE";

            return self.ajax({
                type: method,
                url: url,
                dataType: "json"
            });
        };

        /**
         * Updates a user's personal rating for an item
         * @param {String} userId
         * @param {String} itemId
         * @param {Boolean} likes
         */
        self.updateUserItemRating = function (userId, itemId, likes) {

            if (!userId) {
                throw new Error("null userId");
            }

            if (!itemId) {
                throw new Error("null itemId");
            }

            var url = self.getUrl("Users/" + userId + "/Items/" + itemId + "/Rating", {
                likes: likes
            });

            return self.ajax({
                type: "POST",
                url: url,
                dataType: "json"
            });
        };

        self.getItemCounts = function (userId) {

            var options = {};

            if (userId) {
                options.userId = userId;
            }

            var url = self.getUrl("Items/Counts", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Clears a user's personal rating for an item
         * @param {String} userId
         * @param {String} itemId
         */
        self.clearUserItemRating = function (userId, itemId) {

            if (!userId) {
                throw new Error("null userId");
            }

            if (!itemId) {
                throw new Error("null itemId");
            }

            var url = self.getUrl("Users/" + userId + "/Items/" + itemId + "/Rating");

            return self.ajax({
                type: "DELETE",
                url: url,
                dataType: "json"
            });
        };

        /**
         * Reports the user has started playing something
         * @param {String} userId
         * @param {String} itemId
         */
        self.reportPlaybackStart = function (options) {

            if (!options) {
                throw new Error("null options");
            }

            if (self.isWebSocketOpen()) {

                var deferred = $.Deferred();

                var msg = JSON.stringify(options);

                self.sendWebSocketMessage("ReportPlaybackStart", msg);

                deferred.resolveWith(null, []);
                return deferred.promise();
            }

            var url = self.getUrl("Sessions/Playing");

            return self.ajax({
                type: "POST",
                data: JSON.stringify(options),
                contentType: "application/json",
                url: url
            });
        };

        /**
         * Reports progress viewing an item
         * @param {String} userId
         * @param {String} itemId
         */
        self.reportPlaybackProgress = function (options) {

            if (!options) {
                throw new Error("null options");
            }

            if (self.isWebSocketOpen()) {

                var deferred = $.Deferred();

                var msg = JSON.stringify(options);

                self.sendWebSocketMessage("ReportPlaybackProgress", msg);

                deferred.resolveWith(null, []);
                return deferred.promise();
            }

            var url = self.getUrl("Sessions/Playing/Progress");

            return self.ajax({
                type: "POST",
                data: JSON.stringify(options),
                contentType: "application/json",
                url: url
            });
        };

        /**
         * Reports a user has stopped playing an item
         * @param {String} userId
         * @param {String} itemId
         */
        self.reportPlaybackStopped = function (options) {

            if (!options) {
                throw new Error("null options");
            }

            if (self.isWebSocketOpen()) {

                var deferred = $.Deferred();

                var msg = JSON.stringify(options);

                self.sendWebSocketMessage("ReportPlaybackStopped", msg);

                deferred.resolveWith(null, []);
                return deferred.promise();
            }

            var url = self.getUrl("Sessions/Playing/Stopped");

            return self.ajax({
                type: "POST",
                data: JSON.stringify(options),
                contentType: "application/json",
                url: url
            });
        };

        self.sendPlayCommand = function (sessionId, options) {

            if (!sessionId) {
                throw new Error("null sessionId");
            }

            if (!options) {
                throw new Error("null options");
            }

            var url = self.getUrl("Sessions/" + sessionId + "/Playing", options);

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        self.sendCommand = function (sessionId, command) {

            if (!sessionId) {
                throw new Error("null sessionId");
            }

            if (!command) {
                throw new Error("null command");
            }

            var url = self.getUrl("Sessions/" + sessionId + "/Command");

            var ajaxOptions = {
                type: "POST",
                url: url
            };

            ajaxOptions.data = JSON.stringify(command);
            ajaxOptions.contentType = "application/json";

            return self.ajax(ajaxOptions);
        };

        self.sendMessageCommand = function (sessionId, options) {

            if (!sessionId) {
                throw new Error("null sessionId");
            }

            if (!options) {
                throw new Error("null options");
            }

            var url = self.getUrl("Sessions/" + sessionId + "/Message", options);

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        self.sendPlayStateCommand = function (sessionId, command, options) {

            if (!sessionId) {
                throw new Error("null sessionId");
            }

            if (!command) {
                throw new Error("null command");
            }

            var url = self.getUrl("Sessions/" + sessionId + "/Playing/" + command, options || {});

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        self.createPackageReview = function (review) {

            var url = self.getUrl("Packages/Reviews/" + review.id, review);

            return self.ajax({
                type: "POST",
                url: url,
            });
        };

        self.getPackageReviews = function (packageId, minRating, maxRating, limit) {

            if (!packageId) {
                throw new Error("null packageId");
            }

            var options = {};

            if (minRating) {
                options.MinRating = minRating;
            }
            if (maxRating) {
                options.MaxRating = maxRating;
            }
            if (limit) {
                options.Limit = limit;
            }

            var url = self.getUrl("Packages/" + packageId + "/Reviews", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };


    };

}(jQuery, navigator, window.JSON, window.WebSocket, setTimeout, window, window.FileReader);

(function (store) {

    MediaBrowser.ApiClient.generateDeviceId = function() {

        var keys = [];

        keys.push(navigator.userAgent);
        keys.push((navigator.cpuClass || ""));

        var randomId = '';

        //  Since the above is not guaranteed to be unique per device, add a little more
        randomId = store.getItem('randomId');

        if (!randomId) {

            randomId = new Date().getTime();

            store.setItem('randomId', randomId.toString());
        }

        keys.push(randomId);
        return MediaBrowser.SHA1(keys.join('|'));
    };

    MediaBrowser.ApiClient.generateDeviceName = function () {

        var name = "Web Browser";

        if ($.browser.chrome) {
            name = "Chrome";
        } else if ($.browser.safari) {
            name = "Safari";
        } else if ($.browser.webkit) {
            name = "WebKit";
        } else if ($.browser.msie) {
            name = "Internet Explorer";
        } else if ($.browser.opera) {
            name = "Opera";
        } else if ($.browser.firefox || $.browser.mozilla) {
            name = "Firefox";
        }

        if ($.browser.version) {
            name += " " + $.browser.version;
        }

        if ($.browser.ipad) {
            name += " Ipad";
        } else if ($.browser.iphone) {
            name += " Iphone";
        } else if ($.browser.android) {
            name += " Android";
        }
        return name;
    };

})(window.store);

/**
 * Provides a friendly way to create an api client instance using information from the browser's current url
 */
MediaBrowser.ApiClient.create = function (clientName, applicationVersion, deviceName, deviceId) {

    var loc = window.location;

    var address = loc.protocol + '//' + loc.hostname;

    if (loc.port) {
        address += ':' + loc.port;
    }

    return new MediaBrowser.ApiClient(address, clientName, applicationVersion, deviceName, deviceId);
};

/**
*
*  Secure Hash Algorithm (SHA1)
*  http://www.webtoolkit.info/
*
**/
MediaBrowser.SHA1 = function (msg) {

    function rotate_left(n, s) {
        var t4 = (n << s) | (n >>> (32 - s));
        return t4;
    }

    function lsb_hex(val) {
        var str = "";
        var i;
        var vh;
        var vl;

        for (i = 0; i <= 6; i += 2) {
            vh = (val >>> (i * 4 + 4)) & 0x0f;
            vl = (val >>> (i * 4)) & 0x0f;
            str += vh.toString(16) + vl.toString(16);
        }
        return str;
    }

    function cvt_hex(val) {
        var str = "";
        var i;
        var v;

        for (i = 7; i >= 0; i--) {
            v = (val >>> (i * 4)) & 0x0f;
            str += v.toString(16);
        }
        return str;
    }

    function Utf8Encode(string) {
        string = string.replace(/\r\n/g, "\n");
        var utftext = "";

        for (var n = 0; n < string.length; n++) {

            var c = string.charCodeAt(n);

            if (c < 128) {
                utftext += String.fromCharCode(c);
            }
            else if ((c > 127) && (c < 2048)) {
                utftext += String.fromCharCode((c >> 6) | 192);
                utftext += String.fromCharCode((c & 63) | 128);
            }
            else {
                utftext += String.fromCharCode((c >> 12) | 224);
                utftext += String.fromCharCode(((c >> 6) & 63) | 128);
                utftext += String.fromCharCode((c & 63) | 128);
            }

        }

        return utftext;
    }

    var blockstart;
    var i, j;
    var W = new Array(80);
    var H0 = 0x67452301;
    var H1 = 0xEFCDAB89;
    var H2 = 0x98BADCFE;
    var H3 = 0x10325476;
    var H4 = 0xC3D2E1F0;
    var A, B, C, D, E;
    var temp;

    msg = Utf8Encode(msg);

    var msg_len = msg.length;

    var word_array = new Array();
    for (i = 0; i < msg_len - 3; i += 4) {
        j = msg.charCodeAt(i) << 24 | msg.charCodeAt(i + 1) << 16 |
        msg.charCodeAt(i + 2) << 8 | msg.charCodeAt(i + 3);
        word_array.push(j);
    }

    switch (msg_len % 4) {
        case 0:
            i = 0x080000000;
            break;
        case 1:
            i = msg.charCodeAt(msg_len - 1) << 24 | 0x0800000;
            break;

        case 2:
            i = msg.charCodeAt(msg_len - 2) << 24 | msg.charCodeAt(msg_len - 1) << 16 | 0x08000;
            break;

        case 3:
            i = msg.charCodeAt(msg_len - 3) << 24 | msg.charCodeAt(msg_len - 2) << 16 | msg.charCodeAt(msg_len - 1) << 8 | 0x80;
            break;
    }

    word_array.push(i);

    while ((word_array.length % 16) != 14) word_array.push(0);

    word_array.push(msg_len >>> 29);
    word_array.push((msg_len << 3) & 0x0ffffffff);


    for (blockstart = 0; blockstart < word_array.length; blockstart += 16) {

        for (i = 0; i < 16; i++) W[i] = word_array[blockstart + i];
        for (i = 16; i <= 79; i++) W[i] = rotate_left(W[i - 3] ^ W[i - 8] ^ W[i - 14] ^ W[i - 16], 1);

        A = H0;
        B = H1;
        C = H2;
        D = H3;
        E = H4;

        for (i = 0; i <= 19; i++) {
            temp = (rotate_left(A, 5) + ((B & C) | (~B & D)) + E + W[i] + 0x5A827999) & 0x0ffffffff;
            E = D;
            D = C;
            C = rotate_left(B, 30);
            B = A;
            A = temp;
        }

        for (i = 20; i <= 39; i++) {
            temp = (rotate_left(A, 5) + (B ^ C ^ D) + E + W[i] + 0x6ED9EBA1) & 0x0ffffffff;
            E = D;
            D = C;
            C = rotate_left(B, 30);
            B = A;
            A = temp;
        }

        for (i = 40; i <= 59; i++) {
            temp = (rotate_left(A, 5) + ((B & C) | (B & D) | (C & D)) + E + W[i] + 0x8F1BBCDC) & 0x0ffffffff;
            E = D;
            D = C;
            C = rotate_left(B, 30);
            B = A;
            A = temp;
        }

        for (i = 60; i <= 79; i++) {
            temp = (rotate_left(A, 5) + (B ^ C ^ D) + E + W[i] + 0xCA62C1D6) & 0x0ffffffff;
            E = D;
            D = C;
            C = rotate_left(B, 30);
            B = A;
            A = temp;
        }

        H0 = (H0 + A) & 0x0ffffffff;
        H1 = (H1 + B) & 0x0ffffffff;
        H2 = (H2 + C) & 0x0ffffffff;
        H3 = (H3 + D) & 0x0ffffffff;
        H4 = (H4 + E) & 0x0ffffffff;

    }

    var temp = cvt_hex(H0) + cvt_hex(H1) + cvt_hex(H2) + cvt_hex(H3) + cvt_hex(H4);

    return temp.toLowerCase();
};

(function (jQuery, window, undefined) {
    "use strict";

    var matched, browser;

    jQuery.uaMatch = function (ua) {
        ua = ua.toLowerCase();

        var match = /(chrome)[ \/]([\w.]+)/.exec(ua) ||
            /(webkit)[ \/]([\w.]+)/.exec(ua) ||
            /(opera)(?:.*version|)[ \/]([\w.]+)/.exec(ua) ||
            /(msie) ([\w.]+)/.exec(ua) ||
            ua.indexOf("compatible") < 0 && /(mozilla)(?:.*? rv:([\w.]+)|)/.exec(ua) ||
            [];

        var platform_match = /(ipad)/.exec(ua) ||
            /(iphone)/.exec(ua) ||
            /(android)/.exec(ua) ||
            [];

        var browser = match[1] || "";

        if (ua.indexOf("like gecko") != -1 && ua.indexOf('webkit') == -1 && ua.indexOf('opera') == -1) {
            browser = "msie";
        }

        return {
            browser: browser,
            version: match[2] || "0",
            platform: platform_match[0] || ""
        };
    };

    matched = jQuery.uaMatch(window.navigator.userAgent);
    browser = {};

    if (matched.browser) {
        browser[matched.browser] = true;
        browser.version = matched.version;
    }

    if (matched.platform) {
        browser[matched.platform] = true;
    }

    // Chrome is Webkit, but Webkit is also Safari.
    if (browser.chrome) {
        browser.webkit = true;
    } else if (browser.webkit) {
        browser.safari = true;
    }

    browser.mobile = (/Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent));

    jQuery.browser = browser;

})(jQuery, window);