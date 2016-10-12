define(['dialogHelper', 'globalize', 'userSettings', 'layoutManager', 'connectionManager', 'require', 'loading', 'scrollHelper', 'emby-checkbox', 'css!./../formdialog', 'material-icons'], function (dialogHelper, globalize, userSettings, layoutManager, connectionManager, require, loading, scrollHelper) {
    'use strict';

    function save(context, options) {

        var categories = [];

        var chkCategorys = context.querySelectorAll('.chkCategory');
        for (var i = 0, length = chkCategorys.length; i < length; i++) {

            var type = chkCategorys[i].getAttribute('data-type');

            if (chkCategorys[i].checked) {
                categories.push(type);
            }
        }

        if (categories.length >= 4) {
            categories.push('series');
        }

        // differentiate between none and all
        categories.push('all');
        options.categories = categories;
    }

    function load(context, options) {

        var selectedCategories = options.categories || [];

        var chkCategorys = context.querySelectorAll('.chkCategory');
        for (var i = 0, length = chkCategorys.length; i < length; i++) {

            var type = chkCategorys[i].getAttribute('data-type');

            chkCategorys[i].checked = !selectedCategories.length || selectedCategories.indexOf(type) !== -1;
        }
    }

    function showEditor(options) {

        return new Promise(function (resolve, reject) {

            var settingsChanged = false;

            require(['text!./guide-categories.template.html'], function (template) {

                var dialogOptions = {
                    removeOnClose: true,
                    scrollY: false
                };

                if (layoutManager.tv) {
                    dialogOptions.size = 'fullscreen';
                } else {
                    dialogOptions.size = 'small';
                }

                var dlg = dialogHelper.createDialog(dialogOptions);

                dlg.classList.add('formDialog');

                var html = '';

                html += globalize.translateDocument(template, 'sharedcomponents');

                dlg.innerHTML = html;

                dlg.addEventListener('change', function () {

                    settingsChanged = true;
                });

                dlg.addEventListener('close', function () {

                    if (layoutManager.tv) {
                        scrollHelper.centerFocus.off(dlg.querySelector('.formDialogContent'), false);
                    }

                    save(dlg, options);

                    if (settingsChanged) {
                        resolve(options);
                    } else {
                        reject();
                    }
                });

                dlg.querySelector('.btnCancel').addEventListener('click', function () {
                    dialogHelper.close(dlg);
                });

                if (layoutManager.tv) {
                    scrollHelper.centerFocus.on(dlg.querySelector('.formDialogContent'), false);
                }

                load(dlg, options);
                dialogHelper.open(dlg);
            });
        });
    }

    return {
        show: showEditor
    };
});