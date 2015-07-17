(function ($, document) {

    function getUserViews(userId) {

        var deferred = $.Deferred();

        ApiClient.getUserViews(userId).done(function (result) {

            var items = result.Items;

            deferred.resolveWith(null, [items]);
        });

        return deferred.promise();
    }

    function enableScrollX() {
        return $.browser.mobile && AppInfo.enableAppLayouts;
    }

    function getThumbShape() {
        return enableScrollX() ? 'overflowBackdrop' : 'backdrop';
    }

    function getLibraryButtonsHtml(items) {

        var html = "";

        // "My Library" backgrounds
        for (var i = 0, length = items.length; i < length; i++) {

            var item = items[i];

            var icon;
            var backgroundColor = 'rgba(82, 181, 75, 0.9)';

            switch (item.CollectionType) {
                case "movies":
                    icon = "fa-film";
                    backgroundColor = 'rgba(176, 94, 81, 0.9)';
                    break;
                case "music":
                    icon = "fa-music";
                    backgroundColor = 'rgba(217, 145, 67, 0.9)';
                    break;
                case "photos":
                    icon = "fa-photo";
                    backgroundColor = 'rgba(127, 0, 0, 0.9)';
                    break;
                case "livetv":
                    icon = "fa-video-camera";
                    backgroundColor = 'rgba(217, 145, 67, 0.9)';
                    break;
                case "tvshows":
                    icon = "fa-video-camera";
                    backgroundColor = 'rgba(77, 88, 164, 0.9)';
                    break;
                case "games":
                    icon = "fa-gamepad";
                    backgroundColor = 'rgba(183, 202, 72, 0.9)';
                    break;
                case "trailers":
                    icon = "fa-film";
                    backgroundColor = 'rgba(176, 94, 81, 0.9)';
                    break;
                case "homevideos":
                    icon = "fa-video-camera";
                    backgroundColor = 'rgba(110, 52, 32, 0.9)';
                    break;
                case "musicvideos":
                    icon = "fa-video-camera";
                    backgroundColor = 'rgba(143, 54, 168, 0.9)';
                    break;
                case "books":
                    icon = "fa-book";
                    break;
                case "channels":
                    icon = "fa-globe";
                    backgroundColor = 'rgba(51, 136, 204, 0.9)';
                    break;
                case "playlists":
                    icon = "fa-list";
                    break;
                default:
                    icon = "fa-folder-o";
                    break;
            }

            var cssClass = 'card smallBackdropCard buttonCard';

            if (item.CollectionType) {
                cssClass += ' ' + item.CollectionType + 'buttonCard';
            }

            var href = item.url || LibraryBrowser.getHref(item);

            html += '<a data-itemid="' + item.Id + '" class="' + cssClass + '" href="' + href + '">';
            html += '<div class="cardBox" style="background-color:' + backgroundColor + ';margin:4px;border-radius:4px;">';

            html += "<div class='cardText' style='padding:8px 10px;color:#fff;font-size:14px;'>";
            html += '<i class="fa ' + icon + '"></i>';
            html += '<span style="margin-left:.7em;">' + item.Name + '</span>';
            html += "</div>";

            html += "</div>";

            html += "</a>";
        }

        return html;
    }

    function loadlibraryButtons(elem, userId, index) {

        return getUserViews(userId).done(function (items) {

            var html = '<br/>';

            if (index) {
                html += '<h1 class="listHeader">' + Globalize.translate('HeaderMyMedia') + '</h1>';
            }
            html += '<div>';
            html += getLibraryButtonsHtml(items);
            html += '</div>';

            elem.innerHTML = html;

            handleLibraryLinkNavigations(elem);
        });
    }

    function loadRecentlyAdded(elem, user, context) {

        var limit = AppInfo.hasLowImageBandwidth ?
         16 :
         20;

        var options = {

            Limit: limit,
            Fields: "PrimaryImageAspectRatio,SyncInfo",
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        return ApiClient.getJSON(ApiClient.getUrl('Users/' + user.Id + '/Items/Latest', options)).done(function (items) {

            var html = '';

            var cardLayout = false;

            if (items.length) {
                html += '<div>';
                html += '<h1 class="listHeader">' + Globalize.translate('HeaderLatestMedia') + '</h1>';

                html += '</div>';

                html += '<div class="itemsContainer">';

                html += LibraryBrowser.getPosterViewHtml({
                    items: items,
                    preferThumb: true,
                    shape: 'backdrop',
                    context: context || 'home',
                    showUnplayedIndicator: false,
                    showChildCountIndicator: true,
                    lazy: true,
                    cardLayout: cardLayout,
                    showTitle: cardLayout,
                    showYear: cardLayout,
                    showDetailsMenu: true
                });
                html += '</div>';
            }

            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);

            $(elem).createCardMenus();
        });
    }

    function loadLatestChannelMedia(elem, userId) {

        var screenWidth = $(window).width();

        var options = {

            Limit: screenWidth >= 2400 ? 10 : (screenWidth >= 1600 ? 10 : (screenWidth >= 1440 ? 8 : (screenWidth >= 800 ? 7 : 6))),
            Fields: "PrimaryImageAspectRatio,SyncInfo",
            Filters: "IsUnplayed",
            UserId: userId
        };

        return ApiClient.getJSON(ApiClient.getUrl("Channels/Items/Latest", options)).done(function (result) {

            var html = '';

            if (result.Items.length) {
                html += '<h1 class="listHeader">' + Globalize.translate('HeaderLatestChannelMedia') + '</h1>';
                html += '<div class="itemsContainer">';
                html += LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    preferThumb: true,
                    shape: 'auto',
                    showTitle: true,
                    centerText: true,
                    lazy: true,
                    showDetailsMenu: true
                });
                html += '</div>';
            }

            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);

            $(elem).createCardMenus();
        });
    }

    function loadLibraryTiles(elem, user, shape, index, autoHideOnMobile, showTitles) {

        return getUserViews(user.Id).done(function (items) {

            var html = '';

            if (autoHideOnMobile) {
                html += '<div class="hiddenSectionOnMobile">';
            } else {
                html += '<div>';
            }

            if (items.length) {

                var cssClass = index !== 0 ? 'listHeader' : 'listHeader';

                html += '<div>';
                html += '<h1 class="' + cssClass + '">' + Globalize.translate('HeaderMyMedia') + '</h1>';

                html += '</div>';

                html += '<div class="homeTopViews">';
                html += LibraryBrowser.getPosterViewHtml({
                    items: items,
                    shape: shape,
                    showTitle: showTitles,
                    centerText: true,
                    lazy: true,
                    autoThumb: true,
                    transition: false
                });
                html += '</div>';
            }

            html += '</div>';

            if (autoHideOnMobile) {
                html += '<div class="hiddenSectionOnNonMobile" style="margin-top:1em;">';
                html += getLibraryButtonsHtml(items);
                html += '</div>';
            }

            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);

            $(elem).createCardMenus({ showDetailsMenu: false });

            handleLibraryLinkNavigations(elem);
        });
    }

    function loadResume(elem, userId) {

        var screenWidth = $(window).width();

        var options = {

            SortBy: "DatePlayed",
            SortOrder: "Descending",
            MediaTypes: "Video",
            Filters: "IsResumable",
            Limit: screenWidth >= 1920 ? 10 : (screenWidth >= 1600 ? 8 : (screenWidth >= 1200 ? 9 : 6)),
            Recursive: true,
            Fields: "PrimaryImageAspectRatio,SyncInfo",
            CollapseBoxSetItems: false,
            ExcludeLocationTypes: "Virtual",
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        return ApiClient.getItems(userId, options).done(function (result) {

            var html = '';

            var cardLayout = AppInfo.hasLowImageBandwidth;

            if (result.Items.length) {
                html += '<h1 class="listHeader">' + Globalize.translate('HeaderResume') + '</h1>';
                if (enableScrollX()) {
                    html += '<div class="hiddenScrollX itemsContainer">';
                } else {
                    html += '<div class="itemsContainer">';
                }
                html += LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    preferThumb: true,
                    shape: getThumbShape(),
                    overlayText: screenWidth >= 800 && !cardLayout,
                    showTitle: true,
                    showParentTitle: true,
                    context: 'home',
                    lazy: true,
                    cardLayout: cardLayout,
                    showDetailsMenu: true,
                    overlayPlayButton: true
                });
                html += '</div>';
            }

            elem.innerHTML = html;

            ImageLoader.lazyChildren(elem);
            $(elem).createCardMenus();
        });
    }

    function handleLibraryLinkNavigations(elem) {

        $('a', elem).on('click', function () {

            var card = this;

            if (!this.classList.contains('card')) {
                card = $(this).parents('.card')[0];
            }

            var textElem = $('.cardText', card);
            var text = textElem.text();

            LibraryMenu.setTitle(text);
        });
    }

    function loadLatestChannelItems(elem, userId, options) {

        options = $.extend(options || {}, {

            UserId: userId,
            SupportsLatestItems: true
        });

        return ApiClient.getJSON(ApiClient.getUrl("Channels", options)).done(function (result) {

            var channels = result.Items;

            var channelsHtml = channels.map(function (c) {

                return '<div id="channel' + c.Id + '"></div>';

            }).join('');

            elem.innerHTML = channelsHtml;

            for (var i = 0, length = channels.length; i < length; i++) {

                var channel = channels[i];

                loadLatestChannelItemsFromChannel(elem, channel, i);
            }

        });
    }

    function loadLatestChannelItemsFromChannel(page, channel, index) {

        var screenWidth = $(window).width();

        var options = {

            Limit: screenWidth >= 1600 ? 10 : (screenWidth >= 1440 ? 5 : (screenWidth >= 800 ? 6 : 6)),
            Fields: "PrimaryImageAspectRatio,SyncInfo",
            Filters: "IsUnplayed",
            UserId: Dashboard.getCurrentUserId(),
            ChannelIds: channel.Id
        };

        ApiClient.getJSON(ApiClient.getUrl("Channels/Items/Latest", options)).done(function (result) {

            var html = '';

            if (result.Items.length) {

                var cssClass = index !== 0 ? 'listHeader' : 'listHeader';

                html += '<div>';
                var text = Globalize.translate('HeaderLatestFromChannel').replace('{0}', channel.Name);
                html += '<h1 style="display:inline-block; vertical-align:middle;" class="' + cssClass + '">' + text + '</h1>';
                html += '<a href="channelitems.html?context=channels&id=' + channel.Id + '" class="clearLink" style="margin-left:2em;"><paper-button raised class="more mini"><span>' + Globalize.translate('ButtonMore') + '</span></paper-button></a>';
                html += '</div>';
            }
            html += '<div class="itemsContainer">';
            html += LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: 'autohome',
                defaultShape: 'square',
                showTitle: true,
                centerText: true,
                context: 'channels',
                lazy: true,
                showDetailsMenu: true
            });
            html += '</div>';

            var elem = page.querySelector('#channel' + channel.Id + '');
            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);

            $(elem).createCardMenus();
        });
    }

    function loadLatestLiveTvRecordings(elem, userId, index) {

        return ApiClient.getLiveTvRecordings({

            userId: userId,
            limit: 5,
            IsInProgress: false

        }).done(function (result) {

            var html = '';

            if (result.Items.length) {

                var cssClass = index !== 0 ? 'listHeader' : 'listHeader';

                html += '<div>';
                html += '<h1 style="display:inline-block; vertical-align:middle;" class="' + cssClass + '">' + Globalize.translate('HeaderLatestTvRecordings') + '</h1>';
                html += '<a href="livetvrecordings.html?context=livetv" class="clearLink" style="margin-left:2em;"><paper-button raised class="more mini"><span>' + Globalize.translate('ButtonMore') + '</span></paper-button></a>';
                html += '</div>';
            }

            var screenWidth = $(window).width();

            html += LibraryBrowser.getPosterViewHtml({
                items: result.Items,
                shape: "autohome",
                showTitle: true,
                showParentTitle: true,
                overlayText: screenWidth >= 600,
                coverImage: true,
                lazy: true,
                showDetailsMenu: true
            });

            elem.innerHTML = html;
            ImageLoader.lazyChildren(elem);
        });
    }

    window.Sections = {
        loadRecentlyAdded: loadRecentlyAdded,
        loadLatestChannelMedia: loadLatestChannelMedia,
        loadLibraryTiles: loadLibraryTiles,
        loadResume: loadResume,
        loadLatestChannelItems: loadLatestChannelItems,
        loadLatestLiveTvRecordings: loadLatestLiveTvRecordings,
        loadlibraryButtons: loadlibraryButtons
    };

})(jQuery, document);