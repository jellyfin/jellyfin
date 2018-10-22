define(["dialogHelper", "dom", "layoutManager", "scrollHelper", "globalize", "require", "material-icons", "emby-button", "paper-icon-button-light", "emby-input", "formDialogStyle", "flexStyles"], function(dialogHelper, dom, layoutManager, scrollHelper, globalize, require) {
    "use strict";

    function showDialog(options, template) {
        function onButtonClick() {
            dialogResult = this.getAttribute("data-id"), dialogHelper.close(dlg)
        }
        var dialogOptions = {
                removeOnClose: !0,
                scrollY: !1
            },
            enableTvLayout = layoutManager.tv;
        enableTvLayout && (dialogOptions.size = "fullscreen");
        var dlg = dialogHelper.createDialog(dialogOptions);
        dlg.classList.add("formDialog"), dlg.innerHTML = globalize.translateHtml(template, "sharedcomponents"), dlg.classList.add("align-items-center"), dlg.classList.add("justify-content-center");
        var formDialogContent = dlg.querySelector(".formDialogContent");
        formDialogContent.classList.add("no-grow"), enableTvLayout ? (formDialogContent.style["max-width"] = "50%", formDialogContent.style["max-height"] = "60%", scrollHelper.centerFocus.on(formDialogContent, !1)) : (formDialogContent.style.maxWidth = Math.min(150 * options.buttons.length + 200, dom.getWindowSize().innerWidth - 50) + "px", dlg.classList.add("dialog-fullscreen-lowres")), options.title ? dlg.querySelector(".formDialogHeaderTitle").innerHTML = options.title || "" : dlg.querySelector(".formDialogHeaderTitle").classList.add("hide");
        var displayText = options.html || options.text || "";
        dlg.querySelector(".text").innerHTML = displayText, displayText || dlg.querySelector(".dialogContentInner").classList.add("hide");
        var i, length, html = "",
            hasDescriptions = !1;
        for (i = 0, length = options.buttons.length; i < length; i++) {
            var item = options.buttons[i],
                autoFocus = 0 === i ? " autofocus" : "",
                buttonClass = "btnOption raised formDialogFooterItem formDialogFooterItem-autosize";
            item.type && (buttonClass += " button-" + item.type), item.description && (hasDescriptions = !0), hasDescriptions && (buttonClass += " formDialogFooterItem-vertical formDialogFooterItem-nomarginbottom"), html += '<button is="emby-button" type="button" class="' + buttonClass + '" data-id="' + item.id + '"' + autoFocus + ">" + item.name + "</button>", item.description && (html += '<div class="formDialogFooterItem formDialogFooterItem-autosize fieldDescription" style="margin-top:.25em!important;margin-bottom:1.25em!important;">' + item.description + "</div>")
        }
        dlg.querySelector(".formDialogFooter").innerHTML = html, hasDescriptions && dlg.querySelector(".formDialogFooter").classList.add("formDialogFooter-vertical");
        var dialogResult, buttons = dlg.querySelectorAll(".btnOption");
        for (i = 0, length = buttons.length; i < length; i++) buttons[i].addEventListener("click", onButtonClick);
        return dialogHelper.open(dlg).then(function() {
            return enableTvLayout && scrollHelper.centerFocus.off(dlg.querySelector(".formDialogContent"), !1), dialogResult || Promise.reject()
        })
    }
    return function(text, title) {
        var options;
        return options = "string" == typeof text ? {
            title: title,
            text: text
        } : text, new Promise(function(resolve, reject) {
            require(["text!./dialog.template.html"], function(template) {
                showDialog(options, template).then(resolve, reject)
            })
        })
    }
});