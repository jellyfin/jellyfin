(function (window, document, $) {

    function sendPlayFolderCommand(item, sessionId, popup) {

        ApiClient.getItems(Dashboard.getCurrentUserId(), {

            ParentId: item.Id,
            Filters: "IsNotFolder",
            SortBy: "SortName",
            Recursive: true,
            Limit: 100

        }).done(function (result) {

            ApiClient.sendPlayCommand(sessionId, {

                ItemIds: result.Items.map(function (i) {
                    return i.Id;
                }).join(','),

                PlayCommand: $('#fldPlayCommand', popup).val()
            });

            popup.popup("close");

        });

    }

    function sendPlayArtistCommand(item, sessionId, popup) {

        ApiClient.getItems(Dashboard.getCurrentUserId(), {

            Artists: item.Name,
            SortBy: "SortName",
            IncludeItemTypes: "Audio",
            Recursive: true,
            Limit: 100

        }).done(function (result) {

            ApiClient.sendPlayCommand(sessionId, {

                ItemIds: result.Items.map(function (i) {
                    return i.Id;
                }).join(','),

                PlayCommand: $('#fldPlayCommand', popup).val()
            });

            popup.popup("close");

        });

    }

    function showMenuForItem(options, sessionsPromise) {

        var playFromRendered;
        var trailersRendered;
        var specialFeaturesRendered;
        var themeVideosRendered;
        var themeSongsRendered;

        var item = options.item;

        var html = '<div data-role="popup" class="remoteControlFlyout" data-transition="slidedown" data-theme="a">';

        html += '<a href="#" data-rel="back" data-role="button" data-icon="delete" data-iconpos="notext" class="ui-btn-right" data-theme="b">Close</a>';

        html += '<div class="ui-bar-b" style="text-align:center;">';
        html += '<div style="margin:.5em 0;">Remote Control</div>';
        html += '</div>';

        html += '<div style="padding: 1em;">';

        html += '<form id="sendToForm">';
        html += '<input type="hidden" value="PlayNow" id="fldPlayCommand" />';
        html += '<div class="sessionsPopupContent">';

        html += '<div class="circle"></div><div class="circle1"></div>';

        html += '</div>';

        html += '<p style="text-align:center;margin:.75em 0 0;">';

        html += '<span id="playButtonContainer" style="display:none;"><button onclick="$(\'#fldPlayCommand\').val(\'PlayNow\');" type="submit" data-icon="play" data-mini="true" data-inline="true">Play</button></span>';

        html += '<span id="resumeButtonContainer" style="display:none;"><button onclick="$(\'#fldPlayCommand\').val(\'Resume\');" type="submit" data-icon="play" data-mini="true" data-inline="true">Resume</button></span>';

        html += '<span id="queueButtonContainer" style="display:none;"><button onclick="$(\'#fldPlayCommand\').val(\'PlayLast\');" type="submit" data-icon="plus" data-mini="true" data-inline="true">Queue</button></span>';

        html += '<span id="browseButtonContainer" style="display:none;"><button onclick="$(\'#fldPlayCommand\').val(\'Browse\');" type="submit" data-icon="eye" data-mini="true" data-inline="true">View</button></span>';

        html += '</p>';

        html += '</form></div>';

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

        popup.on('click', '.trSession', function () {

            $('input', this).checked(true);


        }).on('click', '.trSelectPlayTime', function () {

            $('input', this).checked(true);

        }).on('click', '.trItem', function () {

            $('input', this).checked(true);

        });

        $('#sendToForm', popup).on('submit', function () {

            var checkboxes = $('.chkClient', popup);

            if (!checkboxes.length) {
                $('.remoteControlFlyout').popup("close");
                return false;
            }

            checkboxes = $('.chkClient:checked', popup);

            if (!checkboxes.length) {
                Dashboard.alert('Please select a device to control.');
                return false;
            }

            var sessionIds = [];

            checkboxes.parents('.trSession').each(function () {

                sessionIds.push(this.getAttribute('data-sessionid'));

            });

            var command = $('#selectCommand', popup).val();

            var promise;
            var showRemoteControlMenuAfterCommand = true;

            if (command == "Play") {

                if (item.IsFolder) {

                    sendPlayFolderCommand(item, sessionIds[0], popup);

                    return false;
                }
                if (item.Type == "MusicArtist") {

                    sendPlayArtistCommand(item, sessionIds[0], popup);

                    return false;
                }

                var playCommand = $('#fldPlayCommand', popup).val();

                if (playCommand == "Resume") {

                    promise = ApiClient.sendPlayCommand(sessionIds[0], {

                        ItemIds: [item.Id].join(','),
                        PlayCommand: 'PlayNow',
                        StartPositionTicks: item.UserData.PlaybackPositionTicks
                    });

                }
                else if (playCommand == "Browse") {

                    promise = ApiClient.sendBrowseCommand(sessionIds[0], {

                        ItemId: item.Id,
                        ItemName: item.Name,
                        ItemType: item.Type,
                        Context: options.context

                    });

                    showRemoteControlMenuAfterCommand = false;
                }
                else {
                    promise = ApiClient.sendPlayCommand(sessionIds[0], {

                        ItemIds: [item.Id].join(','),
                        PlayCommand: playCommand
                    });
                }
            }
            else if (command == "PlayFromChapter") {

                var checkedChapter = $('.chkSelectPlayTime:checked', popup);

                var ticks = checkedChapter.length ? checkedChapter.parents('.trSelectPlayTime').attr('data-ticks') : 0;

                promise = ApiClient.sendPlayCommand(sessionIds[0], {

                    ItemIds: [item.Id].join(','),
                    PlayCommand: $('#fldPlayCommand', popup).val(),
                    StartPositionTicks: ticks

                });
            }
            else if (command == "Trailer" || command == "SpecialFeature" || command == "ThemeSong" || command == "ThemeVideo") {

                var id = $('.chkSelectItem:checked', popup).parents('.trItem').attr('data-id');

                if (!id) {
                    Dashboard.alert('Please select an item.');
                    return false;
                }
                promise = ApiClient.sendPlayCommand(sessionIds[0], {

                    ItemIds: [id].join(','),
                    PlayCommand: $('#fldPlayCommand', popup).val()

                });
            }

            promise.done(function () {

                popup.popup("close");

                if (showRemoteControlMenuAfterCommand) {
                    RemoteControl.showMenu();
                }
            });

            return false;
        });

        var elem = $('.sessionsPopupContent');

        sessionsPromise.done(function (sessions) {

            var deviceId = ApiClient.deviceId();

            // don't display the current session
            sessions = sessions.filter(function (s) {
                return s.DeviceId != deviceId;
            });

            renderSessionsInPlayMenu(sessions, options, elem, popup);

            if (ApiClient.isWebSocketOpen()) {
                ApiClient.sendWebSocketMessage("SessionsStart", "1000,1000");

                $(ApiClient).on("websocketmessage.remotecontrol", function (e, msg) {

                    if (msg.MessageType === "Sessions") {
                        updateSessionsInPlayMenu(msg.Data, elem);
                    }
                });

            }

            $('#selectCommand', popup).on('change', function () {

                var playFromMenu = $('.playFromMenu', popup).hide();
                var trailersElem = $('.trailers', popup).hide();
                var specialFeaturesElem = $('.specialFeatures', popup).hide();
                var themeSongsElem = $('.themeSongs', popup).hide();
                var themeVideosElem = $('.themeVideos', popup).hide();
                var playButtonContainer = $('#playButtonContainer', popup).hide();
                var queueButtonContainer = $('#queueButtonContainer', popup).hide();
                var resumeButtonContainer = $('#resumeButtonContainer', popup).hide();
                var browseButtonContainer = $('#browseButtonContainer', popup).hide();

                var value = this.value;

                if (value == "Play") {

                    browseButtonContainer.show();

                    if (item.Type != 'Person' && item.Type != 'Genre' && item.Type != 'Studio' && item.Type != 'GameGenre' && item.Type != 'MusicGenre' && item.LocationType != 'Virtual') {
                        playButtonContainer.show();
                        queueButtonContainer.show();
                    }

                    if (!item.IsFolder && item.UserData && item.UserData.PlaybackPositionTicks) {
                        resumeButtonContainer.show();
                    }
                }
                else if (value == "PlayFromChapter" && item.Chapters && item.Chapters.length) {

                    playFromMenu.show();
                    playButtonContainer.show();

                    if (!playFromRendered) {
                        playFromRendered = true;
                        renderPlayFromOptions(playFromMenu, item);
                    }

                    popup.popup("reposition", { tolerance: 0 });
                }
                else if (value == "Trailer") {

                    trailersElem.show();
                    playButtonContainer.show();
                    queueButtonContainer.show();

                    if (!trailersRendered) {
                        trailersRendered = true;

                        ApiClient.getLocalTrailers(Dashboard.getCurrentUserId(), item.Id).done(function (trailers) {

                            renderVideos(trailersElem, trailers, 'Trailers');

                            popup.popup("reposition", { tolerance: 0 });
                        });
                    }
                }
                else if (value == "SpecialFeature") {

                    specialFeaturesElem.show();
                    playButtonContainer.show();
                    queueButtonContainer.show();

                    if (!specialFeaturesRendered) {
                        specialFeaturesRendered = true;

                        ApiClient.getSpecialFeatures(Dashboard.getCurrentUserId(), item.Id).done(function (videos) {

                            renderVideos(specialFeaturesElem, videos, 'Special Features');

                            popup.popup("reposition", { tolerance: 0 });
                        });
                    }
                }
                else if (value == "ThemeSong") {

                    themeSongsElem.show();
                    playButtonContainer.show();
                    queueButtonContainer.show();

                    if (!themeSongsRendered) {
                        themeSongsRendered = true;

                        ApiClient.getThemeSongs(Dashboard.getCurrentUserId(), item.Id).done(function (result) {

                            renderVideos(themeSongsElem, result.Items, 'Theme Songs');

                            $('.remoteControlFlyout').popup("reposition", { tolerance: 0 });
                        });
                    }
                }
                else if (value == "ThemeVideo") {

                    themeVideosElem.show();
                    playButtonContainer.show();
                    queueButtonContainer.show();

                    if (!themeVideosRendered) {
                        themeVideosRendered = true;

                        ApiClient.getThemeVideos(Dashboard.getCurrentUserId(), item.Id).done(function (result) {

                            renderVideos(themeVideosElem, result.Items, 'Theme Videos');

                            popup.popup("reposition", { tolerance: 0 });
                        });
                    }
                }

            }).trigger('change');
        });
    }

    function renderPlayFromOptions(elem, item) {

        var html = '';

        html += '<h4 style="margin: 1em 0 .5em;">Play from scene</h4>';

        html += '<div class="playMenuOptions">';
        html += '<table class="tblRemoteControl tblRemoteControlNoHeader">';

        html += '<tbody>';

        for (var i = 0, length = item.Chapters.length; i < length; i++) {

            var chapter = item.Chapters[i];

            html += '<tr class="trSelectPlayTime" data-ticks="' + chapter.StartPositionTicks + '">';

            var name = chapter.Name || ("Chapter " + (i + 1));

            html += '<td class="tdSelectPlayTime"></td>';

            html += '<td class="tdRemoteControlImage">';

            var imgUrl;

            if (chapter.ImageTag) {

                imgUrl = ApiClient.getImageUrl(item.Id, {
                    maxheight: 80,
                    tag: chapter.ImageTag,
                    type: "Chapter",
                    index: i
                });

            } else {
                imgUrl = "css/images/media/chapterflyout.png";
            }

            html += '<img src="' + imgUrl + '" />';

            html += '</td>';

            html += '<td>' + name + '<br/>' + Dashboard.getDisplayTime(chapter.StartPositionTicks) + '</td>';

            html += '</tr>';
        }

        html += '</tbody>';

        html += '</table>';
        html += '</div>';

        elem.html(html);

        $('.tdSelectPlayTime', elem).html('<input type="radio" class="chkSelectPlayTime" name="chkSelectPlayTime" />');

        $('.chkSelectPlayTime:first', elem).checked(true);
    }

    function renderSessionsInPlayMenu(sessions, options, elem, popup) {

        if (!sessions.length) {
            elem.html('<p>There are currently no available media browser sessions to control.</p>');
            $('.remoteControlFlyout').popup("reposition", {});
            return;
        }

        var item = options.item;

        var html = '';

        html += '<div style="margin-top:0;">';
        html += '<select id="selectCommand" data-mini="true">';

        // Default for virtual & IBN types
        var playLabel = 'View';

        if (item.LocationType != "Virtual") {
            
            if (item.IsFolder) {
                playLabel = "Play All";
            }
            else if (item.MediaType == "Video") {
                playLabel = "Play from beginning";
            }
            else if (item.MediaType) {
                playLabel = "Play";
            }
    }

        html += '<option value="Play" selected>' + playLabel + '</label>';

        if (item.Chapters && item.Chapters.length) {
            html += '<option value="PlayFromChapter">Play from scene</label>';
        }

        if (item.LocalTrailerCount) {
            html += '<option value="Trailer">Play trailer</label>';
        }

        if (item.SpecialFeatureCount) {
            html += '<option value="SpecialFeature">Play special feature</label>';
        }

        if (options.themeSongs) {
            html += '<option value="ThemeSong">Play theme song</label>';
        }

        if (options.themeVideos) {
            html += '<option value="ThemeVideo">Play theme video</label>';
        }

        html += '</select>';
        html += '</div>';

        html += '<div class="playFromMenu" style="display:none;"></div>';
        html += '<div class="trailers" style="display:none;"></div>';
        html += '<div class="specialFeatures" style="display:none;"></div>';
        html += '<div class="themeSongs" style="display:none;"></div>';
        html += '<div class="themeVideos" style="display:none;"></div>';

        html += '<h4 style="margin: 1em 0 .5em;">Select Device</h4>';

        html += '<table class="tblRemoteControl">';

        html += '<thead><tr>';
        html += '<th></th>';
        html += '<th>Device</th>';
        html += '</tr></thead>';

        html += '<tbody>';

        for (var i = 0, length = sessions.length; i < length; i++) {

            var session = sessions[i];

            html += '<tr class="trSession" data-queue="' + session.QueueableMediaTypes.join(',') + '" data-sessionid="' + session.Id + '">';

            html += '<td class="tdSelectSession"></td>';
            html += '<td>' + session.DeviceName;

            if (session.UserName) {
                html += ' - ' + session.UserName;
            }
            html += '</td>';

            html += '</tr>';
        }

        html += '</tbody>';

        html += '</table>';

        html += '</div>';

        elem.html(html).trigger('create');

        $('.tdSelectSession', elem).html('<input type="radio" class="chkClient" name="chkClient" />');

        $('.chkClient:first', elem).checked(true);
    }

    function getSessionNowPlayingTime(session) {

        var html = '';

        if (session.NowPlayingItem) {

            html += Dashboard.getDisplayTime(session.NowPlayingPositionTicks || 0);

            if (session.NowPlayingItem.RunTimeTicks) {

                html += " / ";
                html += Dashboard.getDisplayTime(session.NowPlayingItem.RunTimeTicks);
            }
        }

        return html;
    }

    function updateSessionsInPlayMenu(sessions, elem) {

        for (var i = 0, length = sessions.length; i < length; i++) {

            var session = sessions[i];

            var sessionElem = $('.trSession[data-sessionid=' + session.Id + ']', elem);

            $('.tdUserName', sessionElem).html(session.UserName || '');
            $('.tdNowPlayingTime', sessionElem).html(getSessionNowPlayingTime(session));
            $('.tdNowPlayingName', sessionElem).html(session.NowPlayingItem ? session.NowPlayingItem.Name : '');

        }

    }

    function renderVideos(elem, videos, header) {

        var html = '';

        html += '<h4 style="margin: 1em 0 .5em;">' + header + '</h4>';

        html += '<div class="playMenuOptions">';
        html += '<table class="tblRemoteControl tblRemoteControlNoHeader">';

        html += '<tbody>';

        for (var i = 0, length = videos.length; i < length; i++) {

            var video = videos[i];

            html += '<tr class="trItem" data-id="' + video.Id + '">';


            html += '<td class="tdSelectItem"></td>';

            html += '<td class="tdRemoteControlImage">';

            var imgUrl;

            if (video.ImageTags && video.ImageTags.Primary) {

                imgUrl = ApiClient.getImageUrl(video.Id, {
                    maxheight: 80,
                    tag: video.ImageTags.Primary,
                    type: "Primary"
                });

                html += '<img src="' + imgUrl + '" />';
            }

            html += '</td>';

            html += '<td>' + video.Name;

            if (video.RunTimeTicks) {
                html += '<br/>' + Dashboard.getDisplayTime(video.RunTimeTicks);
            }

            html += '</td>';

            html += '</tr>';
        }

        html += '</tbody>';

        html += '</table>';
        html += '</div>';

        elem.html(html);

        $('.tdSelectItem', elem).html('<input type="radio" class="chkSelectItem" name="chkSelectItem" />');

        $('.chkSelectItem:first', elem).checked(true);
    }

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

        html += getPlaybackHtml();

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

    function getPlaybackHtml() {

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
        html += '<button class="btnFullscreen" type="button" data-icon="action" data-inline="true" data-iconpos="notext">Toggle fullscreen</button>';
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
            return s.DeviceId != deviceId && s.SupportsRemoteControl;
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

        self.showMenuForItem = function (options) {
            showMenuForItem(options, ApiClient.getSessions(sessionQuery));
        };

        self.showMenu = function (options) {
            ApiClient.getSessions(sessionQuery).done(function (sessions) {

                showMenu(sessions, options);

            });
        };
    }

    window.RemoteControl = new remoteControl();

})(window, document, jQuery);