(function () {

    function getLocalMediaSource(serverId, itemId) {
        return null;
    }

    function saveOfflineUser(user) {
        var deferred = DeferredBuilder.Deferred();
        deferred.resolve();
        return deferred.promise();
    }

    function getCameraPhotos() {
        var deferred = DeferredBuilder.Deferred();
        deferred.resolveWith(null, [[]]);
        return deferred.promise();
    }

    window.LocalAssetManager = {
        getLocalMediaSource: getLocalMediaSource,
        saveOfflineUser: saveOfflineUser,
        getCameraPhotos: getCameraPhotos
    };

})();