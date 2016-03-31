define(['appSettings', 'appStorage', 'libraryBrowser', 'jQuery'], function (appSettings, appStorage, LibraryBrowser, $) {

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

        requestAnimationFrame(function () {
            var keyframes = [
              { height: '100%', offset: 0 },
              { height: '0', offset: 1 }];
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

        requestAnimationFrame(function () {
            elem.style.display = 'block';

            var keyframes = [
              { height: '0', offset: 0 },
              { height: '100%', offset: 1 }];
            var timing = { duration: 300, iterations: 1, fill: 'forwards', easing: 'ease-out' };
            elem.animate(keyframes, timing);
        });
    }

    function getOverlayHtml(item, currentUser, card, commands) {

        var html = '';

        html += '<div class="cardOverlayInner">';

        var className = card.className.toLowerCase();

        var isMiniItem = className.indexOf('mini') != -1;
        var isSmallItem = isMiniItem || className.indexOf('small') != -1;
        var isPortrait = className.indexOf('portrait') != -1;
        var isSquare = className.indexOf('square') != -1;

        var parentName = isSmallItem || isMiniItem || isPortrait ? null : item.SeriesName;
        var name = LibraryBrowser.getPosterViewDisplayName(item, true);

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
            html += '<p class="itemMiscInfo" style="white-space:nowrap;">';
            html += LibraryBrowser.getMiscInfoHtml(item);
            html += '</p>';
        }

        if (!isMiniItem) {
            html += '<div style="margin:1em 0 .75em;">';

            if (isPortrait) {
                html += '<div class="itemCommunityRating">';
                html += LibraryBrowser.getRatingHtml(item, false);
                html += '</div>';

                html += '<div class="userDataIcons" style="margin:.5em 0 0em;">';
                html += LibraryBrowser.getUserDataIconsHtml(item);
                html += '</div>';
            } else {

                html += '<span class="itemCommunityRating" style="vertical-align:middle;">';
                html += LibraryBrowser.getRatingHtml(item, false);
                html += '</span>';

                html += '<span class="userDataIcons" style="vertical-align:middle;">';
                html += LibraryBrowser.getUserDataIconsHtml(item);
                html += '</span>';
            }
            html += '</div>';
        }

        html += '<div>';

        var buttonMargin = isPortrait || isSquare ? "margin:0 4px 0 0;" : "margin:0 10px 0 0;";

        var buttonCount = 0;

        if (MediaController.canPlay(item)) {

            var resumePosition = (item.UserData || {}).PlaybackPositionTicks || 0;

            html += '<paper-icon-button icon="play-circle-outline" class="btnPlayItem" data-itemid="' + item.Id + '" data-itemtype="' + item.Type + '" data-isfolder="' + item.IsFolder + '" data-mediatype="' + item.MediaType + '" data-resumeposition="' + resumePosition + '"></paper-icon-button>';
            buttonCount++;
        }

        if (commands.indexOf('trailer') != -1) {
            html += '<paper-icon-button icon="videocam" class="btnPlayTrailer" data-itemid="' + item.Id + '"></paper-icon-button>';
            buttonCount++;
        }

        html += '<paper-icon-button icon="' + AppInfo.moreIcon + '" class="btnMoreCommands"></paper-icon-button>';
        buttonCount++;

        html += '</div>';

        html += '</div>';

        return html;
    }

    function onTrailerButtonClick() {

        var id = this.getAttribute('data-itemid');

        ApiClient.getLocalTrailers(Dashboard.getCurrentUserId(), id).then(function (trailers) {
            MediaController.play({ items: trailers });
        });

        return false;
    }

    function onPlayItemButtonClick() {

        var target = this;

        var id = target.getAttribute('data-itemid');
        var type = target.getAttribute('data-itemtype');
        var isFolder = target.getAttribute('data-isfolder') == 'true';
        var mediaType = target.getAttribute('data-mediatype');
        var resumePosition = parseInt(target.getAttribute('data-resumeposition'));

        LibraryBrowser.showPlayMenu(this, id, type, isFolder, mediaType, resumePosition);

        return false;
    }

    function onMoreButtonClick() {

        var card = parentWithClass(this, 'card');

        showContextMenu(card, {
            showPlayOptions: false
        });

        return false;
    }

    function onContextMenu(e) {

        var card = parentWithClass(e.target, 'card');

        if (card) {
            var itemSelectionPanel = card.querySelector('.itemSelectionPanel');

            if (!itemSelectionPanel) {
                showContextMenu(card, {});
            }

            e.preventDefault();
            return false;
        }
    }

    function showContextMenu(card, options) {

        var displayContextItem = card;

        if (!card.classList.contains('card') && !card.classList.contains('listItem')) {
            card = $(card).parents('.listItem,.card')[0];
        }

        var itemId = card.getAttribute('data-itemid');
        var playlistItemId = card.getAttribute('data-playlistitemid');
        var commands = card.getAttribute('data-commands').split(',');
        var itemType = card.getAttribute('data-itemtype');
        var mediaType = card.getAttribute('data-mediatype');
        var playbackPositionTicks = parseInt(card.getAttribute('data-positionticks') || '0');
        var playAccess = card.getAttribute('data-playaccess');
        var locationType = card.getAttribute('data-locationtype');
        var index = card.getAttribute('data-index');

        var albumid = card.getAttribute('data-albumid');
        var artistid = card.getAttribute('data-artistid');

        Dashboard.getCurrentUser().then(function (user) {

            var items = [];

            if (commands.indexOf('addtocollection') != -1) {
                items.push({
                    name: Globalize.translate('ButtonAddToCollection'),
                    id: 'addtocollection',
                    ironIcon: 'add'
                });
            }

            if (commands.indexOf('playlist') != -1) {
                items.push({
                    name: Globalize.translate('ButtonAddToPlaylist'),
                    id: 'playlist',
                    ironIcon: 'playlist-add'
                });
            }

            if (user.Policy.EnableContentDownloading && AppInfo.supportsDownloading) {
                if (mediaType) {
                    items.push({
                        name: Globalize.translate('ButtonDownload'),
                        id: 'download',
                        ironIcon: 'file-download'
                    });
                }
            }

            if (commands.indexOf('delete') != -1) {
                items.push({
                    name: Globalize.translate('ButtonDelete'),
                    id: 'delete',
                    ironIcon: 'delete'
                });
            }

            if (user.Policy.IsAdministrator) {
                if (commands.indexOf('edit') != -1) {
                    items.push({
                        name: Globalize.translate('ButtonEdit'),
                        id: 'edit',
                        ironIcon: 'mode-edit'
                    });
                }

                if (commands.indexOf('editimages') != -1) {
                    items.push({
                        name: Globalize.translate('ButtonEditImages'),
                        id: 'editimages',
                        ironIcon: 'photo'
                    });
                }

                if (commands.indexOf('editsubtitles') != -1) {
                    items.push({
                        name: Globalize.translate('ButtonEditSubtitles'),
                        id: 'editsubtitles',
                        ironIcon: 'closed-caption'
                    });
                }
            }

            if (commands.indexOf('instantmix') != -1) {
                items.push({
                    name: Globalize.translate('ButtonInstantMix'),
                    id: 'instantmix',
                    ironIcon: 'shuffle'
                });
            }

            items.push({
                name: Globalize.translate('ButtonOpen'),
                id: 'open',
                ironIcon: 'folder-open'
            });

            if (options.showPlayOptions !== false) {

                if (MediaController.canPlayByAttributes(itemType, mediaType, playAccess, locationType)) {
                    items.push({
                        name: Globalize.translate('ButtonPlay'),
                        id: 'play',
                        ironIcon: 'play-arrow'
                    });

                    if (commands.indexOf('playfromhere') != -1) {
                        items.push({
                            name: Globalize.translate('ButtonPlayAllFromHere'),
                            id: 'playallfromhere',
                            ironIcon: 'play-arrow'
                        });
                    }
                }

                if (mediaType == 'Video' && AppInfo.supportsExternalPlayers && appSettings.enableExternalPlayers()) {
                    items.push({
                        name: Globalize.translate('ButtonPlayExternalPlayer'),
                        id: 'externalplayer',
                        ironIcon: 'airplay'
                    });
                }

                if (playbackPositionTicks && mediaType != "Audio") {
                    items.push({
                        name: Globalize.translate('ButtonResume'),
                        id: 'resume',
                        ironIcon: 'play-arrow'
                    });
                }

                if (commands.indexOf('trailer') != -1) {
                    items.push({
                        name: Globalize.translate('ButtonPlayTrailer'),
                        id: 'trailer',
                        ironIcon: 'play-arrow'
                    });
                }
            }

            if (MediaController.canQueueMediaType(mediaType, itemType)) {
                items.push({
                    name: Globalize.translate('ButtonQueue'),
                    id: 'queue',
                    ironIcon: 'playlist-add'
                });

                if (commands.indexOf('queuefromhere') != -1) {
                    items.push({
                        name: Globalize.translate('ButtonQueueAllFromHere'),
                        id: 'queueallfromhere',
                        ironIcon: 'playlist-add'
                    });
                }
            }

            if (commands.indexOf('shuffle') != -1) {
                items.push({
                    name: Globalize.translate('ButtonShuffle'),
                    id: 'shuffle',
                    ironIcon: 'shuffle'
                });
            }

            if (commands.indexOf('record') != -1) {
                items.push({
                    name: Globalize.translate('ButtonRecord'),
                    id: 'record',
                    ironIcon: 'videocam'
                });
            }

            if (commands.indexOf('removefromcollection') != -1) {
                items.push({
                    name: Globalize.translate('ButtonRemoveFromCollection'),
                    id: 'removefromcollection',
                    ironIcon: 'remove'
                });
            }

            if (commands.indexOf('removefromplaylist') != -1) {
                items.push({
                    name: Globalize.translate('ButtonRemoveFromPlaylist'),
                    id: 'removefromplaylist',
                    ironIcon: 'remove'
                });
            }

            if (user.Policy.EnablePublicSharing) {
                items.push({
                    name: Globalize.translate('ButtonShare'),
                    id: 'share',
                    ironIcon: 'share'
                });
            }

            if (commands.indexOf('sync') != -1) {
                items.push({
                    name: Globalize.translate('ButtonSync'),
                    id: 'sync',
                    ironIcon: 'sync'
                });
            }

            if (albumid) {
                items.push({
                    name: Globalize.translate('ButtonViewAlbum'),
                    id: 'album',
                    ironIcon: 'album'
                });
            }

            if (artistid) {
                items.push({
                    name: Globalize.translate('ButtonViewArtist'),
                    id: 'artist',
                    ironIcon: 'person'
                });
            }

            var href = card.getAttribute('data-href') || card.href;

            if (!href) {
                var links = card.getElementsByTagName('a');
                if (links.length) {
                    href = links[0].href;
                }
            }

            require(['actionsheet'], function (actionsheet) {

                actionsheet.show({
                    items: items,
                    positionTo: displayContextItem,
                    callback: function (id) {

                        switch (id) {

                            case 'addtocollection':
                                require(['collectioneditor'], function (collectioneditor) {

                                    new collectioneditor().show([itemId]);
                                });
                                break;
                            case 'playlist':
                                require(['playlistManager'], function (playlistManager) {

                                    playlistManager.showPanel([itemId]);
                                });
                                break;
                            case 'delete':
                                LibraryBrowser.deleteItems([itemId]);
                                break;
                            case 'download':
                                {
                                    require(['fileDownloader'], function (fileDownloader) {
                                        var downloadHref = ApiClient.getUrl("Items/" + itemId + "/Download", {
                                            api_key: ApiClient.accessToken()
                                        });

                                        fileDownloader([{
                                            url: downloadHref,
                                            itemId: itemId
                                        }]);
                                    });

                                    break;
                                }
                            case 'edit':
                                LibraryBrowser.editMetadata(itemId);
                                break;
                            case 'refresh':
                                ApiClient.refreshItem(itemId, {

                                    Recursive: true,
                                    ImageRefreshMode: 'FullRefresh',
                                    MetadataRefreshMode: 'FullRefresh',
                                    ReplaceAllImages: false,
                                    ReplaceAllMetadata: true
                                });
                                break;
                            case 'instantmix':
                                MediaController.instantMix(itemId);
                                break;
                            case 'shuffle':
                                MediaController.shuffle(itemId);
                                break;
                            case 'open':
                                Dashboard.navigate(href);
                                break;
                            case 'album':
                                Dashboard.navigate('itemdetails.html?id=' + albumid);
                                break;
                            case 'record':
                                require(['components/recordingcreator/recordingcreator'], function (recordingcreator) {
                                    recordingcreator.show(itemId);
                                });
                                break;
                            case 'artist':
                                Dashboard.navigate('itemdetails.html?context=music&id=' + artistid);
                                break;
                            case 'play':
                                MediaController.play(itemId);
                                break;
                            case 'playallfromhere':
                                playAllFromHere(index, parentWithClass(card, 'itemsContainer'), 'play');
                                break;
                            case 'queue':
                                MediaController.queue(itemId);
                                break;
                            case 'trailer':
                                ApiClient.getLocalTrailers(Dashboard.getCurrentUserId(), itemId).then(function (trailers) {
                                    MediaController.play({ items: trailers });
                                });
                                break;
                            case 'resume':
                                MediaController.play({
                                    ids: [itemId],
                                    startPositionTicks: playbackPositionTicks
                                });
                                break;
                            case 'queueallfromhere':
                                playAllFromHere(index, parentWithClass(card, 'itemsContainer'), 'queue');
                                break;
                            case 'sync':
                                require(['syncDialog'], function (syncDialog) {
                                    syncDialog.showMenu({
                                        items: [
                                        {
                                            Id: itemId
                                        }]
                                    });
                                });
                                break;
                            case 'editsubtitles':
                                LibraryBrowser.editSubtitles(itemId);
                                break;
                            case 'editimages':
                                LibraryBrowser.editImages(itemId);
                                break;
                            case 'externalplayer':
                                LibraryBrowser.playInExternalPlayer(itemId);
                                break;
                            case 'share':
                                require(['sharingmanager'], function () {
                                    SharingManager.showMenu(Dashboard.getCurrentUserId(), itemId);
                                });
                                break;
                            case 'removefromplaylist':
                                $(card).parents('.itemsContainer').trigger('removefromplaylist', [playlistItemId]);
                                break;
                            case 'removefromcollection':
                                $(card).parents('.collectionItems').trigger('removefromcollection', [itemId]);
                                break;
                            default:
                                break;
                        }
                    }
                });

            });
        });
    }

    function onListViewPlayButtonClick(e, playButton) {

        var card = e.target;

        if (!card.classList.contains('card') && !card.classList.contains('listItem')) {
            card = $(card).parents('.listItem,.card')[0];
        }

        var id = card.getAttribute('data-itemid');
        var type = card.getAttribute('data-itemtype');
        var isFolder = card.getAttribute('data-isfolder') == 'true';
        var mediaType = card.getAttribute('data-mediatype');
        var resumePosition = parseInt(card.getAttribute('data-positionticks'));

        if (type == 'MusicAlbum' || type == 'MusicArtist' || type == 'MusicGenre' || type == 'Playlist') {
            isFolder = true;
        }

        if (type == 'Program') {
            id = card.getAttribute('data-channelid');
        }

        LibraryBrowser.showPlayMenu(playButton, id, type, isFolder, mediaType, resumePosition);

        e.preventDefault();
        return false;
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

        var playButton = parentWithClass(e.target, 'cardOverlayPlayButton');

        if (playButton) {
            return onListViewPlayButtonClick(e, playButton);
        }

        var listviewMenuButton = parentWithClass(e.target, 'listviewMenuButton') || parentWithClass(e.target, 'cardOverlayMoreButton');

        if (listviewMenuButton) {
            showContextMenu(listviewMenuButton, {});

            e.preventDefault();
            return false;
        }

        var button = parentWithClass(e.target, 'btnUserItemRating');
        if (button) {
            e.stopPropagation();
            e.preventDefault();
            return false;
        }

        var card = parentWithClass(e.target, 'card');

        if (card) {

            var itemSelectionPanel = card.querySelector('.itemSelectionPanel');
            if (itemSelectionPanel) {
                return onItemSelectionPanelClick(e, itemSelectionPanel);
            }
            if (card.classList.contains('groupedCard')) {
                return onGroupedCardClick(e, card);
            }
        }
    }

    function onGroupedCardClick(e, card) {

        var itemId = card.getAttribute('data-itemid');
        var context = card.getAttribute('data-context');

        var userId = Dashboard.getCurrentUserId();

        var options = {

            Limit: parseInt($('.playedIndicator', card).html() || '10'),
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
            var commands = dataElement.getAttribute('data-commands').split(',');

            var promise1 = ApiClient.getItem(Dashboard.getCurrentUserId(), id);
            var promise2 = Dashboard.getCurrentUser();

            Promise.all([promise1, promise2]).then(function (responses) {

                var item = responses[0];
                var user = responses[1];

                var card = elem;

                while (!card.classList.contains('card')) {
                    card = card.parentNode;
                }

                innerElem.innerHTML = getOverlayHtml(item, user, card, commands);

                $('.btnPlayItem', innerElem).on('click', onPlayItemButtonClick);
                $('.btnPlayTrailer', innerElem).on('click', onTrailerButtonClick);
                $('.btnMoreCommands', innerElem).on('click', onMoreButtonClick);
            });

            $(innerElem).show();

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

        if (AppInfo.isTouchPreferred) {

            curr.removeEventListener('contextmenu', disableEvent);
            curr.addEventListener('contextmenu', disableEvent);
        }
        else {
            curr.removeEventListener('contextmenu', onContextMenu);
            curr.addEventListener('contextmenu', onContextMenu);

            curr.removeEventListener('mouseenter', onHoverIn);
            curr.addEventListener('mouseenter', onHoverIn, true);

            curr.removeEventListener('mouseleave', onHoverOut);
            curr.addEventListener('mouseleave', onHoverOut, true);

            curr.removeEventListener("touchstart", preventTouchHover);
            curr.addEventListener("touchstart", preventTouchHover);
        }

        initTapHoldMenus(curr);
    };

    $.fn.createCardMenus = function (options) {

        for (var i = 0, length = this.length; i < length; i++) {

            var curr = this[i];
            LibraryBrowser.createCardMenus(curr, options);
        }

        return this;
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
            manager.on('pressup', onTapHoldUp);
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

    function disableEvent(e) {
        e.preventDefault();
        e.stopPropagation();
        return false;
    }

    function onTapHold(e) {

        var card = parentWithClass(e.target, 'card');

        if (card) {

            showSelections(card);

            if (e.stopPropagation) {
                e.stopPropagation();
            }
            e.preventDefault();
            return false;
        }
        e.preventDefault();
        e.stopPropagation();
        return false;
    }

    function onTapHoldUp(e) {

        var itemSelectionPanel = parentWithClass(e.target, 'itemSelectionPanel');

        if (itemSelectionPanel) {
            if (!parentWithClass(e.target, 'chkItemSelect')) {
                var chkItemSelect = itemSelectionPanel.querySelector('.chkItemSelect');

                if (chkItemSelect) {
                    chkItemSelect.checked = !chkItemSelect.checked;
                }
            }
            e.preventDefault();
            return false;
        }
    }

    function onItemSelectionPanelClick(e, itemSelectionPanel) {

        // toggle the checkbox, if it wasn't clicked on
        if (!parentWithClass(e.target, 'chkItemSelect')) {
            var chkItemSelect = itemSelectionPanel.querySelector('.chkItemSelect');

            if (chkItemSelect) {
                var newValue = !chkItemSelect.checked;
                chkItemSelect.checked = newValue;
                updateItemSelection(chkItemSelect, newValue);
            }
        }

        e.preventDefault();
        e.stopPropagation();
        return false;
    }

    function onSelectionChange(e) {
        updateItemSelection(this, this.checked);
    }

    function showSelection(item) {

        var itemSelectionPanel = item.querySelector('.itemSelectionPanel');

        if (!itemSelectionPanel) {

            itemSelectionPanel = document.createElement('div');
            itemSelectionPanel.classList.add('itemSelectionPanel');

            item.querySelector('.cardContent').appendChild(itemSelectionPanel);

            var chkItemSelect = document.createElement('paper-checkbox');
            chkItemSelect.classList.add('chkItemSelect');

            $(chkItemSelect).on('change', onSelectionChange);

            itemSelectionPanel.appendChild(chkItemSelect);
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
            html += '<paper-icon-button class="btnCloseSelectionPanel" icon="close"></paper-icon-button>';
            html += '<span class="itemSelectionCount"></span>';
            html += '</div>';

            html += '<paper-icon-button class="btnSelectionPanelOptions" icon="more-vert" style="margin-left:auto;"></paper-icon-button>';

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
        return elem.animate(keyframes, timing);
    }

    function showSelections(initialCard) {

        require(['paper-checkbox'], function () {
            var cards = document.querySelectorAll('.card');
            for (var i = 0, length = cards.length; i < length; i++) {
                showSelection(cards[i]);
            }

            showSelectionCommands();
            initialCard.querySelector('.chkItemSelect').checked = true;
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

            if (user.Policy.EnableContentDownloading && AppInfo.supportsDownloading) {
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

                        switch (id) {

                            case 'addtocollection':
                                require(['collectioneditor'], function (collectioneditor) {

                                    new collectioneditor().show(items);
                                });
                                hideSelections();
                                break;
                            case 'playlist':
                                require(['playlistManager'], function (playlistManager) {

                                    playlistManager.showPanel(items);
                                    hideSelections();
                                });
                                break;
                            case 'delete':
                                LibraryBrowser.deleteItems(items).then(function () {
                                    Dashboard.navigate('home.html');
                                });
                                hideSelections();
                                break;
                            case 'groupvideos':
                                combineVersions($.mobile.activePage, items);
                                break;
                            case 'refresh':
                                items.map(function (itemId) {

                                    // TODO: Create an endpoint to do this in bulk
                                    ApiClient.refreshItem(itemId, {

                                        Recursive: true,
                                        ImageRefreshMode: 'FullRefresh',
                                        MetadataRefreshMode: 'FullRefresh',
                                        ReplaceAllImages: false,
                                        ReplaceAllMetadata: true
                                    });

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
                    $('.itemsContainer', page).trigger('needsrefresh');
                });
            });
        });
    }

    function onItemWithActionClick(e) {

        var elem = parentWithClass(e.target, 'itemWithAction');

        if (!elem) {
            return;
        }

        var action = elem.getAttribute('data-action');
        var elemWithAttributes = elem;

        if (action) {
            while (!elemWithAttributes.getAttribute('data-itemid')) {
                elemWithAttributes = elemWithAttributes.parentNode;
            }
        }

        var index;
        var itemsContainer;

        var itemId = elemWithAttributes.getAttribute('data-itemid');

        if (action == 'play') {
            MediaController.play(itemId);
        }
        else if (action == 'playallfromhere') {

            index = elemWithAttributes.getAttribute('data-index');

            itemsContainer = parentWithClass(elem, 'itemsContainer');

            playAllFromHere(index, itemsContainer, 'play');
        }
        else if (action == 'instantmix') {

            MediaController.instantMix(itemId);
        }

        e.stopPropagation();
        e.preventDefault();
        return false;
    }

    function playAllFromHere(index, itemsContainer, method) {

        var ids = $('.mediaItem', itemsContainer).get().map(function (i) {

            var node = i;
            var id = node.getAttribute('data-itemid');
            while (!id) {
                node = node.parentNode;
                id = node.getAttribute('data-itemid');
            }
            return id;
        });

        ids = ids.slice(index);

        ApiClient.getItems(Dashboard.getCurrentUserId(), {

            Ids: ids.join(','),
            Fields: 'MediaSources,Chapters',
            Limit: 100

        }).then(function (result) {

            MediaController[method]({
                items: result.Items
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

            if (LibraryBrowser.enableSync(item, user)) {
                $('.categorySyncButton', page).removeClass('hide');
            } else {
                $('.categorySyncButton', page).addClass('hide');
            }
        });
    }

    function onCategorySyncButtonClick(page, button) {

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

        page.addEventListener('click', onItemWithActionClick);

        var itemsContainers = page.querySelectorAll('.itemsContainer:not(.noautoinit)');
        for (var i = 0, length = itemsContainers.length; i < length; i++) {
            LibraryBrowser.createCardMenus(itemsContainers[i]);
        }

        $('.categorySyncButton', page).on('click', function () {

            onCategorySyncButtonClick(page, this);
        });

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

    function renderUserDataChanges(card, userData) {

        if (userData.Played) {

            var playedIndicator = card.querySelector('.playedIndicator');

            if (!playedIndicator) {

                playedIndicator = document.createElement('div');
                playedIndicator.classList.add('playedIndicator');
                card.querySelector('.cardContent').appendChild(playedIndicator);
            }
            playedIndicator.innerHTML = '<iron-icon icon="check"></iron-icon>';
        }
        else if (userData.UnplayedItemCount) {

            var playedIndicator = card.querySelector('.playedIndicator');

            if (!playedIndicator) {

                playedIndicator = document.createElement('div');
                playedIndicator.classList.add('playedIndicator');
                card.querySelector('.cardContent').appendChild(playedIndicator);
            }
            playedIndicator.innerHTML = userData.UnplayedItemCount;
        }

        var progressHtml = LibraryBrowser.getItemProgressBarHtml(userData);

        if (progressHtml) {
            var cardProgress = card.querySelector('.cardProgress');

            if (!cardProgress) {
                cardProgress = document.createElement('div');
                cardProgress.classList.add('cardProgress');

                $('.cardFooter', card).append(cardProgress);
            }

            cardProgress.innerHTML = progressHtml;
        }
        else {
            $('.cardProgress', card).remove();
        }
    }

    function onUserDataChanged(userData) {

        $(document.querySelectorAll("*[data-itemid='" + userData.ItemId + "']")).each(function () {

            var mediaType = this.getAttribute('data-mediatype');

            if (mediaType == 'Video') {
                this.setAttribute('data-positionticks', (userData.PlaybackPositionTicks || 0));

                if (this.classList.contains('card')) {
                    renderUserDataChanges(this, userData);
                }
            }
        });
    }

    function onWebSocketMessage(e, data) {

        var msg = data;

        if (msg.MessageType === "UserDataChanged") {

            if (msg.Data.UserId == Dashboard.getCurrentUserId()) {

                for (var i = 0, length = msg.Data.UserDataList.length; i < length; i++) {
                    onUserDataChanged(msg.Data.UserDataList[i]);
                }
            }
        }

    }

    function initializeApiClient(apiClient) {
        Events.off(apiClient, "websocketmessage", onWebSocketMessage);
        Events.on(apiClient, "websocketmessage", onWebSocketMessage);
    }

    function clearRefreshTimes() {
        $('.hasrefreshtime').removeClass('hasrefreshtime').removeAttr('data-lastrefresh');
    }

    if (window.ApiClient) {
        initializeApiClient(window.ApiClient);
    }

    Events.on(ConnectionManager, 'apiclientcreated', function (e, apiClient) {
        initializeApiClient(apiClient);
    });

    Events.on(ConnectionManager, 'localusersignedin', clearRefreshTimes);
    Events.on(ConnectionManager, 'localusersignedout', clearRefreshTimes);

});