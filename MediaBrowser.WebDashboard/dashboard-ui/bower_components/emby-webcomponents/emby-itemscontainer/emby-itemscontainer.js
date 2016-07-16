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

        var apiClient = connectionManager.getApiClient(serverId);

        return apiClient.getItem(apiClient.getCurrentUserId(), id);
    }

    function showContextMenu(button) {

        getItem(button).then(function (item) {

            require(['itemContextMenu'], function (itemContextMenu) {
                itemContextMenu.show({
                    positionTo: button,
                    item: item
                });

                // TODO: playallfromhere, queueallfromhere
            });
        });
    }

    function onClick(e) {

        var menuButton = parentWithClass(e.target, 'menuButton');
        if (menuButton) {
            showContextMenu(menuButton);
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