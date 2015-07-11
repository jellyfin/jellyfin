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

        html += '<paper-icon-button icon="more-vert" class="btnMoreCommands"></paper-icon-button>';
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

    function onCardTapHold(e) {

        showContextMenu(this, {});

        e.preventDefault();
        return false;
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
                    ironIcon: 'refresh'
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
                                BoxSetEditor.showPanel([itemId]);
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
                                Dashboard.navigate('edititemmetadata.html?id=' + itemId);
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
                            case 'artist':
                                Dashboard.navigate('itembynamedetails.html?context=music&id=' + artistid);
                                break;
                            case 'play':
                                MediaController.play(itemId);
                                break;
                            case 'playallfromhere':
                                $(card).parents('.itemsContainer').trigger('playallfromhere', [index]);
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
                                $(card).parents('.itemsContainer').trigger('queueallfromhere', [index]);
                                break;
                            case 'sync':
                                SyncManager.showMenu({
                                    items: [
                                    {
                                        Id: itemId
                                    }]
                                });
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
                            default:
                                break;
                        }
                    }
                });

            });
        });
    }

    function onListViewMenuButtonClick(e) {

        showContextMenu(this, {});

        e.preventDefault();
        return false;
    }

    function onListViewPlayButtonClick(e) {

        var playButton = this;
        var card = this;

        if (!card.classList.contains('card') && !card.classList.contains('listItem')) {
            card = $(card).parents('.listItem,.card')[0];
        }

        var id = card.getAttribute('data-itemid');
        var type = card.getAttribute('data-itemtype');
        var isFolder = card.getAttribute('data-isfolder') == 'true';
        var mediaType = card.getAttribute('data-mediatype');
        var resumePosition = parseInt(card.getAttribute('data-resumeposition'));

        if (type == 'MusicAlbum' || type == 'MusicArtist' || type == 'MusicGenre' || type == 'Playlist') {
            isFolder = true;
        }

        LibraryBrowser.showPlayMenu(playButton, id, type, isFolder, mediaType, resumePosition);

        e.preventDefault();
        return false;
    }

    function isClickable(target) {

        while (target != null) {
            var tagName = target.tagName || '';
            if (tagName == 'A' || tagName.indexOf('BUTTON') != -1) {
                return true;
            }

            return false;
            //target = target.parentNode;
        }

        return false;
    }

    function onGroupedCardClick(e) {

        var card = this;
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
        html += '<button type="button" class="imageButton detailsMenuLeftButton" data-role="none"><i class="fa fa-arrow-left"></i></button>';
        html += '<h3 style="font-weight:400;margin:.5em 0;"></h3>';
        html += '<button type="button" class="imageButton detailsMenuRightButton" data-role="none"><i class="fa fa-arrow-right"></i></button>';
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

            contentHtml += '<paper-button data-href="' + LibraryBrowser.getHref(item, context) + '" raised class="submit btnSync" style="background-color: #673AB7;" onclick="Dashboard.navigate(this.getAttribute(\'data-href\'));"><iron-icon icon="folder-open"></iron-icon><span>' + Globalize.translate('ButtonOpen') + '</span></paper-button>';

            if (SyncManager.isAvailable(item, user)) {
                contentHtml += '<paper-button raised class="submit btnSync"><iron-icon icon="refresh"></iron-icon><span>' + Globalize.translate('ButtonSync') + '</span></paper-button>';
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

        var elem = getItemsOverlay(options.ids, context);

        setItemIntoOverlay(elem, 0);
    }

    function onCardClick(e) {

        var targetElem = e.target;
        if (targetElem.classList.contains('itemSelectionPanel') || this.querySelector('.itemSelectionPanel')) {
            return false;
        }

        var info = LibraryBrowser.getListItemInfo(this);
        var itemId = info.id;
        var context = info.context;

        var card = this;

        if (card.classList.contains('itemWithAction')) {
            return;
        }

        if (!card.classList.contains('card')) {
            card = $(card).parents('.card')[0];
        }

        if (card.classList.contains('groupedCard')) {
            return;
        }

        if (card.getAttribute('data-detailsmenu') != 'true') {
            return;
        }

        if (isClickable(targetElem)) {
            return;
        }

        var target = $(targetElem);
        if (target.parents('a').length || target.parents('button').length) {
            return;
        }

        if (AppSettings.enableItemPreviews()) {
            showItemsOverlay({
                ids: [itemId],
                context: context
            });

            return false;
        }
    }

    $.fn.createCardMenus = function (options) {

        var preventHover = false;

        function onShowTimerExpired(elem) {

            elem = elem.querySelector('a');

            if ($('.itemSelectionPanel:visible', elem).length) {
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

        this.off('contextmenu', '.card', onCardTapHold);
        this.on('contextmenu', '.card', onCardTapHold);

        this.off('click', '.groupedCard', onGroupedCardClick);
        this.on('click', '.groupedCard', onGroupedCardClick);

        this.off('click', '.listviewMenuButton', onListViewMenuButtonClick);
        this.on('click', '.listviewMenuButton', onListViewMenuButtonClick);

        this.off('click', '.cardOverlayMoreButton', onListViewMenuButtonClick);
        this.on('click', '.cardOverlayMoreButton', onListViewMenuButtonClick);

        this.off('click', '.cardOverlayPlayButton', onListViewPlayButtonClick);
        this.on('click', '.cardOverlayPlayButton', onListViewPlayButtonClick);

        if (!AppInfo.isTouchPreferred) {
            this.off('mouseenter', '.card:not(.bannerCard) .cardContent', onHoverIn);
            this.on('mouseenter', '.card:not(.bannerCard) .cardContent', onHoverIn);

            this.off('mouseleave', '.card:not(.bannerCard) .cardContent', onHoverOut);
            this.on('mouseleave', '.card:not(.bannerCard) .cardContent', onHoverOut);

            this.off("touchstart", '.card:not(.bannerCard) .cardContent', preventTouchHover);
            this.on("touchstart", '.card:not(.bannerCard) .cardContent', preventTouchHover);
        }

        this.off('click', '.mediaItem', onCardClick);
        this.on('click', '.mediaItem', onCardClick);

        return this;
    };

    function toggleSelections(page) {

        Dashboard.showLoadingMsg();

        var selectionCommands = $('.selectionCommands', page);

        if (selectionCommands.is(':visible')) {

            selectionCommands.hide();
            $('.itemSelectionPanel', page).hide();

        } else {

            selectionCommands.show();

            var panels = $('.itemSelectionPanel', page).show();

            if (!panels.length) {

                var index = 0;
                $('.cardContent', page).each(function () {
                    var chkItemSelectId = 'chkItemSelect' + index;

                    $(this).append('<div class="itemSelectionPanel" onclick="return false;"><div class="ui-checkbox"><label class="ui-btn ui-corner-all ui-btn-inherit ui-btn-icon-left ui-checkbox-off" for="' + chkItemSelectId + '">Select</label><input id="' + chkItemSelectId + '" type="checkbox" class="chkItemSelect" data-enhanced="true" /></div></div>');
                    index++;
                });

                $('.itemsContainer', page).trigger('create');
            }

            $('.chkItemSelect:checked', page).checked(false).checkboxradio('refresh');
        }

        Dashboard.hideLoadingMsg();
    }

    function hideSelections(page) {

        var selectionCommands = page.querySelector('.selectionCommands');
        if (selectionCommands) {
            selectionCommands.style.display = 'none';
        }

        var elems = page.getElementsByClassName('itemSelectionPanel');
        for (var i = 0, length = elems.length; i < length; i++) {
            elems[i].style.display = 'none';
        }
    }

    function getSelectedItems(page) {

        var selection = $('.chkItemSelect:checked', page);

        return selection.parents('.card')
            .map(function () {

                return this.getAttribute('data-itemid');

            }).get();
    }

    function onSyncJobListSubmit() {

        hideSelections($($.mobile.activePage)[0]);
    }

    function sync(page) {

        var selection = getSelectedItems(page);

        if (selection.length < 1) {

            Dashboard.alert({
                message: Globalize.translate('MessagePleaseSelectOneItem'),
                title: Globalize.translate('HeaderError')
            });

            return;
        }

        SyncManager.showMenu({
            items: selection
        });

        Events.off(SyncManager, 'jobsubmit', onSyncJobListSubmit);
        Events.on(SyncManager, 'jobsubmit', onSyncJobListSubmit);
    }

    function combineVersions(page) {

        var selection = getSelectedItems(page);

        if (selection.length < 2) {

            Dashboard.alert({
                message: Globalize.translate('MessagePleaseSelectTwoItems'),
                title: Globalize.translate('HeaderError')
            });

            return;
        }

        var names = $('.chkItemSelect:checked', page).parents('.card').get().reverse().map(function (e) {

            return $('.cardText', e).html();

        }).join('<br/>');

        var msg = Globalize.translate('MessageTheFollowingItemsWillBeGrouped') + "<br/><br/>" + names;

        msg += "<br/><br/>" + Globalize.translate('MessageConfirmItemGrouping');

        Dashboard.confirm(msg, Globalize.translate('HeaderGroupVersions'), function (confirmResult) {

            if (confirmResult) {

                Dashboard.showLoadingMsg();

                ApiClient.ajax({

                    type: "POST",
                    url: ApiClient.getUrl("Videos/MergeVersions", { Ids: selection.join(',') })

                }).done(function () {

                    Dashboard.hideLoadingMsg();

                    hideSelections(page);

                    $('.itemsContainer', page).trigger('needsrefresh');
                });
            }
        });
    }

    function addToCollection(page) {

        var selection = getSelectedItems(page);

        if (selection.length < 1) {

            Dashboard.alert({
                message: Globalize.translate('MessagePleaseSelectOneItem'),
                title: Globalize.translate('HeaderError')
            });

            return;
        }

        BoxSetEditor.showPanel(selection);
    }

    function addToPlaylist(page) {

        var selection = getSelectedItems(page);

        if (selection.length < 1) {

            Dashboard.alert({
                message: Globalize.translate('MessagePleaseSelectOneItem'),
                title: Globalize.translate('HeaderError')
            });

            return;
        }

        PlaylistManager.showPanel(selection);
    }

    function onListviewSubLinkClick(e) {

        var elem = e.target;
        Dashboard.navigate(elem.getAttribute('data-href'));
        return false;
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

            itemsContainer.trigger('playallfromhere', [index]);
        }

        return false;
    }

    $(document).on('pageinitdepends', ".libraryPage", function () {

        var page = this;

        var btnAddToPlaylist = page.querySelector('.btnAddToPlaylist');
        if (btnAddToPlaylist) {
            Events.on(btnAddToPlaylist, 'click', function () {
                addToPlaylist(page);
            });
        }

        var btnMergeVersions = page.querySelector('.btnMergeVersions');
        if (btnMergeVersions) {
            Events.on(btnMergeVersions, 'click', function () {
                combineVersions(page);
            });
        }

        var btnSyncItems = page.querySelector('.btnSyncItems');
        if (btnSyncItems) {
            Events.on(btnSyncItems, 'click', function () {
                sync(page);
            });
        }

        var btnAddToCollection = page.querySelector('.btnAddToCollection');
        if (btnAddToCollection) {
            Events.on(btnAddToCollection, 'click', function () {
                addToCollection(page);
            });
        }

        $(page.getElementsByClassName('viewTabButton')).on('click', function () {

            $('.viewTabButton', page).removeClass('ui-btn-active');
            this.classList.add('ui-btn-active');

            $('.viewTab', page).hide();
            $('.' + this.getAttribute('data-tab'), page).show();
        });

        var viewPanel = $('.viewPanel', page);

        $('#selectPageSize', viewPanel).html(LibraryBrowser.getDefaultPageSizeSelections().map(function (i) {

            return '<option value="' + i + '">' + i + '</option>';

        }).join('')).selectmenu('refresh');

        $(page).on('click', '.btnToggleSelections', function () {

            toggleSelections(page);

        }).on('click', '.itemWithAction', onItemWithActionClick).on('click', '.listviewSubLink', onListviewSubLinkClick);

        var itemsContainers = page.getElementsByClassName('itemsContainer');
        for (var i = 0, length = itemsContainers.length; i < length; i++) {
            $(itemsContainers[i]).createCardMenus();
        }

    }).on('pagebeforeshowready', ".libraryPage", function () {

        var page = this;

        hideSelections(page);

        var elem = page.querySelector('.viewTabButton');
        if (elem) {
            Events.trigger(elem, 'click');
        }

    });

    function renderUserDataChanges(card, userData) {

        if (userData.Played) {

            if (!$('.playedIndicator', card).length) {

                $('<div class="playedIndicator"></div>').insertAfter($('.cardOverlayTarget', card));
            }
            $('.playedIndicator', card).html('<iron-icon icon="check"></iron-icon>');
            $('.cardProgress', card).remove();
        }
        else if (userData.UnplayedItemCount) {

            if (!$('.playedIndicator', card).length) {

                $('<div class="playedIndicator"></div>').insertAfter($('.cardOverlayTarget', card));
            }
            $('.playedIndicator', card).html(userData.UnplayedItemCount);
        }
        else {

            $('.playedIndicator', card).remove();

            var progressHtml = LibraryBrowser.getItemProgressBarHtml(userData);

            $('.cardProgress', card).html(progressHtml);
        }
    }

    function onUserDataChanged(userData) {

        var cssClass = LibraryBrowser.getUserDataCssClass(userData.Key);

        if (!cssClass) {
            return;
        }

        $('.' + cssClass).each(function () {

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