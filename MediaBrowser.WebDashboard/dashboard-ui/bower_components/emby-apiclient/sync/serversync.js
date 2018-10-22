define([], function() {
    "use strict";

    function performSync(connectionManager, server, options) {
        console.log("ServerSync.performSync to server: " + server.Id), options = options || {};
        var cameraUploadServers = options.cameraUploadServers || [];
        console.log("ServerSync cameraUploadServers: " + JSON.stringify(cameraUploadServers));
        var uploadPhotos = -1 !== cameraUploadServers.indexOf(server.Id);
        return console.log("ServerSync uploadPhotos: " + uploadPhotos), (uploadPhotos ? uploadContent(connectionManager, server, options) : Promise.resolve()).then(function() {
            return syncMedia(connectionManager, server, options)
        })
    }

    function uploadContent(connectionManager, server, options) {
        return new Promise(function(resolve, reject) {
            require(["contentuploader"], function(ContentUploader) {
                (new ContentUploader).uploadImages(connectionManager, server).then(resolve, reject)
            })
        })
    }

    function syncMedia(connectionManager, server, options) {
        return new Promise(function(resolve, reject) {
            require(["mediasync"], function(MediaSync) {
                var apiClient = connectionManager.getApiClient(server.Id);
                (new MediaSync).sync(apiClient, server, options).then(resolve, reject)
            })
        })
    }

    function ServerSync() {}
    return ServerSync.prototype.sync = function(connectionManager, server, options) {
        if (!server.AccessToken && !server.ExchangeToken) return console.log("Skipping sync to server " + server.Id + " because there is no saved authentication information."), Promise.resolve();
        var connectionOptions = {
            updateDateLastAccessed: !1,
            enableWebSocket: !1,
            reportCapabilities: !1,
            enableAutomaticBitrateDetection: !1
        };
        return connectionManager.connectToServer(server, connectionOptions).then(function(result) {
            return "SignedIn" === result.State ? performSync(connectionManager, server, options) : (console.log("Unable to connect to server id: " + server.Id), Promise.reject())
        }, function(err) {
            throw console.log("Unable to connect to server id: " + server.Id), err
        })
    }, ServerSync
});