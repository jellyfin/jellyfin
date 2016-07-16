define(['itemShortcuts', 'registerElement'], function (itemShortcuts) {

    var ItemsContainerProtoType = Object.create(HTMLDivElement.prototype);

    ItemsContainerProtoType.attachedCallback = function () {
        itemShortcuts.on(this);
    };

    ItemsContainerProtoType.detachedCallback = function () {
        itemShortcuts.off(this);
    };

    document.registerElement('emby-itemscontainer', {
        prototype: ItemsContainerProtoType,
        extends: 'div'
    });
});