define(['itemShortcuts', 'connectionManager', 'registerElement'], function (itemShortcuts, connectionManager) {

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

        var menuButton = parentWithClass(e.target, 'menuButton');
        if (menuButton) {
            showContextMenu(menuButton, itemsContainer);
            e.stopPropagation();
            return false;
        }
    }

    ItemsContainerProtoType.attachedCallback = function () {
        this.addEventListener('click', onClick);
        itemShortcuts.on(this);
    };

    ItemsContainerProtoType.detachedCallback = function () {
        this.removeEventListener('click', onClick);
        itemShortcuts.off(this);
    };

    document.registerElement('emby-itemscontainer', {
        prototype: ItemsContainerProtoType,
        extends: 'div'
    });
});