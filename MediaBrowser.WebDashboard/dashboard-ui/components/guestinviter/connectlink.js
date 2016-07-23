define(['dialogHelper', 'jQuery', 'emby-input', 'emby-button', 'emby-collapse', 'paper-checkbox', 'paper-icon-button-light'], function (dialogHelper, $) {

    function updateUserInfo(user, newConnectUsername, actionCallback, noActionCallback) {
        var currentConnectUsername = user.ConnectUserName || '';
        var enteredConnectUsername = newConnectUsername;

        var linkUrl = ApiClient.getUrl('Users/' + user.Id + '/Connect/Link');

        if (currentConnectUsername && !enteredConnectUsername) {

            // Remove connect info
            // Add/Update connect info
            ApiClient.ajax({

                type: "DELETE",
                url: linkUrl

            }).then(function () {

                Dashboard.alert({

                    message: Globalize.translate('MessageEmbyAccontRemoved'),
                    title: Globalize.translate('HeaderEmbyAccountRemoved'),

                    callback: actionCallback

                });
            }, function () {

                Dashboard.alert({

                    message: Globalize.translate('ErrorRemovingEmbyConnectAccount')

                });
            });

        }
        else if (currentConnectUsername != enteredConnectUsername) {

            // Add/Update connect info
            ApiClient.ajax({
                type: "POST",
                url: linkUrl,
                data: {
                    ConnectUsername: enteredConnectUsername
                },
                dataType: 'json'

            }).then(function (result) {

                var msgKey = result.IsPending ? 'MessagePendingEmbyAccountAdded' : 'MessageEmbyAccountAdded';

                Dashboard.alert({
                    message: Globalize.translate(msgKey),
                    title: Globalize.translate('HeaderEmbyAccountAdded'),

                    callback: actionCallback

                });

            }, function () {

                showEmbyConnectErrorMessage('.');
            });

        } else {
            if (noActionCallback) {
                noActionCallback();
            }
        }
    }

    function showEmbyConnectErrorMessage(username) {

        var html;
        var text;

        if (username) {

            html = Globalize.translate('ErrorAddingEmbyConnectAccount1', '<a href="https://emby.media/connect" target="_blank">https://emby.media/connect</a>');
            html += '<br/><br/>' + Globalize.translate('ErrorAddingEmbyConnectAccount2', 'apps@emby.media');

            text = Globalize.translate('ErrorAddingEmbyConnectAccount1', 'https://emby.media/connect');
            text += '\n\n' + Globalize.translate('ErrorAddingEmbyConnectAccount2', 'apps@emby.media');

        } else {
            html = text = Globalize.translate('DefaultErrorMessage');
        }

        require(['alert'], function (alert) {
            alert({
                text: text,
                html: html
            });
        });
    }

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
                    document.body.appendChild(dlg);

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
                            updateUserInfo(user, dlg.querySelector('#txtConnectUsername').value, function() {
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