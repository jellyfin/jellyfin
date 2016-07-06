define([], function () {
    var currentRecognition = null;


    /// <summary> Starts listening for voice commands </summary>
    /// <returns> . </returns>
    function listenForCommand(lang) {
        return new Promise(function (resolve, reject) {
            cancelListener();

            var recognition = new (window.SpeechRecognition ||
                window.webkitSpeechRecognition ||
                window.mozSpeechRecognition ||
                window.oSpeechRecognition ||
                window.msSpeechRecognition)();
            recognition.lang = lang;

            recognition.onresult = function (event) {
                console.log(event);
                if (event.results.length > 0) {
                    var resultInput = event.results[0][0].transcript || '';
                    resolve(resultInput);
                }
            };

            recognition.onerror = function () {
                reject({ error: event.error, message: event.message });
            };

            recognition.onnomatch = function () {
                reject({ error: "no-match" });
            };
            currentRecognition = recognition;

            currentRecognition.start();
        });
    }


    /// <summary> Cancel listener. </summary>
    /// <returns> . </returns>
    function cancelListener() {

        if (currentRecognition) {
            currentRecognition.abort();
            currentRecognition = null;
        }

    }

    /// <summary> An enum constant representing the window. voice input manager option. </summary>
    return {
        listenForCommand: listenForCommand,
        cancel: cancelListener
    };

});