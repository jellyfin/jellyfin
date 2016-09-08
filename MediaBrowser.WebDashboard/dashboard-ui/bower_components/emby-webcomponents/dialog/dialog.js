define(['dialogHelper', 'layoutManager', 'scrollHelper', 'globalize', 'require', 'material-icons', 'emby-button', 'paper-icon-button-light', 'emby-input', 'formDialogStyle'], function (dialogHelper, layoutManager, scrollHelper, globalize, require) {

    function showTvDialog(options) {
        return new Promise(function (resolve, reject) {

            require(['actionsheet'], function (actionSheet) {

                actionSheet.show({

                    title: options.text,
                    items: options.buttons,
                    timeout: options.timeout

                }).then(resolve, reject);
            });
        });
    }

    function showDialog(options, template) {

        var dialogOptions = {
            removeOnClose: true,
            scrollY: false
        };

        if (layoutManager.tv) {
            dialogOptions.size = 'fullscreen';
        } else {
            //dialogOptions.size = 'mini';
        }

        var dlg = dialogHelper.createDialog(dialogOptions);

        dlg.classList.add('formDialog');

        dlg.innerHTML = globalize.translateHtml(template, 'sharedcomponents');

        if (layoutManager.tv) {
            scrollHelper.centerFocus.on(dlg.querySelector('.formDialogContent'), false);
        } else {
            dlg.querySelector('.dialogContentInner').classList.add('dialogContentInner-mini');
        }

        //dlg.querySelector('.btnCancel').addEventListener('click', function (e) {
        //    dialogHelper.close(dlg);
        //});

        dlg.querySelector('.formDialogHeaderTitle').innerHTML = options.title || '';

        dlg.querySelector('.text').innerHTML = options.html || options.text || '';

        var i, length;
        var html = '';
        for (i = 0, length = options.buttons.length; i < length; i++) {

            var item = options.buttons[i];
            var autoFocus = i == 0 ? ' autofocus' : '';
            html += '<button is="emby-button" type="button" class="btnOption raised block formDialogFooterItem" data-id="' + item.id + '"' + autoFocus + '>' + item.name + '</button>';
        }

        dlg.querySelector('.formDialogFooter').innerHTML = html;

        var dialogResult;
        function onButtonClick() {
            dialogResult = this.getAttribute('data-id');
            dialogHelper.close(dlg);
        }

        var buttons = dlg.querySelectorAll('.btnOption');
        for (i = 0, length = buttons.length; i < length; i++) {
            buttons[i].addEventListener('click', onButtonClick);
        }

        return dialogHelper.open(dlg).then(function () {

            if (layoutManager.tv) {
                scrollHelper.centerFocus.off(dlg.querySelector('.formDialogContent'), false);
            }

            if (dialogResult) {
                return dialogResult;
            } else {
                return Promise.reject();
            }
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

        return new Promise(function (resolve, reject) {
            require(['text!./dialog.template.html'], function (template) {
                showDialog(options, template).then(resolve, reject);
            });
        });
    };
});