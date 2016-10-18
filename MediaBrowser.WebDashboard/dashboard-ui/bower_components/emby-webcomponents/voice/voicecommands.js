// <date>09.10.2015</date>
// <summary>voicecommands class</summary>
define(['require'], function (require) {
    'use strict';

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
            case 'play':
                return processCommand('./commands/playcommands.js', result);
            case 'shuffle':
                return processCommand('./commands/playcommands.js', result);
            case 'search':
                return processCommand('./commands/searchcommands.js', result);
            case 'control':
                return processCommand('./commands/controlcommands.js', result);
            case 'enable':
                return processCommand('./commands/enablecommands.js', result);
            case 'disable':
                return processCommand('./commands/disablecommands.js', result);
            case 'toggle':
                return processCommand('./commands/togglecommands.js', result);
            default:
                return Promise.reject();
        }
    };
});