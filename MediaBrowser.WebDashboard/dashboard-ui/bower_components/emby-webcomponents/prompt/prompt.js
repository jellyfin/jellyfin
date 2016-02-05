define(['paperdialoghelper', 'layoutManager', 'html!./icons.html', 'css!./style.css', 'paper-button', 'paper-input'], function (paperdialoghelper, layoutManager) {

    function show(options, resolve, reject) {

        var dialogOptions = {
            removeOnClose: true
        };

        var backButton = false;
        var raisedButtons = false;

        if (layoutManager.tv) {
            dialogOptions.size = 'fullscreen';
            backButton = true;
            raisedButtons = true;
        }

        var dlg = paperdialoghelper.createDialog(dialogOptions);

        dlg.classList.add('promptDialog');

        var html = '';
        var submitValue = '';

        html += '<div class="promptDialogContent">';
        if (backButton) {
            html += '<paper-icon-button tabindex="-1" icon="dialog:arrow-back" class="btnPromptExit"></paper-icon-button>';
        }

        html += '<paper-input autoFocus class="txtPromptValue"></paper-input>';

        // TODO: An actual form element should probably be added
        html += '<br/>';
        if (raisedButtons) {
            html += '<paper-button raised class="btnSubmit"><iron-icon icon="dialog:check"></iron-icon><span>' + Globalize.translate('core#ButtonOk') + '</span></paper-button>';
        } else {
            html += '<paper-button class="btnSubmit">' + Globalize.translate('core#ButtonOk') + '</paper-button>';
            html += '<paper-button class="btnPromptExit">' + Globalize.translate('core#ButtonCancel') + '</paper-button>';
        }

        html += '</div>';

        dlg.innerHTML = html;

        if (options.text) {
            dlg.querySelector('.txtPromptValue').value = options.text;
        }

        if (options.title) {
            dlg.querySelector('.txtPromptValue').label = options.title;
        }

        document.body.appendChild(dlg);

        dlg.querySelector('.btnSubmit').addEventListener('click', function (e) {

            submitValue = dlg.querySelector('.txtPromptValue').value;
            paperdialoghelper.close(dlg);
        });

        dlg.querySelector('.btnPromptExit').addEventListener('click', function (e) {

            paperdialoghelper.close(dlg);
        });

        dlg.addEventListener('iron-overlay-closed', function () {

            var value = submitValue;
            if (value) {
                resolve(value);
            } else {
                reject();
            }
        });

        paperdialoghelper.open(dlg);
    }

    return function (options) {

        return new Promise(function (resolve, reject) {

            if (typeof options === 'string') {
                options = {
                    title: '',
                    text: options
                };
            }

            show(options, resolve, reject);
        });

    };
});