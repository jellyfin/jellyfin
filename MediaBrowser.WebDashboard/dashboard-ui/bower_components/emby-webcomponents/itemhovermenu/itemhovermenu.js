define(['connectionManager', 'itemHelper', 'mediaInfo', 'userdataButtons', 'playbackManager', 'globalize', 'dom', 'apphost', 'css!./itemhovermenu', 'emby-button'], function (connectionManager, itemHelper, mediaInfo, userdataButtons, playbackManager, globalize, dom, appHost) {

    var preventHover = false;
    var showOverlayTimeout;

    function onHoverOut(e) {

        var elem = e.target;

        if (showOverlayTimeout) {
            clearTimeout(showOverlayTimeout);
            showOverlayTimeout = null;
        }

        elem = elem.classList.contains('cardOverlayTarget') ? elem : elem.querySelector('.cardOverlayTarget');

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
              { transform: 'none', offset: 0 },
              { transform: 'translateY(100%)', offset: 1 }];
            var timing = { duration: 140, iterations: 1, fill: 'forwards', easing: 'ease-out' };

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
              { transform: 'none', offset: 1 }];
            var timing = { duration: 180, iterations: 1, fill: 'forwards', easing: 'ease-out' };
            elem.animate(keyframes, timing);
        });
    }

    function getOverlayHtml(apiClient, item, currentUser, card) {

        var html = '';

        html += '<div class="cardOverlayInner">';

        var className = card.className.toLowerCase();

        var isMiniItem = className.indexOf('mini') != -1;
        var isSmallItem = isMiniItem || className.indexOf('small') != -1;
        var isPortrait = className.indexOf('portrait') != -1;

        var parentName = isSmallItem || isMiniItem || isPortrait ? null : item.SeriesName;
        var name = itemHelper.getDisplayName(item);

        html += '<div>';
        var logoHeight = 26;
        var imgUrl;

        if (parentName && item.ParentLogoItemId) {

            imgUrl = apiClient.getScaledImageUrl(item.ParentLogoItemId, {
                maxHeight: logoHeight,
                type: 'logo',
                tag: item.ParentLogoImageTag
            });

            html += '<img src="' + imgUrl + '" style="max-height:' + logoHeight + 'px;max-width:100%;" />';

        }
        else if (item.ImageTags.Logo) {

            imgUrl = apiClient.getScaledImageUrl(item.Id, {
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
            html += '<div class="cardOverlayMediaInfo">';
            html += mediaInfo.getPrimaryMediaInfoHtml(item, {
                endsAt: false
            });
            html += '</div>';
        }

        html += '<div class="cardOverlayButtons">';

        var buttonCount = 0;

        if (playbackManager.canPlay(item)) {

            html += '<button is="emby-button" class="itemAction autoSize fab cardOverlayFab mini" data-action="playmenu"><i class="md-icon cardOverlayFab-md-icon">&#xE037;</i></button>';
            buttonCount++;
        }

        if (item.LocalTrailerCount) {
            html += '<button title="' + globalize.translate('sharedcomponents#Trailer') + '" is="emby-button" class="itemAction autoSize fab cardOverlayFab mini" data-action="playtrailer"><i class="md-icon cardOverlayFab-md-icon">&#xE04B;</i></button>';
            buttonCount++;
        }

        var moreIcon = appHost.moreIcon == 'dots-horiz' ? '&#xE5D3;' : '&#xE5D4;';
        html += '<button is="emby-button" class="itemAction autoSize fab cardOverlayFab mini" data-action="menu" data-playoptions="false"><i class="md-icon cardOverlayFab-md-icon">' + moreIcon + '</i></button>';
        buttonCount++;

        html += userdataButtons.getIconsHtml({
            item: item,
            style: 'fab-mini',
            cssClass: 'cardOverlayFab',
            iconCssClass: 'cardOverlayFab-md-icon'
        });

        html += '</div>';

        html += '</div>';

        return html;
    }


    function onShowTimerExpired(elem) {

        var innerElem = elem.querySelector('.cardOverlayTarget');

        if (!innerElem) {
            innerElem = document.createElement('div');
            innerElem.classList.add('hide');
            innerElem.classList.add('cardOverlayTarget');

            var appendTo = elem.querySelector('div.cardContent') || elem.querySelector('.cardScalable') || elem.querySelector('.cardBox');

            //if (appendTo && appendTo.tagName == 'BUTTON') {
            //    appendTo = dom.parentWithClass(elem, 'cardScalable');
            //}

            if (!appendTo) {
                appendTo = elem;
            }

            appendTo.classList.add('withHoverMenu');
            appendTo.appendChild(innerElem);
        }

        var dataElement = dom.parentWithAttribute(elem, 'data-id');

        if (!dataElement) {
            return;
        }

        var id = dataElement.getAttribute('data-id');
        var type = dataElement.getAttribute('data-type');

        if (type == 'Timer') {
            return;
        }

        var serverId = dataElement.getAttribute('data-serverid');

        var apiClient = connectionManager.getApiClient(serverId);
        var promise1 = apiClient.getItem(apiClient.getCurrentUserId(), id);
        var promise2 = apiClient.getCurrentUser();

        Promise.all([promise1, promise2]).then(function (responses) {

            var item = responses[0];
            var user = responses[1];

            innerElem.innerHTML = getOverlayHtml(apiClient, item, user, dataElement);
        });

        slideUpToShow(innerElem);
    }

    function onHoverIn(e) {

        var elem = e.target;
        var card = dom.parentWithClass(elem, 'cardBox');

        if (!card) {
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

        showOverlayTimeout = setTimeout(function () {
            onShowTimerExpired(card);

        }, 1000);
    }

    function preventTouchHover() {
        preventHover = true;
    }

    function ItemHoverMenu(parentElement) {

        this.parent = parentElement;

        this.parent.addEventListener('mouseenter', onHoverIn, true);
        this.parent.addEventListener('mouseleave', onHoverOut, true);
        this.parent.addEventListener("touchstart", preventTouchHover);
    }

    ItemHoverMenu.prototype = {

        constructor: ItemHoverMenu,

        destroy: function () {
            this.parent.removeEventListener('mouseenter', onHoverIn, true);
            this.parent.removeEventListener('mouseleave', onHoverOut, true);
            this.parent.removeEventListener("touchstart", preventTouchHover);
        }
    }

    return ItemHoverMenu;
});