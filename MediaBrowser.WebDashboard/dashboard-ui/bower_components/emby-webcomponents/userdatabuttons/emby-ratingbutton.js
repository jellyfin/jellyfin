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

    function showPicker(button, apiClient, itemId, likes, isFavorite) {

        return apiClient.updateFavoriteStatus(apiClient.getCurrentUserId(), itemId, !isFavorite);
    }

    function onClick(e) {

        var button = this;
        var id = button.getAttribute('data-id');
        var serverId = button.getAttribute('data-serverid');
        var apiClient = connectionManager.getApiClient(serverId);

        var likes = this.getAttribute('data-likes');
        var isFavorite = this.getAttribute('data-isfavorite') === 'true';
        if (likes === 'true') {
            likes = true;
        }
        else if (likes === 'false') {
            likes = false;
        } else {
            likes = null;
        }

        showPicker(button, apiClient, id, likes, isFavorite).then(function (userData) {

            setState(button, userData.Likes, userData.IsFavorite);
        });
    }

    function onUserDataChanged(e, apiClient, userData) {

        var button = this;

        if (userData.ItemId === button.getAttribute('data-id')) {

            setState(button, userData.Likes, userData.IsFavorite);
        }
    }

    function setState(button, likes, isFavorite, updateAttribute) {

        var icon = button.querySelector('i');

        if (isFavorite) {

            if (icon) {
                icon.innerHTML = '&#xE87D;';
				icon.classList.add('ratingbutton-icon-withrating');
            }

            button.classList.add('ratingbutton-withrating');

        } else if (likes) {

            if (icon) {
                icon.innerHTML = '&#xE87D;';
				icon.classList.remove('ratingbutton-icon-withrating');
                //icon.innerHTML = '&#xE8DC;';
            }
            button.classList.remove('ratingbutton-withrating');

        } else if (likes === false) {

            if (icon) {
                icon.innerHTML = '&#xE87D;';
				icon.classList.remove('ratingbutton-icon-withrating');
                //icon.innerHTML = '&#xE8DB;';
            }
            button.classList.remove('ratingbutton-withrating');

        } else {

            if (icon) {
                icon.innerHTML = '&#xE87D;';
				icon.classList.remove('ratingbutton-icon-withrating');
                //icon.innerHTML = '&#xE8DD;';
            }
            button.classList.remove('ratingbutton-withrating');
        }

        if (updateAttribute !== false) {
            button.setAttribute('data-isfavorite', isFavorite);

            button.setAttribute('data-likes', (likes === null ? '' : likes));
        }
    }

    function setTitle(button) {
        button.title = globalize.translate('sharedcomponents#Favorite');

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

    var EmbyRatingButtonPrototype = Object.create(EmbyButtonPrototype);

    EmbyRatingButtonPrototype.createdCallback = function () {

        // base method
        if (EmbyButtonPrototype.createdCallback) {
            EmbyButtonPrototype.createdCallback.call(this);
        }
    };

    EmbyRatingButtonPrototype.attachedCallback = function () {

        // base method
        if (EmbyButtonPrototype.attachedCallback) {
            EmbyButtonPrototype.attachedCallback.call(this);
        }

        var itemId = this.getAttribute('data-id');
        var serverId = this.getAttribute('data-serverid');
        if (itemId && serverId) {

            var likes = this.getAttribute('data-likes');
            var isFavorite = this.getAttribute('data-isfavorite') === 'true';
            if (likes === 'true') {
                likes = true;
            }
            else if (likes === 'false') {
                likes = false;
            } else {
                likes = null;
            }

            setState(this, likes, isFavorite, false);
            bindEvents(this);
        }

        setTitle(this);
    };

    EmbyRatingButtonPrototype.detachedCallback = function () {

        // base method
        if (EmbyButtonPrototype.detachedCallback) {
            EmbyButtonPrototype.detachedCallback.call(this);
        }

        clearEvents(this);
    };

    EmbyRatingButtonPrototype.setItem = function (item) {

        if (item) {

            this.setAttribute('data-id', item.Id);
            this.setAttribute('data-serverid', item.ServerId);

            var userData = item.UserData || {};
            setState(this, userData.Likes, userData.IsFavorite);
            bindEvents(this);

        } else {

            this.removeAttribute('data-id');
            this.removeAttribute('data-serverid');
            this.removeAttribute('data-likes');
            this.removeAttribute('data-isfavorite');
            clearEvents(this);
        }
    };

    document.registerElement('emby-ratingbutton', {
        prototype: EmbyRatingButtonPrototype,
        extends: 'button'
    });
});