define(["globalize", "loading", "libraryMenu", "dom", "emby-input", "emby-button", "emby-checkbox", "emby-select"], function(globalize, loading, libraryMenu, dom) {
    "use strict";

    function isM3uVariant(type) {
        return -1 !== ["nextpvr"].indexOf(type || "")
    }

    function fillTypes(view, currentId) {
        return ApiClient.getJSON(ApiClient.getUrl("LiveTv/TunerHosts/Types")).then(function(types) {
            var selectType = view.querySelector(".selectType");
            selectType.innerHTML = types.map(function(t) {
                return '<option value="' + t.Id + '">' + t.Name + "</option>"
            }).join("") + '<option value="other">' + globalize.translate("TabOther") + "</option>", selectType.disabled = null != currentId, selectType.value = "", onTypeChange.call(selectType)
        })
    }

    function reload(view, providerId) {
        view.querySelector(".txtDevicePath").value = "", view.querySelector(".chkFavorite").checked = !1, view.querySelector(".txtDevicePath").value = "", providerId && ApiClient.getNamedConfiguration("livetv").then(function(config) {
            var info = config.TunerHosts.filter(function(i) {
                return i.Id === providerId
            })[0];
            fillTunerHostInfo(view, info)
        })
    }

    function fillTunerHostInfo(view, info) {
        var selectType = view.querySelector(".selectType"),
            type = info.Type || "";
        info.Source && isM3uVariant(info.Source) && (type = info.Source), selectType.value = type, onTypeChange.call(selectType), view.querySelector(".txtDevicePath").value = info.Url || "", view.querySelector(".txtFriendlyName").value = info.FriendlyName || "", view.querySelector(".txtUserAgent").value = info.UserAgent || "", view.querySelector(".fldDeviceId").value = info.DeviceId || "", view.querySelector(".chkFavorite").checked = info.ImportFavoritesOnly, view.querySelector(".chkTranscode").checked = info.AllowHWTranscoding, view.querySelector(".chkStreamLoop").checked = info.EnableStreamLooping, view.querySelector(".txtTunerCount").value = info.TunerCount || "0"
    }

    function submitForm(page) {
        loading.show();
        var info = {
            Type: page.querySelector(".selectType").value,
            Url: page.querySelector(".txtDevicePath").value || null,
            UserAgent: page.querySelector(".txtUserAgent").value || null,
            FriendlyName: page.querySelector(".txtFriendlyName").value || null,
            DeviceId: page.querySelector(".fldDeviceId").value || null,
            TunerCount: page.querySelector(".txtTunerCount").value || 0,
            ImportFavoritesOnly: page.querySelector(".chkFavorite").checked,
            AllowHWTranscoding: page.querySelector(".chkTranscode").checked,
            EnableStreamLooping: page.querySelector(".chkStreamLoop").checked
        };
        isM3uVariant(info.Type) && (info.Source = info.Type, info.Type = "m3u");
        var id = getParameterByName("id");
        id && (info.Id = id);
        info.Id;
        ApiClient.ajax({
            type: "POST",
            url: ApiClient.getUrl("LiveTv/TunerHosts"),
            data: JSON.stringify(info),
            contentType: "application/json"
        }).then(function(result) {
            Dashboard.processServerConfigurationUpdateResult(), Dashboard.navigate("livetvstatus.html")
        }, function() {
            loading.hide(), Dashboard.alert({
                message: globalize.translate("ErrorSavingTvProvider")
            })
        })
    }

    function getRequirePromise(deps) {
        return new Promise(function(resolve, reject) {
            require(deps, resolve)
        })
    }

    function getDetectedDevice() {
        return getRequirePromise(["tunerPicker"]).then(function(tunerPicker) {
            return (new tunerPicker).show({
                serverId: ApiClient.serverId()
            })
        })
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

    function onTypeChange() {
        var value = this.value,
            view = dom.parentWithClass(this, "page"),
            mayIncludeUnsupportedDrmChannels = "hdhomerun" === value,
            supportsTranscoding = "hdhomerun" === value,
            supportsFavorites = "hdhomerun" === value,
            supportsTunerIpAddress = "hdhomerun" === value,
            supportsTunerFileOrUrl = "m3u" === value,
            supportsStreamLooping = "m3u" === value,
            supportsTunerCount = "m3u" === value,
            supportsUserAgent = "m3u" === value,
            suppportsSubmit = "other" !== value,
            supportsSelectablePath = supportsTunerFileOrUrl,
            txtDevicePath = view.querySelector(".txtDevicePath");
        supportsTunerIpAddress ? (txtDevicePath.label(globalize.translate("LabelTunerIpAddress")), view.querySelector(".fldPath").classList.remove("hide")) : supportsTunerFileOrUrl ? (txtDevicePath.label(globalize.translate("LabelFileOrUrl")), view.querySelector(".fldPath").classList.remove("hide")) : view.querySelector(".fldPath").classList.add("hide"), supportsSelectablePath ? (view.querySelector(".btnSelectPath").classList.remove("hide"), view.querySelector(".txtDevicePath").setAttribute("required", "required")) : (view.querySelector(".btnSelectPath").classList.add("hide"), view.querySelector(".txtDevicePath").removeAttribute("required")), supportsUserAgent ? view.querySelector(".fldUserAgent").classList.remove("hide") : view.querySelector(".fldUserAgent").classList.add("hide"), supportsFavorites ? view.querySelector(".fldFavorites").classList.remove("hide") : view.querySelector(".fldFavorites").classList.add("hide"), supportsTranscoding ? view.querySelector(".fldTranscode").classList.remove("hide") : view.querySelector(".fldTranscode").classList.add("hide"), supportsStreamLooping ? view.querySelector(".fldStreamLoop").classList.remove("hide") : view.querySelector(".fldStreamLoop").classList.add("hide"), supportsTunerCount ? (view.querySelector(".fldTunerCount").classList.remove("hide"), view.querySelector(".txtTunerCount").setAttribute("required", "required")) : (view.querySelector(".fldTunerCount").classList.add("hide"), view.querySelector(".txtTunerCount").removeAttribute("required")), mayIncludeUnsupportedDrmChannels ? view.querySelector(".drmMessage").classList.remove("hide") : view.querySelector(".drmMessage").classList.add("hide"), suppportsSubmit ? (view.querySelector(".button-submit").classList.remove("hide"), view.querySelector(".otherOptionsMessage").classList.add("hide")) : (view.querySelector(".button-submit").classList.add("hide"), view.querySelector(".otherOptionsMessage").classList.remove("hide"))
    }
    return function(view, params) {
        params.id || view.querySelector(".btnDetect").classList.remove("hide"), view.addEventListener("viewshow", function() {
            libraryMenu.setTabs("livetvadmin", 0, getTabs);
            var currentId = params.id;
            fillTypes(view, currentId).then(function() {
                reload(view, currentId)
            })
        }), view.querySelector("form").addEventListener("submit", function(e) {
            return submitForm(view), e.preventDefault(), e.stopPropagation(), !1
        }), view.querySelector(".selectType").addEventListener("change", onTypeChange), view.querySelector(".btnDetect").addEventListener("click", function() {
            getDetectedDevice().then(function(info) {
                fillTunerHostInfo(view, info)
            })
        }), view.querySelector(".btnSelectPath").addEventListener("click", function() {
            require(["directorybrowser"], function(directoryBrowser) {
                var picker = new directoryBrowser;
                picker.show({
                    includeFiles: !0,
                    callback: function(path) {
                        path && (view.querySelector(".txtDevicePath").value = path), picker.close()
                    }
                })
            })
        })
    }
});