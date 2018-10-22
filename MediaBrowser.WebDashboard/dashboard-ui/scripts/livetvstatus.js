define(["jQuery", "globalize", "scripts/taskbutton", "dom", "libraryMenu", "layoutManager", "loading", "listViewStyle", "flexStyles", "emby-itemscontainer", "cardStyle", "material-icons", "emby-linkbutton"], function($, globalize, taskButton, dom, libraryMenu, layoutManager, loading) {
    "use strict";

    function getDeviceHtml(device) {
        var padderClass, html = "",
            cssClass = "card scalableCard",
            cardBoxCssClass = "cardBox visualCardBox";
        return cssClass += " backdropCard backdropCard-scalable", padderClass = "cardPadder-backdrop", layoutManager.tv && (cssClass += " card-focusscale", cardBoxCssClass += " cardBox-focustransform"), cardBoxCssClass += " card-focuscontent", html += '<div type="button" class="' + cssClass + '" data-id="' + device.Id + '">', html += '<div class="' + cardBoxCssClass + '">', html += '<div class="cardScalable visualCardBox-cardScalable">', html += '<div class="' + padderClass + '"></div>', html += '<div class="cardContent searchImage">', html += '<div class="cardImageContainer coveredImage"><i class="cardImageIcon md-icon">dvr</i></div>', html += "</div>", html += "</div>", html += '<div class="cardFooter visualCardBox-cardFooter">', html += '<button is="paper-icon-button-light" class="itemAction btnCardOptions autoSize" data-action="menu"><i class="md-icon">more_horiz</i></button>', html += '<div class="cardText">' + (device.FriendlyName || getTunerName(device.Type)) + "</div>", html += '<div class="cardText cardText-secondary">', html += device.Url || "&nbsp;", html += "</div>", html += "</div>", html += "</div>", html += "</div>"
    }

    function renderDevices(page, devices) {
        var html = devices.map(getDeviceHtml).join("");
        page.querySelector(".devicesList").innerHTML = html
    }

    function deleteDevice(page, id) {
        var message = globalize.translate("MessageConfirmDeleteTunerDevice");
        require(["confirm"], function(confirm) {
            confirm(message, globalize.translate("HeaderDeleteDevice")).then(function() {
                loading.show(), ApiClient.ajax({
                    type: "DELETE",
                    url: ApiClient.getUrl("LiveTv/TunerHosts", {
                        Id: id
                    })
                }).then(function() {
                    reload(page)
                })
            })
        })
    }

    function reload(page) {
        loading.show(), ApiClient.getNamedConfiguration("livetv").then(function(config) {
            renderDevices(page, config.TunerHosts), renderProviders(page, config.ListingProviders)
        }), loading.hide()
    }

    function submitAddDeviceForm(page) {
        page.querySelector(".dlgAddDevice").close(), loading.show(), ApiClient.ajax({
            type: "POST",
            url: ApiClient.getUrl("LiveTv/TunerHosts"),
            data: JSON.stringify({
                Type: $("#selectTunerDeviceType", page).val(),
                Url: $("#txtDevicePath", page).val()
            }),
            contentType: "application/json"
        }).then(function() {
            reload(page)
        }, function() {
            Dashboard.alert({
                message: globalize.translate("ErrorAddingTunerDevice")
            })
        })
    }

    function renderProviders(page, providers) {
        var html = "";
        if (providers.length) {
            html += '<div class="paperList">';
            for (var i = 0, length = providers.length; i < length; i++) {
                var provider = providers[i];
                html += '<div class="listItem">', html += '<i class="listItemIcon md-icon">dvr</i>', html += '<div class="listItemBody two-line">', html += '<a is="emby-linkbutton" style="display:block;padding:0;margin:0;text-align:left;" class="clearLink" href="' + getProviderConfigurationUrl(provider.Type) + "&id=" + provider.Id + '">', html += '<h3 class="listItemBodyText">', html += getProviderName(provider.Type), html += "</h3>", html += '<div class="listItemBodyText secondary">', html += provider.Path || provider.ListingsId || "", html += "</div>", html += "</a>", html += "</div>", html += '<button type="button" is="paper-icon-button-light" class="btnOptions" data-id="' + provider.Id + '"><i class="md-icon listItemAside">more_horiz</i></button>', html += "</div>"
            }
            html += "</div>"
        }
        var elem = $(".providerList", page).html(html);
        $(".btnOptions", elem).on("click", function() {
            var id = this.getAttribute("data-id");
            showProviderOptions(page, id, this)
        })
    }

    function showProviderOptions(page, providerId, button) {
        var items = [];
        items.push({
            name: globalize.translate("ButtonDelete"),
            id: "delete"
        }), items.push({
            name: globalize.translate("MapChannels"),
            id: "map"
        }), require(["actionsheet"], function(actionsheet) {
            actionsheet.show({
                items: items,
                positionTo: button
            }).then(function(id) {
                switch (id) {
                    case "delete":
                        deleteProvider(page, providerId);
                        break;
                    case "map":
                        mapChannels(page, providerId)
                }
            })
        })
    }

    function mapChannels(page, providerId) {
        require(["components/channelmapper/channelmapper"], function(channelmapper) {
            new channelmapper({
                serverId: ApiClient.serverInfo().Id,
                providerId: providerId
            }).show()
        })
    }

    function deleteProvider(page, id) {
        var message = globalize.translate("MessageConfirmDeleteGuideProvider");
        require(["confirm"], function(confirm) {
            confirm(message, globalize.translate("HeaderDeleteProvider")).then(function() {
                loading.show(), ApiClient.ajax({
                    type: "DELETE",
                    url: ApiClient.getUrl("LiveTv/ListingProviders", {
                        Id: id
                    })
                }).then(function() {
                    reload(page)
                }, function() {
                    reload(page)
                })
            })
        })
    }

    function getTunerName(providerId) {
        switch (providerId = providerId.toLowerCase()) {
            case "m3u":
                return "M3U";
            case "hdhomerun":
                return "HDHomerun";
            case "hauppauge":
                return "Hauppauge";
            case "satip":
                return "DVB";
            default:
                return "Unknown"
        }
    }

    function getProviderName(providerId) {
        switch (providerId = providerId.toLowerCase()) {
            case "schedulesdirect":
                return "Schedules Direct";
            case "xmltv":
                return "Xml TV";
            case "emby":
                return "Emby Guide";
            default:
                return "Unknown"
        }
    }

    function getProviderConfigurationUrl(providerId) {
        switch (providerId = providerId.toLowerCase()) {
            case "xmltv":
                return "livetvguideprovider.html?type=xmltv";
            case "schedulesdirect":
                return "livetvguideprovider.html?type=schedulesdirect";
            case "emby":
                return "livetvguideprovider.html?type=emby"
        }
    }

    function addProvider(button) {
        var menuItems = [];
        menuItems.push({
            name: "Schedules Direct",
            id: "SchedulesDirect"
        }), menuItems.push({
            name: "Xml TV",
            id: "xmltv"
        }), menuItems.push({
            name: globalize.translate("ButtonOther"),
            id: "other"
        }), require(["actionsheet"], function(actionsheet) {
            actionsheet.show({
                items: menuItems,
                positionTo: button,
                callback: function(id) {
                    "other" == id ? Dashboard.alert({
                        message: globalize.translate("ForAdditionalLiveTvOptions")
                    }) : Dashboard.navigate(getProviderConfigurationUrl(id))
                }
            })
        })
    }

    function addDevice(button) {
        Dashboard.navigate("livetvtuner.html")
    }

    function getTabs() {
        return [{
            href: "livetvstatus.html",
            name: globalize.translate("TabDevices")
        }, {
            href: "appservices.html?context=livetv",
            name: globalize.translate("TabServices")
        }]
    }

    function showDeviceMenu(button, tunerDeviceId) {
        var items = [];
        items.push({
            name: globalize.translate("ButtonDelete"),
            id: "delete"
        }), items.push({
            name: globalize.translate("ButtonEdit"),
            id: "edit"
        }), require(["actionsheet"], function(actionsheet) {
            actionsheet.show({
                items: items,
                positionTo: button
            }).then(function(id) {
                switch (id) {
                    case "delete":
                        deleteDevice(dom.parentWithClass(button, "page"), tunerDeviceId);
                        break;
                    case "edit":
                        Dashboard.navigate("livetvtuner.html?id=" + tunerDeviceId)
                }
            })
        })
    }

    function onDevicesListClick(e) {
        var card = dom.parentWithClass(e.target, "card");
        if (card) {
            var id = card.getAttribute("data-id"),
                btnCardOptions = dom.parentWithClass(e.target, "btnCardOptions");
            btnCardOptions ? showDeviceMenu(btnCardOptions, id) : Dashboard.navigate("livetvtuner.html?id=" + id)
        }
    }
    $(document).on("pageinit", "#liveTvStatusPage", function() {
        var page = this;
        $(".btnAddDevice", page).on("click", function() {
            addDevice(this)
        }), $(".formAddDevice", page).on("submit", function() {
            return submitAddDeviceForm(page), !1
        }), $(".btnAddProvider", page).on("click", function() {
            addProvider(this)
        }), page.querySelector(".devicesList").addEventListener("click", onDevicesListClick)
    }).on("pageshow", "#liveTvStatusPage", function() {
        libraryMenu.setTabs("livetvadmin", 0, getTabs);
        var page = this;
        reload(page), taskButton({
            mode: "on",
            progressElem: page.querySelector(".refreshGuideProgress"),
            taskKey: "RefreshGuide",
            button: page.querySelector(".btnRefresh")
        })
    }).on("pagehide", "#liveTvStatusPage", function() {
        var page = this;
        taskButton({
            mode: "off",
            progressElem: page.querySelector(".refreshGuideProgress"),
            taskKey: "RefreshGuide",
            button: page.querySelector(".btnRefresh")
        })
    })
});