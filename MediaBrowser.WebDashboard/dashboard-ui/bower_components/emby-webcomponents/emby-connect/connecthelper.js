define(['globalize', 'loading', 'alert'], function (globalize, loading, alert) {
    'use strict';

    function showNewUserInviteMessage(result) {

        if (!result.IsNewUserInvitation && !result.IsPending) {

            // It was immediately approved
            return Promise.resolve();
        }

        var message = result.IsNewUserInvitation ?
            globalize.translate('MessageInvitationSentToNewUser', result.GuestDisplayName) :
            globalize.translate('MessageInvitationSentToUser', result.GuestDisplayName);

        alert({
            text: message,
            title: globalize.translate('HeaderInvitationSent')
        });
    }

    function inviteGuest(options) {

        var apiClient = options.apiClient;

        loading.show();

        // Add/Update connect info
        return apiClient.ajax({

            type: "POST",
            url: apiClient.getUrl('Connect/Invite'),
            dataType: 'json',
            data: options.guestOptions || {}

        }).then(function (result) {

            loading.hide();
            return showNewUserInviteMessage(result);

        }, function (response) {

            loading.hide();

            if (response.status === 404) {
                // User doesn't exist
                alert({
                    text: globalize.translate('GuestUserNotFound')
                });

            } else if ((response.status || 0) >= 500) {

                // Unable to reach connect server ?
                alert({
                    text: globalize.translate('ErrorReachingEmbyConnect')
                });

            } else {

                // status 400 = account not activated

                // General error
                showGuestGeneralErrorMessage();
            }
        });
    }

    function showGuestGeneralErrorMessage() {

        var html = globalize.translate('ErrorAddingGuestAccount1', '<a href="https://emby.media/connect" target="_blank">https://emby.media/connect</a>');
        html += '<br/><br/>' + globalize.translate('ErrorAddingGuestAccount2', 'apps@emby.media');

        var text = globalize.translate('ErrorAddingGuestAccount1', 'https://emby.media/connect');
        text += '\n\n' + globalize.translate('ErrorAddingGuestAccount2', 'apps@emby.media');

        alert({
            text: text,
            html: html
        });
    }

    function showLinkUserMessage(username) {

        var html;
        var text;

        if (username) {

            html = globalize.translate('ErrorAddingEmbyConnectAccount1', '<a href="https://emby.media/connect" target="_blank">https://emby.media/connect</a>');
            html += '<br/><br/>' + globalize.translate('ErrorAddingEmbyConnectAccount2', 'apps@emby.media');

            text = globalize.translate('ErrorAddingEmbyConnectAccount1', 'https://emby.media/connect');
            text += '\n\n' + globalize.translate('ErrorAddingEmbyConnectAccount2', 'apps@emby.media');

        } else {
            html = text = globalize.translate('DefaultErrorMessage');
        }

        return alert({
            text: text,
            html: html
        });
    }

    function updateUserLink(apiClient, user, newConnectUsername) {
        var currentConnectUsername = user.ConnectUserName || '';
        var enteredConnectUsername = newConnectUsername;

        var linkUrl = apiClient.getUrl('Users/' + user.Id + '/Connect/Link');

        if (currentConnectUsername && !enteredConnectUsername) {

            // Remove connect info
            // Add/Update connect info
            return apiClient.ajax({

                type: "DELETE",
                url: linkUrl

            }).then(function () {

                return alert({
                    text: globalize.translate('MessageEmbyAccontRemoved'),
                    title: globalize.translate('HeaderEmbyAccountRemoved'),

                }).catch(function () {
                    return Promise.resolve();
                });

            }, function () {

                return alert({
                    text: globalize.translate('ErrorRemovingEmbyConnectAccount')

                }).then(function () {
                    return Promise.reject();
                });
            });

        }
        else if (currentConnectUsername !== enteredConnectUsername) {

            // Add/Update connect info
            return apiClient.ajax({
                type: "POST",
                url: linkUrl,
                data: {
                    ConnectUsername: enteredConnectUsername
                },
                dataType: 'json'

            }).then(function (result) {

                var msgKey = result.IsPending ? 'MessagePendingEmbyAccountAdded' : 'MessageEmbyAccountAdded';

                return alert({
                    text: globalize.translate(msgKey),
                    title: globalize.translate('HeaderEmbyAccountAdded'),

                }).catch(function () {
                    return Promise.resolve();
                });

            }, function () {

                return showLinkUserMessage('.').then(function () {
                    return Promise.reject();
                });
            });

        } else {
            return Promise.reject();
        }
    }

    return {
        inviteGuest: inviteGuest,
        updateUserLink: updateUserLink
    };
});