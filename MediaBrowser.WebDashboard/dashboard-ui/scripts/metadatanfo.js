define(["jQuery", "loading", "libraryMenu"], function($, loading, libraryMenu) {
    "use strict";

    function loadPage(page, config, users) {
        var html = '<option value="" selected="selected">' + Globalize.translate("OptionNone") + "</option>";
        html += users.map(function(user) {
            return '<option value="' + user.Id + '">' + user.Name + "</option>"
        }).join(""), $("#selectUser", page).html(html).val(config.UserId || ""), $("#selectReleaseDateFormat", page).val(config.ReleaseDateFormat), page.querySelector("#chkSaveImagePaths").checked = config.SaveImagePathsInNfo, page.querySelector("#chkEnablePathSubstitution").checked = config.EnablePathSubstitution, page.querySelector("#chkEnableExtraThumbs").checked = config.EnableExtraThumbsDuplication, loading.hide()
    }

    function onSubmit() {
        loading.show();
        var form = this;
        return ApiClient.getNamedConfiguration(metadataKey).then(function(config) {
            config.UserId = $("#selectUser", form).val() || null, config.ReleaseDateFormat = $("#selectReleaseDateFormat", form).val(), config.SaveImagePathsInNfo = form.querySelector("#chkSaveImagePaths").checked, config.EnablePathSubstitution = form.querySelector("#chkEnablePathSubstitution").checked, config.EnableExtraThumbsDuplication = form.querySelector("#chkEnableExtraThumbs").checked, ApiClient.updateNamedConfiguration(metadataKey, config).then(function() {
                Dashboard.processServerConfigurationUpdateResult(), showConfirmMessage(config)
            })
        }), !1
    }

    function showConfirmMessage(config) {
        var msg = [];
        msg.push(Globalize.translate("MetadataSettingChangeHelp")), require(["alert"], function(alert) {
            alert({
                text: msg.join("<br/><br/>")
            })
        })
    }

    function getTabs() {
        return [{
            href: "library.html",
            name: Globalize.translate("HeaderLibraries")
        }, {
            href: "librarydisplay.html",
            name: Globalize.translate("TabDisplay")
        }, {
            href: "metadataimages.html",
            name: Globalize.translate("TabMetadata")
        }, {
            href: "metadatanfo.html",
            name: Globalize.translate("TabNfoSettings")
        }, {
            href: "librarysettings.html",
            name: Globalize.translate("TabAdvanced")
        }]
    }
    var metadataKey = "xbmcmetadata";
    $(document).on("pageinit", "#metadataNfoPage", function() {
        $(".metadataNfoForm").off("submit", onSubmit).on("submit", onSubmit)
    }).on("pageshow", "#metadataNfoPage", function() {
        libraryMenu.setTabs("metadata", 3, getTabs), loading.show();
        var page = this,
            promise1 = ApiClient.getUsers(),
            promise2 = ApiClient.getNamedConfiguration(metadataKey);
        Promise.all([promise1, promise2]).then(function(responses) {
            loadPage(page, responses[1], responses[0])
        })
    })
});