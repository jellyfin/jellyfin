define(['itemShortcuts', 'connectionManager', 'layoutManager', 'browser', 'dom', 'registerElement'], function (itemShortcuts, connectionManager, layoutManager, browser, dom) {

    var ItemsContainerProtoType = Object.create(HTMLDivElement.prototype);

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
        var card = dom.parentWithAttribute(target, 'data-id');

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

            e.preventDefault();
            e.stopPropagation();
            return false;
        }
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

    ItemsContainerProtoType.enableMultiSelect = function (enabled) {

        var current = this.multiSelect;

        if (!enabled && current) {
            current.destroy();
            this.multiSelect = null;
            return;
        }

        if (current) {
            return;
        }

        var self = this;
        require(['multiSelect'], function (MultiSelect) {
            self.multiSelect = new MultiSelect(self);
        });
    };

    ItemsContainerProtoType.attachedCallback = function () {

        this.addEventListener('click', onClick);

        if (browser.mobile) {
            this.addEventListener('contextmenu', disableEvent);
        } else {
            this.addEventListener('contextmenu', onContextMenu);
        }

        if (layoutManager.desktop) {
            this.enableHoverMenu(true);
        }

        if (layoutManager.desktop || layoutManager.mobile) {
            this.enableMultiSelect(true);
        }

        itemShortcuts.on(this, getShortcutOptions());
    };

    ItemsContainerProtoType.detachedCallback = function () {

        this.enableHoverMenu(false);
        this.enableMultiSelect(false);
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