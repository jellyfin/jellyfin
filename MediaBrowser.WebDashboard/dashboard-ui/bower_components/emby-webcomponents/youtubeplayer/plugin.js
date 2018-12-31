define(['require', 'events', 'browser', 'appRouter', 'loading'], function (require, events, browser, appRouter, loading) {
    "use strict";

    function zoomIn(elem, iterations) {
        var keyframes = [
            { transform: 'scale3d(.2, .2, .2)  ', opacity: '.6', offset: 0 },
            { transform: 'none', opacity: '1', offset: 1 }
        ];

        var timing = { duration: 240, iterations: iterations };
        return elem.animate(keyframes, timing);
    }

    function createMediaElement(instance, options) {

        return new Promise(function (resolve, reject) {

            var dlg = document.querySelector('.youtubePlayerContainer');

            if (!dlg) {

                require(['css!./style'], function () {

                    loading.show();

                    var dlg = document.createElement('div');

                    dlg.classList.add('youtubePlayerContainer');

                    if (options.fullscreen) {
                        dlg.classList.add('onTop');
                    }

                    dlg.innerHTML = '<div id="player"></div>';
                    var videoElement = dlg.querySelector('#player');

                    document.body.insertBefore(dlg, document.body.firstChild);
                    instance.videoDialog = dlg;

                    if (options.fullscreen && dlg.animate && !browser.slow) {
                        zoomIn(dlg, 1).onfinish = function () {
                            resolve(videoElement);
                        };
                    } else {
                        resolve(videoElement);
                    }

                });

            } else {
                resolve(dlg.querySelector('#player'));
            }
        });
    }

    function onVideoResize() {
        var instance = this;
        var player = instance.currentYoutubePlayer;
        var dlg = instance.videoDialog;
        if (player && dlg) {
            player.setSize(dlg.offsetWidth, dlg.offsetHeight);
        }
    }

    function clearTimeUpdateInterval(instance) {
        if (instance.timeUpdateInterval) {
            clearInterval(instance.timeUpdateInterval);
        }
        instance.timeUpdateInterval = null;
    }

    function onEndedInternal(instance) {

        clearTimeUpdateInterval(instance);
        var resizeListener = instance.resizeListener;
        if (resizeListener) {
            window.removeEventListener('resize', resizeListener);
            window.removeEventListener('orientationChange', resizeListener);
            instance.resizeListener = null;
        }

        var stopInfo = {
            src: instance._currentSrc
        };

        events.trigger(instance, 'stopped', [stopInfo]);

        instance._currentSrc = null;
        if (instance.currentYoutubePlayer) {
            instance.currentYoutubePlayer.destroy();
        }
        instance.currentYoutubePlayer = null;
    }

    // 4. The API will call this function when the video player is ready.
    function onPlayerReady(event) {
        event.target.playVideo();
    }

    function onTimeUpdate(e) {

        events.trigger(this, 'timeupdate');
    }

    function onPlaying(instance, playOptions, resolve) {

        if (!instance.started) {

            instance.started = true;
            resolve();
            clearTimeUpdateInterval(instance);
            instance.timeUpdateInterval = setInterval(onTimeUpdate.bind(instance), 500);

            if (playOptions.fullscreen) {

                appRouter.showVideoOsd().then(function () {
                    instance.videoDialog.classList.remove('onTop');
                });

            } else {
                appRouter.setTransparency('backdrop');
                instance.videoDialog.classList.remove('onTop');
            }

            require(['loading'], function (loading) {

                loading.hide();
            });
        }
    }

    function setCurrentSrc(instance, elem, options) {

        return new Promise(function (resolve, reject) {

            require(['queryString'], function (queryString) {


                instance._currentSrc = options.url;
                var params = queryString.parse(options.url.split('?')[1]);
                // 3. This function creates an <iframe> (and YouTube player)
                //    after the API code downloads.
                window.onYouTubeIframeAPIReady = function () {
                    instance.currentYoutubePlayer = new YT.Player('player', {
                        height: instance.videoDialog.offsetHeight,
                        width: instance.videoDialog.offsetWidth,
                        videoId: params.v,
                        events: {
                            'onReady': onPlayerReady,
                            'onStateChange': function (event) {
                                if (event.data === YT.PlayerState.PLAYING) {
                                    onPlaying(instance, options, resolve);
                                } else if (event.data === YT.PlayerState.ENDED) {
                                    onEndedInternal(instance);
                                } else if (event.data === YT.PlayerState.PAUSED) {
                                    events.trigger(instance, 'pause');
                                }
                            }
                        },
                        playerVars: {
                            controls: 0,
                            enablejsapi: 1,
                            modestbranding: 1,
                            rel: 0,
                            showinfo: 0,
                            fs: 0,
                            playsinline: 1
                        }
                    });

                    var resizeListener = instance.resizeListener;
                    if (resizeListener) {
                        window.removeEventListener('resize', resizeListener);
                        window.addEventListener('resize', resizeListener);
                    } else {
                        resizeListener = instance.resizeListener = onVideoResize.bind(instance);
                        window.addEventListener('resize', resizeListener);
                    }
                    window.removeEventListener('orientationChange', resizeListener);
                    window.addEventListener('orientationChange', resizeListener);
                };

                if (!window.YT) {
                    var tag = document.createElement('script');
                    tag.src = "https://www.youtube.com/iframe_api";
                    var firstScriptTag = document.getElementsByTagName('script')[0];
                    firstScriptTag.parentNode.insertBefore(tag, firstScriptTag);
                } else {
                    window.onYouTubeIframeAPIReady();
                }
            });

        });
    }

    function YoutubePlayer() {

        this.name = 'Youtube Player';
        this.type = 'mediaplayer';
        this.id = 'youtubeplayer';

        // Let any players created by plugins take priority
        this.priority = 1;
    }

    YoutubePlayer.prototype.play = function (options) {

        this.started = false;
        var instance = this;

        return createMediaElement(this, options).then(function (elem) {

            return setCurrentSrc(instance, elem, options);
        });
    };

    YoutubePlayer.prototype.stop = function (destroyPlayer) {

        var src = this._currentSrc;

        if (src) {

            if (this.currentYoutubePlayer) {
                this.currentYoutubePlayer.stopVideo();
            }
            onEndedInternal(this);

            if (destroyPlayer) {
                this.destroy();
            }
        }

        return Promise.resolve();
    };

    YoutubePlayer.prototype.destroy = function () {

        appRouter.setTransparency('none');

        var dlg = this.videoDialog;
        if (dlg) {

            this.videoDialog = null;

            dlg.parentNode.removeChild(dlg);
        }
    };

    YoutubePlayer.prototype.canPlayMediaType = function (mediaType) {

        mediaType = (mediaType || '').toLowerCase();

        return mediaType === 'audio' || mediaType === 'video';
    };

    YoutubePlayer.prototype.canPlayItem = function (item) {

        // Does not play server items
        return false;
    };

    YoutubePlayer.prototype.canPlayUrl = function (url) {

        return url.toLowerCase().indexOf('youtube.com') !== -1;
    };

    YoutubePlayer.prototype.getDeviceProfile = function () {

        return Promise.resolve({});
    };

    YoutubePlayer.prototype.currentSrc = function () {
        return this._currentSrc;
    };

    YoutubePlayer.prototype.setSubtitleStreamIndex = function (index) {
    };

    YoutubePlayer.prototype.canSetAudioStreamIndex = function () {
        return false;
    };

    YoutubePlayer.prototype.setAudioStreamIndex = function (index) {

    };

    // Save this for when playback stops, because querying the time at that point might return 0
    YoutubePlayer.prototype.currentTime = function (val) {

        var currentYoutubePlayer = this.currentYoutubePlayer;

        if (currentYoutubePlayer) {
            if (val != null) {
                currentYoutubePlayer.seekTo(val / 1000, true);
                return;
            }

            return currentYoutubePlayer.getCurrentTime() * 1000;
        }
    };

    YoutubePlayer.prototype.duration = function (val) {

        var currentYoutubePlayer = this.currentYoutubePlayer;

        if (currentYoutubePlayer) {
            return currentYoutubePlayer.getDuration() * 1000;
        }
        return null;
    };

    YoutubePlayer.prototype.pause = function () {

        var currentYoutubePlayer = this.currentYoutubePlayer;

        if (currentYoutubePlayer) {
            currentYoutubePlayer.pauseVideo();

            var instance = this;

            // This needs a delay before the youtube player will report the correct player state
            setTimeout(function () {
                events.trigger(instance, 'pause');
            }, 200);
        }
    };

    YoutubePlayer.prototype.unpause = function () {

        var currentYoutubePlayer = this.currentYoutubePlayer;

        if (currentYoutubePlayer) {
            currentYoutubePlayer.playVideo();

            var instance = this;

            // This needs a delay before the youtube player will report the correct player state
            setTimeout(function () {
                events.trigger(instance, 'unpause');
            }, 200);
        }
    };

    YoutubePlayer.prototype.paused = function () {

        var currentYoutubePlayer = this.currentYoutubePlayer;

        if (currentYoutubePlayer) {
            return currentYoutubePlayer.getPlayerState() === 2;
        }

        return false;
    };

    YoutubePlayer.prototype.volume = function (val) {
        if (val != null) {
            return this.setVolume(val);
        }

        return this.getVolume();
    };

    YoutubePlayer.prototype.setVolume = function (val) {

        var currentYoutubePlayer = this.currentYoutubePlayer;

        if (currentYoutubePlayer) {
            if (val != null) {
                currentYoutubePlayer.setVolume(val);
            }
        }
    };

    YoutubePlayer.prototype.getVolume = function () {

        var currentYoutubePlayer = this.currentYoutubePlayer;

        if (currentYoutubePlayer) {
            return currentYoutubePlayer.getVolume();
        }
    };

    YoutubePlayer.prototype.setMute = function (mute) {

        var currentYoutubePlayer = this.currentYoutubePlayer;

        if (mute) {
            if (currentYoutubePlayer) {
                currentYoutubePlayer.mute();
            }
        } else {

            if (currentYoutubePlayer) {
                currentYoutubePlayer.unMute();
            }
        }
    };

    YoutubePlayer.prototype.isMuted = function () {

        var currentYoutubePlayer = this.currentYoutubePlayer;

        if (currentYoutubePlayer) {
            return currentYoutubePlayer.isMuted();
        }
    };

    return YoutubePlayer;
});