(function (window, document, $, setTimeout, clearTimeout) {

    var currentPlayer;

    var currentTimeElement;
    var nowPlayingImageElement;
    var nowPlayingTextElement;
    var unmuteButton;
    var muteButton;
    var volumeSlider;
    var isVolumeSliderActive;
    var unpauseButton;
    var pauseButton;
    var positionSlider;
    var isPositionSliderActive;

    var lastPlayerState;

    function getNowPlayingBarHtml() {

        var html = '';

        html += '<div id="nowPlayingBar" class="nowPlayingBar" style="display:none;">';
        html += '<div style="display:inline-block;width:12px;"></div>';
        html += '<a id="playlistButton" class="mediaButton playlistButton" href="playlist.html" data-role="button" data-icon="bullets" data-iconpos="notext" data-inline="true" title="Playlist">Playlist</a>';
        html += '<button id="previousTrackButton" class="mediaButton previousTrackButton" title="Previous Track" type="button" data-icon="previous-track" data-iconpos="notext" data-inline="true">Previous Track</button>';
        html += '<button id="playButton" class="mediaButton unpauseButton" title="Play" type="button" data-icon="play" data-iconpos="notext" data-inline="true">Play</button>';
        html += '<button id="pauseButton" class="mediaButton pauseButton" title="Pause" type="button" data-icon="pause" data-iconpos="notext" data-inline="true">Pause</button>';

        html += '<button id="stopButton" class="mediaButton stopButton" title="Stop" type="button" data-icon="stop" data-iconpos="notext" data-inline="true">Stop</button>';
        html += '<button id="nextTrackButton" class="mediaButton nextTrackButton" title="Next Track" type="button" data-icon="next-track" data-iconpos="notext" data-inline="true">Next Track</button>';

        html += '<div id="mediaElement"></div>';

        html += '<div class="positionSliderContainer sliderContainer">';
        html += '<input type="range" class="mediaSlider positionSlider slider" step=".001" min="0" max="100" value="0" style="display:none;" data-mini="true" data-theme="a" data-highlight="true" />';
        html += '</div>';

        html += '<div class="currentTime"></div>';
        html += '<div class="nowPlayingImage"></div>';
        html += '<div class="nowPlayingText"></div>';

        html += '<button id="muteButton" class="mediaButton muteButton" title="Mute" type="button" data-icon="audio" data-iconpos="notext" data-inline="true">Mute</button>';
        html += '<button id="unmuteButton" class="mediaButton unmuteButton" title="Unmute" type="button" data-icon="volume-off" data-iconpos="notext" data-inline="true">Unmute</button>';

        html += '<div class="volumeSliderContainer sliderContainer">';
        html += '<input type="range" class="mediaSlider volumeSlider slider" step=".05" min="0" max="100" value="0" style="display:none;" data-mini="true" data-theme="a" data-highlight="true" />';
        html += '</div>';

        html += '</div>';

        return html;
    }

    function bindEvents(elem) {

        currentTimeElement = $('.currentTime', elem);
        nowPlayingImageElement = $('.nowPlayingImage', elem);
        nowPlayingTextElement = $('.nowPlayingText', elem);

        unmuteButton = $('.unmuteButton', elem).on('click', function () {

            if (currentPlayer) {
                currentPlayer.unMute();
            }
        });

        muteButton = $('.muteButton', elem).on('click', function () {

            if (currentPlayer) {
                currentPlayer.mute();
            }
        });

        $('.stopButton', elem).on('click', function () {

            if (currentPlayer) {
                currentPlayer.stop();
            }
        });

        pauseButton = $('.pauseButton', elem).on('click', function () {

            if (currentPlayer) {
                currentPlayer.pause();
            }
        });

        unpauseButton = $('.unpauseButton', elem).on('click', function () {

            if (currentPlayer) {
                currentPlayer.unpause();
            }
        });

        $('.nextTrackButton', elem).on('click', function () {

            if (currentPlayer) {
                currentPlayer.nextTrack();
            }
        });

        $('.previousTrackButton', elem).on('click', function () {

            if (currentPlayer) {
                currentPlayer.previousTrack();
            }
        });

        volumeSlider = $('.volumeSlider', elem).on('slidestart', function () {

            isVolumeSliderActive = true;

        }).on('slidestop', function () {

            isVolumeSliderActive = false;

            if (currentPlayer) {
                currentPlayer.setVolume(this.value);
            }
        });

        positionSlider = $('.positionSlider', elem).on('slidestart', function () {

            isPositionSliderActive = true;

        }).on('slidestop', function () {

            isPositionSliderActive = false;

            if (currentPlayer && lastPlayerState) {

                var ticks = lastPlayerState.runtimeTicks * this.value / 100;

                currentPlayer.seek(ticks);
            }
        });
    }

    function getNowPlayingBar() {

        var elem = $('.nowPlayingBar');

        if (elem.length) {
            return elem;
        }

        elem = $(getNowPlayingBarHtml()).insertBefore('#footerNotifications').trigger('create');

        bindEvents(elem);

        return elem;
    }

    function updatePlayerState(state) {

        lastPlayerState = state;

        if (!muteButton) {
            getNowPlayingBar();
        }

        if (state.isMuted) {

            muteButton.hide();
            unmuteButton.show();

        } else {

            muteButton.show();
            unmuteButton.hide();
        }

        if (state.isPaused) {

            pauseButton.hide();
            unpauseButton.show();

        } else {

            pauseButton.show();
            unpauseButton.hide();
        }

        if (!isVolumeSliderActive) {
            volumeSlider.val(state.volumeLevel || 0).slider('refresh');
        }

        if (!isPositionSliderActive) {
            if (state.canSeek) {

                var pct = state.positionTicks / state.runtimeTicks;
                pct *= 100;

                positionSlider.val(pct).prop('disabled', '');

            } else {

                positionSlider.val(0).prop('disabled', 'disabled');
            }

            positionSlider.slider('refresh');
        }

        var timeText = Dashboard.getDisplayTime(state.positionTicks);

        if (state.runtimeTicks) {

            timeText += " / " + Dashboard.getDisplayTime(state.runtimeTicks);

        }

        currentTimeElement.html(timeText);
    }

    function updateNowPlayingInfo(state) {

        var name = state.itemName;

        if (state.itemSubName) {
            name += '<br/>' + state.itemSubName;
            nowPlayingTextElement.addClass('nowPlayingDoubleText');
        } else {
            nowPlayingTextElement.removeClass('nowPlayingDoubleText');
        }

        nowPlayingTextElement.html(name);

        var url;

        if (state.primaryImageTag) {

            url = ApiClient.getImageUrl(state.primaryImageItemId, {
                type: "Primary",
                height: 80,
                tag: state.primaryImageTag
            });
        }
        else if (state.backdropImageTag) {

            url = ApiClient.getImageUrl(state.backdropItemId, {
                type: "Backdrop",
                height: 80,
                tag: state.backdropImageTag,
                index: 0
            });

        } else if (state.thumbImageTag) {

            url = ApiClient.getImageUrl(state.thumbImageItemId, {
                type: "Thumb",
                height: 80,
                tag: state.thumbImageTag
            });
        }

        else if (item.Type == "TvChannel" || item.Type == "Recording") {
            url = "css/images/items/detail/tv.png";
        }
        else if (item.MediaType == "Audio") {
            url = "css/images/items/detail/audio.png";
        }
        else {
            url = "css/images/items/detail/video.png";
        }

        nowPlayingImageElement.html('<img src="' + url + '" />');
    }

    function onPlaybackStart(e, state) {

        var player = this;

        if (player.isDefaultPlayer && state.mediaType == 'Video') {
            return;
        }

        showNowPlayingBar();

        updatePlayerState(state);
        updateNowPlayingInfo(state);
    }

    var nowPlayingBarTimeout;
    function showNowPlayingBar() {

        if (nowPlayingBarTimeout) {
            clearTimeout(nowPlayingBarTimeout);
            nowPlayingBarTimeout = null;
        }

        var nowPlayingBar = getNowPlayingBar();

        nowPlayingBar.show();
    }

    function hideNowPlayingBar() {

        if (nowPlayingBarTimeout) {
            clearTimeout(nowPlayingBarTimeout);
            nowPlayingBarTimeout = null;
        }

        // Use a timeout to prevent the bar from hiding and showing quickly
        // in the event of a stop->play command
        nowPlayingBarTimeout = setTimeout(function () {
            getNowPlayingBar().hide();
        }, 200);
    }

    function onPlaybackStopped(e, state) {

        hideNowPlayingBar();
    }

    function onVolumeChanged(e, state) {

        var player = this;

        if (player.isDefaultPlayer && state.mediaType == 'Video') {
            return;
        }

        updatePlayerState(state);
    }

    function releaseCurrentPlayer() {

        if (currentPlayer) {

            $(currentPlayer).off('.nowplayingbar');
            currentPlayer = null;
        }
    }

    function bindToPlayer(player) {

        releaseCurrentPlayer();

        currentPlayer = player;

        $(player).on('playbackstart.nowplayingbar', onPlaybackStart)
            .on('playbackstop.nowplayingbar', onPlaybackStopped)
            .on('volumechange.nowplayingbar', onVolumeChanged)
            .on('playstatechange.nowplayingbar', onVolumeChanged)
            .on('positionchange.nowplayingbar', onVolumeChanged);
    }

    $(function () {

        $(MediaController).on('playerchange', function () {

            bindToPlayer(MediaController.getCurrentPlayer());
        });

        bindToPlayer(MediaController.getCurrentPlayer());
    });

})(window, document, jQuery, setTimeout, clearTimeout);