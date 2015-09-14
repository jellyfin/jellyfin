(function () {

    var currentRecognition;

    function shuffleArray(array) {
        var currentIndex = array.length, temporaryValue, randomIndex;

        // While there remain elements to shuffle...
        while (0 !== currentIndex) {

            // Pick a remaining element...
            randomIndex = Math.floor(Math.random() * currentIndex);
            currentIndex -= 1;

            // And swap it with the current element.
            temporaryValue = array[currentIndex];
            array[currentIndex] = array[randomIndex];
            array[randomIndex] = temporaryValue;
        }

        return array;
    }

    function getSampleCommands() {

        var deferred = DeferredBuilder.Deferred();

        var commands = [];

        //commands.push('show my movies');
        //commands.push('pull up my tv shows');

        commands.push('play my latest episodes');
        commands.push('play next up');
        commands.push('shuffle my favorite songs');

        commands.push('show my tv guide');
        commands.push('pull up my recordings');
        commands.push('control chromecast');
        commands.push('control [device name]');
        commands.push('turn on display mirroring');
        commands.push('turn off display mirroring');
        commands.push('toggle display mirroring');

        deferred.resolveWith(null, [shuffleArray(commands)]);

        return deferred.promise();
    }

    function processText(text) {

        var deferred = DeferredBuilder.Deferred();

        require(['voice/textprocessor-en-us.js'], function (parseText) {

            var result = parseText(text);

            switch (result.action) {

                case 'show':
                    showCommand(result);
                    break;
                case 'play':
                    playCommand(result);
                    break;
                case 'shuffle':
                    playCommand(result, true);
                    break;
                case 'search':
                    playCommand(result);
                    break;
                case 'control':
                    controlCommand(result);
                    break;
                case 'enable':
                    enableCommand(result);
                    break;
                case 'disable':
                    disableCommand(result);
                    break;
                case 'toggle':
                    toggleCommand(result);
                    break;
                default:
                    deferred.reject();
                    return;
            }

            deferred.resolve();
        });

        return deferred.promise();
    }

    function showCommand(result) {

        if (result.category == 'tvguide') {
            Dashboard.navigate('livetv.html?tab=1');
            return;
        }

        if (result.category == 'recordings') {
            Dashboard.navigate('livetv.html?tab=3');
            return;
        }
    }

    function enableCommand(result) {

        var what = result.what.toLowerCase();

        if (what.indexOf('mirror') != -1) {
            MediaController.enableDisplayMirroring(true);
        }
    }

    function disableCommand(result) {

        var what = result.what.toLowerCase();

        if (what.indexOf('mirror') != -1) {
            MediaController.enableDisplayMirroring(false);
        }
    }

    function toggleCommand(result) {

        var what = result.what.toLowerCase();

        if (what.indexOf('mirror') != -1) {
            MediaController.toggleDisplayMirroring();
        }
    }

    function controlCommand(result) {

        MediaController.trySetActiveDeviceName(result.what);
    }

    function playCommand(result, shuffle) {

        var query = {

            Limit: result.limit || 100,
            UserId: result.userId,
            ExcludeLocationTypes: "Virtual"
        };

        if (result.category == 'nextup') {

            ApiClient.getNextUpEpisodes(query).done(function (queryResult) {

                playItems(queryResult.Items, shuffle);

            });
            return;
        }

        if (shuffle) {
            result.sortby = result.sortby ? 'Random,' + result.sortby : 'Random';
        }

        query.SortBy = result.sortby;
        query.SortOrder = result.sortorder;
        query.Recursive = true;

        if (result.filters.indexOf('unplayed') != -1) {
            query.IsPlayed = false;
        }
        if (result.filters.indexOf('played') != -1) {
            query.IsPlayed = true;
        }
        if (result.filters.indexOf('favorite') != -1) {
            query.Filters = 'IsFavorite';
        }

        if (result.itemType) {
            query.IncludeItemTypes = result.itemType;
        }

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).done(function (queryResult) {

            playItems(queryResult.Items, shuffle);
        });
    }

    function playItems(items, shuffle) {

        if (shuffle) {
            items = shuffleArray(items);
        }

        items = items.map(function (i) {
            return i.Id;
        });

        if (items.length) {
            MediaController.play({
                ids: items
            });
        } else {
            Dashboard.alert({
                message: Globalize.translate('MessageNoItemsFound')
            });
        }
    }

    function searchCommand(result) {


    }

    function renderSampleCommands(elem, commands) {

        commands.length = Math.min(commands.length, 4);

        commands = commands.map(function (c) {

            return '<div class="exampleCommand"><i class="fa fa-quote-left"></i><span class="exampleCommandText">' + c + '</span><i class="fa fa-quote-right"></i></div>';

        }).join('');

        $('.exampleCommands', elem).html(commands);
    }

    function showVoiceHelp() {

        var elem = $('.voiceInputHelp');

        if (elem.length) {
            $('.unrecognizedCommand').hide();
            $('.defaultVoiceHelp').show();
            return;
        }

        require(['fontawesome']);

        var html = '';

        var getCommandsPromise = getSampleCommands();

        html += '<div class="voiceInputHelp">';
        html += '<div class="voiceInputHelpInner">';

        html += '<div class="voiceHelpContent">';

        html += '<div class="defaultVoiceHelp">';

        html += '<h1>' + Globalize.translate('HeaderSaySomethingLike') + '</h1>';

        html += '<div class="exampleCommands">';
        html += '</div>';

        // defaultVoiceHelp
        html += '</div>';

        html += '<div class="unrecognizedCommand" style="display:none;">';
        html += '<h1>' + Globalize.translate('HeaderYouSaid') + '</h1>';
        html += '<p class="exampleCommand voiceInputContainer"><i class="fa fa-quote-left"></i><span class="voiceInputText exampleCommandText"></span><i class="fa fa-quote-right"></i></p>';
        html += '<p>' + Globalize.translate('MessageWeDidntRecognizeCommand') + '</p>';

        html += '<br/>';
        html += '<paper-button raised class="submit block btnRetry"><iron-icon icon="mic"></iron-icon><span>' + Globalize.translate('ButtonTryAgain') + '</span></paper-button>';
        html += '<p class="blockedMessage" style="display:none;">' + Globalize.translate('MessageIfYouBlockedVoice') + '<br/><br/></p>';

        html += '</div>';

        html += '<paper-button raised class="block btnCancel" style="background-color:#444;"><iron-icon icon="close"></iron-icon><span>' + Globalize.translate('ButtonCancel') + '</span></paper-button>';

        // voiceHelpContent
        html += '</div>';

        // voiceInputHelpInner
        html += '</div>';

        // voiceInputHelp
        html += '</div>';

        $(document.body).append(html);

        elem = $('.voiceInputHelp');

        getCommandsPromise.done(function (commands) {
            renderSampleCommands(elem, commands);
        });

        $('.btnCancel', elem).on('click', cancelListener);
        $('.btnRetry', elem).on('click', startListening);
    }

    function showUnrecognizedCommandHelp() {

        $('.unrecognizedCommand').show();
        $('.defaultVoiceHelp').hide();
    }

    function hideVoiceHelp() {

        $('.voiceInputHelp').remove();
    }

    function cancelListener() {

        destroyCurrentRecognition();
        hideVoiceHelp();
    }

    function destroyCurrentRecognition() {

        var recognition = currentRecognition;
        if (recognition) {
            recognition.abort();
            currentRecognition = null;
        }
    }

    function processTranscript(text) {

        $('.voiceInputText').html(text);

        if (text || AppInfo.isNativeApp) {
            $('.blockedMessage').hide();
        } else {
            $('.blockedMessage').show();
        }

        processText(text).done(hideVoiceHelp).fail(showUnrecognizedCommandHelp);
    }

    function startListening() {

        destroyCurrentRecognition();

        Dashboard.importCss('voice/voice.css');
        startListeningInternal();
    }

    function startListeningInternal() {

        var recognition = new (window.SpeechRecognition || window.webkitSpeechRecognition)();

        //recognition.continuous = true;
        //recognition.interimResults = true;

        recognition.onresult = function (event) {
            if (event.results.length > 0) {
                processTranscript(event.results[0][0].transcript || '');
            }
        };

        recognition.onerror = function () {
            processTranscript('');
        };

        recognition.onnomatch = function () {
            processTranscript('');
        };

        recognition.start();
        currentRecognition = recognition;
        showVoiceHelp();
    }

    window.VoiceInputManager = {

        isSupported: function () {

            return window.SpeechRecognition || window.webkitSpeechRecognition;
        },

        startListening: startListening
    };

})();