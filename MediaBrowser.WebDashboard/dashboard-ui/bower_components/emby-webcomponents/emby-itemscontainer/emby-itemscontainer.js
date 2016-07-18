define(['itemShortcuts', 'connectionManager', 'layoutManager', 'browser', 'registerElement'], function (itemShortcuts, connectionManager, layoutManager, browser) {

    var ItemsContainerProtoType = Object.create(HTMLDivElement.prototype);

    function parentWithAttribute(elem, name) {

        while (!elem.getAttribute(name)) {
            elem = elem.parentNode;

            if (!elem || !elem.getAttribute) {
                return null;
            }
        }

        return elem;
    }

    function onClick(e) {

        var itemsContainer = this;
        var target = e.target;

        itemShortcuts.onClick.call(this, e);
    }

    function disableEvent(e) {

        e.preventDefault();
        e.stopPropagation();
        return false;
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

            itemShortcuts.showContextMenu(card, {
                identify: false,
                positionTo: target,
                itemsContainer: itemsContainer
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

    ItemsContainerProtoType.enableHoverMenu = function (enabled) {

        var current = this.hoverMenu;

        if (!enabled && current) {
            current.destroy();
            this.hoverMenu = null;
            return;
        }

        if (current) {
            return;
        }

        var self = this;
        require(['itemHoverMenu'], function (ItemHoverMenu) {
            self.hoverMenu = new ItemHoverMenu(self);
        });
    };

    ItemsContainerProtoType.attachedCallback = function () {
        this.addEventListener('click', onClick);

        // mobile safari doesn't allow contextmenu override
        if (browser.safari && browser.mobile) {
            this.addEventListener('contextmenu', disableEvent);
            // todo: use tap hold
        } else {
            this.addEventListener('contextmenu', onContextMenu);
        }

        if (layoutManager.desktop) {
            this.enableHoverMenu(true);
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