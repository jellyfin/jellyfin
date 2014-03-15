(function (document, setTimeout, clearTimeout, screen, localStorage, $, setInterval, window) {

    function mediaPlayer() {

        var self = this;

        var testableVideoElement = document.createElement('video');
        var currentMediaElement;
        var currentProgressInterval;
        var positionSlider;
        var currentTimeElement;
        var currentItem;
        var muteButton;
        var unmuteButton;
        var curentDurationTicks;
        var canClientSeek;
        var currentPlaylistIndex = 0;

        self.isPositionSliderActive;
        self.volumeSlider;
        self.startTimeTicksOffset;
        self.playlist = [];

        self.updateCanClientSeek = function (elem) {
            var duration = elem.duration;
            canClientSeek = duration && !isNaN(duration) && duration != Number.POSITIVE_INFINITY && duration != Number.NEGATIVE_INFINITY;
        }

        $(window).on("beforeunload popstate", function () {

            var item = currentItem;
            var media = currentMediaElement;

            // Try to report playback stopped before the browser closes
            if (item && media && currentProgressInterval) {

                var endTime = currentMediaElement.currentTime;

                var position = Math.floor(10000000 * endTime) + self.startTimeTicksOffset;

                ApiClient.reportPlaybackStopped(Dashboard.getCurrentUserId(), currentItem.Id, position);
            }
        });

        function replaceQueryString(url, param, value) {
            var re = new RegExp("([?|&])" + param + "=.*?(&|$)", "i");
            if (url.match(re))
                return url.replace(re, '$1' + param + "=" + value + '$2');
            else
                return url + '&' + param + "=" + value;
        }

        self.updateVolumeButtons = function (vol) {

            if (vol) {
                muteButton.show();
                unmuteButton.hide();
            } else {
                muteButton.hide();
                unmuteButton.show();
            }
        }

        self.getCurrentTicks = function (mediaElement) {
            return Math.floor(10000000 * (mediaElement || currentMediaElement).currentTime) + self.startTimeTicksOffset;
        }

        self.onPlaybackStopped = function () {

            $(this).off('ended.playbackstopped');

            currentTimeElement.empty();

            var endTime = this.currentTime;

            clearProgressInterval();

            var position = Math.floor(10000000 * endTime) + self.startTimeTicksOffset;

            ApiClient.reportPlaybackStopped(Dashboard.getCurrentUserId(), currentItem.Id, position);

            if (currentItem.MediaType == "Video") {
                ApiClient.stopActiveEncodings();
                if (self.isFullScreen()) {
                    self.exitFullScreen();
                }
                self.resetEnhancements();
            }
        }

        self.playNextAfterEnded = function () {

            $(this).off('ended.playnext');

            self.nextTrack();
        }

        self.startProgressInterval = function (itemId) {

            clearProgressInterval();

            var intervalTime = ApiClient.isWebSocketOpen() ? 2000 : 20000;

            currentProgressInterval = setInterval(function () {

                if (currentMediaElement) {
                    sendProgressUpdate(itemId);
                }

            }, intervalTime);
        }

        function sendProgressUpdate(itemId) {

            ApiClient.reportPlaybackProgress(Dashboard.getCurrentUserId(), itemId, self.getCurrentTicks(), currentMediaElement.paused, currentMediaElement.volume == 0);
        }

        function clearProgressInterval() {

            if (currentProgressInterval) {
                clearTimeout(currentProgressInterval);
                currentProgressInterval = null;
            }
        }

        function canPlayWebm() {

            return testableVideoElement.canPlayType('video/webm').replace(/no/, '');
        }

        self.getTranscodingExtension = function () {

            var media = testableVideoElement;

            // safari
            if (media.canPlayType('application/x-mpegURL').replace(/no/, '') ||
                media.canPlayType('application/vnd.apple.mpegURL').replace(/no/, '')) {
                return '.m3u8';
            }

            // Chrome or IE with plugin installed
            if (canPlayWebm() && !$.browser.mozilla) {
                return '.webm';
            }

            return '.mp4';
        }

        self.changeStream = function (ticks, params) {

            var element = currentMediaElement;

            if (canClientSeek && params == null) {

                element.currentTime = ticks / (1000 * 10000);

            } else {

                params = params || {};

                var currentSrc = element.currentSrc;
                currentSrc = replaceQueryString(currentSrc, 'starttimeticks', ticks);

                if (params.AudioStreamIndex != null) {
                    currentSrc = replaceQueryString(currentSrc, 'AudioStreamIndex', params.AudioStreamIndex);
                }
                if (params.SubtitleStreamIndex != null) {
                    currentSrc = replaceQueryString(currentSrc, 'SubtitleStreamIndex', (params.SubtitleStreamIndex == -1 ? '' : params.SubtitleStreamIndex));
                }

                var maxWidth = params.MaxWidth || getParameterByName('MaxWidth', currentSrc);
                var audioStreamIndex = params.AudioStreamIndex == null ? getParameterByName('AudioStreamIndex', currentSrc) : params.AudioStreamIndex;
                var subtitleStreamIndex = params.SubtitleStreamIndex == null ? getParameterByName('SubtitleStreamIndex', currentSrc) : params.SubtitleStreamIndex;
                var videoBitrate = parseInt(getParameterByName('VideoBitrate', currentSrc) || '0');
                var audioBitrate = parseInt(getParameterByName('AudioBitrate', currentSrc) || '0');
                var bitrate = params.Bitrate || (videoBitrate + audioBitrate);

                var transcodingExtension = self.getTranscodingExtension();

                var finalParams = self.getFinalVideoParams(currentItem, maxWidth, bitrate, audioStreamIndex, subtitleStreamIndex, transcodingExtension);
                currentSrc = replaceQueryString(currentSrc, 'MaxWidth', finalParams.maxWidth);
                currentSrc = replaceQueryString(currentSrc, 'VideoBitrate', finalParams.videoBitrate);
                currentSrc = replaceQueryString(currentSrc, 'AudioBitrate', finalParams.audioBitrate);
                currentSrc = replaceQueryString(currentSrc, 'Static', finalParams.isStatic);

                if (finalParams.isStatic) {
                    currentSrc = currentSrc.replace('.webm', '.mp4').replace('.m3u8', '.mp4');
                } else {
                    currentSrc = currentSrc.replace('.mp4', transcodingExtension);
                }

                clearProgressInterval();

                $(element).off('ended.playbackstopped').off('ended.playnext').on("play.onceafterseek", function () {

                    self.updateCanClientSeek(this);

                    $(this).off('play.onceafterseek').on('ended.playbackstopped', self.onPlaybackStopped).on('ended.playnext', self.playNextAfterEnded);

                    self.startProgressInterval(currentItem.Id);
                    sendProgressUpdate(currentItem.Id);

                });

                ApiClient.stopActiveEncodings().done(function () {

                    self.startTimeTicksOffset = ticks;
                    element.src = currentSrc;

                });
            }
        }

        function onPositionSliderChange() {

            self.isPositionSliderActive = false;

            var newPercent = parseInt(this.value);

            var newPositionTicks = (newPercent / 100) * currentItem.RunTimeTicks;

            self.changeStream(Math.floor(newPositionTicks));
        }

        $(function () {

            muteButton = $('#muteButton');
            unmuteButton = $('#unmuteButton');

            currentTimeElement = $('.currentTime');

            self.volumeSlider = $('.volumeSlider').on('slidestop', function () {

                console.log("slidestop");

                var vol = this.value;

                self.updateVolumeButtons(vol);
                currentMediaElement.volume = vol;
            });

            positionSlider = $(".positionSlider").on('slidestart', function () {

                self.isPositionSliderActive = true;

            }).on('slidestop', onPositionSliderChange);
        });

        function endsWith(text, pattern) {

            text = text.toLowerCase();
            pattern = pattern.toLowerCase();

            var d = text.length - pattern.length;
            return d >= 0 && text.lastIndexOf(pattern) === d;
        }

        self.setCurrentTime = function (ticks, item, updateSlider) {

            // Convert to ticks
            ticks = Math.floor(ticks);

            var timeText = Dashboard.getDisplayTime(ticks);

            if (curentDurationTicks) {

                timeText += " / " + Dashboard.getDisplayTime(curentDurationTicks);

                if (updateSlider) {
                    var percent = ticks / curentDurationTicks;
                    percent *= 100;

                    positionSlider.val(percent).slider('enable').slider('refresh');
                }
            } else {
                positionSlider.slider('disable').slider('refresh');
            }

            currentTimeElement.html(timeText);
        }

        function playAudio(item, startPositionTicks) {

            startPositionTicks = startPositionTicks || 0;

            var baseParams = {
                audioChannels: 2,
                audioBitrate: 128000,
                StartTimeTicks: startPositionTicks
            };

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

            var isStatic = false;
            var seekParam = isStatic && startPositionTicks ? '#t=' + (startPositionTicks / 10000000) : '';

            for (var i = 0, length = mediaStreams.length; i < length; i++) {

                var stream = mediaStreams[i];

                if (stream.Type == "Audio") {

                    // Stream statically when possible
                    if (endsWith(item.Path, ".aac") && stream.BitRate <= 256000) {
                        aacUrl += "&static=true" + seekParam;
                        isStatic = true;
                    }
                    else if (endsWith(item.Path, ".mp3") && stream.BitRate <= 256000) {
                        mp3Url += "&static=true" + seekParam;
                        isStatic = true;
                    }
                    break;
                }
            }

            self.startTimeTicksOffset = isStatic ? 0 : startPositionTicks;

            var html = '';

            var requiresControls = $.browser.android || ($.browser.webkit && !$.browser.chrome);

            // Can't autoplay in these browsers so we need to use the full controls
            if (requiresControls) {
                html += '<audio preload="auto" autoplay controls>';
            } else {
                html += '<audio preload="auto" style="display:none;" autoplay>';
            }
            html += '<source type="audio/mpeg" src="' + mp3Url + '" />';
            html += '<source type="audio/aac" src="' + aacUrl + '" />';
            html += '<source type="audio/webm" src="' + webmUrl + '" />';
            html += '</audio';

            var footer = $("#footer").show();
            var nowPlayingBar = $('#nowPlayingBar');
            //show stop button
            $('#stopButton', nowPlayingBar).show();
            $('#playButton', nowPlayingBar).hide();
            $('#pauseButton', nowPlayingBar).show();
            $('#fullscreenButton', nowPlayingBar).hide();

            $('#previousTrackButton', nowPlayingBar).show();
            $('#nextTrackButton', nowPlayingBar).show();
            $('#playlistButton', nowPlayingBar).show();

            $('#qualityButton', nowPlayingBar).hide();
            $('#audioTracksButton', nowPlayingBar).hide();
            $('#subtitleButton', nowPlayingBar).hide();
            $('#chaptersButton', nowPlayingBar).hide();

            var mediaElement = $('#mediaElement', footer).html(html);
            var audioElement = $("audio", mediaElement);

            var initialVolume = localStorage.getItem("volume") || 0.5;

            audioElement.each(function () {
                this.volume = initialVolume;
            });

            self.volumeSlider.val(initialVolume).slider('refresh');
            self.updateVolumeButtons(initialVolume);

            audioElement.on("volumechange", function () {

                var vol = this.volume;

                localStorage.setItem("volume", vol);

                self.updateVolumeButtons(vol);

            }).on("play.once", function () {

                if (!requiresControls) {
                    audioElement.hide();
                }

                self.updateCanClientSeek(this);

                audioElement.off("play.once");

                ApiClient.reportPlaybackStart(Dashboard.getCurrentUserId(), item.Id, true, item.MediaType);

                self.startProgressInterval(item.Id);

            }).on("pause", function () {

                $('#playButton', nowPlayingBar).show();
                $('#pauseButton', nowPlayingBar).hide();

            }).on("playing", function () {

                $('#playButton', nowPlayingBar).hide();
                $('#pauseButton', nowPlayingBar).show();

            }).on("timeupdate", function () {

                if (!self.isPositionSliderActive) {

                    self.setCurrentTime(self.getCurrentTicks(this), item, true);
                }

            }).on("ended.playbackstopped", self.onPlaybackStopped).on('ended.playnext', self.playNextAfterEnded);

            currentItem = item;
            curentDurationTicks = item.RunTimeTicks;

            return audioElement[0];
        }

        self.canPlayVideoDirect = function (item, videoStream, audioStream, subtitleStream, maxWidth, bitrate) {

            if (item.VideoType != "VideoFile" || item.LocationType != "FileSystem") {
                console.log('Transcoding because the content is not a video file');
                return false;
            }

            if ((videoStream.Codec || '').toLowerCase().indexOf('h264') == -1) {
                console.log('Transcoding because the content is not h264');
                return false;
            }

            if (audioStream && !canPlayAudioStreamDirect(audioStream)) {
                console.log('Transcoding because the audio cannot be played directly.');
                return false;
            }

            if (subtitleStream) {
                console.log('Transcoding because subtitles are required');
                return false;
            }

            if (!videoStream.Width || videoStream.Width > maxWidth) {
                console.log('Transcoding because resolution is too high');
                return false;
            }

            var videoBitrate = videoStream.BitRate || 0;
            var audioBitrate = audioStream ? audioStream.BitRate || 0 : null;

            if ((videoBitrate + audioBitrate) > bitrate) {
                console.log('Transcoding because bitrate is too high');
                return false;
            }

            var extension = item.Path.substring(item.Path.lastIndexOf('.') + 1).toLowerCase();

            if (extension == 'm4v') {
                return $.browser.chrome;
            }

            return extension.toLowerCase() == 'mp4';
        }

        function canPlayAudioStreamDirect(audioStream) {

            var audioCodec = (audioStream.Codec || '').toLowerCase().replace('-', '');

            if (audioCodec.indexOf('aac') == -1 &&
                audioCodec.indexOf('mp3') == -1 &&
                audioCodec.indexOf('mpeg') == -1) {

                return false;
            }

            if (audioStream.Channels == null) {
                return false;
            }

            // IE won't play at all if more than two channels
            if (audioStream.Channels > 2 && $.browser.msie) {
                return false;
            }

            return true;
        }

        self.getFinalVideoParams = function (item, maxWidth, bitrate, audioStreamIndex, subtitleStreamIndex, transcodingExtension) {

            var videoStream = (item.MediaStreams || []).filter(function (stream) {
                return stream.Type === "Video";
            })[0];

            var audioStream = (item.MediaStreams || []).filter(function (stream) {
                return stream.Index === audioStreamIndex;
            })[0];

            var subtitleStream = (item.MediaStreams || []).filter(function (stream) {
                return stream.Index === subtitleStreamIndex;
            })[0];

            var canPlayDirect = self.canPlayVideoDirect(item, videoStream, audioStream, subtitleStream, maxWidth, bitrate);

            var audioBitrate = bitrate >= 700000 ? 128000 : 64000;

            var videoBitrate = bitrate - audioBitrate;

            return {

                isStatic: canPlayDirect,
                maxWidth: maxWidth,
                audioCodec: transcodingExtension == '.webm' ? 'vorbis' : 'aac',
                videoCodec: transcodingExtension == '.webm' ? 'vpx' : 'h264',
                audioBitrate: audioBitrate,
                videoBitrate: videoBitrate
            };
        }

        self.canPlay = function (item, user) {

            if (item.PlayAccess != 'Full') {
                return false;
            }

            if (item.LocationType == "Virtual" || item.IsPlaceHolder) {
                return false;
            }
            if (item.Type == "MusicAlbum" || item.Type == "MusicArtist" || item.Type == "MusicGenre") {
                return true;
            }

            if (item.GameSystem == "Nintendo" && item.MediaType == "Game" && item.ProviderIds.NesBox && item.ProviderIds.NesBoxRom) {
                return true;
            }

            if (item.GameSystem == "Super Nintendo" && item.MediaType == "Game" && item.ProviderIds.NesBox && item.ProviderIds.NesBoxRom) {
                return true;
            }

            return self.canPlayMediaType(item.MediaType);
        };

        self.getPlayUrl = function (item) {


            if (item.GameSystem == "Nintendo" && item.MediaType == "Game" && item.ProviderIds.NesBox && item.ProviderIds.NesBoxRom) {

                return "http://nesbox.com/game/" + item.ProviderIds.NesBox + '/rom/' + item.ProviderIds.NesBoxRom;
            }

            if (item.GameSystem == "Super Nintendo" && item.MediaType == "Game" && item.ProviderIds.NesBox && item.ProviderIds.NesBoxRom) {

                return "http://snesbox.com/game/" + item.ProviderIds.NesBox + '/rom/' + item.ProviderIds.NesBoxRom;
            }

            return null;
        };

        self.canPlayMediaType = function (mediaType) {

            if (mediaType === "Video") {
                return true;
            }

            if (mediaType === "Audio") {
                return true;
            }

            return false;
        };

        self.play = function (items, startPosition) {

            Dashboard.getCurrentUser().done(function (user) {

                var item = items[0];

                var videoType = (item.VideoType || "").toLowerCase();

                var expirementalText = "This feature is experimental. It may not work at all with some titles. Do you wish to continue?";

                if (videoType == "dvd") {

                    self.playWithWarning(items, startPosition, user, "dvdstreamconfirmed", "Dvd Folder Streaming", expirementalText);
                    return;
                }
                else if (videoType == "bluray") {

                    self.playWithWarning(items, startPosition, user, "bluraystreamconfirmed", "Blu-ray Folder Streaming", expirementalText);
                    return;
                }
                else if (videoType == "iso") {

                    var isoType = (item.IsoType || "").toLowerCase();

                    if (isoType == "dvd") {

                        self.playWithWarning(items, startPosition, user, "dvdisostreamconfirmed", "Dvd Iso Streaming", expirementalText);
                        return;
                    }
                    else if (isoType == "bluray") {

                        self.playWithWarning(items, startPosition, user, "blurayisostreamconfirmed", "Blu-ray Iso Streaming", expirementalText);
                        return;
                    }
                }

                self.playInternal(items[0], startPosition, user);
                self.onPlaybackStarted(items);
            });
        };

        self.playWithWarning = function (items, startPosition, user, localStorageKeyName, header, text) {

            // Increment this version when changes are made and we want users to see the prompts again
            var warningVersion = "2";
            localStorageKeyName += new Date().getMonth() + warningVersion;

            if (localStorage.getItem(localStorageKeyName) == "1") {

                self.playInternal(items[0], startPosition, user);

                self.onPlaybackStarted(items);

                return;
            }

            Dashboard.confirm(text, header, function (result) {

                if (result) {

                    localStorage.setItem(localStorageKeyName, "1");

                    self.playInternal(items[0], startPosition, user);

                    self.onPlaybackStarted(items);
                }

            });

        };

        self.onPlaybackStarted = function (items) {

            self.playlist = items;
            currentPlaylistIndex = 0;
        };

        self.playInternal = function (item, startPosition, user) {

            if (item == null) {
                throw new Error("item cannot be null");
            }

            if (self.isPlaying()) {
                self.stop();
            }

            var mediaElement;

            if (item.MediaType === "Video") {

                videoPlayer(self, item, startPosition, user);
                mediaElement = self.initVideoPlayer();
                currentItem = item;
                curentDurationTicks = item.RunTimeTicks;

            } else if (item.MediaType === "Audio") {

                mediaElement = playAudio(item, startPosition);
            } else {
                throw new Error("Unrecognized media type");
            }

            currentMediaElement = mediaElement;

            //display image and title
            var imageTags = item.ImageTags || {};
            var html = '';

            var url = "";

            if (imageTags.Primary) {

                url = ApiClient.getImageUrl(item.Id, {
                    type: "Primary",
                    height: 80,
                    tag: item.ImageTags.Primary
                });
            }
            else if (item.BackdropImageTags && item.BackdropImageTags.length) {

                url = ApiClient.getImageUrl(item.Id, {
                    type: "Backdrop",
                    height: 80,
                    tag: item.BackdropImageTags[0]
                });
            } else if (imageTags.Thumb) {

                url = ApiClient.getImageUrl(item.Id, {
                    type: "Thumb",
                    height: 80,
                    tag: item.ImageTags.Thumb
                });

            }
            else if (item.Type == "Channel" || item.Type == "Recording") {
                url = "css/images/items/detail/tv.png";
            }
            else if (item.MediaType == "Audio") {
                url = "css/images/items/detail/audio.png";
            }
            else {
                url = "css/images/items/detail/video.png";
            }

            var name = item.Name;
            var seriesName = '';

            // Channel number
            if (item.Number) {
                name = item.Number + ' ' + name;
            }
            if (item.IndexNumber != null) {
                name = item.IndexNumber + " - " + name;
            }
            if (item.ParentIndexNumber != null) {
                name = item.ParentIndexNumber + "." + name;
            }
            if (item.SeriesName || item.Album || item.ProductionYear) {
                seriesName = item.SeriesName || item.Album || item.ProductionYear;
            }
            if (item.CurrentProgram) {
                seriesName = item.CurrentProgram.Name;
            }

            var href = LibraryBrowser.getHref(item.CurrentProgram || item);

            var nowPlayingText = (name ? name + "\n" : "") + (seriesName || "---");
            if (item.SeriesName || item.Album || item.CurrentProgram) {
                nowPlayingText = (seriesName ? seriesName : "") + "\n" + (name || "---");
            }

            console.log("name", name);
            console.log("seriesName", seriesName);
            console.log("nowPlayingText", nowPlayingText);

            // Fix for apostrophes and quotes
            var htmlTitle = trimTitle(nowPlayingText).replace(/'/g, '&apos;').replace(/"/g, '&quot;');
            html += "<div><a href='" + href + "'><img class='nowPlayingBarImage' alt='" + htmlTitle +
                "' title='" + htmlTitle + "' src='" + url + "' style='height:40px;display:inline-block;' /></a></div>";
            html += "<div class='nowPlayingText' title='" + htmlTitle + "'>" + titleHtml(nowPlayingText) + "</div>";

            var nowPlayingBar = $('#nowPlayingBar');
            $('.nowPlayingMediaInfo', nowPlayingBar).html(html);
        };

        function trunc(string, len) {
            if (string.length > 0 && string.length < len) return string;
            var trimmed = $.trim(string).substring(0, len).split(" ").slice(0, -1).join(" ");
            if (trimmed) {
                trimmed += "...";
            } else {
                trimmed = "---"
            }
            return trimmed;
        }

        function trimTitle(title) {
            return title.replace("\n---", "");
        }

        function titleHtml(title) {
            var titles = title.split("\n");
            return (trunc(titles[0], 30) + "<br />" + trunc(titles[1], 30)).replace("---", "&nbsp;");
        }

        var getItemFields = "MediaStreams,Chapters,Path";

        self.getItemsForPlayback = function (query) {

            var userId = Dashboard.getCurrentUserId();

            query.Limit = query.Limit || 100;
            query.Fields = getItemFields;

            return ApiClient.getItems(userId, query);
        };

        self.playById = function (id, startPositionTicks) {

            ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(function (item) {

                if (item.IsFolder) {

                    self.getItemsForPlayback({

                        ParentId: id,
                        Recursive: true,
                        SortBy: "SortName"

                    }).done(function (result) {

                        self.play(result.Items, startPositionTicks);

                    });

                } else {
                    self.play([item], startPositionTicks);
                }

            });

        };

        self.playInstantMixFromSong = function (id) {

            ApiClient.getInstantMixFromSong(id, {

                UserId: Dashboard.getCurrentUserId(),
                Fields: getItemFields,
                Limit: 50

            }).done(function (result) {

                self.play(result.Items);
            });

        };

        self.playInstantMixFromAlbum = function (id) {

            ApiClient.getInstantMixFromAlbum(id, {

                UserId: Dashboard.getCurrentUserId(),
                Fields: getItemFields,
                Limit: 50

            }).done(function (result) {

                self.play(result.Items);
            });

        };

        self.playInstantMixFromArtist = function (name) {

            ApiClient.getInstantMixFromArtist(name, {

                UserId: Dashboard.getCurrentUserId(),
                Fields: getItemFields,
                Limit: 50

            }).done(function (result) {

                self.play(result.Items);
            });

        };

        self.playInstantMixFromMusicGenre = function (name) {

            ApiClient.getInstantMixFromMusicGenre(name, {

                UserId: Dashboard.getCurrentUserId(),
                Fields: getItemFields,
                Limit: 50

            }).done(function (result) {

                self.play(result.Items);
            });

        };

        self.playArtist = function (artist) {

            self.getItemsForPlayback({

                Artists: artist,
                Recursive: true,
                SortBy: "Album,SortName",
                IncludeItemTypes: "Audio"

            }).done(function (result) {

                self.play(result.Items);

            });

        };

        self.shuffleArtist = function (artist) {

            self.getItemsForPlayback({

                Artists: artist,
                Recursive: true,
                SortBy: "Random",
                IncludeItemTypes: "Audio"

            }).done(function (result) {

                self.play(result.Items);

            });

        };

        self.shuffleMusicGenre = function (genre) {

            self.getItemsForPlayback({

                Genres: genre,
                Recursive: true,
                SortBy: "Random",
                IncludeItemTypes: "Audio"

            }).done(function (result) {

                self.play(result.Items);

            });

        };

        self.shuffleFolder = function (id) {

            self.getItemsForPlayback({

                ParentId: id,
                Recursive: true,
                SortBy: "Random"

            }).done(function (result) {

                self.play(result.Items);

            });

        };

        self.removeFromPlaylist = function (index) {

            self.playlist.remove(index);

        };

        // Gets or sets the current playlist index
        self.currentPlaylistIndex = function (i) {

            if (i == null) {
                return currentPlaylistIndex;
            }

            var newItem = self.playlist[i];

            Dashboard.getCurrentUser().done(function (user) {

                self.playInternal(newItem, 0, user);
                currentPlaylistIndex = i;
            });
        };

        self.nextTrack = function () {

            var newIndex = currentPlaylistIndex + 1;
            var newItem = self.playlist[newIndex];

            if (newItem) {
                Dashboard.getCurrentUser().done(function (user) {

                    self.playInternal(newItem, 0, user);
                    currentPlaylistIndex = newIndex;
                });
            }
        };

        self.previousTrack = function () {
            var newIndex = currentPlaylistIndex - 1;
            if (newIndex >= 0) {
                var newItem = self.playlist[newIndex];

                if (newItem) {
                    Dashboard.getCurrentUser().done(function (user) {

                        self.playInternal(newItem, 0, user);
                        currentPlaylistIndex = newIndex;
                    });
                }
            }
        };

        self.queueItemsNext = function (items) {

            var insertIndex = 1;

            for (var i = 0, length = items.length; i < length; i++) {

                self.playlist.splice(insertIndex, 0, items[i]);

                insertIndex++;
            }
        };

        self.queueItems = function (items) {

            for (var i = 0, length = items.length; i < length; i++) {

                self.playlist.push(items[i]);
            }
        };

        self.queue = function (id) {

            if (!currentMediaElement) {
                self.playById(id);
                return;
            }

            ApiClient.getItem(Dashboard.getCurrentUserId(), id).done(function (item) {

                if (item.IsFolder) {

                    self.getItemsForPlayback({

                        ParentId: id,
                        Recursive: true,
                        SortBy: "SortName"

                    }).done(function (result) {

                        self.queueItems(result.Items);

                    });

                } else {
                    self.queueItems([item]);
                }

            });
        };

        self.queueArtist = function (artist) {

            self.getItemsForPlayback({

                Artists: artist,
                Recursive: true,
                SortBy: "Album,SortName",
                IncludeItemTypes: "Audio"

            }).done(function (result) {

                self.queueItems(result.Items);

            });

        };

        self.pause = function () {
            currentMediaElement.pause();
        };

        self.unpause = function () {
            currentMediaElement.play();
        };

        self.seek = function (position) {

            self.changeStream(position);

        };

        self.mute = function () {

            if (currentMediaElement) {
                currentMediaElement.volume = 0;
                currentMediaElement.muted = true;
                self.volumeSlider.val(0).slider('refresh');
            }
        };

        self.unmute = function () {

            if (currentMediaElement) {
                var volume = localStorage.getItem("volume") || self.volumeSlider.val();
                currentMediaElement.volume = volume;
                currentMediaElement.muted = false;
                self.volumeSlider.val(volume).slider('refresh');
            }
        };

        self.toggleMute = function () {

            if (currentMediaElement) {
                var volume = localStorage.getItem("volume") || self.volumeSlider.val();
                currentMediaElement.volume = currentMediaElement.volume ? 0 : volume;
                currentMediaElement.muted = currentMediaElement.volume == 0;
                self.volumeSlider.val(volume).slider('refresh');
            }
        };

        self.volumeDown = function () {

            if (currentMediaElement) {
                currentMediaElement.volume = Math.max(currentMediaElement.volume - .02, 0);
                localStorage.setItem("volume", currentMediaElement.volume);
                self.volumeSlider.val(currentMediaElement.volume).slider('refresh');
            }
        };

        self.volumeUp = function () {

            if (currentMediaElement) {
                currentMediaElement.volume = Math.min(currentMediaElement.volume + .02, 1);
                localStorage.setItem("volume", currentMediaElement.volume);
                self.volumeSlider.val(currentMediaElement.volume).slider('refresh');
            }
        };

        self.stop = function () {

            var elem = currentMediaElement;

            elem.pause();

            $(elem).off('ended.playnext').on('ended', function () {

                $(this).remove();
                elem.src = "";
                currentMediaElement = null;

            }).trigger('ended');

            var footer = $('#footer');
            footer.hide();

            if (currentItem.MediaType == "Video") {
                if (self.isFullScreen()) {
                    self.exitFullScreen();
                }
                self.resetEnhancements();
            }
        };

        self.isPlaying = function () {
            return currentMediaElement;
        };

        self.showSendMediaMenu = function () {

            RemoteControl.showMenuForItem({
                item: currentItem
            });

        };
    }

    window.MediaPlayer = new mediaPlayer();

})(document, setTimeout, clearTimeout, screen, localStorage, $, setInterval, window);