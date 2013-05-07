(function (document, clearTimeout, screen, localStorage, _V_, $, setInterval, window) {

    function mediaPlayer() {
        var self = this;

        var testableAudioElement = document.createElement('audio');
        var testableVideoElement = document.createElement('video');
        var currentMediaElement;
        var currentProgressInterval;

	    if (typeof(self.playing) == 'undefined') {
		    self.playing = '';
	    }

	    if (typeof(self.queue) == 'undefined') {
		    self.queue = [];
	    }

        function endsWith(text, pattern) {

            text = text.toLowerCase();
            pattern = pattern.toLowerCase();

            var d = text.length - pattern.length;
            return d >= 0 && text.lastIndexOf(pattern) === d;
        }

        function playAudio(item, params) {

            var volume = localStorage.getItem("volume") || 0.5;

            var baseParams = {
                audioChannels: 2,
                audioBitrate: 128000
            };

            $.extend(baseParams, params);

            var mp3Url = ApiClient.getUrl('Audio/' + item.Id + '/stream.mp3', $.extend({}, baseParams, {
                audioCodec: 'mp3'
            }));

            var aacUrl = ApiClient.getUrl('Audio/' + item.Id + '/stream.aac', $.extend({}, baseParams, {
                audioCodec: 'aac'
            }));

            var webmUrl = ApiClient.getUrl('Audio/' + item.Id + '/stream.webm', $.extend({}, baseParams, {
                audioCodec: 'Vorbis'
            }));

            var mediaStreams = item.MediaStreams || [];

            for (var i = 0, length = mediaStreams.length; i < length; i++) {

                var stream = mediaStreams[i];

                if (stream.Type == "Audio") {

                    // Stream statically when possible
                    if (endsWith(item.Path, ".aac") && stream.BitRate <= 256000) {
                        aacUrl += "&static=true";
                    }
                    else if (endsWith(item.Path, ".mp3") && stream.BitRate <= 256000) {
                        mp3Url += "&static=true";
                    }
                    break;
                }

            }

            /* ffmpeg always says the ogg stream is corrupt after conversion
             var oggUrl = ApiClient.getUrl('Audio/' + item.Id + '/stream.oga', $.extend({}, baseParams, {
             audioCodec: 'Vorbis'
             }));
             */

            var html = '';
            html += '<audio class="itemAudio" preload="none" controls autoplay>';
            html += '<source type="audio/mpeg" src="' + mp3Url + '" />';
            html += '<source type="audio/aac" src="' + aacUrl + '" />';
            html += '<source type="audio/webm" src="' + webmUrl + '" />';
            //html += '<source type="audio/ogg" src="' + oggUrl + '" />';
            html += '</audio';

            var nowPlayingBar = $('#nowPlayingBar').show();
            //show stop button
            $('#stopButton', nowPlayingBar).show();

            $('#mediaElement', nowPlayingBar).html(html);

            $(".itemAudio").each(function () {
                this.volume = volume;
            });

            $(".itemAudio").on("ended", function () {
                MediaPlayer.stopAudio(item.Id);

	            MediaPlayer.queuePlayNext();
            });

            $(".itemAudio").on("volumechange", function () {
                localStorage.setItem("volume", this.volume);
            });

            $(".itemAudio").on("play", updateAudioProgress(item.Id));

	        MediaPlayer.nowPlaying(item);

            return $('audio', nowPlayingBar)[0];
        }

        function updateAudioProgress(itemId) {
            ApiClient.reportPlaybackStart(Dashboard.getCurrentUserId(), itemId);

            currentProgressInterval = setInterval(function () {
                var position;
                $(".itemAudio").each(function () {
                    position = Math.floor(10000000 * this.currentTime);
                });

                ApiClient.reportPlaybackProgress(Dashboard.getCurrentUserId(), itemId, position);
            }, 30000);
        }

        function playVideo(item, startPosition) {

            //stop/kill videoJS
            if (currentMediaElement) self.stop();

            // Account for screen rotation. Use the larger dimension as the width.
            var screenWidth = Math.max(screen.height, screen.width);
            var screenHeight = Math.min(screen.height, screen.width);

            var volume = localStorage.getItem("volume") || 0.5;
            var user = Dashboard.getCurrentUser();
            var defaults = { languageIndex: null, subtitleIndex: null };

            var userConfig = user.Configuration || {};
            if (item.MediaStreams && item.MediaStreams.length) {
                $.each(item.MediaStreams, function (i, stream) {
                    //get default subtitle stream
                    if (stream.Type == "Subtitle") {
                        if (userConfig.UseForcedSubtitlesOnly == true && userConfig.SubtitleLanguagePreference && !defaults.subtitleIndex) {
                            if (stream.Language == userConfig.SubtitleLanguagePreference && stream.IsForced == true) {
                                defaults.subtitleIndex = i;
                            }
                        } else if (userConfig.SubtitleLanguagePreference && !defaults.subtitleIndex) {
                            if (stream.Language == userConfig.SubtitleLanguagePreference) {
                                defaults.subtitleIndex = i;
                            }
                        } else if (userConfig.UseForcedSubtitlesOnly == true && !defaults.subtitleIndex) {
                            if (stream.IsForced == true) {
                                defaults.subtitleIndex = i;
                            }
                        }
                    } else if (stream.Type == "Audio") {
                        //get default language stream
                        if (userConfig.AudioLanguagePreference && !defaults.languageIndex) {
                            if (stream.Language == userConfig.AudioLanguagePreference) {
                                defaults.languageIndex = i;
                            }
                        }
                    }
                });
            }

            var baseParams = {
                audioChannels: 2,
                audioBitrate: 128000,
                videoBitrate: 1500000,
                maxWidth: screenWidth,
                maxHeight: screenHeight,
                StartTimeTicks: 0,
                SubtitleStreamIndex: null,
                AudioStreamIndex: null
            };

            if (typeof (startPosition) != "undefined") {
                baseParams['StartTimeTicks'] = startPosition;
            }

            var html = '<video id="videoWindow" class="itemVideo video-js tubecss"></video>';

            var nowPlayingBar = $('#nowPlayingBar');
            //hide stop button
            $('#stopButton', nowPlayingBar).hide();

            $('#mediaElement', nowPlayingBar).addClass("video").html(html).show();

            _V_("videoWindow", { 'controls': true, 'autoplay': true, 'preload': 'auto' }, function () {

                var mp4VideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.mp4', $.extend({}, baseParams, {
                    videoCodec: 'h264',
                    audioCodec: 'aac',
                    profile: 'high',
	                videoBitrate: 2500000
                }));

                var tsVideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.ts', $.extend({}, baseParams, {
                    videoCodec: 'h264',
                    audioCodec: 'aac',
                    profile: 'high',
	                videoBitrate: 2500000
                }));

                var webmVideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.webm', $.extend({}, baseParams, {
                    videoCodec: 'vpx',
                    audioCodec: 'Vorbis',
	                videoBitrate: 2500000
                }));

                var hlsVideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.m3u8', $.extend({}, baseParams, {
                    videoCodec: 'h264',
                    audioCodec: 'aac'
                }));

                var ogvVideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.ogv', $.extend({}, baseParams, {
                    videoCodec: 'theora',
                    audioCodec: 'Vorbis'
                }));

                // HLS must be at the top for safari
                // Webm must be ahead of mp4 due to the issue of mp4 playing too fast in chrome

                (this).src([
                    { type: "application/x-mpegURL", src: hlsVideoUrl },
                    { type: "video/webm", src: webmVideoUrl },
                    { type: "video/mp4", src: mp4VideoUrl },
                    { type: "video/mp2t; codecs='h264, aac'", src: tsVideoUrl },
                    { type: "video/ogg", src: ogvVideoUrl }]
                ).volume(volume);

                videoJSextension.setup_video($('#videoWindow'), item, defaults);

                (this).addEvent("loadstart", function () {
                    $(".vjs-remaining-time-display").hide();
                });

                (this).addEvent("durationchange", function () {
                    if ((this).duration() != "Infinity")
                        $(".vjs-remaining-time-display").show();
                });

                (this).addEvent("volumechange", function () {
                    localStorage.setItem("volume", this.volume());
                });

                (this).addEvent("play", updateProgress);

                (this).addEvent("ended", function () {
	                MediaPlayer.stop();
                });

            });

            return $('video', nowPlayingBar)[0];
        }

        function updateProgress() {
            var player = _V_("videoWindow");
            var itemString = player.tag.src.match(new RegExp("Videos/[0-9a-z\-]+", "g"));
            var itemId = itemString[0].replace("Videos/", "");

            ApiClient.reportPlaybackStart(Dashboard.getCurrentUserId(), itemId);

            currentProgressInterval = setInterval(function () {
                var player = _V_("videoWindow");

                var startTimeTicks = player.tag.src.match(new RegExp("StartTimeTicks=[0-9]+", "g"));
                var startTime = startTimeTicks[0].replace("StartTimeTicks=", "");

                var itemString = player.tag.src.match(new RegExp("Videos/[0-9a-z\-]+", "g"));
                var itemId = itemString[0].replace("Videos/", "");

                var positionTicks = parseInt(startTime) + Math.floor(10000000 * player.currentTime());

                ApiClient.reportPlaybackProgress(Dashboard.getCurrentUserId(), itemId, positionTicks);
            }, 30000);
        }

        self.canPlay = function (item) {
            var media;

            if (item.MediaType === "Video") {
                media = testableVideoElement;
                if (media.canPlayType) {

                    return media.canPlayType('video/mp4').replace(/no/, '') || media.canPlayType('video/mp2t').replace(/no/, '') || media.canPlayType('video/webm').replace(/no/, '') || media.canPlayType('application/x-mpegURL').replace(/no/, '') || media.canPlayType('video/ogv').replace(/no/, '');
                }

                return false;
            }

            if (item.MediaType === "Audio") {
                media = testableAudioElement;
                if (media.canPlayType) {

                    return media.canPlayType('audio/mpeg').replace(/no/, '') || media.canPlayType('audio/webm').replace(/no/, '') || media.canPlayType('audio/aac').replace(/no/, '') || media.canPlayType('audio/ogg').replace(/no/, '');
                }

                return false;
            }

            return false;
        };

        self.play = function (items, startPosition) {

            if (self.isPlaying()) {
                self.stop();
            }

            var item = items[0];

            var mediaElement;

            if (item.MediaType === "Video") {

                mediaElement = playVideo(item, startPosition);
            } else if (item.MediaType === "Audio") {

                mediaElement = playAudio(item);
            }

            if (!mediaElement) {
                return;
            }

            currentMediaElement = mediaElement;

            var nowPlayingBar = $('#nowPlayingBar').show();

            if (items.length > 1) {
                $('#previousTrackButton', nowPlayingBar)[0].disabled = false;
                $('#nextTrackButton', nowPlayingBar)[0].disabled = false;
            } else {
                $('#previousTrackButton', nowPlayingBar)[0].disabled = true;
                $('#nextTrackButton', nowPlayingBar)[0].disabled = true;
            }

            //display image and title
            var imageTags = item.ImageTags || {};
            var html = '';

            var url = "";

            if (item.BackdropImageTags && item.BackdropImageTags.length) {

                url = ApiClient.getImageUrl(item.Id, {
                    type: "Backdrop",
                    height: 36,
                    tag: item.BackdropImageTags[0]
                });
            } else if (imageTags.Thumb) {

                url = ApiClient.getImageUrl(item.Id, {
                    type: "Thumb",
                    height: 36,
                    tag: item.ImageTags.Thumb
                });
            } else if (imageTags.Primary) {

                url = ApiClient.getImageUrl(item.Id, {
                    type: "Primary",
                    height: 36,
                    tag: item.ImageTags.Primary
                });
            } else {
                url = "css/images/items/detail/video.png";
            }

            var name = item.Name;
            var seriesName = '';

            if (item.IndexNumber != null) {
                name = item.IndexNumber + " - " + name;
            }
            if (item.ParentIndexNumber != null) {
                name = item.ParentIndexNumber + "." + name;
            }
            if (item.SeriesName || item.Album || item.ProductionYear) {
                seriesName = item.SeriesName || item.Album || item.ProductionYear;
            }

            html += "<div><a href='itemdetails.html?id=" + item.Id + "'><img class='nowPlayingBarImage ' alt='' title='' src='" + url + "' style='height:36px;display:inline-block;' /></a></div>";
            if (item.Type == "Movie")
                html += '<div>' + name + '<br/>' + seriesName + '</div>';
            else
                html += '<div>' + seriesName + '<br/>' + name + '</div>';

            $('#mediaInfo', nowPlayingBar).html(html);
        };

        self.playById = function (id, startPositionTicks) {

            ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(function (item) {

                self.play([item], startPositionTicks);

            });

        };



	    self.nowPlaying = function (item) {
		    self.playing = item;
	    };

	    self.canQueue = function (mediaType) {
            return mediaType == "Audio";
        };

	    self.queueAdd = function (item) {
		    self.queue.push(item);
	    };

	    self.queueRemove = function (elem) {
		    var index = $(elem).attr("data-queue-index");

		    self.queue.splice(index, 1);

		    $(elem).parent().parent().remove();
		    return false;
	    };

	    self.queuePlay = function (elem) {
		    var index = $(elem).attr("data-queue-index");

		    MediaPlayer.play(new Array(self.queue[index]));
		    self.queue.splice(index, 1);
	    };

	    self.queuePlayNext = function (item) {
		    if (typeof self.queue[0] != "undefined") {
			    MediaPlayer.play(new Array(self.queue[0]));
			    self.queue.shift();
		    }
	    };

	    self.queueAddNext = function (item) {
		    if (typeof self.queue[0] != "undefined") {
			    self.queue.unshift(item);
		    }else {
			    self.queueAdd(item);
		    }
	    };

	    self.inQueue = function (item) {
		    $.each(MediaPlayer.queue, function(i, queueItem){
			    if (item.Id == queueItem.Id) {
				    return true;
			    }
		    });
		    return false;
	    };

        self.playLast = function (itemId) {
	        ApiClient.getItem(Dashboard.getCurrentUserId(), itemId).done(function (item) {
				self.queueAdd(item);
	        });
        };

        self.playNext = function (itemId) {
	        ApiClient.getItem(Dashboard.getCurrentUserId(), itemId).done(function (item) {
		        self.queueAddNext(item);
	        });
        };

        self.stop = function () {

            var elem = currentMediaElement;

            //check if it's a video using VideoJS
            if ($(elem).hasClass("vjs-tech")) {
                var player = _V_("videoWindow");

                self.stopVideo();

                if (player.techName == "html5") {
                    player.tag.src = "";
                    player.tech.removeTriggers();
                    player.load();
                }

                //remove custom buttons
                delete _V_.ControlBar.prototype.options.components.ResolutionSelectorButton;
                delete _V_.ControlBar.prototype.options.components.SubtitleSelectorButton;
                delete _V_.ControlBar.prototype.options.components.LanguageSelectorButton;
                delete _V_.ControlBar.prototype.options.components.ChapterSelectorButton;

                //player.tech.destroy();
                player.destroy();
            } else {
                self.stopAudio();

                elem.pause();
                elem.src = "";
            }

            $(elem).remove();

            $('#nowPlayingBar').hide();

            currentMediaElement = null;
        };

        self.stopVideo = function () {
            var player = _V_("videoWindow");

            var startTimeTicks = player.tag.src.match(new RegExp("StartTimeTicks=[0-9]+", "g"));
            var startTime = startTimeTicks[0].replace("StartTimeTicks=", "");

            var itemString = player.tag.src.match(new RegExp("Videos/[0-9a-z\-]+", "g"));
            var itemId = itemString[0].replace("Videos/", "");

            var positionTicks = parseInt(startTime) + Math.floor(10000000 * player.currentTime());

            ApiClient.reportPlaybackStopped(Dashboard.getCurrentUserId(), itemId, positionTicks);

            if (currentProgressInterval) {
                clearTimeout(currentProgressInterval);
            }
        };

        self.stopAudio = function () {
            var itemString = $(".itemAudio source").attr('src').match(new RegExp("Audio/[0-9a-z\-]+", "g"));
            var itemId = itemString[0].replace("Audio/", "");

            var position;
            $(".itemAudio").each(function () {
                position = Math.floor(10000000 * this.currentTime);
            });

            ApiClient.reportPlaybackStopped(Dashboard.getCurrentUserId(), itemId, position);

            if (currentProgressInterval) {
                clearTimeout(currentProgressInterval);
            }
        };

        self.isPlaying = function () {
            return currentMediaElement;
        };
    }

    window.MediaPlayer = new mediaPlayer();

})(document, clearTimeout, screen, localStorage, _V_, $, setInterval, window);