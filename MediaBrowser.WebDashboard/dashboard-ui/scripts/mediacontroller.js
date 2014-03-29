(function ($, window) {

    function mediaController() {

        var self = this;
        var currentPlayer;
        var currentTargetInfo;

        var players = [];

        self.registerPlayer = function (player) {

            players.push(player);
        };

        self.getPlayerInfo = function () {

            return {

                name: currentPlayer.name,
                isLocalPlayer: currentPlayer.isLocalPlayer,
                targetInfo: currentTargetInfo
            };
        };

        self.setActivePlayer = function (player, targetInfo) {

            if (typeof (player) === 'string') {
                player = players.filter(function (p) {
                    return p.name == player;
                })[0];
            }

            if (!player) {
                throw new Error('null player');
            }

            if (!targetInfo) {
                throw new Error('null targetInfo');
            }

            currentPlayer = player;
            currentTargetInfo = targetInfo;

            $(self).trigger('playerchange');
        };

        self.getTargets = function () {

            var deferred = $.Deferred();

            var promises = players.map(function (p) {
                return p.getTargets();
            });

            $.when.apply($, promises).done(function () {

                var targets = [];

                for (var i = 0; i < arguments.length; i++) {

                    var subTargets = arguments[i];

                    for (var j = 0; j < subTargets.length; j++) {

                        targets.push(subTargets[j]);
                    }

                }

                deferred.resolveWith(null, [targets]);
            });

            return deferred.promise();
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

    function onWebSocketMessageReceived(msg) {

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

    function getTargetsHtml(targets) {

        var playerInfo = MediaController.getPlayerInfo();

        var html = '';
        html += '<fieldset data-role="controlgroup" data-mini="true">';
        html += '<legend>Select Player:</legend>';

        for (var i = 0, length = targets.length; i < length; i++) {

            var target = targets[i];

            var id = 'radioPlayerTarget' + i;

            var isChecked = target.id == playerInfo.targetInfo.id;
            var checkedHtml = isChecked ? ' checked="checked"' : '';

            html += '<input type="radio" class="radioSelectPlayerTarget" name="radioSelectPlayerTarget" data-playername="' + target.playerName + '" data-targetid="' + target.id + '" data-targetname="' + target.name + '" id="' + id + '" value="' + target.id + '"' + checkedHtml + '>';
            html += '<label for="' + id + '">' + target.name + '</label>';
        }

        html += '</fieldset>';
        return html;
    }

    function showPlayerSelection() {

        var promise = MediaController.getTargets();

        var html = '<div data-role="panel" data-position="right" data-display="overlay" id="playerFlyout" data-theme="b">';

        html += '<div class="players"></div>';

        html += '</div>';

        $(document.body).append(html);

        var elem = $('#playerFlyout').panel({}).trigger('create').panel("open").on("panelafterclose", function () {

            $(this).off("panelafterclose").remove();
        });

        promise.done(function (targets) {

            $('.players', elem).html(getTargetsHtml(targets)).trigger('create');

            $('.radioSelectPlayerTarget', elem).on('change', function () {

                var playerName = this.getAttribute('data-playername');
                var targetId = this.getAttribute('data-targetid');
                var targetName = this.getAttribute('data-targetname');

                MediaController.setActivePlayer(playerName, {
                    id: targetId,
                    name: targetName

                });
            });
        });
    }

    $(document).on('headercreated', ".libraryPage", function () {

        var page = this;

        $('.btnCast', page).on('click', function () {

            showPlayerSelection();
        });
    });

})(jQuery, window);