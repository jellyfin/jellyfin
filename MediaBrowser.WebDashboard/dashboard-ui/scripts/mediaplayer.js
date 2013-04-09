var MediaPlayer = (function (document, clearTimeout, screen, localStorage, _V_, $, setInterval) {

    var testableAudioElement = document.createElement('audio');
    var testableVideoElement = document.createElement('video');
    var currentMediaElement;
    var currentProgressInterval;

    function playAudio(items, params) {
        var item = items[0];

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

        return $('audio', nowPlayingBar)[0];
    }

    function playVideo(items, startPosition) {
        //stop/kill videoJS
        if (currentMediaElement) self.stop();

        var item = items[0];

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

        var html = '<video id="videoWindow" class="itemVideo video-js vjs-default-skin"></video>';

        var nowPlayingBar = $('#nowPlayingBar');
        //hide stop button
        $('#stopButton', nowPlayingBar).hide();

        $('#mediaElement', nowPlayingBar).addClass("video").html(html).show();

        _V_("videoWindow", { 'controls': true, 'autoplay': true, 'preload': 'auto' }, function () {

            var mp4VideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.mp4', $.extend({}, baseParams, {
                videoCodec: 'h264',
                audioCodec: 'aac'
            }));

            var tsVideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.ts', $.extend({}, baseParams, {
                videoCodec: 'h264',
                audioCodec: 'aac'
            }));

            var webmVideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.webm', $.extend({}, baseParams, {
                videoCodec: 'vpx',
                audioCodec: 'Vorbis'
            }));

            var hlsVideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.m3u8', $.extend({}, baseParams, {
                videoCodec: 'h264',
                audioCodec: 'aac'
            }));

            var ogvVideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.ogv', $.extend({}, baseParams, {
                videoCodec: 'theora',
                audioCodec: 'Vorbis'
            }));

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
                localStorage.setItem("volume", (this).volume());
            });

            (this).addEvent("play", updateProgress);

            ApiClient.reportPlaybackStart(Dashboard.getCurrentUserId(), item.Id);
        });

        return $('video', nowPlayingBar)[0];
    }

    function updateProgress() {
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

        if (item.MediaType === "Video") {

            var media = testableVideoElement;

            if (media.canPlayType) {

                return media.canPlayType('video/mp4').replace(/no/, '') || media.canPlayType('video/mp2t').replace(/no/, '') || media.canPlayType('video/webm').replace(/no/, '') || media.canPlayType('application/x-mpegURL').replace(/no/, '') || media.canPlayType('video/ogv').replace(/no/, '');
            }

            return false;
        }

        if (item.MediaType === "Audio") {

            var media = testableAudioElement;

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

            mediaElement = playVideo(items, startPosition);
        } else if (item.MediaType === "Audio") {

            mediaElement = playAudio(items);
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

        html += "<div><img class='nowPlayingBarImage ' alt='' title='' src='" + url + "' style='height:36px;display:inline-block;' /></div>";
        if (item.Type == "Movie")
            html += '<div>' + name + '<br/>' + seriesName + '</div>';
        else
            html += '<div>' + seriesName + '<br/>' + name + '</div>';

        $('#mediaInfo', nowPlayingBar).html(html);
    };

    self.stop = function () {

        var elem = currentMediaElement;

        //check if it's a video using VideoJS
        if ($(elem).hasClass("vjs-tech")) {
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
            elem.pause();
            elem.src = "";
        }

        $(elem).remove();

        $('#nowPlayingBar').hide();

        currentMediaElement = null;
    };

    self.isPlaying = function () {
        return currentMediaElement;
    };

    return self;

})(document, clearTimeout, screen, localStorage, _V_, $, setInterval);