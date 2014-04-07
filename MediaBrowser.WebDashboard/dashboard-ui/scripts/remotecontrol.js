(function (window, document, $) {

    function showMenu(sessions, options) {

        var html = '<div data-role="popup" data-transition="slidedown" class="remoteControlFlyout" data-theme="a">';

        html += '<a href="#" data-rel="back" data-role="button" data-icon="delete" data-iconpos="notext" class="ui-btn-right" data-theme="b">Close</a>';

        html += '<div class="ui-bar-b" style="text-align:center;">';
        html += '<div style="margin:.5em 0;">Remote Control</div>';
        html += '</div>';

        html += '<div style="padding: 1em;">';

        html += '<div class="sessionsPopupContent">';

        // Add controls here
        html += '<div>';
        html += '<select id="selectSession" name="selectSession" data-mini="true"></select></div>';

        html += '</div>';

        html += '<div class="nowPlayingInfo" style="margin:1em 0;">';

        html += '<div class="nowPlaying" style="display:none;">';

        html += getPlaybackHtml(sessions.currentSession);

        html += '</div>';

        html += '<p class="nothingPlaying" style="display:none;">Nothing is currently playing.</p>';

        html += '</div>';

        html += '<div class="commandsCollapsible" data-role="collapsible" data-collapsed="true" data-mini="true" style="margin-top: 1em;display:none;">';
        html += '<h4>Send Command</h4>';
        html += '<div>';

        html += '<p class="sessionButtons" style="text-align:center;">';

        html += '<button class="btnGoHome" type="button" data-icon="home" data-mini="true" data-inline="true">Go Home</button>';
        html += '<button class="btnGoToSettings" type="button" data-icon="gear" data-mini="true" data-inline="true">View Settings</button>';

        html += '</p>';

        html += '<p style="text-align:center;">';

        html += '<div><label for="txtMessage">Message text</label></div>';

        html += '<input id="txtMessage" name="txtMessage" type="text" />';

        html += '<button type="button" data-icon="mail" class="btnSendMessage" data-mini="true">Send Message</button>';

        html += '</p>';

        html += '</div>';
        html += '</div>';

        html += '</div>';

        html += '</div>';

        $(document.body).append(html);

        var popup = $('.remoteControlFlyout').popup({ history: false, tolerance: 0, corners: false }).trigger('create').popup("open").on("popupafterclose", function () {

            if (ApiClient.isWebSocketOpen()) {
                ApiClient.sendWebSocketMessage("SessionsStop");
            }

            $(ApiClient).off("websocketmessage.remotecontrol");

            $(this).off("popupafterclose").remove();

            $('.remoteControlFlyout').popup("destroy").remove();
        });

        renderSessionsInControlMenu(popup, sessions, options);
        updateSessionInfo(popup, sessions, options);

        if (ApiClient.isWebSocketOpen()) {
            ApiClient.sendWebSocketMessage("SessionsStart", "1000,1000");

            $(ApiClient).on("websocketmessage.remotecontrol", function (e, msg) {

                if (msg.MessageType === "Sessions") {

                    // Update existing data
                    updateSessionInfo(popup, msg.Data);
                }
            });

        }

        $('.btnGoHome', popup).on('click', function () {

            var id = $('#selectSession', popup).val();

            ApiClient.sendSystemCommand(id, 'GoHome');
        });

        $('.btnGoToSettings', popup).on('click', function () {

            var id = $('#selectSession', popup).val();

            ApiClient.sendSystemCommand(id, 'GoToSettings');
        });

        $('.btnSendMessage', popup).on('click', function () {

            var id = $('#selectSession', popup).val();

            var messageText = $('#txtMessage', popup).val();

            if (messageText) {
                Dashboard.getCurrentUser().done(function (user) {

                    ApiClient.sendMessageCommand(id, {
                        Header: "Message from " + user.Name,
                        Text: messageText
                    });
                });
            } else {
                $('#txtMessage', popup)[0].focus();
            }
        });

        $('.btnVolumeDown', popup).on('click', function () {

            var id = $('#selectSession', popup).val();

            ApiClient.sendSystemCommand(id, 'VolumeDown');
        });

        $('.btnVolumeUp', popup).on('click', function () {

            var id = $('#selectSession', popup).val();

            ApiClient.sendSystemCommand(id, 'VolumeUp');
        });

        $('.btnToggleMute', popup).on('click', function () {

            var id = $('#selectSession', popup).val();

            ApiClient.sendSystemCommand(id, 'ToggleMute');
        });

        $('.btnStop', popup).on('click', function () {

            var id = $('#selectSession', popup).val();

            ApiClient.sendPlayStateCommand(id, 'Stop');
        });

        $('.btnPause', popup).on('click', function () {

            var id = $('#selectSession', popup).val();

            ApiClient.sendPlayStateCommand(id, 'Pause');
        });

        $('.btnPlay', popup).on('click', function () {

            var id = $('#selectSession', popup).val();

            ApiClient.sendPlayStateCommand(id, 'Unpause');
        });

        $('.btnNextTrack', popup).on('click', function () {

            var id = $('#selectSession', popup).val();

            ApiClient.sendPlayStateCommand(id, 'NextTrack');
        });

        $('.btnPreviousTrack', popup).on('click', function () {

            var id = $('#selectSession', popup).val();

            ApiClient.sendPlayStateCommand(id, 'PreviousTrack');
        });

        $("#positionSlider", popup).on("slidestart", function () {

            this.isSliding = true;

        }).on("slidestop", function () {

            var id = $('#selectSession', popup).val();

            var percent = $(this).val();

            var duration = parseInt($(this).attr('data-duration'));

            var position = duration * percent / 100;

            ApiClient.sendPlayStateCommand(id, 'Seek',
                {
                    SeekPositionTicks: parseInt(position)
                });

            this.isSliding = false;
        });

        $('.btnFullscreen', popup).on('click', function () {

            var id = $('#selectSession', popup).val();

            ApiClient.sendPlayStateCommand(id, 'Fullscreen');
        });
    }

    function getPlaybackHtml(session) {

        var html = '';

        html += '<p class="nowPlayingTitle" style="text-align:center;margin:1.5em 0 0;"></p>';

        html += '<p class="nowPlayingImage" style="text-align:center;margin-top:.5em;"></p>';

        html += '<div style="text-align:center;margin: 1em 0;">';

        html += '<div style="text-align:right;vertical-align:middle;padding-right:20px;font-weight: bold;">';
        html += '<span class="nowPlayingTime"></span>';
        html += '<span> / </span>';
        html += '<span class="duration"></span>';
        html += '</div>';

        html += '<div class="remotePositionSliderContainer"><input type="range" name="positionSlider" id="positionSlider" min="0" max="100" value="50" step=".1" style="display:none;" /></div>';
        html += '</div>';

        html += '<div style="text-align:center; margin: 0 0 2em;">';
        html += '<button class="btnPreviousTrack" type="button" data-icon="previous-track" data-inline="true" data-iconpos="notext">Previous track</button>';
        html += '<span class="btnPauseParent"><button class="btnPause" type="button" data-icon="pause" data-inline="true" data-iconpos="notext">Pause</button></span>';
        html += '<span class="btnPlayParent"><button class="btnPlay" type="button" data-icon="play" data-inline="true" data-iconpos="notext">Play</button></span>';
        html += '<button class="btnStop" type="button" data-icon="stop" data-inline="true" data-iconpos="notext">Stop</button>';
        html += '<button class="btnNextTrack" type="button" data-icon="next-track" data-inline="true" data-iconpos="notext">Next track</button>';
        html += '<button class="btnVolumeDown" type="button" data-icon="volume-down" data-inline="true" data-iconpos="notext">Decrease volume</button>';
        html += '<button class="btnVolumeUp" type="button" data-icon="volume-up" data-inline="true" data-iconpos="notext">Increase volume</button>';
        html += '<button class="btnToggleMute" type="button" data-icon="volume-off" data-inline="true" data-iconpos="notext">Toggle mute</button>';

        if (session && session.SupportsFullscreenToggle) {
            html += '<button class="btnFullscreen" type="button" data-icon="action" data-inline="true" data-iconpos="notext">Toggle fullscreen</button>';
        }

        html += '</div>';


        return html;
    }

    function updateSessionInfo(popup, sessions) {

        var id = $('#selectSession', popup).val();

        // don't display the current session
        var session = sessions.filter(function (s) {
            return s.Id == id;
        })[0];

        if (!session) {

            $('.nothingPlaying', popup).hide();
            $('.nowPlaying', popup).hide();
            $('.commandsCollapsible', popup).hide();

        }
        else if (session.NowPlayingItem) {

            $('.commandsCollapsible', popup).show();
            $('.nothingPlaying', popup).hide();

            var elem = $('.nowPlaying', popup).show();

            updateNowPlaying(elem, session);

        } else {

            $('.commandsCollapsible', popup).show();
            $('.nothingPlaying', popup).show();
            $('.nowPlaying', popup).hide();
        }
    }

    function updateNowPlaying(elem, session) {

        var item = session.NowPlayingItem;

        $('.nowPlayingTitle', elem).html(item.Name);

        var imageContainer = $('.nowPlayingImage', elem);

        if (item.PrimaryImageTag) {
            imageContainer.show();

            var img = $('img', imageContainer)[0];

            var imgUrl = ApiClient.getImageUrl(item.Id, {
                maxheight: 300,
                type: 'Primary',
                tag: item.PrimaryImageTag
            });

            if (!img || img.src.toLowerCase().indexOf(imgUrl.toLowerCase()) == -1) {
                imageContainer.html('<img style="max-height:150px;" src="' + imgUrl + '" />');
            }

        } else {
            imageContainer.hide();
        }

        if (session.CanSeek) {
            $('.remotePositionSliderContainer', elem).show();
        } else {
            $('.remotePositionSliderContainer', elem).hide();
        }

        var time = session.NowPlayingPositionTicks || 0;
        var duration = item.RunTimeTicks || 0;

        var percent = duration ? 100 * time / duration : 0;

        var slider = $('#positionSlider', elem);

        if (!slider[0].isSliding) {
            slider.val(percent).slider('refresh');
        }

        slider.attr('data-duration', duration);

        $('.nowPlayingTime', elem).html(Dashboard.getDisplayTime(time));
        $('.duration', elem).html(Dashboard.getDisplayTime(duration));

        if (session.IsPaused) {
            $('.btnPauseParent', elem).hide();
            $('.btnPlayParent', elem).show();
        } else {
            $('.btnPauseParent', elem).show();
            $('.btnPlayParent', elem).hide();
        }
    }

    function renderSessionsInControlMenu(popup, sessions, options) {

        options = options || {};

        var deviceId = ApiClient.deviceId();

        // don't display the current session
        sessions = sessions.filter(function (s) {
            return s.DeviceId != deviceId && (s.SupportsRemoteControl || s.Client == "Chromecast");
        });

        var elem = $('#selectSession', popup);

        var currentValue = options.sessionId || elem.val();

        if (currentValue) {

            // Make sure the session is still active
            var currentSession = sessions.filter(function (s) {
                return s.Id == currentValue;
            })[0];

            if (!currentSession) {
                currentValue = null;
            }
        }

        if (!currentValue && sessions.length) {
            currentValue = sessions[0].Id;
        }

        var html = '';

        for (var i = 0, length = sessions.length; i < length; i++) {

            var session = sessions[i];

            var text = session.DeviceName;

            if (session.UserName) {
                text += ' - ' + session.UserName;
            }

            html += '<option value="' + session.Id + '">' + text + '</option>';
        }

        elem.html(html).val(currentValue).selectmenu('refresh');

    }

    function remoteControl() {

        var self = this;

        var sessionQuery = {
            SupportsRemoteControl: true,
            ControllableByUserId: Dashboard.getCurrentUserId()
        };

        self.showMenu = function (options) {
            ApiClient.getSessions(sessionQuery).done(function (sessions) {

                console.log("showMenu", sessions);

                showMenu(sessions, options);

            });
        };
    }

    window.RemoteControl = new remoteControl();

    function sendPlayCommand(options, playType) {

        var sessionId = MediaController.getPlayerInfo().id;

        var ids = options.ids || options.items.map(function (i) {
            return i.Id;
        });

        var remoteOptions = {
            ItemIds: ids.join(','),

            PlayCommand: playType
        };

        if (options.startPositionTicks) {
            remoteOptions.startPositionTicks = options.startPositionTicks;
        }

        ApiClient.sendPlayCommand(sessionId, remoteOptions);
    }

    function remoteControlPlayer() {

        var self = this;

        self.name = 'Remote Control';

        self.play = function (options) {

            sendPlayCommand(options, 'PlayNow');
        };

        self.shuffle = function (id) {

            sendPlayCommand({ ids: [id] }, 'PlayShuffle');
        };

        self.instantMix = function (id) {

            sendPlayCommand({ ids: [id] }, 'PlayInstantMix');
        };

        self.queue = function (options) {

            sendPlayCommand(options, 'PlayNext');
        };

        self.queueNext = function (options) {

            sendPlayCommand(options, 'PlayLast');
        };

        self.canQueueMediaType = function (mediaType) {

            return mediaType == 'Audio' || mediaType == 'Video';
        };

        self.stop = function () {

        };

        self.mute = function () {

        };

        self.unMute = function () {

        };

        self.toggleMute = function () {

        };

        self.getTargets = function () {

            var deferred = $.Deferred();

            var sessionQuery = {
                SupportsRemoteControl: true,
                ControllableByUserId: Dashboard.getCurrentUserId()
            };

            ApiClient.getSessions(sessionQuery).done(function (sessions) {

                var targets = sessions.filter(function (s) {

                    return s.DeviceId != ApiClient.deviceId();

                }).map(function (s) {
                    return {
                        name: s.DeviceName,
                        id: s.Id,
                        playerName: self.name,
                        appName: s.Client,
                        playableMediaTypes: s.PlayableMediaTypes,
                        isLocalPlayer: false
                    };
                });

                deferred.resolveWith(null, [targets]);

            }).fail(function () {

                deferred.reject();
            });

            return deferred.promise();
        };
    }

    MediaController.registerPlayer(new remoteControlPlayer());

    function onWebSocketMessageReceived(e, msg) {

        if (msg.MessageType === "SessionEnded") {

            console.log("Server reports another session ended");

            if (MediaController.getPlayerInfo().id == msg.Data.Id) {
                MediaController.setDefaultPlayerActive();
            }
        }
    }

    $(ApiClient).on("websocketmessage", onWebSocketMessageReceived);

})(window, document, jQuery);