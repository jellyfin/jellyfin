define(['appSettings', 'events', 'browser', 'loading', 'playbackManager', 'appRouter', 'globalize', 'apphost'], function (appSettings, events, browser, loading, playbackManager, appRouter, globalize, appHost) {
    'use strict';

    function mirrorItem(info, player) {

        var item = info.item;

        playbackManager.displayContent({

            ItemName: item.Name,
            ItemId: item.Id,
            ItemType: item.Type,
            Context: info.context
        }, player);
    }

    function mirrorIfEnabled(info) {

        if (info && playbackManager.enableDisplayMirroring()) {

            var getPlayerInfo = playbackManager.getPlayerInfo();

            if (getPlayerInfo) {
                if (!getPlayerInfo.isLocalPlayer && getPlayerInfo.supportedCommands.indexOf('DisplayContent') !== -1) {
                    mirrorItem(info, playbackManager.getCurrentPlayer());
                }
            }
        }
    }

    function emptyCallback() {
        // avoid console logs about uncaught promises
    }

    function getTargetSecondaryText(target) {

        if (target.user) {

            return target.user.Name;
        }

        return null;
    }

    function getIcon(target) {

        var deviceType = target.deviceType;

        if (!deviceType && target.isLocalPlayer) {
            if (browser.tv) {
                deviceType = 'tv';
            } else if (browser.mobile) {
                deviceType = 'smartphone';
            } else {
                deviceType = 'desktop';
            }
        }

        if (!deviceType) {
            deviceType = 'tv';
        }

        switch (deviceType) {

            case 'smartphone':
                return '&#xE32C;';
            case 'tablet':
                return '&#xE32F;';
            case 'tv':
                return '&#xE333;';
            case 'cast':
                return '&#xE307;';
            case 'desktop':
                return '&#xE30A;';
            default:
                return '&#xE333;';
        }
    }

    function showPlayerSelection(button) {

        var currentPlayerInfo = playbackManager.getPlayerInfo();

        if (currentPlayerInfo) {
            if (!currentPlayerInfo.isLocalPlayer) {
                showActivePlayerMenu(currentPlayerInfo);
                return;
            }
        }

        var currentPlayerId = currentPlayerInfo ? currentPlayerInfo.id : null;

        loading.show();

        playbackManager.getTargets().then(function (targets) {

            var menuItems = targets.map(function (t) {

                var name = t.name;

                if (t.appName && t.appName !== t.name) {
                    name += " - " + t.appName;
                }

                return {
                    name: name,
                    id: t.id,
                    selected: currentPlayerId === t.id,
                    secondaryText: getTargetSecondaryText(t),
                    icon: getIcon(t)
                };

            });

            require(['actionsheet'], function (actionsheet) {

                loading.hide();

                var menuOptions = {
                    title: globalize.translate('sharedcomponents#HeaderPlayOn'),
                    items: menuItems,
                    positionTo: button,

                    resolveOnClick: true,
                    border: true
                };

                // Unfortunately we can't allow the url to change or chromecast will throw a security error
                // Might be able to solve this in the future by moving the dialogs to hashbangs
                if (!(!browser.chrome || appHost.supports('castmenuhashchange'))) {
                    menuOptions.enableHistory = false;
                }

                actionsheet.show(menuOptions).then(function (id) {

                    var target = targets.filter(function (t) {
                        return t.id === id;
                    })[0];

                    playbackManager.trySetActivePlayer(target.playerName, target);

                    mirrorIfEnabled();

                }, emptyCallback);
            });
        });
    }

    function showActivePlayerMenu(playerInfo) {

        require(['dialogHelper', 'dialog', 'emby-checkbox', 'emby-button'], function (dialogHelper) {
            showActivePlayerMenuInternal(dialogHelper, playerInfo);
        });
    }


    function disconnectFromPlayer(currentDeviceName) {

        if (playbackManager.getSupportedCommands().indexOf('EndSession') !== -1) {

            require(['dialog'], function (dialog) {

                var menuItems = [];

                menuItems.push({
                    name: globalize.translate('sharedcomponents#Yes'),
                    id: 'yes'
                });
                menuItems.push({
                    name: globalize.translate('sharedcomponents#No'),
                    id: 'no'
                });

                dialog({
                    buttons: menuItems,
                    //positionTo: positionTo,
                    text: globalize.translate('sharedcomponents#ConfirmEndPlayerSession', currentDeviceName)

                }).then(function (id) {
                    switch (id) {

                        case 'yes':
                            playbackManager.getCurrentPlayer().endSession();
                            playbackManager.setDefaultPlayerActive();
                            break;
                        case 'no':
                            playbackManager.setDefaultPlayerActive();
                            break;
                        default:
                            break;
                    }
                });

            });


        } else {

            playbackManager.setDefaultPlayerActive();
        }
    }

    function showActivePlayerMenuInternal(dialogHelper, playerInfo) {

        var html = '';

        var dialogOptions = {
            removeOnClose: true
        };

        dialogOptions.modal = false;
        dialogOptions.entryAnimationDuration = 160;
        dialogOptions.exitAnimationDuration = 160;
        dialogOptions.autoFocus = false;

        var dlg = dialogHelper.createDialog(dialogOptions);

        dlg.classList.add('promptDialog');

        var currentDeviceName = (playerInfo.deviceName || playerInfo.name);

        html += '<div class="promptDialogContent" style="padding:1.5em;">';
        html += '<h2 style="margin-top:.5em;">';
        html += currentDeviceName;
        html += '</h2>';

        html += '<div>';

        if (playerInfo.supportedCommands.indexOf('DisplayContent') !== -1) {

            html += '<label class="checkboxContainer">';
            var checkedHtml = playbackManager.enableDisplayMirroring() ? ' checked' : '';
            html += '<input type="checkbox" is="emby-checkbox" class="chkMirror"' + checkedHtml + '/>';
            html += '<span>' + globalize.translate('sharedcomponents#EnableDisplayMirroring') + '</span>';
            html += '</label>';
        }

        html += '</div>';

        html += '<div style="margin-top:1em;display:flex;justify-content: flex-end;">';

        html += '<button is="emby-button" type="button" class="button-flat btnRemoteControl promptDialogButton">' + globalize.translate('sharedcomponents#HeaderRemoteControl') + '</button>';
        html += '<button is="emby-button" type="button" class="button-flat btnDisconnect promptDialogButton ">' + globalize.translate('sharedcomponents#Disconnect') + '</button>';
        html += '<button is="emby-button" type="button" class="button-flat btnCancel promptDialogButton">' + globalize.translate('sharedcomponents#ButtonCancel') + '</button>';
        html += '</div>';

        html += '</div>';
        dlg.innerHTML = html;

        var chkMirror = dlg.querySelector('.chkMirror');

        if (chkMirror) {
            chkMirror.addEventListener('change', onMirrorChange);
        }

        var destination = '';

        var btnRemoteControl = dlg.querySelector('.btnRemoteControl');
        if (btnRemoteControl) {
            btnRemoteControl.addEventListener('click', function () {
                destination = 'nowplaying';
                dialogHelper.close(dlg);
            });
        }

        dlg.querySelector('.btnDisconnect').addEventListener('click', function () {
            destination = 'disconnectFromPlayer';
            dialogHelper.close(dlg);
        });

        dlg.querySelector('.btnCancel').addEventListener('click', function () {
            dialogHelper.close(dlg);
        });

        dialogHelper.open(dlg).then(function () {
            if (destination === 'nowplaying') {
                appRouter.showNowPlaying();
            }
            else if (destination === 'disconnectFromPlayer') {
                disconnectFromPlayer(currentDeviceName);
            }
        }, emptyCallback);
    }

    function onMirrorChange() {
        playbackManager.enableDisplayMirroring(this.checked);
    }

    document.addEventListener('viewshow', function (e) {

        var state = e.detail.state || {};
        var item = state.item;

        if (item && item.ServerId) {
            mirrorIfEnabled({
                item: item
            });
            return;
        }
    });

    events.on(appSettings, 'change', function (e, name) {
        if (name === 'displaymirror') {
            mirrorIfEnabled();
        }
    });

    events.on(playbackManager, 'pairing', function (e) {
        loading.show();
    });

    events.on(playbackManager, 'paired', function (e) {
        loading.hide();
    });

    events.on(playbackManager, 'pairerror', function (e) {
        loading.hide();
    });

    return {
        show: showPlayerSelection
    };
});