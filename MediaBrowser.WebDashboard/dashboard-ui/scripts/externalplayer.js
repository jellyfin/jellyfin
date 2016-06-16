define(['appSettings', 'datetime', 'jQuery', 'emby-slider', 'emby-button'], function (appSettings, datetime, $) {

    function getDeviceProfile(serverAddress, deviceId, item, startPositionTicks, maxBitrate, mediaSourceId, audioStreamIndex, subtitleStreamIndex) {

        var bitrateSetting = appSettings.maxStreamingBitrate();

        var profile = {};

        profile.MaxStreamingBitrate = bitrateSetting;
        profile.MaxStaticBitrate = 100000000;
        profile.MusicStreamingTranscodingBitrate = 192000;

        profile.DirectPlayProfiles = [];

        profile.DirectPlayProfiles.push({
            Container: 'm4v,3gp,ts,mpegts,mov,xvid,vob,mkv,wmv,asf,ogm,ogv,m2v,avi,mpg,mpeg,mp4,webm,wtv,dvr-ms',
            Type: 'Video'
        });

        profile.DirectPlayProfiles.push({
            Container: 'aac,mp3,mpa,wav,wma,mp2,ogg,oga,webma,ape,opus,flac',
            Type: 'Audio'
        });

        profile.TranscodingProfiles = [];

        profile.TranscodingProfiles.push({
            Container: 'mkv',
            Type: 'Video',
            AudioCodec: 'aac,mp3,ac3',
            VideoCodec: 'h264',
            Context: 'Streaming'
        });

        profile.TranscodingProfiles.push({
            Container: 'mp3',
            Type: 'Audio',
            AudioCodec: 'mp3',
            Context: 'Streaming',
            Protocol: 'http'
        });

        profile.ContainerProfiles = [];

        profile.CodecProfiles = [];

        // Subtitle profiles
        // External vtt or burn in
        profile.SubtitleProfiles = [];
        profile.SubtitleProfiles.push({
            Format: 'srt',
            Method: 'Embed'
        });
        profile.SubtitleProfiles.push({
            Format: 'subrip',
            Method: 'Embed'
        });
        profile.SubtitleProfiles.push({
            Format: 'ass',
            Method: 'Embed'
        });
        profile.SubtitleProfiles.push({
            Format: 'ssa',
            Method: 'Embed'
        });
        profile.SubtitleProfiles.push({
            Format: 'pgs',
            Method: 'Embed'
        });
        profile.SubtitleProfiles.push({
            Format: 'pgssub',
            Method: 'Embed'
        });
        profile.SubtitleProfiles.push({
            Format: 'dvdsub',
            Method: 'Embed'
        });
        profile.SubtitleProfiles.push({
            Format: 'vtt',
            Method: 'Embed'
        });
        profile.SubtitleProfiles.push({
            Format: 'sub',
            Method: 'Embed'
        });
        profile.SubtitleProfiles.push({
            Format: 'idx',
            Method: 'Embed'
        });
        profile.SubtitleProfiles.push({
            Format: 'smi',
            Method: 'Embed'
        });

        profile.ResponseProfiles = [];

        return profile;
    }

    var currentMediaSource;
    var currentItem;
    var basePlayerState;
    var progressInterval;

    function getVideoStreamInfo(item) {

        var deviceProfile = getDeviceProfile();
        var startPosition = 0;

        return new Promise(function (resolve, reject) {

            MediaPlayer.tryStartPlayback(deviceProfile, item, startPosition, function (mediaSource) {

                playInternalPostMediaSourceSelection(item, mediaSource, startPosition).then(resolve);
            });
        });
    }

    function playInternalPostMediaSourceSelection(item, mediaSource, startPosition) {

        Dashboard.hideLoadingMsg();

        currentItem = item;
        currentMediaSource = mediaSource;

        basePlayerState = {
            PlayState: {

            }
        };

        return MediaPlayer.createStreamInfo('Video', item, mediaSource, startPosition).then(function (streamInfo) {

            var currentSrc = streamInfo.url;

            var audioStreamIndex = getParameterByName('AudioStreamIndex', currentSrc);

            if (audioStreamIndex) {
                basePlayerState.PlayState.AudioStreamIndex = parseInt(audioStreamIndex);
            }
            basePlayerState.PlayState.SubtitleStreamIndex = self.currentSubtitleStreamIndex;

            basePlayerState.PlayState.PlayMethod = getParameterByName('static', currentSrc) == 'true' ?
                'DirectStream' :
                'Transcode';

            basePlayerState.PlayState.LiveStreamId = getParameterByName('LiveStreamId', currentSrc);
            basePlayerState.PlayState.PlaySessionId = getParameterByName('PlaySessionId', currentSrc);

            basePlayerState.PlayState.MediaSourceId = mediaSource.Id;
            basePlayerState.PlayState.CanSeek = false;
            basePlayerState.NowPlayingItem = MediaPlayer.getNowPlayingItemForReporting(item, mediaSource);

            return streamInfo;
        });
    }

    function getPlayerState(positionTicks) {

        var state = basePlayerState;

        state.PlayState.PositionTicks = Math.round(positionTicks);

        return state;
    }

    function onPlaybackStart() {

        var state = getPlayerState();

        var info = {
            ItemId: state.NowPlayingItem.Id,
            NowPlayingItem: state.NowPlayingItem
        };

        info = $.extend(info, state.PlayState);

        ApiClient.reportPlaybackStart(info);

        // This is really just a ping to let the server know we're still playing
        progressInterval = setInterval(function () {
            onPlaybackProgress(null);

        }, 10000);

        showPostPlayMenu(currentItem);
    }

    function onPlaybackProgress(positionTicks) {

        var state = getPlayerState(positionTicks);

        var info = {
            ItemId: state.NowPlayingItem.Id,
            NowPlayingItem: state.NowPlayingItem
        };

        info = $.extend(info, state.PlayState);

        ApiClient.reportPlaybackProgress(info);
    }

    function onPlaybackStopped(positionTicks) {

        var state = getPlayerState(positionTicks);

        var stopInfo = {
            ItemId: state.NowPlayingItem.Id,
            MediaSourceId: state.PlayState.MediaSourceId,
            PositionTicks: state.PlayState.PositionTicks
        };

        if (state.PlayState.LiveStreamId) {
            stopInfo.LiveStreamId = state.PlayState.LiveStreamId;
        }

        if (state.PlayState.PlaySessionId) {
            stopInfo.PlaySessionId = state.PlayState.PlaySessionId;
        }

        if (progressInterval) {
            clearInterval(progressInterval);
            progressInterval = null;
        }

        // Make sure this is after progress reports have stopped
        setTimeout(function () {
            ApiClient.reportPlaybackStopped(stopInfo);
        }, 1000);
    }

    function showPostPlayMenu(item) {

        require(['jqmpopup', 'jqmlistview'], function () {
            $('.externalPlayerPostPlayFlyout').popup("close").remove();

            var html = '<div data-role="popup" class="externalPlayerPostPlayFlyout" data-history="false" data-theme="a" data-dismissible="false">';

            html += '<ul data-role="listview" style="min-width: 220px;">';
            html += '<li data-role="list-divider" style="padding: 1em;text-align:center;">' + Globalize.translate('HeaderExternalPlayerPlayback') + '</li>';
            html += '</ul>';

            html += '<div style="padding:1.5em;">';

            var autoMarkWatched = item.RunTimeTicks;

            if (item.RunTimeTicks && item.RunTimeTicks >= 3000000000) {

                autoMarkWatched = false;

                html += '<label for="selectMarkAs" class="selectLabel">' + Globalize.translate('LabelMarkAs') + '</label>';
                html += '<select id="selectMarkAs">';
                html += '<option value="0">' + Globalize.translate('OptionWatched') + '</option>';
                html += '<option value="1">' + Globalize.translate('OptionUnwatched') + '</option>';
                html += '<option value="2">' + Globalize.translate('OptionInProgress') + '</option>';
                html += '</select>';

                html += '<br/>';

                html += '<div class="fldResumePoint hide">';
                html += '<p style="margin-top: 0;">' + Globalize.translate('LabelResumePoint') + '</p>';

                html += '<div class="sliderContainer">';
                html += '<input type="range" is="emby-slider" pin step=".001" min="0" max="100" value="0" class="playstateSlider"/>';
                html += '</div>';
                html += '<div class="sliderValue" style="text-align:center;margin:2px 0 4px;">0:00:00</div>';
                html += '</div>';

                html += '<br/>';
            }

            html += '<button is="emby-button" type="button" class="block submit btnDone" raised>' + Globalize.translate('ButtonImDone') + '</button>';

            html += '</div>';

            html += '</div>';

            $(document.body).append(html);

            var elem = $('.externalPlayerPostPlayFlyout').popup({}).trigger('create').popup("open").on("popupafterclose", function () {

                $(this).off("popupafterclose").remove();

            })[0];

            $('#selectMarkAs', elem).on('change', function () {

                if (this.value == '2') {

                    elem.querySelector('.fldResumePoint').classList.remove('hide');

                } else {
                    elem.querySelector('.fldResumePoint').classList.add('hide');
                }

            }).trigger('change');

            $('.btnDone', elem).on('click', function () {

                var position = 0;
                var playstateOption = $('#selectMarkAs', elem).val();
                if (playstateOption == '2') {

                    var pct = $(".playstateSlider", elem).val();
                    var ticks = item.RunTimeTicks * (Number(pct) * .01);

                    position = ticks;
                }
                else if (autoMarkWatched || playstateOption == '0') {

                    position = currentMediaSource.RunTimeTicks;
                }
                else if (playstateOption == '1') {

                    position = 0;
                }
                onPlaybackStopped(position);
                $('.externalPlayerPostPlayFlyout').popup("close").remove();
            });

            $(".playstateSlider", elem).on("change", function (e) {

                var pct = $(this).val();

                var time = item.RunTimeTicks * (Number(pct) * .01);

                var tooltext = datetime.getDisplayRunningTime(time);

                $('.sliderValue', elem).html(tooltext);

                console.log("slidin", pct, self.currentDurationTicks, time);

            });
        });
    }

    function showMenuForItem(item, players) {

        require(['actionsheet'], function (actionsheet) {

            actionsheet.show({
                items: players,
                callback: function (id) {
                    var player = players.filter(function (p) {
                        return p.id == id;
                    })[0];

                    if (player) {
                        window.open(player.url, '_blank');
                        onPlaybackStart();
                    }
                }
            });
        });
    }

    function showPlayMenu(itemId) {

        var userId = Dashboard.getCurrentUserId();

        ApiClient.getItem(userId, itemId).then(function (item) {

            getVideoStreamInfo(item).then(function (streamInfo) {

                setTimeout(function () {
                    ExternalPlayer.showPlayerSelectionMenu(item, streamInfo.url, streamInfo.mimeType);
                }, 500);
            });
        });
    }

    function getExternalPlayers(url, mimeType) {

        var players = [
            {
                name: 'Vlc', url: 'vlc://' + url, id: 'vlc',
                ironIcon: 'airplay'
            }
        ];

        return Promise.resolve(players);
    }

    function showPlayerSelectionMenu(item, url, mimeType) {

        ExternalPlayer.getExternalPlayers(url, mimeType).then(function (players) {
            showMenuForItem(item, players);
        });
    }

    window.ExternalPlayer = {

        showMenu: showPlayMenu,
        onPlaybackStart: onPlaybackStart,
        getExternalPlayers: getExternalPlayers,
        showPlayerSelectionMenu: showPlayerSelectionMenu
    };

});