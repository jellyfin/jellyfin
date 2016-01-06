define(['browser'], function (browser) {

    function canPlayH264() {
        var v = document.createElement('video');
        return !!(v.canPlayType && v.canPlayType('video/mp4; codecs="avc1.42E01E, mp4a.40.2"').replace(/no/, ''));
    }

    var supportedFormats;
    function getSupportedFormats() {

        if (supportedFormats) {
            return supportedFormats;
        }

        var list = [];
        var elem = document.createElement('video');

        if (elem.canPlayType('video/webm').replace(/no/, '')) {
            list.push('webm');
        }
        if (elem.canPlayType('audio/mp4; codecs="ac-3"').replace(/no/, '')) {
            list.push('ac3');
        }
        if (browser.chrome) {
            list.push('mkv');
        }

        if (canPlayH264()) {
            list.push('h264');
        }

        if (document.createElement('audio').canPlayType('audio/aac').replace(/no/, '')) {
            list.push('aac');
        }

        if (document.createElement('audio').canPlayType('audio/mp3').replace(/no/, '')) {
            list.push('mp3');
        }
        if (document.createElement('audio').canPlayType('audio/ogg; codecs="opus"').replace(/no/, '')) {
            list.push('opus');
        }

        if (document.createElement('audio').canPlayType('audio/webm').replace(/no/, '')) {
            list.push('webma');
        }

        if (document.createElement('audio').canPlayType('audio/flac').replace(/no/, '')) {
            list.push('flac');
        }

        supportedFormats = list;
        return list;
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
            _canPlayHls = window.MediaSource != null || canPlayNativeHls();
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

    return function () {

        var bitrateSetting = 100000000;

        var supportedFormats = getSupportedFormats();

        var canPlayWebm = supportedFormats.indexOf('webm') != -1;
        var canPlayAc3 = supportedFormats.indexOf('ac3') != -1;
        var canPlayMp3 = supportedFormats.indexOf('mp3') != -1;
        var canPlayAac = supportedFormats.indexOf('aac') != -1;
        var canPlayMkv = supportedFormats.indexOf('mkv') != -1;

        var profile = {};

        profile.MaxStreamingBitrate = bitrateSetting;
        profile.MaxStaticBitrate = 100000000;
        profile.MusicStreamingTranscodingBitrate = Math.min(bitrateSetting, 192000);

        profile.DirectPlayProfiles = [];

        if (supportedFormats.indexOf('h264') != -1) {
            profile.DirectPlayProfiles.push({
                Container: 'mp4,m4v',
                Type: 'Video',
                VideoCodec: 'h264',
                AudioCodec: 'aac' + (canPlayMp3 ? ',mp3' : '') + (canPlayAc3 ? ',ac3' : '')
            });
        }

        if (canPlayMkv) {
            profile.DirectPlayProfiles.push({
                Container: 'mkv,mov',
                Type: 'Video',
                VideoCodec: 'h264',
                AudioCodec: 'aac' + (canPlayMp3 ? ',mp3' : '') + (canPlayAc3 ? ',ac3' : '')
            });
        }

        ['opus', 'mp3', 'aac', 'flac', 'webma'].forEach(function (audioFormat) {

            if (supportedFormats.indexOf(audioFormat) != -1) {
                profile.DirectPlayProfiles.push({
                    Container: audioFormat == 'webma' ? 'webma,webm' : audioFormat,
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

        ['opus', 'mp3', 'aac'].forEach(function (audioFormat) {

            if (supportedFormats.indexOf(audioFormat) != -1) {
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
            }
        });

        var videoAudioCodecs = [];
        if (canPlayMp3) {
            videoAudioCodecs.push('mp3');
        }
        if (canPlayAac) {
            videoAudioCodecs.push('aac');
        }
        if (canPlayAc3) {
            videoAudioCodecs.push('ac3');
        }

        // Can't use mkv on mobile because we have to use the native player controls and they won't be able to seek it
        if (canPlayMkv && !browser.mobile) {
            profile.TranscodingProfiles.push({
                Container: 'mkv',
                Type: 'Video',
                AudioCodec: videoAudioCodecs.join(','),
                VideoCodec: 'h264',
                Context: 'Streaming'
            });
        }

        if (canPlayHls()) {
            profile.TranscodingProfiles.push({
                Container: 'ts',
                Type: 'Video',
                AudioCodec: videoAudioCodecs.join(','),
                VideoCodec: 'h264',
                Context: 'Streaming',
                Protocol: 'hls'
            });
        }

        if (canPlayWebm) {

            profile.TranscodingProfiles.push({
                Container: 'webm',
                Type: 'Video',
                AudioCodec: 'vorbis',
                VideoCodec: 'vpx',
                Context: 'Streaming',
                Protocol: 'http'
            });
        }

        profile.TranscodingProfiles.push({
            Container: 'mp4',
            Type: 'Video',
            AudioCodec: videoAudioCodecs.join(','),
            VideoCodec: 'h264',
            Context: 'Streaming',
            Protocol: 'http'
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

        var videoAudioChannels = browser.safari ? '2' : '6';

        profile.CodecProfiles.push({
            Type: 'VideoAudio',
            Codec: 'aac',
            Container: 'mkv,mov',
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
                    Condition: 'Equals',
                    Property: 'IsSecondaryAudio',
                    Value: 'false',
                    IsRequired: 'false'
                }
                // Disabling this is going to require us to learn why it was disabled in the first place
                //,
                //{
                //    Condition: 'NotEquals',
                //    Property: 'AudioProfile',
                //    Value: 'LC'
                //}
            ]
        });

        profile.CodecProfiles.push({
            Type: 'VideoAudio',
            Codec: 'aac,mp3',
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
                Value: '41'
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

        profile.ResponseProfiles.push({
            Type: 'Video',
            Container: 'mov',
            MimeType: 'video/webm'
        });

        return profile;
    }();
});