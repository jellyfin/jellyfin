define(["globalize", "dom", "dialogHelper", "emby-checkbox", "emby-select", "emby-input"], function(globalize, dom, dialogHelper) {
    "use strict";

    function getDefaultImageConfig(itemType, type) {
        return {
            Type: type,
            MinWidth: 0,
            Limit: "Primary" === type ? 1 : 0
        }
    }

    function findImageOptions(imageOptions, type) {
        return imageOptions.filter(function(i) {
            return i.Type == type
        })[0]
    }

    function getImageConfig(options, availableOptions, imageType, itemType) {
        return findImageOptions(options.ImageOptions || [], imageType) || findImageOptions(availableOptions.DefaultImageOptions || [], imageType) || getDefaultImageConfig(itemType, imageType)
    }

    function setVisibilityOfBackdrops(elem, visible) {
        visible ? (elem.classList.remove("hide"), elem.querySelector("input").setAttribute("required", "required")) : (elem.classList.add("hide"), elem.querySelector("input").setAttribute("required", ""), elem.querySelector("input").removeAttribute("required"))
    }

    function loadValues(context, itemType, options, availableOptions) {
        var supportedImageTypes = availableOptions.SupportedImageTypes || [];
        setVisibilityOfBackdrops(context.querySelector(".backdropFields"), -1 != supportedImageTypes.indexOf("Backdrop")), setVisibilityOfBackdrops(context.querySelector(".screenshotFields"), -1 != supportedImageTypes.indexOf("Screenshot")), Array.prototype.forEach.call(context.querySelectorAll(".imageType"), function(i) {
            var imageType = i.getAttribute("data-imagetype"),
                container = dom.parentWithTag(i, "LABEL"); - 1 == supportedImageTypes.indexOf(imageType) ? container.classList.add("hide") : container.classList.remove("hide"), getImageConfig(options, availableOptions, imageType, itemType).Limit ? i.checked = !0 : i.checked = !1
        });
        var backdropConfig = getImageConfig(options, availableOptions, "Backdrop", itemType);
        context.querySelector("#txtMaxBackdrops").value = backdropConfig.Limit, context.querySelector("#txtMinBackdropDownloadWidth").value = backdropConfig.MinWidth;
        var screenshotConfig = getImageConfig(options, availableOptions, "Screenshot", itemType);
        context.querySelector("#txtMaxScreenshots").value = screenshotConfig.Limit, context.querySelector("#txtMinScreenshotDownloadWidth").value = screenshotConfig.MinWidth
    }

    function saveValues(context, options) {
        options.ImageOptions = Array.prototype.map.call(context.querySelectorAll(".imageType:not(.hide)"), function(c) {
            return {
                Type: c.getAttribute("data-imagetype"),
                Limit: c.checked ? 1 : 0,
                MinWidth: 0
            }
        }), options.ImageOptions.push({
            Type: "Backdrop",
            Limit: context.querySelector("#txtMaxBackdrops").value,
            MinWidth: context.querySelector("#txtMinBackdropDownloadWidth").value
        }), options.ImageOptions.push({
            Type: "Screenshot",
            Limit: context.querySelector("#txtMaxScreenshots").value,
            MinWidth: context.querySelector("#txtMinScreenshotDownloadWidth").value
        })
    }

    function editor() {
        this.show = function(itemType, options, availableOptions) {
            return new Promise(function(resolve, reject) {
                var xhr = new XMLHttpRequest;
                xhr.open("GET", "components/imageoptionseditor/imageoptionseditor.template.html", !0), xhr.onload = function(e) {
                    var template = this.response,
                        dlg = dialogHelper.createDialog({
                            size: "medium-tall",
                            removeOnClose: !0,
                            scrollY: !1
                        });
                    dlg.classList.add("formDialog"), dlg.innerHTML = globalize.translateDocument(template), dlg.addEventListener("close", function() {
                        saveValues(dlg, options)
                    }), loadValues(dlg, itemType, options, availableOptions), dialogHelper.open(dlg).then(resolve, resolve), dlg.querySelector(".btnCancel").addEventListener("click", function() {
                        dialogHelper.close(dlg)
                    })
                }, xhr.send()
            })
        }
    }
    return editor
});