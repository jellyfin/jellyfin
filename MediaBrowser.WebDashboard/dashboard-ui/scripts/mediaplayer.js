var MediaPlayer = {

    testableAudioElement: document.createElement('audio'),
    testableVideoElement: document.createElement('video'),

    canPlay: function (item) {

        if (item.MediaType === "Video") {

            var media = MediaPlayer.testableVideoElement;

            if (media.canPlayType) {

                return media.canPlayType('video/mp4').replace(/no/, '') || media.canPlayType('video/mp2t').replace(/no/, '') || media.canPlayType('video/webm').replace(/no/, '') || media.canPlayType('application/x-mpegURL').replace(/no/, '') || media.canPlayType('video/ogv').replace(/no/, '');
            }

            return false;
        }

        if (item.MediaType === "Audio") {

            var media = MediaPlayer.testableAudioElement;

            if (media.canPlayType) {
                return media.canPlayType('audio/mpeg').replace(/no/, '') || media.canPlayType('audio/aac').replace(/no/, '');
            }

            return false;
        }

        return false;
    },

    play: function (items, startPosition) {

        if (MediaPlayer.isPlaying()) {
            MediaPlayer.stop();
        }

        var item = items[0];

        var mediaElement;

        if (item.MediaType === "Video") {

            mediaElement = MediaPlayer.playVideo(items, startPosition);
        }

        else if (item.MediaType === "Audio") {

            mediaElement = MediaPlayer.playAudio(items);
        }

        if (!mediaElement) {
            return;
        }

        MediaPlayer.mediaElement = mediaElement;

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
        }
        else if (imageTags.Thumb) {

            url = ApiClient.getImageUrl(item.Id, {
                type: "Thumb",
                height: 36,
                tag: item.ImageTags.Thumb
            });
        }
        else if (imageTags.Primary) {

            url = ApiClient.getImageUrl(item.Id, {
                type: "Primary",
                height: 36,
                tag: item.ImageTags.Primary
            });
        }else {
            url = "css/images/items/detail/video.png";
        }

        var name = item.Name;
        var series_name = '';

        if (item.IndexNumber != null) {
            name = item.IndexNumber + " - " + name;
        }
        if (item.ParentIndexNumber != null) {
            name = item.ParentIndexNumber + "." + name;
        }
        if (item.SeriesName || item.Album || item.ProductionYear) {
            series_name = item.SeriesName || item.Album || item.ProductionYear;
        }

        html += "<div><img class='nowPlayingBarImage ' alt='' title='' src='" + url + "' style='height:36px;display:inline-block;' /></div>";
        html += '<div>'+name+'<br/>'+series_name+'</div>';

        $('#mediaInfo', nowPlayingBar).html(html);
    },

    playAudio: function (items, params) {
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

        var webmUrl = ApiClient.getUrl('Audio/' + item.Id + '/stream.webma', $.extend({}, baseParams, {
            audioCodec: 'Vorbis'
        }));

        var oggUrl = ApiClient.getUrl('Audio/' + item.Id + '/stream.oga', $.extend({}, baseParams, {
            audioCodec: 'Vorbis'
        }));

        var html = '';
        html += '<audio class="itemAudio" preload="none" controls autoplay>';
        html += '<source type="audio/mpeg" src="' + mp3Url + '" />';
        html += '<source type="audio/aac" src="' + aacUrl + '" />';
        html += '<source type="audio/webm" src="' + webmUrl + '" />';
        html += '<source type="audio/ogg" src="' + oggUrl + '" />';
        html += '</audio';

        var nowPlayingBar = $('#nowPlayingBar').show();

        $('#mediaElement', nowPlayingBar).html(html);

        return $('audio', nowPlayingBar)[0];
    },

    playVideo: function (items, startPosition) {

        var item = items[0];

        // Account for screen rotation. Use the larger dimension as the width.
        var screenWidth = Math.max(screen.height, screen.width);
        var screenHeight = Math.min(screen.height, screen.width);

        var volume = localStorage.getItem("volume") || 0.5;

        var baseParams = {
            audioChannels: 2,
            audioBitrate: 128000,
            videoBitrate: 1500000,
            maxWidth: screenWidth,
            maxHeight: screenHeight,
            StartTimeTicks: 0
        };

        if (typeof(startPosition) != "undefined") {
            baseParams['StartTimeTicks'] = startPosition;
        }

        var html = '<video id="videoWindow" class="itemVideo video-js vjs-default-skin"></video>';

        var nowPlayingBar = $('#nowPlayingBar');

        $('#mediaElement', nowPlayingBar).html(html).show();

        _V_("videoWindow", {'controls': true, 'autoplay': true, 'preload': 'auto'}, function(){

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

            (this).src([{ type: "video/webm", src: webmVideoUrl },
                { type: "video/mp4", src: mp4VideoUrl },
                { type: "video/mp2t; codecs='h264, aac'", src: tsVideoUrl },
                { type: "application/x-mpegURL", src: hlsVideoUrl },
                { type: "video/ogg", src: ogvVideoUrl }]
            ).volume(volume);

            videoJSextension.setup_video( $( '#videoWindow' ), item );

            (this).addEvent("loadstart",function(){
                $(".vjs-remaining-time-display").hide();
            });

            (this).addEvent("durationchange",function(){
                if ((this).duration() != "Infinity")
                    $(".vjs-remaining-time-display").show();
            });

            (this).addEvent("volumechange",function(){
                localStorage.setItem("volume", (this).volume());
            });

            (this).addEvent("play", MediaPlayer.updateProgress);

            ApiClient.reportPlaybackStart(Dashboard.getCurrentUserId(), item.Id);
        });

        return $('video', nowPlayingBar)[0];
    },

    stop: function () {

        var elem = MediaPlayer.mediaElement;

        //check if it's a video using VideoJS
        if ($(elem).hasClass("vjs-tech")) {
            var player = _V_("videoWindow");

            var startTimeTicks = player.tag.src.match(new RegExp("StartTimeTicks=[0-9]+","g"));
            var start_time = startTimeTicks[0].replace("StartTimeTicks=","");

            var item_string = player.tag.src.match(new RegExp("Videos/[0-9a-z\-]+","g"));
            var item_id = item_string[0].replace("Videos/","");

            var positionTicks = parseInt(start_time) + Math.floor(10000000*player.currentTime());

            ApiClient.reportPlaybackStopped(Dashboard.getCurrentUserId(), item_id, positionTicks);

            clearTimeout(progressInterval);

            if (player.techName == "html5") {
                player.tag.src = "";
                player.tech.removeTriggers();
                player.load();
            }
            //player.tech.destroy();
            player.destroy();
        }else {
            elem.pause();
            elem.src = "";
        }

        $(elem).remove();

        $('#nowPlayingBar').hide();

        MediaPlayer.mediaElement = null;
    },

    isPlaying: function () {
        return MediaPlayer.mediaElement;
    },

    updateProgress: function () {
        progressInterval = setInterval(function(){
            var player = _V_("videoWindow");

            var startTimeTicks = player.tag.src.match(new RegExp("StartTimeTicks=[0-9]+","g"));
            var start_time = startTimeTicks[0].replace("StartTimeTicks=","");

            var item_string = player.tag.src.match(new RegExp("Videos/[0-9a-z\-]+","g"));
            var item_id = item_string[0].replace("Videos/","");

            var positionTicks = parseInt(start_time) + Math.floor(10000000*player.currentTime());

            ApiClient.reportPlaybackProgress(Dashboard.getCurrentUserId(), item_id, positionTicks);
        },30000);
    }

};

var videoJSextension = {

    /*
     Add our video quality selector button to the videojs controls. This takes
     a mandatory jQuery object of the <video> element we are setting up the
     videojs video for.
     */
    setup_video : function( $video, item ) {

        var vid_id = $video.attr( 'id' ),
            available_res = ['high','medium','low'],
            default_res,
            vjs_sources = [], // This will be an array of arrays of objects, see the video.js api documentation for myPlayer.src()
            vjs_source = {},
            vjs_chapters = [], // This will be an array of arrays of objects, see the video.js api documentation for myPlayer.src()
            vjs_chapter = {};

        // Determine this video's default res (it might not have the globally determined default available)
        default_res = available_res[0];

        // Put together the videojs source arrays for each available resolution
        $.each( available_res, function( i, res ) {

            vjs_sources[i] = [];

            vjs_source = {};
            vjs_source.res = res;

            vjs_sources[i].push( vjs_source );

        });

        _V_.ResolutionSelectorButton = _V_.ResolutionSelector.extend({
            buttonText: default_res,
            availableRes: vjs_sources
        });

        // Add the resolution selector button.
        _V_.merge( _V_.ControlBar.prototype.options.components, { ResolutionSelectorButton : {} } );

        //chceck if chapters exist and add chapter selector
        if (item.Chapters && item.Chapters.length) {
            // Put together the videojs source arrays for each available chapter
            $.each( item.Chapters, function( i, chapter ) {

                vjs_chapters[i] = [];

                vjs_chapter = {};
                vjs_chapter.Name = chapter.Name + " (" + ticks_to_human(chapter.StartPositionTicks) + ")";
                vjs_chapter.StartPositionTicks = chapter.StartPositionTicks;

                vjs_chapters[i].push( vjs_chapter );

            });

            _V_.ChapterSelectorButton = _V_.ChapterSelector.extend({
                buttonText: '',
                Chapters: vjs_chapters
            });

            // Add the chapter selector button.
            _V_.merge( _V_.ControlBar.prototype.options.components, { ChapterSelectorButton : {} } );
        }

    }
};