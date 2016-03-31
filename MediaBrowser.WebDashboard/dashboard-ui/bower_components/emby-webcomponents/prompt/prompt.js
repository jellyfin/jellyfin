define(['dialogHelper', 'layoutManager', 'dialogText', 'html!./icons.html', 'css!./style.css', 'paper-button', 'paper-input'], function (dialogHelper, layoutManager, dialogText) {

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
        var submitValue = '';

        html += '<div class="promptDialogContent">';
        if (backButton) {
            html += '<paper-icon-button tabindex="-1" icon="dialog:arrow-back" class="btnPromptExit"></paper-icon-button>';
        }

        if (options.title) {
            html += '<h2>';
            html += options.title;
            html += '</h2>';
        }

        html += '<form>';

        html += '<paper-input autoFocus class="txtPromptValue" value="' + (options.value || '') + '" label="' + (options.label || '') + '"></paper-input>';

        if (options.description) {
            html += '<div class="fieldDescription">';
            html += options.description;
            html += '</div>';
        }

        html += '<br/>';
        if (raisedButtons) {
            html += '<paper-button raised class="btnSubmit"><iron-icon icon="dialog:check"></iron-icon><span>' + dialogText.get('Ok') + '</span></paper-button>';
        } else {
            html += '<div class="buttons">';
            html += '<paper-button class="btnSubmit">' + dialogText.get('Ok') + '</paper-button>';
            html += '<paper-button class="btnPromptExit">' + dialogText.get('Cancel') + '</paper-button>';
            html += '</div>';
        }
        html += '</form>';

        html += '</div>';

        dlg.innerHTML = html;

        document.body.appendChild(dlg);

        dlg.querySelector('form').addEventListener('submit', function (e) {

            submitValue = dlg.querySelector('.txtPromptValue').value;
            e.preventDefault();
            e.stopPropagation();

            // Important, don't close the dialog until after the form has completed submitting, or it will cause an error in Chrome
            setTimeout(function () {
                dialogHelper.close(dlg);
            }, 300);

            return false;
        });

        dlg.querySelector('.btnSubmit').addEventListener('click', function (e) {

            // Do a fake form submit this the button isn't a real submit button
            var fakeSubmit = document.createElement('input');
            fakeSubmit.setAttribute('type', 'submit');
            fakeSubmit.style.display = 'none';
            var form = dlg.querySelector('form');
            form.appendChild(fakeSubmit);
            fakeSubmit.click();
            form.removeChild(fakeSubmit);
        });

        dlg.querySelector('.btnPromptExit').addEventListener('click', function (e) {

            dialogHelper.close(dlg);
        });

        return dialogHelper.open(dlg).then(function () {
            var value = submitValue;

            if (value) {
                return value;
            } else {
                return Promise.reject();
            }
        });
    };
});