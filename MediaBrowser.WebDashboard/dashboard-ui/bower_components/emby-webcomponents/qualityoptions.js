define(["globalize"], function(globalize) {
    "use strict";

    function getVideoQualityOptions(options) {
        var maxStreamingBitrate = options.currentMaxBitrate,
            videoWidth = options.videoWidth,
            maxAllowedWidth = videoWidth || 4096,
            qualityOptions = [];
        maxAllowedWidth >= 3800 && (qualityOptions.push({
            name: "4K - 120 Mbps",
            maxHeight: 2160,
            bitrate: 12e7
        }), qualityOptions.push({
            name: "4K - 100 Mbps",
            maxHeight: 2160,
            bitrate: 1e8
        }), qualityOptions.push({
            name: "4K - 80 Mbps",
            maxHeight: 2160,
            bitrate: 8e7
        })), maxAllowedWidth >= 1900 ? (qualityOptions.push({
            name: "1080p - 60 Mbps",
            maxHeight: 1080,
            bitrate: 6e7
        }), qualityOptions.push({
            name: "1080p - 50 Mbps",
            maxHeight: 1080,
            bitrate: 5e7
        }), qualityOptions.push({
            name: "1080p - 40 Mbps",
            maxHeight: 1080,
            bitrate: 4e7
        }), qualityOptions.push({
            name: "1080p - 30 Mbps",
            maxHeight: 1080,
            bitrate: 3e7
        }), qualityOptions.push({
            name: "1080p - 25 Mbps",
            maxHeight: 1080,
            bitrate: 25e6
        }), qualityOptions.push({
            name: "1080p - 20 Mbps",
            maxHeight: 1080,
            bitrate: 2e7
        }), qualityOptions.push({
            name: "1080p - 15 Mbps",
            maxHeight: 1080,
            bitrate: 15e6
        }), qualityOptions.push({
            name: "1080p - 10 Mbps",
            maxHeight: 1080,
            bitrate: 10000001
        }), qualityOptions.push({
            name: "1080p - 8 Mbps",
            maxHeight: 1080,
            bitrate: 8000001
        }), qualityOptions.push({
            name: "1080p - 6 Mbps",
            maxHeight: 1080,
            bitrate: 6000001
        }), qualityOptions.push({
            name: "1080p - 5 Mbps",
            maxHeight: 1080,
            bitrate: 5000001
        }), qualityOptions.push({
            name: "1080p - 4 Mbps",
            maxHeight: 1080,
            bitrate: 4000002
        })) : maxAllowedWidth >= 1260 ? (qualityOptions.push({
            name: "720p - 10 Mbps",
            maxHeight: 720,
            bitrate: 1e7
        }), qualityOptions.push({
            name: "720p - 8 Mbps",
            maxHeight: 720,
            bitrate: 8e6
        }), qualityOptions.push({
            name: "720p - 6 Mbps",
            maxHeight: 720,
            bitrate: 6e6
        }), qualityOptions.push({
            name: "720p - 5 Mbps",
            maxHeight: 720,
            bitrate: 5e6
        })) : maxAllowedWidth >= 620 && (qualityOptions.push({
            name: "480p - 4 Mbps",
            maxHeight: 480,
            bitrate: 4000001
        }), qualityOptions.push({
            name: "480p - 3 Mbps",
            maxHeight: 480,
            bitrate: 3000001
        }), qualityOptions.push({
            name: "480p - 2.5 Mbps",
            maxHeight: 480,
            bitrate: 25e5
        }), qualityOptions.push({
            name: "480p - 2 Mbps",
            maxHeight: 480,
            bitrate: 2000001
        }), qualityOptions.push({
            name: "480p - 1.5 Mbps",
            maxHeight: 480,
            bitrate: 1500001
        })), maxAllowedWidth >= 1260 && (qualityOptions.push({
            name: "720p - 4 Mbps",
            maxHeight: 720,
            bitrate: 4e6
        }), qualityOptions.push({
            name: "720p - 3 Mbps",
            maxHeight: 720,
            bitrate: 3e6
        }), qualityOptions.push({
            name: "720p - 2 Mbps",
            maxHeight: 720,
            bitrate: 2e6
        }), qualityOptions.push({
            name: "720p - 1.5 Mbps",
            maxHeight: 720,
            bitrate: 15e5
        }), qualityOptions.push({
            name: "720p - 1 Mbps",
            maxHeight: 720,
            bitrate: 1000001
        })), qualityOptions.push({
            name: "480p - 1 Mbps",
            maxHeight: 480,
            bitrate: 1e6
        }), qualityOptions.push({
            name: "480p - 720 kbps",
            maxHeight: 480,
            bitrate: 72e4
        }), qualityOptions.push({
            name: "480p - 420 kbps",
            maxHeight: 480,
            bitrate: 42e4
        }), qualityOptions.push({
            name: "360p",
            maxHeight: 360,
            bitrate: 4e5
        }), qualityOptions.push({
            name: "240p",
            maxHeight: 240,
            bitrate: 32e4
        }), qualityOptions.push({
            name: "144p",
            maxHeight: 144,
            bitrate: 192e3
        });
        var autoQualityOption = {
            name: globalize.translate("sharedcomponents#Auto"),
            bitrate: 0,
            selected: options.isAutomaticBitrateEnabled
        };
        if (options.enableAuto && qualityOptions.push(autoQualityOption), maxStreamingBitrate) {
            for (var selectedIndex = -1, i = 0, length = qualityOptions.length; i < length; i++) {
                var option = qualityOptions[i]; - 1 === selectedIndex && option.bitrate <= maxStreamingBitrate && (selectedIndex = i)
            } - 1 === selectedIndex && (selectedIndex = qualityOptions.length - 1);
            var currentQualityOption = qualityOptions[selectedIndex];
            options.isAutomaticBitrateEnabled ? autoQualityOption.autoText = currentQualityOption.name : currentQualityOption.selected = !0
        }
        return qualityOptions
    }

    function getAudioQualityOptions(options) {
        var maxStreamingBitrate = options.currentMaxBitrate,
            qualityOptions = [];
        qualityOptions.push({
            name: "2 Mbps",
            bitrate: 2e6
        }), qualityOptions.push({
            name: "1.5 Mbps",
            bitrate: 15e5
        }), qualityOptions.push({
            name: "1 Mbps",
            bitrate: 1e6
        }), qualityOptions.push({
            name: "320 kbps",
            bitrate: 32e4
        }), qualityOptions.push({
            name: "256 kbps",
            bitrate: 256e3
        }), qualityOptions.push({
            name: "192 kbps",
            bitrate: 192e3
        }), qualityOptions.push({
            name: "128 kbps",
            bitrate: 128e3
        }), qualityOptions.push({
            name: "96 kbps",
            bitrate: 96e3
        }), qualityOptions.push({
            name: "64 kbps",
            bitrate: 64e3
        });
        var autoQualityOption = {
            name: globalize.translate("sharedcomponents#Auto"),
            bitrate: 0,
            selected: options.isAutomaticBitrateEnabled
        };
        if (options.enableAuto && qualityOptions.push(autoQualityOption), maxStreamingBitrate) {
            for (var selectedIndex = -1, i = 0, length = qualityOptions.length; i < length; i++) {
                var option = qualityOptions[i]; - 1 === selectedIndex && option.bitrate <= maxStreamingBitrate && (selectedIndex = i)
            } - 1 === selectedIndex && (selectedIndex = qualityOptions.length - 1);
            var currentQualityOption = qualityOptions[selectedIndex];
            options.isAutomaticBitrateEnabled ? autoQualityOption.autoText = currentQualityOption.name : currentQualityOption.selected = !0
        }
        return qualityOptions
    }
    return {
        getVideoQualityOptions: getVideoQualityOptions,
        getAudioQualityOptions: getAudioQualityOptions
    }
});