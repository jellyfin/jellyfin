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

        return new Promise(function (resolve, reject) {

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

            resolve(shuffleArray(commands));
        });
    }

    function processText(text) {

        return new Promise(function (resolve, reject) {

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
                        reject();
                        return;
                }

                var dlg = currentDialog;
                if (dlg) {
                    PaperDialogHelper.close(dlg);
                }

                resolve();
            });
        });
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

            ApiClient.getNextUpEpisodes(query).then(function (queryResult) {

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

        ApiClient.getItems(Dashboard.getCurrentUserId(), query).then(function (queryResult) {

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

            return '<div class="exampleCommand"><span class="exampleCommandText">"' + c + '"</span></div>';

        }).join('');

        $('.exampleCommands', elem).html(commands);
    }

    var currentDialog;
    function showVoiceHelp(paperDialogHelper) {

        var dlg = paperDialogHelper.createDialog({
            size: 'medium',
            removeOnClose: true
        });

        var html = '';
        html += '<h2 class="dialogHeader">';
        html += '<paper-fab icon="arrow-back" mini class="btnCancelVoiceInput"></paper-fab>';
        html += '</h2>';

        html += '<div>';

        var getCommandsPromise = getSampleCommands();

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

        html += '<paper-button raised class="block btnCancelVoiceInput" style="background-color:#444;"><iron-icon icon="close"></iron-icon><span>' + Globalize.translate('ButtonCancel') + '</span></paper-button>';

        // voiceHelpContent
        html += '</div>';

        html += '</div>';

        dlg.innerHTML = html;
        document.body.appendChild(dlg);

        paperDialogHelper.open(dlg);
        currentDialog = dlg;

        dlg.addEventListener('iron-overlay-closed', function () {
            currentDialog = null;
        });

        $('.btnCancelVoiceInput', dlg).on('click', function () {
            destroyCurrentRecognition();
            paperDialogHelper.close(dlg);
        });

        $('.btnRetry', dlg).on('click', function () {
            $('.unrecognizedCommand').hide();
            $('.defaultVoiceHelp').show();
            startListening(false);
        });

        getCommandsPromise.then(function (commands) {
            renderSampleCommands(dlg.querySelector('.voiceHelpContent'), commands);
        });
    }

    function showUnrecognizedCommandHelp() {

        $('.unrecognizedCommand').show();
        $('.defaultVoiceHelp').hide();
    }

    function destroyCurrentRecognition() {

        var recognition = currentRecognition;
        if (recognition) {
            recognition.cancelled = true;
            recognition.abort();
            currentRecognition = null;
        }
    }

    function processTranscript(text, isCancelled) {

        $('.voiceInputText').html(text);

        if (text || AppInfo.isNativeApp) {
            $('.blockedMessage').hide();
        } else {
            $('.blockedMessage').show();
        }

        if (text) {
            processText(text).catch(showUnrecognizedCommandHelp);
        } else if (!isCancelled) {
            showUnrecognizedCommandHelp();
        }
    }

    function startListening(createUI) {

        destroyCurrentRecognition();

        var recognition = new (window.SpeechRecognition || window.webkitSpeechRecognition)();

        //recognition.continuous = true;
        //recognition.interimResults = true;

        recognition.onresult = function (event) {
            if (event.results.length > 0) {
                processTranscript(event.results[0][0].transcript || '');
            }
        };

        recognition.onerror = function () {
            processTranscript('', recognition.cancelled);
        };

        recognition.onnomatch = function () {
            processTranscript('', recognition.cancelled);
        };

        recognition.start();
        currentRecognition = recognition;

        if (createUI !== false) {
            require(['components/paperdialoghelper', 'paper-fab', 'css!voice/voice.css'], showVoiceHelp);
        }
    }

    window.VoiceInputManager = {

        isSupported: function () {

            if (AppInfo.isNativeApp) {
                // Crashes on some amazon devices
                if (window.device && (device.platform || '').toLowerCase().indexOf('amazon') != -1) {
                    return false;
                }
            }

            return window.SpeechRecognition || window.webkitSpeechRecognition;
        },

        startListening: startListening
    };

})();