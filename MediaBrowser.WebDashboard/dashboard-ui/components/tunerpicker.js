define(["dialogHelper", "dom", "layoutManager", "connectionManager", "globalize", "loading", "material-icons", "formDialogStyle", "emby-button", "emby-itemscontainer", "cardStyle"], function(dialogHelper, dom, layoutManager, connectionManager, globalize, loading) {
    "use strict";

    function getEditorHtml() {
        var html = "";
        return html += '<div class="formDialogContent scrollY">', html += '<div class="dialogContentInner dialog-content-centered">', html += '<div class="loadingContent hide">', html += "<h1>" + globalize.translate("DetectingDevices") + "...</h1>", html += "<p>" + globalize.translate("MessagePleaseWait") + "</p>", html += "</div>", html += '<h1 style="margin-bottom:.25em;" class="devicesHeader hide">' + globalize.translate("HeaderNewDevices") + "</h1>", html += '<div is="emby-itemscontainer" class="results vertical-wrap">', html += "</div>", html += "</div>", html += "</div>"
    }

    function getDeviceHtml(device) {
        var padderClass, html = "",
            cssClass = "card scalableCard",
            cardBoxCssClass = "cardBox visualCardBox";
        return cssClass += " backdropCard backdropCard-scalable", padderClass = "cardPadder-backdrop", layoutManager.tv && (cssClass += " card-focusscale", cardBoxCssClass += " cardBox-focustransform"), cardBoxCssClass += " card-focuscontent", html += '<button type="button" class="' + cssClass + '" data-id="' + device.DeviceId + '" style="min-width:33.3333%;">', html += '<div class="' + cardBoxCssClass + '">', html += '<div class="cardScalable visualCardBox-cardScalable">', html += '<div class="' + padderClass + '"></div>', html += '<div class="cardContent searchImage">', html += '<div class="cardImageContainer coveredImage"><i class="cardImageIcon md-icon">dvr</i></div>', html += "</div>", html += "</div>", html += '<div class="cardFooter visualCardBox-cardFooter">', html += '<div class="cardText cardTextCentered">' + getTunerName(device.Type) + "</div>", html += '<div class="cardText cardTextCentered cardText-secondary">' + device.FriendlyName + "</div>", html += '<div class="cardText cardText-secondary cardTextCentered">', html += device.Url || "&nbsp;", html += "</div>", html += "</div>", html += "</div>", html += "</button>"
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

    function renderDevices(view, devices) {
        var i, length, html = "";
        for (i = 0, length = devices.length; i < length; i++) html += getDeviceHtml(devices[i]);
        devices.length ? view.querySelector(".devicesHeader").classList.remove("hide") : (html = "<p><br/>" + globalize.translate("NoNewDevicesFound") + "</p>", view.querySelector(".devicesHeader").classList.add("hide"));
        var elem = view.querySelector(".results");
        elem.innerHTML = html, layoutManager.tv && focusManager.autoFocus(elem)
    }

    function discoverDevices(view, apiClient) {
        return loading.show(), view.querySelector(".loadingContent").classList.remove("hide"), ApiClient.getJSON(ApiClient.getUrl("LiveTv/Tuners/Discvover", {
            NewDevicesOnly: !0
        })).then(function(devices) {
            currentDevices = devices, renderDevices(view, devices), view.querySelector(".loadingContent").classList.add("hide"), loading.hide()
        })
    }

    function tunerPicker() {
        this.show = function(options) {
            var dialogOptions = {
                removeOnClose: !0,
                scrollY: !1
            };
            layoutManager.tv ? dialogOptions.size = "fullscreen" : dialogOptions.size = "small";
            var dlg = dialogHelper.createDialog(dialogOptions);
            dlg.classList.add("formDialog");
            var html = "";
            html += '<div class="formDialogHeader">', html += '<button is="paper-icon-button-light" class="btnCancel autoSize" tabindex="-1"><i class="md-icon">&#xE5C4;</i></button>', html += '<h3 class="formDialogHeaderTitle">', html += globalize.translate("HeaderLiveTvTunerSetup"), html += "</h3>", html += "</div>", html += getEditorHtml(), dlg.innerHTML = html, dlg.querySelector(".btnCancel").addEventListener("click", function() {
                dialogHelper.close(dlg)
            });
            var deviceResult;
            dlg.querySelector(".results").addEventListener("click", function(e) {
                var tunerCard = dom.parentWithClass(e.target, "card");
                if (tunerCard) {
                    var deviceId = tunerCard.getAttribute("data-id");
                    deviceResult = currentDevices.filter(function(d) {
                        return d.DeviceId === deviceId
                    })[0], dialogHelper.close(dlg)
                }
            }), layoutManager.tv && centerFocus(dlg.querySelector(".formDialogContent"), !1, !0);
            var apiClient = connectionManager.getApiClient(options.serverId);
            return discoverDevices(dlg, apiClient), layoutManager.tv && centerFocus(dlg.querySelector(".formDialogContent"), !1, !1), dialogHelper.open(dlg).then(function() {
                return deviceResult ? Promise.resolve(deviceResult) : Promise.reject()
            })
        }
    }
    var currentDevices = [];
    return tunerPicker
});