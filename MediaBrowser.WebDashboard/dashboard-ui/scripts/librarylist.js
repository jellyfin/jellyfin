define(['appSettings', 'appStorage', 'libraryBrowser', 'apphost', 'itemHelper', 'mediaInfo'], function (appSettings, appStorage, LibraryBrowser, appHost, itemHelper, mediaInfo) {

    var showOverlayTimeout;

    function onHoverOut(e) {

        var elem = e.target;

        if (!elem.classList.contains('card')) {
            return;
        }

        if (showOverlayTimeout) {
            clearTimeout(showOverlayTimeout);
            showOverlayTimeout = null;
        }

        elem = elem.querySelector('.cardOverlayTarget');

        if (elem) {
            slideDownToHide(elem);
        }
    }

    function slideDownToHide(elem) {

        if (elem.classList.contains('hide')) {
            return;
        }

        if (!elem.animate) {
            elem.classList.add('hide');
            return;
        }

        requestAnimationFrame(function () {
            var keyframes = [
              { transform: 'translateY(0)', offset: 0 },
              { transform: 'translateY(100%)', offset: 1 }];
            var timing = { duration: 300, iterations: 1, fill: 'forwards', easing: 'ease-out' };

            elem.animate(keyframes, timing).onfinish = function () {
                elem.classList.add('hide');
            };
        });
    }

    function slideUpToShow(elem) {

        if (!elem.classList.contains('hide')) {
            return;
        }

        elem.classList.remove('hide');

        if (!elem.animate) {
            return;
        }

        requestAnimationFrame(function () {

            var keyframes = [
              { transform: 'translateY(100%)', offset: 0 },
              { transform: 'translateY(0)', offset: 1 }];
            var timing = { duration: 300, iterations: 1, fill: 'forwards', easing: 'ease-out' };
            elem.animate(keyframes, timing);
        });
    }

    function getOverlayHtml(item, currentUser, card) {

        var html = '';

        html += '<div class="cardOverlayInner">';

        var className = card.className.toLowerCase();

        var isMiniItem = className.indexOf('mini') != -1;
        var isSmallItem = isMiniItem || className.indexOf('small') != -1;
        var isPortrait = className.indexOf('portrait') != -1;
        var isSquare = className.indexOf('square') != -1;

        var parentName = isSmallItem || isMiniItem || isPortrait ? null : item.SeriesName;
        var name = itemHelper.getDisplayName(item);

        html += '<div style="margin-bottom:1em;">';
        var logoHeight = isSmallItem || isMiniItem ? 20 : 26;
        var imgUrl;

        if (parentName && item.ParentLogoItemId) {

            imgUrl = ApiClient.getScaledImageUrl(item.ParentLogoItemId, {
                maxHeight: logoHeight,
                type: 'logo',
                tag: item.ParentLogoImageTag
            });

            html += '<img src="' + imgUrl + '" style="max-height:' + logoHeight + 'px;max-width:100%;" />';

        }
        else if (item.ImageTags.Logo) {

            imgUrl = ApiClient.getScaledImageUrl(item.Id, {
                maxHeight: logoHeight,
                type: 'logo',
                tag: item.ImageTags.Logo
            });

            html += '<img src="' + imgUrl + '" style="max-height:' + logoHeight + 'px;max-width:100%;" />';
        }
        else {
            html += parentName || name;
        }
        html += '</div>';

        if (parentName) {
            html += '<p>';
            html += name;
            html += '</p>';
        } else if (!isSmallItem && !isMiniItem) {
            html += '<div class="itemMiscInfo">';
            html += mediaInfo.getPrimaryMediaInfoHtml(item, {
                endsAt: false
            });
            html += '</div>';
        }

        if (!isMiniItem) {
            html += '<div style="margin:1em 0 .75em;">';

            if (isPortrait) {
                html += '<div class="userDataIcons" style="margin:.5em 0 0em;">';
                html += LibraryBrowser.getUserDataIconsHtml(item);
                html += '</div>';
            } else {

                html += '<span class="userDataIcons" style="vertical-align:middle;">';
                html += LibraryBrowser.getUserDataIconsHtml(item);
                html += '</span>';
            }
            html += '</div>';
        }

        html += '<div>';

        var buttonCount = 0;

        if (MediaController.canPlay(item)) {

            var resumePosition = (item.UserData || {}).PlaybackPositionTicks || 0;

            html += '<button is="paper-icon-button-light" class="itemAction autoSize" data-action="playmenu" data-itemid="' + item.Id + '" data-itemtype="' + item.Type + '" data-isfolder="' + item.IsFolder + '" data-mediatype="' + item.MediaType + '" data-resumeposition="' + resumePosition + '"><i class="md-icon">play_circle_outline</i></button>';
            buttonCount++;
        }

        if (item.LocalTrailerCount) {
            html += '<button is="paper-icon-button-light" class="btnPlayTrailer autoSize" data-itemid="' + item.Id + '"><i class="md-icon">videocam</i></button>';
            buttonCount++;
        }

        html += '<button is="paper-icon-button-light" class="btnMoreCommands autoSize"><i class="md-icon">more_vert</i></button>';
        buttonCount++;

        html += '</div>';

        html += '</div>';

        return html;
    }

    function onTrailerButtonClick(e) {

        var id = this.getAttribute('data-itemid');

        ApiClient.getLocalTrailers(Dashboard.getCurrentUserId(), id).then(function (trailers) {
            MediaController.play({ items: trailers });
        });

        e.preventDefault();
        e.stopPropagation();
        return false;
    }

    function onMoreButtonClick(e) {

        var card = parentWithClass(this, 'card');

        showContextMenu(card, {
            shuffle: false,
            instantMix: false,
            play: false,
            playAllFromHere: false,
            queue: false,
            queueAllFromHere: false
        });

        e.preventDefault();
        e.stopPropagation();
        return false;
    }

    function showContextMenu(card, options) {

        var displayContextItem = card;

        card = parentWithClass(card, 'card');

        if (!card) {
            return;
        }

        var itemId = card.getAttribute('data-itemid');
        var serverId = ApiClient.serverInfo().Id;
        var type = card.getAttribute('data-itemtype');

        var apiClient = ConnectionManager.getApiClient(serverId);

        var promise = type == 'Timer' ? apiClient.getLiveTvTimer(itemId) : apiClient.getItem(apiClient.getCurrentUserId(), itemId);

        promise.then(function (item) {

            require(['itemContextMenu'], function (itemContextMenu) {

                itemContextMenu.show(Object.assign(options || {}, {
                    item: item,
                    positionTo: displayContextItem
                }));
            });
        });
    }

    function isClickable(target) {

        while (target != null) {
            var tagName = target.tagName || '';
            if (tagName == 'A' || tagName.indexOf('BUTTON') != -1 || tagName.indexOf('INPUT') != -1) {
                return true;
            }

            return false;
            //target = target.parentNode;
        }

        return false;
    }

    function onCardClick(e) {

        var card = parentWithClass(e.target, 'card');

        if (card) {

            var itemSelectionPanel = card.querySelector('.itemSelectionPanel');
            if (itemSelectionPanel) {
                return onItemSelectionPanelClick(e, itemSelectionPanel);
            }
            else if (card.classList.contains('groupedCard')) {
                return onGroupedCardClick(e, card);
            }
        }
    }

    function onGroupedCardClick(e, card) {

        var itemId = card.getAttribute('data-itemid');
        var context = card.getAttribute('data-context');

        var userId = Dashboard.getCurrentUserId();

        var playedIndicator = card.querySelector('.playedIndicator');
        var playedIndicatorHtml = playedIndicator ? playedIndicator.innerHTML : null;
        var options = {

            Limit: parseInt(playedIndicatorHtml || '10'),
            Fields: "PrimaryImageAspectRatio,DateCreated",
            ParentId: itemId,
            GroupItems: false
        };

        var target = e.target;
        if (isClickable(target)) {
            return;
        }

        ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options)).then(function (items) {

            if (items.length == 1) {
                Dashboard.navigate(LibraryBrowser.getHref(items[0], context));
                return;
            }

            var url = 'itemdetails.html?id=' + itemId;
            if (context) {
                url += '&context=' + context;
            }

            Dashboard.navigate(url);
        });

        e.stopPropagation();
        e.preventDefault();
        return false;
    }

    function parentWithClass(elem, className) {

        while (!elem.classList || !elem.classList.contains(className)) {
            elem = elem.parentNode;

            if (!elem) {
                return null;
            }
        }

        return elem;
    }

    LibraryBrowser.createCardMenus = function (curr, options) {

        var preventHover = false;

        function onShowTimerExpired(elem) {

            elem = elem.querySelector('a');

            if (elem.querySelector('.itemSelectionPanel')) {
                return;
            }

            var innerElem = elem.querySelector('.cardOverlayTarget');

            if (!innerElem) {
                innerElem = document.createElement('div');
                innerElem.classList.add('hide');
                innerElem.classList.add('cardOverlayTarget');
                parentWithClass(elem, 'cardContent').appendChild(innerElem);
            }

            var dataElement = elem;
            while (dataElement && !dataElement.getAttribute('data-itemid')) {
                dataElement = dataElement.parentNode;
            }

            var id = dataElement.getAttribute('data-itemid');
            var type = dataElement.getAttribute('data-itemtype');

            if (type == 'Timer') {
                return;
            }

            var promise1 = ApiClient.getItem(Dashboard.getCurrentUserId(), id);
            var promise2 = Dashboard.getCurrentUser();

            Promise.all([promise1, promise2]).then(function (responses) {

                var item = responses[0];
                var user = responses[1];

                var card = elem;

                while (!card.classList.contains('card')) {
                    card = card.parentNode;
                }

                innerElem.innerHTML = getOverlayHtml(item, user, card);

                var btnPlayTrailer = innerElem.querySelector('.btnPlayTrailer');
                if (btnPlayTrailer) {
                    btnPlayTrailer.addEventListener('click', onTrailerButtonClick);
                }
                var btnMoreCommands = innerElem.querySelector('.btnMoreCommands');
                if (btnMoreCommands) {
                    btnMoreCommands.addEventListener('click', onMoreButtonClick);
                }
            });

            slideUpToShow(innerElem);
        }

        function onHoverIn(e) {

            var elem = e.target;

            if (!elem.classList.contains('cardImage')) {
                return;
            }

            if (preventHover === true) {
                preventHover = false;
                return;
            }

            if (showOverlayTimeout) {
                clearTimeout(showOverlayTimeout);
                showOverlayTimeout = null;
            }

            while (!elem.classList.contains('card')) {
                elem = elem.parentNode;
            }

            showOverlayTimeout = setTimeout(function () {
                onShowTimerExpired(elem);

            }, 1200);
        }

        function preventTouchHover() {
            preventHover = true;
        }

        curr.removeEventListener('click', onCardClick);
        curr.addEventListener('click', onCardClick);

        if (!AppInfo.isTouchPreferred) {

            curr.removeEventListener('mouseenter', onHoverIn);
            curr.addEventListener('mouseenter', onHoverIn, true);

            curr.removeEventListener('mouseleave', onHoverOut);
            curr.addEventListener('mouseleave', onHoverOut, true);

            curr.removeEventListener("touchstart", preventTouchHover);
            curr.addEventListener("touchstart", preventTouchHover);
        }

        //initTapHoldMenus(curr);
    };

    function initTapHoldMenus(elem) {

        if (elem.classList.contains('itemsContainer')) {
            initTapHold(elem);
            return;
        }

        var elems = elem.querySelectorAll('.itemsContainer');

        for (var i = 0, length = elems.length; i < length; i++) {
            initTapHold(elems[i]);
        }
    }

    function initTapHold(element) {

        if (!LibraryBrowser.allowSwipe(element)) {
            return;
        }

        if (element.classList.contains('hasTapHold')) {
            return;
        }

        require(['hammer'], function (Hammer) {

            var manager = new Hammer.Manager(element);

            var press = new Hammer.Press({
                time: 500
            });

            manager.add(press);

            //var hammertime = new Hammer(element);
            element.classList.add('hasTapHold');

            manager.on('press', onTapHold);
        });

        showTapHoldHelp(element);
    }

    function showTapHoldHelp(element) {

        var page = parentWithClass(element, 'page');

        if (!page) {
            return;
        }

        // Don't do this on the home page
        if (page.classList.contains('homePage') || page.classList.contains('itemDetailPage') || page.classList.contains('liveTvPage')) {
            return;
        }

        var expectedValue = "8";
        if (appStorage.getItem("tapholdhelp") == expectedValue) {
            return;
        }

        appStorage.setItem("tapholdhelp", expectedValue);

        Dashboard.alert({
            message: Globalize.translate('TryMultiSelectMessage'),
            title: Globalize.translate('HeaderTryMultiSelect')
        });
    }

    function onTapHold(e) {

        var card = parentWithClass(e.target, 'card');

        if (card) {

            showSelections(card);

            // It won't have this if it's a hammer event
            if (e.stopPropagation) {
                e.stopPropagation();
            }
            e.preventDefault();
            return false;
        }
        e.preventDefault();
        // It won't have this if it's a hammer event
        if (e.stopPropagation) {
            e.stopPropagation();
        }
        return false;
    }

    function onItemSelectionPanelClick(e, itemSelectionPanel) {

        // toggle the checkbox, if it wasn't clicked on
        if (!parentWithClass(e.target, 'chkItemSelect')) {
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

    function onSelectionChange(e) {
        updateItemSelection(this, this.checked);
    }

    function showSelection(item, isChecked) {

        var itemSelectionPanel = item.querySelector('.itemSelectionPanel');

        if (!itemSelectionPanel) {

            itemSelectionPanel = document.createElement('div');
            itemSelectionPanel.classList.add('itemSelectionPanel');

            item.querySelector('.cardContent').appendChild(itemSelectionPanel);

            var cssClass = 'chkItemSelect';
            if (isChecked && !browserInfo.firefox) {
                // In firefox, the initial tap hold doesnt' get treated as a click
                // In other browsers it does, so we need to make sure that initial click is ignored
                cssClass += ' checkedInitial';
            }
            var checkedAttribute = isChecked ? ' checked' : '';
            itemSelectionPanel.innerHTML = '<label class="checkboxContainer"><input type="checkbox" is="emby-checkbox" class="' + cssClass + '"' + checkedAttribute + '/><span></span></label>>';
            var chkItemSelect = itemSelectionPanel.querySelector('.chkItemSelect');
            chkItemSelect.addEventListener('change', onSelectionChange);
        }
    }

    function showSelectionCommands() {

        var selectionCommandsPanel = document.querySelector('.selectionCommandsPanel');

        if (!selectionCommandsPanel) {

            selectionCommandsPanel = document.createElement('div');
            selectionCommandsPanel.classList.add('selectionCommandsPanel');

            document.body.appendChild(selectionCommandsPanel);

            var html = '';

            html += '<div style="float:left;">';
            html += '<button is="paper-icon-button-light" class="btnCloseSelectionPanel autoSize"><i class="md-icon">close</i></button>';
            html += '<span class="itemSelectionCount"></span>';
            html += '</div>';

            html += '<button is="paper-icon-button-light" class="btnSelectionPanelOptions autoSize" style="margin-left:auto;"><i class="md-icon">more_vert</i></button>';

            selectionCommandsPanel.innerHTML = html;

            selectionCommandsPanel.querySelector('.btnCloseSelectionPanel').addEventListener('click', hideSelections);

            var btnSelectionPanelOptions = selectionCommandsPanel.querySelector('.btnSelectionPanelOptions');

            btnSelectionPanelOptions.addEventListener('click', showMenuForSelectedItems);

            if (!browserInfo.mobile) {
                shake(btnSelectionPanelOptions, 1);
            }
        }
    }

    function shake(elem, iterations) {
        var keyframes = [
          { transform: 'translate3d(0, 0, 0)', offset: 0 },
          { transform: 'translate3d(-10px, 0, 0)', offset: 0.1 },
          { transform: 'translate3d(10px, 0, 0)', offset: 0.2 },
          { transform: 'translate3d(-10px, 0, 0)', offset: 0.3 },
          { transform: 'translate3d(10px, 0, 0)', offset: 0.4 },
          { transform: 'translate3d(-10px, 0, 0)', offset: 0.5 },
          { transform: 'translate3d(10px, 0, 0)', offset: 0.6 },
          { transform: 'translate3d(-10px, 0, 0)', offset: 0.7 },
          { transform: 'translate3d(10px, 0, 0)', offset: 0.8 },
          { transform: 'translate3d(-10px, 0, 0)', offset: 0.9 },
          { transform: 'translate3d(0, 0, 0)', offset: 1 }];
        var timing = { duration: 900, iterations: iterations };

        if (elem.animate) {
            elem.animate(keyframes, timing);
        }
    }

    function showSelections(initialCard) {

        require(['emby-checkbox'], function () {
            var cards = document.querySelectorAll('.card');
            for (var i = 0, length = cards.length; i < length; i++) {
                showSelection(cards[i], initialCard == cards[i]);
            }

            showSelectionCommands();
            updateItemSelection(initialCard, true);
        });
    }

    function hideSelections() {

        var selectionCommandsPanel = document.querySelector('.selectionCommandsPanel');
        if (selectionCommandsPanel) {

            selectionCommandsPanel.parentNode.removeChild(selectionCommandsPanel);

            selectedItems = [];
            var elems = document.querySelectorAll('.itemSelectionPanel');
            for (var i = 0, length = elems.length; i < length; i++) {
                elems[i].parentNode.removeChild(elems[i]);
            }
        }
    }

    var selectedItems = [];
    function updateItemSelection(chkItemSelect, selected) {

        var id = parentWithClass(chkItemSelect, 'card').getAttribute('data-itemid');

        if (selected) {

            var current = selectedItems.filter(function (i) {
                return i == id;
            });

            if (!current.length) {
                selectedItems.push(id);
            }

        } else {
            selectedItems = selectedItems.filter(function (i) {
                return i != id;
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

    function showMenuForSelectedItems(e) {

        Dashboard.getCurrentUser().then(function (user) {

            var items = [];

            items.push({
                name: Globalize.translate('ButtonAddToCollection'),
                id: 'addtocollection',
                ironIcon: 'add'
            });

            items.push({
                name: Globalize.translate('ButtonAddToPlaylist'),
                id: 'playlist',
                ironIcon: 'playlist-add'
            });

            if (user.Policy.EnableContentDeletion) {
                items.push({
                    name: Globalize.translate('ButtonDelete'),
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

            items.push({
                name: Globalize.translate('HeaderGroupVersions'),
                id: 'groupvideos',
                ironIcon: 'call-merge'
            });

            items.push({
                name: Globalize.translate('MarkPlayed'),
                id: 'markplayed'
            });

            items.push({
                name: Globalize.translate('MarkUnplayed'),
                id: 'markunplayed'
            });

            items.push({
                name: Globalize.translate('ButtonRefresh'),
                id: 'refresh',
                ironIcon: 'refresh'
            });

            items.push({
                name: Globalize.translate('ButtonSync'),
                id: 'sync',
                ironIcon: 'sync'
            });

            require(['actionsheet'], function (actionsheet) {

                actionsheet.show({
                    items: items,
                    positionTo: e.target,
                    callback: function (id) {

                        var items = selectedItems.slice(0);
                        var serverId = ApiClient.serverInfo().Id;

                        switch (id) {

                            case 'addtocollection':
                                require(['collectionEditor'], function (collectionEditor) {

                                    new collectionEditor().show({
                                        items: items,
                                        serverId: serverId
                                    });
                                });
                                hideSelections();
                                break;
                            case 'playlist':
                                require(['playlistEditor'], function (playlistEditor) {
                                    new playlistEditor().show({
                                        items: items,
                                        serverId: serverId
                                    });
                                });
                                hideSelections();
                                break;
                            case 'delete':
                                LibraryBrowser.deleteItems(items).then(function () {
                                    Dashboard.navigate('home.html');
                                });
                                hideSelections();
                                break;
                            case 'groupvideos':
                                combineVersions(parentWithClass(e.target, 'page'), items);
                                break;
                            case 'markplayed':
                                items.forEach(function (itemId) {
                                    ApiClient.markPlayed(Dashboard.getCurrentUserId(), itemId);
                                });
                                hideSelections();
                                break;
                            case 'markunplayed':
                                items.forEach(function (itemId) {
                                    ApiClient.markUnplayed(Dashboard.getCurrentUserId(), itemId);
                                });
                                hideSelections();
                                break;
                            case 'refresh':
                                require(['refreshDialog'], function (refreshDialog) {
                                    new refreshDialog({
                                        itemIds: items,
                                        serverId: serverId
                                    }).show();
                                });
                                hideSelections();
                                break;
                            case 'sync':
                                require(['syncDialog'], function (syncDialog) {
                                    syncDialog.showMenu({
                                        items: items.map(function (i) {
                                            return {
                                                Id: i
                                            };
                                        })
                                    });
                                });
                                hideSelections();
                                break;
                            default:
                                break;
                        }
                    }
                });

            });
        });
    }

    function combineVersions(page, selection) {

        if (selection.length < 2) {

            Dashboard.alert({
                message: Globalize.translate('MessagePleaseSelectTwoItems'),
                title: Globalize.translate('HeaderError')
            });

            return;
        }

        var msg = Globalize.translate('MessageTheSelectedItemsWillBeGrouped');

        require(['confirm'], function (confirm) {

            confirm(msg, Globalize.translate('HeaderGroupVersions')).then(function () {

                Dashboard.showLoadingMsg();

                ApiClient.ajax({

                    type: "POST",
                    url: ApiClient.getUrl("Videos/MergeVersions", { Ids: selection.join(',') })

                }).then(function () {

                    Dashboard.hideLoadingMsg();
                    hideSelections();
                    page.querySelector('.itemsContainer').dispatchEvent(new CustomEvent('needsrefresh', {}));
                });
            });
        });
    }

    function showSyncButtonsPerUser(page) {

        var apiClient = window.ApiClient;

        if (!apiClient || !apiClient.getCurrentUserId()) {
            return;
        }

        Dashboard.getCurrentUser().then(function (user) {

            var item = {
                SupportsSync: true
            };

            var categorySyncButtons = page.querySelectorAll('.categorySyncButton');
            for (var i = 0, length = categorySyncButtons.length; i < length; i++) {
                if (itemHelper.canSync(user, item)) {
                    categorySyncButtons[i].classList.remove('hide');
                } else {
                    categorySyncButtons[i].classList.add('hide');
                }
            }
        });
    }

    function onCategorySyncButtonClick(e) {

        var button = this;
        var category = button.getAttribute('data-category');
        var parentId = LibraryMenu.getTopParentId();

        require(['syncDialog'], function (syncDialog) {
            syncDialog.showMenu({
                ParentId: parentId,
                Category: category
            });
        });
    }

    pageClassOn('pageinit', "libraryPage", function () {

        var page = this;

        var categorySyncButtons = page.querySelectorAll('.categorySyncButton');
        for (var i = 0, length = categorySyncButtons.length; i < length; i++) {
            categorySyncButtons[i].addEventListener('click', onCategorySyncButtonClick);
        }
    });

    pageClassOn('pageshow', "libraryPage", function () {

        var page = this;

        if (!Dashboard.isServerlessPage()) {
            showSyncButtonsPerUser(page);
        }
    });

    pageClassOn('pagebeforehide', "libraryPage", function () {

        var page = this;

        hideSelections();
    });

});