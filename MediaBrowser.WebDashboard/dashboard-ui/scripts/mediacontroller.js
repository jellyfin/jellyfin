(function ($, window) {

    function mediaController() {

        var self = this;
        var currentPlayer;

        var players = [];

        self.registerPlayer = function (player) {

            players.push(player);

            if (!currentPlayer) {
                currentPlayer = player;
            }
        };

        self.play = function (options) {

            if (typeof (options) === 'string') {
                options = { ids: [options] };
            }

            currentPlayer.play(options);
        };

        self.shuffle = function (id) {
            
            currentPlayer.shuffle(id);
        };

        self.instantMix = function (id) {
            currentPlayer.instantMix(id);
        };

        self.queue = function (options) {

            if (typeof (options) === 'string') {
                options = { ids: [options] };
            }

            currentPlayer.queue(options);
        };

        self.queueNext = function (options) {

            if (typeof (options) === 'string') {
                options = { ids: [options] };
            }

            currentPlayer.queueNext(options);
        };

        self.canPlay = function (item) {

            if (item.PlayAccess != 'Full') {
                return false;
            }

            if (item.LocationType == "Virtual" || item.IsPlaceHolder) {
                return false;
            }

            if (item.IsFolder || item.Type == "MusicGenre") {
                return true;
            }

            return currentPlayer.canPlayMediaType(item.MediaType);
        };

        self.canQueueMediaType = function (mediaType) {

            return currentPlayer.canQueueMediaType(mediaType);
        };

        self.isPlaying = function () {

            return currentPlayer.isPlaying();
        };

        self.getLocalPlayer = function () {
            
            return currentPlayer.isLocalPlayer ?
                
                currentPlayer :
                
                players.filter(function (p) {
                    return p.isLocalPlayer;
                })[0];
        };
    }

    window.MediaController = new mediaController();

    function onWebSocketMessageReceived() {

        var msg = data;

        var localPlayer = msg.MessageType === "Play" || msg.MessageType === "Play" ?
            MediaController.getLocalPlayer() :
            null;

        if (msg.MessageType === "Play") {

            if (msg.Data.PlayCommand == "PlayNext") {
                localPlayer.queueNext({ ids: msg.Data.ItemIds });
            }
            else if (msg.Data.PlayCommand == "PlayLast") {
                localPlayer.queue({ ids: msg.Data.ItemIds });
            }
            else {
                localPlayer.play({ ids: msg.Data.ItemIds, startPositionTicks: msg.Data.StartPositionTicks });
            }

        }
        else if (msg.MessageType === "Playstate") {

            if (msg.Data.Command === 'Stop') {
                localPlayer.stop();
            }
            else if (msg.Data.Command === 'Pause') {
                localPlayer.pause();
            }
            else if (msg.Data.Command === 'Unpause') {
                localPlayer.unpause();
            }
            else if (msg.Data.Command === 'Seek') {
                localPlayer.seek(msg.Data.SeekPositionTicks);
            }
            else if (msg.Data.Command === 'NextTrack') {
                localPlayer.nextTrack();
            }
            else if (msg.Data.Command === 'PreviousTrack') {
                localPlayer.previousTrack();
            }
            else if (msg.Data.Command === 'Fullscreen') {
                localPlayer.remoteFullscreen();
            }
        }
    }

    $(ApiClient).on("websocketmessage", onWebSocketMessageReceived);

})(jQuery, window);