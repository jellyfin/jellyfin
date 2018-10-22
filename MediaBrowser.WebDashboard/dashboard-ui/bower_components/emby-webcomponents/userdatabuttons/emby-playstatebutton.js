define(["connectionManager", "serverNotifications", "events", "globalize", "emby-button"], function(connectionManager, serverNotifications, events, globalize, EmbyButtonPrototype) {
    "use strict";

    function addNotificationEvent(instance, name, handler) {
        var localHandler = handler.bind(instance);
        events.on(serverNotifications, name, localHandler), instance[name] = localHandler
    }

    function removeNotificationEvent(instance, name) {
        var handler = instance[name];
        handler && (events.off(serverNotifications, name, handler), instance[name] = null)
    }

    function onClick(e) {
        var button = this,
            id = button.getAttribute("data-id"),
            serverId = button.getAttribute("data-serverid"),
            apiClient = connectionManager.getApiClient(serverId);
        button.classList.contains("playstatebutton-played") ? (apiClient.markUnplayed(apiClient.getCurrentUserId(), id, new Date), setState(button, !1)) : (apiClient.markPlayed(apiClient.getCurrentUserId(), id, new Date), setState(button, !0))
    }

    function onUserDataChanged(e, apiClient, userData) {
        var button = this;
        userData.ItemId === button.getAttribute("data-id") && setState(button, userData.Played)
    }

    function setState(button, played, updateAttribute) {
        var icon = button.iconElement;
        icon || (button.iconElement = button.querySelector("i"), icon = button.iconElement), played ? (button.classList.add("playstatebutton-played"), icon && (icon.classList.add("playstatebutton-icon-played"), icon.classList.remove("playstatebutton-icon-unplayed"))) : (button.classList.remove("playstatebutton-played"), icon && (icon.classList.remove("playstatebutton-icon-played"), icon.classList.add("playstatebutton-icon-unplayed"))), !1 !== updateAttribute && button.setAttribute("data-played", played)
    }

    function setTitle(button, itemType) {
        button.title = "AudioBook" !== itemType && "AudioPodcast" !== itemType ? globalize.translate("sharedcomponents#Watched") : globalize.translate("sharedcomponents#Played");
        var text = button.querySelector(".button-text");
        text && (text.innerHTML = button.title)
    }

    function clearEvents(button) {
        button.removeEventListener("click", onClick), removeNotificationEvent(button, "UserDataChanged")
    }

    function bindEvents(button) {
        clearEvents(button), button.addEventListener("click", onClick), addNotificationEvent(button, "UserDataChanged", onUserDataChanged)
    }
    var EmbyPlaystateButtonPrototype = Object.create(EmbyButtonPrototype);
    EmbyPlaystateButtonPrototype.createdCallback = function() {
        EmbyButtonPrototype.createdCallback && EmbyButtonPrototype.createdCallback.call(this)
    }, EmbyPlaystateButtonPrototype.attachedCallback = function() {
        EmbyButtonPrototype.attachedCallback && EmbyButtonPrototype.attachedCallback.call(this);
        var itemId = this.getAttribute("data-id"),
            serverId = this.getAttribute("data-serverid");
        itemId && serverId && (setState(this, "true" === this.getAttribute("data-played"), !1), bindEvents(this), setTitle(this, this.getAttribute("data-type")))
    }, EmbyPlaystateButtonPrototype.detachedCallback = function() {
        EmbyButtonPrototype.detachedCallback && EmbyButtonPrototype.detachedCallback.call(this), clearEvents(this), this.iconElement = null
    }, EmbyPlaystateButtonPrototype.setItem = function(item) {
        if (item) {
            this.setAttribute("data-id", item.Id), this.setAttribute("data-serverid", item.ServerId);
            setState(this, item.UserData && item.UserData.Played), bindEvents(this), setTitle(this, item.Type)
        } else this.removeAttribute("data-id"), this.removeAttribute("data-serverid"), this.removeAttribute("data-played"), clearEvents(this)
    }, document.registerElement("emby-playstatebutton", {
        prototype: EmbyPlaystateButtonPrototype,
        extends: "button"
    })
});