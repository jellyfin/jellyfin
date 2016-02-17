define(['paperdialoghelper'], function (paperDialogHelper) {

    var currentRecognition;
    var lang = 'grammar';
    //var lang = 'en-US';

    var commandgroups = getGrammarCommands(lang);


    
    /// <summary> Shuffle array. </summary>
    /// <param name="array"> The array. </param>
    /// <returns> array </returns>
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

    /// <summary> Gets sample commands. </summary>
    /// <returns> The sample commands. </returns>
    function getSampleCommands(groupid) {

        return new Promise(function (resolve, reject) {

            groupid = typeof (groupid) !== 'undefined' ? groupid : '';

            var commands = [];
            commandgroups.map(function (group) {
                if ((group.items && group.items.length > 0) && (groupid == group.groupid || groupid == '')) {

                    group.items.map(function (item) {

                        if (item.commandtemplates && item.commandtemplates.length > 0) {

                            item.commandtemplates.map(function (templates) {
                                commands.push(templates);
                            });
                        }

                    });
                }
            });

            resolve(shuffleArray(commands));

        });

    }

    /// <summary> Gets command group. </summary>
    /// <param name="groupid"> The groupid. </param>
    /// <returns> The command group. </returns>
    function getCommandGroup(groupid) {
        if (commandgroups) {
            var idx = -1;
            idx = commandgroups.map(function (e) { return e.groupid; }).indexOf(groupid);

            if (idx > -1)
                return commandgroups[idx];
            else
                return null;
        }
        else
            return null;
    }

    /// <summary> Renders the sample commands. </summary>
    /// <param name="elem"> The element. </param>
    /// <param name="commands"> The commands. </param>
    /// <returns> . </returns>
    function renderSampleCommands(elem, commands) {

        commands.length = Math.min(commands.length, 6);

        commands = commands.map(function (c) {

            return '<div class="exampleCommand"><span class="exampleCommandText">"' + c + '"</span></div>';

        }).join('');

        $('.exampleCommands', elem).html(commands);
    }

    var currentDialog;
    /// <summary> Shows the voice help. </summary>
    /// <returns> . </returns>
    function showVoiceHelp(groupid, title) {

        var dlg = paperDialogHelper.createDialog({
            size: 'medium',
            removeOnClose: true
        });

        dlg.classList.add('ui-body-b');
        dlg.classList.add('background-theme-b');

        var html = '';
        html += '<h2 class="dialogHeader">';
        html += '<paper-fab icon="arrow-back" mini class="btnCancelVoiceInput"></paper-fab>';
        if (groupid) {
            var grp = getCommandGroup(groupid);
            if (grp)
                html += '  ' + grp.name;
        }
        html += '</h2>';

        html += '<div>';

        var getCommandsPromise = getSampleCommands(groupid);

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

    /// <summary> Hides the voice help. </summary>
    /// <returns> . </returns>
    function hideVoiceHelp() {

        $('.voiceInputHelp').remove();
    }

    /// <summary> Shows the unrecognized command help. </summary>
    /// <returns> . </returns>
    function showUnrecognizedCommandHelp() {
        //speak("I don't understend this command");
        $('.unrecognizedCommand').show();
        $('.defaultVoiceHelp').hide();
    }

    /// <summary> Process the transcript described by text. </summary>
    /// <param name="text"> The text. </param>
    /// <returns> . </returns>
    function processTranscript(text, isCancelled) {

        $('.voiceInputText').html(text);

        if (text || AppInfo.isNativeApp) {
            $('.blockedMessage').hide();
        }
        else {
            $('.blockedMessage').show();
        }

        if (text) {
            require(['voice/voicecommands.js', 'voice/grammarprocessor.js'], function (voicecommands, grammarprocessor) {

                var processor = grammarprocessor(commandgroups, text);
                if (processor && processor.command) {
                    voicecommands(processor)
                        .then(function (result) {
                            if (result.item.actionid === 'show' && result.item.sourceid === 'group') {
                                var dlg = currentDialog;
                                if (dlg)
                                    showCommands(false, result)
                                else
                                    showCommands(true, result)
                            }
                        })
                        .catch(showUnrecognizedCommandHelp);
                }
                else
                    showUnrecognizedCommandHelp();

                var dlg = currentDialog;
                if (dlg) {
                    PaperDialogHelper.close(dlg);
                }
            });

        }
        else if (!isCancelled) {
            showUnrecognizedCommandHelp();
        }
    }

    /// <summary> Starts listening internal. </summary>
    /// <returns> . </returns>
    function startListening(createUI) {

        destroyCurrentRecognition();
        var recognition = new (window.SpeechRecognition || window.webkitSpeechRecognition || window.mozSpeechRecognition || window.oSpeechRecognition || window.msSpeechRecognition)();
        recognition.lang = lang;
        var groupid = '';
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
        showCommands(createUI);
    }

    /// <summary> Destroys the current recognition. </summary>
    /// <returns> . </returns>
    function destroyCurrentRecognition() {

        var recognition = currentRecognition;
        if (recognition) {
            recognition.abort();
            currentRecognition = null;
        }
    }

    /// <summary> Cancel listener. </summary>
    /// <returns> . </returns>
    function cancelListener() {

        destroyCurrentRecognition();
        hideVoiceHelp();
    }

    /// <summary> Shows the commands. </summary>
    /// <param name="createUI"> The create user interface. </param>
    /// <returns> . </returns>
    function showCommands(createUI, result) {
        if (createUI !== false) {
            //speak('Hello, what can I do for you?');
            require(['paper-fab', 'css!voice/voice.css'], function () {
                if (result)
                    showVoiceHelp(result.groupid, result.name);
                else
                    showVoiceHelp();
            });
        }
    }

    /// <summary> Getgrammars the given language. </summary>
    /// <param name="language"> The language. </param>
    /// <returns> . </returns>
    function getGrammarCommands(language) {

        var file = "grammar";
        if (language && language.length > 0)
            file = language;

        var grammar = (function () {
            var grm = null;
            $.ajax({
                async: false,
                global: false,
                url: "voice/grammar/" + file + ".json",
                dataType: "json",
                success: function (data) {
                    grm = data;
                }
            });
            return grm;
        })();
        return grammar;
    }

    /// <summary> Speaks the given text. </summary>
    /// <param name="text"> The text. </param>
    /// <returns> . </returns>
    function speak(text) {

        if (!SpeechSynthesisUtterance) {
            console.log('API not supported');
        }

        var utterance = new SpeechSynthesisUtterance(text);
        utterance.lang = lang;
        utterance.rate = 0.9;
        utterance.pitch = 1;
        utterance.addEventListener('end', function () {
            console.log('Synthesizing completed');
        });

        utterance.addEventListener('error', function (event) {
            console.log('Synthesizing error');
        });

        console.log('Synthesizing the text: ' + text);
        speechSynthesis.speak(utterance);
    }


    /// <summary> An enum constant representing the window. voice input manager option. </summary>
    window.VoiceInputManager = {

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

        startListening: startListening,
    };

});