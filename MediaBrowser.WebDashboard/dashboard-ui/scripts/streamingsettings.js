define(["jQuery", "libraryMenu", "loading"], function($, libraryMenu, loading) {
    "use strict";

    function loadPage(page, config) {
        $("#txtRemoteClientBitrateLimit", page).val(config.RemoteClientBitrateLimit / 1e6 || ""), loading.hide()
    }

    function onSubmit() {
        loading.show();
        var form = this;
        return ApiClient.getServerConfiguration().then(function(config) {
            config.RemoteClientBitrateLimit = parseInt(1e6 * parseFloat($("#txtRemoteClientBitrateLimit", form).val() || "0")), ApiClient.updateServerConfiguration(config).then(Dashboard.processServerConfigurationUpdateResult)
        }), !1
    }

    function getTabs() {
        return [{
            href: "playbackconfiguration.html",
            name: Globalize.translate("TabResumeSettings")
        }, {
            href: "streamingsettings.html",
            name: Globalize.translate("TabStreaming")
        }]
    }
    $(document).on("pageinit", "#streamingSettingsPage", function() {
        var page = this;
        $("#btnSelectTranscodingTempPath", page).on("click.selectDirectory", function() {
            require(["directorybrowser"], function(directoryBrowser) {
                var picker = new directoryBrowser;
                picker.show({
                    callback: function(path) {
                        path && $("#txtTranscodingTempPath", page).val(path), picker.close()
                    },
                    validateWriteable: !0,
                    header: Globalize.translate("HeaderSelectTranscodingPath"),
                    instruction: Globalize.translate("HeaderSelectTranscodingPathHelp")
                })
            })
        }), $(".streamingSettingsForm").off("submit", onSubmit).on("submit", onSubmit)
    }).on("pageshow", "#streamingSettingsPage", function() {
        loading.show(), libraryMenu.setTabs("playback", 1, getTabs);
        var page = this;
        ApiClient.getServerConfiguration().then(function(config) {
            loadPage(page, config)
        })
    })
});