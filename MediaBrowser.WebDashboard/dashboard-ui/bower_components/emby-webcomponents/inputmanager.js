define(['playbackManager', 'focusManager', 'appRouter', 'dom'], function (playbackManager, focusManager, appRouter, dom) {
    'use strict';

    var lastInputTime = new Date().getTime();

    function notify() {
        lastInputTime = new Date().getTime();

        handleCommand('unknown');
    }

    function notifyMouseMove() {
        lastInputTime = new Date().getTime();
    }

    function idleTime() {
        return new Date().getTime() - lastInputTime;
    }

    function select(sourceElement) {

        sourceElement.click();
    }

    var eventListenerCount = 0;
    function on(scope, fn) {
        eventListenerCount++;
        dom.addEventListener(scope, 'command', fn, {

        });
    }

    function off(scope, fn) {

        if (eventListenerCount) {
            eventListenerCount--;
        }

        dom.removeEventListener(scope, 'command', fn, {

        });
    }

    var commandTimes = {};

    function checkCommandTime(command) {

        var last = commandTimes[command] || 0;
        var now = new Date().getTime();

        if ((now - last) < 1000) {
            return false;
        }

        commandTimes[command] = now;
        return true;
    }

    function handleCommand(name, options) {

        lastInputTime = new Date().getTime();

        var sourceElement = (options ? options.sourceElement : null);

        if (sourceElement) {
            sourceElement = focusManager.focusableParent(sourceElement);
        }

        sourceElement = sourceElement || document.activeElement || window;

        if (eventListenerCount) {
            var customEvent = new CustomEvent("command", {
                detail: {
                    command: name
                },
                bubbles: true,
                cancelable: true
            });

            var eventResult = sourceElement.dispatchEvent(customEvent);
            if (!eventResult) {
                // event cancelled
                return;
            }
        }

        switch (name) {

            case 'up':
                focusManager.moveUp(sourceElement);
                break;
            case 'down':
                focusManager.moveDown(sourceElement);
                break;
            case 'left':
                focusManager.moveLeft(sourceElement);
                break;
            case 'right':
                focusManager.moveRight(sourceElement);
                break;
            case 'home':
                appRouter.goHome();
                break;
            case 'settings':
                appRouter.showSettings();
                break;
            case 'back':
                appRouter.back();
                break;
            case 'forward':
                break;
            case 'select':
                select(sourceElement);
                break;
            case 'pageup':
                break;
            case 'pagedown':
                break;
            case 'end':
                break;
            case 'menu':
            case 'info':
                break;
            case 'nextchapter':
                playbackManager.nextChapter();
                break;
            case 'next':
            case 'nexttrack':
                playbackManager.nextTrack();
                break;
            case 'previous':
            case 'previoustrack':
                playbackManager.previousTrack();
                break;
            case 'previouschapter':
                playbackManager.previousChapter();
                break;
            case 'guide':
                appRouter.showGuide();
                break;
            case 'recordedtv':
                appRouter.showRecordedTV();
                break;
            case 'record':
                break;
            case 'livetv':
                appRouter.showLiveTV();
                break;
            case 'mute':
                playbackManager.setMute(true);
                break;
            case 'unmute':
                playbackManager.setMute(false);
                break;
            case 'togglemute':
                playbackManager.toggleMute();
                break;
            case 'channelup':
                playbackManager.channelUp();
                break;
            case 'channeldown':
                playbackManager.channelDown();
                break;
            case 'volumedown':
                playbackManager.volumeDown();
                break;
            case 'volumeup':
                playbackManager.volumeUp();
                break;
            case 'play':
                playbackManager.unpause();
                break;
            case 'pause':
                playbackManager.pause();
                break;
            case 'playpause':
                playbackManager.playPause();
                break;
            case 'stop':
                if (checkCommandTime('stop')) {
                    playbackManager.stop();
                }
                break;
            case 'changezoom':
                playbackManager.toggleAspectRatio();
                break;
            case 'changeaudiotrack':
                playbackManager.changeAudioStream();
                break;
            case 'changesubtitletrack':
                playbackManager.changeSubtitleStream();
                break;
            case 'search':
                appRouter.showSearch();
                break;
            case 'favorites':
                appRouter.showFavorites();
                break;
            case 'fastforward':
                playbackManager.fastForward();
                break;
            case 'rewind':
                playbackManager.rewind();
                break;
            case 'togglefullscreen':
                playbackManager.toggleFullscreen();
                break;
            case 'disabledisplaymirror':
                playbackManager.enableDisplayMirroring(false);
                break;
            case 'enabledisplaymirror':
                playbackManager.enableDisplayMirroring(true);
                break;
            case 'toggledisplaymirror':
                playbackManager.toggleDisplayMirroring();
                break;
            case 'togglestats':
                //playbackManager.toggleStats();
                break;
            case 'movies':
                // TODO
                appRouter.goHome();
                break;
            case 'music':
                // TODO
                appRouter.goHome();
                break;
            case 'tv':
                // TODO
                appRouter.goHome();
                break;
            case 'nowplaying':
                appRouter.showNowPlaying();
                break;
            case 'save':
                break;
            case 'screensaver':
                // TODO
                break;
            case 'refresh':
                // TODO
                break;
            case 'changebrightness':
                // TODO
                break;
            case 'red':
                // TODO
                break;
            case 'green':
                // TODO
                break;
            case 'yellow':
                // TODO
                break;
            case 'blue':
                // TODO
                break;
            case 'grey':
                // TODO
                break;
            case 'brown':
                // TODO
                break;
            case 'repeatnone':
                playbackManager.setRepeatMode('RepeatNone');
                break;
            case 'repeatall':
                playbackManager.setRepeatMode('RepeatAll');
                break;
            case 'repeatone':
                playbackManager.setRepeatMode('RepeatOne');
                break;
            default:
                break;
        }
    }

    dom.addEventListener(document, 'click', notify, {
        passive: true
    });

    return {
        trigger: handleCommand,
        handle: handleCommand,
        notify: notify,
        notifyMouseMove: notifyMouseMove,
        idleTime: idleTime,
        on: on,
        off: off
    };
});