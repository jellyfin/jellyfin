define(['dialogHelper', 'layoutManager', 'dialogText', 'html!./../prompt/icons.html', 'css!./../prompt/style.css', 'paper-button', 'paper-input'], function (dialogHelper, layoutManager, dialogText) {

    return function (options) {

        if (typeof options === 'string') {
            options = {
                title: '',
                text: options
            };
        }

        var dialogOptions = {
            removeOnClose: true
        };

        var backButton = false;
        var raisedButtons = false;

        if (layoutManager.tv) {
            dialogOptions.size = 'fullscreen';
            backButton = true;
            raisedButtons = true;
        } else {

            dialogOptions.modal = false;
            dialogOptions.entryAnimationDuration = 160;
            dialogOptions.exitAnimationDuration = 200;
        }

        var dlg = dialogHelper.createDialog(dialogOptions);

        dlg.classList.add('promptDialog');

        var html = '';

        html += '<div class="promptDialogContent">';
        if (backButton) {
            html += '<paper-icon-button tabindex="-1" icon="dialog:arrow-back" class="btnPromptExit"></paper-icon-button>';
        }

        if (options.title) {
            html += '<h2>';
            html += options.title;
            html += '</h2>';
        }

        var text = options.html || options.text;

        if (text) {

            if (options.title) {
                html += '<p style="margin-top:2em;">';
            } else {
                html += '<p>';
            }

            html += text;
            html += '</p>';
        }

        var buttonText = options.type == 'error' ? 'Ok' : 'GotIt';
        if (raisedButtons) {
            html += '<paper-button raised class="btnSubmit"><iron-icon icon="dialog:check"></iron-icon><span>' + dialogText.get(buttonText) + '</span></paper-button>';
        } else {
            html += '<div style="text-align:right;">';
            html += '<paper-button class="btnSubmit">' + dialogText.get(buttonText) + '</paper-button>';
            html += '</div>';
        }

        html += '</div>';

        dlg.innerHTML = html;

        document.body.appendChild(dlg);

        dlg.querySelector('.btnSubmit').addEventListener('click', function (e) {

            dialogHelper.close(dlg);
        });

        return dialogHelper.open(dlg);
    };
});