define(['layoutManager', 'globalize'], function (layoutManager, globalize) {

    function showTvDialog(options) {
        return new Promise(function (resolve, reject) {

            require(['actionsheet'], function (actionSheet) {

                actionSheet.show({

                    title: options.text,
                    items: options.buttons

                }).then(resolve, reject);
            });
        });
    }

    function showDialogInternal(options, dialogHelper, resolve, reject) {

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
            dialogOptions.autoFocus = true;
        }

        var dlg = dialogHelper.createDialog(dialogOptions);
        var html = '';

        if (options.title) {
            html += '<h2>' + options.title + '</h2>';
        } 

        var text = options.html || options.text;

        if (text) {
            html += '<div style="margin:1em 0;">' + text + '</div>';
        }

        html += '<div class="buttons">';

        var i, length;
        for (i = 0, length = options.buttons.length; i < length; i++) {

            var item = options.buttons[i];
            var autoFocus = i == 0 ? ' autofocus' : '';
            html += '<button is="emby-button" type="button" class="btnOption" data-id="' + item.id + '"' + autoFocus + '>' + item.name + '</button>';
        }

        html += '</div>';

        dlg.innerHTML = html;
        document.body.appendChild(dlg);

        var dialogResult;
        function onButtonClick() {
            dialogResult = this.getAttribute('data-id');
            dialogHelper.close(dlg);
        }

        var buttons = dlg.querySelectorAll('.btnOption');
        for (i = 0, length = options.buttons.length; i < length; i++) {
            buttons[i].addEventListener('click', onButtonClick);
        }

        dialogHelper.open(dlg).then(function () {

            if (dialogResult) {
                resolve(dialogResult);
            } else {
                reject();
            }
        });
    }

    function showDialog(options) {
        return new Promise(function (resolve, reject) {

            require(['dialogHelper', 'emby-button'], function (dialogHelper) {
                showDialogInternal(options, dialogHelper, resolve, reject);
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
            return showTvDialog(options);
        }

        return showDialog(options);
    };
});