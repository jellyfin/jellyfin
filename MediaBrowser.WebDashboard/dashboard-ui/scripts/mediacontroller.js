(function ($, window, store) {

    function setMirrorModeEnabled(enabled) {

        var val = enabled ? '1' : '0';

        store.setItem('displaymirror--' + Dashboard.getCurrentUserId(), val);

    }
    function isMirrorModeEnabled() {
        return (store.getItem('displaymirror--' + Dashboard.getCurrentUserId()) || '') != '0';
    }

    var currentDisplayInfo;
    function mirrorItem(info) {

        var item = info.item;

        MediaController.getCurrentPlayer().displayContent({

            ItemName: item.Name,
            ItemId: item.Id,
            ItemType: item.Type,
            Context: info.context
        });
    }

    function mirrorIfEnabled(info) {

        if (isMirrorModeEnabled()) {

            var player = MediaController.getPlayerInfo();

            if (!player.isLocalPlayer && player.supportedCommands.indexOf('DisplayContent') != -1) {
                mirrorItem(info);
            }
        }
    }

    function monitorPlayer(player) {

        $(player).on('playbackstart.mediacontroller', function (e, state) {

            var info = {
                QueueableMediaTypes: state.NowPlayingItem.MediaType,
                ItemId: state.NowPlayingItem.Id,
                NowPlayingItem: state.NowPlayingItem
            };

            info = $.extend(info, state.PlayState);

            ApiClient.reportPlaybackStart(info);

        }).on('playbackstop.mediacontroller', function (e, state) {

            ApiClient.reportPlaybackStopped({

                itemId: state.NowPlayingItem.Id,
                mediaSourceId: state.PlayState.MediaSourceId,
                positionTicks: state.PlayState.PositionTicks

            });

        }).on('positionchange.mediacontroller', function (e, state) {


        });
    }

    function mediaController() {

        var self = this;
        var currentPlayer;
        var currentTargetInfo;
        var players = [];

        var keys = new bindKeys(self);

        $(window).on("keydown", keys.keyBinding);
        $(window).on("keypress keyup", keys.keyPrevent);

        self.registerPlayer = function (player) {

            players.push(player);

            if (player.isLocalPlayer) {
                monitorPlayer(player);
            }
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

            console.log('Active player: ' + JSON.stringify(currentTargetInfo));

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

            return self.canPlayByAttributes(item.Type, item.MediaType, item.PlayAccess, item.LocationType, item.IsPlaceHolder);
        };

        self.canPlayByAttributes = function (itemType, mediaType, playAccess, locationType, isPlaceHolder) {

            if (playAccess != 'Full') {
                return false;
            }

            if (locationType == "Virtual" || isPlaceHolder) {
                return false;
            }

            if (itemType == "MusicGenre" || itemType == "Season" || itemType == "Series" || itemType == "BoxSet" || itemType == "MusicAlbum" || itemType == "MusicArtist" || itemType == "Playlist") {
                return true;
            }

            return self.getPlayerInfo().playableMediaTypes.indexOf(mediaType) != -1;
        };

        self.canQueueMediaType = function (mediaType, itemType) {

            if (itemType == 'MusicAlbum' || itemType == 'MusicArtist' || itemType == 'MusicGenre') {
                mediaType = 'Audio';
            }

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

        self.playlist = function () {
            return currentPlayer.playlist || [];
        };

        self.sendCommand = function (cmd, player) {

            player = player || self.getLocalPlayer();

            // Full list
            // https://github.com/MediaBrowser/MediaBrowser/blob/master/MediaBrowser.Model/Session/GeneralCommand.cs#L23
            console.log('MediaController received command: ' + cmd.Name);
            switch (cmd.Name) {

                case 'VolumeUp':
                    player.volumeUp();
                    break;
                case 'VolumeDown':
                    player.volumeDown();
                    break;
                case 'Mute':
                    player.mute();
                    break;
                case 'Unmute':
                    player.unMute();
                    break;
                case 'ToggleMute':
                    player.toggleMute();
                    break;
                case 'SetVolume':
                    player.setVolume(cmd.Arguments.Volume);
                    break;
                case 'SetAudioStreamIndex':
                    player.setAudioStreamIndex(parseInt(cmd.Arguments.Index));
                    break;
                case 'SetSubtitleStreamIndex':
                    player.setSubtitleStreamIndex(parseInt(cmd.Arguments.Index));
                    break;
                case 'ToggleFullscreen':
                    player.toggleFullscreen();
                    break;
                default:
                    {
                        if (player.isLocalPlayer) {
                            // Not player-related
                            Dashboard.processGeneralCommand(cmd);
                        } else {
                            player.sendCommand(cmd);
                        }
                        break;
                    }
            }
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

            MediaController.sendCommand(cmd, localPlayer);
        }
    }



    function initializeApiClient(apiClient) {
        $(apiClient).on("websocketmessage", onWebSocketMessageReceived);
    }

    $(function () {

        $(ConnectionManager).on('apiclientcreated', function (e, apiClient) {

            initializeApiClient(apiClient);
        });
    });

    function getTargetsHtml(targets) {

        var playerInfo = MediaController.getPlayerInfo();

        var html = '';
        html += '<form>';

        html += '<h3>' + Globalize.translate('HeaderSelectPlayer') + '</h3>';
        html += '<fieldset data-role="controlgroup" data-mini="true">';

        var checkedHtml;

        for (var i = 0, length = targets.length; i < length; i++) {

            var target = targets[i];

            var id = 'radioPlayerTarget' + i;

            var isChecked = target.id == playerInfo.id;
            checkedHtml = isChecked ? ' checked="checked"' : '';

            var mirror = (!target.isLocalPlayer && target.supportedCommands.indexOf('DisplayContent') != -1) ? 'true' : 'false';

            html += '<input type="radio" class="radioSelectPlayerTarget" name="radioSelectPlayerTarget" data-mirror="' + mirror + '" data-commands="' + target.supportedCommands.join(',') + '" data-mediatypes="' + target.playableMediaTypes.join(',') + '" data-playername="' + target.playerName + '" data-targetid="' + target.id + '" data-targetname="' + target.name + '" data-devicename="' + (target.deviceName || '') + '" id="' + id + '" value="' + target.id + '"' + checkedHtml + '>';
            html += '<label for="' + id + '" style="font-weight:normal;">' + target.name;

            if (target.appName && target.appName != target.name) {
                html += '<br/><span>' + target.appName + '</span>';
            }

            html += '</label>';
        }

        html += '</fieldset>';

        html += '<p class="fieldDescription">' + Globalize.translate('LabelAllPlaysSentToPlayer') + '</p>';

        checkedHtml = isMirrorModeEnabled() ? ' checked="checked"' : '';

        html += '<div style="margin-top:1.5em;" class="fldMirrorMode"><label for="chkEnableMirrorMode">Enable display mirroring</label><input type="checkbox" class="chkEnableMirrorMode" id="chkEnableMirrorMode" data-mini="true"' + checkedHtml + ' /></div>';

        html += '</form>';

        return html;
    }

    function showPlayerSelection(page) {

        var promise = MediaController.getTargets();

        var html = '<div data-role="panel" data-position="right" data-display="overlay" data-position-fixed="true" id="playerSelectionPanel" class="playerSelectionPanel" data-theme="a">';

        html += '<div class="players"></div>';

        html += '<br/>';
        html += '<p><a href="nowplaying.html" data-role="button" data-icon="remote">' + Globalize.translate('ButtonRemoteControl') + '</a></p>';

        html += '</div>';

        $(document.body).append(html);

        var elem = $('#playerSelectionPanel').panel({}).trigger('create').panel("open").on("panelclose", function () {

            $(this).off("panelclose").remove();
        });

        promise.done(function (targets) {

            $('.players', elem).html(getTargetsHtml(targets)).trigger('create');

            $('.chkEnableMirrorMode', elem).on('change', function () {
                setMirrorModeEnabled(this.checked);

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
                }

            }).each(function () {

                if (this.checked) {
                    $(this).trigger('change');
                }

            }).on('change', function () {

                var playerName = this.getAttribute('data-playername');
                var targetId = this.getAttribute('data-targetid');
                var targetName = this.getAttribute('data-targetname');
                var deviceName = this.getAttribute('data-deviceName');
                var playableMediaTypes = this.getAttribute('data-mediatypes').split(',');
                var supportedCommands = this.getAttribute('data-commands').split(',');

                MediaController.setActivePlayer(playerName, {
                    id: targetId,
                    name: targetName,
                    playableMediaTypes: playableMediaTypes,
                    supportedCommands: supportedCommands,
                    deviceName: deviceName

                });

                if (currentDisplayInfo) {

                    mirrorIfEnabled(currentDisplayInfo);
                }

            });
        });
    }

    function bindKeys(controller) {

        var self = this;
        var keyResult = {};

        self.keyBinding = function (e) {

            if (bypass()) return;

            console.log("keyCode", e.keyCode);

            if (keyResult[e.keyCode]) {
                e.preventDefault();
                keyResult[e.keyCode](e);
            }
        };

        self.keyPrevent = function (e) {

            if (bypass()) return;

            var codes = [32, 38, 40, 37, 39, 81, 77, 65, 84, 83, 70];

            if (codes.indexOf(e.keyCode) != -1) {
                e.preventDefault();
            }
        };

        keyResult[32] = function () { // spacebar

            var player = controller.getCurrentPlayer();

            player.getPlayerState().done(function (result) {

                var state = result;

                if (state.NowPlayingItem && state.PlayState) {
                    if (state.PlayState.IsPaused) {
                        player.unpause();
                    } else {
                        player.pause();
                    }
                }
            });
        };

        var bypass = function () {
            // Get active elem to see what type it is
            var active = document.activeElement;
            var type = active.type || active.tagName.toLowerCase();
            return (type === "text" || type === "select" || type === "textarea" || type == "password");
        };
    }

    $(document).on('headercreated', function () {

        $('.btnCast').on('click', function () {

            showPlayerSelection($.mobile.activePage);
        });
    });

    $(document).on('pagebeforeshow', ".page", function () {

        var page = this;

        currentDisplayInfo = null;

    }).on('displayingitem', ".libraryPage", function (e, info) {

        var page = this;

        currentDisplayInfo = info;

        mirrorIfEnabled(info);
    });

})(jQuery, window, window.store);