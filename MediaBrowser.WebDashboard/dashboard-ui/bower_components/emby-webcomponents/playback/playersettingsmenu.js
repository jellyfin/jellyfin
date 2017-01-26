define(['actionsheet', 'datetime', 'playbackManager', 'globalize', 'appSettings', 'qualityoptions'], function (actionsheet, datetime, playbackManager, globalize, appSettings, qualityoptions) {
    'use strict';

    function showQualityMenu(player, btn) {
        
        var videoStream = playbackManager.currentMediaSource(player).MediaStreams.filter(function (stream) {
            return stream.Type === "Video";
        })[0];
        var videoWidth = videoStream ? videoStream.Width : null;

        var options = qualityoptions.getVideoQualityOptions(playbackManager.getMaxStreamingBitrate(player), videoWidth);

        //if (isStatic) {
        //    options[0].name = "Direct";
        //}

        var menuItems = options.map(function (o) {

            var opt = {
                name: o.name,
                id: o.bitrate
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
                playbackManager.setMaxStreamingBitrate(bitrate, player);
            }
        });
    }

    function showSettingsMenu(player, btn) {

    }

    function show(options) {

        var player = options.player;
        var mediaType = options.mediaType;
        return showQualityMenu(player, options.positionTo);

        //var menuItems = [];

        //menuItems.push({
        //    name: globalize.translate('sharedcomponents#Quality'),
        //    id: 'quality'
        //});

        //menuItems.push({
        //    name: globalize.translate('sharedcomponents#Settings'),
        //    id: 'settings'
        //});

        //return actionsheet.show({

        //    items: menuItems,
        //    positionTo: options.positionTo

        //}).then(function (id) {

        //    switch (id) {

        //        case 'quality':
        //            return showQualityMenu(player, options.positionTo);
        //        case 'settings':
        //            return showSettingsMenu(player, options.positionTo);
        //        default:
        //            break;
        //    }

        //    return Promise.reject();
        //});
    }

    return {
        show: show
    };
});