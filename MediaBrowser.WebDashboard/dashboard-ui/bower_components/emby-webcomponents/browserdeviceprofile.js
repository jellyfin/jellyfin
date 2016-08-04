define(['browser'], function (browser) {

    function canPlayH264() {
        var v = document.createElement('video');
        return !!(v.canPlayType && v.canPlayType('video/mp4; codecs="avc1.42E01E, mp4a.40.2"').replace(/no/, ''));
    }

    var _supportsTextTracks;
    function supportsTextTracks() {

        if (_supportsTextTracks == null) {
            _supportsTextTracks = document.createElement('video').textTracks != null;
        }

        // For now, until ready
        return _supportsTextTracks;
    }

    var _canPlayHls;
    function canPlayHls(src) {

        if (_canPlayHls == null) {
            _canPlayHls = canPlayNativeHls() || canPlayHlsWithMSE();
        }
        return _canPlayHls;
    }

    function canPlayNativeHls() {
        var media = document.createElement('video');

        if (media.canPlayType('application/x-mpegURL').replace(/no/, '') ||
            media.canPlayType('application/vnd.apple.mpegURL').replace(/no/, '')) {
            return true;
        }

        return false;
    }

    function canPlayHlsWithMSE() {
        if (window.MediaSource != null && !browser.firefox) {
            // text tracks don’t work with this in firefox
            return true;
        }

        return false;
    }

    function canPlayAudioFormat(format) {

        var typeString;

        if (format == 'flac') {
            if (browser.tizen) {
                return true;
            }
            if (isEdgeUniversal()) {
                return true;
            }
        }

        else if (format == 'wma') {
            if (browser.tizen) {
                return true;
            }
            if (isEdgeUniversal()) {
                return true;
            }
        }

        else if (format == 'opus') {
            typeString = 'audio/ogg; codecs="opus"';

            if (document.createElement('audio').canPlayType(typeString).replace(/no/, '')) {
                return true;
            }

            return false;
        }

        if (format == 'webma') {
            typeString = 'audio/webm';
        } else {
            typeString = 'audio/' + format;
        }

        if (document.createElement('audio').canPlayType(typeString).replace(/no/, '')) {
            return true;
        }

        return false;
    }

    function isEdgeUniversal() {

        if (browser.edge) {

            var userAgent = navigator.userAgent.toLowerCase();
            if (userAgent.indexOf('msapphost') != -1) {
                return true;
            }
        }

        return false;
    }

    function testCanPlayMkv(videoTestElement) {

        if (videoTestElement.canPlayType('video/x-matroska') ||
            videoTestElement.canPlayType('video/mkv')) {
            return true;
        }

        var userAgent = navigator.userAgent.toLowerCase();

        // Unfortunately there's no real way to detect mkv support
        if (browser.chrome) {

            // Not supported on opera tv
            if (browser.operaTv) {
                return false;
            }

            // Filter out browsers based on chromium that don't support mkv
            if (userAgent.indexOf('vivaldi') != -1 || userAgent.indexOf('opera') != -1) {
                return false;
            }

            return true;
        }

        if (browser.tizen) {
            return true;
        }

        if (isEdgeUniversal()) {

            return true;
        }

        return false;
    }

    function testCanPlayTs() {

        return browser.tizen || browser.web0s;
    }

    function getDirectPlayProfileForVideoContainer(container, videoAudioCodecs) {

        var supported = false;
        var profileContainer = container;

        switch (container) {

            case 'asf':
                supported = browser.tizen || isEdgeUniversal();
                videoAudioCodecs = [];
                break;
            case 'avi':
                supported = isEdgeUniversal();
                break;
            case 'mpg':
            case 'mpeg':
                supported = isEdgeUniversal();
                break;
            case '3gp':
            case 'flv':
            case 'mts':
            case 'trp':
            case 'vob':
            case 'vro':
                supported = browser.tizen;
                break;
            case 'mov':
                supported = browser.chrome || isEdgeUniversal();
                break;
            case 'm2ts':
                supported = browser.tizen || browser.web0s || isEdgeUniversal();
                break;
            case 'wmv':
                supported = browser.tizen || browser.web0s || isEdgeUniversal();
                videoAudioCodecs = [];
                break;
            case 'ts':
                supported = browser.tizen || browser.web0s || isEdgeUniversal();
                profileContainer = 'ts,mpegts';
                break;
            default:
                break;
        }

        if (!supported) {
            return null;
        }

        return {
            Container: profileContainer,
            Type: 'Video',
            AudioCodec: videoAudioCodecs.join(',')
        };
    }

    function getMaxBitrate() {

        // 10mbps
        if (browser.xboxOne) {
            return 10000000;
        }

        if (browser.ps4) {
            return 8000000;
        }

        var userAgent = navigator.userAgent.toLowerCase();

        if (browser.tizen) {

            // 2015 models
            if (userAgent.indexOf('tizen 2.3') != -1) {
                return 20000000;
            }

            // 2016 models
            return 40000000;
        }

        return 100000000;
    }

    return function (options) {

        options = options || {};
        var physicalAudioChannels = options.audioChannels || 2;

        var bitrateSetting = getMaxBitrate();

        var videoTestElement = document.createElement('video');

        var canPlayWebm = videoTestElement.canPlayType('video/webm').replace(/no/, '');

        var canPlayMkv = testCanPlayMkv(videoTestElement);
        var canPlayTs = testCanPlayTs();

        var profile = {};

        profile.MaxStreamingBitrate = bitrateSetting;
        profile.MaxStaticBitrate = 100000000;
        profile.MusicStreamingTranscodingBitrate = Math.min(bitrateSetting, 192000);

        profile.DirectPlayProfiles = [];

        var videoAudioCodecs = [];
        var hlsVideoAudioCodecs = [];

        var supportsMp3VideoAudio = videoTestElement.canPlayType('video/mp4; codecs="avc1.640029, mp4a.69"').replace(/no/, '') ||
            videoTestElement.canPlayType('video/mp4; codecs="avc1.640029, mp4a.6B"').replace(/no/, '');

        // Only put mp3 first if mkv support is there
        // Otherwise with HLS and mp3 audio we're seeing some browsers
        if (videoTestElement.canPlayType('audio/mp4; codecs="ac-3"').replace(/no/, '') || isEdgeUniversal()) {
            // safari is lying
            if (!browser.safari) {
                videoAudioCodecs.push('ac3');

                // This works in edge desktop, but not mobile
                if (!browser.edge || !browser.mobile) {
                    hlsVideoAudioCodecs.push('ac3');
                }
            }
        }

        var mp3Added = false;
        if (canPlayMkv || canPlayTs) {
            if (supportsMp3VideoAudio) {
                mp3Added = true;
                videoAudioCodecs.push('mp3');
                hlsVideoAudioCodecs.push('mp3');
            }
        }
        if (videoTestElement.canPlayType('video/mp4; codecs="avc1.640029, mp4a.40.2"').replace(/no/, '')) {
            videoAudioCodecs.push('aac');
            hlsVideoAudioCodecs.push('aac');
        }
        if (!mp3Added && supportsMp3VideoAudio) {
            videoAudioCodecs.push('mp3');
            hlsVideoAudioCodecs.push('mp3');
        }

        if (isEdgeUniversal()) {
            videoAudioCodecs.push('dca');
            //videoAudioCodecs.push('truehd');
        }

        if (canPlayH264()) {
            profile.DirectPlayProfiles.push({
                Container: 'mp4,m4v',
                Type: 'Video',
                VideoCodec: 'h264',
                AudioCodec: videoAudioCodecs.join(',')
            });
        }

        if (canPlayMkv) {
            profile.DirectPlayProfiles.push({
                Container: 'mkv',
                Type: 'Video',
                VideoCodec: 'h264',
                AudioCodec: videoAudioCodecs.join(',')
            });

            if (isEdgeUniversal()) {
                profile.DirectPlayProfiles.push({
                    Container: 'mkv',
                    Type: 'Video',
                    VideoCodec: 'vc1',
                    AudioCodec: videoAudioCodecs.join(',')
                });
            }
        }

        // These are formats we can't test for but some devices will support
        ['m2ts', 'mov', 'wmv', 'ts', 'asf', 'avi', 'mpg', 'mpeg'].map(function (container) {
            return getDirectPlayProfileForVideoContainer(container, videoAudioCodecs);
        }).filter(function (i) {
            return i != null;
        }).forEach(function (i) {
            profile.DirectPlayProfiles.push(i);
        });

        ['opus', 'mp3', 'aac', 'flac', 'webma', 'wma', 'wav'].filter(canPlayAudioFormat).forEach(function (audioFormat) {

            profile.DirectPlayProfiles.push({
                Container: audioFormat == 'webma' ? 'webma,webm' : audioFormat,
                Type: 'Audio'
            });

            // aac also appears in the m4a container
            if (audioFormat == 'aac') {
                profile.DirectPlayProfiles.push({
                    Container: 'm4a',
                    AudioCodec: audioFormat,
                    Type: 'Audio'
                });
            }
        });

        if (canPlayWebm) {
            profile.DirectPlayProfiles.push({
                Container: 'webm',
                Type: 'Video'
            });
        }

        profile.TranscodingProfiles = [];

        ['opus', 'mp3', 'aac'].filter(canPlayAudioFormat).forEach(function (audioFormat) {

            profile.TranscodingProfiles.push({
                Container: audioFormat,
                Type: 'Audio',
                AudioCodec: audioFormat,
                Context: 'Streaming',
                Protocol: 'http'
            });
            profile.TranscodingProfiles.push({
                Container: audioFormat,
                Type: 'Audio',
                AudioCodec: audioFormat,
                Context: 'Static',
                Protocol: 'http'
            });
        });

        var copyTimestamps = false;
        if (browser.chrome) {
            copyTimestamps = true;
        }

        // Can't use mkv on mobile because we have to use the native player controls and they won't be able to seek it
        if (canPlayMkv && options.supportsCustomSeeking && !browser.tizen) {
            profile.TranscodingProfiles.push({
                Container: 'mkv',
                Type: 'Video',
                AudioCodec: videoAudioCodecs.join(','),
                VideoCodec: 'h264',
                Context: 'Streaming',
                CopyTimestamps: copyTimestamps
            });
        }

        if (canPlayTs && options.supportsCustomSeeking && !browser.tizen && !browser.web0s) {
            profile.TranscodingProfiles.push({
                Container: 'ts',
                Type: 'Video',
                AudioCodec: videoAudioCodecs.join(','),
                VideoCodec: 'h264',
                Context: 'Streaming',
                CopyTimestamps: copyTimestamps,
                // If audio transcoding is needed, limit channels to number of physical audio channels
                // Trying to transcode to 5 channels when there are only 2 speakers generally does not sound good
                MaxAudioChannels: physicalAudioChannels.toString()
            });
        }

        if (canPlayHls()) {
            profile.TranscodingProfiles.push({
                Container: 'ts',
                Type: 'Video',
                AudioCodec: hlsVideoAudioCodecs.join(','),
                VideoCodec: 'h264',
                Context: 'Streaming',
                Protocol: 'hls'
            });
        }

        // Put mp4 ahead of webm
        if (browser.firefox) {
            profile.TranscodingProfiles.push({
                Container: 'mp4',
                Type: 'Video',
                AudioCodec: videoAudioCodecs.join(','),
                VideoCodec: 'h264',
                Context: 'Streaming',
                Protocol: 'http'
                // Edit: Can't use this in firefox because we're seeing situations of no sound when downmixing from 6 channel to 2
                //MaxAudioChannels: physicalAudioChannels.toString()
            });
        }

        if (canPlayWebm) {

            profile.TranscodingProfiles.push({
                Container: 'webm',
                Type: 'Video',
                AudioCodec: 'vorbis',
                VideoCodec: 'vpx',
                Context: 'Streaming',
                Protocol: 'http',
                // If audio transcoding is needed, limit channels to number of physical audio channels
                // Trying to transcode to 5 channels when there are only 2 speakers generally does not sound good
                MaxAudioChannels: physicalAudioChannels.toString()
            });
        }

        profile.TranscodingProfiles.push({
            Container: 'mp4',
            Type: 'Video',
            AudioCodec: videoAudioCodecs.join(','),
            VideoCodec: 'h264',
            Context: 'Streaming',
            Protocol: 'http',
            // If audio transcoding is needed, limit channels to number of physical audio channels
            // Trying to transcode to 5 channels when there are only 2 speakers generally does not sound good
            MaxAudioChannels: physicalAudioChannels.toString()
        });

        profile.TranscodingProfiles.push({
            Container: 'mp4',
            Type: 'Video',
            AudioCodec: videoAudioCodecs.join(','),
            VideoCodec: 'h264',
            Context: 'Static',
            Protocol: 'http'
        });

        profile.ContainerProfiles = [];

        profile.CodecProfiles = [];
        profile.CodecProfiles.push({
            Type: 'Audio',
            Conditions: [{
                Condition: 'LessThanEqual',
                Property: 'AudioChannels',
                Value: '2'
            }]
        });

        var videoAudioChannels = '6';

        // Handle he-aac not supported
        if (!videoTestElement.canPlayType('video/mp4; codecs="avc1.640029, mp4a.40.5"').replace(/no/, '')) {
            profile.CodecProfiles.push({
                Type: 'VideoAudio',
                Codec: 'aac',
                Conditions: [
                    {
                        Condition: 'NotEquals',
                        Property: 'AudioProfile',
                        Value: 'HE-AAC'
                    },
                    {
                        Condition: 'LessThanEqual',
                        Property: 'AudioChannels',
                        Value: videoAudioChannels
                    },
                    {
                        Condition: 'LessThanEqual',
                        Property: 'AudioBitrate',
                        Value: '128000'
                    },
                    {
                        Condition: 'Equals',
                        Property: 'IsSecondaryAudio',
                        Value: 'false',
                        IsRequired: 'false'
                    }
                ]
            });
        }

        profile.CodecProfiles.push({
            Type: 'VideoAudio',
            Conditions: [
                {
                    Condition: 'LessThanEqual',
                    Property: 'AudioChannels',
                    Value: videoAudioChannels
                },
                {
                    Condition: 'Equals',
                    Property: 'IsSecondaryAudio',
                    Value: 'false',
                    IsRequired: 'false'
                }
            ]
        });

        var maxLevel = '41';

        if (browser.chrome && !browser.mobile) {
            maxLevel = '51';
        }

        profile.CodecProfiles.push({
            Type: 'Video',
            Codec: 'h264',
            Conditions: [
            {
                Condition: 'NotEquals',
                Property: 'IsAnamorphic',
                Value: 'true',
                IsRequired: false
            },
            {
                Condition: 'EqualsAny',
                Property: 'VideoProfile',
                Value: 'high|main|baseline|constrained baseline'
            },
            {
                Condition: 'LessThanEqual',
                Property: 'VideoLevel',
                Value: maxLevel
            }]
        });

        profile.CodecProfiles.push({
            Type: 'Video',
            Codec: 'vpx',
            Conditions: [
            {
                Condition: 'NotEquals',
                Property: 'IsAnamorphic',
                Value: 'true',
                IsRequired: false
            }]
        });

        // Subtitle profiles
        // External vtt or burn in
        profile.SubtitleProfiles = [];
        if (supportsTextTracks()) {

            profile.SubtitleProfiles.push({
                Format: 'vtt',
                Method: 'External'
            });
        }

        profile.ResponseProfiles = [];

        profile.ResponseProfiles.push({
            Type: 'Video',
            Container: 'm4v',
            MimeType: 'video/mp4'
        });

        if (browser.chrome) {
            profile.ResponseProfiles.push({
                Type: 'Video',
                Container: 'mov',
                MimeType: 'video/webm'
            });
        }

        return profile;
    };
});