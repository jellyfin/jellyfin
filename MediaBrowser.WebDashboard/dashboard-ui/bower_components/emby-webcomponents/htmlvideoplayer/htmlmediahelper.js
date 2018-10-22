define(["appSettings", "browser", "events"], function(appSettings, browser, events) {
    "use strict";

    function getSavedVolume() {
        return appSettings.get("volume") || 1
    }

    function saveVolume(value) {
        value && appSettings.set("volume", value)
    }

    function getCrossOriginValue(mediaSource) {
        return mediaSource.IsRemote ? null : "anonymous"
    }

    function canPlayNativeHls() {
        var media = document.createElement("video");
        return !(!media.canPlayType("application/x-mpegURL").replace(/no/, "") && !media.canPlayType("application/vnd.apple.mpegURL").replace(/no/, ""))
    }

    function enableHlsShakaPlayer(item, mediaSource, mediaType) {
        if (window.MediaSource && MediaSource.isTypeSupported) {
            if (canPlayNativeHls()) {
                if (browser.edge && "Video" === mediaType) return !0;
                mediaSource.RunTimeTicks
            }
            return !0
        }
        return !1
    }

    function enableHlsJsPlayer(runTimeTicks, mediaType) {
        if (null == window.MediaSource) return !1;
        if (browser.iOS) return !1;
        if (browser.tizen || browser.web0s) return !1;
        if (canPlayNativeHls()) {
            if (browser.android && "Audio" === mediaType) return !0;
            if (browser.edge, runTimeTicks) return !1
        }
        return !0
    }

    function handleHlsJsMediaError(instance, reject) {
        var hlsPlayer = instance._hlsPlayer;
        if (hlsPlayer) {
            var now = Date.now();
            window.performance && window.performance.now && (now = performance.now()), !recoverDecodingErrorDate || now - recoverDecodingErrorDate > 3e3 ? (recoverDecodingErrorDate = now, console.log("try to recover media Error ..."), hlsPlayer.recoverMediaError()) : !recoverSwapAudioCodecDate || now - recoverSwapAudioCodecDate > 3e3 ? (recoverSwapAudioCodecDate = now, console.log("try to swap Audio Codec and recover media Error ..."), hlsPlayer.swapAudioCodec(), hlsPlayer.recoverMediaError()) : (console.error("cannot recover, last media error recovery failed ..."), reject ? reject() : onErrorInternal(instance, "mediadecodeerror"))
        }
    }

    function onErrorInternal(instance, type) {
        instance.destroyCustomTrack && instance.destroyCustomTrack(instance._mediaElement), events.trigger(instance, "error", [{
            type: type
        }])
    }

    function isValidDuration(duration) {
        return !(!duration || isNaN(duration) || duration === Number.POSITIVE_INFINITY || duration === Number.NEGATIVE_INFINITY)
    }

    function setCurrentTimeIfNeeded(element, seconds, allowance) {
        Math.abs((element.currentTime || 0) - seconds) >= allowance && (element.currentTime = seconds)
    }

    function seekOnPlaybackStart(instance, element, ticks) {
        var seconds = (ticks || 0) / 1e7;
        if (seconds) {
            (instance.currentSrc() || "").toLowerCase();
            setCurrentTimeIfNeeded(element, seconds, 5), setTimeout(function() {
                setCurrentTimeIfNeeded(element, seconds, 10)
            }, 2500)
        }
    }

    function applySrc(elem, src, options) {
        return window.Windows && options.mediaSource && options.mediaSource.IsLocal ? Windows.Storage.StorageFile.getFileFromPathAsync(options.url).then(function(file) {
            var playlist = new Windows.Media.Playback.MediaPlaybackList,
                source1 = Windows.Media.Core.MediaSource.createFromStorageFile(file),
                startTime = (options.playerStartPositionTicks || 0) / 1e4;
            return playlist.items.append(new Windows.Media.Playback.MediaPlaybackItem(source1, startTime)), elem.src = URL.createObjectURL(playlist, {
                oneTimeOnly: !0
            }), Promise.resolve()
        }) : (elem.src = src, Promise.resolve())
    }

    function onSuccessfulPlay(elem, onErrorFn) {
        elem.addEventListener("error", onErrorFn)
    }

    function playWithPromise(elem, onErrorFn) {
        try {
            var promise = elem.play();
            return promise && promise.then ? promise.catch(function(e) {
                var errorName = (e.name || "").toLowerCase();
                return "notallowederror" === errorName || "aborterror" === errorName ? (onSuccessfulPlay(elem, onErrorFn), Promise.resolve()) : Promise.reject()
            }) : (onSuccessfulPlay(elem, onErrorFn), Promise.resolve())
        } catch (err) {
            return console.log("error calling video.play: " + err), Promise.reject()
        }
    }

    function destroyCastPlayer(instance) {
        var player = instance._castPlayer;
        if (player) {
            try {
                player.unload()
            } catch (err) {
                console.log(err)
            }
            instance._castPlayer = null
        }
    }

    function destroyShakaPlayer(instance) {
        var player = instance._shakaPlayer;
        if (player) {
            try {
                player.destroy()
            } catch (err) {
                console.log(err)
            }
            instance._shakaPlayer = null
        }
    }

    function destroyHlsPlayer(instance) {
        var player = instance._hlsPlayer;
        if (player) {
            try {
                player.destroy()
            } catch (err) {
                console.log(err)
            }
            instance._hlsPlayer = null
        }
    }

    function destroyFlvPlayer(instance) {
        var player = instance._flvPlayer;
        if (player) {
            try {
                player.unload(), player.detachMediaElement(), player.destroy()
            } catch (err) {
                console.log(err)
            }
            instance._flvPlayer = null
        }
    }

    function bindEventsToHlsPlayer(instance, hls, elem, onErrorFn, resolve, reject) {
        hls.on(Hls.Events.MANIFEST_PARSED, function() {
            playWithPromise(elem, onErrorFn).then(resolve, function() {
                reject && (reject(), reject = null)
            })
        }), hls.on(Hls.Events.ERROR, function(event, data) {
            switch (console.log("HLS Error: Type: " + data.type + " Details: " + (data.details || "") + " Fatal: " + (data.fatal || !1)), data.type) {
                case Hls.ErrorTypes.NETWORK_ERROR:
                    if (data.response && data.response.code && data.response.code >= 400) return console.log("hls.js response error code: " + data.response.code), hls.destroy(), void(reject ? (reject("servererror"), reject = null) : onErrorInternal(instance, "servererror"))
            }
            if (data.fatal) switch (data.type) {
                case Hls.ErrorTypes.NETWORK_ERROR:
                    data.response && 0 === data.response.code ? (console.log("hls.js response error code: " + data.response.code), hls.destroy(), reject ? (reject("network"), reject = null) : onErrorInternal(instance, "network")) : (console.log("fatal network error encountered, try to recover"), hls.startLoad());
                    break;
                case Hls.ErrorTypes.MEDIA_ERROR:
                    console.log("fatal media error encountered, try to recover");
                    var currentReject = reject;
                    reject = null, handleHlsJsMediaError(instance, currentReject);
                    break;
                default:
                    console.log("Cannot recover from hls error - destroy and trigger error"), hls.destroy(), reject ? (reject(), reject = null) : onErrorInternal(instance, "mediadecodeerror")
            }
        })
    }

    function onEndedInternal(instance, elem, onErrorFn) {
        elem.removeEventListener("error", onErrorFn), elem.src = "", elem.innerHTML = "", elem.removeAttribute("src"), destroyHlsPlayer(instance), destroyFlvPlayer(instance), destroyShakaPlayer(instance), destroyCastPlayer(instance);
        var stopInfo = {
            src: instance._currentSrc
        };
        events.trigger(instance, "stopped", [stopInfo]), instance._currentTime = null, instance._currentSrc = null, instance._currentPlayOptions = null
    }

    function getBufferedRanges(instance, elem) {
        var offset, ranges = [],
            seekable = elem.buffered || [],
            currentPlayOptions = instance._currentPlayOptions;
        currentPlayOptions && (offset = currentPlayOptions.transcodingOffsetTicks), offset = offset || 0;
        for (var i = 0, length = seekable.length; i < length; i++) {
            var start = seekable.start(i),
                end = seekable.end(i);
            isValidDuration(start) || (start = 0), isValidDuration(end) ? ranges.push({
                start: 1e7 * start + offset,
                end: 1e7 * end + offset
            }) : end = 0
        }
        return ranges
    }
    var recoverDecodingErrorDate, recoverSwapAudioCodecDate;
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
    }
});