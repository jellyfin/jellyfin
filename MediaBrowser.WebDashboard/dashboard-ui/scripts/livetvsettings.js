define(["jQuery", "loading", "fnchecked", "emby-linkbutton"], function($, loading) {
    "use strict";

    function loadPage(page, config) {
        $(".liveTvSettingsForm", page).show(), $(".noLiveTvServices", page).hide(), $("#selectGuideDays", page).val(config.GuideDays || ""), $("#txtPrePaddingMinutes", page).val(config.PrePaddingSeconds / 60), $("#txtPostPaddingMinutes", page).val(config.PostPaddingSeconds / 60), page.querySelector("#txtRecordingPath").value = config.RecordingPath || "", page.querySelector("#txtMovieRecordingPath").value = config.MovieRecordingPath || "", page.querySelector("#txtSeriesRecordingPath").value = config.SeriesRecordingPath || "", page.querySelector("#txtPostProcessor").value = config.RecordingPostProcessor || "", page.querySelector("#txtPostProcessorArguments").value = config.RecordingPostProcessorArguments || "", loading.hide()
    }

    function onSubmit() {
        loading.show();
        var form = this;
        return ApiClient.getNamedConfiguration("livetv").then(function(config) {
            config.GuideDays = $("#selectGuideDays", form).val() || null;
            var recordingPath = form.querySelector("#txtRecordingPath").value || null,
                movieRecordingPath = form.querySelector("#txtMovieRecordingPath").value || null,
                seriesRecordingPath = form.querySelector("#txtSeriesRecordingPath").value || null,
                recordingPathChanged = recordingPath != config.RecordingPath || movieRecordingPath != config.MovieRecordingPath || seriesRecordingPath != config.SeriesRecordingPath;
            config.RecordingPath = recordingPath, config.MovieRecordingPath = movieRecordingPath, config.SeriesRecordingPath = seriesRecordingPath, config.RecordingEncodingFormat = "mkv", config.PrePaddingSeconds = 60 * $("#txtPrePaddingMinutes", form).val(), config.PostPaddingSeconds = 60 * $("#txtPostPaddingMinutes", form).val(), config.RecordingPostProcessor = $("#txtPostProcessor", form).val(), config.RecordingPostProcessorArguments = $("#txtPostProcessorArguments", form).val(), ApiClient.updateNamedConfiguration("livetv", config).then(function() {
                Dashboard.processServerConfigurationUpdateResult(), showSaveMessage(recordingPathChanged)
            })
        }), !1
    }

    function showSaveMessage(recordingPathChanged) {
        var msg = "";
        recordingPathChanged && (msg += Globalize.translate("RecordingPathChangeMessage")), msg && require(["alert"], function(alert) {
            alert(msg)
        })
    }
    $(document).on("pageinit", "#liveTvSettingsPage", function() {
        var page = this;
        $(".liveTvSettingsForm").off("submit", onSubmit).on("submit", onSubmit), $("#btnSelectRecordingPath", page).on("click.selectDirectory", function() {
            require(["directorybrowser"], function(directoryBrowser) {
                var picker = new directoryBrowser;
                picker.show({
                    callback: function(path) {
                        path && $("#txtRecordingPath", page).val(path), picker.close()
                    },
                    validateWriteable: !0
                })
            })
        }), $("#btnSelectMovieRecordingPath", page).on("click.selectDirectory", function() {
            require(["directorybrowser"], function(directoryBrowser) {
                var picker = new directoryBrowser;
                picker.show({
                    callback: function(path) {
                        path && $("#txtMovieRecordingPath", page).val(path), picker.close()
                    },
                    validateWriteable: !0
                })
            })
        }), $("#btnSelectSeriesRecordingPath", page).on("click.selectDirectory", function() {
            require(["directorybrowser"], function(directoryBrowser) {
                var picker = new directoryBrowser;
                picker.show({
                    callback: function(path) {
                        path && $("#txtSeriesRecordingPath", page).val(path), picker.close()
                    },
                    validateWriteable: !0
                })
            })
        }), $("#btnSelectPostProcessorPath", page).on("click.selectDirectory", function() {
            require(["directorybrowser"], function(directoryBrowser) {
                var picker = new directoryBrowser;
                picker.show({
                    includeFiles: !0,
                    callback: function(path) {
                        path && $("#txtPostProcessor", page).val(path), picker.close()
                    }
                })
            })
        })
    }).on("pageshow", "#liveTvSettingsPage", function() {
        loading.show();
        var page = this;
        ApiClient.getNamedConfiguration("livetv").then(function(config) {
            loadPage(page, config)
        })
    })
});