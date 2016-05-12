define(['dialogHelper', 'layoutManager', 'globalize', 'html!./../icons/nav.html', 'css!./../prompt/style.css', 'paper-button', 'paper-icon-button-light', 'paper-input'], function (dialogHelper, layoutManager, globalize) {

    function getIcon(icon, cssClass, canFocus, autoFocus) {

        var tabIndex = canFocus ? '' : ' tabindex="-1"';
        autoFocus = autoFocus ? ' autofocus' : '';
        return '<button is="paper-icon-button-light" class="' + cssClass + '"' + tabIndex + autoFocus + '><iron-icon icon="' + icon + '"></iron-icon></button>';
    }

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
        var isFullscreen = false;

        if (layoutManager.tv) {
            dialogOptions.size = 'fullscreen';
            backButton = true;
            raisedButtons = true;
            isFullscreen = true;
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
            html += getIcon('dialog:arrow-back', 'btnPromptExit', false);
        }

        if (options.title) {
            html += '<h2>';
            html += options.title;
            html += '</h2>';
        } else if (!isFullscreen) {
            // Add a little space so it's not hugging the border
            html += '<br/>';
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

        var buttonText = options.type == 'error' ? 'sharedcomponents#ButtonOk' : 'sharedcomponents#ButtonGotIt';
        if (raisedButtons) {
            html += '<paper-button raised class="btnSubmit"><iron-icon icon="nav:check"></iron-icon><span>' + globalize.translate(buttonText) + '</span></paper-button>';
        } else {
            html += '<div class="buttons" style="text-align:right;">';
            html += '<paper-button class="btnSubmit">' + globalize.translate(buttonText) + '</paper-button>';
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