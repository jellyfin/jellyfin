(function ($, document) {

    function getUserViews(userId) {

        var deferred = $.Deferred();

        ApiClient.getUserViews(userId).done(function (result) {

            var items = result.Items;

            deferred.resolveWith(null, [items]);
        });

        return deferred.promise();
    }

    function getLibraryButtonsHtml(items) {

        var html = "";

        // "My Library" backgrounds
        for (var i = 0, length = items.length; i < length; i++) {

            var item = items[i];

            var icon;
            var backgroundColor = 'rgba(82, 181, 75, 0.7)';

            switch (item.CollectionType) {
                case "movies":
                    icon = "fa-film";
                    backgroundColor = 'rgba(176, 94, 81, 0.7)';
                    break;
                case "music":
                    icon = "fa-music";
                    backgroundColor = 'rgba(217, 145, 67, 0.7)';
                    break;
                case "photos":
                    icon = "fa-photo";
                    backgroundColor = 'rgba(127, 0, 0, 0.7)';
                    break;
                case "livetv":
                    icon = "fa-video-camera";
                    backgroundColor = 'rgba(255, 233, 127, 0.7)';
                    break;
                case "tvshows":
                    icon = "fa-video-camera";
                    backgroundColor = 'rgba(77, 88, 164, 0.7)';
                    break;
                case "games":
                    icon = "fa-gamepad";
                    backgroundColor = 'rgba(183, 202, 72, 0.7)';
                    break;
                case "trailers":
                    icon = "fa-film";
                    backgroundColor = 'rgba(176, 94, 81, 0.7)';
                    break;
                case "homevideos":
                    icon = "fa-video-camera";
                    backgroundColor = 'rgba(110, 52, 32, 0.7)';
                    break;
                case "musicvideos":
                    icon = "fa-video-camera";
                    backgroundColor = 'rgba(143, 54, 168, 0.7)';
                    break;
                case "books":
                    icon = "fa-book";
                    break;
                case "channels":
                    icon = "fa-globe";
                    backgroundColor = 'rgba(51, 136, 204, 0.7)';
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

            html += "<div class='cardText' style='padding:7px 10px;color:#fff;'>";
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

            $(elem).html(html);

            handleLibraryLinkNavigations(elem);
        });
    }

    function loadRecentlyAdded(elem, user, context) {

        var limit = AppInfo.hasLowImageBandwidth ?
         16 :
         24;

        var options = {

            Limit: limit,
            Fields: "PrimaryImageAspectRatio,SyncInfo",
            ImageTypeLimit: 1,
            EnableImageTypes: "Primary,Backdrop,Banner,Thumb"
        };

        return ApiClient.getJSON(ApiClient.getUrl('Users/' + user.Id + '/Items/Latest', options)).done(function (items) {

            var html = '';

            if (items.length) {
                html += '<div>';
                html += '<h1 style="display:inline-block; vertical-align:middle;" class="listHeader">' + Globalize.translate('HeaderLatestMedia') + '</h1>';

                if (user.Policy.EnableUserPreferenceAccess) {
                    html += '<a href="mypreferencesdisplay.html" class="accentButton"><i class="fa fa-pencil"></i>' + Globalize.translate('ButtonEdit') + '</a>';
                }

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
                });
                html += '</div>';
            }

            $(elem).html(html).lazyChildren();
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
                    lazy: true
                });
                html += '</div>';
            }

            $(elem).html(html).lazyChildren();
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

                var cssClass = index !== 0 ? 'listHeader' : 'listHeader firstListHeader';

                html += '<div>';
                html += '<h1 style="display:inline-block; vertical-align:middle;" class="' + cssClass + '">' + Globalize.translate('HeaderMyMedia') + '</h1>';

                if (user.Policy.EnableUserPreferenceAccess) {
                    html += '<a href="mypreferencesdisplay.html" class="accentButton"><i class="fa fa-pencil"></i>' + Globalize.translate('ButtonEdit') + '</a>';
                }

                html += '</div>';

                html += '<div class="homeTopViews">';
                html += LibraryBrowser.getPosterViewHtml({
                    items: items,
                    shape: shape,
                    showTitle: showTitles,
                    centerText: true,
                    lazy: true,
                    autoThumb: true
                });
                html += '</div>';
            }

            html += '</div>';

            if (autoHideOnMobile) {
                html += '<div class="hiddenSectionOnNonMobile" style="margin-top:1em;">';
                html += getLibraryButtonsHtml(items);
                html += '</div>';
            }

            $(elem).html(html).lazyChildren().createCardMenus();

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

            if (result.Items.length) {
                html += '<h1 class="listHeader">' + Globalize.translate('HeaderResume') + '</h1>';
                html += '<div>';
                html += LibraryBrowser.getPosterViewHtml({
                    items: result.Items,
                    preferThumb: true,
                    shape: 'backdrop',
                    overlayText: screenWidth >= 600 && !AppInfo.hasLowImageBandwidth,
                    showTitle: true,
                    showParentTitle: true,
                    context: 'home',
                    lazy: true
                });
                html += '</div>';
            }

            $(elem).html(html).lazyChildren();
            $(elem).createCardMenus();
        });
    }

    function handleLibraryLinkNavigations(elem) {

        $('a.posterItem', elem).on('click', function () {

            var textElem = $('.posterItemText span', this);

            if (!textElem.length) {
                textElem = $('.posterItemText', this);
            }
            var text = textElem.html();

            LibraryMenu.setText(text);
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

            $(elem).html(channelsHtml);

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

                var cssClass = index !== 0 ? 'listHeader' : 'listHeader firstListHeader';

                html += '<div>';
                var text = Globalize.translate('HeaderLatestFromChannel').replace('{0}', channel.Name);
                html += '<h1 style="display:inline-block; vertical-align:middle;" class="' + cssClass + '">' + text + '</h1>';
                html += '<a href="channelitems.html?context=channels&id=' + channel.Id + '" data-role="button" data-icon="arrow-r" data-mini="true" data-inline="true" data-iconpos="notext" class="sectionHeaderButton"></a>';
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
                lazy: true
            });
            html += '</div>';

            var elem = $('#channel' + channel.Id + '', page).html(html).lazyChildren().trigger('create');
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

                var cssClass = index !== 0 ? 'listHeader' : 'listHeader firstListHeader';

                html += '<div>';
                html += '<h1 style="display:inline-block; vertical-align:middle;" class="' + cssClass + '">' + Globalize.translate('HeaderLatestTvRecordings') + '</h1>';
                html += '<a href="livetvrecordings.html?context=livetv" data-role="button" data-icon="arrow-r" data-mini="true" data-inline="true" data-iconpos="notext" class="sectionHeaderButton"></a>';
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
                lazy: true
            });

            elem.html(html).lazyChildren().trigger('create');

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

(function ($, document) {

    var defaultFirstSection = 'smalllibrarytiles-automobile';

    function getDefaultSection(index) {

        switch (index) {

            case 0:
                return defaultFirstSection;
            case 1:
                return 'resume';
            case 2:
                return 'latestmedia';
            case 3:
                return '';
            default:
                return '';
        }

    }

    function loadSection(page, user, displayPreferences, index) {

        var userId = user.Id;

        var section = displayPreferences.CustomPrefs['home' + index] || getDefaultSection(index);

        if (section == 'folders') {
            section = defaultFirstSection;
        }

        var showLibraryTileNames = displayPreferences.CustomPrefs.enableLibraryTileNames != '0';

        var elem = $('.section' + index, page);

        if (section == 'latestmedia') {
            return Sections.loadRecentlyAdded(elem, user);
        }
        else if (section == 'librarytiles') {
            return Sections.loadLibraryTiles(elem, user, 'backdrop', index, false, showLibraryTileNames);
        }
        else if (section == 'smalllibrarytiles') {
            return Sections.loadLibraryTiles(elem, user, 'homePageSmallBackdrop', index, false, showLibraryTileNames);
        }
        else if (section == 'smalllibrarytiles-automobile') {
            return Sections.loadLibraryTiles(elem, user, 'homePageSmallBackdrop', index, true, showLibraryTileNames);
        }
        else if (section == 'librarytiles-automobile') {
            return Sections.loadLibraryTiles(elem, user, 'backdrop', index, true, showLibraryTileNames);
        }
        else if (section == 'librarybuttons') {
            return Sections.loadlibraryButtons(elem, userId, index);
        }
        else if (section == 'resume') {
            return Sections.loadResume(elem, userId);
        }

        else if (section == 'latesttvrecordings') {
            return Sections.loadLatestLiveTvRecordings(elem, userId);
        }
        else if (section == 'latestchannelmedia') {
            return Sections.loadLatestChannelMedia(elem, userId);

        } else {

            elem.empty();

            var deferred = DeferredBuilder.Deferred();
            deferred.resolve();
            return deferred.promise();
        }
    }

    function loadSections(page, user, displayPreferences) {

        var i, length;
        var sectionCount = 4;

        var elem = $('.sections', page);

        if (!elem.html().length) {
            var html = '';
            for (i = 0, length = sectionCount; i < length; i++) {

                html += '<div class="homePageSection section' + i + '"></div>';
            }

            elem.html(html);
        }

        var promises = [];

        for (i = 0, length = sectionCount; i < length; i++) {

            promises.push(loadSection(page, user, displayPreferences, i));
        }

        return $.when(promises);
    }

    var homePageDismissValue = '14';
    var homePageTourKey = 'homePageTour';

    function dismissWelcome(page, userId) {

        ApiClient.getDisplayPreferences('home', userId, 'webclient').done(function (result) {

            result.CustomPrefs[homePageTourKey] = homePageDismissValue;
            ApiClient.updateDisplayPreferences('home', result, userId, 'webclient');
        });
    }

    function showWelcomeIfNeeded(page, displayPreferences) {

        if (displayPreferences.CustomPrefs[homePageTourKey] == homePageDismissValue) {
            $('.welcomeMessage', page).hide();
        } else {

            var elem = $('.welcomeMessage', page).show();

            if (displayPreferences.CustomPrefs[homePageTourKey]) {

                $('.tourHeader', elem).html(Globalize.translate('HeaderWelcomeBack'));
                $('.tourButtonText', elem).html(Globalize.translate('ButtonTakeTheTourToSeeWhatsNew'));

            } else {

                $('.tourHeader', elem).html(Globalize.translate('HeaderWelcomeToProjectWebClient'));
                $('.tourButtonText', elem).html(Globalize.translate('ButtonTakeTheTour'));
            }
        }
    }

    function takeTour(page, userId) {

        Dashboard.loadSwipebox().done(function () {

            $.swipebox([
                    { href: 'css/images/tour/web/tourcontent.jpg', title: Globalize.translate('WebClientTourContent') },
                    { href: 'css/images/tour/web/tourmovies.jpg', title: Globalize.translate('WebClientTourMovies') },
                    { href: 'css/images/tour/web/tourmouseover.jpg', title: Globalize.translate('WebClientTourMouseOver') },
                    { href: 'css/images/tour/web/tourtaphold.jpg', title: Globalize.translate('WebClientTourTapHold') },
                    { href: 'css/images/tour/web/tourmysync.png', title: Globalize.translate('WebClientTourMySync') },
                    { href: 'css/images/tour/web/toureditor.png', title: Globalize.translate('WebClientTourMetadataManager') },
                    { href: 'css/images/tour/web/tourplaylist.png', title: Globalize.translate('WebClientTourPlaylists') },
                    { href: 'css/images/tour/web/tourcollections.jpg', title: Globalize.translate('WebClientTourCollections') },
                    { href: 'css/images/tour/web/tourusersettings1.png', title: Globalize.translate('WebClientTourUserPreferences1') },
                    { href: 'css/images/tour/web/tourusersettings2.png', title: Globalize.translate('WebClientTourUserPreferences2') },
                    { href: 'css/images/tour/web/tourusersettings3.png', title: Globalize.translate('WebClientTourUserPreferences3') },
                    { href: 'css/images/tour/web/tourusersettings4.png', title: Globalize.translate('WebClientTourUserPreferences4') },
                    { href: 'css/images/tour/web/tourmobile1.jpg', title: Globalize.translate('WebClientTourMobile1') },
                    { href: 'css/images/tour/web/tourmobile2.png', title: Globalize.translate('WebClientTourMobile2') },
                    { href: 'css/images/tour/enjoy.jpg', title: Globalize.translate('MessageEnjoyYourStay') }
            ], {
                afterClose: function () {
                    dismissWelcome(page, userId);
                    $('.welcomeMessage', page).hide();

                    loadConfigureViewsWelcomeMessage(page, userId);
                },
                hideBarsDelay: 30000
            });
        });
    }

    function loadConfigureViewsWelcomeMessage(page, userId) {

        Dashboard.getCurrentUser().done(function (user) {

            if (user.Policy.EnableUserPreferenceAccess) {
                $('.btnMyPreferences', page).attr('href', 'mypreferencesdisplay.html?userId=' + userId);

                // Need the timeout because previous methods in the chain have popups that will be in the act of closing
                setTimeout(function () {

                    $('.popupConfigureViews', page).popup('open');

                }, 500);
            }
        });
    }

    $(document).on('pageinit', "#indexPage", function () {

        var page = this;

        var userId = Dashboard.getCurrentUserId();

        $('.btnTakeTour', page).on('click', function () {
            takeTour(page, userId);
        });

    }).on('pagebeforeshow', "#indexPage", function () {

        var page = this;

        var userId = Dashboard.getCurrentUserId();

        ApiClient.getDisplayPreferences('home', userId, 'webclient').done(function (result) {

            Dashboard.getCurrentUser().done(function (user) {

                loadSections(page, user, result).done(function () {
                    showWelcomeIfNeeded(page, result);
                });

            });
        });

    });

})(jQuery, document);
