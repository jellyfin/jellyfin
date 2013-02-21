var MediaPlayer = {

    canPlay: function (item) {

        if (item.MediaType === "Video") {

            var media = document.createElement('video');

            if (media.canPlayType) {

                return media.canPlayType('video/mp2t').replace(/no/, '') || media.canPlayType('video/webm').replace(/no/, '') || media.canPlayType('video/ogv').replace(/no/, '');
            }

            return false;
        }

        if (item.MediaType === "Audio") {

            var media = document.createElement('audio');

            if (media.canPlayType) {
                return media.canPlayType('audio/mpeg').replace(/no/, '') || media.canPlayType('audio/aac').replace(/no/, '');
            }

            return false;
        }

        return false;
    },

    play: function (items) {

        if (MediaPlayer.isPlaying()) {
            MediaPlayer.stop();
        }

        var item = items[0];

        var mediaElement;
        
        if (item.MediaType === "Video") {

            mediaElement = MediaPlayer.playVideo(items);
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
    },

    playAudio: function (items) {
        var item = items[0];

        var baseParams = {
            id: item.Id,
            audioChannels: 2,
            audioBitrate: 128000
        };

        var mp3Url = ApiClient.getUrl('audio.mp3', $.extend({}, baseParams, {
            audioCodec: 'mp3'
        }));

        var aacUrl = ApiClient.getUrl('audio.aac', $.extend({}, baseParams, {
            audioCodec: 'aac'
        }));

        var webmUrl = ApiClient.getUrl('audio.webma', $.extend({}, baseParams, {
            audioCodec: 'Vorbis'
        }));

        var oggUrl = ApiClient.getUrl('audio.oga', $.extend({}, baseParams, {
            audioCodec: 'Vorbis'
        }));

        var html = '';
        html += '<audio class="itemAudio" preload="none" controls autoplay>';
        html += '<source type="audio/mpeg" src="' + mp3Url + '">';
        html += '<source type="audio/aac" src="' + aacUrl + '">';
        html += '<source type="audio/webm" src="' + webmUrl + '">';
        html += '<source type="audio/ogg" src="' + oggUrl + '">';
        html += '</audio';

        var nowPlayingBar = $('#nowPlayingBar').show();

        $('#mediaElement', nowPlayingBar).html(html);

        return $('audio', nowPlayingBar)[0];
    },

    playVideo: function (items) {

        var item = items[0];

        var screenWidth = Math.max(screen.height, screen.width);
        var screenHeight = Math.min(screen.height, screen.width);

        var baseParams = {
            id: item.Id,
            audioChannels: 2,
            audioBitrate: 128000,
            videoBitrate: 500000,
            maxWidth: screenWidth,
            maxHeight: screenHeight
        };

        var tsVideoUrl = ApiClient.getUrl('video.ts', $.extend({}, baseParams, {
            videoCodec: 'h264',
            audioCodec: 'aac'
        }));

        var webmVideoUrl = ApiClient.getUrl('video.webm', $.extend({}, baseParams, {
            videoCodec: 'vpx',
            audioCodec: 'Vorbis'
        }));

        var ogvVideoUrl = ApiClient.getUrl('video.ogv', $.extend({}, baseParams, {
            videoCodec: 'theora',
            audioCodec: 'Vorbis'
        }));

        var html = '';
        html += '<video class="itemVideo" preload="none" controls autoplay>';
        html += '<source type=\'video/mp2t; codecs="h264, aac"\' src="' + tsVideoUrl + '">';
        html += '<source type=\'video/webm; codecs="vp8, vorbis"\' src="' + webmVideoUrl + '">';
        html += '<source type=\'video/ogg; codecs="theora, vorbis"\' src="' + ogvVideoUrl + '">';
        html += '</video';

        var nowPlayingBar = $('#nowPlayingBar').show();

        $('#mediaElement', nowPlayingBar).html(html);

        return $('video', nowPlayingBar)[0];
    },

    stop: function () {

        var elem = MediaPlayer.mediaElement;

        elem.pause();
        elem.src = "";

        $(elem).remove();

        $('#nowPlayingBar').hide();

        MediaPlayer.mediaElement = null;
    },
    
    isPlaying: function() {
        return MediaPlayer.mediaElement;
    }
};