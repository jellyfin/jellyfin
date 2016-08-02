define(['connectionManager', 'globalize', 'paper-icon-button-light', 'material-icons', 'emby-button', 'css!./userdatabuttons'], function (connectionManager, globalize) {

    function getUserDataButtonHtml(method, itemId, buttonCssClass, iconCssClass, icon, tooltip, style) {

        if (style == 'fab-mini') {
            style = 'fab';
            buttonCssClass = buttonCssClass ? (buttonCssClass + ' mini') : 'mini';
        }

        var is = style == 'fab' ? 'emby-button' : 'paper-icon-button-light';
        var className = style == 'fab' ? 'autoSize fab' : 'autoSize';

        if (buttonCssClass) {
            className += ' ' + buttonCssClass;
        }

        if (iconCssClass) {
            iconCssClass += ' ';
        } else {
            iconCssClass = '';
        }

        iconCssClass += 'md-icon';

        return '<button title="' + tooltip + '" data-itemid="' + itemId + '" is="' + is + '" class="' + className + '" onclick="UserDataButtons.' + method + '(this);return false;">\
                <i class="'+ iconCssClass + '">' + icon + '</i>\
            </button>';
    }

    function fill(options) {

        var html = getIconsHtml(options);

        options.element.innerHTML = html;
    }

    function getIconsHtml(options) {

        var item = options.item;
        var includePlayed = options.includePlayed;
        var cssClass = options.cssClass;
        var style = options.style;

        var html = '';

        var userData = item.UserData || {};

        var itemId = item.Id;

        var btnCssClass = "btnUserData";

        if (cssClass) {
            btnCssClass += " " + cssClass;
        }

        var iconCssClass = options.iconCssClass;

        if (includePlayed !== false) {
            var tooltipPlayed = globalize.translate('sharedcomponents#MarkPlayed');

            if (item.MediaType == 'Video' || item.Type == 'Series' || item.Type == 'Season' || item.Type == 'BoxSet' || item.Type == 'Playlist') {
                if (item.Type != 'TvChannel') {
                    if (userData.Played) {
                        html += getUserDataButtonHtml('markPlayed', itemId, btnCssClass + ' btnUserDataOn', iconCssClass, 'check', tooltipPlayed, style);
                    } else {
                        html += getUserDataButtonHtml('markPlayed', itemId, btnCssClass, iconCssClass, 'check', tooltipPlayed, style);
                    }
                }
            }
        }

        //var tooltipLike = globalize.translate('sharedcomponents#Like');
        //var tooltipDislike = globalize.translate('sharedcomponents#Dislike');

        //if (typeof userData.Likes == "undefined") {
        //    html += getUserDataButtonHtml('markDislike', itemId, btnCssClass + ' btnUserData btnDislike', 'thumb-down', tooltipDislike);
        //    html += getUserDataButtonHtml('markLike', itemId, btnCssClass + ' btnUserData btnLike', 'thumb-up', tooltipLike);
        //}
        //else if (userData.Likes) {
        //    html += getUserDataButtonHtml('markDislike', itemId, btnCssClass + ' btnUserData btnDislike', 'thumb-down', tooltipDislike);
        //    html += getUserDataButtonHtml('markLike', itemId, btnCssClass + ' btnUserData btnLike btnUserDataOn', 'thumb-up', tooltipLike);
        //}
        //else {
        //    html += getUserDataButtonHtml('markDislike', itemId, btnCssClass + ' btnUserData btnDislike btnUserDataOn', 'thumb-down', tooltipDislike);
        //    html += getUserDataButtonHtml('markLike', itemId, btnCssClass + ' btnUserData btnLike', 'thumb-up', tooltipLike);
        //}

        var tooltipFavorite = globalize.translate('sharedcomponents#Favorite');
        if (userData.IsFavorite) {

            html += getUserDataButtonHtml('markFavorite', itemId, btnCssClass + ' btnUserData btnUserDataOn', iconCssClass, 'favorite', tooltipFavorite, style);
        } else {
            html += getUserDataButtonHtml('markFavorite', itemId, btnCssClass + ' btnUserData', iconCssClass, 'favorite', tooltipFavorite, style);
        }

        return html;
    }

    function markFavorite(link) {

        var id = link.getAttribute('data-itemid');

        var markAsFavorite = !link.classList.contains('btnUserDataOn');

        favorite(id, markAsFavorite);

        if (markAsFavorite) {
            link.classList.add('btnUserDataOn');
        } else {
            link.classList.remove('btnUserDataOn');
        }
    }

    function markLike(link) {

        var id = link.getAttribute('data-itemid');

        if (!link.classList.contains('btnUserDataOn')) {

            likes(id, true);

            link.classList.add('btnUserDataOn');

        } else {

            clearLike(id);

            link.classList.remove('btnUserDataOn');
        }

        link.parentNode.querySelector('.btnDislike').classList.remove('btnUserDataOn');
    }

    function markDislike(link) {

        var id = link.getAttribute('data-itemid');

        if (!link.classList.contains('btnUserDataOn')) {

            likes(id, false);

            link.classList.add('btnUserDataOn');

        } else {

            clearLike(id);

            link.classList.remove('btnUserDataOn');
        }

        link.parentNode.querySelector('.btnLike').classList.remove('btnUserDataOn');
    }

    function markPlayed(link) {

        var id = link.getAttribute('data-itemid');

        if (!link.classList.contains('btnUserDataOn')) {

            played(id, true);

            link.classList.add('btnUserDataOn');

        } else {

            played(id, false);

            link.classList.remove('btnUserDataOn');
        }
    }

    function likes(id, isLiked) {
        var apiClient = connectionManager.currentApiClient();
        return apiClient.updateUserItemRating(apiClient.getCurrentUserId(), id, isLiked);
    }

    function played(id, isPlayed) {
        var apiClient = connectionManager.currentApiClient();

        var method = isPlayed ? 'markPlayed' : 'markUnplayed';

        return apiClient[method](apiClient.getCurrentUserId(), id, new Date());
    }

    function favorite(id, isFavorite) {
        var apiClient = connectionManager.currentApiClient();

        return apiClient.updateFavoriteStatus(apiClient.getCurrentUserId(), id, isFavorite);
    }

    function clearLike(id) {

        var apiClient = connectionManager.currentApiClient();

        return apiClient.clearUserItemRating(apiClient.getCurrentUserId(), id);
    }

    window.UserDataButtons = {
        markPlayed: markPlayed,
        markDislike: markDislike,
        markLike: markLike,
        markFavorite: markFavorite
    };

    return {
        fill: fill,
        getIconsHtml: getIconsHtml
    };

});