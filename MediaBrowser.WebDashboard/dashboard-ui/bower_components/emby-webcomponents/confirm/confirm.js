define(['layoutManager', 'dialogText'], function (layoutManager, dialogText) {

    function showTvConfirm(options) {
        return new Promise(function (resolve, reject) {

            require(['actionsheet'], function (actionSheet) {

                var items = [];

                items.push({
                    name: dialogText.get('Ok'),
                    id: 'ok'
                });

                items.push({
                    name: dialogText.get('Cancel'),
                    id: 'cancel'
                });

                actionSheet.show({

                    title: options.text,
                    items: items

                }).then(function (id) {

                    switch (id) {

                        case 'ok':
                            resolve();
                            break;
                        default:
                            reject();
                            break;
                    }

                }, reject);
            });
        });
    }

    function showConfirmInternal(options, paperdialoghelper, resolve, reject) {

        var dialogOptions = {
            removeOnClose: true
        };

        var backButton = false;

        if (layoutManager.tv) {
            dialogOptions.size = 'fullscreen';
            backButton = true;
            dialogOptions.autoFocus = true;
        } else {

            dialogOptions.modal = false;
            dialogOptions.entryAnimationDuration = 160;
            dialogOptions.exitAnimationDuration = 200;
            dialogOptions.autoFocus = false;
        }

        var dlg = paperdialoghelper.createDialog(dialogOptions);
        var html = '';

        if (options.title) {
            html += '<h2>' + options.title + '</h2>';
        }

        if (options.text) {
            html += '<div>' + options.text + '</div>';
        }

        html += '<div class="buttons">';

        html += '<paper-button class="btnConfirm" dialog-confirm autofocus>' + dialogText.get('Ok') + '</paper-button>';

        html += '<paper-button dialog-dismiss>' + dialogText.get('Cancel') + '</paper-button>';

        html += '</div>';

        dlg.innerHTML = html;
        document.body.appendChild(dlg);

        paperdialoghelper.open(dlg).then(function () {

            var confirmed = dlg.closingReason.confirmed;

            if (confirmed) {
                resolve();
            } else {
                reject();
            }
        });
    }

    function showConfirm(options) {
        return new Promise(function (resolve, reject) {

            require(['paperdialoghelper', 'paper-button'], function (paperdialoghelper) {
                showConfirmInternal(options, paperdialoghelper, resolve, reject);
            });
        });
    }

    return function (text, title) {

        var options;
        if (typeof text === 'string') {
            options = {
                title: title,
                text: text
            };
        } else {
            options = text;
        }

        if (layoutManager.tv) {
            return showTvConfirm(options);
        }

        return showConfirm(options);
    };
});