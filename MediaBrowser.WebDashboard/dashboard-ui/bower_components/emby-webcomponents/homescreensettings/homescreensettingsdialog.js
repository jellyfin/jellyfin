define(['dialogHelper', 'layoutManager', 'globalize', 'require', 'events', 'homescreenSettings', 'paper-icon-button-light', 'css!./../formdialog'], function (dialogHelper, layoutManager, globalize, require, events, HomescreenSettings) {
    'use strict';

    function centerFocus(elem, horiz, on) {
        require(['scrollHelper'], function (scrollHelper) {
            var fn = on ? 'on' : 'off';
            scrollHelper.centerFocus[fn](elem, horiz);
        });
    }

    function show(options) {
        return new Promise(function (resolve, reject) {

            require(['text!./homescreensettingsdialog.template.html'], function (template) {

                var dialogOptions = {
                    removeOnClose: true,
                    scrollY: false
                };

                if (layoutManager.tv) {
                    dialogOptions.size = 'fullscreen';
                } else {
                    dialogOptions.size = 'medium-tall';
                }

                var dlg = dialogHelper.createDialog(dialogOptions);

                dlg.classList.add('formDialog');

                var html = '';
                var submitted = false;

                html += globalize.translateDocument(template, 'sharedcomponents');

                dlg.innerHTML = html;

                if (layoutManager.tv) {
                    centerFocus(dlg.querySelector('.formDialogContent'), false, true);
                }

                var homescreenSettingsInstance = new HomescreenSettings({
                    serverId: options.serverId,
                    userId: options.userId,
                    element: dlg.querySelector('.settingsContent'),
                    userSettings: options.userSettings,
                    enableSaveButton: false,
                    enableSaveConfirmation: false
                });

                dialogHelper.open(dlg);

                dlg.addEventListener('close', function () {

                    if (layoutManager.tv) {
                        centerFocus(dlg.querySelector('.formDialogContent'), false, false);
                    }

                    if (submitted) {
                        resolve();
                    } else {
                        reject();
                    }
                });

                dlg.querySelector('.btnCancel').addEventListener('click', function (e) {

                    dialogHelper.close(dlg);
                });

                dlg.querySelector('.btnSave').addEventListener('click', function (e) {

                    submitted = true;
                    homescreenSettingsInstance.submit();
                });

                events.on(homescreenSettingsInstance, 'saved', function () {
                    submitted = true;
                    dialogHelper.close(dlg);
                });
            });
        });
    }

    return {
        show: show
    };
});