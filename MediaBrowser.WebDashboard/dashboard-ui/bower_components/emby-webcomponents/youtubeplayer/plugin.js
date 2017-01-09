define(['pluginManager', 'events', 'browser', 'embyRouter'], function (pluginManager, Events, browser, embyRouter) {
    "use strict";

    return function () {

        var self = this;

        self.name = 'Youtube Player';
        self.type = 'mediaplayer';
        self.id = 'youtubeplayer';

        // Let any players created by plugins take priority
        self.priority = 1;

        var videoDialog;
        var currentSrc;
        var started = false;

        var currentYoutubePlayer;
        var timeUpdateInterval;

        self.canPlayMediaType = function (mediaType) {

            mediaType = (mediaType || '').toLowerCase();

            return mediaType === 'audio' || mediaType === 'video';
        };

        self.canPlayItem = function (item) {

            // Does not play server items
            return false;
        };

        self.canPlayUrl = function (url) {

            return url.toLowerCase().indexOf('youtube.com') !== -1;
        };

        self.getDeviceProfile = function () {

            return Promise.resolve({});
        };

        self.currentSrc = function () {
            return currentSrc;
        };

        self.play = function (options) {

            started = false;

            return createMediaElement(options).then(function (elem) {

                return setCurrentSrc(elem, options);
            });
        };

        function setCurrentSrc(elem, options) {

            return new Promise(function (resolve, reject) {

                require(['queryString'], function (queryString) {


                    currentSrc = options.url;
                    var params = queryString.parse(options.url.split('?')[1]);
                    // 3. This function creates an <iframe> (and YouTube player)
                    //    after the API code downloads.
                    window.onYouTubeIframeAPIReady = function () {
                        currentYoutubePlayer = new YT.Player('player', {
                            height: videoDialog.offsetHeight,
                            width: videoDialog.offsetWidth,
                            videoId: params.v,
                            events: {
                                'onReady': onPlayerReady,
                                'onStateChange': function (event) {
                                    if (event.data === YT.PlayerState.PLAYING) {
                                        onPlaying(options, resolve);
                                    } else if (event.data === YT.PlayerState.ENDED) {
                                        onEnded();
                                    } else if (event.data === YT.PlayerState.PAUSED) {
                                        onPause();
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

                        window.removeEventListener('resize', onVideoResize);
                        window.addEventListener('resize', onVideoResize);
                        window.removeEventListener('orientationChange', onVideoResize);
                        window.addEventListener('orientationChange', onVideoResize);
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

        function onVideoResize() {
            var player = currentYoutubePlayer;
            var dlg = videoDialog;
            if (player && dlg) {
                player.setSize(dlg.offsetWidth, dlg.offsetHeight);
            }
        }

        // 4. The API will call this function when the video player is ready.
        function onPlayerReady(event) {
            event.target.playVideo();
        }

        self.setSubtitleStreamIndex = function (index) {
        };

        self.canSetAudioStreamIndex = function () {
            return false;
        };

        self.setAudioStreamIndex = function (index) {

        };

        // Save this for when playback stops, because querying the time at that point might return 0
        self.currentTime = function (val) {

            if (currentYoutubePlayer) {
                if (val != null) {
                    currentYoutubePlayer.setCurrentTime(val / 1000);
                    return;
                }

                return currentYoutubePlayer.getCurrentTime() * 1000;
            }
        };

        self.duration = function (val) {

            if (currentYoutubePlayer) {
                return currentYoutubePlayer.getDuration() * 1000;
            }
            return null;
        };

        self.stop = function (destroyPlayer, reportEnded) {

            var src = currentSrc;

            if (src) {

                if (currentYoutubePlayer) {
                    currentYoutubePlayer.stopVideo();
                }
                onEndedInternal(reportEnded);

                if (destroyPlayer) {
                    self.destroy();
                }
            }

            return Promise.resolve();
        };

        self.destroy = function () {

            embyRouter.setTransparency('none');

            var dlg = videoDialog;
            if (dlg) {

                videoDialog = null;

                dlg.parentNode.removeChild(dlg);
            }
        };

        self.pause = function () {
            if (currentYoutubePlayer) {
                currentYoutubePlayer.pauseVideo();

                // This needs a delay before the youtube player will report the correct player state
                setTimeout(onPause, 200);
            }
        };

        self.unpause = function () {
            if (currentYoutubePlayer) {
                currentYoutubePlayer.playVideo();

                // This needs a delay before the youtube player will report the correct player state
                setTimeout(onPlaying, 200);
            }
        };

        self.paused = function () {

            if (currentYoutubePlayer) {
                console.log(currentYoutubePlayer.getPlayerState());
                return currentYoutubePlayer.getPlayerState() === 2;
            }

            return false;
        };

        self.volume = function (val) {
            if (val != null) {
                return self.setVolume(val);
            }

            return self.getVolume();
        };

        self.setVolume = function (val) {
            if (currentYoutubePlayer) {
                if (val != null) {
                    currentYoutubePlayer.setVolume(val);
                }
            }
        };

        self.getVolume = function () {
            if (currentYoutubePlayer) {
                return currentYoutubePlayer.getVolume();
            }
        };

        self.setMute = function (mute) {

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

        self.isMuted = function () {
            if (currentYoutubePlayer) {
                currentYoutubePlayer.isMuted();
            }
        };

        function onEnded() {

            onEndedInternal(true);
        }

        function clearTimeUpdateInterval() {
            if (timeUpdateInterval) {
                clearInterval(timeUpdateInterval);
            }
            timeUpdateInterval = null;
        }

        function onEndedInternal(triggerEnded) {

            clearTimeUpdateInterval();
            window.removeEventListener('resize', onVideoResize);
            window.removeEventListener('orientationChange', onVideoResize);

            if (triggerEnded) {
                var stopInfo = {
                    src: currentSrc
                };

                Events.trigger(self, 'stopped', [stopInfo]);
            }

            currentSrc = null;
            if (currentYoutubePlayer) {
                currentYoutubePlayer.destroy();
            }
            currentYoutubePlayer = null;
        }

        function onTimeUpdate(e) {

            Events.trigger(self, 'timeupdate');
        }

        function onVolumeChange() {

            Events.trigger(self, 'volumechange');
        }

        function onPlaying(playOptions, resolve) {

            if (!started) {

                started = true;
                resolve();
                clearTimeUpdateInterval();
                timeUpdateInterval = setInterval(onTimeUpdate, 500);

                if (playOptions.fullscreen) {

                    embyRouter.showVideoOsd().then(function () {
                        videoDialog.classList.remove('onTop');
                    });

                } else {
                    embyRouter.setTransparency('backdrop');
                    videoDialog.classList.remove('onTop');
                }

                require(['loading'], function (loading) {

                    loading.hide();
                });
            }
            Events.trigger(self, 'playing');
        }

        function onClick() {
            Events.trigger(self, 'click');
        }

        function onDblClick() {
            Events.trigger(self, 'dblclick');
        }

        function onPause() {
            Events.trigger(self, 'pause');
        }

        function onError() {

            var errorCode = this.error ? this.error.code : '';
            console.log('Media element error code: ' + errorCode);

            Events.trigger(self, 'error');
        }

        function zoomIn(elem, iterations) {
            var keyframes = [
                { transform: 'scale3d(.2, .2, .2)  ', opacity: '.6', offset: 0 },
                { transform: 'none', opacity: '1', offset: 1 }
            ];

            var timing = { duration: 240, iterations: iterations };
            return elem.animate(keyframes, timing);
        }

        function createMediaElement(options) {

            return new Promise(function (resolve, reject) {

                var dlg = document.querySelector('.youtubePlayerContainer');

                if (!dlg) {

                    require(['loading', 'css!' + pluginManager.mapPath(self, 'style.css')], function (loading) {

                        loading.show();

                        var dlg = document.createElement('div');

                        dlg.classList.add('youtubePlayerContainer');

                        if (options.fullscreen) {
                            dlg.classList.add('onTop');
                        }

                        dlg.innerHTML = '<div id="player"></div>';
                        var videoElement = dlg.querySelector('#player');

                        document.body.insertBefore(dlg, document.body.firstChild);
                        videoDialog = dlg;

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
    };
});