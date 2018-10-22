define(["connectionManager"], function(connectionManager) {
    "use strict";
    var isSyncing;
    return {
        sync: function(options) {
            return console.log("localSync.sync starting..."), isSyncing ? Promise.resolve() : (isSyncing = !0, new Promise(function(resolve, reject) {
                require(["multiserversync", "appSettings"], function(MultiServerSync, appSettings) {
                    options = options || {}, options.cameraUploadServers = appSettings.cameraUploadServers(), (new MultiServerSync).sync(connectionManager, options).then(function() {
                        isSyncing = null, resolve()
                    }, function(err) {
                        isSyncing = null, reject(err)
                    })
                })
            }))
        }
    }
});