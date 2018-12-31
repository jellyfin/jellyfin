define(['browser'], function (browser) {
    'use strict';

    function canPlayH264(videoTestElement) {
        return !!(videoTestElement.canPlayType && videoTestElement.canPlayType('video/mp4; codecs="avc1.42E01E, mp4a.40.2"').replace(/no/, ''));
    }

    function canPlayH265(videoTestElement, options) {

        if (browser.tizen || browser.orsay || browser.xboxOne || browser.web0s || options.supportsHevc) {
            return true;
        }

        var userAgent = navigator.userAgent.toLowerCase();

        if (browser.chromecast) {

            var isChromecastUltra = userAgent.indexOf('aarch64') !== -1;
            if (isChromecastUltra) {
                return true;
            }
        }

        // Unfortunately haven't yet found a canPlayType for proper detection
        if (browser.iOS && (browser.iOSVersion || 0) >= 11) {
            return true;
        }

        return !!(videoTestElement.canPlayType && videoTestElement.canPlayType('video/hevc; codecs="hevc, aac"').replace(/no/, ''));
    }

    var _supportsTextTracks;
    function supportsTextTracks() {

        if (browser.tizen || browser.orsay) {
            return true;
        }

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

        if (browser.tizen || browser.orsay) {
            return true;
        }

        var media = document.createElement('video');
        if (media.canPlayType('application/x-mpegURL').replace(/no/, '') ||
            media.canPlayType('application/vnd.apple.mpegURL').replace(/no/, '')) {
            return true;
        }

        return false;
    }

    function canPlayHlsWithMSE() {
        if (window.MediaSource != null) {
            // text tracks don’t work with this in firefox
            return true;
        }

        return false;
    }

    function canPlayAudioFormat(format) {

        var typeString;

        if (format === 'flac') {
            if (browser.tizen || browser.orsay || browser.web0s) {
                return true;
            }
            if (browser.edgeUwp) {
                return true;
            }
        }

        else if (format === 'wma') {
            if (browser.tizen || browser.orsay) {
                return true;
            }
            if (browser.edgeUwp) {
                return true;
            }
        }

        else if (format === 'opus') {
            typeString = 'audio/ogg; codecs="opus"';

            if (document.createElement('audio').canPlayType(typeString).replace(/no/, '')) {
                return true;
            }

            return false;
        }

        else if (format === 'mp2') {

            // For now
            return false;
        }

        if (format === 'webma') {
            typeString = 'audio/webm';
        } else if (format === 'mp2') {
            typeString = 'audio/mpeg';
        } else if (format === 'ogg' || format === 'oga') {

            // chrome says probably, but seeing failures
            if (browser.chrome) {
                return false;
            }
            typeString = 'audio/' + format;

        } else {
            typeString = 'audio/' + format;
        }

        if (document.createElement('audio').canPlayType(typeString).replace(/no/, '')) {
            return true;
        }

        return false;
    }

    function testCanPlayMkv(videoTestElement) {

        if (browser.tizen || browser.orsay || browser.web0s) {
            return true;
        }

        if (videoTestElement.canPlayType('video/x-matroska').replace(/no/, '') ||
            videoTestElement.canPlayType('video/mkv').replace(/no/, '')) {
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
            if (userAgent.indexOf('vivaldi') !== -1 || userAgent.indexOf('opera') !== -1) {
                return false;
            }

            return true;
        }

        if (browser.edgeUwp) {

            return true;
        }

        return false;
    }

    function testCanPlayTs() {
        return browser.tizen || browser.orsay || browser.web0s || browser.edgeUwp;
    }

    function supportsMpeg2Video() {
        return browser.orsay || browser.tizen || browser.edgeUwp || browser.web0s;
    }

    function supportsVc1() {
        return browser.orsay || browser.tizen || browser.edgeUwp || browser.web0s;
    }

    function getFlvMseDirectPlayProfile() {

        var videoAudioCodecs = ['aac'];

        if (!browser.edge && !browser.msie) {
            videoAudioCodecs.push('mp3');
        }

        return {
            Container: 'flv',
            Type: 'Video',
            VideoCodec: 'h264',
            AudioCodec: videoAudioCodecs.join(',')
        };
    }

    function getDirectPlayProfileForVideoContainer(container, videoAudioCodecs, videoTestElement, options) {

        var supported = false;
        var profileContainer = container;
        var videoCodecs = [];

        switch (container) {

            case 'asf':
                supported = browser.tizen || browser.orsay || browser.edgeUwp;
                videoAudioCodecs = [];
                break;
            case 'avi':
                supported = browser.tizen || browser.orsay || browser.edgeUwp;
                break;
            case 'mpg':
            case 'mpeg':
                supported = browser.edgeUwp || browser.tizen || browser.orsay;
                break;
            case 'flv':
                supported = browser.tizen || browser.orsay;
                //if (!supported && window.MediaSource != null && window.MediaSource.isTypeSupported('video/mp4; codecs="avc1.42E01E,mp4a.40.2"')) {
                //    return getFlvMseDirectPlayProfile();
                //}
                break;
            case '3gp':
            case 'mts':
            case 'trp':
            case 'vob':
            case 'vro':
                supported = browser.tizen || browser.orsay;
                break;
            case 'mov':
                supported = browser.tizen || browser.orsay || browser.chrome || browser.edgeUwp;
                videoCodecs.push('h264');
                break;
            case 'm2ts':
                supported = browser.tizen || browser.orsay || browser.web0s || browser.edgeUwp;
                videoCodecs.push('h264');
                if (supportsVc1()) {
                    videoCodecs.push('vc1');
                }
                if (supportsMpeg2Video()) {
                    videoCodecs.push('mpeg2video');
                }
                break;
            case 'wmv':
                supported = browser.tizen || browser.orsay || browser.web0s || browser.edgeUwp;
                videoAudioCodecs = [];
                break;
            case 'ts':
                supported = testCanPlayTs();
                videoCodecs.push('h264');
                if (canPlayH265(videoTestElement, options)) {
                    videoCodecs.push('h265');
                    videoCodecs.push('hevc');
                }
                if (supportsVc1()) {
                    videoCodecs.push('vc1');
                }
                if (supportsMpeg2Video()) {
                    videoCodecs.push('mpeg2video');
                }
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
            VideoCodec: videoCodecs.join(','),
            AudioCodec: videoAudioCodecs.join(',')
        };
    }

    function getMaxBitrate() {

        return 120000000;
    }

    function getGlobalMaxVideoBitrate() {

        var userAgent = navigator.userAgent.toLowerCase();

        if (browser.chromecast) {

            var isChromecastUltra = userAgent.indexOf('aarch64') !== -1;
            if (isChromecastUltra) {
                return null;
            }

            // This is a hack to try and detect chromecast on vizio
            if (self.screen && self.screen.width >= 3800) {
                return null;
            }

            return 30000000;
        }

        var isTizenFhd = false;
        if (browser.tizen) {
            try {
                var isTizenUhd = webapis.productinfo.isUdPanelSupported();
                isTizenFhd = !isTizenUhd;
                console.log("isTizenFhd = " + isTizenFhd);
            } catch (error) {
                console.log("isUdPanelSupported() error code = " + error.code);
            }
        }

        return browser.ps4 ? 8000000 :
            (browser.xboxOne ? 12000000 :
                (browser.edgeUwp ? null :
                    (browser.tizen && isTizenFhd ? 20000000 : null)));
    }

    function supportsAc3(videoTestElement) {

        if (browser.edgeUwp || browser.tizen || browser.orsay || browser.web0s) {
            return true;
        }

        return (videoTestElement.canPlayType('audio/mp4; codecs="ac-3"').replace(/no/, '') && !browser.osx && !browser.iOS);
    }

    function supportsEac3(videoTestElement) {

        if (browser.tizen || browser.orsay || browser.web0s) {
            return true;
        }

        return videoTestElement.canPlayType('audio/mp4; codecs="ec-3"').replace(/no/, '');
    }

    return function (options) {

        options = options || {};
        var physicalAudioChannels = options.audioChannels || (browser.tv || browser.ps4 || browser.xboxOne ? 6 : 2);

        var bitrateSetting = getMaxBitrate();

        var videoTestElement = document.createElement('video');

        var canPlayVp8 = videoTestElement.canPlayType('video/webm; codecs="vp8"').replace(/no/, '');
        var canPlayVp9 = videoTestElement.canPlayType('video/webm; codecs="vp9"').replace(/no/, '');
        var webmAudioCodecs = ['vorbis'];

        var canPlayMkv = testCanPlayMkv(videoTestElement);

        var profile = {};

        profile.MaxStreamingBitrate = bitrateSetting;
        profile.MaxStaticBitrate = 100000000;
        profile.MusicStreamingTranscodingBitrate = Math.min(bitrateSetting, 192000);

        profile.DirectPlayProfiles = [];

        var videoAudioCodecs = [];
        var hlsVideoAudioCodecs = [];

        var supportsMp3VideoAudio = videoTestElement.canPlayType('video/mp4; codecs="avc1.640029, mp4a.69"').replace(/no/, '') ||
            videoTestElement.canPlayType('video/mp4; codecs="avc1.640029, mp4a.6B"').replace(/no/, '');

        // Not sure how to test for this
        var supportsMp2VideoAudio = browser.edgeUwp || browser.tizen || browser.orsay || browser.web0s;

        var maxVideoWidth = browser.xboxOne ?
            (self.screen ? self.screen.width : null) :
            null;

        if (options.maxVideoWidth) {
            maxVideoWidth = options.maxVideoWidth;
        }

        var canPlayAacVideoAudio = videoTestElement.canPlayType('video/mp4; codecs="avc1.640029, mp4a.40.2"').replace(/no/, '');

        if (canPlayAacVideoAudio && browser.chromecast && physicalAudioChannels <= 2) {
            // prioritize this first
            videoAudioCodecs.push('aac');
        }

        // Only put mp3 first if mkv support is there
        // Otherwise with HLS and mp3 audio we're seeing some browsers
        // safari is lying
        if (supportsAc3(videoTestElement)) {

            videoAudioCodecs.push('ac3');

            var eAc3 = supportsEac3(videoTestElement);
            if (eAc3) {
                videoAudioCodecs.push('eac3');
            }

            // This works in edge desktop, but not mobile
            // TODO: Retest this on mobile
            var supportsAc3InHls = (!browser.edge || !browser.touch || browser.edgeUwp);
            if (supportsAc3InHls) {
                hlsVideoAudioCodecs.push('ac3');
                if (eAc3) {
                    hlsVideoAudioCodecs.push('eac3');
                }
            }
        }

        if (canPlayAacVideoAudio && browser.chromecast && videoAudioCodecs.indexOf('aac') === -1) {
            // prioritize this first
            videoAudioCodecs.push('aac');
        }

        if (supportsMp3VideoAudio) {
            videoAudioCodecs.push('mp3');

            // PS4 fails to load HLS with mp3 audio
            if (!browser.ps4) {

                // mp3 encoder only supports 2 channels, so only make that preferred if we're only requesting 2 channels
                // Also apply it for chromecast because it no longer supports AAC 5.1
                if (physicalAudioChannels <= 2) {
                    hlsVideoAudioCodecs.push('mp3');
                }
            }
        }
        if (canPlayAacVideoAudio) {

            if (videoAudioCodecs.indexOf('aac') === -1) {
                videoAudioCodecs.push('aac');
            }

            hlsVideoAudioCodecs.push('aac');
        }
        if (supportsMp3VideoAudio) {
            // PS4 fails to load HLS with mp3 audio
            if (!browser.ps4) {
                if (hlsVideoAudioCodecs.indexOf('mp3') === -1) {
                    hlsVideoAudioCodecs.push('mp3');
                }
            }
        }

        if (supportsMp2VideoAudio) {
            videoAudioCodecs.push('mp2');
        }

        var supportsDts = browser.tizen || browser.orsay || browser.web0s || options.supportsDts;

        if (self.tizen && self.tizen.systeminfo) {
            var v = tizen.systeminfo.getCapability('http://tizen.org/feature/platform.version');

            // DTS audio not supported in 2018 models (Tizen 4.0)
            if (v && parseFloat(v) >= parseFloat('4.0')) {
                supportsDts = false;
            }
        }

        if (supportsDts) {
            videoAudioCodecs.push('dca');
            videoAudioCodecs.push('dts');
        }

        if (browser.tizen || browser.orsay || browser.web0s) {
            videoAudioCodecs.push('pcm_s16le');
            videoAudioCodecs.push('pcm_s24le');
        }

        if (options.supportsTrueHd) {
            videoAudioCodecs.push('truehd');
        }

        if (browser.tizen || browser.orsay) {
            videoAudioCodecs.push('aac_latm');
        }

        if (canPlayAudioFormat('opus')) {
            videoAudioCodecs.push('opus');
            hlsVideoAudioCodecs.push('opus');
            webmAudioCodecs.push('opus');
        }

        if (canPlayAudioFormat('flac')) {
            videoAudioCodecs.push('flac');
        }

        videoAudioCodecs = videoAudioCodecs.filter(function (c) {
            return (options.disableVideoAudioCodecs || []).indexOf(c) === -1;
        });

        hlsVideoAudioCodecs = hlsVideoAudioCodecs.filter(function (c) {
            return (options.disableHlsVideoAudioCodecs || []).indexOf(c) === -1;
        });

        var mp4VideoCodecs = [];
        var hlsVideoCodecs = [];

        if (canPlayH264(videoTestElement)) {
            mp4VideoCodecs.push('h264');
            hlsVideoCodecs.push('h264');
        }
        if (canPlayH265(videoTestElement, options)) {
            mp4VideoCodecs.push('h265');
            mp4VideoCodecs.push('hevc');

            if (browser.tizen || browser.web0s) {
                hlsVideoCodecs.push('h265');
                hlsVideoCodecs.push('hevc');
            }
        }

        if (supportsMpeg2Video()) {
            mp4VideoCodecs.push('mpeg2video');
        }

        if (supportsVc1()) {
            mp4VideoCodecs.push('vc1');
        }

        if (browser.tizen || browser.orsay) {
            mp4VideoCodecs.push('msmpeg4v2');
        }

        if (canPlayVp8) {
            mp4VideoCodecs.push('vp8');
        }
        if (canPlayVp9) {
            mp4VideoCodecs.push('vp9');
        }

        if (canPlayVp8 || browser.tizen || browser.orsay) {
            videoAudioCodecs.push('vorbis');
        }

        if (mp4VideoCodecs.length) {
            profile.DirectPlayProfiles.push({
                Container: 'mp4,m4v',
                Type: 'Video',
                VideoCodec: mp4VideoCodecs.join(','),
                AudioCodec: videoAudioCodecs.join(',')
            });
        }

        if (canPlayMkv && mp4VideoCodecs.length) {
            profile.DirectPlayProfiles.push({
                Container: 'mkv',
                Type: 'Video',
                VideoCodec: mp4VideoCodecs.join(','),
                AudioCodec: videoAudioCodecs.join(',')
            });
        }

        // These are formats we can't test for but some devices will support
        ['m2ts', 'wmv', 'ts', 'asf', 'avi', 'mpg', 'mpeg', 'flv', '3gp', 'mts', 'trp', 'vob', 'vro', 'mov'].map(function (container) {
            return getDirectPlayProfileForVideoContainer(container, videoAudioCodecs, videoTestElement, options);
        }).filter(function (i) {
            return i != null;
        }).forEach(function (i) {
            profile.DirectPlayProfiles.push(i);
        });

        ['opus', 'mp3', 'mp2', 'aac', 'flac', 'alac', 'webma', 'wma', 'wav', 'ogg', 'oga'].filter(canPlayAudioFormat).forEach(function (audioFormat) {

            if (audioFormat === 'mp2') {

                profile.DirectPlayProfiles.push({
                    Container: 'mp2,mp3',
                    Type: 'Audio',
                    AudioCodec: audioFormat
                });
            }

            else if (audioFormat === 'mp3') {

                profile.DirectPlayProfiles.push({
                    Container: audioFormat,
                    Type: 'Audio',
                    AudioCodec: audioFormat
                });

            } else {
                profile.DirectPlayProfiles.push({
                    Container: audioFormat === 'webma' ? 'webma,webm' : audioFormat,
                    Type: 'Audio'
                });
            }

            // aac also appears in the m4a container
            if (audioFormat === 'aac' || audioFormat === 'alac') {

                profile.DirectPlayProfiles.push({
                    Container: 'm4a',
                    AudioCodec: audioFormat,
                    Type: 'Audio'
                });
            }
        });

        if (canPlayVp8) {
            profile.DirectPlayProfiles.push({
                Container: 'webm',
                Type: 'Video',
                AudioCodec: webmAudioCodecs.join(','),
                VideoCodec: 'VP8'
            });
        }

        if (canPlayVp9) {
            profile.DirectPlayProfiles.push({
                Container: 'webm',
                Type: 'Video',
                AudioCodec: webmAudioCodecs.join(','),
                VideoCodec: 'VP9'
            });
        }

        profile.TranscodingProfiles = [];

        var hlsBreakOnNonKeyFrames = browser.iOS || browser.osx || browser.edge || !canPlayNativeHls() ? true : false;

        if (canPlayHls() && browser.enableHlsAudio !== false) {
            profile.TranscodingProfiles.push({

                // hlsjs, edge, and android all seem to require ts container
                Container: !canPlayNativeHls() || browser.edge || browser.android ? 'ts' : 'aac',
                Type: 'Audio',
                AudioCodec: 'aac',
                Context: 'Streaming',
                Protocol: 'hls',
                MaxAudioChannels: physicalAudioChannels.toString(),
                MinSegments: browser.iOS || browser.osx ? '2' : '1',
                BreakOnNonKeyFrames: hlsBreakOnNonKeyFrames
            });
        }

        // For streaming, prioritize opus transcoding after mp3/aac. It is too problematic with random failures
        // But for static (offline sync), it will be just fine.
        // Prioritize aac higher because the encoder can accept more channels than mp3
        ['aac', 'mp3', 'opus', 'wav'].filter(canPlayAudioFormat).forEach(function (audioFormat) {

            profile.TranscodingProfiles.push({
                Container: audioFormat,
                Type: 'Audio',
                AudioCodec: audioFormat,
                Context: 'Streaming',
                Protocol: 'http',
                MaxAudioChannels: physicalAudioChannels.toString()
            });
        });

        ['opus', 'mp3', 'aac', 'wav'].filter(canPlayAudioFormat).forEach(function (audioFormat) {

            profile.TranscodingProfiles.push({
                Container: audioFormat,
                Type: 'Audio',
                AudioCodec: audioFormat,
                Context: 'Static',
                Protocol: 'http',
                MaxAudioChannels: physicalAudioChannels.toString()
            });
        });

        if (canPlayMkv && !browser.tizen && !browser.orsay && options.enableMkvProgressive !== false) {
            profile.TranscodingProfiles.push({
                Container: 'mkv',
                Type: 'Video',
                AudioCodec: videoAudioCodecs.join(','),
                VideoCodec: mp4VideoCodecs.join(','),
                Context: 'Streaming',
                MaxAudioChannels: physicalAudioChannels.toString(),
                CopyTimestamps: true
            });
        }

        if (canPlayMkv) {
            profile.TranscodingProfiles.push({
                Container: 'mkv',
                Type: 'Video',
                AudioCodec: videoAudioCodecs.join(','),
                VideoCodec: mp4VideoCodecs.join(','),
                Context: 'Static',
                MaxAudioChannels: physicalAudioChannels.toString(),
                CopyTimestamps: true
            });
        }

        if (canPlayHls() && options.enableHls !== false) {
            profile.TranscodingProfiles.push({
                Container: 'ts',
                Type: 'Video',
                AudioCodec: hlsVideoAudioCodecs.join(','),
                VideoCodec: hlsVideoCodecs.join(','),
                Context: 'Streaming',
                Protocol: 'hls',
                MaxAudioChannels: physicalAudioChannels.toString(),
                MinSegments: browser.iOS || browser.osx ? '2' : '1',
                BreakOnNonKeyFrames: hlsBreakOnNonKeyFrames
            });
        }

        if (canPlayVp8) {
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
            Context: 'Static',
            Protocol: 'http'
        });

        profile.ContainerProfiles = [];

        profile.CodecProfiles = [];

        var supportsSecondaryAudio = browser.tizen || browser.orsay || videoTestElement.audioTracks;

        var aacCodecProfileConditions = [];

        // Handle he-aac not supported
        if (!videoTestElement.canPlayType('video/mp4; codecs="avc1.640029, mp4a.40.5"').replace(/no/, '')) {
            // TODO: This needs to become part of the stream url in order to prevent stream copy
            aacCodecProfileConditions.push({
                Condition: 'NotEquals',
                Property: 'AudioProfile',
                Value: 'HE-AAC'
            });
        }

        if (!supportsSecondaryAudio) {
            aacCodecProfileConditions.push({
                Condition: 'Equals',
                Property: 'IsSecondaryAudio',
                Value: 'false',
                IsRequired: 'false'
            });
        }

        if (browser.chromecast) {
            aacCodecProfileConditions.push({
                Condition: 'LessThanEqual',
                Property: 'AudioChannels',
                Value: '2',
                IsRequired: true
            });
        }

        if (aacCodecProfileConditions.length) {
            profile.CodecProfiles.push({
                Type: 'VideoAudio',
                Codec: 'aac',
                Conditions: aacCodecProfileConditions
            });
        }

        if (!supportsSecondaryAudio) {
            profile.CodecProfiles.push({
                Type: 'VideoAudio',
                Conditions: [
                    {
                        Condition: 'Equals',
                        Property: 'IsSecondaryAudio',
                        Value: 'false',
                        IsRequired: 'false'
                    }
                ]
            });
        }

        var maxH264Level = browser.chromecast ? 42 : 51;
        var h264Profiles = 'high|main|baseline|constrained baseline';

        if (maxH264Level >= 51 && browser.chrome && !browser.osx) {
            h264Profiles += '|high 10';
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
                    Value: h264Profiles
                },
                {
                    Condition: 'LessThanEqual',
                    Property: 'VideoLevel',
                    Value: maxH264Level.toString()
                }]
        });

        if (!browser.edgeUwp && !browser.tizen && !browser.orsay && !browser.web0s) {
            //profile.CodecProfiles[profile.CodecProfiles.length - 1].Conditions.push({
            //    Condition: 'NotEquals',
            //    Property: 'IsAVC',
            //    Value: 'false',
            //    IsRequired: false
            //});

            //profile.CodecProfiles[profile.CodecProfiles.length - 1].Conditions.push({
            //    Condition: 'NotEquals',
            //    Property: 'IsInterlaced',
            //    Value: 'true',
            //    IsRequired: false
            //});
        }

        if (maxVideoWidth) {
            profile.CodecProfiles[profile.CodecProfiles.length - 1].Conditions.push({
                Condition: 'LessThanEqual',
                Property: 'Width',
                Value: maxVideoWidth.toString(),
                IsRequired: false
            });
        }

        var globalMaxVideoBitrate = (getGlobalMaxVideoBitrate() || '').toString();

        var h264MaxVideoBitrate = globalMaxVideoBitrate;

        if (h264MaxVideoBitrate) {
            profile.CodecProfiles[profile.CodecProfiles.length - 1].Conditions.push({
                Condition: 'LessThanEqual',
                Property: 'VideoBitrate',
                Value: h264MaxVideoBitrate,
                IsRequired: true
            });
        }

        var globalVideoConditions = [];

        if (globalMaxVideoBitrate) {
            globalVideoConditions.push({
                Condition: 'LessThanEqual',
                Property: 'VideoBitrate',
                Value: globalMaxVideoBitrate
            });
        }

        if (maxVideoWidth) {
            globalVideoConditions.push({
                Condition: 'LessThanEqual',
                Property: 'Width',
                Value: maxVideoWidth.toString(),
                IsRequired: false
            });
        }

        if (globalVideoConditions.length) {
            profile.CodecProfiles.push({
                Type: 'Video',
                Conditions: globalVideoConditions
            });
        }

        if (browser.chromecast) {
            profile.CodecProfiles.push({
                Type: 'Audio',
                Codec: 'flac',
                Conditions: [
                    {
                        Condition: 'LessThanEqual',
                        Property: 'AudioSampleRate',
                        Value: '96000'
                    }]
            });
        }

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

        return profile;
    };
});