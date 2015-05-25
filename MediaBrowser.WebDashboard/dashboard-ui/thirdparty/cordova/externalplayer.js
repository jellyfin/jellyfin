(function () {

    function showPlayerSelectionMenu(item, url, mimeType) {

        window.plugins.launcher.launch({
            uri: url,
            dataType: mimeType

        }, function () {

            console.log('plugin launch success');
            ExternalPlayer.onPlaybackStart();

        }, function () {

            console.log('plugin launch error');
            ExternalPlayer.onPlaybackStart();
        });
    }

    function getExternalPlayers(url, mimeType) {

        var deferred = $.Deferred();

        window.plugins.launcher.canLaunch({
            uri: url,
            dataType: mimeType,
            getAppList: true
        }, function (data) {

            console.log('plugin canLaunch succcess');
            var players = data.appList.map(function (p) {

            });
            deferred.resolveWith(null, [players]);

        }, function () {
            console.log('plugin canLaunch error');
            deferred.reject();
        });

        deferred.resolveWith(null, [players]);

        return deferred.promise();
    }

    window.ExternalPlayer.getExternalPlayers = getExternalPlayers;
    window.ExternalPlayer.showPlayerSelectionMenu = showPlayerSelectionMenu;

})();