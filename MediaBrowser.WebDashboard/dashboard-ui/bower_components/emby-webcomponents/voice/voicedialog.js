define(['dialogHelper', 'voiceReceiver', 'voiceProcessor', 'globalize', 'emby-button', 'css!./voice.css', 'material-icons', 'css!./../formdialog'], function (dialogHelper, voicereceiver, voiceprocessor, globalize) {

    var lang = 'en-US';

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

        return voiceprocessor.getCommandGroups().then(function (commandGroups) {
            groupid = typeof (groupid) !== 'undefined' ? groupid : '';

            var commands = [];
            commandGroups.map(function (group) {
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

            return shuffleArray(commands);
        });
    }

    /// <summary> Gets command group. </summary>
    /// <param name="groupid"> The groupid. </param>
    /// <returns> The command group. </returns>
    function getCommandGroup(groupid) {
        return voicereceiver.getCommandGroups()
            .then(function (commandgroups) {
                if (commandgroups) {
                    var idx = -1;

                    idx = commandgroups.map(function (e) { return e.groupid; }).indexOf(groupid);

                    if (idx > -1)
                        return commandgroups[idx];
                    else
                        return null;
                } else
                    return null;
            });
    }

    /// <summary> Renders the sample commands. </summary>
    /// <param name="elem"> The element. </param>
    /// <param name="commands"> The commands. </param>
    /// <returns> . </returns>
    function renderSampleCommands(elem, commands) {

        commands.length = Math.min(commands.length, 4);

        commands = commands.map(function (c) {

            return '<div class="exampleCommand"><span class="exampleCommandText">"' + c + '"</span></div>';

        }).join('');

        elem.querySelector('.exampleCommands').innerHTML = commands;
    }

    var currentDialog;
    /// <summary> Shows the voice help. </summary>
    /// <returns> . </returns>
    function showVoiceHelp(groupid, title) {

        console.log("Showing Voice Help", groupid, title);

        var isNewDialog = false;
        var dlg;

        if (!currentDialog) {

            isNewDialog = true;

            dlg = dialogHelper.createDialog({
                size: 'medium',
                removeOnClose: true
            });

            dlg.classList.add('formDialog');

            var html = '';
            html += '<div class="dialogHeader">';
            html += '<button is="paper-icon-button-light" class="btnCancelVoiceInput autoSize" tabindex="-1"><i class="md-icon">&#xE5C4;</i></button>';
            html += '<div class="dialogHeaderTitle">';
            html += globalize.translate('sharedcomponents#VoiceInput');
            html += '</div>';
            html += '</div>';

            html += '<div>';

            html += '<div class="dialogContent smoothScrollY" style="padding-top:2em;">';
            html += '<div class="dialogContentInner dialog-content-centered">';
            html += '<div class="voiceHelpContent">';

            html += '<div class="defaultVoiceHelp">';

            html += '<h1 style="margin-bottom:1.25em;margin-top:0;">' + globalize.translate('sharedcomponents#HeaderSaySomethingLike') + '</h1>';

            html += '<div class="exampleCommands">';
            html += '</div>';

            // defaultVoiceHelp
            html += '</div>';

            html += '<div class="unrecognizedCommand hide">';
            html += '<h1 style="margin-top:0;">' + globalize.translate('sharedcomponents#HeaderYouSaid') + '</h1>';
            html +=
                '<p class="exampleCommand voiceInputContainer"><i class="fa fa-quote-left"></i><span class="voiceInputText exampleCommandText"></span><i class="fa fa-quote-right"></i></p>';
            html += '<p>' + globalize.translate('sharedcomponents#MessageWeDidntRecognizeCommand') + '</p>';

            html += '<br/>';
            html += '<button is="emby-button" type="button" class="submit block btnRetry raised"><i class="md-icon">mic</i><span>' +
                globalize.translate('sharedcomponents#ButtonTryAgain') +
                '</span></button>';
            html += '<p class="blockedMessage hide">' +
                globalize.translate('sharedcomponents#MessageIfYouBlockedVoice') +
                '<br/><br/></p>';

            html += '</div>';

            html += '</div>';
            html += '</div>';
            html += '</div>';

            html += '</div>';

            dlg.innerHTML = html;
            document.body.appendChild(dlg);

            dialogHelper.open(dlg);
            currentDialog = dlg;

            dlg.addEventListener('close', function () {
                voicereceiver.cancel();
                currentDialog = null;
            });

            function onCancelClick() {
                dialogHelper.close(dlg);
            }

            var closeButtons = dlg.querySelectorAll('.btnCancelVoiceInput');
            for (var i = 0, length = closeButtons.length; i < length; i++) {
                closeButtons[i].addEventListener('click', onCancelClick);
            }

            dlg.querySelector('.btnRetry').addEventListener('click', function () {
                dlg.querySelector('.unrecognizedCommand').classList.add('hide');
                dlg.querySelector('.defaultVoiceHelp').classList.remove('hide');
                listen();
            });
        }

        dlg = currentDialog;

        if (groupid) {
            getCommandGroup(groupid)
                .then(
                    function (grp) {
                        dlg.querySelector('#voiceDialogGroupName').innerText = '  ' + grp.name;
                    });


            getSampleCommands(groupid)
                .then(function (commands) {
                    renderSampleCommands(currentDialog, commands);
                    listen();
                })
                .catch(function (e) { console.log("Error", e); });
        } else if (isNewDialog) {
            getSampleCommands()
                .then(function (commands) {
                    renderSampleCommands(currentDialog, commands);
                });

        }
    }
    function processInput(input) {
        return voiceprocessor.processTranscript(input);
    }

    /// <summary> Shows the unrecognized command help. </summary>
    /// <returns> . </returns>
    function showUnrecognizedCommandHelp(command) {
        //speak("I don't understend this command");
        if (command)
            currentDialog.querySelector('.voiceInputText').innerText = command;
        currentDialog.querySelector('.unrecognizedCommand').classList.remove('hide');
        currentDialog.querySelector('.defaultVoiceHelp').classList.add('hide');
    }

    /// <summary> Shows the commands. </summary>
    /// <param name="createUI"> The create user interface. </param>
    /// <returns> . </returns>
    function showCommands(result) {
        //speak('Hello, what can I do for you?');
        if (result)
            showVoiceHelp(result.groupid, result.name);
        else
            showVoiceHelp();
    }

    function resetDialog() {
        if (currentDialog) {
            currentDialog.querySelector('.unrecognizedCommand').classList.add('hide');
            currentDialog.querySelector('.defaultVoiceHelp').classList.remove('hide');
        }
    }
    function showDialog() {
        resetDialog();
        showCommands();
        listen();
    }
    function listen() {
        voicereceiver.listen({

            lang: lang || "en-US"

        }).then(processInput).then(function (result) {

            closeDialog();

            // Put a delay here in case navigation/popstate is involved. Allow that to flush out
            setTimeout(function () {
                result.fn();
            }, 1);

        }, function (result) {
            if (result.error == 'group') {
                showVoiceHelp(result.item.groupid, result.groupName);
                return;
            }
            showUnrecognizedCommandHelp(result.text || '');
        });
    }
    function closeDialog() {
        dialogHelper.close(currentDialog);
        voicereceiver.cancel();
    }

    /// <summary> An enum constant representing the window. voice input manager option. </summary>
    return {
        showDialog: showDialog
    };

});