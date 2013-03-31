var MediaPlayer = {

    testableAudioElement: document.createElement('audio'),
    testableVideoElement: document.createElement('video'),

    canPlay: function (item) {

        if (item.MediaType === "Video") {

            var media = MediaPlayer.testableVideoElement;

            if (media.canPlayType) {

                return media.canPlayType('video/mp4').replace(/no/, '') || media.canPlayType('video/webm').replace(/no/, '') || media.canPlayType('video/ogv').replace(/no/, '');
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

        //need to store current play position (reset to 0 on new video load)
        MediaPlayer.playingTime = 0;

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

        var mp4VideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.mp4', $.extend({}, baseParams, {
            videoCodec: 'h264',
            audioCodec: 'aac'
        }));

        var webmVideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.webm', $.extend({}, baseParams, {
            videoCodec: 'vpx',
            audioCodec: 'Vorbis'
        }));

        var ogvVideoUrl = ApiClient.getUrl('Videos/' + item.Id + '/stream.ogv', $.extend({}, baseParams, {
            videoCodec: 'theora',
            audioCodec: 'Vorbis'
        }));

        $("#media_player").jPlayer({
            ready: function () {
                $(this).jPlayer("setMedia", {
                    m4v: mp4VideoUrl,
                    ogv: ogvVideoUrl,
                    webm: webmVideoUrl
                }).jPlayer("play");

                $('.jp_duration').html(ticks_to_human(item.RunTimeTicks));

                $(this).bind($.jPlayer.event.timeupdate,function(event){
                    MediaPlayer.playingTime = event.jPlayer.status.currentTime;
                });

                $(this).bind($.jPlayer.event.volumechange,function(event){
                    localStorage.setItem("volume", event.jPlayer.options.volume );
                });

                //add quality selector
                var available_res = ['high','medium','low'];
                $('.jp_quality').html('');
                $.each(available_res, function(i, value) {
                    var html = '<li><a href="javascript:;" onclick="MediaPlayer.setResolution(\''+value+'\');">'+value+'</a></li>';
                    $('.jp_quality').append(html);
                });

                $('.jp_chapters').html('');
                if (item.Chapters && item.Chapters.length) {
                    // Put together the available chapter list
                    $.each( item.Chapters, function( i, chapter ) {
                        var chapter_name = chapter.Name + " (" + ticks_to_human(chapter.StartPositionTicks) + ")";
                        var html = '<li><a href="javascript:;" onclick="MediaPlayer.setChapter(\''+i+'\',\''+chapter.StartPositionTicks+'\');">'+chapter_name+'</a></li>';
                        $('.jp_chapters').append(html);
                    });
                }

                MediaPlayer.updateProgress();
                ApiClient.reportPlaybackStart(Dashboard.getCurrentUserId(), item.Id);

            },
            volume: volume,
            supplied: "m4v, ogv, webm",
            cssSelectorAncestor: "#media_container",
            emulateHtml: true
        });

        var nowPlayingBar = $('#nowPlayingBar');

        $('#mediaElement', nowPlayingBar).show();

        return $('video', nowPlayingBar)[0];
    },

    stop: function () {

        var startTimeTicks = $("#media_player video").attr("src").match(new RegExp("StartTimeTicks=[0-9]+","g"));
        var start_time = startTimeTicks[0].replace("StartTimeTicks=","");

        var item_string = $("#media_player video").attr("src").match(new RegExp("Videos/[0-9a-z\-]+","g"));
        var item_id = item_string[0].replace("Videos/","");

        var current_time = MediaPlayer.playingTime;
        var positionTicks = parseInt(start_time) + Math.floor(10000000*current_time);

        ApiClient.reportPlaybackStopped(Dashboard.getCurrentUserId(), item_id, positionTicks);

        clearTimeout(MediaPlayer.progressInterval);

        $("#media_player").jPlayer("destroy");

        $('#nowPlayingBar').hide();

        MediaPlayer.mediaElement = null;
    },

    isPlaying: function () {
        return MediaPlayer.mediaElement;
    },

    updateProgress: function () {
        MediaPlayer.progressInterval = setInterval(function(){
            var current_time = MediaPlayer.playingTime;

            var startTimeTicks = $("#media_player video").attr("src").match(new RegExp("StartTimeTicks=[0-9]+","g"));
            var start_time = startTimeTicks[0].replace("StartTimeTicks=","");

            var item_string = $("#media_player video").attr("src").match(new RegExp("Videos/[0-9a-z\-]+","g"));
            var item_id = item_string[0].replace("Videos/","");

            var positionTicks = parseInt(start_time) + Math.floor(10000000*current_time);

            ApiClient.reportPlaybackProgress(Dashboard.getCurrentUserId(), item_id, positionTicks);
        },30000);
    },

    setResolution: function (new_res) {
        var resolutions = new Array();
        resolutions['high'] = new Array(1500000, 128000, 1920, 1080);
        resolutions['medium'] = new Array(750000, 128000, 1280, 720);
        resolutions['low'] = new Array(200000, 128000, 720, 480);

        var current_time = MediaPlayer.playingTime;

        // Set the button text to the newly chosen quality


        // Change the source and make sure we don't start the video over
        var currentSrc = $("#media_player video").attr("src");
        var src = parse_src_url(currentSrc);
        var newSrc = "/mediabrowser/"+src.Type+"/"+src.item_id+"/stream."+src.stream+"?audioChannels="+src.audioChannels+"&audioBitrate="+resolutions[new_res][1]+
            "&videoBitrate="+resolutions[new_res][0]+"&maxWidth="+resolutions[new_res][2]+"&maxHeight="+resolutions[new_res][3]+
            "&videoCodec="+src.videoCodec+"&audioCodec="+src.audioCodec;

        if (currentSrc.indexOf("StartTimeTicks") >= 0) {
            var startTimeTicks = currentSrc.match(new RegExp("StartTimeTicks=[0-9]+","g"));
            var start_time = startTimeTicks[0].replace("StartTimeTicks=","");

            newSrc += "&StartTimeTicks="+Math.floor(parseInt(start_time)+(10000000*current_time));
        }else {
            newSrc += "&StartTimeTicks="+Math.floor(10000000*current_time);
        }

        //need to store current play position (reset to 0 on new video load)
        MediaPlayer.playingTime = 0;

            $("#media_player").jPlayer("setMedia",{
            m4v: newSrc,
            ogv: newSrc,
            webm: newSrc
        }).jPlayer("play");

    },

    setChapter: function (chapter_id, new_time) {

        var currentSrc = $("#media_player video").attr("src");

        if (currentSrc.indexOf("StartTimeTicks") >= 0) {
            var newSrc = currentSrc.replace(new RegExp("StartTimeTicks=[0-9]+","g"),"StartTimeTicks="+new_time);
        }else {
            var newSrc = currentSrc += "&StartTimeTicks="+new_time;
        }

        $("#media_player").jPlayer("setMedia",{
            m4v: newSrc,
            ogv: newSrc,
            webm: newSrc
        }).jPlayer("play");
    }

};
