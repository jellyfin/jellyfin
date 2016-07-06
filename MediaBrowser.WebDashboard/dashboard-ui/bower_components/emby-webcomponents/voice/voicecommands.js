// <date>09.10.2015</date>
// <summary>voicecommands class</summary>
define(['require'], function (require) {

    /// <summary> Process the command. </summary>
    /// <param name="commandPath"> Full pathname of the command file. </param>
    /// <param name="result"> The result. </param>
    /// <returns> . </returns>
    function processCommand(commandPath, result) {

        return new Promise(function (resolve, reject) {

            require([commandPath], function (command) {
                command(result);
                if (result.success) {
                    resolve(result);
                }
                reject();
            });
        });

    }

    return function (result) {

        return new Promise(function (resolve, reject) {

            switch (result.item.actionid) {

                case 'show':
                    processCommand('./commands/showcommands.js', result).then(function (result) { resolve(result); });
                    break;
                case 'play':
                    processCommand('./commands/playcommands.js', result).then(function (result) { resolve(result); });
                    break;
                case 'shuffle':
                    processCommand('./commands/playcommands.js', result).then(function (result) { resolve(result); });
                    break;
                case 'search':
                    processCommand('./commands/searchcommands.js', result).then(function (result) { resolve(result); });
                    break;
                case 'control':
                    processCommand('./commands/controlcommands.js', result).then(function (result) { resolve(result); });
                    break;
                case 'enable':
                    processCommand('./commands/enablecommands.js', result).then(function (result) { resolve(result); });
                    break;
                case 'disable':
                    processCommand('./commands/disablecommands.js', result).then(function (result) { resolve(result); });
                    break;
                case 'toggle':
                    processCommand('./commands/togglecommands.js', result).then(function (result) { resolve(result); });
                    break;
                default:
                    reject();
                    return;
            }
        });
    }
});