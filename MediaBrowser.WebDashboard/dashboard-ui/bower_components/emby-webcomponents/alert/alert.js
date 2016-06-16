define(['dialogHelper', 'layoutManager', 'globalize', 'material-icons', 'css!./../prompt/style.css', 'emby-button', 'paper-icon-button-light'], function (dialogHelper, layoutManager, globalize) {

    function getIcon(icon, cssClass, canFocus, autoFocus) {

        var tabIndex = canFocus ? '' : ' tabindex="-1"';
        autoFocus = autoFocus ? ' autofocus' : '';
        return '<button is="paper-icon-button-light" class="autoSize ' + cssClass + '"' + tabIndex + autoFocus + '><i class="md-icon">' + icon + '</i></button>';
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
            html += getIcon('arrow_back', 'btnPromptExit', false);
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
            html += '<button is="emby-button" type="button" class="raised btnSubmit"><i class="md-icon">check</i><span>' + globalize.translate(buttonText) + '</span></button>';
        } else {
            html += '<div class="buttons" style="text-align:right;">';
            html += '<button is="emby-button" type="button" class="btnSubmit">' + globalize.translate(buttonText) + '</button>';
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