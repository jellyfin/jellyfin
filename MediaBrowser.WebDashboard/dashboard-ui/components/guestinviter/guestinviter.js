define(['paperdialoghelper', 'jQuery', 'paper-input', 'paper-button', 'emby-collapsible', 'paper-checkbox'], function (paperDialogHelper, $) {

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
                paperDialogHelper.close(dlg);

                Dashboard.hideLoadingMsg();

                showNewUserInviteMessage(dlg, result);

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

        Dashboard.alert({
            message: message,
            title: Globalize.translate('HeaderInvitationSent')
        });
    }

    return {
        show: function () {
            return new Promise(function (resolve, reject) {

                var xhr = new XMLHttpRequest();
                xhr.open('GET', 'components/guestinviter/guestinviter.template.html', true);

                xhr.onload = function (e) {

                    var template = this.response;
                    var dlg = paperDialogHelper.createDialog({
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

                    paperDialogHelper.open(dlg);

                    dlg.addEventListener('iron-overlay-closed', function () {

                        if (dlg.submitted) {
                            resolve();
                        } else {
                            reject();
                        }
                    });

                    dlg.querySelector('.btnCancel').addEventListener('click', function (e) {

                        paperDialogHelper.close(dlg);
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