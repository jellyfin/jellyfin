(function ($, document, window) {

    var showOverlayTimeout;

    function onHoverOut() {

        if (showOverlayTimeout) {
            clearTimeout(showOverlayTimeout);
            showOverlayTimeout = null;
        }

        var elem = this.querySelector('.cardOverlayTarget');

        if ($(elem).is(':visible')) {
            require(["jquery", "velocity"], function ($, Velocity) {

                Velocity.animate(elem, { "height": "0" },
                {
                    complete: function () {
                        $(elem).hide();
                    }
                });
            });
        }
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
        var maxLogoWidth = isPortrait ? 100 : 200;
        var imgUrl;

        if (parentName && item.ParentLogoItemId) {

            imgUrl = ApiClient.getScaledImageUrl(item.ParentLogoItemId, {
                height: logoHeight,
                type: 'logo',
                tag: item.ParentLogoImageTag
            });

            html += '<img src="' + imgUrl + '" style="max-height:' + logoHeight + 'px;max-width:100%;" />';

        }
        else if (item.ImageTags.Logo) {

            imgUrl = ApiClient.getScaledImageUrl(item.Id, {
                height: logoHeight,
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

        ApiClient.getLocalTrailers(Dashboard.getCurrentUserId(), id).done(function (trailers) {
            MediaController.play({ items: trailers });
        });

        return false;
    }

    function onPlayItemButtonClick() {

        var id = this.getAttribute('data-itemid');
        var type = this.getAttribute('data-itemtype');
        var isFolder = this.getAttribute('data-isfolder') == 'true';
        var mediaType = this.getAttribute('data-mediatype');
        var resumePosition = parseInt(this.getAttribute('data-resumeposition'));

        LibraryBrowser.showPlayMenu(this, id, type, isFolder, mediaType, resumePosition);

        return false;
    }

    function onMoreButtonClick() {

        var card = $(this).parents('.card')[0];

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

        Dashboard.getCurrentUser().done(function (user) {

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

            if (commands.indexOf('delete') != -1) {
                items.push({
                    name: Globalize.translate('ButtonDelete'),
                    id: 'delete',
                    ironIcon: 'delete'
                });
            }

            if (user.Policy.IsAdministrator && commands.indexOf('edit') != -1) {
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

                if (mediaType == 'Video' && AppSettings.enableExternalPlayers()) {
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

            require(['actionsheet'], function () {

                ActionSheetElement.show({
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
                                PlaylistManager.showPanel([itemId]);
                                break;
                            case 'delete':
                                LibraryBrowser.deleteItem(itemId);
                                break;
                            case 'download':
                                {
                                    var downloadHref = ApiClient.getUrl("Items/" + itemId + "/Download", {
                                        api_key: ApiClient.accessToken()
                                    });
                                    window.location.href = downloadHref;

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
                                Dashboard.navigate('livetvnewrecording.html?programid=' + itemId);
                                break;
                            case 'artist':
                                Dashboard.navigate('itemdetails.html?context=music&id=' + artistid);
                                break;
                            case 'play':
                                MediaController.play(itemId);
                                break;
                            case 'playallfromhere':
                                playAllFromHere(index, $(card).parents('.itemsContainer'), 'play');
                                break;
                            case 'queue':
                                MediaController.queue(itemId);
                                break;
                            case 'trailer':
                                ApiClient.getLocalTrailers(Dashboard.getCurrentUserId(), itemId).done(function (trailers) {
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
                                playAllFromHere(index, $(card).parents('.itemsContainer'), 'queue');
                                break;
                            case 'sync':
                                SyncManager.showMenu({
                                    items: [
                                    {
                                        Id: itemId
                                    }]
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

        var buttonParents = $(target).parents('a:not(.card,.cardContent),button:not(.card,.cardContent)');
        if (buttonParents.length) {
            return;
        }

        ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options)).done(function (items) {

            if (items.length == 1) {

                Dashboard.navigate(LibraryBrowser.getHref(items[0], context));
                return;
            }

            var url = 'itemdetails.html?id=' + itemId;
            if (context) {
                url += '&context=' + context;
            }
            Dashboard.navigate(url);
            return;

            var ids = items.map(function (i) {
                return i.Id;
            });

            showItemsOverlay({
                ids: ids,
                context: context
            });
        });

        e.preventDefault();
        return false;
    }

    function getItemsOverlay(ids, context) {

        $('.detailsMenu').remove();

        var html = '<div data-role="popup" class="detailsMenu" style="border:0;padding:0;" data-ids="' + ids.join(',') + '" data-context="' + (context || '') + '">';

        html += '<div style="padding:1em 1em;background:rgba(20,20,20,1);margin:0;text-align:center;" class="detailsMenuHeader">';
        html += '<paper-icon-button icon="keyboard-arrow-left" class="detailsMenuLeftButton"></paper-icon-button>';
        html += '<h3 style="font-weight:400;margin:.5em 0;"></h3>';
        html += '<paper-icon-button icon="keyboard-arrow-right" class="detailsMenuRightButton"></paper-icon-button>';
        html += '</div>';

        html += '<div class="detailsMenuContent" style="background-position:center center;background-repeat:no-repeat;background-size:cover;">';
        html += '<div style="padding:.5em 1em 1em;background:rgba(10,10,10,.80);" class="detailsMenuContentInner">';
        html += '</div>';
        html += '</div>';

        html += '</div>';

        $($.mobile.activePage).append(html);

        var elem = $('.detailsMenu').popup().trigger('create').popup("open").on("popupafterclose", function () {

            $(this).off("popupafterclose").remove();
        })[0];

        $('.detailsMenuLeftButton', elem).on('click', function () {

            var overlay = $(this).parents('.detailsMenu')[0];
            setItemIntoOverlay(overlay, parseInt(overlay.getAttribute('data-index') || '0') - 1, context);
        });

        $('.detailsMenuRightButton', elem).on('click', function () {

            var overlay = $(this).parents('.detailsMenu')[0];
            setItemIntoOverlay(overlay, parseInt(overlay.getAttribute('data-index') || '0') + 1, context);
        });

        return elem;
    }

    function setItemIntoOverlay(elem, index) {

        var ids = elem.getAttribute('data-ids').split(',');
        var itemId = ids[index];
        var userId = Dashboard.getCurrentUserId();
        var context = elem.getAttribute('data-context');

        elem.setAttribute('data-index', index);

        if (index > 0) {
            $('.detailsMenuLeftButton', elem).show();
        } else {
            $('.detailsMenuLeftButton', elem).hide();
        }

        if (index < ids.length - 1) {
            $('.detailsMenuRightButton', elem).show();
        } else {
            $('.detailsMenuRightButton', elem).hide();
        }

        var promise1 = ApiClient.getItem(userId, itemId);
        var promise2 = Dashboard.getCurrentUser();

        $.when(promise1, promise2).done(function (response1, response2) {

            var item = response1[0];
            var user = response2[0];

            var background = 'none';

            if (AppInfo.enableDetailsMenuImages) {
                var backdropUrl;
                var screenWidth = $(window).width();
                var backdropWidth = Math.min(screenWidth, 800);

                if (item.BackdropImageTags && item.BackdropImageTags.length) {

                    backdropUrl = ApiClient.getScaledImageUrl(item.Id, {
                        type: "Backdrop",
                        index: 0,
                        maxWidth: backdropWidth,
                        tag: item.BackdropImageTags[0]
                    });
                }
                else if (item.ParentBackdropItemId && item.ParentBackdropImageTags && item.ParentBackdropImageTags.length) {

                    backdropUrl = ApiClient.getScaledImageUrl(item.ParentBackdropItemId, {
                        type: 'Backdrop',
                        index: 0,
                        tag: item.ParentBackdropImageTags[0],
                        maxWidth: backdropWidth
                    });
                }

                if (backdropUrl) {
                    background = 'url(' + backdropUrl + ')';
                }
            }

            $('.detailsMenuContent', elem).css('backgroundImage', background);

            var headerHtml = LibraryBrowser.getPosterViewDisplayName(item);
            $('.detailsMenuHeader', elem).removeClass('detailsMenuHeaderWithLogo');
            if (AppInfo.enableDetailsMenuImages) {

                var logoUrl;

                var logoHeight = 30;
                if (item.ImageTags && item.ImageTags.Logo) {

                    logoUrl = ApiClient.getScaledImageUrl(item.Id, {
                        type: "Logo",
                        index: 0,
                        height: logoHeight,
                        tag: item.ImageTags.Logo
                    });
                }

                if (logoUrl) {
                    headerHtml = '<img src="' + logoUrl + '" style="height:' + logoHeight + 'px;" />';
                    $('.detailsMenuHeader', elem).addClass('detailsMenuHeaderWithLogo');
                }
            }

            $('h3', elem).html(headerHtml);

            var contentHtml = '';

            var miscInfo = LibraryBrowser.getMiscInfoHtml(item);
            if (miscInfo) {

                contentHtml += '<p>' + miscInfo + '</p>';
            }

            var userData = LibraryBrowser.getUserDataIconsHtml(item);
            if (userData) {

                contentHtml += '<p class="detailsMenuUserData">' + userData + '</p>';
            }

            var ratingHtml = LibraryBrowser.getRatingHtml(item);
            if (ratingHtml) {

                contentHtml += '<p>' + ratingHtml + '</p>';
            }

            if (item.Overview) {
                contentHtml += '<p class="detailsMenuOverview">' + item.Overview + '</p>';
            }

            contentHtml += '<div class="detailsMenuButtons">';

            if (MediaController.canPlay(item)) {
                if (item.MediaType == 'Video' && !item.IsFolder && item.UserData && item.UserData.PlaybackPositionTicks) {
                    contentHtml += '<paper-button raised class="secondary btnResume" style="background-color:#ff8f00;"><iron-icon icon="play-arrow"></iron-icon><span>' + Globalize.translate('ButtonResume') + '</span></paper-button>';
                }

                contentHtml += '<paper-button raised class="secondary btnPlay"><iron-icon icon="play-arrow"></iron-icon><span>' + Globalize.translate('ButtonPlay') + '</span></paper-button>';
            }

            contentHtml += '<paper-button data-href="' + LibraryBrowser.getHref(item, context) + '" raised class="submit" style="background-color: #673AB7;" onclick="Dashboard.navigate(this.getAttribute(\'data-href\'));"><iron-icon icon="folder-open"></iron-icon><span>' + Globalize.translate('ButtonOpen') + '</span></paper-button>';

            if (SyncManager.isAvailable(item, user)) {
                contentHtml += '<paper-button raised class="submit btnSync"><iron-icon icon="sync"></iron-icon><span>' + Globalize.translate('ButtonSync') + '</span></paper-button>';
            }

            contentHtml += '</div>';

            $('.detailsMenuContentInner', elem).html(contentHtml).trigger('create');

            $('.btnSync', elem).on('click', function () {

                $(elem).popup('close');

                SyncManager.showMenu({
                    items: [item]
                });
            });

            $('.btnPlay', elem).on('click', function () {

                $(elem).popup('close');

                MediaController.play({
                    items: [item]
                });
            });

            $('.btnResume', elem).on('click', function () {

                $(elem).popup('close');

                MediaController.play({
                    items: [item],
                    startPositionTicks: item.UserData.PlaybackPositionTicks
                });
            });
        });
    }

    function showItemsOverlay(options) {

        var context = options.context;

        require(['jqmpopup'], function () {
            var elem = getItemsOverlay(options.ids, context);

            setItemIntoOverlay(elem, 0);
        });
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

    $.fn.createCardMenus = function (options) {

        var preventHover = false;

        function onShowTimerExpired(elem) {

            elem = elem.querySelector('a');

            if (elem.querySelector('.itemSelectionPanel')) {
                return;
            }

            var innerElem = elem.querySelector('.cardOverlayTarget');

            var dataElement = elem;
            while (dataElement && !dataElement.getAttribute('data-itemid')) {
                dataElement = dataElement.parentNode;
            }

            var id = dataElement.getAttribute('data-itemid');
            var commands = dataElement.getAttribute('data-commands').split(',');

            var promise1 = ApiClient.getItem(Dashboard.getCurrentUserId(), id);
            var promise2 = Dashboard.getCurrentUser();

            $.when(promise1, promise2).done(function (response1, response2) {

                var item = response1[0];
                var user = response2[0];

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
            innerElem.style.height = '0';

            require(["jquery", "velocity"], function ($, Velocity) {

                Velocity.animate(innerElem, { "height": "100%" }, "fast");
            });
        }

        function onHoverIn(e) {

            if (preventHover === true) {
                preventHover = false;
                return;
            }

            if (showOverlayTimeout) {
                clearTimeout(showOverlayTimeout);
                showOverlayTimeout = null;
            }

            var elem = this;

            while (!elem.classList.contains('card')) {
                elem = elem.parentNode;
            }

            showOverlayTimeout = setTimeout(function () {

                onShowTimerExpired(elem);

            }, 1000);
        }

        function preventTouchHover() {
            preventHover = true;
        }

        this.off('click', onCardClick);
        this.on('click', onCardClick);

        if (AppInfo.isTouchPreferred) {
            this.off('contextmenu', disableEvent);
            this.on('contextmenu', disableEvent);
            //this.off('contextmenu', onContextMenu);
            //this.on('contextmenu', onContextMenu);
        }
        else {
            this.off('contextmenu', onContextMenu);
            this.on('contextmenu', onContextMenu);

            this.off('mouseenter', '.card:not(.bannerCard) .cardContent', onHoverIn);
            this.on('mouseenter', '.card:not(.bannerCard) .cardContent', onHoverIn);

            this.off('mouseleave', '.card:not(.bannerCard) .cardContent', onHoverOut);
            this.on('mouseleave', '.card:not(.bannerCard) .cardContent', onHoverOut);

            this.off("touchstart", '.card:not(.bannerCard) .cardContent', preventTouchHover);
            this.on("touchstart", '.card:not(.bannerCard) .cardContent', preventTouchHover);
        }

        for (var i = 0, length = this.length; i < length; i++) {
            initTapHoldMenus(this[i]);
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

        require(['hammer'], function (Hammer) {

            var hammertime = new Hammer(element);

            hammertime.on('press', onTapHold);
            hammertime.on('pressup', onTapHoldUp);
        });
        showTapHoldHelp(element);
    }

    function showTapHoldHelp(element) {

        var page = $(element).parents('.page')[0];

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
        return false;
    }

    function onTapHold(e) {

        var card = parentWithClass(e.target, 'card');

        if (card) {

            showSelections(card);

            e.preventDefault();
            return false;
        }
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

            html += '<paper-icon-button class="btnSelectionPanelOptions" icon="more-vert" style="float:right;"></paper-icon-button>';

            selectionCommandsPanel.innerHTML = html;

            $('.btnCloseSelectionPanel', selectionCommandsPanel).on('click', hideSelections);

            var btnSelectionPanelOptions = selectionCommandsPanel.querySelector('.btnSelectionPanelOptions');

            $(btnSelectionPanelOptions).on('click', showMenuForSelectedItems);

            if (!$.browser.mobile) {
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

        var cards = document.querySelectorAll('.card');
        for (var i = 0, length = cards.length; i < length; i++) {
            showSelection(cards[i]);
        }

        showSelectionCommands();
        initialCard.querySelector('.chkItemSelect').checked = true;
        updateItemSelection(initialCard, true);
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

        Dashboard.getCurrentUser().done(function (user) {

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

            require(['actionsheet'], function () {

                ActionSheetElement.show({
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
                                PlaylistManager.showPanel(items);
                                hideSelections();
                                break;
                            case 'groupvideos':
                                combineVersions($($.mobile.activePage)[0], items);
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
                                SyncManager.showMenu({
                                    items: items.map(function (i) {
                                        return {
                                            Id: i
                                        };
                                    })
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

        Dashboard.confirm(msg, Globalize.translate('HeaderGroupVersions'), function (confirmResult) {

            if (confirmResult) {

                Dashboard.showLoadingMsg();

                ApiClient.ajax({

                    type: "POST",
                    url: ApiClient.getUrl("Videos/MergeVersions", { Ids: selection.join(',') })

                }).done(function () {

                    Dashboard.hideLoadingMsg();
                    hideSelections();
                    $('.itemsContainer', page).trigger('needsrefresh');
                });
            }
        });
    }

    function onItemWithActionClick(e) {

        var elem = this;

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

            itemsContainer = $(elem).parents('.itemsContainer');

            playAllFromHere(index, itemsContainer, 'play');
        }
        else if (action == 'instantmix') {

            MediaController.instantMix(itemId);
        }

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

        }).done(function (result) {

            MediaController[method]({
                items: result.Items
            });
        });
    }

    pageClassOn('pageinit', "libraryPage", function () {

        var page = this;

        $(page).on('click', '.itemWithAction', onItemWithActionClick);

        var itemsContainers = page.querySelectorAll('.itemsContainer:not(.noautoinit)');
        for (var i = 0, length = itemsContainers.length; i < length; i++) {
            $(itemsContainers[i]).createCardMenus();
        }

    });

    pageClassOn('pagebeforehide', "libraryPage", function () {

        var page = this;

        hideSelections();
    });

    function renderUserDataChanges(card, userData) {

        if (userData.Played) {

            if (!$('.playedIndicator', card).length) {

                $('<div class="playedIndicator"></div>').insertAfter($('.cardOverlayTarget', card));
            }
            $('.playedIndicator', card).html('<iron-icon icon="check"></iron-icon>');
        }
        else if (userData.UnplayedItemCount) {

            if (!$('.playedIndicator', card).length) {

                $('<div class="playedIndicator"></div>').insertAfter($('.cardOverlayTarget', card));
            }
            $('.playedIndicator', card).html(userData.UnplayedItemCount);
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

                if ($(this).hasClass('card')) {
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
        $(apiClient).off('websocketmessage', onWebSocketMessage).on('websocketmessage', onWebSocketMessage);
    }

    function clearRefreshTimes() {
        $('.hasrefreshtime').removeClass('hasrefreshtime').removeAttr('data-lastrefresh');
    }

    Dashboard.ready(function () {

        if (window.ApiClient) {
            initializeApiClient(window.ApiClient);
        }

        $(ConnectionManager).on('apiclientcreated', function (e, apiClient) {
            initializeApiClient(apiClient);
        });

        Events.on(ConnectionManager, 'localusersignedin', clearRefreshTimes);
        Events.on(ConnectionManager, 'localusersignedout', clearRefreshTimes);
    });

})(jQuery, document, window);