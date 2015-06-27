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

        initAjax();
    }

    var baseAjaxMethod;
    var currentId = 0;
    function getNewRequestId() {
        var id = currentId++;
        return id.toString();
    }
    function initAjax() {
        baseAjaxMethod = AjaxApi.ajax;
        AjaxApi.ajax = sendRequest;
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

        var javaRequest = {
            Method: request.type || "GET",
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

        Logger.log("Sending request: " + JSON.stringify(javaRequest));

        ApiClientBridge.sendRequest(JSON.stringify(javaRequest), request.dataType, id);

        Events.on(AndroidAjax, 'response' + id, function (e, isSuccess, status, response) {

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

        error.status = response.StatusCode;
        error.ResponseHeaders = response.ResponseHeaders || {};

        error.getResponseHeader = function (name) {
            return error.ResponseHeaders[name];
        };

        return error;
    }

    Events.on(ConnectionManager.credentialProvider(), 'credentialsupdated', updateCredentials);

    updateCredentials();
    initNativeConnectionManager();

    window.AndroidAjax = {

        onResponse: function (id, status, response) {

            Events.trigger(AndroidAjax, 'response' + id, [true, status, response]);
        },
        onError: function (id, status, response) {

            Events.trigger(AndroidAjax, 'response' + id, [false, status, response]);
        }
    };

})();