define(['events'], function (events) {
    'use strict';

    var receiver = {

    };

    var currentRecognition = null;

    function normalizeInput(text, options) {
        
        if (options.requireNamedIdentifier) {

            var srch = 'jarvis';
            var index = text.toLowerCase().indexOf(srch);

            if (index !== -1) {
                text = text.substring(index + srch.length);
            } else {
                return null;
            }
        }

        return text;
    }

    /// <summary> Starts listening for voice commands </summary>
    /// <returns> . </returns>
    function listen(options) {

        return new Promise(function (resolve, reject) {
            cancelListener();

            var recognitionObj = window.SpeechRecognition ||
                window.webkitSpeechRecognition ||
                window.mozSpeechRecognition ||
                window.oSpeechRecognition ||
                window.msSpeechRecognition;

            var recognition = new recognitionObj();

            recognition.lang = options.lang;
            recognition.continuous = options.continuous || false;

            var resultCount = 0;

            recognition.onresult = function (event) {
                console.log(event);
                if (event.results.length > 0) {

                    var resultInput = event.results[resultCount][0].transcript || '';
                    resultCount++;

                    resultInput = normalizeInput(resultInput, options);

                    if (resultInput) {
                        if (options.continuous) {
                            events.trigger(receiver, 'input', [
                                {
                                    text: resultInput
                                }
                            ]);
                        } else {
                            resolve(resultInput);
                        }
                    }
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

    receiver.listen = listen;
    receiver.cancel = cancelListener;

    /// <summary> An enum constant representing the window. voice input manager option. </summary>
    return receiver;
});