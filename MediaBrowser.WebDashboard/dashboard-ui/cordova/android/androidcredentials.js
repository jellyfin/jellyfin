(function () {

    function updateCredentials() {

        Logger.log('sending updated credentials to ApiClientBridge');

        var json = JSON.stringify(ConnectionManager.credentialProvider().credentials());
        var credentials = JSON.parse(json);

        for (var i = 0, length = credentials.Servers.length; i < length; i++) {
            var server = credentials.Servers[i];

            if (server.DateLastAccessed != null) {
                server.DateLastAccessed = new Date(server.DateLastAccessed).toISOString();
            }
        }

        json = JSON.stringify(credentials);
        ApiClientBridge.updateCredentials(json);
    }

    function initNativeConnectionManager() {

        Logger.log('initNativeConnectionManager');

        var capabilities = ConnectionManager.capabilities();

        ApiClientBridge.init(AppInfo.appName, AppInfo.appVersion, AppInfo.deviceId, AppInfo.deviceName, JSON.stringify(capabilities));

        //initAjax();
    }

    var baseAjaxMethod;
    var currentId = 0;
    function getNewRequestId() {
        var id = currentId++;
        return id.toString();
    }
    function initAjax() {
        baseAjaxMethod = HttpClient.send;
        HttpClient.send = sendRequest;
    }

    function sendRequest(request) {

        // For now, we can only handle json responses
        if (request.dataType) {
            if (request.dataType != 'json') {
                return baseAjaxMethod(request);
            }
        }

        if (request.data) {
            // For now, we can only handle request bodies that are strings
            if (typeof (request.data) != 'string') {
                return baseAjaxMethod(request);
            }
        }

        var deferred = DeferredBuilder.Deferred();

        var id = getNewRequestId();

        request.headers = request.headers || {};

        if (request.dataType == 'json') {
            request.headers.accept = 'application/json';
        }

        var method = request.type || "GET";

        var javaRequest = {
            Method: method,
            Url: request.url,
            RequestHeaders: request.headers
        };

        if (request.timeout) {
            javaRequest.Timeout = request.timeout;
        }

        if (request.data) {
            javaRequest.RequestContent = request.data;
        }

        if (request.contentType) {
            javaRequest.RequestContentType = request.contentType;
        }

        //Logger.log("Sending request: " + JSON.stringify(javaRequest));

        ApiClientBridge.sendRequest(JSON.stringify(javaRequest), request.dataType, id);

        Events.on(AndroidAjax, 'response' + id, function (e, isSuccess, response) {

            Events.off(AndroidAjax, 'response' + id);

            if (isSuccess) {

                if (response) {
                    deferred.resolveWith(null, [response]);
                } else {
                    deferred.resolve();
                }
            }
            else {

                // Need to mimic the jquery ajax error response
                deferred.rejectWith(request, [getErrorResponse(response)]);
            }

        });

        return deferred.promise();
    }

    function getErrorResponse(response) {

        var error = {};

        if (response.StatusCode) {
            error.status = response.StatusCode;
        }

        error.ResponseHeaders = response.ResponseHeaders || {};

        error.getResponseHeader = function (name) {
            return error.ResponseHeaders[name];
        };

        return error;
    }

    function getDownloadSpeed(bytes, url) {

        var deferred = DeferredBuilder.Deferred();

        ApiClientBridge.getDownloadSpeed(bytes, url);

        Events.on(AndroidAjax, 'downloadspeedresponse', function (e, response) {

            Events.off(AndroidAjax, 'downloadspeedresponse');

            if (response) {

                deferred.resolveWith(null, [response]);
            }
            else {

                // Need to mimic the jquery ajax error response
                deferred.reject();
            }

        });

        return deferred.promise();
    }

    function initApiClient(newApiClient) {
        newApiClient.getDownloadSpeed = function (bytes) {
            return getDownloadSpeed(bytes, newApiClient.getUrl('Playback/BitrateTest', {
                api_key: newApiClient.accessToken(),
                Size: bytes
            }));
        };
    }

    Events.on(ConnectionManager, 'apiclientcreated', function (e, newApiClient) {

        initApiClient(newApiClient);
    });

    Events.on(ConnectionManager.credentialProvider(), 'credentialsupdated', updateCredentials);

    updateCredentials();
    initNativeConnectionManager();

    if (window.ApiClient) {
        initApiClient(window.ApiClient);
    }

    window.AndroidAjax = {

        onResponse: function (id, response) {

            Events.trigger(AndroidAjax, 'response' + id, [true, response]);
        },
        onError: function (id, response) {

            Events.trigger(AndroidAjax, 'response' + id, [false, response]);
        },
        onDownloadSpeedResponse: function (response) {

            Events.trigger(AndroidAjax, 'downloadspeedresponse', [response]);
        }
    };

})();