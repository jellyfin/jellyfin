define(["loading", "libraryMenu", "globalize", "emby-checkbox", "emby-select"], function(loading, libraryMenu, globalize) {
    "use strict";

    function onSubmit(e) {
        var form = this,
            localAddress = form.querySelector("#txtLocalAddress").value,
            enableUpnp = form.querySelector("#chkEnableUpnp").checked;
        confirmSelections(localAddress, enableUpnp, function() {
            var validationResult = getValidationAlert(form);
            if (validationResult) return void alertText(validationResult);
            validateHttps(form).then(function() {
                loading.show(), ApiClient.getServerConfiguration().then(function(config) {
                    config.LocalNetworkSubnets = form.querySelector("#txtLanNetworks").value.split(",").map(function(s) {
                        return s.trim()
                    }).filter(function(s) {
                        return s.length > 0
                    }), config.RemoteIPFilter = form.querySelector("#txtExternalAddressFilter").value.split(",").map(function(s) {
                        return s.trim()
                    }).filter(function(s) {
                        return s.length > 0
                    }), config.IsRemoteIPFilterBlacklist = "blacklist" === form.querySelector("#selectExternalAddressFilterMode").value, config.PublicPort = form.querySelector("#txtPublicPort").value, config.PublicHttpsPort = form.querySelector("#txtPublicHttpsPort").value;
                    var httpsMode = form.querySelector("#selectHttpsMode").value;
                    "proxy" === httpsMode ? (config.EnableHttps = !0, config.RequireHttps = !1, config.IsBehindProxy = !0) : "required" === httpsMode ? (config.EnableHttps = !0, config.RequireHttps = !0, config.IsBehindProxy = !1) : "enabled" === httpsMode ? (config.EnableHttps = !0, config.RequireHttps = !1, config.IsBehindProxy = !1) : (config.EnableHttps = !1, config.RequireHttps = !1, config.IsBehindProxy = !1), config.HttpsPortNumber = form.querySelector("#txtHttpsPort").value, config.HttpServerPortNumber = form.querySelector("#txtPortNumber").value, config.EnableUPnP = enableUpnp, config.WanDdns = form.querySelector("#txtDdns").value, config.EnableRemoteAccess = form.querySelector("#chkRemoteAccess").checked, config.CertificatePath = form.querySelector("#txtCertificatePath").value || null, config.CertificatePassword = form.querySelector("#txtCertPassword").value || null, config.LocalNetworkAddresses = localAddress ? [localAddress] : [], ApiClient.updateServerConfiguration(config).then(Dashboard.processServerConfigurationUpdateResult, Dashboard.processErrorResponse)
                })
            })
        }), e.preventDefault()
    }

    function triggerChange(select) {
        var evt = document.createEvent("HTMLEvents");
        evt.initEvent("change", !1, !0), select.dispatchEvent(evt)
    }

    function getValidationAlert(form) {
        return form.querySelector("#txtPublicPort").value === form.querySelector("#txtPublicHttpsPort").value ? "The public http and https ports must be different." : form.querySelector("#txtPortNumber").value === form.querySelector("#txtHttpsPort").value ? "The http and https ports must be different." : null
    }

    function validateHttps(form) {
        var certPath = form.querySelector("#txtCertificatePath").value || null,
            httpsMode = form.querySelector("#selectHttpsMode").value;
        return "enabled" !== httpsMode && "required" !== httpsMode || certPath ? Promise.resolve() : new Promise(function(resolve, reject) {
            return alertText({
                title: globalize.translate("TitleHostingSettings"),
                text: globalize.translate("HttpsRequiresCert")
            }).then(reject, reject)
        })
    }

    function alertText(options) {
        return new Promise(function(resolve, reject) {
            require(["alert"], function(alert) {
                alert(options).then(resolve, reject)
            })
        })
    }

    function confirmSelections(localAddress, enableUpnp, callback) {
        localAddress || !enableUpnp ? alertText({
            title: globalize.translate("TitleHostingSettings"),
            text: globalize.translate("SettingsWarning")
        }).then(callback) : callback()
    }

    function getTabs() {
        return [{
            href: "dashboardhosting.html",
            name: globalize.translate("TabHosting")
        }, {
            href: "serversecurity.html",
            name: globalize.translate("TabSecurity")
        }]
    }
    return function(view, params) {
        function loadPage(page, config) {
            page.querySelector("#txtPortNumber").value = config.HttpServerPortNumber, page.querySelector("#txtPublicPort").value = config.PublicPort, page.querySelector("#txtPublicHttpsPort").value = config.PublicHttpsPort, page.querySelector("#txtLocalAddress").value = config.LocalNetworkAddresses[0] || "", page.querySelector("#txtLanNetworks").value = (config.LocalNetworkSubnets || []).join(", "), page.querySelector("#txtExternalAddressFilter").value = (config.RemoteIPFilter || []).join(", "), page.querySelector("#selectExternalAddressFilterMode").value = config.IsRemoteIPFilterBlacklist ? "blacklist" : "whitelist", page.querySelector("#chkRemoteAccess").checked = null == config.EnableRemoteAccess || config.EnableRemoteAccess;
            var selectHttpsMode = page.querySelector("#selectHttpsMode");
            config.IsBehindProxy ? selectHttpsMode.value = "proxy" : config.RequireHttps ? selectHttpsMode.value = "required" : config.EnableHttps ? selectHttpsMode.value = "enabled" : selectHttpsMode.value = "disabled", page.querySelector("#txtHttpsPort").value = config.HttpsPortNumber, page.querySelector("#txtDdns").value = config.WanDdns || "";
            var txtCertificatePath = page.querySelector("#txtCertificatePath");
            txtCertificatePath.value = config.CertificatePath || "", page.querySelector("#txtCertPassword").value = config.CertificatePassword || "", page.querySelector("#chkEnableUpnp").checked = config.EnableUPnP, onCertPathChange.call(txtCertificatePath), triggerChange(page.querySelector("#chkRemoteAccess")), loading.hide()
        }

        function onCertPathChange() {
            this.value ? view.querySelector("#txtDdns").setAttribute("required", "required") : view.querySelector("#txtDdns").removeAttribute("required")
        }
        view.querySelector("#chkRemoteAccess").addEventListener("change", function() {
            this.checked ? (view.querySelector(".fldExternalAddressFilter").classList.remove("hide"), view.querySelector(".fldExternalAddressFilterMode").classList.remove("hide"), view.querySelector(".fldPublicPort").classList.remove("hide"), view.querySelector(".fldPublicHttpsPort").classList.remove("hide"), view.querySelector(".fldDdns").classList.remove("hide"), view.querySelector(".fldCertificatePath").classList.remove("hide"), view.querySelector(".fldCertPassword").classList.remove("hide"), view.querySelector(".fldHttpsMode").classList.remove("hide"), view.querySelector(".fldEnableUpnp").classList.remove("hide")) : (view.querySelector(".fldExternalAddressFilter").classList.add("hide"), view.querySelector(".fldExternalAddressFilterMode").classList.add("hide"), view.querySelector(".fldPublicPort").classList.add("hide"), view.querySelector(".fldPublicHttpsPort").classList.add("hide"), view.querySelector(".fldDdns").classList.add("hide"), view.querySelector(".fldCertificatePath").classList.add("hide"), view.querySelector(".fldCertPassword").classList.add("hide"), view.querySelector(".fldHttpsMode").classList.add("hide"), view.querySelector(".fldEnableUpnp").classList.add("hide"))
        }), view.querySelector("#btnSelectCertPath").addEventListener("click", function() {
            require(["directorybrowser"], function(directoryBrowser) {
                var picker = new directoryBrowser;
                picker.show({
                    includeFiles: !0,
                    includeDirectories: !0,
                    callback: function(path) {
                        path && (view.querySelector("#txtCertificatePath").value = path), picker.close()
                    },
                    header: globalize.translate("HeaderSelectCertificatePath")
                })
            })
        }), view.querySelector(".dashboardHostingForm").addEventListener("submit", onSubmit), view.querySelector("#txtCertificatePath").addEventListener("change", onCertPathChange), view.addEventListener("viewshow", function(e) {
            libraryMenu.setTabs("adminadvanced", 0, getTabs), loading.show(), ApiClient.getServerConfiguration().then(function(config) {
                loadPage(view, config)
            })
        })
    }
});