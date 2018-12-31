define(['connectionManager', 'actionsheet', 'datetime', 'playbackManager', 'globalize', 'appSettings', 'qualityoptions'], function (connectionManager, actionsheet, datetime, playbackManager, globalize, appSettings, qualityoptions) {
    'use strict';

    function showQualityMenu(player, btn) {

        var videoStream = playbackManager.currentMediaSource(player).MediaStreams.filter(function (stream) {
            return stream.Type === "Video";
        })[0];
        var videoWidth = videoStream ? videoStream.Width : null;

        var options = qualityoptions.getVideoQualityOptions({
            currentMaxBitrate: playbackManager.getMaxStreamingBitrate(player),
            isAutomaticBitrateEnabled: playbackManager.enableAutomaticBitrateDetection(player),
            videoWidth: videoWidth,
            enableAuto: true
        });

        var menuItems = options.map(function (o) {

            var opt = {
                name: o.name,
                id: o.bitrate,
                asideText: o.secondaryText
            };

            if (o.selected) {
                opt.selected = true;
            }

            return opt;
        });

        var selectedId = options.filter(function (o) {
            return o.selected;
        });

        selectedId = selectedId.length ? selectedId[0].bitrate : null;

        return actionsheet.show({
            items: menuItems,
            positionTo: btn

        }).then(function (id) {
            var bitrate = parseInt(id);
            if (bitrate !== selectedId) {

                playbackManager.setMaxStreamingBitrate({

                    enableAutomaticBitrateDetection: bitrate ? false : true,
                    maxBitrate: bitrate

                }, player);
            }
        });
    }

    function showRepeatModeMenu(player, btn) {

        var menuItems = [];

        var currentValue = playbackManager.getRepeatMode(player);

        menuItems.push({
            name: globalize.translate('sharedcomponents#RepeatAll'),
            id: 'RepeatAll',
            selected: currentValue === 'RepeatAll'
        });
        menuItems.push({
            name: globalize.translate('sharedcomponents#RepeatOne'),
            id: 'RepeatOne',
            selected: currentValue === 'RepeatOne'
        });

        menuItems.push({
            name: globalize.translate('sharedcomponents#None'),
            id: 'RepeatNone',
            selected: currentValue === 'RepeatNone'
        });

        return actionsheet.show({
            items: menuItems,
            positionTo: btn

        }).then(function (mode) {

            if (mode) {
                playbackManager.setRepeatMode(mode, player);
            }
        });
    }

    function getQualitySecondaryText(player) {

        var state = playbackManager.getPlayerState(player);

        var isAutoEnabled = playbackManager.enableAutomaticBitrateDetection(player);
        var currentMaxBitrate = playbackManager.getMaxStreamingBitrate(player);

        var videoStream = playbackManager.currentMediaSource(player).MediaStreams.filter(function (stream) {
            return stream.Type === "Video";
        })[0];
        var videoWidth = videoStream ? videoStream.Width : null;

        var options = qualityoptions.getVideoQualityOptions({
            currentMaxBitrate: playbackManager.getMaxStreamingBitrate(player),
            isAutomaticBitrateEnabled: playbackManager.enableAutomaticBitrateDetection(player),
            videoWidth: videoWidth,
            enableAuto: true
        });

        var menuItems = options.map(function (o) {

            var opt = {
                name: o.name,
                id: o.bitrate,
                asideText: o.secondaryText
            };

            if (o.selected) {
                opt.selected = true;
            }

            return opt;
        });

        var selectedOption = options.filter(function (o) {
            return o.selected;
        });

        if (!selectedOption.length) {
            return null;
        }

        selectedOption = selectedOption[0];

        var text = selectedOption.name;

        if (selectedOption.autoText) {
            if (state.PlayState && state.PlayState.PlayMethod !== 'Transcode') {
                text += ' - Direct';
            } else {
                text += ' ' + selectedOption.autoText;
            }
        }

        return text;
    }

    function showAspectRatioMenu(player, btn) {

        // Each has name/id
        var currentId = playbackManager.getAspectRatio(player);
        var menuItems = playbackManager.getSupportedAspectRatios(player).map(function (i) {
            return {
                id: i.id,
                name: i.name,
                selected: i.id === currentId
            };
        });

        return actionsheet.show({

            items: menuItems,
            positionTo: btn

        }).then(function (id) {

            if (id) {
                playbackManager.setAspectRatio(id, player);
                return Promise.resolve();
            }

            return Promise.reject();
        });
    }

    function showWithUser(options, player, user) {

        var supportedCommands = playbackManager.getSupportedCommands(player);

        var mediaType = options.mediaType;

        var menuItems = [];

        if (supportedCommands.indexOf('SetAspectRatio') !== -1) {

            var currentAspectRatioId = playbackManager.getAspectRatio(player);
            var currentAspectRatio = playbackManager.getSupportedAspectRatios(player).filter(function (i) {
                return i.id === currentAspectRatioId;
            })[0];

            menuItems.push({
                name: globalize.translate('sharedcomponents#AspectRatio'),
                id: 'aspectratio',
                asideText: currentAspectRatio ? currentAspectRatio.name : null
            });
        }

        menuItems.push({
            name: globalize.translate('sharedcomponents#PlaybackSettings'),
            id: 'playbacksettings'
        });

        if (user && user.Policy.EnableVideoPlaybackTranscoding) {
            var secondaryQualityText = getQualitySecondaryText(player);

            menuItems.push({
                name: globalize.translate('sharedcomponents#Quality'),
                id: 'quality',
                asideText: secondaryQualityText
            });
        }

        var repeatMode = playbackManager.getRepeatMode(player);

        if (supportedCommands.indexOf('SetRepeatMode') !== -1 && playbackManager.currentMediaSource(player).RunTimeTicks) {

            menuItems.push({
                name: globalize.translate('sharedcomponents#RepeatMode'),
                id: 'repeatmode',
                asideText: repeatMode === 'RepeatNone' ? globalize.translate('sharedcomponents#None') : globalize.translate('sharedcomponents#' + repeatMode)
            });
        }

        if (options.stats) {

            menuItems.push({
                name: globalize.translate('sharedcomponents#StatsForNerds'),
                id: 'stats',
                asideText: null
            });
        }

        menuItems.push({
            name: globalize.translate('sharedcomponents#SubtitleSettings'),
            id: 'subtitlesettings'
        });

        return actionsheet.show({

            items: menuItems,
            positionTo: options.positionTo

        }).then(function (id) {

            return handleSelectedOption(id, options, player);
        });
    }

    function show(options) {

        var player = options.player;

        var currentItem = playbackManager.currentItem(player);

        if (!currentItem || !currentItem.ServerId) {
            return showWithUser(options, player, null);
        }

        var apiClient = connectionManager.getApiClient(currentItem.ServerId);

        return apiClient.getCurrentUser().then(function (user) {
            return showWithUser(options, player, user);
        });
    }

    function alertText(text) {

        return new Promise(function (resolve, reject) {

            require(['alert'], function (alert) {

                alert(text).then(resolve);
            });
        });
    }

    function showSubtitleSettings(player, btn) {
        return alertText(globalize.translate('sharedcomponents#SubtitleSettingsIntro'));
    }

    function showPlaybackSettings(player, btn) {
        return alertText(globalize.translate('sharedcomponents#PlaybackSettingsIntro'));
    }

    function handleSelectedOption(id, options, player) {

        switch (id) {

            case 'quality':
                return showQualityMenu(player, options.positionTo);
            case 'aspectratio':
                return showAspectRatioMenu(player, options.positionTo);
            case 'repeatmode':
                return showRepeatModeMenu(player, options.positionTo);
            case 'subtitlesettings':
                return showSubtitleSettings(player, options.positionTo);
            case 'playbacksettings':
                return showPlaybackSettings(player, options.positionTo);
            case 'stats':
                if (options.onOption) {
                    options.onOption('stats');
                }
                return Promise.resolve();
            default:
                break;
        }

        return Promise.reject();
    }

    return {
        show: show
    };
});