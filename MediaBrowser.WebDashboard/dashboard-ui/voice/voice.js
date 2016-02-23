define([], function () {

    return {

        isSupported: function () {

            if (AppInfo.isNativeApp) {
                // Crashes on some amazon devices
                if (window.device && (device.platform || '').toLowerCase().indexOf('amazon') != -1) {
                    return false;
                }
            }

            return window.SpeechRecognition ||
                   window.webkitSpeechRecognition ||
                   window.mozSpeechRecognition ||
                   window.oSpeechRecognition ||
                   window.msSpeechRecognition;
        },

        startListening: function () {
            require(['voice/voicedialog'], function (voicedialog) {
                voicedialog.startListening();
            });
        }
    };

});