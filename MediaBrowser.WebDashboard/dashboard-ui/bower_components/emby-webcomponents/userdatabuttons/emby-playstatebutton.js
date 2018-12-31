define(['connectionManager', 'serverNotifications', 'events', 'globalize', 'emby-button'], function (connectionManager, serverNotifications, events, globalize, EmbyButtonPrototype) {
    'use strict';

    function addNotificationEvent(instance, name, handler) {

        var localHandler = handler.bind(instance);
        events.on(serverNotifications, name, localHandler);
        instance[name] = localHandler;
    }

    function removeNotificationEvent(instance, name) {

        var handler = instance[name];
        if (handler) {
            events.off(serverNotifications, name, handler);
            instance[name] = null;
        }
    }

    function onClick(e) {

        var button = this;
        var id = button.getAttribute('data-id');
        var serverId = button.getAttribute('data-serverid');
        var apiClient = connectionManager.getApiClient(serverId);

        if (!button.classList.contains('playstatebutton-played')) {

            apiClient.markPlayed(apiClient.getCurrentUserId(), id, new Date());

            setState(button, true);

        } else {

            apiClient.markUnplayed(apiClient.getCurrentUserId(), id, new Date());

            setState(button, false);
        }
    }

    function onUserDataChanged(e, apiClient, userData) {

        var button = this;

        if (userData.ItemId === button.getAttribute('data-id')) {

            setState(button, userData.Played);
        }
    }

    function setState(button, played, updateAttribute) {

        var icon = button.iconElement;
        if (!icon) {
            button.iconElement = button.querySelector('i');
            icon = button.iconElement;
        }

        if (played) {

            button.classList.add('playstatebutton-played');

            if (icon) {
                icon.classList.add('playstatebutton-icon-played');
                icon.classList.remove('playstatebutton-icon-unplayed');
            }

        } else {

            button.classList.remove('playstatebutton-played');

            if (icon) {
                icon.classList.remove('playstatebutton-icon-played');
                icon.classList.add('playstatebutton-icon-unplayed');
            }
        }

        if (updateAttribute !== false) {
            button.setAttribute('data-played', played);
        }
    }

    function setTitle(button, itemType) {

        if (itemType !== 'AudioBook' && itemType !== 'AudioPodcast') {
            button.title = globalize.translate('sharedcomponents#Watched');
        } else {
            button.title = globalize.translate('sharedcomponents#Played');
        }

        var text = button.querySelector('.button-text');
        if (text) {
            text.innerHTML = button.title;
        }
    }

    function clearEvents(button) {

        button.removeEventListener('click', onClick);
        removeNotificationEvent(button, 'UserDataChanged');
    }

    function bindEvents(button) {

        clearEvents(button);

        button.addEventListener('click', onClick);
        addNotificationEvent(button, 'UserDataChanged', onUserDataChanged);
    }

    var EmbyPlaystateButtonPrototype = Object.create(EmbyButtonPrototype);

    EmbyPlaystateButtonPrototype.createdCallback = function () {

        // base method
        if (EmbyButtonPrototype.createdCallback) {
            EmbyButtonPrototype.createdCallback.call(this);
        }
    };

    EmbyPlaystateButtonPrototype.attachedCallback = function () {

        // base method
        if (EmbyButtonPrototype.attachedCallback) {
            EmbyButtonPrototype.attachedCallback.call(this);
        }

        var itemId = this.getAttribute('data-id');
        var serverId = this.getAttribute('data-serverid');
        if (itemId && serverId) {

            setState(this, this.getAttribute('data-played') === 'true', false);
            bindEvents(this);
            setTitle(this, this.getAttribute('data-type'));
        }
    };

    EmbyPlaystateButtonPrototype.detachedCallback = function () {

        // base method
        if (EmbyButtonPrototype.detachedCallback) {
            EmbyButtonPrototype.detachedCallback.call(this);
        }

        clearEvents(this);
        this.iconElement = null;
    };

    EmbyPlaystateButtonPrototype.setItem = function (item) {

        if (item) {

            this.setAttribute('data-id', item.Id);
            this.setAttribute('data-serverid', item.ServerId);

            var played = item.UserData && item.UserData.Played;
            setState(this, played);
            bindEvents(this);

            setTitle(this, item.Type);

        } else {

            this.removeAttribute('data-id');
            this.removeAttribute('data-serverid');
            this.removeAttribute('data-played');
            clearEvents(this);
        }
    };

    document.registerElement('emby-playstatebutton', {
        prototype: EmbyPlaystateButtonPrototype,
        extends: 'button'
    });
});