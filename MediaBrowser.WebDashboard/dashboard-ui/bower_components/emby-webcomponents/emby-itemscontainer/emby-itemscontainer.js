define(['itemShortcuts', 'connectionManager', 'layoutManager', 'browser', 'registerElement'], function (itemShortcuts, connectionManager, layoutManager, browser) {

    var ItemsContainerProtoType = Object.create(HTMLDivElement.prototype);

    function parentWithClass(elem, className) {

        while (!elem.classList || !elem.classList.contains(className)) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    function parentWithAttribute(elem, name) {

        while (!elem.getAttribute(name)) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    function getItem(button) {

        button = parentWithAttribute(button, 'data-id');
        var serverId = button.getAttribute('data-serverid');
        var id = button.getAttribute('data-id');
        var type = button.getAttribute('data-type');

        var apiClient = connectionManager.getApiClient(serverId);

        return apiClient.getItem(apiClient.getCurrentUserId(), id);
    }

    function showContextMenu(button, itemsContainer) {

        getItem(button).then(function (item) {

            var playlistId = itemsContainer.getAttribute('data-playlistid');
            var collectionId = itemsContainer.getAttribute('data-collectionid');

            if (playlistId) {
                var elem = parentWithAttribute(button, 'data-playlistitemid');
                item.PlaylistItemId = elem ? elem.getAttribute('data-playlistitemid') : null;
            }

            require(['itemContextMenu'], function (itemContextMenu) {
                itemContextMenu.show({
                    positionTo: button,
                    item: item,
                    play: true,
                    queue: true,
                    playAllFromHere: !item.IsFolder,
                    queueAllFromHere: !item.IsFolder,
                    identify: false,
                    playlistId: playlistId,
                    collectionId: collectionId

                }).then(function (result) {

                    if (result.command == 'playallfromhere' || result.command == 'queueallfromhere') {
                        itemShortcuts.execute(button, result.command);
                    }
                    else if (result.command == 'removefromplaylist' || result.command == 'removefromcollection') {

                        itemsContainer.dispatchEvent(new CustomEvent('needsrefresh', {
                            detail: {},
                            cancelable: false,
                            bubbles: true
                        }));
                    }
                });
            });
        });
    }

    function onClick(e) {

        var itemsContainer = this;
        var target = e.target;

        var menuButton = parentWithClass(target, 'menuButton');
        if (menuButton) {
            var card = parentWithAttribute(target, 'data-id');
            if (card) {
                showContextMenu(card, target);
                e.stopPropagation();
                return false;
            }
        }

        itemShortcuts.onClick.call(this, e);
    }

    function disableEvent(e) {

        e.preventDefault();
        e.stopPropagation();
        return false;
    }

    function showContextMenu(card, target, options) {

        var itemId = card.getAttribute('data-id');
        var serverId = card.getAttribute('data-serverid');
        var type = card.getAttribute('data-type');

        var apiClient = connectionManager.getApiClient(serverId);

        var promise = type == 'Timer' ? apiClient.getLiveTvTimer(itemId) : apiClient.getItem(apiClient.getCurrentUserId(), itemId);

        promise.then(function (item) {

            require(['itemContextMenu'], function (itemContextMenu) {

                itemContextMenu.show(Object.assign(options || {}, {
                    item: item,
                    positionTo: target
                }));
            });
        });
    }

    function onContextMenu(e) {

        var itemsContainer = this;

        var target = e.target;
        var card = parentWithAttribute(target, 'data-id');
        if (card) {

            //var itemSelectionPanel = card.querySelector('.itemSelectionPanel');

            //if (!itemSelectionPanel) {
            //    showContextMenu(card, {});
            //}

            showContextMenu(card, target, {
                identify: false
            });
        }

        e.preventDefault();
        e.stopPropagation();
        return false;
    }

    function getShortcutOptions() {
        return {
            click: false
        };
    }

    ItemsContainerProtoType.attachedCallback = function () {
        this.addEventListener('click', onClick);

        // mobile safari doesn't allow contextmenu override
        if (browser.safari && browser.mobile) {
            this.addEventListener('contextmenu', disableEvent);
            // todo: use tap hold
        } else {
            this.addEventListener('contextmenu', onContextMenu);
        }

        itemShortcuts.on(this, getShortcutOptions());
    };

    ItemsContainerProtoType.detachedCallback = function () {
        this.removeEventListener('click', onClick);
        this.removeEventListener('contextmenu', onContextMenu);
        this.removeEventListener('contextmenu', disableEvent);
        itemShortcuts.off(this, getShortcutOptions());
    };

    document.registerElement('emby-itemscontainer', {
        prototype: ItemsContainerProtoType,
        extends: 'div'
    });
});