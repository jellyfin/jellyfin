define(['dialogHelper', 'dom', 'layoutManager', 'scrollHelper', 'globalize', 'require', 'material-icons', 'emby-button', 'paper-icon-button-light', 'emby-input', 'formDialogStyle'], function (dialogHelper, dom, layoutManager, scrollHelper, globalize, require) {
    'use strict';

    function showDialog(options, template) {

        var dialogOptions = {
            removeOnClose: true,
            scrollY: false
        };

        var enableTvLayout = layoutManager.tv;

        if (enableTvLayout) {
            dialogOptions.size = 'fullscreen';
        }

        var dlg = dialogHelper.createDialog(dialogOptions);

        dlg.classList.add('formDialog');

        dlg.innerHTML = globalize.translateHtml(template, 'sharedcomponents');

        dlg.style['align-items'] = 'center';
        dlg.style['justify-content'] = 'center';
        var formDialogContent = dlg.querySelector('.formDialogContent');
        formDialogContent.style['flex-grow'] = 'initial';

        if (enableTvLayout) {
            formDialogContent.style['max-width'] = '50%';
            formDialogContent.style['max-height'] = '60%';
            scrollHelper.centerFocus.on(formDialogContent, false);
        } else {
            formDialogContent.style.maxWidth = (Math.min((options.buttons.length * 150) + 200, dom.getWindowSize().innerWidth - 50)) + 'px';
            dlg.classList.add('dialog-fullscreen-lowres');
        }

        //dlg.querySelector('.btnCancel').addEventListener('click', function (e) {
        //    dialogHelper.close(dlg);
        //});

        if (options.title) {
            dlg.querySelector('.formDialogHeaderTitle').innerHTML = options.title || '';
        } else {
            dlg.querySelector('.formDialogHeaderTitle').classList.add('hide');
        }

        dlg.querySelector('.text').innerHTML = options.html || options.text || '';

        var i, length;
        var html = '';
        for (i = 0, length = options.buttons.length; i < length; i++) {

            var item = options.buttons[i];
            var autoFocus = i === 0 ? ' autofocus' : '';

            var buttonClass = 'btnOption raised formDialogFooterItem formDialogFooterItem-autosize';

            if (item.type) {
                buttonClass += ' button-' + item.type;
            }

            html += '<button is="emby-button" type="button" class="' + buttonClass + '" data-id="' + item.id + '"' + autoFocus + '>' + item.name + '</button>';
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

            if (enableTvLayout) {
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

        return new Promise(function (resolve, reject) {
            require(['text!./dialog.template.html'], function (template) {
                showDialog(options, template).then(resolve, reject);
            });
        });
    };
});