define(['connectionManager', 'globalize', 'dom', 'itemHelper', 'paper-icon-button-light', 'material-icons', 'emby-button', 'css!./userdatabuttons'], function (connectionManager, globalize, dom, itemHelper) {
    'use strict';

    var userDataMethods = {
        markPlayed: markPlayed,
        markDislike: markDislike,
        markLike: markLike,
        markFavorite: markFavorite
    };

    function getUserDataButtonHtml(method, itemId, serverId, buttonCssClass, iconCssClass, icon, tooltip, style) {

        if (style === 'fab-mini') {
            style = 'fab';
            buttonCssClass = buttonCssClass ? (buttonCssClass + ' mini') : 'mini';
        }

        var is = style === 'fab' ? 'emby-button' : 'paper-icon-button-light';
        var className = style === 'fab' ? 'autoSize fab' : 'autoSize';

        if (buttonCssClass) {
            className += ' ' + buttonCssClass;
        }

        if (iconCssClass) {
            iconCssClass += ' ';
        } else {
            iconCssClass = '';
        }

        iconCssClass += 'md-icon';

        return '<button title="' + tooltip + '" data-itemid="' + itemId + '" data-serverid="' + serverId + '" is="' + is + '" data-method="' + method + '" class="' + className + '"><i class="' + iconCssClass + '">' + icon + '</i></button>';
    }

    function onContainerClick(e) {

        var btnUserData = dom.parentWithClass(e.target, 'btnUserData');

        if (!btnUserData) {
            return;
        }

        var method = btnUserData.getAttribute('data-method');
        userDataMethods[method](btnUserData);
    }

    function fill(options) {

        var html = getIconsHtml(options);

        if (options.fillMode === 'insertAdjacent') {
            options.element.insertAdjacentHTML(options.insertLocation || 'beforeend', html);
        } else {
            options.element.innerHTML = html;
        }

        dom.removeEventListener(options.element, 'click', onContainerClick, {
            passive: true
        });

        dom.addEventListener(options.element, 'click', onContainerClick, {
            passive: true
        });
    }

    function destroy(options) {

        options.element.innerHTML = '';

        dom.removeEventListener(options.element, 'click', onContainerClick, {
            passive: true
        });
    }

    function getIconsHtml(options) {

        var item = options.item;
        var includePlayed = options.includePlayed;
        var cssClass = options.cssClass;
        var style = options.style;

        var html = '';

        var userData = item.UserData || {};

        var itemId = item.Id;

        if (itemHelper.isLocalItem(item)) {
            return html;
        }

        var btnCssClass = "btnUserData";

        if (cssClass) {
            btnCssClass += " " + cssClass;
        }

        var iconCssClass = options.iconCssClass;

        var serverId = item.ServerId;

        if (includePlayed !== false) {
            var tooltipPlayed = globalize.translate('sharedcomponents#MarkPlayed');

            if (item.MediaType === 'Video' || item.Type === 'Series' || item.Type === 'Season' || item.Type === 'BoxSet' || item.Type === 'Playlist') {
                if (item.Type !== 'TvChannel') {
                    if (userData.Played) {
                        html += getUserDataButtonHtml('markPlayed', itemId, serverId, btnCssClass + ' btnUserDataOn', iconCssClass, '&#xE5CA;', tooltipPlayed, style);
                    } else {
                        html += getUserDataButtonHtml('markPlayed', itemId, serverId, btnCssClass, iconCssClass, '&#xE5CA;', tooltipPlayed, style);
                    }
                }
            }
        }

        //var tooltipLike = globalize.translate('sharedcomponents#Like');
        //var tooltipDislike = globalize.translate('sharedcomponents#Dislike');

        //if (typeof userData.Likes == "undefined") {
        //    html += getUserDataButtonHtml('markDislike', itemId, serverId, btnCssClass + ' btnUserData btnDislike', 'thumb-down', tooltipDislike);
        //    html += getUserDataButtonHtml('markLike', itemId, serverId, btnCssClass + ' btnUserData btnLike', 'thumb-up', tooltipLike);
        //}
        //else if (userData.Likes) {
        //    html += getUserDataButtonHtml('markDislike', itemId, serverId, btnCssClass + ' btnUserData btnDislike', 'thumb-down', tooltipDislike);
        //    html += getUserDataButtonHtml('markLike', itemId, serverId, btnCssClass + ' btnUserData btnLike btnUserDataOn', 'thumb-up', tooltipLike);
        //}
        //else {
        //    html += getUserDataButtonHtml('markDislike', itemId, serverId, btnCssClass + ' btnUserData btnDislike btnUserDataOn', 'thumb-down', tooltipDislike);
        //    html += getUserDataButtonHtml('markLike', itemId, serverId, btnCssClass + ' btnUserData btnLike', 'thumb-up', tooltipLike);
        //}

        var tooltipFavorite = globalize.translate('sharedcomponents#Favorite');
        if (userData.IsFavorite) {

            html += getUserDataButtonHtml('markFavorite', itemId, serverId, btnCssClass + ' btnUserData btnUserDataOn', iconCssClass, '&#xE87D;', tooltipFavorite, style);
        } else {
            html += getUserDataButtonHtml('markFavorite', itemId, serverId, btnCssClass + ' btnUserData', iconCssClass, '&#xE87D;', tooltipFavorite, style);
        }

        return html;
    }

    function markFavorite(link) {

        var id = link.getAttribute('data-itemid');
        var serverId = link.getAttribute('data-serverid');

        var markAsFavorite = !link.classList.contains('btnUserDataOn');

        favorite(id, serverId, markAsFavorite);

        if (markAsFavorite) {
            link.classList.add('btnUserDataOn');
        } else {
            link.classList.remove('btnUserDataOn');
        }
    }

    function markLike(link) {

        var id = link.getAttribute('data-itemid');
        var serverId = link.getAttribute('data-serverid');

        if (!link.classList.contains('btnUserDataOn')) {

            likes(id, serverId, true);

            link.classList.add('btnUserDataOn');

        } else {

            clearLike(id, serverId);

            link.classList.remove('btnUserDataOn');
        }

        link.parentNode.querySelector('.btnDislike').classList.remove('btnUserDataOn');
    }

    function markDislike(link) {

        var id = link.getAttribute('data-itemid');
        var serverId = link.getAttribute('data-serverid');

        if (!link.classList.contains('btnUserDataOn')) {

            likes(id, serverId, false);

            link.classList.add('btnUserDataOn');

        } else {

            clearLike(id, serverId);

            link.classList.remove('btnUserDataOn');
        }

        link.parentNode.querySelector('.btnLike').classList.remove('btnUserDataOn');
    }

    function markPlayed(link) {

        var id = link.getAttribute('data-itemid');
        var serverId = link.getAttribute('data-serverid');

        if (!link.classList.contains('btnUserDataOn')) {

            played(id, serverId, true);

            link.classList.add('btnUserDataOn');

        } else {

            played(id, serverId, false);

            link.classList.remove('btnUserDataOn');
        }
    }

    function likes(id, serverId, isLiked) {
        var apiClient = connectionManager.getApiClient(serverId);
        return apiClient.updateUserItemRating(apiClient.getCurrentUserId(), id, isLiked);
    }

    function played(id, serverId, isPlayed) {
        var apiClient = connectionManager.getApiClient(serverId);

        var method = isPlayed ? 'markPlayed' : 'markUnplayed';

        return apiClient[method](apiClient.getCurrentUserId(), id, new Date());
    }

    function favorite(id, serverId, isFavorite) {
        var apiClient = connectionManager.getApiClient(serverId);

        return apiClient.updateFavoriteStatus(apiClient.getCurrentUserId(), id, isFavorite);
    }

    function clearLike(id, serverId) {

        var apiClient = connectionManager.getApiClient(serverId);

        return apiClient.clearUserItemRating(apiClient.getCurrentUserId(), id);
    }

    return {
        fill: fill,
        destroy: destroy,
        getIconsHtml: getIconsHtml
    };

});