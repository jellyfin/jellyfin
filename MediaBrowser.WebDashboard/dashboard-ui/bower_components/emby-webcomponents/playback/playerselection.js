define(["appSettings", "events", "browser", "loading", "playbackManager", "appRouter", "globalize", "apphost"], function(appSettings, events, browser, loading, playbackManager, appRouter, globalize, appHost) {
    "use strict";

    function mirrorItem(info, player) {
        var item = info.item;
        playbackManager.displayContent({
            ItemName: item.Name,
            ItemId: item.Id,
            ItemType: item.Type,
            Context: info.context
        }, player)
    }

    function mirrorIfEnabled(info) {
        if (info && playbackManager.enableDisplayMirroring()) {
            var getPlayerInfo = playbackManager.getPlayerInfo();
            getPlayerInfo && (getPlayerInfo.isLocalPlayer || -1 === getPlayerInfo.supportedCommands.indexOf("DisplayContent") || mirrorItem(info, playbackManager.getCurrentPlayer()))
        }
    }

    function emptyCallback() {}

    function getTargetSecondaryText(target) {
        return target.user ? target.user.Name : null
    }

    function getIcon(target) {
        var deviceType = target.deviceType;
        switch (!deviceType && target.isLocalPlayer && (deviceType = browser.tv ? "tv" : browser.mobile ? "smartphone" : "desktop"), deviceType || (deviceType = "tv"), deviceType) {
            case "smartphone":
                return "&#xE32C;";
            case "tablet":
                return "&#xE32F;";
            case "tv":
                return "&#xE333;";
            case "cast":
                return "&#xE307;";
            case "desktop":
                return "&#xE30A;";
            default:
                return "&#xE333;"
        }
    }

    function showPlayerSelection(button) {
        var currentPlayerInfo = playbackManager.getPlayerInfo();
        if (currentPlayerInfo && !currentPlayerInfo.isLocalPlayer) return void showActivePlayerMenu(currentPlayerInfo);
        var currentPlayerId = currentPlayerInfo ? currentPlayerInfo.id : null;
        loading.show(), playbackManager.getTargets().then(function(targets) {
            var menuItems = targets.map(function(t) {
                var name = t.name;
                return t.appName && t.appName !== t.name && (name += " - " + t.appName), {
                    name: name,
                    id: t.id,
                    selected: currentPlayerId === t.id,
                    secondaryText: getTargetSecondaryText(t),
                    icon: getIcon(t)
                }
            });
            require(["actionsheet"], function(actionsheet) {
                loading.hide();
                var menuOptions = {
                    title: globalize.translate("sharedcomponents#HeaderPlayOn"),
                    items: menuItems,
                    positionTo: button,
                    resolveOnClick: !0,
                    border: !0
                };
                browser.chrome && !appHost.supports("castmenuhashchange") && (menuOptions.enableHistory = !1), actionsheet.show(menuOptions).then(function(id) {
                    var target = targets.filter(function(t) {
                        return t.id === id
                    })[0];
                    playbackManager.trySetActivePlayer(target.playerName, target), mirrorIfEnabled()
                }, emptyCallback)
            })
        })
    }

    function showActivePlayerMenu(playerInfo) {
        require(["dialogHelper", "dialog", "emby-checkbox", "emby-button"], function(dialogHelper) {
            showActivePlayerMenuInternal(dialogHelper, playerInfo)
        })
    }

    function disconnectFromPlayer(currentDeviceName) {
        -1 !== playbackManager.getSupportedCommands().indexOf("EndSession") ? require(["dialog"], function(dialog) {
            var menuItems = [];
            menuItems.push({
                name: globalize.translate("sharedcomponents#Yes"),
                id: "yes"
            }), menuItems.push({
                name: globalize.translate("sharedcomponents#No"),
                id: "no"
            }), dialog({
                buttons: menuItems,
                text: globalize.translate("sharedcomponents#ConfirmEndPlayerSession", currentDeviceName)
            }).then(function(id) {
                switch (id) {
                    case "yes":
                        playbackManager.getCurrentPlayer().endSession(), playbackManager.setDefaultPlayerActive();
                        break;
                    case "no":
                        playbackManager.setDefaultPlayerActive()
                }
            })
        }) : playbackManager.setDefaultPlayerActive()
    }

    function showActivePlayerMenuInternal(dialogHelper, playerInfo) {
        var html = "",
            dialogOptions = {
                removeOnClose: !0
            };
        dialogOptions.modal = !1, dialogOptions.entryAnimationDuration = 160, dialogOptions.exitAnimationDuration = 160, dialogOptions.autoFocus = !1;
        var dlg = dialogHelper.createDialog(dialogOptions);
        dlg.classList.add("promptDialog");
        var currentDeviceName = playerInfo.deviceName || playerInfo.name;
        if (html += '<div class="promptDialogContent" style="padding:1.5em;">', html += '<h2 style="margin-top:.5em;">', html += currentDeviceName, html += "</h2>", html += "<div>", -1 !== playerInfo.supportedCommands.indexOf("DisplayContent")) {
            html += '<label class="checkboxContainer">';
            html += '<input type="checkbox" is="emby-checkbox" class="chkMirror"' + (playbackManager.enableDisplayMirroring() ? " checked" : "") + "/>", html += "<span>" + globalize.translate("sharedcomponents#EnableDisplayMirroring") + "</span>", html += "</label>"
        }
        html += "</div>", html += '<div style="margin-top:1em;display:flex;justify-content: flex-end;">', html += '<button is="emby-button" type="button" class="button-flat btnRemoteControl promptDialogButton">' + globalize.translate("sharedcomponents#HeaderRemoteControl") + "</button>", html += '<button is="emby-button" type="button" class="button-flat btnDisconnect promptDialogButton ">' + globalize.translate("sharedcomponents#Disconnect") + "</button>", html += '<button is="emby-button" type="button" class="button-flat btnCancel promptDialogButton">' + globalize.translate("sharedcomponents#ButtonCancel") + "</button>", html += "</div>", html += "</div>", dlg.innerHTML = html;
        var chkMirror = dlg.querySelector(".chkMirror");
        chkMirror && chkMirror.addEventListener("change", onMirrorChange);
        var destination = "",
            btnRemoteControl = dlg.querySelector(".btnRemoteControl");
        btnRemoteControl && btnRemoteControl.addEventListener("click", function() {
            destination = "nowplaying", dialogHelper.close(dlg)
        }), dlg.querySelector(".btnDisconnect").addEventListener("click", function() {
            destination = "disconnectFromPlayer", dialogHelper.close(dlg)
        }), dlg.querySelector(".btnCancel").addEventListener("click", function() {
            dialogHelper.close(dlg)
        }), dialogHelper.open(dlg).then(function() {
            "nowplaying" === destination ? appRouter.showNowPlaying() : "disconnectFromPlayer" === destination && disconnectFromPlayer(currentDeviceName)
        }, emptyCallback)
    }

    function onMirrorChange() {
        playbackManager.enableDisplayMirroring(this.checked)
    }
    return document.addEventListener("viewshow", function(e) {
        var state = e.detail.state || {},
            item = state.item;
        if (item && item.ServerId) return void mirrorIfEnabled({
            item: item
        })
    }), events.on(appSettings, "change", function(e, name) {
        "displaymirror" === name && mirrorIfEnabled()
    }), events.on(playbackManager, "pairing", function(e) {
        loading.show()
    }), events.on(playbackManager, "paired", function(e) {
        loading.hide()
    }), events.on(playbackManager, "pairerror", function(e) {
        loading.hide()
    }), {
        show: showPlayerSelection
    }
});