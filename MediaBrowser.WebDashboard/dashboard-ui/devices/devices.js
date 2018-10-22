define(["loading", "dom", "libraryMenu", "globalize", "humanedate", "emby-linkbutton", "emby-itemscontainer", "cardStyle"], function(loading, dom, libraryMenu, globalize) {
    "use strict";

    function canDelete(deviceId) {
        return deviceId !== ApiClient.deviceId()
    }

    function deleteDevice(page, id) {
        var msg = globalize.translate("DeleteDeviceConfirmation");
        require(["confirm"], function(confirm) {
            confirm({
                text: msg,
                title: globalize.translate("HeaderDeleteDevice"),
                confirmText: globalize.translate("ButtonDelete"),
                primary: "cancel"
            }).then(function() {
                loading.show(), ApiClient.ajax({
                    type: "DELETE",
                    url: ApiClient.getUrl("Devices", {
                        Id: id
                    })
                }).then(function() {
                    loadData(page)
                })
            })
        })
    }

    function showDeviceMenu(view, btn, deviceId) {
        var menuItems = [];
        canEdit && menuItems.push({
            name: globalize.translate("sharedcomponents#Edit"),
            id: "open",
            ironIcon: "mode-edit"
        }), canDelete(deviceId) && menuItems.push({
            name: globalize.translate("sharedcomponents#Delete"),
            id: "delete",
            ironIcon: "delete"
        }), require(["actionsheet"], function(actionsheet) {
            actionsheet.show({
                items: menuItems,
                positionTo: btn,
                callback: function(id) {
                    switch (id) {
                        case "open":
                            Dashboard.navigate("devices/device.html?id=" + deviceId);
                            break;
                        case "delete":
                            deleteDevice(view, deviceId)
                    }
                }
            })
        })
    }

    function load(page, devices) {
        var html = "";
        html += devices.map(function(device) {
            var deviceHtml = "";
            deviceHtml += "<div data-id='" + device.Id + "' class='card backdropCard'>", deviceHtml += '<div class="cardBox visualCardBox">', deviceHtml += '<div class="cardScalable">', deviceHtml += '<div class="cardPadder cardPadder-backdrop"></div>', deviceHtml += '<a is="emby-linkbutton" href="' + (canEdit ? "devices/device.html?id=" + device.Id : "#") + '" class="cardContent cardImageContainer">';
            var iconUrl = device.IconUrl;
            return iconUrl && -1 === iconUrl.indexOf("://") && (iconUrl = ApiClient.getUrl(iconUrl)), iconUrl ? (deviceHtml += '<div class="cardImage" style="background-image:url(\'' + iconUrl + "');background-size: auto 64%;background-position:center center;\">", deviceHtml += "</div>") : deviceHtml += '<i class="cardImageIcon md-icon">tablet_android</i>', deviceHtml += "</a>", deviceHtml += "</div>", deviceHtml += '<div class="cardFooter">', (canEdit || canDelete(device.Id)) && (deviceHtml += '<div style="text-align:right; float:right;padding-top:5px;">', deviceHtml += '<button type="button" is="paper-icon-button-light" data-id="' + device.Id + '" title="' + globalize.translate("sharedcomponents#Menu") + '" class="btnDeviceMenu"><i class="md-icon">&#xE5D3;</i></button>', deviceHtml += "</div>"), deviceHtml += "<div class='cardText'>", deviceHtml += device.Name, deviceHtml += "</div>", deviceHtml += "<div class='cardText cardText-secondary'>", deviceHtml += device.AppName + " " + device.AppVersion, deviceHtml += "</div>", deviceHtml += "<div class='cardText cardText-secondary'>", device.LastUserName && (deviceHtml += device.LastUserName, deviceHtml += ", " + humane_date(device.DateLastActivity)), deviceHtml += "&nbsp;", deviceHtml += "</div>", deviceHtml += "</div>", deviceHtml += "</div>", deviceHtml += "</div>"
        }).join(""), page.querySelector(".devicesList").innerHTML = html
    }

    function loadData(page) {
        loading.show(), ApiClient.getJSON(ApiClient.getUrl("Devices")).then(function(result) {
            load(page, result.Items), loading.hide()
        })
    }
    var canEdit = ApiClient.isMinServerVersion("3.4.1.31");
    return function(view, params) {
        view.querySelector(".devicesList").addEventListener("click", function(e) {
            var btnDeviceMenu = dom.parentWithClass(e.target, "btnDeviceMenu");
            btnDeviceMenu && showDeviceMenu(view, btnDeviceMenu, btnDeviceMenu.getAttribute("data-id"))
        }), view.addEventListener("viewshow", function() {
            loadData(this)
        })
    }
});