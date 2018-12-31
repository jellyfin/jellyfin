define(['appSettings', 'browser', 'events'], function (appSettings, browser, events) {
    'use strict';

    function getSavedVolume() {
        return appSettings.get("volume") || 1;
    }

    function saveVolume(value) {
        if (value) {
            appSettings.set("volume", value);
        }
    }

    function getCrossOriginValue(mediaSource) {

        if (mediaSource.IsRemote) {
            return null;
        }

        return 'anonymous';
    }

    function canPlayNativeHls() {
        var media = document.createElement('video');

        if (media.canPlayType('application/x-mpegURL').replace(/no/, '') ||
            media.canPlayType('application/vnd.apple.mpegURL').replace(/no/, '')) {
            return true;
        }

        return false;
    }

    function enableHlsShakaPlayer(item, mediaSource, mediaType) {

        if (!!window.MediaSource && !!MediaSource.isTypeSupported) {

            if (canPlayNativeHls()) {

                if (browser.edge && mediaType === 'Video') {
                    return true;
                }

                // simple playback should use the native support
                if (mediaSource.RunTimeTicks) {
                    //if (!browser.edge) {
                    //return false;
                    //}
                }

                //return false;
            }

            return true;
        }

        return false;
    }

    function enableHlsJsPlayer(runTimeTicks, mediaType) {

        if (window.MediaSource == null) {
            return false;
        }

        // hls.js is only in beta. needs more testing.
        if (browser.iOS) {
            return false;
        }

        // The native players on these devices support seeking live streams, no need to use hls.js here
        if (browser.tizen || browser.web0s) {
            return false;
        }

        if (canPlayNativeHls()) {

            // Having trouble with chrome's native support and transcoded music
            if (browser.android && mediaType === 'Audio') {
                return true;
            }

            if (browser.edge && mediaType === 'Video') {
                //return true;
            }

            // simple playback should use the native support
            if (runTimeTicks) {
                //if (!browser.edge) {
                return false;
                //}
            }

            //return false;
        }

        return true;
    }

    var recoverDecodingErrorDate, recoverSwapAudioCodecDate;
    function handleHlsJsMediaError(instance, reject) {

        var hlsPlayer = instance._hlsPlayer;

        if (!hlsPlayer) {
            return;
        }

        var now = Date.now();

        if (window.performance && window.performance.now) {
            now = performance.now();
        }

        if (!recoverDecodingErrorDate || (now - recoverDecodingErrorDate) > 3000) {
            recoverDecodingErrorDate = now;
            console.log('try to recover media Error ...');
            hlsPlayer.recoverMediaError();
        } else {
            if (!recoverSwapAudioCodecDate || (now - recoverSwapAudioCodecDate) > 3000) {
                recoverSwapAudioCodecDate = now;
                console.log('try to swap Audio Codec and recover media Error ...');
                hlsPlayer.swapAudioCodec();
                hlsPlayer.recoverMediaError();
            } else {
                console.error('cannot recover, last media error recovery failed ...');

                if (reject) {
                    reject();
                } else {
                    onErrorInternal(instance, 'mediadecodeerror');
                }
            }
        }
    }

    function onErrorInternal(instance, type) {

        // Needed for video
        if (instance.destroyCustomTrack) {
            instance.destroyCustomTrack(instance._mediaElement);
        }

        events.trigger(instance, 'error', [
            {
                type: type
            }]);
    }

    function isValidDuration(duration) {
        if (duration && !isNaN(duration) && duration !== Number.POSITIVE_INFINITY && duration !== Number.NEGATIVE_INFINITY) {
            return true;
        }

        return false;
    }

    function setCurrentTimeIfNeeded(element, seconds) {

        if (Math.abs(element.currentTime || 0, seconds) <= 1) {
            element.currentTime = seconds;
        }
    }

    function seekOnPlaybackStart(instance, element, ticks) {

        var seconds = (ticks || 0) / 10000000;

        if (seconds) {
            var src = (instance.currentSrc() || '').toLowerCase();

            // Appending #t=xxx to the query string doesn't seem to work with HLS
            // For plain video files, not all browsers support it either
            var delay = browser.safari ? 2500 : 0;
            if (delay) {
                setTimeout(function () {
                    setCurrentTimeIfNeeded(element, seconds);
                }, delay);
            } else {
                setCurrentTimeIfNeeded(element, seconds);
            }
        }
    }

    function applySrc(elem, src, options) {

        if (window.Windows && options.mediaSource && options.mediaSource.IsLocal) {

            return Windows.Storage.StorageFile.getFileFromPathAsync(options.url).then(function (file) {

                var playlist = new Windows.Media.Playback.MediaPlaybackList();

                var source1 = Windows.Media.Core.MediaSource.createFromStorageFile(file);
                var startTime = (options.playerStartPositionTicks || 0) / 10000;
                playlist.items.append(new Windows.Media.Playback.MediaPlaybackItem(source1, startTime));
                elem.src = URL.createObjectURL(playlist, { oneTimeOnly: true });
                return Promise.resolve();
            });

        } else {

            elem.src = src;
        }

        return Promise.resolve();
    }

    function onSuccessfulPlay(elem, onErrorFn) {

        elem.addEventListener('error', onErrorFn);
    }

    function playWithPromise(elem, onErrorFn) {

        try {
            var promise = elem.play();
            if (promise && promise.then) {
                // Chrome now returns a promise
                return promise.catch(function (e) {

                    var errorName = (e.name || '').toLowerCase();
                    // safari uses aborterror
                    if (errorName === 'notallowederror' ||
                        errorName === 'aborterror') {
                        // swallow this error because the user can still click the play button on the video element
                        onSuccessfulPlay(elem, onErrorFn);
                        return Promise.resolve();
                    }
                    return Promise.reject();
                });
            } else {
                onSuccessfulPlay(elem, onErrorFn);
                return Promise.resolve();
            }
        } catch (err) {
            console.log('error calling video.play: ' + err);
            return Promise.reject();
        }
    }

    function destroyCastPlayer(instance) {

        var player = instance._castPlayer;
        if (player) {
            try {
                player.unload();
            } catch (err) {
                console.log(err);
            }

            instance._castPlayer = null;
        }
    }

    function destroyShakaPlayer(instance) {
        var player = instance._shakaPlayer;
        if (player) {
            try {
                player.destroy();
            } catch (err) {
                console.log(err);
            }

            instance._shakaPlayer = null;
        }
    }

    function destroyHlsPlayer(instance) {
        var player = instance._hlsPlayer;
        if (player) {
            try {
                player.destroy();
            } catch (err) {
                console.log(err);
            }

            instance._hlsPlayer = null;
        }
    }

    function destroyFlvPlayer(instance) {
        var player = instance._flvPlayer;
        if (player) {
            try {
                player.unload();
                player.detachMediaElement();
                player.destroy();
            } catch (err) {
                console.log(err);
            }

            instance._flvPlayer = null;
        }
    }

    function bindEventsToHlsPlayer(instance, hls, elem, onErrorFn, resolve, reject) {

        hls.on(Hls.Events.MANIFEST_PARSED, function () {
            playWithPromise(elem, onErrorFn).then(resolve, function () {

                if (reject) {
                    reject();
                    reject = null;
                }
            });
        });

        hls.on(Hls.Events.ERROR, function (event, data) {

            console.log('HLS Error: Type: ' + data.type + ' Details: ' + (data.details || '') + ' Fatal: ' + (data.fatal || false));

            switch (data.type) {
                case Hls.ErrorTypes.NETWORK_ERROR:
                    // try to recover network error
                    if (data.response && data.response.code && data.response.code >= 400) {

                        console.log('hls.js response error code: ' + data.response.code);

                        // Trigger failure differently depending on whether this is prior to start of playback, or after
                        hls.destroy();

                        if (reject) {
                            reject('servererror');
                            reject = null;
                        } else {
                            onErrorInternal(instance, 'servererror');
                        }

                        return;

                    }

                    break;
                default:
                    break;
            }

            if (data.fatal) {
                switch (data.type) {
                    case Hls.ErrorTypes.NETWORK_ERROR:

                        if (data.response && data.response.code === 0) {

                            // This could be a CORS error related to access control response headers

                            console.log('hls.js response error code: ' + data.response.code);

                            // Trigger failure differently depending on whether this is prior to start of playback, or after
                            hls.destroy();

                            if (reject) {
                                reject('network');
                                reject = null;
                            } else {
                                onErrorInternal(instance, 'network');
                            }
                        }

                        else {
                            console.log("fatal network error encountered, try to recover");
                            hls.startLoad();
                        }

                        break;
                    case Hls.ErrorTypes.MEDIA_ERROR:
                        console.log("fatal media error encountered, try to recover");
                        var currentReject = reject;
                        reject = null;
                        handleHlsJsMediaError(instance, currentReject);
                        break;
                    default:

                        console.log('Cannot recover from hls error - destroy and trigger error');
                        // cannot recover
                        // Trigger failure differently depending on whether this is prior to start of playback, or after
                        hls.destroy();

                        if (reject) {
                            reject();
                            reject = null;
                        } else {
                            onErrorInternal(instance, 'mediadecodeerror');
                        }
                        break;
                }
            }
        });
    }

    function onEndedInternal(instance, elem, onErrorFn) {

        elem.removeEventListener('error', onErrorFn);

        elem.src = '';
        elem.innerHTML = '';
        elem.removeAttribute("src");

        destroyHlsPlayer(instance);
        destroyFlvPlayer(instance);
        destroyShakaPlayer(instance);
        destroyCastPlayer(instance);

        var stopInfo = {
            src: instance._currentSrc
        };

        events.trigger(instance, 'stopped', [stopInfo]);

        instance._currentTime = null;
        instance._currentSrc = null;
        instance._currentPlayOptions = null;
    }

    function getBufferedRanges(instance, elem) {

        var ranges = [];
        var seekable = elem.buffered || [];

        var offset;
        var currentPlayOptions = instance._currentPlayOptions;
        if (currentPlayOptions) {
            offset = currentPlayOptions.transcodingOffsetTicks;
        }

        offset = offset || 0;

        for (var i = 0, length = seekable.length; i < length; i++) {

            var start = seekable.start(i);
            var end = seekable.end(i);

            if (!isValidDuration(start)) {
                start = 0;
            }
            if (!isValidDuration(end)) {
                end = 0;
                continue;
            }

            ranges.push({
                start: (start * 10000000) + offset,
                end: (end * 10000000) + offset
            });
        }

        return ranges;
    }

    return {
        getSavedVolume: getSavedVolume,
        saveVolume: saveVolume,
        enableHlsJsPlayer: enableHlsJsPlayer,
        enableHlsShakaPlayer: enableHlsShakaPlayer,
        handleHlsJsMediaError: handleHlsJsMediaError,
        isValidDuration: isValidDuration,
        onErrorInternal: onErrorInternal,
        seekOnPlaybackStart: seekOnPlaybackStart,
        applySrc: applySrc,
        playWithPromise: playWithPromise,
        destroyHlsPlayer: destroyHlsPlayer,
        destroyFlvPlayer: destroyFlvPlayer,
        destroyCastPlayer: destroyCastPlayer,
        bindEventsToHlsPlayer: bindEventsToHlsPlayer,
        onEndedInternal: onEndedInternal,
        getCrossOriginValue: getCrossOriginValue,
        getBufferedRanges: getBufferedRanges
    };
});