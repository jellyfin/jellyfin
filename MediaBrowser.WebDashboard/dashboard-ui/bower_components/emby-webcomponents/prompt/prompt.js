define(['paperdialoghelper', 'layoutManager', 'html!./icons.html', 'css!./style.css', 'paper-button', 'paper-input'], function (paperdialoghelper, layoutManager) {

    function show(options, resolve, reject) {

        var dialogOptions = {
            removeOnClose: true
        };

        if (layoutManager.tv) {
            dialogOptions.size = 'fullscreen';
        }

        var dlg = paperdialoghelper.createDialog(dialogOptions);

        dlg.classList.add('promptDialog');

        var html = '';
        var submitValue = '';

        html += '<div style="margin:0;padding:0;width:50%;text-align:left;">';
        html += '<paper-icon-button tabindex="-1" icon="dialog:arrow-back" class="btnPromptExit"></paper-icon-button>';

        if (options.title) {
            html += '<h1 style="margin-bottom:0;">';
            html += options.title;
            html += '</h1>';
        }

        html += '<paper-input autoFocus class="txtPromptValue"></paper-input>';

        // TODO: An actual form element should probably be added
        html += '<br/>';
        html += '<paper-button raised class="block paperSubmit"><iron-icon icon="dialog:check"></iron-icon><span>' + Globalize.translate('core#ButtonOk') + '</span></paper-button>';

        html += '</div>';

        dlg.innerHTML = html;

        if (options.text) {
            dlg.querySelector('.txtPromptValue').value = options.text;
        }

        document.body.appendChild(dlg);

        dlg.querySelector('.paperSubmit').addEventListener('click', function (e) {

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