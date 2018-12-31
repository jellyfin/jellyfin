define(['browser', 'appStorage', 'apphost', 'loading', 'connectionManager', 'globalize', 'appRouter', 'dom', 'css!./multiselect'], function (browser, appStorage, appHost, loading, connectionManager, globalize, appRouter, dom) {
    'use strict';

    var selectedItems = [];
    var selectedElements = [];
    var currentSelectionCommandsPanel;

    function hideSelections() {

        var selectionCommandsPanel = currentSelectionCommandsPanel;
        if (selectionCommandsPanel) {

            selectionCommandsPanel.parentNode.removeChild(selectionCommandsPanel);
            currentSelectionCommandsPanel = null;

            selectedItems = [];
            selectedElements = [];
            var elems = document.querySelectorAll('.itemSelectionPanel');
            for (var i = 0, length = elems.length; i < length; i++) {

                var parent = elems[i].parentNode;
                parent.removeChild(elems[i]);
                parent.classList.remove('withMultiSelect');
            }
        }
    }

    function onItemSelectionPanelClick(e, itemSelectionPanel) {

        // toggle the checkbox, if it wasn't clicked on
        if (!dom.parentWithClass(e.target, 'chkItemSelect')) {
            var chkItemSelect = itemSelectionPanel.querySelector('.chkItemSelect');

            if (chkItemSelect) {

                if (chkItemSelect.classList.contains('checkedInitial')) {
                    chkItemSelect.classList.remove('checkedInitial');
                } else {
                    var newValue = !chkItemSelect.checked;
                    chkItemSelect.checked = newValue;
                    updateItemSelection(chkItemSelect, newValue);
                }
            }
        }

        e.preventDefault();
        e.stopPropagation();
        return false;
    }

    function updateItemSelection(chkItemSelect, selected) {

        var id = dom.parentWithAttribute(chkItemSelect, 'data-id').getAttribute('data-id');

        if (selected) {

            var current = selectedItems.filter(function (i) {
                return i === id;
            });

            if (!current.length) {
                selectedItems.push(id);
                selectedElements.push(chkItemSelect);
            }

        } else {
            selectedItems = selectedItems.filter(function (i) {
                return i !== id;
            });
            selectedElements = selectedElements.filter(function (i) {
                return i !== chkItemSelect;
            });
        }

        if (selectedItems.length) {
            var itemSelectionCount = document.querySelector('.itemSelectionCount');
            if (itemSelectionCount) {
                itemSelectionCount.innerHTML = selectedItems.length;
            }
        } else {
            hideSelections();
        }
    }

    function onSelectionChange(e) {
        updateItemSelection(this, this.checked);
    }

    function showSelection(item, isChecked) {

        var itemSelectionPanel = item.querySelector('.itemSelectionPanel');

        if (!itemSelectionPanel) {

            itemSelectionPanel = document.createElement('div');
            itemSelectionPanel.classList.add('itemSelectionPanel');

            var parent = item.querySelector('.cardBox') || item.querySelector('.cardContent');
            parent.classList.add('withMultiSelect');
            parent.appendChild(itemSelectionPanel);

            var cssClass = 'chkItemSelect';
            if (isChecked && !browser.firefox) {
                // In firefox, the initial tap hold doesnt' get treated as a click
                // In other browsers it does, so we need to make sure that initial click is ignored
                cssClass += ' checkedInitial';
            }
            var checkedAttribute = isChecked ? ' checked' : '';
            itemSelectionPanel.innerHTML = '<label class="checkboxContainer"><input type="checkbox" is="emby-checkbox" data-outlineclass="multiSelectCheckboxOutline" class="' + cssClass + '"' + checkedAttribute + '/><span></span></label>';
            var chkItemSelect = itemSelectionPanel.querySelector('.chkItemSelect');
            chkItemSelect.addEventListener('change', onSelectionChange);
        }
    }

    function showSelectionCommands() {

        var selectionCommandsPanel = currentSelectionCommandsPanel;

        if (!selectionCommandsPanel) {

            selectionCommandsPanel = document.createElement('div');
            selectionCommandsPanel.classList.add('selectionCommandsPanel');

            document.body.appendChild(selectionCommandsPanel);
            currentSelectionCommandsPanel = selectionCommandsPanel;

            var html = '';

            html += '<button is="paper-icon-button-light" class="btnCloseSelectionPanel autoSize"><i class="md-icon">close</i></button>';
            html += '<h1 class="itemSelectionCount"></h1>';

            var moreIcon = '&#xE5D3;';
            html += '<button is="paper-icon-button-light" class="btnSelectionPanelOptions autoSize" style="margin-left:auto;"><i class="md-icon">' + moreIcon + '</i></button>';

            selectionCommandsPanel.innerHTML = html;

            selectionCommandsPanel.querySelector('.btnCloseSelectionPanel').addEventListener('click', hideSelections);

            var btnSelectionPanelOptions = selectionCommandsPanel.querySelector('.btnSelectionPanelOptions');

            dom.addEventListener(btnSelectionPanelOptions, 'click', showMenuForSelectedItems, { passive: true });
        }
    }

    function alertText(options) {

        return new Promise(function (resolve, reject) {

            require(['alert'], function (alert) {
                alert(options).then(resolve, resolve);
            });
        });
    }

    function deleteItems(apiClient, itemIds) {

        return new Promise(function (resolve, reject) {

            var msg = globalize.translate('sharedcomponents#ConfirmDeleteItem');
            var title = globalize.translate('sharedcomponents#HeaderDeleteItem');

            if (itemIds.length > 1) {
                msg = globalize.translate('sharedcomponents#ConfirmDeleteItems');
                title = globalize.translate('sharedcomponents#HeaderDeleteItems');
            }

            require(['confirm'], function (confirm) {

                confirm(msg, title).then(function () {
                    var promises = itemIds.map(function (itemId) {
                        apiClient.deleteItem(itemId);
                    });

                    Promise.all(promises).then(resolve, function () {

                        alertText(globalize.translate('sharedcomponents#ErrorDeletingItem')).then(reject, reject);
                    });
                }, reject);

            });
        });
    }

    function showMenuForSelectedItems(e) {

        var apiClient = connectionManager.currentApiClient();

        apiClient.getCurrentUser().then(function (user) {

            var menuItems = [];

            menuItems.push({
                name: globalize.translate('sharedcomponents#AddToCollection'),
                id: 'addtocollection',
                ironIcon: 'add'
            });

            menuItems.push({
                name: globalize.translate('sharedcomponents#AddToPlaylist'),
                id: 'playlist',
                ironIcon: 'playlist-add'
            });

            // TODO: Be more dynamic based on what is selected
            if (user.Policy.EnableContentDeletion) {
                menuItems.push({
                    name: globalize.translate('sharedcomponents#Delete'),
                    id: 'delete',
                    ironIcon: 'delete'
                });
            }

            if (user.Policy.EnableContentDownloading && appHost.supports('filedownload')) {
                //items.push({
                //    name: Globalize.translate('ButtonDownload'),
                //    id: 'download',
                //    ironIcon: 'file-download'
                //});
            }

            if (user.Policy.EnableContentDownloading && appHost.supports('sync')) {
                menuItems.push({
                    name: globalize.translate('sharedcomponents#Download'),
                    id: 'synclocal'
                });
            }

            menuItems.push({
                name: globalize.translate('sharedcomponents#GroupVersions'),
                id: 'groupvideos',
                ironIcon: 'call-merge'
            });

            menuItems.push({
                name: globalize.translate('sharedcomponents#MarkPlayed'),
                id: 'markplayed'
            });

            menuItems.push({
                name: globalize.translate('sharedcomponents#MarkUnplayed'),
                id: 'markunplayed'
            });

            menuItems.push({
                name: globalize.translate('sharedcomponents#RefreshMetadata'),
                id: 'refresh'
            });

            if (user.Policy.EnableContentDownloading) {
                menuItems.push({
                    name: globalize.translate('sharedcomponents#Sync'),
                    id: 'sync'
                });
            }

            require(['actionsheet'], function (actionsheet) {

                actionsheet.show({
                    items: menuItems,
                    positionTo: e.target,
                    callback: function (id) {

                        var items = selectedItems.slice(0);
                        var serverId = apiClient.serverInfo().Id;

                        switch (id) {

                            case 'addtocollection':
                                require(['collectionEditor'], function (collectionEditor) {

                                    new collectionEditor().show({
                                        items: items,
                                        serverId: serverId
                                    });
                                });
                                hideSelections();
                                dispatchNeedsRefresh();
                                break;
                            case 'playlist':
                                require(['playlistEditor'], function (playlistEditor) {
                                    new playlistEditor().show({
                                        items: items,
                                        serverId: serverId
                                    });
                                });
                                hideSelections();
                                dispatchNeedsRefresh();
                                break;
                            case 'delete':
                                deleteItems(apiClient, items).then(dispatchNeedsRefresh);
                                hideSelections();
                                dispatchNeedsRefresh();
                                break;
                            case 'groupvideos':
                                combineVersions(apiClient, items);
                                break;
                            case 'markplayed':
                                items.forEach(function (itemId) {
                                    apiClient.markPlayed(apiClient.getCurrentUserId(), itemId);
                                });
                                hideSelections();
                                dispatchNeedsRefresh();
                                break;
                            case 'markunplayed':
                                items.forEach(function (itemId) {
                                    apiClient.markUnplayed(apiClient.getCurrentUserId(), itemId);
                                });
                                hideSelections();
                                dispatchNeedsRefresh();
                                break;
                            case 'refresh':
                                require(['refreshDialog'], function (refreshDialog) {
                                    new refreshDialog({
                                        itemIds: items,
                                        serverId: serverId
                                    }).show();
                                });
                                hideSelections();
                                dispatchNeedsRefresh();
                                break;
                            case 'sync':
                                require(['syncDialog'], function (syncDialog) {
                                    syncDialog.showMenu({
                                        items: items.map(function (i) {
                                            return {
                                                Id: i
                                            };
                                        }),
                                        serverId: serverId
                                    });
                                });
                                hideSelections();
                                dispatchNeedsRefresh();
                                break;
                            case 'synclocal':
                                require(['syncDialog'], function (syncDialog) {
                                    syncDialog.showMenu({
                                        items: items.map(function (i) {
                                            return {
                                                Id: i
                                            };
                                        }),
                                        isLocalSync: true,
                                        serverId: serverId
                                    });
                                });
                                hideSelections();
                                dispatchNeedsRefresh();
                                break;
                            default:
                                break;
                        }
                    }
                });

            });
        });
    }

    function dispatchNeedsRefresh() {

        var elems = [];

        [].forEach.call(selectedElements, function (i) {

            var container = dom.parentWithAttribute(i, 'is', 'emby-itemscontainer');

            if (container && elems.indexOf(container) === -1) {
                elems.push(container);
            }
        });

        for (var i = 0, length = elems.length; i < length; i++) {
            elems[i].notifyRefreshNeeded(true);
        }
    }

    function combineVersions(apiClient, selection) {

        if (selection.length < 2) {

            require(['alert'], function (alert) {
                alert({
                    text: globalize.translate('sharedcomponents#PleaseSelectTwoItems')
                });
            });
            return;
        }

        loading.show();

        apiClient.ajax({

            type: "POST",
            url: apiClient.getUrl("Videos/MergeVersions", { Ids: selection.join(',') })

        }).then(function () {

            loading.hide();
            hideSelections();
            dispatchNeedsRefresh();
        });
    }

    function showSelections(initialCard) {

        require(['emby-checkbox'], function () {
            var cards = document.querySelectorAll('.card');
            for (var i = 0, length = cards.length; i < length; i++) {
                showSelection(cards[i], initialCard === cards[i]);
            }

            showSelectionCommands();
            updateItemSelection(initialCard, true);
        });
    }

    function onContainerClick(e) {

        var target = e.target;

        if (selectedItems.length) {

            var card = dom.parentWithClass(target, 'card');
            if (card) {
                var itemSelectionPanel = card.querySelector('.itemSelectionPanel');
                if (itemSelectionPanel) {
                    return onItemSelectionPanelClick(e, itemSelectionPanel);
                }
            }

            e.preventDefault();
            e.stopPropagation();
            return false;
        }
    }

    document.addEventListener('viewbeforehide', hideSelections);

    return function (options) {

        var self = this;

        var container = options.container;

        function onTapHold(e) {

            var card = dom.parentWithClass(e.target, 'card');

            if (card) {

                showSelections(card);
            }

            e.preventDefault();
            // It won't have this if it's a hammer event
            if (e.stopPropagation) {
                e.stopPropagation();
            }
            return false;
        }

        function getTouches(e) {

            return e.changedTouches || e.targetTouches || e.touches;
        }

        var touchTarget;
        var touchStartTimeout;
        var touchStartX;
        var touchStartY;
        function onTouchStart(e) {

            var touch = getTouches(e)[0];
            touchTarget = null;
            touchStartX = 0;
            touchStartY = 0;

            if (touch) {
                touchStartX = touch.clientX;
                touchStartY = touch.clientY;
                var element = touch.target;

                if (element) {
                    var card = dom.parentWithClass(element, 'card');

                    if (card) {

                        if (touchStartTimeout) {
                            clearTimeout(touchStartTimeout);
                            touchStartTimeout = null;
                        }

                        touchTarget = card;
                        touchStartTimeout = setTimeout(onTouchStartTimerFired, 550);
                    }
                }
            }
        }

        function onTouchMove(e) {

            if (touchTarget) {
                var touch = getTouches(e)[0];
                var deltaX;
                var deltaY;

                if (touch) {
                    var touchEndX = touch.clientX || 0;
                    var touchEndY = touch.clientY || 0;
                    deltaX = Math.abs(touchEndX - (touchStartX || 0));
                    deltaY = Math.abs(touchEndY - (touchStartY || 0));
                } else {
                    deltaX = 100;
                    deltaY = 100;
                }
                if (deltaX >= 5 || deltaY >= 5) {
                    onMouseOut(e);
                }
            }
        }

        function onTouchEnd(e) {

            onMouseOut(e);
        }

        function onMouseDown(e) {

            if (touchStartTimeout) {
                clearTimeout(touchStartTimeout);
                touchStartTimeout = null;
            }

            touchTarget = e.target;
            touchStartTimeout = setTimeout(onTouchStartTimerFired, 550);
        }

        function onMouseOut(e) {

            if (touchStartTimeout) {
                clearTimeout(touchStartTimeout);
                touchStartTimeout = null;
            }
            touchTarget = null;
        }

        function onTouchStartTimerFired() {

            if (!touchTarget) {
                return;
            }

            var card = dom.parentWithClass(touchTarget, 'card');
            touchTarget = null;

            if (card) {

                showSelections(card);
            }
        }

        function initTapHold(element) {

            // mobile safari doesn't allow contextmenu override
            if (browser.touch && !browser.safari) {
                element.addEventListener('contextmenu', onTapHold);
            } else {
                dom.addEventListener(element, 'touchstart', onTouchStart, {
                    passive: true
                });
                dom.addEventListener(element, 'touchmove', onTouchMove, {
                    passive: true
                });
                dom.addEventListener(element, 'touchend', onTouchEnd, {
                    passive: true
                });
                dom.addEventListener(element, 'touchcancel', onTouchEnd, {
                    passive: true
                });
                dom.addEventListener(element, 'mousedown', onMouseDown, {
                    passive: true
                });
                dom.addEventListener(element, 'mouseleave', onMouseOut, {
                    passive: true
                });
                dom.addEventListener(element, 'mouseup', onMouseOut, {
                    passive: true
                });
            }
        }

        initTapHold(container);

        if (options.bindOnClick !== false) {
            container.addEventListener('click', onContainerClick);
        }

        self.onContainerClick = onContainerClick;

        self.destroy = function () {

            container.removeEventListener('click', onContainerClick);
            container.removeEventListener('contextmenu', onTapHold);

            var element = container;

            dom.removeEventListener(element, 'touchstart', onTouchStart, {
                passive: true
            });
            dom.removeEventListener(element, 'touchmove', onTouchMove, {
                passive: true
            });
            dom.removeEventListener(element, 'touchend', onTouchEnd, {
                passive: true
            });
            // this fires in safari due to magnifying class
            //dom.removeEventListener(element, 'touchcancel', onTouchEnd, {
            //    passive: true
            //});
            dom.removeEventListener(element, 'mousedown', onMouseDown, {
                passive: true
            });
            dom.removeEventListener(element, 'mouseleave', onMouseOut, {
                passive: true
            });
            dom.removeEventListener(element, 'mouseup', onMouseOut, {
                passive: true
            });
        };
    };
});