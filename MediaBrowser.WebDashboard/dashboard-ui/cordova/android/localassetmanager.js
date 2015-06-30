(function () {

    function getLocalMediaSource(serverId, itemId) {
        var json = ApiClientBridge.getLocalMediaSource(serverId, itemId);

        if (json) {
            return JSON.parse(json);
        }

        return null;
    }

    window.LocalAssetManager = {
        getLocalMediaSource: getLocalMediaSource
    };

})();