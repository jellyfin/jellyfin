(function ($, document, window) {

    var showOverlayTimeout;

    function onHoverOut() {

        if (showOverlayTimeout) {
            clearTimeout(showOverlayTimeout);
            showOverlayTimeout = null;
        }

        $('.cardOverlayTarget:visible', this).each(function () {

            var elem = this;

            $(this).animate({ "height": "0" }, "fast", function () {

                $(elem).hide();

            });

        });

        $('.cardOverlayTarget:visible', this).stop().animate({ "height": "0" }, function () {

            $(this).hide();

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

            html += '<button type="button" class="btnPlayItem" data-itemid="' + item.Id + '" data-itemtype="' + item.Type + '" data-isfolder="' + item.IsFolder + '" data-mediatype="' + item.MediaType + '" data-resumeposition="' + resumePosition + '" data-mini="true" data-inline="true" data-icon="play" data-iconpos="notext" title="' + Globalize.translate('ButtonPlay') + '" style="' + buttonMargin + '">' + Globalize.translate('ButtonPlay') + '</button>';
            buttonCount++;
        }

        if (commands.indexOf('trailer') != -1) {
            html += '<button type="button" data-mini="true" data-inline="true" data-icon="video" data-iconpos="notext" class="btnPlayTrailer" data-itemid="' + item.Id + '" title="' + Globalize.translate('ButtonPlayTrailer') + '" style="' + buttonMargin + '">' + Globalize.translate('ButtonPlayTrailer') + '</button>';
            buttonCount++;
        }

        html += '<button data-role="button" class="btnMoreCommands" data-mini="true" data-inline="true" data-icon="ellipsis-v" data-iconpos="notext" title="' + Globalize.translate('ButtonMore') + '" style="' + buttonMargin + '">' + Globalize.translate('ButtonMore') + '</button>';
        buttonCount++;

        html += '</div>';

        html += '</div>';

        return html;
    }

    function closeContextMenu() {

        // Used by the tab menu, not the slide up
        $('.tapHoldMenu').popup('close');
    }

    function onTrailerButtonClick() {

        var id = this.getAttribute('data-itemid');

        ApiClient.getLocalTrailers(Dashboard.getCurrentUserId(), id).done(function (trailers) {
            MediaController.play({ items: trailers });
        });

        closeContextMenu();

        return false;
    }

    function onPlayItemButtonClick() {

        var id = this.getAttribute('data-itemid');
        var type = this.getAttribute('data-itemtype');
        var isFolder = this.getAttribute('data-isfolder') == 'true';
        var mediaType = this.getAttribute('data-mediatype');
        var resumePosition = parseInt(this.getAttribute('data-resumeposition'));

        closeContextMenu();

        LibraryBrowser.showPlayMenu(this, id, type, isFolder, mediaType, resumePosition);

        return false;
    }

    function onMoreButtonClick() {

        var card = $(this).parents('.card')[0];

        closeContextMenu();

        showContextMenu(card, {
            showPlayOptions: false
        });

        return false;
    }

    function onAddToCollectionButtonClick() {

        var id = this.getAttribute('data-itemid');

        closeContextMenu();

        BoxSetEditor.showPanel([id]);

        return false;
    }

    function onAddToPlaylistButtonClick() {

        var id = this.getAttribute('data-itemid');

        closeContextMenu();

        PlaylistManager.showPanel([id]);

        return false;
    }

    function onShuffleButtonClick() {

        var id = this.getAttribute('data-itemid');

        MediaController.shuffle(id);

        closeContextMenu();

        return false;
    }

    function onInstantMixButtonClick() {

        var id = this.getAttribute('data-itemid');

        MediaController.instantMix(id);

        closeContextMenu();

        return false;
    }

    function onQueueButtonClick() {

        var id = this.getAttribute('data-itemid');

        MediaController.queue(id);

        closeContextMenu();

        return false;
    }

    function onPlayButtonClick() {

        var id = this.getAttribute('data-itemid');

        MediaController.play(id);

        closeContextMenu();

        return false;
    }

    function onDeleteButtonClick() {

        var id = this.getAttribute('data-itemid');

        closeContextMenu();

        LibraryBrowser.deleteItem(id);

        return false;
    }

    function onSyncButtonClick() {

        var id = this.getAttribute('data-itemid');

        closeContextMenu();

        SyncManager.showMenu({
            items: [
            {
                Id: id
            }]
        });

        return false;
    }

    function onExternalPlayerButtonClick() {

        closeContextMenu();

        var id = this.getAttribute('data-itemid');

        ExternalPlayer.showMenu(id);

        return false;
    }

    function onPlayAllFromHereButtonClick() {

        var index = this.getAttribute('data-index');

        var page = $(this).parents('.page');

        var itemsContainer = $('.hasContextMenu', page).parents('.itemsContainer');

        closeContextMenu();

        itemsContainer.trigger('playallfromhere', [index]);

        return false;
    }

    function onQueueAllFromHereButtonClick() {

        var index = this.getAttribute('data-index');

        var page = $(this).parents('.page');

        var itemsContainer = $('.hasContextMenu', page).parents('.itemsContainer');

        closeContextMenu();

        itemsContainer.trigger('queueallfromhere', [index]);

        return false;
    }

    function onRemoveFromPlaylistButtonClick() {

        var playlistItemId = this.getAttribute('data-playlistitemid');

        var page = $(this).parents('.page');

        var itemsContainer = $('.hasContextMenu', page).parents('.itemsContainer');

        itemsContainer.trigger('removefromplaylist', [playlistItemId]);

        closeContextMenu();

        return false;
    }

    function onResumeButtonClick() {

        var id = this.getAttribute('data-itemid');

        MediaController.play({
            ids: [id],
            startPositionTicks: parseInt(this.getAttribute('data-ticks'))
        });

        closeContextMenu();

        return false;
    }

    function onCardTapHold(e) {

        showContextMenu(this, {});

        e.preventDefault();
        return false;
    }

    function showContextMenu(card, options) {

        closeContextMenu();

        var displayContextItem = card;

        if ($(card).hasClass('listviewMenuButton')) {
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

        $(card).addClass('hasContextMenu');

        Dashboard.getCurrentUser().done(function (user) {

            var html = '<div data-role="popup" class="tapHoldMenu" data-theme="a">';

            html += '<ul data-role="listview" style="min-width: 240px;">';
            html += '<li data-role="list-divider">' + Globalize.translate('HeaderMenu') + '</li>';

            var href = card.getAttribute('data-href') || card.href || $('a', card).attr('href');

            if (commands.indexOf('addtocollection') != -1) {
                html += '<li data-icon="plus"><a href="#" class="btnAddToCollection" data-itemid="' + itemId + '">' + Globalize.translate('ButtonAddToCollection') + '</a></li>';
            }

            if (commands.indexOf('playlist') != -1) {
                html += '<li data-icon="plus"><a href="#" class="btnAddToPlaylist" data-itemid="' + itemId + '">' + Globalize.translate('ButtonAddToPlaylist') + '</a></li>';
            }

            if (commands.indexOf('delete') != -1) {
                html += '<li data-icon="delete"><a href="#" class="btnDelete" data-itemId="' + itemId + '">' + Globalize.translate('ButtonDelete') + '</a></li>';
            }

            if (user.Policy.IsAdministrator && commands.indexOf('edit') != -1) {
                html += '<li data-icon="edit"><a href="edititemmetadata.html?id=' + itemId + '">' + Globalize.translate('ButtonEdit') + '</a></li>';
            }

            if (commands.indexOf('instantmix') != -1) {
                html += '<li data-icon="recycle"><a href="#" class="btnInstantMix" data-itemid="' + itemId + '">' + Globalize.translate('ButtonInstantMix') + '</a></li>';
            }

            html += '<li><a href="' + href + '">' + Globalize.translate('ButtonOpen') + '</a></li>';
            //html += '<li><a href="' + href + '" target="_blank">' + Globalize.translate('ButtonOpenInNewTab') + '</a></li>';

            if (options.showPlayOptions !== false) {

                if (MediaController.canPlayByAttributes(itemType, mediaType, playAccess, locationType)) {
                    html += '<li data-icon="play"><a href="#" class="btnPlay" data-itemid="' + itemId + '">' + Globalize.translate('ButtonPlay') + '</a></li>';

                    if (commands.indexOf('playfromhere') != -1) {
                        html += '<li data-icon="play"><a href="#" class="btnPlayAllFromHere" data-index="' + index + '">' + Globalize.translate('ButtonPlayAllFromHere') + '</a></li>';
                    }
                }

                if (mediaType == 'Video' && ExternalPlayer.getExternalPlayers().length) {
                    html += '<li data-icon="play"><a href="#" class="btnExternalPlayer" data-itemid="' + itemId + '">' + Globalize.translate('ButtonPlayExternalPlayer') + '</a></li>';
                }

                if (playbackPositionTicks && mediaType != "Audio") {
                    html += '<li data-icon="play"><a href="#" class="btnResume" data-ticks="' + playbackPositionTicks + '" data-itemid="' + itemId + '">' + Globalize.translate('ButtonResume') + '</a></li>';
                }

                if (commands.indexOf('trailer') != -1) {
                    html += '<li data-icon="video"><a href="#" class="btnPlayTrailer" data-itemid="' + itemId + '">' + Globalize.translate('ButtonPlayTrailer') + '</a></li>';
                }
            }

            if (MediaController.canQueueMediaType(mediaType, itemType)) {
                html += '<li data-icon="plus"><a href="#" class="btnQueue" data-itemid="' + itemId + '">' + Globalize.translate('ButtonQueue') + '</a></li>';

                if (commands.indexOf('queuefromhere') != -1) {
                    html += '<li data-icon="plus"><a href="#" class="btnQueueAllFromHere" data-index="' + index + '">' + Globalize.translate('ButtonQueueAllFromHere') + '</a></li>';
                }
            }

            if (commands.indexOf('shuffle') != -1) {
                html += '<li data-icon="recycle"><a href="#" class="btnShuffle" data-itemid="' + itemId + '">' + Globalize.translate('ButtonShuffle') + '</a></li>';
            }

            if (commands.indexOf('removefromplaylist') != -1) {
                html += '<li data-icon="delete"><a href="#" class="btnRemoveFromPlaylist" data-playlistitemid="' + playlistItemId + '">' + Globalize.translate('ButtonRemoveFromPlaylist') + '</a></li>';
            }

            if (commands.indexOf('sync') != -1) {
                html += '<li data-icon="cloud"><a href="#" class="btnSync" data-itemId="' + itemId + '">' + Globalize.translate('ButtonSync') + '</a></li>';
            }

            html += '</ul>';

            html += '</div>';

            $($.mobile.activePage).append(html);

            var elem = $('.tapHoldMenu').popup({ positionTo: displayContextItem }).trigger('create').popup("open").on("popupafterclose", function () {

                $(this).off("popupafterclose").remove();
                $(card).removeClass('hasContextMenu');
            });

            $('.btnPlay', elem).on('click', onPlayButtonClick);
            $('.btnResume', elem).on('click', onResumeButtonClick);
            $('.btnQueue', elem).on('click', onQueueButtonClick);
            $('.btnInstantMix', elem).on('click', onInstantMixButtonClick);
            $('.btnShuffle', elem).on('click', onShuffleButtonClick);
            $('.btnPlayTrailer', elem).on('click', onTrailerButtonClick);
            $('.btnAddToPlaylist', elem).on('click', onAddToPlaylistButtonClick);
            $('.btnRemoveFromPlaylist', elem).on('click', onRemoveFromPlaylistButtonClick);
            $('.btnPlayAllFromHere', elem).on('click', onPlayAllFromHereButtonClick);
            $('.btnQueueAllFromHere', elem).on('click', onQueueAllFromHereButtonClick);
            $('.btnExternalPlayer', elem).on('click', onExternalPlayerButtonClick);
            $('.btnDelete', elem).on('click', onDeleteButtonClick);
            $('.btnSync', elem).on('click', onSyncButtonClick);
            $('.btnAddToCollection', elem).on('click', onAddToCollectionButtonClick);
        });
    }

    function onListViewMenuButtonClick(e) {

        showContextMenu(this, {});

        e.preventDefault();
        return false;
    }

    function onGroupedCardClick(e) {

        var card = this;
        var itemId = card.getAttribute('data-itemid');
        var context = card.getAttribute('data-context');

        $(card).addClass('hasContextMenu');

        var userId = Dashboard.getCurrentUserId();

        var options = {

            Limit: parseInt($('.playedIndicator', card).html() || '10'),
            Fields: "PrimaryImageAspectRatio,DateCreated",
            ParentId: itemId,
            GroupItems: false
        };

        ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options)).done(function (items) {

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

        var html = '<div data-role="popup" class="detailsMenu" data-transition="slidedown" style="border:0;padding:0;" data-ids="' + ids.join(',') + '" data-context="' + (context || '') + '">';

        html += '<div style="padding:1em 1em;background:rgba(20,20,20,1);margin:0;text-align:center;" class="detailsMenuHeader">';
        html += '<button type="button" class="imageButton detailsMenuLeftButton" data-role="none"><i class="fa fa-arrow-left"></i></button>';
        html += '<h3 style="font-weight:400;margin:.5em 0;line-height:0;"></h3>';
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
        });

        $('.detailsMenuLeftButton', elem).on('click', function () {

            var overlay = $(this).parents('.detailsMenu');
            setItemIntoOverlay(overlay, parseInt(overlay.attr('data-index')) - 1, context);
        });

        $('.detailsMenuRightButton', elem).on('click', function () {

            var overlay = $(this).parents('.detailsMenu');
            setItemIntoOverlay(overlay, parseInt(overlay.attr('data-index')) + 1, context);
        });

        return elem;
    }

    function setItemIntoOverlay(elem, index) {

        var ids = elem.attr('data-ids').split(',');
        var itemId = ids[index];
        var userId = Dashboard.getCurrentUserId();
        var context = elem.attr('data-context');

        elem.attr('data-index', index);

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

                contentHtml += '<p>' + userData + '</p>';
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
                    contentHtml += '<div class="detailsMenuButtonContainer">';
                    contentHtml += '<a href="#" class="btn btnAltAction btnResume">';
                    contentHtml += '<i class="fa fa-play"></i>';
                    contentHtml += '<span>' + Globalize.translate('ButtonResume') + '</span>';
                    contentHtml += '</a>';
                    contentHtml += '</div>';
                }

                contentHtml += '<div class="detailsMenuButtonContainer">';
                contentHtml += '<a href="#" class="btn btnActionAccent btnPlay">';
                contentHtml += '<i class="fa fa-play"></i>';
                contentHtml += '<span>' + Globalize.translate('ButtonPlay') + '</span>';
                contentHtml += '</a>';
                contentHtml += '</div>';

            }

            contentHtml += '<div class="detailsMenuButtonContainer">';
            contentHtml += '<a href="' + LibraryBrowser.getHref(item, context) + '" class="btn" style="background-color: #673AB7;">';
            contentHtml += '<i class="fa fa-folder-open"></i>';
            contentHtml += '<span>' + Globalize.translate('ButtonOpen') + '</span>';
            contentHtml += '</a>';
            contentHtml += '</div>';

            if (SyncManager.isAvailable(item, user)) {
                contentHtml += '<div class="detailsMenuButtonContainer">';
                contentHtml += '<a href="#" class="btn btnSync">';
                contentHtml += '<i class="fa fa-cloud"></i>';
                contentHtml += '<span>' + Globalize.translate('ButtonSync') + '</span>';
                contentHtml += '</a>';
                contentHtml += '</div>';
            }

            contentHtml += '</div>';

            $('.detailsMenuContentInner', elem).html(contentHtml).trigger('create');

            $('.btnSync', elem).on('click', function () {

                elem.popup('close');

                SyncManager.showMenu({
                    items: [item]
                });
            });

            $('.btnPlay', elem).on('click', function () {

                elem.popup('close');

                MediaController.play({
                    items: [item]
                });
            });

            $('.btnResume', elem).on('click', function () {

                elem.popup('close');

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

    function onCardClick() {

        var info = LibraryBrowser.getListItemInfo(this);
        var itemId = info.id;
        var context = info.context;

        var card = $(this);

        if (card.hasClass('itemWithAction')) {
            return;
        }

        if (!card.hasClass('card')) {
            card = $(card).parents('.card');
        }

        if (card.hasClass('groupedCard')) {
            return;
        }

        if (card.attr('data-detailsmenu') != 'true') {
            return;
        }

        showItemsOverlay({
            ids: [itemId],
            context: context
        });

        return false;
    }

    $.fn.createCardMenus = function (options) {

        var preventHover = false;

        function onShowTimerExpired(elem) {

            elem = $('a', elem)[0];

            if ($(elem).hasClass('hasContextMenu')) {
                return;
            }

            if ($('.itemSelectionPanel:visible', elem).length) {
                return;
            }

            var innerElem = $('.cardOverlayTarget', elem);

            var dataElement = elem;
            while (!dataElement.getAttribute('data-itemid')) {
                dataElement = dataElement.parentNode;
            }

            var id = dataElement.getAttribute('data-itemid');
            var commands = dataElement.getAttribute('data-commands').split(',');

            var promise1 = ApiClient.getItem(Dashboard.getCurrentUserId(), id);
            var promise2 = Dashboard.getCurrentUser();

            $.when(promise1, promise2).done(function (response1, response2) {

                var item = response1[0];
                var user = response2[0];

                var card = $(elem);

                if (!card.hasClass('card')) {
                    card = card.parents('.card');
                }

                innerElem.html(getOverlayHtml(item, user, card[0], commands)).trigger('create');

                $('.btnPlayItem', innerElem).on('click', onPlayItemButtonClick);
                $('.btnPlayTrailer', innerElem).on('click', onTrailerButtonClick);
                $('.btnMoreCommands', innerElem).on('click', onMoreButtonClick);
            });

            innerElem.show().each(function () {

                this.style.height = 0;

            }).animate({ "height": "100%" }, "fast");
        }

        function onHoverIn() {

            if (preventHover === true) {
                preventHover = false;
                return;
            }

            if (showOverlayTimeout) {
                clearTimeout(showOverlayTimeout);
                showOverlayTimeout = null;
            }

            var elem = this;

            showOverlayTimeout = setTimeout(function () {

                onShowTimerExpired(elem);

            }, 1000);
        }

        function preventTouchHover() {
            preventHover = true;
        }

        this.off('.cardMenu')
            .on('contextmenu.cardMenu', '.card', onCardTapHold)
            .off('.latestgroupings')
            .on('click.latestgroupings', '.groupedCard', onGroupedCardClick)
            .off('.dotmenu')
            .on('click.dotmenu', '.listviewMenuButton', onListViewMenuButtonClick)
            .off('.cardHoverMenu')
            .on('mouseenter.cardHoverMenu', '.card:not(.bannerCard)', onHoverIn)
            .on('mouseleave.cardHoverMenu', '.card:not(.bannerCard)', onHoverOut)
            .on("touchstart.cardHoverMenu", '.card:not(.bannerCard)', preventTouchHover);

        this.off('.mediaDetails').on('click.mediaDetails', '.mediaItem', onCardClick);

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

        $('.selectionCommands', page).hide();

        $('.itemSelectionPanel', page).hide();
    }

    function getSelectedItems(page) {

        var selection = $('.chkItemSelect:checked', page);

        return selection.parents('.card')
            .map(function () {

                return this.getAttribute('data-itemid');

            }).get();
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

        $(SyncManager).off('jobsubmit.librarylist').on('jobsubmit.librarylist', function () {

            hideSelections(page);
        });
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

        Dashboard.confirm(msg, "Group Versions", function (confirmResult) {

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

            closeContextMenu();

            itemsContainer.trigger('playallfromhere', [index]);
        }

        return false;
    }

    function resetImages(page) {

        $('cardImage', page).remove();
    }

    $(document).on('pageinit', ".libraryPage", function () {

        var page = this;

        $('.btnAddToPlaylist', page).on('click', function () {
            addToPlaylist(page);
        });

        $('.btnMergeVersions', page).on('click', function () {
            combineVersions(page);
        });

        $('.btnSyncItems', page).on('click', function () {
            sync(page);
        });

        $('.btnAddToCollection', page).on('click', function () {
            addToCollection(page);
        });

        $('.viewTabButton', page).on('click', function () {

            $('.viewTabButton', page).removeClass('ui-btn-active');
            $(this).addClass('ui-btn-active');

            $('.viewTab', page).hide();
            $('.' + this.getAttribute('data-tab'), page).show();
        });

        var viewPanel = $('.viewPanel', page);

        $('#selectPageSize', viewPanel).html(LibraryBrowser.getDefaultPageSizeSelections().map(function (i) {

            return '<option value="' + i + '">' + i + '</option>';

        }).join('')).selectmenu('refresh');

        $(page).on('click', '.btnToggleSelections', function () {

            toggleSelections(page);

        }).on('click', '.itemWithAction', onItemWithActionClick);

        $('.itemsContainer', page).createCardMenus();

    }).on('pagebeforeshow', ".libraryPage", function () {

        var page = this;

        hideSelections(page);

        $('.viewTabButton:first', page).trigger('click');

    }).on('pagebeforehide', ".libraryPage", function () {

        var page = this;
        resetImages(page);
    });

    function renderUserDataChanges(card, userData) {

        if (userData.Played) {

            if (!$('.playedIndicator', card).length) {

                $('<div class="playedIndicator"></div>').insertAfter($('.cardOverlayTarget', card));
            }
            $('.playedIndicator', card).html('<div class="ui-icon-check ui-btn-icon-notext"></div>');
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
        $(apiClient).off('websocketmessage.librarylist', onWebSocketMessage).on('websocketmessage.librarylist', onWebSocketMessage);
    }

    $(ConnectionManager).on('apiclientcreated', function (e, apiClient) {

        initializeApiClient(apiClient);
    });

})(jQuery, document, window);