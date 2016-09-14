define(['events'], function (events) {

    /**
     * Creates a new api client instance
     * @param {String} serverAddress
     * @param {String} clientName s
     * @param {String} applicationVersion 
     */
    return function (serverAddress, clientName, applicationVersion, deviceName, deviceId, devicePixelRatio) {

        if (!serverAddress) {
            throw new Error("Must supply a serverAddress");
        }

        console.log('ApiClient serverAddress: ' + serverAddress);
        console.log('ApiClient clientName: ' + clientName);
        console.log('ApiClient applicationVersion: ' + applicationVersion);
        console.log('ApiClient deviceName: ' + deviceName);
        console.log('ApiClient deviceId: ' + deviceId);

        var self = this;
        var webSocket;
        var serverInfo = {};

        /**
         * Gets the server address.
         */
        self.serverAddress = function (val) {

            if (val != null) {

                if (val.toLowerCase().indexOf('http') != 0) {
                    throw new Error('Invalid url: ' + val);
                }

                var changed = val != serverAddress;

                serverAddress = val;

                if (changed) {
                    events.trigger(this, 'serveraddresschanged');
                }
            }

            return serverAddress;
        };

        self.serverInfo = function (info) {

            serverInfo = info || serverInfo;

            return serverInfo;
        };

        self.serverId = function () {
            return self.serverInfo().Id;
        };

        var currentUser;
        /**
         * Gets or sets the current user id.
         */
        self.getCurrentUser = function () {

            if (currentUser) {
                return Promise.resolve(currentUser);
            }

            var userId = self.getCurrentUserId();

            if (!userId) {
                return Promise.reject();
            }

            return self.getUser(userId).then(function (user) {
                currentUser = user;
                return user;
            });
        };

        self.isLoggedIn = function () {

            var info = self.serverInfo();
            if (info) {
                if (info.UserId && info.AccessToken) {
                    return true;
                }
            }

            return false;
        };

        /**
         * Gets or sets the current user id.
         */
        self.getCurrentUserId = function () {

            return serverInfo.UserId;
        };

        self.accessToken = function () {
            return serverInfo.AccessToken;
        };

        self.deviceName = function () {
            return deviceName;
        };

        self.deviceId = function () {
            return deviceId;
        };

        self.appName = function () {
            return clientName;
        };

        self.appVersion = function () {
            return applicationVersion;
        };

        self.clearAuthenticationInfo = function () {
            self.setAuthenticationInfo(null, null);
        };

        self.setAuthenticationInfo = function (accessKey, userId) {
            currentUser = null;

            serverInfo.AccessToken = accessKey;
            serverInfo.UserId = userId;
        };

        self.encodeName = function (name) {

            name = name.split('/').join('-');
            name = name.split('&').join('-');
            name = name.split('?').join('-');

            var val = paramsToString({ name: name });
            return val.substring(val.indexOf('=') + 1).replace("'", '%27');
        };

        function onFetchFail(url, response) {

            events.trigger(self, 'requestfail', [
            {
                url: url,
                status: response.status,
                errorCode: response.headers ? response.headers.get('X-Application-Error-Code') : null
            }]);
        }

        self.setRequestHeaders = function (headers) {

            var currentServerInfo = self.serverInfo();

            if (clientName) {

                var auth = 'MediaBrowser Client="' + clientName + '", Device="' + deviceName + '", DeviceId="' + deviceId + '", Version="' + applicationVersion + '"';

                var userId = currentServerInfo.UserId;

                if (userId) {
                    auth += ', UserId="' + userId + '"';
                }

                headers["X-Emby-Authorization"] = auth;
            }

            var accessToken = currentServerInfo.AccessToken;

            if (accessToken) {
                headers['X-MediaBrowser-Token'] = accessToken;
            }
        };

        /**
         * Wraps around jQuery ajax methods to add additional info to the request.
         */
        self.ajax = function (request, includeAuthorization) {

            if (!request) {
                throw new Error("Request cannot be null");
            }

            return self.fetch(request, includeAuthorization);
        };

        function getFetchPromise(request) {

            var headers = request.headers || {};

            if (request.dataType == 'json') {
                headers.accept = 'application/json';
            }

            var fetchRequest = {
                headers: headers,
                method: request.type,
                credentials: 'same-origin'
            };

            var contentType = request.contentType;

            if (request.data) {

                if (typeof request.data === 'string') {
                    fetchRequest.body = request.data;
                } else {
                    fetchRequest.body = paramsToString(request.data);

                    contentType = contentType || 'application/x-www-form-urlencoded; charset=UTF-8';
                }
            }

            if (contentType) {

                headers['Content-Type'] = contentType;
            }

            if (!request.timeout) {
                return fetch(request.url, fetchRequest);
            }

            return fetchWithTimeout(request.url, fetchRequest, request.timeout);
        }

        function fetchWithTimeout(url, options, timeoutMs) {

            return new Promise(function (resolve, reject) {

                var timeout = setTimeout(reject, timeoutMs);

                options = options || {};
                options.credentials = 'same-origin';

                fetch(url, options).then(function (response) {
                    clearTimeout(timeout);
                    resolve(response);
                }, function (error) {
                    clearTimeout(timeout);
                    reject();
                });
            });
        }

        function paramsToString(params) {

            var values = [];

            for (var key in params) {

                var value = params[key];

                if (value !== null && value !== undefined && value !== '') {
                    values.push(encodeURIComponent(key) + "=" + encodeURIComponent(value));
                }
            }
            return values.join('&');
        }

        /**
         * Wraps around jQuery ajax methods to add additional info to the request.
         */
        self.fetch = function (request, includeAuthorization) {

            if (!request) {
                throw new Error("Request cannot be null");
            }

            request.headers = request.headers || {};

            if (includeAuthorization !== false) {

                self.setRequestHeaders(request.headers);
            }

            if (self.enableAutomaticNetworking === false || request.type != "GET") {
                console.log('Requesting url without automatic networking: ' + request.url);

                return getFetchPromise(request).then(function (response) {

                    if (response.status < 400) {

                        if (request.dataType == 'json' || request.headers.accept == 'application/json') {
                            return response.json();
                        } else if (request.dataType == 'text' || (response.headers.get('Content-Type') || '').toLowerCase().indexOf('text/') == 0) {
                            return response.text();
                        } else {
                            return response;
                        }
                    } else {
                        onFetchFail(request.url, response);
                        return Promise.reject(response);
                    }

                }, function (error) {
                    onFetchFail(request.url, {});
                    throw error;
                });
            }

            return self.fetchWithFailover(request, true);
        };

        self.getJSON = function (url, includeAuthorization) {

            return self.fetch({

                url: url,
                type: 'GET',
                dataType: 'json',
                headers: {
                    accept: 'application/json'
                }

            }, includeAuthorization);
        };

        function switchConnectionMode(connectionMode) {

            var currentServerInfo = self.serverInfo();
            var newConnectionMode = connectionMode;

            newConnectionMode--;
            if (newConnectionMode < 0) {
                newConnectionMode = MediaBrowser.ConnectionMode.Manual;
            }

            if (MediaBrowser.ServerInfo.getServerAddress(currentServerInfo, newConnectionMode)) {
                return newConnectionMode;
            }

            newConnectionMode--;
            if (newConnectionMode < 0) {
                newConnectionMode = MediaBrowser.ConnectionMode.Manual;
            }

            if (MediaBrowser.ServerInfo.getServerAddress(currentServerInfo, newConnectionMode)) {
                return newConnectionMode;
            }

            return connectionMode;
        }

        function tryReconnectInternal(resolve, reject, connectionMode, currentRetryCount) {

            connectionMode = switchConnectionMode(connectionMode);
            var url = MediaBrowser.ServerInfo.getServerAddress(self.serverInfo(), connectionMode);

            console.log("Attempting reconnection to " + url);

            var timeout = connectionMode == MediaBrowser.ConnectionMode.Local ? 7000 : 15000;

            fetchWithTimeout(url + "/system/info/public", {

                method: 'GET',
                accept: 'application/json'

                // Commenting this out since the fetch api doesn't have a timeout option yet
                //timeout: timeout

            }, timeout).then(function () {

                console.log("Reconnect succeeded to " + url);

                self.serverInfo().LastConnectionMode = connectionMode;
                self.serverAddress(url);

                resolve();

            }, function () {

                console.log("Reconnect attempt failed to " + url);

                if (currentRetryCount < 5) {

                    var newConnectionMode = switchConnectionMode(connectionMode);

                    setTimeout(function () {
                        tryReconnectInternal(resolve, reject, newConnectionMode, currentRetryCount + 1);
                    }, 300);

                } else {
                    reject();
                }
            });
        }

        function tryReconnect() {

            return new Promise(function (resolve, reject) {

                setTimeout(function () {
                    tryReconnectInternal(resolve, reject, self.serverInfo().LastConnectionMode, 0);
                }, 300);
            });
        }

        self.fetchWithFailover = function (request, enableReconnection) {

            console.log("Requesting " + request.url);

            request.timeout = 30000;

            return getFetchPromise(request).then(function (response) {

                if (response.status < 400) {

                    if (request.dataType == 'json' || request.headers.accept == 'application/json') {
                        return response.json();
                    } else if (request.dataType == 'text' || (response.headers.get('Content-Type') || '').toLowerCase().indexOf('text/') == 0) {
                        return response.text();
                    } else {
                        return response;
                    }
                } else {
                    onFetchFail(request.url, response);
                    return Promise.reject(response);
                }

            }, function (error) {

                console.log("Request failed to " + request.url);

                // http://api.jquery.com/jQuery.ajax/
                if (enableReconnection) {

                    console.log("Attempting reconnection");

                    var previousServerAddress = self.serverAddress();

                    return tryReconnect().then(function () {

                        console.log("Reconnect succeesed");
                        request.url = request.url.replace(previousServerAddress, self.serverAddress());

                        return self.fetchWithFailover(request, false);

                    }, function (innerError) {

                        console.log("Reconnect failed");
                        onFetchFail(request.url, {});
                        throw innerError;
                    });

                } else {

                    console.log("Reporting request failure");

                    onFetchFail(request.url, {});
                    throw error;
                }
            });
        };

        self.get = function (url) {

            return self.ajax({
                type: "GET",
                url: url
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

            if (!url) {
                throw new Error("serverAddress is yet not set");
            }
            var lowered = url.toLowerCase();
            if (lowered.indexOf('/emby') == -1 && lowered.indexOf('/mediabrowser') == -1) {
                url += '/emby';
            }

            if (name.charAt(0) != '/') {
                url += '/';
            }

            url += name;

            if (params) {
                params = paramsToString(params);
                if (params) {
                    url += "?" + params;
                }
            }

            return url;
        };

        self.updateServerInfo = function (server, connectionMode) {

            if (server == null) {
                throw new Error('server cannot be null');
            }

            if (connectionMode == null) {
                throw new Error('connectionMode cannot be null');
            }

            console.log('Begin updateServerInfo. connectionMode: ' + connectionMode);

            self.serverInfo(server);

            var serverUrl = MediaBrowser.ServerInfo.getServerAddress(server, connectionMode);

            if (!serverUrl) {
                throw new Error('serverUrl cannot be null. serverInfo: ' + JSON.stringify(server));
            }
            console.log('Setting server address to ' + serverUrl);
            self.serverAddress(serverUrl);
        };

        self.isWebSocketSupported = function () {
            try {
                return WebSocket != null;
            }
            catch (err) {
                return false;
            }
        };

        self.ensureWebSocket = function () {
            if (self.isWebSocketOpenOrConnecting() || !self.isWebSocketSupported()) {
                return;
            }

            self.openWebSocket();
        };

        function replaceAll(originalString, strReplace, strWith) {
            var reg = new RegExp(strReplace, 'ig');
            return originalString.replace(reg, strWith);
        }

        self.openWebSocket = function () {

            var accessToken = self.accessToken();

            if (!accessToken) {
                throw new Error("Cannot open web socket without access token.");
            }

            var url = self.getUrl("socket");

            url = replaceAll(url, 'emby/socket', 'embywebsocket');
            url = replaceAll(url, 'http', 'ws');

            url += "?api_key=" + accessToken;
            url += "&deviceId=" + deviceId;

            webSocket = new WebSocket(url);

            webSocket.onmessage = function (msg) {

                msg = JSON.parse(msg.data);
                onWebSocketMessage(msg);
            };

            webSocket.onopen = function () {

                console.log('web socket connection opened');
                setTimeout(function () {
                    events.trigger(self, 'websocketopen');
                }, 0);
            };
            webSocket.onerror = function () {
                events.trigger(self, 'websocketerror');
            };
            webSocket.onclose = function () {
                setTimeout(function () {
                    events.trigger(self, 'websocketclose');
                }, 0);
            };
        };

        self.closeWebSocket = function () {
            if (webSocket && webSocket.readyState === WebSocket.OPEN) {
                webSocket.close();
            }
        };

        function onWebSocketMessage(msg) {

            if (msg.MessageType === "UserDeleted") {
                currentUser = null;
            }
            else if (msg.MessageType === "UserUpdated" || msg.MessageType === "UserConfigurationUpdated") {

                var user = msg.Data;
                if (user.Id == self.getCurrentUserId()) {

                    currentUser = null;
                }
            }

            events.trigger(self, 'websocketmessage', [msg]);
        }

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

            return self.getJSON(url);
        };

        self.getDownloadSpeed = function (byteSize) {

            var url = self.getUrl('Playback/BitrateTest', {

                Size: byteSize
            });

            var now = new Date().getTime();

            return self.ajax({

                type: "GET",
                url: url,
                timeout: 5000

            }).then(function () {

                var responseTimeSeconds = (new Date().getTime() - now) / 1000;
                var bytesPerSecond = byteSize / responseTimeSeconds;
                var bitrate = Math.round(bytesPerSecond * 8);

                return bitrate;
            });
        };

        self.detectBitrate = function () {

            // First try a small amount so that we don't hang up their mobile connection
            return self.getDownloadSpeed(1000000).then(function (bitrate) {

                if (bitrate < 1000000) {
                    return Math.round(bitrate * .8);
                } else {

                    // If that produced a fairly high speed, try again with a larger size to get a more accurate result
                    return self.getDownloadSpeed(2400000).then(function (bitrate) {

                        return Math.round(bitrate * .8);
                    });
                }

            });
        };

        /**
         * Gets an item from the server
         * Omit itemId to get the root folder.
         */
        self.getItem = function (userId, itemId) {

            if (!itemId) {
                throw new Error("null itemId");
            }

            var url = userId ?
                self.getUrl("Users/" + userId + "/Items/" + itemId) :
                self.getUrl("Items/" + itemId);

            return self.getJSON(url);
        };

        /**
         * Gets the root folder from the server
         */
        self.getRootFolder = function (userId) {

            if (!userId) {
                throw new Error("null userId");
            }

            var url = self.getUrl("Users/" + userId + "/Items/Root");

            return self.getJSON(url);
        };

        self.getNotificationSummary = function (userId) {

            if (!userId) {
                throw new Error("null userId");
            }

            var url = self.getUrl("Notifications/" + userId + "/Summary");

            return self.getJSON(url);
        };

        self.getNotifications = function (userId, options) {

            if (!userId) {
                throw new Error("null userId");
            }

            var url = self.getUrl("Notifications/" + userId, options || {});

            return self.getJSON(url);
        };

        self.markNotificationsRead = function (userId, idList, isRead) {

            if (!userId) {
                throw new Error("null userId");
            }

            if (!idList) {
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

            self.closeWebSocket();

            var done = function () {
                self.setAuthenticationInfo(null, null);
            };

            if (self.accessToken()) {
                var url = self.getUrl("Sessions/Logout");

                return self.ajax({
                    type: "POST",
                    url: url

                }).then(done, done);
            }

            return new Promise(function (resolve, reject) {

                done();
                resolve();
            });
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

            return self.getJSON(url);
        };

        self.getAvailableRemoteImages = function (options) {

            if (!options) {
                throw new Error("null options");
            }

            var urlPrefix = getRemoteImagePrefix(options);

            var url = self.getUrl(urlPrefix + "/RemoteImages", options);

            return self.getJSON(url);
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

            return self.getJSON(url);
        };

        self.getLiveTvGuideInfo = function (options) {

            var url = self.getUrl("LiveTv/GuideInfo", options || {});

            return self.getJSON(url);
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

            return self.getJSON(url);
        };

        self.getLiveTvChannels = function (options) {

            var url = self.getUrl("LiveTv/Channels", options || {});

            return self.getJSON(url);
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

            return self.getJSON(url);
        };

        self.getLiveTvRecordingGroups = function (options) {

            var url = self.getUrl("LiveTv/Recordings/Groups", options || {});

            return self.getJSON(url);
        };

        self.getLiveTvRecordingGroup = function (id) {

            if (!id) {
                throw new Error("null id");
            }

            var url = self.getUrl("LiveTv/Recordings/Groups/" + id);

            return self.getJSON(url);
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

            return self.getJSON(url);
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

            return self.getJSON(url);
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

            return self.getJSON(url);
        };

        self.getLiveTvTimer = function (id) {

            if (!id) {
                throw new Error("null id");
            }

            var url = self.getUrl("LiveTv/Timers/" + id);

            return self.getJSON(url);
        };

        self.getNewLiveTvTimerDefaults = function (options) {

            options = options || {};

            var url = self.getUrl("LiveTv/Timers/Defaults", options);

            return self.getJSON(url);
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

            return self.getJSON(url);
        };

        self.getFileOrganizationResults = function (options) {

            var url = self.getUrl("Library/FileOrganization", options || {});

            return self.getJSON(url);
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

            var url = self.getUrl("Library/FileOrganizations/" + id + "/Episode/Organize");

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(options),
                contentType: 'application/json'
            });
        };

        self.getLiveTvSeriesTimer = function (id) {

            if (!id) {
                throw new Error("null id");
            }

            var url = self.getUrl("LiveTv/SeriesTimers/" + id);

            return self.getJSON(url);
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

        self.getRegistrationInfo = function (feature) {

            var url = self.getUrl("Registrations/" + feature);

            return self.getJSON(url);
        };

        /**
         * Gets the current server status
         */
        self.getSystemInfo = function () {

            var url = self.getUrl("System/Info");

            return self.getJSON(url);
        };

        /**
         * Gets the current server status
         */
        self.getPublicSystemInfo = function () {

            var url = self.getUrl("System/Info/Public");

            return self.getJSON(url, false);
        };

        self.getInstantMixFromItem = function (itemId, options) {

            var url = self.getUrl("Items/" + itemId + "/InstantMix", options);

            return self.getJSON(url);
        };

        self.getEpisodes = function (itemId, options) {

            var url = self.getUrl("Shows/" + itemId + "/Episodes", options);

            return self.getJSON(url);
        };

        self.getDisplayPreferences = function (id, userId, app) {

            var url = self.getUrl("DisplayPreferences/" + id, {
                userId: userId,
                client: app
            });

            return self.getJSON(url);
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

            return self.getJSON(url);
        };

        self.getSimilarItems = function (itemId, options) {

            var url = self.getUrl("Items/" + itemId + "/Similar", options);

            return self.getJSON(url);
        };

        /**
         * Gets all cultures known to the server
         */
        self.getCultures = function () {

            var url = self.getUrl("Localization/cultures");

            return self.getJSON(url);
        };

        /**
         * Gets all countries known to the server
         */
        self.getCountries = function () {

            var url = self.getUrl("Localization/countries");

            return self.getJSON(url);
        };

        /**
         * Gets plugin security info
         */
        self.getPluginSecurityInfo = function () {

            var url = self.getUrl("Plugins/SecurityInfo");

            return self.getJSON(url);
        };

        /**
         * Gets the directory contents of a path on the server
         */
        self.getDirectoryContents = function (path, options) {

            if (!path) {
                throw new Error("null path");
            }
            if (typeof (path) !== 'string') {
                throw new Error('invalid path');
            }

            options = options || {};

            options.path = path;

            var url = self.getUrl("Environment/DirectoryContents", options);

            return self.getJSON(url);
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

            return self.getJSON(url);
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
                url: url,
                dataType: 'text'
            });
        };

        /**
         * Gets a list of physical drives from the server
         */
        self.getDrives = function () {

            var url = self.getUrl("Environment/Drives");

            return self.getJSON(url);
        };

        /**
         * Gets a list of network devices from the server
         */
        self.getNetworkDevices = function () {

            var url = self.getUrl("Environment/NetworkDevices");

            return self.getJSON(url);
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

            return self.getJSON(url);
        };

        /**
         * Gets the latest available application update (if any)
         */
        self.getAvailableApplicationUpdate = function () {

            var url = self.getUrl("Packages/Updates", { PackageType: "System" });

            return self.getJSON(url);
        };

        /**
         * Gets the latest available plugin updates (if any)
         */
        self.getAvailablePluginUpdates = function () {

            var url = self.getUrl("Packages/Updates", { PackageType: "UserInstalled" });

            return self.getJSON(url);
        };

        /**
         * Gets the virtual folder list
         */
        self.getVirtualFolders = function () {

            var url = "Library/VirtualFolders";

            url = self.getUrl(url);

            return self.getJSON(url);
        };

        /**
         * Gets all the paths of the locations in the physical root.
         */
        self.getPhysicalPaths = function () {

            var url = self.getUrl("Library/PhysicalPaths");

            return self.getJSON(url);
        };

        /**
         * Gets the current server configuration
         */
        self.getServerConfiguration = function () {

            var url = self.getUrl("System/Configuration");

            return self.getJSON(url);
        };

        /**
         * Gets the current server configuration
         */
        self.getDevicesOptions = function () {

            var url = self.getUrl("System/Configuration/devices");

            return self.getJSON(url);
        };

        /**
         * Gets the current server configuration
         */
        self.getContentUploadHistory = function () {

            var url = self.getUrl("Devices/CameraUploads", {
                DeviceId: self.deviceId()
            });

            return self.getJSON(url);
        };

        self.getNamedConfiguration = function (name) {

            var url = self.getUrl("System/Configuration/" + name);

            return self.getJSON(url);
        };

        /**
         * Gets the server's scheduled tasks
         */
        self.getScheduledTasks = function (options) {

            options = options || {};

            var url = self.getUrl("ScheduledTasks", options);

            return self.getJSON(url);
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

            return self.getJSON(url);
        };

        self.getNextUpEpisodes = function (options) {

            var url = self.getUrl("Shows/NextUp", options);

            return self.getJSON(url);
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

            return self.getJSON(url);
        };

        /**
         * Gets a list of plugins that are available to be installed
         */
        self.getAvailablePlugins = function (options) {

            options = options || {};
            options.PackageType = "UserInstalled";

            var url = self.getUrl("Packages", options);

            return self.getJSON(url);
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
        self.addVirtualFolder = function (name, type, refreshLibrary, initialPaths, libraryOptions) {

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
                url: url,
                data: JSON.stringify({
                    Paths: initialPaths,
                    LibraryOptions: libraryOptions
                }),
                contentType: 'application/json'
            });
        };
        self.updateVirtualFolderOptions = function (id, libraryOptions) {

            if (!id) {
                throw new Error("null name");
            }

            var url = "Library/VirtualFolders/LibraryOptions";

            url = self.getUrl(url);

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify({
                    Id: id,
                    LibraryOptions: libraryOptions
                }),
                contentType: 'application/json'
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

        self.stopActiveEncodings = function (playSessionId) {

            var options = {
                deviceId: deviceId
            };

            if (playSessionId) {
                options.PlaySessionId = playSessionId;
            }

            var url = self.getUrl("Videos/ActiveEncodings", options);

            return self.ajax({
                type: "DELETE",
                url: url
            });
        };

        self.reportCapabilities = function (options) {

            var url = self.getUrl("Sessions/Capabilities/Full");

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(options),
                contentType: "application/json"
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

            return self.getJSON(url);
        };

        self.getCriticReviews = function (itemId, options) {

            if (!itemId) {
                throw new Error("null itemId");
            }

            var url = self.getUrl("Items/" + itemId + "/CriticReviews", options);

            return self.getJSON(url);
        };

        self.getSessions = function (options) {

            var url = self.getUrl("Sessions", options);

            return self.getJSON(url);
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

            return new Promise(function (resolve, reject) {

                var reader = new FileReader();

                reader.onerror = function () {
                    reject();
                };

                reader.onabort = function () {
                    reject();
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
                    }).then(function (result) {

                        resolve(result);

                    }, function () {
                        reject();
                    });
                };

                // Read in the image file as a data URL.
                reader.readAsDataURL(file);
            });
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

            return new Promise(function (resolve, reject) {

                var reader = new FileReader();

                reader.onerror = function () {
                    reject();
                };

                reader.onabort = function () {
                    reject();
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
                    }).then(function (result) {

                        resolve(result);

                    }, function () {
                        reject();
                    });
                };

                // Read in the image file as a data URL.
                reader.readAsDataURL(file);
            });
        };

        /**
         * Gets the list of installed plugins on the server
         */
        self.getInstalledPlugins = function () {

            var options = {};

            var url = self.getUrl("Plugins", options);

            return self.getJSON(url);
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

            return self.getJSON(url);
        };

        /**
         * Gets a user by id
         * @param {String} id
         */
        self.getOfflineUser = function (id) {

            if (!id) {
                throw new Error("Must supply a userId");
            }

            var url = self.getUrl("Users/" + id + "/Offline");

            return self.getJSON(url);
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

            return self.getJSON(url);
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

            return self.getJSON(url);
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

            return self.getJSON(url);
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

            return self.getJSON(url);
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

            return self.getJSON(url);
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

            return self.getJSON(url);
        };

        self.getPublicUsers = function () {

            var url = self.getUrl("users/public");

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"

            }, false);
        };

        /**
         * Gets all users from the server
         */
        self.getUsers = function (options) {

            var url = self.getUrl("users", options || {});

            return self.getJSON(url);
        };

        /**
         * Gets all available parental ratings from the server
         */
        self.getParentalRatings = function () {

            var url = self.getUrl("Localization/ParentalRatings");

            return self.getJSON(url);
        };

        self.getDefaultImageQuality = function (imageType) {
            return imageType.toLowerCase() == 'backdrop' ? 80 : 90;
        };

        function normalizeImageOptions(options) {

            var ratio = devicePixelRatio || 1;

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

            options.quality = options.quality || self.getDefaultImageQuality(options.type);

            if (self.normalizeImageOptions) {
                self.normalizeImageOptions(options);
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

            options = options || {};

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

            options.quality = options.quality || self.getDefaultImageQuality(options.type);

            if (self.normalizeImageOptions) {
                self.normalizeImageOptions(options);
            }

            // Don't put these on the query string
            delete options.type;
            delete options.index;

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

            if (item.ImageTags && item.ImageTags.Thumb) {

                options.tag = item.ImageTags.Thumb;
                return self.getImageUrl(item.Id, options);
            }
            else if (item.ParentThumbItemId) {

                options.tag = item.ImageTags.ParentThumbImageTag;
                return self.getImageUrl(item.ParentThumbItemId, options);

            } else {
                return null;
            }
        };

        /**
         * Authenticates a user
         * @param {String} name
         * @param {String} password
         */
        self.authenticateUserByName = function (name, password) {

            return new Promise(function (resolve, reject) {

                if (!name) {
                    reject();
                    return;
                }

                var url = self.getUrl("Users/authenticatebyname");

                require(["cryptojs-sha1"], function () {
                    var postData = {
                        password: CryptoJS.SHA1(password || "").toString(),
                        Username: name
                    };

                    self.ajax({
                        type: "POST",
                        url: url,
                        data: JSON.stringify(postData),
                        dataType: "json",
                        contentType: "application/json"

                    }).then(function (result) {

                        if (self.onAuthenticated) {
                            self.onAuthenticated(self, result);
                        }

                        resolve(result);

                    }, reject);
                });
            });
        };

        /**
         * Updates a user's password
         * @param {String} userId
         * @param {String} currentPassword
         * @param {String} newPassword
         */
        self.updateUserPassword = function (userId, currentPassword, newPassword) {

            return new Promise(function (resolve, reject) {

                if (!userId) {
                    reject();
                    return;
                }

                var url = self.getUrl("Users/" + userId + "/Password");

                require(["cryptojs-sha1"], function () {

                    self.ajax({
                        type: "POST",
                        url: url,
                        data: {
                            currentPassword: CryptoJS.SHA1(currentPassword).toString(),
                            newPassword: CryptoJS.SHA1(newPassword).toString()
                        }
                    }).then(resolve, reject);
                });
            });
        };

        /**
         * Updates a user's easy password
         * @param {String} userId
         * @param {String} newPassword
         */
        self.updateEasyPassword = function (userId, newPassword) {

            return new Promise(function (resolve, reject) {

                if (!userId) {
                    reject();
                    return;
                }

                var url = self.getUrl("Users/" + userId + "/EasyPassword");

                require(["cryptojs-sha1"], function () {

                    self.ajax({
                        type: "POST",
                        url: url,
                        data: {
                            newPassword: CryptoJS.SHA1(newPassword).toString()
                        }
                    }).then(resolve, reject);
                });
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

        self.resetEasyPassword = function (userId) {

            if (!userId) {
                throw new Error("null userId");
            }

            var url = self.getUrl("Users/" + userId + "/EasyPassword");

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
        self.createUser = function (name) {

            var url = self.getUrl("Users/New");

            return self.ajax({
                type: "POST",
                url: url,
                data: {
                    Name: name
                },
                dataType: "json"
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

        self.updateUserPolicy = function (userId, policy) {

            if (!userId) {
                throw new Error("null userId");
            }
            if (!policy) {
                throw new Error("null policy");
            }

            var url = self.getUrl("Users/" + userId + "/Policy");

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(policy),
                contentType: "application/json"
            });
        };

        self.updateUserConfiguration = function (userId, configuration) {

            if (!userId) {
                throw new Error("null userId");
            }
            if (!configuration) {
                throw new Error("null configuration");
            }

            var url = self.getUrl("Users/" + userId + "/Configuration");

            return self.ajax({
                type: "POST",
                url: url,
                data: JSON.stringify(configuration),
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

            return self.getJSON(url);
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

            var url;

            if ((typeof userId).toString().toLowerCase() == 'string') {
                url = self.getUrl("Users/" + userId + "/Items", options);
            } else {

                url = self.getUrl("Items", options);
            }

            return self.getJSON(url);
        };

        self.getMovieRecommendations = function (options) {

            return self.getJSON(self.getUrl('Movies/Recommendations', options));
        };

        self.getUpcomingEpisodes = function (options) {

            return self.getJSON(self.getUrl('Shows/Upcoming', options));
        };

        self.getChannels = function (query) {

            return self.getJSON(self.getUrl("Channels", query || {}));
        };

        self.getLatestChannelItems = function (query) {

            return self.getJSON(self.getUrl("Channels/Items/Latest", query));
        };

        self.getUserViews = function (options, userId) {

            options = options || {};

            var url = self.getUrl("Users/" + (userId || self.getCurrentUserId()) + "/Views", options);

            return self.getJSON(url);
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

            return self.getJSON(url);
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

            return self.getJSON(url);
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

            return self.getJSON(url);
        };

        self.getMusicGenres = function (userId, options) {

            if (!userId) {
                throw new Error("null userId");
            }

            options = options || {};
            options.userId = userId;

            var url = self.getUrl("MusicGenres", options);

            return self.getJSON(url);
        };

        self.getGameGenres = function (userId, options) {

            if (!userId) {
                throw new Error("null userId");
            }

            options = options || {};
            options.userId = userId;

            var url = self.getUrl("GameGenres", options);

            return self.getJSON(url);
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

            return self.getJSON(url);
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

            return self.getJSON(url);
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

            return self.getJSON(url);
        };

        self.getGameSystems = function () {

            var options = {};

            var userId = self.getCurrentUserId();
            if (userId) {
                options.userId = userId;
            }

            var url = self.getUrl("Games/SystemSummaries", options);

            return self.getJSON(url);
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

            return self.getJSON(url);
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

            return self.getJSON(url);
        };

        self.getSearchHints = function (options) {

            var url = self.getUrl("Search/Hints", options);

            return self.getJSON(url).then(function (result) {
                var serverId = self.serverId();
                result.SearchHints.forEach(function (i) {
                    i.ServerId = serverId;
                });
                return result;
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

            return self.getJSON(url);
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

            return self.getJSON(url);
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

                return new Promise(function (resolve, reject) {

                    var msg = JSON.stringify(options);
                    self.sendWebSocketMessage("ReportPlaybackProgress", msg);
                    resolve();
                });
            }

            var url = self.getUrl("Sessions/Playing/Progress");

            return self.ajax({
                type: "POST",
                data: JSON.stringify(options),
                contentType: "application/json",
                url: url
            });
        };

        self.reportOfflineActions = function (actions) {

            if (!actions) {
                throw new Error("null actions");
            }

            var url = self.getUrl("Sync/OfflineActions");

            return self.ajax({
                type: "POST",
                data: JSON.stringify(actions),
                contentType: "application/json",
                url: url
            });
        };

        self.syncData = function (data) {

            if (!data) {
                throw new Error("null data");
            }

            var url = self.getUrl("Sync/Data");

            return self.ajax({
                type: "POST",
                data: JSON.stringify(data),
                contentType: "application/json",
                url: url,
                dataType: "json"
            });
        };

        self.getReadySyncItems = function (deviceId) {

            if (!deviceId) {
                throw new Error("null deviceId");
            }

            var url = self.getUrl("Sync/Items/Ready", {
                TargetId: deviceId
            });

            return self.getJSON(url);
        };

        self.reportSyncJobItemTransferred = function (syncJobItemId) {

            if (!syncJobItemId) {
                throw new Error("null syncJobItemId");
            }

            var url = self.getUrl("Sync/JobItems/" + syncJobItemId + "/Transferred");

            return self.ajax({
                type: "POST",
                url: url
            });
        };

        self.cancelSyncItems = function (itemIds, targetId) {

            if (!itemIds) {
                throw new Error("null itemIds");
            }

            var url = self.getUrl("Sync/" + (targetId || self.deviceId()) + "/Items", {
                ItemIds: itemIds.join(',')
            });

            return self.ajax({
                type: "DELETE",
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

            return self.getJSON(url);
        };

        self.getSmartMatchInfos = function (options) {

            options = options || {};

            var url = self.getUrl("Library/FileOrganizations/SmartMatches", options);

            return self.ajax({
                type: "GET",
                url: url,
                dataType: "json"
            });
        };

        self.deleteSmartMatchEntries = function (entries) {

            var url = self.getUrl("Library/FileOrganizations/SmartMatches/Delete");

            var postData = {
                Entries: entries
            };

            return self.ajax({

                type: "POST",
                url: url,
                data: JSON.stringify(postData),
                contentType: "application/json"
            });
        };

        self.createPin = function () {

            return self.ajax({
                type: "POST",
                url: self.getUrl('Auth/Pin'),
                data: {
                    deviceId: self.deviceId(),
                    appName: self.appName()
                },
                dataType: "json"
            });
        };

        self.getPinStatus = function (pinInfo) {

            var queryString = {
                deviceId: pinInfo.DeviceId,
                pin: pinInfo.Pin
            };

            return self.ajax({
                type: 'GET',
                url: self.getUrl('Auth/Pin', queryString),
                dataType: 'json'
            });
        };

        function exchangePin(pinInfo) {

            return self.ajax({
                type: 'POST',
                url: self.getUrl('Auth/Pin/Exchange'),
                data: {
                    deviceId: pinInfo.DeviceId,
                    pin: pinInfo.Pin
                },
                dataType: 'json'
            });
        }

        self.exchangePin = function (pinInfo) {

            return exchangePin(pinInfo).then(function (result) {

                if (self.onAuthenticated) {
                    self.onAuthenticated(self, result);
                }

                return result;
            });
        };
    };
});