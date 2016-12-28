define(['dialogHelper', 'connectHelper', 'emby-input', 'emby-button', 'emby-collapse', 'paper-icon-button-light', 'formDialogStyle'], function (dialogHelper, connectHelper) {
    'use strict';

    return {
        show: function () {
            return new Promise(function (resolve, reject) {

                var xhr = new XMLHttpRequest();
                xhr.open('GET', 'components/guestinviter/connectlink.template.html', true);

                xhr.onload = function (e) {

                    var template = this.response;
                    var dlg = dialogHelper.createDialog({
                        removeOnClose: true,
                        size: 'small'
                    });

                    dlg.classList.add('ui-body-a');
                    dlg.classList.add('background-theme-a');

                    dlg.classList.add('formDialog');

                    var html = '';

                    html += Globalize.translateDocument(template);

                    dlg.innerHTML = html;

                    dialogHelper.open(dlg);

                    dlg.addEventListener('close', function () {

                        if (dlg.submitted) {
                            resolve();
                        } else {
                            reject();
                        }
                    });

                    dlg.querySelector('.btnCancel').addEventListener('click', function (e) {

                        dialogHelper.close(dlg);
                    });

                    dlg.querySelector('form').addEventListener('submit', function (e) {

                        ApiClient.getCurrentUser().then(function (user) {

                            connectHelper.updateUserLink(ApiClient, user, dlg.querySelector('#txtConnectUsername').value).then(function () {
                                dialogHelper.close(dlg);
                            }, function () {
                                dialogHelper.close(dlg);
                            });
                        });

                        e.preventDefault();
                        return false;
                    });
                }

                xhr.send();
            });
        }
    };
});