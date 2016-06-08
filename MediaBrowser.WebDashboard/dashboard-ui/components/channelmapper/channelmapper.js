define(['dialogHelper', 'loading', 'connectionManager', 'globalize', 'paper-checkbox', 'emby-input', 'paper-icon-button-light', 'emby-select', 'emby-button'],
function (dialogHelper, loading, connectionManager, globalize) {

    var currentServerId;

    function getEditorHtml() {

        var html = '';

        html += '<div class="dialogContent">';
        html += '<div class="dialogContentInner centeredContent">';
        html += 'coming soon';
        html += '</div>';
        html += '</div>';

        return html;
    }

    function initEditor(content, items) {

    }

    return function () {

        var self = this;

        self.show = function (options) {

            var items = options.items || {};
            currentServerId = options.serverId;

            var dialogOptions = {
                removeOnClose: true
            };

            dialogOptions.size = 'small';

            var dlg = dialogHelper.createDialog(dialogOptions);

            dlg.classList.add('formDialog');
            dlg.classList.add('ui-body-a');
            dlg.classList.add('background-theme-a');

            var html = '';
            var title = globalize.translate('MapChannels');

            html += '<div class="dialogHeader" style="margin:0 0 2em;">';
            html += '<button is="paper-icon-button-light" class="btnCancel" tabindex="-1"><iron-icon icon="nav:arrow-back"></iron-icon></button>';
            html += '<div class="dialogHeaderTitle">';
            html += title;
            html += '</div>';

            html += '</div>';

            html += getEditorHtml();

            dlg.innerHTML = html;
            document.body.appendChild(dlg);

            initEditor(dlg, items);

            dlg.querySelector('.btnCancel').addEventListener('click', function () {

                dialogHelper.close(dlg);
            });

            return new Promise(function (resolve, reject) {

                dlg.addEventListener('close', resolve);
                dialogHelper.open(dlg);
            });
        };
    };
});