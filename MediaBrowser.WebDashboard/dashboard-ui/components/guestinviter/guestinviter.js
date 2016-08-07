define(['dialogHelper', 'jQuery', 'emby-input', 'emby-button', 'emby-collapse', 'paper-checkbox', 'paper-icon-button-light', 'formDialogStyle'], function (dialogHelper, $) {

    function renderLibrarySharingList(context, result) {

        var folderHtml = '';

        folderHtml += '<div class="paperCheckboxList">';

        folderHtml += result.Items.map(function (i) {

            var currentHtml = '';

            var isChecked = true;
            var checkedHtml = isChecked ? ' checked="checked"' : '';

            currentHtml += '<paper-checkbox class="chkShareFolder" data-folderid="' + i.Id + '" type="checkbox"' + checkedHtml + '>' + i.Name + '</paper-checkbox>';

            return currentHtml;

        }).join('');

        folderHtml += '</div>';

        context.querySelector('.librarySharingList').innerHTML = folderHtml;
    }

    function inviteUser(dlg) {

        Dashboard.showLoadingMsg();

        ApiClient.getJSON(ApiClient.getUrl("Channels", {})).then(function (channelsResult) {

            var shareExcludes = $(".chkShareFolder", dlg).get().filter(function (i) {

                return i.checked;

            }).map(function (i) {

                return i.getAttribute('data-folderid');
            });

            // Add/Update connect info
            ApiClient.ajax({

                type: "POST",
                url: ApiClient.getUrl('Connect/Invite'),
                dataType: 'json',
                data: {

                    ConnectUsername: dlg.querySelector('#txtConnectUsername').value,
                    EnabledLibraries: shareExcludes.join(','),
                    SendingUserId: Dashboard.getCurrentUserId(),
                    EnableLiveTv: false
                }

            }).then(function (result) {

                dlg.submitted = true;
                dialogHelper.close(dlg);

                Dashboard.hideLoadingMsg();

                showNewUserInviteMessage(dlg, result);

            }, function (response) {

                Dashboard.hideLoadingMsg();

                if (!response.status) {
                    // General error
                    require(['alert'], function (alert) {
                        alert({
                            text: Globalize.translate('DefaultErrorMessage')
                        });
                    });
                } else if (response.status == 404) {
                    // User doesn't exist
                    require(['alert'], function (alert) {
                        alert({
                            text: Globalize.translate('GuestUserNotFound')
                        });
                    });
                } else {

                    // status 400 = account not activated

                    // General error
                    showAccountErrorMessage();
                }
            });
        });
    }

    function showAccountErrorMessage() {

        var html = Globalize.translate('ErrorAddingGuestAccount1', '<a href="https://emby.media/connect" target="_blank">https://emby.media/connect</a>');
        html += '<br/><br/>' + Globalize.translate('ErrorAddingGuestAccount2', 'apps@emby.media');

        var text = Globalize.translate('ErrorAddingGuestAccount1', 'https://emby.media/connect');
        text += '\n\n' + Globalize.translate('ErrorAddingGuestAccount2', 'apps@emby.media');

        require(['alert'], function (alert) {
            alert({
                text: text,
                html: html
            });
        });
    }

    function showNewUserInviteMessage(page, result) {

        if (!result.IsNewUserInvitation && !result.IsPending) {

            // It was immediately approved
            return;
        }

        var message = result.IsNewUserInvitation ?
            Globalize.translate('MessageInvitationSentToNewUser', result.GuestDisplayName) :
            Globalize.translate('MessageInvitationSentToUser', result.GuestDisplayName);

        require(['alert'], function (alert) {
            alert({
                text: message,
                title: Globalize.translate('HeaderInvitationSent')
            });
        });
    }

    return {
        show: function () {
            return new Promise(function (resolve, reject) {

                var xhr = new XMLHttpRequest();
                xhr.open('GET', 'components/guestinviter/guestinviter.template.html', true);

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

                        inviteUser(dlg);

                        e.preventDefault();
                        return false;
                    });

                    ApiClient.getJSON(ApiClient.getUrl("Library/MediaFolders", { IsHidden: false })).then(function (result) {

                        renderLibrarySharingList(dlg, result);
                    });
                }

                xhr.send();
            });
        }
    };
});