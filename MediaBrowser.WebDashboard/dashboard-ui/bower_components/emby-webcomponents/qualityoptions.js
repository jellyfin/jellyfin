define(['globalize'], function (globalize) {
    'use strict';

    function getVideoQualityOptions(options) {

        var maxStreamingBitrate = options.currentMaxBitrate;
        var videoWidth = options.videoWidth;

        var maxAllowedWidth = videoWidth || 4096;
        //var maxAllowedHeight = videoHeight || 2304;

        var qualityOptions = [];

        if (maxAllowedWidth >= 3800) {
            qualityOptions.push({ name: '4K - 120 Mbps', maxHeight: 2160, bitrate: 120000000 });
            qualityOptions.push({ name: '4K - 100 Mbps', maxHeight: 2160, bitrate: 100000000 });
            qualityOptions.push({ name: '4K - 80 Mbps', maxHeight: 2160, bitrate: 80000000 });
        }

        // Some 1080- videos are reported as 1912?
        if (maxAllowedWidth >= 1900) {

            qualityOptions.push({ name: '1080p - 60 Mbps', maxHeight: 1080, bitrate: 60000000 });
            qualityOptions.push({ name: '1080p - 50 Mbps', maxHeight: 1080, bitrate: 50000000 });
            qualityOptions.push({ name: '1080p - 40 Mbps', maxHeight: 1080, bitrate: 40000000 });
            qualityOptions.push({ name: '1080p - 30 Mbps', maxHeight: 1080, bitrate: 30000000 });
            qualityOptions.push({ name: '1080p - 25 Mbps', maxHeight: 1080, bitrate: 25000000 });
            qualityOptions.push({ name: '1080p - 20 Mbps', maxHeight: 1080, bitrate: 20000000 });
            qualityOptions.push({ name: '1080p - 15 Mbps', maxHeight: 1080, bitrate: 15000000 });
            qualityOptions.push({ name: '1080p - 10 Mbps', maxHeight: 1080, bitrate: 10000001 });
            qualityOptions.push({ name: '1080p - 8 Mbps', maxHeight: 1080, bitrate: 8000001 });
            qualityOptions.push({ name: '1080p - 6 Mbps', maxHeight: 1080, bitrate: 6000001 });
            qualityOptions.push({ name: '1080p - 5 Mbps', maxHeight: 1080, bitrate: 5000001 });
            qualityOptions.push({ name: '1080p - 4 Mbps', maxHeight: 1080, bitrate: 4000002 });

        } else if (maxAllowedWidth >= 1260) {
            qualityOptions.push({ name: '720p - 10 Mbps', maxHeight: 720, bitrate: 10000000 });
            qualityOptions.push({ name: '720p - 8 Mbps', maxHeight: 720, bitrate: 8000000 });
            qualityOptions.push({ name: '720p - 6 Mbps', maxHeight: 720, bitrate: 6000000 });
            qualityOptions.push({ name: '720p - 5 Mbps', maxHeight: 720, bitrate: 5000000 });

        } else if (maxAllowedWidth >= 620) {
            qualityOptions.push({ name: '480p - 4 Mbps', maxHeight: 480, bitrate: 4000001 });
            qualityOptions.push({ name: '480p - 3 Mbps', maxHeight: 480, bitrate: 3000001 });
            qualityOptions.push({ name: '480p - 2.5 Mbps', maxHeight: 480, bitrate: 2500000 });
            qualityOptions.push({ name: '480p - 2 Mbps', maxHeight: 480, bitrate: 2000001 });
            qualityOptions.push({ name: '480p - 1.5 Mbps', maxHeight: 480, bitrate: 1500001 });
        }

        if (maxAllowedWidth >= 1260) {
            qualityOptions.push({ name: '720p - 4 Mbps', maxHeight: 720, bitrate: 4000000 });
            qualityOptions.push({ name: '720p - 3 Mbps', maxHeight: 720, bitrate: 3000000 });
            qualityOptions.push({ name: '720p - 2 Mbps', maxHeight: 720, bitrate: 2000000 });

            // The extra 1 is because they're keyed off the bitrate value
            qualityOptions.push({ name: '720p - 1.5 Mbps', maxHeight: 720, bitrate: 1500000 });
            qualityOptions.push({ name: '720p - 1 Mbps', maxHeight: 720, bitrate: 1000001 });
        }

        qualityOptions.push({ name: '480p - 1 Mbps', maxHeight: 480, bitrate: 1000000 });
        qualityOptions.push({ name: '480p - 720 kbps', maxHeight: 480, bitrate: 720000 });
        qualityOptions.push({ name: '480p - 420 kbps', maxHeight: 480, bitrate: 420000 });
        qualityOptions.push({ name: '360p', maxHeight: 360, bitrate: 400000 });
        qualityOptions.push({ name: '240p', maxHeight: 240, bitrate: 320000 });
        qualityOptions.push({ name: '144p', maxHeight: 144, bitrate: 192000 });

        var autoQualityOption = {
            name: globalize.translate('sharedcomponents#Auto'),
            bitrate: 0,
            selected: options.isAutomaticBitrateEnabled
        };

        if (options.enableAuto) {
            qualityOptions.push(autoQualityOption);
        }

        if (maxStreamingBitrate) {
            var selectedIndex = -1;
            for (var i = 0, length = qualityOptions.length; i < length; i++) {

                var option = qualityOptions[i];

                if (selectedIndex === -1 && option.bitrate <= maxStreamingBitrate) {
                    selectedIndex = i;
                }
            }

            if (selectedIndex === -1) {

                selectedIndex = qualityOptions.length - 1;
            }

            var currentQualityOption = qualityOptions[selectedIndex];

            if (!options.isAutomaticBitrateEnabled) {
                currentQualityOption.selected = true;
            } else {
                autoQualityOption.autoText = currentQualityOption.name;
            }
        }

        return qualityOptions;
    }

    function getAudioQualityOptions(options) {

        var maxStreamingBitrate = options.currentMaxBitrate;

        var qualityOptions = [];

        qualityOptions.push({ name: '2 Mbps', bitrate: 2000000 });
        qualityOptions.push({ name: '1.5 Mbps', bitrate: 1500000 });
        qualityOptions.push({ name: '1 Mbps', bitrate: 1000000 });
        qualityOptions.push({ name: '320 kbps', bitrate: 320000 });
        qualityOptions.push({ name: '256 kbps', bitrate: 256000 });
        qualityOptions.push({ name: '192 kbps', bitrate: 192000 });
        qualityOptions.push({ name: '128 kbps', bitrate: 128000 });
        qualityOptions.push({ name: '96 kbps', bitrate: 96000 });
        qualityOptions.push({ name: '64 kbps', bitrate: 64000 });

        var autoQualityOption = {
            name: globalize.translate('sharedcomponents#Auto'),
            bitrate: 0,
            selected: options.isAutomaticBitrateEnabled
        };

        if (options.enableAuto) {
            qualityOptions.push(autoQualityOption);
        }

        if (maxStreamingBitrate) {
            var selectedIndex = -1;
            for (var i = 0, length = qualityOptions.length; i < length; i++) {

                var option = qualityOptions[i];

                if (selectedIndex === -1 && option.bitrate <= maxStreamingBitrate) {
                    selectedIndex = i;
                }
            }

            if (selectedIndex === -1) {

                selectedIndex = qualityOptions.length - 1;
            }

            var currentQualityOption = qualityOptions[selectedIndex];

            if (!options.isAutomaticBitrateEnabled) {
                currentQualityOption.selected = true;
            } else {
                autoQualityOption.autoText = currentQualityOption.name;
            }
        }

        return qualityOptions;
    }

    return {
        getVideoQualityOptions: getVideoQualityOptions,
        getAudioQualityOptions: getAudioQualityOptions
    };
});