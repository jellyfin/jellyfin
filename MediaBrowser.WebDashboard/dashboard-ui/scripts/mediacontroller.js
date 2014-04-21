(function ($, window) {

    var enableMirrorMode;
    var currentDisplayInfo;

    function mirrorItem(info) {

        var item = info.item;

        MediaController.getCurrentPlayer().displayContent({

            itemName: item.Name,
            itemId: item.Id,
            itemType: item.Type,
            context: info.context
        });
    }

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
                id: currentTargetInfo.id,
                deviceName: currentTargetInfo.deviceName,
                playableMediaTypes: currentTargetInfo.playableMediaTypes,
                supportedCommands: currentTargetInfo.supportedCommands
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

            currentPlayer = player;
            currentTargetInfo = targetInfo || player.getCurrentTargetInfo();

            $(self).trigger('playerchange');
        };

        self.setDefaultPlayerActive = function () {
            self.setActivePlayer(self.getDefaultPlayer());
        };

        self.removeActivePlayer = function (name) {

            if (self.getPlayerInfo().name == name) {
                self.setDefaultPlayerActive();
            }

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

                targets = targets.sort(function (a, b) {

                    var aVal = a.isLocalPlayer ? 0 : 1;
                    var bVal = b.isLocalPlayer ? 0 : 1;

                    aVal = aVal.toString() + a.name;
                    bVal = bVal.toString() + b.name;

                    return aVal.localeCompare(bVal);
                });

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

            return self.getPlayerInfo().playableMediaTypes.indexOf(item.MediaType) != -1;
        };

        self.canQueueMediaType = function (mediaType) {

            return currentPlayer.canQueueMediaType(mediaType);
        };

        self.getLocalPlayer = function () {

            return currentPlayer.isLocalPlayer ?

                currentPlayer :

                players.filter(function (p) {
                    return p.isLocalPlayer;
                })[0];
        };

        self.getDefaultPlayer = function () {

            return currentPlayer.isLocalPlayer ?

                currentPlayer :

                players.filter(function (p) {
                    return p.isDefaultPlayer;
                })[0];
        };

        self.getCurrentPlayer = function () {

            return currentPlayer;
        };

        self.pause = function () {
            currentPlayer.pause();
        };

        self.stop = function () {
            currentPlayer.stop();
        };

        self.unpause = function () {
            currentPlayer.unpause();
        };

        self.seek = function (position) {
            currentPlayer.seek(position);
        };

        self.currentPlaylistIndex = function (i) {
            currentPlayer.currentPlaylistIndex(i);
        };

        self.removeFromPlaylist = function (i) {
            currentPlayer.removeFromPlaylist(i);
        };

        self.nextTrack = function () {
            currentPlayer.nextTrack();
        };

        self.previousTrack = function () {
            currentPlayer.previousTrack();
        };

        self.mute = function () {
            currentPlayer.mute();
        };

        self.unMute = function () {
            currentPlayer.unMute();
        };

        self.toggleMute = function () {
            currentPlayer.toggleMute();
        };

        self.volumeDown = function () {
            currentPlayer.volumeDown();
        };

        self.volumeUp = function () {
            currentPlayer.volumeUp();
        };

        self.shuffle = function (id) {
            currentPlayer.shuffle(id);
        };
    }

    window.MediaController = new mediaController();

    function onWebSocketMessageReceived(e, msg) {

        var localPlayer;

        if (msg.MessageType === "Play") {

            localPlayer = MediaController.getLocalPlayer();

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
        else if (msg.MessageType === "ServerShuttingDown") {
            MediaController.setDefaultPlayerActive();
        }
        else if (msg.MessageType === "ServerRestarting") {
            MediaController.setDefaultPlayerActive();
        }
        else if (msg.MessageType === "Playstate") {

            localPlayer = MediaController.getLocalPlayer();

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
        }
        else if (msg.MessageType === "GeneralCommand") {

            var cmd = msg.Data;

            localPlayer = MediaController.getLocalPlayer();

            if (cmd.Name === 'Mute') {
                localPlayer.mute();
            }
            else if (cmd.Name === 'Unmute') {
                localPlayer.unMute();
            }
            else if (cmd.Name === 'VolumeUp') {
                localPlayer.volumeUp();
            }
            else if (cmd.Name === 'VolumeDown') {
                localPlayer.volumeDown();
            }
            else if (cmd.Name === 'ToggleMute') {
                localPlayer.toggleMute();
            }
            else if (cmd.Name === 'Fullscreen') {
                localPlayer.remoteFullscreen();
            }
            else if (cmd.Name === 'SetVolume') {
                localPlayer.setVolume(parseFloat(cmd.Arguments.Volume));
            }
        }
    }

    $(ApiClient).on("websocketmessage", onWebSocketMessageReceived);

    function getTargetsHtml(targets) {

        var playerInfo = MediaController.getPlayerInfo();

        var html = '';
        html += '<form>';

        html += '<form><h3>Select Player:</h3>';
        html += '<fieldset data-role="controlgroup" data-mini="true">';

        var checkedHtml;

        for (var i = 0, length = targets.length; i < length; i++) {

            var target = targets[i];

            var id = 'radioPlayerTarget' + i;

            var isChecked = target.id == playerInfo.id;
            checkedHtml = isChecked ? ' checked="checked"' : '';

            var mirror = (!target.isLocalPlayer && target.supportedCommands.indexOf('DisplayContent') != -1) ? 'true' : 'false';

            html += '<input type="radio" class="radioSelectPlayerTarget" name="radioSelectPlayerTarget" data-mirror="' + mirror + '" data-commands="' + target.supportedCommands.join(',') + '" data-mediatypes="' + target.playableMediaTypes.join(',') + '" data-playername="' + target.playerName + '" data-targetid="' + target.id + '" data-targetname="' + target.name + '" id="' + id + '" value="' + target.id + '"' + checkedHtml + '>';
            html += '<label for="' + id + '" style="font-weight:normal;">' + target.name;

            if (target.appName) {
                html += '<br/><span style="color:#bbb;">' + target.appName + '</span>';
            }

            html += '</label>';
        }

        html += '</fieldset>';

        html += '<p class="fieldDescription">All plays will be sent to the selected player.</p>';

        checkedHtml = enableMirrorMode ? ' checked="checked"' : '';
        html += '<div style="margin-top:1.5em;" class="fldMirrorMode"><label for="chkEnableMirrorMode">Enable display mirroring</label><input type="checkbox" class="chkEnableMirrorMode" id="chkEnableMirrorMode" data-mini="true"' + checkedHtml + ' /></div>';

        html += '</form>';

        return html;
    }

    function showPlayerSelection(page) {

        var promise = MediaController.getTargets();

        var html = '<div data-role="panel" data-position="right" data-display="overlay" data-position-fixed="true" id="playerSelectionPanel" class="playerSelectionPanel" data-theme="b">';

        html += '<div class="players"></div>';

        html += '</div>';

        $(document.body).append(html);

        var elem = $('#playerSelectionPanel').panel({}).trigger('create').panel("open").on("panelafterclose", function () {

            $(this).off("panelafterclose").remove();
        });

        promise.done(function (targets) {

            $('.players', elem).html(getTargetsHtml(targets)).trigger('create');

            $('.chkEnableMirrorMode', elem).on().on('change', function () {
                enableMirrorMode = this.checked;

                if (this.checked && currentDisplayInfo) {

                    mirrorItem(currentDisplayInfo);

                }

            });

            $('.radioSelectPlayerTarget', elem).on('change', function () {

                var supportsMirror = this.getAttribute('data-mirror') == 'true';

                if (supportsMirror) {
                    $('.fldMirrorMode', elem).show();
                } else {
                    $('.fldMirrorMode', elem).hide();
                    $('.chkEnableMirrorMode', elem).checked(false).trigger('change').checkboxradio('refresh');
                }

            }).each(function () {

                if (this.checked) {
                    $(this).trigger('change');
                }

            }).on('change', function () {

                var playerName = this.getAttribute('data-playername');
                var targetId = this.getAttribute('data-targetid');
                var targetName = this.getAttribute('data-targetname');
                var playableMediaTypes = this.getAttribute('data-mediatypes').split(',');
                var supportedCommands = this.getAttribute('data-commands').split(',');

                MediaController.setActivePlayer(playerName, {
                    id: targetId,
                    name: targetName,
                    playableMediaTypes: playableMediaTypes,
                    supportedCommands: supportedCommands

                });
            });
        });
    }

    $(document).on('headercreated', ".libraryPage", function () {

        var page = this;

        $('.btnCast', page).on('click', function () {

            showPlayerSelection(page);
        });
    });

    $(document).on('pagebeforeshow', ".page", function () {

        var page = this;

        currentDisplayInfo = null;

    }).on('displayingitem', ".libraryPage", function (e, info) {

        var page = this;

        currentDisplayInfo = info;

        if (enableMirrorMode) {
            mirrorItem(info);
        }
    });

})(jQuery, window);