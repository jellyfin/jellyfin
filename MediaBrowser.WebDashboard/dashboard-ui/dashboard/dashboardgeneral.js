define(["jQuery", "loading", "fnchecked", "emby-checkbox", "emby-textarea", "emby-input", "emby-select", "emby-linkbutton"], function($, loading) {
    "use strict";

    function loadPage(page, config, languageOptions, systemInfo) {
        systemInfo.CanLaunchWebBrowser ? page.querySelector("#fldRunWebAppAtStartup").classList.remove("hide") : page.querySelector("#fldRunWebAppAtStartup").classList.add("hide"), page.querySelector("#txtCachePath").value = config.CachePath || "", $("#selectLocalizationLanguage", page).html(languageOptions.map(function(l) {
            return '<option value="' + l.Value + '">' + l.Name + "</option>"
        })).val(config.UICulture), currentLanguage = config.UICulture, systemInfo.CanSelfUpdate ? page.querySelector(".fldAutomaticUpdates").classList.remove("hide") : page.querySelector(".fldAutomaticUpdates").classList.add("hide"), $("#chkEnableAutomaticServerUpdates", page).checked(config.EnableAutoUpdate), $("#chkEnableAutomaticRestart", page).checked(config.EnableAutomaticRestart), systemInfo.CanSelfRestart ? page.querySelector("#fldEnableAutomaticRestart").classList.remove("hide") : page.querySelector("#fldEnableAutomaticRestart").classList.add("hide"), systemInfo.CanSelfRestart || systemInfo.CanSelfUpdate ? $(".autoUpdatesContainer", page).removeClass("hide") : $(".autoUpdatesContainer", page).addClass("hide"), loading.hide()
    }

    function onSubmit() {
        loading.show();
        var form = this;
        $(form).parents(".page");
        return ApiClient.getServerConfiguration().then(function(config) {
            config.UICulture = $("#selectLocalizationLanguage", form).val(), config.CachePath = form.querySelector("#txtCachePath").value;
            var requiresReload = !1;
            config.UICulture !== currentLanguage && (requiresReload = !0), config.EnableAutomaticRestart = $("#chkEnableAutomaticRestart", form).checked(), config.EnableAutoUpdate = $("#chkEnableAutomaticServerUpdates", form).checked(), ApiClient.updateServerConfiguration(config).then(function() {
                ApiClient.getNamedConfiguration(brandingConfigKey).then(function(brandingConfig) {
                    brandingConfig.LoginDisclaimer = form.querySelector("#txtLoginDisclaimer").value, brandingConfig.CustomCss = form.querySelector("#txtCustomCss").value, currentBrandingOptions && brandingConfig.CustomCss !== currentBrandingOptions.CustomCss && (requiresReload = !0), ApiClient.updateNamedConfiguration(brandingConfigKey, brandingConfig).then(function() {
                        Dashboard.processServerConfigurationUpdateResult(), requiresReload && !AppInfo.isNativeApp && window.location.reload(!0)
                    })
                })
            })
        }), !1
    }
    var currentBrandingOptions, currentLanguage, brandingConfigKey = "branding";
    return function(view, params) {
        $("#btnSelectCachePath", view).on("click.selectDirectory", function() {
            require(["directorybrowser"], function(directoryBrowser) {
                var picker = new directoryBrowser;
                picker.show({
                    callback: function(path) {
                        path && (view.querySelector("#txtCachePath").value = path), picker.close()
                    },
                    validateWriteable: !0,
                    header: Globalize.translate("HeaderSelectServerCachePath"),
                    instruction: Globalize.translate("HeaderSelectServerCachePathHelp")
                })
            })
        }), $(".dashboardGeneralForm", view).off("submit", onSubmit).on("submit", onSubmit), view.addEventListener("viewshow", function() {
            var promise1 = ApiClient.getServerConfiguration(),
                promise2 = ApiClient.getJSON(ApiClient.getUrl("Localization/Options")),
                promise3 = ApiClient.getSystemInfo();
            Promise.all([promise1, promise2, promise3]).then(function(responses) {
                loadPage(view, responses[0], responses[1], responses[2])
            }), ApiClient.getNamedConfiguration(brandingConfigKey).then(function(config) {
                currentBrandingOptions = config, view.querySelector("#txtLoginDisclaimer").value = config.LoginDisclaimer || "", view.querySelector("#txtCustomCss").value = config.CustomCss || ""
            })
        })
    }
});
