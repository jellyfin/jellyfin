define(["loading", "appRouter", "layoutManager", "appSettings", "apphost", "focusManager", "connectionManager", "backdrop", "globalize", "staticBackdrops", "actionsheet", "dom", "material-icons", "flexStyles", "emby-scroller", "emby-itemscontainer", "cardStyle", "emby-button"], function(loading, appRouter, layoutManager, appSettings, appHost, focusManager, connectionManager, backdrop, globalize, staticBackdrops, actionSheet, dom) {
    "use strict";

    function renderSelectServerItems(view, servers) {
        var items = servers.map(function(server) {
                return {
                    name: server.Name,
                    showIcon: !0,
                    icon: "&#xE307;",
                    cardType: "",
                    id: server.Id,
                    server: server
                }
            }),
            html = items.map(function(item) {
                var cardImageContainer;
                cardImageContainer = item.showIcon ? '<i class="cardImageIcon md-icon">' + item.icon + "</i>" : '<div class="cardImage" style="' + item.cardImageStyle + '"></div>';
                var cardBoxCssClass = "cardBox";
                layoutManager.tv && (cardBoxCssClass += " cardBox-focustransform");
                var innerOpening = '<div class="' + cardBoxCssClass + '">';
                return '<button raised class="card overflowSquareCard loginSquareCard scalableCard overflowSquareCard-scalable" style="display:inline-block;" data-id="' + item.id + '" data-url="' + (item.url || "") + '" data-cardtype="' + item.cardType + '">' + innerOpening + '<div class="cardScalable card-focuscontent"><div class="cardPadder cardPadder-square"></div><div class="cardContent"><div class="cardImageContainer coveredImage" style="background:#0288D1;border-radius:.15em;">' + cardImageContainer + '</div></div></div><div class="cardFooter"><div class="cardText cardTextCentered">' + item.name + "</div></div></div></button>"
            }).join(""),
            itemsContainer = view.querySelector(".servers");
        items.length || (html = "<p>" + globalize.translate("sharedcomponents#MessageNoServersAvailableToConnect") + "</p>"), itemsContainer.innerHTML = html, loading.hide()
    }

    function updatePageStyle(view, params) {
        "1" == params.showuser ? (view.classList.add("libraryPage"), view.classList.remove("standalonePage"), view.classList.add("noSecondaryNavPage")) : (view.classList.add("standalonePage"), view.classList.remove("libraryPage"), view.classList.remove("noSecondaryNavPage"))
    }

    function showGeneralError() {
        loading.hide(), alertText(globalize.translate("sharedcomponents#DefaultErrorMessage"))
    }

    function alertText(text) {
        alertTextWithOptions({
            text: text
        })
    }

    function alertTextWithOptions(options) {
        require(["alert"], function(alert) {
            alert(options)
        })
    }

    function showServerConnectionFailure() {
        alertText(globalize.translate("MessageUnableToConnectToServer"), globalize.translate("HeaderConnectionFailure"))
    }
    return function(view, params) {
        function connectToServer(server) {
            loading.show(), connectionManager.connectToServer(server, {
                enableAutoLogin: appSettings.enableAutoLogin()
            }).then(function(result) {
                loading.hide();
                var apiClient = result.ApiClient;
                switch (result.State) {
                    case "SignedIn":
                        Dashboard.onServerChanged(apiClient.getCurrentUserId(), apiClient.accessToken(), apiClient), Dashboard.navigate("home.html");
                        break;
                    case "ServerSignIn":
                        Dashboard.onServerChanged(null, null, apiClient), Dashboard.navigate("login.html?serverid=" + result.Servers[0].Id);
                        break;
                    case "ServerUpdateNeeded":
                        alertTextWithOptions({
                            text: globalize.translate("core#ServerUpdateNeeded", "https://github.com/jellyfin/jellyfin"),
                            html: globalize.translate("core#ServerUpdateNeeded", '<a href="https://github.com/jellyfin/jellyfin">https://github.com/jellyfin/jellyfin</a>')
                        });
                        break;
                    default:
                        showServerConnectionFailure()
                }
            })
        }

        function deleteServer(server) {
            loading.show(), connectionManager.deleteServer(server.Id).then(function() {
                loading.hide(), loadServers()
            }, function() {
                loading.hide(), loadServers()
            })
        }

        function acceptInvitation(id) {
            loading.show(), connectionManager.acceptServer(id).then(function() {
                loading.hide(), loadServers(), loadInvitations()
            }, showGeneralError)
        }

        function rejectInvitation(id) {
            loading.show(), connectionManager.rejectServer(id).then(function() {
                loading.hide(), loadServers(), loadInvitations()
            }, showGeneralError)
        }

        function showPendingInviteMenu(elem) {
            var card = dom.parentWithClass(elem, "inviteItem"),
                invitationId = card.getAttribute("data-id"),
                menuItems = [];
            menuItems.push({
                name: globalize.translate("sharedcomponents#Accept"),
                id: "accept"
            }), menuItems.push({
                name: globalize.translate("sharedcomponents#Reject"),
                id: "reject"
            }), require(["actionsheet"], function(actionsheet) {
                actionsheet.show({
                    items: menuItems,
                    positionTo: elem,
                    callback: function(id) {
                        switch (id) {
                            case "accept":
                                acceptInvitation(invitationId);
                                break;
                            case "reject":
                                rejectInvitation(invitationId)
                        }
                    }
                })
            })
        }

        function getPendingInviteHtml(item) {
            var cardBoxCssClass = "cardBox";
            layoutManager.tv && (cardBoxCssClass += " cardBox-focustransform");
            var innerOpening = '<div class="' + cardBoxCssClass + '">';
            return '<button raised class="card overflowSquareCard loginSquareCard scalableCard overflowSquareCard-scalable btnInviteMenu inviteItem" style="display:inline-block;" data-id="' + item.Id + '">' + innerOpening + '<div class="cardScalable card-focuscontent"><div class="cardPadder cardPadder-square"></div><div class="cardContent"><div class="cardImageContainer coveredImage" style="background:#0288D1;border-radius:.15em;"><i class="cardImageIcon md-icon">&#xE1BA;</i></div></div></div><div class="cardFooter"><div class="cardText cardTextCentered">' + item.Name + "</div></div></div></button>"
        }

        function renderInvitations(list) {
            list.length ? view.querySelector(".invitationSection").classList.remove("hide") : view.querySelector(".invitationSection").classList.add("hide");
            var html = list.map(getPendingInviteHtml).join("");
            view.querySelector(".invitations").innerHTML = html
        }

        function loadInvitations() {
            connectionManager.isLoggedIntoConnect() ? connectionManager.getUserInvitations().then(renderInvitations) : renderInvitations([])
        }

        function onServerClick(server) {
            var menuItems = [];
            menuItems.push({
                name: globalize.translate("sharedcomponents#Connect"),
                id: "connect"
            }), menuItems.push({
                name: globalize.translate("sharedcomponents#Delete"),
                id: "delete"
            });
            var apiClient = connectionManager.getApiClient(server.Id);
            apiClient && apiClient.supportsWakeOnLan() && menuItems.push({
                name: globalize.translate("sharedcomponents#WakeServer"),
                id: "wol"
            }), actionSheet.show({
                items: menuItems,
                title: server.Name
            }).then(function(id) {
                switch (id) {
                    case "connect":
                        connectToServer(server);
                        break;
                    case "delete":
                        deleteServer(server);
                        break;
                    case "wol":
                        sendWolPacket(server)
                }
            })
        }

        function sendWolPacket(server) {
            var apiClient = connectionManager.getApiClient(server.Id);
            require(["loadingDialog"], function(LoadingDialog) {
                var dlg = new LoadingDialog({
                    title: globalize.translate("sharedcomponents#HeaderWakeServer"),
                    text: globalize.translate("sharedcomponents#AttemptingWakeServer")
                });
                dlg.show();
                var afterWol = function() {
                    setTimeout(function() {
                        apiClient.getPublicSystemInfo().then(onWolSuccess.bind(dlg), onWolFail.bind(dlg))
                    }, 12e3)
                };
                apiClient.wakeOnLan().then(afterWol, afterWol)
            })
        }

        function onWolSuccess() {
            var dlg = this;
            dlg.hide(), dlg.destroy(), require(["alert"], function(alert) {
                alert({
                    text: globalize.translate("sharedcomponents#WakeServerSuccess"),
                    title: globalize.translate("sharedcomponents#HeaderWakeServer")
                })
            })
        }

        function onWolFail() {
            var dlg = this;
            dlg.hide(), dlg.destroy(), require(["alert"], function(alert) {
                alert({
                    text: globalize.translate("sharedcomponents#WakeServerError"),
                    title: globalize.translate("sharedcomponents#HeaderWakeServer")
                })
            })
        }

        function onServersRetrieved(result) {
            servers = result, renderSelectServerItems(view, result), layoutManager.tv && focusManager.autoFocus(view)
        }

        function loadServers() {
            loading.show(), connectionManager.getAvailableServers().then(onServersRetrieved, function(result) {
                onServersRetrieved([])
            })
        }
        var servers;
        layoutManager.desktop;
        (function() {
            updatePageStyle(view, params), view.querySelector(".btnOfflineText").innerHTML = globalize.translate("sharedcomponents#HeaderMyDownloads"), appHost.supports("sync") && view.querySelector(".btnOffline").classList.remove("hide")
        })();
        var backdropUrl = staticBackdrops.getRandomImageUrl();
        view.addEventListener("viewshow", function(e) {
            var isRestored = e.detail.isRestored;
            appRouter.setTitle(null), backdrop.setBackdrop(backdropUrl), isRestored || (loadServers(), loadInvitations())
        }), view.querySelector(".btnOffline").addEventListener("click", function(e) {
            appRouter.show("/offline/offline.html")
        }), view.querySelector(".servers").addEventListener("click", function(e) {
            var card = dom.parentWithClass(e.target, "card");
            if (card) {
                var url = card.getAttribute("data-url");
                if (url) appRouter.show(url);
                else {
                    var id = card.getAttribute("data-id");
                    onServerClick(servers.filter(function(s) {
                        return s.Id === id
                    })[0])
                }
            }
        }), view.querySelector(".invitations").addEventListener("click", function(e) {
            var btnInviteMenu = dom.parentWithClass(e.target, "btnInviteMenu");
            btnInviteMenu && showPendingInviteMenu(btnInviteMenu)
        })
    }
});
