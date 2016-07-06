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

                var fn = command(result);

                if (fn) {
                    result.fn = fn;
                    resolve(result);
                } else {
                    reject();
                }
            });
        });

    }

    return function (result) {

        switch (result.item.actionid) {

            case 'show':
                return processCommand('./commands/showcommands.js', result);
                break;
            case 'play':
                return processCommand('./commands/playcommands.js', result);
                break;
            case 'shuffle':
                return processCommand('./commands/playcommands.js', result);
                break;
            case 'search':
                return processCommand('./commands/searchcommands.js', result);
                break;
            case 'control':
                return processCommand('./commands/controlcommands.js', result);
                break;
            case 'enable':
                return processCommand('./commands/enablecommands.js', result);
                break;
            case 'disable':
                return processCommand('./commands/disablecommands.js', result);
                break;
            case 'toggle':
                return processCommand('./commands/togglecommands.js', result);
                break;
            default:
                return Promise.reject();
        }
    }
});