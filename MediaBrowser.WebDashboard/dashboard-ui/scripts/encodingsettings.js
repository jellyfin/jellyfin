define(["jQuery", "loading", "globalize", "dom"], function($, loading, globalize, dom) {
    "use strict";

    function loadPage(page, config, systemInfo) {
        Array.prototype.forEach.call(page.querySelectorAll(".chkDecodeCodec"), function(c) {
            c.checked = -1 !== (config.HardwareDecodingCodecs || []).indexOf(c.getAttribute("data-codec"))
        }), page.querySelector("#chkHardwareEncoding").checked = config.EnableHardwareEncoding, $("#selectVideoDecoder", page).val(config.HardwareAccelerationType), $("#selectThreadCount", page).val(config.EncodingThreadCount), $("#txtDownMixAudioBoost", page).val(config.DownMixAudioBoost), page.querySelector(".txtEncoderPath").value = config.EncoderAppPath || "", $("#txtTranscodingTempPath", page).val(config.TranscodingTempPath || ""), $("#txtVaapiDevice", page).val(config.VaapiDevice || ""), page.querySelector("#selectH264Preset").value = config.H264Preset || "", page.querySelector("#txtH264Crf").value = config.H264Crf || "", page.querySelector("#chkEnableSubtitleExtraction").checked = config.EnableSubtitleExtraction || !1, page.querySelector("#selectVideoDecoder").dispatchEvent(new CustomEvent("change", {
            bubbles: !0
        })), loading.hide()
    }

    function onSaveEncodingPathFailure(response) {
        loading.hide();
        var msg = "";
        msg = globalize.translate("FFmpegSavePathNotFound"), require(["alert"], function(alert) {
            alert(msg)
        })
    }

    function updateEncoder(form) {
        return ApiClient.getSystemInfo().then(function(systemInfo) {
            return ApiClient.ajax({
                url: ApiClient.getUrl("System/MediaEncoder/Path"),
                type: "POST",
                data: {
                    Path: form.querySelector(".txtEncoderPath").value,
                    PathType: "Custom"
                }
            }).then(Dashboard.processServerConfigurationUpdateResult, onSaveEncodingPathFailure)
        })
    }

    function onSubmit() {
        var form = this,
            onDecoderConfirmed = function() {
                loading.show(), ApiClient.getNamedConfiguration("encoding").then(function(config) {
                    config.DownMixAudioBoost = $("#txtDownMixAudioBoost", form).val(), config.TranscodingTempPath = $("#txtTranscodingTempPath", form).val(), config.EncodingThreadCount = $("#selectThreadCount", form).val(), config.HardwareAccelerationType = $("#selectVideoDecoder", form).val(), config.VaapiDevice = $("#txtVaapiDevice", form).val(), config.H264Preset = form.querySelector("#selectH264Preset").value, config.H264Crf = parseInt(form.querySelector("#txtH264Crf").value || "0"), config.EnableSubtitleExtraction = form.querySelector("#chkEnableSubtitleExtraction").checked, config.HardwareDecodingCodecs = Array.prototype.map.call(Array.prototype.filter.call(form.querySelectorAll(".chkDecodeCodec"), function(c) {
                        return c.checked
                    }), function(c) {
                        return c.getAttribute("data-codec")
                    }), config.EnableHardwareEncoding = form.querySelector("#chkHardwareEncoding").checked, ApiClient.updateNamedConfiguration("encoding", config).then(function() {
                        updateEncoder(form)
                    })
                })
            };
        return $("#selectVideoDecoder", form).val() ? require(["alert"], function(alert) {
            alert({
                title: globalize.translate("TitleHardwareAcceleration"),
                text: globalize.translate("HardwareAccelerationWarning")
            }).then(onDecoderConfirmed)
        }) : onDecoderConfirmed(), !1
    }

    function setDecodingCodecsVisible(context, value) {
        value = value || "";
        var any;
        Array.prototype.forEach.call(context.querySelectorAll(".chkDecodeCodec"), function(c) {
            -1 === c.getAttribute("data-types").split(",").indexOf(value) ? dom.parentWithTag(c, "LABEL").classList.add("hide") : (dom.parentWithTag(c, "LABEL").classList.remove("hide"), any = !0)
        }), any ? context.querySelector(".decodingCodecsList").classList.remove("hide") : context.querySelector(".decodingCodecsList").classList.add("hide")
    }
    $(document).on("pageinit", "#encodingSettingsPage", function() {
        var page = this;
        page.querySelector("#selectVideoDecoder").addEventListener("change", function() {
            "vaapi" == this.value ? (page.querySelector(".fldVaapiDevice").classList.remove("hide"), page.querySelector("#txtVaapiDevice").setAttribute("required", "required")) : (page.querySelector(".fldVaapiDevice").classList.add("hide"), page.querySelector("#txtVaapiDevice").removeAttribute("required")), this.value ? page.querySelector(".hardwareAccelerationOptions").classList.remove("hide") : page.querySelector(".hardwareAccelerationOptions").classList.add("hide"), setDecodingCodecsVisible(page, this.value)
        }), $("#btnSelectEncoderPath", page).on("click.selectDirectory", function() {
            require(["directorybrowser"], function(directoryBrowser) {
                var picker = new directoryBrowser;
                picker.show({
                    includeFiles: !0,
                    callback: function(path) {
                        path && $(".txtEncoderPath", page).val(path), picker.close()
                    }
                })
            })
        }), $("#btnSelectTranscodingTempPath", page).on("click.selectDirectory", function() {
            require(["directorybrowser"], function(directoryBrowser) {
                var picker = new directoryBrowser;
                picker.show({
                    callback: function(path) {
                        path && $("#txtTranscodingTempPath", page).val(path), picker.close()
                    },
                    validateWriteable: !0,
                    header: globalize.translate("HeaderSelectTranscodingPath"),
                    instruction: globalize.translate("HeaderSelectTranscodingPathHelp")
                })
            })
        }), $(".encodingSettingsForm").off("submit", onSubmit).on("submit", onSubmit)
    }).on("pageshow", "#encodingSettingsPage", function() {
        loading.show();
        var page = this;
        ApiClient.getNamedConfiguration("encoding").then(function(config) {
            ApiClient.getSystemInfo().then(function(systemInfo) {
                "External" == systemInfo.EncoderLocationType ? (page.querySelector(".fldEncoderPath").classList.add("hide"), page.querySelector(".txtEncoderPath").removeAttribute("required")) : (page.querySelector(".fldEncoderPath").classList.remove("hide"), page.querySelector(".txtEncoderPath").setAttribute("required", "required")), loadPage(page, config, systemInfo)
            })
        })
    })
});