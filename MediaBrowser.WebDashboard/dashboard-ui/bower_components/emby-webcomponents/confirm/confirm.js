define(['layoutManager', 'globalize'], function (layoutManager, globalize) {

    function showTvConfirm(options) {
        return new Promise(function (resolve, reject) {

            require(['actionsheet'], function (actionSheet) {

                var items = [];

                items.push({
                    name: globalize.translate('sharedcomponents#ButtonOk'),
                    id: 'ok'
                });

                items.push({
                    name: globalize.translate('sharedcomponents#ButtonCancel'),
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

    function showConfirmInternal(options, dialogHelper, resolve, reject) {

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
            dialogOptions.exitAnimationDuration = 160;
            dialogOptions.autoFocus = false;
        }

        var dlg = dialogHelper.createDialog(dialogOptions);
        var html = '';

        if (options.title) {
            html += '<h2>' + options.title + '</h2>';
        }

        var text = options.html || options.text;

        if (text) {
            html += '<div>' + text + '</div>';
        }

        html += '<div class="buttons">';

        html += '<button is="emby-button" type="button" class="btnConfirm" autofocus>' + globalize.translate('sharedcomponents#ButtonOk') + '</button>';

        html += '<button is="emby-button" type="button" class="btnCancel">' + globalize.translate('sharedcomponents#ButtonCancel') + '</button>';

        html += '</div>';

        dlg.innerHTML = html;
        document.body.appendChild(dlg);

        var confirmed = false;
        dlg.querySelector('.btnConfirm').addEventListener('click', function () {
            confirmed = true;
            dialogHelper.close(dlg);
        });
        dlg.querySelector('.btnCancel').addEventListener('click', function () {
            confirmed = false;
            dialogHelper.close(dlg);
        });

        dialogHelper.open(dlg).then(function () {

            if (confirmed) {
                resolve();
            } else {
                reject();
            }
        });
    }

    function showConfirm(options) {
        return new Promise(function (resolve, reject) {

            require(['dialogHelper', 'emby-button'], function (dialogHelper) {
                showConfirmInternal(options, dialogHelper, resolve, reject);
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