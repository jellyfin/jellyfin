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

    Events.on(ConnectionManager.credentialProvider(), 'credentialsupdated', updateCredentials);

    updateCredentials();
    initNativeConnectionManager();

    window.AndroidAjax = {

        onResponse: function (id, status, response) {

            Events.trigger(AndroidAjax, 'response' + id, [status, response]);
        }
    };

})();