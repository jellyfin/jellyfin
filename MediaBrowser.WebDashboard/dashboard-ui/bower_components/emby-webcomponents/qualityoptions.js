define([], function () {
    'use strict';

    function getVideoQualityOptions(maxStreamingBitrate, videoWidth) {

        var maxAllowedWidth = videoWidth || 4096;
        //var maxAllowedHeight = videoHeight || 2304;

        var options = [];

        // Some 1080- videos are reported as 1912?
        if (maxAllowedWidth >= 1900) {

            options.push({ name: '1080p - 60Mbps', maxHeight: 1080, bitrate: 60000000 });
            options.push({ name: '1080p - 50Mbps', maxHeight: 1080, bitrate: 50000000 });
            options.push({ name: '1080p - 40Mbps', maxHeight: 1080, bitrate: 40000000 });
            options.push({ name: '1080p - 30Mbps', maxHeight: 1080, bitrate: 30000000 });
            options.push({ name: '1080p - 25Mbps', maxHeight: 1080, bitrate: 25000000 });
            options.push({ name: '1080p - 20Mbps', maxHeight: 1080, bitrate: 20000000 });
            options.push({ name: '1080p - 15Mbps', maxHeight: 1080, bitrate: 15000000 });
            options.push({ name: '1080p - 10Mbps', maxHeight: 1080, bitrate: 10000001 });
            options.push({ name: '1080p - 8Mbps', maxHeight: 1080, bitrate: 8000001 });
            options.push({ name: '1080p - 6Mbps', maxHeight: 1080, bitrate: 6000001 });
            options.push({ name: '1080p - 5Mbps', maxHeight: 1080, bitrate: 5000001 });
            options.push({ name: '1080p - 4Mbps', maxHeight: 1080, bitrate: 4000002 });

        } else if (maxAllowedWidth >= 1260) {
            options.push({ name: '720p - 10Mbps', maxHeight: 720, bitrate: 10000000 });
            options.push({ name: '720p - 8Mbps', maxHeight: 720, bitrate: 8000000 });
            options.push({ name: '720p - 6Mbps', maxHeight: 720, bitrate: 6000000 });
            options.push({ name: '720p - 5Mbps', maxHeight: 720, bitrate: 5000000 });

        } else if (maxAllowedWidth >= 700) {
            options.push({ name: '480p - 4Mbps', maxHeight: 480, bitrate: 4000001 });
            options.push({ name: '480p - 3Mbps', maxHeight: 480, bitrate: 3000001 });
            options.push({ name: '480p - 2.5Mbps', maxHeight: 480, bitrate: 2500000 });
            options.push({ name: '480p - 2Mbps', maxHeight: 480, bitrate: 2000001 });
            options.push({ name: '480p - 1.5Mbps', maxHeight: 480, bitrate: 1500001 });
        }

        if (maxAllowedWidth >= 1260) {
            options.push({ name: '720p - 4Mbps', maxHeight: 720, bitrate: 4000000 });
            options.push({ name: '720p - 3Mbps', maxHeight: 720, bitrate: 3000000 });
            options.push({ name: '720p - 2Mbps', maxHeight: 720, bitrate: 2000000 });

            // The extra 1 is because they're keyed off the bitrate value
            options.push({ name: '720p - 1.5Mbps', maxHeight: 720, bitrate: 1500000 });
            options.push({ name: '720p - 1Mbps', maxHeight: 720, bitrate: 1000001 });
        }

        options.push({ name: '480p - 1.0Mbps', maxHeight: 480, bitrate: 1000000 });
        options.push({ name: '480p - 720kbps', maxHeight: 480, bitrate: 720000 });
        options.push({ name: '480p - 420kbps', maxHeight: 480, bitrate: 420000 });
        options.push({ name: '360p', maxHeight: 360, bitrate: 400000 });
        options.push({ name: '240p', maxHeight: 240, bitrate: 320000 });
        options.push({ name: '144p', maxHeight: 144, bitrate: 192000 });

        if (maxStreamingBitrate) {
            var selectedIndex = -1;
            for (var i = 0, length = options.length; i < length; i++) {

                var option = options[i];

                if (selectedIndex === -1 && option.bitrate <= maxStreamingBitrate) {
                    selectedIndex = i;
                }
            }

            if (selectedIndex === -1) {

                selectedIndex = options.length - 1;
            }

            options[selectedIndex].selected = true;
        }

        return options;
    }

    return {
        getVideoQualityOptions: getVideoQualityOptions
    };
});