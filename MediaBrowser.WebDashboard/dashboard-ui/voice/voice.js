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

        processTextInternal(text, deferred);

        return deferred.promise();
    }

    function parseContext(text, result) {

        text = text.toLowerCase();

        var i, length;

        for (i = 0, length = result.removeWords.length; i < length; i++) {

            text = text.replace(result.removeWords[i], '');
        }

        text = text.trim();

        var removeAtStart = [
            'my'
        ];

        for (i = 0, length = removeAtStart.length; i < length; i++) {

            if (text.indexOf(removeAtStart[i]) == 0) {
                text = text.substring(removeAtStart[i].length);
            }
        }

        result.what = text;

        text = text.trim();
        var words = text.toLowerCase().split(' ');

        if (words.indexOf('favorite') != -1) {
            result.filters.push('favorite');
        }

        if (text.indexOf('latest movies') != -1 || text.indexOf('latest films') != -1) {

            result.sortby = 'datecreated';
            result.sortorder = 'Descending';
            result.filters.push('unplayed');
            result.itemType = 'Movie';

            return;
        }

        if (text.indexOf('latest episodes') != -1) {

            result.sortby = 'datecreated';
            result.sortorder = 'Descending';
            result.filters.push('unplayed');
            result.itemType = 'Episode';

            return;
        }

        if (text.indexOf('next up') != -1) {

            result.category = 'nextup';

            return;
        }

        if (text.indexOf('movies') != -1 || text.indexOf('films') != -1) {

            result.itemType = 'Movie';

            return;
        }

        if (text.indexOf('shows') != -1 || text.indexOf('series') != -1) {

            result.itemType = 'Series';

            return;
        }

        if (text.indexOf('songs') != -1) {

            result.itemType = 'Audio';

            return;
        }
    }

    function parseText(text) {

        var result = {
            action: '',
            itemName: '',
            itemType: '',
            category: '',
            filters: [],
            removeWords: [],
            sortby: '',
            sortorder: 'Ascending',
            limit: null,
            userId: Dashboard.getCurrentUserId()
        };

        var textLower = text.toLowerCase();
        var words = text.toLowerCase().split(' ');

        var displayWords = [
            'show',
            'pull up',
            'display',
            'go to',
            'view'
        ];

        if (displayWords.filter(function (w) { return textLower.indexOf(w) == 0; }).length) {

            if (words.indexOf('guide') != -1) {
                result.action = 'show';
                result.category = 'tvguide';
            }

            if (words.indexOf('recordings') != -1) {
                result.action = 'show';
                result.category = 'recordings';
            }

            result.removeWords = displayWords;
            return result;
        }

        var searchWords = [
         'search',
         'search for',
         'find',
         'query'
        ];

        if (searchWords.filter(function (w) { return textLower.indexOf(w) == 0; }).length) {

            // Search
            result.action = 'search';

            result.removeWords = searchWords;
            return result;
        }

        var playWords = [
         'play',
         'watch'
        ];

        if (playWords.filter(function (w) { return textLower.indexOf(w) == 0; }).length) {

            // Play
            result.action = 'play';

            result.removeWords = playWords;
            return result;
        }

        var controlWords = [
         'use',
         'control'
        ];

        if (controlWords.filter(function (w) { return textLower.indexOf(w) == 0; }).length) {

            // Play
            result.action = 'control';

            result.removeWords = controlWords;
            return result;
        }

        var enableWords = [
         'enable',
         'turn on'
        ];

        if (enableWords.filter(function (w) { return textLower.indexOf(w) == 0; }).length) {

            // Play
            result.action = 'enable';

            result.removeWords = enableWords;
            return result;
        }

        var disableWords = [
         'disable',
         'turn off'
        ];

        if (disableWords.filter(function (w) { return textLower.indexOf(w) == 0; }).length) {

            // Play
            result.action = 'disable';

            result.removeWords = disableWords;
            return result;
        }

        var toggleWords = [
         'toggle'
        ];

        if (toggleWords.filter(function (w) { return textLower.indexOf(w) == 0; }).length) {

            // Play
            result.action = 'toggle';

            result.removeWords = toggleWords;
            return result;
        }

        if (words.indexOf('shuffle') != -1) {

            // Play
            result.action = 'shuffle';

            result.removeWords.push('shuffle');
            return result;
        }

        if (words.indexOf('record') != -1) {

            // Record
            result.action = 'record';

            result.removeWords.push('record');
            return result;
        }

        if (words.indexOf('guide') != -1) {
            result.action = 'show';
            result.category = 'tvguide';
            return result;
        }

        return result;
    }

    function processTextInternal(text, deferred) {

        var result = parseText(text);

        switch (result.action) {

            case 'show':
                parseContext(text, result);
                showCommand(result);
                break;
            case 'play':
                parseContext(text, result);
                playCommand(result);
                break;
            case 'shuffle':
                parseContext(text, result);
                playCommand(result, true);
                break;
            case 'search':
                parseContext(text, result);
                playCommand(result);
                break;
            case 'control':
                parseContext(text, result);
                controlCommand(result);
                break;
            case 'enable':
                parseContext(text, result);
                enableCommand(result);
                break;
            case 'disable':
                parseContext(text, result);
                disableCommand(result);
                break;
            case 'toggle':
                parseContext(text, result);
                toggleCommand(result);
                break;
            default:
                deferred.reject();
                return;
        }

        deferred.resolve();
    }

    function showCommand(result) {

        if (result.category == 'tvguide') {
            Dashboard.navigate('livetvguide.html');
            return;
        }

        if (result.category == 'recordings') {
            Dashboard.navigate('livetvrecordings.html');
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