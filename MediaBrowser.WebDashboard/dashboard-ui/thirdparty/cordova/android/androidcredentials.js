(function () {

    function updateCredentials() {

        console.log('sending updated credentials to ApiClientBridge');

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

        console.log('initNativeConnectionManager');

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
        baseAjaxMethod = AjaxApi.ajax;
        AjaxApi.ajax = sendRequest;
    }

    function sendRequest(request) {

        if (request.data || request.contentType || request.dataType != 'json') {
            return baseAjaxMethod(request);
        }

        var deferred = DeferredBuilder.Deferred();

        var id = getNewRequestId();

        request.headers = request.headers || {};

        if (request.dataType == 'json') {
            request.headers.accept = 'application/json';
        }

        var requestHeaders = [];
        for (name in request.headers) {
            requestHeaders.push(name + "=" + request.headers[name]);
        }

        ApiClientBridge.sendRequest(request.url, request.type, requestHeaders.join('|||||'), "window.AndroidAjax.onResponse", id);

        Events.on(AndroidAjax, 'response' + id, function (e, status, response) {

            Events.off(AndroidAjax, 'response' + id);

            response = decodeURIComponent(response);

            if (status >= 400) {
                alert(status);
                deferred.reject();
            }
            else if (request.dataType == 'json') {
                deferred.resolveWith(null, [JSON.parse(response)]);
            }
            else {
                deferred.resolveWith(null, [response]);
            }

        });

        return deferred.promise();
    }

    Events.on(ConnectionManager.credentialProvider(), 'credentialsupdated', updateCredentials);

    updateCredentials();
    initNativeConnectionManager();

    window.AndroidAjax = {

        onResponse: function (id, status, response) {

            Events.trigger(AndroidAjax, 'response' + id, [status, response]);
        }
    };

})();