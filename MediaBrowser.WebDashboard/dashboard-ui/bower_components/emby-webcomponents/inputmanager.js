define(['playbackManager', 'focusManager', 'embyRouter', 'dom'], function (playbackManager, focusManager, embyRouter, dom) {
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
        scope.addEventListener('command', fn);
    }

    function off(scope, fn) {

        if (eventListenerCount) {
            eventListenerCount--;
        }

        scope.removeEventListener('command', fn);
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
                embyRouter.goHome();
                break;
            case 'settings':
                embyRouter.showSettings();
                break;
            case 'back':
                embyRouter.back();
                break;
            case 'forward':
                // TODO
                break;
            case 'select':
                select(sourceElement);
                break;
            case 'pageup':
                // TODO
                break;
            case 'pagedown':
                // TODO
                break;
            case 'end':
                // TODO
                break;
            case 'menu':
            case 'info':
                // TODO
                break;
            case 'next':
                if (playbackManager.isPlaying()) {
                    playbackManager.nextChapter();
                }
                break;
            case 'previous':

                if (playbackManager.isPlaying()) {
                    playbackManager.previousChapter();
                }
                break;
            case 'guide':
                embyRouter.showGuide();
                break;
            case 'recordedtv':
                embyRouter.showRecordedTV();
                break;
            case 'record':
                // TODO
                break;
            case 'livetv':
                embyRouter.showLiveTV();
                break;
            case 'mute':
                playbackManager.mute();
                break;
            case 'unmute':
                playbackManager.unMute();
                break;
            case 'togglemute':
                playbackManager.toggleMute();
                break;
            case 'channelup':
                playbackManager.nextTrack();
                break;
            case 'channeldown':
                playbackManager.previousTrack();
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
                // TODO
                break;
            case 'changeaudiotrack':
                // TODO
                break;
            case 'changesubtitletrack':
                break;
            case 'search':
                embyRouter.showSearch();
                break;
            case 'favorites':
                embyRouter.showFavorites();
                break;
            case 'fastforward':
                playbackManager.fastForward();
                break;
            case 'rewind':
                playbackManager.rewind();
                break;
            case 'togglefullscreen':
                // TODO
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
            case 'movies':
                // TODO
                break;
            case 'music':
                // TODO
                break;
            case 'tv':
                // TODO
                break;
            case 'latestepisodes':
                // TODO
                break;
            case 'nowplaying':
                // TODO
                break;
            case 'upcomingtv':
                // TODO
                break;
            case 'nextup':
                // TODO
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