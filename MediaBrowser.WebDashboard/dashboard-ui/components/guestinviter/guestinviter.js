define(['paperdialoghelper', 'paper-input', 'paper-button', 'jqmcollapsible'], function (paperDialogHelper) {

    function renderLibrarySharingList(context, result) {

        var folderHtml = '';

        folderHtml += '<div data-role="controlgroup">';

        folderHtml += result.Items.map(function (i) {

            var currentHtml = '';

            var id = 'chkShareFolder' + i.Id;

            currentHtml += '<label for="' + id + '">' + i.Name + '</label>';

            var isChecked = true;
            var checkedHtml = isChecked ? ' checked="checked"' : '';

            currentHtml += '<input data-mini="true" class="chkShareFolder" data-folderid="' + i.Id + '" type="checkbox" id="' + id + '"' + checkedHtml + ' />';

            return currentHtml;

        }).join('');

        folderHtml += '</div>';

        $('.librarySharingList', context).html(folderHtml).trigger('create');
    }

    function inviteUser(dlg) {

        Dashboard.showLoadingMsg();

        ApiClient.getJSON(ApiClient.getUrl("Channels", {})).then(function (channelsResult) {

            var shareExcludes = $(".chkShareFolder:checked", dlg).get().map(function (i) {

                return i.getAttribute('data-folderid');
            });

            // Add/Update connect info
            ApiClient.ajax({

                type: "POST",
                url: ApiClient.getUrl('Connect/Invite'),
                dataType: 'json',
                data: {

                    ConnectUsername: $('#txtConnectUsername', dlg).val(),
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
                    // needed for the collapsible
                    $(dlg.querySelector('form')).trigger('create');

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