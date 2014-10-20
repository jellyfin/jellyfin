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

            html += '<img src="' + imgUrl + '" style="max-height:' + logoHeight + 'px;max-width:' + maxLogoWidth + 'px;" />';

        }
        else if (item.ImageTags.Logo) {

            imgUrl = ApiClient.getScaledImageUrl(item.Id, {
                height: logoHeight,
                type: 'logo',
                tag: item.ImageTags.Logo
            });

            html += '<img src="' + imgUrl + '" style="max-height:' + logoHeight + 'px;max-width:' + maxLogoWidth + 'px;" />';
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
            html += '<div style="margin:1.25em 0;">';
            html += '<span class="itemCommunityRating">';
            html += LibraryBrowser.getRatingHtml(item, false);
            html += '</span>';

            if (isPortrait) {
                html += '<span class="userDataIcons" style="display:block;margin:1.25em 0;">';
                html += LibraryBrowser.getUserDataIconsHtml(item);
                html += '</span>';
            } else {
                html += '<span class="userDataIcons">';
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
            card = $(card).parents('.listItem')[0];
        }

        var itemId = card.getAttribute('data-itemid');
        var playlistItemId = card.getAttribute('data-playlistitemid');
        var commands = card.getAttribute('data-commands').split(',');
        var itemType = card.getAttribute('data-itemtype');
        var mediaType = card.getAttribute('data-mediatype');
        var playbackPositionTicks = parseInt(card.getAttribute('data-positionticks') || '0');
        var playAccess = card.getAttribute('data-playaccess');
        var locationType = card.getAttribute('data-locationtype');
        var isPlaceHolder = card.getAttribute('data-placeholder') == 'true';
        var index = card.getAttribute('data-index');

        $(card).addClass('hasContextMenu');

        Dashboard.getCurrentUser().done(function (user) {

            var html = '<div data-role="popup" class="tapHoldMenu" data-theme="a">';

            html += '<ul data-role="listview" style="min-width: 240px;">';
            html += '<li data-role="list-divider">' + Globalize.translate('HeaderMenu') + '</li>';

            var href = card.getAttribute('data-href') || card.href;

            if (commands.indexOf('playlist') != -1) {
                html += '<li data-icon="plus"><a href="#" class="btnAddToPlaylist" data-itemid="' + itemId + '">' + Globalize.translate('ButtonAddToPlaylist') + '</a></li>';
            }

            if (user.Configuration.IsAdministrator && commands.indexOf('edit') != -1) {
                html += '<li data-icon="edit"><a href="edititemmetadata.html?id=' + itemId + '">' + Globalize.translate('ButtonEdit') + '</a></li>';
            }

            if (commands.indexOf('instantmix') != -1) {
                html += '<li data-icon="recycle"><a href="#" class="btnInstantMix" data-itemid="' + itemId + '">' + Globalize.translate('ButtonInstantMix') + '</a></li>';
            }

            html += '<li><a href="' + href + '">' + Globalize.translate('ButtonOpen') + '</a></li>';
            html += '<li><a href="' + href + '" target="_blank">' + Globalize.translate('ButtonOpenInNewTab') + '</a></li>';

            if (options.showPlayOptions !== false) {

                if (MediaController.canPlayByAttributes(itemType, mediaType, playAccess, locationType, isPlaceHolder)) {
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
        });
    }

    function onListViewMenuButtonClick(e) {

        showContextMenu(this, {});

        e.preventDefault();
        return false;
    }

    function onGroupedCardClick(e) {

        var target = $(e.target);

        var card = this;
        var itemId = card.getAttribute('data-itemid');
        var context = card.getAttribute('data-context');

        $(card).addClass('hasContextMenu');

        var userId = Dashboard.getCurrentUserId();

        var promise1 = ApiClient.getItem(userId, itemId);

        var options = {

            Limit: parseInt($('.playedIndicator', card).html() || '10'),
            Fields: "PrimaryImageAspectRatio,DateCreated",
            ParentId: itemId,
            GroupItems: false
        };

        if ($(card).hasClass('unplayedGroupings')) {
            options.IsPlayed = false;
        }

        var promise2 = ApiClient.getJSON(ApiClient.getUrl('Users/' + userId + '/Items/Latest', options));

        $.when(promise1, promise2).done(function (response1, response2) {

            var item = response1[0];
            var latestItems = response2[0];

            if (latestItems.length == 1) {

                if (!target.is('a,button')) {
                    var first = latestItems[0];
                    Dashboard.navigate(LibraryBrowser.getHref(first, context));
                    return;
                }
            }

            var html = '<div data-role="popup" class="groupingMenu" data-theme="a">';

            html += '<a href="#" data-rel="back" class="ui-btn ui-corner-all ui-shadow ui-btn-b ui-icon-delete ui-btn-icon-notext ui-btn-right">Close</a>';
            html += '<div>';
            html += '<ul data-role="listview">';

            var href = card.href || LibraryBrowser.getHref(item, context);
            var header = Globalize.translate('HeaderLatestFromChannel').replace('{0}', '<a href="' + href + '">' + item.Name + '</a>');
            html += '<li data-role="list-divider">' + header + '</li>';

            html += '</ul>';

            html += '<div class="groupingMenuScroller">';
            html += '<ul data-role="listview">';

            html += latestItems.map(function (latestItem) {

                var itemHtml = '';

                href = LibraryBrowser.getHref(latestItem, context);
                itemHtml += '<li class="ui-li-has-thumb"><a href="' + href + '">';

                var imgUrl;

                if (latestItem.ImageTags.Primary) {

                    // Scaling 400w episode images to 80 doesn't turn out very well
                    var width = latestItem.Type == 'Episode' ? 160 : 80;
                    imgUrl = ApiClient.getScaledImageUrl(latestItem.Id, {
                        width: width,
                        tag: latestItem.ImageTags.Primary,
                        type: "Primary",
                        index: 0
                    });

                }
                if (imgUrl) {
                    itemHtml += '<div class="listviewImage ui-li-thumb" style="background-image:url(\'' + imgUrl + '\');"></div>';
                }

                itemHtml += '<h3>';
                itemHtml += LibraryBrowser.getPosterViewDisplayName(latestItem);
                itemHtml += '</h3>';

                var date = parseISO8601Date(latestItem.DateCreated, { toLocal: true });

                itemHtml += '<p>';
                itemHtml += Globalize.translate('LabelAddedOnDate').replace('{0}', date.toLocaleDateString());
                itemHtml += '</p>';

                itemHtml += '</a></li>';

                return itemHtml;

            }).join('');

            html += '</ul>';
            html += '</div>';

            html += '</div>';
            html += '</div>';

            $($.mobile.activePage).append(html);

            var elem = $('.groupingMenu').popup().trigger('create').popup("open").on("popupafterclose", function () {

                $(this).off("popupafterclose").remove();
                $(card).removeClass('hasContextMenu');

            });
        });

        e.preventDefault();
        return false;
    }


    $.fn.createCardMenus = function () {

        var preventHover = false;

        function onShowTimerExpired(elem) {

            if ($(elem).hasClass('hasContextMenu')) {
                return;
            }

            if ($('.itemSelectionPanel:visible', elem).length) {
                return;
            }

            var innerElem = $('.cardOverlayTarget', elem);
            var id = elem.getAttribute('data-itemid');
            var commands = elem.getAttribute('data-commands').split(',');

            var promise1 = ApiClient.getItem(Dashboard.getCurrentUserId(), id);
            var promise2 = Dashboard.getCurrentUser();

            $.when(promise1, promise2).done(function (response1, response2) {

                var item = response1[0];
                var user = response2[0];

                innerElem.html(getOverlayHtml(item, user, elem, commands)).trigger('create');

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

        var elems = '.card:not(.bannerCard)';

        $('.card', this).on('contextmenu.cardMenu', onCardTapHold);

        $('.listviewMenuButton', this).on('click', onListViewMenuButtonClick);

        $('.groupedCard', this).on('click', onGroupedCardClick);

        return this.off('.cardHoverMenu')
            .on('mouseenter.cardHoverMenu', elems, onHoverIn)
            .on('mouseleave.cardHoverMenu', elems, onHoverOut)
            .on("touchstart.cardHoverMenu", elems, preventTouchHover)
            .trigger('itemsrendered');
    };

    function toggleSelections(page) {

        Dashboard.showLoadingMsg();

        var selectionCommands = $('.selectionCommands', page);

        if (selectionCommands.is(':visible')) {

            selectionCommands.hide();
            $('.itemSelectionPanel', page).hide();

        } else {

            selectionCommands.show();

            $('.itemSelectionPanel', page).show();

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

        BoxSetEditor.showPanel(page, selection);
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

    function onItemWithActionClick() {

        var elem = this;
        var action = elem.getAttribute('data-action');
        var elemWithAttributes = elem.getAttribute('data-itemid') ? elem : elem.parentNode;

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
        else if (action == 'photoslideshow') {

            itemsContainer = $(elem).parents('.itemsContainer');
            index = $('.card', itemsContainer).get().indexOf(elem);

            closeContextMenu();

            itemsContainer.trigger('photoslideshow', [index]);
        }

        return false;
    }

    $(document).on('pageinit', ".libraryPage", function () {

        var page = this;

        $('.btnAddToPlaylist', page).on('click', function () {
            addToPlaylist(page);
        });

        $('.btnMergeVersions', page).on('click', function () {
            combineVersions(page);
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

        var viewPanel = $('.viewPanel', page).panel('option', 'classes.modalOpen', 'viewPanelModelOpen ui-panel-dismiss-open');

        $('#selectPageSize', viewPanel).html(LibraryBrowser.getDefaultPageSizeSelections().map(function (i) {

            return '<option value="' + i + '">' + i + '</option>';

        }).join('')).selectmenu('refresh');

        $('.itemsContainer', page).on('itemsrendered', function () {

            $('.btnToggleSelections', page).off('click.toggleselections').on('click.toggleselections', function () {
                toggleSelections(page);
            });

            $('.itemWithAction', this).on('click', onItemWithActionClick);
        });

    }).on('pagebeforeshow', ".libraryPage", function () {

        var page = this;

        hideSelections(page);

        $('.viewTabButton:first', page).trigger('click');
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

            this.setAttribute('data-positionticks', (userData.PlaybackPositionTicks || 0));

            if ($(this).hasClass('card')) {
                renderUserDataChanges(this, userData);
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

    $(ApiClient).on('websocketmessage', onWebSocketMessage);

})(jQuery, document, window);