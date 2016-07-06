define(['voice/voicecommands.js', 'voice/grammarprocessor.js'], function (voicecommands, grammarprocessor) {

    var commandgroups;

    function getCommandGroups() {

        if (commandgroups) {
            return Promise.resolve(commandgroups);
        }

        return new Promise(function (resolve, reject) {

            var file = "grammar";
            //if (language && language.length > 0)
            //    file = language;

            var xhr = new XMLHttpRequest();
            xhr.open('GET', "voice/grammar/" + file + ".json", true);

            xhr.onload = function (e) {

                commandgroups = JSON.parse(this.response);
                resolve(commandgroups);
            }

            xhr.onerror = reject;

            xhr.send();
        });
    }
    /// <summary> Process the transcript described by text. </summary>
    /// <param name="text"> The text. </param>
    /// <returns> . </returns>
    function processTranscript(text) {
        if (text) {
            var processor = grammarprocessor(commandgroups, text);
            if (processor && processor.command) {
                console.log("Command from Grammar Processor", processor);
                return voicecommands(processor)
                    .then(function (result) {
                        console.log("Result of executed command", result);
                        if (result.item.actionid === 'show' && result.item.sourceid === 'group') {
                            return Promise.reject({ error: "group", item: result.item, groupName: result.name });
                        } else {
                            return Promise.resolve({ item: result.item });
                        }
                    }, function () {
                        return Promise.reject({ error: "unrecognized-command", text: text });
                    });
            } else {
                return Promise.reject({ error: "unrecognized-command", text: text });
            }

        } else {
            return Promise.reject({ error: "empty" });
        }
    }

    /// <summary> An enum constant representing the window. voice input manager option. </summary>
    return {
        processTranscript: processTranscript,
        getCommandGroups: getCommandGroups
    };

});